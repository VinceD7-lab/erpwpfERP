using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using erpWeb.Core.Autorisation;
using erpWeb.Core.Communs;
using erpWeb.Core.Utilisateurs;
using erpWpf.Wpf.Navigation;
using erpWpf.Wpf.Securite;
using erpWpf.Wpf.Services;
using erpWpf.Wpf.ViewModels.Communs;

namespace erpWpf.Wpf.ViewModels.Utilisateurs;

/// <summary>Comptes utilisateurs (ex-Utilisateurs/Index) : création, modification, désactivation et réactivation.</summary>
public sealed partial class ListeUtilisateursViewModel : ViewModelBase, INavigable
{
    private readonly IExecuteurOperations _executeur;
    private readonly IServiceNavigation _navigation;
    private readonly IServiceDialogues _dialogues;
    private readonly IServiceNotifications _notifications;

    public ListeUtilisateursViewModel(
        IExecuteurOperations executeur,
        IServiceNavigation navigation,
        IServiceDialogues dialogues,
        IServiceNotifications notifications,
        IAutorisation autorisation)
    {
        _executeur = executeur;
        _navigation = navigation;
        _dialogues = dialogues;
        _notifications = notifications;
        PeutGerer = autorisation.APermission(Permissions.Utilisateurs.Gerer);
    }

    public override string Titre => "Utilisateurs";

    public bool PeutGerer { get; }

    public ObservableCollection<UtilisateurDto> Utilisateurs { get; } = [];

    public Task InitialiserAsync(string? parametre, CancellationToken jetonAnnulation) => ChargerAsync();

    [RelayCommand]
    private Task ChargerAsync() => ExecuterOccupeAsync(async jeton =>
    {
        var utilisateurs = await _executeur.ExecuterAsync<IServiceUtilisateurs, IReadOnlyList<UtilisateurDto>>(
            (service, jetonOperation) => service.ListerAsync(jetonOperation),
            jeton);

        Utilisateurs.Clear();
        foreach (var utilisateur in utilisateurs)
        {
            Utilisateurs.Add(utilisateur);
        }
    });

    [RelayCommand(CanExecute = nameof(PeutGerer))]
    private Task CreerAsync() => _navigation.NaviguerAsync(Destinations.CreationUtilisateur, JetonEcran);

    [RelayCommand(CanExecute = nameof(PeutGerer))]
    private Task ModifierAsync(UtilisateurDto? utilisateur)
        => utilisateur is null ? Task.CompletedTask : _navigation.NaviguerAsync(Destinations.ModificationUtilisateur(utilisateur.Id), JetonEcran);

    [RelayCommand(CanExecute = nameof(PeutGerer))]
    private Task BasculerActivationAsync(UtilisateurDto? utilisateur) => ExecuterOccupeAsync(async jeton =>
    {
        if (utilisateur is null)
        {
            return;
        }

        var activer = !utilisateur.EstActif;
        var question = activer
            ? $"Réactiver le compte de {utilisateur.NomComplet} ?"
            : $"Désactiver le compte de {utilisateur.NomComplet} ? Il sera déconnecté dans la minute.";
        if (!_dialogues.Confirmer(question))
        {
            return;
        }

        var resultat = await _executeur.ExecuterAsync<IServiceUtilisateurs, ResultatOperation>(
            (service, jetonOperation) => service.DefinirActivationAsync(utilisateur.Id, activer, jetonOperation),
            jeton);
        if (!resultat.Reussi)
        {
            _notifications.Erreur(string.Join(" ", resultat.Erreurs));
            return;
        }

        _notifications.Succes(activer ? $"Le compte de {utilisateur.NomComplet} a été réactivé." : $"Le compte de {utilisateur.NomComplet} a été désactivé.");
        await ChargerAsync();
    });
}
