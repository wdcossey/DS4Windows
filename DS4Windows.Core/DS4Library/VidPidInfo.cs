using DS4Windows.InputDevices;

namespace DS4Windows;

public class VidPidInfo
{
    public readonly int Vid;
    public readonly int Pid;
    public readonly string Name;
    public readonly InputDeviceType InputDevType;
    public readonly VidPidFeatureSet FeatureSet;
    public readonly CheckConnectionDelegate CheckConnection;
    internal VidPidInfo(int vid, int pid, string name = "Generic DS4", InputDeviceType inputDevType = InputDeviceType.DS4,
        VidPidFeatureSet featureSet = VidPidFeatureSet.DefaultDS4, CheckConnectionDelegate? checkConnection = null)
    {
        Vid = vid;
        Pid = pid;
        Name = name;
        InputDevType = inputDevType;
        FeatureSet = featureSet;
        CheckConnection = checkConnection ?? DS4Device.HidConnectionType;
    }
}