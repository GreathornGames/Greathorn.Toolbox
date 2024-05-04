using Greathorn.Core;
using Greathorn.Core.Loggers;
using Greathorn.Core.Utils;
using Greathorn.Services.Perforce;

namespace Greathorn
{
	internal class WorkspaceSetup
	{
        static void Main()
        {
            using ConsoleApplication framework = new(
            new Greathorn.Core.ConsoleApplicationSettings()
            {
                DefaultLogCategory = "WORKSPACE",
                LogOutputs = [new Greathorn.Core.Loggers.ConsoleLogOutput()],
                PauseOnExit = true,
                RequiresElevatedAccess = true,
            });

			try
			{
				string? workspaceRoot = PerforceUtil.GetWorkspaceRoot();
				if (workspaceRoot == null)
				{
					Log.WriteLine("Unable to find workspace root.", ILogOutput.LogType.Error);
					framework.Environment.UpdateExitCode(-1, true);
					return;
				}
				else
				{
                    // Start a file log
                    Log.AddLogOutput(new FileLogOutput(Path.Combine(workspaceRoot, "Logs", "WorkspaceSetup.log")));
                    Log.WriteLine($"Workspace Root: {workspaceRoot}", ILogOutput.LogType.Info);
				}



				// Setup Perforce
				Log.WriteLine("Setup Perforce", ILogOutput.LogType.Notice);

				Log.WriteLine("Set P4 Flags ...", ILogOutput.LogType.Default);
				ProcessUtil.SpawnHidden(PerforceProvider.GetExecutablePath(), $"set P4IGNORE={Greathorn.Services.Perforce.PerforceConfig.P4Ignore} P4CONFIG={PerforceConfig.FileName} P4CHARSET={Greathorn.Services.Perforce.PerforceConfig.CharacterSet}");

				Log.WriteLine($"Configure {PerforceConfig.FileName} ...", ILogOutput.LogType.Default);
				string configPath = Path.Combine(workspaceRoot, PerforceConfig.FileName);
				if (!File.Exists(configPath))
				{
					Log.WriteLine($"Writing default {PerforceConfig.FileName} ...", ILogOutput.LogType.Default);
					PerforceConfig.WriteDefault(configPath);
					Log.WriteLine($"Opening {PerforceConfig.FileName} for edit.", ILogOutput.LogType.Default);
					ProcessUtil.OpenFileWithDefault(configPath);
				}
				else
				{
					// We're not going to overwrite, but maybe there is a todo here where we load it and validate?
					Log.WriteLine($"Existing {PerforceConfig.FileName} was found.", ILogOutput.LogType.Default);
				}

				// Setup VSCode
				Log.WriteLine("Setup VSCode", ILogOutput.LogType.Notice);
				string vscodePath = Path.Combine(workspaceRoot, ".vscode", "settings.json");
				if (!File.Exists(vscodePath))
				{
					FileUtil.EnsureFileFolderHierarchyExists(vscodePath);

					string shellSetup = Path.Combine(workspaceRoot, "Greathorn", "Binaries", "DotNET", "ShellSetup.exe").Replace("\\", "\\\\");
					File.WriteAllLines(vscodePath, [
						"{",
						"\t\"terminal.integrated.profiles.windows\": {",
							"\t\t\"Command Prompt\": {",
								$"\t\t\t\"args\": [\"/K\", \"{shellSetup}\"]",
							"\t\t},",
							"\t\t\"PowerShell\": {",
								$"\t\t\t\"args\": [\"-NoExit\", \"{shellSetup}\", \"1\"]",
							"\t\t},",
						"\t}",
					"}"
					]);
				}

				// Ensure executable flags are setup across the workspace
				switch (framework.Platform.OperatingSystem)
				{
					case Core.Modules.PlatformModule.PlatformType.macOS:
					case Core.Modules.PlatformModule.PlatformType.Linux:
						string[] shFiles = Directory.GetFiles(workspaceRoot, "*.sh", SearchOption.AllDirectories);
						string[] commandFiles = Directory.GetFiles(workspaceRoot, "*.command", SearchOption.AllDirectories);

						foreach (string s in shFiles)
						{
							ProcessUtil.SpawnHidden("chmod", $"+x {s}");
						}
						foreach (string c in commandFiles)
						{
							ProcessUtil.SpawnHidden("chmod", $"+x {c}");
						}
						break;
				}

				Log.WriteLine("Setup Unreal Engine", ILogOutput.LogType.Notice);
				switch (framework.Platform.OperatingSystem)
				{
					case Greathorn.Core.Modules.PlatformModule.PlatformType.Windows:
						string prereqExecutable = Path.Combine(workspaceRoot, "Engine", "Extras", "Redist", "en-us", "UEPrereqSetup_x64.exe");
						Log.WriteLine($"Running {prereqExecutable} ...", ILogOutput.LogType.Default);
						ProcessUtil.SpawnHidden(prereqExecutable, "/quiet /norestart");


						string versionSelector = Path.Combine(workspaceRoot, "Engine", "Binaries", "Win64", "UnrealVersionSelector-Win64-Shipping.exe");
						if (File.Exists(versionSelector))
						{
							Log.WriteLine($"Running {versionSelector} ...", ILogOutput.LogType.Default);
							ProcessUtil.SpawnHidden(versionSelector, "/register");
						}
						break;
					case Greathorn.Core.Modules.PlatformModule.PlatformType.macOS:
						// TODO: Implement macOS requirements
						break;
					case Greathorn.Core.Modules.PlatformModule.PlatformType.Linux:
						// TODO: Implement Linux requirements
						break;
				}
			}
			catch(Exception ex)
			{
				framework.ExceptionHandler(ex);
			}
        }

        static bool Symlink(string source, string target, bool deleteInPlace = true)
        {
            // Do we want to delete the in-place file because it could have been a copy instead of a symlink
            if (deleteInPlace)
            {
                FileUtil.ForceDeleteFile(target);
            }

            if(File.Exists(target) && !deleteInPlace)
            {
                Log.WriteLine($"Unable to symlink {source}->{target} as a file already exists at that location.", "SYMLINK", ILogOutput.LogType.Error);
                return false;
            }
            else
            {
                Log.WriteLine($"Symlink {source}->{target} ...", "SYMLINK", ILogOutput.LogType.Default);
                try
                {
                    File.CreateSymbolicLink(target, source);
                    Log.WriteLine($"Created.", "SYMLINK", ILogOutput.LogType.Default);
                }
                catch (IOException)
                {
                    Log.WriteLine("An exception occurred, falling back to simply copying the file.", "SYMLINK", ILogOutput.LogType.Info);
                    File.Copy(source, target);
                }
                return true;
            }
        }
    }
}
