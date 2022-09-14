using DS4WinWPF.DS4Forms;

namespace DS4WinWPF;

public interface IUpdaterWindowFactory
{
    UpdaterWindow Create(string withVersion);
}

public class UpdaterWindowFactory : IUpdaterWindowFactory
{
    private readonly IServiceProvider _serviceProvider;

    public UpdaterWindowFactory(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

    public UpdaterWindow Create(string withVersion)
    {
        var viewModel = _serviceProvider.GetRequiredService<IUpdaterWindowViewModelFactory>().Create(withVersion);
        return ActivatorUtilities.CreateInstance<UpdaterWindow>(_serviceProvider, viewModel, withVersion);
    }
}