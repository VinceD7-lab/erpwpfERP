using erpWeb.Core.Autorisation;
using erpWeb.Core.Utilisateurs;
using erpWeb.UnitTests.Outils;
using erpWpf.Wpf.Securite;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace erpWeb.UnitTests.Wpf;

public sealed class SecuriteTests
{
    private static readonly SessionDto _session = new(
        "id-1",
        "camille@erpweb.local",
        "Camille Martin",
        "tampon-1",
        new HashSet<string> { Permissions.Clients.Lire });

    [Fact]
    public void APermission_SansSession_EstRefuse()
    {
        var autorisation = new ServiceAutorisation(new SessionUtilisateur());

        Assert.False(autorisation.APermission(null));
        Assert.False(autorisation.APermission(Permissions.Clients.Lire));
    }

    [Fact]
    public void APermission_SessionOuverte_AccordeSeulementLesPermissionsDesRoles()
    {
        var session = new SessionUtilisateur();
        session.Ouvrir(_session);
        var autorisation = new ServiceAutorisation(session);

        Assert.True(autorisation.APermission(null));
        Assert.True(autorisation.APermission(Permissions.Clients.Lire));
        Assert.False(autorisation.APermission(Permissions.Clients.Gerer));
    }

    [Fact]
    public void SessionUtilisateur_Ouverte_FournitLUtilisateurCourantALAudit()
    {
        var session = new SessionUtilisateur();

        session.Ouvrir(_session);

        Assert.Equal("id-1", session.Identifiant);
        Assert.Equal("camille@erpweb.local", session.NomUtilisateur);
    }

    [Fact]
    public void Actualiser_AutreUtilisateur_EstIgnore()
    {
        var session = new SessionUtilisateur();
        session.Ouvrir(_session);

        session.Actualiser(_session with { IdUtilisateur = "id-2", NomComplet = "Autre" });

        Assert.Equal("Camille Martin", session.Session!.NomComplet);
    }

    [Fact]
    public void Fermer_SessionOuverte_NotifieUneSeuleFois()
    {
        var session = new SessionUtilisateur();
        session.Ouvrir(_session);
        var notifications = 0;
        session.SessionModifiee += (_, _) => notifications++;

        session.Fermer();
        session.Fermer();

        Assert.False(session.EstOuverte);
        Assert.Equal(1, notifications);
    }

    [Fact]
    public async Task VerifierAsync_SessionInvalidee_FermeLaSessionEtSignaleLExpiration()
    {
        var session = new SessionUtilisateur();
        session.Ouvrir(_session);
        var authentification = new Mock<IServiceAuthentification>();
        authentification.Setup(service => service.ActualiserSessionAsync("id-1", "tampon-1", It.IsAny<CancellationToken>())).ReturnsAsync((SessionDto?)null);
        using var surveillance = CreerSurveillance(session, authentification.Object);
        var expiree = false;
        surveillance.SessionExpiree += (_, _) => expiree = true;

        await surveillance.VerifierAsync();

        Assert.False(session.EstOuverte);
        Assert.True(expiree);
    }

    [Fact]
    public async Task VerifierAsync_SessionValide_ActualiseLesPermissions()
    {
        var session = new SessionUtilisateur();
        session.Ouvrir(_session);
        var actualisee = _session with { Permissions = new HashSet<string> { Permissions.Clients.Gerer } };
        var authentification = new Mock<IServiceAuthentification>();
        authentification.Setup(service => service.ActualiserSessionAsync("id-1", "tampon-1", It.IsAny<CancellationToken>())).ReturnsAsync(actualisee);
        using var surveillance = CreerSurveillance(session, authentification.Object);

        await surveillance.VerifierAsync();

        Assert.True(new ServiceAutorisation(session).APermission(Permissions.Clients.Gerer));
    }

    [Fact]
    public async Task VerifierAsync_BaseInjoignable_ConserveLaSession()
    {
        var session = new SessionUtilisateur();
        session.Ouvrir(_session);
        var authentification = new Mock<IServiceAuthentification>();
        authentification.Setup(service => service.ActualiserSessionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Serveur SQL injoignable"));
        using var surveillance = CreerSurveillance(session, authentification.Object);

        await surveillance.VerifierAsync();

        Assert.True(session.EstOuverte);
    }

    private static SurveillanceSession CreerSurveillance(SessionUtilisateur session, IServiceAuthentification authentification)
        => new(
            session,
            new ExecuteurOperationsTest().Ajouter(authentification),
            new FakeTimeProvider(BaseDonneesTest.DateReference),
            NullLogger<SurveillanceSession>.Instance);
}
