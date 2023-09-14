
using eyecandy;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace opengl_upscale_test;

internal class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("\nOpenGL Upscaling Performance Test\n\n");
        Console.WriteLine("Average FPS and other details are output every 10 seconds.\n");
        Console.WriteLine("SPACE - Toggle fullscreen / windowed");
        Console.WriteLine("ENTER - Toggle buffered / direct output");
        Console.WriteLine("  Z   - Toggle small / full-size buffer (only when buffered)");

        // uses eyecandy Shader class but not the base window
        ErrorLogging.Strategy = LoggingStrategy.AlwaysOutputToConsole;

        var otkWinSettings = GameWindowSettings.Default;
        var otkNativeSettings = NativeWindowSettings.Default;

        otkNativeSettings.Title = "Upscaling Test";
        otkNativeSettings.Size = (960, 540);
        otkNativeSettings.APIVersion = new Version(4, 6);

        // Debug-message callbacks using the modern 4.3+ KHR style
        // https://opentk.net/learn/appendix_opengl/debug_callback.html?tabs=debug-context-4%2Cdelegate-gl%2Cenable-gl
        otkNativeSettings.Flags = ContextFlags.Debug;

        var win = new Win(otkWinSettings, otkNativeSettings);
        win.Focus();
        win.Run();
        win.Dispose();
    }
}
