using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace AvalonStudio.Terminals.Unix
{
    public class UnixPsuedoTerminal : IPsuedoTerminal
    {
        private int _handle;
        private int _cfg;
        private Stream _stdin = null;
        private Stream _stdout = null;
        private Process _process;
        private bool _isDisposed = false;

        public UnixPsuedoTerminal(Process process, int handle, int cfg, Stream stdin, Stream stdout)
        {
            _process = process;

            _handle = handle;
            _stdin = stdin;
            _stdout = stdout;

            _cfg = cfg;
        }

        public static void Trampoline(string[] args)
        {
            if (args.Length > 2 && args[0] == "--trampoline")
            {
                Native.setsid();
                Native.ioctl(0, Native.TIOCSCTTY, IntPtr.Zero);
                Native.chdir(args[1]);

                var envVars = new List<string>();
                var env = Environment.GetEnvironmentVariables();

                foreach (var variable in env.Keys)
                {
                    if (variable.ToString() != "TERM")
                    {
                        envVars.Add($"{variable}={env[variable]}");
                    }
                }

                envVars.Add("TERM=xterm-256color");
                envVars.Add(null);

                var argsArray = args.Skip(3).ToList();
                argsArray.Add(null);

                Native.execve(args[2], argsArray.ToArray(), envVars.ToArray());
            }
            else
            {
                return;
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                _stdin?.Dispose();
                _stdout?.Dispose();

                // TODO close file descriptors and terminate processes?
            }
        }

        public async Task<int> ReadAsync(byte[] buffer, int offset, int count)
        {
            return await _stdout.ReadAsync(buffer, offset, count);
        }

        public async Task WriteAsync(byte[] buffer, int offset, int count)
        {
            if (buffer.Length == 1 && buffer[0] == 10)
            {
                buffer[0] = 13;
            }

            await Task.Run(() =>
            {
                var buf = Marshal.AllocHGlobal(count);
                Marshal.Copy(buffer, offset, buf, count);
                Native.write(_cfg, buf, count);

                Marshal.FreeHGlobal(buf);
            });
        }

        public void SetSize(int columns, int rows)
        {
            Native.winsize size = new Native.winsize();
            int ret;
            size.ws_row = (ushort)(rows > 0 ? rows : 24);
            size.ws_col = (ushort)(columns > 0 ? columns : 80);

            var ptr = Native.StructToPtr(size);

            ret = Native.ioctl(_cfg, Native.TIOCSWINSZ, ptr);

            Marshal.FreeHGlobal(ptr);

            var error = Marshal.GetLastWin32Error();
        }

        public struct winsize
        {
            public ushort ws_row;   /* rows, in characters */
            public ushort ws_col;   /* columns, in characters */
            public ushort ws_xpixel;    /* horizontal size, pixels */
            public ushort ws_ypixel;    /* vertical size, pixels */
        };

        public Process Process => _process;
    }
}
