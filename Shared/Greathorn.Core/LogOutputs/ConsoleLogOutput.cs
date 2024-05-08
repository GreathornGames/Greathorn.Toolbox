// Copyright Greathorn Games Inc. All Rights Reserved.

using System;
using static Greathorn.Core.ILogOutput;

namespace Greathorn.Core.Loggers
{
    public class ConsoleLogOutput : ILogOutput
    {

        public ConsoleColor DefaultForegroundColor = ConsoleColor.Gray;
        public ConsoleColor DefaultBackgroundColor = ConsoleColor.Black;

        public void LineFeed()
        {
            Console.WriteLine();
        }

        public void Shutdown()
        {
            Console.ResetColor();
        }

        public void WriteLine(LogType logType, string message)
        {
			Console.ForegroundColor = logType switch
			{
				LogType.Notice => ConsoleColor.DarkGreen,
				LogType.Error => ConsoleColor.DarkRed,
				LogType.Info => ConsoleColor.DarkCyan,
                LogType.Warning => ConsoleColor.DarkYellow,
				LogType.ExternalProcess => ConsoleColor.DarkGray,
				_ => DefaultForegroundColor,
			};
			Console.WriteLine(message);

            Console.ForegroundColor = DefaultForegroundColor;
        }
    }
}
