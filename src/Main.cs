#region Global Usings
    global using System;
    global using System.Collections;
    global using System.Collections.Generic;
    global using System.Collections.Specialized;
    global using System.Linq;
    global using System.Text;
    global using System.Text.RegularExpressions;
    global using System.Reflection;
    global using System.Collections.ObjectModel;
    global using System.IO;
    global using System.ComponentModel;  // Debug stuff
    global using System.Threading;

    global using BepInEx;
    global using UnityEngine;
    global using UnityEngine.UI;
    global using MonoMod.RuntimeDetour;
    global using MonoMod.Cil;
    global using Mono.Cecil.Cil; //Instruction
    global using FullSerializer;
    global using HarmonyLib; //

    global using Gungeon;
    global using Dungeonator;
    global using HutongGames.PlayMaker; //FSM___ stuff
    global using HutongGames.PlayMaker.Actions; //FSM___ stuff
    global using Alexandria.BreakableAPI;
    global using Alexandria.CharacterAPI;
    global using Alexandria.ItemAPI;
    global using Alexandria.EnemyAPI;
    global using Alexandria.Misc;
    global using Alexandria.NPCAPI;
    global using Alexandria.cAPI;
    global using Alexandria.CustomDodgeRollAPI;
    global using Brave.BulletScript;
    global using Gunfiguration;

    global using SaveAPI; // only nonstandard api copied in from elsewhere, hopefully Alexandria standardizes this eventually
#endregion

global using ResourceExtractor        = Alexandria.ItemAPI.ResourceExtractor;
global using Component                = UnityEngine.Component;
global using ShopAPI                  = Alexandria.NPCAPI.ShopAPI;
global using RoomFactory              = Alexandria.DungeonAPI.RoomFactory;
global using ExoticObjects            = Alexandria.DungeonAPI.SetupExoticObjects;
global using StaticReferences         = Alexandria.DungeonAPI.StaticReferences;
global using CustomShopController     = Alexandria.NPCAPI.CustomShopController;
global using CustomShopItemController = Alexandria.NPCAPI.CustomShopItemController;

global using static ProjectileModule;        //ShootStyle, ProjectileSequenceStyle
global using static tk2dBaseSprite;          //Anchor
global using static PickupObject;            //ItemQuality
global using static BasicBeamController;     //BeamState
global using static DeadlyDeadlyGoopManager; //GoopPositionData
global using static PlayerStats;             //StatType

namespace CwaffingTheGungy;

[BepInPlugin(C.MOD_GUID, C.MOD_NAME, C.MOD_VERSION)]
[BepInDependency(ETGModMainBehaviour.GUID, "1.9.2")]
[BepInDependency(Alexandria.Alexandria.GUID, "0.4.22")]
[BepInDependency(Gunfiguration.C.MOD_GUID, "1.1.5")]
public class Initialisation : BaseUnityPlugin
{
    public static Initialisation Instance;
    internal static Harmony _Harmony;

    public void Start()
    {
        ETGModMainBehaviour.WaitForGameManagerStart(GMStart);
    }

    public void GMStart(GameManager manager)
    {
        try
        {
            #if DEBUG
                // ConstructorProfiler.Enable();
                // ConstructorProfiler.Toggle();
            #endif
            var watch = System.Diagnostics.Stopwatch.StartNew();
            System.Diagnostics.Process currentProcess = System.Diagnostics.Process.GetCurrentProcess();
            long oldMemory = currentProcess.WorkingSet64;
            #if DEBUG
                ETGModConsole.Log("Cwaffing the Gungy initializing...[DEBUG BUILD]");
            #endif

            Instance = this;
            _Harmony = new Harmony(C.MOD_GUID);

            #region Set up Early Harmony Patches (needs to be synchronous due to call to AmmonomiconController.ForceInstance)
                System.Diagnostics.Stopwatch setupEarlyHarmonyWatch = System.Diagnostics.Stopwatch.StartNew();
                AtlasHelper.InitSetupPatches(_Harmony);
                setupEarlyHarmonyWatch.Stop();
            #endregion

            #region Set up Late Harmony Patches (async, nothing else is needed until floor loading patches)
                System.Diagnostics.Stopwatch setupLateHarmonyWatch = null;
                Thread setupLateHarmonyThread = new Thread(() => {
                    setupLateHarmonyWatch = System.Diagnostics.Stopwatch.StartNew();
                    _Harmony.PatchAll();
                    setupLateHarmonyWatch.Stop();
                });
                setupLateHarmonyThread.Start();
            #endregion

            #region Set up Packed Texture Atlases, including UI sprites (absolutely cannot be async, handles the meat of texture loading)
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

            #region Initial Config (could be async since it's mostly hooks and database stuff where no sprites are needed, but it's fast enough that we just leave it sync)
                System.Diagnostics.Stopwatch setupConfigWatch = System.Diagnostics.Stopwatch.StartNew();
                // Load our configuration files
                CwaffConfig.Init();
                //Tools and Toolboxes
                CwaffPrerequisite.Init();
                // Modded Shop Item Setup
                ModdedShopItemAdder.Init();
                //Commands and Other Console Utilities
                Commands.Init();
                // Synergy enum setup
                CwaffSynergies.InitEnums();
                // Shader setup
                CwaffShaders.Init();
                // Game tweaks
                HeckedMode.Init();
                // Basic VFX Setup
                VFX.Init();
                // Status Effect Setup
                SoulLinkStatus.Init();
                //Goop Setup
                EasyGoopDefinitions.DefineDefaultGoops();
                // Boss Builder API
                BossBuilder.Init();
                // Note Does Setup
                CustomNoteDoer.Init();
                // Miscellaneous tweaks
                CwaffTweaks.Init();
                // Hecked Mode Tribute Statues
                HeckedShrine.Init();
                // Midrun data
                CwaffRunData.Init();
                setupConfigWatch.Stop();
            #endregion

            #region Hats
                System.Diagnostics.Stopwatch setupHatsWatch = System.Diagnostics.Stopwatch.StartNew();
                CwaffHats.Init();
                setupHatsWatch.Stop();
            #endregion

            #region Audio (Async)
                System.Diagnostics.Stopwatch setupAudioWatch = null;
                Thread setupAudioThread = new Thread(() => {
                    setupAudioWatch = System.Diagnostics.Stopwatch.StartNew();
                    AudioResourceLoader.AutoloadFromAssembly();  // Load Audio Banks
                    setupAudioWatch.Stop();
                });
                setupAudioThread.Start();
            #endregion

            #region Save API Setup (Async)
                System.Diagnostics.Stopwatch setupSaveWatch = null;
                Thread setupSaveThread = new Thread(() => {
                    setupSaveWatch = System.Diagnostics.Stopwatch.StartNew();
                    //WARNING: setup code has been modified to disable CustomHuntQuests setup, re-enable if needed later
                    SaveAPI.SaveAPIManager.Setup(C.MOD_PREFIX);  // Needed for prerequisite checking and save serialization
                    setupSaveWatch.Stop();
                });
                setupSaveThread.Start();
            #endregion

            #region Guns
                System.Diagnostics.Stopwatch setupGunsWatch = System.Diagnostics.Stopwatch.StartNew();

                IronMaid.Init();
                Natascha.Init();
                PaintballCannon.Init();
                Tranquilizer.Init();
                SoulKaliber.Init();
                KiBlast.Init();
                Deadline.Init();
                BBGun.Init();
                Bouncer.Init();
                Grandmaster.Init();
                QuarterPounder.Init();
                HolyWaterGun.Init();
                Alyx.Init();
                VacuumCleaner.Init();
                Gunbrella.Init();
                Blackjack.Init();
                SchrodingersGat.Init();
                RacketLauncher.Init();
                Outbreak.Init();
                HandCannon.Init();
                HatchlingGun.Init();
                Ticonderogun.Init();
                IceCreamGun.Init(); //NOTE: adding this here because it's a pseudo-gun and it ruins threading if initialized with other items
                AimuHakurei.Init();
                SeltzerPelter.Init();
                Missiletoe.Init();
                PlatinumStar.Init();
                PistolWhip.Init();
                Jugglernaut.Init();
                SubtractorBeam.Init();
                Alligator.Init();
                Lightwing.Init();
                KingsLaw.Init();
                Pincushion.Init();
                Crapshooter.Init();
                CarpetBomber.Init();
                Uppskeruvel.Init();
                Glockarina.Init();
                Magunet.Init();
                Wavefront.Init();
                Scotsman.Init();
                ChekhovsGun.Init();
                Vladimir.Init();
                Blamethrower.Init();
                Suncaster.Init();
                KALI.Init();
                AlienNailgun.Init();
                OmnidirectionalLaser.Init();
                RCLauncher.Init();
                Breegull.Init();
                SubMachineGun.Init();
                MacchiAuto.Init();
                Nycterian.Init();
                Maestro.Init();
                Starmageddon.Init();
                Widowmaker.Init();
                Zag.Init();
                BlasTechF4.Init();
                Telefragger.Init();
                English.Init();
                Femtobyte.Init();
                Exceptional.Init();
                Gunflower.Init();
                Hallaeribut.Init();
                Bubblebeam.Init();
                Groundhog.Init();
                DerailGun.Init();
                Yggdrashell.Init();
                Chroma.Init();
                Oddjob.Init();
                Overflow.Init();
                Plasmarble.Init();
                Sunderbuss.Init();
                Macheening.Init();
                Stereoscope.Init();
                Flakseed.Init();
                Xelsior.Init();
                Empath.Init();
                Sextant.Init();
                Wayfarer.Init();
                Leafblower.Init();

                Lazy.FinalizeGuns(); // Make sure encounter trackables are finalized so shoot styles properly display in the Ammonomicon

                setupGunsWatch.Stop();
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
                ChamberJammer.Init();
                Cuppajoe.Init();
                StopSign.Init();
                GunSynthesizer.Init();
                ChestScanner.Init();
                BulletbotImplant.Init();
                Frisbee.Init();
                WeightedRobes.Init();
                Detergent.Init();
                BottledAbyss.Init();
                PogoStick.Init();

                setupActivesWatch.Stop();
            #endregion

            #region Passives
                System.Diagnostics.Stopwatch setupPassivesWatch = System.Diagnostics.Stopwatch.StartNew();

                DriftersHeadgear.Init();
                RatPoison.Init();
                JohnsWick.Init();
                Gyroscope.Init();
                CustodiansBadge.Init();
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
                SafetyGloves.Init();
                DrabOutfit.Init();
                RingOfDefenestration.Init();
                AmmoConservationManual.Init();
                ReserveAmmolet.Init();
                ReflexAmmolet.Init();
                ScavengingArms.Init();
                ArmorPiercingRounds.Init();
                MMReloading.Init();
                MMAiming.Init();
                Calculator.Init();
                VolcanicAmmolet.Init();
                TryhardSnacks.Init();
                BulletproofTablecloth.Init();
                AmethystShard.Init();
                PrismaticScope.Init();
                Lichguard.Init();
                ScaldingJelly.Init();
                Domino.Init();

                GameManager.Instance.ResolveModdedLootChances(); // make sure loot chances between items are resolved

                setupPassivesWatch.Stop();
            #endregion

            // we have to wait for the rest of the harmony patches to finish before loading in floors
            System.Diagnostics.Stopwatch awaitLateHarmonyWatch = System.Diagnostics.Stopwatch.StartNew();
            setupLateHarmonyThread.Join();
            awaitLateHarmonyWatch.Stop();

            #region Custom Character Initialization
                Rogo.Init();
            #endregion

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
                    CwaffDungeons.InitDungeonFlows(sharedAssets2);
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
                System.Diagnostics.Stopwatch setupBossesWatch = System.Diagnostics.Stopwatch.StartNew();
                SansBoss.Init();
                setupBossesWatch.Stop();
            #endregion

            // Need to wait for all items and SaveAPI to be loaded before setting up synergies and shops
            System.Diagnostics.Stopwatch awaitItemsWatch = System.Diagnostics.Stopwatch.StartNew();
            setupSaveThread.Join();
            awaitItemsWatch.Stop();

            #region Synergies (not async due to safety concerns + it's fast already)
                System.Diagnostics.Stopwatch setupSynergiesWatch = System.Diagnostics.Stopwatch.StartNew();
                CwaffSynergies.Init();
                setupSynergiesWatch.Stop();
            #endregion

            #region Shop NPCs (can't be async due to post-load barter table setup issues causing null derefs)
                System.Diagnostics.Stopwatch setupShopsWatch = System.Diagnostics.Stopwatch.StartNew();
                Cammy.Init();
                Bart.Init();
                Kevlar.Init();
                setupShopsWatch.Stop();
            #endregion

            #region Wait for remaining async stuff to finish up
                System.Diagnostics.Stopwatch awaitAsyncWatch = System.Diagnostics.Stopwatch.StartNew();
                setupAudioThread.Join();
                awaitAsyncWatch.Stop();
            #endregion

            watch.Stop();
            ETGModConsole.Log($"Yay! :D Initialized <color=#{ColorUtility.ToHtmlStringRGB(C.MOD_COLOR).ToLower()}>{C.MOD_NAME} v{C.MOD_VERSION}</color> in "+(watch.ElapsedMilliseconds/1000.0f)+" seconds");
            #if DEBUG
                ETGModConsole.Log($"  {setupEarlyHarmonyWatch.ElapsedMilliseconds, 5}ms       setupEarlyHarmony");
                ETGModConsole.Log($"  {setupLateHarmonyWatch.ElapsedMilliseconds,  5}ms ASYNC setupLateHarmony ");
                ETGModConsole.Log($"  {setupAtlasesWatch.ElapsedMilliseconds,      5}ms       setupAtlases     ");
                ETGModConsole.Log($"  {setupConfigWatch.ElapsedMilliseconds,       5}ms       setupConfig      ");
                ETGModConsole.Log($"  {setupHatsWatch.ElapsedMilliseconds,         5}ms       setupHats        ");
                ETGModConsole.Log($"  {setupAudioWatch.ElapsedMilliseconds,        5}ms ASYNC setupAudio       ");
                ETGModConsole.Log($"  {setupSaveWatch.ElapsedMilliseconds,         5}ms ASYNC setupSave        ");
                ETGModConsole.Log($"  {setupGunsWatch.ElapsedMilliseconds,         5}ms       setupGuns        ");
                ETGModConsole.Log($"  {setupActivesWatch.ElapsedMilliseconds,      5}ms       setupActives     ");
                ETGModConsole.Log($"  {setupPassivesWatch.ElapsedMilliseconds,     5}ms       setupPassives    ");
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
                // ETGModConsole.Log($"you've played {GameStatsManager.Instance.GetPlayerStatValue(TrackedStats.TIME_PLAYED)} seconds");
            #endif
        }
        catch (Exception e)
        {
            ETGModConsole.Log(e.Message);
            ETGModConsole.Log(e.StackTrace);
        }
        finally
        {
            AtlasHelper.RemoveSetupPatches(_Harmony); // make sure setup-specific harmony patches get disabled even if an error occurs
            // if (C.DEBUG_BUILD)
            //     ConstructorProfiler.Toggle();
        }
        if (C.DEBUG_BUILD)
        {
            // ConstructorProfiler.Enable();
        }
    }
}
