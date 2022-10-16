using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace GungeonAPI
{
	// Token: 0x0200000F RID: 15
	public static class Tools
	{
		// Token: 0x06000076 RID: 118 RVA: 0x00005C0C File Offset: 0x00003E0C
		public static void Init()
		{
			bool flag = File.Exists(Tools.defaultLog);
			if (flag)
			{
				File.Delete(Tools.defaultLog);
			}
		}

		// Token: 0x06000077 RID: 119 RVA: 0x00005C34 File Offset: 0x00003E34
		public static void Print<T>(T obj, string color = "FFFFFF", bool force = false)
		{
			bool flag = Tools.verbose || force;
			if (flag)
			{
				string[] array = obj.ToString().Split(new char[]
				{
					'\n'
				});
				foreach (string text in array)
				{
					Tools.LogToConsole(string.Concat(new string[]
					{
						"<color=#",
						color,
						">[",
						Tools.modID,
						"] ",
						text,
						"</color>"
					}));
				}
			}
			Tools.Log<string>(obj.ToString());
		}

		// Token: 0x06000078 RID: 120 RVA: 0x00005CD8 File Offset: 0x00003ED8
		public static void PrintRaw<T>(T obj, bool force = false)
		{
			bool flag = Tools.verbose || force;
			if (flag)
			{
				Tools.LogToConsole(obj.ToString());
			}
			Tools.Log<string>(obj.ToString());
		}

		// Token: 0x06000079 RID: 121 RVA: 0x00005D18 File Offset: 0x00003F18
		public static void PrintError<T>(T obj, string color = "FF0000")
		{
			string[] array = obj.ToString().Split(new char[]
			{
				'\n'
			});
			foreach (string text in array)
			{
				Tools.LogToConsole(string.Concat(new string[]
				{
					"<color=#",
					color,
					">[",
					Tools.modID,
					"] ",
					text,
					"</color>"
				}));
			}
			Tools.Log<string>(obj.ToString());
		}

		// Token: 0x0600007A RID: 122 RVA: 0x00005DB0 File Offset: 0x00003FB0
		public static void PrintException(Exception e, string color = "FF0000")
		{
			string text = e.Message + "\n" + e.StackTrace;
			string[] array = text.Split(new char[]
			{
				'\n'
			});
			foreach (string text2 in array)
			{
				Tools.LogToConsole(string.Concat(new string[]
				{
					"<color=#",
					color,
					">[",
					Tools.modID,
					"] ",
					text2,
					"</color>"
				}));
			}
			Tools.Log<string>(e.Message);
			Tools.Log<string>("\t" + e.StackTrace);
		}

		// Token: 0x0600007B RID: 123 RVA: 0x00005E64 File Offset: 0x00004064
		public static void Log<T>(T obj)
		{
			using (StreamWriter streamWriter = new StreamWriter(Path.Combine(ETGMod.ResourcesDirectory, Tools.defaultLog), true))
			{
				streamWriter.WriteLine(obj.ToString());
			}
		}

		// Token: 0x0600007C RID: 124 RVA: 0x00005EBC File Offset: 0x000040BC
		public static void Log<T>(T obj, string fileName)
		{
			bool flag = !Tools.verbose;
			if (!flag)
			{
				using (StreamWriter streamWriter = new StreamWriter(Path.Combine(ETGMod.ResourcesDirectory, fileName), true))
				{
					streamWriter.WriteLine(obj.ToString());
				}
			}
		}

		// Token: 0x0600007D RID: 125 RVA: 0x00005F20 File Offset: 0x00004120
		public static void LogToConsole(string message)
		{
			message.Replace("\t", "    ");
			ETGModConsole.Log(message, false);
		}

		// Token: 0x0600007E RID: 126 RVA: 0x00005F3C File Offset: 0x0000413C
		private static void BreakdownComponentsInternal(this GameObject obj, int lvl = 0)
		{
			string text = "";
			for (int i = 0; i < lvl; i++)
			{
				text += "\t";
			}
			Tools.Log<string>(text + obj.name + "...");
			foreach (Component component in obj.GetComponents<Component>())
			{
				string str = text;
				string str2 = "    -";
				Type type = component.GetType();
				Tools.Log<string>(str + str2 + ((type != null) ? type.ToString() : null));
			}
			foreach (Transform transform in obj.GetComponentsInChildren<Transform>())
			{
				bool flag = transform != obj.transform;
				if (flag)
				{
					transform.gameObject.BreakdownComponentsInternal(lvl + 1);
				}
			}
		}

		// Token: 0x0600007F RID: 127 RVA: 0x00006013 File Offset: 0x00004213
		public static void BreakdownComponents(this GameObject obj)
		{
			obj.BreakdownComponentsInternal(0);
		}

		// Token: 0x06000080 RID: 128 RVA: 0x00006020 File Offset: 0x00004220
		public static void ExportTexture(Texture texture, string folder = "")
		{
			string text = Path.Combine(ETGMod.ResourcesDirectory, folder);
			bool flag = !Directory.Exists(text);
			if (flag)
			{
				Directory.CreateDirectory(text);
			}
			File.WriteAllBytes(Path.Combine(text, texture.name + DateTime.Now.Ticks.ToString() + ".png"), ((Texture2D)texture).EncodeToPNG());
		}

		// Token: 0x06000081 RID: 129 RVA: 0x0000608C File Offset: 0x0000428C
		public static T GetEnumValue<T>(string val) where T : Enum
		{
			return (T)((object)Enum.Parse(typeof(T), val.ToUpper()));
		}

		// Token: 0x06000082 RID: 130 RVA: 0x000060B8 File Offset: 0x000042B8
		public static void LogPropertiesAndFields<T>(T obj, string header = "")
		{
			Tools.Log<string>(header);
			Tools.Log<string>("=======================");
			bool flag = obj == null;
			if (flag)
			{
				Tools.Log<string>("LogPropertiesAndFields: Null object");
			}
			else
			{
				Type type = obj.GetType();
				Tools.Log<string>(string.Format("Type: {0}", type));
				PropertyInfo[] properties = type.GetProperties();
				Tools.Log<string>(string.Format("{0} Properties: ", typeof(T)));
				foreach (PropertyInfo propertyInfo in properties)
				{
					try
					{
						object value = propertyInfo.GetValue(obj, null);
						string text = value.ToString();
						bool flag2 = ((obj != null) ? obj.GetType().GetGenericTypeDefinition() : null) == typeof(List<>);
						bool flag3 = flag2;
						if (flag3)
						{
							List<object> list = value as List<object>;
							text = string.Format("List[{0}]", list.Count);
							foreach (object obj2 in list)
							{
								text = text + "\n\t\t" + obj2.ToString();
							}
						}
						Tools.Log<string>("\t" + propertyInfo.Name + ": " + text);
					}
					catch
					{
					}
				}
				Tools.Log<string>(string.Format("{0} Fields: ", typeof(T)));
				FieldInfo[] fields = type.GetFields();
				foreach (FieldInfo fieldInfo in fields)
				{
					Tools.Log<string>(string.Format("\t{0}: {1}", fieldInfo.Name, fieldInfo.GetValue(obj)));
				}
			}
		}

		// Token: 0x06000083 RID: 131 RVA: 0x000062BC File Offset: 0x000044BC
		public static void StartTimer(string name)
		{
			string key = name.ToLower();
			bool flag = Tools.timers.ContainsKey(key);
			if (flag)
			{
				Tools.PrintError<string>("Timer " + name + " already exists.", "FF0000");
			}
			else
			{
				Tools.timers.Add(key, Time.realtimeSinceStartup);
			}
		}

		// Token: 0x06000084 RID: 132 RVA: 0x00006310 File Offset: 0x00004510
		public static void StopTimerAndReport(string name)
		{
			string key = name.ToLower();
			bool flag = !Tools.timers.ContainsKey(key);
			if (flag)
			{
				Tools.PrintError<string>("Could not stop timer " + name + ", no such timer exists", "FF0000");
			}
			else
			{
				float num = Tools.timers[key];
				int num2 = (int)((Time.realtimeSinceStartup - num) * 1000f);
				Tools.timers.Remove(key);
				Tools.Print<string>(name + " finished in " + num2.ToString() + "ms", "FFFFFF", false);
			}
		}

		// Token: 0x0400003F RID: 63
		public static bool verbose = false;

		// Token: 0x04000040 RID: 64
		private static string defaultLog = Path.Combine(ETGMod.ResourcesDirectory, "customCharacterLog.txt");

		// Token: 0x04000041 RID: 65
		public static string modID = "NN";

		// Token: 0x04000042 RID: 66
		private static Dictionary<string, float> timers = new Dictionary<string, float>();
	}
}
