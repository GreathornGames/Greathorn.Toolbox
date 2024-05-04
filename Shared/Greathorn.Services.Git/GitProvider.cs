// Copyright Greathorn Games Inc. All Rights Reserved.

using Greathorn.Core.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Greathorn.Core.Services.Git
{
	public static class GitProvider
	{
		public static string GetExecutablePath()
		{
			if (Environment.OSVersion.Platform == PlatformID.Win32NT)
			{
				return "git.exe";
			}
			else
			{
				return "git";
			}
		}

		public static string? GetRemote(string checkoutFolder)
		{
			string? output = null;
			ProcessUtil.Execute(GetExecutablePath(), checkoutFolder,
				"status -sb", null, (processIdentifier, line) =>
				{
					if (!string.IsNullOrEmpty(line))
					{
						output = line;
					}
				});

			if (!string.IsNullOrEmpty(output))
			{
				string[] parts = output.Split(' ')[1].Split("...");
				return parts[1];
			}
			return null;
		}
		public static string? GetBranch(string checkoutFolder)
		{
			string? output = null;
			ProcessUtil.Execute(GetExecutablePath(), checkoutFolder,
				"status -sb", null, (processIdentifier, line) =>
				{
					if (!string.IsNullOrEmpty(line))
					{
						output = line;
					}
				});

			if (!string.IsNullOrEmpty(output))
			{
				string[] parts = output.Split(' ')[1].Split("...");
				return parts[0];
			}
			return null;
		}

		public static string GetLocalCommit(string checkoutFolder)
		{
			// Check current
			List<string> output = new List<string>();

			// Get status of the repository
			ProcessUtil.Execute(GetExecutablePath(), checkoutFolder,
				$"rev-parse HEAD", null, (System.Action<int, string>)((processIdentifier, line) =>
				{
					Core.Log.WriteLine(line, "GIT", ILogOutput.LogType.ExternalProcess);
					output.Add(line);
				}));

			return output[0].Trim();
		}

		public static void CheckoutRepo(string uri, string checkoutFolder, string? branch = null, string? commit = null, int depth = -1, bool submodules = true, bool shallowsubmodules = true)
		{
			string executablePath = GetExecutablePath();
			StringBuilder commandLineBuilder = new StringBuilder();
			commandLineBuilder.Append("clone ");

			// Was a branch defined?
			if (branch != null)
			{
				commandLineBuilder.AppendFormat("--branch {0} --single-branch ", branch);
			}

			// Do we have a specific depth to go too?
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

			Core.Log.WriteLine($"{commandLineBuilder}{uri} {checkoutFolder}", "GIT");
			ProcessUtil.Execute(executablePath, Directory.GetParent(checkoutFolder).GetPathWithCorrectCase(),
				$"{commandLineBuilder}{uri} {checkoutFolder}", null, (System.Action<int, string>)((processIdentifier, line) =>
				{
					Core.Log.WriteLine(line, "GIT", ILogOutput.LogType.ExternalProcess);
				}));

			// Was a commit specified?
			if (commit != null)
			{
				Core.Log.WriteLine($"Checkout Commit {commit}", "GIT", ILogOutput.LogType.ExternalProcess);
				ProcessUtil.Execute(executablePath, checkoutFolder,
					$"checkout {commit}", null, (System.Action<int, string>)((processIdentifier, line) =>
					{
						Core.Log.WriteLine(line, "GIT", ILogOutput.LogType.ExternalProcess);
					}));
			}
		}

		public static void InitializeSubmodules(string checkoutFolder) //, int depth = -1)
		{
			StringBuilder commandLineBuilder = new StringBuilder();
			commandLineBuilder.Append("submodule init ");

			// if (depth != -1)
			// {
			//     commandLineBuilder.AppendFormat("--depth {0} ", depth.ToString());
			// }
			// if (depth == 1)
			// {
			//     commandLineBuilder.Append("--shallow-submodules ");
			// }

			Core.Log.WriteLine("Initialize Submodules", "GIT", ILogOutput.LogType.ExternalProcess);
			ProcessUtil.Execute(GetExecutablePath(), checkoutFolder,
				commandLineBuilder.ToString(), null, (System.Action<int, string>)((processIdentifier, line) =>
				{
					Core.Log.WriteLine(line, "GIT", ILogOutput.LogType.ExternalProcess);
				}));
		}
		public static void UpdateSubmodule(string checkoutFolder, int depth = -1, string? submodule = null)
		{
			StringBuilder commandLineBuilder = new StringBuilder();
			commandLineBuilder.Append("submodule update ");
			if (depth == 1)
			{
				commandLineBuilder.Append("--recommend-shallow ");
			}
			commandLineBuilder.Append("--remote ");

			if (!string.IsNullOrEmpty(submodule))
			{
				commandLineBuilder.Append(submodule);
			}

			Core.Log.WriteLine(submodule != null ? $"Update Submodule [{submodule}]" : $"Update Submodule", "GIT",
				ILogOutput.LogType.ExternalProcess);

			ProcessUtil.Execute(GetExecutablePath(), checkoutFolder,
				commandLineBuilder.ToString().Trim(), null, (System.Action<int, string>)((processIdentifier, line) =>
				{
					Core.Log.WriteLine(line, "GIT", ILogOutput.LogType.ExternalProcess);
				}));
		}

		public static void UpdateRepo(string checkoutFolder, string? branch = null, string? commit = null, bool forceUpdate = true)
		{
			string executablePath = GetExecutablePath();
			// Check current
			List<string> output = new List<string>();

			// Get status of the repository
			ProcessUtil.Execute(executablePath, checkoutFolder,
				$"fetch origin", null, (System.Action<int, string>)((processIdentifier, line) =>
				{
					Core.Log.WriteLine(line, "GIT", ILogOutput.LogType.ExternalProcess);
				}));
			ProcessUtil.Execute(executablePath, checkoutFolder,
				"status -uno --long", null, (System.Action<int, string>)((processIdentifier, line) =>
				{
					Core.Log.WriteLine(line, "GIT", ILogOutput.LogType.ExternalProcess);
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
				ProcessUtil.Execute(executablePath, checkoutFolder,
					$"reset --hard", null, (System.Action<int, string>)((processIdentifier, line) =>
					{
						Core.Log.WriteLine(line, "GIT");
					}));

				if (branch != null)
				{
					ProcessUtil.Execute(executablePath, checkoutFolder,
						$"switch {branch}", null, (System.Action<int, string>)((processIdentifier, line) =>
						{
							Core.Log.WriteLine(line, "GIT");
						}));
				}

				ProcessUtil.Execute(executablePath, checkoutFolder,
					"pull", null, (System.Action<int, string>)((processIdentifier, line) =>
					{
						Core.Log.WriteLine(line, "GIT");
					}));

				if (commit != null)
				{
					ProcessUtil.Execute(executablePath, checkoutFolder,
						$"checkout {commit}", null, (System.Action<int, string>)((processIdentifier, line) =>
						{
							Core.Log.WriteLine(line, "GIT");
						}));
					Core.Log.WriteLine($"{checkoutFolder} detached updated to {commit}.", "CHECKOUT");
				}
				else
				{
					Core.Log.WriteLine($"{checkoutFolder} detached head reset to latest.", "CHECKOUT");
				}
			}
			else if ((!fastForward && branchBehind) || forceUpdate)
			{
				// We actually need to do something to upgrade this repository
				Core.Log.WriteLine($"{checkoutFolder} needs updating, resetting as it could not be cleanly updated.", "CHECKOUT");

				ProcessUtil.Execute(executablePath, checkoutFolder,
					"reset --hard", null, (System.Action<int, string>)((processIdentifier, line) =>
					{
						Core.Log.WriteLine(line, "GIT");
					}));
				ProcessUtil.Execute(executablePath, checkoutFolder,
					"pull", null, (System.Action<int, string>)((processIdentifier, line) =>
					{
						Core.Log.WriteLine(line, "GIT");
					}));
			}
			else if (fastForward)
			{
				// We actually need to do something to upgrade this repository
				Core.Log.WriteLine($"Fast-forwarding {checkoutFolder}.", "CHECKOUT");

				ProcessUtil.Execute(executablePath, checkoutFolder,
					"pull", null, (System.Action<int, string>)((processIdentifier, line) =>
					{
						Core.Log.WriteLine(line, "GIT");
					}));
			}
			else
			{
				Core.Log.WriteLine($"{checkoutFolder} is up-to-date.", "CHECKOUT");
			}

			// Clear our cached output
			output.Clear();
		}

	}
}