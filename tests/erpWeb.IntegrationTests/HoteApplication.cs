using erpWeb.Core.Autorisation;
using erpWeb.Core.Utilisateurs;
using erpWeb.Infrastructure;
using erpWeb.Infrastructure.Donnees;
using erpWpf.Wpf.Hote;
using erpWpf.Wpf.Securite;
using erpWpf.Wpf.Services;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Internal;

namespace erpWeb.IntegrationTests;

/// <summary>
/// Composition réelle de l'application (ConstructionHote.AjouterServices) sur une base SQL Server temporaire
/// (migrations réelles), supprimée à la fin. Serveur : variable ERPWEB_TESTS_SQLSERVER (CI), sinon LocalDB.
/// Les services d'interface (dialogues, fichiers, impression) sont remplacés par des doubles de test.
/// </summary>
public sealed class HoteApplication : IAsyncLifetime
{
    public const string EmailAdministrateur = "admin-tests@erpweb.local";
    public const string MotDePasseAdministrateur = "Admin#Tests2026";
    public const string VariableServeurTests = "ERPWEB_TESTS_SQLSERVER";

    private const string ServeurParDefaut = @"Server=(localdb)\MSSQLLocalDB;Trusted_Connection=True;TrustServerCertificate=True";

    private readonly string _nomBase;
    private readonly string _chaineServeur;
    private readonly string _dossierTemporaire;
    private ServiceProvider _services = default!;

    public HoteApplication()
    {
        var identifiant = Guid.NewGuid().ToString("N");
        _nomBase = $"erpWeb_Tests_{identifiant}";
        _chaineServeur = Environment.GetEnvironmentVariable(VariableServeurTests) ?? ServeurParDefaut;
        _dossierTemporaire = Path.Combine(Path.GetTempPath(), $"erpWpf-tests-{identifiant}");
    }

    public IServiceProvider Services => _services;

    public DialoguesTest Dialogues { get; } = new();

    public FichiersTest Fichiers { get; private set; } = default!;

    public ISessionUtilisateur Session => _services.GetRequiredService<ISessionUtilisateur>();

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_dossierTemporaire);
        Fichiers = new FichiersTest(Path.Combine(_dossierTemporaire, "poste"));

        var configuration = new ConfigurationManager();
        configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:ParDefaut"] = new SqlConnectionStringBuilder(_chaineServeur) { InitialCatalog = _nomBase }.ConnectionString,
            ["Administrateur:Email"] = EmailAdministrateur,
            ["Administrateur:NomComplet"] = "Administrateur des tests",
            ["Administrateur:MotDePasse"] = MotDePasseAdministrateur,
            ["Stockage:Dossier"] = Path.Combine(_dossierTemporaire, "documents"),
            ["Stockage:TailleMaximaleOctets"] = "10485760",
            ["Facturation:TarifParDefaut"] = "40",
            ["Facturation:TauxTva"] = "20",
            ["Facturation:DelaiEcheanceEnJours"] = "30",
            ["Facturation:Devise"] = "EUR",
            ["Facturation:ModePaiementParDefaut"] = "Virement",
            ["Facturation:NomEmetteur"] = "erpWpf Tests",
        });

        // Environnement « Test » : ni secrets utilisateur ni données de démonstration.
        var environnement = new HostingEnvironment { EnvironmentName = "Test", ApplicationName = "erpWpf.Wpf", ContentRootPath = AppContext.BaseDirectory };
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        ConstructionHote.AjouterServices(services, configuration, environnement, new RepertoireDonneesLocales(Path.Combine(_dossierTemporaire, "profil")));
        services.AddSingleton<IServiceDialogues>(Dialogues);
        services.AddSingleton<IServiceFichiers>(Fichiers);
        services.AddSingleton<IServiceImpression, ImpressionTest>();

        // Mêmes contrôles que l'hôte en développement : durée de vie des services et résolution complète.
        _services = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        await _services.InitialiserDonneesAsync(appliquerMigrations: true);
    }

    public async Task DisposeAsync()
    {
        await _services.DisposeAsync();
        SupprimerBaseDeTest();
        if (Directory.Exists(_dossierTemporaire))
        {
            Directory.Delete(_dossierTemporaire, recursive: true);
        }
    }

    public T Obtenir<T>()
        where T : notnull
        => _services.GetRequiredService<T>();

    /// <summary>Ouvre la session du poste comme le ferait l'écran de connexion.</summary>
    public async Task<SessionDto> ConnecterAsync(string email, string motDePasse)
    {
        await using var portee = _services.CreateAsyncScope();
        var resultat = await portee.ServiceProvider.GetRequiredService<IServiceAuthentification>().ConnecterAsync(email, motDePasse);
        Assert.True(resultat.Reussi, string.Join(" ", resultat.Erreurs));
        Session.Ouvrir(resultat.Valeur!);
        return resultat.Valeur!;
    }

    public Task<SessionDto> ConnecterAdministrateurAsync() => ConnecterAsync(EmailAdministrateur, MotDePasseAdministrateur);

    /// <summary>Crée un compte (sans rôle par défaut, donc sans permission) et ouvre sa session.</summary>
    public async Task<SessionDto> ConnecterNouveauCompteAsync(params string[] roles)
    {
        var email = $"compte-{Guid.NewGuid():N}@erpweb.local";
        const string motDePasse = "Compte#Tests2026";
        await using (var portee = _services.CreateAsyncScope())
        {
            var creation = await portee.ServiceProvider.GetRequiredService<IServiceUtilisateurs>().CreerAsync(new CreationUtilisateurDto
            {
                Email = email,
                NomComplet = "Compte de test",
                MotDePasse = motDePasse,
                Roles = [.. roles],
            });
            Assert.True(creation.Reussi, string.Join(" ", creation.Erreurs));
        }

        return await ConnecterAsync(email, motDePasse);
    }

    public Task<SessionDto> ConnecterUtilisateurStandardAsync() => ConnecterNouveauCompteAsync(RolesApplication.Utilisateur);

    public AppDbContext CreerContexte(out IServiceScope portee)
    {
        portee = _services.CreateScope();
        return portee.ServiceProvider.GetRequiredService<AppDbContext>();
    }

    private void SupprimerBaseDeTest()
    {
        SqlConnection.ClearAllPools();

        var chaineMaster = new SqlConnectionStringBuilder(_chaineServeur) { InitialCatalog = "master" }.ConnectionString;
        using var connexion = new SqlConnection(chaineMaster);
        connexion.Open();
        using var commande = connexion.CreateCommand();
        commande.CommandText = $"""
            IF DB_ID(N'{_nomBase}') IS NOT NULL
            BEGIN
                ALTER DATABASE [{_nomBase}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{_nomBase}];
            END
            """;
        commande.ExecuteNonQuery();
    }
}
