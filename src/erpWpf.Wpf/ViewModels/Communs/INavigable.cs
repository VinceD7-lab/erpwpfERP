namespace erpWpf.Wpf.ViewModels.Communs;

/// <summary>Écran chargé après la navigation, avec le paramètre éventuel de la destination (« Clients/Modifier/12 » → « 12 »).</summary>
public interface INavigable
{
    Task InitialiserAsync(string? parametre, CancellationToken jetonAnnulation);
}

/// <summary>Écran qui peut refuser d'être quitté (modifications non enregistrées).</summary>
public interface IConfirmationDepart
{
    bool PeutQuitter();
}
