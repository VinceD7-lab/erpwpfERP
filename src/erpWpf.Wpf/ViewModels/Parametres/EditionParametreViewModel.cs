using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using erpWeb.Core.Communs;
using erpWeb.Core.Parametres;
using erpWpf.Wpf.Navigation;
using erpWpf.Wpf.Services;
using erpWpf.Wpf.ViewModels.Communs;

namespace erpWpf.Wpf.ViewModels.Parametres;

/// <summary>
/// Création ou modification d'un paramètre (ex-Parametres/Modifier) : la clé n'est saisissable qu'à la création ;
/// la configuration est rechargée par le service après enregistrement.
/// </summary>
public sealed partial class EditionParametreViewModel : ViewModelBase, INavigable
{
    private readonly IExecuteurOperations _executeur;
    private readonly IServiceNavigation _navigation;
    private readonly IServiceNotifications _notifications;

    public EditionParametreViewModel(IExecuteurOperations executeur, IServiceNavigation navigation, IServiceNotifications notifications)
    {
        _executeur = executeur;
        _navigation = navigation;
        _notifications = notifications;
    }

    public override string Titre => EstNouveau ? "Nouveau paramètre" : "Modifier le paramètre";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Titre))]
    public partial bool EstNouveau { get; private set; } = true;

    [ObservableProperty]
    public partial string Cle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Valeur { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? Description { get; set; }

    public Task InitialiserAsync(string? parametre, CancellationToken jetonAnnulation) => ExecuterOccupeAsync(async jeton =>
    {
        if (string.IsNullOrEmpty(parametre))
        {
            return;
        }

        var existant = await _executeur.ExecuterAsync<IServiceParametres, ParametreDto?>(
            (service, jetonOperation) => service.ObtenirAsync(parametre, jetonOperation),
            jeton);
        if (existant is null)
        {
            _notifications.Erreur("Paramètre introuvable.");
            await _navigation.NaviguerAsync(Destinations.Parametres, jeton);
            return;
        }

        EstNouveau = false;
        Cle = existant.Cle;
        Valeur = existant.Valeur;
        Description = existant.Description;
    });

    [RelayCommand]
    private Task EnregistrerAsync() => ExecuterOccupeAsync(async jeton =>
    {
        EffacerErreurs();
        var modification = new ModificationParametreDto { Cle = Cle, Valeur = Valeur, Description = Description };
        var resultat = await _executeur.ExecuterAsync<IServiceParametres, ResultatOperation>(
            (service, jetonOperation) => service.DefinirAsync(modification, jetonOperation),
            jeton);
        if (!resultat.Reussi)
        {
            AfficherErreurs(resultat.Erreurs);
            return;
        }

        _notifications.Succes($"Le paramètre « {Cle} » a été enregistré.");
        await _navigation.NaviguerAsync(Destinations.Parametres, jeton);
    });

    [RelayCommand]
    private Task AnnulerAsync() => _navigation.NaviguerAsync(Destinations.Parametres, JetonEcran);
}
