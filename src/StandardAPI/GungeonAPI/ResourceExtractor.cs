using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace GungeonAPI
{
	// Token: 0x02000007 RID: 7
	public static class ResourceExtractor
	{
		// Token: 0x0600002E RID: 46 RVA: 0x000035EC File Offset: 0x000017EC
		public static List<Texture2D> GetTexturesFromDirectory(string directoryPath)
		{
			bool flag = !Directory.Exists(directoryPath);
			List<Texture2D> result;
			if (flag)
			{
				Tools.PrintError<string>(directoryPath + " not found.", "FF0000");
				result = null;
			}
			else
			{
				List<Texture2D> list = new List<Texture2D>();
				foreach (string text in Directory.GetFiles(directoryPath))
				{
					bool flag2 = !text.EndsWith(".png");
					if (!flag2)
					{
						Texture2D item = ResourceExtractor.BytesToTexture(File.ReadAllBytes(text), Path.GetFileName(text).Replace(".png", ""));
						list.Add(item);
					}
				}
				result = list;
			}
			return result;
		}

		// Token: 0x0600002F RID: 47 RVA: 0x00003694 File Offset: 0x00001894
		public static Texture2D GetTextureFromFile(string fileName, string extension = ".png")
		{
			fileName = fileName.Replace(extension, "");
			string text = Path.Combine(ResourceExtractor.spritesDirectory, fileName + extension);
			bool flag = !File.Exists(text);
			Texture2D result;
			if (flag)
			{
				Tools.PrintError<string>(text + " not found.", "FF0000");
				result = null;
			}
			else
			{
				Texture2D texture2D = ResourceExtractor.BytesToTexture(File.ReadAllBytes(text), fileName);
				result = texture2D;
			}
			return result;
		}

		// Token: 0x06000030 RID: 48 RVA: 0x000036FC File Offset: 0x000018FC
		public static List<string> GetCollectionFiles()
		{
			List<string> list = new List<string>();
			foreach (string text in Directory.GetFiles(ResourceExtractor.spritesDirectory))
			{
				bool flag = text.EndsWith(".png");
				if (flag)
				{
					list.Add(Path.GetFileName(text).Replace(".png", ""));
				}
			}
			return list;
		}

		// Token: 0x06000031 RID: 49 RVA: 0x00003768 File Offset: 0x00001968
		public static Texture2D BytesToTexture(byte[] bytes, string resourceName)
		{
			Texture2D texture2D = new Texture2D(1, 1, TextureFormat.RGBA32, false);
			texture2D.LoadImage(bytes);
			texture2D.filterMode = FilterMode.Point;
			texture2D.name = resourceName;
			return texture2D;
		}

		// Token: 0x06000032 RID: 50 RVA: 0x000037A0 File Offset: 0x000019A0
		public static string[] GetLinesFromEmbeddedResource(string filePath)
		{
			string text = ResourceExtractor.BytesToString(ResourceExtractor.ExtractEmbeddedResource(filePath));
			return text.Split(new char[]
			{
				'\n'
			});
		}

		// Token: 0x06000033 RID: 51 RVA: 0x000037D0 File Offset: 0x000019D0
		public static string[] GetLinesFromFile(string filePath)
		{
			string text = ResourceExtractor.BytesToString(File.ReadAllBytes(filePath));
			return text.Split(new char[]
			{
				'\n'
			});
		}

		// Token: 0x06000034 RID: 52 RVA: 0x00003800 File Offset: 0x00001A00
		public static string BytesToString(byte[] bytes)
		{
			return Encoding.UTF8.GetString(bytes, 0, bytes.Length);
		}

		// Token: 0x06000035 RID: 53 RVA: 0x00003824 File Offset: 0x00001A24
		public static List<string> GetResourceFolders()
		{
			List<string> list = new List<string>();
			string path = Path.Combine(ETGMod.ResourcesDirectory, "sprites");
			bool flag = Directory.Exists(path);
			if (flag)
			{
				foreach (string path2 in Directory.GetDirectories(path))
				{
					list.Add(Path.GetFileName(path2));
				}
			}
			return list;
		}

		// Token: 0x06000036 RID: 54 RVA: 0x0000388C File Offset: 0x00001A8C
		public static byte[] ExtractEmbeddedResource(string filePath)
		{
			filePath = filePath.Replace("/", ".");
			filePath = filePath.Replace("\\", ".");
			Assembly callingAssembly = Assembly.GetCallingAssembly();
			byte[] result;
			using (Stream manifestResourceStream = callingAssembly.GetManifestResourceStream(filePath))
			{
				bool flag = manifestResourceStream == null;
				if (flag)
				{
					result = null;
				}
				else
				{
					byte[] array = new byte[manifestResourceStream.Length];
					manifestResourceStream.Read(array, 0, array.Length);
					result = array;
				}
			}
			return result;
		}

		// Token: 0x06000037 RID: 55 RVA: 0x00003918 File Offset: 0x00001B18
		public static Texture2D GetTextureFromResource(string resourceName)
		{
			byte[] array = ResourceExtractor.ExtractEmbeddedResource(resourceName);
			bool flag = array == null;
			Texture2D result;
			if (flag)
			{
				Tools.PrintError<string>("No bytes found in " + resourceName, "FF0000");
				result = null;
			}
			else
			{
				Texture2D texture2D = new Texture2D(1, 1, TextureFormat.RGBAFloat, false);
				texture2D.LoadImage(array);
				texture2D.filterMode = FilterMode.Point;
				string text = resourceName.Substring(0, resourceName.LastIndexOf('.'));
				bool flag2 = text.LastIndexOf('.') >= 0;
				if (flag2)
				{
					text = text.Substring(text.LastIndexOf('.') + 1);
				}
				texture2D.name = text;
				result = texture2D;
			}
			return result;
		}

		// Token: 0x06000038 RID: 56 RVA: 0x000039B8 File Offset: 0x00001BB8
		public static string[] GetResourceNames()
		{
			Assembly callingAssembly = Assembly.GetCallingAssembly();
			string[] manifestResourceNames = callingAssembly.GetManifestResourceNames();
			bool flag = manifestResourceNames == null;
			string[] result;
			if (flag)
			{
				ETGModConsole.Log("No manifest resources found.", false);
				result = null;
			}
			else
			{
				result = manifestResourceNames;
			}
			return result;
		}

		// Token: 0x0400000F RID: 15
		private static string spritesDirectory = Path.Combine(ETGMod.ResourcesDirectory, "sprites");
	}
}
