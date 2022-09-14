namespace DS4WinWPF;

public interface IUpdaterWindowViewModelFactory
{
    UpdaterWindowViewModel Create(string withVersion);
}

public class UpdaterWindowViewModelFactory : IUpdaterWindowViewModelFactory
{
    private readonly IServiceProvider _serviceProvider;

    public UpdaterWindowViewModelFactory(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

    public UpdaterWindowViewModel Create(string withVersion) =>
        ActivatorUtilities.CreateInstance<UpdaterWindowViewModel>(_serviceProvider, withVersion);
}

