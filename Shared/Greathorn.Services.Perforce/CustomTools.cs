using System;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Collections.Generic;
using Greathorn.Core.Utils;
using Greathorn.Core;

namespace Greathorn.Services.Perforce
{
    public static class CustomTools
    {
        public static readonly string ConfigFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".p4qt", "customtools.xml");

        public static CustomToolDefList Get(string? customPath = null)
        {
            if (customPath == null)
            {
                customPath = ConfigFile;
            }

            if (!File.Exists(customPath))
            {
                Log.WriteLine($"Unable to find provided tool file @ {customPath}, returning empty data.", "PERFORCE", ILogOutput.LogType.Warning);
                return new CustomToolDefList();
            }

            XmlSerializer serializer = new XmlSerializer(typeof(CustomToolDefList));
            using (StreamReader reader = new StreamReader(customPath))
            {
                CustomToolDefList? returnObject;
                try
                {
                    returnObject = (CustomToolDefList)serializer.Deserialize(reader);
                }
                catch
                {
                    Log.WriteLine($"Unable to parse provided tool file @ {customPath}, returning empty data.", "PERFORCE", ILogOutput.LogType.Warning);
                    returnObject = new CustomToolDefList();
                }
                return returnObject;
            }
        }


        public static void AddOrReplace(this CustomToolDefList lhs, CustomToolDefList rhs, bool replaceTopLevelFolder = true)
        {
            // Handle Folders
            int rhsFolderCount = rhs.CustomToolFolder.Count;
            for (int j = 0; j < rhsFolderCount; j++)
            {
                int lhsFolderCount = lhs.CustomToolFolder.Count;
                bool found = false;
                for (int i = 0; i < lhsFolderCount; i++)
                {
                    if (lhs.CustomToolFolder[i].Name == rhs.CustomToolFolder[j].Name)
                    {
                        lhs.CustomToolFolder[i] = rhs.CustomToolFolder[j];
                        found = true;
                    }
                }
                if (!found)
                {
                    lhs.CustomToolFolder.Add(rhs.CustomToolFolder[j]);
                }
            }

            // Handle Tools
            int rhsToolCount = rhs.CustomToolDef.Count;
            for (int j = 0; j < rhsToolCount; j++)
            {
                int lsToolCount = lhs.CustomToolDef.Count;
                bool found = false;
                for (int i = 0; i < lsToolCount; i++)
                {

                    if (lhs.CustomToolDef[i].Definition.Name == rhs.CustomToolDef[j].Definition.Name)
                    {
                        lhs.CustomToolDef[i] = rhs.CustomToolDef[j];
                        found = true;
                    }
                }
                if (!found)
                {
                    lhs.CustomToolDef.Add(rhs.CustomToolDef[j]);
                }
            }
        }

        public static void Output(this CustomToolDefList definition, string path)
        {
            FileUtil.EnsureFileFolderHierarchyExists(path);

            XmlWriterSettings settings = new XmlWriterSettings { Indent = true };
            XmlSerializer serializer = new XmlSerializer(typeof(CustomToolDefList));
            using (XmlWriter writer = XmlWriter.Create(path, settings))
            {
                serializer.Serialize(writer, definition);
            }
        }

        #region File DOM Definition
        [XmlRoot(ElementName = "Definition")]
        public class Definition
        {
            [XmlElement(ElementName = "Name")]
            public string Name { get; set; }

            [XmlElement(ElementName = "Command")]
            public string Command { get; set; }

            [XmlElement(ElementName = "Arguments")]
            public string Arguments { get; set; }

            [XmlElement(ElementName = "Shortcut")]
            public object Shortcut { get; set; }
        }

        [XmlRoot(ElementName = "Console")]
        public class Console
        {
            [XmlElement(ElementName = "CloseOnExit")]
            public bool CloseOnExit { get; set; }
        }

        [XmlRoot(ElementName = "CustomToolDef")]
        public class CustomToolDef
        {
            [XmlElement(ElementName = "Definition")]
            public Definition Definition { get; set; }

            [XmlElement(ElementName = "Console")]
            public Console Console { get; set; }

            [XmlElement(ElementName = "AddToContext")]
            public bool AddToContext { get; set; } = false;
        }

        [XmlRoot(ElementName = "CustomToolDefList")]
        public class CustomToolDefList
        {
            [XmlElement(ElementName = "CustomToolDef")]
            public List<CustomToolDef> CustomToolDef { get; set; } = new List<CustomToolDef>();

            [XmlElement(ElementName = "CustomToolFolder")]
            public List<CustomToolFolder> CustomToolFolder { get; set; } = new List<CustomToolFolder>();

            [XmlAttribute(AttributeName = "varName")]
            public string VarName { get; set; } = "customtooldeflist";

            [XmlText]
            public string Text { get; set; }
        }

        [XmlRoot(ElementName = "CustomToolFolder")]
        public class CustomToolFolder
        {
            [XmlElement(ElementName = "Name")]
            public string Name { get; set; }

            [XmlElement(ElementName = "CustomToolDefList")]
            public CustomToolDefList CustomToolDefList { get; set; }
        }
        #endregion
    }
}
