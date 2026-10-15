using erpWeb.Core.Clients;
using erpWpf.Wpf.Services;

namespace erpWpf.Wpf.ViewModels.Communs;

/// <summary>Choix d'une liste déroulante de filtre ; une valeur nulle signifie « tous ».</summary>
public sealed record OptionFiltre<T>(T? Valeur, string Libelle);

/// <summary>Listes de choix partagées par les écrans de recherche.</summary>
public static class OptionsFiltre
{
    public static async Task<IReadOnlyList<OptionFiltre<int?>>> ClientsAsync(IExecuteurOperations executeur, CancellationToken jetonAnnulation)
    {
        var clients = await executeur.ExecuterAsync<IServiceClients, IReadOnlyList<ClientDto>>(
            (service, jeton) => service.ListerAsync(jeton),
            jetonAnnulation);

        return [new OptionFiltre<int?>(null, "Tous les clients"), .. clients.Select(client => new OptionFiltre<int?>(client.Id, client.RaisonSociale))];
    }

    /// <summary>Statuts techniques (Emise, Recu…) avec leur libellé d'affichage.</summary>
    public static IReadOnlyList<OptionFiltre<string>> Statuts(IReadOnlyList<string> statuts, Func<string, string> libelle, string libelleTous = "Tous les statuts")
        => [new OptionFiltre<string>(null, libelleTous), .. statuts.Select(statut => new OptionFiltre<string>(statut, libelle(statut)))];
}
