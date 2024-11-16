﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace CnGalWebSite.EventBus.Services
{
    public class EventBusRabbitMQ(IConfiguration configuration, ILogger<EventBusRabbitMQ> logger) : IEventBus, IDisposable
    {
        private readonly IConfiguration _configuration = configuration;
        private readonly ILogger<EventBusRabbitMQ> _logger = logger;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<byte[]>> callbackMapper = new();

        private IConnection _connection;
        private IChannel _channel;
        private string replyQueueName;

        public async Task Init()
        {
            if (_channel == null && _connection == null)
            {
                var factory = new ConnectionFactory
                {
                    HostName = _configuration["EventBus_HostName"],
                    Port = int.Parse(_configuration["EventBus_Port"]),
                    UserName = _configuration["EventBus_UserName"],
                    Password = _configuration["EventBus_Password"],
                };
                _connection = await factory.CreateConnectionAsync();
                _channel = await _connection.CreateChannelAsync();
            }
        }

        public async Task SendMessage<T>(string queue, T message)
        {
            await Init();
            await _channel.QueueDeclareAsync(
                queue: queue,   // 队列名称
                durable: true,     // 是否持久化，true持久化，队列会保存磁盘，服务器重启时可以保证不丢失相关信息
                exclusive: false,   // 是否排他，如果一个队列声明为排他队列，该队列仅对时候次声明它的连接可见，并在连接断开时自动删除
                autoDelete: false,  // 是否自动删除，自动删除的前提是：至少有一个消费者连接到这个队列，之后所有与这个队列连接的消费者都断开时，才会自动删除
                arguments: null     // 设置队列的其他参数
            );

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);
            await _channel.BasicPublishAsync(exchange: "", routingKey: queue, body: body);
        }

        public async Task SubscribeMessages<T>(string queue, Action<T> action)
        {
            await Init();
            await _channel.QueueDeclareAsync(
                queue: queue,   // 队列名称
                durable: true,     // 是否持久化，true持久化，队列会保存磁盘，服务器重启时可以保证不丢失相关信息
                exclusive: false,   // 是否排他，如果一个队列声明为排他队列，该队列仅对时候次声明它的连接可见，并在连接断开时自动删除
                autoDelete: false,  // 是否自动删除，自动删除的前提是：至少有一个消费者连接到这个队列，之后所有与这个队列连接的消费者都断开时，才会自动删除
                arguments: null     // 设置队列的其他参数
            );

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (model, eventArgs) =>
            {
                var body = eventArgs.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                var obj = JsonSerializer.Deserialize<T>(message);
                action(obj);
            };
            await _channel.BasicConsumeAsync(queue: queue, autoAck: true, consumer: consumer);
        }

        /// <summary>
        /// 创建RPC调用服务端
        /// </summary>
        /// <typeparam name="TInput"></typeparam>
        /// <typeparam name="TOutput"></typeparam>
        /// <param name="queue"></param>
        /// <param name="func"></param>
        public async Task CreateRpcServer<TInput, TOutput>(string queue, Func<TInput, Task<TOutput>> func)
        {
            await Init();
            await _channel.QueueDeclareAsync(
                queue: queue,
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            // 限制每次只传递一个事件
            await _channel.BasicQosAsync(0, 1, false);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                TOutput response = default;

                var body = ea.Body.ToArray();
                var props = ea.BasicProperties;

                try
                {
                    // 解析输入并调用处理方法
                    var input = JsonSerializer.Deserialize<TInput>(body);
                    response = await func(input);

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "调用RPC处理方法失败");
                }
                finally
                {
                    try
                    {
                        // 解析输出
                        var json = JsonSerializer.Serialize(response);
                        var responseBytes = Encoding.UTF8.GetBytes(json);
                        // 发布
                        await _channel.BasicPublishAsync("", props.ReplyTo, false, new BasicProperties(), responseBytes);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "回传RPC结果失败");
                    }
                    finally
                    {
                        await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                    }

                }
            };
            await _channel.BasicConsumeAsync(queue: queue, autoAck: false, consumer: consumer);
        }

        /// <summary>
        /// 创建RPC客户端 只调用一次
        /// </summary>
        public async Task CreateRpcClient()
        {
            await Init();
            // declare a server-named queue
            replyQueueName = (await _channel.QueueDeclareAsync()).QueueName;
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                if (!callbackMapper.TryRemove(ea.BasicProperties.CorrelationId, out var tcs))
                    return;
                var body = ea.Body.ToArray();
                tcs.TrySetResult(body);
            };

            await _channel.BasicConsumeAsync(consumer: consumer,
                                   queue: replyQueueName,
                                   autoAck: true);
        }

        /// <summary>
        /// 尝试调用RPC方法
        /// </summary>
        /// <typeparam name="TInput"></typeparam>
        /// <typeparam name="TOutput"></typeparam>
        /// <param name="queue"></param>
        /// <param name="input"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<TOutput> CallRpcAsync<TInput, TOutput>(string queue, TInput input, CancellationToken cancellationToken = default)
        {
            await Init();

            // 创建唯一标识
            BasicProperties props = new BasicProperties();
            var correlationId = Guid.NewGuid().ToString();
            props.CorrelationId = correlationId;
            props.ReplyTo = replyQueueName;


            // 序列化输入
            var json = JsonSerializer.Serialize(input);
            var messageBytes = Encoding.UTF8.GetBytes(json);

            // 添加任务
            var tcs = new TaskCompletionSource<byte[]>();
            callbackMapper.TryAdd(correlationId, tcs);

            // 发布
            await _channel.BasicPublishAsync(string.Empty, queue, true, props, messageBytes);

            cancellationToken.Register(() => callbackMapper.TryRemove(correlationId, out _));


            // 创建解析任务
            var ret = Task.Run(() =>
            {
                // 等待响应完成
                var responseTask = tcs.Task;
                responseTask.Wait(cancellationToken);

                // 输出响应结果
                var response = responseTask.Result;
                var output = JsonSerializer.Deserialize<TOutput>(response);

                return output;
            }, cancellationToken);

            return await ret;
        }

        public void Dispose()
        {
            _connection?.Dispose();
            _connection = null;
            _channel?.Dispose();
            _channel = null;
        }
    }
}
