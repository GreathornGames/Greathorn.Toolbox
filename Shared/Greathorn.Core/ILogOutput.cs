// Copyright Greathorn Games Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Text;
using static Greathorn.Core.Log;

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
            Error
        }

        public static string GetName(LogType type)
        {
            switch(type)
            {
                case LogType.Notice:
                    return "NOTICE";
                case LogType.Info:
                    return "INFO";
                case LogType.ExternalProcess:
                    return "EXTERNAL";
                case LogType.Error:
                    return "ERROR";
                default:
                    return "DEFAULT";
            }
        }

        public void WriteLine(LogType logType, string message);
        public void LineFeed();

        public void Shutdown();

    }
}
