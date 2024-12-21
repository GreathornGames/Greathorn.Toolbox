// Copyright Greathorn Games Inc. All Rights Reserved.

using System.Text.Json;
using Greathorn.Core;

namespace SteamToken
{
    //[Serializable]
    //public class SteamTokenJournal
    //{
    //    public FileStream Stream;


    //    ~SteamTokenJournal()
    //    {
    //    }

    //    [Serializable]
    //    public class SteamUploader
    //    {
    //        public string Username;
    //        public bool Available;
    //        public string TokenFile;
    //    }

    //    public SteamUploader[] Accounts = new SteamUploader[0];


    //    public bool Output(string filePath)
    //    {
    //        string content = JsonSerializer.Serialize<SteamTokenJournal>(this);
    //        File.WriteAllText(filePath, content);
    //        return true;
    //    }

    //    public static SteamTokenJournal? Get(string filePath)
    //    {
    //        SteamTokenJournal? returnValue = null;
    //        if (File.Exists(filePath))
    //        {
    //            returnValue = JsonSerializer.Deserialize<SteamTokenJournal>(File.ReadAllText(filePath));
    //        }
    //        return returnValue;
    //    }
    //}
}
