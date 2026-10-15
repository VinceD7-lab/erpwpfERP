using System.Globalization;
using erpWeb.Core.Communs;
using erpWeb.Core.Echantillons;
using erpWeb.Core.Tournees;
using erpWpf.Wpf.Navigation;
using erpWpf.Wpf.ViewModels.Compte;
using erpWpf.Wpf.ViewModels.Echantillons;

namespace erpWeb.IntegrationTests;

/// <summary>Écran des résultats d'échantillons sur la base réelle : filtres, pagination, tri, permissions.</summary>
public sealed class EchantillonsTests : IClassFixture<HoteApplication>
{
    private readonly HoteApplication _hote;

    public EchantillonsTests(HoteApplication hote)
    {
        _hote = hote;
    }

    [Fact]
    public async Task Filtre_ParDateDeTournee_RestitueLEchantillonEtSonGroupe()
    {
        var code = $"ECH-{Guid.NewGuid():N}";
        await using (var contexte = _hote.CreerContexte(out var portee))
        {
            using var _ = portee;
            contexte.Echantillons.Add(new Echantillon
            {
                CodeBarresAnonyme = code,
                DatePrelevement = new DateTime(2026, 3, 3, 9, 0, 0, DateTimeKind.Utc),
                Filiere = "Grandes cultures",
                StatutAnalyse = StatutsAnalyse.Recu,
                Tournee = new TourneeRamassage { DateTournee = new DateOnly(2026, 3, 4), NomChauffeur = "Chauffeur Test" },
            });
            await contexte.SaveChangesAsync();
        }

        // L'application force fr-FR au démarrage (App.xaml.cs) ; les agents de CI sont en en-US.
        CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
        await _hote.ConnecterAdministrateurAsync();
        var grille = await OuvrirAsync();

        grille.DateTourneeDebut = new DateTime(2026, 3, 4);
        grille.DateTourneeFin = new DateTime(2026, 3, 4);
        await grille.FiltrerAsync();

        var resultat = Assert.Single(grille.Elements, element => element.CodeBarresAnonyme == code);
        Assert.Equal(new DateOnly(2026, 3, 4), resultat.DateTournee);
        Assert.StartsWith("04/03/2026 — ", ResultatsEchantillonsViewModel.Groupe(resultat), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Tri_ColonneHorsListeBlanche_EstIgnoreSansErreur()
    {
        await _hote.ConnecterAdministrateurAsync();
        var grille = await OuvrirAsync();

        await grille.TrierCommand.ExecuteAsync("Inconnu");

        Assert.Equal(ChampTriResultatsEchantillons.DateTournee, grille.Tri);
        Assert.Empty(grille.Erreurs);
    }

    [Fact]
    public async Task Ecran_UtilisateurStandard_EstAutorise()
    {
        await _hote.ConnecterUtilisateurStandardAsync();

        var grille = await OuvrirAsync();

        Assert.Equal(1, grille.NumeroPage);
        Assert.Equal("Tous les clients", grille.Clients[0].Libelle);
    }

    [Fact]
    public async Task Ecran_CompteSansRole_EstRefuse()
    {
        await _hote.ConnecterNouveauCompteAsync();
        var navigation = _hote.Obtenir<IServiceNavigation>();

        await navigation.NaviguerAsync(Destinations.Echantillons);

        Assert.IsType<AccesRefuseViewModel>(navigation.PageCourante);
    }

    private async Task<ResultatsEchantillonsViewModel> OuvrirAsync()
    {
        var navigation = _hote.Obtenir<IServiceNavigation>();
        await navigation.NaviguerAsync(Destinations.Echantillons);
        return Assert.IsType<ResultatsEchantillonsViewModel>(navigation.PageCourante);
    }
}
