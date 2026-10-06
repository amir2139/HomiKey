using System.Diagnostics;

namespace HomiKey.App.Native;

/// <summary>Identifies the program that owns a window.</summary>
internal static class ForegroundApp
{
    /// <summary>The program name of the window's process, like "notepad.exe", or null if unknown.</summary>
    public static string? ProcessName(IntPtr window)
    {
        if (window == IntPtr.Zero) return null;
        NativeMethods.GetWindowThreadProcessId(window, out var pid);
        if (pid == 0) return null;
        try
        {
            using var process = Process.GetProcessById((int)pid);
            return process.ProcessName + ".exe";
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return null;
        }
    }

    /// <summary>Program names of running apps that have a window, sorted, for the exclude-list picker.</summary>
    public static IReadOnlyList<string> RunningWithWindows()
    {
        var names = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        var self = Environment.ProcessId;
        foreach (var process in Process.GetProcesses())
        {
            using (process)
            {
                try
                {
                    if (process.Id != self && process.MainWindowHandle != IntPtr.Zero)
                        names.Add(process.ProcessName + ".exe");
                }
                catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
                {
                }
            }
        }
        return names.ToList();
    }

    /// <summary>"Notepad.EXE " and "notepad" both become "notepad.exe".</summary>
    public static string Normalize(string name)
    {
        var n = name.Trim().Trim('"');
        n = Path.GetFileName(n);
        if (!n.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) n += ".exe";
        return n.ToLowerInvariant();
    }
}
