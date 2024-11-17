// Copyright Greathorn Games Inc. All Rights Reserved.

namespace Greathorn.Core
{
    public interface ILogOutput
    {
        public enum LogType
        {
            Default,
            Notice,
            Info,
            ExternalProcess,
            Warning,
            Error
        }

        public static string GetName(LogType type)
        {
            switch (type)
            {
                case LogType.Notice:
                    return "NOTICE";
                case LogType.Info:
                    return "INFO";
                case LogType.ExternalProcess:
                    return "EXTERNAL";
                case LogType.Warning:
                    return "WARNING";
                case LogType.Error:
                    return "ERROR";
                default:
                    return "DEFAULT";
            }
        }

        public void WriteLine(LogType logType, string message);
        public void LineFeed();

        public void Shutdown();

        public bool IsThreadSafe();
    }
}
