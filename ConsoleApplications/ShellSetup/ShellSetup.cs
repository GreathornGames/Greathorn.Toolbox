using Greathorn.Core;
using Greathorn.Core.Loggers;
using Greathorn.Services.Perforce;

namespace Greathorn
{
	internal class ShellSetup
	{
        static void Main()
		{
            using ConsoleApplication framework = new(
            new Greathorn.Core.ConsoleApplicationSettings()
            {
                DefaultLogCategory = "SHELL",
                LogOutputs = [new Greathorn.Core.Loggers.ConsoleLogOutput()]
            });

			try
			{
                // Find our root
				string? workspaceRoot = PerforceUtil.GetWorkspaceRoot();
                if(workspaceRoot == null)
                {
                    Log.WriteLine("Unable to find workspace root.", ILogOutput.LogType.Error);
                    framework.Environment.UpdateExitCode(1, true);
                    return;
                }

                // Try to standardize our file/locations, etc.
                SettingsProvider settings = new SettingsProvider(workspaceRoot);              
			
                // Start a file log
                Log.AddLogOutput(new FileLogOutput(Path.Combine(settings.LogsFolder, "ShellSetup.log")));

                // General environment variables
                Environment.SetEnvironmentVariable("Workspace", settings.RootFolder);
				Environment.SetEnvironmentVariable("BatchFiles", settings.BuildBatchFilesFolder);
				Environment.SetEnvironmentVariable("GGTemp", settings.TempFile);

                // Setup some known UE related variables
				Environment.SetEnvironmentVariable("COMPUTERNAME", System.Environment.MachineName);

                // Add DotNET to path
                string? existingPath = Environment.GetEnvironmentVariable("PATH");
                if (existingPath == null || !existingPath.Contains(settings.DotNETExecutablesFolder))
                {
                    Environment.SetEnvironmentVariable("PATH", $"{existingPath};{settings.DotNETExecutablesFolder}");
                }

				// P4 Config
				if (File.Exists(settings.P4ConfigFile))
				{
					PerforceConfig config = new(settings.P4ConfigFile);
					Environment.SetEnvironmentVariable("P4CLIENT", config.Client);
					Environment.SetEnvironmentVariable("P4PORT", config.Port);
				}

					Log.WriteLine("Ready.");
			}
			catch(Exception ex)
			{
				framework.ExceptionHandler(ex);
			}
		}
	}
}
