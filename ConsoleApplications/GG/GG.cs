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

                        ProcessUtil.SpawnSeperate(command, arguments, workingDirectory);
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
    }
}