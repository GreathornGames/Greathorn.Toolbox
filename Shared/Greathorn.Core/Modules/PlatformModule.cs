// Copyright Greathorn Games Inc. All Rights Reserved.

using System;
using System.Runtime.InteropServices;

namespace Greathorn.Core.Modules
{
	public class PlatformModule : IModule
	{
		public enum PlatformType
		{
			Windows,
			macOS,
			Linux
		}

		public enum ArchitectureType
		{
			None,
			x64,
			x86,
			Intel,
			Arm64
		}

		public readonly PlatformType OperatingSystem;
		public readonly ArchitectureType Architecture;
		public readonly string MachineName;

		public PlatformModule()
		{
			if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				OperatingSystem = PlatformType.Windows;
				Architecture = RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.X86 ? ArchitectureType.x86 : ArchitectureType.x64;
			}
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			{
				OperatingSystem = PlatformType.macOS;
				Architecture = RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.Arm64 ? ArchitectureType.Arm64 : ArchitectureType.Intel;
			}
			else if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				OperatingSystem = PlatformType.Linux;
				Architecture = RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.X86 ? ArchitectureType.x86 : ArchitectureType.x64;
			}

			MachineName = Environment.MachineName;
		}
	}
}
