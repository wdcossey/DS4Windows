/*namespace DS4Windows;

public abstract class LogNotification : INotification
{
    public DateTime TimeStamp { get; } = DateTime.Now;
    public string Message { get; }
    public bool Warning { get; }
    public bool Temporary { get; }

    public LogNotification(string message, bool warning, bool temporary)
    {
        Message = message;
        Warning = warning;
        Temporary = temporary;
    }
}

public class LogGuiNotification : LogNotification
{
    public LogGuiNotification(string message, bool warning = false, bool temporary = false) : base(message, warning, temporary) { }
}

public class LogTrayNotification : LogNotification
{
    public LogTrayNotification(string message, bool warning = false, bool temporary = false) : base(message, warning, temporary) { }
}

public class LogDebugNotification : LogNotification
{
    public LogDebugNotification(string message, bool warning = false, bool temporary = false) : base(message, warning, temporary) { }
}

public class LogWarningNotification : LogNotification
{
    public LogWarningNotification(string message, bool warning = false, bool temporary = false) : base(message, warning, temporary) { }
}*/