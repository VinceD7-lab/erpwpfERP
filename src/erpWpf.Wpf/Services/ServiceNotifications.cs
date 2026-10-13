using CommunityToolkit.Mvvm.ComponentModel;

namespace erpWpf.Wpf.Services;

public sealed partial class ServiceNotifications : ObservableObject, IServiceNotifications, IDisposable
{
    public static readonly TimeSpan DureeAffichageSucces = TimeSpan.FromSeconds(5);

    private readonly TimeProvider _horloge;
    private ITimer? _minuterie;

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
        Afficher(message, estErreur: false);
        _minuterie = _horloge.CreateTimer(_ => Masquer(), null, DureeAffichageSucces, Timeout.InfiniteTimeSpan);
    }

    public void Erreur(string message) => Afficher(message, estErreur: true);

    public void Masquer()
    {
        ArreterMinuterie();
        EstVisible = false;
    }

    public void Dispose() => ArreterMinuterie();

    private void Afficher(string message, bool estErreur)
    {
        ArreterMinuterie();
        Message = message;
        EstErreur = estErreur;
        EstVisible = true;
    }

    private void ArreterMinuterie()
    {
        _minuterie?.Dispose();
        _minuterie = null;
    }
}
