using System;
using Microsoft.Win32;

public class RegistryIconManager
{
    public static int Main()
    {
        string subkeyPath = @"Control Panel\NotifyIconSettings";  // Target registry path

        int changed = 0;   // Count of successful updates
        int errors = 0;    // Count of failures

        Console.WriteLine("Starting registry icon promotion process");

        try
        {
            // Open the target registry key with write access
            using (RegistryKey? root = Registry.CurrentUser.OpenSubKey(subkeyPath, true))
            {
                if (root == null)
                {
                    Console.Error.WriteLine($"Registry key not found: HKCU\\{subkeyPath}");
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
                    }
                }

                // Summary output
                Console.WriteLine($"Done. Updated {changed} icon entries. Errors: {errors}.");
            }
        }
        catch (Exception ex)
        {
            // Report and return on critical error
            Console.Error.WriteLine($"Critical registry operation failed: {ex.Message}");
            return 3;
        }

        Console.WriteLine("Finished.");
        return errors == 0 ? 0 : 1;
    }
}
