// Copyright Greathorn Games Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Greathorn.Services.Perforce.Records
{
    public class InfoRecord
    {
        public string ClientAddress;
        public string HostName;
        public TimeSpan ServerTimeZone;
        public string UserName;

        public InfoRecord(Dictionary<string, string> tags)
        {
            tags.TryGetValue("userName", out UserName);
            tags.TryGetValue("clientHost", out HostName);
            tags.TryGetValue("clientAddress", out ClientAddress);

            if (tags.TryGetValue("serverDate", out string serverDateTime))
            {
                string[] fields = serverDateTime.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (fields.Length >= 3)
                {
                    if (DateTimeOffset.TryParse(string.Join(" ", fields.Take(3)), out DateTimeOffset offset))
                    {
                        ServerTimeZone = offset.Offset;
                    }
                }
            }
        }
    }
}