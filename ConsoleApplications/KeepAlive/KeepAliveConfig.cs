// Copyright Greathorn Games Inc. All Rights Reserved.

using System.Text.Json.Serialization;
using System.Text.Json;
using Greathorn.Core;

namespace KeepAlive
{
    public class KeepAliveConfig
    {
        public const int UpdateSleep = 1000 * 1;
        public const int IssueSleep = 1000 * 10;
        public const int StartSleep = 1000 * 2;

        public string Application { get; set; } = "C:\\Program Files\\Epic Games\\Horde\\Agent\\HordeAgent.exe";
        public string? Arguments { get; set; }
        public string? WorkingDirectory { get; set; } = "C:\\Program Files\\Epic Games\\Horde\\Agent\\";
        public string ProcessInfoPath { get; set; } = Path.Combine(Path.GetTempPath(), "HordeAgent.pid");
        public int SleepMilliseconds { get; set; }
        public int TimeoutSleepMilliseconds { get; set; }
        public int StartSleepMilliseconds { get; set; }

        public bool CheckResponding { get; set; } = true;
        public bool CheckHasExited { get; set; } = true;

        public static KeepAliveConfig Get(string? jsonPath = null)
        {
            JsonSerializerOptions jsonSettings = new()
            {
                AllowTrailingCommas = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true
            };

            try
            {
                KeepAliveConfig? foundConfig = null;
                if (jsonPath == null)
                {
                    string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "keepalive.json");
                    if (!File.Exists(filePath)) return new KeepAliveConfig();

                    string content = File.ReadAllText(filePath);

                    foundConfig = JsonSerializer.Deserialize<KeepAliveConfig>(content, jsonSettings)!;
                }
                else
                {
                    if (!File.Exists(jsonPath)) return Get();

                    string content = File.ReadAllText(jsonPath);

                    foundConfig = JsonSerializer.Deserialize<KeepAliveConfig>(content, jsonSettings);
                    if (foundConfig == null) return Get();
                }

                CleanUpConfig(foundConfig);
                return foundConfig;
            }
            catch (Exception e)
            {
                Log.WriteLine(e.Message, ILogOutput.LogType.Error);
                if(e.StackTrace != null)
                {
                    Log.WriteLine(e.StackTrace, ILogOutput.LogType.Error);
                }
                return Get();
            }
        }

        public static void CleanUpConfig(KeepAliveConfig config)
        {
            string appName = Path.GetFileNameWithoutExtension(config.Application);
            if (string.IsNullOrEmpty(config.ProcessInfoPath))
            {
                config.ProcessInfoPath = Path.Combine(Path.GetTempPath(), $"{appName}.pid");
            }

            if (string.IsNullOrEmpty(config.WorkingDirectory) || !Directory.Exists(config.WorkingDirectory))
            {
                config.WorkingDirectory = Directory.GetParent(config.Application)?.FullName;
            }

            if (config.SleepMilliseconds <= 0)
            {
                config.SleepMilliseconds = UpdateSleep;
            }

            if (config.TimeoutSleepMilliseconds <= 0)
            {
                config.TimeoutSleepMilliseconds = IssueSleep;
            }

            if (config.StartSleepMilliseconds <= 0)
            {
                config.StartSleepMilliseconds = StartSleep;
            }
        }

        public bool IsValid()
        {
            if (string.IsNullOrEmpty(Application))
            {
                Log.WriteLine("No application found", ILogOutput.LogType.Error);
                return false;
            }

            if(!File.Exists(Application))
            {
                Log.WriteLine("No application found at provided path.", ILogOutput.LogType.Error);
                return false;
            }

            return true;
        }
    }
}
