using System.Runtime.InteropServices;

namespace Canopus.App.Services;

/// <summary>
/// Private working set, the figure Task Manager shows as "Memory". <c>Process.WorkingSet64</c>
/// also counts pages shared with other processes (DLLs), which roughly doubles a WinUI app.
/// </summary>
internal static class ProcessMemory
{
    private const uint ProcessQueryLimitedInformation = 0x1000;

    public static double? PrivateWorkingSetMb(int processId)
    {
        IntPtr handle = OpenProcess(ProcessQueryLimitedInformation, false, processId);
        if (handle == IntPtr.Zero)
            return null;

        try
        {
            var counters = new ProcessMemoryCountersEx2 { Size = (uint)Marshal.SizeOf<ProcessMemoryCountersEx2>() };
            return GetProcessMemoryInfo(handle, ref counters, counters.Size)
                ? counters.PrivateWorkingSetSize / 1024d / 1024d
                : null;
        }
        finally
        {
            CloseHandle(handle);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessMemoryCountersEx2
    {
        public uint Size;
        public uint PageFaultCount;
        public nuint PeakWorkingSetSize;
        public nuint WorkingSetSize;
        public nuint QuotaPeakPagedPoolUsage;
        public nuint QuotaPagedPoolUsage;
        public nuint QuotaPeakNonPagedPoolUsage;
        public nuint QuotaNonPagedPoolUsage;
        public nuint PagefileUsage;
        public nuint PeakPagefileUsage;
        public nuint PrivateUsage;
        public nuint PrivateWorkingSetSize;
        public ulong SharedCommitUsage;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint desiredAccess, bool inheritHandle, int processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr handle);

    [DllImport("kernel32.dll", EntryPoint = "K32GetProcessMemoryInfo", SetLastError = true)]
    private static extern bool GetProcessMemoryInfo(IntPtr process, ref ProcessMemoryCountersEx2 counters, uint size);
}
