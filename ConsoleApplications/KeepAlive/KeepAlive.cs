// Copyright Greathorn Games Inc. All Rights Reserved.

using System.Diagnostics;
using Greathorn.Core;
using KeepAlive;

namespace Greathorn
{
    internal class KeepAlive
    {
        static bool s_Alive = true;
        static DateTime s_LastHeartbeat;
        static ProcessMonitor? s_ProcessMonitor = null;
        static KeepAliveConfig? s_Settings;

        static void Main(string[] args)
        {
            using ConsoleApplication framework = new(
             new Greathorn.Core.ConsoleApplicationSettings()
             {
                 DefaultLogCategory = "KEEPALIVE",
                 LogOutputs = [new Greathorn.Core.Loggers.ConsoleLogOutput()],
                 PauseOnExit = false,
                 RequiresElevatedAccess = false,
             });

            try
            {
                // Only argument is the path to a settings file
                if(framework.Arguments.Arguments.Count  > 0)
                {
                    string arg = framework.Arguments.Arguments[0];
                    if(File.Exists(arg))
                    {
                        s_Settings = KeepAliveConfig.Get(arg);
                    }
                }

                // Ok no argument, use default
                if(s_Settings == null)
                {
                    s_Settings =  KeepAliveConfig.Get();
                }

                // Setup exit logic
                Log.WriteLine("Press CTRL+C to Exit");
                Console.CancelKeyPress += delegate (object? _, ConsoleCancelEventArgs e)
                {
                    e.Cancel = true;
                    s_Alive = false;
                };


                // Get existing running server, just in case its still there and this app failed?
                int pid = ProcessMonitor.GetPIDFromFile(s_Settings.ProcessInfoPath);
                if (pid != ProcessMonitor.BAD_PID && ProcessMonitor.IsValidPID(pid))
                {
                    s_ProcessMonitor = new ProcessMonitor(pid);
                    if (s_ProcessMonitor.IsValid())
                    {
                        Log.WriteLine("Found valid process from PID file.");
                    }
                    else
                    {
                        s_ProcessMonitor = null;
                    }
                }

                // Main loop
                while (s_Alive)
                {
                    if (s_ProcessMonitor == null)
                    {
                        StartMonitorProcess();
                    }
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    s_ProcessMonitor.Refresh();
#pragma warning restore CS8602 // Dereference of a possibly null reference.

                    if (!s_ProcessMonitor.IsValid())
                    {
                        Log.WriteLine($"Monitor has reported an issue, waiting {s_Settings.TimeoutSleepMilliseconds / 1000} seconds to see if it resolves it self. Last good heartbeat was at {s_LastHeartbeat.ToLongDateString()} on {s_LastHeartbeat.ToLongTimeString()}");
                        Thread.Sleep(s_Settings.TimeoutSleepMilliseconds);

                        if (!s_ProcessMonitor.IsValid())
                        {
                            Log.WriteLine("Restarting!");
                            KillMonitorProcess();
                            continue;
                        }
                    }
                    else
                    {
                        s_LastHeartbeat = DateTime.Now;
                    }

                    Thread.Sleep(s_Settings.SleepMilliseconds);
                }

                KillMonitorProcess();
            }
            catch (Exception ex)
            {
                framework.ExceptionHandler(ex);
            }

            framework.Shutdown(false);
        }
        static void StartMonitorProcess()
        {
            if (s_Settings == null) return;

            Process startProcess = new Process();

            startProcess.StartInfo.WorkingDirectory = s_Settings.WorkingDirectory;
            startProcess.StartInfo.FileName = s_Settings.Application;
            startProcess.StartInfo.Arguments = s_Settings.Arguments;
            startProcess.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
            startProcess.StartInfo.CreateNoWindow = false;
            startProcess.StartInfo.UseShellExecute = true;

            startProcess.Start();
            Thread.Sleep(s_Settings.StartSleepMilliseconds);

            s_ProcessMonitor = new ProcessMonitor(startProcess);

            Log.WriteLine($"Started with PID of {startProcess.Id.ToString()}");
            File.WriteAllText(s_Settings.ProcessInfoPath, startProcess.Id.ToString());
        }

        static void KillMonitorProcess()
        {
            if (s_ProcessMonitor == null) return;

            s_ProcessMonitor.Kill();
            s_ProcessMonitor = null;
        }

    }
}
