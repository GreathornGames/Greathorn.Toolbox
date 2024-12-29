// Copyright Greathorn Games Inc. All Rights Reserved.

using Greathorn.Core;

namespace SteamToken
{
    public class SteamTokenConfig()
    {
        public bool InstallFlag;
        public bool CheckOutFlag;
        public bool CheckInFlag;
        public bool ForceFlag;
        public bool CopyLibraryFlag;

        public string? Token;
        public string InstallPackage = "\\\\192.168.20.21\\Horde\\Steamworks\\SDK\\161.zip";
        public string InstallLocation = "D:\\Steam";
        public string TokenFolder = "\\\\192.168.20.21\\Horde\\Steamworks\\Tokens";
        public string UsernameEnvironmentVariable = "horde.SteamLogin";

        public int RetryCount = 5;

        public string? TokenTarget;

        public static SteamTokenConfig Get(ConsoleApplication framework)
        {
            SteamTokenConfig config = new SteamTokenConfig();

            if (framework.Arguments.OverrideArguments.ContainsKey("TOKEN-TARGET"))
            {
                config.TokenTarget = framework.Arguments.OverrideArguments["TOKEN-TARGET"];
            }


            if (framework.Arguments.OverrideArguments.ContainsKey("TOKEN-FOLDER"))
            {
                config.TokenFolder = framework.Arguments.OverrideArguments["TOKEN-FOLDER"];
            }

            if (!Directory.Exists(config.TokenFolder))
            {
                throw (new DirectoryNotFoundException($"Unable to reach the token folder @ {config.TokenFolder}"));
            }

            if (framework.Arguments.OverrideArguments.ContainsKey("INSTALL-PACKAGE"))
            {
                config.InstallPackage = framework.Arguments.OverrideArguments["INSTALL-PACKAGE"];
            }

            if (!File.Exists(config.InstallPackage))
            {
                throw (new DirectoryNotFoundException($"Unable to reach the install package @ {config.InstallPackage}"));
            }

            if (framework.Arguments.OverrideArguments.ContainsKey("INSTALL-LOCATION"))
            {
                config.InstallLocation = framework.Arguments.OverrideArguments["INSTALL-LOCATION"];
            }

            if (framework.Arguments.OverrideArguments.ContainsKey("TOKEN"))
            {
                config.Token = framework.Arguments.OverrideArguments["TOKEN"];
            }

            if (framework.Arguments.OverrideArguments.ContainsKey("RETRYCOUNT"))
            {
                int.TryParse(framework.Arguments.OverrideArguments["RETRYCOUNT"], out config.RetryCount);
            }

            if (config.RetryCount < 0)
            {
                throw new ArgumentOutOfRangeException("Retry count must not be negative.");
            }

            config.InstallFlag = framework.Arguments.BaseArguments.Contains("INSTALL");
            config.CheckOutFlag = framework.Arguments.BaseArguments.Contains("OUT");
            config.CheckInFlag = framework.Arguments.BaseArguments.Contains("IN");
            config.ForceFlag = framework.Arguments.BaseArguments.Contains("FORCE");
            config.CopyLibraryFlag = framework.Arguments.BaseArguments.Contains("LIBRARY");

            if (config.CheckOutFlag && config.TokenTarget == null)
            {
                throw new Exception("You need to provide a TOKEN-TARGET when checking out a token.");
            }

            if (config.CheckInFlag && config.TokenTarget == null)
            {
                throw new Exception("You need to provide a TOKEN-TARGET when checking in a token.");
            }

            if (!config.InstallFlag && !config.CheckInFlag && !config.CheckOutFlag)
            {
                throw new Exception("You need to provide an action IN, OUT, or INSTALL action.");
            }

            return config;
        }
    }
}