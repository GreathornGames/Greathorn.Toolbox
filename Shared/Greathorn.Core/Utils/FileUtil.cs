// Copyright Greathorn Games Inc. All Rights Reserved.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using Greathorn.Core.IO;
using Greathorn.Core.IO.FileAccessors;

namespace Greathorn.Core.Utils
{
	public static class FileUtil
	{
		public static void AlwaysWrite(string outputPath, string contents)
		{
			File.WriteAllText(outputPath, contents);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void EnsureFileFolderHierarchyExists(string filePath)
		{
			string targetDirectory = Path.GetDirectoryName(filePath);
			EnsureFolderHierarchyExists(targetDirectory);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void EnsureFolderHierarchyExists(string folderPath)
		{
			if (!string.IsNullOrEmpty(folderPath) && !Directory.Exists(folderPath))
			{
				Directory.CreateDirectory(folderPath);
			}
		}

		public static void ForceDeleteFile(string filePath)
		{
			if (File.Exists(filePath))
			{
				File.SetAttributes(filePath, File.GetAttributes(filePath) & ~FileAttributes.ReadOnly);
				File.Delete(filePath);
			}
		}

		public static bool IsSafeToWrite(string filePath)
		{
			if (File.Exists(filePath))
			{
				FileAttributes attributes = File.GetAttributes(filePath);
				if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly ||
					(attributes & FileAttributes.Offline) == FileAttributes.Offline)
				{
					return false;
				}
			}

			return true;
		}

		public static void MakeWritable(this string absolutePath)
		{
			string fileName = Path.GetFileName(absolutePath);
			if (fileName != null)
			{
				string directoryPath = absolutePath.TrimEnd(fileName.ToCharArray());
				if (!Directory.Exists(directoryPath))
				{
					Directory.CreateDirectory(directoryPath);
				}
			}

			if (File.Exists(absolutePath))
			{
				File.SetAttributes(absolutePath,
					File.GetAttributes(absolutePath).RemoveAttribute(FileAttributes.ReadOnly));
			}
		}

		private static FileAttributes RemoveAttribute(this FileAttributes attributes, FileAttributes attributesToRemove)
		{
			return attributes & ~attributesToRemove;
		}

		public static string? GetPathWithCorrectCase(this FileInfo info)
		{
			DirectoryInfo parentInfo = info.Directory;
			return parentInfo != null
				? Path.Combine(GetPathWithCorrectCase(parentInfo),
					info.Exists ? parentInfo.GetFiles(info.Name)[0].Name : info.Name)
				: null;
		}

		public static string GetPathWithCorrectCase(this DirectoryInfo info)
		{
			DirectoryInfo parentInfo = info.Parent;
			return parentInfo == null
				? info.FullName.ToUpperInvariant()
				: Path.Combine(GetPathWithCorrectCase(parentInfo),
					info.Exists ? parentInfo.GetDirectories(info.Name)[0].Name : info.Name);
		}

		public static MemoryStream? GetMemoryStream(this string filePath)
		{
			return File.Exists(filePath) ? new MemoryStream(File.ReadAllBytes(filePath)) : null;
		}

		public static string FixDirectorySeparator(this string filePath)
		{
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Unix:
                case PlatformID.MacOSX:
                    return filePath.Replace('\\', Path.DirectorySeparatorChar);
                default:
                    return filePath.Replace('/', Path.DirectorySeparatorChar);
            }
        }

		public static void WriteAllLinesNoExtraLine(string path, params string[] lines)
		{
			if (path == null)
				throw new ArgumentNullException("path");
			if (lines == null)
				throw new ArgumentNullException("lines");

			using var stream = File.OpenWrite(path);
			using StreamWriter writer = new StreamWriter(stream);
			if (lines.Length > 0)
			{
				for (int i = 0; i < lines.Length - 1; i++)
				{
					writer.WriteLine(lines[i]);
				}
				writer.Write(lines[^1]);
			}
		}

		public static void WriteStream(Stream inputStream, string outputPath)
		{
			IFileAccessor? outputHandler = GetFileAccessor(outputPath);
			if (outputHandler != null)
			{

				int bufferSize = outputHandler.GetWriteBufferSize();
				using Stream outputFile = outputHandler.GetWriter();

				long inputStreamLength = inputStream.Length;
				byte[] bytes = new byte[bufferSize];
				long writtenLength = 0;
				Timer timer = new Timer();
				while (writtenLength < inputStreamLength)
				{
					int readAmount = bufferSize;
					if (writtenLength + bufferSize > inputStreamLength)
					{
						readAmount = (int)(inputStreamLength - writtenLength);
					}

					inputStream.Read(bytes, 0, readAmount);

					// Write read data
					outputFile.Write(bytes, 0, readAmount);

					// Add to our offset
					writtenLength += readAmount;
				}

				outputFile.Close();
				Log.WriteLine(
					$"Wrote {writtenLength} of {inputStreamLength} bytes in {timer.GetElapsedSeconds()} seconds (∼{timer.TransferRate(writtenLength)}).", "FILE");
			}
			else
			{
				Log.WriteLine(
					$"File accessor was null for {outputPath}", "FILE");
			}
		}

		public static IFileAccessor? GetFileAccessor(string connectionString)
		{
			//Uri uri = new Uri(connectionString);
			//IFileAccessor.Type type = IFileAccessor.Type.Default;
			//switch (uri.Scheme.ToUpper())
			//{
			//	case "SMB":
			//		type = Greathorn.Core.IO.IFileAccessor.Type.SMB;
			//		break;
			//}

			//if (type != Greathorn.Core.IO.IFileAccessor.Type.Default)
			//{
			//	// Determine address
			//	string address = uri.Host;

			//	// Need to figure out the share/file path
			//	string share = string.Empty;
			//	string filePath = string.Empty;
			//	if (!string.IsNullOrEmpty(uri.AbsolutePath))
			//	{
			//		string fullPath = uri.AbsolutePath;
			//		if (fullPath.StartsWith('/'))
			//		{
			//			fullPath = fullPath.Substring(1);
			//		}

			//		string[] info = fullPath.Split('/', 2);
			//		share = info[0];
			//		filePath = info[1];
			//	}

			//	// Handle Authentication
			//	string username = string.Empty;
			//	string password = string.Empty;
			//	if (!string.IsNullOrEmpty(uri.UserInfo))
			//	{
			//		string[] info = uri.UserInfo.Split(':', 2);
			//		username = info[0];
			//		password = info[1];
			//	}

			//	switch (type)
			//	{
			//		case IFileAccessor.Type.SMB:
			//			return null; //new SMBFileAccessor(address, username, password, share, filePath);
			//	}
			//}

			// Default to a system level file stream
			return new SystemFileAccessor(connectionString);
		}
	}
}
