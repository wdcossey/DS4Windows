namespace DS4WinWPF;

public interface ISpecialActionsListViewModelFactory
{
    SpecialActionsListViewModel Create(int device);
}

public class SpecialActionsListViewModelFactory : ISpecialActionsListViewModelFactory
{
    private readonly IServiceProvider _serviceProvider;

    public SpecialActionsListViewModelFactory(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

    public SpecialActionsListViewModel Create(int device) =>
        ActivatorUtilities.CreateInstance<SpecialActionsListViewModel>(_serviceProvider, device);
}