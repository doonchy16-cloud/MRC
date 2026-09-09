using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace MRC.Core.Runtime;

internal static class WindowsNativeProcess
{
    private const uint ProcessQueryLimitedInformation = 0x1000;

    public static bool TryGetImagePath(int processId, out string? path, out string? error)
    {
        path = null;
        error = null;

        if (!OperatingSystem.IsWindows())
        {
            error = "Process path inspection is supported only on Windows.";
            return false;
        }

        var handle = OpenProcess(ProcessQueryLimitedInformation, false, processId);
        if (handle == IntPtr.Zero)
        {
            error = new Win32Exception(Marshal.GetLastWin32Error()).Message;
            return false;
        }

        try
        {
            var builder = new StringBuilder(32768);
            var size = builder.Capacity;
            if (!QueryFullProcessImageName(handle, 0, builder, ref size))
            {
                error = new Win32Exception(Marshal.GetLastWin32Error()).Message;
                return false;
            }

            path = builder.ToString();
            return !string.IsNullOrWhiteSpace(path);
        }
        finally
        {
            CloseHandle(handle);
        }
    }

    public static bool TryGetParentProcessId(int processId, out int? parentProcessId, out string? error)
    {
        parentProcessId = null;
        error = null;

        if (!OperatingSystem.IsWindows())
        {
            error = "Parent-process inspection is supported only on Windows.";
            return false;
        }

        var handle = OpenProcess(ProcessQueryLimitedInformation, false, processId);
        if (handle == IntPtr.Zero)
        {
            error = new Win32Exception(Marshal.GetLastWin32Error()).Message;
            return false;
        }

        try
        {
            var status = NtQueryInformationProcess(
                handle,
                0,
                out var info,
                Marshal.SizeOf<ProcessBasicInformation>(),
                out _);

            if (status != 0)
            {
                error = $"NtQueryInformationProcess failed with NTSTATUS 0x{status:X8}.";
                return false;
            }

            var rawParent = info.InheritedFromUniqueProcessId.ToInt64();
            if (rawParent < 0 || rawParent > int.MaxValue)
            {
                error = "Parent process ID was outside the supported range.";
                return false;
            }

            parentProcessId = (int)rawParent;
            return true;
        }
        finally
        {
            CloseHandle(handle);
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint processAccess, bool inheritHandle, int processId);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool QueryFullProcessImageName(
        IntPtr processHandle,
        int flags,
        StringBuilder executablePath,
        ref int size);

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr handle);

    [DllImport("ntdll.dll")]
    private static extern int NtQueryInformationProcess(
        IntPtr processHandle,
        int processInformationClass,
        out ProcessBasicInformation processInformation,
        int processInformationLength,
        out int returnLength);

    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessBasicInformation
    {
        public IntPtr ExitStatus;
        public IntPtr PebBaseAddress;
        public IntPtr AffinityMask;
        public IntPtr BasePriority;
        public IntPtr UniqueProcessId;
        public IntPtr InheritedFromUniqueProcessId;
    }
}
