// Copyright Greathorn Games Inc. All Rights Reserved.

using Greathorn.Core;
using Greathorn.Core.Loggers;
using Greathorn.Core.Utils;
using Greathorn.Services.Perforce;

namespace Greathorn
{
    internal class UnrealTypes
    {
        public enum FileType
        {
            Text,
            Binary,
            UTF8,
            UTF16,
            Symlink
        }

        public static string GetPerforceType(FileType type)
        {
            switch (type)
            {
                case FileType.UTF8:
                    return "utf8";
                case FileType.UTF16:
                    return "utf16";
                case FileType.Text:
                    return "text";
                case FileType.Symlink:
                    return "symlink";
            }
            return "binary";
        }


        public struct WorkUnit
        {
            public FileType Type;
            public string Path;
            public WorkUnit(FileType type, string path)
            {
                Type = type;
                Path = path;
            }

        }

        static void Main()
        {
            using ConsoleApplication framework = new(
            new Greathorn.Core.ConsoleApplicationSettings()
            {
                DefaultLogCategory = "UNREALTYPES",
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

                if (!framework.Arguments.OverrideArguments.ContainsKey("changelist"))
                {
                    Log.WriteLine("A changelist must be defined.", ILogOutput.LogType.Error);
                    framework.Environment.UpdateExitCode(1, true);
                    return;
                }
                string changelist = framework.Arguments.OverrideArguments["changelist"];

                // Try to standardize our file/locations, etc.
                SettingsProvider settings = new(workspaceRoot);

                Log.AddLogOutput(new FileLogOutput(settings.LogsFolder, "UnrealTypes"));
                settings.Output();

                string rootDirectory = workspaceRoot;
                if (framework.Arguments.OverrideArguments.ContainsKey("directory"))
                {
                    rootDirectory = framework.Arguments.OverrideArguments["directory"];
                }

                WorkUnit[] workUnits = FindUntypedFiles(rootDirectory);
                UpdateFileTypes(workspaceRoot, workUnits, changelist);
            }
            catch (Exception ex)
            {
                framework.ExceptionHandler(ex);
            }
        }


        static WorkUnit[] FindUntypedFiles(string rootDirectory)
        {
            Log.SetThreadSafeMode();
            System.Collections.Concurrent.ConcurrentBag<WorkUnit> workUnits = new System.Collections.Concurrent.ConcurrentBag<WorkUnit>();
            _ = Parallel.ForEach(Directory.EnumerateFiles(rootDirectory, "*.*", SearchOption.AllDirectories), path =>
            {
                byte[] bom = new byte[4];
                try
                {
                    using FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                    file.Read(bom, 0, 4);

                    if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf)
                    {
                        workUnits.Add(new WorkUnit(FileType.UTF8, path));
                    }

                    if ((bom[0] == 0xff && bom[1] == 0xfe) || (bom[0] == 0xfe && bom[1] == 0xff))
                    {
                        workUnits.Add(new WorkUnit(FileType.UTF16, path));
                    }
                }
                catch (Exception)
                {
                    Log.WriteLine($"Skipping {path} ...");
                }
            });

            Log.ClearThreadSafeMode();
            return workUnits.ToArray();
        }


        static void UpdateFileTypes(string workspaceRoot, WorkUnit[] files, string changelist)
        {
            // Turn on explicit thread safety for logging
            Log.SetThreadSafeMode();

            int count = files.Length;
            Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = 32 }, file =>
            {
                WorkUnit currentUnit = file;
                string currentPath = $"\"{currentUnit.Path}\"";
                string currentP4Type = GetPerforceType(currentUnit.Type);

                // Get current type                
                string processResponse = string.Empty;
                ProcessUtil.Execute("p4", workspaceRoot, $"files {currentPath}", null, (processIdentifier, line) =>
                {
                    processResponse = $"{processResponse}\n{line}";
                });
                processResponse = processResponse.Trim();


                // Lets check its fstat
                processResponse = string.Empty;
                ProcessUtil.Execute("p4", workspaceRoot, $"fstat {currentPath}", null, (processIdentifier, line) =>
                {
                    if (line.StartsWith("... type"))
                    {
                        processResponse = line.Replace("... type", string.Empty).Trim();
                    }
                });
                if (processResponse != string.Empty)
                {
                    if (processResponse == currentP4Type)
                    {
                        return;
                    }

                }

                // Check if the file is under the clients root
                if (processResponse.Contains("is not under client's root"))
                {
                    Log.WriteLine($"{currentUnit.Path} - is not under client's root.", ILogOutput.LogType.Error);
                    return;
                }

                // The file is new to perforce
                if (processResponse.EndsWith("no such file(s)."))
                {
                    processResponse = string.Empty;
                    ProcessUtil.Execute("p4", workspaceRoot, $"add -t {currentP4Type} -c {changelist} {currentPath}", null, (processIdentifier, line) =>
                    {
                        processResponse = $"{processResponse}\n{line}";
                    });
                    processResponse = processResponse.Trim();

                    if (processResponse.EndsWith("use 'reopen'"))
                    {
                        ReopenFile(workspaceRoot, currentP4Type, changelist, currentPath, currentUnit.Path);
                    }
                    else
                    {
                        Log.WriteLine($"Changed type on ADD ({currentP4Type}) of {currentUnit.Path}");
                    }
                }
                else
                {
                    ReopenFile(workspaceRoot, currentP4Type, changelist, currentPath, currentUnit.Path);
                }
            });

            // Add log items to unsafe items
            Log.ClearThreadSafeMode();
        }

        static void ReopenFile(string workspaceRoot, string currentP4Type, string changelist, string currentPath, string rawPath)
        {
            string processResponse = string.Empty;
            ProcessUtil.Execute("p4", workspaceRoot, $"reopen -t {currentP4Type} -c {changelist} {currentPath}", null, (processIdentifier, line) =>
            {
                processResponse = $"{processResponse}\n{line}";
            });
            processResponse = processResponse.Trim();

            if (processResponse.EndsWith($"type {currentP4Type}; change {changelist}"))
            {
                Log.WriteLine($"Changed type ({currentP4Type}) of {rawPath}");
                return;
            }
            else if (processResponse.EndsWith($"reopened; change {changelist}"))
            {
                // Log.WriteLine($"NOOP type ({currentP4Type}) of {rawPath}");
                return;
            }
            else
            {
                Log.WriteLine($"Failed to change type ({currentP4Type}) of {rawPath}", ILogOutput.LogType.Error);
                return;
            }
        }
    }
}