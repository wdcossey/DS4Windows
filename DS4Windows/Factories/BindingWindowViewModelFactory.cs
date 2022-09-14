namespace DS4WinWPF;

public interface IBindingWindowViewModelFactory
{
    BindingWindowViewModel Create(int deviceNum, DS4ControlSettings settings);
}

public class BindingWindowViewModelFactory : IBindingWindowViewModelFactory
{
    private readonly IServiceProvider _serviceProvider;

    public BindingWindowViewModelFactory(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

    public BindingWindowViewModel Create(int deviceNum, DS4ControlSettings settings) =>
        ActivatorUtilities.CreateInstance<BindingWindowViewModel>(_serviceProvider, deviceNum, settings);
}