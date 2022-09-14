using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using Microsoft.Win32.SafeHandles; 

namespace DS4Windows
{
    [SuppressUnmanagedCodeSecurity]
    internal static class NativeMethods
    {
        internal const uint FILE_ATTRIBUTE_NORMAL = 0x80;
        internal const int FILE_FLAG_OVERLAPPED = 0x40000000;
        internal const uint FILE_FLAG_NO_BUFFERING = 0x20000000;
        internal const uint FILE_FLAG_WRITE_THROUGH = 0x80000000;
        internal const uint FILE_ATTRIBUTE_TEMPORARY = 0x100;

        internal const short FILE_SHARE_READ = 0x1;
        internal const short FILE_SHARE_WRITE = 0x2;
        internal const uint GENERIC_READ = 0x80000000;
        internal const uint GENERIC_WRITE = 0x40000000;
        internal const Int32 FileShareRead = 1;
        internal const Int32 FileShareWrite = 2;
        internal const Int32 OpenExisting = 3;
        internal const int ACCESS_NONE = 0;
        internal const int INVALID_HANDLE_VALUE = -1;
        internal const short OPEN_EXISTING = 3;
        internal const int WAIT_TIMEOUT = 0x102;
        internal const uint WAIT_OBJECT_0 = 0;
        internal const uint WAIT_FAILED = 0xffffffff;

        internal const int WAIT_INFINITE = 0xffff;
        [StructLayout(LayoutKind.Sequential)]
        internal struct OVERLAPPED
        {
            public int Internal;
            public int InternalHigh;
            public int Offset;
            public int OffsetHigh;
            public int hEvent;
        }

        internal const int DBT_DEVICEARRIVAL = 0x8000;
        internal const int DBT_DEVICEREMOVECOMPLETE = 0x8004;
        internal const int DBT_DEVTYP_DEVICEINTERFACE = 5;
        internal const int DBT_DEVTYP_HANDLE = 6;
        internal const int DEVICE_NOTIFY_ALL_INTERFACE_CLASSES = 4;
        internal const int DEVICE_NOTIFY_SERVICE_HANDLE = 1;
        internal const int DEVICE_NOTIFY_WINDOW_HANDLE = 0;
        internal const int WM_DEVICECHANGE = 0x219;
        

    }
}
