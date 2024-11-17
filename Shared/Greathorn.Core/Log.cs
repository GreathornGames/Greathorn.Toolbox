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

        static bool s_UseThreadSafeCache = false;
        static int s_LogOutputCount = 0;
        static ILogOutput[] s_LogOutputs = new ILogOutput[0];
        static System.Collections.Concurrent.ConcurrentBag<CachedLogOuput> s_ThreadSafeCache = new System.Collections.Concurrent.ConcurrentBag<CachedLogOuput>();

        struct CachedLogOuput
        {
            public string Output;
            public LogType Type;
            public CachedLogOuput(string output, LogType type)
            {
                Output = output;
                Type = type;
            }
        }

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

            if (s_UseThreadSafeCache)
            {
                s_ThreadSafeCache.Add(new CachedLogOuput($"[{DateTime.Now.ToString(k_DateStampFormat)}] {category.ToUpper(),k_FixedCategoryLength} > {output}", logType));
            }

            for (int i = 0; i < s_LogOutputCount; i++)
            {
                if (s_UseThreadSafeCache && !s_LogOutputs[i].IsThreadSafe())
                {
                    continue;
                }
                s_LogOutputs[i].WriteLine(logType, $"[{DateTime.Now.ToString(k_DateStampFormat)}] {category.ToUpper(),k_FixedCategoryLength} > {output}");
            }
        }

        public static void SetThreadSafeMode()
        {
            s_UseThreadSafeCache = true;
        }
        public static void ClearThreadSafeMode()
        {
            s_UseThreadSafeCache = false;
            CachedLogOuput[] output = s_ThreadSafeCache.ToArray();
            s_ThreadSafeCache.Clear();
            int count = output.Length;
            for (int i = 0; i < count; i++)
            {
                for (int j = 0; j < s_LogOutputCount; j++)
                {
                    if (s_LogOutputs[j].IsThreadSafe())
                    {
                        continue;
                    }
                    s_LogOutputs[j].WriteLine(output[i].Type, output[i].Output);
                }
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

            for (int i = 0; i < s_LogOutputCount; i++)
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
