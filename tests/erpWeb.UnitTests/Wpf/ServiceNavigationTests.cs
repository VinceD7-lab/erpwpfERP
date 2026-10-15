using erpWpf.Wpf.Navigation;
using erpWpf.Wpf.Securite;
using erpWpf.Wpf.Services;
using erpWpf.Wpf.ViewModels.Communs;
using erpWpf.Wpf.ViewModels.Compte;
using Moq;

namespace erpWeb.UnitTests.Wpf;

public sealed class ServiceNavigationTests
{
    private const string PermissionPage = "Module.Lire";

    private readonly Mock<IAutorisation> _autorisation = new();
    private readonly Mock<IServiceNotifications> _notifications = new();
    private readonly Dictionary<Type, Func<object>> _fabriques = [];
    private readonly PageTest _page = new();
    private readonly ActionTest _action = new();

    public ServiceNavigationTests()
    {
        _autorisation.Setup(autorisation => autorisation.APermission(It.IsAny<string?>())).Returns(true);
        _fabriques[typeof(PageTest)] = () => _page;
        _fabriques[typeof(ActionTest)] = () => _action;
        _fabriques[typeof(AccesRefuseViewModel)] = () => new AccesRefuseViewModel(Mock.Of<IServiceNavigation>());
    }

    [Fact]
    public async Task NaviguerAsync_RouteAvecParametre_AfficheLaPageEtLuiTransmetLeParametre()
    {
        var navigation = CreerNavigation();

        var resultat = await navigation.NaviguerAsync("Module/Modifier/12");

        Assert.True(resultat);
        Assert.Same(_page, navigation.PageCourante);
        Assert.Equal("Module/Modifier", navigation.RouteCourante);
        Assert.Equal("12", _page.Parametre);
    }

    [Fact]
    public async Task NaviguerAsync_RouteSimple_RetientLaRouteLaPlusLongueConnue()
    {
        var navigation = CreerNavigation();

        await navigation.NaviguerAsync("Module/Inconnu");

        Assert.Equal("Module", navigation.RouteCourante);
        Assert.Equal("Inconnu", _page.Parametre);
    }

    [Fact]
    public async Task NaviguerAsync_PermissionManquante_AfficheAccesRefuse()
    {
        _autorisation.Setup(autorisation => autorisation.APermission(PermissionPage)).Returns(false);
        var navigation = CreerNavigation();

        await navigation.NaviguerAsync("Module");

        Assert.IsType<AccesRefuseViewModel>(navigation.PageCourante);
        Assert.Null(_page.Parametre);
    }

    [Fact]
    public async Task NaviguerAsync_DestinationInconnue_SignaleLErreurSansChangerDePage()
    {
        var navigation = CreerNavigation();

        var resultat = await navigation.NaviguerAsync("Absent");

        Assert.False(resultat);
        Assert.Null(navigation.PageCourante);
        _notifications.Verify(notifications => notifications.Erreur(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task NaviguerAsync_Action_ExecuteSansChangerDePage()
    {
        var navigation = CreerNavigation();
        await navigation.NaviguerAsync("Module");

        await navigation.NaviguerAsync("Module/Telecharger/7");

        Assert.Same(_page, navigation.PageCourante);
        Assert.Equal("7", _action.Parametre);
    }

    [Fact]
    public async Task NaviguerAsync_PageQuiRefuseDEtreQuittee_ResteAffichee()
    {
        var navigation = CreerNavigation();
        await navigation.NaviguerAsync("Module");
        _page.PeutEtreQuittee = false;
        _fabriques[typeof(PageTest)] = () => new PageTest();

        var resultat = await navigation.NaviguerAsync("Module/Modifier/1");

        Assert.False(resultat);
        Assert.Same(_page, navigation.PageCourante);
    }

    [Fact]
    public async Task NaviguerAsync_DepuisUnePageAvecSonJeton_InitialiseLaNouvellePageAvecUnJetonActif()
    {
        var navigation = CreerNavigation();
        await navigation.NaviguerAsync("Module");
        var suivante = new PageTest();
        _fabriques[typeof(PageTest)] = () => suivante;

        // Comme un bouton « Retour » : la page courante navigue avec son propre jeton, annulé dès qu'elle est fermée.
        await navigation.NaviguerAsync("Module/Modifier/3", _page.JetonFermeture);

        Assert.True(_page.JetonFermeture.IsCancellationRequested);
        Assert.False(suivante.JetonInitialisation.IsCancellationRequested);
        Assert.Equal(suivante.JetonFermeture, suivante.JetonInitialisation);
    }

    [Fact]
    public async Task NaviguerAsync_NouvellePage_FermeLaPrecedente()
    {
        var navigation = CreerNavigation();
        await navigation.NaviguerAsync("Module");
        _fabriques[typeof(PageTest)] = () => new PageTest();

        await navigation.NaviguerAsync("Module/Modifier/2");

        Assert.True(_page.EstFermee);
    }

    private ServiceNavigation CreerNavigation()
    {
        var fabrique = new Mock<IFabriqueViewModels>();
        fabrique.Setup(f => f.Creer(It.IsAny<Type>())).Returns<Type>(type => _fabriques[type]());
        return new ServiceNavigation(
            [new DefinitionPage("Module", typeof(PageTest), PermissionPage), new DefinitionPage("Module/Modifier", typeof(PageTest), PermissionPage)],
            [new DefinitionAction("Module/Telecharger", typeof(ActionTest), PermissionPage)],
            fabrique.Object,
            _autorisation.Object,
            _notifications.Object);
    }

    private sealed class PageTest : ViewModelBase, INavigable, IConfirmationDepart
    {
        public string? Parametre { get; private set; }

        public bool PeutEtreQuittee { get; set; } = true;

        public bool EstFermee { get; private set; }

        public CancellationToken JetonInitialisation { get; private set; }

        public Task InitialiserAsync(string? parametre, CancellationToken jetonAnnulation)
        {
            Parametre = parametre;
            JetonInitialisation = jetonAnnulation;
            return Task.CompletedTask;
        }

        public bool PeutQuitter() => PeutEtreQuittee;

        protected override void SurFermeture() => EstFermee = true;
    }

    private sealed class ActionTest : IActionNavigation
    {
        public string? Parametre { get; private set; }

        public Task ExecuterAsync(string? parametre, CancellationToken jetonAnnulation)
        {
            Parametre = parametre;
            return Task.CompletedTask;
        }
    }
}
