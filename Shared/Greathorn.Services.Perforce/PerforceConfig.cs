// Copyright Greathorn Games Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Greathorn.Core.Utils;
using Microsoft.Extensions.Configuration;

namespace Greathorn.Services.Perforce
{
    public class PerforceConfig
    {
        public const int MaxParallelConnections = 4;

        private readonly string m_Client;
        private readonly string m_Port;
        private readonly string m_Username;

        public static void WriteDefault(string path, string defaultPort, string characterSet, string p4ignore)
        {

            StringBuilder builder = new StringBuilder();

            string usernameDefault = "[USERNAME]";
            string clientDefault = "[CLIENT]";

            // Check for environment set things
            string? envP4Client = Environment.GetEnvironmentVariable("P4CLIENT");
            if (!string.IsNullOrEmpty(envP4Client))
            {
                clientDefault = envP4Client.Trim();
            }
            string? envP4User = Environment.GetEnvironmentVariable("P4USER");
            if (!string.IsNullOrEmpty(envP4User))
            {
                usernameDefault = envP4User.Trim();
            }

            // Check for defaults p4 might already know
            ProcessUtil.Execute("p4", Path.GetDirectoryName(path), "info", null, out List<string> returnLines);
            int returnLinesCount = returnLines.Count;
            for (int i = 0; i < returnLinesCount; i++)
            {
                string line = returnLines[i].Trim();
                if (line.StartsWith("Client name: "))
                {
                    clientDefault = line.Replace("Client name: ", string.Empty).Trim();
                }

                if (line.StartsWith("User name: "))
                {
                    usernameDefault = line.Replace("User name: ", string.Empty).Trim();
                }
            }

            builder.AppendLine("# P4CONFIG");
            builder.AppendLine("# See https://greathorn.youtrack.cloud/articles/OPS-A-22/p4config.txt for more information!");
            builder.AppendLine("#");
            builder.AppendLine("# This is the username that you use to connect to our Perforce server.");
            builder.AppendLine($"P4USER={usernameDefault}");
            builder.AppendLine("#");
            builder.AppendLine("# This is the full workspace name that you created previously for this depot (probably not what this default says!)");
            builder.AppendLine($"P4CLIENT={clientDefault}");
            builder.AppendLine("#");
            builder.AppendLine("# This is the hostname and port of our Perforce server, it is unlikely that you will need to change this.");
            builder.AppendLine($"P4PORT={defaultPort}");
            builder.AppendLine("#");
            builder.AppendLine("## DO NOT EDIT BELOW THIS LINE ###");
            builder.AppendLine($"P4CHARSET={characterSet}");
            builder.AppendLine($"P4IGNORE={p4ignore}");

            File.WriteAllText(path, builder.ToString());
        }

        public PerforceConfig(string path)
        {

            // TODO: Check for environment based?
            if (File.Exists(path))
            {
                Core.Log.WriteLine("P4Config found at " + path, PerforceProvider.LogCategory);

                IConfiguration config = new ConfigurationBuilder()
                    .AddIniFile(path)
                    .Build();

                string? configUser = config["P4USER"];
                if (configUser != null)
                {
                    m_Username = configUser;
                }
                else
                {
                    m_Username = string.Empty;
                }

                string? configClient = config["P4CLIENT"];
                if (configClient != null)
                {
                    m_Client = configClient;
                }

                string? configPort = config["P4PORT"];
                if (configPort != null)
                {
                    m_Port = configPort;
                }
            }

            // Failsafe resets
            m_Username ??= string.Empty;
            m_Client ??= string.Empty;
            m_Port ??= string.Empty;

            Output();
        }

        public string Username => m_Username;

        public string Port => m_Port;

        public string Client => m_Client;

        private void Output()
        {
            if (!string.IsNullOrEmpty(Username))
            {
                Core.Log.WriteLine("\tP4USER: " + Username, PerforceProvider.LogCategory);
            }

            if (!string.IsNullOrEmpty(Port))
            {
                Core.Log.WriteLine("\tP4PORT: " + Port, PerforceProvider.LogCategory);
            }

            if (!string.IsNullOrEmpty(Client))
            {
                Core.Log.WriteLine("\tP4CLIENT: " + Client, PerforceProvider.LogCategory);
            }
        }
    }
}