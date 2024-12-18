// Copyright Greathorn Games Inc. All Rights Reserved.

using Greathorn.Core;

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

            }
            catch (Exception ex)
            {
                framework.ExceptionHandler(ex);
            }
        }
    }
}