namespace CwaffingTheGungy;

public class Commands
{
    internal static bool _DebugStealth    = false;
    internal static bool _DebugCameraLock = false;

    internal static Action _OnDebugKeyPressed = null;

    internal static Material _WiggleMat = null;
    private static bool _Wiggling = false;
    private static int _WaterLayer = -1;

    public static void Init()
    {
        if (!C.DEBUG_BUILD)
            return; // do nothing in non-debug builds
        //
        // Base command for doing whatever I'm testing at the moment
        ETGModConsole.Commands.AddGroup("hh", delegate (string[] args)
        {
            GameManager.Instance.PrimaryPlayer.HealthAndArmorSwapped ^= true;
        });
        ETGModConsole.Commands.AddGroup("gg", delegate (string[] args)
        {
            DebrisObject debris = LootEngine.SpawnItem(
                PickupObjectDatabase.GetById(Lazy.PickupId<Domino>()).gameObject,
                // PickupObjectDatabase.GetById(IDs.Pickups["bubblebeam"]).gameObject,
                GameManager.Instance.PrimaryPlayer.CenterPosition,
                Vector2.zero,
                0);
            MasteryRitualComponent.PrepareDroppedItemForMasteryRitual(debris.GetComponent<DebrisObject>());
            // ETGModConsole.Log("<size=100><color=#ff0000ff>Please specify a command. Type 'nn help' for a list of commands.</color></size>", false);
        });
        ETGModConsole.Commands.AddGroup("oo", delegate (string[] args)
        {//
            Lazy.CreateHoveringGun(GameManager.Instance.PrimaryPlayer);
        });
        ETGModConsole.Commands.AddGroup("zz", delegate (string[] args)
        {
            PlayerController pc = GameManager.Instance.PrimaryPlayer;
            tk2dMeshSprite ms = Lazy.CreateMeshSpriteObject(pc.sprite, pc.sprite.WorldTopCenter, pointMesh: true);
            ms.renderer.material.shader = CwaffShaders.ShatterShader;  // CwaffShaders.WiggleShader;
            ms.renderer.material.SetFloat("_Progressive", 1f);
            ms.renderer.material.SetFloat("_Amplitude", 10f);
            ms.gameObject.AddComponent<Distortyboi>().Setup(ms.renderer.material);
        });
        ETGModConsole.Commands.AddGroup("ww", delegate (string[] args)
        {//
            _WiggleMat ??= new Material(CwaffShaders.ScreenWiggleShader);
            if (_WaterLayer < 0)
                _WaterLayer = 1 << LayerMask.NameToLayer("Water");
            _Wiggling = !_Wiggling;
            if (_Wiggling)
            {
                // ShadowSystem.AllLights.Clear();
                // Pixelator.Instance.cm_core1 = 0;  // disable background floor / tiles
                // Pixelator.Instance.cm_core2 = 0;  // disable background reflections / dynamic lighting?
                // Pixelator.Instance.cm_core3 = 0;  // disable walls, sprite shadows, and basic enemy health bars
                // Pixelator.Instance.cm_core3 &= ~_WaterLayer;
                Pixelator.Instance.RegisterAdditionalRenderPass(_WiggleMat);
                // Pixelator.Instance.AdditionalCoreStackRenderPass = _WiggleMat;
                GameManager.Instance.PrimaryPlayer.ToggleShadowVisiblity(false);
                SpriteOutlineManager.ToggleOutlineRenderers(GameManager.Instance.PrimaryPlayer.sprite, false);
                PlayerController pc = GameManager.Instance.PrimaryPlayer;
                MeshRenderer mr = pc.sprite.gameObject.GetComponent<MeshRenderer>();
                mr.additionalVertexStreams = null;
                foreach (var mat in mr.materials)
                    System.Console.WriteLine($"  have material {mat?.name ?? ("nothin")}");
                foreach (var mat in mr.sharedMaterials)
                    System.Console.WriteLine($"  have shared material {mat?.name ?? ("nothin")}");


                // pc.TogglePitClipping(false);
                // pc.ToggleShadowVisiblity(false);
                // pc.SetStencilVal(146);

                // mr.useLightProbes = false;
                // mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;
                // mr.allowOcclusionWhenDynamic = false;
                // UnityEngine.Object.Destroy(pc.sprite.gameObject.GetComponent<MeshFilter>());
                // UnityEngine.Object.Destroy(pc.sprite.gameObject.GetComponent<MeshRenderer>());
                // UnityEngine.Object.Destroy(pc.sprite.gameObject.GetComponentInChildren<PlayerLightController>());
                // pc.sprite.gameObject.GetComponent<MeshFilter>().sharedMesh = null;
                // GameManager.Instance.PrimaryPlayer.actorReflectionAdditionalOffset = 1000f;
                // GameManager.Instance.PrimaryPlayer.sprite.GetComponent<MeshRenderer>().
                // Transform t = pc.gameObject.transform;
                // int nc = t.childCount;
                // for (int i = 0; i < nc; ++i)
                // {
                //     Transform tc = t.GetChild(i);
                //     System.Console.WriteLine($"child {i} is {tc.gameObject.name}");
                //     foreach (Component c in tc.GetComponents(typeof(Component)))
                //     {
                //         System.Console.WriteLine($"  has component {c.GetType().Name}");
                //     }
                // }
                // UnityEngine.Object.Destroy(t.GetChild(3).gameObject);
                // UnityEngine.Object.Destroy(t.GetChild(2).gameObject.GetComponent<PlayerLightController>());
                // Shader.SetGlobalFloat("_GlobalReflectionsEnabled", 0f);
                // Shader.SetGlobalFloat("_MeduziReflectionsEnabled", 0f);
                // pc.sprite.enabled = false;
                // int i = 0;
                foreach (MeshRenderer mr2 in pc.gameObject.GetComponentsInChildren<MeshRenderer>())
                {
                    Lazy.DebugLog($"  disabling MR for {mr2.gameObject.name}");
                    mr2.enabled = false;
                }
                pc.sprite.m_renderer.enabled = true;
            }
            else
            {
                Pixelator.Instance.DeregisterAdditionalRenderPass(_WiggleMat);
                // Pixelator.Instance.cm_core3 |= _WaterLayer;
                // Pixelator.Instance.AdditionalCoreStackRenderPass = null;
            }
            // Pixelator.Instance.AdditionalCoreStackRenderPass = new Material(CwaffShaders.ScreenWiggleShader);
            // PixelatorRenderGame_PrettyPatch._SuperFancyShaderMaterial = new Material(CwaffShaders.ScreenBasicShader);
            // PixelatorRenderGame_PrettyPatch._SuperFancyShaderMaterial = new Material(CwaffShaders.ScreenWiggleShader);
            // PixelatorRenderGame_PrettyPatch._SuperFancyShadersEnabled ^= true;
            // GameManager.Instance.MainCameraController.SetManualControl(true, true);
            // GameManager.Instance.MainCameraController.OverridePosition = Vector3.zero;
        });
        // Shader tests
        ETGModConsole.Commands.AddGroup("shiny", delegate (string[] args)
        {
            tk2dBaseSprite s = GameManager.Instance.PrimaryPlayer.sprite;
            SpriteOutlineManager.RemoveOutlineFromSprite(s);
            s.usesOverrideMaterial = true;
            Material m = s.renderer.material;
            m.shader = CwaffShaders.EmissiveAlphaShader;
            // m.SetFloat("_EmissivePower", 50f);
            // m.SetFloat("_Fade", 0.01f);
            float power = 100f;
            if (args.Length >= 1)
                power = float.Parse(args[0]);
            float alpha = 0.5f;
            if (args.Length >= 2)
                alpha = float.Parse(args[1]);

            ETGModConsole.Log($"testing custom shader power {power} alpha {alpha}");
            m.SetFloat("_EmissivePower", power);
            m.SetFloat("_Fade", alpha);
        });
        ETGModConsole.Commands.AddGroup("shader", delegate (string[] args)
        {
            if (args == null || args.Length < 1)
            {
                ETGModConsole.Log($"need a shader name and property and value");
                return;
            }
            try
            {
                tk2dBaseSprite s = GameManager.Instance.PrimaryPlayer.sprite;
                s.usesOverrideMaterial = true;
                Material m = s.renderer.material;
                if (args.Length == 2 && args[0].StartsWithInvariant("_"))
                {
                    if (args[0] == "_OverrideColor")
                    {
                        m.SetVector(args[0], new Vector4(1.0f,1.0f,0.0f,1.0f));
                    }
                    else
                    {
                        ETGModConsole.Log($"Setting property {args[0]} of current shader to {args[1]}");
                        m.SetFloat(args[0], float.Parse(args[1]));
                    }
                }
                else
                {
                    ETGModConsole.Log($"Setting shader to {args[0]}");
                    m.shader = ShaderCache.Acquire(args[0]);
                }
            }
            catch(Exception ex) {
                ETGModConsole.Log($"something went wrong D: {ex}");
            }
        });
        ETGModConsole.Commands.AddGroup("ss", delegate (string[] args)
        {
            GameManager.Instance.LoadCustomLevel(SansDungeon.INTERNAL_NAME);
        });
        ETGModConsole.Commands.AddGroup("nukefloor", delegate (string[] args)
        {
            NukeFloor();
        });
    }

    internal static void NukeFloor()
    {
        GameManager.Instance.StartCoroutine(NukeFloor_CR());
    }

    internal static IEnumerator NukeFloor_CR()
    {
        // nuke all rooms
        List<RoomHandler> rooms = GameManager.Instance.Dungeon.data.rooms;
        List<AIActor> enemiesToKill = new();
        for (int i = 0; i < rooms.Count; i++)
        {
            RoomHandler room = rooms[i];
            if (room == null)
                continue;
            room.ClearReinforcementLayers();
            room.GetActiveEnemies(RoomHandler.ActiveEnemyType.All, ref enemiesToKill);
            for (int k = 0; k < enemiesToKill.Count; k++)
                if (enemiesToKill[k])
                    enemiesToKill[k].enabled = true;
            yield return null;
            for (int j = 0; j < enemiesToKill.Count; j++)
                if (enemiesToKill[j])
                    UnityEngine.Object.Destroy(enemiesToKill[j].gameObject);
        }
        Lazy.DebugLog($"floor nuked");
    }

    [HarmonyPatch]
    private class DebugInputPatch // handle debug input
    {
        [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.HandlePlayerInput))]
        static bool Prefix(PlayerController __instance)
        {
            if (!C.DEBUG_BUILD)
                return true; // disable debug keys in non-debug builds
            if (!Input.GetKey(KeyCode.RightControl))
                return true; // all debug keys require left control to be held

            if (Input.GetKeyDown(KeyCode.S)) // debug stealth
            {
                _DebugStealth = !_DebugStealth;
                __instance.SetIsStealthed(_DebugStealth, "Debug stealth");
            }

            if (Input.GetKeyDown(KeyCode.C)) // debug camera lock
            {
                _DebugCameraLock = !_DebugCameraLock;
                GameManager.Instance.MainCameraController.OverridePosition = GameManager.Instance.MainCameraController.previousBasePosition;
                GameManager.Instance.MainCameraController.SetManualControl(_DebugCameraLock, true);
            }

            if (Input.GetKeyDown(KeyCode.M)) // acquire mastery token for current gun
            {
                __instance.AcquireMastery(__instance.CurrentGun);
                if (__instance.CurrentGun.gameObject.GetComponent<CwaffGun>() is CwaffGun cg)
                    cg.OnSwitchedToThisGun(); //NOTE: remove this if this causes problems
            }

            if (Input.GetKeyDown(KeyCode.B)) // toggle constructor profiler
            {
                ConstructorProfiler.Toggle();
            }

            if (Input.GetKeyDown(KeyCode.E)) // throw an error immediately
            {
                __instance.GetComponent<Projectile>().DieInAir();
            }

            if (Input.GetKeyDown(KeyCode.Z)) // set ammo to 1 / max
            {
                Gun gun = GameManager.Instance.PrimaryPlayer.CurrentGun;
                if (gun.CurrentAmmo > 1)
                    gun.CurrentAmmo = 1;
                else
                    gun.CurrentAmmo = gun.AdjustedMaxAmmo;
            }

            if (Input.GetKeyDown(KeyCode.Alpha0)) // call debug events
            {
                if (_OnDebugKeyPressed != null)
                    _OnDebugKeyPressed();
            }

            return true;
        }
    }
}

// A slightly rewritten version of old Anywhere Mod by stellatedHexahedron
//   blatantly stolen from Apache by me o:
public class FlowCommands {
    private static List<string> knownFlows = new List<string>();
    private static List<string> knownTilesets = new List<string>();
    private static List<string> knownScenes = new List<string>();

    private static string[] ReturnMatchesFromList(string matchThis, List<string> inThis) {
        List<string> result = new List<string>();
        string matchString = matchThis.ToLower();
        foreach (string text in inThis) {
            string textString = text.ToLower();
            bool flag = StringAutocompletionExtensions.AutocompletionMatch(textString, matchString);
            if (flag) { result.Add(textString); }
        }
        return result.ToArray();
    }

    public static void Install() {
        if (CwaffDungeonFlow._KnownFlows != null && CwaffDungeonFlow._KnownFlows.Count > 0) {
            foreach (DungeonFlow flow in CwaffDungeonFlow._KnownFlows) {
                if (flow.name != null && flow.name != string.Empty) { knownFlows.Add(flow.name.ToLower()); }
            }
        }

        foreach (GameLevelDefinition dungeonFloors in GameManager.Instance.dungeonFloors) {
            if (dungeonFloors.dungeonPrefabPath != null && dungeonFloors.dungeonPrefabPath != string.Empty) {
                knownTilesets.Add(dungeonFloors.dungeonPrefabPath.ToLower());
            }
            if (dungeonFloors.dungeonSceneName != null && dungeonFloors.dungeonSceneName != string.Empty) {
                knownScenes.Add(dungeonFloors.dungeonSceneName.ToLower());
            }
        }

        foreach (GameLevelDefinition customFloors in GameManager.Instance.customFloors) {
            if (customFloors.dungeonPrefabPath != null && customFloors.dungeonPrefabPath != string.Empty) {
                knownTilesets.Add(customFloors.dungeonPrefabPath.ToLower());
            }
            if (customFloors.dungeonSceneName != null && customFloors.dungeonSceneName != string.Empty) {
                knownScenes.Add(customFloors.dungeonSceneName.ToLower());
            }
        }

        ETGModConsole.Commands.AddUnit("load_flow", new Action<string[]>(LoadFlowFunction), new AutocompletionSettings(delegate(int index, string input) {
            if (index == 0) {
                return ReturnMatchesFromList(input.ToLower(), knownFlows);
            } else if (index == 1) {
                return ReturnMatchesFromList(input.ToLower(), knownTilesets);
            } else if (index == 2) {
                return ReturnMatchesFromList(input.ToLower(), knownScenes);
            } else {
                return new string[0];
            }
        }));
    }

    public static void LoadFlowFunction(string[] args) {
        if (args == null | args.Length == 0 | args[0].ToLower() == "help") {
        ETGModConsole.Log("WARNING: this command can crash gungeon! \nIf the game hangs on loading screen, use console to load a different level!\nUsage: load_flow [FLOW NAME] [TILESET NAME]. [TILESET NAME] is optional. Press tab for a list of each.\nOnce you run the command and you press escape, you should see the loading screen. If nothing happens when you use the command, the flow you tried to load doesn't exist or the path to it needs to be manually specified. Example: \"load_flow NPCParadise\".\nIf it hangs on loading screen then the tileset you tried to use doesn't exist, is no longer functional, or the flow uses rooms that are not compatible with the chosen tileset.\nAlso, you should probably know that if you run this command from the breach, the game never gives you the ability to shoot or use active items, so you should probably start a run first.");
        } else if (args != null && args.Length > 3) {
            ETGModConsole.Log("ERROR: Too many arguments specified! DungoenFlow name, dungoen prefab name, and dungoen scene name are the expected arguments!");
        } else {
            bool tilesetSpecified = args.Length > 1;
            bool sceneSpecified = args.Length > 2;

            if (tilesetSpecified && !knownTilesets.Contains(args[1]) && DungeonDatabase.GetOrLoadByName(args[1])) {
                knownTilesets.Add(args[1]);
            }

            bool invalidTileset = tilesetSpecified && !knownTilesets.Contains(args[1]);
            string flowName = args[0].Replace('-', ' ');

            if (invalidTileset) {
                ETGModConsole.Log("Not a valid tileset!");
            } else {
                try {
                    string LogMessage = ("Attempting to load Dungeon Flow \"" + args[0] + "\"");

                    if (tilesetSpecified) { LogMessage += (" with tileset \"" + args[1] + "\""); }
                    if (sceneSpecified) { LogMessage += (" and scene \"" + args[2] + "\""); }
                    LogMessage += ".";
                    // ETGModConsole.Log("Attempting to load Dungeon Flow \"" + args[0] + (tilesetSpecified ? ("\" with tileset \"" + args[1]) : string.Empty) + "\"" + (sceneSpecified ? ("\" and scene " + args[2]) : string.Empty) + ".");
                    ETGModConsole.Log(LogMessage);
                    if (args.Length == 1) {
                        GameManager.Instance.LoadCustomFlowForDebug(flowName);
                    } else if (args.Length == 2) {
                        string tilesetName = args[1];
                        GameManager.Instance.LoadCustomFlowForDebug(flowName, tilesetName);
                    } else if (args.Length == 3) {
                        string tilesetName = args[1];
                        string sceneName = args[2];
                        GameManager.Instance.LoadCustomFlowForDebug(flowName, tilesetName, sceneName);
                    } else {
                        ETGModConsole.Log("If you're trying to go nowhere, you're succeeding.");
                    }
                } catch (Exception ex) {
                    ETGModConsole.Log("WHOOPS! Something went wrong! Most likely you tried to load a broken flow, or the tileset is incomplete and doesn't have the right tiles for the flow.");
                    ETGModConsole.Log("In order to get the game back into working order, the mod is now loading NPCParadise, with the castle tileset.");
                    Debug.Log("WHOOPS! Something went wrong! Most likely you tried to load a broken flow, or the tileset is incomplete and doesn't have the right tiles for the flow.");
                    Debug.Log("In order to get the game back into working order, the mod is now loading NPCParadise, with the castle tileset.");
                    Debug.LogException(ex);
                    GameManager.Instance.LoadCustomFlowForDebug("npcparadise", "Base_Castle", "tt_castle");
                }
            }
        }
    }
}
