using erpWeb.UnitTests.Outils;
using erpWpf.Wpf.Services;
using Microsoft.Extensions.Time.Testing;

namespace erpWeb.UnitTests.Wpf;

public sealed class ServiceNotificationsTests
{
    [Fact]
    public void Succes_ApresLaDureeDAffichage_EstMasque()
    {
        var horloge = new FakeTimeProvider(BaseDonneesTest.DateReference);
        using var notifications = new ServiceNotifications(horloge);

        notifications.Succes("Client enregistré.");
        Assert.True(notifications.EstVisible);

        horloge.Advance(ServiceNotifications.DureeAffichageSucces);

        Assert.False(notifications.EstVisible);
    }

    [Fact]
    public void Erreur_ResteAfficheeJusquAFermeture()
    {
        var horloge = new FakeTimeProvider(BaseDonneesTest.DateReference);
        using var notifications = new ServiceNotifications(horloge);

        notifications.Erreur("Enregistrement impossible.");
        horloge.Advance(TimeSpan.FromMinutes(5));

        Assert.True(notifications.EstVisible);
        Assert.True(notifications.EstErreur);
        Assert.Equal("Enregistrement impossible.", notifications.Message);
    }

    [Fact]
    public void Erreur_ApresUnSucces_NEstPasMasqueeParLaMinuterieDuSucces()
    {
        var horloge = new FakeTimeProvider(BaseDonneesTest.DateReference);
        using var notifications = new ServiceNotifications(horloge);

        notifications.Succes("Premier message.");
        notifications.Erreur("Second message.");
        horloge.Advance(ServiceNotifications.DureeAffichageSucces);

        Assert.True(notifications.EstVisible);
    }
}
