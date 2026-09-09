using System.Windows;
using MRC.Gui.Presentation;

namespace MRC.Gui;

public partial class DiagnosticDialog : Window
{
    public DiagnosticDialog(RunnerRowViewModel row)
    {
        ArgumentNullException.ThrowIfNull(row);
        InitializeComponent();
        DataContext = row;
    }
}
