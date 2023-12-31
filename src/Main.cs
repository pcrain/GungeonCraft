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
    global using HutongGames.PlayMaker; //FSM___ stuff
    global using HutongGames.PlayMaker.Actions; //FSM___ stuff
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

// global using Gunfiguration;

global using static ProjectileModule;      //ShootStyle, ProjectileSequenceStyle
global using static tk2dBaseSprite;        //Anchor
global using static PickupObject;          //ItemQuality

namespace CwaffingTheGungy;

[BepInPlugin(C.MOD_GUID, "Cwaffing the Gungy", C.MOD_VERSION)]
[BepInDependency(ETGModMainBehaviour.GUID)]
[BepInDependency("alexandria.etgmod.alexandria")]
// [BepInDependency(Gunfiguration.C.MOD_GUID)]
public class Initialisation : BaseUnityPlugin
{
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
            System.Diagnostics.Process currentProcess = System.Diagnostics.Process.GetCurrentProcess();
            long oldMemory = currentProcess.WorkingSet64;
            if (C.DEBUG_BUILD)
                ETGModConsole.Log("Cwaffing the Gungy initializing...");

            Instance = this;

            #region Round 1 Config (hooks and database stuff where no sprites are needed, so it can be async)
            System.Diagnostics.Stopwatch setupConfig1Watch = null;
            Thread setupConfig1Thread = new Thread(() => {
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

                setupConfig1Watch.Stop();
            });
            setupConfig1Thread.Start();
            #endregion

            #region Hotfixes for bugs and issues mostly out of my control (Async)
                System.Diagnostics.Stopwatch setupHotfixesWatch = null;
                Thread setupHotfixesThread = new Thread(() => {
                    setupHotfixesWatch = System.Diagnostics.Stopwatch.StartNew();
                    DragunFightHotfix.Init();
                    CoopTurboModeHotfix.Init();
                    LargeGunAnimationHotfix.Init();
                    DuctTapeSaveLoadHotfix.Init();
                    // CoopDrillSoftlockHotfix.Init(); // incomplete
                    QuickRestartRoomCacheHotfix.Init();
                    RoomShuffleOffByOneHotfix.Init();
                    setupHotfixesWatch.Stop();
                });
                setupHotfixesThread.Start();
            #endregion

            #region Save API Setup (Async)
                System.Diagnostics.Stopwatch setupSaveWatch = null;
                Thread setupSaveThread = new Thread(() => {
                    setupSaveWatch = System.Diagnostics.Stopwatch.StartNew();
                    SaveAPI.SaveAPIManager.Setup("cg");  // Needed for prerequisite checking and save serialization
                    setupSaveWatch.Stop();
                });
                setupSaveThread.Start();
            #endregion

            #region Sprite Setup (Anything that requires sprites cannot be async)
                System.Diagnostics.Stopwatch setupSpritesWatch = System.Diagnostics.Stopwatch.StartNew();
                long usedMemoryBeforeSpriteSetup = currentProcess.WorkingSet64;
                // UnprocessedSpriteHotfix.Init();  // prevent SetupSpritesFromAssembly() from loading unprocessed sprites (saves about 50MB of RAM, which is a good chunk)
                ETGMod.Assets.SetupSpritesFromAssembly(Assembly.GetExecutingAssembly(), "CwaffingTheGungy.Resources");
                // UnprocessedSpriteHotfix.DeInit();  // we don't want to affect other mods
                ETGModConsole.Log($"  allocated {(currentProcess.WorkingSet64 - usedMemoryBeforeSpriteSetup).ToString("N0")} bytes of memory for sprite setup");
                setupSpritesWatch.Stop();
            #endregion

            #region Round 2 Config (Requires sprites, cannot be async)
                System.Diagnostics.Stopwatch setupConfig2Watch = System.Diagnostics.Stopwatch.StartNew();
                setupConfig1Thread.Join(); // we need to wait for our ResMap to be built, so wait here
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
                System.Diagnostics.Stopwatch setupAudioWatch = null;
                Thread setupAudioThread = new Thread(() => {
                    setupAudioWatch = System.Diagnostics.Stopwatch.StartNew();
                    ETGModMainBehaviour.Instance.gameObject.AddComponent<AudioSource>(); // is this necessary?
                    AudioResourceLoader.AutoloadFromAssembly("CwaffingTheGungy");  // Load Audio Banks
                    setupAudioWatch.Stop();
                });
                setupAudioThread.Start();
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
                Cuppajoe.Init();
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
                // DisplayStand.Init();  // scrapped since discouraging item use is not very fun
                SafetyGloves.Init();
                DrabOutfit.Init();
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
                Glockarina.Add();
                Magunet.Add();
                Wavefront.Add();
                setupGunsWatch.Stop();
            #endregion

            #region Synergies (Async)
                System.Diagnostics.Stopwatch setupSynergiesWatch = null;
                Thread setupSynergiesThread = new Thread(() => {
                    setupSynergiesWatch = System.Diagnostics.Stopwatch.StartNew();
                    CwaffSynergies.Init();
                    setupSynergiesWatch.Stop();
                });
                setupSynergiesThread.Start();
            #endregion

            #region UI Sprites (cannot be async, must set up textures on main thread)
                System.Diagnostics.Stopwatch setupUIWatch = System.Diagnostics.Stopwatch.StartNew();
                BetterAtlas.AddUISpriteBatch(new(){
                    ResMap.Get("barter_s_icon")[0]+".png",         Bart._BarterSpriteS,
                    ResMap.Get("barter_a_icon")[0]+".png",         Bart._BarterSpriteA,
                    ResMap.Get("barter_b_icon")[0]+".png",         Bart._BarterSpriteB,
                    ResMap.Get("barter_c_icon")[0]+".png",         Bart._BarterSpriteC,
                    ResMap.Get("soul_sprite_ui_icon")[0]+".png",   Uppskeruvel._SoulSpriteUI,
                    ResMap.Get("glockarina_storm_ui_icon")[0]+".png", Glockarina._StormSpriteUI,
                    ResMap.Get("glockarina_time_ui_icon")[0]+".png",  Glockarina._TimeSpriteUI,
                    ResMap.Get("glockarina_saria_ui_icon")[0]+".png", Glockarina._SariaSpriteUI,
                    ResMap.Get("glockarina_empty_ui_icon")[0]+".png", Glockarina._EmptySpriteUI,
                    // needs to be three separate sprites or the UI breaks
                    ResMap.Get("adrenaline_heart")[0]+".png", AdrenalineShot._FullHeartSpriteUI,
                    ResMap.Get("adrenaline_heart")[0]+".png", AdrenalineShot._HalfHeartSpriteUI,
                    ResMap.Get("adrenaline_heart")[0]+".png", AdrenalineShot._EmptyHeartSpriteUI,
                });
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

            #region Wait for Async stuff to finish up
                System.Diagnostics.Stopwatch awaitAsyncWatch = System.Diagnostics.Stopwatch.StartNew();
                setupConfig1Thread.Join();
                setupHotfixesThread.Join();
                setupSaveThread.Join();
                setupAudioThread.Join();
                setupSynergiesThread.Join();
                awaitAsyncWatch.Stop();
            #endregion

            watch.Stop();
            ETGModConsole.Log($"Yay! :D Initialized <color=#{ColorUtility.ToHtmlStringRGB(C.MOD_COLOR).ToLower()}>{C.MOD_NAME} v{C.MOD_VERSION}</color> in "+(watch.ElapsedMilliseconds/1000.0f)+" seconds");
            if (C.DEBUG_BUILD)
            {
                ETGModConsole.Log($"    setupConfig1   finished in {setupConfig1Watch.ElapsedMilliseconds} milliseconds (ASYNC)");
                ETGModConsole.Log($"    setupHotfixes  finished in {setupHotfixesWatch.ElapsedMilliseconds} milliseconds (ASYNC)");
                ETGModConsole.Log($"    setupSave      finished in {setupSaveWatch.ElapsedMilliseconds} milliseconds (ASYNC)");
                ETGModConsole.Log($"    setupSprites   finished in {setupSpritesWatch.ElapsedMilliseconds} milliseconds");
                ETGModConsole.Log($"    setupConfig2   finished in {setupConfig2Watch.ElapsedMilliseconds} milliseconds");
                ETGModConsole.Log($"    setupAudio     finished in {setupAudioWatch.ElapsedMilliseconds} milliseconds (ASYNC)");
                ETGModConsole.Log($"    setupUI        finished in {setupUIWatch.ElapsedMilliseconds} milliseconds");
                ETGModConsole.Log($"    setupActives   finished in {setupActivesWatch.ElapsedMilliseconds} milliseconds");
                ETGModConsole.Log($"    setupPassives  finished in {setupPassivesWatch.ElapsedMilliseconds} milliseconds");
                ETGModConsole.Log($"    setupGuns      finished in {setupGunsWatch.ElapsedMilliseconds} milliseconds");
                ETGModConsole.Log($"    setupSynergies finished in {setupSynergiesWatch.ElapsedMilliseconds} milliseconds (ASYNC)");
                ETGModConsole.Log($"    setupShops     finished in {setupShopsWatch.ElapsedMilliseconds} milliseconds");
                ETGModConsole.Log($"    setupBosses    finished in {setupBossesWatch.ElapsedMilliseconds} milliseconds");
                ETGModConsole.Log($"    setupFloors    finished in {setupFloorsWatch.ElapsedMilliseconds} milliseconds");
                ETGModConsole.Log($"    awaitAsync     finished in {awaitAsyncWatch.ElapsedMilliseconds} milliseconds");
                long newMemory = currentProcess.WorkingSet64;
                ETGModConsole.Log($"allocated {(newMemory - oldMemory).ToString("N0")} bytes of memory along the way");
                AkSoundEngine.PostEvent("vc_kirby_appeal01", ETGModMainBehaviour.Instance.gameObject);
            }

            // CwaffConfig.Init();

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
