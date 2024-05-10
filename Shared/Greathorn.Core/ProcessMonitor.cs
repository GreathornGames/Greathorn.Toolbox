// Copyright Greathorn Games Inc. All Rights Reserved.

using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Greathorn.Core
{
    public class ProcessMonitor
    {
        public bool CheckResponding { get; set; }
        public bool CheckHasExited {  get; set; }

        public const int BAD_PID = -1;

        private readonly Process m_Process;
        public ProcessMonitor(int pid)
        {
            m_Process = Process.GetProcessById(pid);
        }

        public ProcessMonitor(Process process)
        {
            m_Process = process;
        }

        public bool IsValid()
        {
            if (m_Process == null) return false;

            if (CheckResponding && !m_Process.Responding)
            {
                return false;
            }

            if (CheckHasExited && m_Process.HasExited)
            {
                return false;
            }

            return true;
        }

        public void Refresh()
        {
            if (m_Process == null) return;
            m_Process.Refresh();
        }

        public void Kill()
        {
            m_Process.Kill();
            Thread.Sleep(500);
        }

        public static int GetPIDFromFile(string path)
        {
            Log.WriteLine($"Attempt to get PID from {path} ...");
            int pid = BAD_PID;

            // Look for ProcessInfoPath
            if (File.Exists(path) && int.TryParse(File.ReadAllText(path).Trim(), out pid))
            {
                Log.WriteLine($"PID found: {pid}");
            }
            else
            {
                Log.WriteLine("PID not found.");
            }
            return pid;
        }

        public static bool IsValidPID(int pid)
        {
            try
            {
                Process.GetProcessById(pid);
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}