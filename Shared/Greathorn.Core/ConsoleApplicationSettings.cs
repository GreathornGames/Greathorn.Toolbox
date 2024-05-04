// Copyright Greathorn Games Inc. All Rights Reserved.

namespace Greathorn.Core
{
    public class ConsoleApplicationSettings
    {
        public string? DefaultLogCategory;
        public ILogOutput[]? LogOutputs;
        public bool PauseOnExit = false;
        public bool RequiresElevatedAccess = false;
    }
}
