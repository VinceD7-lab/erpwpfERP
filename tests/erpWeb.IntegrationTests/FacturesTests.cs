using erpWeb.Core.Clients;
using erpWeb.Core.Communs;
using erpWeb.Core.Echantillons;
using erpWeb.Core.Factures;
using erpWeb.Core.Tournees;
using erpWpf.Wpf.Navigation;
using erpWpf.Wpf.Services;
using erpWpf.Wpf.ViewModels.Compte;
using erpWpf.Wpf.ViewModels.Factures;
using Microsoft.EntityFrameworkCore;

namespace erpWeb.IntegrationTests;

/// <summary>Écrans des factures sur la base SQL Server réelle : génération, recherche, impression, permissions.</summary>
public sealed class FacturesTests : IClassFixture<HoteApplication>
{
    private readonly HoteApplication _hote;

    public FacturesTests(HoteApplication hote)
    {
        _hote = hote;
    }

    [Fact]
    public async Task Generer_EchantillonValideSansFacture_GenereLaFactureEtLAfficheDansLaGrille()
    {
        var code = await AjouterEchantillonValideAsync();
        await _hote.ConnecterAdministrateurAsync();
        var liste = await OuvrirListeAsync();

        await liste.GenererCommand.ExecuteAsync(null);

        Assert.Equal(1, await CompterFacturesAsync(code));
        Assert.True(_hote.Obtenir<IServiceNotifications>().EstVisible);
        Assert.Contains(liste.Elements, facture => facture.CodeBarresAnonyme == code);
    }

    [Fact]
    public async Task Generer_AppelsRepetes_NeDupliquentPasLesFactures()
    {
        var code = await AjouterEchantillonValideAsync();
        await _hote.ConnecterAdministrateurAsync();
        var liste = await OuvrirListeAsync();

        await liste.GenererCommand.ExecuteAsync(null);
        await liste.GenererCommand.ExecuteAsync(null);

        Assert.Equal(1, await CompterFacturesAsync(code));
    }

    [Fact]
    public async Task Ecran_CompteSansPermission_EstRefuse()
    {
        await _hote.ConnecterNouveauCompteAsync();
        var navigation = _hote.Obtenir<IServiceNavigation>();

        await navigation.NaviguerAsync(Destinations.Factures);

        Assert.IsType<AccesRefuseViewModel>(navigation.PageCourante);
    }

    [Fact]
    public async Task Generer_UtilisateurStandard_EstAutorise()
    {
        await _hote.ConnecterUtilisateurStandardAsync();

        var liste = await OuvrirListeAsync();

        Assert.True(liste.PeutGenerer);
        Assert.True(liste.GenererCommand.CanExecute(null));
    }

    [Fact]
    public async Task Recherche_FiltreParClient_RestitueLaFactureAvecSonClient()
    {
        var code = await AjouterEchantillonValideAsync();
        await _hote.ConnecterAdministrateurAsync();
        var liste = await OuvrirListeAsync();
        await liste.GenererCommand.ExecuteAsync(null);
        var facture = liste.Elements.Single(element => element.CodeBarresAnonyme == code);

        liste.IdClient = facture.CodeClient;
        await liste.FiltrerAsync();

        Assert.All(liste.Elements, element => Assert.Equal(facture.NomClient, element.NomClient));
        Assert.Contains(liste.Clients, option => option.Valeur == facture.CodeClient);
    }

    [Fact]
    public async Task Tri_ParMontantPuisColonneInconnue_ConserveLeDernierTriValide()
    {
        await _hote.ConnecterAdministrateurAsync();
        var liste = await OuvrirListeAsync();

        await liste.TrierCommand.ExecuteAsync(nameof(ChampTriFactures.MontantToutesTaxesComprises));
        await liste.TrierCommand.ExecuteAsync("MontantHorsTaxe; DROP TABLE Factures");

        Assert.Equal(ChampTriFactures.MontantToutesTaxesComprises, liste.Tri);
        Assert.Empty(liste.Erreurs);
    }

    [Fact]
    public async Task Impression_FactureExistante_AfficheLApercuEtLEnvoieALImprimante()
    {
        var code = await AjouterEchantillonValideAsync();
        await _hote.ConnecterAdministrateurAsync();
        var liste = await OuvrirListeAsync();
        await liste.GenererCommand.ExecuteAsync(null);
        var facture = liste.Elements.Single(element => element.CodeBarresAnonyme == code);
        var navigation = _hote.Obtenir<IServiceNavigation>();

        await navigation.NaviguerAsync(Destinations.ImpressionFacture(facture.IdFacture));
        var apercu = Assert.IsType<ImpressionFactureViewModel>(navigation.PageCourante);
        apercu.ImprimerCommand.Execute(null);

        Assert.Equal(facture.NumeroFacture, apercu.Impression!.Facture.NumeroFacture);
        Assert.Equal("erpWpf Tests", apercu.Impression.NomEmetteur);
        Assert.Equal(facture.IdFacture, Assert.IsType<ImpressionTest>(_hote.Obtenir<IServiceImpression>()).DerniereFacture!.Facture.IdFacture);
    }

    [Fact]
    public async Task Impression_Retour_RechargeLaListeDesFactures()
    {
        var code = await AjouterEchantillonValideAsync();
        await _hote.ConnecterAdministrateurAsync();
        var liste = await OuvrirListeAsync();
        await liste.GenererCommand.ExecuteAsync(null);
        var navigation = _hote.Obtenir<IServiceNavigation>();
        await navigation.NaviguerAsync(Destinations.ImpressionFacture(liste.Elements.Single(element => element.CodeBarresAnonyme == code).IdFacture));
        var apercu = Assert.IsType<ImpressionFactureViewModel>(navigation.PageCourante);

        await apercu.RetourCommand.ExecuteAsync(null);

        var retour = Assert.IsType<ListeFacturesViewModel>(navigation.PageCourante);
        Assert.NotEmpty(retour.Elements);
        Assert.NotEmpty(retour.Clients);
    }

    [Fact]
    public async Task Impression_FactureInconnue_RevientALaListeAvecUnMessage()
    {
        await _hote.ConnecterAdministrateurAsync();
        var navigation = _hote.Obtenir<IServiceNavigation>();

        await navigation.NaviguerAsync(Destinations.ImpressionFacture(int.MaxValue));

        var liste = Assert.IsType<ListeFacturesViewModel>(navigation.PageCourante);
        Assert.True(_hote.Obtenir<IServiceNotifications>().EstErreur);
        Assert.NotEmpty(liste.Clients);
    }

    private async Task<ListeFacturesViewModel> OuvrirListeAsync()
    {
        var navigation = _hote.Obtenir<IServiceNavigation>();
        await navigation.NaviguerAsync(Destinations.Factures);
        return Assert.IsType<ListeFacturesViewModel>(navigation.PageCourante);
    }

    /// <summary>Échantillon analysé et validé, rattaché à un client et à une tournée : facturable.</summary>
    private async Task<string> AjouterEchantillonValideAsync()
    {
        var code = $"ECH-{Guid.NewGuid():N}";
        await using var contexte = _hote.CreerContexte(out var portee);
        using var _ = portee;
        contexte.Echantillons.Add(new Echantillon
        {
            CodeBarresAnonyme = code,
            DatePrelevement = new DateTime(2026, 3, 3, 9, 0, 0, DateTimeKind.Utc),
            Filiere = "Elevage",
            StatutAnalyse = StatutsAnalyse.Termine,
            Client = new Client { RaisonSociale = $"Client {Guid.NewGuid():N}", Ville = "Lyon", Pays = "France" },
            Tournee = new TourneeRamassage { DateTournee = new DateOnly(2026, 3, 3), NomChauffeur = "Chauffeur Test" },
            ResultatAgronomie = new ResultatAgronomie { DateValidation = new DateTime(2026, 3, 5, 10, 0, 0) },
        });
        await contexte.SaveChangesAsync();
        return code;
    }

    private async Task<int> CompterFacturesAsync(string codeEchantillon)
    {
        await using var contexte = _hote.CreerContexte(out var portee);
        using var _ = portee;
        return await contexte.Factures.CountAsync(facture => facture.Echantillon!.CodeBarresAnonyme == codeEchantillon);
    }
}
