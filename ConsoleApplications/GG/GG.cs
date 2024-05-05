// Copyright Greathorn Games Inc. All Rights Reserved.

using System.Text.Json;
using System.Text.Json.Nodes;
using GG;
using Greathorn.Core;
using Greathorn.Core.Loggers;
using Greathorn.Services.Perforce;

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
                string[] programFolderCommands = Directory.GetFiles(ggProgramFolder, $"*{CommandsFile.Extension}", SearchOption.TopDirectoryOnly);
                string[] projectFolderCommands = Directory.GetFiles(settings.ProjectsFolder, $"*{CommandsFile.Extension}", SearchOption.AllDirectories);


                Dictionary<string, CommandsFile.Action> parsedActions = [];

                // Parse Programs
                int programFolderCommandsCount = programFolderCommands.Length;
                for(int i = 0; i < programFolderCommandsCount; i++)
                {
                    string json = File.ReadAllText(programFolderCommands[i]);
                    CommandsFile? c = JsonSerializer.Deserialize<CommandsFile>(json);
                    if(c == null)
                    {
                        Log.WriteLine($"Unable to parse {programFolderCommands[i]}.", "JSON", ILogOutput.LogType.Error);
                        continue;
                    }
                    else if(c.Actions == null || c.Actions.Length == 0)
                    {
                        Log.WriteLine($"No actions found in {projectFolderCommands[i]}.", "JSON", ILogOutput.LogType.Info);
                        continue;
                    }
                    CommandsFile.AddActions(c.Actions, parsedActions);
                }

                // Parse Project
                int projectFolderCommandsCount = projectFolderCommands.Length;
                for (int i = 0; i < projectFolderCommandsCount; i++)
                {
                    string json = File.ReadAllText(projectFolderCommands[i]);
                    CommandsFile? c = JsonSerializer.Deserialize<CommandsFile>(json);
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
                    CommandsFile.AddActions(c.Actions, parsedActions);
                }
            }
            catch (Exception ex)
            {
                framework.ExceptionHandler(ex);
            }
        }
    }
}