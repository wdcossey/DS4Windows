using System.Runtime.InteropServices;

// ReSharper disable InconsistentNaming

namespace DS4Windows.VJoyFeeder;

internal static class vJoyInterface
{
    /***************************************************/
    /***** Import from file vJoyInterface.dll (C) ******/
    /***************************************************/

    /////	General driver data
    [DllImport("vJoyInterface.dll", EntryPoint = "GetvJoyVersion")]
    internal static extern short GetvJoyVersion();

    [DllImport("vJoyInterface.dll", EntryPoint = "vJoyEnabled")]
    internal static extern bool vJoyEnabled();

    [DllImport("vJoyInterface.dll", EntryPoint = "GetvJoyProductString")]
    internal static extern IntPtr GetvJoyProductString();

    [DllImport("vJoyInterface.dll", EntryPoint = "GetvJoyManufacturerString")]
    internal static extern IntPtr GetvJoyManufacturerString();

    [DllImport("vJoyInterface.dll", EntryPoint = "GetvJoySerialNumberString")]
    internal static extern IntPtr GetvJoySerialNumberString();

    [DllImport("vJoyInterface.dll", EntryPoint = "DriverMatch")]
    internal static extern bool DriverMatch(ref uint DllVer, ref uint DrvVer);

    /////	vJoy Device properties
    [DllImport("vJoyInterface.dll", EntryPoint = "GetVJDButtonNumber")]
    internal static extern int GetVJDButtonNumber(uint rID);

    [DllImport("vJoyInterface.dll", EntryPoint = "GetVJDDiscPovNumber")]
    internal static extern int GetVJDDiscPovNumber(uint rID);

    [DllImport("vJoyInterface.dll", EntryPoint = "GetVJDContPovNumber")]
    internal static extern int GetVJDContPovNumber(uint rID);

    [DllImport("vJoyInterface.dll", EntryPoint = "GetVJDAxisExist")]
    internal static extern uint GetVJDAxisExist(uint rID, uint Axis);

    [DllImport("vJoyInterface.dll", EntryPoint = "GetVJDAxisMax")]
    internal static extern bool GetVJDAxisMax(uint rID, uint Axis, ref long Max);

    [DllImport("vJoyInterface.dll", EntryPoint = "GetVJDAxisMin")]
    internal static extern bool GetVJDAxisMin(uint rID, uint Axis, ref long Min);

    [DllImport("vJoyInterface.dll", EntryPoint = "isVJDExists")]
    internal static extern bool isVJDExists(uint rID);

    [DllImport("vJoyInterface.dll", EntryPoint = "GetOwnerPid")]
    internal static extern int GetOwnerPid(uint rID);

    /////	Write access to vJoy Device - Basic
    [DllImport("vJoyInterface.dll", EntryPoint = "AcquireVJD")]
    internal static extern bool AcquireVJD(uint rID);

    [DllImport("vJoyInterface.dll", EntryPoint = "RelinquishVJD")]
    internal static extern void RelinquishVJD(uint rID);

    [DllImport("vJoyInterface.dll", EntryPoint = "UpdateVJD")]
    internal static extern bool UpdateVJD(uint rID, ref VJoy.JoystickState pData);

    [DllImport("vJoyInterface.dll", EntryPoint = "GetVJDStatus")]
    internal static extern int GetVJDStatus(uint rID);


    //// Reset functions
    [DllImport("vJoyInterface.dll", EntryPoint = "ResetVJD")]
    internal static extern bool ResetVJD(uint rID);

    [DllImport("vJoyInterface.dll", EntryPoint = "ResetAll")]
    internal static extern bool ResetAll();

    [DllImport("vJoyInterface.dll", EntryPoint = "ResetButtons")]
    internal static extern bool ResetButtons(uint rID);

    [DllImport("vJoyInterface.dll", EntryPoint = "ResetPovs")]
    internal static extern bool ResetPovs(uint rID);

    ////// Write data
    [DllImport("vJoyInterface.dll", EntryPoint = "SetAxis")]
    internal static extern bool SetAxis(int Value, uint rID, HID_USAGES Axis);

    [DllImport("vJoyInterface.dll", EntryPoint = "SetBtn")]
    internal static extern bool SetBtn(bool Value, uint rID, byte nBtn);

    [DllImport("vJoyInterface.dll", EntryPoint = "SetDiscPov")]
    internal static extern bool SetDiscPov(int Value, uint rID, uint nPov);

    [DllImport("vJoyInterface.dll", EntryPoint = "SetContPov")]
    internal static extern bool SetContPov(int Value, uint rID, uint nPov);

    [DllImport("vJoyInterface.dll", EntryPoint = "RegisterRemovalCB", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void RegisterRemovalCB(VJoy.WrapRemovalCbFunc cb, IntPtr data);
}