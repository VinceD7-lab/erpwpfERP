using erpWpf.Wpf.Services;

namespace erpWeb.UnitTests.Outils;

/// <summary>
/// Exécuteur des tests de ViewModels : chaque opération reçoit le service (souvent un mock) enregistré
/// pour son type, sans portée d'injection ni base de données.
/// </summary>
public sealed class ExecuteurOperationsTest : IExecuteurOperations
{
    private readonly Dictionary<Type, object> _services = [];

    public ExecuteurOperationsTest Ajouter<TService>(TService service)
        where TService : notnull
    {
        _services[typeof(TService)] = service;
        return this;
    }

    public Task<TResultat> ExecuterAsync<TService, TResultat>(Func<TService, CancellationToken, Task<TResultat>> operation, CancellationToken jetonAnnulation = default)
        where TService : notnull
        => operation(Obtenir<TService>(), jetonAnnulation);

    public Task ExecuterAsync<TService>(Func<TService, CancellationToken, Task> operation, CancellationToken jetonAnnulation = default)
        where TService : notnull
        => operation(Obtenir<TService>(), jetonAnnulation);

    private TService Obtenir<TService>()
        => _services.TryGetValue(typeof(TService), out var service)
            ? (TService)service
            : throw new InvalidOperationException($"Service {typeof(TService).Name} non enregistré dans l'exécuteur de test.");
}
