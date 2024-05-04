// Copyright Greathorn Games Inc. All Rights Reserved.

using System.Collections.Generic;

namespace Greathorn.Services.Perforce.Records
{
    public class ClientRecord
    {
        public string Host;
        public string Name;
        public string Owner;
        public string Root;
        public string Stream;

        public ClientRecord(Dictionary<string, string> tags)
        {
            tags.TryGetValue("client", out Name);
            tags.TryGetValue("Owner", out Owner);
            tags.TryGetValue("Host", out Host);
            tags.TryGetValue("Stream", out Stream);
            tags.TryGetValue("Root", out Root);
        }
    }
}