// Copyright Greathorn Games Inc. All Rights Reserved.

using System;
using System.IO;

namespace Greathorn.Core.Modules
{
	public class EnvironmentModule : IModule
	{
		public int ExitCode = 0;
        public string? OriginalWorkingDirectory;

		public void UpdateExitCode(int code, bool forceSet = false)
		{
			if (forceSet)
			{
				ExitCode = code;
				return;
			}

			// This will update the exit code to the last known bad code
			if (code != 0)
			{
				ExitCode = code;
			}
		}

        public void Init(PlatformModule platform)
        {
            Environment.SetEnvironmentVariable("DOTNET_CLI_TELEMETRY_OPTOUT", "1");

            OriginalWorkingDirectory = Directory.GetCurrentDirectory();
            Environment.SetEnvironmentVariable("OriginalWorkingDirectory", OriginalWorkingDirectory);

            if(platform.OperatingSystem == PlatformModule.PlatformType.Windows)
            {
                Environment.SetEnvironmentVariable("Win32", "C:\\Windows\\System32");
            }

            Environment.SetEnvironmentVariable("COMPUTERNAME", System.Environment.MachineName);
        }
	}
}
