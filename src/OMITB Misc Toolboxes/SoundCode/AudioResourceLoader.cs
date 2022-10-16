/*using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace NevernamedsItems
{

	public class AudioResourceLoader
	{

		public static readonly string zippath = Initialisation.ZipFilePath;
		public static readonly string filepath = Initialisation.FilePath;
		public static readonly string modName = "OnceMoreIntoTheBreach";

		public static void InitAudio()
		{
			int FilesLoaded = 0;
			if (File.Exists(zippath))
			{
				Debug.Log("Zip Found");
				using (ZipFile ModZIP = ZipFile.Read(zippath))
				{
					if (ModZIP != null && ModZIP.Entries.Count > 0)
					{
						foreach (ZipEntry entry in ModZIP.Entries)
						{
							if (entry.FileName.EndsWith(".bnk"))
							{
								using (MemoryStream ms = new MemoryStream())
								{
									entry.Extract(ms);
									ms.Seek(0, SeekOrigin.Begin);
									LoadSoundbankFromStream(ms, entry.FileName.ToLower().Replace(".bnk", string.Empty));
									FilesLoaded++;
								}
							}
						}
						if (FilesLoaded > 0) { return; }
					}
				}
			}
			// Zip file wasn't found. Try to load from Mod folder instead.
			AutoloadFromPath(filepath, modName);
		}

		public static void AutoloadFromModPath(string path, string prefix)
		{
			if (string.IsNullOrEmpty(path)) { throw new ArgumentNullException("path", "Path cannot be null."); }
			if (string.IsNullOrEmpty(prefix)) { throw new ArgumentNullException("prefix", "Prefix name cannot be null."); }
			prefix = prefix.Trim();
			if (string.IsNullOrEmpty(prefix)) { throw new ArgumentException("Prefix name cannot be an empty (or whitespace only) string.", "prefix"); }
			path = path.Replace('/', Path.DirectorySeparatorChar);
			path = path.Replace('\\', Path.DirectorySeparatorChar);
			if (!Directory.Exists(path))
			{

				Console.WriteLine(string.Format("{0}: No autoload directory in path, not autoloading anything. Path='{1}'.", typeof(AudioResourceLoader), path));

			}
			else
			{
				List<string> list = new List<string>(Directory.GetFiles(path, "*.bnk", SearchOption.AllDirectories));
				for (int i = 0; i < list.Count; i++)
				{
					string text = list[i];
					string text2 = text;
					text2 = text2.Replace('/', Path.DirectorySeparatorChar);
					text2 = text2.Replace('\\', Path.DirectorySeparatorChar);
					text2 = text2.Substring(text2.IndexOf(path) + path.Length);
					text2 = text2.Substring(0, text2.Length - ".bnk".Length);
					bool flag5 = text2.IndexOf(Path.DirectorySeparatorChar) == 0;
					if (flag5) { text2 = text2.Substring(1); }
					text2 = prefix + ":" + text2;

					Console.WriteLine(string.Format("{0}: Soundbank found, attempting to autoload: name='{1}' file='{2}'", typeof(AudioResourceLoader), text2, text));

					using (FileStream fileStream = File.OpenRead(text)) { LoadSoundbankFromStream(fileStream, text2); }
				}
			}
		}

		public static void AutoloadFromPath(string path, string prefix)
		{
			if (string.IsNullOrEmpty(path)) { throw new ArgumentNullException("path", "Path cannot be null."); }
			if (string.IsNullOrEmpty(prefix)) { throw new ArgumentNullException("prefix", "Prefix name cannot be null."); }
			prefix = prefix.Trim();
			if (string.IsNullOrEmpty(prefix)) { throw new ArgumentException("Prefix name cannot be an empty (or whitespace only) string.", "prefix"); }
			path = path.Replace('/', Path.DirectorySeparatorChar);
			path = path.Replace('\\', Path.DirectorySeparatorChar);
			if (!Directory.Exists(path))
			{

				Console.WriteLine(string.Format("{0}: No autoload directory in path, not autoloading anything. Path='{1}'.", typeof(AudioResourceLoader), path));

			}
			else
			{
				List<string> list = new List<string>(Directory.GetFiles(path, "*.bnk", SearchOption.AllDirectories));
				for (int i = 0; i < list.Count; i++)
				{
					string text = list[i];
					string text2 = text;
					text2 = text2.Replace('/', Path.DirectorySeparatorChar);
					text2 = text2.Replace('\\', Path.DirectorySeparatorChar);
					text2 = text2.Substring(text2.IndexOf(path) + path.Length);
					text2 = text2.Substring(0, text2.Length - ".bnk".Length);
					bool flag5 = text2.IndexOf(Path.DirectorySeparatorChar) == 0;
					if (flag5) { text2 = text2.Substring(1); }
					text2 = prefix + ":" + text2;
					Console.WriteLine(string.Format("{0}: Soundbank found, attempting to autoload: name='{1}' file='{2}'", typeof(AudioResourceLoader), text2, text));

					using (FileStream fileStream = File.OpenRead(text)) { LoadSoundbankFromStream(fileStream, text2); }
				}
			}
		}

		private static void LoadSoundbankFromStream(Stream stream, string name)
		{
			byte[] array = StreamToByteArray(stream);
			IntPtr intPtr = Marshal.AllocHGlobal(array.Length);
			try
			{
				Marshal.Copy(array, 0, intPtr, array.Length);
				uint num;
				AKRESULT akresult = AkSoundEngine.LoadAndDecodeBankFromMemory(intPtr, (uint)array.Length, false, name, false, out num);

				Console.WriteLine(string.Format("Result of soundbank load: {0}.", akresult));

			}
			finally
			{
				Marshal.FreeHGlobal(intPtr);
			}
		}

		public static byte[] StreamToByteArray(Stream input)
		{
			byte[] array = new byte[16384];
			byte[] result;
			using (MemoryStream memoryStream = new MemoryStream())
			{
				int count;
				while ((count = input.Read(array, 0, array.Length)) > 0) { memoryStream.Write(array, 0, count); }
				result = memoryStream.ToArray();
			}
			return result;
		}
	}
}*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;

namespace NevernamedsItems
{

	public class AudioResourceLoader
	{

		public static void InitAudio() { AutoloadFromAssembly(Assembly.GetExecutingAssembly(), "NevernamedsItems"); }

		public static void AutoloadFromAssembly(Assembly assembly, string prefix)
		{
			bool flag = assembly == null;
			if (flag) { throw new ArgumentNullException("assembly", "Assembly cannot be null."); }
			bool flag2 = prefix == null;
			if (flag2) { throw new ArgumentNullException("prefix", "Prefix name cannot be null."); }
			prefix = prefix.Trim();
			bool flag3 = prefix == "";
			if (flag3) { throw new ArgumentException("Prefix name cannot be an empty (or whitespace only) string.", "prefix"); }
			List<string> list = new List<string>(assembly.GetManifestResourceNames());
			for (int i = 0; i < list.Count; i++)
			{
				string text = list[i];
				string text2 = text;
				text2 = text2.Replace('/', Path.DirectorySeparatorChar);
				text2 = text2.Replace('\\', Path.DirectorySeparatorChar);
				bool flag4 = text2.IndexOf(prefix) != 0;
				if (!flag4)
				{
					text2 = text2.Substring(text2.IndexOf(prefix) + prefix.Length);
					bool flag5 = text2.LastIndexOf(".bnk") != text2.Length - ".bnk".Length;
					if (!flag5)
					{
						text2 = text2.Substring(0, text2.Length - ".bnk".Length);
						bool flag6 = text2.IndexOf(Path.DirectorySeparatorChar) == 0;
						if (flag6) { text2 = text2.Substring(1); }
						text2 = prefix + ":" + text2;
						// Console.WriteLine(string.Format("{0}: Soundbank found, attempting to autoload: name='{1}' resource='{2}'", typeof(ResourceLoaderSoundbanks), text2, text));
						using (Stream manifestResourceStream = assembly.GetManifestResourceStream(text))
						{
							LoadSoundbankFromStream(manifestResourceStream, text2);
						}
					}
				}
			}
		}

		private static void LoadSoundbankFromStream(Stream stream, string name)
		{
			byte[] array = StreamToByteArray(stream);
			IntPtr intPtr = Marshal.AllocHGlobal(array.Length);
			try
			{
				Marshal.Copy(array, 0, intPtr, array.Length);
				uint num;
				AKRESULT akresult = AkSoundEngine.LoadAndDecodeBankFromMemory(intPtr, (uint)array.Length, false, name, false, out num);
				// Console.WriteLine(string.Format("Result of soundbank load: {0}.", akresult));
			}
			finally
			{
				Marshal.FreeHGlobal(intPtr);
			}
		}

		public static byte[] StreamToByteArray(Stream input)
		{
			byte[] array = new byte[16384];
			byte[] result;
			using (MemoryStream memoryStream = new MemoryStream())
			{
				int count;
				while ((count = input.Read(array, 0, array.Length)) > 0) { memoryStream.Write(array, 0, count); }
				result = memoryStream.ToArray();
			}
			return result;
		}
	}
}
