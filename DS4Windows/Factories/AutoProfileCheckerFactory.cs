namespace DS4WinWPF;

public interface IAutoProfileCheckerFactory
{
    AutoProfileChecker Create(AutoProfileHolder profileHolder);
}

public class AutoProfileCheckerFactory : IAutoProfileCheckerFactory
{
    private readonly IServiceProvider _serviceProvider;

    public AutoProfileCheckerFactory(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

    public AutoProfileChecker Create(AutoProfileHolder profileHolder) =>
        ActivatorUtilities.CreateInstance<AutoProfileChecker>(_serviceProvider, profileHolder);
}