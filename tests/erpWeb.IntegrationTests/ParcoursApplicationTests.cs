using erpWeb.Core.Autorisation;
using erpWeb.Core.Communs;
using erpWeb.Core.TableauDeBord;
using erpWeb.Core.Utilisateurs;
using erpWpf.Wpf.Navigation;
using erpWpf.Wpf.Securite;
using erpWpf.Wpf.ViewModels.Clients;
using erpWpf.Wpf.ViewModels.Compte;
using erpWpf.Wpf.ViewModels.TableauDeBord;
using erpWpf.Wpf.ViewModels.Utilisateurs;
using Microsoft.Extensions.DependencyInjection;

namespace erpWeb.IntegrationTests;

/// <summary>Parcours de bout en bout sur la composition réelle : connexion, navigation, permissions, session.</summary>
public sealed class ParcoursApplicationTests : IClassFixture<HoteApplication>
{
    private readonly HoteApplication _hote;

    public ParcoursApplicationTests(HoteApplication hote)
    {
        _hote = hote;
    }

    [Fact]
    public void Composition_ChaqueEcranEtChaqueAction_EstResolvable()
    {
        var types = _hote.Obtenir<IEnumerable<DefinitionPage>>().Select(page => page.TypeViewModel)
            .Concat(_hote.Obtenir<IEnumerable<DefinitionAction>>().Select(action => action.TypeAction))
            .Distinct()
            .ToList();

        Assert.All(types, type => Assert.NotNull(_hote.Services.GetRequiredService(type)));
        Assert.Contains(typeof(ListeClientsViewModel), types);
    }

    [Fact]
    public async Task Connexion_Administrateur_OuvreLaSessionAvecToutesLesPermissions()
    {
        var connexion = _hote.Obtenir<ConnexionViewModel>();
        connexion.Email = HoteApplication.EmailAdministrateur;
        connexion.MotDePasse = HoteApplication.MotDePasseAdministrateur;

        await connexion.ConnecterCommand.ExecuteAsync(null);

        Assert.True(_hote.Session.EstOuverte);
        Assert.Superset(new HashSet<string>(Permissions.Toutes), new HashSet<string>(_hote.Session.Session!.Permissions));
        Assert.Equal(Permissions.Toutes.Count, _hote.Session.Session.Permissions.Count);
    }

    [Fact]
    public async Task Connexion_MotDePasseIncorrect_AfficheLErreurSansOuvrirDeSession()
    {
        _hote.Session.Fermer();
        var connexion = _hote.Obtenir<ConnexionViewModel>();
        connexion.Email = HoteApplication.EmailAdministrateur;
        connexion.MotDePasse = "Incorrect#2026";

        await connexion.ConnecterCommand.ExecuteAsync(null);

        Assert.False(_hote.Session.EstOuverte);
        Assert.Equal([ServiceAuthentification.MessageIdentifiantsIncorrects], connexion.Erreurs);
    }

    [Fact]
    public async Task Inscription_NouveauCompte_OuvreLaSessionAvecLeRoleUtilisateur()
    {
        _hote.Session.Fermer();
        var inscription = _hote.Obtenir<InscriptionViewModel>();
        inscription.NomComplet = "Nouvel Inscrit";
        inscription.Email = $"inscrit-{Guid.NewGuid():N}@erpweb.local";
        inscription.MotDePasse = "Inscrit#2026";
        inscription.ConfirmationMotDePasse = "Inscrit#2026";

        await inscription.InscrireCommand.ExecuteAsync(null);

        Assert.True(_hote.Session.EstOuverte, string.Join(" ", inscription.Erreurs));
        Assert.Contains(Permissions.Clients.Lire, _hote.Session.Session!.Permissions);
        Assert.DoesNotContain(Permissions.Utilisateurs.Lire, _hote.Session.Session.Permissions);
    }

    [Fact]
    public async Task TableauDeBord_Administrateur_AfficheLesQuatreWidgetsAvecLeurDonnees()
    {
        await _hote.ConnecterAdministrateurAsync();
        var navigation = _hote.Obtenir<IServiceNavigation>();

        await navigation.NaviguerAsync(Destinations.TableauDeBord);

        var tableau = Assert.IsType<TableauDeBordViewModel>(navigation.PageCourante);
        Assert.Equal(["utilisateurs-actifs", "activite-audit", "derniers-documents", "raccourcis"], tableau.Widgets.Select(widget => widget.Nom));
        Assert.All(tableau.Widgets, widget => Assert.Null(widget.Erreur));
        Assert.IsType<DonneesGraphique>(tableau.Widgets[1].Donnees);
    }

    [Fact]
    public async Task TableauDeBord_CompteSansRole_NAfficheQueLesRaccourcisSansEntreeAutorisee()
    {
        await _hote.ConnecterNouveauCompteAsync();
        var navigation = _hote.Obtenir<IServiceNavigation>();

        await navigation.NaviguerAsync(Destinations.TableauDeBord);

        var tableau = Assert.IsType<TableauDeBordViewModel>(navigation.PageCourante);
        var raccourcis = Assert.IsType<DonneesRaccourcis>(Assert.Single(tableau.Widgets).Donnees);
        Assert.Empty(raccourcis.Raccourcis);
    }

    [Theory]
    [InlineData(Destinations.Utilisateurs)]
    [InlineData(Destinations.JournalAudit)]
    [InlineData(Destinations.Parametres)]
    [InlineData(Destinations.Clients)]
    [InlineData(Destinations.Factures)]
    public async Task Navigation_CompteSansPermission_AfficheAccesRefuse(string destination)
    {
        await _hote.ConnecterNouveauCompteAsync();
        var navigation = _hote.Obtenir<IServiceNavigation>();

        await navigation.NaviguerAsync(destination);

        Assert.IsType<AccesRefuseViewModel>(navigation.PageCourante);
    }

    [Fact]
    public async Task Navigation_UtilisateurStandard_AccedeAuxClientsMaisPasALAdministration()
    {
        await _hote.ConnecterUtilisateurStandardAsync();
        var navigation = _hote.Obtenir<IServiceNavigation>();

        await navigation.NaviguerAsync(Destinations.Clients);
        Assert.IsType<ListeClientsViewModel>(navigation.PageCourante);

        await navigation.NaviguerAsync(Destinations.CreationUtilisateur);
        Assert.IsType<AccesRefuseViewModel>(navigation.PageCourante);
    }

    [Fact]
    public async Task SurveillanceSession_CompteDesactive_FermeLaSession()
    {
        var session = await _hote.ConnecterUtilisateurStandardAsync();
        await using (var portee = _hote.Services.CreateAsyncScope())
        {
            await portee.ServiceProvider.GetRequiredService<IServiceUtilisateurs>().DefinirActivationAsync(session.IdUtilisateur, estActif: false);
        }

        await _hote.Obtenir<SurveillanceSession>().VerifierAsync();

        Assert.False(_hote.Session.EstOuverte);
    }

    [Fact]
    public async Task ReinitialisationMotDePasse_ParLAdministrateur_PermetLaConnexionAvecLeNouveauMotDePasse()
    {
        var compte = await _hote.ConnecterUtilisateurStandardAsync();
        await _hote.ConnecterAdministrateurAsync();
        var edition = _hote.Obtenir<EditionUtilisateurViewModel>();
        await edition.InitialiserAsync(compte.IdUtilisateur, CancellationToken.None);
        edition.NouveauMotDePasse = "Nouveau#Secret2026";

        await edition.ReinitialiserMotDePasseCommand.ExecuteAsync(null);

        Assert.Empty(edition.Erreurs);
        var reconnexion = await _hote.ConnecterAsync(compte.Email, "Nouveau#Secret2026");
        Assert.NotEqual(compte.TamponSecurite, reconnexion.TamponSecurite);
    }

    [Fact]
    public async Task Clients_AdministrateurCreeUnClient_LeClientApparaitDansLaListe()
    {
        await _hote.ConnecterAdministrateurAsync();
        var raisonSociale = $"Client parcours {Guid.NewGuid():N}";
        var edition = _hote.Obtenir<EditionClientViewModel>();
        edition.Champs.RaisonSociale = raisonSociale;
        edition.Champs.Ville = "Lyon";

        await edition.EnregistrerCommand.ExecuteAsync(null);

        var navigation = _hote.Obtenir<IServiceNavigation>();
        var liste = Assert.IsType<ListeClientsViewModel>(navigation.PageCourante);
        Assert.Contains(liste.Clients, client => client.RaisonSociale == raisonSociale);
    }
}
