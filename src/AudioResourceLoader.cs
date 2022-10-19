using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;

namespace CwaffingTheGungy
{

    public class AudioResourceLoader
    {

        public static void InitAudio() { AutoloadFromAssembly(Assembly.GetExecutingAssembly(), "CwaffingTheGungy"); }

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
