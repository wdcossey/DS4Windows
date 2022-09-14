namespace DS4WinWPF;

public interface IMappingListViewModelFactory
{
    MappingListViewModel Create(int device, OutContType devType);
}

public class MappingListViewModelFactory : IMappingListViewModelFactory
{
    private readonly IServiceProvider _serviceProvider;

    public MappingListViewModelFactory(IServiceProvider serviceProvider) => this._serviceProvider = serviceProvider;

    public MappingListViewModel Create(int device, OutContType devType) => 
        ActivatorUtilities.CreateInstance<MappingListViewModel>(_serviceProvider, device, devType);
}