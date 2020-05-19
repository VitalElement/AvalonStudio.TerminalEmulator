using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AvalonStudio.TerminalEmulator.ViewModels;
using VtNetCore.Avalonia;

namespace AvalonStudio.TerminalEmulator.Views
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            DataContext = new TerminalViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}