using System;
using System.IO;
using System.Reflection;
using Greathorn.Core.Extensions;

namespace Greathorn.Services.Perforce
{
	public static class PerforceUtil
	{
        static readonly int k_CachedGenerateProjectFilesHash = "GenerateProjectFiles.bat".GetStableUpperCaseHashCode();
        static readonly int k_CachedSetupHash = "Setup.bat".GetStableUpperCaseHashCode();
        static string? s_CachedWorkspaceRoot = null;

        public static string? GetWorkspaceRoot(string? workingDirectory = null)
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
            for(int i = 0; i < localFileCount; i++)
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
            if(foundCount == 2)
            {
                s_CachedWorkspaceRoot = workingDirectory;
                return s_CachedWorkspaceRoot;
            }

            // Go back another directory
            DirectoryInfo parent = Directory.GetParent(workingDirectory);
            if (parent != null)
            {
                return GetWorkspaceRoot(parent.FullName);
            }
            return null;
        }

		public static bool IsValidTag(string line, int startIndex)
		{
			// Annoyingly, we sometimes get commentary with an info1: prefix. Since it typically starts with a depot or file path, we can pick it out.
			for (int index = startIndex; index < line.Length && line[index] != ' '; index++)
			{
				if (line[index] == '/' || line[index] == '\\')
				{
					return false;
				}
			}
			return true;
		}

		public static bool IgnoreCommandOutput(string text, PerforceProvider.CommandOptions options)
		{
			if (text.StartsWith("exit: ") || text.StartsWith("info2: ") || text.Length == 0)
			{
				return true;
			}

			if (options.HasFlag(PerforceProvider.CommandOptions.IgnoreFilesUpToDateError) && text.StartsWith("error: ") &&
				text.EndsWith("- file(s) up-to-date."))
			{
				return true;
			}

			if (options.HasFlag(PerforceProvider.CommandOptions.IgnoreNoSuchFilesError) && text.StartsWith("error: ") &&
				text.EndsWith(" - no such file(s)."))
			{
				return true;
			}

			if (options.HasFlag(PerforceProvider.CommandOptions.IgnoreFilesNotInClientViewError) && text.StartsWith("error: ") &&
				text.EndsWith("- file(s) not in client view."))
			{
				return true;
			}

			if (options.HasFlag(PerforceProvider.CommandOptions.IgnoreFilesNotOnClientError) && text.StartsWith("error: ") &&
				text.EndsWith("- file(s) not on client."))
			{
				return true;
			}

			if (options.HasFlag(PerforceProvider.CommandOptions.IgnoreFilesNotOpenedOnThisClientError) &&
				text.StartsWith("error: ") && text.EndsWith(" - file(s) not opened on this client."))
			{
				return true;
			}

			if (options.HasFlag(PerforceProvider.CommandOptions.IgnoreProtectedNamespaceError) && text.StartsWith("error: ") &&
				text.EndsWith(" - protected namespace - access denied."))
			{
				return true;
			}

			if (options.HasFlag(PerforceProvider.CommandOptions.IgnoreEnterPassword) && text.StartsWith("Enter password:"))
			{
				return true;
			}

			return false;
		}

		public static bool TryGetDepotName(string depotPath, out string? depotName)
		{
			return TryGetClientName(depotPath, out depotName);
		}

		public static bool TryGetClientName(string clientPath, out string? clientName)
		{
			if (!clientPath.StartsWith("//"))
			{
				clientName = null;
				return false;
			}

			int slashIndex = clientPath.IndexOf('/', 2);
			if (slashIndex == -1)
			{
				clientName = null;
				return false;
			}

			clientName = clientPath[2..slashIndex];
			return true;
		}

		public static string GetClientOrDepotDirectoryName(string clientFile)
		{
			int index = clientFile.LastIndexOf('/');
			if (index == -1)
			{
				return "";
			}

            return clientFile[..index];
		}

		public static string EscapePath(string path)
		{
			string newPath = path;
			newPath = newPath.Replace("%", "%25");
			newPath = newPath.Replace("*", "%2A");
			newPath = newPath.Replace("#", "%23");
			newPath = newPath.Replace("@", "%40");
			return newPath;
		}

		public static string UnescapePath(string path)
		{
			string newPath = path;
			newPath = newPath.Replace("%40", "@");
			newPath = newPath.Replace("%23", "#");
			newPath = newPath.Replace("%2A", "*");
			newPath = newPath.Replace("%2a", "*");
			newPath = newPath.Replace("%25", "%");
			return newPath;
		}

		public static bool ParseCommandOutput(string text, PerforceProvider.HandleOutputDelegate handleOutput,
            PerforceProvider.CommandOptions options)
        {
            if (options.HasFlag(PerforceProvider.CommandOptions.NoChannels))
            {
                OutputLine line = new OutputLine(OutputLine.OutputChannel.Unknown, text);
                return handleOutput(line);
            }

            if (!IgnoreCommandOutput(text, options))
            {
                OutputLine line;
                if (text.StartsWith("text: "))
                {
                    line = new OutputLine(OutputLine.OutputChannel.Text, text[6..]);
                }
                else if (text.StartsWith("info: "))
                {
                    line = new OutputLine(OutputLine.OutputChannel.Info, text[6..]);
                }
                else if (text.StartsWith("info1: "))
                {
                    line = new OutputLine(IsValidTag(text, 7) ? OutputLine.OutputChannel.TaggedInfo : OutputLine.OutputChannel.Info,
                        text[7..]);
                }
                else if (text.StartsWith("warning: "))
                {
                    line = new OutputLine(OutputLine.OutputChannel.Warning, text[9..]);
                }
                else if (text.StartsWith("error: "))
                {
                    line = new OutputLine(OutputLine.OutputChannel.Error, text[7..]);
                }
                else
                {
                    line = new OutputLine(OutputLine.OutputChannel.Unknown, text);
                }

                return handleOutput(line) &&
                       (line.Channel != OutputLine.OutputChannel.Error || options.HasFlag(PerforceProvider.CommandOptions.NoFailOnErrors)) &&
                       line.Channel != OutputLine.OutputChannel.Unknown;
            }

            return true;
        }
    }
}
