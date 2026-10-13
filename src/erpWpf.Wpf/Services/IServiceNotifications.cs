using System.ComponentModel;

namespace erpWpf.Wpf.Services;

/// <summary>
/// Bandeau de messages de la fenêtre principale (équivalent des messages TempData du web) :
/// un message de succès disparaît après quelques secondes, un message d'erreur reste jusqu'à fermeture.
/// </summary>
public interface IServiceNotifications : INotifyPropertyChanged
{
    string? Message { get; }

    bool EstErreur { get; }

    bool EstVisible { get; }

    void Succes(string message);

    void Erreur(string message);

    void Masquer();
}
