namespace erpWpf.Wpf.Securite;

public sealed class ServiceAutorisation : IAutorisation
{
    private readonly ISessionUtilisateur _session;

    public ServiceAutorisation(ISessionUtilisateur session)
    {
        _session = session;
    }

    public bool APermission(string? permission)
    {
        var session = _session.Session;
        if (session is null)
        {
            return false;
        }

        return string.IsNullOrEmpty(permission) || session.Permissions.Contains(permission);
    }
}
