using System.Reflection;
using erpWeb.Core.Communs;
using erpWeb.Infrastructure.Donnees;
using erpWpf.Wpf.Hote;
using erpWpf.Wpf.ViewModels.Communs;
using NetArchTest.Rules;

namespace erpWeb.UnitTests.Architecture;

/// <summary>
/// Vérifie automatiquement les règles d'architecture du socle (dépendances entre couches,
/// inversion de dépendances, MVVM, absence d'accès direct au système de fichiers dans Core).
/// </summary>
public sealed class ArchitectureTests
{
    private const string EspaceViewModels = "erpWpf.Wpf.ViewModels";

    private static readonly Assembly _core = typeof(IAppDbContext).Assembly;
    private static readonly Assembly _infrastructure = typeof(AppDbContext).Assembly;
    private static readonly Assembly _presentation = typeof(ConstructionHote).Assembly;

    [Theory]
    [InlineData("erpWeb.Infrastructure")]
    [InlineData("erpWpf.Wpf")]
    [InlineData("PresentationFramework")]
    [InlineData("PresentationCore")]
    [InlineData("WindowsBase")]
    [InlineData("Microsoft.EntityFrameworkCore.SqlServer")]
    [InlineData("Microsoft.Data.SqlClient")]
    [InlineData("Microsoft.EntityFrameworkCore.Sqlite")]
    [InlineData("Microsoft.Data.Sqlite")]
    [InlineData("MailKit")]
    [InlineData("MimeKit")]
    [InlineData("ClosedXML")]
    public void Core_NeReferencePasLAssemblage(string assemblageInterdit)
    {
        var references = _core.GetReferencedAssemblies().Select(reference => reference.Name);

        Assert.DoesNotContain(assemblageInterdit, references);
    }

    [Fact]
    public void Core_NeDependPasDesAutresCouches()
    {
        var resultat = Types.InAssembly(_core)
            .ShouldNot()
            .HaveDependencyOnAny("erpWeb.Infrastructure", "erpWpf.Wpf", "System.Windows")
            .GetResult();

        AssertReussi(resultat);
    }

    [Fact]
    public void Core_NAccedePasDirectementAuSystemeDeFichiers()
    {
        var resultat = Types.InAssembly(_core)
            .ShouldNot()
            .HaveDependencyOnAny("System.IO.File", "System.IO.Directory", "System.Net.Http")
            .GetResult();

        AssertReussi(resultat);
    }

    [Fact]
    public void Presentation_NeDependDInfrastructureQueDansLaCompositionRoot()
    {
        var resultat = Types.InAssembly(_presentation)
            .That()
            .DoNotResideInNamespace("erpWpf.Wpf.Hote")
            .And()
            .DoNotHaveName("App")
            .ShouldNot()
            .HaveDependencyOn("erpWeb.Infrastructure")
            .GetResult();

        AssertReussi(resultat);
    }

    [Fact]
    public void ViewModels_NAccedentPasDirectementAuxDonnees()
    {
        var resultat = Types.InAssembly(_presentation)
            .That()
            .ResideInNamespace(EspaceViewModels)
            .ShouldNot()
            .HaveDependencyOnAny(typeof(IAppDbContext).FullName!, "Microsoft.EntityFrameworkCore")
            .GetResult();

        AssertReussi(resultat);
    }

    /// <summary>
    /// Les ViewModels restent testables sans interface graphique : pas de contrôles, de médias, de documents,
    /// de boîtes de dialogue ni de MessageBox. System.Windows.Input (ICommand) reste autorisé.
    /// </summary>
    [Fact]
    public void ViewModels_NeDependentPasDesTypesGraphiques()
    {
        var resultat = Types.InAssembly(_presentation)
            .That()
            .ResideInNamespace(EspaceViewModels)
            .ShouldNot()
            .HaveDependencyOnAny(
                "System.Windows.Controls",
                "System.Windows.Media",
                "System.Windows.Documents",
                "System.Windows.MessageBox",
                "System.Windows.Window",
                "Microsoft.Win32")
            .GetResult();

        AssertReussi(resultat);
    }

    [Fact]
    public void ViewModels_NUtilisentPasLeServiceLocator()
    {
        var resultat = Types.InAssembly(_presentation)
            .That()
            .ResideInNamespace(EspaceViewModels)
            .ShouldNot()
            .HaveDependencyOnAny(typeof(IServiceProvider).FullName!, "Microsoft.Extensions.DependencyInjection.IServiceScopeFactory")
            .GetResult();

        AssertReussi(resultat);
    }

    [Fact]
    public void ViewModels_ResidentDansLEspaceDeNomsViewModels()
    {
        // Ferme la porte de sortie des règles précédentes : un ViewModel placé ailleurs n'y serait plus soumis.
        var horsEspaceDeNoms = Types.InAssembly(_presentation)
            .That()
            .HaveNameEndingWith("ViewModel")
            .And()
            .DoNotResideInNamespace(EspaceViewModels)
            .GetTypes();

        Assert.Empty(horsEspaceDeNoms);
    }

    [Fact]
    public void Ecrans_HeritentDeViewModelBase()
    {
        var ecrans = Types.InAssembly(_presentation)
            .That()
            .ResideInNamespace(EspaceViewModels)
            .And()
            .ImplementInterface(typeof(INavigable))
            .GetTypes()
            .Where(type => !typeof(ViewModelBase).IsAssignableFrom(type))
            .ToList();

        Assert.Empty(ecrans);
    }

    [Fact]
    public void Infrastructure_SeulAppDbContextImplementeIAppDbContext()
    {
        var implementations = Types.InAssemblies([_core, _infrastructure, _presentation])
            .That()
            .ImplementInterface(typeof(IAppDbContext))
            .GetTypes()
            .ToList();

        Assert.Equal([typeof(AppDbContext)], implementations);
    }

    [Fact]
    public void LesInterfacesSontPrefixeesParI()
    {
        var resultat = Types.InAssemblies([_core, _presentation])
            .That()
            .AreInterfaces()
            .Should()
            .HaveNameStartingWith("I")
            .GetResult();

        AssertReussi(resultat);
    }

    private static void AssertReussi(TestResult resultat)
        => Assert.True(resultat.IsSuccessful, "Types en infraction : " + string.Join(", ", resultat.FailingTypeNames ?? []));
}
