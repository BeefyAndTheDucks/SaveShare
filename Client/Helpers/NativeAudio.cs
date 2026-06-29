using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Client.Helpers;

public static class NativeAudio
{
    public static void PlayAlertSound()
    {
        if (AppDomain.CurrentDomain.FriendlyName.Contains("ReSharper") || Console.IsOutputRedirected)
            return; // Don't play sounds in testing environments such as ReSharper or unit tests.
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            System.Media.SystemSounds.Hand.Play();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            using Process process = Process.Start("osascript", "-e \"beep\"");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            if (TryExecuteCommand("canberra-gtk-play", "--id=\"dialog-warning\""))
                return;

            if (TryExecuteCommand("pw-play", "/usr/share/sounds/freedesktop/stereo/dialog-warning.oga"))
                return;

            if (TryExecuteCommand("paplay", "/usr/share/sounds/freedesktop/stereo/dialog-warning.oga"))
                return;

            TryExecuteCommand("aplay", "/usr/share/sounds/freedesktop/stereo/dialog-warning.oga");
        }
    }
    
    private static bool TryExecuteCommand(string command, string arguments)
    {
        try
        {
            using Process? process = Process.Start(new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            // A short timeout to ensure the command exists and didn't immediately fail
            if (process != null && !process.WaitForExit(50))
            {
                return true; // Process started successfully and is playing the sound
            }
            
            return process?.ExitCode == 0;
        }
        catch
        {
            return false; // Command doesn't exist on this system, move to the next fallback
        }
    }
}