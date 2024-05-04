// Copyright Greathorn Games Inc. All Rights Reserved.

using System;
using System.Collections.Generic;

namespace Greathorn.Services.Perforce.Records
{
    public class FileRecord
    {
        [Flags]
        public enum FileFlags
        {
            ModTime = 1,
            AlwaysWritable = 2,
            Executable = 4,
            ExclusiveCheckout = 8
        }

        public string Action;
        public string ClientPath;
        public string DepotPath;
        public string Digest;
        public FileFlags Flags;
        public bool IsMapped;
        public string Path;
        public int Revision;
        public bool Unmap;

        public FileRecord(Dictionary<string, string> tags)
        {
            tags.TryGetValue("depotFile", out DepotPath);
            tags.TryGetValue("clientFile", out ClientPath);
            tags.TryGetValue("path", out Path);
            if (!tags.TryGetValue("action", out Action))
            {
                tags.TryGetValue("headAction", out Action);
            }

            if (tags.TryGetValue("headType", out string type))
            {
                int attributeIndex = type.IndexOf('+');
                if (attributeIndex != -1)
                {
                    for (int index = attributeIndex + 1; index < type.Length; index++)
                    {
                        switch (type[index])
                        {
                            case 'm':
                                Flags |= FileFlags.ModTime;
                                break;
                            case 'w':
                                Flags |= FileFlags.AlwaysWritable;
                                break;
                            case 'x':
                                Flags |= FileFlags.Executable;
                                break;
                            case 'l':
                                Flags |= FileFlags.ExclusiveCheckout;
                                break;
                        }
                    }
                }
            }

            IsMapped = tags.ContainsKey("isMapped");
            Unmap = tags.ContainsKey("unmap");

            if (tags.TryGetValue("rev", out string revisionString))
            {
                int.TryParse(revisionString, out Revision);
            }

            tags.TryGetValue("digest", out Digest);
        }
    }
}