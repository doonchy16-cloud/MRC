using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace MRC.Core.Runtime;

internal static class WindowsNativeProcess
{
    private const uint ProcessQueryLimitedInformation = 0x1000;
    private const uint Th32CsSnapProcess = 0x00000002;
    private static readonly IntPtr InvalidHandleValue = new(-1);

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

    public static IReadOnlyDictionary<int, int?> CaptureParentProcessIds()
    {
        if (!OperatingSystem.IsWindows())
        {
            return new Dictionary<int, int?>();
        }

        var snapshot = CreateToolhelp32Snapshot(Th32CsSnapProcess, 0);
        if (snapshot == IntPtr.Zero || snapshot == InvalidHandleValue)
        {
            return new Dictionary<int, int?>();
        }

        try
        {
            var parents = new Dictionary<int, int?>();
            var entry = new ProcessEntry32
            {
                Size = (uint)Marshal.SizeOf<ProcessEntry32>()
            };

            if (!Process32First(snapshot, ref entry))
            {
                return parents;
            }

            do
            {
                if (entry.ProcessId <= int.MaxValue)
                {
                    int? parent = entry.ParentProcessId <= int.MaxValue
                        ? (int)entry.ParentProcessId
                        : null;
                    parents[(int)entry.ProcessId] = parent;
                }

                entry.Size = (uint)Marshal.SizeOf<ProcessEntry32>();
            }
            while (Process32Next(snapshot, ref entry));

            return parents;
        }
        finally
        {
            CloseHandle(snapshot);
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

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr CreateToolhelp32Snapshot(uint flags, uint processId);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "Process32FirstW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Process32First(IntPtr snapshot, ref ProcessEntry32 entry);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "Process32NextW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Process32Next(IntPtr snapshot, ref ProcessEntry32 entry);

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

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ProcessEntry32
    {
        public uint Size;
        public uint Usage;
        public uint ProcessId;
        public IntPtr DefaultHeapId;
        public uint ModuleId;
        public uint ThreadCount;
        public uint ParentProcessId;
        public int PriorityClassBase;
        public uint Flags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string ExecutableFile;
    }

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
