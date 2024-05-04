// Copyright Greathorn Games Inc. All Rights Reserved.

namespace Greathorn.Services.Perforce.Records
{
    public class DescribeFileRecord
    {
        public string? Action;
        public string? DepotFile;
        public string? Digest;
        public int FileSize;
        public int Revision;
        public string? Type;
    }
}