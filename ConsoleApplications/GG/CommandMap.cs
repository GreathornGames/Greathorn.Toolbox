// Copyright Greathorn Games Inc. All Rights Reserved.

using System;
using System.Runtime.InteropServices.Marshalling;
using System.Text;

namespace GG
{
    public class CommandMap
    {
        Dictionary<string, CommandMapAction> m_Map = new Dictionary<string, CommandMapAction>();

        public class CommandMapAction
        {
            public string? Command;
            public string? Description;
            public Dictionary<string, CommandMapAction> Children = new Dictionary<string, CommandMapAction>();

            public CommandMapAction(Commands.CommandVerb action)
            {
                Command = action.Command;
                Description = action.Description;

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
            StringBuilder builder = new StringBuilder();


            builder.AppendLine("GG Registered Actions");

            return builder.ToString();
        }
    }
}
