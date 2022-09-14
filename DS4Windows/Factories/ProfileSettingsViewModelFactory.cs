namespace DS4WinWPF;

public interface IProfileSettingsViewModelFactory
{
    ProfileSettingsViewModel Create(int device);
}

public class ProfileSettingsViewModelFactory : IProfileSettingsViewModelFactory
{
    private readonly IServiceProvider _serviceProvider;

    public ProfileSettingsViewModelFactory(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

    public ProfileSettingsViewModel Create(int device) =>
        ActivatorUtilities.CreateInstance<ProfileSettingsViewModel>(_serviceProvider, device);
}