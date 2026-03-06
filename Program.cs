using Avalonia;
using System;
using System.Runtime.InteropServices;

namespace GPU_T;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static int Main(string[] args) // Changed from void to int
    {
        // 1. Intercept the disposable child process for Linux library probing
        if (args.Length == 2 && args[0] == "--check-lib")
        {
            try
            {
                if (NativeLibrary.TryLoad(args[1], out IntPtr handle))
                {
                    NativeLibrary.Free(handle);
                    return 0; // Success: Library exists and loaded safely
                }
            }
            catch 
            { 
                // Ignore exceptions, just return failure below
            }
            
            return 1; // Failure: Library failed to load (or LLVM crashed us)
        }

        // 2. Normal Avalonia UI Startup
        // StartWithClassicDesktopLifetime conveniently returns an int exit code!
        return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}