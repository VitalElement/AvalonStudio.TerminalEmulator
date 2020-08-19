using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace AvalonStudio.Terminals.Unix
{
    public class UnixPsuedoTerminalProvider : IPsuedoTerminalProvider
    {
        public IPsuedoTerminal Create(int columns, int rows, string initialDirectory, string environment, string command, params string[] arguments)
        {
            //StartWithDev(new string[0]);

            var fdm = Native.open("/dev/ptmx", Native.O_RDWR | Native.O_NOCTTY);

            var res = Native.grantpt(fdm);
            res = Native.unlockpt(fdm);

            var namePtr = Native.ptsname(fdm);
            var name = Marshal.PtrToStringAnsi(namePtr);
            var fds = Native.open(name, (int)Native.O_RDWR);

            var fileActions = Marshal.AllocHGlobal(1024);
            Native.posix_spawn_file_actions_init(fileActions);
            res = Native.posix_spawn_file_actions_adddup2(fileActions, (int)fds, 0);
            res = Native.posix_spawn_file_actions_adddup2(fileActions, (int)fds, 1);
            res = Native.posix_spawn_file_actions_adddup2(fileActions, (int)fds, 2);
            res = Native.posix_spawn_file_actions_addclose(fileActions, (int)fdm);
            res = Native.posix_spawn_file_actions_addclose(fileActions, (int)fds);

            var attributes = Marshal.AllocHGlobal(1024);
            res = Native.posix_spawnattr_init(attributes);

            int pid = Native.fork(); //Divided into two processes

            if(pid == 0) RunBash(initialDirectory, name);

            var stdin = Native.dup(fdm);
            var process = Process.GetProcessById((int)pid);
            return new UnixPsuedoTerminal(process, fds, stdin, new FileStream(new SafeFileHandle(new IntPtr(stdin), true), FileAccess.Write), new FileStream(new SafeFileHandle(new IntPtr(fdm), true), FileAccess.Read));
        }
         
        public static void RunBash(string path, string pt) {

            int slave = Native.open(pt, Native.O_RDWR);
 
            Native.setsid();
            Native.ioctl(slave, Native.TIOCSCTTY, IntPtr.Zero);
            Native.chdir(path);

            var envVars = new List<string>();
            var env = Environment.GetEnvironmentVariables();

            foreach (var variable in env.Keys) {
                if (variable.ToString() != "TERM") {
                    envVars.Add($"{variable}={env[variable]}");
                }
            }

            envVars.Add("TERM=xterm-256color");
            envVars.Add(null);

            Native.dup2(slave, 0);
            Native.dup2(slave, 1);
            Native.dup2(slave, 2);

            var argsArray = new List<string>();
            argsArray.Add("/bin/bash");
            argsArray.Add(null);

            Native.execve(argsArray[0], argsArray.ToArray(), envVars.ToArray());
        }        
    }
}
