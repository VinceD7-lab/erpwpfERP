using System.Windows.Controls;
using System.Windows.Documents;
using erpWeb.Core.Factures;
using erpWpf.Wpf.Impression;

namespace erpWpf.Wpf.Services;

public sealed class ServiceImpression : IServiceImpression
{
    public bool ImprimerFacture(ImpressionFactureDto impression)
    {
        var dialogue = new PrintDialog();
        if (dialogue.ShowDialog() != true)
        {
            return false;
        }

        var document = ConstructeurDocumentFacture.Construire(impression);
        document.PageHeight = dialogue.PrintableAreaHeight;
        document.PageWidth = dialogue.PrintableAreaWidth;
        dialogue.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, $"Facture {impression.Facture.NumeroFacture:D6}");
        return true;
    }
}
