namespace DS4Windows.Native;

internal static class SetupApi
{
    internal const short DIGCF_PRESENT = 0x2;
    internal const short DIGCF_DEVICEINTERFACE = 0x10;
    internal const int DIGCF_ALLCLASSES = 0x4;
    internal const int DICS_ENABLE = 1;
    internal const int DICS_DISABLE = 2;
    internal const int DICS_FLAG_GLOBAL = 1;
    internal const int DIF_PROPERTYCHANGE = 0x12;

    internal const int MAX_DEV_LEN = 1000;
    internal const int SPDRP_ADDRESS = 0x1c;
    internal const int SPDRP_BUSNUMBER = 0x15;
    internal const int SPDRP_BUSTYPEGUID = 0x13;
    internal const int SPDRP_CAPABILITIES = 0xf;
    internal const int SPDRP_CHARACTERISTICS = 0x1b;
    internal const int SPDRP_CLASS = 7;
    internal const int SPDRP_CLASSGUID = 8;
    internal const int SPDRP_COMPATIBLEIDS = 2;
    internal const int SPDRP_CONFIGFLAGS = 0xa;
    internal const int SPDRP_DEVICE_POWER_DATA = 0x1e;
    internal const int SPDRP_DEVICEDESC = 0;
    internal const int SPDRP_DEVTYPE = 0x19;
    internal const int SPDRP_DRIVER = 9;
    internal const int SPDRP_ENUMERATOR_NAME = 0x16;
    internal const int SPDRP_EXCLUSIVE = 0x1a;
    internal const int SPDRP_FRIENDLYNAME = 0xc;
    internal const int SPDRP_HARDWAREID = 1;
    internal const int SPDRP_LEGACYBUSTYPE = 0x14;
    internal const int SPDRP_LOCATION_INFORMATION = 0xd;
    internal const int SPDRP_LOWERFILTERS = 0x12;
    internal const int SPDRP_MFG = 0xb;
    internal const int SPDRP_PHYSICAL_DEVICE_OBJECT_NAME = 0xe;
    internal const int SPDRP_REMOVAL_POLICY = 0x1f;
    internal const int SPDRP_REMOVAL_POLICY_HW_DEFAULT = 0x20;
    internal const int SPDRP_REMOVAL_POLICY_OVERRIDE = 0x21;
    internal const int SPDRP_SECURITY = 0x17;
    internal const int SPDRP_SECURITY_SDS = 0x18;
    internal const int SPDRP_SERVICE = 4;
    internal const int SPDRP_UI_NUMBER = 0x10;
    internal const int SPDRP_UI_NUMBER_DESC_FORMAT = 0x1d;

    internal const int SPDRP_UPPERFILTERS = 0x11;

    [StructLayout(LayoutKind.Sequential)]
    internal class DEV_BROADCAST_DEVICEINTERFACE
    {
        internal int dbcc_size;
        internal int dbcc_devicetype;
        internal int dbcc_reserved;
        internal Guid dbcc_classguid;
        internal short dbcc_name;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal class DEV_BROADCAST_DEVICEINTERFACE_1
    {
        internal int dbcc_size;
        internal int dbcc_devicetype;
        internal int dbcc_reserved;
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 16)]
        internal byte[] dbcc_classguid;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 255)]
        internal char[] dbcc_name;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class DEV_BROADCAST_HANDLE
    {
        internal int dbch_size;
        internal int dbch_devicetype;
        internal int dbch_reserved;
        internal int dbch_handle;
        internal int dbch_hdevnotify;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class DEV_BROADCAST_HDR
    {
        internal int dbch_size;
        internal int dbch_devicetype;
        internal int dbch_reserved;
    }

    [DllImport("setupapi.dll", EntryPoint = "SetupDiGetDeviceRegistryProperty")]
    internal static extern bool SetupDiGetDeviceRegistryProperty(IntPtr deviceInfoSet, ref SP_DEVINFO_DATA deviceInfoData, int propertyVal, ref int propertyRegDataType, byte[] propertyBuffer, int propertyBufferSize, ref int requiredSize);

    [DllImport("setupapi.dll", EntryPoint = "SetupDiGetDevicePropertyW", SetLastError = true)]
    internal static extern bool SetupDiGetDeviceProperty(IntPtr deviceInfo, ref SP_DEVINFO_DATA deviceInfoData, ref DEVPROPKEY propkey, ref ulong propertyDataType, byte[] propertyBuffer, int propertyBufferSize, ref int requiredSize, uint flags);

    [DllImport("setupapi.dll", EntryPoint = "SetupDiGetDeviceInterfacePropertyW", SetLastError = true)]
    internal static extern bool SetupDiGetDeviceInterfaceProperty(IntPtr deviceInfo, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData,
        ref DEVPROPKEY propkey, ref ulong propertyDataType, byte[] propertyBuffer, int propertyBufferSize, ref int requiredSize, uint flags);

    [DllImport("setupapi.dll")]
    internal static extern bool SetupDiEnumDeviceInfo(IntPtr deviceInfoSet, int memberIndex, ref SP_DEVINFO_DATA deviceInfoData);

    [DllImport("setupapi.dll")]
    internal static extern int SetupDiCreateDeviceInfoList(ref Guid classGuid, int hwndParent);

    [DllImport("setupapi.dll")]
    internal static extern int SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);

    [DllImport("setupapi.dll")]
    internal static extern bool SetupDiEnumDeviceInterfaces(IntPtr deviceInfoSet, ref SP_DEVINFO_DATA deviceInfoData, ref Guid interfaceClassGuid, int memberIndex, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData);

    [DllImport("setupapi.dll")]
    internal static extern bool SetupDiEnumDeviceInterfaces(IntPtr deviceInfoSet, IntPtr deviceInfoData, ref Guid interfaceClassGuid, int memberIndex, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData);

    [DllImport("setupapi.dll", CharSet = CharSet.Auto)]
    internal static extern IntPtr SetupDiGetClassDevs(ref System.Guid classGuid, string enumerator, int hwndParent, int flags);

    [DllImport("setupapi.dll", CharSet = CharSet.Auto)]
    internal static extern IntPtr SetupDiGetClassDevs(IntPtr classGuid, string enumerator, int hwndParent, int flags);

    [DllImport("setupapi.dll", CharSet = CharSet.Auto, EntryPoint = "SetupDiGetDeviceInterfaceDetail")]
    internal static extern bool SetupDiGetDeviceInterfaceDetailBuffer(IntPtr deviceInfoSet, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData, IntPtr deviceInterfaceDetailData, int deviceInterfaceDetailDataSize, ref int requiredSize, IntPtr deviceInfoData);

    [DllImport("setupapi.dll", CharSet = CharSet.Auto)]
    internal static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr deviceInfoSet, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData, ref SP_DEVICE_INTERFACE_DETAIL_DATA deviceInterfaceDetailData, int deviceInterfaceDetailDataSize, ref int requiredSize, IntPtr deviceInfoData);

    [DllImport("setupapi.dll", CharSet = CharSet.Auto)]
    internal static extern bool SetupDiSetClassInstallParams(IntPtr deviceInfoSet, ref SP_DEVINFO_DATA deviceInfoData, ref SP_PROPCHANGE_PARAMS classInstallParams, int classInstallParamsSize);

    [DllImport("setupapi.dll", CharSet = CharSet.Auto)]
    internal static extern bool SetupDiCallClassInstaller(int installFunction, IntPtr deviceInfoSet, ref SP_DEVINFO_DATA deviceInfoData);

    [DllImport("setupapi.dll", CharSet = CharSet.Auto)]
    internal static extern bool SetupDiGetDeviceInstanceId(IntPtr deviceInfoSet, ref SP_DEVINFO_DATA deviceInfoData, char[] deviceInstanceId, Int32 deviceInstanceIdSize, ref int requiredSize);

    [DllImport("setupapi.dll", SetLastError = true)]
    internal static extern bool SetupDiClassGuidsFromName(string ClassName, ref Guid ClassGuidArray1stItem, UInt32 ClassGuidArraySize, out UInt32 RequiredSize);

    [StructLayout(LayoutKind.Sequential)]
    internal struct DEVPROPKEY
    {
        public Guid fmtid;
        public ulong pid;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SP_DEVICE_INTERFACE_DATA
    {
        internal int cbSize;
        internal System.Guid InterfaceClassGuid;
        internal int Flags;
        internal IntPtr Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SP_DEVINFO_DATA
    {
        internal int cbSize;
        internal Guid ClassGuid;
        internal int DevInst;
        internal IntPtr Reserved;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    internal struct SP_DEVICE_INTERFACE_DETAIL_DATA
    {
        internal int Size;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        internal string DevicePath;
    }



    [StructLayout(LayoutKind.Sequential)]
    internal struct SP_CLASSINSTALL_HEADER
    {
        internal int cbSize;
        internal int installFunction;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SP_PROPCHANGE_PARAMS
    {
        internal SP_CLASSINSTALL_HEADER classInstallHeader;
        internal int stateChange;
        internal int scope;
        internal int hwProfile;
    }

    internal static DEVPROPKEY DEVPKEY_Device_BusReportedDeviceDesc =
            new DEVPROPKEY { fmtid = new Guid(0x540b947e, 0x8b40, 0x45bc, 0xa8, 0xa2, 0x6a, 0x0b, 0x89, 0x4c, 0xbd, 0xa2), pid = 4 };

    internal static DEVPROPKEY DEVPKEY_Device_DeviceDesc =
        new DEVPROPKEY { fmtid = new Guid(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0), pid = 2 };

    internal static DEVPROPKEY DEVPKEY_Device_HardwareIds =
        new DEVPROPKEY { fmtid = new Guid(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0), pid = 3 };

    internal static DEVPROPKEY DEVPKEY_Device_UINumber =
        new DEVPROPKEY { fmtid = new Guid(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0), pid = 18 };

    internal static DEVPROPKEY DEVPKEY_Device_DriverVersion =
        new DEVPROPKEY { fmtid = new Guid(0xa8b865dd, 0x2e3d, 0x4094, 0xad, 0x97, 0xe5, 0x93, 0xa7, 0xc, 0x75, 0xd6), pid = 3 };

    internal static DEVPROPKEY DEVPKEY_Device_Manufacturer =
        new DEVPROPKEY { fmtid = new Guid(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0), pid = 13 };

    internal static DEVPROPKEY DEVPKEY_Device_Provider =
        new DEVPROPKEY { fmtid = new Guid(0xa8b865dd, 0x2e3d, 0x4094, 0xad, 0x97, 0xe5, 0x93, 0xa7, 0xc, 0x75, 0xd6), pid = 9 };

    internal static DEVPROPKEY DEVPKEY_Device_Parent =
        new DEVPROPKEY { fmtid = new Guid(0x4340a6c5, 0x93fa, 0x4706, 0x97, 0x2c, 0x7b, 0x64, 0x80, 0x08, 0xa5, 0xa7), pid = 8 };

    internal static DEVPROPKEY DEVPKEY_Device_Siblings =
        new DEVPROPKEY { fmtid = new Guid(0x4340a6c5, 0x93fa, 0x4706, 0x97, 0x2c, 0x7b, 0x64, 0x80, 0x08, 0xa5, 0xa7), pid = 10 };

    internal static DEVPROPKEY DEVPKEY_Device_InstanceId =
        new DEVPROPKEY { fmtid = new Guid(0x78c34fc8, 0x104a, 0x4aca, 0x9e, 0xa4, 0x52, 0x4d, 0x52, 0x99, 0x6e, 0x57), pid = 256 };


}