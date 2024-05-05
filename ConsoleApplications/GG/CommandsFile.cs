// Copyright Greathorn Games Inc. All Rights Reserved.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace GG
{   
    public class CommandsFile
    {
        public const string Extension = ".gg.json";

        [JsonPropertyName("actions")]
        public CommandAction[] Actions { get; set; }

        public class CommandAction
        {
            [JsonPropertyName("verb")]
            public string? Identifier { get; set;}

            [JsonPropertyName("command")]
            public string? Command { get; set; }

            [JsonPropertyName("description")]
            public string? Description { get; set; }

            [JsonPropertyName("actions")]
            public CommandAction[]? Actions { get; set; }
        }


        public string ToJson()
        {
            return JsonSerializer.Serialize<CommandsFile>(this);
        }

        public static CommandsFile? Get(string path)
        {
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<CommandsFile>(json);
        }

        public static void BuildMap(CommandAction[] actionBase, Dictionary<string, CommandAction> data)
        {
            data ??= [];

            // TODO This doesnt do recruseive we need to pass the action to add too ? 
            foreach(CommandAction action in actionBase) {
                data[action.Identifier] = action;
                if(action.Actions != null && action.Actions.Length > 0)
                {
                    BuildMap(action.Actions, data);
                }
            }
        }
    }
}
