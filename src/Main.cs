using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Runtime;

using System.Collections.ObjectModel;
using System.IO;

using BepInEx;
using UnityEngine;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

using GungeonAPI;
using ItemAPI;
using EnemyAPI;
using SaveAPI;

namespace CwaffingTheGungy
{
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

                // BraveMemory.EnsureHeapSize(1024*1024); ETGModConsole.Log("Ensured 1GB heap...");

                Instance = this;

                ETGMod.Assets.SetupSpritesFromAssembly(Assembly.GetExecutingAssembly(), "CwaffingTheGungy.Resources");

                // Build resource map for ease of access
                ResMap.Build();

                //Tools and Toolboxes
                StaticReferences.Init();
                Tools.Init();
                CwaffEvents.Init();

                FakePrefabHooks.Init();
                HUDController.Init(); // Need to load early
                CustomAmmoDisplay.Init(); // Also need to load early

                ItemBuilder.Init();
                // SaveAPIManager.Setup("cg");
                AudioResourceLoader.InitAudio();
                ETGModMainBehaviour.Instance.gameObject.AddComponent<AudioSource>();

                PlayerToolsSetup.Init();
                VFX.Init();

                //Status Effect Setup
                SoulLinkStatus.Init();
                //Goop Setup
                EasyGoopDefinitions.DefineDefaultGoops();

                //Hats
                HatUtility.NecessarySetup();
                HatDefinitions.Init();

                //Commands and Other Console Utilities
                Commands.Init();

                #region Actives
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
                #endregion

                #region Passives
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
                #endregion

                #region Guns
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
                #endregion

                #region Synergies
                    CwaffSynergies.Init();
                #endregion

                #region Shop NPCs
                    // Boomhildr.Init();
                #endregion

                #region Fancy NPCs
                    // Bombo.Init();  //disabled for now until i can find a better way to turn him off within game
                #endregion

                #region Bosses yo
                    BossBuilder.Init();
                    SansBoss.Init();
                #endregion

                #region Flow stuff stolen from Apache
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
                        // Init Prefab Databases
                        CwaffDungeonPrefabs.InitCustomPrefabs(sharedAssets, sharedAssets2, braveResources, enemiesBase);
                        // Init Custom Enemy Prefabs
                        // ExpandCustomEnemyDatabase.InitPrefabs(expandSharedAssets1);
                        // Init Custom Room Prefabs
                        // ExpandRoomPrefabs.InitCustomRooms(sharedAssets, sharedAssets2, braveResources, enemiesBase);
                        // Init Custom DungeonFlow(s)
                        CwaffDungeonFlow.InitDungeonFlowsAndHooks(sharedAssets2);
                    } catch (Exception ex) {
                        ETGModConsole.Log("[CtG] ERROR: Exception occured while building prefabs!", true);
                        Debug.LogException(ex);
                    } finally {
                        // Null bundles when done with them to avoid game crash issues
                        sharedAssets = null;
                        sharedAssets2 = null;
                        braveResources = null;
                        enemiesBase = null;
                    }
                #endregion

                #region Floor Initialization
                    SansDungeon.InitCustomDungeon();
                #endregion

                #region Old Asset Stuff
                    Dissect.FindDefaultResource("DefaultTorch");
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

                // Modified version of Anywhere mod, further stolen and modified from Apache's version
                FlowCommands.Install();

                //Misc. Tweaks
                CustomNoteDoer.Init();
                CustomDodgeRoll.InitCustomDodgeRollHooks();
                CwaffTweaks.Init();
                HeckedMode.Init();

                // ETGMod.StartGlobalCoroutine(this.delayedstarthandler());

                watch.Stop();
                ETGModConsole.Log($"Yay! :D Initialized <color=#aaffaaff>{C.MOD_NAME} v{C.MOD_VERSION}</color> in "+(watch.ElapsedMilliseconds/1000.0f)+" seconds");
                if (C.DEBUG_BUILD)
                    AkSoundEngine.PostEvent("vc_kirby_appeal01", ETGModMainBehaviour.Instance.gameObject);

                // Debug.LogError("Gungy o.o!");
                // Debug.LogAssertion("Gungy o.o!");
                // Debug.LogWarning("Gungy o.o!");
                // Debug.Log("Gungy o.o!");
                // Debug.LogException("Gungy o.o!");
                // foreach (tk2dSpriteDefinition def in AmmonomiconController.ForceInstance.EncounterIconCollection.spriteDefinitions)
                //     ETGModConsole.Log($"  def: {def.name}");

                // var watch2 = System.Diagnostics.Stopwatch.StartNew();
                // for (float i = 0f; i < 100000f; i += 1f)
                // {
                //     Vector2 v = new Vector2(i, i);
                //     float a = v.magnitude;
                // }
                // watch2.Stop();
                // ETGModConsole.Log($"sqrt = {watch2.ElapsedTicks} ticks");

                // var watch3 = System.Diagnostics.Stopwatch.StartNew();
                // for (float i = 0f; i < 100000f; i += 1f)
                // {
                //     Vector2 v = new Vector2(i, i);
                //     float a = v.sqrMagnitude; // 3X FASTER
                // }
                // watch3.Stop();
                // ETGModConsole.Log($"no sqrt = {watch3.ElapsedTicks} ticks");

            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
                ETGModConsole.Log(e.StackTrace);
            }
        }

        // public IEnumerator delayedstarthandler()
        // {
        //     yield return null;
        //     this.DelayedInitialisation();
        //     yield break;
        // }
        // public void DelayedInitialisation()
        // {
        //     try
        //     {
        //         // CrossmodNPCLootPoolSetup.CheckItems();

        //         // OMITBChars.Shade = ETGModCompatibility.ExtendEnum<PlayableCharacters>(Initialisation.GUID, "Shade");

        //         ETGModConsole.Log("(Also finished DelayedInitialization)");
        //     }
        //     catch (Exception e)
        //     {
        //         ETGModConsole.Log(e.Message);
        //         ETGModConsole.Log(e.StackTrace);
        //     }
        // }
    }
}


