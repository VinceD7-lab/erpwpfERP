namespace erpWeb.Core.Utilisateurs;

/// <summary>Utilisateur authentifié et permissions accordées par ses rôles.</summary>
public sealed record SessionDto(
    string IdUtilisateur,
    string Email,
    string NomComplet,
    string TamponSecurite,
    IReadOnlySet<string> Permissions);
