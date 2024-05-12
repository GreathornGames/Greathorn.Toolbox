// Copyright Greathorn Games Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;

namespace Greathorn.Core.Utils
{
	public static class ProcessUtil
	{
		public static void AddDefaultEnvironmentVariables(this Process process)
		{
            if(process.StartInfo.UseShellExecute)
            {
                Log.WriteLine("Unable to add environment variables to shell executed process.", "PROCESS", ILogOutput.LogType.Error);
                return;
            }
			process.StartInfo.EnvironmentVariables["DOTNET_CLI_TELEMETRY_OPTOUT"] = "true";
		}

        public static bool Spawn(string executablePath, string? arguments, string? workingDirectory)
		{
			using Process childProcess = new Process();
			AddDefaultEnvironmentVariables(childProcess);
            childProcess.StartInfo.WorkingDirectory = string.IsNullOrEmpty(workingDirectory) ? null : workingDirectory;
			childProcess.StartInfo.FileName = executablePath;
			childProcess.StartInfo.Arguments = string.IsNullOrEmpty(arguments) ? string.Empty : arguments;
            childProcess.StartInfo.UseShellExecute = false;
			return childProcess.Start();
		}

        public static bool SpawnHidden(string executablePath, string? arguments)
		{
			using Process childProcess = new Process();
			AddDefaultEnvironmentVariables(childProcess);
			childProcess.StartInfo.FileName = executablePath;
			childProcess.StartInfo.Arguments = string.IsNullOrEmpty(arguments) ? "" : arguments;
			childProcess.StartInfo.UseShellExecute = false;
			childProcess.StartInfo.RedirectStandardOutput = true;
			childProcess.StartInfo.RedirectStandardError = true;
			childProcess.StartInfo.CreateNoWindow = true;
			return childProcess.Start();
		}

        public static bool SpawnSeperate(string executablePath, string? arguments, string? workingDirectory, bool elevate = false)
        {
            using Process childProcess = new Process();
            if (elevate)
            {
                childProcess.StartInfo.Verb = "runas";
            }
            childProcess.StartInfo.WorkingDirectory = string.IsNullOrEmpty(workingDirectory) ? null : workingDirectory;
            childProcess.StartInfo.FileName = executablePath;
            childProcess.StartInfo.Arguments = string.IsNullOrEmpty(arguments) ? "" : arguments;
            childProcess.StartInfo.UseShellExecute = true;
            return childProcess.Start();
        }

        public static bool SpawnWithEnvironment(string executablePath, string? arguments, string? workingDirectory, Dictionary<string, string>? environmentVariables)
        {
            using Process childProcess = new Process();
            childProcess.StartInfo.WorkingDirectory = string.IsNullOrEmpty(workingDirectory) ? null : workingDirectory;
            childProcess.StartInfo.FileName = executablePath;
            childProcess.StartInfo.Arguments = string.IsNullOrEmpty(arguments) ? "" : arguments;
            childProcess.StartInfo.UseShellExecute = false;
            childProcess.AddDefaultEnvironmentVariables();
            // Add custom
            if(environmentVariables != null && environmentVariables.Count > 0)
            {
                foreach(KeyValuePair<string,string> kvp in environmentVariables)
                {
                    childProcess.StartInfo.EnvironmentVariables[kvp.Key] = kvp.Value;
                }
            }
            return childProcess.Start();
        }

        public static int Execute(string executablePath, string? workingDirectory, string? arguments, string? input, TextWriter log)
		{
			return Execute(executablePath, workingDirectory, arguments, input, (processIdentifier, line) => log.WriteLine(line));
		}

		public static int Execute(string executablePath, string? workingDirectory, string? arguments, string? input,
			out List<string> outputLines)
		{
			List<string> output = new List<string>();
			int returnValue = Execute(executablePath, workingDirectory, arguments, input, (processIdentifier, line) => output.Add(line));
			outputLines = output;
			return returnValue;
		}

		public static int Execute(string executablePath, string? workingDirectory, string? arguments, string? input, Action<int, string> outputLine)
		{
			using Process childProcess = new Process();
			object lockObject = new object();

			void OutputHandler(object x, DataReceivedEventArgs y)
			{
				if (y.Data != null)
				{
					lock (lockObject)
					{
						outputLine(childProcess.Id, y.Data.TrimEnd());
					}
				}
			}

			AddDefaultEnvironmentVariables(childProcess);
			if (workingDirectory != null)
			{
				childProcess.StartInfo.WorkingDirectory = workingDirectory;
			}
			childProcess.StartInfo.FileName = executablePath;
			childProcess.StartInfo.Arguments = string.IsNullOrEmpty(arguments) ? "" : arguments;
			childProcess.StartInfo.UseShellExecute = false;
			childProcess.StartInfo.RedirectStandardOutput = true;
			childProcess.StartInfo.RedirectStandardError = true;
			childProcess.OutputDataReceived += OutputHandler;
			childProcess.ErrorDataReceived += OutputHandler;
			childProcess.StartInfo.RedirectStandardInput = input != null;
			childProcess.StartInfo.CreateNoWindow = true;
			childProcess.StartInfo.StandardOutputEncoding = new UTF8Encoding(false, false);
			childProcess.Start();
			childProcess.BeginOutputReadLine();
			childProcess.BeginErrorReadLine();

			if (!string.IsNullOrEmpty(input))
			{
				childProcess.StandardInput.WriteLine(input);
				childProcess.StandardInput.Close();
			}

			// Busy wait for the process to exit so we can get a ThreadAbortException if the thread is terminated.
			// It won't wait until we enter managed code again before it throws otherwise.
			for (; ; )
			{
				if (childProcess.WaitForExit(20))
				{
					childProcess.WaitForExit();
					break;
				}
			}

			return childProcess.ExitCode;
		}

		public static int Interactive(string executablePath, string? workingDirectory, string? arguments, Action<string, StreamWriter> interactionHandler)
		{
			using Process childProcess = new Process();
			object lockObject = new object();

			void OutputHandler(object x, DataReceivedEventArgs y)
			{
				if (y.Data != null)
				{
					lock (lockObject)
					{
						interactionHandler(y.Data.TrimEnd(), childProcess.StandardInput);
					}
				}
			}

			AddDefaultEnvironmentVariables(childProcess);
			if (workingDirectory != null)
			{
				childProcess.StartInfo.WorkingDirectory = workingDirectory;
			}
			childProcess.StartInfo.FileName = executablePath;
			childProcess.StartInfo.Arguments = string.IsNullOrEmpty(arguments) ? "" : arguments;
			childProcess.StartInfo.UseShellExecute = false;
			childProcess.StartInfo.RedirectStandardOutput = true;
			childProcess.StartInfo.RedirectStandardError = true;
			childProcess.OutputDataReceived += OutputHandler;
			childProcess.ErrorDataReceived += OutputHandler;
			childProcess.StartInfo.RedirectStandardInput = true;
			childProcess.StartInfo.CreateNoWindow = true;
			childProcess.StartInfo.StandardOutputEncoding = new UTF8Encoding(false, false);
			childProcess.Start();
			childProcess.BeginOutputReadLine();
			childProcess.BeginErrorReadLine();

			// Busy wait for the process to exit so we can get a ThreadAbortException if the thread is terminated.
			// It won't wait until we enter managed code again before it throws otherwise.
			for (; ; )
			{
				if (childProcess.WaitForExit(20))
				{
					childProcess.WaitForExit();
					break;
				}
			}

			return childProcess.ExitCode;
		}

		public static int OpenFileWithDefault(string filePath)
		{
			if (File.Exists(filePath)) 
			{
				if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				{
                    if (SpawnHidden("notepad", filePath))
                    {
                        return 0;
                    }
                    return 1;
                }
				else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
				{
					if (SpawnHidden("open", filePath))
					{
						return 0;
					}
					return 1;
				}
			}
			return 1;
		}

        public static int Elevate(string executablePath, string? workingDirectory, string? arguments, bool shouldWait = true)
        {
            using Process childProcess = new Process();
            if (workingDirectory != null)
            {
                childProcess.StartInfo.WorkingDirectory = workingDirectory;
            }
            childProcess.StartInfo.FileName = executablePath;
            childProcess.StartInfo.Arguments = string.IsNullOrEmpty(arguments) ? "" : arguments;
            childProcess.StartInfo.UseShellExecute = true;
            childProcess.StartInfo.Verb = "runas";
            childProcess.StartInfo.CreateNoWindow = true;
            childProcess.Start();


            // Busy wait for the process to exit so we can get a ThreadAbortException if the thread is terminated.
            // It won't wait until we enter managed code again before it throws otherwise.'
            if (shouldWait)
            {
                for (; ; )
                {
                    if (childProcess.WaitForExit(20))
                    {
                        childProcess.WaitForExit();
                        break;
                    }
                }
            }

            return childProcess.ExitCode;
        }

        public static bool IsElevated()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                new WindowsPrincipal(WindowsIdentity.GetCurrent())
                    .IsInRole(WindowsBuiltInRole.Administrator) :
                Mono.Unix.Native.Syscall.geteuid() == 0;
        }
    }
}
