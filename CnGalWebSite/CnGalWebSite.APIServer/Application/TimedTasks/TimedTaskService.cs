﻿using BootstrapBlazor.Components;
using CnGalWebSite.APIServer.Application.BackUpArchives;
using CnGalWebSite.APIServer.Application.ElasticSearches;
using CnGalWebSite.APIServer.Application.News;
using CnGalWebSite.APIServer.Application.Perfections;
using CnGalWebSite.APIServer.Application.SteamInfors;
using CnGalWebSite.APIServer.Application.Tables;
using CnGalWebSite.APIServer.DataReositories;
using CnGalWebSite.DataModel.Helper;
using CnGalWebSite.DataModel.Model;
using CnGalWebSite.DataModel.ViewModel.TimedTasks;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CnGalWebSite.APIServer.Application.TimedTasks
{
    public class TimedTaskService : ITimedTaskService
    {
        private readonly IRepository<TimedTask, int> _timedTaskRepository;
        private readonly ISteamInforService _steamInforService;
        private readonly ITableService _tableService;
        private readonly IBackUpArchiveService _backUpArchiveService;
        private readonly IPerfectionService _perfectionService;
        private readonly IElasticsearchService _elasticsearchService;
        private readonly INewsService _newsService;

        private static readonly ConcurrentDictionary<Type, Func<IEnumerable<TimedTask>, string, SortOrder, IEnumerable<TimedTask>>> SortLambdaCacheApplicationUser = new();

        public TimedTaskService(IRepository<TimedTask, int> timedTaskRepository, ISteamInforService steamInforService, IBackUpArchiveService backUpArchiveService, ITableService tableService,
            IPerfectionService perfectionService, IElasticsearchService elasticsearchService, INewsService newsService)
        {
            _timedTaskRepository = timedTaskRepository;
            _steamInforService = steamInforService;
            _backUpArchiveService = backUpArchiveService;
            _tableService = tableService;
            _perfectionService = perfectionService;
            _elasticsearchService = elasticsearchService;
            _newsService = newsService;
        }

        public Task<QueryData<ListTimedTaskAloneModel>> GetPaginatedResult(CnGalWebSite.DataModel.ViewModel.Search.QueryPageOptions options, ListTimedTaskAloneModel searchModel)
        {
            IEnumerable<TimedTask> items = _timedTaskRepository.GetAll().AsNoTracking();
            // 处理高级搜索
            if (!string.IsNullOrWhiteSpace(searchModel.Name))
            {
                items = items.Where(item => item.Name?.Contains(searchModel.Name, StringComparison.OrdinalIgnoreCase) ?? false);
            }
            if (searchModel.Type != null)
            {
                items = items.Where(item => item.Type == searchModel.Type);
            }
            if (searchModel.ExecuteType != null)
            {
                items = items.Where(item => item.ExecuteType == searchModel.ExecuteType);
            }




            // 处理 SearchText 模糊搜索
            if (!string.IsNullOrWhiteSpace(options.SearchText))
            {
                items = items.Where(item => (item.Name?.Contains(options.SearchText) ?? false));
            }


            // 排序
            var isSorted = false;
            if (!string.IsNullOrWhiteSpace(options.SortName))
            {
                // 外部未进行排序，内部自动进行排序处理
                var invoker = SortLambdaCacheApplicationUser.GetOrAdd(typeof(TimedTask), key => LambdaExtensions.GetSortLambda<TimedTask>().Compile());
                items = invoker(items, options.SortName, (BootstrapBlazor.Components.SortOrder)options.SortOrder);
                isSorted = true;
            }

            // 设置记录总数
            var total = items.Count();

            // 内存分页
            items = items.Skip((options.PageIndex - 1) * options.PageItems).Take(options.PageItems).ToList();

            //复制数据
            var resultItems = new List<ListTimedTaskAloneModel>();
            foreach (var item in items)
            {
                resultItems.Add(new ListTimedTaskAloneModel
                {
                    Id = item.Id,
                    Name = item.Name,
                    Type = item.Type,
                    ExecuteType = item.ExecuteType,
                    IntervalTime = item.IntervalTime,
                    EveryTime = item.EveryTime?.ToString("yyyy-MM-dd HH:mm"),
                    LastExecutedTime = item.LastExecutedTime,
                    Parameter = item.Parameter,
                    IsPause = item.IsPause,
                    IsRuning = item.IsRuning,
                    IsLastFail = item.IsLastFail
                });
            }

            return Task.FromResult(new QueryData<ListTimedTaskAloneModel>()
            {
                Items = resultItems,
                TotalCount = total,
                IsSorted = isSorted,
                // IsFiltered = isFiltered
            });
        }

        public async Task RunTimedTask(TimedTask item)
        {
            if (item.IsRuning == true || item.IsPause == true)
            {
                return;
            }
            try
            {

                //更新执行状态
                _timedTaskRepository.Clear();
                item = await _timedTaskRepository.FirstOrDefaultAsync(s => s.Id == item.Id);
                if (item != null)
                {
                    item.IsRuning = true;
                    await _timedTaskRepository.UpdateAsync(item);
                }
                //根据不同类型任务进行调用
                switch (item.Type)
                {
                    case TimedTaskType.UpdateGameSteamInfor:
                        await _steamInforService.UpdateAllGameSteamInfor();
                        break;
                    case TimedTaskType.UpdateUserSteamInfor:
                        await _steamInforService.UpdateAllUserSteamInfor();
                        break;
                    case TimedTaskType.BackupEntry:
                        var maxNum = 10;
                        try
                        {
                            if (string.IsNullOrWhiteSpace(item.Parameter) == false)
                            {
                                maxNum = int.Parse(item.Parameter);
                            }
                        }
                        catch { }
                        await _backUpArchiveService.BackUpAllEntries(maxNum);
                        break;
                    case TimedTaskType.BackupArticle:
                        maxNum = 10;
                        try
                        {
                            if (string.IsNullOrWhiteSpace(item.Parameter) == false)
                            {
                                maxNum = int.Parse(item.Parameter);
                            }
                        }
                        catch { }
                        await _backUpArchiveService.BackUpAllArticles(maxNum);
                        break;
                    case TimedTaskType.UpdateDataSummary:
                        await _tableService.UpdateAllInforListAsync();
                        break;
                    case TimedTaskType.UpdateSitemap:
                        await _backUpArchiveService.UpdateSitemap();
                        break;
                    case TimedTaskType.UpdatePerfectionCountAndVictoryPercentage:
                        await _perfectionService.UpdatePerfectionCountAndVictoryPercentage();
                        break;
                    case TimedTaskType.UpdatePerfectionOverview:
                        await _perfectionService.UpdatePerfectionOverview();
                        break;
                    case TimedTaskType.UpdatePerfection:
                        await _perfectionService.UpdateAllEntryPerfectionsAsync();
                        break;
                    case TimedTaskType.UpdateDataToElasticsearch:
                        await _elasticsearchService.UpdateDataToElasticsearch(item.LastExecutedTime ?? DateTime.MinValue);
                        break;
                    case TimedTaskType.UpdateGameNews:
                        await _newsService.UpdateNewestGameNews();
                        break;
                    case TimedTaskType.UpdateWeiboUserInfor:
                        await _newsService.UpdateWeiboUserInforCache();
                        break;
                }
                //记录执行时间
                _timedTaskRepository.Clear();
                item = await _timedTaskRepository.FirstOrDefaultAsync(s => s.Id == item.Id);
                if (item != null)
                {
                    item.IsLastFail = false;
                    item.IsRuning = false;
                    item.LastExecutedTime = DateTime.Now.ToCstTime();
                    await _timedTaskRepository.UpdateAsync(item);
                }
            }
            catch
            {
                _timedTaskRepository.Clear();
                item = await _timedTaskRepository.FirstOrDefaultAsync(s => s.Id == item.Id);
                if (item != null)
                {
                    item.IsLastFail = false;
                    item.IsRuning = false;
                    item.IsLastFail = true;
                    item.LastExecutedTime = DateTime.Now.ToCstTime();
                    await _timedTaskRepository.UpdateAsync(item);
                }
            }
            finally
            {

            }
        }

    }
}
