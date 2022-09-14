using DS4WinWPF.DS4Forms;

namespace DS4WinWPF;

public interface IProfileEditorFactory
{
    ProfileEditor Create(int device);
}

public class ProfileEditorFactory : IProfileEditorFactory
{
    private readonly IServiceProvider _serviceProvider;

    public ProfileEditorFactory(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

    public ProfileEditor Create(int device)
    {
        var profileSettingsViewModel = _serviceProvider.GetRequiredService<IProfileSettingsViewModelFactory>().Create(device);
        var mappingListViewModel = _serviceProvider.GetRequiredService<IMappingListViewModelFactory>().Create(device, profileSettingsViewModel.ContType);
        var specialActionsListViewModel = _serviceProvider.GetRequiredService<ISpecialActionsListViewModelFactory>().Create(device);
        return ActivatorUtilities.CreateInstance<ProfileEditor>(_serviceProvider, profileSettingsViewModel, mappingListViewModel, specialActionsListViewModel, device);
    }
}