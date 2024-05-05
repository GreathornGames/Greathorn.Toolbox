// Copyright Greathorn Games Inc. All Rights Reserved.

using System;
using System.Runtime.CompilerServices;
using static Greathorn.Core.ILogOutput;

namespace Greathorn.Core
{
    public static class Log
    {
        private const string k_DateStampFormat = "yyyy-MM-dd HH:mm:ss";
        private const int k_FixedCategoryLength = 12;

        public static string DefaultCategory = "DEFAULT";

        static int s_LogOutputCount = 0;
        static ILogOutput[] s_LogOutputs = new ILogOutput[0];

        public static void AddLogOutput(ILogOutput output)
        {
            int newIndex = s_LogOutputCount;
            Array.Resize<ILogOutput>(ref s_LogOutputs, ++s_LogOutputCount);
            s_LogOutputs[newIndex] = output;
        }
        public static void AddLogOutputs(ILogOutput[]? outputs)
        {
            if (outputs == null) return;
            int count = outputs.Length;
            for (int i = 0; i < count; i++)
            {
                AddLogOutput(outputs[i]);
            }
        }

        public static void Shutdown()
        {
            if (!HasOutputs()) return;

            for (int i = 0; i < s_LogOutputCount; i++)
            {
                s_LogOutputs[i].Shutdown();
            }
        }

		public static void WriteLine(object output, string? category = null, LogType logType = LogType.Default)
		{
			WriteLine(output.ToString(), logType, category);
		}

		public static void WriteLine(string output, string category, LogType logType = LogType.Default)
		{
			WriteLine(output, logType, category);
		}

		public static void WriteLine(string output, LogType logType = LogType.Default, string? category = null)
        {
			if (!HasOutputs() || string.IsNullOrEmpty(output))
			{
				return;
			}

            category ??= DefaultCategory;

			if (category.Length > k_FixedCategoryLength)
			{
				category = category[..k_FixedCategoryLength];
			}


			for (int i = 0; i < s_LogOutputCount; i++)
			{
				s_LogOutputs[i].WriteLine(logType, $"[{DateTime.Now.ToString(k_DateStampFormat)}] {category.ToUpper(),k_FixedCategoryLength} > {output}");
			}
		}


        public static void WriteRaw(string output, LogType logType = LogType.Default)
        {
            if (!HasOutputs()) return;

            for (int i = 0; i < s_LogOutputCount; i++)
            {
                s_LogOutputs[i].WriteLine(logType, output);
            }
        }

        public static void LineFeed()
        {
            if (!HasOutputs()) return;

            for(int i = 0; i < s_LogOutputCount; i++)
            {
                s_LogOutputs[i].LineFeed();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool HasOutputs()
        {
            return s_LogOutputCount > 0;
        }

       
    }
}
