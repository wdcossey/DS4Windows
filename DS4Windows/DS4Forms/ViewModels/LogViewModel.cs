﻿using System.Collections;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows.Data;

namespace DS4WinWPF.DS4Forms.ViewModels;

public class LogViewModel
{
    //private object _colLockobj = new object();
    private ReaderWriterLockSlim _logListLocker = new ReaderWriterLockSlim();
    private ObservableCollection<LogItem> logItems = new ObservableCollection<LogItem>();

    public ObservableCollection<LogItem> LogItems => logItems;

    public ReaderWriterLockSlim LogListLocker => _logListLocker;

    public LogViewModel(IControlService service)
    {
        string version = Global.exeversion;
        logItems.Add(new LogItem { Datetime = DateTime.Now, Message = $"DS4Windows version {version}" });
        logItems.Add(new LogItem { Datetime = DateTime.Now, Message = $"DS4Windows Assembly Architecture: {(Environment.Is64BitProcess ? "x64" : "x86")}" });
        logItems.Add(new LogItem { Datetime = DateTime.Now, Message = $"OS Version: {Environment.OSVersion}" });
        logItems.Add(new LogItem { Datetime = DateTime.Now, Message = $"OS Product Name: {DS4Windows.Util.GetOSProductName()}" });
        logItems.Add(new LogItem { Datetime = DateTime.Now, Message = $"OS Release ID: {DS4Windows.Util.GetOSReleaseId()}" });
        logItems.Add(new LogItem { Datetime = DateTime.Now, Message = $"System Architecture: {(Environment.Is64BitOperatingSystem ? "x64" : "x32")}" });

        //logItems.Add(new LogItem { Datetime = DateTime.Now, Message = "DS4Windows version 2.0" });
        //BindingOperations.EnableCollectionSynchronization(logItems, _colLockobj);
        BindingOperations.EnableCollectionSynchronization(logItems, _logListLocker, LogLockCallback);
        service.Debug += AddLogMessage;
        AppLogger.GuiLog += AddLogMessage;
    }

    private void LogLockCallback(IEnumerable collection, object context, Action? accessMethod, bool writeAccess)
    {
        if (writeAccess)
        {
            using var _ = new WriteLocker(_logListLocker);
            accessMethod?.Invoke();
        }
        else
        {
            using var _ = new ReadLocker(_logListLocker);
            accessMethod?.Invoke();
        }
    }

    private void AddLogMessage(object? sender, DebugEventArgs e)
    {
        LogItem item = new LogItem { Datetime = e.Time, Message = e.Data, Warning = e.Warning };
        _logListLocker.EnterWriteLock();
        logItems.Add(item);
        _logListLocker.ExitWriteLock();
        //lock (_colLockobj)
        //{
        //    logItems.Add(item);
        //}
    }
}