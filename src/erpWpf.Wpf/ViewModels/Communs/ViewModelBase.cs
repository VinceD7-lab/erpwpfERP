using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace erpWpf.Wpf.ViewModels.Communs;

/// <summary>
/// Base des écrans : état d'occupation, liste d'erreurs métier (équivalent du résumé de validation du web)
/// et jeton annulé à la fermeture de l'écran, pour abandonner les opérations en cours.
/// </summary>
public abstract partial class ViewModelBase : ObservableObject, IDisposable
{
    private readonly CancellationTokenSource _annulation = new();
    private bool _estFerme;

    protected ViewModelBase()
    {
        Erreurs.CollectionChanged += (_, _) => OnPropertyChanged(nameof(AErreurs));
    }

    /// <summary>Titre affiché dans l'en-tête de l'écran.</summary>
    public virtual string Titre => string.Empty;

    [ObservableProperty]
    public partial bool EstOccupe { get; protected set; }

    public ObservableCollection<string> Erreurs { get; } = [];

    public bool AErreurs => Erreurs.Count > 0;

    protected CancellationToken JetonEcran => _annulation.Token;

    public void Dispose()
    {
        if (_estFerme)
        {
            return;
        }

        _estFerme = true;
        _annulation.Cancel();
        SurFermeture();
        // Source non libérée : JetonEcran reste lisible (déjà annulé) pour une opération qui se termine après la fermeture.
        GC.SuppressFinalize(this);
    }

    /// <summary>Libère les écrans enfants ou abonnements propres à l'écran.</summary>
    protected virtual void SurFermeture()
    {
    }

    protected void AfficherErreurs(IEnumerable<string> erreurs)
    {
        Erreurs.Clear();
        foreach (var erreur in erreurs)
        {
            Erreurs.Add(erreur);
        }
    }

    protected void EffacerErreurs() => Erreurs.Clear();

    /// <summary>Exécute une opération en signalant l'écran occupé ; l'opération reçoit le jeton de l'écran.</summary>
    protected async Task ExecuterOccupeAsync(Func<CancellationToken, Task> operation)
    {
        EstOccupe = true;
        try
        {
            await operation(JetonEcran);
        }
        catch (OperationCanceledException) when (JetonEcran.IsCancellationRequested)
        {
            // Écran fermé pendant l'opération : rien à afficher.
        }
        finally
        {
            EstOccupe = false;
        }
    }
}
