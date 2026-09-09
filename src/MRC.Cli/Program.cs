namespace MRC.Cli;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var dispatcher = new CliDispatcher(new GuiLauncher());
        return await dispatcher.ExecuteAsync(args, Console.Out, Console.Error);
    }
}
