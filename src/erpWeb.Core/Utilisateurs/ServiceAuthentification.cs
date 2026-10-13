using erpWeb.Core.Autorisation;
using erpWeb.Core.Communs;
using Microsoft.AspNetCore.Identity;

namespace erpWeb.Core.Utilisateurs;

public sealed class ServiceAuthentification : IServiceAuthentification
{
    public const string MessageIdentifiantsIncorrects = "Email ou mot de passe incorrect.";
    public const string MessageCompteVerrouille = "Compte temporairement verrouillé après plusieurs échecs. Réessayer plus tard.";
    public const string MessageCompteDesactive = "Ce compte est désactivé.";

    private readonly UserManager<Utilisateur> _gestionnaireUtilisateurs;
    private readonly RoleManager<IdentityRole> _gestionnaireRoles;

    public ServiceAuthentification(UserManager<Utilisateur> gestionnaireUtilisateurs, RoleManager<IdentityRole> gestionnaireRoles)
    {
        _gestionnaireUtilisateurs = gestionnaireUtilisateurs;
        _gestionnaireRoles = gestionnaireRoles;
    }

    public async Task<ResultatOperation<SessionDto>> ConnecterAsync(string email, string motDePasse, CancellationToken jetonAnnulation = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(motDePasse))
        {
            return ResultatOperation<SessionDto>.Echec(MessageIdentifiantsIncorrects);
        }

        jetonAnnulation.ThrowIfCancellationRequested();
        var utilisateur = await _gestionnaireUtilisateurs.FindByEmailAsync(email.Trim());
        if (utilisateur is null)
        {
            return ResultatOperation<SessionDto>.Echec(MessageIdentifiantsIncorrects);
        }

        if (await _gestionnaireUtilisateurs.IsLockedOutAsync(utilisateur))
        {
            return ResultatOperation<SessionDto>.Echec(MessageCompteVerrouille);
        }

        if (!await _gestionnaireUtilisateurs.CheckPasswordAsync(utilisateur, motDePasse))
        {
            await _gestionnaireUtilisateurs.AccessFailedAsync(utilisateur);
            return await _gestionnaireUtilisateurs.IsLockedOutAsync(utilisateur)
                ? ResultatOperation<SessionDto>.Echec(MessageCompteVerrouille)
                : ResultatOperation<SessionDto>.Echec(MessageIdentifiantsIncorrects);
        }

        // Comme SignInManager : le compte désactivé n'est signalé qu'après un mot de passe correct.
        if (!utilisateur.EstActif)
        {
            return ResultatOperation<SessionDto>.Echec(MessageCompteDesactive);
        }

        await _gestionnaireUtilisateurs.ResetAccessFailedCountAsync(utilisateur);
        return ResultatOperation<SessionDto>.Succes(await CreerSessionAsync(utilisateur));
    }

    public async Task<SessionDto?> ActualiserSessionAsync(string idUtilisateur, string tamponSecurite, CancellationToken jetonAnnulation = default)
    {
        jetonAnnulation.ThrowIfCancellationRequested();
        var utilisateur = await _gestionnaireUtilisateurs.FindByIdAsync(idUtilisateur);
        if (utilisateur is null
            || !utilisateur.EstActif
            || await _gestionnaireUtilisateurs.IsLockedOutAsync(utilisateur)
            || !string.Equals(await _gestionnaireUtilisateurs.GetSecurityStampAsync(utilisateur), tamponSecurite, StringComparison.Ordinal))
        {
            return null;
        }

        return await CreerSessionAsync(utilisateur);
    }

    private async Task<SessionDto> CreerSessionAsync(Utilisateur utilisateur)
    {
        var permissions = new HashSet<string>(StringComparer.Ordinal);
        foreach (var nomRole in await _gestionnaireUtilisateurs.GetRolesAsync(utilisateur))
        {
            var role = await _gestionnaireRoles.FindByNameAsync(nomRole);
            if (role is null)
            {
                continue;
            }

            var revendications = await _gestionnaireRoles.GetClaimsAsync(role);
            permissions.UnionWith(revendications
                .Where(revendication => revendication.Type == Permissions.TypeClaim)
                .Select(revendication => revendication.Value));
        }

        return new SessionDto(
            utilisateur.Id,
            utilisateur.Email ?? string.Empty,
            utilisateur.NomComplet,
            await _gestionnaireUtilisateurs.GetSecurityStampAsync(utilisateur),
            permissions);
    }
}
