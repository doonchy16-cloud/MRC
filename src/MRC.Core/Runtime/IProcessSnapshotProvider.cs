namespace MRC.Core.Runtime;

internal interface IProcessSnapshotProvider
{
    ProcessInventory Capture();
}
