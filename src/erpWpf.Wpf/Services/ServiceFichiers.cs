using System.IO;
using Microsoft.Win32;

namespace erpWpf.Wpf.Services;

public sealed class ServiceFichiers : IServiceFichiers
{
    public FichierLocal? ChoisirFichierAOuvrir(string filtre = "Tous les fichiers|*.*")
    {
        var dialogue = new OpenFileDialog { Filter = filtre, CheckFileExists = true, Multiselect = false };
        if (dialogue.ShowDialog() != true)
        {
            return null;
        }

        var fichier = new FileInfo(dialogue.FileName);
        return new FichierLocal(fichier.FullName, fichier.Name, fichier.Length);
    }

    public string? ChoisirEmplacementEnregistrement(string nomPropose, string filtre = "Tous les fichiers|*.*")
    {
        var dialogue = new SaveFileDialog { FileName = nomPropose, Filter = filtre, OverwritePrompt = true, AddExtension = true };
        return dialogue.ShowDialog() == true ? dialogue.FileName : null;
    }

    public Stream OuvrirEnLecture(string chemin) => File.OpenRead(chemin);

    public Stream CreerEnEcriture(string chemin) => File.Create(chemin);
}
