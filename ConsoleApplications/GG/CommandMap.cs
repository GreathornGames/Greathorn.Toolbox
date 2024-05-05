// Copyright Greathorn Games Inc. All Rights Reserved.

using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using Greathorn.Core;

namespace GG
{
    public class CommandMap
    {
        Dictionary<string, CommandMapAction> m_Map = new Dictionary<string, CommandMapAction>();

        public class CommandMapAction
        {
            public string? Command;
            public string? Description;
            public string? WorkingDirectory;
            public string? Arguments;

            public Dictionary<string, CommandMapAction> Children = new Dictionary<string, CommandMapAction>();

            public CommandMapAction(Commands.CommandVerb action)
            {
                Command = action.Command;
                Description = action.Description;
                WorkingDirectory = action.WorkingDirectory;
                Arguments = action.Arguments;

                if(action.Actions != null && action.Actions.Length > 0)
                {
                    int count = action.Actions.Length;
            
                    for(int i = 0; i < count; i++)
                    {
                        Commands.CommandVerb childAction = action.Actions[i];
                        if (childAction.Identifier != null)
                        {
                            if (!Children.ContainsKey(childAction.Identifier))
                            {
                               Children.Add(childAction.Identifier, new CommandMapAction(childAction));
                            }
                            else
                            {
                                Children[childAction.Identifier].Append(childAction);
                            }
                        }
                    }
                }
            }
            public void Append(Commands.CommandVerb action)
            {
                Command = action.Command;
                Description = action.Description;
                WorkingDirectory = action.WorkingDirectory;
                Arguments = action.Arguments;

                if (action.Actions != null && action.Actions.Length > 0)
                {
                    int count = action.Actions.Length;

                    for (int i = 0; i < count; i++)
                    {
                        Commands.CommandVerb childAction = action.Actions[i];
                        if (childAction.Identifier != null)
                        {
                            if (!Children.ContainsKey(childAction.Identifier))
                            {
                                Children.Add(childAction.Identifier, new CommandMapAction(childAction));
                            }
                            else
                            {
                                Children[childAction.Identifier].Append(childAction);
                            }
                        }
                    }
                }
            }

            public void AddHelpCommands(List<HelpCommand> commands, string prefix = "")
            {               
                if (!string.IsNullOrEmpty(Command))
                {
                    commands.Add(new HelpCommand(prefix, Description));
                }

                if(Children.Count > 0)
                {
                    foreach(KeyValuePair<string, CommandMapAction> kvp in Children)
                    {
                        kvp.Value.AddHelpCommands(commands, $"{prefix} {kvp.Key}");
                    }                                
                }
            }
        }

        public void AddCommands(Commands commands)
        {
            int count = commands.Actions.Length;
            for (int i = 0; i < count; i++)
            {
                Commands.CommandVerb action = commands.Actions[i];
                if(action.Identifier != null)
                {
                    if (!m_Map.ContainsKey(action.Identifier))
                    {
                        m_Map.Add(action.Identifier, new CommandMapAction(action));
                    }
                    else
                    {
                        m_Map[action.Identifier].Append(action);
                    }
                }            
            }
        }

        public CommandMapAction? GetAction(string query)
        {
            string[] parts = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            int partCount = parts.Length;
            int depth = 0;

            // Early out
            if (partCount == 1)
            {
                if (m_Map.ContainsKey(parts[0]))
                {
                    return m_Map[parts[0]];
                }
                else
                {
                    return null;
                }
            }

            // Find right base
            CommandMapAction? currentActionMap = null;
            if (m_Map.ContainsKey(parts[0]))
            {
                currentActionMap = m_Map[parts[0]];
            }
            if (currentActionMap == null) return null;
            depth++;

            while (depth < partCount)
            {
                if(currentActionMap.Children.ContainsKey(parts[depth]))
                {
                    currentActionMap = currentActionMap.Children[parts[depth]];
                    depth++;
                }

                if(depth == partCount)
                {
                    return currentActionMap;
                }
            }

            return null;
        }

        public string GetOutput()
        {
            List<HelpCommand> commands = new List<HelpCommand>();
            foreach(KeyValuePair<string, CommandMapAction> kvp in m_Map)
            {
                // Odd case where a top level command exists
                if (kvp.Value.Command != null)
                {
                    commands.Add(new HelpCommand(kvp.Key, kvp.Value.Description));
                   
                }               

                if(kvp.Value.Children.Count > 0)
                {
                    kvp.Value.AddHelpCommands(commands, kvp.Key);
                }
            }

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("GG Registered Actions");
            builder.AppendLine();

            int helpCount = commands.Count;
            int lhsCharacterCount = 0;
            for(int i = 0; i < helpCount; i++)
            {
                if(commands[i].Command.Length > lhsCharacterCount)
                {
                    lhsCharacterCount = commands[i].Command.Length;
                }
            }

            int padding = lhsCharacterCount + 5;
            for(int i = 0; i < helpCount; i++)
            {
                builder.AppendLine($"{commands[i].Command.PadRight(padding)}{commands[i].Description}");
            }


            return builder.ToString();
        }

        public struct HelpCommand
        {
            public string Command;
            public string? Description;

            public HelpCommand(string command, string? description)
            {
                Command = command;
                Description = description;
            }
        }
    }
}