//NOTE: uncomment to make every chest item the same
// #define DEBUGCHESTS
//NOTE: uncomment to print out properties of all spawned VFXComplex object
// #define DEBUGVFX

namespace CwaffingTheGungy;

/// <summary>Class mostly containing debug Harmony patches that can be commented / uncommented as needed</summary>
[HarmonyPatch]
internal static class CwaffDebug
{

  #if DEBUG && DEBUGCHESTS
    // private static int _DebugItemId => Lazy.PickupId<Gunbrella>();
    // private static int _DebugItemId => (int)Items.CoolantLeak;
    // private static int _DebugItemId => (int)Items.Casey;
    // private static int _DebugItemId => (int)Items.Ration;
    private static int _DebugItemId => (int)Items.Gunther;

    [HarmonyPatch(typeof(RewardPedestal), nameof(RewardPedestal.DetermineContents))]
    [HarmonyPrefix]
    static void DebugRewardPedestalPatch(RewardPedestal __instance, PlayerController player)
    {
      if (C.DEBUG_BUILD)
        __instance.contents = PickupObjectDatabase.GetById(_DebugItemId);
    }

    [HarmonyPatch(typeof(Chest), nameof(Chest.DetermineContents))]
    [HarmonyPrefix]
    static void DebugChestContentsPatch(Chest __instance, PlayerController player, int tierShift)
    {
      if (C.DEBUG_BUILD)
        __instance.forceContentIds = new(){_DebugItemId};
    }

    [HarmonyPatch(typeof(ShopItemController), nameof(ShopItemController.InitializeInternal))]
    [HarmonyPrefix]
    static void DebugShopItemPatch(ShopItemController __instance, ref PickupObject i)
    {
      if (C.DEBUG_BUILD)
        i = PickupObjectDatabase.GetById(_DebugItemId);
    }

    [HarmonyPatch(typeof(CustomShopItemController), nameof(CustomShopItemController.InitializeInternal))]
    [HarmonyPrefix]
    static void DebugCustomShopItemPatch(CustomShopItemController __instance, ref PickupObject i)
    {
      if (C.DEBUG_BUILD)
        i = PickupObjectDatabase.GetById(_DebugItemId);
    }
  #endif

  #if DEBUG && DEBUGVFX
    // protected void InternalSpawnAtLocation(Vector3 position, float zRotation, Transform parent, Vector2? sourceNormal, Vector2? sourceVelocity, Action<VFXObject, tk2dSprite> vfxSpriteManipulator, bool keepReferences, SpawnMethod spawnMethod, bool ignoresPools)
    [HarmonyPatch]
    private static class VFXComplexInternalSpawnAtLocationPatch
    {
        [HarmonyPatch(typeof(VFXComplex), nameof(VFXComplex.InternalSpawnAtLocation))]
        static void Prefix(VFXComplex __instance, Vector3 position, float zRotation, Transform parent, Vector2? sourceNormal, Vector2? sourceVelocity, Action<VFXObject, tk2dSprite> vfxSpriteManipulator, bool keepReferences, VFXComplex.SpawnMethod spawnMethod, bool ignoresPools)
        {
            System.Console.WriteLine($"spawning VFX with the following properties:");
            System.Console.WriteLine($"  num vfx: {__instance.effects.Length}");
            for (int i = 0; i < __instance.effects.Length; ++i)
            {
                GameObject v = __instance.effects[i].effect;
                if (!v)
                {
                    System.Console.WriteLine($"   vfx {i+1} name: NULL");
                    continue;
                }
                System.Console.WriteLine($"   vfx {i+1} name: {v.name}");
                System.Console.WriteLine($"    vfx {i+1} gameObject layer: {v.layer} ({LayerMask.LayerToName(v.layer)})");
                System.Console.WriteLine($"    vfx {i+1} render layer: {v.GetComponent<MeshRenderer>().sortingLayerName} ({v.GetComponent<MeshRenderer>().sortingLayerID})");
                tk2dBaseSprite s = v.GetComponent<tk2dBaseSprite>();
                System.Console.WriteLine($"    vfx {i+1} sprite: {s.GetCurrentSpriteDef().name}");
                Material m = s.renderer.material;
                System.Console.WriteLine($"    vfx {i+1} material: {m.name} (override? {s.usesOverrideMaterial})");
                System.Console.WriteLine($"    vfx {i+1} shader: {m.shader.name}");
                System.Console.WriteLine($"    vfx {i+1} emissive color: {m.GetColor("_EmissiveColor")}");
                System.Console.WriteLine($"    vfx {i+1} emissive power: {m.GetFloat("_EmissivePower")}");
                System.Console.WriteLine($"    vfx {i+1} emissive color power: {m.GetFloat("_EmissiveColorPower")}");
                System.Console.WriteLine($"    vfx {i+1} emissive sensitivity: {m.GetFloat("_EmissiveThresholdSensitivity")}");
            }
        }
    }
  #endif
}

/// <summary>Profiling helper class</summary>
public static class CwaffProfile
{
    private class ProfileData
    {
        public System.Diagnostics.Stopwatch watch = new();
        public long startBytes = 0;
        public string shortName = null;
    }

    private static readonly Dictionary<string, ProfileData> _ProfileData = new();

    public static void ProfileType<T>(this Harmony harmony)
    {
        Type t = typeof(T);
        MethodInfo Prefix = typeof(CwaffProfile).GetMethod(nameof(ProfileMethodPrefix), AccessTools.all);
        MethodInfo Postfix = typeof(CwaffProfile).GetMethod(nameof(ProfileMethodPostfix), AccessTools.all);
        foreach (MethodInfo mi in typeof(T).GetMethods())
        {
            if (mi.DeclaringType != t)
                continue;
            try
            {
                harmony.Patch(mi, prefix: new HarmonyMethod(Prefix), new HarmonyMethod(Postfix));
                ETGModConsole.Log($"  Patched {mi.FullDescription()}");
            }
            catch (Exception)
            {
                ETGModConsole.Log($"  Exception patching {mi.FullDescription()}");
            }
        }
    }

    private static readonly string _RX = @".*<.*::(.*)>";
    private static void ProfileMethodPrefix()
    {
        MethodBase m = new System.Diagnostics.StackTrace().GetFrame(1).GetMethod();
        string name = m.Name;
        if (!_ProfileData.TryGetValue(name, out ProfileData pd))
        {
            pd = _ProfileData[name] = new();
            pd.shortName = Regex.Replace(name, _RX, "$1");
        }
        pd.watch.Reset();
        pd.watch.Start();
        pd.startBytes = GC.GetTotalMemory(false);
    }

    private static void ProfileMethodPostfix()
    {
        string name = new System.Diagnostics.StackTrace().GetFrame(1).GetMethod().Name;
        if (!_ProfileData.TryGetValue(name, out ProfileData pd))
            return;
        pd.watch.Stop();
        long bytesUsed = GC.GetTotalMemory(false) - pd.startBytes;
        if (bytesUsed <= 8192)
            return;
        System.Console.WriteLine($"  used {bytesUsed:8} bytes in {pd.watch.ElapsedTicks,8} ticks during {pd.shortName}");
    }
}

// Constructor profiler, adapted from: https://github.com/BepInEx/BepInEx.Debug/blob/master/src/ConstructorProfiler/ConstructorProfiler.cs
public static class ConstructorProfiler
{
    private static string[] AssFilter = new[] { "Assembly-CSharp", "UnityEngine" };
    private static Dictionary<string, StackData> CallCounter = new Dictionary<string, StackData>();

    [System.Diagnostics.Conditional("DEBUG")]
    public static void Enable()
    {
        Harmony harmony = new Harmony(nameof(ConstructorProfiler));
        string[] bannedAssemblies = new[] { "ConstructorProfiler", "mscorlib" };
        var asses = AppDomain.CurrentDomain.GetAssemblies().Where(x =>
            {
                // return x.FullName.Contains("Alexandria");
                if (bannedAssemblies.Any(y => x.FullName.Contains(y))) return false;
                //try{if (x.Location.Contains("BepInEx")) return true;}
                //catch{}


                return true; //false; //AssFilter.Contains(x.FullName.Split(',')[0]);
            })
            .ToList(); //.Where(ass => AssFilter.Contains(ass.FullName.Split(',')[0])).ToList();
        var types = asses.SelectMany(ass =>
            {
                try
                {
                    return ass.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    return ex.Types.Where(x => x != null);
                }
            }).Where(x => x.IsClass && !x.IsGenericType && !x.FullName.Contains("ConstructorProfiler") && !x.FullName.Contains("ReflectionUtil"))
            // }).Where(x => x.IsClass && !x.IsGenericType && !x.FullName.Contains("ConstructorProfiler"))
            //.Where(x =>
            //{
            //    if (x.Assembly.FullName.Contains("mscorlib"))
            //    {
            //        return  //(x.Namespace == null || x.Namespace == "System") &&
            //                !x.Name.Contains("Exception") &&
            //                !x.Name.Contains("Object") &&
            //                !x.Name.Contains("String") &&
            //                x.Namespace?.Contains("Diagnostics") != true;
            //    }
            //
            //    return true;
            //})
            .ToList();
        // var constructors = types.SelectMany(type => type.GetConstructors()).Where(x => !x.FullDescription().Contains("<") || !x.FullDescription().Contains(">")).ToList();
        var constructors = types.SelectMany(type => type.GetConstructors()).ToList(); // get all class constructors
        // var constructors = typeof(List<object>).GetConstructors().ToList(); // get all list constructors
        // var constructors = typeof(string).GetConstructors().ToList(); // doesn't work
        // var constructors = typeof(string).GetMethods().ToList();
        // var constructors = types.SelectMany(type => type.GetMethods().Where(m => m.FullDescription().Contains("Alexandria"))).ToList();

        foreach (var constructor in constructors)
        {
            try
            {
                System.Console.WriteLine($"Patching {constructor.FullDescription()}");
                harmony.Patch(constructor, new HarmonyMethod(AddCallMethodInfo));
            }
            catch (Exception/* e*/)
            {
                System.Console.WriteLine($"  Exception patching {constructor.FullDescription()}");
                // System.Console.WriteLine($"  Exception patching {constructor.FullDescription()}:\n{e}");
            }
        }
    }

    private static bool _Adding = false;
    private static MethodInfo AddCallMethodInfo = typeof(ConstructorProfiler).GetMethod(nameof(AddCall), AccessTools.all);
    [System.Diagnostics.Conditional("DEBUG")]
    private static void AddCall()
    {
        if (!run || _Adding)
        {
            return;
        }
        _Adding = true;
        var stackTrace = new System.Diagnostics.StackTrace();
        var key = stackTrace.ToString();

        if (CallCounter.TryGetValue(key, out var data))
        {
            CallCounter[key].count = data.count + 1;
        }
        else
        {
            CallCounter.Add(key, new StackData(stackTrace));
        }
        _Adding = false;
    }

    private static bool run;
    [System.Diagnostics.Conditional("DEBUG")]
    public static void Toggle()
    {
        // if (!Input.GetKeyDown(KeyCode.B))
        //     return;

        if (!run)
        {
            ETGModConsole.Log("Started collecting data");
            run = true;
            return;
        }

        ETGModConsole.Log($"Outputting data ({CallCounter.Count})");

        var counter = CallCounter;
        CallCounter = new Dictionary<string, StackData>();

        var results = counter.Values.OrderByDescending(x => x.count).Select(item =>
        {
            var ctorFrame = item.stackTrace.GetFrame(1);
            var createdType = ctorFrame.GetMethod().DeclaringType;
            var createdTypeStr = createdType?.FullName ?? ctorFrame.ToString();
            var stack = string.Join("\n", item.stackTrace.ToString().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Skip(2).ToArray());

            //var stack = string.Join("\n", item.stackTrace.GetFrames().Skip(2).Select(x =>
            //{
            //    var m = x.GetMethod();
            //    return m.DeclaringType?.FullName ?? "Unknown" + "." + m;
            //}).ToArray());
            return new { stack, createdTypeStr, count = item.count.ToString() };
        }).ToList();

        results.Insert(0, new { stack = "Stack", createdTypeStr = "Created object", count = "Count" });

        File.WriteAllLines(
            Path.Combine(Paths.GameRootPath, $"ConstructorProfiler{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.csv"),
            results.Select(x => $"\"{x.stack}\",\"{x.createdTypeStr}\",\"{x.count}\"").ToArray());
        run = false;
    }

    public class StackData
    {
        public System.Diagnostics.StackTrace stackTrace;
        public int count;

        public StackData(System.Diagnostics.StackTrace stackTrace)
        {
            this.stackTrace = stackTrace;
            count = 0;
        }
    }
}

internal static class DebugDraw
{
    [System.Diagnostics.Conditional("DEBUG")]
    internal static void DrawDebugCircle(this GameObject go, Vector2? pos = null, float? radius = null, Color? color = null)
    {
        go.GetOrAddComponent<DebugCircle>().Setup(pos, radius, color);
    }

    public class DebugCircle : MonoBehaviour
    {
        public Color color = default;
        public Vector2 pos = default;
        public float radius = 1f;

        private const int _CIRCLE_SEGMENTS = 12;

        private bool _didSetup = false;
        private GameObject _meshObject = null;
        private Mesh _mesh = null;
        private MeshRenderer _meshRenderer = null;
        private Vector3[] _vertices;

        private void CreateMesh()
        {
            this._meshObject = new GameObject("debug_circle");
            this._meshObject.SetLayerRecursively(LayerMask.NameToLayer("FG_Critical"));

            this._mesh = new Mesh();

            this._vertices  = new Vector3[_CIRCLE_SEGMENTS + 2];
            int[] triangles = new int[3 * _CIRCLE_SEGMENTS];
            for (int i = 0; i < _CIRCLE_SEGMENTS; i++) //NOTE: triangle fan
            {
                triangles[i * 3]     = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }
            this._mesh.vertices  = this._vertices;
            this._mesh.triangles = triangles;
            this._mesh.uv        = new Vector2[_CIRCLE_SEGMENTS + 2];

            this._meshObject.AddComponent<MeshFilter>().mesh = this._mesh;

            this._meshRenderer = this._meshObject.AddComponent<MeshRenderer>();
            Material mat = this._meshRenderer.material = BraveResources.Load("Global VFX/WhiteMaterial", ".mat") as Material;
            mat.shader = ShaderCache.Acquire("tk2d/BlendVertexColorAlphaTintableTilted");
            mat.SetColor(CwaffVFX._OverrideColorId, this.color);
        }

        private void RebuildMeshes()
        {
            Vector3 basePos = this._vertices[0] = this.pos;
            for (int i = 0; i <= _CIRCLE_SEGMENTS; ++i)
                this._vertices[i + 1] = basePos + (i * (360f / _CIRCLE_SEGMENTS)).ToVector3(this.radius);
            this._mesh.vertices = this._vertices; // necessary to actually trigger an update for some reason
            this._mesh.RecalculateBounds();
            this._mesh.RecalculateNormals();
        }

        public void Setup(Vector2? pos = null, float? radius = null, Color? color = null)
        {
            if (!this._didSetup)
                CreateMesh();
            this.color  = color ?? this.color;
            this.pos    = pos ?? this.pos;
            this.radius = radius ?? this.radius;
            if (color.HasValue)
                this._meshRenderer.material.SetColor(CwaffVFX._OverrideColorId, this.color);
            if (!this._didSetup || pos.HasValue || radius.HasValue)
                RebuildMeshes();
            this._didSetup = true;
        }

        private void Update()
        {
          // enter update code here
        }

        private void OnDestroy()
        {
            if (this._meshObject)
                UnityEngine.Object.Destroy(this._meshObject);
        }
    }
}

/// <summary>Class for testing tk2dMeshSprite shaders</summary>
public class Distortyboi : MonoBehaviour
{
    private Material _mat = null;

    public void Setup(Material mat)
    {
        this._mat = mat;
    }

    private void Update()
    {
      if (this._mat)
      {
        // this._mat.SetFloat("_Amplitude", Mathf.Sin(3f * BraveTime.ScaledTimeSinceStartup));
        this._mat.SetFloat(CwaffVFX._FadeId, Mathf.Max(0f, Mathf.Sin(0.9f * BraveTime.ScaledTimeSinceStartup)));
      }
    }
}
