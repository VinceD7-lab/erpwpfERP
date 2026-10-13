using erpWpf.Wpf.Services;
using Microsoft.Extensions.DependencyInjection;

namespace erpWpf.Wpf.Hote;

/// <summary>Une portée d'injection (donc un DbContext) par opération : seul point d'accès à IServiceScopeFactory.</summary>
public sealed class ExecuteurOperations : IExecuteurOperations
{
    private readonly IServiceScopeFactory _fabriquePortees;

    public ExecuteurOperations(IServiceScopeFactory fabriquePortees)
    {
        _fabriquePortees = fabriquePortees;
    }

    public async Task<TResultat> ExecuterAsync<TService, TResultat>(Func<TService, CancellationToken, Task<TResultat>> operation, CancellationToken jetonAnnulation = default)
        where TService : notnull
    {
        await using var portee = _fabriquePortees.CreateAsyncScope();
        return await operation(portee.ServiceProvider.GetRequiredService<TService>(), jetonAnnulation);
    }

    public async Task ExecuterAsync<TService>(Func<TService, CancellationToken, Task> operation, CancellationToken jetonAnnulation = default)
        where TService : notnull
    {
        await using var portee = _fabriquePortees.CreateAsyncScope();
        await operation(portee.ServiceProvider.GetRequiredService<TService>(), jetonAnnulation);
    }
}
