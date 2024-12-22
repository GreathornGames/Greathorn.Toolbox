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
                using(FileLock journalLock = new FileLock(config.JournalPath))
                {
                    if (!journalLock.SafeLock(config.RetryCount, 5000))
                    {
                        throw new Exception("Unable to get lock on journal file in time.");
                    }
                    SteamTokenJournal? journal = SteamTokenJournal.Get(journalLock);

                    
                    
                }
            }
            catch (Exception ex)
            {
                framework.ExceptionHandler(ex);
            }
        }
    }
}