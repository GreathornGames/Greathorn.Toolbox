// Copyright Greathorn Games Inc. All Rights Reserved.

using System.Text.Json.Serialization;

namespace GG
{
    public class Commands
    {
        public class Verb
        {
            public class Action
            {
                [JsonPropertyName("action")]
                public required string Identifier;

                [JsonPropertyName("command")]
                public required string Command;

                [JsonPropertyName("description")]
                public required string Description;
            }

            [JsonPropertyName("verb")]
            public required string Identifier;

            [JsonPropertyName("actions")]
            public required List<Action> Actions;
        }

        [JsonPropertyName("verbs")]
        public required List<string> Verbs;
    }
}
