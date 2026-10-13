using erpWeb.Core.Factures;

namespace erpWpf.Wpf.Services;

/// <summary>Impression via la boîte de dialogue Windows (ex-window.print() de la page imprimable).</summary>
public interface IServiceImpression
{
    /// <summary>Retourne faux si l'utilisateur annule la boîte de dialogue d'impression.</summary>
    bool ImprimerFacture(ImpressionFactureDto impression);
}
