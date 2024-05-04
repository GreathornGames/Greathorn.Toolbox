// Copyright Greathorn Games Inc. All Rights Reserved.

using System.IO;

namespace Greathorn.Core.IO
{
	public interface IFileAccessor
	{
		public enum Type
		{
			Default,
			SMB
		}
		public uint GetBlockSize();

		public int GetReadBufferSize();
		public int GetWriteBufferSize();
		public bool ValidConnection();
		public Stream GetReader();
		public Stream GetWriter();
	}
}
