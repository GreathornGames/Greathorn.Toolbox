// Copyright Greathorn Games Inc. All Rights Reserved.

using System.Collections.Generic;

namespace Greathorn.Services.Perforce.Records
{
    public class StreamRecord
    {
        public string Identifier;
        public string Name;
        public string? Parent;

        public StreamRecord(Dictionary<string, string> tags)
        {
            tags.TryGetValue("Stream", out Identifier);
            tags.TryGetValue("Name", out Name);
            if (tags.TryGetValue("Parent", out Parent) && Parent == "none")
            {
                Parent = null;
            }
        }
    }
}