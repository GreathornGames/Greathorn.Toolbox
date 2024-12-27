// Copyright Greathorn Games Inc. All Rights Reserved.

using Greathorn.Core;
using Greathorn.Core.IO;
using SteamToken;

namespace Greathorn
{
    internal class SteamToken
    {
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
                FileLock? token = null;

                // Get token list / lock
                string[] foundTokens = Directory.GetFiles(config.TokenFolder, "*.vdf");
                if (foundTokens.Length <= 0)
                {
                    throw new Exception($"Unable to find tokens (*.vdf) at {config.TokenFolder}.");
                }
                Log.WriteLine($"Found {foundTokens.Length} Tokens In Pool.");

                // --- CHECKOUT ---
                if (config.CheckOutFlag)
                {
                    // Handle Specific Target
                    if (config.Token != null)
                    {
                        token = new FileLock(Path.Combine(config.TokenFolder, config.Token + ".vdf"));
                        token.Lock(config.ForceFlag);
                        if (!token.HasLock())
                        {

                            if (!token.SafeLock())
                            {
                                throw new Exception($"Was unable to acquire lock to {config.Token}");
                            }
                        }
                    }
                    if (token == null)
                    {
                        for (int i = 0; i < foundTokens.Length; i++)
                        {
                            // We don't allow force when we don't have a target
                            token = new FileLock(foundTokens[i]);
                            if (token.Lock())
                            {
                                break;
                            }
                            token = null;
                        }
                    }
                    if (token == null)
                    {
                        throw new Exception($"Was unable to acquire a token.");
                    }

#pragma warning disable CS8604
                    // Because we are checking this out we want the lock to be persistent, this complicates things if
                    // a process hangs with a lock, but we have to do _something_. The locking mechanism does account
                    // for this by looking at the timestamps
                    token.IsPersistant = true;

                    Log.WriteLine($"Checked out {token.FilePath} to {config.TokenTarget}.");

                    File.Copy(token.FilePath, config.TokenTarget, true);

                    // Write a previous file so we know where we got this token from
                    File.WriteAllText(config.TokenTarget + ".checkout", token.FilePath);
#pragma warning restore CS8604
                }

                // --- CHECKIN ---
                if (config.CheckInFlag)
                {
#pragma warning disable CS8604
                    string previousCheckout = config.TokenTarget + ".checkout";
                    // We need to get the checkout data
                    if (!File.Exists(previousCheckout))
                    {
                        throw new Exception("Unable to find previous checkout data.");
                    }
                    string previousPath = File.ReadAllText(previousCheckout).Trim();
                    string tokenFilePath = Path.Combine(config.TokenFolder, Path.GetFileName(previousPath));

#pragma warning restore CS8604
                    token = new FileLock(tokenFilePath);

                    // The idea is that the lock has already been gotten for the file at this point and we are just
                    // continuing the persistent lock from before.
                    token.Lock(config.ForceFlag);
                    if (!token.HasLock())
                    {
                        if (!token.SafeLock())
                        {
                            throw new Exception($"Was unable to acquire lock to {config.Token}");
                        }
                    }

                    Log.WriteLine($"Returned {config.TokenTarget} to {tokenFilePath}.");

                    if (config.TokenTarget != null)
                    {
                        // Copy File
                        File.Copy(config.TokenTarget, tokenFilePath, true);
                    }

                    token.Unlock();
                    File.Delete(previousCheckout);
                }
            }
            catch (Exception ex)
            {
                framework.ExceptionHandler(ex);
            }
        }
    }
}