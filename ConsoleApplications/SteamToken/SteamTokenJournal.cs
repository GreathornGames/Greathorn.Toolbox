// Copyright Greathorn Games Inc. All Rights Reserved.

using System.Text.Json;
using Greathorn.Core;
using Greathorn.Core.IO;

namespace SteamToken
{
    [Serializable]
    public class SteamTokenJournal
    {
        [Serializable]
        public class SteamUploader
        {
            public bool InUse = false;
            public string? Username;
            public string? TokenFileName;
        }

        public SteamUploader[] Accounts = new SteamUploader[0];

        FileLock? m_LockFile;

        string? m_BaseFolder;
        string? m_TokenFolder;
        string? m_CacheFolder;

        public bool IsValid()
        {
            return m_LockFile != null &&
                m_LockFile.HasLock() &&
                m_BaseFolder != null &&
                m_TokenFolder != null &&
                m_CacheFolder != null;
        }


        public bool TryGetTokenFile(out string tokenFilePath)
        {
            int tokenCount = Accounts.Length;
            tokenFilePath = "";

            //for (int i = 0; i < tokenCount; i++)
            //{
            //    if (Accounts[i].Available)
            //    {
            //        // Check out
            //        Accounts[i].Available = false;
            //        return TokenFile


            //        // Lock file
            //        using (FileLock tokenLock = new FileLock()
            //                journal.Output(journalLock.FilePath);
            //    }
            //}

            return false;
        }

        public bool Save()
        {
            if (m_LockFile != null)
            {
                return m_LockFile.WriteAllTextToLockedFile(JsonSerializer.Serialize<SteamTokenJournal>(this));
            }
            return false;
        }

        public static SteamTokenJournal? Get(FileLock lockFile)
        {
            string? content = lockFile.ReadAllTextFromLockedFile();
            if (content != null)
            {
                SteamTokenJournal? returnValue = JsonSerializer.Deserialize<SteamTokenJournal>(content);
                if (returnValue != null)
                {

                    returnValue.m_LockFile = lockFile;

                    // Establish base folder
                    returnValue.m_BaseFolder = System.IO.Path.GetDirectoryName(lockFile.FilePath);
                    if (returnValue.m_BaseFolder != null)
                    {
                        returnValue.m_BaseFolder = System.IO.Path.GetFullPath(returnValue.m_BaseFolder);
                        returnValue.m_TokenFolder = Path.Combine(returnValue.m_BaseFolder, "Tokens");
                        returnValue.m_CacheFolder = Path.Combine(returnValue.m_BaseFolder, "Cache");
                    }
                }
                return returnValue;
            }
            return null;
            
        }
    }
}
