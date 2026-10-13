using System.Globalization;
using erpWeb.Core.Documents;
using erpWpf.Wpf.Navigation;
using erpWpf.Wpf.Services;

namespace erpWpf.Wpf.ViewModels.Documents;

/// <summary>
/// Action « Documents/Telecharger/{id} » (ex-Documents/Telecharger) : enregistre une copie du document
/// à l'emplacement choisi. Utilisée par la liste des documents et le widget « Derniers documents ».
/// </summary>
public sealed class TelechargementDocument : IActionNavigation
{
    private readonly IExecuteurOperations _executeur;
    private readonly IServiceFichiers _fichiers;
    private readonly IServiceNotifications _notifications;

    public TelechargementDocument(IExecuteurOperations executeur, IServiceFichiers fichiers, IServiceNotifications notifications)
    {
        _executeur = executeur;
        _fichiers = fichiers;
        _notifications = notifications;
    }

    public async Task ExecuterAsync(string? parametre, CancellationToken jetonAnnulation)
    {
        if (!int.TryParse(parametre, NumberStyles.None, CultureInfo.InvariantCulture, out var id))
        {
            _notifications.Erreur("Document introuvable.");
            return;
        }

        var nomEnregistre = await _executeur.ExecuterAsync<IServiceDocuments, string?>(
            async (service, jeton) =>
            {
                var fichier = await service.OuvrirAsync(id, jeton);
                if (fichier is null)
                {
                    return null;
                }

                await using var contenu = fichier.Contenu;
                var chemin = _fichiers.ChoisirEmplacementEnregistrement(fichier.Document.NomFichier);
                if (chemin is null)
                {
                    return string.Empty;
                }

                await using var destination = _fichiers.CreerEnEcriture(chemin);
                await contenu.CopyToAsync(destination, jeton);
                return fichier.Document.NomFichier;
            },
            jetonAnnulation);

        if (nomEnregistre is null)
        {
            _notifications.Erreur("Document introuvable.");
        }
        else if (nomEnregistre.Length > 0)
        {
            _notifications.Succes($"Le document « {nomEnregistre} » a été enregistré.");
        }
    }
}
