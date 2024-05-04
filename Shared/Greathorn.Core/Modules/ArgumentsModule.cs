// Copyright Greathorn Games Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Greathorn.Core.Modules
{	public class ArgumentsModule : IModule
	{
		private const string LogCategory = "ARGS";
       
		public readonly List<string> Arguments = new List<string>();
		public readonly Dictionary<string, string> OverrideArguments = new Dictionary<string, string>();

		public ArgumentsModule()
		{
			// Clean Arguments
			string[] args = Environment.GetCommandLineArgs();

			foreach (string arg in args)
			{
				// Quoted Argument that do not need to be
				if (arg.StartsWith("\"") && arg.EndsWith("\"") && !arg.Contains(' '))
				{
					Arguments.Add(arg[1..^1]);
				}
				else
				{
					Arguments.Add(arg);
				}
			}

			// Look for arguments that come in that need to be spliced into one with a quote
			int argCount = Arguments.Count - 1;
			List<int> removeIndices = new List<int>();
			if (argCount >= 2)
			{
				for (int i = 0; i < argCount; i++)
				{
					if (Arguments[i].StartsWith("\"") && !Arguments[i].EndsWith("\""))
					{
						StringBuilder newArg = new StringBuilder();
						newArg.Append(Arguments[i]);
						for (int j = i + 1; j < argCount; j++)
						{
							newArg.Append(' ');
							newArg.Append(Arguments[j]);
							removeIndices.Add(j);
							if (!Arguments[j].StartsWith("\"") && Arguments[j].EndsWith("\""))
							{
								Arguments[i] = newArg.ToString();
								i = j;
								break;
							}
						}
					}
				}
				// Post remove
				foreach (int i in removeIndices)
				{
					Arguments.RemoveAt(i);
				}
			}
		}

		public void Init(AssemblyModule assemblyModule)
		{
			// Check if first argument is actually passing in a DLL
			string fakePath = Path.GetFullPath(Arguments[0]);
			
			if (File.Exists(fakePath) && fakePath.EndsWith(".dll") && assemblyModule.AssemblyPath == fakePath)
			{
				Arguments.RemoveAt(0);
			}

			// Handle some possible trickery with our command lines
			OverrideArguments.Clear();
			for (int i = Arguments.Count - 1; i >= 0; i--)
			{
				string arg = Arguments[i];

				// Our parser will only work with arguments that comply with the ---ARG=VALUE format
				if (arg.StartsWith("---"))
				{
					if (arg.Contains("="))
					{
						int split = arg.IndexOf('=');
						OverrideArguments.Add(arg[3..split].ToUpper(), arg[(split + 1)..]);
					}
					else
					{
						OverrideArguments.Add(arg[3..].ToUpper(), "T");
					}

					// Take it out of the normal argument list
					Arguments.RemoveAt(i);
				}
			}

			if (Arguments.Count > 0)
			{
				Core.Log.WriteLine("Arguments:", LogCategory, ILogOutput.LogType.Info);
				foreach (string s in Arguments)
				{
					Core.Log.WriteLine($"\t{s}", LogCategory, ILogOutput.LogType.Info);
				}
			}

			if (OverrideArguments.Count > 0)
			{
				Core.Log.WriteLine("Override Arguments:", LogCategory, ILogOutput.LogType.Info);
				foreach (KeyValuePair<string, string> pair in OverrideArguments)
				{
					Core.Log.WriteLine($"\t{pair.Key}={pair.Value}", LogCategory, ILogOutput.LogType.Info);
				}
			}
		}

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            int arguementCount = Arguments.Count;
            for(int i = 0; i < arguementCount; i++)
            {
                builder.Append($"{Arguments[i]} ");
            }

            foreach(KeyValuePair<string,string> pair in OverrideArguments)
            {
                builder.Append($"---{pair.Key}=\"{pair.Value}\" ");
            }

            return builder.ToString().Trim();
        }
    }
}
