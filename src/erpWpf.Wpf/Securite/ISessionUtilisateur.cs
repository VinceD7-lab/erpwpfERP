using erpWeb.Core.Utilisateurs;

namespace erpWpf.Wpf.Securite;

/// <summary>Session de l'utilisateur connecté au poste : remplace le cookie d'authentification du web.</summary>
public interface ISessionUtilisateur
{
    SessionDto? Session { get; }

    bool EstOuverte { get; }

    /// <summary>Déclenché à l'ouverture, à l'actualisation et à la fermeture de la session.</summary>
    event EventHandler? SessionModifiee;

    void Ouvrir(SessionDto session);

    void Actualiser(SessionDto session);

    void Fermer();
}
