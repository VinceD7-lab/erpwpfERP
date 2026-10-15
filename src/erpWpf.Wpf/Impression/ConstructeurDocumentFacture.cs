using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using erpWeb.Core.Factures;
using erpWpf.Wpf.ViewModels.Communs;

namespace erpWpf.Wpf.Impression;

/// <summary>
/// Document imprimable d'une facture (ex-vue Factures/Imprimer.cshtml et règles @media print) :
/// partagé par l'aperçu à l'écran et l'impression.
/// </summary>
public static class ConstructeurDocumentFacture
{
    private static readonly CultureInfo _culture = CultureInfo.GetCultureInfo("fr-FR");

    public static FlowDocument Construire(ImpressionFactureDto impression)
    {
        var facture = impression.Facture;
        var document = new FlowDocument
        {
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 12,
            PagePadding = new Thickness(48),
            ColumnWidth = double.PositiveInfinity,
            Language = System.Windows.Markup.XmlLanguage.GetLanguage(_culture.IetfLanguageTag),
        };

        document.Blocks.Add(EnTete(impression));
        document.Blocks.Add(new Paragraph(new Run("Facturé à")) { FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 24, 0, 4) });
        document.Blocks.Add(Bloc(
            facture.NomClient,
            facture.AdresseFacturation,
            string.Join(" ", new[] { facture.CodePostal, facture.Ville }.Where(valeur => !string.IsNullOrWhiteSpace(valeur))),
            facture.Pays,
            $"Code client : {facture.CodeClient}"));

        document.Blocks.Add(TableauLignes(facture));
        document.Blocks.Add(Totaux(facture));

        document.Blocks.Add(Bloc(
            $"Mode de paiement : {facture.ModePaiement ?? "—"}",
            facture.DatePaiement is { } datePaiement ? $"Payée le {datePaiement.ToString("d", _culture)}" : null,
            facture.Commentaire));
        return document;
    }

    private static Table EnTete(ImpressionFactureDto impression)
    {
        var facture = impression.Facture;
        var tableau = new Table { CellSpacing = 0 };
        tableau.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        tableau.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });

        var emetteur = Bloc([impression.NomEmetteur, .. impression.AdresseEmetteur.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)]);
        if (emetteur.Blocks.FirstBlock is Paragraph nomEmetteur)
        {
            nomEmetteur.FontSize = 16;
            nomEmetteur.FontWeight = FontWeights.Bold;
        }

        var references = Bloc(
            $"Facture n° {facture.NumeroFacture.ToString("D6", _culture)}",
            $"Date : {facture.DateFacture.ToString("d", _culture)}",
            facture.DateEcheance is { } echeance ? $"Échéance : {echeance.ToString("d", _culture)}" : null,
            $"Statut : {LibellesStatuts.Libelle(facture.StatutFacture)}");
        ((Paragraph)references.Blocks.FirstBlock).FontSize = 16;
        ((Paragraph)references.Blocks.FirstBlock).FontWeight = FontWeights.Bold;
        references.TextAlignment = TextAlignment.Right;

        var ligne = new TableRow();
        ligne.Cells.Add(new TableCell(emetteur));
        ligne.Cells.Add(new TableCell(references));
        tableau.RowGroups.Add(new TableRowGroup { Rows = { ligne } });
        return tableau;
    }

    private static Table TableauLignes(FactureDto facture)
    {
        var tableau = new Table { CellSpacing = 0, Margin = new Thickness(0, 24, 0, 0), BorderBrush = Brushes.Gray, BorderThickness = new Thickness(0, 0, 0, 1) };
        tableau.Columns.Add(new TableColumn { Width = new GridLength(3, GridUnitType.Star) });
        tableau.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });

        var entete = new TableRow { Background = new SolidColorBrush(Color.FromRgb(0xF0, 0xF2, 0xF5)), FontWeight = FontWeights.SemiBold };
        entete.Cells.Add(Cellule("Désignation"));
        entete.Cells.Add(Cellule("Montant HT", TextAlignment.Right));

        var ligne = new TableRow();
        ligne.Cells.Add(Cellule($"Analyse de l'échantillon {facture.CodeBarresAnonyme} ({facture.Filiere})"));
        ligne.Cells.Add(Cellule(Montant(facture.MontantHorsTaxeAvantRemise, facture.Devise), TextAlignment.Right));

        tableau.RowGroups.Add(new TableRowGroup { Rows = { entete, ligne } });
        return tableau;
    }

    private static Table Totaux(FactureDto facture)
    {
        var tableau = new Table { CellSpacing = 0, Margin = new Thickness(0, 12, 0, 24) };
        tableau.Columns.Add(new TableColumn { Width = new GridLength(3, GridUnitType.Star) });
        tableau.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });

        var lignes = new TableRowGroup();
        if (facture.Remise is > 0)
        {
            lignes.Rows.Add(LigneTotal($"Remise ({facture.Remise.Value.ToString("0.##", _culture)} %)", string.Empty));
        }

        lignes.Rows.Add(LigneTotal("Total HT", Montant(facture.MontantHorsTaxe, facture.Devise)));
        lignes.Rows.Add(LigneTotal($"TVA ({facture.TauxTva.ToString("0.##", _culture)} %)", Montant(facture.MontantTva, facture.Devise)));
        var total = LigneTotal("Total TTC", Montant(facture.MontantToutesTaxesComprises, facture.Devise));
        total.FontWeight = FontWeights.Bold;
        lignes.Rows.Add(total);

        tableau.RowGroups.Add(lignes);
        return tableau;
    }

    private static TableRow LigneTotal(string libelle, string montant)
    {
        var ligne = new TableRow();
        ligne.Cells.Add(Cellule(libelle, TextAlignment.Right));
        ligne.Cells.Add(Cellule(montant, TextAlignment.Right));
        return ligne;
    }

    private static TableCell Cellule(string texte, TextAlignment alignement = TextAlignment.Left)
        => new(new Paragraph(new Run(texte)) { TextAlignment = alignement, Margin = new Thickness(0) }) { Padding = new Thickness(6, 4, 6, 4) };

    /// <summary>Une ligne de texte par valeur renseignée (adresse, références…).</summary>
    private static Section Bloc(params string?[] lignes)
    {
        var section = new Section();
        foreach (var ligne in lignes.Where(ligne => !string.IsNullOrWhiteSpace(ligne)))
        {
            section.Blocks.Add(new Paragraph(new Run(ligne)) { Margin = new Thickness(0) });
        }

        return section;
    }

    private static string Montant(decimal montant, string devise) => $"{montant.ToString("N2", _culture)} {devise}";
}
