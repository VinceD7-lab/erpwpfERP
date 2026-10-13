using erpWeb.Core.Autorisation;
using erpWeb.Core.Communs;
using erpWpf.Wpf.Navigation;
using erpWpf.Wpf.ViewModels.Clients;
using erpWpf.Wpf.ViewModels.TableauDeBord;
using Microsoft.Extensions.DependencyInjection;

namespace erpWpf.Wpf.Hote;

/// <summary>
/// Pages, actions et entrées de menu de chaque module métier : un nouveau module ajoute sa section ici
/// (équivalent des contrôleurs et du menu de _Layout.cshtml de la version web).
/// </summary>
public static class EnregistrementModules
{
    public const string SectionAdministration = "ADMINISTRATION";

    public static IServiceCollection AddModules(this IServiceCollection services)
    {
        // Tableau de bord : accessible à tout utilisateur connecté, chaque widget porte sa propre permission.
        services.AjouterPage<TableauDeBordViewModel>(Destinations.TableauDeBord, permission: null);
        services.AjouterMenu(new EntreeMenu(10, "Tableau de bord", "tableau-de-bord", Destinations.TableauDeBord, Permission: null));

        // Clients
        services.AddTransient<EchantillonsClientViewModel>();
        services.AjouterPage<ListeClientsViewModel>(Destinations.Clients, Permissions.Clients.Lire);
        services.AjouterPage<EditionClientViewModel>(Destinations.CreationClient, Permissions.Clients.Gerer);
        services.AjouterPage<EditionClientViewModel>(Destinations.RouteModificationClient, Permissions.Clients.Gerer);
        services.AjouterMenu(new EntreeMenu(30, "Clients", "clients", Destinations.Clients, Permissions.Clients.Lire));

        return services;
    }
}
