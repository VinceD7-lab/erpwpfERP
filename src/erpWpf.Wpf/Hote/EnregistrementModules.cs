using erpWeb.Core.Autorisation;
using erpWeb.Core.Communs;
using erpWpf.Wpf.Navigation;
using erpWpf.Wpf.ViewModels.Clients;
using erpWpf.Wpf.ViewModels.Documents;
using erpWpf.Wpf.ViewModels.Echantillons;
using erpWpf.Wpf.ViewModels.Factures;
using erpWpf.Wpf.ViewModels.JournalAudit;
using erpWpf.Wpf.ViewModels.Parametres;
using erpWpf.Wpf.ViewModels.PlanningAnalyses;
using erpWpf.Wpf.ViewModels.TableauDeBord;
using erpWpf.Wpf.ViewModels.Utilisateurs;
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

        // Documents
        services.AjouterPage<ListeDocumentsViewModel>(Destinations.Documents, Permissions.Documents.Lire);
        services.AjouterPage<DepotDocumentViewModel>(Destinations.DepotDocument, Permissions.Documents.Deposer);
        services.AjouterAction<TelechargementDocument>(Destinations.RouteTelechargementDocument, Permissions.Documents.Lire);
        services.AjouterMenu(new EntreeMenu(20, "Documents", "dossier", Destinations.Documents, Permissions.Documents.Lire));

        // Clients
        services.AddTransient<EchantillonsClientViewModel>();
        services.AjouterPage<ListeClientsViewModel>(Destinations.Clients, Permissions.Clients.Lire);
        services.AjouterPage<EditionClientViewModel>(Destinations.CreationClient, Permissions.Clients.Gerer);
        services.AjouterPage<EditionClientViewModel>(Destinations.RouteModificationClient, Permissions.Clients.Gerer);
        services.AjouterMenu(new EntreeMenu(30, "Clients", "clients", Destinations.Clients, Permissions.Clients.Lire));

        // Échantillons
        services.AjouterPage<ResultatsEchantillonsViewModel>(Destinations.Echantillons, Permissions.Echantillons.Lire);
        services.AjouterMenu(new EntreeMenu(40, "Échantillons", "echantillons", Destinations.Echantillons, Permissions.Echantillons.Lire));

        // Factures
        services.AjouterPage<ListeFacturesViewModel>(Destinations.Factures, Permissions.Factures.Lire);
        services.AjouterPage<ImpressionFactureViewModel>(Destinations.RouteImpressionFacture, Permissions.Factures.Lire);
        services.AjouterMenu(new EntreeMenu(50, "Factures", "factures", Destinations.Factures, Permissions.Factures.Lire));

        // Planning des analyses
        services.AjouterPage<PlanningAnalysesViewModel>(Destinations.PlanningAnalyses, Permissions.PlanningAnalyses.Lire);
        services.AjouterMenu(new EntreeMenu(60, "Planning analyses", "planning", Destinations.PlanningAnalyses, Permissions.PlanningAnalyses.Lire));

        // Administration
        services.AjouterPage<ListeUtilisateursViewModel>(Destinations.Utilisateurs, Permissions.Utilisateurs.Lire);
        services.AjouterPage<EditionUtilisateurViewModel>(Destinations.CreationUtilisateur, Permissions.Utilisateurs.Gerer);
        services.AjouterPage<EditionUtilisateurViewModel>(Destinations.RouteModificationUtilisateur, Permissions.Utilisateurs.Gerer);
        services.AjouterMenu(new EntreeMenu(100, "Utilisateurs", "utilisateurs", Destinations.Utilisateurs, Permissions.Utilisateurs.Lire, SectionAdministration));

        services.AjouterPage<JournalAuditViewModel>(Destinations.JournalAudit, Permissions.JournalAudit.Lire);
        services.AjouterMenu(new EntreeMenu(110, "Journal d'audit", "journal", Destinations.JournalAudit, Permissions.JournalAudit.Lire, SectionAdministration));

        services.AjouterPage<ListeParametresViewModel>(Destinations.Parametres, Permissions.Parametres.Lire);
        services.AjouterPage<EditionParametreViewModel>(Destinations.RouteModificationParametre, Permissions.Parametres.Modifier);
        services.AjouterMenu(new EntreeMenu(120, "Paramètres", "parametres", Destinations.Parametres, Permissions.Parametres.Lire, SectionAdministration));

        return services;
    }
}
