using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using erpWeb.Core.Autorisation;
using erpWeb.Core.Communs;
using erpWeb.Core.Factures;
using erpWpf.Wpf.Navigation;
using erpWpf.Wpf.Securite;
using erpWpf.Wpf.Services;
using erpWpf.Wpf.ViewModels.Communs;

namespace erpWpf.Wpf.ViewModels.Factures;

/// <summary>
/// Factures (ex-Factures/Index, îlot tableau-factures) : grille paginée côté serveur, génération
/// des factures manquantes et ouverture de l'aperçu d'impression.
/// </summary>
public sealed partial class ListeFacturesViewModel : GrillePagineeViewModel<FactureDto, ChampTriFactures>
{
    private readonly IExecuteurOperations _executeur;
    private readonly IServiceNavigation _navigation;
    private readonly IServiceNotifications _notifications;

    public ListeFacturesViewModel(IExecuteurOperations executeur, IServiceNavigation navigation, IServiceNotifications notifications, IAutorisation autorisation)
        : base(CriteresFactures.TaillePageParDefaut, ChampTriFactures.DateFacture, triDescendantParDefaut: true)
    {
        _executeur = executeur;
        _navigation = navigation;
        _notifications = notifications;
        PeutGenerer = autorisation.APermission(Permissions.Factures.Generer);
        Statuts = OptionsFiltre.Statuts(StatutsFacture.Tous, LibellesStatuts.Libelle);
    }

    public override string Titre => "Factures";

    public bool PeutGenerer { get; }

    public IReadOnlyList<OptionFiltre<string>> Statuts { get; }

    [ObservableProperty]
    public partial IReadOnlyList<OptionFiltre<int?>> Clients { get; private set; } = [];

    [ObservableProperty]
    public partial DateTime? DateFactureDebut { get; set; }

    [ObservableProperty]
    public partial DateTime? DateFactureFin { get; set; }

    [ObservableProperty]
    public partial int? IdClient { get; set; }

    [ObservableProperty]
    public partial string? StatutFacture { get; set; }

    public override async Task InitialiserAsync(string? parametre, CancellationToken jetonAnnulation)
    {
        Clients = await OptionsFiltre.ClientsAsync(_executeur, jetonAnnulation);
        await ChargerAsync();
    }

    [RelayCommand(CanExecute = nameof(PeutGenerer))]
    private Task GenererAsync() => ExecuterOccupeAsync(async jeton =>
    {
        var nombre = await _executeur.ExecuterAsync<IGenerationFactures, int>(
            (service, jetonOperation) => service.GenererManquantesAsync(jetonOperation),
            jeton);

        _notifications.Succes(nombre == 0
            ? "Toutes les factures sont déjà générées."
            : $"{nombre} facture(s) générée(s).");
        await FiltrerAsync();
    });

    [RelayCommand]
    private Task ImprimerAsync(FactureDto? facture)
        => facture is null ? Task.CompletedTask : _navigation.NaviguerAsync(Destinations.ImpressionFacture(facture.IdFacture), JetonEcran);

    protected override Task<ResultatPagine<FactureDto>> RechercherAsync(
        int numeroPage, int taillePage, ChampTriFactures tri, bool triDescendant, CancellationToken jetonAnnulation)
    {
        var criteres = new CriteresFactures
        {
            NumeroPage = numeroPage,
            TaillePage = taillePage,
            DateFactureDebut = DateFactureDebut is { } debut ? DateOnly.FromDateTime(debut) : null,
            DateFactureFin = DateFactureFin is { } fin ? DateOnly.FromDateTime(fin) : null,
            IdClient = IdClient,
            StatutFacture = StatutFacture,
            Tri = tri,
            TriDescendant = triDescendant,
        };

        return _executeur.ExecuterAsync<IRechercheFactures, ResultatPagine<FactureDto>>(
            (service, jeton) => service.RechercherAsync(criteres, jeton),
            jetonAnnulation);
    }

    protected override void ReinitialiserFiltres()
    {
        DateFactureDebut = null;
        DateFactureFin = null;
        IdClient = null;
        StatutFacture = null;
    }
}
