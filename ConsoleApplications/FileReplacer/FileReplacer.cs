// Copyright Greathorn Games Inc. All Rights Reserved.

using FileReplacer;
using Greathorn.Core;

namespace Greathorn
{
    internal class FileReplacer
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
                FileReplacerConfig config = FileReplacerConfig.Get(framework);
                if (config.TargetFile == null || config.SourceFile == null) return;

                string content = File.ReadAllText(config.SourceFile);
                foreach (KeyValuePair<string, string> kvp in config.Replaces)
                {
                    content = content.Replace(kvp.Key, kvp.Value);
                }
                File.WriteAllText(config.TargetFile, content);
            }
            catch (Exception ex)
            {
                framework.ExceptionHandler(ex);
            }
        }
    }
}