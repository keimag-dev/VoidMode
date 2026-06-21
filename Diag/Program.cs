using System;
using System.Runtime.InteropServices;
using NAudio.CoreAudioApi;

class Program
{
    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    private const uint WM_SYSCOMMAND = 0x0112;
    private const int SC_MONITORPOWER = 0xF170;
    private const int MONITOR_OFF = 2;

    static void Main(string[] args)
    {
        Console.WriteLine("Starting VoidMode Crash Reproduction Test...");

        // Test 1: NAudio (Confirmed working)
        try
        {
            var device = new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            Console.WriteLine($"[1/2] NAudio Test: SUCCESS (Device: {device.FriendlyName})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[1/2] NAudio Test: FAILED - {ex.Message}");
        }

        // Test 2: SendMessage (The suspected culprit)
        Console.WriteLine("[2/2] Testing SendMessage (Monitor Off)...");
        try
        {
            // ブロードキャスト送信を試行
            SendMessage((IntPtr)0xffff, WM_SYSCOMMAND, (IntPtr)SC_MONITORPOWER, (IntPtr)MONITOR_OFF);
            Console.WriteLine("SendMessage executed. If it didn't crash, this part is safe.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[2/2] SendMessage Test: FAILED - {ex.Message}");
        }

        Console.WriteLine("\nAll tests finished. If you see this, the app didn't crash.");
    }
}
