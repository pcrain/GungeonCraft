#region Global Usings
    global using System;
    global using System.Collections;
    global using System.Collections.Generic;
    global using System.Linq;
    global using System.Text;
    global using System.Text.RegularExpressions;
    global using System.Reflection;
    global using System.Runtime;
    global using System.Collections.ObjectModel;
    global using System.IO;
    global using System.Globalization; // CultureInfo
    global using System.ComponentModel;  // Debug stuff
    global using System.Runtime.InteropServices; // Audio loading
    global using System.Threading;

    global using BepInEx;
    global using UnityEngine;
    global using UnityEngine.UI;
    global using UnityEngine.Events; // UnityEventBase
    global using MonoMod.RuntimeDetour;
    global using MonoMod.Utils;
    global using MonoMod.Cil;
    global using Mono.Cecil.Cil; //Instruction
    global using SGUI;
    global using FullSerializer;
    // global using ETGGUI; // unneeded???

    global using Gungeon;
    global using Dungeonator;
    global using Alexandria.ItemAPI;
    global using Alexandria.EnemyAPI;
    global using Alexandria.DungeonAPI;
    global using Alexandria.Misc;
    global using Alexandria.NPCAPI;
    global using Brave.BulletScript;

    global using SaveAPI; // only nonstandard api copied in from elsewhere, hopefully Alexandria standardizes this eventually
#endregion

global using ResourceExtractor = Alexandria.ItemAPI.ResourceExtractor;
global using Component         = UnityEngine.Component;
global using ShopAPI           = Alexandria.NPCAPI.ShopAPI;
global using RoomFactory       = Alexandria.DungeonAPI.RoomFactory;

global using static ProjectileModule; //ShootStyle, ProjectileSequenceStyle
global using static tk2dBaseSprite;   //Anchor
global using static PickupObject;     //ItemQuality

namespace CwaffingTheGungy;

[BepInPlugin(GUID, "Cwaffing the Gungy", C.MOD_VERSION)]
[BepInDependency(ETGModMainBehaviour.GUID)]
[BepInDependency("etgmodding.etg.mtgapi")]
[BepInDependency("alexandria.etgmod.alexandria")]
public class Initialisation : BaseUnityPlugin
{
    public const string GUID = "pretzel.etg.cwaff";
    public static Initialisation Instance;

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
            if (C.DEBUG_BUILD)
                ETGModConsole.Log("Cwaffing the Gungy initialising...");

            // #region Memory Setup
            //     System.Diagnostics.Stopwatch setupMemoryWatch = System.Diagnostics.Stopwatch.StartNew();
            //     BraveMemory.EnsureHeapSize(1024*1024); Lazy.DebugLog("Ensured 1GB heap...");
            //     setupMemoryWatch.Stop();
            // #endregion

            Instance = this;

            #region Round 1 Config (hooks and database stuff where no sprites are needed, so it can be async)
            System.Diagnostics.Stopwatch setupConfig1Watch = null;
            bool asyncConfig1Setup = false;
            ThreadPool.QueueUserWorkItem((object stateInfo) => {
                setupConfig1Watch = System.Diagnostics.Stopwatch.StartNew();

                // Build resource map for ease of access
                ResMap.Build();

                //Tools and Toolboxes
                CwaffEvents.Init();  // Event handlers
                CwaffPrerequisite.Init();  // must be set up after CwaffEvents
                // HUDController.Init(); // Need to load early (unused for now)
                CustomAmmoDisplay.Init(); // Also need to load early
                CustomDodgeRoll.InitCustomDodgeRollHooks();
                ModdedShopItemAdder.Init(); // must be set up after CwaffEvents
                PlayerToolsSetup.Init();
                //Commands and Other Console Utilities
                Commands.Init();

                // Game tweaks
                HeckedMode.Init();

                asyncConfig1Setup = true;
                setupConfig1Watch.Stop();
            });
            #endregion

            #region Save API Setup (Async)
                bool asyncSaveApiSetup = false;
                System.Diagnostics.Stopwatch setupSaveWatch = null;
                ThreadPool.QueueUserWorkItem((object stateInfo) => {
                    setupSaveWatch = System.Diagnostics.Stopwatch.StartNew();
                    SaveAPI.SaveAPIManager.Setup("cg");  // Needed for prerequisite checking and save serialization
                    asyncSaveApiSetup = true;
                    setupSaveWatch.Stop();
                });
            #endregion

            #region Sprite Setup
                System.Diagnostics.Stopwatch setupSpritesWatch = System.Diagnostics.Stopwatch.StartNew();
                ETGMod.Assets.SetupSpritesFromAssembly(Assembly.GetExecutingAssembly(), "CwaffingTheGungy.Resources");
                setupSpritesWatch.Stop();
            #endregion

            #region Round 2 Config (Anything that requires sprites, cannot be async)
                System.Diagnostics.Stopwatch setupConfig2Watch = System.Diagnostics.Stopwatch.StartNew();
                while (!asyncConfig1Setup) Thread.Sleep(10); // we need to wait for our ResMap to be built, so wait here
                // Basic VFX Setup
                VFX.Init();
                //Status Effect Setup
                SoulLinkStatus.Init();
                //Goop Setup
                EasyGoopDefinitions.DefineDefaultGoops();
                // Note Does Setup
                CustomNoteDoer.Init();
                // Miscellaneous tweaks
                CwaffTweaks.Init();
                setupConfig2Watch.Stop();
            #endregion

            #region Audio (Async)
                bool asyncLoadedAudio = false;
                System.Diagnostics.Stopwatch setupAudioWatch = null;
                ThreadPool.QueueUserWorkItem((object stateInfo) => {
                    setupAudioWatch = System.Diagnostics.Stopwatch.StartNew();
                    ETGModMainBehaviour.Instance.gameObject.AddComponent<AudioSource>(); // is this necessary?
                    AudioResourceLoader.AutoloadFromAssembly("CwaffingTheGungy");  // Load Audio Banks
                    asyncLoadedAudio = true;
                    setupAudioWatch.Stop();
                });
            #endregion

            #region Actives
                System.Diagnostics.Stopwatch setupActivesWatch = System.Diagnostics.Stopwatch.StartNew();
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
                setupActivesWatch.Stop();
            #endregion

            #region Passives
                System.Diagnostics.Stopwatch setupPassivesWatch = System.Diagnostics.Stopwatch.StartNew();
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
                setupPassivesWatch.Stop();
            #endregion

            #region Guns
                System.Diagnostics.Stopwatch setupGunsWatch = System.Diagnostics.Stopwatch.StartNew();
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
                setupGunsWatch.Stop();
            #endregion

            #region Synergies
                System.Diagnostics.Stopwatch setupSynergiesWatch = System.Diagnostics.Stopwatch.StartNew();
                CwaffSynergies.Init();
                setupSynergiesWatch.Stop();
            #endregion

            #region UI Sprites (cannot be async, must set up textures on main thread)
                System.Diagnostics.Stopwatch setupUIWatch = System.Diagnostics.Stopwatch.StartNew();
                if (!C.SKIP_UI_LOAD)  // skip loading UI sprites in debug fast load mode
                {
                    Assembly ourAssembly = Assembly.GetExecutingAssembly();
                    ShopAPI.AddCustomCurrencyType(ResMap.Get("barter_s_icon")[0]+".png", Bart._BarterSpriteS, ourAssembly);
                    ShopAPI.AddCustomCurrencyType(ResMap.Get("barter_a_icon")[0]+".png", Bart._BarterSpriteA, ourAssembly);
                    ShopAPI.AddCustomCurrencyType(ResMap.Get("barter_b_icon")[0]+".png", Bart._BarterSpriteB, ourAssembly);
                    ShopAPI.AddCustomCurrencyType(ResMap.Get("barter_c_icon")[0]+".png", Bart._BarterSpriteC, ourAssembly);
                    ShopAPI.AddCustomCurrencyType(ResMap.Get("soul_sprite_ui_icon")[0]+".png", Uppskeruvel._SoulSpriteUI, ourAssembly);
                }
                setupUIWatch.Stop();
            #endregion

            #region Shop NPCs
                System.Diagnostics.Stopwatch setupShopsWatch = System.Diagnostics.Stopwatch.StartNew();
                // InsuranceBoi.Init();
                WhiteMage.Init();
                Bart.Init();
                setupShopsWatch.Stop();
            #endregion

            #region Fancy NPCs
                // Bombo.Init();  //disabled for now until i can find a better way to turn him off within game
            #endregion

            #region Bosses yo
                System.Diagnostics.Stopwatch setupBossesWatch = System.Diagnostics.Stopwatch.StartNew();
                BossBuilder.Init();
                SansBoss.Init();
                setupBossesWatch.Stop();
            #endregion

            #region Floor and Flow Initialization
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
                CwaffDungeons.Init(); // must be done before creating any custom floors / flows
                SansDungeon.Init();

                // Modified version of Anywhere mod, further stolen and modified from Apache's version
                FlowCommands.Install();

                setupFloorsWatch.Stop();
            #endregion

            #region Old Asset Stuff
                // Dissect.FindDefaultResource("DefaultTorch");
                // ETGModConsole.Log("Trying to load some stuff");
                // try
                // {
                //     // ETGModConsole.Log($"  we got {sharedAssets.LoadAsset<GameObject>("Bonfire")?.name ?? "null"}");
                //     // ETGModConsole.Log($"  we got {sharedAssets2.LoadAsset<GameObject>("Bonfire")?.name ?? "null"}");
                //     // ETGModConsole.Log($"  we got {braveResources.LoadAsset<GameObject>("Bonfire")?.name ?? "null"}");
                //     // ETGModConsole.Log($"  we got {enemiesBase.LoadAsset<GameObject>("Bonfire")?.name ?? "null"}");
                //     // ETGModConsole.Log($"  we got {sharedBase.LoadAsset<GameObject>("Bonfire")?.name ?? "null"}");
                //     // ETGModConsole.Log($"  we got {encounterAssets.LoadAsset<GameObject>("Bonfire")?.name ?? "null"}");
                //     // ETGModConsole.Log($"  we got {sharedAssets.LoadAsset<GameObject>("NapalmStrikeReticle")?.name ?? "null"}");
                //     // ETGModConsole.Log($"  we got {sharedAssets2.LoadAsset<GameObject>("NapalmStrikeReticle")?.name ?? "null"}");
                //     // ETGModConsole.Log($"  we got {braveResources.LoadAsset<GameObject>("NapalmStrikeReticle")?.name ?? "null"}");
                //     // ETGModConsole.Log($"  we got {enemiesBase.LoadAsset<GameObject>("NapalmStrikeReticle")?.name ?? "null"}");
                //     // ETGModConsole.Log($"  we got {sharedBase.LoadAsset<GameObject>("NapalmStrikeReticle")?.name ?? "null"}");

                //     // GameObject napalm = sharedAssets.LoadAsset<GameObject>("NapalmStrike");
                //     // if (napalm != null)
                //     //     ETGModConsole.Log($"  we got {napalm}");
                //     // else
                //     //     ETGModConsole.Log("  nullyboi o.o");
                // }
                // catch (Exception ex)
                // {
                //     ETGModConsole.Log($"  you broke it: {ex}");
                // }
            #endregion

            #region Hotfixes for bugs and issues mostly out of my control
                System.Diagnostics.Stopwatch setupHotfixesWatch = System.Diagnostics.Stopwatch.StartNew();
                DragunFightHotfix.Init();
                CoopTurboModeHotfix.Init();
                LargeGunAnimationHotfix.Init();
                DuctTapeSaveLoadHotfix.Init();
                // CoopDrillSoftlockHotfix.Init(); // incomplete
                QuickRestartRoomCacheHotfix.Init();
                RoomShuffleOffByOneHotfix.Init();
                setupHotfixesWatch.Stop();
            #endregion

            #region Wait for Async stuff to finish up
                System.Diagnostics.Stopwatch awaitAsyncWatch = System.Diagnostics.Stopwatch.StartNew();
                while (!asyncLoadedAudio) Thread.Sleep(10);
                while (!asyncSaveApiSetup) Thread.Sleep(10);
                awaitAsyncWatch.Stop();
            #endregion

            watch.Stop();
            ETGModConsole.Log($"Yay! :D Initialized <color=#aaffaaff>{C.MOD_NAME} v{C.MOD_VERSION}</color> in "+(watch.ElapsedMilliseconds/1000.0f)+" seconds");
            if (C.DEBUG_BUILD)
            {
                // ETGModConsole.Log($"    setupMemory    finished in "+(setupMemoryWatch.ElapsedMilliseconds/1000.0f)+" seconds");
                ETGModConsole.Log($"    setupConfig1   finished in {setupConfig1Watch.ElapsedMilliseconds} milliseconds (ASYNC)");
                ETGModConsole.Log($"    setupSprites   finished in {setupSpritesWatch.ElapsedMilliseconds} milliseconds");
                ETGModConsole.Log($"    setupConfig2   finished in {setupConfig2Watch.ElapsedMilliseconds} milliseconds");
                ETGModConsole.Log($"    setupSave      finished in {setupSaveWatch.ElapsedMilliseconds} milliseconds (ASYNC)");
                ETGModConsole.Log($"    setupAudio     finished in {setupAudioWatch.ElapsedMilliseconds} milliseconds (ASYNC)");
                ETGModConsole.Log($"    setupUI        finished in {setupUIWatch.ElapsedMilliseconds} milliseconds");
                ETGModConsole.Log($"    setupActives   finished in {setupActivesWatch.ElapsedMilliseconds} milliseconds");
                ETGModConsole.Log($"    setupPassives  finished in {setupPassivesWatch.ElapsedMilliseconds} milliseconds");
                ETGModConsole.Log($"    setupGuns      finished in {setupGunsWatch.ElapsedMilliseconds} milliseconds");
                ETGModConsole.Log($"    setupSynergies finished in {setupSynergiesWatch.ElapsedMilliseconds} milliseconds");
                ETGModConsole.Log($"    setupShops     finished in {setupShopsWatch.ElapsedMilliseconds} milliseconds");
                ETGModConsole.Log($"    setupBosses    finished in {setupBossesWatch.ElapsedMilliseconds} milliseconds");
                ETGModConsole.Log($"    setupFloors    finished in {setupFloorsWatch.ElapsedMilliseconds} milliseconds");
                ETGModConsole.Log($"    setupHotfixes  finished in {setupHotfixesWatch.ElapsedMilliseconds} milliseconds");
                ETGModConsole.Log($"    awaitAsync     finished in {awaitAsyncWatch.ElapsedMilliseconds} milliseconds");
                AkSoundEngine.PostEvent("vc_kirby_appeal01", ETGModMainBehaviour.Instance.gameObject);
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
}
