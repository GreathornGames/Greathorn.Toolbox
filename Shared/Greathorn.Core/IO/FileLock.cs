// Copyright Greathorn Games Inc. All Rights Reserved.


using System;
using System.Threading;

namespace Greathorn.Core.IO
{
    public class FileLock : IDisposable
    {
        public static int Ticket { get; private set; } = 0;

        public string Identifier { get; private set; }
        public string FilePath { get; private set; }
        public string LockFilePath { get; private set; }

        public FileLock(string targetFilePath)
        {
            // The path to the actual file
            FilePath = targetFilePath;

            // The path to the lock file which will hold the hash of the owning file lock
            LockFilePath = $"{targetFilePath}.lock";

            // A unique identifier for this 
            Identifier = $"{DateTime.Now.Ticks}_{System.Environment.MachineName}_{Ticket++}";
        }

        ~FileLock()
        {
            Unlock();
        }

        public string? ReadAllTextFromLockedFile()
        {
            if (HasLock())
            {
                return System.IO.File.ReadAllText(FilePath);
            }
            return null;
        }
        public bool WriteAllTextToLockedFile(string content)
        {
            if(HasLock())
            {
                System.IO.File.WriteAllText(FilePath, content);
                return true;
            }
            return false;
        }

        public bool HasLock()
        {
            if (System.IO.File.Exists(LockFilePath))
            {
                string lockIdentifier = System.IO.File.ReadAllText(LockFilePath);
                if (lockIdentifier == Identifier)
                {
                    return true;
                }
            }
            return false;
        }

        public bool SafeLock(int retryCount = 5, int sleepTime = 5000)
        {
            Lock();
            while(!HasLock() && retryCount > 0)
            {
                retryCount--;
                Thread.Sleep(sleepTime);
                Lock();
            }
            return HasLock();
        }

        public bool Lock(bool force = false)
        {
            // Check if already locked
            if (System.IO.File.Exists(LockFilePath))
            {
                string lockIdentifier = System.IO.File.ReadAllText(LockFilePath);
                if (lockIdentifier == Identifier)
                {
                    return true;
                }
                else if(force)
                {
                    // Steal the lock
                    System.IO.File.WriteAllText(LockFilePath, Identifier);
                    return HasLock();
                }
                return false;
            }

            System.IO.File.WriteAllText(LockFilePath, Identifier);
            return HasLock();
        }
        
        public bool Unlock(bool force = false)
        {
            if (System.IO.File.Exists(LockFilePath))
            {
                // If we force were just going to delete it without reading
                if(force)
                {
                    System.IO.File.Delete(LockFilePath);
                    return !System.IO.File.Exists(LockFilePath);
                }

                // Check to see if we actually hold the lock
                string lockIdentifier = System.IO.File.ReadAllText(LockFilePath);
                if (lockIdentifier == Identifier)
                {
                    System.IO.File.Delete(LockFilePath);
                    return !System.IO.File.Exists(LockFilePath);
                }
            }
            return false;
        }

        public void Dispose()
        {
            Unlock();
        }
    }
}
