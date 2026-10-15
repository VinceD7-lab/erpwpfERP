namespace erpWpf.Wpf.Securite;

/// <summary>
/// Contrôle des permissions de l'utilisateur connecté : remplace les politiques [Authorize]
/// et le Tag Helper asp-permission de la version web.
/// </summary>
public interface IAutorisation
{
    /// <summary>
    /// Vrai si une session est ouverte et, lorsque <paramref name="permission"/> est renseignée,
    /// si les rôles de l'utilisateur l'accordent.
    /// </summary>
    bool APermission(string? permission);
}
