namespace DS4Windows;

public interface IControlServiceFactory
{
    IControlService Create();
}

public class ControlServiceFactory : IControlServiceFactory
{
    private readonly IServiceProvider _serviceProvider;

    public ControlServiceFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IControlService Create()
    {
        ControlService controlService = null;
        
        var controlThread = new Thread(() => {
            controlService = ActivatorUtilities.CreateInstance<ControlService>(_serviceProvider);
            //DS4Windows.Program.rootHub = rootHub;
            //requestClient = new HttpClient();
            //collectTimer = new Timer(GarbageTask, null, 30000, 30000);
        })
        {
            Priority = ThreadPriority.Normal,
            IsBackground = true
        };
        controlThread.Start();
        while (controlThread.IsAlive)
            Thread.SpinWait(500);
        
        return controlService!;
    }
}