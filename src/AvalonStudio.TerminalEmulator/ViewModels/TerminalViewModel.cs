using Avalonia.Threading;
using AvalonStudio.Terminals;
using AvalonStudio.Terminals.Unix;
using AvalonStudio.Terminals.Win32;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using VtNetCore.Avalonia;

namespace AvalonStudio.TerminalEmulator.ViewModels
{
    public class TerminalViewModel : ReactiveObject
    {
        private IConnection _connection;
        private bool _terminalVisible;
        private object _createLock = new object();
        static IPsuedoTerminalProvider s_provider = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? new Win32PsuedoTerminalProvider() : new UnixPsuedoTerminalProvider() as IPsuedoTerminalProvider;

        public TerminalViewModel()
        {
            Dispatcher.UIThread.Post(() =>
            {
                CreateConnection();
            });
        }


        private void CreateConnection(string workingDirectory = null)
        {
            lock (_createLock)
            {
                if (workingDirectory == null)
                {
                    workingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                }

                CloseConnection();

                var args = new List<string>();

                if(RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    args.Add("-l");
                }

                var shellExecutable = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? Environment.ExpandEnvironmentVariables("%SystemRoot%\\system32\\WindowsPowerShell\\v1.0\\powershell.exe") : "/bin/bash";

                if (shellExecutable != null)
                {
                    var terminal = s_provider.Create(80, 32, workingDirectory, null, shellExecutable, args.ToArray());

                    Connection = new PsuedoTerminalConnection(terminal);

                    TerminalVisible = true;

                    Connection.Closed += Connection_Closed;
                }
            }
        }

        private void CloseConnection()
        {
            if (Connection != null)
            {
                System.Console.WriteLine("Closing Terminal");
                Connection.Closed -= Connection_Closed;
                Connection.Disconnect();
                Connection = null;
            }
        }

        private void Connection_Closed(object sender, System.EventArgs e)
        {
            (sender as IConnection).Closed -= Connection_Closed;
            TerminalVisible = false;
        }

        public IConnection Connection
        {
            get { return _connection; }
            set { this.RaiseAndSetIfChanged(ref _connection, value); }
        }

        public bool TerminalVisible
        {
            get { return _terminalVisible; }
            set { this.RaiseAndSetIfChanged(ref _terminalVisible, value); }
        }
    }
}
