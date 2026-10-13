using erpWeb.Core;
using erpWeb.Core.Factures;
using erpWeb.Core.Utilisateurs;
using erpWeb.Infrastructure;
using erpWpf.Wpf.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace erpWpf.Wpf.Hote;

/// <summary>
/// Composition root de l'application (ex-Program.cs) : seul endroit, avec App.xaml.cs, qui référence Infrastructure.
/// Partagée avec les tests d'intégration pour qu'ils exercent exactement la même composition.
/// </summary>
public static class ConstructionHote
{
    public static HostApplicationBuilder CreerConstructeur(string[] arguments)
    {
        // Racine de contenu = dossier de l'exécutable : appsettings.json est trouvé même lancé depuis un raccourci.
        var constructeur = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            Args = arguments,
            ContentRootPath = AppContext.BaseDirectory,
        });

        // Paramètres en base : dernière source, ils surchargent les fichiers de configuration.
        constructeur.Configuration.AddParametresBaseDeDonnees();

        AjouterServices(constructeur.Services, constructeur.Configuration, constructeur.Environment, RepertoireDonneesLocales.ProfilUtilisateur());
        return constructeur;
    }

    public static IServiceCollection AjouterServices(
        IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environnement,
        RepertoireDonneesLocales repertoireDonnees)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddOptions<OptionsFacturation>().Bind(configuration.GetSection(OptionsFacturation.Section)).ValidateOnStart();

        services.AddCore();
        services.AddInfrastructure(configuration, repertoireDonnees.Chemin, environnementDeveloppement: environnement.IsDevelopment());

        // Mêmes règles que la version web ; pas de Data Protection : jetons TOTP (AddJetonsIdentite).
        services
            .AddIdentityCore<Utilisateur>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.SignIn.RequireConfirmedAccount = false;
            })
            .AddRoles<IdentityRole>()
            .AddStockageIdentite()
            .AddJetonsIdentite();

        services.AddPresentation(repertoireDonnees);
        return services;
    }
}
