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

namespace CwaffingTheGungy
{
    [BepInPlugin(GUID, "Cwaffing the Gungy", "0.0.2")]
    [BepInDependency(ETGModMainBehaviour.GUID)]
    [BepInDependency("etgmodding.etg.mtgapi")]
    [BepInDependency("kyle.etg.gapi")]
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
                ETGModConsole.Log("Cwaffing the Gungy initialising...");

                // BraveMemory.EnsureHeapSize(1024*1024); ETGModConsole.Log("Ensured 1GB heap...");

                Instance = this;

                ETGMod.Assets.SetupSpritesFromAssembly(Assembly.GetExecutingAssembly(), "CwaffingTheGungy.Resources");

                // Build resource map for ease of access
                ResMap.Build();

                //Tools and Toolboxes
                StaticReferences.Init();
                Tools.Init();

                FakePrefabHooks.Init();
                HUDController.Init(); //Need to load early

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
                #endregion

                #region Passives
                    // Shine.Init(); unfinished
                    // Superstitious.Init(); unfinished
                    DriftersHeadgear.Init();
                    // Siphon.Init(); unfinished
                    ZoolandersDiary.Init();
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
                #endregion

                #region Guns
                    IronMaid.Add();
                    Natascha.Add();
                    PaintballCannon.Add();
                    // DerailGun.Add(); unfinished
                    // PopcornGun.Add(); no sprite
                    Tranquilizer.Add();
                    // LastResort.Add(); no sprite
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
                    // MasterSword.Add(); unfinished
                #endregion

                #region Synergies
                    CwaffSynergies.Init();
                #endregion

                #region Shop NPCs
                    Boomhildr.Init();
                #endregion

                #region Fancy NPCs
                    // Bombo.Init();  //disabled for now until i can find a better way to turn him off within game
                #endregion

                #region Flow stuff stolen from Apache
                    // AssetBundle sharedAssets = ResourceManager.LoadAssetBundle("shared_auto_001");
                    // AssetBundle sharedAssets2 = ResourceManager.LoadAssetBundle("shared_auto_002");
                    // AssetBundle sharedBase = ResourceManager.LoadAssetBundle("shared_base_001");
                    // AssetBundle braveResources = ResourceManager.LoadAssetBundle("brave_resources_001");
                    // AssetBundle enemiesBase = ResourceManager.LoadAssetBundle("enemies_base_001");
                    // AssetBundle encounterAssets = ResourceManager.LoadAssetBundle("encounters_base_001");

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
                    // try {
                    //     // Init Prefab Databases
                    //     CwaffDungeonPrefabs.InitCustomPrefabs(sharedAssets, sharedAssets2, braveResources, enemiesBase);
                    //     // Init Custom Enemy Prefabs
                    //     // ExpandCustomEnemyDatabase.InitPrefabs(expandSharedAssets1);
                    //     // Init Custom Room Prefabs
                    //     // ExpandRoomPrefabs.InitCustomRooms(sharedAssets, sharedAssets2, braveResources, enemiesBase);
                    //     // Init Custom DungeonFlow(s)
                    //     CwaffDungeonFlow.InitDungeonFlowsAndHooks(sharedAssets2);
                    // } catch (Exception ex) {
                    //     ETGModConsole.Log("[CtG] ERROR: Exception occured while building prefabs!", true);
                    //     Debug.LogException(ex);
                    //     sharedAssets = null;
                    //     sharedAssets2 = null;
                    //     braveResources = null;
                    //     enemiesBase = null;
                    //     return;
                    // }

                    // // Null bundles when done with them to avoid game crash issues
                    // sharedAssets = null;
                    // sharedAssets2 = null;
                    // braveResources = null;
                    // enemiesBase = null;
                #endregion

                #region Bosses yo
                    BossBuilder.Init();
                    SansBoss.Init();
                #endregion

                // Modified version of Anywhere mod, further stolen and modified from Apache's version
                FlowCommands.Install();

                //Misc. Tweaks
                CustomNoteDoer.Init();
                CustomDodgeRoll.InitCustomDodgeRollHooks();
                CwaffTweaks.Init();

                // ETGMod.StartGlobalCoroutine(this.delayedstarthandler());

                watch.Stop();
                AkSoundEngine.PostEvent("vc_kirby_appeal01", ETGModMainBehaviour.Instance.gameObject);
                ETGModConsole.Log("  Yay! :D CtG initialized in "+(watch.ElapsedMilliseconds/1000.0f)+" seconds");
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


