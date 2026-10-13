using erpWeb.Core.Autorisation;
using erpWeb.Core.Clients;
using erpWeb.Core.Communs;
using erpWeb.Core.PlanningAnalyses;
using erpWeb.Core.TableauDeBord;
using erpWeb.Core.Utilisateurs;
using erpWeb.UnitTests.Outils;
using erpWpf.Wpf.Navigation;
using erpWpf.Wpf.Securite;
using erpWpf.Wpf.Services;
using erpWpf.Wpf.ViewModels;
using erpWpf.Wpf.ViewModels.Clients;
using erpWpf.Wpf.ViewModels.Compte;
using erpWpf.Wpf.ViewModels.PlanningAnalyses;
using erpWpf.Wpf.ViewModels.TableauDeBord;
using Moq;

namespace erpWeb.UnitTests.Wpf;

public sealed class ViewModelsTests
{
    private static readonly SessionDto _session = new("id-1", "camille@erpweb.local", "Camille Martin", "tampon", new HashSet<string>());

    [Fact]
    public async Task Connexion_IdentifiantsValides_OuvreLaSessionEtVideLeMotDePasse()
    {
        var authentification = new Mock<IServiceAuthentification>();
        authentification.Setup(service => service.ConnecterAsync("camille@erpweb.local", "Secret#2026", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ResultatOperation<SessionDto>.Succes(_session));
        var session = new SessionUtilisateur();
        using var connexion = new ConnexionViewModel(new ExecuteurOperationsTest().Ajouter(authentification.Object), session)
        {
            Email = "camille@erpweb.local",
            MotDePasse = "Secret#2026",
        };

        await connexion.ConnecterCommand.ExecuteAsync(null);

        Assert.True(session.EstOuverte);
        Assert.Empty(connexion.MotDePasse);
        Assert.Empty(connexion.Erreurs);
    }

    [Fact]
    public async Task Connexion_Refusee_AfficheLErreurSansOuvrirDeSession()
    {
        var authentification = new Mock<IServiceAuthentification>();
        authentification.Setup(service => service.ConnecterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ResultatOperation<SessionDto>.Echec(ServiceAuthentification.MessageCompteVerrouille));
        var session = new SessionUtilisateur();
        using var connexion = new ConnexionViewModel(new ExecuteurOperationsTest().Ajouter(authentification.Object), session)
        {
            Email = "camille@erpweb.local",
            MotDePasse = "Mauvais",
        };

        await connexion.ConnecterCommand.ExecuteAsync(null);

        Assert.False(session.EstOuverte);
        Assert.Equal([ServiceAuthentification.MessageCompteVerrouille], connexion.Erreurs);
    }

    [Fact]
    public void Connexion_ChampsVides_NEstPasExecutable()
    {
        using var connexion = new ConnexionViewModel(new ExecuteurOperationsTest(), new SessionUtilisateur());

        Assert.False(connexion.ConnecterCommand.CanExecute(null));
    }

    [Fact]
    public void Menu_EntreesNonAutorisees_SontMasqueesAvecLeurSection()
    {
        var autorisation = new Mock<IAutorisation>();
        autorisation.Setup(service => service.APermission(It.IsAny<string?>())).Returns<string?>(permission => permission is null or Permissions.Clients.Lire);
        EntreeMenu[] entrees =
        [
            new(10, "Tableau de bord", "tableau-de-bord", Destinations.TableauDeBord, null),
            new(30, "Clients", "clients", Destinations.Clients, Permissions.Clients.Lire),
            new(100, "Utilisateurs", "utilisateurs", Destinations.Utilisateurs, Permissions.Utilisateurs.Lire, "ADMINISTRATION"),
        ];

        using var fenetre = new FenetrePrincipaleViewModel(
            Mock.Of<IServiceNavigation>(),
            entrees,
            autorisation.Object,
            new SessionUtilisateur(),
            Mock.Of<IServiceNotifications>(),
            Mock.Of<IPreferencesLocales>());

        Assert.Equal(["Tableau de bord", "Clients"], fenetre.Menu.Select(element => element.Libelle));
    }

    [Fact]
    public void Menu_SectionAutorisee_AfficheSonTitreAvantSesEntrees()
    {
        var autorisation = new Mock<IAutorisation>();
        autorisation.Setup(service => service.APermission(It.IsAny<string?>())).Returns(true);
        EntreeMenu[] entrees =
        [
            new(110, "Journal d'audit", "journal", Destinations.JournalAudit, Permissions.JournalAudit.Lire, "ADMINISTRATION"),
            new(100, "Utilisateurs", "utilisateurs", Destinations.Utilisateurs, Permissions.Utilisateurs.Lire, "ADMINISTRATION"),
        ];

        using var fenetre = new FenetrePrincipaleViewModel(
            Mock.Of<IServiceNavigation>(),
            entrees,
            autorisation.Object,
            new SessionUtilisateur(),
            Mock.Of<IServiceNotifications>(),
            Mock.Of<IPreferencesLocales>());

        Assert.Equal(["ADMINISTRATION", "Utilisateurs", "Journal d'audit"], fenetre.Menu.Select(element => element.Libelle));
        Assert.True(fenetre.Menu[0].EstSection);
    }

    [Fact]
    public async Task Widget_Raccourcis_SontFiltresParPermission()
    {
        var widget = new Mock<IWidgetTableauDeBord>();
        widget.SetupGet(w => w.Nom).Returns("raccourcis");
        widget.Setup(w => w.ObtenirDonneesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new DonneesRaccourcis(
        [
            new Raccourci("Paramètres", Icones.Parametres, Destinations.Parametres, Permissions.Parametres.Lire),
            new Raccourci("Journal", Icones.Journal, Destinations.JournalAudit, Permissions.JournalAudit.Lire),
        ]));
        var autorisation = new Mock<IAutorisation>();
        autorisation.Setup(service => service.APermission(Permissions.JournalAudit.Lire)).Returns(true);
        var executeur = new ExecuteurOperationsTest().Ajouter<IEnumerable<IWidgetTableauDeBord>>([widget.Object]);
        var vue = new WidgetViewModel(new DescriptionWidget("raccourcis", "Raccourcis", Icones.Raccourcis, 40, null), executeur, autorisation.Object, Mock.Of<IServiceNavigation>());

        await vue.RafraichirAsync(CancellationToken.None);

        var raccourcis = Assert.IsType<DonneesRaccourcis>(vue.Donnees);
        Assert.Equal(["Journal"], raccourcis.Raccourcis.Select(raccourci => raccourci.Libelle));
    }

    [Fact]
    public async Task Widget_EchecDuService_AfficheLeMessageSansLever()
    {
        var widget = new Mock<IWidgetTableauDeBord>();
        widget.SetupGet(w => w.Nom).Returns("activite");
        widget.Setup(w => w.ObtenirDonneesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException());
        var executeur = new ExecuteurOperationsTest().Ajouter<IEnumerable<IWidgetTableauDeBord>>([widget.Object]);
        var vue = new WidgetViewModel(new DescriptionWidget("activite", "Activité", Icones.Activite, 20, null), executeur, Mock.Of<IAutorisation>(), Mock.Of<IServiceNavigation>());

        await vue.RafraichirAsync(CancellationToken.None);

        Assert.Equal(WidgetViewModel.MessageEchec, vue.Erreur);
        Assert.False(vue.EstOccupe);
    }

    [Fact]
    public async Task LigneClient_EditionValidee_EnregistreTousLesChampsDontLesChampsNonAffiches()
    {
        var service = new Mock<IServiceClients>();
        service.Setup(s => s.ListerAsync(It.IsAny<CancellationToken>())).ReturnsAsync([new ClientDto { Id = 4, RaisonSociale = "Acier SA", Adresse2 = "Bâtiment B", Telephone2 = "0102030405" }]);
        service.Setup(s => s.ModifierAsync(It.IsAny<ModificationClientDto>(), It.IsAny<CancellationToken>())).ReturnsAsync(ResultatOperation.Succes());
        service.Setup(s => s.ObtenirAsync(4, It.IsAny<CancellationToken>())).ReturnsAsync(new ClientDto { Id = 4, RaisonSociale = "Acier Lyon", Adresse2 = "Bâtiment B" });
        using var liste = CreerListeClients(service.Object);
        await liste.InitialiserAsync(null, CancellationToken.None);
        var ligne = liste.Clients.Single();

        ligne.BeginEdit();
        ligne.RaisonSociale = "Acier Lyon";
        ligne.EndEdit();

        service.Verify(s => s.ModifierAsync(
            It.Is<ModificationClientDto>(modification => modification.Id == 4 && modification.RaisonSociale == "Acier Lyon" && modification.Adresse2 == "Bâtiment B" && modification.Telephone2 == "0102030405"),
            It.IsAny<CancellationToken>()), Times.Once);
        Assert.Null(ligne.Erreur);
    }

    [Fact]
    public async Task LigneClient_EditionRefusee_SignaleLaLigneEnErreur()
    {
        var service = new Mock<IServiceClients>();
        service.Setup(s => s.ListerAsync(It.IsAny<CancellationToken>())).ReturnsAsync([new ClientDto { Id = 4, RaisonSociale = "Acier SA" }]);
        service.Setup(s => s.ModifierAsync(It.IsAny<ModificationClientDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ResultatOperation.Echec("La raison sociale est obligatoire."));
        using var liste = CreerListeClients(service.Object);
        await liste.InitialiserAsync(null, CancellationToken.None);
        var ligne = liste.Clients.Single();

        ligne.BeginEdit();
        ligne.RaisonSociale = string.Empty;
        ligne.EndEdit();

        Assert.Equal("La raison sociale est obligatoire.", ligne.Erreur);
    }

    [Fact]
    public async Task LigneClient_EditionAnnulee_RestaureLesValeursSansEnregistrer()
    {
        var service = new Mock<IServiceClients>();
        service.Setup(s => s.ListerAsync(It.IsAny<CancellationToken>())).ReturnsAsync([new ClientDto { Id = 4, RaisonSociale = "Acier SA", Ville = "Lyon" }]);
        using var liste = CreerListeClients(service.Object);
        await liste.InitialiserAsync(null, CancellationToken.None);
        var ligne = liste.Clients.Single();

        ligne.BeginEdit();
        ligne.Ville = "Paris";
        ligne.CancelEdit();

        Assert.Equal("Lyon", ligne.Ville);
        service.Verify(s => s.ModifierAsync(It.IsAny<ModificationClientDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ListeClients_Recherche_FiltreSurLesColonnesAffichees()
    {
        var service = new Mock<IServiceClients>();
        service.Setup(s => s.ListerAsync(It.IsAny<CancellationToken>())).ReturnsAsync(
        [
            new ClientDto { Id = 1, RaisonSociale = "Acier SA", Ville = "Lyon" },
            new ClientDto { Id = 2, RaisonSociale = "Bois SARL", Ville = "Nantes" },
        ]);
        using var liste = CreerListeClients(service.Object);
        await liste.InitialiserAsync(null, CancellationToken.None);

        liste.Recherche = "nant";

        Assert.Equal(["Bois SARL"], liste.Clients.Select(client => client.RaisonSociale));
    }

    [Fact]
    public async Task Planning_CaseModifiee_SauvegardeLeMoisCompletSansLesCasesVides()
    {
        var service = new Mock<IServicePlanningAnalyses>();
        var lundi = new DateOnly(2031, 3, 3);
        var mardi = new DateOnly(2031, 3, 4);
        service.Setup(s => s.ObtenirMoisAsync(null, null, It.IsAny<CancellationToken>())).ReturnsAsync(new PlanningMensuelDto(
            2031,
            3,
            [new JourPlanningDto(lundi, true), new JourPlanningDto(mardi, true)],
            [new ClientPlanningDto(7, "Acier SA")],
            [new CasePlanningDto { IdClient = 7, Date = lundi, TypeAnalyse = TypesAnalysePlanning.A }]));
        service.Setup(s => s.SauvegarderMoisAsync(It.IsAny<SauvegardePlanningDto>(), It.IsAny<CancellationToken>())).ReturnsAsync(ResultatOperation.Succes());
        using var planning = CreerPlanning(service.Object, peutGerer: true);
        await planning.InitialiserAsync(null, CancellationToken.None);

        planning.Lignes[0].Cases[1].TypeAnalyse = TypesAnalysePlanning.B;
        await planning.SauvegarderCommand.ExecuteAsync(null);

        service.Verify(s => s.SauvegarderMoisAsync(
            It.Is<SauvegardePlanningDto>(sauvegarde => sauvegarde.Annee == 2031 && sauvegarde.Mois == 3 && sauvegarde.Cases.Count == 2),
            It.IsAny<CancellationToken>()), Times.Once);
        Assert.False(planning.EstModifie);
    }

    [Fact]
    public async Task Planning_SansPermissionDeGestion_LesCasesNeSontPasModifiables()
    {
        var service = new Mock<IServicePlanningAnalyses>();
        var lundi = new DateOnly(2031, 3, 3);
        service.Setup(s => s.ObtenirMoisAsync(null, null, It.IsAny<CancellationToken>())).ReturnsAsync(new PlanningMensuelDto(
            2031, 3, [new JourPlanningDto(lundi, true)], [new ClientPlanningDto(7, "Acier SA")], []));
        using var planning = CreerPlanning(service.Object, peutGerer: false);

        await planning.InitialiserAsync(null, CancellationToken.None);

        Assert.False(planning.Lignes[0].Cases[0].EstModifiable);
        Assert.False(planning.SauvegarderCommand.CanExecute(null));
    }

    [Fact]
    public async Task Planning_ModificationsNonEnregistrees_DemandeConfirmationAvantDeQuitter()
    {
        var service = new Mock<IServicePlanningAnalyses>();
        var lundi = new DateOnly(2031, 3, 3);
        service.Setup(s => s.ObtenirMoisAsync(null, null, It.IsAny<CancellationToken>())).ReturnsAsync(new PlanningMensuelDto(
            2031, 3, [new JourPlanningDto(lundi, true)], [new ClientPlanningDto(7, "Acier SA")], []));
        var dialogues = new Mock<IServiceDialogues>();
        dialogues.Setup(d => d.Confirmer(PlanningAnalysesViewModel.MessageModificationsPerdues, It.IsAny<string>())).Returns(false);
        using var planning = CreerPlanning(service.Object, peutGerer: true, dialogues.Object);
        await planning.InitialiserAsync(null, CancellationToken.None);

        planning.Lignes[0].Cases[0].TypeAnalyse = TypesAnalysePlanning.C;

        Assert.False(planning.PeutQuitter());
    }

    private static ListeClientsViewModel CreerListeClients(IServiceClients service)
    {
        var autorisation = new Mock<IAutorisation>();
        autorisation.Setup(a => a.APermission(It.IsAny<string?>())).Returns<string?>(permission => permission != Permissions.Echantillons.Lire);
        var executeur = new ExecuteurOperationsTest().Ajouter(service);
        return new ListeClientsViewModel(
            executeur,
            Mock.Of<IServiceNavigation>(),
            Mock.Of<IServiceDialogues>(),
            Mock.Of<IServiceNotifications>(),
            autorisation.Object,
            new EchantillonsClientViewModel(executeur));
    }

    private static PlanningAnalysesViewModel CreerPlanning(IServicePlanningAnalyses service, bool peutGerer, IServiceDialogues? dialogues = null)
    {
        var autorisation = new Mock<IAutorisation>();
        autorisation.Setup(a => a.APermission(Permissions.PlanningAnalyses.Gerer)).Returns(peutGerer);
        return new PlanningAnalysesViewModel(
            new ExecuteurOperationsTest().Ajouter(service),
            dialogues ?? Mock.Of<IServiceDialogues>(),
            Mock.Of<IServiceNotifications>(),
            autorisation.Object);
    }
}
