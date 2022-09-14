using DS4WinWPF.DS4Forms;

namespace DS4WinWPF;

public interface IBindingWindowFactory
{
    BindingWindow Create(int deviceNum, DS4ControlSettings settings, BindingWindow.ExposeMode expose = BindingWindow.ExposeMode.Full);
}

public class BindingWindowFactory : IBindingWindowFactory
{
    private readonly IServiceProvider _serviceProvider;

    public BindingWindowFactory(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

    public BindingWindow Create(int deviceNum, DS4ControlSettings settings, BindingWindow.ExposeMode expose = BindingWindow.ExposeMode.Full) =>
        ActivatorUtilities.CreateInstance<BindingWindow>(_serviceProvider, deviceNum, settings, expose);
}