using erpWeb.Core;
using erpWeb.Core.Autorisation;
using erpWeb.Core.Communs;
using erpWeb.Core.Documents;
using erpWeb.Core.Email;
using erpWeb.Core.Parametres;
using erpWeb.Core.Utilisateurs;
using erpWeb.Infrastructure;
using erpWeb.Infrastructure.Donnees;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace erpWeb.UnitTests.Outils;

/// <summary>
/// Identity réel (UserManager/RoleManager) sur SQLite en mémoire, configuré comme l'application :
/// les services qui en dépendent sont testés sans mock de UserManager.
/// </summary>
public sealed class HoteIdentiteTest : IAsyncDisposable
{
    public const int NombreEchecsAvantVerrouillage = 5;

    private readonly SqliteConnection _connexion;
    private readonly ServiceProvider _fournisseur;
    private readonly AsyncServiceScope _portee;

    private HoteIdentiteTest(SqliteConnection connexion, ServiceProvider fournisseur)
    {
        _connexion = connexion;
        _fournisseur = fournisseur;
        _portee = fournisseur.CreateAsyncScope();
    }

    public IServiceProvider Services => _portee.ServiceProvider;

    public UserManager<Utilisateur> GestionnaireUtilisateurs => Services.GetRequiredService<UserManager<Utilisateur>>();

    public RoleManager<IdentityRole> GestionnaireRoles => Services.GetRequiredService<RoleManager<IdentityRole>>();

    public static async Task<HoteIdentiteTest> CreerAsync(IServiceEmail serviceEmail)
    {
        var connexion = new SqliteConnection("DataSource=:memory:");
        await connexion.OpenAsync();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<TimeProvider>(new FakeTimeProvider(BaseDonneesTest.DateReference));
        services.AddSingleton(Mock.Of<IUtilisateurCourant>());
        services.AddSingleton(Mock.Of<IRechargementConfiguration>());
        services.AddSingleton(Mock.Of<IStockageFichiers>());
        services.AddSingleton(serviceEmail);
        services.AddScoped<IntercepteurAudit>();
        services.AddDbContext<AppDbContext>((fournisseur, options) => options
            .UseSqlite(connexion)
            .AddInterceptors(fournisseur.GetRequiredService<IntercepteurAudit>()));
        services.AddScoped<IAppDbContext>(fournisseur => fournisseur.GetRequiredService<AppDbContext>());
        services.AddIdentityCore<Utilisateur>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Lockout.MaxFailedAccessAttempts = NombreEchecsAvantVerrouillage;
            })
            .AddRoles<IdentityRole>()
            .AddStockageIdentite()
            .AddJetonsIdentite();
        services.AddCore();

        var hote = new HoteIdentiteTest(connexion, services.BuildServiceProvider());
        await hote.Services.GetRequiredService<AppDbContext>().Database.EnsureCreatedAsync();
        await hote.GestionnaireRoles.CreateAsync(new IdentityRole(RolesApplication.Administrateur));
        await hote.GestionnaireRoles.CreateAsync(new IdentityRole(RolesApplication.Utilisateur));
        return hote;
    }

    public async ValueTask DisposeAsync()
    {
        await _portee.DisposeAsync();
        await _fournisseur.DisposeAsync();
        await _connexion.DisposeAsync();
    }
}
