// Copyright Greathorn Games Inc. All Rights Reserved.

using Greathorn.Core;
using Greathorn.Core.Utils;

namespace SteamToken
{
    public class SteamTokenConfig()
    {
        public bool ForceFlag;

        public string? Token;
        public string InstallPackage = "H:\\Steamworks\\SDK\\161.zip";
        public string InstallLocation = "D:\\Steam";
        public string TokenFolder = "H:\\Steamworks\\Tokens";
        public string UsernameEnvironmentVariable = "horde.SteamLogin";


        public string? NetworkUsername;
        public string? NetworkPassword;
        public string NetworkDrive = "H:";
        public string NetworkShare = "\\\\192.168.20.21\\Horde";

        public string? AppBuild;


        public int RetryCount = 5;

        public string? TokenTarget;



        public static SteamTokenConfig Get(ConsoleApplication framework)
        {
            SteamTokenConfig config = new SteamTokenConfig();

            config.ForceFlag = framework.Arguments.BaseArguments.Contains("FORCE");

            if (framework.Arguments.OverrideArguments.ContainsKey("NETWORK-USERNAME"))
            {
                config.NetworkUsername = framework.Arguments.OverrideArguments["NETWORK-USERNAME"];
            }
            if (framework.Arguments.OverrideArguments.ContainsKey("NETWORK-PASSWORD"))
            {
                config.NetworkPassword = framework.Arguments.OverrideArguments["NETWORK-PASSWORD"];
            }
            if (framework.Arguments.OverrideArguments.ContainsKey("NETWORK-DRIVE"))
            {
                config.NetworkDrive = framework.Arguments.OverrideArguments["NETWORK-DRIVE"];
            }
            if (framework.Arguments.OverrideArguments.ContainsKey("NETWORK-SHARE"))
            {
                config.NetworkShare = framework.Arguments.OverrideArguments["NETWORK-SHARE"];
            }

            // We need to early configure the network share if we have a password
            if (!string.IsNullOrEmpty(config.NetworkPassword) && !string.IsNullOrEmpty(config.NetworkUsername) && !Directory.Exists(config.TokenFolder))
            {
                Log.WriteLine($"Establishing network share {config.NetworkDrive} -> {config.NetworkShare}");
                ProcessUtil.Execute("net", null, $"use {config.NetworkDrive} {config.NetworkShare} /USER:{config.NetworkUsername} {config.NetworkPassword}", null, (processIdentifier, line) =>
                {
                    Log.WriteLine($"[{processIdentifier}]\t{line}");
                });
            }

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

            if (framework.Arguments.OverrideArguments.ContainsKey("APPBUILD"))
            {
                config.AppBuild = framework.Arguments.OverrideArguments["APPBUILD"];
                if (!File.Exists(config.AppBuild))
                {
                    throw new FileNotFoundException($"Unable to access {config.AppBuild}");
                }
            }
            if (string.IsNullOrEmpty(config.AppBuild))
            {
                throw new Exception("You need to provide an APPBUILD to utilizing the LAUNCH action.");
            }

            return config;
        }
    }
}