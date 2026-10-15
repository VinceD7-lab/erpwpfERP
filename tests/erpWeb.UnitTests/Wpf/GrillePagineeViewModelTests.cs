using erpWeb.Core.Communs;
using erpWpf.Wpf.ViewModels.Communs;

namespace erpWeb.UnitTests.Wpf;

public sealed class GrillePagineeViewModelTests
{
    [Fact]
    public async Task Charger_ResultatDuService_AlimenteLaGrilleEtLaPagination()
    {
        using var grille = new GrilleTest(nombreTotal: 60);

        await grille.ChargerAsync();

        Assert.Equal(25, grille.Elements.Count);
        Assert.Equal(60, grille.NombreTotal);
        Assert.Equal(3, grille.NombrePages);
        Assert.Equal("Page 1 sur 3 · 60 élément(s)", grille.Resume);
    }

    [Fact]
    public async Task PageSuivante_DernierePageAtteinte_NEstPlusExecutable()
    {
        using var grille = new GrilleTest(nombreTotal: 30);
        await grille.ChargerAsync();

        await grille.PageSuivanteCommand.ExecuteAsync(null);

        Assert.Equal(2, grille.NumeroPage);
        Assert.False(grille.PageSuivanteCommand.CanExecute(null));
        Assert.True(grille.PagePrecedenteCommand.CanExecute(null));
    }

    [Fact]
    public async Task Trier_MemeColonneDeuxFois_InverseLeSensEtRevientEnPremierePage()
    {
        using var grille = new GrilleTest(nombreTotal: 60);
        await grille.ChargerAsync();
        await grille.PageSuivanteCommand.ExecuteAsync(null);

        await grille.TrierCommand.ExecuteAsync(nameof(ChampTest.Nom));
        await grille.TrierCommand.ExecuteAsync(nameof(ChampTest.Nom));

        Assert.Equal(ChampTest.Nom, grille.Tri);
        Assert.True(grille.TriDescendant);
        Assert.Equal(1, grille.DerniereRecherche.NumeroPage);
    }

    [Theory]
    [InlineData("Inconnu")]
    [InlineData("42")]
    [InlineData(null)]
    public async Task Trier_ColonneHorsListeBlanche_EstIgnore(string? colonne)
    {
        using var grille = new GrilleTest(nombreTotal: 10);
        await grille.ChargerAsync();
        var recherches = grille.NombreRecherches;

        await grille.TrierCommand.ExecuteAsync(colonne);

        Assert.Equal(ChampTest.Date, grille.Tri);
        Assert.Equal(recherches, grille.NombreRecherches);
    }

    [Fact]
    public async Task Reinitialiser_RetablitLeTriParDefautEtLesFiltres()
    {
        using var grille = new GrilleTest(nombreTotal: 10);
        await grille.TrierCommand.ExecuteAsync(nameof(ChampTest.Nom));

        await grille.ReinitialiserCommand.ExecuteAsync(null);

        Assert.Equal(ChampTest.Date, grille.Tri);
        Assert.True(grille.TriDescendant);
        Assert.True(grille.FiltresReinitialises);
    }

    public enum ChampTest
    {
        Date,
        Nom,
    }

    private sealed class GrilleTest : GrillePagineeViewModel<int, ChampTest>
    {
        private readonly int _nombreTotal;

        public GrilleTest(int nombreTotal)
            : base(25, ChampTest.Date, triDescendantParDefaut: true)
        {
            _nombreTotal = nombreTotal;
        }

        public (int NumeroPage, ChampTest Tri, bool TriDescendant) DerniereRecherche { get; private set; }

        public int NombreRecherches { get; private set; }

        public bool FiltresReinitialises { get; private set; }

        protected override Task<ResultatPagine<int>> RechercherAsync(int numeroPage, int taillePage, ChampTest tri, bool triDescendant, CancellationToken jetonAnnulation)
        {
            DerniereRecherche = (numeroPage, tri, triDescendant);
            NombreRecherches++;
            var elements = Enumerable.Range((numeroPage - 1) * taillePage, Math.Max(0, Math.Min(taillePage, _nombreTotal - ((numeroPage - 1) * taillePage)))).ToList();
            return Task.FromResult(new ResultatPagine<int>(elements, numeroPage, taillePage, _nombreTotal));
        }

        protected override void ReinitialiserFiltres() => FiltresReinitialises = true;
    }
}
