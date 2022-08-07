﻿using GetStoreApp.Contracts.Services.App;
using GetStoreApp.Contracts.Services.Settings;
using GetStoreApp.Helpers;
using GetStoreApp.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace GetStoreApp.Services.Settings
{
    /// <summary>
    /// 区域设置服务
    /// </summary>
    public class RegionService : IRegionService
    {
        private string SettingsKey { get; init; } = "AppRegion";

        private RegionModel DefaultAppRegion { get; set; }

        public RegionModel AppRegion { get; set; }

        private IConfigStorageService ConfigStorageService { get; set; } = IOCHelper.GetService<IConfigStorageService>();

        public List<RegionModel> RegionList { get; set; } = GeographicalLocationHelper.GetGeographicalLocations().OrderBy(item => item.FriendlyName).ToList();

        /// <summary>
        /// 应用在初始化前获取设置存储的区域值
        /// </summary>
        public async Task InitializeRegionAsync()
        {
            DefaultAppRegion = RegionList.Find(item => item.ISO2 == RegionInfo.CurrentRegion.TwoLetterISORegionName);

            AppRegion = await GetRegionAsync();
        }

        /// <summary>
        /// 获取设置存储的区域值，如果设置没有存储，使用默认值
        /// </summary>
        private async Task<RegionModel> GetRegionAsync()
        {
            string region = await ConfigStorageService.GetSettingStringValueAsync(SettingsKey);

            if (string.IsNullOrEmpty(region))
            {
                return RegionList.Find(item => item.ISO2.Equals(DefaultAppRegion.ISO2, StringComparison.OrdinalIgnoreCase));
            }

            return RegionList.Find(item => item.ISO2.Equals(region, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 应用区域发生修改时修改设置存储的主题值
        /// </summary>
        public async Task SetRegionAsync(RegionModel region)
        {
            AppRegion = region;

            await ConfigStorageService.SaveSettingStringValueAsync(SettingsKey, region.ISO2);
        }
    }
}
