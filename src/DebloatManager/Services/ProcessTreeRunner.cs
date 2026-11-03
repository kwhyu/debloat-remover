using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DebloatManager.Services;

public sealed record ProcessTreeResult(int ExitCode, bool TimedOut);

public static partial class ProcessTreeRunner
{
    private const int JobObjectBasicAccountingInformation = 1;

    // Many uninstallers copy themselves to a temp folder and exit, so wait for the whole job to finish.
    public static async Task<ProcessTreeResult> RunAsync(ProcessStartInfo startInfo, TimeSpan timeout)
    {
        var job = CreateJobObjectW(IntPtr.Zero, null);
        try
        {
            using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("The process did not start.");
            var tracked = job != IntPtr.Zero && AssignProcessToJobObject(job, process.Handle);

            using var cancellation = new CancellationTokenSource(timeout);
            try
            {
                await process.WaitForExitAsync(cancellation.Token);
                while (tracked && ActiveProcessCount(job) > 0)
                {
                    await Task.Delay(250, cancellation.Token);
                }
            }
            catch (OperationCanceledException)
            {
                return new ProcessTreeResult(process.HasExited ? process.ExitCode : -1, true);
            }

            return new ProcessTreeResult(process.ExitCode, false);
        }
        finally
        {
            if (job != IntPtr.Zero) CloseHandle(job);
        }
    }

    private static uint ActiveProcessCount(IntPtr job)
    {
        var info = default(JobBasicAccounting);
        var size = (uint)Marshal.SizeOf<JobBasicAccounting>();
        return QueryInformationJobObject(job, JobObjectBasicAccountingInformation, ref info, size, IntPtr.Zero)
            ? info.ActiveProcesses
            : 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct JobBasicAccounting
    {
        public long TotalUserTime;
        public long TotalKernelTime;
        public long ThisPeriodTotalUserTime;
        public long ThisPeriodTotalKernelTime;
        public uint TotalPageFaultCount;
        public uint TotalProcesses;
        public uint ActiveProcesses;
        public uint TotalTerminatedProcesses;
    }

    [LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    private static partial IntPtr CreateJobObjectW(IntPtr attributes, string? name);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AssignProcessToJobObject(IntPtr job, IntPtr process);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool QueryInformationJobObject(IntPtr job, int infoClass, ref JobBasicAccounting info, uint length, IntPtr returnLength);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CloseHandle(IntPtr handle);
}
