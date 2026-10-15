using erpWeb.Core.Factures;
using erpWpf.Wpf.Services;

namespace erpWeb.IntegrationTests;

/// <summary>Boîtes de dialogue simulées : réponse configurable, questions mémorisées.</summary>
public sealed class DialoguesTest : IServiceDialogues
{
    public bool Reponse { get; set; } = true;

    public List<string> Questions { get; } = [];

    public bool Confirmer(string message, string titre = "Confirmation")
    {
        Questions.Add(message);
        return Reponse;
    }

    public void Informer(string message, string titre = "Information") => Questions.Add(message);
}

/// <summary>Fichiers du poste simulés dans un dossier temporaire : le choix de l'utilisateur est préparé par le test.</summary>
public sealed class FichiersTest : IServiceFichiers
{
    public FichiersTest(string dossier)
    {
        Dossier = dossier;
        Directory.CreateDirectory(dossier);
    }

    public string Dossier { get; }

    /// <summary>Fichier « choisi » à la prochaine ouverture.</summary>
    public FichierLocal? FichierAOuvrir { get; set; }

    /// <summary>Dernier fichier enregistré par l'application.</summary>
    public string? DernierEnregistrement { get; private set; }

    public FichierLocal? ChoisirFichierAOuvrir(string filtre = "Tous les fichiers|*.*") => FichierAOuvrir;

    public string? ChoisirEmplacementEnregistrement(string nomPropose, string filtre = "Tous les fichiers|*.*")
        => DernierEnregistrement = Path.Combine(Dossier, nomPropose);

    public Stream OuvrirEnLecture(string chemin) => File.OpenRead(chemin);

    public Stream CreerEnEcriture(string chemin) => File.Create(chemin);

    public FichierLocal Creer(string nom, string contenu)
    {
        var chemin = Path.Combine(Dossier, nom);
        File.WriteAllText(chemin, contenu);
        return new FichierLocal(chemin, nom, new FileInfo(chemin).Length);
    }
}

/// <summary>Impression simulée : mémorise la facture envoyée à l'imprimante.</summary>
public sealed class ImpressionTest : IServiceImpression
{
    public ImpressionFactureDto? DerniereFacture { get; private set; }

    public bool ImprimerFacture(ImpressionFactureDto impression)
    {
        DerniereFacture = impression;
        return true;
    }
}
