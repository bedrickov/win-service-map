using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace WinServiceSentry;

public class ServiceAuditor
{
    private const uint SC_MANAGER_ENUMERATE_SERVICE = 0x0004;
    private const uint SERVICE_WIN32 = 0x00000030;
    private const uint SERVICE_STATE_ALL = 0x00000003;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ENUM_SERVICE_STATUS_PROCESS
    {
        public IntPtr lpServiceName;
        public IntPtr lpDisplayName;
        public SERVICE_STATUS_PROCESS ServiceStatusProcess;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SERVICE_STATUS_PROCESS
    {
        public uint dwServiceType;
        public uint dwCurrentState;
        public uint dwControlsAccepted;
        public uint dwWin32ExitCode;
        public uint dwServiceSpecificExitCode;
        public uint dwCheckPoint;
        public uint dwWaitHint;
        public uint dwProcessId;
        public uint dwServiceFlags;
    }

    [DllImport("advapi32.dll", EntryPoint = "OpenSCManagerW", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr OpenSCManager(string? lpMachineName, string? lpDatabaseName, uint dwDesiredAccess);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool CloseServiceHandle(IntPtr hSCObject);

    [DllImport("advapi32.dll", EntryPoint = "EnumServicesStatusExW", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool EnumServicesStatusEx(
        IntPtr hSCManager,
        int InfoLevel,
        uint dwServiceType,
        uint dwServiceState,
        IntPtr lpServices,
        uint cbBufSize,
        out uint pcbBytesNeeded,
        out uint lpServicesReturned,
        ref uint lpResumeHandle,
        string? pszGroupName);

    public record ServiceEntry(string Name, string DisplayName, uint Pid, uint State);

    public static List<ServiceEntry> EnumerateServices()
    {
        var list = new List<ServiceEntry>();
        IntPtr scm = OpenSCManager(null, null, SC_MANAGER_ENUMERATE_SERVICE);
        if (scm == IntPtr.Zero) return list;

        try
        {
            uint bytesNeeded = 0;
            uint servicesReturned = 0;
            uint resumeHandle = 0;

            EnumServicesStatusEx(scm, 0, SERVICE_WIN32, SERVICE_STATE_ALL, IntPtr.Zero, 0,
                out bytesNeeded, out servicesReturned, ref resumeHandle, null);

            IntPtr buffer = Marshal.AllocHGlobal((int)bytesNeeded);
            try
            {
                if (EnumServicesStatusEx(scm, 0, SERVICE_WIN32, SERVICE_STATE_ALL, buffer, bytesNeeded,
                    out bytesNeeded, out servicesReturned, ref resumeHandle, null))
                {
                    int structSize = Marshal.SizeOf<ENUM_SERVICE_STATUS_PROCESS>();
                    for (int i = 0; i < servicesReturned; i++)
                    {
                        IntPtr currentPtr = IntPtr.Add(buffer, i * structSize);
                        var status = Marshal.PtrToStructure<ENUM_SERVICE_STATUS_PROCESS>(currentPtr);
                        string sName = Marshal.PtrToStringUni(status.lpServiceName) ?? "";
                        string dName = Marshal.PtrToStringUni(status.lpDisplayName) ?? "";
                        list.Add(new ServiceEntry(sName, dName, status.ServiceStatusProcess.dwProcessId, status.ServiceStatusProcess.dwCurrentState));
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
        finally
        {
            CloseServiceHandle(scm);
        }

        return list;
    }
}

class Program
{
    static int Main()
    {
        Console.WriteLine("[WinServiceSentry] High-speed Windows Services & Ghost PID Auditor");
        var services = ServiceAuditor.EnumerateServices();
        Console.WriteLine($"Total active & registered Win32 services: {services.Count}\n");

        Console.WriteLine($"{"PID",-8} {"STATE",-8} {"SERVICE NAME",-28} {"DISPLAY NAME"}");
        Console.WriteLine(new string('-', 75));

        int shown = 0;
        foreach (var s in services)
        {
            if (s.Pid > 0 && shown++ < 12)
            {
                string state = s.State == 4 ? "RUNNING" : "STOPPED";
                Console.WriteLine($"{s.Pid,-8} {state,-8} {(s.Name.Length > 26 ? s.Name[..26] : s.Name),-28} {s.DisplayName}");
            }
        }

        return services.Count > 0 ? 0 : 1;
    }
}
