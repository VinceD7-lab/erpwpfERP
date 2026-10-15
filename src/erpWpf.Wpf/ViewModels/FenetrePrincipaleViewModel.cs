using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using erpWeb.Core.Communs;
using erpWpf.Wpf.Navigation;
using erpWpf.Wpf.Securite;
using erpWpf.Wpf.Services;
using erpWpf.Wpf.ViewModels.Communs;

namespace erpWpf.Wpf.ViewModels;

/// <summary>Élément affiché dans le menu latéral : titre de section ou lien vers un écran.</summary>
public sealed partial class ElementMenuViewModel : ObservableObject
{
    public ElementMenuViewModel(string libelle, string? icone, string? destination)
    {
        Libelle = libelle;
        Icone = icone;
        Destination = destination;
    }

    public string Libelle { get; }

    public string? Icone { get; }

    public string? Destination { get; }

    public bool EstSection => Destination is null;

    [ObservableProperty]
    public partial bool EstActif { get; set; }
}

/// <summary>Fenêtre principale (ex-_Layout.cshtml) : menu filtré par permission, page courante, bandeau de messages.</summary>
public sealed partial class FenetrePrincipaleViewModel : ObservableObject, IDisposable
{
    private readonly IServiceNavigation _navigation;
    private readonly IReadOnlyList<EntreeMenu> _entrees;
    private readonly IAutorisation _autorisation;
    private readonly ISessionUtilisateur _session;
    private readonly IPreferencesLocales _preferences;

    public FenetrePrincipaleViewModel(
        IServiceNavigation navigation,
        IEnumerable<EntreeMenu> entrees,
        IAutorisation autorisation,
        ISessionUtilisateur session,
        IServiceNotifications notifications,
        IPreferencesLocales preferences)
    {
        _navigation = navigation;
        _entrees = entrees.OrderBy(entree => entree.Ordre).ToList();
        _autorisation = autorisation;
        _session = session;
        _preferences = preferences;
        Notifications = notifications;
        MenuReplie = preferences.MenuReplie;

        _navigation.PageChangee += SurPageChangee;
        _session.SessionModifiee += SurSessionModifiee;
        ConstruireMenu();
    }

    public IServiceNotifications Notifications { get; }

    public ObservableCollection<ElementMenuViewModel> Menu { get; } = [];

    public ViewModelBase? PageCourante => _navigation.PageCourante;

    public string NomUtilisateur => _session.Session?.NomComplet ?? string.Empty;

    [ObservableProperty]
    public partial bool MenuReplie { get; set; }

    /// <summary>Ouvre le tableau de bord par la commande : une erreur atteint le gestionnaire global.</summary>
    public void Demarrer() => NaviguerCommand.Execute(Destinations.TableauDeBord);

    [RelayCommand]
    private Task NaviguerAsync(string? destination)
        => string.IsNullOrEmpty(destination) ? Task.CompletedTask : _navigation.NaviguerAsync(destination);

    [RelayCommand]
    private void BasculerMenu() => MenuReplie = !MenuReplie;

    [RelayCommand]
    private void Deconnecter() => _session.Fermer();

    [RelayCommand]
    private void FermerNotification() => Notifications.Masquer();

    partial void OnMenuReplieChanged(bool value) => _preferences.MenuReplie = value;

    public void Dispose()
    {
        _navigation.PageChangee -= SurPageChangee;
        _session.SessionModifiee -= SurSessionModifiee;
    }

    /// <summary>
    /// Entrées autorisées uniquement ; un titre de section n'apparaît que si au moins une de ses entrées est visible.
    /// </summary>
    private void ConstruireMenu()
    {
        Menu.Clear();
        string? sectionCourante = null;
        foreach (var entree in _entrees.Where(entree => _autorisation.APermission(entree.Permission)))
        {
            if (entree.Section is not null && entree.Section != sectionCourante)
            {
                Menu.Add(new ElementMenuViewModel(entree.Section, icone: null, destination: null));
            }

            sectionCourante = entree.Section;
            Menu.Add(new ElementMenuViewModel(entree.Libelle, entree.Icone, entree.Destination));
        }

        MarquerEntreeActive();
    }

    private void MarquerEntreeActive()
    {
        var module = _navigation.RouteCourante?.Split('/')[0];
        foreach (var element in Menu)
        {
            element.EstActif = element.Destination is not null
                && string.Equals(element.Destination.Split('/')[0], module, StringComparison.OrdinalIgnoreCase);
        }
    }

    private void SurPageChangee(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(PageCourante));
        MarquerEntreeActive();
    }

    private void SurSessionModifiee(object? sender, EventArgs e)
    {
        // Les permissions peuvent changer à l'actualisation de la session (rôles modifiés).
        OnPropertyChanged(nameof(NomUtilisateur));
        if (_session.EstOuverte)
        {
            ConstruireMenu();
        }
    }
}
