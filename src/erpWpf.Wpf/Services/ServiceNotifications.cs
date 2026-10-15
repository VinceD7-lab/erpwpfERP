using CommunityToolkit.Mvvm.ComponentModel;

namespace erpWpf.Wpf.Services;

public sealed partial class ServiceNotifications : ObservableObject, IServiceNotifications, IDisposable
{
    public static readonly TimeSpan DureeAffichageSucces = TimeSpan.FromSeconds(5);

    private readonly TimeProvider _horloge;
    private ITimer? _minuterie;
    private int _version;

    public ServiceNotifications(TimeProvider horloge)
    {
        _horloge = horloge;
    }

    [ObservableProperty]
    public partial string? Message { get; private set; }

    [ObservableProperty]
    public partial bool EstErreur { get; private set; }

    [ObservableProperty]
    public partial bool EstVisible { get; private set; }

    public void Succes(string message)
    {
        var version = Afficher(message, estErreur: false);

        // Le rappel s'exécute sur le pool de threads : il ne masque que le message pour lequel il a été armé.
        _minuterie = _horloge.CreateTimer(
            _ =>
            {
                if (Volatile.Read(ref _version) == version)
                {
                    EstVisible = false;
                }
            },
            null,
            DureeAffichageSucces,
            Timeout.InfiniteTimeSpan);
    }

    public void Erreur(string message) => _ = Afficher(message, estErreur: true);

    public void Masquer()
    {
        ArreterMinuterie();
        Interlocked.Increment(ref _version);
        EstVisible = false;
    }

    public void Dispose() => ArreterMinuterie();

    private int Afficher(string message, bool estErreur)
    {
        ArreterMinuterie();
        var version = Interlocked.Increment(ref _version);
        Message = message;
        EstErreur = estErreur;
        EstVisible = true;
        return version;
    }

    private void ArreterMinuterie()
    {
        _minuterie?.Dispose();
        _minuterie = null;
    }
}
