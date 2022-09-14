namespace DS4Windows;

public class RequestElevationArgs : EventArgs
{
    public const int STATUS_SUCCESS = 0;
    public const int STATUS_INIT_FAILURE = -1;
    public int StatusCode { get; set; } = STATUS_INIT_FAILURE;

    public string InstanceId { get; }

    public RequestElevationArgs(string instanceId) => 
        InstanceId = instanceId;
}

