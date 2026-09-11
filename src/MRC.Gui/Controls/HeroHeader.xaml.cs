using System.Windows;
using System.Windows.Controls;

namespace MRC.Gui.Controls;

public partial class HeroHeader : UserControl
{
    public static readonly DependencyProperty InventoryTextProperty = DependencyProperty.Register(
        nameof(InventoryText), typeof(string), typeof(HeroHeader), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty TruthTextProperty = DependencyProperty.Register(
        nameof(TruthText), typeof(string), typeof(HeroHeader), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty VersionTextProperty = DependencyProperty.Register(
        nameof(VersionText), typeof(string), typeof(HeroHeader), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty AuthorizedProperty = DependencyProperty.Register(
        nameof(Authorized), typeof(bool), typeof(HeroHeader), new PropertyMetadata(false));

    public static readonly DependencyProperty CompactProperty = DependencyProperty.Register(
        nameof(Compact), typeof(bool), typeof(HeroHeader), new PropertyMetadata(false));

    public HeroHeader()
    {
        InitializeComponent();
        Loaded += (_, _) => UpdateResponsiveMode();
        SizeChanged += (_, _) => UpdateResponsiveMode();
    }

    public string InventoryText
    {
        get => (string)GetValue(InventoryTextProperty);
        set => SetValue(InventoryTextProperty, value);
    }

    public string TruthText
    {
        get => (string)GetValue(TruthTextProperty);
        set => SetValue(TruthTextProperty, value);
    }

    public string VersionText
    {
        get => (string)GetValue(VersionTextProperty);
        set => SetValue(VersionTextProperty, value);
    }

    public bool Authorized
    {
        get => (bool)GetValue(AuthorizedProperty);
        set => SetValue(AuthorizedProperty, value);
    }

    public bool Compact
    {
        get => (bool)GetValue(CompactProperty);
        set => SetValue(CompactProperty, value);
    }

    public event RoutedEventHandler? MenuRequested;

    private void UpdateResponsiveMode() => Compact = ActualWidth <= 960;

    private void Menu_OnClick(object sender, RoutedEventArgs e) =>
        MenuRequested?.Invoke(this, e);
}
