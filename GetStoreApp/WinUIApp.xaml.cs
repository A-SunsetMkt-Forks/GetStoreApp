﻿using GetStoreApp.Extensions.DataType.Constant;
using GetStoreApp.Helpers.Root;
using GetStoreApp.Services.Controls.Download;
using GetStoreApp.Services.Controls.Settings;
using GetStoreApp.Services.Root;
using GetStoreApp.Views.Windows;
using GetStoreApp.WindowsAPI.PInvoke.Kernel32;
using GetStoreApp.WindowsAPI.PInvoke.User32;
using Microsoft.UI.Xaml;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Foundation.Diagnostics;
using Windows.Graphics;
using Windows.UI.StartScreen;

namespace GetStoreApp
{
    /// <summary>
    /// 获取商店应用程序
    /// </summary>
    public partial class App : Application, IDisposable
    {
        private bool isDisposed;

        public bool IsAppRunning { get; set; } = true;

        public bool IsAppLaunched { get; set; } = false;

        public MainWindow MainWindow { get; private set; }

        public JumpList TaskbarJumpList { get; private set; }

        public App()
        {
            InitializeComponent();
            UnhandledException += OnUnhandledException;
        }

        /// <summary>
        /// 应用启动后执行其他操作
        /// </summary>
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            base.OnLaunched(args);
            ResourceDictionaryHelper.InitializeResourceDictionary();

            MainWindow = new MainWindow();
            IsAppLaunched = true;
            ActivateWindow();

            InitializeJumpList();
            Startup();
        }

        /// <summary>
        /// 初始化任务栏的跳转列表
        /// </summary>
        private void InitializeJumpList()
        {
            if (JumpList.IsSupported())
            {
                Task.Run(async () =>
                {
                    TaskbarJumpList = await JumpList.LoadCurrentAsync();
                    TaskbarJumpList.Items.Clear();
                    TaskbarJumpList.SystemGroupKind = JumpListSystemGroupKind.None;

                    JumpListItem storeItem = JumpListItem.CreateWithArguments("JumpList Store", ResourceService.GetLocalized("Window/Store"));
                    storeItem.Logo = new Uri("ms-appx:///Assets/ControlIcon/Store.png");
                    TaskbarJumpList.Items.Add(storeItem);

                    JumpListItem historyItem = JumpListItem.CreateWithArguments("JumpList History", ResourceService.GetLocalized("Window/History"));
                    historyItem.Logo = new Uri("ms-appx:///Assets/ControlIcon/History.png");
                    TaskbarJumpList.Items.Add(historyItem);

                    JumpListItem downloadItem = JumpListItem.CreateWithArguments("JumpList Download", ResourceService.GetLocalized("Window/Download"));
                    downloadItem.Logo = new Uri("ms-appx:///Assets/ControlIcon/Download.png");
                    TaskbarJumpList.Items.Add(downloadItem);

                    TaskbarJumpList.Items.Add(JumpListItem.CreateSeparator());

                    JumpListItem wingetItem = JumpListItem.CreateWithArguments("JumpList WinGet", ResourceService.GetLocalized("Window/WinGet"));
                    wingetItem.Logo = new Uri("ms-appx:///Assets/ControlIcon/WinGet.png");
                    TaskbarJumpList.Items.Add(wingetItem);

                    JumpListItem uwpAppItem = JumpListItem.CreateWithArguments("JumpList UWPApp", ResourceService.GetLocalized("Window/UWPApp"));
                    uwpAppItem.Logo = new Uri("ms-appx:///Assets/ControlIcon/UWPApp.png");
                    TaskbarJumpList.Items.Add(uwpAppItem);

                    TaskbarJumpList.Items.Add(JumpListItem.CreateSeparator());

                    JumpListItem webItem = JumpListItem.CreateWithArguments("JumpList Web", ResourceService.GetLocalized("Window/Web"));
                    webItem.Logo = new Uri("ms-appx:///Assets/ControlIcon/Web.png");
                    TaskbarJumpList.Items.Add(webItem);

                    await TaskbarJumpList.SaveAsync();
                });
            }
        }

        /// <summary>
        /// 处理应用程序未知异常处理
        /// </summary>
        private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs args)
        {
            args.Handled = true;

            // 系统背景色弹出的异常，不进行处理
            if (args.Exception.HResult is -2147024809 && args.Exception.StackTrace.Contains("SystemBackdropConfiguration"))
            {
                LogService.WriteLog(LoggingLevel.Warning, "System backdrop config warning.", args.Exception);
                return;
            }
            // 处理其他异常
            else
            {
                LogService.WriteLog(LoggingLevel.Error, "Unknown unhandled exception.", args.Exception);

                // 退出应用
                Dispose();
            }
        }

        /// <summary>
        /// 窗口激活前进行配置
        /// </summary>
        public void ActivateWindow()
        {
            bool? IsWindowMaximized = ConfigService.ReadSetting<bool?>(ConfigKey.IsWindowMaximizedKey);
            int? WindowWidth = ConfigService.ReadSetting<int?>(ConfigKey.WindowWidthKey);
            int? WindowHeight = ConfigService.ReadSetting<int?>(ConfigKey.WindowHeightKey);
            int? WindowPositionXAxis = ConfigService.ReadSetting<int?>(ConfigKey.WindowPositionXAxisKey);
            int? WindowPositionYAxis = ConfigService.ReadSetting<int?>(ConfigKey.WindowPositionYAxisKey);

            if (IsWindowMaximized.HasValue && IsWindowMaximized.Value is true)
            {
                MainWindow.MaximizeOrRestore();
            }
            else
            {
                if (WindowWidth.HasValue && WindowHeight.HasValue && WindowPositionXAxis.HasValue && WindowPositionYAxis.HasValue)
                {
                    MainWindow.AppWindow.MoveAndResize(new RectInt32(
                        WindowPositionXAxis.Value,
                        WindowPositionYAxis.Value,
                        WindowWidth.Value,
                        WindowHeight.Value
                        ));
                }
            }

            MainWindow.AppWindow.SetIcon("Assets/Logo.ico");
            MainWindow.AppWindow.Show();
        }

        /// <summary>
        /// 窗口激活后配置其他设置
        /// </summary>
        public void Startup()
        {
            // 设置应用主题
            ThemeService.SetWindowTheme();

            // 设置应用背景色
            BackdropService.SetAppBackdrop();

            // 设置应用置顶状态
            TopMostService.SetAppTopMost();

            // 初始化 WinGet 程序包安装信息
            WinGetService.InitializeService();

            // 初始化下载监控服务
            DownloadSchedulerService.InitializeDownloadScheduler();

            // 初始化Aria2配置文件信息
            Aria2Service.InitializeAria2Conf();

            // 启动Aria2下载服务
            Aria2Service.StartAria2Process();
        }

        /// <summary>
        /// 关闭窗口时保存窗口的大小和位置信息
        /// </summary>
        private void SaveWindowInformation()
        {
            ConfigService.SaveSetting(ConfigKey.IsWindowMaximizedKey, MainWindow.IsWindowMaximized);
            ConfigService.SaveSetting(ConfigKey.WindowWidthKey, MainWindow.AppWindow.Size.Width);
            ConfigService.SaveSetting(ConfigKey.WindowHeightKey, MainWindow.AppWindow.Size.Height);
            ConfigService.SaveSetting(ConfigKey.WindowPositionXAxisKey, MainWindow.AppWindow.Position.X);
            ConfigService.SaveSetting(ConfigKey.WindowPositionYAxisKey, MainWindow.AppWindow.Position.Y);
        }

        /// <summary>
        /// 重启应用
        /// </summary>
        public void Restart()
        {
            MainWindow.AppWindow.Hide();

            Kernel32Library.GetStartupInfo(out STARTUPINFO WinGetProcessStartupInfo);
            WinGetProcessStartupInfo.lpReserved = IntPtr.Zero;
            WinGetProcessStartupInfo.lpDesktop = IntPtr.Zero;
            WinGetProcessStartupInfo.lpTitle = IntPtr.Zero;
            WinGetProcessStartupInfo.dwX = 0;
            WinGetProcessStartupInfo.dwY = 0;
            WinGetProcessStartupInfo.dwXSize = 0;
            WinGetProcessStartupInfo.dwYSize = 0;
            WinGetProcessStartupInfo.dwXCountChars = 500;
            WinGetProcessStartupInfo.dwYCountChars = 500;
            WinGetProcessStartupInfo.dwFlags = STARTF.STARTF_USESHOWWINDOW;
            WinGetProcessStartupInfo.wShowWindow = WindowShowStyle.SW_SHOWNORMAL;
            WinGetProcessStartupInfo.cbReserved2 = 0;
            WinGetProcessStartupInfo.lpReserved2 = IntPtr.Zero;
            WinGetProcessStartupInfo.cb = Marshal.SizeOf(typeof(STARTUPINFO));

            bool createResult = Kernel32Library.CreateProcess(null, "GetStoreApp.exe Restart", IntPtr.Zero, IntPtr.Zero, false, CreateProcessFlags.None, IntPtr.Zero, null, ref WinGetProcessStartupInfo, out PROCESS_INFORMATION WinGetProcessInformation);

            if (createResult)
            {
                if (WinGetProcessInformation.hProcess != IntPtr.Zero) Kernel32Library.CloseHandle(WinGetProcessInformation.hProcess);
                if (WinGetProcessInformation.hThread != IntPtr.Zero) Kernel32Library.CloseHandle(WinGetProcessInformation.hThread);
            }

            Dispose();
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~App()
        {
            Dispose(false);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    IsAppRunning = false;
                    if (RuntimeHelper.IsElevated && MainWindow.Handle != IntPtr.Zero)
                    {
                        CHANGEFILTERSTRUCT changeFilterStatus = new CHANGEFILTERSTRUCT();
                        changeFilterStatus.cbSize = Marshal.SizeOf(typeof(CHANGEFILTERSTRUCT));
                        User32Library.ChangeWindowMessageFilterEx(MainWindow.Handle, WindowMessage.WM_COPYDATA, ChangeFilterAction.MSGFLT_RESET, in changeFilterStatus);
                    }
                    SaveWindowInformation();
                    DownloadSchedulerService.CloseDownloadScheduler();
                    Aria2Service.CloseAria2();
                }

                isDisposed = true;
            }

            Environment.Exit(0);
        }
    }
}