namespace erpWpf.Wpf.Navigation;

/// <summary>
/// Associe une route (« Module » ou « Module/Action ») à l'écran qui l'affiche et à la permission exigée.
/// Chaque module enregistre ses pages dans l'injection de dépendances : la navigation n'est pas modifiée (OCP).
/// </summary>
public sealed record DefinitionPage(string Route, Type TypeViewModel, string? Permission);

/// <summary>
/// Route qui déclenche une action sans changer d'écran (ex. télécharger un document depuis un widget).
/// </summary>
public sealed record DefinitionAction(string Route, Type TypeAction, string? Permission);

/// <summary>Entrée du menu latéral ; les entrées d'une même section sont regroupées sous son titre.</summary>
public sealed record EntreeMenu(int Ordre, string Libelle, string Icone, string Destination, string? Permission, string? Section = null);

/// <summary>Action exécutée par une <see cref="DefinitionAction"/>.</summary>
public interface IActionNavigation
{
    Task ExecuterAsync(string? parametre, CancellationToken jetonAnnulation);
}
