using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.ComponentModel;  //debug
using System.IO;
using System.Runtime.InteropServices; // audio loading
using System.Text.RegularExpressions;

using UnityEngine;
using MonoMod.RuntimeDetour;
using MonoMod.Cil;
using Mono.Cecil.Cil; //Instruction

using Gungeon;
using Dungeonator;
using Alexandria.ItemAPI;

namespace CwaffingTheGungy
{
    public class C // constants
    {
        public static readonly bool DEBUG_BUILD = true; // set to false for release builds (must be readonly instead of const to avoid build warnings)
        public const string MOD_NAME        = "Gungeon Craft";
        public const string MOD_INT_NAME    = "CwaffingTheGungy";

        public const string MOD_PREFIX      = "cg";
        public const float  PIXELS_PER_TILE = 16f;
        public const float  PIXELS_PER_CELL = 64f;
        public const float  FPS             = 60f;
    }

    public class IDs // global IDs for this mod's guns and items
    {
        public static Dictionary<string, int>    Pickups       { get; set; } = new Dictionary<string, int>();
        public static Dictionary<string, int>    Guns          { get; set; } = new Dictionary<string, int>();
        public static Dictionary<string, int>    Actives       { get; set; } = new Dictionary<string, int>();
        public static Dictionary<string, int>    Passives      { get; set; } = new Dictionary<string, int>();
        public static Dictionary<string, string> InternalNames { get; set; } = new Dictionary<string, string>();
    }

    public static class ResMap // Resource map from PNG stem names to lists of paths to all PNGs with those names (i.e., animation frames)
    {
        private static Regex _NumberAtEnd = new Regex(@"^(.*?)(_?)([0-9]+)$",
          RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static Dictionary<string, List<string>> _ResMap = new ();

        // Gets a list of resource paths with numbered sprites from the resource's base name
        // Does not work with CreateProjectileAnimation(), which expects direct sprite names in the mod's "sprites" directory
        public static List<string> Get(string resource, bool expectFailure = false)
        {
            if (_ResMap.ContainsKey(resource))
                return _ResMap[resource];
            if (!expectFailure)
                ETGModConsole.Log($"failed to retrieve \"{resource}\" from resmap");
            return null;
        }

        // Gets only the basenames for each item in a list of strings
        public static List<string> Base(this List<string> paths)
        {
            List<string> bases = new();
            foreach(string s in paths)
                bases.Add(s.Substring(s.LastIndexOf("/") + 1));
            return bases;
        }


        // Builds a resource map from every PNG embedded in the assembly
        public static void Build()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            Dictionary<string, string[]> tempMap = new ();
            // Get the name of each PNG resource and stuff it into a sorted array by its index number
            foreach(string s in ResourceExtractor.GetResourceNames())
            {
                if (!s.EndsWithInvariant(".png"))
                    continue;
                string path = s.Replace('.','/').Substring(0, s.Length - 4);
                string[] tokens = path.Split('/');
                string baseName = tokens[tokens.Length - 1];
                MatchCollection matches = _NumberAtEnd.Matches(baseName);
                // If we aren't numbered at the end, we're just a singular sprite
                if (matches.Count == 0)
                {
                    if (!tempMap.ContainsKey(baseName))
                        tempMap[baseName] = new string[1];
                    tempMap[baseName][0] = path;
                    continue;
                }
                foreach (Match match in matches)
                {
                    string name = match.Groups[1].Value;
                    if (name.Length == 0)
                        continue; // don't allow 0-length keys
                    int index = Int32.Parse(match.Groups[3].Value);
                    if (index == 0)
                        continue; // don't allow 0 for an index
                    if (!tempMap.ContainsKey(name))
                        tempMap[name] = new string[index];
                    if (index > tempMap[name].Length)
                    {
                        string[] arr = tempMap[name];
                        Array.Resize(ref arr, index);
                        tempMap[name] = arr;
                    }
                    tempMap[name][index - 1] = path;
                }
            }

            // Convert our arrays to lists
            foreach(KeyValuePair<string, string[]> entry in tempMap)
                _ResMap[entry.Key] = new List<string>(entry.Value);

            // Hint to the GC we want to unload the tempMap
            tempMap = null;

            // Debug sanity check
            // foreach(KeyValuePair<string, List<string>> entry in _ResMap)
            // {
            //     ETGModConsole.Log($"{entry.Key} ->");
            //     foreach (string s in entry.Value)
            //         ETGModConsole.Log($"  {s}");
            // }
            watch.Stop();
            if (C.DEBUG_BUILD)
                ETGModConsole.Log($"Built resource map in {watch.ElapsedMilliseconds} milliseconds");
        }
    }

    public static class Dissect // reflection helper methods for being a lazy dumdum
    {
        public static void DumpComponents(this GameObject g)
        {
            foreach (var c in g.GetComponents(typeof(object)))
                ETGModConsole.Log("  "+c.GetType().Name);

        }

        public static void DumpFieldsAndProperties<T>(T o)
        {
            // Type type = o.GetType();
            Type type = typeof(T);
            foreach (var f in type.GetFields())
                Console.WriteLine(String.Format("field {0} = {1}", f.Name, f.GetValue(o)));
            foreach(PropertyDescriptor d in TypeDescriptor.GetProperties(o))
                Console.WriteLine(" prop {0} = {1}", d.Name, d.GetValue(o));
        }

        public static void CompareFieldsAndProperties<T>(T o1, T o2)
        {
            // Type type = o.GetType();
            Type type = typeof(T);
            foreach (var f in type.GetFields()) {
                try
                {
                    if (f.GetValue(o1) == null)
                    {
                        if (f.GetValue(o2) == null)
                            continue;
                    }
                    else if (f.GetValue(o2) != null && f.GetValue(o1).Equals(f.GetValue(o2)))
                        continue;
                    Console.WriteLine(
                        String.Format("field {0} = {1} -> {2}", f.Name, f.GetValue(o1), f.GetValue(o2)));
                }
                catch (Exception)
                {
                    Console.WriteLine(" prop {0} = {1} -> {2}", f.Name, "ERROR", "ERROR");
                }
            }
            foreach(PropertyDescriptor f in TypeDescriptor.GetProperties(o1))
            {
                try {
                    if (f.GetValue(o1) == null)
                    {
                        if (f.GetValue(o2) == null)
                            continue;
                    }
                    else if (f.GetValue(o2) != null && f.GetValue(o1).Equals(f.GetValue(o2)))
                        continue;
                    Console.WriteLine(" prop {0} = {1} -> {2}", f.Name, f.GetValue(o1), f.GetValue(o2));
                }
                catch (Exception)
                {
                    Console.WriteLine(" prop {0} = {1} -> {2}", f.Name, "ERROR", "ERROR");
                }
            }
        }

        public static void PrintSpriteCollectionNames(tk2dSpriteCollectionData theCollection)
        {
            for (int i = 0; i < theCollection.spriteDefinitions.Length; ++i)
                ETGModConsole.Log(theCollection.spriteDefinitions[i].name);
        }

        public static void DumpILInstruction(this Instruction c)
        {
            try
            {
                ETGModConsole.Log($"  {c.ToStringSafe()}");
            }
            catch (Exception)
            {
                try
                {
                    ILLabel label = null;
                    if (label == null) c.MatchBr(out label);
                    if (label == null) c.MatchBeq(out label);
                    if (label == null) c.MatchBge(out label);
                    if (label == null) c.MatchBgeUn(out label);
                    if (label == null) c.MatchBgt(out label);
                    if (label == null) c.MatchBgtUn(out label);
                    if (label == null) c.MatchBle(out label);
                    if (label == null) c.MatchBleUn(out label);
                    if (label == null) c.MatchBlt(out label);
                    if (label == null) c.MatchBltUn(out label);
                    if (label == null) c.MatchBrfalse(out label);
                    if (label == null) c.MatchBrtrue(out label);
                    if (label == null) c.MatchBneUn(out label);
                    if (label != null)
                        ETGModConsole.Log($"  IL_{c.Offset.ToString("x4")}: {c.OpCode.Name} IL_{label.Target.Offset.ToString("x4")}");
                    else
                        ETGModConsole.Log($"[UNKNOWN INSTRUCTION]");
                        // ETGModConsole.Log($"  IL_{c.Offset.ToString("x4")}: {c.OpCode.Name} {c.Operand.ToStringSafe()}");
                }
                catch (Exception)
                {
                    ETGModConsole.Log($"  <error>");
                }
            }
        }

        // Dump IL instructions for an IL Hook
        private static HashSet<string> _DumpedKeys = new();
        public static void DumpILOnce(this ILCursor cursor, string key)
        {
            if (_DumpedKeys.Contains(key))
                return;
            _DumpedKeys.Add(key);
            foreach (Instruction c in cursor.Instrs)
                DumpILInstruction(c);
        }

        private static List<string> _Bundles = new(){
            "shared_auto_001",
            "shared_auto_002",
            "shared_base_001",
            "brave_resources_001",
            "enemies_base_001",
            "encounters_base_001",
            // "dungeon_scene_001", // seems to work in base game code???

            "dungeons/base_castle",
            "dungeons/base_sewer",
            "dungeons/base_gungeon",
            "dungeons/base_cathedral",
            "dungeons/base_mines",
            "dungeons/base_resourcefulrat",
            "dungeons/base_catacombs",
            "dungeons/base_forge",
            "dungeons/base_bullethell",

            // "dungeons/base_office", // not real asset bundles???
            // "dungeons/base_space",
            // "dungeons/base_jungle",
            // "dungeons/base_belly",
            // "dungeons/base_west",
            // "dungeons/base_phobos",
            };

        // Attempt to load a prefab from various default resource packs (SLOW, DEBUG ONLY)
        public static GameObject FindDefaultResource(string name)
        {
            foreach (string bundle in _Bundles)
            {
                try
                {
                    GameObject res = ResourceManager.LoadAssetBundle(bundle).LoadAsset<GameObject>(name);
                    if (res == null)
                        continue;
                    ETGModConsole.Log($"found asset {name} in bundle {bundle}");
                    return res;
                }
                catch (Exception)
                {
                    ETGModConsole.Log($"failed to load bundle {bundle}");
                    continue;
                }
            }
            ETGModConsole.Log($"  could not find asset {name} in any bundle!");
            return null;
        }

        public static void FindDefaultResource<T>(string name)
            where T : MonoBehaviour
        {
            foreach (string bundle in _Bundles)
            {
                if (ResourceManager.LoadAssetBundle(bundle).LoadAsset<T>(name) == null)
                    continue;
                ETGModConsole.Log($"found asset {name} in bundle {bundle}");
                break;
            }
        }
    }

    public static class ReflectionHelpers // reflection helpers ultimately stolen from apache
    {
        public static T ReflectGetField<T>(Type classType, string fieldName, object o = null) {
            FieldInfo field = classType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | ((o != null) ? BindingFlags.Instance : BindingFlags.Static));
            return (T)field.GetValue(o);
        }

        public static void ReflectSetField<T>(Type classType, string fieldName, T value, object o = null) {
            FieldInfo field = classType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | ((o != null) ? BindingFlags.Instance : BindingFlags.Static));
            field.SetValue(o, value);
        }
    }

    public class SpawnObjectManager : MonoBehaviour // stolen from nn
    {
        public static GameObject SpawnObject(GameObject thingToSpawn, Vector3 convertedVector, GameObject SpawnVFX = null, bool correctForWalls = false)
        {
            Vector2 Vector2Position = convertedVector;

            GameObject newObject = Instantiate(thingToSpawn, convertedVector, Quaternion.identity);

            SpeculativeRigidbody ObjectSpecRigidBody = newObject.GetComponentInChildren<SpeculativeRigidbody>();
            UnityEngine.Component[] componentsInChildren = newObject.GetComponentsInChildren(typeof(IPlayerInteractable));
            for (int i = 0; i < componentsInChildren.Length; i++)
            {
                // ETGModConsole.Log(" == "+componentsInChildren[i].GetType());
                IPlayerInteractable interactable = componentsInChildren[i] as IPlayerInteractable;
                if (interactable != null)
                {
                    newObject.transform.position.GetAbsoluteRoom().RegisterInteractable(interactable);
                }
            }
            UnityEngine.Component[] componentsInChildren2 = newObject.GetComponentsInChildren(typeof(IPlaceConfigurable));
            for (int i = 0; i < componentsInChildren2.Length; i++)
            {
                IPlaceConfigurable placeConfigurable = componentsInChildren2[i] as IPlaceConfigurable;
                if (placeConfigurable != null)
                {
                    placeConfigurable.ConfigureOnPlacement(GameManager.Instance.Dungeon.data.GetAbsoluteRoomFromPosition(Vector2Position.ToIntVector2()));
                }
            }
            /* FlippableCover component7 = newObject.GetComponentInChildren<FlippableCover>();
             component7.transform.position.XY().GetAbsoluteRoom().RegisterInteractable(component7);
             component7.ConfigureOnPlacement(component7.transform.position.XY().GetAbsoluteRoom());*/

            ObjectSpecRigidBody.Initialize();
            PhysicsEngine.Instance.RegisterOverlappingGhostCollisionExceptions(ObjectSpecRigidBody, null, false);

            if (SpawnVFX != null)
                UnityEngine.Object.Instantiate<GameObject>(SpawnVFX, ObjectSpecRigidBody.sprite.WorldCenter, Quaternion.identity);
            if (correctForWalls) CorrectForWalls(newObject);

            return newObject;
        }
        private static void CorrectForWalls(GameObject portal)
        {
            SpeculativeRigidbody rigidbody = portal.GetComponent<SpeculativeRigidbody>();
            if (!rigidbody)
                return;

            bool flag = PhysicsEngine.Instance.OverlapCast(rigidbody, null, true, false, null, null, false, null, null, new SpeculativeRigidbody[0]);
            if (!flag)
                return;

            Vector2 vector = portal.transform.position.XY();
            IntVector2[] cardinalsAndOrdinals = IntVector2.CardinalsAndOrdinals;
            for (int num2 = 1; num2 <= 200; ++num2)
            {
                for (int i = 0; i < cardinalsAndOrdinals.Length; i++)
                {
                    portal.transform.position = vector + PhysicsEngine.PixelToUnit(cardinalsAndOrdinals[i] * num2);
                    rigidbody.Reinitialize();
                    if (!PhysicsEngine.Instance.OverlapCast(rigidbody, null, true, false, null, null, false, null, null, new SpeculativeRigidbody[0]))
                        return;
                }
            }
            UnityEngine.Debug.LogError("FREEZE AVERTED!  TELL RUBEL!  (you're welcome) 147");
        }
    }

    public class AudioResourceLoader // example audio resource loading class and functions
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
                if (text2.IndexOf(prefix) != 0)
                    continue;

                text2 = text2.Substring(text2.IndexOf(prefix) + prefix.Length);
                if (text2.LastIndexOf(".bnk") != text2.Length - ".bnk".Length)
                    continue;

                text2 = text2.Substring(0, text2.Length - ".bnk".Length);
                bool flag6 = text2.IndexOf(Path.DirectorySeparatorChar) == 0;
                if (flag6) { text2 = text2.Substring(1); }
                text2 = prefix + ":" + text2;
                // Console.WriteLine(string.Format("{0}: Soundbank found, attempting to autoload: name='{1}' resource='{2}'", "hi", text2, text));
                using (Stream manifestResourceStream = assembly.GetManifestResourceStream(text))
                {
                    LoadSoundbankFromStream(manifestResourceStream, text2);
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

