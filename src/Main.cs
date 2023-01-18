using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using BepInEx;
using UnityEngine;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

using GungeonAPI;
using ItemAPI;

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
        public static Initialisation instance;

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

                instance = this;

                ETGMod.Assets.SetupSpritesFromFolder(System.IO.Path.Combine(this.FolderPath(), "sprites"));

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
                SoulLinkStatusEffectSetup.Init();
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
                #endregion

                #region Passives
                    Shine.Init();
                    Superstitious.Init();
                    HLD.Init();
                    Siphon.Init();
                    ZoolandersDiary.Init();
                    RatPoison.Init();
                    JohnsWick.Init();
                #endregion

                #region Guns
                    // TestLightning.Add();

                    RainCheck.Add();
                    Natasha.Add();
                    PaintballGun.Add();
                    DerailGun.Add();
                    PopcornGun.Add();
                    Tranquilizer.Add();
                    LastResort.Add();
                    Encircler.Add();
                    Nug.Add();
                    SoulKaliber.Add();
                    GamblersFallacy.Add();
                    GasterBlaster.Add();
                    Commitment.Add();
                    // HeadCannon.Add();
                    Telefragger.Add();
                    Kinsurrection.Add();
                    SpinCycle.Add();
                    TimingGun.Add();
                    KiBlast.Add();
                    Deadline.Add();
                    BBGun.Add();

                    MasterSword.Add();
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
                    AssetBundle sharedAssets = ResourceManager.LoadAssetBundle("shared_auto_001");
                    AssetBundle sharedAssets2 = ResourceManager.LoadAssetBundle("shared_auto_002");
                    AssetBundle braveResources = ResourceManager.LoadAssetBundle("brave_resources_001");
                    AssetBundle enemiesBase = ResourceManager.LoadAssetBundle("enemies_base_001");

                    try {
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
                        sharedAssets = null;
                        sharedAssets2 = null;
                        braveResources = null;
                        enemiesBase = null;
                        return;
                    }

                    // Null bundles when done with them to avoid game crash issues
                    sharedAssets = null;
                    sharedAssets2 = null;
                    braveResources = null;
                    enemiesBase = null;
                #endregion

                // Modified version of Anywhere mod, further stolen and modified from Apache's version
                FlowCommands.Install();

                //Misc. Tweaks
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


