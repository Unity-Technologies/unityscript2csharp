using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Assets.Editor
{
    internal class ProcessOutputStreamReader
    {
        private readonly Func<bool> hostProcessExited;
        private readonly StreamReader stream;
        internal List<string> lines;
        private Thread thread;

        internal ProcessOutputStreamReader(Process p, StreamReader stream) : this(() => p.HasExited, stream)
        {
        }

        internal ProcessOutputStreamReader(Func<bool> hostProcessExited, StreamReader stream)
        {
            this.hostProcessExited = hostProcessExited;
            this.stream = stream;
            lines = new List<string>();

            thread = new Thread(ThreadFunc);
            thread.Start();
        }

        private void ThreadFunc()
        {
            if (hostProcessExited()) return;
            while (true)
            {
                if (stream.BaseStream == null) return;

                var line = stream.ReadLine();
                if (line == null)
                    return;

                lock (lines)
                {
                    lines.Add(line);
                }
            }
        }

        internal string[] GetOutput()
        {
            if (hostProcessExited())
                thread.Join();

            lock (lines)
            {
                return lines.ToArray();
            }
        }
    }
}
