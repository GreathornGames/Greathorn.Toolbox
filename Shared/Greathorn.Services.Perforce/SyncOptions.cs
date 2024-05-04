// Copyright Greathorn Games Inc. All Rights Reserved.

namespace Greathorn.Services.Perforce
{
    public class SyncOptions
    {
        public int NumRetries;
        public int NumThreads;
        public int TcpBufferSize;

        public SyncOptions Clone()
        {
            return (SyncOptions)MemberwiseClone();
        }
    }
}