using erpWpf.Wpf.Navigation;
using Microsoft.Extensions.DependencyInjection;

namespace erpWpf.Wpf.Hote;

public sealed class FabriqueViewModels : IFabriqueViewModels
{
    private readonly IServiceProvider _services;

    public FabriqueViewModels(IServiceProvider services)
    {
        _services = services;
    }

    public object Creer(Type type) => _services.GetRequiredService(type);
}
