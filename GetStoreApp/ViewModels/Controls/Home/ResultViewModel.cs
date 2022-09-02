﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GetStoreApp.Helpers;
using GetStoreApp.Messages;
using GetStoreApp.Models;
using GetStoreApp.UI.Dialogs;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GetStoreApp.ViewModels.Controls.Home
{
    public class ResultViewModel : ObservableRecipient
    {
        // 临界区资源访问互斥锁
        private readonly object ResultDataListLock = new object();

        public ObservableCollection<ResultModel> ResultDataList { get; } = new ObservableCollection<ResultModel>();

        private bool _resultCotnrolVisable = false;

        public bool ResultControlVisable
        {
            get { return _resultCotnrolVisable; }

            set { SetProperty(ref _resultCotnrolVisable, value); }
        }

        private string _categoryId;

        public string CategoryId
        {
            get { return _categoryId; }

            set { SetProperty(ref _categoryId, value); }
        }

        private bool _isSelectMode = false;

        public bool IsSelectMode
        {
            get { return _isSelectMode; }

            set { SetProperty(ref _isSelectMode, value); }
        }

        // 复制CategoryID
        public IAsyncRelayCommand CopyIDCommand => new AsyncRelayCommand(async () =>
        {
            CopyPasteHelper.CopyToClipBoard(CategoryId);
            WeakReferenceMessenger.Default.Send(new InAppNotificationMessage(new InAppNotificationModel
            {
                NotificationContent = "ResultIDCopy",
                NotificationValue = new object[] { true }
            }));

            await Task.CompletedTask;
        });

        // 进入多选模式
        public IAsyncRelayCommand SelectCommand => new AsyncRelayCommand(async () =>
        {
            // 保证线程安全
            lock (ResultDataListLock)
            {
                foreach (ResultModel resultItem in ResultDataList)
                {
                    resultItem.IsSelected = false;
                }
            }

            IsSelectMode = true;
            await Task.CompletedTask;
        });

        // 全选
        public IAsyncRelayCommand SelectAllCommand => new AsyncRelayCommand(async () =>
        {
            // 保证线程安全
            lock (ResultDataListLock)
            {
                foreach (ResultModel resultItem in ResultDataList)
                {
                    resultItem.IsSelected = true;
                }
            }

            await Task.CompletedTask;
        });

        // 全部不选
        public IAsyncRelayCommand SelectNoneCommand => new AsyncRelayCommand(async () =>
        {
            // 保证线程安全
            lock (ResultDataListLock)
            {
                foreach (ResultModel resultItem in ResultDataList)
                {
                    resultItem.IsSelected = false;
                }
            }

            await Task.CompletedTask;
        });

        // 复制选定项目的内容
        public IAsyncRelayCommand CopySelectedCommand => new AsyncRelayCommand(async () =>
        {
            List<ResultModel> SelectedResultDataList = ResultDataList.Where(item => item.IsSelected == true).ToList();

            // 内容为空时显示空提示对话框
            if (SelectedResultDataList.Count == 0)
            {
                await new SelectEmptyPromptDialog().ShowAsync();
                return;
            };

            StringBuilder stringBuilder = new StringBuilder();

            foreach (ResultModel resultItem in SelectedResultDataList)
            {
                stringBuilder.Append(string.Format("[\n{0}\n{1}\n{2}\n{3}\n]\n",
                    resultItem.FileName,
                    resultItem.FileLink,
                    resultItem.FileSHA1,
                    resultItem.FileSize)
                    );
            }

            CopyPasteHelper.CopyToClipBoard(stringBuilder.ToString());

            WeakReferenceMessenger.Default.Send(new InAppNotificationMessage(new InAppNotificationModel
            {
                NotificationContent = "ResultContentCopy",
                NotificationValue = new object[] { true, true, SelectedResultDataList.Count }
            }));

            await Task.CompletedTask;
        });

        // 复制选定项目的链接
        public IAsyncRelayCommand CopySelectedLinkCommand => new AsyncRelayCommand(async () =>
        {
            List<ResultModel> SelectedResultDataList = ResultDataList.Where(item => item.IsSelected == true).ToList();

            // 内容为空时显示空提示对话框
            if (SelectedResultDataList.Count == 0)
            {
                await new SelectEmptyPromptDialog().ShowAsync();
                return;
            };

            StringBuilder stringBuilder = new StringBuilder();

            foreach (ResultModel resultItem in SelectedResultDataList)
            {
                stringBuilder.Append(string.Format("{0}\n", resultItem.FileLink));
            }

            CopyPasteHelper.CopyToClipBoard(stringBuilder.ToString());

            WeakReferenceMessenger.Default.Send(new InAppNotificationMessage(new InAppNotificationModel
            {
                NotificationContent = "ResultLinkCopy",
                NotificationValue = new object[] { true, true, SelectedResultDataList.Count }
            }));

            await Task.CompletedTask;
        });

        // 下载选定项目
        public IAsyncRelayCommand DownloadSelectedCommand { get; }

        // 退出多选模式
        public IAsyncRelayCommand CancelCommand => new AsyncRelayCommand(async () =>
        {
            IsSelectMode = false;
            await Task.CompletedTask;
        });

        // 在多选模式下点击项目选择相应的条目
        public IAsyncRelayCommand ItemClickCommand => new AsyncRelayCommand<ItemClickEventArgs>(async (param) =>
        {
            ResultModel resultItem = (ResultModel)param.ClickedItem;
            int ClickedIndex = ResultDataList.IndexOf(resultItem);

            lock (ResultDataListLock)
            {
                ResultDataList[ClickedIndex].IsSelected = !ResultDataList[ClickedIndex].IsSelected;
            }

            await Task.CompletedTask;
        });

        // 根据设置存储的文件链接操作方式操作获取到的文件链接
        public IAsyncRelayCommand FileOperationCommand => new AsyncRelayCommand<string>(async (param) =>
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri(param));
        });

        // 复制指定项目的链接
        public IAsyncRelayCommand CopyLinkCommand => new AsyncRelayCommand<string>(async (param) =>
        {
            CopyPasteHelper.CopyToClipBoard(param);

            WeakReferenceMessenger.Default.Send(new InAppNotificationMessage(new InAppNotificationModel
            {
                NotificationContent = "ResultLinkCopy",
                NotificationValue = new object[] { true, false }
            }));

            await Task.CompletedTask;
        });

        // 复制指定项目的内容
        public IAsyncRelayCommand CopyContentCommand => new AsyncRelayCommand<ResultModel>(async (param) =>
        {
            string CopyContent = string.Format("[\n{0}\n{1}\n{2}\n{3}\n]\n",
                param.FileName,
                param.FileLink,
                param.FileSHA1,
                param.FileSize
                );

            CopyPasteHelper.CopyToClipBoard(CopyContent);

            WeakReferenceMessenger.Default.Send(new InAppNotificationMessage(new InAppNotificationModel()
            {
                NotificationContent = "ResultContentCopy",
                NotificationValue = new object[] { true, false }
            }));

            await Task.CompletedTask;
        });

        public ResultViewModel()
        {
            WeakReferenceMessenger.Default.Register<ResultViewModel, ResultControlVisableMessage>(this, (resultViewModel, resultControlVisableMessage) =>
            {
                resultViewModel.ResultControlVisable = resultControlVisableMessage.Value;
            });

            WeakReferenceMessenger.Default.Register<ResultViewModel, ResultCategoryIdMessage>(this, (resultViewModel, resultCategoryIdMessage) =>
            {
                resultViewModel.CategoryId = resultCategoryIdMessage.Value;
            });

            WeakReferenceMessenger.Default.Register<ResultViewModel, ResultDataListMessage>(this, async (resultViewModel, resultDataListMessage) =>
            {
                lock (ResultDataListLock)
                {
                    resultViewModel.ResultDataList.Clear();
                }

                lock (ResultDataListLock)
                {
                    foreach (ResultModel resultItem in resultDataListMessage.Value)
                    {
                        resultItem.IsSelected = false;
                        resultViewModel.ResultDataList.Add(resultItem);
                    }
                }

                await Task.CompletedTask;
            });
        }
    }
}
