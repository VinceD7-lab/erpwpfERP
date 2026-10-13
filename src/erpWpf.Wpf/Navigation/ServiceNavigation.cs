using erpWpf.Wpf.Securite;
using erpWpf.Wpf.Services;
using erpWpf.Wpf.ViewModels.Communs;
using erpWpf.Wpf.ViewModels.Compte;

namespace erpWpf.Wpf.Navigation;

public sealed class ServiceNavigation : IServiceNavigation
{
    private readonly Dictionary<string, DefinitionPage> _pages;
    private readonly Dictionary<string, DefinitionAction> _actions;
    private readonly IFabriqueViewModels _fabrique;
    private readonly IAutorisation _autorisation;
    private readonly IServiceNotifications _notifications;

    public ServiceNavigation(
        IEnumerable<DefinitionPage> pages,
        IEnumerable<DefinitionAction> actions,
        IFabriqueViewModels fabrique,
        IAutorisation autorisation,
        IServiceNotifications notifications)
    {
        _pages = pages.ToDictionary(page => page.Route, StringComparer.OrdinalIgnoreCase);
        _actions = actions.ToDictionary(action => action.Route, StringComparer.OrdinalIgnoreCase);
        _fabrique = fabrique;
        _autorisation = autorisation;
        _notifications = notifications;
    }

    public ViewModelBase? PageCourante { get; private set; }

    public string? RouteCourante { get; private set; }

    public event EventHandler? PageChangee;

    public async Task<bool> NaviguerAsync(string destination, CancellationToken jetonAnnulation = default)
    {
        if (!Resoudre(destination, out var route, out var parametre))
        {
            _notifications.Erreur($"Écran introuvable : « {destination} ».");
            return false;
        }

        if (_actions.TryGetValue(route, out var action))
        {
            if (!_autorisation.APermission(action.Permission))
            {
                return Afficher(route, (ViewModelBase)_fabrique.Creer(typeof(AccesRefuseViewModel)));
            }

            var executable = (IActionNavigation)_fabrique.Creer(action.TypeAction);
            await executable.ExecuterAsync(parametre, jetonAnnulation);
            return true;
        }

        if (PageCourante is IConfirmationDepart confirmation && !confirmation.PeutQuitter())
        {
            return false;
        }

        var page = _pages[route];
        if (!_autorisation.APermission(page.Permission))
        {
            return Afficher(route, (ViewModelBase)_fabrique.Creer(typeof(AccesRefuseViewModel)));
        }

        var viewModel = (ViewModelBase)_fabrique.Creer(page.TypeViewModel);
        Afficher(route, viewModel);
        if (viewModel is INavigable navigable)
        {
            await navigable.InitialiserAsync(parametre, jetonAnnulation);
        }

        return true;
    }

    public void Reinitialiser()
    {
        PageCourante?.Dispose();
        PageCourante = null;
        RouteCourante = null;
        PageChangee?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// « Module/Action/parametre » : la route la plus longue enregistrée l'emporte
    /// (« Clients/Modifier/12 » → route « Clients/Modifier », paramètre « 12 » ; « Clients » → route « Clients »).
    /// </summary>
    private bool Resoudre(string destination, out string route, out string? parametre)
    {
        var segments = destination.Trim('/').Split('/', 3, StringSplitOptions.RemoveEmptyEntries);
        for (var longueur = Math.Min(segments.Length, 2); longueur >= 1; longueur--)
        {
            var candidate = string.Join('/', segments.Take(longueur));
            if (_pages.ContainsKey(candidate) || _actions.ContainsKey(candidate))
            {
                route = candidate;
                parametre = segments.Length > longueur ? string.Join('/', segments.Skip(longueur)) : null;
                return true;
            }
        }

        route = string.Empty;
        parametre = null;
        return false;
    }

    private bool Afficher(string route, ViewModelBase viewModel)
    {
        PageCourante?.Dispose();
        PageCourante = viewModel;
        RouteCourante = route;
        PageChangee?.Invoke(this, EventArgs.Empty);
        return true;
    }
}
