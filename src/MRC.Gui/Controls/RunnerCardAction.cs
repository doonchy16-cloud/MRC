using MRC.Gui.Presentation;

namespace MRC.Gui.Controls;

public enum RunnerCardAction
{
    Start,
    Stop,
    Restart,
    ForceStop,
    Details
}

public sealed class RunnerCardActionRequestedEventArgs : EventArgs
{
    public RunnerCardActionRequestedEventArgs(RunnerRowViewModel row, RunnerCardAction action)
    {
        Row = row ?? throw new ArgumentNullException(nameof(row));
        Action = action;
    }

    public RunnerRowViewModel Row { get; }
    public RunnerCardAction Action { get; }
}
