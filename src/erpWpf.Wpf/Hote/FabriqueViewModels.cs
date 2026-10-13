using erpWpf.Wpf.Navigation;
using Microsoft.Extensions.DependencyInjection;

namespace erpWpf.Wpf.Hote;

/// <summary>
/// Crée écrans et actions avec <see cref="ActivatorUtilities"/> : un transient jetable résolu depuis le fournisseur
/// racine serait conservé par le conteneur jusqu'à la fermeture de l'application (fuite à chaque navigation).
/// L'écran est libéré par la navigation ; ses dépendances sont des singletons.
/// </summary>
public sealed class FabriqueViewModels : IFabriqueViewModels
{
    private readonly IServiceProvider _services;

    public FabriqueViewModels(IServiceProvider services)
    {
        _services = services;
    }

    public object Creer(Type type) => ActivatorUtilities.CreateInstance(_services, type);
}
