namespace MRC.Pass4.Tests;

internal static class Program
{
    private static int Main()
    {
        var failures = 0;
        failures += OperationsAcceptance.Run();
        Console.WriteLine();
        failures += GuiOperationsAcceptance.Run();
        Console.WriteLine();
        failures += GuiVisualPolishAcceptance.Run();
        Console.WriteLine();
        failures += DoctorAcceptance.Run();
        Console.WriteLine();
        failures += UpdateAcceptance.Run();
        return failures == 0 ? 0 : 1;
    }
}
