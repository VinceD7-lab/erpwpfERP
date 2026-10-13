using erpWeb.Core.Communs;

namespace erpWeb.Core.Utilisateurs;

/// <summary>
/// Authentifie un utilisateur de l'application et contrôle la validité d'une session ouverte.
/// </summary>
public interface IServiceAuthentification
{
    /// <summary>
    /// Vérifie les identifiants (compte actif, verrouillage après échecs répétés) et retourne la session ouverte.
    /// </summary>
    Task<ResultatOperation<SessionDto>> ConnecterAsync(string email, string motDePasse, CancellationToken jetonAnnulation = default);

    /// <summary>
    /// Relit une session ouverte : retourne la session à jour (nom, permissions) ou null si le compte n'existe plus,
    /// est désactivé ou verrouillé, ou si son tampon de sécurité a changé (mot de passe réinitialisé, désactivation).
    /// </summary>
    Task<SessionDto?> ActualiserSessionAsync(string idUtilisateur, string tamponSecurite, CancellationToken jetonAnnulation = default);
}
