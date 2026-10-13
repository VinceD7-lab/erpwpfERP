using System.IO;

namespace erpWpf.Wpf.Services;

/// <summary>
/// Type MIME d'un fichier déposé, déduit de son extension (le navigateur le fournissait dans la version web).
/// </summary>
public static class TypesContenu
{
    public const string ParDefaut = "application/octet-stream";

    private static readonly Dictionary<string, string> _parExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = "application/pdf",
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".gif"] = "image/gif",
        [".txt"] = "text/plain",
        [".csv"] = "text/csv",
        [".xml"] = "application/xml",
        [".json"] = "application/json",
        [".zip"] = "application/zip",
        [".doc"] = "application/msword",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".xls"] = "application/vnd.ms-excel",
        [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
    };

    public static string Deduire(string nomFichier)
        => _parExtension.TryGetValue(Path.GetExtension(nomFichier), out var type) ? type : ParDefaut;
}
