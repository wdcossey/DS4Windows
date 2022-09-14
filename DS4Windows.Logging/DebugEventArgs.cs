namespace DS4Windows;

public class DebugEventArgs : EventArgs
{
    protected DateTime m_Time = DateTime.Now;
    protected string m_Data = string.Empty;
    protected bool warning = false;
    protected bool temporary = false;
    public DebugEventArgs(string Data, bool warn, bool temporary = false)
    {
        m_Data = Data;
        warning = warn;
        this.temporary = temporary;
    }

    public DateTime Time => m_Time;
    public string Data => m_Data;
    public bool Warning => warning;
    public bool Temporary => temporary;
}