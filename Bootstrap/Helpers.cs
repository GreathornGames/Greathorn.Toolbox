// Copyright Greathorn Games Inc. All Rights Reserved.

using System.Diagnostics;
using System.Reflection;
using System.Security;
using System.Text;

namespace Greathorn
{
    /// <summary>
    /// Mini collection of helpers from the wider pool explicitly used to get this thing off the ground.
    /// </summary>
    internal static class Helpers
    {
        internal const int k_AsciiCaseShift = 32;
        internal const int k_AsciiLowerCaseStart = 97;
        internal const int k_AsciiLowerCaseEnd = 122;

        static readonly int k_CachedGenerateProjectFilesHash = "GenerateProjectFiles.bat".GetStableUpperCaseHashCode();
        static readonly int k_CachedP4IgnoreHash = "p4ignore.txt".GetStableUpperCaseHashCode();
        static readonly int k_CachedSetupHash = "Setup.bat".GetStableUpperCaseHashCode();
        static string? s_CachedWorkspaceRoot = null;

        internal static string? GetWorkspaceRoot(string? workingDirectory = null)
        {
            // Use our cached version!
            if (s_CachedWorkspaceRoot != null)
            {
                return s_CachedWorkspaceRoot;
            }

            // If we don't have anything provided, we need to start somewhere.
            workingDirectory ??= Directory.GetParent(Assembly.GetEntryAssembly().Location).FullName;

            // Check local files for marker
            string[] localFiles = Directory.GetFiles(workingDirectory);
            int localFileCount = localFiles.Length;
            int foundCount = 0;

            // Iterate over the directory files
            for (int i = 0; i < localFileCount; i++)
            {
                int fileNameHash = Path.GetFileName(localFiles[i]).GetStableUpperCaseHashCode();

                if (fileNameHash == k_CachedGenerateProjectFilesHash)
                {
                    foundCount++;
                }

                if (fileNameHash == k_CachedP4IgnoreHash)
                {
                    foundCount++;
                }

                if (fileNameHash == k_CachedSetupHash)
                {
                    foundCount++;
                }
            }

            // We know this is the root based on found files
            if (foundCount == 3)
            {
                s_CachedWorkspaceRoot = workingDirectory;
                return s_CachedWorkspaceRoot;
            }

            // Go back another directory
            DirectoryInfo? parent = Directory.GetParent(workingDirectory);
            if (parent != null)
            {
                return GetWorkspaceRoot(parent.FullName);
            }
            return null;
        }
        [SecuritySafeCritical]
        internal static unsafe int GetStableUpperCaseHashCode(this string targetString)
        {
            fixed (char* src = targetString)
            {
                int hash1 = 5381;
                int hash2 = hash1;
                int c;
                char* s = src;

                // Get character
                while ((c = s[0]) != 0)
                {
                    // Check character value and shift it if necessary (32)
                    if (c >= k_AsciiLowerCaseStart && c <= k_AsciiLowerCaseEnd)
                    {
                        c ^= k_AsciiCaseShift;
                    }

                    // Add to Hash #1
                    hash1 = ((hash1 << 5) + hash1) ^ c;

                    // Get our second character
                    c = s[1];

                    if (c == 0)
                    {
                        break;
                    }

                    // Check character value and shift it if necessary (32)
                    if (c >= k_AsciiLowerCaseStart && c <= k_AsciiLowerCaseEnd)
                    {
                        c ^= k_AsciiCaseShift;
                    }

                    hash2 = ((hash2 << 5) + hash2) ^ c;
                    s += 2;
                }

                return hash1 + hash2 * 1566083941;
            }
        }

        public static int Execute(string executablePath, string? workingDirectory, string? arguments, string? input, Action<int, string> outputLine)
        {
            using Process childProcess = new();
            object lockObject = new();

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

            childProcess.StartInfo.EnvironmentVariables["DOTNET_CLI_TELEMETRY_OPTOUT"] = "true";
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
        public static int Elevate(string executablePath, string? workingDirectory, string? arguments)
        {
            Process childProcess = new();
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

        public static void CheckoutRepo(string uri, string checkoutFolder, string? branch = null, string? commit = null, int depth = -1, bool submodules = true, bool shallowsubmodules = true)
        {
            string executablePath;
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                executablePath = "git.exe";
            }
            else
            {
                executablePath = "git";
            }            
            StringBuilder commandLineBuilder = new StringBuilder();
            commandLineBuilder.Append("clone ");

            if (branch != null)
            {
                commandLineBuilder.AppendFormat("--branch {0} --single-branch ", branch);
            }

            if (depth != -1)
            {
                commandLineBuilder.AppendFormat("--depth {0} ", depth.ToString());
            }

            if (submodules)
            {
                commandLineBuilder.Append("--recurse-submodules --remote-submodules ");
                if (shallowsubmodules)
                {
                    commandLineBuilder.Append("--shallow-submodules ");
                }
            }

            Console.WriteLine($"{commandLineBuilder}{uri} {checkoutFolder}");
            Execute(executablePath, Directory.GetParent(checkoutFolder),
                $"{commandLineBuilder}{uri} {checkoutFolder}", null, (System.Action<int, string>)((processIdentifier, line) =>
                {
                    Console.WriteLine(line);
                }));

            // Was a commit specified?
            if (commit != null)
            {
                Console.WriteLine($"Checkout Commit {commit}");
                Execute(executablePath, checkoutFolder,
                    $"checkout {commit}", null, (System.Action<int, string>)((processIdentifier, line) =>
                    {
                        Console.WriteLine(line);
                    }));
            }
        }

    }
}
