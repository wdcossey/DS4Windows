namespace DS4Windows;

public delegate void SixAxisHandler<in TEventArgs>(DS4SixAxis sender, TEventArgs args) where TEventArgs : EventArgs;
public delegate void RequestElevationDelegate(RequestElevationArgs args);
public delegate CheckVirtualInfo CheckVirtualDelegate(string deviceInstanceId);
public delegate ConnectionType CheckConnectionDelegate(HidDevice hidDevice);
public delegate void PrepareInitDelegate(DS4Device device);
public delegate bool CheckPendingDevice(HidDevice device, VidPidInfo vidPidInfo);