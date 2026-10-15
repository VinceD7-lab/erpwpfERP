using erpWeb.Core.Communs;
using erpWeb.Core.Echantillons;
using erpWpf.Wpf.Services;
using erpWpf.Wpf.ViewModels.Communs;

namespace erpWpf.Wpf.ViewModels.Clients;

/// <summary>
/// Échantillons du client sélectionné (ex-îlot tableau-echantillons-client) : aucune requête tant qu'aucun client
/// n'est sélectionné ; une réponse arrivée après un changement de sélection est ignorée par la grille.
/// </summary>
public sealed class EchantillonsClientViewModel : GrillePagineeViewModel<ResultatEchantillonDto, ChampTriResultatsEchantillons>
{
    public const int TaillePageParDefaut = 10;

    private readonly IExecuteurOperations _executeur;

    public EchantillonsClientViewModel(IExecuteurOperations executeur)
        : base(TaillePageParDefaut, ChampTriResultatsEchantillons.DateTournee, triDescendantParDefaut: true, taillesPage: [10, 25, 50])
    {
        _executeur = executeur;
    }

    public int? IdClient { get; private set; }

    public Task AfficherClientAsync(int? idClient)
    {
        IdClient = idClient;
        return FiltrerAsync();
    }

    /// <summary>Sélection depuis la liste : passe par la commande, dont les erreurs atteignent le gestionnaire global.</summary>
    public void SelectionnerClient(int? idClient)
    {
        IdClient = idClient;
        FiltrerCommand.Execute(null);
    }

    public override Task InitialiserAsync(string? parametre, CancellationToken jetonAnnulation) => Task.CompletedTask;

    protected override Task<ResultatPagine<ResultatEchantillonDto>> RechercherAsync(
        int numeroPage, int taillePage, ChampTriResultatsEchantillons tri, bool triDescendant, CancellationToken jetonAnnulation)
    {
        if (IdClient is not { } idClient)
        {
            return Task.FromResult(new ResultatPagine<ResultatEchantillonDto>([], 1, taillePage, 0));
        }

        var criteres = new CriteresResultatsEchantillons
        {
            NumeroPage = numeroPage,
            TaillePage = taillePage,
            IdClient = idClient,
            Tri = tri,
            TriDescendant = triDescendant,
        };
        return _executeur.ExecuterAsync<IRechercheResultatsEchantillons, ResultatPagine<ResultatEchantillonDto>>(
            (service, jeton) => service.RechercherAsync(criteres, jeton),
            jetonAnnulation);
    }
}
