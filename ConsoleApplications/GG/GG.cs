// Copyright Greathorn Games Inc. All Rights Reserved.

using GG;
using Greathorn.Core;
using Greathorn.Core.Loggers;
using Greathorn.Core.Utils;
using Greathorn.Services.Perforce;
using static GG.CommandMap;

namespace Greathorn
{
    internal class GG
    {
        static void Main()
        {
            using ConsoleApplication framework = new(
            new Greathorn.Core.ConsoleApplicationSettings()
            {
                DefaultLogCategory = "GG",
                LogOutputs = [new Greathorn.Core.Loggers.ConsoleLogOutput()],
                PauseOnExit = false,
                DisplayHeader = false,
                DisplayRuntime = false
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

                Log.AddLogOutput(new FileLogOutput(Path.Combine(settings.LogsFolder, "GG.log")));

                // Find all command macros
                string ggProgramFolder = Path.Combine(settings.GreathornProgramsFolder, "GG");

                // Try to find the desired execution
                string[] programFolderCommands = Directory.GetFiles(ggProgramFolder, $"*{Commands.Extension}", SearchOption.TopDirectoryOnly);
                string[] projectFolderCommands = Directory.GetFiles(settings.ProjectsFolder, $"*{Commands.Extension}", SearchOption.AllDirectories);

                CommandMap map = new();

                // Parse Programs
                int programFolderCommandsCount = programFolderCommands.Length;
                for(int i = 0; i < programFolderCommandsCount; i++)
                {
                    Commands? c = Commands.Get(programFolderCommands[i]);
                    if(c == null)
                    {
                        Log.WriteLine($"Unable to parse {programFolderCommands[i]}.", "JSON", ILogOutput.LogType.Error);
                        continue;
                    }
                    else if(c.Actions == null || c.Actions.Length == 0)
                    {
                        Log.WriteLine($"No actions found in {programFolderCommands[i]}.", "JSON", ILogOutput.LogType.Info);
                        continue;
                    }

                    map.AddCommands(c);
                }

                // Parse Project
                int projectFolderCommandsCount = projectFolderCommands.Length;
                for (int i = 0; i < projectFolderCommandsCount; i++)
                {
                    Commands? c = Commands.Get(projectFolderCommands[i]);
                    if (c == null)
                    {
                        Log.WriteLine($"Unable to parse {projectFolderCommands[i]}.", "JSON", ILogOutput.LogType.Error);
                        continue;
                    }
                    else if(c.Actions == null || c.Actions.Length == 0)
                    {
                        Log.WriteLine($"No actions found in {projectFolderCommands[i]}.", "JSON", ILogOutput.LogType.Info);
                        continue;
                    }

                    map.AddCommands(c);
                }

                if(!map.HasCommands())
                {
                    Log.WriteLine($"No actions found.", "JSON", ILogOutput.LogType.Info);
                    return;
                }

                if (framework.Arguments.Arguments.Contains("help") || framework.Arguments.Arguments.Count == 0)
                {
                    Log.WriteLine(map.GetOutput(), "GG", ILogOutput.LogType.Info);
                }
                else
                {
                    CommandMapAction? action = map.GetAction(framework.Arguments.ToString());
                    if (action != null && action.Command != null)
                    {
                        string? arguments = action.Arguments;
                        if (arguments != null)
                        {
                            arguments = arguments.Replace("{ROOT}", settings.RootFolder);
                        }

                        string? workingDirectory = action.WorkingDirectory;
                        if(workingDirectory != null)
                        {
                            workingDirectory = workingDirectory.Replace("{ROOT}", settings.RootFolder);
                        }


                        // We cant actually just run batch files they have to be ran from a command prompt
                        string? command = action.Command.Replace("{ROOT}", settings.RootFolder);
                        if(action.Command.EndsWith(".bat"))
                        {
                            arguments = $"/K {command} {arguments}";
                            command = "cmd.exe";
                        }

                        // GG will exit immediately following this 'start'.
                        ProcessUtil.SpawnWithEnvironment(command, arguments, workingDirectory, GetEnvironmentVariables(settings));
                    }
                    else
                    {
                        Log.WriteLine($"Unable to find valid command for query `{framework.Arguments}`.", "GG", ILogOutput.LogType.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                framework.ExceptionHandler(ex);
            }
        }

        static Dictionary<string, string> GetEnvironmentVariables(SettingsProvider settings)
        {
            Dictionary<string, string> returnData = new()
            {
                // Universal flag that this was launched from GG
                ["GG"] = "1",

                ["DOTNET_CLI_TELEMETRY_OPTOUT"] = "true",

                // Our own few things
                ["Workspace"] = settings.RootFolder,
                ["BatchFiles"] = settings.BuildBatchFilesFolder,
                ["GGTemp"] = settings.TempFile,

                // Some things UE uses
                ["COMPUTERNAME"] = System.Environment.MachineName             
            };

            // P4 Config
            if (File.Exists(settings.P4ConfigFile))
            {
                PerforceConfig config = new(settings.P4ConfigFile);
                returnData["P4CLIENT"] = config.Client;
                returnData["P4PORT"] = config.Port;
            }

            return returnData;
        }
    }
}