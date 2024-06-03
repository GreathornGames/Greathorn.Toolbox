using Greathorn.Core;
using Greathorn.Core.Loggers;
using Greathorn.Core.Utils;
using Greathorn.Services.Perforce;

namespace Greathorn
{
    internal class CleanSlate
    {
        static int s_ProcessCount = 0;

        static void Main()
        {
            using ConsoleApplication framework = new(
            new Greathorn.Core.ConsoleApplicationSettings()
            {
                DefaultLogCategory = "CLEANSLATE",
                LogOutputs = [new Greathorn.Core.Loggers.ConsoleLogOutput()]
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

                Log.AddLogOutput(new FileLogOutput(settings.LogsFolder, "CleanSlate"));
                settings.Output();

                ClearEngine(framework, settings);
                ClearProjectPlugins(framework, settings);
                ClearProject(framework, settings);

                if (s_ProcessCount == 0)
                {
                    Log.WriteLine("No valid commands found in arguments", ILogOutput.LogType.Warning);
                    Log.WriteLine("Valid arguments include 'engine', 'project' and 'project-plugins'", ILogOutput.LogType.Info);                    
                }
            }
            catch (Exception ex)
            {
                framework.ExceptionHandler(ex);
            }
        }

        static void ClearProject(ConsoleApplication framework, SettingsProvider settingsProvider)
        {
            if (!framework.Arguments.BaseArguments.Contains("project"))
            {
                return;
            }
            s_ProcessCount++;
            Log.WriteLine("Clearing Project Artifacts ...", ILogOutput.LogType.Default);
            string[] projectDirectories = Directory.GetDirectories(settingsProvider.ProjectsFolder);
            int projectCount = projectDirectories.Length;
            for (int i = 0; i < projectCount; i++)
            {
                string intermediateFolder = Path.Combine(projectDirectories[i], "Intermediate");
                if (Path.Exists(intermediateFolder))
                {
                    Log.WriteLine($"Removing {intermediateFolder} ...", ILogOutput.LogType.Default);
                    Directory.Delete(intermediateFolder, true);
                }
                string binariesFolder = Path.Combine(projectDirectories[i], "Binaries");
                if (Path.Exists(binariesFolder))
                {
                    Log.WriteLine($"Removing {binariesFolder} ...", ILogOutput.LogType.Default);
                    Directory.Delete(binariesFolder, true);
                }
            }
        }
        static void ClearProjectPlugins(ConsoleApplication framework, SettingsProvider settingsProvider)
        {
            if (!framework.Arguments.BaseArguments.Contains("project-plugins"))
            {
                return;
            }
            s_ProcessCount++;
            Log.WriteLine("Clearing Project Plugin Artifacts ...", ILogOutput.LogType.Default);
            string[] projectDirectories = Directory.GetDirectories(settingsProvider.ProjectsFolder);
            int projectCount = projectDirectories.Length;
            Log.WriteLine($"Found  {projectCount} Projects.", ILogOutput.LogType.Default);
            for (int i = 0; i < projectCount; i++)
            {
                string projectDirectory = projectDirectories[i];
                string pluginBaseDirectory = Path.Combine(projectDirectory, "Plugins");
                if (Path.Exists(pluginBaseDirectory))
                {
                    string[] pluginDefinitions = Directory.GetFiles(pluginBaseDirectory, "*.uplugin", SearchOption.AllDirectories);
                    int pluginDefinitionsCount = pluginDefinitions.Length;
                    Log.WriteLine($"Found  {pluginDefinitionsCount} plugins in {pluginBaseDirectory}.", ILogOutput.LogType.Default);
                    for (int j = 0; j < pluginDefinitionsCount; j++)
                    {
                        string? pluginFolder = Path.GetDirectoryName(pluginDefinitions[j]);
                        if (pluginFolder != null)
                        {
                            Log.WriteLine($"Evaluating {pluginFolder} ...", ILogOutput.LogType.Default);
                            string intermediateFolder = Path.Combine(pluginFolder, "Intermediate");
                            if (Path.Exists(intermediateFolder))
                            {
                                Log.WriteLine($"Removing {intermediateFolder} ...", ILogOutput.LogType.Default);
                                Directory.Delete(intermediateFolder, true);
                            }
                            string binariesFolder = Path.Combine(pluginFolder, "Binaries");
                            if (Path.Exists(binariesFolder))
                            {
                                Log.WriteLine($"Removing {binariesFolder} ...", ILogOutput.LogType.Default);
                                Directory.Delete(binariesFolder, true);
                            }
                        }
                    }
                }
            }
        }


        static void ClearEngine(ConsoleApplication framework, SettingsProvider provider)
        {
            if (!framework.Arguments.BaseArguments.Contains("engine"))
            {
                return;
            }

            // Is UE5.sln present? build clean
            if(File.Exists(provider.SolutionFile))
            {
                //ProcessUtil.Execute
            }
        }
    }
}