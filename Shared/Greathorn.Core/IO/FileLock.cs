// Copyright Greathorn Games Inc. All Rights Reserved.

using System;
using System.Threading;

namespace Greathorn.Core.IO
{
    public class FileLock : IDisposable
    {
        public bool IsPersistant = false;
        public static int Ticket { get; private set; } = 0;

        public string FilePath { get; private set; }
        public string LockFilePath { get; private set; }

        public LockIdentifier Identifier { get; private set; }

        public FileLock(string targetFilePath)
        {
            // The path to the actual file
            FilePath = targetFilePath;

            // The path to the lock file which will hold the hash of the owning file lock
            LockFilePath = $"{targetFilePath}.lock";

            Identifier = new LockIdentifier();
        }

        ~FileLock()
        {
            if (!IsPersistant)
            {
                Unlock();
            }
        }

        public string? ReadAllTextFromLockedFile(LockComparisonLevel comparisonLevel = LockComparisonLevel.MachineName)
        {
            if (HasLock(comparisonLevel))
            {
                return System.IO.File.ReadAllText(FilePath);
            }
            return null;
        }
        public bool WriteAllTextToLockedFile(string content, LockComparisonLevel comparisonLevel = LockComparisonLevel.MachineName)
        {
            if (HasLock(comparisonLevel))
            {
                System.IO.File.WriteAllText(FilePath, content);
                return true;
            }
            return false;
        }

        public bool HasLock(LockComparisonLevel comparisonLevel = LockComparisonLevel.MachineName)
        {
            if (System.IO.File.Exists(LockFilePath))
            {
                if (Identifier.IsSame(System.IO.File.ReadAllText(LockFilePath), comparisonLevel))
                {
                    return true;
                }
            }
            return false;
        }

        public bool SafeLock(int retryCount = 5, int sleepTime = 5000, LockComparisonLevel comparisonLevel = LockComparisonLevel.MachineName)
        {
            Lock(false, comparisonLevel);
            while (!HasLock(comparisonLevel) && retryCount > 0)
            {
                retryCount--;
                Thread.Sleep(sleepTime);
                Lock(false, comparisonLevel);
            }
            return HasLock(comparisonLevel);
        }

        public bool Lock(bool force = false, LockComparisonLevel comparisonLevel = LockComparisonLevel.MachineName)
        {
            // Check if already locked
            if (System.IO.File.Exists(LockFilePath))
            {
                if (Identifier.IsSame(System.IO.File.ReadAllText(LockFilePath), comparisonLevel))
                {
                    return true;
                }
                else if (force)
                {
                    // Steal the lock
                    System.IO.File.WriteAllText(LockFilePath, Identifier.ToString());
                    return HasLock(comparisonLevel);
                }
                return false;
            }

            System.IO.File.WriteAllText(LockFilePath, Identifier.ToString());
            return HasLock(comparisonLevel);
        }

        public bool Unlock(bool force = false, LockComparisonLevel comparisonLevel = LockComparisonLevel.MachineName)
        {
            if (System.IO.File.Exists(LockFilePath))
            {
                // If we force were just going to delete it without reading
                if (force)
                {
                    System.IO.File.Delete(LockFilePath);
                    return !System.IO.File.Exists(LockFilePath);
                }

                // Check to see if we actually hold the lock                
                if (Identifier.IsSame(System.IO.File.ReadAllText(LockFilePath), comparisonLevel))
                {
                    System.IO.File.Delete(LockFilePath);
                    return !System.IO.File.Exists(LockFilePath);
                }
            }
            return false;
        }

        public void Dispose()
        {
            if (!IsPersistant)
            {
                Unlock();
            }
        }

        [Flags]
        public enum LockComparisonLevel
        {
            Timestamp = 1,
            MachineName = 2,
            Ticket = 4
        }
        public class LockIdentifier
        {
            public long Timestamp;
            public string? MachineName;
            public int Ticket;

            public LockIdentifier()
            {
                Timestamp = DateTime.Now.Ticks;
                MachineName = System.Environment.MachineName;
                Ticket = FileLock.Ticket++;
            }

            public bool IsSame(string identifier, LockComparisonLevel comparisonLevel = LockComparisonLevel.MachineName)
            {
                if (comparisonLevel.HasFlag(LockComparisonLevel.Timestamp))
                {
                    if (!IsSameTimestamp(identifier)) return false;
                }
                if (comparisonLevel.HasFlag(LockComparisonLevel.MachineName))
                {
                    if (!IsSameMachine(identifier)) return false;
                }
                if (comparisonLevel.HasFlag(LockComparisonLevel.Ticket))
                {
                    if (!IsSameTicket(identifier)) return false;
                }
                return true;
            }

            public bool IsSameMachine(string identifier)
            {
                string[] parts = identifier.Split('_', StringSplitOptions.RemoveEmptyEntries);
                return parts[1] == MachineName;
            }

            public bool IsSameTicket(string identifier)
            {
                string[] parts = identifier.Split('_', StringSplitOptions.RemoveEmptyEntries);
                if (int.TryParse(parts[2], out int foundTicket))
                {
                    return (foundTicket == Ticket);
                }
                return false;
            }
            public bool IsSameTimestamp(string identifier)
            {
                string[] parts = identifier.Split('_', StringSplitOptions.RemoveEmptyEntries);
                if (long.TryParse(parts[0], out long foundTimestamp))
                {
                    return (foundTimestamp == Timestamp);
                }
                return false;
            }

            public override string ToString()
            {
                return $"{Timestamp}_{MachineName}_{Ticket}";
            }
        }
    }
}
