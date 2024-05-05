// Copyright Greathorn Games Inc. All Rights Reserved.

using System.Reflection;

namespace Greathorn
{
    class Bootstrap
    {
        static bool s_QuietMode = false;
        static bool s_ShouldBuild = true;
        static bool s_ShouldSetupWorkspace = true;

        static string? s_WorkspaceRoot;

        static void Main(string[] args)
        {
			try
			{
				Assembly? assembly = Assembly.GetAssembly(typeof(Bootstrap));

				if (assembly != null)
					Console.WriteLine($"Greathorn Bootstrap {assembly.GetName().Version}");

				// Ensure we have a builder
				Microsoft.Build.Locator.VisualStudioInstance visualStudioInstance = Microsoft.Build.Locator.MSBuildLocator.QueryVisualStudioInstances().OrderByDescending(instance => instance.Version).First();
				if (visualStudioInstance == null)
				{
					Console.WriteLine("Unable to find an installation of MSBuild.\nYou need to install .NET SDK found at https://dotnet.microsoft.com/en-us/download/dotnet/8.0");
					Environment.ExitCode = 1;
					PressAnyKeyToContinue();
					return;
				}
				Console.WriteLine($"Using MSBuild @ {visualStudioInstance.MSBuildPath}");

	

				// Find the workspace root

				s_WorkspaceRoot = Helpers.GetWorkspaceRoot();
				if (s_WorkspaceRoot == null)
				{
					Console.WriteLine("Unable to find workspace root.");
					Environment.ExitCode = 2;
					PressAnyKeyToContinue();
					return;
				}
				Console.WriteLine($"Workspace Root @ {s_WorkspaceRoot}");

                ParseArguments(args);

                Clone();
				Build();
				SetupWorkspace();
			}
			catch(Exception ex)
			{
				Console.WriteLine("EXCEPTION");
				Console.WriteLine(ex);
				Environment.ExitCode = ex.HResult;
			}
            PressAnyKeyToContinue();
        }

        static void PressAnyKeyToContinue()
        {
            if (s_QuietMode) return;

            Console.WriteLine("Press Any Key To Continue ...");
            Console.ReadKey();
        }

        static void ParseArguments(string[] arguments)
        {
            int count = arguments.Length;

            for (int i = 0; i < count; i++)
            {
                if (arguments[i] == "no-workspace")
                {
                    s_ShouldSetupWorkspace = false;
                }

                if (arguments[i] == "no-build")
                {
                    s_ShouldBuild = false;
                }
                if (arguments[i] == "quiet")
                {
                    s_QuietMode = true;
                }
            }
        }

        static void Clone()
        {
            if (s_WorkspaceRoot == null)
            {
                return;
            }
            string cliSource = Path.Combine(s_WorkspaceRoot, "Greathorn", "Source", "Programs");
            if (!string.IsNullOrEmpty(cliSource) && !Directory.Exists(cliSource))
            {
                Directory.CreateDirectory(cliSource);
            }
            string checkoutPath = Path.Combine(cliSource, "Greathorn.CLI");
            if (Directory.Exists(Path.Combine(checkoutPath, ".git")))
            {
                Helpers.UpdateRepo(checkoutPath);
            }
            else
            {
                Helpers.CheckoutRepo("https://github.com/GreathornGames/Greathorn.CLI.git", checkoutPath);
            }
        }

        static void Build()
        {
            if (!s_ShouldBuild || s_WorkspaceRoot == null) return;

            string programsFolder = Path.Combine(s_WorkspaceRoot, "Greathorn", "Source", "Programs", "Greathorn.CLI");
            string sharedFolder = Path.Combine(programsFolder, "Shared");

            Console.WriteLine($"Programs Folder @ {programsFolder}");

            // Find all projects, exclude Shared and Bootstrap
            string[] projectFiles = Directory.GetFiles(programsFolder, "*.csproj", SearchOption.AllDirectories);
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

            for (int i = 0; i < compileCount; i++)
            {
                Console.WriteLine($"Building {parsedFiles[i]} ...");
                Helpers.Execute("dotnet", s_WorkspaceRoot, $"build {parsedFiles[i]} /property:Configuration=Release /property:Platform=AnyCPU /t:Rebuild", null, (processIdentifier, line) =>
                {
                    Console.WriteLine($"[{processIdentifier}]\t{line}");
                });
            }
        }
        static void SetupWorkspace()
        {
            if (!s_ShouldSetupWorkspace || s_WorkspaceRoot == null) return;

            // We need to run this process elevated, the main executable is bundled to ensure its elevated, but the library is not.
            string args = Path.Combine(s_WorkspaceRoot, "Greathorn", "Binaries", "DotNET", "WorkspaceSetup.dll");
            if(s_QuietMode)
            {
                args += " quiet";
            }
            Helpers.Elevate("dotnet", s_WorkspaceRoot, args);
        }
    }
}
