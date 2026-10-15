using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using erpWeb.Core.Autorisation;
using erpWeb.Core.Communs;
using erpWeb.Core.Utilisateurs;
using erpWpf.Wpf.Navigation;
using erpWpf.Wpf.Services;
using erpWpf.Wpf.ViewModels.Communs;

namespace erpWpf.Wpf.ViewModels.Utilisateurs;

/// <summary>Rôle proposé dans le formulaire (ex-partielle _SelectionRoles).</summary>
public sealed partial class SelectionRoleViewModel : ObservableObject
{
    public SelectionRoleViewModel(string nom, bool estSelectionne)
    {
        Nom = nom;
        EstSelectionne = estSelectionne;
    }

    public string Nom { get; }

    [ObservableProperty]
    public partial bool EstSelectionne { get; set; }
}

/// <summary>
/// Création (ex-Utilisateurs/Creer) ou modification d'un compte (ex-Utilisateurs/Modifier/{id}),
/// avec réinitialisation du mot de passe en modification.
/// </summary>
public sealed partial class EditionUtilisateurViewModel : ViewModelBase, INavigable
{
    private readonly IExecuteurOperations _executeur;
    private readonly IServiceNavigation _navigation;
    private readonly IServiceNotifications _notifications;
    private readonly IServiceDialogues _dialogues;

    public EditionUtilisateurViewModel(IExecuteurOperations executeur, IServiceNavigation navigation, IServiceNotifications notifications, IServiceDialogues dialogues)
    {
        _executeur = executeur;
        _navigation = navigation;
        _notifications = notifications;
        _dialogues = dialogues;
    }

    public override string Titre => EstCreation ? "Nouvel utilisateur" : "Modifier l'utilisateur";

    public ObservableCollection<SelectionRoleViewModel> Roles { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Titre), nameof(EstCreation))]
    public partial string? IdUtilisateur { get; private set; }

    public bool EstCreation => IdUtilisateur is null;

    [ObservableProperty]
    public partial string NomComplet { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Email { get; set; } = string.Empty;

    /// <summary>Mot de passe initial (création uniquement).</summary>
    [ObservableProperty]
    public partial string MotDePasse { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ReinitialiserMotDePasseCommand))]
    public partial string NouveauMotDePasse { get; set; } = string.Empty;

    public Task InitialiserAsync(string? parametre, CancellationToken jetonAnnulation) => ExecuterOccupeAsync(async jeton =>
    {
        var rolesDisponibles = await _executeur.ExecuterAsync<IServiceUtilisateurs, IReadOnlyList<string>>(
            (service, jetonOperation) => service.ListerRolesAsync(jetonOperation),
            jeton);

        IReadOnlyList<string> rolesSelectionnes = [RolesApplication.Utilisateur];
        if (!string.IsNullOrEmpty(parametre))
        {
            var utilisateur = await _executeur.ExecuterAsync<IServiceUtilisateurs, UtilisateurDto?>(
                (service, jetonOperation) => service.ObtenirAsync(parametre, jetonOperation),
                jeton);
            if (utilisateur is null)
            {
                _notifications.Erreur("Utilisateur introuvable.");
                await _navigation.NaviguerAsync(Destinations.Utilisateurs, jeton);
                return;
            }

            IdUtilisateur = utilisateur.Id;
            NomComplet = utilisateur.NomComplet;
            Email = utilisateur.Email;
            rolesSelectionnes = utilisateur.Roles;
        }

        Roles.Clear();
        foreach (var role in rolesDisponibles)
        {
            Roles.Add(new SelectionRoleViewModel(role, rolesSelectionnes.Contains(role)));
        }
    });

    [RelayCommand]
    private Task EnregistrerAsync() => ExecuterOccupeAsync(async jeton =>
    {
        EffacerErreurs();
        var roles = Roles.Where(role => role.EstSelectionne).Select(role => role.Nom).ToList();
        ResultatOperation resultat;
        if (IdUtilisateur is { } id)
        {
            var modification = new ModificationUtilisateurDto { Id = id, NomComplet = NomComplet, Email = Email, Roles = roles };
            resultat = await _executeur.ExecuterAsync<IServiceUtilisateurs, ResultatOperation>(
                (service, jetonOperation) => service.ModifierAsync(modification, jetonOperation),
                jeton);
        }
        else
        {
            var creation = new CreationUtilisateurDto { NomComplet = NomComplet, Email = Email, MotDePasse = MotDePasse, Roles = roles };
            var resultatCreation = await _executeur.ExecuterAsync<IServiceUtilisateurs, ResultatOperation<string>>(
                (service, jetonOperation) => service.CreerAsync(creation, jetonOperation),
                jeton);
            resultat = new ResultatOperation(resultatCreation.Reussi, resultatCreation.Erreurs);
            MotDePasse = string.Empty;
        }

        if (!resultat.Reussi)
        {
            AfficherErreurs(resultat.Erreurs);
            return;
        }

        _notifications.Succes(EstCreation ? $"Le compte de {NomComplet} a été créé." : $"Le compte de {NomComplet} a été modifié.");
        await _navigation.NaviguerAsync(Destinations.Utilisateurs, jeton);
    });

    [RelayCommand(CanExecute = nameof(PeutReinitialiser))]
    private Task ReinitialiserMotDePasseAsync() => ExecuterOccupeAsync(async jeton =>
    {
        if (IdUtilisateur is not { } id || !_dialogues.Confirmer($"Réinitialiser le mot de passe de {NomComplet} ?"))
        {
            return;
        }

        EffacerErreurs();
        var reinitialisation = new ReinitialisationMotDePasseDto { Id = id, NouveauMotDePasse = NouveauMotDePasse };
        var resultat = await _executeur.ExecuterAsync<IServiceUtilisateurs, ResultatOperation>(
            (service, jetonOperation) => service.ReinitialiserMotDePasseAsync(reinitialisation, jetonOperation),
            jeton);
        NouveauMotDePasse = string.Empty;

        if (resultat.Reussi)
        {
            _notifications.Succes($"Le mot de passe de {NomComplet} a été réinitialisé.");
        }
        else
        {
            AfficherErreurs(resultat.Erreurs);
        }
    });

    [RelayCommand]
    private Task AnnulerAsync() => _navigation.NaviguerAsync(Destinations.Utilisateurs, JetonEcran);

    private bool PeutReinitialiser() => !EstCreation && !string.IsNullOrEmpty(NouveauMotDePasse);
}
