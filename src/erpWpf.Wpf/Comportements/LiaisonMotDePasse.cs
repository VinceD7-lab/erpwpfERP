using System.Windows;
using System.Windows.Controls;

namespace erpWpf.Wpf.Comportements;

/// <summary>
/// Rend PasswordBox.Password liable au ViewModel (la propriété n'est pas une dependency property).
/// Liaison dans les deux sens : un ViewModel qui vide le mot de passe vide aussi le champ.
/// </summary>
public static class LiaisonMotDePasse
{
    public static readonly DependencyProperty MotDePasseProperty = DependencyProperty.RegisterAttached(
        "MotDePasse",
        typeof(string),
        typeof(LiaisonMotDePasse),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, SurMotDePasseModifie));

    private static readonly DependencyProperty AbonneProperty =
        DependencyProperty.RegisterAttached("Abonne", typeof(bool), typeof(LiaisonMotDePasse));

    public static string GetMotDePasse(DependencyObject element) => (string)element.GetValue(MotDePasseProperty);

    public static void SetMotDePasse(DependencyObject element, string valeur) => element.SetValue(MotDePasseProperty, valeur);

    private static void SurMotDePasseModifie(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        if (element is not PasswordBox champ)
        {
            return;
        }

        if (!(bool)champ.GetValue(AbonneProperty))
        {
            champ.SetValue(AbonneProperty, true);
            champ.PasswordChanged += (_, _) => SetMotDePasse(champ, champ.Password);
        }

        var valeur = e.NewValue as string ?? string.Empty;
        if (champ.Password != valeur)
        {
            champ.Password = valeur;
        }
    }
}
