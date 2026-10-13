using erpWeb.Core.Communs;
using erpWeb.Core.Utilisateurs;

namespace erpWpf.Wpf.Securite;

/// <summary>
/// Session unique du poste, enregistrée en singleton. Fournit aussi l'utilisateur courant
/// à l'audit (champs CreePar / ModifiePar) comme le faisait UtilisateurCourantHttp.
/// </summary>
public sealed class SessionUtilisateur : ISessionUtilisateur, IUtilisateurCourant
{
    private SessionDto? _session;

    public SessionDto? Session => Volatile.Read(ref _session);

    public bool EstOuverte => Session is not null;

    public string? Identifiant => Session?.IdUtilisateur;

    public string? NomUtilisateur => Session?.Email;

    public event EventHandler? SessionModifiee;

    public void Ouvrir(SessionDto session)
    {
        ArgumentNullException.ThrowIfNull(session);
        Volatile.Write(ref _session, session);
        SessionModifiee?.Invoke(this, EventArgs.Empty);
    }

    public void Actualiser(SessionDto session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (Session?.IdUtilisateur != session.IdUtilisateur)
        {
            return;
        }

        Volatile.Write(ref _session, session);
        SessionModifiee?.Invoke(this, EventArgs.Empty);
    }

    public void Fermer()
    {
        if (Interlocked.Exchange(ref _session, null) is not null)
        {
            SessionModifiee?.Invoke(this, EventArgs.Empty);
        }
    }
}
