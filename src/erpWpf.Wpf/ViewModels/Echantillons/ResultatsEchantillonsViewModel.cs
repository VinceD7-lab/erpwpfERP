using CommunityToolkit.Mvvm.ComponentModel;
using erpWeb.Core.Communs;
using erpWeb.Core.Echantillons;
using erpWpf.Wpf.Services;
using erpWpf.Wpf.ViewModels.Communs;

namespace erpWpf.Wpf.ViewModels.Echantillons;

/// <summary>
/// Résultats d'analyse des échantillons (ex-Echantillons/Index, îlot tableau-resultats-echantillons) :
/// filtres par période de tournée, client et statut ; lignes regroupées par « date de tournée — client ».
/// </summary>
public sealed partial class ResultatsEchantillonsViewModel : GrillePagineeViewModel<ResultatEchantillonDto, ChampTriResultatsEchantillons>
{
    private readonly IExecuteurOperations _executeur;

    public ResultatsEchantillonsViewModel(IExecuteurOperations executeur)
        : base(CriteresResultatsEchantillons.TaillePageParDefaut, ChampTriResultatsEchantillons.DateTournee, triDescendantParDefaut: true)
    {
        _executeur = executeur;
        Statuts = OptionsFiltre.Statuts(StatutsAnalyse.Tous, LibellesStatuts.Libelle);
    }

    public override string Titre => "Échantillons";

    public IReadOnlyList<OptionFiltre<string>> Statuts { get; }

    [ObservableProperty]
    public partial IReadOnlyList<OptionFiltre<int?>> Clients { get; private set; } = [];

    [ObservableProperty]
    public partial DateTime? DateTourneeDebut { get; set; }

    [ObservableProperty]
    public partial DateTime? DateTourneeFin { get; set; }

    [ObservableProperty]
    public partial int? IdClient { get; set; }

    [ObservableProperty]
    public partial string? StatutAnalyse { get; set; }

    public override async Task InitialiserAsync(string? parametre, CancellationToken jetonAnnulation)
    {
        Clients = await OptionsFiltre.ClientsAsync(_executeur, jetonAnnulation);
        await ChargerAsync();
    }

    protected override Task<ResultatPagine<ResultatEchantillonDto>> RechercherAsync(
        int numeroPage, int taillePage, ChampTriResultatsEchantillons tri, bool triDescendant, CancellationToken jetonAnnulation)
    {
        var criteres = new CriteresResultatsEchantillons
        {
            NumeroPage = numeroPage,
            TaillePage = taillePage,
            DateTourneeDebut = DateTourneeDebut is { } debut ? DateOnly.FromDateTime(debut) : null,
            DateTourneeFin = DateTourneeFin is { } fin ? DateOnly.FromDateTime(fin) : null,
            IdClient = IdClient,
            StatutAnalyse = StatutAnalyse,
            Tri = tri,
            TriDescendant = triDescendant,
        };

        return _executeur.ExecuterAsync<IRechercheResultatsEchantillons, ResultatPagine<ResultatEchantillonDto>>(
            (service, jeton) => service.RechercherAsync(criteres, jeton),
            jetonAnnulation);
    }

    protected override void ReinitialiserFiltres()
    {
        DateTourneeDebut = null;
        DateTourneeFin = null;
        IdClient = null;
        StatutAnalyse = null;
    }

    /// <summary>Libellé du groupe d'une ligne (ex-groupBy Tabulator).</summary>
    public static string Groupe(ResultatEchantillonDto resultat)
        => $"{(resultat.DateTournee is { } date ? date.ToString("d", System.Globalization.CultureInfo.CurrentCulture) : "Sans tournée")} — {resultat.RaisonSocialeClient ?? "Sans client"}";
}
