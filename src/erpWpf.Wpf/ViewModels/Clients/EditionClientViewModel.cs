using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using erpWeb.Core.Clients;
using erpWeb.Core.Communs;
using erpWpf.Wpf.Navigation;
using erpWpf.Wpf.Services;
using erpWpf.Wpf.ViewModels.Communs;

namespace erpWpf.Wpf.ViewModels.Clients;

/// <summary>Formulaire client (ex-Clients/Creer et Clients/Modifier) : destination « Clients/Modifier/{id} » ou « Clients/Creer ».</summary>
public sealed partial class EditionClientViewModel : ViewModelBase, INavigable
{
    private readonly IExecuteurOperations _executeur;
    private readonly IServiceNavigation _navigation;
    private readonly IServiceNotifications _notifications;

    public EditionClientViewModel(IExecuteurOperations executeur, IServiceNavigation navigation, IServiceNotifications notifications)
    {
        _executeur = executeur;
        _navigation = navigation;
        _notifications = notifications;
    }

    public override string Titre => IdClient is null ? "Nouveau client" : "Modifier le client";

    public ChampsClientViewModel Champs { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Titre))]
    public partial int? IdClient { get; private set; }

    public Task InitialiserAsync(string? parametre, CancellationToken jetonAnnulation) => ExecuterOccupeAsync(async jeton =>
    {
        if (!int.TryParse(parametre, NumberStyles.None, CultureInfo.InvariantCulture, out var id))
        {
            return;
        }

        var client = await _executeur.ExecuterAsync<IServiceClients, ModificationClientDto?>(
            (service, jetonOperation) => service.ObtenirPourModificationAsync(id, jetonOperation),
            jeton);
        if (client is null)
        {
            _notifications.Erreur("Client introuvable.");
            await _navigation.NaviguerAsync(Destinations.Clients, jeton);
            return;
        }

        IdClient = client.Id;
        Champs.Copier(client);
    });

    [RelayCommand]
    private Task EnregistrerAsync() => ExecuterOccupeAsync(async jeton =>
    {
        EffacerErreurs();
        ResultatOperation resultat;
        if (IdClient is { } id)
        {
            var modification = Champs.VersModification(id);
            resultat = await _executeur.ExecuterAsync<IServiceClients, ResultatOperation>(
                (service, jetonOperation) => service.ModifierAsync(modification, jetonOperation),
                jeton);
        }
        else
        {
            var creation = Champs.VersCreation();
            var resultatCreation = await _executeur.ExecuterAsync<IServiceClients, ResultatOperation<int>>(
                (service, jetonOperation) => service.CreerAsync(creation, jetonOperation),
                jeton);
            resultat = new ResultatOperation(resultatCreation.Reussi, resultatCreation.Erreurs);
        }

        if (!resultat.Reussi)
        {
            AfficherErreurs(resultat.Erreurs);
            return;
        }

        _notifications.Succes(IdClient is null
            ? $"Le client « {Champs.RaisonSociale} » a été créé."
            : $"Le client « {Champs.RaisonSociale} » a été modifié.");
        await _navigation.NaviguerAsync(Destinations.Clients, jeton);
    });

    [RelayCommand]
    private Task AnnulerAsync() => _navigation.NaviguerAsync(Destinations.Clients, JetonEcran);
}
