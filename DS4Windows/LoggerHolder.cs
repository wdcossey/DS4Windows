namespace DS4WinWPF;

public class LoggerHolder
{
    private Logger logger;// = LogManager.GetCurrentClassLogger();
    public Logger Logger { get => logger; }

    public LoggerHolder(IControlService service)
    {
        var configuration = LogManager.Configuration;
        var wrapTarget = configuration.FindTargetByName<WrapperTargetBase>("logfile") as WrapperTargetBase;
        var fileTarget = wrapTarget.WrappedTarget as NLog.Targets.FileTarget;
        fileTarget.FileName = $@"{Global.appdatapath}\Logs\ds4windows_log.txt";
        fileTarget.ArchiveFileName = $@"{Global.appdatapath}\Logs\ds4windows_log_{{#}}.txt";
        LogManager.Configuration = configuration;
        LogManager.ReconfigExistingLoggers();

        logger = LogManager.GetCurrentClassLogger();

        service.Debug += WriteToLog;
        AppLogger.GuiLog += WriteToLog;
    }

    private void WriteToLog(object sender, DebugEventArgs e)
    {
        if (e.Temporary)
        {
            return;
        }

        if (!e.Warning)
        {
            logger.Info(e.Data);
        }
        else
        {
            logger.Warn(e.Data);
        }
    }
}