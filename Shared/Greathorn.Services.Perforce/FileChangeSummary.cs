// Copyright Greathorn Games Inc. All Rights Reserved.

using System;

namespace Greathorn.Services.Perforce
{
    public class FileChangeSummary
    {
        public string? Action;
        public int ChangeNumber;
        public string? Client;
        public DateTime Date;
        public string? Description;
        public int Revision;
        public string? Type;
        public string? User;
    }
}