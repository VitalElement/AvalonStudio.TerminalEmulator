using System;
using Avalonia;
using Avalonia.Logging.Serilog;
using AvalonStudio.TerminalEmulator.ViewModels;
using AvalonStudio.TerminalEmulator.Views;

namespace AvalonStudio.TerminalEmulator
{
    class Program
    {
        static void Main(string[] args)
        {
            BuildAvaloniaApp().Start<MainWindow>(() => new TerminalViewModel());
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .UseReactiveUI()
                .LogToDebug();
    }
}
