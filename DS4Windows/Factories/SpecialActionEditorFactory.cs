using DS4WinWPF.DS4Forms;

namespace DS4WinWPF;

public interface ISpecialActionEditorFactory
{
    SpecialActionEditor Create(int device, ProfileList profileList, SpecialAction specialAction = null);
}

public class SpecialActionEditorFactory : ISpecialActionEditorFactory
{
    private readonly IServiceProvider _serviceProvider;

    public SpecialActionEditorFactory(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

    public SpecialActionEditor Create(int device, ProfileList profileList, SpecialAction specialAction = null) =>
        ActivatorUtilities.CreateInstance<SpecialActionEditor>(_serviceProvider, device, profileList, specialAction);
}