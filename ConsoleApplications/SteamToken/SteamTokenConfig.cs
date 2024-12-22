// Copyright Greathorn Games Inc. All Rights Reserved.

using Greathorn.Core;

namespace SteamToken
{
    public class SteamTokenConfig()
    {
        public string JournalPath = "H:\\SteamUploader\\journal.db";
        public int RetryCount = 5;

        public static SteamTokenConfig Get(ConsoleApplication framework)
        {
            SteamTokenConfig config = new SteamTokenConfig();

            if (framework.Arguments.OverrideArguments.ContainsKey("JOURNAL"))
            {
                config.JournalPath = framework.Arguments.OverrideArguments["JOURNAL"];
            }

            if (!File.Exists(config.JournalPath))
            {
                throw (new FileNotFoundException($"Unable to reach / find journal @ {config.JournalPath}"));
            }

            if (framework.Arguments.OverrideArguments.ContainsKey("RETRYCOUNT"))
            {
                int.TryParse(framework.Arguments.OverrideArguments["RETRYCOUNT"], out config.RetryCount);
            }

            if(config.RetryCount < 0)
            {
                throw new ArgumentOutOfRangeException("Retry count must not be negative.");
            }

            return config;
        }
    }
}