using erpWpf.Wpf.ViewModels.Communs;

namespace erpWpf.Wpf.Navigation;

/// <summary>
/// Navigation entre écrans à partir d'une clé « Module/Action/parametre » (voir erpWeb.Core.Communs.Destinations),
/// avec contrôle de la permission de l'écran demandé.
/// </summary>
public interface IServiceNavigation
{
    ViewModelBase? PageCourante { get; }

    /// <summary>Route de la page courante (« Clients », « Clients/Modifier »…), pour signaler l'entrée de menu active.</summary>
    string? RouteCourante { get; }

    event EventHandler? PageChangee;

    /// <summary>Retourne faux si la destination est inconnue ou si l'écran courant refuse d'être quitté.</summary>
    Task<bool> NaviguerAsync(string destination, CancellationToken jetonAnnulation = default);

    /// <summary>Ferme la page courante (déconnexion).</summary>
    void Reinitialiser();
}
