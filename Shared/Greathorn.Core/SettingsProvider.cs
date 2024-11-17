// Copyright Greathorn Games Inc. All Rights Reserved.

using System;
using System.IO;

namespace Greathorn.Core
{
    /// <remarks>
    ///     If these are changed you should triple check for anything in the Bootstrap project that would need updating.
    ///     Because it does not reference anything else, changing here won't change Bootstrap.
    /// </remarks>
    public class SettingsProvider
    {
        public const string P4Port = "ssl:perforce.greathorn.games:1666";
        public const string P4CharacterSet = "utf16";
        public const string P4IgnoreFileName = "p4ignore.txt";
        public const string P4ConfigFileName = "p4config.txt";
        public const string P4CustomToolsFileName = "p4v-custom-tools.xml";
        public const string BuildHashFileName = "GG_BUILD_SHA";

        public readonly string BoostrapLibrary;
        public readonly string RootFolder;
        public readonly string LogsFolder;
        public readonly string BuildBatchFilesFolder;
        public readonly string TempFile;
        public readonly string P4ConfigFile;
        public readonly string SolutionFile;

        public readonly string ProjectsFolder;
        public readonly string GreathornFolder;

        public readonly string GreathornToolboxFolder;
        public readonly string GreathornDotNETFolder;
        public readonly string GreathornProgramsFolder;


        public readonly string GreathornWorkspaceSettingsFile;
        public readonly string GreathornWorkspaceVersionFile;

        public readonly string EngineBuildVersionFile;

        public readonly string AppDataFolder;
        public readonly string AppDataLocalFolder;
        public readonly string AppDataLocalLowFolder;
        public readonly string AppDataRoamingFolder;

        public SettingsProvider(string root)
        {
            RootFolder = root;

            SolutionFile = Path.Combine(RootFolder, "UE5.sln");
            GreathornFolder = Path.Combine(RootFolder, "Greathorn");
            BoostrapLibrary = Path.Combine(GreathornFolder, "Binaries", "Bootstrap", "Bootstrap.dll");

            LogsFolder = Path.Combine(RootFolder, "Logs");
            BuildBatchFilesFolder = Path.Combine(RootFolder, "Engine", "Build", "BatchFiles");
            TempFile = Path.Combine(RootFolder, "gg.tmp");

            P4ConfigFile = Path.Combine(RootFolder, SettingsProvider.P4ConfigFileName);
            ProjectsFolder = Path.Combine(RootFolder, "Projects");

            GreathornToolboxFolder = Path.Combine(GreathornFolder, "Source", "Programs", "Greathorn.Toolbox");
            GreathornDotNETFolder = Path.Combine(GreathornFolder, "Binaries", "DotNET");
            GreathornProgramsFolder = Path.Combine(GreathornFolder, "Programs");

            GreathornWorkspaceSettingsFile = Path.Combine(GreathornFolder, "GG_SETTINGS");
            GreathornWorkspaceVersionFile = Path.Combine(GreathornFolder, "GG_WORKSPACE");

            EngineBuildVersionFile = Path.Combine(RootFolder, "Engine", "Build", "Build.version");

            AppDataFolder = Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "..");
            AppDataLocalFolder = Path.Combine(AppDataFolder, "Local");
            AppDataLocalLowFolder = Path.Combine(AppDataFolder, "LocalLow");
            AppDataRoamingFolder = Path.Combine(AppDataFolder, "Roaming");
        }

        public string ReplaceKeywords(string sourceString)
        {
            return sourceString.Replace("{ROOT}", RootFolder)
                               .Replace("{LOCALLOW}", AppDataLocalLowFolder)
                               .Replace("{LOCAL}", AppDataLocalFolder)
                               .Replace("{ROAMING}", AppDataRoamingFolder);
        }

        public void Output()
        {
            Log.WriteLine("Settings:");
            Log.WriteLine($"Root: {RootFolder}", "LOCATION", ILogOutput.LogType.Info);
        }
    }
}
