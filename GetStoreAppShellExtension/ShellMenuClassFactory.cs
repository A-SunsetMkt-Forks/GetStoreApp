﻿using GetStoreAppShellExtension.Commands;
using GetStoreAppShellExtension.WindowsAPI.ComTypes;
using System;
using System.Runtime.InteropServices.Marshalling;

namespace GetStoreAppShellExtension
{
    /// <summary>
    /// 允许创建对象的类
    /// </summary>
    [GeneratedComClass]
    public partial class ShellMenuClassFactory : IClassFactory
    {
        private readonly IExplorerCommand rootExplorerCommand = new RootExplorerCommand();

        public unsafe int CreateInstance(IntPtr pUnkOuter, in Guid riid, out IntPtr ppvObject)
        {
            if (pUnkOuter != IntPtr.Zero)
            {
                ppvObject = IntPtr.Zero;
                return unchecked((int)0x80040110);
            }

            ppvObject = (IntPtr)ComInterfaceMarshaller<IExplorerCommand>.ConvertToUnmanaged(rootExplorerCommand);
            return 0;
        }

        public int LockServer(bool fLock)
        {
            return 0;
        }
    }
}