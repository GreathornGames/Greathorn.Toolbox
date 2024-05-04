using Greathorn.Core;
using Greathorn.Core.Loggers;
using Greathorn.Core.Utils;
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
				string? workspaceRoot = PerforceUtil.GetWorkspaceRoot();
                
				if (workspaceRoot != null)
				{
                    // Start a file log
                    Log.AddLogOutput(new FileLogOutput(Path.Combine(workspaceRoot, "Logs", "ShellSetup.log")));

                    // General Environment
                    Environment.SetEnvironmentVariable("Workspace", workspaceRoot);
					Environment.SetEnvironmentVariable("BatchFiles", Path.Combine(workspaceRoot, "Engine", "Build", "BatchFiles"));
					Environment.SetEnvironmentVariable("GGTemp", Path.Combine(workspaceRoot, "gg.tmp"));
					Environment.SetEnvironmentVariable("COMPUTERNAME", System.Environment.MachineName);


                    // Add DotNET to path
                    string? existingPath = Environment.GetEnvironmentVariable("PATH");
                    string programs = Path.Combine(workspaceRoot, "Greathorn", "Binaries", "DotNET");
                    if (existingPath == null || !existingPath.Contains(programs))
                    {
                        Environment.SetEnvironmentVariable("PATH", $"{existingPath};{programs}");
                    }

					// P4 Config
					string p4Config = Path.Combine(workspaceRoot, PerforceConfig.FileName);
					if (File.Exists(p4Config))
					{
						PerforceConfig config = new(p4Config);
						Environment.SetEnvironmentVariable("P4CLIENT", config.Client);
						Environment.SetEnvironmentVariable("P4PORT", config.Port);
					}

					Log.WriteLine("Ready.");
				}
				else
				{
					Log.WriteLine("Unable to find workspace.", ILogOutput.LogType.Error);
					framework.Environment.UpdateExitCode(1, true);
				}
			}
			catch(Exception ex)
			{
				framework.ExceptionHandler(ex);
			}
		}
	}
}
