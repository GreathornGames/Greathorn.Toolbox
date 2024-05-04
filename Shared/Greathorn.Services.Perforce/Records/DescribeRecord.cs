// Copyright Greathorn Games Inc. All Rights Reserved.

using System.Collections.Generic;

namespace Greathorn.Services.Perforce.Records
{
    public class DescribeRecord
    {
        public int ChangeNumber;
        public string ChangeType;
        public string Client;
        public string Description;
        public List<DescribeFileRecord> Files = new List<DescribeFileRecord>();
        public string Path;
        public string Status;
        public long Time;
        public string User;

        public DescribeRecord(Dictionary<string, string> tags)
        {
            if (!tags.TryGetValue("change", out string changeString) || !int.TryParse(changeString, out ChangeNumber))
            {
                ChangeNumber = -1;
            }

            tags.TryGetValue("user", out User);
            tags.TryGetValue("client", out Client);

            if (!tags.TryGetValue("time", out string timeString) || !long.TryParse(timeString, out Time))
            {
                Time = -1;
            }

            tags.TryGetValue("desc", out Description);
            tags.TryGetValue("status", out Status);
            tags.TryGetValue("changeType", out ChangeType);
            tags.TryGetValue("path", out Path);

            for (int index = 0;; index++)
            {
                string suffix = string.Format("{0}", index);

                DescribeFileRecord file = new DescribeFileRecord();
                if (!tags.TryGetValue("depotFile" + suffix, out file.DepotFile))
                {
                    break;
                }

                tags.TryGetValue("action" + suffix, out file.Action);
                tags.TryGetValue("type" + suffix, out file.Type);

                if (!tags.TryGetValue("rev" + suffix, out string revisionString) ||
                    !int.TryParse(revisionString, out file.Revision))
                {
                    file.Revision = -1;
                }

                if (!tags.TryGetValue("fileSize" + suffix, out string fileSizeString) ||
                    !int.TryParse(fileSizeString, out file.FileSize))
                {
                    file.FileSize = -1;
                }

                tags.TryGetValue("digest" + suffix, out file.Digest);
                Files.Add(file);
            }
        }
    }
}