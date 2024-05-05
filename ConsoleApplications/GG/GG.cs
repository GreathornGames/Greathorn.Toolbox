// Copyright Greathorn Games Inc. All Rights Reserved.

using System.Text.Json;
using System.Text.Json.Nodes;
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
                PauseOnExit = true
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
                settings.Output();

                // Find all command macros
                string ggProgramFolder = Path.Combine(settings.GreathornProgramsFolder, "GG");

                // Try to find the desired execution
                string[] programFolderCommands = Directory.GetFiles(ggProgramFolder, $"*{Commands.Extension}", SearchOption.TopDirectoryOnly);
                string[] projectFolderCommands = Directory.GetFiles(settings.ProjectsFolder, $"*{Commands.Extension}", SearchOption.AllDirectories);

                CommandMap map = new CommandMap();

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
                        string command = action.Command;

                        command = command.Replace("{ROOT}", settings.RootFolder);
                        string[] split = command.Split(' ', 1);
                        if (split.Length > 1)
                        {
                            ProcessUtil.Spawn(split[0], split[1]);
                        }
                        else
                        {
                            ProcessUtil.Spawn(command, null);
                        }
                    }
                    else
                    {
                        Log.WriteLine($"Unable to find valid command for query `{framework.Arguments.ToString()}`.", "GG", ILogOutput.LogType.Error);
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