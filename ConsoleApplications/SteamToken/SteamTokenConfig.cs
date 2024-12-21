// Copyright Greathorn Games Inc. All Rights Reserved.

using Greathorn.Core;

namespace SteamToken
{
    public class SteamTokenConfig()
    {
        public string DatabaseFolder = "H:\\SteamUploader";
        public string JournalFileName = "journal.json";

        public string? JournalPath;

        public static SteamTokenConfig Get(ConsoleApplication framework)
        {
            SteamTokenConfig config = new SteamTokenConfig();

            if (framework.Arguments.OverrideArguments.ContainsKey("DATABASE"))
            {
                config.DatabaseFolder = framework.Arguments.OverrideArguments["DATABASE"];
            }

            if (!Directory.Exists(config.DatabaseFolder))
            {
                throw (new DirectoryNotFoundException($"Unable to reach / find database directory @ {config.DatabaseFolder}"));
            }

            if (framework.Arguments.OverrideArguments.ContainsKey("JOURNAL"))
            {
                config.JournalFileName = framework.Arguments.OverrideArguments["JOURNAL"];
            }

            config.JournalPath = Path.Combine(config.DatabaseFolder, config.JournalFileName);


            if (!File.Exists(config.JournalPath))
            {
                throw (new FileNotFoundException($"Unable to find journal file @ {config.JournalPath }"));
            }

            return config;
        }
    }
}