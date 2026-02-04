using System;
using Microsoft.Win32;
using System.IO;

public class RegistryIconManager
{
    public static int Main()
    {
        string subkeyPath = @"Control Panel\NotifyIconSettings";  // Target registry path

        int changed = 0;   // Count of successful updates
        int errors = 0;    // Count of failures

        // Full file path for logs in the same folder as the executable
        string logFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.txt");

        FileStream? logStream = null;
        StreamWriter? logWriter = null;

        try
        {
            // Open log file in append mode to avoid overwriting previous entries
            logStream = new FileStream(logFile, FileMode.Append, FileAccess.Write);
            logWriter = new StreamWriter(logStream);

            // Start with a timestamp and initial status message
            string startMsg = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Starting registry icon promotion process";
            Console.WriteLine(startMsg);
            logWriter.WriteLine(startMsg);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to initialize logging: {ex.Message}");
            return 1; // Return error code
        }

        try
        {
            // Open the target registry key with write access
            using (RegistryKey? root = Registry.CurrentUser.OpenSubKey(subkeyPath, true))
            {
                if (root == null)
                {
                    string msg = $"Registry key not found: HKCU\\{subkeyPath}";
                    Console.Error.WriteLine(msg);
                    logWriter?.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {msg}");
                    return 2; // Key missing
                }

                foreach (string name in root.GetSubKeyNames())
                {
                    try
                    {
                        using RegistryKey? subkey = root.OpenSubKey(name, true);
                        if (subkey == null) continue;

                        object? currentVal = subkey.GetValue("IsPromoted");
                        int val = (currentVal is int i) ? i : -1;

                        // If "IsPromoted" not set to 1, update it
                        if (val != 1)
                        {
                            subkey.SetValue("IsPromoted", 1, RegistryValueKind.DWord);
                            changed++;
                        }
                    }
                    catch (Exception ex)
                    {
                        errors++;
                        Console.Error.WriteLine($"Failed on {name}: {ex.Message}");
                        logWriter?.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Failed on {name}: {ex.Message}");
                    }
                }

                // Summary output
                string summary = $"Done. Updated {changed} icon entries. Errors: {errors}.";
                Console.WriteLine(summary);
                logWriter?.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {summary}");
            }
        }
        catch (Exception ex)
        {
            // Log and return on critical error
            string err = $"Critical registry operation failed: {ex.Message}";
            Console.Error.WriteLine(err);
            logWriter?.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {err}");
            return 3;
        }
        finally
        {
            logWriter?.Dispose();
            logStream?.Close();
        }

        // Final output to console only
        Console.WriteLine("Finished.");
        return errors == 0 ? 0 : 1;
    }
}
