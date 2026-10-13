using erpWeb.Infrastructure.Donnees;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace erpWpf.Wpf.Hote;

/// <summary>
/// Utilisée uniquement par les outils « dotnet ef » (-s src/erpWpf.Wpf) : une application WPF n'expose pas
/// d'hôte que les outils sauraient construire. Lit la même configuration que l'application (Development par défaut).
/// </summary>
public sealed class FabriqueContexteConception : IDesignTimeDbContextFactory<AppDbContext>
{
    private const string NomChaineConnexion = erpWeb.Infrastructure.DependencyInjection.NomChaineConnexion;

    public AppDbContext CreateDbContext(string[] args)
    {
        var environnement = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environnement}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var chaineConnexion = configuration.GetConnectionString(NomChaineConnexion)
            ?? throw new InvalidOperationException($"Chaîne de connexion « {NomChaineConnexion} » absente de la configuration.");

        return new ServiceCollection()
            .AddDbContext<AppDbContext>(options => options.UseSqlServer(chaineConnexion))
            .BuildServiceProvider()
            .GetRequiredService<AppDbContext>();
    }
}
