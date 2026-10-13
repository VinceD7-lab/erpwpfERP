using System.IO;

namespace erpWpf.Wpf.Services;

/// <summary>Fichier choisi par l'utilisateur sur son poste.</summary>
public sealed record FichierLocal(string Chemin, string Nom, long Taille);

/// <summary>Choix et accès aux fichiers du poste (dépôt, téléchargement, export).</summary>
public interface IServiceFichiers
{
    FichierLocal? ChoisirFichierAOuvrir(string filtre = "Tous les fichiers|*.*");

    /// <summary>Retourne le chemin choisi, ou null si l'utilisateur annule.</summary>
    string? ChoisirEmplacementEnregistrement(string nomPropose, string filtre = "Tous les fichiers|*.*");

    Stream OuvrirEnLecture(string chemin);

    Stream CreerEnEcriture(string chemin);
}
