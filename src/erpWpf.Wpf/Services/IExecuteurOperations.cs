namespace erpWpf.Wpf.Services;

/// <summary>
/// Exécute une opération sur un service de Core dans sa propre portée d'injection (un DbContext par opération),
/// comme une requête HTTP dans la version web. Les ViewModels, de longue durée, ne capturent ainsi aucun service scoped.
/// </summary>
public interface IExecuteurOperations
{
    Task<TResultat> ExecuterAsync<TService, TResultat>(Func<TService, CancellationToken, Task<TResultat>> operation, CancellationToken jetonAnnulation = default)
        where TService : notnull;

    Task ExecuterAsync<TService>(Func<TService, CancellationToken, Task> operation, CancellationToken jetonAnnulation = default)
        where TService : notnull;
}
