using erpWeb.Core.Utilisateurs;
using erpWpf.Wpf.Services;
using Microsoft.Extensions.Logging;

namespace erpWpf.Wpf.Securite;

/// <summary>
/// Relit périodiquement la session ouverte (équivalent du SecurityStampValidator du web) :
/// un compte désactivé ou dont le mot de passe a été réinitialisé est déconnecté en moins d'une minute,
/// et les permissions suivent les changements de rôles.
/// </summary>
public sealed class SurveillanceSession : IDisposable
{
    public static readonly TimeSpan Intervalle = TimeSpan.FromMinutes(1);

    private readonly ISessionUtilisateur _session;
    private readonly IExecuteurOperations _executeur;
    private readonly TimeProvider _horloge;
    private readonly ILogger<SurveillanceSession> _journal;
    private ITimer? _minuterie;
    private int _verificationEnCours;

    public SurveillanceSession(ISessionUtilisateur session, IExecuteurOperations executeur, TimeProvider horloge, ILogger<SurveillanceSession> journal)
    {
        _session = session;
        _executeur = executeur;
        _horloge = horloge;
        _journal = journal;
    }

    /// <summary>Déclenché quand la session ouverte n'est plus valide et vient d'être fermée.</summary>
    public event EventHandler? SessionExpiree;

    /// <summary>
    /// À appeler depuis le thread d'interface : les vérifications y sont replacées, si bien que les événements
    /// de session (reconstruction du menu, retour à la connexion) s'exécutent sur ce thread.
    /// </summary>
    public void Demarrer()
    {
        Arreter();
        var contexte = SynchronizationContext.Current;
        _minuterie = _horloge.CreateTimer(
            _ =>
            {
                if (contexte is null)
                {
                    _ = VerifierAsync();
                }
                else
                {
                    contexte.Post(__ => _ = VerifierAsync(), null);
                }
            },
            null,
            Intervalle,
            Intervalle);
    }

    public void Arreter()
    {
        _minuterie?.Dispose();
        _minuterie = null;
    }

    public async Task VerifierAsync(CancellationToken jetonAnnulation = default)
    {
        var session = _session.Session;
        if (session is null || Interlocked.Exchange(ref _verificationEnCours, 1) == 1)
        {
            return;
        }

        try
        {
            var actualisee = await _executeur.ExecuterAsync<IServiceAuthentification, SessionDto?>(
                (service, jeton) => service.ActualiserSessionAsync(session.IdUtilisateur, session.TamponSecurite, jeton),
                jetonAnnulation);

            if (!ReferenceEquals(_session.Session, session))
            {
                // Session fermée ou remplacée pendant la vérification : résultat sans objet.
                return;
            }

            if (actualisee is null)
            {
                _session.Fermer();
                SessionExpiree?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                _session.Actualiser(actualisee);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Base momentanément injoignable : la session est conservée, nouvelle vérification au prochain intervalle.
            _journal.LogWarning(exception, "Vérification de la session impossible.");
        }
        finally
        {
            Volatile.Write(ref _verificationEnCours, 0);
        }
    }

    public void Dispose() => Arreter();
}
