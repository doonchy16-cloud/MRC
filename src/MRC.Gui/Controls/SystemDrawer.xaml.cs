using System;
using System.Windows;
using System.Windows.Controls;

namespace MRC.Gui.Controls;

public partial class SystemDrawer : UserControl
{
    public static readonly DependencyProperty StatusTextProperty = DependencyProperty.Register(
        nameof(StatusText), typeof(string), typeof(SystemDrawer), new PropertyMetadata(string.Empty));

    public SystemDrawer()
    {
        InitializeComponent();
    }

    public event EventHandler? CloseRequested;

    public string StatusText
    {
        get => (string)GetValue(StatusTextProperty);
        set => SetValue(StatusTextProperty, value);
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
        e.Handled = true;
    }
}
