using Microsoft.Win32; // Provides access to the Windows Registry classes (Registry, RegistryKey, etc.)
using System;          // Basic .NET types like Exception, Console, etc.

class Program
{
    static int Main(string[] args)
    {
        // This is the per-user registry path where Windows stores tray icon visibility settings.
        // "HKCU" (HKEY_CURRENT_USER) means these settings apply only to the currently logged-in user.
        const string subkeyPath = @"Control Panel\NotifyIconSettings";

        // Track how many entries we actually changed from something else to "promoted".
        int changed = 0;

        // Track how many subkeys we failed to process due to permissions/corruption/races/etc.
        int errors = 0;

        try
        {
            // Open the NotifyIconSettings key under HKCU with write access (writable: true),
            // because we intend to modify values inside its subkeys.
            //
            // Equivalent full path:
            // HKEY_CURRENT_USER\Control Panel\NotifyIconSettings
            using RegistryKey? root = Registry.CurrentUser.OpenSubKey(subkeyPath, writable: true);

            // If Windows doesn't have this key (unexpected on a typical system), there's nothing to do.
            if (root == null)
            {
                Console.Error.WriteLine($"Registry key not found: HKCU\\{subkeyPath}");
                return 2; // Non-zero exit code indicates a "not found / nothing to do" condition.
            }

            // Each notification area icon entry is typically represented by a GUID-named subkey under NotifyIconSettings.
            // We enumerate all subkeys so we can set each one to "promoted" (visible).
            foreach (var name in root.GetSubKeyNames())
            {
                try
                {
                    // Open the specific subkey with write access.
                    // Each subkey corresponds to one icon entry Windows knows about.
                    using RegistryKey? k = root.OpenSubKey(name, writable: true);

                    // If we couldn't open it (rare, but possible), skip it.
                    if (k == null) continue;

                    // Read the current value for "IsPromoted".
                    // This DWORD typically controls whether the icon is "shown" (promoted) in the system tray area.
                    object? current = k.GetValue("IsPromoted");

                    // Convert to int if it is already stored as an integer.
                    // If it isn't an int (missing, different type, etc.), we treat it as not being set to 1.
                    int currentInt = (current is int i) ? i : -1;

                    // Only write if it's not already promoted.
                    // This avoids unnecessary registry writes.
                    if (currentInt != 1)
                    {
                        // Write DWORD value 1 for "IsPromoted" to mark it as promoted/visible.
                        k.SetValue("IsPromoted", 1, RegistryValueKind.DWord);

                        // Count it as a change.
                        changed++;
                    }
                }
                catch (Exception ex)
                {
                    // If any individual subkey fails (locked, permissions, transient issue),
                    // we count an error but keep going so one failure doesn't stop the entire run.
                    errors++;
                    Console.Error.WriteLine($"Failed on subkey {name}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            // If something more fundamental fails (e.g., registry access fails entirely),
            // print the exception and return a different error code.
            Console.Error.WriteLine(ex.ToString());
            return 3;
        }

        // Summarize what happened.
        Console.WriteLine($"Done. Updated {changed} icon entries. Errors: {errors}.");

        // Exit code 0 = success with no per-subkey errors.
        // Exit code 1 = ran, but at least one subkey update failed.
        return errors == 0 ? 0 : 1;
    }
}
