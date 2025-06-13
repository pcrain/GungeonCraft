namespace CwaffingTheGungy;

public class C // constants and common variables
{
    public static readonly bool DEBUG_BUILD = true; // set to false for release builds (must be readonly instead of const to avoid build warnings)

    public const string MOD_NAME     = "GungeonCraft";
    public const string MOD_INT_NAME = "CwaffingTheGungy";
    public const string MOD_VERSION  = "1.26.2";
    public const string MOD_GUID     = "pretzel.etg.cwaff";
    public const string MOD_PREFIX   = "cg";

    public static readonly Color MOD_COLOR = new Color(0.67f, 1.00f, 0.67f);

    public const float  PIXELS_PER_TILE = 16f;
    public const float  PIXELS_PER_CELL = 64f;
    public const float  FPS             = 60f;
    public const float  FRAME           = 1f / FPS;
    public const float  PIXEL_SIZE      = 1f / PIXELS_PER_TILE;
}

public static class ResMap // Resource map from PNG stem names to lists of paths to all PNGs with those names (i.e., animation frames)
{
    private static Regex _NumberAtEnd = new Regex(@"^(.*?)(_?)([0-9]+)$",
      RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static Dictionary<string, List<string>> _ResMap = new();

    // Gets a list of resource paths with numbered sprites from the resource's base name
    // Does not work with CreateProjectileAnimation(), which expects direct sprite names in the mod's "sprites" directory
    public static List<string> Get(string resource, bool quietFailure = false)
    {
        if (_ResMap.TryGetValue(resource, out List<string> paths))
            return paths;
        if (!quietFailure)
            ETGModConsole.Log($"failed to retrieve \"{resource}\" from resmap");
        return null;
    }

    public static bool Has(string resource) => _ResMap.ContainsKey(resource);

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
        Dictionary<string, string[]> tempMap = new ();
        // Get the name of each PNG resource and stuff it into a sorted array by its index number
        foreach(string s in AtlasHelper._PackedTextures.Keys)
        {
            Match match = _NumberAtEnd.Match(s);
            // If we aren't numbered at the end, we're just a singular sprite
            if (!match.Success)
            {
                string baseName = s.Substring(s.LastIndexOf('.') + 1);
                if (!tempMap.ContainsKey(baseName))
                    tempMap[baseName] = new string[1];
                tempMap[baseName][0] = s.Replace('.','/');
                continue;
            }
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
            tempMap[name][index - 1] = s.Replace('.','/');
        }

        // Convert our arrays to lists
        foreach(KeyValuePair<string, string[]> entry in tempMap)
            _ResMap[entry.Key] = new List<string>(entry.Value);

        // Hint to the GC we want to unload the tempMap
        tempMap = null;
    }
}

public static class Dissect // reflection helper methods for being a lazy dumdum
{
    public static void DumpComponents(this GameObject g, bool recursive = true, int indent=0)
    {
        ETGModConsole.Log($"{string.Empty.PadLeft(indent)}components in {g.name}");
        foreach (var c in g.GetComponents(typeof(object)))
            ETGModConsole.Log("  "+c.GetType().Name);
        if (!recursive)
            return;
        foreach (Transform child in g.transform.Children())
            DumpComponents(child.gameObject, true, indent + 1);
    }

    public static void DumpFieldsAndProperties<T>(T o)
    {
        foreach (var f in typeof(T).GetFields())
            Console.WriteLine(String.Format("field {0} = {1}", f.Name, f.GetValue(o)));
        foreach(PropertyDescriptor d in TypeDescriptor.GetProperties(o))
            Console.WriteLine(" prop {0} = {1}", d.Name, d.GetValue(o));
    }

    public static void CompareFieldsAndProperties<T>(T o1, T o2)
    {
        // System.Console.WriteLine($"comparing fields and properties for {typeof(T)}");
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

    public static string DumpILInstruction(this Instruction c)
    {
        try { return c.ToString(); }
        catch { }
        try
        {
            if (c.OpCode.OperandType is OperandType.InlineBrTarget or OperandType.ShortInlineBrTarget && c.Operand is ILLabel l)
                return $"IL_{c.Offset:x4}: {c.OpCode.Name} IL_{l.Target.Offset:x4}";

            if (c.OpCode.OperandType is OperandType.InlineSwitch && c.Operand is IEnumerable<ILLabel> en)
                return $"IL_{c.Offset:x4}: {c.OpCode.Name} {string.Join(", ", en.Select(x => x.Target.Offset.ToString("x4")).ToArray())}";
        }
        catch { }
        return "This shouldn't be happening";
    }

    public static void DumpIL(this ILCursor cursor)
    {
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
                // if (C.DEBUG_BUILD)
                //     ETGModConsole.Log($"found asset {name} in bundle {bundle}");
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
}

public static class AudioResourceLoader
{
    public static unsafe void AutoloadFromAssembly()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        string assemblyName = assembly.GetName().Name;
        foreach (string resPath in assembly.GetManifestResourceNames())
        {
            int extPos = resPath.Length - ".bnk".Length;
            if (resPath.LastIndexOf(".bnk") != extPos)
                continue;
            int bankPos = resPath.LastIndexOf('.', extPos - 1) + 1;
            string bankName = assemblyName + ":" + resPath.Substring(bankPos, extPos - bankPos);
            using (Stream stream = assembly.GetManifestResourceStream(resPath))
            {
                byte[] array = new byte[stream.Length];
                stream.Read(array, 0, array.Length);
                fixed (byte* p = array)
                    AkSoundEngine.LoadAndDecodeBankFromMemory((IntPtr)p, (uint)array.Length, false, bankName, false, out _);
            }
        }
    }
}

// Easing functions from https://gist.github.com/Kryzarel/bba64622057f21a1d6d44879f9cd7bd4
// Made with the help of this great post: https://joshondesign.com/2013/03/01/improvedEasingEquations

// --------------------------------- Other Related Links --------------------------------------------------------------------
// Original equations, bad formulation: https://github.com/danro/jquery-easing/blob/master/jquery.easing.js
// A few equations, very simplified:    https://gist.github.com/gre/1650294
// Easings.net equations, simplified:   https://github.com/ai/easings.net/blob/master/src/easings/easingsFunctions.ts

public static class Ease
{
    public static float Linear(float t) => t;

    public static float InQuad(float t) => t * t;
    public static float OutQuad(float t) => 1 - InQuad(1 - t);
    public static float InOutQuad(float t)
    {
        if (t < 0.5) return InQuad(t * 2) / 2;
        return 1 - InQuad((1 - t) * 2) / 2;
    }

    public static float InCubic(float t) => t * t * t;
    public static float OutCubic(float t) => 1 - InCubic(1 - t);
    public static float InOutCubic(float t)
    {
        if (t < 0.5) return InCubic(t * 2) / 2;
        return 1 - InCubic((1 - t) * 2) / 2;
    }

    public static float InQuart(float t) => t * t * t * t;
    public static float OutQuart(float t) => 1 - InQuart(1 - t);
    public static float InOutQuart(float t)
    {
        if (t < 0.5) return InQuart(t * 2) / 2;
        return 1 - InQuart((1 - t) * 2) / 2;
    }

    public static float InQuint(float t) => t * t * t * t * t;
    public static float OutQuint(float t) => 1 - InQuint(1 - t);
    public static float InOutQuint(float t)
    {
        if (t < 0.5) return InQuint(t * 2) / 2;
        return 1 - InQuint((1 - t) * 2) / 2;
    }

    public static float InSine(float t) => (float)-Math.Cos(t * Math.PI / 2);
    public static float OutSine(float t) => (float)Math.Sin(t * Math.PI / 2);
    public static float InOutSine(float t) => (float)(Math.Cos(t * Math.PI) - 1) / -2;

    public static float InExpo(float t) => (float)Math.Pow(2, 10 * (t - 1));
    public static float OutExpo(float t) => 1 - InExpo(1 - t);
    public static float InOutExpo(float t)
    {
        if (t < 0.5) return InExpo(t * 2) / 2;
        return 1 - InExpo((1 - t) * 2) / 2;
    }

    public static float InCirc(float t) => -((float)Math.Sqrt(1 - t * t) - 1);
    public static float OutCirc(float t) => 1 - InCirc(1 - t);
    public static float InOutCirc(float t)
    {
        if (t < 0.5) return InCirc(t * 2) / 2;
        return 1 - InCirc((1 - t) * 2) / 2;
    }

    public static float InElastic(float t) => 1 - OutElastic(1 - t);
    public static float OutElastic(float t)
    {
        float p = 0.3f;
        return (float)Math.Pow(2, -10 * t) * (float)Math.Sin((t - p / 4) * (2 * Math.PI) / p) + 1;
    }
    public static float InOutElastic(float t)
    {
        if (t < 0.5) return InElastic(t * 2) / 2;
        return 1 - InElastic((1 - t) * 2) / 2;
    }

    public static float InBack(float t)
    {
        float s = 1.70158f;
        return t * t * ((s + 1) * t - s);
    }
    public static float OutBack(float t) => 1 - InBack(1 - t);
    public static float InOutBack(float t)
    {
        if (t < 0.5) return InBack(t * 2) / 2;
        return 1 - InBack((1 - t) * 2) / 2;
    }

    public static float InBounce(float t) => 1 - OutBounce(1 - t);
    public static float OutBounce(float t)
    {
        float div = 2.75f;
        float mult = 7.5625f;

        if (t < 1 / div)
        {
            return mult * t * t;
        }
        else if (t < 2 / div)
        {
            t -= 1.5f / div;
            return mult * t * t + 0.75f;
        }
        else if (t < 2.5 / div)
        {
            t -= 2.25f / div;
            return mult * t * t + 0.9375f;
        }
        else
        {
            t -= 2.625f / div;
            return mult * t * t + 0.984375f;
        }
    }
    public static float InOutBounce(float t)
    {
        if (t < 0.5) return InBounce(t * 2) / 2;
        return 1 - InBounce((1 - t) * 2) / 2;
    }
}
