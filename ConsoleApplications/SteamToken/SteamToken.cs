// Copyright Greathorn Games Inc. All Rights Reserved.

using Greathorn.Core;
using Greathorn.Core.IO;
using Greathorn.Core.Utils;
using SteamToken;

namespace Greathorn
{
    internal class SteamToken
    {
        static FileLock? s_Token = null;
        static string? s_TokenUsername;


        static bool CheckoutToken(SteamTokenConfig config)
        {
            // Get token list / lock
            string[] foundTokens = Directory.GetFiles(config.TokenFolder, "*.vdf");
            if (foundTokens.Length <= 0)
            {
                throw new Exception($"Unable to find tokens (*.vdf) at {config.TokenFolder}.");
            }
            Log.WriteLine($"Found {foundTokens.Length} Tokens In Pool.");

            // Handle Specific Target
            if (config.Token != null)
            {
                s_Token = new FileLock(Path.Combine(config.TokenFolder, config.Token + ".vdf"));
                s_Token.Lock(config.ForceFlag);
                if (!s_Token.HasLock())
                {

                    if (!s_Token.SafeLock())
                    {
                        throw new Exception($"Was unable to acquire lock to {config.Token}");
                    }
                }
            }
            if (s_Token == null)
            {
                for (int i = 0; i < foundTokens.Length; i++)
                {
                    // We don't allow force when we don't have a target
                    s_Token = new FileLock(foundTokens[i]);
                    if (s_Token.Lock())
                    {
                        break;
                    }
                    s_Token = null;
                }
            }


            if (s_Token == null)
            {
                return false;
            }



            // We need to ensure the folder we are writing to exists as it might be a brand new installation
#pragma warning disable CS8604 // Possible null reference argument.
            FileUtil.EnsureFileFolderHierarchyExists(config.TokenTarget);
#pragma warning restore CS8604 // Possible null reference argument.

            File.Copy(s_Token.FilePath, config.TokenTarget, true);

            s_TokenUsername = Path.GetFileNameWithoutExtension(s_Token.FilePath);

            Log.WriteLine($"Checked out {s_Token.FilePath} to {config.TokenTarget} for user {s_TokenUsername}.");

            return true;
        }
        static bool CheckinToken(SteamTokenConfig config)
        {

            if (s_Token == null)
            {
                throw new Exception("Attempting to check-in a token that is null.");
            }
            if (s_Token.HasLock())
            {
                if (config.TokenTarget != null)
                {
                    Log.WriteLine($"Returned {config.TokenTarget} to {s_Token.FilePath}.");

                    // Copy File
                    File.Copy(config.TokenTarget, s_Token.FilePath, true);
                }
                s_Token.Unlock();
                return true;
            }
            return false;
        }

        static void Main()
        {
            using ConsoleApplication framework = new(
            new Greathorn.Core.ConsoleApplicationSettings()
            {
                DefaultLogCategory = "STEAMTOKEN",
                LogOutputs = [new Greathorn.Core.Loggers.ConsoleLogOutput()]
            });



            try
            {
                SteamTokenConfig config = SteamTokenConfig.Get(framework);

                // Check for existing install
                if (!Directory.Exists(Path.Combine(config.InstallLocation, "sdk")))
                {

                    // We don't have an existing version install we need to grab the package
                    Log.WriteLine($"Installing Steamworks SDK to {config.InstallLocation}.");
                    System.IO.Compression.ZipFile.ExtractToDirectory(config.InstallPackage, config.InstallLocation);
                }

                string steamCmd = Path.Combine(config.InstallLocation, "sdk", "tools", "ContentBuilder", "builder", "steamcmd.exe");
                if (!File.Exists(steamCmd))
                {
                    throw new Exception("Unable to find SteamCMD");
                }
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                string steamCmdDirectory = Path.GetDirectoryName(steamCmd);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

                // Make sure the install is up to date
                int updateExitCode = ProcessUtil.Execute(steamCmd, steamCmdDirectory, "+quit", null, (processIdentifier, line) =>
                {
                    Log.WriteLine(line, ILogOutput.LogType.ExternalProcess);
                });
                framework.Environment.UpdateExitCode(updateExitCode);


                if (!CheckoutToken(config))
                {
                    throw new Exception("Unable to checkout a token.");
                }

                // Run the upload
                int uploadExitCode = ProcessUtil.Execute(steamCmd, steamCmdDirectory, $"+login {s_TokenUsername} +run_app_build {config.AppBuild} +quit", null, (processIdentifier, line) =>
                {
                    Log.WriteLine(line, ILogOutput.LogType.ExternalProcess);
                });
                framework.Environment.UpdateExitCode(uploadExitCode);

                if (!CheckinToken(config))
                {
                    Log.WriteLine("There was an issue returning the token.", ILogOutput.LogType.Warning);
                }

            }
            catch (Exception ex)
            {
                s_Token?.Unlock();
                framework.ExceptionHandler(ex);
            }
        }
    }
}