using erpWeb.Core.Communs;
using erpWpf.Wpf.Fenetres;
using erpWpf.Wpf.Navigation;
using erpWpf.Wpf.Securite;
using erpWpf.Wpf.Services;
using erpWpf.Wpf.ViewModels;
using erpWpf.Wpf.ViewModels.Compte;
using Microsoft.Extensions.DependencyInjection;

namespace erpWpf.Wpf.Hote;

/// <summary>
/// Enregistrements de la couche présentation. Les ViewModels sont transients et ne reçoivent que des singletons :
/// les services de Core (scoped) sont atteints par <see cref="IExecuteurOperations"/>, une portée par opération.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services, RepertoireDonneesLocales repertoireDonnees)
    {
        services.AddSingleton(repertoireDonnees);

        services.AddSingleton<SessionUtilisateur>();
        services.AddSingleton<ISessionUtilisateur>(fournisseur => fournisseur.GetRequiredService<SessionUtilisateur>());
        services.AddSingleton<IUtilisateurCourant>(fournisseur => fournisseur.GetRequiredService<SessionUtilisateur>());
        services.AddSingleton<IAutorisation, ServiceAutorisation>();
        services.AddSingleton<SurveillanceSession>();

        services.AddSingleton<IExecuteurOperations, ExecuteurOperations>();
        services.AddSingleton<IFabriqueViewModels, FabriqueViewModels>();
        services.AddSingleton<IServiceNavigation, ServiceNavigation>();
        services.AddSingleton<IServiceNotifications, ServiceNotifications>();
        services.AddSingleton<IServiceDialogues, ServiceDialogues>();
        services.AddSingleton<IServiceFichiers, ServiceFichiers>();
        services.AddSingleton<IServiceImpression, ServiceImpression>();
        services.AddSingleton<IPreferencesLocales, PreferencesLocales>();

        services.AddSingleton<GestionnaireFenetres>();
        services.AddTransient<FenetreAccueil>();
        services.AddTransient<FenetrePrincipale>();
        services.AddTransient<AccueilViewModel>();
        services.AddTransient<ConnexionViewModel>();
        services.AddTransient<InscriptionViewModel>();
        services.AddTransient<FenetrePrincipaleViewModel>();
        services.AddTransient<AccesRefuseViewModel>();

        return services.AddModules();
    }

    /// <summary>Déclare une page navigable et sa permission (ex-attribut [Authorize(Policy = …)] du contrôleur).</summary>
    public static IServiceCollection AjouterPage<TViewModel>(this IServiceCollection services, string route, string? permission)
        where TViewModel : class
    {
        services.AddTransient<TViewModel>();
        return services.AddSingleton(new DefinitionPage(route, typeof(TViewModel), permission));
    }

    /// <summary>Déclare une action de navigation sans changement d'écran.</summary>
    public static IServiceCollection AjouterAction<TAction>(this IServiceCollection services, string route, string? permission)
        where TAction : class, IActionNavigation
    {
        services.AddTransient<TAction>();
        return services.AddSingleton(new DefinitionAction(route, typeof(TAction), permission));
    }

    /// <summary>Déclare une entrée du menu latéral (ex-lien de _Layout.cshtml avec asp-permission).</summary>
    public static IServiceCollection AjouterMenu(this IServiceCollection services, EntreeMenu entree)
        => services.AddSingleton(entree);
}
