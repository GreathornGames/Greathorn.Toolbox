// Copyright Greathorn Games Inc. All Rights Reserved.

using Greathorn.Core;
using Greathorn.Core.Loggers;
using Greathorn.Services.Perforce;

namespace Greathorn
{
    internal class GG
    {
        static void Main()
        {
            using ConsoleApplication framework = new(
            new Greathorn.Core.ConsoleApplicationSettings()
            {
                DefaultLogCategory = "GG",
                LogOutputs = [new Greathorn.Core.Loggers.ConsoleLogOutput()]
            });

            try
            {
                string? workspaceRoot = PerforceUtil.GetWorkspaceRoot();

                if (workspaceRoot != null)
                {
                    // Start a file log
                    Log.AddLogOutput(new FileLogOutput(Path.Combine(workspaceRoot, "Logs", "GG.log")));

                    string batchFiles = Path.Combine(workspaceRoot, "Engine", "Build", "BatchFiles");
                }
            }
            catch (Exception ex)
            {
                framework.ExceptionHandler(ex);
            }
        }
    }
}