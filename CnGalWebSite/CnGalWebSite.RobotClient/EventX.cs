﻿using CnGalWebSite.DataModel.Helper;
using CnGalWebSite.DataModel.Model;
using CnGalWebSite.Helper.Extensions;
using CnGalWebSite.Helper.Helper;
using Newtonsoft.Json;
using System.Net.Http.Json;
using File = System.IO.File;

namespace CnGalWebSite.RobotClient
{
    public class EventX
    {
        public List<RobotEvent> Events { get; set; } = new List<RobotEvent>();
        private List<EventExecuteInfor> ExecuteInfors { get; set; } = new List<EventExecuteInfor>();
        private readonly Setting _setting;
        private readonly List<MessageArg> _messageArgs;
        private readonly HttpClient _httpClient;


        public EventX(Setting setting, HttpClient client, List<MessageArg> messageArgs)
        {
            _setting = setting;
            _httpClient = client;
            _messageArgs = messageArgs;
        }


        public void Init()
        {
            LoadEvents();
            LoadExecuteInfors();
        }

        public void LoadEvents()
        {
            try
            {
                var path = Path.Combine(_setting.RootPath, "Events.json");

                using (var file = File.OpenText(path))
                {
                    var serializer = new JsonSerializer();
                    Events = (List<RobotEvent>)serializer.Deserialize(file, typeof(List<RobotEvent>));
                    file.Close();
                    file.Dispose();
                }
            }
            catch
            {
                SaveEvents();
            }

        }

        public void SaveEvents()
        {
            var path = Path.Combine(_setting.RootPath, "Events.json");

            using (var file = File.CreateText(path))
            {
                var serializer = new JsonSerializer();
                serializer.Serialize(file, Events);
                file.Close();
                file.Dispose();
            }
        }
        public void LoadExecuteInfors()
        {
            try
            {
                var path = Path.Combine(_setting.RootPath, "ExecuteInfors.json");

                using (var file = File.OpenText(path))
                {
                    var serializer = new JsonSerializer();
                    ExecuteInfors = (List<EventExecuteInfor>)serializer.Deserialize(file, typeof(List<EventExecuteInfor>));
                    file.Close();
                    file.Dispose();
                }
            }
            catch
            {
                SaveExecuteInfors();
            }

        }

        public void SaveExecuteInfors()
        {
            var path = Path.Combine(_setting.RootPath, "ExecuteInfors.json");

            using (var file = File.CreateText(path))
            {
                var serializer = new JsonSerializer();
                serializer.Serialize(file, ExecuteInfors);
                file.Close();
                file.Dispose();
            }
        }

        public string GetCurrentTimeEvent()
        {
            var events = Events.Where(s => s.Time.TimeOfDay < DateTime.Now.ToCstTime().TimeOfDay && s.Time.AddSeconds(s.DelaySecond).TimeOfDay > DateTime.Now.ToCstTime().TimeOfDay && s.IsHidden == false && s.Type == RobotEventType.FixedTime);

            var todos = new List<RobotEvent>();

            foreach (var item in events)
            {
                if (ExecuteInfors.Any(s => (s.Id == item.Id && s.LastRunTime.Date >= DateTime.Now.ToCstTime().Date) || (s.LastRunTime.Date == DateTime.Now.ToCstTime().Date && s.Note == item.Note)))
                {
                    continue;
                }

                todos.Add(item);
            }

            if (todos.Count == 0)
            {
                return null;
            }

            todos.Random();

            RobotEvent currentEvent = null;

            foreach (var item in todos)
            {
                //查找同类型的任务
                var sameCount = Events.Where(s => s.Note == item.Note && s.Time.TimeOfDay >= item.Time.TimeOfDay).Count();
                if (new Random().Next(0, sameCount) == 0)
                {
                    currentEvent = item;
                    break;
                }

            }
            if(currentEvent==null)
            {
                return null;
            }
           
           
                ExecuteInfors.Add(new EventExecuteInfor
                {
                    Id = currentEvent.Id,
                    Note = currentEvent.Note,
                    LastRunTime = DateTime.Now.ToCstTime(),
                });
            

            SaveExecuteInfors();

            return currentEvent?.Text;

        }

        public string GetProbabilityEvents()
        {
            var p = new Random().NextDouble();

            var events = Events.Where(s => s.Time.TimeOfDay < DateTime.Now.ToCstTime().TimeOfDay && s.Time.AddSeconds(s.DelaySecond).TimeOfDay > DateTime.Now.ToCstTime().TimeOfDay && s.IsHidden == false && s.Type == RobotEventType.PreTime && s.Probability / 100 > p);

            var todos = new List<RobotEvent>();

            foreach (var item in events)
            {
                if (ExecuteInfors.Any(s => s.LastRunTime.Date == DateTime.Now.ToCstTime().Date && s.Id == item.Id))
                {
                    continue;
                }

                todos.Add(item);
            }

            if (todos.Count == 0)
            {
                return null;
            }


            todos.Random();

            var temp = todos.FirstOrDefault();
            ExecuteInfors.Add(new EventExecuteInfor
            {
                Id = temp.Id,
                Note = temp.Note,
                LastRunTime = DateTime.Now.ToCstTime(),
            });
            SaveExecuteInfors();

            return temp.Text;
        }

        public async Task RefreshAsync()
        {
            try
            {
                var model = await _httpClient.GetFromJsonAsync<List<RobotEvent>>(ToolHelper.WebApiPath + "api/robot/getrobotEvents");
                var name = _messageArgs.FirstOrDefault(s => s.Name == "name")?.Value;
                foreach (var reply in model)
                {
                    reply.Text = reply.Text.Replace("$(name)", name);
                }

                Events.Clear();
                Events.AddRange(model);
                SaveEvents();
            }
            catch (Exception ex)
            {
                OutputHelper.PressError(ex, "无法获取事件列表");
            }
        }
    }

    public class EventExecuteInfor
    {
        public long Id { get; set; }

        public string Note { get; set; }

        public DateTime LastRunTime { get; set; }
    }
}
