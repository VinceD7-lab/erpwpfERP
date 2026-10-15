namespace erpWpf.Wpf.Services;

/// <summary>Préférences d'affichage propres à l'utilisateur du poste (équivalent du localStorage du web).</summary>
public interface IPreferencesLocales
{
    bool MenuReplie { get; set; }
}
