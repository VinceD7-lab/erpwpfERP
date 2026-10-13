using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace erpWpf.Wpf.Services;

/// <summary>Préférences enregistrées en JSON dans le profil local de l'utilisateur Windows.</summary>
public sealed class PreferencesLocales : IPreferencesLocales
{
    private readonly string _chemin;
    private readonly ILogger<PreferencesLocales> _journal;
    private ContenuPreferences _contenu;

    public PreferencesLocales(RepertoireDonneesLocales repertoire, ILogger<PreferencesLocales> journal)
    {
        _chemin = Path.Combine(repertoire.Chemin, "preferences.json");
        _journal = journal;
        _contenu = Lire();
    }

    public bool MenuReplie
    {
        get => _contenu.MenuReplie;
        set
        {
            _contenu = _contenu with { MenuReplie = value };
            Ecrire();
        }
    }

    private ContenuPreferences Lire()
    {
        try
        {
            return File.Exists(_chemin)
                ? JsonSerializer.Deserialize<ContenuPreferences>(File.ReadAllText(_chemin)) ?? new ContenuPreferences()
                : new ContenuPreferences();
        }
        catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
        {
            _journal.LogWarning(exception, "Préférences locales illisibles : valeurs par défaut utilisées.");
            return new ContenuPreferences();
        }
    }

    private void Ecrire()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_chemin)!);
            File.WriteAllText(_chemin, JsonSerializer.Serialize(_contenu));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _journal.LogWarning(exception, "Préférences locales non enregistrées.");
        }
    }

    private sealed record ContenuPreferences(bool MenuReplie = false);
}
