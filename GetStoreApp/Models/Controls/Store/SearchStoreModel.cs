﻿namespace GetStoreApp.Models.Controls.Store
{
    /// <summary>
    /// 查询链接返回结果的数据模型
    /// </summary>
    public class SearchStoreModel
    {
        /// <summary>
        /// 应用包名称
        /// </summary>
        public string StoreAppName { get; set; }

        /// <summary>
        /// 应用发布者
        /// </summary>
        public string StoreAppPublisher { get; set; }

        /// <summary>
        /// 应用链接
        /// </summary>
        public string StoreAppLink { get; set; }
    }
}
