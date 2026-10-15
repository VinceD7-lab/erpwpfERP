using System.IO;

namespace erpWpf.Wpf.Services;

/// <summary>
/// Répertoire de base des données propres au poste (%LOCALAPPDATA%\erpWpf) : préférences et,
/// lorsque Stockage:Dossier est relatif, documents déposés. Jamais le dossier d'installation (bin/).
/// </summary>
public sealed record RepertoireDonneesLocales(string Chemin)
{
    public static RepertoireDonneesLocales ProfilUtilisateur()
        => new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "erpWpf"));
}
