using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using erpWeb.Core.Communs;
using erpWeb.Core.Factures;
using erpWpf.Wpf.Navigation;
using erpWpf.Wpf.Services;
using erpWpf.Wpf.ViewModels.Communs;

namespace erpWpf.Wpf.ViewModels.Factures;

/// <summary>Aperçu et impression d'une facture (ex-Factures/Imprimer/{id}).</summary>
public sealed partial class ImpressionFactureViewModel : ViewModelBase, INavigable
{
    private readonly IExecuteurOperations _executeur;
    private readonly IServiceNavigation _navigation;
    private readonly IServiceNotifications _notifications;
    private readonly IServiceImpression _impression;

    public ImpressionFactureViewModel(IExecuteurOperations executeur, IServiceNavigation navigation, IServiceNotifications notifications, IServiceImpression impression)
    {
        _executeur = executeur;
        _navigation = navigation;
        _notifications = notifications;
        _impression = impression;
    }

    public override string Titre => Impression is null
        ? "Facture"
        : $"Facture n° {Impression.Facture.NumeroFacture.ToString("D6", CultureInfo.CurrentCulture)}";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Titre))]
    [NotifyCanExecuteChangedFor(nameof(ImprimerCommand))]
    public partial ImpressionFactureDto? Impression { get; private set; }

    public Task InitialiserAsync(string? parametre, CancellationToken jetonAnnulation) => ExecuterOccupeAsync(async jeton =>
    {
        ImpressionFactureDto? impression = null;
        if (int.TryParse(parametre, NumberStyles.None, CultureInfo.InvariantCulture, out var id))
        {
            impression = await _executeur.ExecuterAsync<IServiceImpressionFacture, ImpressionFactureDto?>(
                (service, jetonOperation) => service.ObtenirAsync(id, jetonOperation),
                jeton);
        }

        if (impression is null)
        {
            _notifications.Erreur("Facture introuvable.");
            await _navigation.NaviguerAsync(Destinations.Factures, jeton);
            return;
        }

        Impression = impression;
    });

    [RelayCommand(CanExecute = nameof(EstChargee))]
    private void Imprimer() => _impression.ImprimerFacture(Impression!);

    [RelayCommand]
    private Task RetourAsync() => _navigation.NaviguerAsync(Destinations.Factures, JetonEcran);

    private bool EstChargee() => Impression is not null;
}
