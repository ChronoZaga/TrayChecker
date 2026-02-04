Checks all the boxes so that every single icon is displayed in your Windows 11 system tray, and not hidden in the "^" menu. 

Requires .NET v8 to be installed.

Just double click the EXE, it runs and exits without displaying anything itself.  But Windows 11 should now display all your tray icons.  This is a per user setting.

I know this is a simple PowerShell one-liner, but I like building helpful little EXEs for myself, and I might as well share them.  View the code here on GitHub if you're curious.

Set-ItemProperty -Path "HKCU:\Control Panel\NotifyIconSettings\*" -Name IsPromoted -Value 1
