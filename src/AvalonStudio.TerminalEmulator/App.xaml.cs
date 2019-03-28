using Avalonia;
using Avalonia.Markup.Xaml;

namespace AvalonStudio.TerminalEmulator
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }
   }
}