#region Global Usings
    global using System;
    global using System.Collections;
    global using System.Collections.Generic;
    global using System.Linq;
    // global using System.Text;
    global using System.Text.RegularExpressions;
    global using System.Reflection;
    // global using System.Runtime;
    global using System.Collections.ObjectModel;
    global using System.IO;
    // global using System.Globalization; // CultureInfo
    global using System.ComponentModel;  // Debug stuff
    // global using System.Runtime.InteropServices; // Audio loading
    global using System.Threading;

    global using BepInEx;
    global using UnityEngine;
    global using UnityEngine.UI;
    // global using UnityEngine.Events; // UnityEventBase
    global using MonoMod.RuntimeDetour;
    // global using MonoMod.Utils;
    global using MonoMod.Cil;
    global using Mono.Cecil.Cil; //Instruction
    global using SGUI;
    global using FullSerializer;
    global using HarmonyLib;
    // global using ETGGUI; // unneeded???

    global using Gungeon;
    global using Dungeonator;
    global using HutongGames.PlayMaker; //FSM___ stuff
    global using HutongGames.PlayMaker.Actions; //FSM___ stuff
    global using Alexandria.ItemAPI;
    global using Alexandria.EnemyAPI;
    // global using Alexandria.DungeonAPI;
    global using Alexandria.Misc;
    global using Alexandria.NPCAPI;
    global using Brave.BulletScript;

    global using SaveAPI; // only nonstandard api copied in from elsewhere, hopefully Alexandria standardizes this eventually
#endregion

global using ResourceExtractor = Alexandria.ItemAPI.ResourceExtractor;
global using Component         = UnityEngine.Component;
global using ShopAPI           = Alexandria.NPCAPI.ShopAPI;
global using RoomFactory       = Alexandria.DungeonAPI.RoomFactory;

global using Gunfiguration;

global using static ProjectileModule;      //ShootStyle, ProjectileSequenceStyle
global using static tk2dBaseSprite;        //Anchor
global using static PickupObject;          //ItemQuality

namespace CwaffingTheGungy;

[BepInPlugin(C.MOD_GUID, C.MOD_NAME, C.MOD_VERSION)]
[BepInDependency(ETGModMainBehaviour.GUID)]
[BepInDependency(Alexandria.Alexandria.GUID)]
[BepInDependency(Gunfiguration.C.MOD_GUID)]
public class Initialisation : BaseUnityPlugin
{
    public static Initialisation Instance;

    public unsafe void Test()
    {
        var nums = stackalloc int[3];
    }

    public void Awake()
    {
    }
    public void Start()
    {
        ETGModMainBehaviour.WaitForGameManagerStart(GMStart);
    }

    public void GMStart(GameManager manager)
    {
        try
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            System.Diagnostics.Process currentProcess = System.Diagnostics.Process.GetCurrentProcess();
            long oldMemory = currentProcess.WorkingSet64;
            if (C.DEBUG_BUILD)
                ETGModConsole.Log("Cwaffing the Gungy initializing...");

            Instance = this;
            Harmony harmony = new Harmony(C.MOD_GUID);

            #region Set up Early Harmony Patches (async, but needed as soon as atlases and shaders are loaded)
                System.Diagnostics.Stopwatch setupEarlyHarmonyWatch = null;
                Thread setupEarlyHarmonyThread = new Thread(() => {
                    setupEarlyHarmonyWatch = System.Diagnostics.Stopwatch.StartNew();
                    AtlasHelper.InitSetupPatches(harmony);
                    setupEarlyHarmonyWatch.Stop();
                });
                setupEarlyHarmonyThread.Start();
            #endregion

            #region Set up Late Harmony Patches (async, nothing else is needed until floor loading patches)
                System.Diagnostics.Stopwatch setupLateHarmonyWatch = null;
                Thread setupLateHarmonyThread = new Thread(() => {
                    setupLateHarmonyWatch = System.Diagnostics.Stopwatch.StartNew();
                    harmony.PatchAll();
                    setupLateHarmonyWatch.Stop();
                });
                setupLateHarmonyThread.Start();
            #endregion

            #region Set up Packed Texture Atlases (absolutely cannot be async, handles the meat of texture loading)
                System.Diagnostics.Stopwatch setupAtlasesWatch = System.Diagnostics.Stopwatch.StartNew();

                Assembly asmb = Assembly.GetExecutingAssembly();

                Dictionary<string, tk2dSpriteDefinition.AttachPoint[]> attachPoints =
                    AtlasHelper.ReadAttachPointsFromTSV(asmb, $"{C.MOD_INT_NAME}.Resources.Atlases.attach_points.tsv");

                //WARNING: I know this looks like it can be threaded, but it can't...I've tried three times now, so much can go wrong...don't do it pretzel D:
                for (int i = 1; ; ++i)
                {
                    string atlasPath = $"{C.MOD_INT_NAME}.Resources.Atlases.atlas_{i}.png";
                    using (Stream s = asmb.GetManifestResourceStream(atlasPath))
                    {
                      if (s == null)
                          break;
                    }
                    Texture2D atlas = ResourceExtractor.GetTextureFromResource(atlasPath, asmb);
                    AtlasHelper.LoadPackedTextureResource(
                      atlas: atlas, attachPoints: attachPoints, metaDataResourcePath: $"{C.MOD_INT_NAME}.Resources.Atlases.atlas_{i}.atlas");
                }
                // Build resource map for ease of access
                ResMap.Build();

                setupAtlasesWatch.Stop();
            #endregion

            //NOTE: add any new shaders we ever need to use here
            #region Acquire Shaders (absolutely cannot be async when calling Shader.Find(), but once they're cached we're fine)
                System.Diagnostics.Stopwatch setupShadersWatch = System.Diagnostics.Stopwatch.StartNew();
                ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTintableTiltedCutoutEmissive");
                ShaderCache.Acquire("Brave/Internal/SinglePassOutline");
                ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTiltedCutoutEmissive");
                ShaderCache.Acquire("Brave/Internal/SimpleAlphaFadeUnlit");
                ShaderCache.Acquire("Daikon Forge/Default UI Shader");
                setupShadersWatch.Stop();
            #endregion

            System.Diagnostics.Stopwatch awaitEarlyHarmonyWatch = System.Diagnostics.Stopwatch.StartNew();
            // We have to wait for Harmony to finish early patches before we can do anything else
            setupEarlyHarmonyThread.Join();
            awaitEarlyHarmonyWatch.Stop();

            #region Round 1 Config (could be async since it's mostly hooks and database stuff where no sprites are needed, but it's fast enough that we just leave it sync)
                System.Diagnostics.Stopwatch setupConfig1Watch = System.Diagnostics.Stopwatch.StartNew();

                // Load our configuration files
                CwaffConfig.Init();

                //Tools and Toolboxes
                CwaffPrerequisite.Init();  // must be set up after CwaffEvents
                // HUDController.Init(); // Need to load early (unused for now)
                ModdedShopItemAdder.Init(); // must be set up after CwaffEvents

                //Commands and Other Console Utilities
                Commands.Init();

                // Game tweaks
                HeckedMode.Init();

                setupConfig1Watch.Stop();
            #endregion

            #region Round 2 Config (Requires sprites and bundled asset loading, cannot be async)
                System.Diagnostics.Stopwatch setupConfig2Watch = System.Diagnostics.Stopwatch.StartNew();
                // Basic VFX Setup
                VFX.Init(); //NOTE: accesses shared resource databases, so must be synchronous
                //Status Effect Setup
                SoulLinkStatus.Init();
                //Goop Setup
                EasyGoopDefinitions.DefineDefaultGoops();
                // Boss Builder API //WARNING: moved from boss setup due to threading error in PathologicalGames.PoolManagerUtils.SetActive()
                BossBuilder.Init();
                // Note Does Setup
                CustomNoteDoer.Init();
                // Miscellaneous tweaks
                CwaffTweaks.Init();
                // Hecked Mode Tribute Statues
                HeckedShrine.Init();
                setupConfig2Watch.Stop();
            #endregion

            #region Audio (Async)
                System.Diagnostics.Stopwatch setupAudioWatch = null;
                Thread setupAudioThread = new Thread(() => {
                    setupAudioWatch = System.Diagnostics.Stopwatch.StartNew();
                    ETGModMainBehaviour.Instance.gameObject.AddComponent<AudioSource>(); // is this necessary?
                    AudioResourceLoader.AutoloadFromAssembly(C.MOD_INT_NAME);  // Load Audio Banks
                    setupAudioWatch.Stop();
                });
                setupAudioThread.Start();
            #endregion

            #region Save API Setup (Async)
                System.Diagnostics.Stopwatch setupSaveWatch = null;
                Thread setupSaveThread = new Thread(() => {
                    setupSaveWatch = System.Diagnostics.Stopwatch.StartNew();
                    SaveAPI.SaveAPIManager.Setup(C.MOD_PREFIX);  // Needed for prerequisite checking and save serialization
                    setupSaveWatch.Stop();
                });
                setupSaveThread.Start();
            #endregion

            #region Guns
                System.Diagnostics.Stopwatch setupGunsWatch = null;
                // Thread setupGunsThread = new Thread(() => {
                    setupGunsWatch = System.Diagnostics.Stopwatch.StartNew();

                    IronMaid.Add();
                    Natascha.Add();
                    PaintballCannon.Add();
                    // DerailGun.Add(); unfinished
                    // PopcornGun.Add(); no sprite
                    Tranquilizer.Add();
                    // LastResort.Add(); no sprite
                    // MasterSword.Add(); unfinished
                    // Encircler.Add(); no sprite
                    // Nug.Add(); no sprite
                    SoulKaliber.Add();
                    // GamblersFallacy.Add(); no sprite
                    // GasterBlaster.Add(); no sprite
                    // Commitment.Add(); no sprite
                    // HeadCannon.Add(); unfinished
                    // Telefragger.Add(); no sprite
                    // Kinsurrection.Add(); unfinished
                    // SpinCycle.Add(); no sprite
                    // TimingGun.Add(); unfinished
                    KiBlast.Add();
                    Deadline.Add();
                    BBGun.Add();
                    Bouncer.Add();
                    Grandmaster.Add();
                    QuarterPounder.Add();
                    // DeathNote.Add(); unfinished
                    HolyWaterGun.Add();
                    Alyx.Add();
                    VacuumCleaner.Add();
                    Gunbrella.Add();
                    Blackjack.Add();
                    SchrodingersGat.Add();
                    // Taomislav.Add(); unfinished
                    RacketLauncher.Add();
                    Outbreak.Add();
                    HandCannon.Add();
                    HatchlingGun.Add();
                    Ticonderogun.Add();
                    IceCreamGun.Add(); //NOTE: adding this here because it's a pseudo-gun and it ruins threading if initialized with other items
                    AimuHakurei.Add();
                    SeltzerPelter.Add();
                    Missiletoe.Add();
                    PlatinumStar.Add();
                    PistolWhip.Add();
                    Jugglernaut.Add();
                    SubtractorBeam.Add();
                    Alligator.Add();
                    Lightwing.Add();
                    KingsLaw.Add();
                    Pincushion.Add();
                    Crapshooter.Add();
                    CarpetBomber.Add();
                    Uppskeruvel.Add();
                    Glockarina.Add();
                    Magunet.Add();
                    Wavefront.Add();
                    Scotsman.Add();
                    ChekhovsGun.Add();
                    Vladimir.Add();
                    Blamethrower.Add();
                    Suncaster.Add();
                    KALI.Add();
                    AlienNailgun.Add();
                    OmnidirectionalLaser.Add();
                    RCLauncher.Add();
                    Breegull.Add();
                    SubMachineGun.Add();
                    setupGunsWatch.Stop();
                // });
                // setupGunsThread.Start();
            #endregion

            #region Actives
                System.Diagnostics.Stopwatch setupActivesWatch = null;
                // Thread setupActivesThread = new Thread(() => {
                    setupActivesWatch = System.Diagnostics.Stopwatch.StartNew();

                    BorrowedTime.Init();
                    BulletThatCanKillTheFuture.Init();
                    GunPowderer.Init();
                    AmazonPrimer.Init();
                    EmergencySiren.Init();
                    Itemfinder.Init();
                    KalibersJustice.Init();
                    GasterBlaster.Init();
                    StackOfTorches.Init();
                    InsurancePolicy.Init();
                    IceCream.Init();
                    // GungeonitePickaxe.Init();
                    ChamberJammer.Init();
                    Cuppajoe.Init();
                    StopSign.Init();
                    GunSynthesizer.Init();
                    ChestScanner.Init();
                    BulletbotImplant.Init();

                    setupActivesWatch.Stop();
                // });
                // setupActivesThread.Start();
            #endregion

            #region Passives
                System.Diagnostics.Stopwatch setupPassivesWatch = null;
                // Thread setupPassivesThread = new Thread(() => {
                    setupPassivesWatch = System.Diagnostics.Stopwatch.StartNew();
                    // Shine.Init(); unfinished
                    // Superstitious.Init(); unfinished
                    DriftersHeadgear.Init();
                    // Siphon.Init(); unfinished
                    // ZoolandersDiary.Init(); unfinished / mediocre
                    RatPoison.Init();
                    JohnsWick.Init();
                    Gyroscope.Init();
                    CuratorsBadge.Init();
                    CreditCard.Init();
                    LibraryCardtridge.Init();
                    BlankChecks.Init();
                    DeadRinger.Init();
                    VoodooDoll.Init();
                    CampingSupplies.Init();
                    WeddingRing.Init();
                    GorgunEye.Init();
                    UtilityVest.Init();
                    WarriorsGi.Init();
                    CatEarHeadband.Init();
                    Blazer.Init();
                    PlotArmor.Init();
                    FourDBullets.Init();
                    AstralProjector.Init();
                    EchoChamber.Init();
                    BionicFinger.Init();
                    BubbleWand.Init();
                    AdrenalineShot.Init();
                    StuntHelmet.Init();
                    ComfySlippers.Init();
                    // DisplayStand.Init();  // scrapped since discouraging item use is not very fun
                    SafetyGloves.Init();
                    DrabOutfit.Init();
                    RingOfDefenestration.Init();
                    AmmoConservationManual.Init();
                    ReserveAmmolet.Init();
                    ReflexAmmolet.Init();
                    // Protractor.Init();  // unfinished
                    // PlaybulletMagazine.Init();
                    setupPassivesWatch.Stop();
                // });
                // setupPassivesThread.Start();
            #endregion

            #region UI Sprites (cannot be async, must set up textures on main thread)
                System.Diagnostics.Stopwatch setupUIWatch = System.Diagnostics.Stopwatch.StartNew();
                AtlasHelper.AddUISpriteBatch(new(){
                    "barter_s_icon",            Bart._BarterSpriteS,
                    "barter_a_icon",            Bart._BarterSpriteA,
                    "barter_b_icon",            Bart._BarterSpriteB,
                    "barter_c_icon",            Bart._BarterSpriteC,
                    "soul_sprite_ui_icon",      Uppskeruvel._SoulSpriteUI,
                    "prism_ui_icon",            Suncaster._PrismUI,
                    "glockarina_storm_ui_icon", Glockarina._StormSpriteUI,
                    "glockarina_time_ui_icon",  Glockarina._TimeSpriteUI,
                    "glockarina_saria_ui_icon", Glockarina._SariaSpriteUI,
                    "glockarina_empty_ui_icon", Glockarina._EmptySpriteUI,
                    // needs to be three separate sprites or the UI breaks
                    "adrenaline_heart",         AdrenalineShot._FullHeartSpriteUI,
                    "adrenaline_heart",         AdrenalineShot._HalfHeartSpriteUI,
                    "adrenaline_heart",         AdrenalineShot._EmptyHeartSpriteUI,

                    "breegull_clockwork_ui",    Breegull._ClockworkUI,
                    "breegull_fire_ui",         Breegull._FireUI,
                    "breegull_grenade_ui",      Breegull._GrenadeUI,
                    "breegull_ice_ui",          Breegull._IceUI,
                    "breegull_normal_ui",       Breegull._NormalUI,
                });
                setupUIWatch.Stop();
            #endregion

            // we have to wait for the rest of the harmony patches to finish before loading in floors
            System.Diagnostics.Stopwatch awaitLateHarmonyWatch = System.Diagnostics.Stopwatch.StartNew();
            setupLateHarmonyThread.Join();
            awaitLateHarmonyWatch.Stop();

            #region Floor and Flow Initialization (sync for now, might not need to be)
                System.Diagnostics.Stopwatch setupFloorsWatch = System.Diagnostics.Stopwatch.StartNew();

                // Flow stuff stolen from Apache
                AssetBundle sharedAssets;
                AssetBundle sharedAssets2;
                AssetBundle sharedBase;
                AssetBundle braveResources;
                AssetBundle enemiesBase;
                AssetBundle encounterAssets;
                try {
                    // Init some asset bundles
                    sharedAssets    = ResourceManager.LoadAssetBundle("shared_auto_001");
                    sharedAssets2   = ResourceManager.LoadAssetBundle("shared_auto_002");
                    sharedBase      = ResourceManager.LoadAssetBundle("shared_base_001");
                    braveResources  = ResourceManager.LoadAssetBundle("brave_resources_001");
                    enemiesBase     = ResourceManager.LoadAssetBundle("enemies_base_001");
                    encounterAssets = ResourceManager.LoadAssetBundle("encounters_base_001");

                    CwaffDungeonPrefabs.InitCustomPrefabs(sharedAssets, sharedAssets2, braveResources, enemiesBase);
                    CwaffDungeonFlow.InitDungeonFlowsAndHooks(sharedAssets2);
                } catch (Exception ex) {
                    ETGModConsole.Log("[CtG] ERROR: Exception occured while building prefabs!", true);
                    Debug.LogException(ex);
                } finally {
                    // Null bundles when done with them to avoid game crash issues
                    sharedAssets    = null;
                    sharedAssets2   = null;
                    sharedBase      = null;
                    braveResources  = null;
                    enemiesBase     = null;
                    encounterAssets = null;
                }

                // Actual floor Initialization
                SansDungeon.Init();

                // Modified version of Anywhere mod, further stolen and modified from Apache's version
                FlowCommands.Install();

                setupFloorsWatch.Stop();
            #endregion

            #region Bosses yo (not async for now due to needing to load boss card textures)
                System.Diagnostics.Stopwatch setupBossesWatch = null;
                // Thread setupBossesThread = new Thread(() => {
                    setupBossesWatch = System.Diagnostics.Stopwatch.StartNew();
                    SansBoss.Init();
                    setupBossesWatch.Stop();
                // });
                // setupBossesThread.Start();
            #endregion

            // Need to wait for all items and SaveAPI to be loaded before setting up synergies and shops
            System.Diagnostics.Stopwatch awaitItemsWatch = System.Diagnostics.Stopwatch.StartNew();
            // setupActivesThread.Join();
            // setupPassivesThread.Join();
            // setupGunsThread.Join();
            setupSaveThread.Join();
            awaitItemsWatch.Stop();

            #region Synergies (not async due to safety concerns + it's fast already)
                System.Diagnostics.Stopwatch setupSynergiesWatch = null;
                // Thread setupSynergiesThread = new Thread(() => {
                    setupSynergiesWatch = System.Diagnostics.Stopwatch.StartNew();

                    CwaffSynergies.Init();
                    setupSynergiesWatch.Stop();
                // });
                // setupSynergiesThread.Start();
            #endregion

            #region Shop NPCs (can't be async due to post-load barter table setup issues causing null derefs)
                System.Diagnostics.Stopwatch setupShopsWatch = null;
                // Thread setupShopsThread = new Thread(() => {
                    setupShopsWatch = System.Diagnostics.Stopwatch.StartNew();
                    // InsuranceBoi.Init();
                    Cammy.Init();
                    Bart.Init();
                    Kevlar.Init();
                    setupShopsWatch.Stop();
                // });
                // setupShopsThread.Start();
            #endregion

            #region Wait for remaining async stuff to finish up
                System.Diagnostics.Stopwatch awaitAsyncWatch = System.Diagnostics.Stopwatch.StartNew();
                setupAudioThread.Join();
                // setupSynergiesThread.Join();
                awaitAsyncWatch.Stop();
            #endregion

            C._ModSetupFinished = true;
            watch.Stop();
            ETGModConsole.Log($"Yay! :D Initialized <color=#{ColorUtility.ToHtmlStringRGB(C.MOD_COLOR).ToLower()}>{C.MOD_NAME} v{C.MOD_VERSION}</color> in "+(watch.ElapsedMilliseconds/1000.0f)+" seconds");
            if (C.DEBUG_BUILD)
            {
                ETGModConsole.Log($"  {setupEarlyHarmonyWatch.ElapsedMilliseconds, 5}ms ASYNC setupEarlyHarmony");
                ETGModConsole.Log($"  {setupLateHarmonyWatch.ElapsedMilliseconds,  5}ms ASYNC setupLateHarmony ");
                ETGModConsole.Log($"  {setupAtlasesWatch.ElapsedMilliseconds,      5}ms       setupAtlases     ");
                ETGModConsole.Log($"  {setupShadersWatch.ElapsedMilliseconds,      5}ms       setupShaders     ");
                ETGModConsole.Log($"  {awaitEarlyHarmonyWatch.ElapsedMilliseconds, 5}ms       awaitEarlyHarmony");
                ETGModConsole.Log($"  {setupConfig1Watch.ElapsedMilliseconds,      5}ms       setupConfig1     ");
                ETGModConsole.Log($"  {setupConfig2Watch.ElapsedMilliseconds,      5}ms       setupConfig2     ");
                ETGModConsole.Log($"  {setupAudioWatch.ElapsedMilliseconds,        5}ms ASYNC setupAudio       ");
                ETGModConsole.Log($"  {setupSaveWatch.ElapsedMilliseconds,         5}ms ASYNC setupSave        ");
                ETGModConsole.Log($"  {setupGunsWatch.ElapsedMilliseconds,         5}ms       setupGuns        ");
                ETGModConsole.Log($"  {setupActivesWatch.ElapsedMilliseconds,      5}ms       setupActives     ");
                ETGModConsole.Log($"  {setupPassivesWatch.ElapsedMilliseconds,     5}ms       setupPassives    ");
                ETGModConsole.Log($"  {setupUIWatch.ElapsedMilliseconds,           5}ms       setupUI          ");
                ETGModConsole.Log($"  {awaitLateHarmonyWatch.ElapsedMilliseconds,  5}ms       awaitLateHarmony ");
                ETGModConsole.Log($"  {setupFloorsWatch.ElapsedMilliseconds,       5}ms       setupFloors      ");
                ETGModConsole.Log($"  {setupBossesWatch.ElapsedMilliseconds,       5}ms       setupBosses      ");
                ETGModConsole.Log($"  {awaitItemsWatch.ElapsedMilliseconds,        5}ms       awaitItems       ");
                ETGModConsole.Log($"  {setupSynergiesWatch.ElapsedMilliseconds,    5}ms       setupSynergies   ");
                ETGModConsole.Log($"  {setupShopsWatch.ElapsedMilliseconds,        5}ms       setupShops       ");
                ETGModConsole.Log($"  {awaitAsyncWatch.ElapsedMilliseconds,        5}ms       awaitAsync       ");
                long newMemory = currentProcess.WorkingSet64;
                ETGModConsole.Log($"allocated {(newMemory - oldMemory).ToString("N0")} bytes of memory along the way");
                ETGModMainBehaviour.Instance.gameObject.Play("vc_kirby_appeal01");
            }

            // Debug.LogError("Gungy o.o!");
            // Debug.LogAssertion("Gungy o.o!");
            // Debug.LogWarning("Gungy o.o!");
            // Debug.Log("Gungy o.o!");
            // Debug.LogException("Gungy o.o!");
            // foreach (tk2dSpriteDefinition def in AmmonomiconController.ForceInstance.EncounterIconCollection.spriteDefinitions)
            //     ETGModConsole.Log($"  def: {def.name}");
        }
        catch (Exception e)
        {
            ETGModConsole.Log(e.Message);
            ETGModConsole.Log(e.StackTrace);
        }
    }

    // For Debugging chest stuff
    // [HarmonyPatch(typeof(Chest), nameof(Chest.DetermineContents))]
    // private class DetermineContentsPatch // Fix oversized gun idle animations in vanilla shops and make sure they are aligned properly
    // {
    //     private static void Prefix(Chest __instance, PlayerController player, int tierShift)
    //     {
    //         __instance.forceContentIds = new(){IDs.Pickups["platinum_star"]}; // for debugging
    //     }
    // }
}
