using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using erpWeb.Core.Autorisation;
using erpWeb.Core.Clients;
using erpWeb.Core.Communs;
using erpWpf.Wpf.Navigation;
using erpWpf.Wpf.Securite;
using erpWpf.Wpf.Services;
using erpWpf.Wpf.ViewModels.Communs;

namespace erpWpf.Wpf.ViewModels.Clients;

/// <summary>
/// Liste des clients (ex-Clients/Index) : recherche, édition en ligne, détail du client sélectionné
/// et grille de ses échantillons, suppression avec confirmation.
/// </summary>
public sealed partial class ListeClientsViewModel : ViewModelBase, INavigable
{
    private readonly IExecuteurOperations _executeur;
    private readonly IServiceNavigation _navigation;
    private readonly IServiceDialogues _dialogues;
    private readonly IServiceNotifications _notifications;
    private readonly List<LigneClientViewModel> _tousLesClients = [];

    public ListeClientsViewModel(
        IExecuteurOperations executeur,
        IServiceNavigation navigation,
        IServiceDialogues dialogues,
        IServiceNotifications notifications,
        IAutorisation autorisation)
    {
        _executeur = executeur;
        _navigation = navigation;
        _dialogues = dialogues;
        _notifications = notifications;
        PeutGerer = autorisation.APermission(Permissions.Clients.Gerer);
        PeutVoirEchantillons = autorisation.APermission(Permissions.Echantillons.Lire);
        Echantillons = new EchantillonsClientViewModel(executeur);
    }

    public override string Titre => "Clients";

    public bool PeutGerer { get; }

    public bool PeutVoirEchantillons { get; }

    public EchantillonsClientViewModel Echantillons { get; }

    public ObservableCollection<LigneClientViewModel> Clients { get; } = [];

    [ObservableProperty]
    public partial string? Recherche { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ModifierCommand), nameof(SupprimerCommand))]
    public partial LigneClientViewModel? ClientSelectionne { get; set; }

    public Task InitialiserAsync(string? parametre, CancellationToken jetonAnnulation) => ChargerAsync();

    [RelayCommand]
    private Task ChargerAsync() => ExecuterOccupeAsync(async jeton =>
    {
        var clients = await _executeur.ExecuterAsync<IServiceClients, IReadOnlyList<ClientDto>>(
            (service, jetonOperation) => service.ListerAsync(jetonOperation),
            jeton);

        _tousLesClients.Clear();
        _tousLesClients.AddRange(clients.Select(client => new LigneClientViewModel(client, EnregistrerLigneAsync)));
        AppliquerRecherche();
    });

    [RelayCommand(CanExecute = nameof(PeutGerer))]
    private Task CreerAsync() => _navigation.NaviguerAsync(Destinations.CreationClient, JetonEcran);

    [RelayCommand(CanExecute = nameof(PeutAgirSurLaSelection))]
    private Task ModifierAsync(LigneClientViewModel? ligne)
    {
        var cible = ligne ?? ClientSelectionne;
        return cible is null ? Task.CompletedTask : _navigation.NaviguerAsync(Destinations.ModificationClient(cible.Id), JetonEcran);
    }

    [RelayCommand(CanExecute = nameof(PeutAgirSurLaSelection))]
    private Task SupprimerAsync(LigneClientViewModel? ligne) => ExecuterOccupeAsync(async jeton =>
    {
        var cible = ligne ?? ClientSelectionne;
        if (cible is null || !_dialogues.Confirmer($"Supprimer le client « {cible.RaisonSociale} » ?"))
        {
            return;
        }

        var resultat = await _executeur.ExecuterAsync<IServiceClients, ResultatOperation>(
            (service, jetonOperation) => service.SupprimerAsync(cible.Id, jetonOperation),
            jeton);
        if (!resultat.Reussi)
        {
            _notifications.Erreur(string.Join(" ", resultat.Erreurs));
            return;
        }

        _tousLesClients.Remove(cible);
        Clients.Remove(cible);
        _notifications.Succes($"Le client « {cible.RaisonSociale} » a été supprimé.");
    });

    partial void OnRechercheChanged(string? value) => AppliquerRecherche();

    partial void OnClientSelectionneChanged(LigneClientViewModel? value)
    {
        if (PeutVoirEchantillons)
        {
            Echantillons.SelectionnerClient(value?.Id);
        }
    }

    private bool PeutAgirSurLaSelection(LigneClientViewModel? ligne) => PeutGerer && (ligne ?? ClientSelectionne) is not null;

    /// <summary>Recherche sur toutes les colonnes affichées (ex-recherche DataTables).</summary>
    private void AppliquerRecherche()
    {
        var termes = Recherche?.Trim();
        Clients.Clear();
        foreach (var ligne in _tousLesClients.Where(ligne => string.IsNullOrEmpty(termes) || Correspond(ligne, termes)))
        {
            Clients.Add(ligne);
        }
    }

    private static bool Correspond(LigneClientViewModel ligne, string termes)
        => new[] { ligne.RaisonSociale, ligne.CodePostal, ligne.Ville, ligne.Pays, ligne.Telephone1, ligne.Email }
            .Any(valeur => valeur?.Contains(termes, StringComparison.CurrentCultureIgnoreCase) == true);

    /// <summary>Enregistrement d'une ligne éditée en place (ex-action ModifierLigne).</summary>
    private async Task EnregistrerLigneAsync(LigneClientViewModel ligne)
    {
        try
        {
            if (!PeutGerer)
            {
                await RelireLigneAsync(ligne);
                _notifications.Erreur("Vous n'avez pas la permission de modifier les clients.");
                return;
            }

            var modification = ligne.VersModification(ligne.Id);
            var resultat = await _executeur.ExecuterAsync<IServiceClients, ResultatOperation>(
                (service, jeton) => service.ModifierAsync(modification, jeton),
                JetonEcran);
            if (!resultat.Reussi)
            {
                ligne.Erreur = string.Join(" ", resultat.Erreurs);
                _notifications.Erreur($"« {ligne.RaisonSociale} » n'a pas été enregistré : {ligne.Erreur}");
                return;
            }

            // Relecture : le service normalise certaines valeurs (téléphones).
            await RelireLigneAsync(ligne);
            _notifications.Succes($"Les modifications de « {ligne.RaisonSociale} » ont été enregistrées.");
        }
        catch (OperationCanceledException)
        {
            // Écran fermé pendant l'enregistrement.
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            ligne.Erreur = "Enregistrement impossible.";
            _notifications.Erreur($"« {ligne.RaisonSociale} » n'a pas pu être enregistré.");
        }
    }

    private async Task RelireLigneAsync(LigneClientViewModel ligne)
    {
        var client = await _executeur.ExecuterAsync<IServiceClients, ClientDto?>(
            (service, jeton) => service.ObtenirAsync(ligne.Id, jeton),
            JetonEcran);
        if (client is not null)
        {
            ligne.Actualiser(client);
        }
    }

    protected override void SurFermeture() => Echantillons.Dispose();
}
