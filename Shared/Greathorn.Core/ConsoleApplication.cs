using System;
using System.Diagnostics;
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
        bool m_ShouldPause = false;

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
            if(Arguments.Arguments.Contains("no-pause"))
            {
                m_ShouldPause = false;
            }

            Log.WriteLine($"Core Framework v{Assembly.CoreAssembly.GetName().Version}", ILogOutput.LogType.Notice);

            if(settings.RequiresElevatedAccess && !ProcessUtil.IsElevated())
            {
                Log.WriteLine("Elevation REQUIRED", k_LogCategory, ILogOutput.LogType.Error);
                ProcessUtil.Elevate("dotnet", System.IO.Directory.GetCurrentDirectory(), $"{Assembly.ExecutingAssembly.Location} { Arguments}", false); ;
                m_ShouldPause = false;
                Shutdown();
            }
        }

		public void Shutdown()
		{
            if (m_HasTerminated) return;
            m_HasTerminated = true;

			Log.WriteLine($"Runtime {m_RuntimeTimer.GetElapsedMilliseconds()}ms", ILogOutput.LogType.Info, k_LogCategory);
            Log.Shutdown();

            // Set our last know code
            System.Environment.ExitCode = Environment.ExitCode;
            if(m_ShouldPause)
            {
                Console.WriteLine("Press Any Key To Continue ...");
                Console.ReadKey();
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
