namespace DS4Windows;

public class CheckVirtualInfo : EventArgs
{
    public string? DeviceInstanceId { get; set; }
    public string? PropertyValue { get; set; }
}