// Copyright Greathorn Games Inc. All Rights Reserved.

using System.Text.Json.Serialization;

namespace GG
{   
    public class CommandsFile
    {
        public const string Extension = "gg.json";

        [JsonPropertyName("actions")]
        public required Action[] Actions;

        public class Action
        {
            [JsonPropertyName("verb")]
            public required string Identifier;

            [JsonPropertyName("command")]
            public string? Command;

            [JsonPropertyName("description")]
            public string? Description;

            [JsonPropertyName("actions")]
            public Action[]? Actions;
        }

        public static void AddActions(Action[] actionBase, Dictionary<string, Action> data)
        {
            data ??= [];

            // TODO This doesnt do recruseive we need to pass the action to add too ? 
            foreach(Action action in actionBase) {
                data[action.Identifier] = action;
                if(action.Actions != null && action.Actions.Length > 0)
                {
                    AddActions(action.Actions, data);
                }
            }
        }
    }
}
