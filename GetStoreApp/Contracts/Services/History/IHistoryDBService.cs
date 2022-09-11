﻿using GetStoreApp.Models.History;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GetStoreApp.Contracts.Services.History
{
    public interface IHistoryDBService
    {
        Task AddAsync(HistoryModel history);

        Task<Tuple<List<HistoryModel>, bool, bool>> QueryAllAsync(bool timeSortOrder = false, string typeFilter = "None", string channelFilter = "None");

        Task<List<HistoryModel>> QueryAsync(int value);

        Task<bool> DeleteAsync(List<HistoryModel> selectedHistoryDataList);

        Task<bool> ClearAsync();
    }
}