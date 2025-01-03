﻿using CnGalWebSite.DataModel.Helper;
using CnGalWebSite.PublicToolbox.Models;

using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CnGalWebSite.Shared.Service
{
    public class EventService : IEventService
    {
        public event Action SavaTheme;
        public event Action CleanTempEffectTheme;
        public event Action<bool?, bool?, bool?,string> TempEffectTheme;
        public event Action ToggleMiniMode;
        public event Action<string> ChangeTitle;
        public event Action SwitchEntryStyle;
        public event Action KanbanChanged;
        public event Action UserInfoChanged;
        public event Action UserCommodityChanged;
        public event Action ThemeChanged;

        private readonly IMauiService _mauiService;
        private readonly IJSRuntime JS;
        private readonly ILogger<EventService> _logger;

        public EventService(IMauiService mauiService, IJSRuntime js, ILogger<EventService> logger)
        {
            _mauiService = mauiService;
            JS = js;
            _logger = logger;
        }

        //用户购买商品
        public void OnUserCommodityChanged()
        {
            UserCommodityChanged?.Invoke();
        }

        //用户信息更改
        public void OnUserInfoChanged()
        {
            UserInfoChanged?.Invoke();
        }

        //切换看板娘
        public void OnKanbanChanged()
        {
            KanbanChanged?.Invoke();
        }

        //主题设置修改
        public void OnThemeChanged()
        {
            ThemeChanged?.Invoke();
        }

        //切换词条样式
        public void OnSwitchStyle()
        {
            SwitchEntryStyle?.Invoke();
        }

        /// <summary>
        /// 保存主题设置
        /// </summary>
        public void OnSavaTheme()
        {
            SavaTheme?.Invoke();
        }

        /// <summary>
        /// 修改标题
        /// </summary>
        public void OnChangeTitle(string title)
        {
            ChangeTitle?.Invoke(title);
        }
        /// <summary>
        /// 读取主题设置
        /// </summary>
        public void OnCleanTempEffectTheme()
        {
            CleanTempEffectTheme?.Invoke();
        }

        /// <summary>
        /// 临时设置主题
        /// </summary>
        public void OnTempEffectTheme(bool? isDark, bool? isFullScreen, bool? isTransparent, string themeColor)
        {
            TempEffectTheme?.Invoke(isDark,isFullScreen, isTransparent, themeColor);
        }

        public async Task OpenNewPage(string url)
        {
            if (ToolHelper.IsMaui)
            {
               await _mauiService.OpenNewPage(url);
            }
            else
            {
                try
                {
                    await JS.InvokeAsync<string>("openNewPage", url);
                }
                catch
                {
                    _logger.LogError( "尝试通过JS打开新标签页失败");
                }
               
            }
        }

        public void OnToggleMiniMode()
        {
            ToggleMiniMode?.Invoke();
        }
    }
}
