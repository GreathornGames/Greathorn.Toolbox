// Copyright Greathorn Games Inc. All Rights Reserved.

using System;
using System.Diagnostics;
using System.IO;
using Greathorn.Core.Modules;
using Greathorn.Core.Utils;

namespace Greathorn.Core
{
	public class ConsoleApplication : IDisposable
    {
		private const string k_LogCategory = "CORE";

        // Builtin Modules
        public readonly ArgumentsModule Arguments = new ArgumentsModule();
		public readonly AssemblyModule Assembly = new AssemblyModule();
		public readonly EnvironmentModule Environment = new EnvironmentModule();
		public readonly PlatformModule Platform = new PlatformModule();

        readonly Timer m_RuntimeTimer = new Timer();
        bool m_HasTerminated = false;
        readonly bool m_ShouldPause = false;
        readonly bool m_DisplayRuntime;

		public ConsoleApplication(ConsoleApplicationSettings settings)
		{
            // Immediately setup logging
            if (settings.DefaultLogCategory != null)
            {
                Log.DefaultCategory = settings.DefaultLogCategory;
            }
            Log.AddLogOutputs(settings.LogOutputs);

            
            Arguments.Init(Assembly);
            Assembly.Init(Arguments);
            Environment.Init(Platform);

            // Should we pause on leaving?
            m_ShouldPause = settings.PauseOnExit;
            if(Arguments.BaseArguments.Contains("no-pause") || Arguments.BaseArguments.Contains("quiet"))
            {
                m_ShouldPause = false;
            }
           
            if (settings.DisplayHeader)
            {
                Log.WriteLine($"Core Framework v{Assembly.CoreAssembly.GetName().Version}", ILogOutput.LogType.Notice);
            }
            m_DisplayRuntime = settings.DisplayRuntime;

            if (settings.RequiresElevatedAccess && !ProcessUtil.IsElevated())
            {
                string relaunchTarget = Assembly.EntryAssembly != null ? Assembly.EntryAssembly.Location : Assembly.ExecutingAssembly.Location;
                Log.WriteLine($"Elevation REQUIRED: {relaunchTarget}", k_LogCategory, ILogOutput.LogType.Error);
                if (Arguments.BaseArguments.Contains("elevation-check"))
                {
                    Log.WriteLine($"Elevation FAILED: {relaunchTarget}", k_LogCategory, ILogOutput.LogType.Error);
                }
                else
                {
                    Log.Shutdown(); // Need to unlock files
                    ProcessUtil.Elevate("dotnet", System.IO.Directory.GetCurrentDirectory(), $"{relaunchTarget} {Arguments} elevation-check", false);
                }
                m_ShouldPause = false;
                Shutdown();
            }
        }

		public void Shutdown(bool forced = false)
		{
            if (m_HasTerminated) return;
            m_HasTerminated = true;

            if (m_DisplayRuntime)
            {
                Log.WriteLine($"Runtime {m_RuntimeTimer.GetElapsedMilliseconds()}ms", ILogOutput.LogType.Info, k_LogCategory);
            }
            Log.Shutdown();

            // Set our last know code
            System.Environment.ExitCode = Environment.ExitCode;
            if(m_ShouldPause && !forced)
            {
                Console.WriteLine("Press Any Key To Continue ...");
                try
                {
                    Console.ReadKey();
                }
                catch(Exception)
                {
                    Console.WriteLine("Unable to capture keystroke. Skipping.");
                }
            }

            // Return to original directory of launch
            if (Environment.OriginalWorkingDirectory != null)
            {
                Directory.SetCurrentDirectory(Environment.OriginalWorkingDirectory);
            }

            System.Environment.Exit(Environment.ExitCode);
		}

		public void ExceptionHandler(Exception e)
		{
			Log.LineFeed();
			Log.WriteLine(e, "EXCEPTION", ILogOutput.LogType.Error);
			Log.LineFeed();

			// Update exit code
			Environment.UpdateExitCode(e.HResult);
		}

        public void Dispose()
        {
            Shutdown();
        }
    }
}
