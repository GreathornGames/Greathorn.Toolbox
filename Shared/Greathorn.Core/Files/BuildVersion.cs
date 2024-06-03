using System;
using System.IO;
using System.Text.Json;


namespace Greathorn.Core.Files
{
    [Serializable]
    public class BuildVersion
    {
        public int MajorVersion;
        public int MinorVersion;
        public int PatchVersion;
        public int Changelist;
        public int CompatibleChangelist;
        public int IsLicenseeVersion;
        public string BranchName = "UE5";

        private string? m_Path;

        public static BuildVersion? Get(string filePath)
        {
            BuildVersion? returnValue = null;
            if (File.Exists(filePath))
            {
                returnValue = JsonSerializer.Deserialize<BuildVersion>(File.ReadAllText(filePath));
                if(returnValue != null)
                {
                    returnValue.m_Path = filePath;
                }
            }
            return returnValue;
        }
        public static BuildVersion? Get(SettingsProvider settings)
        {
            return Get(Path.Combine(settings.RootFolder, "Engine", "Build", "Build.version"));            
        }
    }
}