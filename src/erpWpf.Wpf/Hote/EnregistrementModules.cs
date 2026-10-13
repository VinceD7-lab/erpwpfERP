using Microsoft.Extensions.DependencyInjection;

namespace erpWpf.Wpf.Hote;

/// <summary>
/// Pages, actions et entrées de menu de chaque module métier : un nouveau module ajoute sa section ici
/// (équivalent des contrôleurs et du menu de _Layout.cshtml de la version web).
/// </summary>
public static class EnregistrementModules
{
    public static IServiceCollection AddModules(this IServiceCollection services)
    {
        return services;
    }
}
