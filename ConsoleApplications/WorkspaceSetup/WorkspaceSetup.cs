using Greathorn.Core;
using Greathorn.Core.Loggers;
using Greathorn.Core.Services.Git;
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
                // Find our root
                string? workspaceRoot = PerforceUtil.GetWorkspaceRoot();
                if (workspaceRoot == null)
                {
                    Log.WriteLine("Unable to find workspace root.", ILogOutput.LogType.Error);
                    framework.Environment.UpdateExitCode(1, true);
                    return;
                }

                // Try to standardize our file/locations, etc.
                SettingsProvider settings = new(workspaceRoot);


                Log.AddLogOutput(new FileLogOutput(settings.LogsFolder, "WorkspaceSetup"));
                settings.Output();

                UpdateSourceCode(framework, settings);
                BuildSource(framework, settings);
                SetupEnvironment(settings);
                SetupPerforce(settings);
                SetupVSCode(settings);
                SetupExecutionFlags(framework, settings);
                SetupSecurityExclusions(framework, settings);
                SetupUnrealEngine(framework, settings);

            }
			catch(Exception ex)
			{
				framework.ExceptionHandler(ex);
			}
        }

        #region Process
        static void UpdateSourceCode(ConsoleApplication framework, SettingsProvider settings)
        {
            if(framework.Arguments.BaseArguments.Contains("no-source"))
            {
                Log.WriteLine("Skipping Source Check (Argument) ...", "SOURCE", ILogOutput.LogType.Default);
                return;
            }
           
            string? branch = GitProvider.GetBranch(settings.GreathornToolboxFolder);
            branch ??= "main";

            string localCommitHash = GitProvider.GetLocalCommit(settings.GreathornToolboxFolder);
            string? remoteCommitHash = GitProvider.GetRemoteCommit(settings.GreathornToolboxFolder, branch);
         
            if(localCommitHash == remoteCommitHash)
            {
                Log.WriteLine($"Local repository up to date ({localCommitHash}).", "SOURCE", ILogOutput.LogType.Default);
                return;
            }
            else
            {
                Log.WriteLine($"Depot needs updating as the local {localCommitHash} differs from {remoteCommitHash}.", "SOURCE", ILogOutput.LogType.Info);
#if DEBUG
                Log.WriteLine("Skipping Cloning (Debug Mode) ...");
                return;
#else
                GitProvider.UpdateRepo(settings.GreathornToolboxFolder, branch);
                return;
#endif
            }
        }

        static void BuildSource(ConsoleApplication framework, SettingsProvider settings)
        {
            if (framework.Arguments.BaseArguments.Contains("no-build"))
            {
                Log.WriteLine("Skipping Build Check (Argument) ...", "BUILD", ILogOutput.LogType.Default);
                return;
            }

            string localCommitHash = GitProvider.GetLocalCommit(settings.GreathornToolboxFolder);
            string builtTagFile = Path.Combine(settings.GreathornToolboxFolder, SettingsProvider.BuildHashFileName);
            bool shouldRebuild = !File.Exists(builtTagFile);
            if (!shouldRebuild)
            {
                shouldRebuild = (File.ReadAllText(builtTagFile).Trim() != localCommitHash);
            }

            if (shouldRebuild)
            {
                Log.WriteLine($"A rebuild of programs is needed.", "BUILD", ILogOutput.LogType.Notice);
#if DEBUG
                Log.WriteLine("Skipping Building (Debug Mode) ...");
#else
                ProcessUtil.SpawnSeperate("dotnet", $"{settings.BoostrapLibrary} quiet", null, true);
                framework.Shutdown(true);
#endif
            }
        }

        static void SetupPerforce(SettingsProvider settings)
        {
            Log.WriteLine("Setup Perforce", ILogOutput.LogType.Notice);

            Log.WriteLine("Set P4 Flags ...", ILogOutput.LogType.Default);
            ProcessUtil.SpawnHidden(PerforceProvider.GetExecutablePath(), $"set P4IGNORE={SettingsProvider.P4IgnoreFileName} P4CONFIG={SettingsProvider.P4ConfigFileName} P4CHARSET={SettingsProvider.P4CharacterSet}");

            Log.WriteLine($"Configure P4Config ...", ILogOutput.LogType.Default);
            if (!File.Exists(settings.P4ConfigFile))
            {
                Log.WriteLine($"Writing default P4Config ...", ILogOutput.LogType.Default);
                PerforceConfig.WriteDefault(settings.P4ConfigFile, SettingsProvider.P4Port, SettingsProvider.P4CharacterSet, SettingsProvider.P4IgnoreFileName);
                Log.WriteLine($"Opening P4Config for edit.", ILogOutput.LogType.Default);
                ProcessUtil.OpenFileWithDefault(settings.P4ConfigFile);
            }
            else
            {
                // We're not going to overwrite, but maybe there is a todo here where we load it and validate?
                Log.WriteLine($"Existing P4Config was found.", ILogOutput.LogType.Default);
            }

            Log.WriteLine("Install P4V Tools ...", ILogOutput.LogType.Default);
            CustomTools.CustomToolDefList baseCustomTools = CustomTools.Get();

            // We need to find all the extra tools throughout the workspace
            List<string> p4tools =
            [
                .. Directory.GetFiles(settings.GreathornFolder, SettingsProvider.P4CustomToolsFileName, SearchOption.AllDirectories),
                .. Directory.GetFiles(settings.ProjectsFolder, SettingsProvider.P4CustomToolsFileName, SearchOption.AllDirectories),
            ];
            int foundTools = p4tools.Count;
            for(int i = 0; i < foundTools; i++)
            {
                string toolPath = p4tools[i];
                Log.WriteLine($"Adding {toolPath} ...");
                CustomTools.CustomToolDefList customTools = CustomTools.Get(toolPath);
                baseCustomTools.AddOrReplace(customTools);
            }
            if(foundTools > 0)
            {
                baseCustomTools.Output(CustomTools.ConfigFile);
            }

        }
        static void SetupEnvironment(SettingsProvider settings)
        {
            bool restartShellsRequired = false;
            Log.WriteLine("Setup Environment", ILogOutput.LogType.Notice);

            string? existingMachinePath = System.Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine);
            if(existingMachinePath != null && !existingMachinePath.Contains(settings.GreathornDotNETFolder))
            {
                Log.WriteLine($"Adding to PATH variable ...", ILogOutput.LogType.Default);
                System.Environment.SetEnvironmentVariable("PATH", $"{existingMachinePath};{settings.GreathornDotNETFolder};", EnvironmentVariableTarget.Machine);
                restartShellsRequired = true;
            }

            if(restartShellsRequired)
            {
                Log.WriteLine("Restarting of terminals required to pickup new environment variables.", ILogOutput.LogType.Info);
            }
        }
        static void SetupVSCode(SettingsProvider settings)
        {
            Log.WriteLine("Setup VSCode", ILogOutput.LogType.Notice);
            string vscodePath = Path.Combine(settings.RootFolder, ".vscode", "settings.json");
            if (!File.Exists(vscodePath))
            {
                FileUtil.EnsureFileFolderHierarchyExists(vscodePath);
                string ggPath = Path.Combine(settings.GreathornDotNETFolder, "GG.exe").Replace("\\", "\\\\");
                File.WriteAllLines(vscodePath, [
                    "{",
                        $"\t\"terminal.integrated.cwd\": \"{settings.GreathornDotNETFolder.Replace("\\", "\\\\")}\",",
                        "\t\"terminal.integrated.profiles.windows\": {",
                            "\t\t\"Command Prompt\": {",
                                $"\t\t\t\"args\": [\"/K\", \"{ggPath}\"]",
                            "\t\t},",
                            "\t\t\"PowerShell\": {",
                                $"\t\t\t\"args\": [\"-NoExit\", \"{ggPath}\"]",
                            "\t\t}",
                        "\t}",
                    "}"
                ]);
            }
        }
        static void SetupExecutionFlags(ConsoleApplication framework, SettingsProvider settings)
        {
            Log.WriteLine("Ensuring Execution Flags", ILogOutput.LogType.Notice);
            // Ensure executable flags are setup across the workspace
            switch (framework.Platform.OperatingSystem)
            {
                case Core.Modules.PlatformModule.PlatformType.macOS:
                case Core.Modules.PlatformModule.PlatformType.Linux:
                    string[] shFiles = Directory.GetFiles(settings.RootFolder, "*.sh", SearchOption.AllDirectories);
                    string[] commandFiles = Directory.GetFiles(settings.RootFolder, "*.command", SearchOption.AllDirectories);

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
        }
        static void SetupSecurityExclusions(ConsoleApplication framework, SettingsProvider settings)
        {
            Log.WriteLine("Adding Security Exclusions", ILogOutput.LogType.Notice);
            switch (framework.Platform.OperatingSystem)
            {
                case Greathorn.Core.Modules.PlatformModule.PlatformType.Windows:
                    ProcessUtil.Elevate("powershell", settings.RootFolder,
                        $"-inputformat none -outputformat none -NonInteractive -Command Add-MpPreference -ExclusionPath \"{settings.RootFolder}\"");
                    break;
            } 
        }

        static void SetupUnrealEngine(ConsoleApplication framework, SettingsProvider settings)
        {
            Log.WriteLine("Setup Unreal Engine", ILogOutput.LogType.Notice);
            switch (framework.Platform.OperatingSystem)
            {
                case Greathorn.Core.Modules.PlatformModule.PlatformType.Windows:
                    string prereqExecutable = Path.Combine(settings.RootFolder, "Engine", "Extras", "Redist", "en-us", "UEPrereqSetup_x64.exe");
                    Log.WriteLine($"Running {prereqExecutable} ...", ILogOutput.LogType.Default);
                    ProcessUtil.SpawnHidden(prereqExecutable, "/quiet /norestart");


                    string versionSelector = Path.Combine(settings.RootFolder, "Engine", "Binaries", "Win64", "UnrealVersionSelector-Win64-Shipping.exe");
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
#endregion

        //static bool Symlink(string source, string target, bool deleteInPlace = true)
        //{
        //    // Do we want to delete the in-place file because it could have been a copy instead of a symlink
        //    if (deleteInPlace)
        //    {
        //        FileUtil.ForceDeleteFile(target);
        //    }

        //    if(File.Exists(target) && !deleteInPlace)
        //    {
        //        Log.WriteLine($"Unable to symlink {source}->{target} as a file already exists at that location.", "SYMLINK", ILogOutput.LogType.Error);
        //        return false;
        //    }
        //    else
        //    {
        //        Log.WriteLine($"Symlink {source}->{target} ...", "SYMLINK", ILogOutput.LogType.Default);
        //        try
        //        {
        //            File.CreateSymbolicLink(target, source);
        //            Log.WriteLine($"Created.", "SYMLINK", ILogOutput.LogType.Default);
        //        }
        //        catch (IOException)
        //        {
        //            Log.WriteLine("An exception occurred, falling back to simply copying the file.", "SYMLINK", ILogOutput.LogType.Info);
        //            File.Copy(source, target);
        //        }
        //        return true;
        //    }
        //}
    }
}
