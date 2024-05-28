// Copyright Greathorn Games Inc. All Rights Reserved.

using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.Security;
using System.Text;

namespace Greathorn
{
    static class Bootstrap
    {
        static bool s_QuietMode = false;
        static bool s_ShouldClone = true;
        static bool s_ShouldBuild = true;
        static bool s_ShouldSetupWorkspace = true;

        const int k_AsciiCaseShift = 32;
        const int k_AsciiLowerCaseStart = 97;
        const int k_AsciiLowerCaseEnd = 122;

        const string k_BuildInfo = "GG_BUILD_SHA";

        static readonly int k_CachedGenerateProjectFilesHash = "GenerateProjectFiles.bat".GetStableUpperCaseHashCode();
        static readonly int k_CachedSetupHash = "Setup.bat".GetStableUpperCaseHashCode();

        static void Main(string[] args)
        {
			try
			{
				Assembly? assembly = Assembly.GetAssembly(typeof(Bootstrap));

				if (assembly != null)
					Console.WriteLine($"Greathorn Bootstrap {assembly.GetName().Version}");

				// Ensure we have a builder
				Microsoft.Build.Locator.VisualStudioInstance visualStudioInstance =
                    Microsoft.Build.Locator.MSBuildLocator.QueryVisualStudioInstances().OrderByDescending(instance => instance.Version).First();
				if (visualStudioInstance == null)
				{
					Console.WriteLine("Unable to find an installation of MSBuild.\nYou need to install .NET SDK found at https://dotnet.microsoft.com/en-us/download/dotnet/8.0");
					Environment.ExitCode = 1;
					PressAnyKeyToContinue();
					return;
				}
				Console.WriteLine($"Using MSBuild @ {visualStudioInstance.MSBuildPath}");

				// Find the workspace root
				string? workspaceRoot = GetWorkspaceRoot();
				if (workspaceRoot == null)
				{
					Console.WriteLine("Unable to find workspace root.");
					Environment.ExitCode = 2;
					PressAnyKeyToContinue();
					return;
				}
				Console.WriteLine($"Workspace Root @ {workspaceRoot}");

                // Build our source folder path
                string sourceFolder = Path.Combine(workspaceRoot, "Greathorn", "Source", "Programs", "Greathorn.Toolbox");
                Console.WriteLine($"Source Folder @ {sourceFolder}");              

                // Grab anything relevant from the command line args
                ParseArguments(args);

                // Run through steps
                CloneSource(sourceFolder);
                BuildSource(sourceFolder);
                WorkspaceSetup(workspaceRoot);            
			}
			catch(Exception ex)
			{
				Console.WriteLine("BOOTSTRAP EXCEPTION !!!");
				Console.WriteLine(ex);
				Environment.ExitCode = ex.HResult;
			}
            PressAnyKeyToContinue();
        }

        #region Process
        static void ParseArguments(string[] arguments)
        {
            int count = arguments.Length;

            for (int i = 0; i < count; i++)
            {

                if (arguments[i] == "no-clone")
                {
                    s_ShouldClone = false;
                }

                if (arguments[i] == "no-build")
                {
                    s_ShouldBuild = false;
                }

                if (arguments[i] == "no-workspace")
                {
                    s_ShouldSetupWorkspace = false;
                }
                
                if (arguments[i] == "quiet")
                {
                    s_QuietMode = true;
                }
            }

            if (!s_QuietMode)
            {
                Console.WriteLine("Settings");
                Console.WriteLine($"\tClone\t\t{s_ShouldClone}");
                Console.WriteLine($"\tBuild\t\t{s_ShouldBuild}");
                Console.WriteLine($"\tSetup Workspace\t{s_ShouldSetupWorkspace}");
                Console.WriteLine($"\tQuiet Mode\t{s_QuietMode}");
            }
        }
        static void CloneSource(string sourceFolder)
        {
            if(!s_ShouldClone)
            {
                Console.WriteLine("Skipping Cloning (Argument) ...");
                return;
            }

            // Extra safe
            if(sourceFolder == null)
            {
                Console.WriteLine("Skipping Cloning (Null Source Folder) ...");
                return;
            }

            // To ensure the folder exists we need to look for the parent
            string? sourceFolderParent = Directory.GetParent(sourceFolder)?.FullName;
            if (!string.IsNullOrEmpty(sourceFolderParent) && !Directory.Exists(sourceFolderParent))
            {
                Directory.CreateDirectory(sourceFolderParent);
            }

#if DEBUG
            Console.WriteLine("Skipping Cloning (Debug Mode) ...");
#else
            // Get or update the source
            if (Directory.Exists(Path.Combine(sourceFolder, ".git")))
            {
                GitUpdateRepo(sourceFolder);
            }
            else
            {
                GitCheckoutRepo("https://github.com/GreathornGames/Greathorn.Toolbox.git", sourceFolder);
            }
#endif
        }
        static void BuildSource(string sourceFolder)
        {
            if (!s_ShouldBuild)
            {
                Console.WriteLine("Skipping Building (Argument) ...");
                return;
            }

            string sharedFolder = Path.Combine(sourceFolder, "Shared");

            // Find all projects, exclude Shared and Bootstrap
            string[] projectFiles = Directory.GetFiles(sourceFolder, "*.csproj", SearchOption.AllDirectories);
            int foundCount = projectFiles.Length;
            List<string> parsedFiles = new(foundCount);
            for (int i = 0; i < foundCount; i++)
            {
                if (projectFiles[i].EndsWith("Bootstrap.csproj")) continue;

                // Might be a bad way to 
                if (projectFiles[i].StartsWith(sharedFolder)) continue;

                parsedFiles.Add(projectFiles[i]);
            }

            int compileCount = parsedFiles.Count;
            Console.WriteLine($"Found {compileCount} projects to compile.");

            int exitCode = 0;
            for (int i = 0; i < compileCount; i++)
            {
                Console.WriteLine($"Building {parsedFiles[i]} ...");
                exitCode = ProcessExecute("dotnet", sourceFolder, $"build {parsedFiles[i]} /property:Configuration=Workspace /property:Platform=AnyCPU /t:Rebuild", null, (processIdentifier, line) =>
                {
                    Console.WriteLine($"[{processIdentifier}]\t{line}");
                });
            }

            // Check that we had a good build
            if(exitCode == 0)
            {
                File.WriteAllText(Path.Combine(sourceFolder, k_BuildInfo), GitGetLocalCommit(sourceFolder));
            }
        }
        static void WorkspaceSetup(string workspaceRoot)
        {
            if (!s_ShouldSetupWorkspace)
            {
                Console.WriteLine("Skipping Workspace Setup (Argument) ...");
                return;
            }

            // We need to run this process elevated, the main executable is bundled to ensure its elevated, but the library is not.
            string args = $"{Path.Combine(workspaceRoot, "Greathorn", "Binaries", "DotNET", "WorkspaceSetup.dll")} no-source no-build";
            if (s_QuietMode)
            {
                args += " quiet";
            }
            ProcessElevate("dotnet", workspaceRoot, args);
        }
#endregion

        #region Helpers
        static string? GetWorkspaceRoot(string? workingDirectory = null)
        {
            // If we don't have anything provided, we need to start somewhere.
            if(workingDirectory == null)
            {
                Assembly? assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
                if(assembly != null)
                {
                    DirectoryInfo? parentInfo  = Directory.GetParent(assembly.Location);
                    if(parentInfo != null)
                    {
                        workingDirectory = parentInfo.FullName;
                    }
                    else
                    {
                        workingDirectory = assembly.Location;
                    }
                   
                }
                if(workingDirectory == null)
                {
                    throw new Exception("Unable to find assembly to determine running location, this is required to find the workspace root.");                    
                }
            }
            
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

                if (fileNameHash == k_CachedSetupHash)
                {
                    foundCount++;
                }
            }

            // We know this is the root based on found files
            if (foundCount == 2)
            {
                return workingDirectory;
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
        static unsafe int GetStableUpperCaseHashCode(this string targetString)
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
        static void PressAnyKeyToContinue()
        {
            if (s_QuietMode) return;

            Console.WriteLine("Press Any Key To Continue ...");
            try
            {
                Console.ReadKey();
            }
            catch (Exception)
            {
                Console.WriteLine("Unable to capture keystroke. Skipping.");
            }
        }
        static int ProcessExecute(string executablePath, string? workingDirectory, string? arguments, string? input, Action<int, string> outputLine)
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
        static int ProcessElevate(string executablePath, string? workingDirectory, string? arguments)
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
        static void GitCheckoutRepo(string uri, string checkoutFolder, string? branch = null, string? commit = null, int depth = -1, bool submodules = true, bool shallowsubmodules = true)
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
            StringBuilder commandLineBuilder = new();
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
            ProcessExecute(executablePath, null,
                $"{commandLineBuilder}{uri} {checkoutFolder}", null, (System.Action<int, string>)((processIdentifier, line) =>
                {
                    Console.WriteLine(line);
                }));

            // Was a commit specified?
            if (commit != null)
            {
                Console.WriteLine($"Checkout Commit {commit}");
                ProcessExecute(executablePath, checkoutFolder,
                    $"checkout {commit}", null, (System.Action<int, string>)((processIdentifier, line) =>
                    {
                        Console.WriteLine(line);
                    }));
            }
        }
        static string GitGetLocalCommit(string checkoutFolder)
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

            // Check current
            List<string> output = [];

            // Get status of the repository
            ProcessExecute(executablePath, checkoutFolder,
                $"rev-parse HEAD", null, (System.Action<int, string>)((processIdentifier, line) =>
                {
                    Console.WriteLine(line);
                    output.Add(line);
                }));

            return output[0].Trim();
        }
        static void GitUpdateRepo(string checkoutFolder, string? branch = null, string? commit = null, bool forceUpdate = true)
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
            // Check current
            List<string> output = [];

            // Get status of the repository
            ProcessExecute(executablePath, checkoutFolder,
                $"fetch origin", null, (System.Action<int, string>)((processIdentifier, line) =>
                {
                    Console.WriteLine(line);
                }));
            ProcessExecute(executablePath, checkoutFolder,
                "status -uno --long", null, (System.Action<int, string>)((processIdentifier, line) =>
                {
                    Console.WriteLine(line);
                    output.Add(line);
                }));

            bool branchBehind = false;
            bool fastForward = false;
            bool detached = false;
            foreach (string s in output)
            {
                if (s.Contains("branch is behind"))
                {
                    branchBehind = true;
                }

                if (s.Contains("and can be fast-forwarded"))
                {
                    fastForward = true;
                }

                if (s.Contains("HEAD detached at"))
                {
                    detached = true;
                }
            }

            if (detached)
            {
                ProcessExecute(executablePath, checkoutFolder,
                    $"reset --hard", null, (System.Action<int, string>)((processIdentifier, line) =>
                    {
                        Console.WriteLine(line);
                    }));

                if (branch != null)
                {
                    ProcessExecute(executablePath, checkoutFolder,
                        $"switch {branch}", null, (System.Action<int, string>)((processIdentifier, line) =>
                        {
                            Console.WriteLine(line);
                        }));
                }

                ProcessExecute(executablePath, checkoutFolder,
                    "pull", null, (System.Action<int, string>)((processIdentifier, line) =>
                    {
                        Console.WriteLine(line);
                    }));

                if (commit != null)
                {
                    ProcessExecute(executablePath, checkoutFolder,
                        $"checkout {commit}", null, (System.Action<int, string>)((processIdentifier, line) =>
                        {
                            Console.WriteLine(line);
                        }));
                    Console.WriteLine($"{checkoutFolder} detached updated to {commit}.");
                }
                else
                {
                    Console.WriteLine($"{checkoutFolder} detached head reset to latest.");
                }
            }
            else if ((!fastForward && branchBehind) || forceUpdate)
            {
                // We actually need to do something to upgrade this repository
                Console.WriteLine($"{checkoutFolder} needs updating, resetting as it could not be cleanly updated.");

                ProcessExecute(executablePath, checkoutFolder,
                    "reset --hard", null, (System.Action<int, string>)((processIdentifier, line) =>
                    {
                        Console.WriteLine(line);
                    }));
                ProcessExecute(executablePath, checkoutFolder,
                    "pull", null, (System.Action<int, string>)((processIdentifier, line) =>
                    {
                        Console.WriteLine(line);
                    }));
            }
            else if (fastForward)
            {
                // We actually need to do something to upgrade this repository
                Console.WriteLine($"Fast-forwarding {checkoutFolder}.");

                ProcessExecute(executablePath, checkoutFolder,
                    "pull", null, (System.Action<int, string>)((processIdentifier, line) =>
                    {
                        Console.WriteLine(line);
                    }));
            }
            else
            {
                Console.WriteLine($"{checkoutFolder} is up-to-date.");
            }

            // Clear our cached output
            output.Clear();
        }
        #endregion

    }
}
