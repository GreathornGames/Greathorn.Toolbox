// Copyright Greathorn Games Inc. All Rights Reserved.

using System;
using System.IO;
using System.Text.Json;

namespace Greathorn.Core.Files
{
    [Serializable]
    class WorkspaceVersion
    {
#pragma warning disable 0649
        public string? Engine; // Do we need to force a clean and build of the engine?

        public string? Project; // Do we need to force a clean and build of the project?

        public string? Toolbox; // Do we need to pull and update the toolbox?
#pragma warning restore 0649

        private string? m_Path;

        public static WorkspaceVersion? Get(string filePath)
        {
            WorkspaceVersion? returnValue = null;
            if (File.Exists(filePath))
            {
                returnValue = JsonSerializer.Deserialize<WorkspaceVersion>(File.ReadAllText(filePath));
                if (returnValue != null)
                {
                    returnValue.m_Path = filePath;
                }
            }
            return returnValue;
        }

        public override bool Equals(object obj)
        {
            WorkspaceVersion? rhs = obj as WorkspaceVersion;

            if (rhs == null) return false;
            if (Engine != rhs.Engine) return false;
            if (Toolbox != rhs.Engine) return false;

            return true;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Engine, Toolbox);
        }
    }
}