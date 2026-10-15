using System.Windows;
using System.Windows.Controls;

namespace erpWpf.Wpf.Controles;

/// <summary>Titre d'écran et zone de boutons d'action (ex-Tag Helper &lt;en-tete-page&gt;) ; gabarit dans Styles.xaml.</summary>
public sealed class EnTetePage : ContentControl
{
    public static readonly DependencyProperty TitreProperty =
        DependencyProperty.Register(nameof(Titre), typeof(string), typeof(EnTetePage), new PropertyMetadata(string.Empty));

    public string Titre
    {
        get => (string)GetValue(TitreProperty);
        set => SetValue(TitreProperty, value);
    }
}
