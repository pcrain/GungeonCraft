using Dungeonator;
using ItemAPI;
using MonoMod.RuntimeDetour;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NevernamedsItems
{
    public class LeadOfLife : PassiveItem
    {
        #region Vanilla Item Stat Setup
        //Dictionary
        public static Dictionary<int, List<string>> CompanionItemDictionary;
        //Companion definitions
        public static LeadOfLifeCompanionStats HotLeadCompanion;
        public static LeadOfLifeCompanionStats IrradiatedLeadCompanion;
        public static LeadOfLifeCompanionStats BatteryBulletsCompanion;
        public static LeadOfLifeCompanionStats PlusOneBulletsCompanion;
        public static LeadOfLifeCompanionStats AngryBulletsCompanion;
        public static LeadOfLifeCompanionStats CursedBulletsCompanion;
        public static LeadOfLifeCompanionStats EasyReloadBulletsCompanion;
        public static LeadOfLifeCompanionStats GhostBulletsCompanion;
        public static LeadOfLifeCompanionStats FlakBulletsCompanion;
        public static LeadOfLifeCompanionStats HeavyBulletsCompanion;
        public static LeadOfLifeCompanionStats KatanaBulletsCompanion;
        public static LeadOfLifeCompanionStats RemoteBulletsCompanion;
        public static LeadOfLifeCompanionStats BouncyBulletsCompanion;
        public static LeadOfLifeCompanionStats SilverBulletsCompanion;
        public static LeadOfLifeCompanionStats ZombieBulletsCompanion;
        public static LeadOfLifeCompanionStats Bloody9mmCompanion;
        public static LeadOfLifeCompanionStats BumbulletsCompanion;
        public static LeadOfLifeCompanionStats ChanceBulletsCompanion;
        public static LeadOfLifeCompanionStats CharmingRoundsCompanion;
        public static LeadOfLifeCompanionStats DevolverRoundsCompanion;
        public static LeadOfLifeCompanionStats GildedBulletsCompanion;
        public static LeadOfLifeCompanionStats HelixBulletsCompanion;
        public static LeadOfLifeCompanionStats HomingBulletsCompanion;
        public static LeadOfLifeCompanionStats MagicBulletsCompanion;
        public static LeadOfLifeCompanionStats RocketPoweredBulletsCompanion;
        public static LeadOfLifeCompanionStats ScattershotCompanion;
        public static LeadOfLifeCompanionStats ShadowBulletsCompanion;
        public static LeadOfLifeCompanionStats StoutBulletsCompanion;
        public static LeadOfLifeCompanionStats AlphaBulletsCompanion;
        public static LeadOfLifeCompanionStats OmegaBulletsCompanion;
        public static LeadOfLifeCompanionStats ChaosBulletsCompanion;
        public static LeadOfLifeCompanionStats ExplosiveRoundsCompanion;
        public static LeadOfLifeCompanionStats FatBulletsCompanion;
        public static LeadOfLifeCompanionStats FrostBulletsCompanion;
        public static LeadOfLifeCompanionStats HungryBulletsCompanion;
        public static LeadOfLifeCompanionStats OrbitalBulletsCompanion;
        public static LeadOfLifeCompanionStats ShockRoundsCompanion;
        public static LeadOfLifeCompanionStats SnowballetsCompanion;
        public static LeadOfLifeCompanionStats VorpalBulletsCompanion;
        public static LeadOfLifeCompanionStats BlankBulletsCompanion;
        public static LeadOfLifeCompanionStats PlatinumBulletsCompanion;
        public static LeadOfLifeCompanionStats LichsEyeBulletsCompanionA;
        public static LeadOfLifeCompanionStats LichsEyeBulletsCompanionB;
        public static LeadOfLifeCompanionStats BulletTimeCompanion;
        public static LeadOfLifeCompanionStats DarumaCompanion;
        public static LeadOfLifeCompanionStats RiddleOfLeadCompanion;
        public static LeadOfLifeCompanionStats ShotgunCoffeeCompanion;
        public static LeadOfLifeCompanionStats ShotgaColaCompanion;
        public static LeadOfLifeCompanionStats ElderBlankCompanion;
        public static LeadOfLifeCompanionStats BulletGunCompanion;
        public static LeadOfLifeCompanionStats ShellGunCompanion;
        public static LeadOfLifeCompanionStats CaseyCompanion;
        public static LeadOfLifeCompanionStats BTCKTPCompanion;
        #endregion

        #region Modded Item Stat Setup
        public static LeadOfLifeCompanionStats OneShotCompanion;
        public static LeadOfLifeCompanionStats FiftyCalRoundsCompanion;
        public static LeadOfLifeCompanionStats AlkaliBulletsCompanion;
        public static LeadOfLifeCompanionStats AntimagicRoundsCompanion;
        public static LeadOfLifeCompanionStats AntimatterBulletsCompanion;
        public static LeadOfLifeCompanionStats BashfulShotCompanion;
        public static LeadOfLifeCompanionStats BashingBulletsCompanion;
        public static LeadOfLifeCompanionStats BirdshotCompanion;
        public static LeadOfLifeCompanionStats BlightShellCompanion;
        public static LeadOfLifeCompanionStats BloodthirstyBulletsCompanion;

        public static LeadOfLifeCompanionStats TitanBulletsCompanion;



        #endregion



        public static void Init()
        {
            HotLeadCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_hotlead" };
            IrradiatedLeadCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_irradiatedlead" };
            BatteryBulletsCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_batterybullets" };
            PlusOneBulletsCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_plusonebullets" };
            AngryBulletsCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_angrybullets" };
            CursedBulletsCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_cursedbullets" };
            EasyReloadBulletsCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_easyreloadbullets" };
            GhostBulletsCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_ghostbullets" };
            FlakBulletsCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_flakbullets" };
            HeavyBulletsCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_heavybullets" };
            KatanaBulletsCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_katanabullets" };
            RemoteBulletsCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_remotebullets" };
            BouncyBulletsCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_bouncybullets" };
            SilverBulletsCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_silverbullets" };
            ZombieBulletsCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_zombiebullets" };
            Bloody9mmCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_bloody9mm" };
            BumbulletsCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_bumbullets" };
            ChanceBulletsCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_chancebullets" };
            CharmingRoundsCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_charmingrounds" };
            DevolverRoundsCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_devolverrounds" };
            GildedBulletsCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_gildedbullets" };
            HelixBulletsCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_helixbullets" };
            HomingBulletsCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_homingbullets" };
            MagicBulletsCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_magicbullets" };
            RocketPoweredBulletsCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_rocketpoweredbullets" };
            ScattershotCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_scattershot" };
            ShadowBulletsCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_shadowbullets" };
            StoutBulletsCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_stoutbullets" };
            AlphaBulletsCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_alphabullets" };
            OmegaBulletsCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_omegabullets" };
            ChaosBulletsCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_chaosbullets" };
            ExplosiveRoundsCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_explosiverounds" };
            FatBulletsCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_fatbullets" };
            FrostBulletsCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_frostbullets" };
            HungryBulletsCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_hungrybullets" };
            OrbitalBulletsCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_orbitalbullets" };
            ShockRoundsCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_shockrounds" };
            SnowballetsCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_snowballets" };
            VorpalBulletsCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_vorpalbullets" };
            BlankBulletsCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_blankbullets" };
            PlatinumBulletsCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_platinumbullets" };
            LichsEyeBulletsCompanionA = new LeadOfLifeCompanionStats() { guid = "leadoflife_lichseyebullets_a" };
            LichsEyeBulletsCompanionB = new LeadOfLifeCompanionStats() { guid = "leadoflife_lichseyebullets_b" };
            BulletTimeCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_bullettime" };
            DarumaCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_daruma" };
            RiddleOfLeadCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_riddleoflead" };
            ShotgunCoffeeCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_shotguncoffee" };
            ShotgaColaCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_shotgacola" };
            ElderBlankCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_elderblank" };
            BulletGunCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_bulletgun" };
            ShellGunCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_shellgun" };
            CaseyCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_casey" };
            BTCKTPCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_btcktp" };

            OneShotCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_oneshot" };
            FiftyCalRoundsCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_fiftycalrounds" };
            AlkaliBulletsCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_alkalibullets" };
            AntimagicRoundsCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_antimagicrounds" };
            AntimatterBulletsCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_antimatterbullets" };
            BashfulShotCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_bashfulshot" };
            BashingBulletsCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_bashingbullets" };
            BirdshotCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_birdshot" };
            BlightShellCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_blightshell" };
            BloodthirstyBulletsCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_bloodthirstybullets" };
            TitanBulletsCompanion = new LeadOfLifeCompanionStats() { guid = "leadoflife_titanbullets" };



            string name = "Lead of Life";
            string resourcePath = "NevernamedsItems/Resources/Companions/LeadOfLife/leadoflife_icon";
            GameObject gameObject = new GameObject(name);
            var item = gameObject.AddComponent<LeadOfLife>();
            ItemBuilder.AddSpriteToObject(name, resourcePath, gameObject);
            string shortDesc = "Forged Friends";
            string longDesc = "Brings bullet upgrades to life!" + "\n\nA tiny fragment of lead left over from the creation of the very first Bullet Kin." + "\n\nIt still glows with lifegiving power...";
            item.quality = PickupObject.ItemQuality.S;
            item.SetupItem(shortDesc, longDesc, "nn");

            LeadOfLifeID = item.PickupObjectId;

            activeItemDropHook = new Hook(
                typeof(PlayerController).GetMethod("DropActiveItem"),
                typeof(LeadOfLife).GetMethod("DropActiveHook")
            );

            CompanionItemDictionary = new Dictionary<int, List<string>>();           
        }
        public static int LeadOfLifeID;
        public static Hook activeItemDropHook;

        //REMOVE THIS HOOK WHEN ALEXANDRIA IS UPDATED, THIS HOOK WILL BE HANDLED BY ALEXANDRIA
        public static DebrisObject DropActiveHook(Func<PlayerController, PlayerItem, float, bool, DebrisObject> orig, PlayerController self, PlayerItem item, float force = 4f, bool deathdrop = false)
        {
            try
            {
                if (self)
                {
                    foreach (PassiveItem potentialLOL in self.passiveItems)
                    {
                        if (potentialLOL.GetComponent<LeadOfLife>() != null) potentialLOL.GetComponent<LeadOfLife>().RecalculateCompanions(true);
                    } 
                }                    
                return orig(self, item, force, deathdrop);
            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
                ETGModConsole.Log(e.StackTrace);
                return null;
            }
        }
      
        //Methods which change the number of companions, by spawning new ones and deleting old ones
        public void RecalculateCompanions(bool late = false)
        {
            if (late) { StartCoroutine(LateRecalculation()); }
            else
            {
                DestroyAllCompanions();
                foreach (PassiveItem item in Owner.passiveItems) TrySpawnCompanionForID(item.PickupObjectId, item);
                foreach (PlayerItem item in Owner.activeItems) TrySpawnCompanionForID(item.PickupObjectId, item);
                foreach (Gun item in Owner.inventory.AllGuns) TrySpawnCompanionForID(item.PickupObjectId, item);
            }
        }
        private IEnumerator LateRecalculation()
        {
            yield return null;
            DestroyAllCompanions();
            foreach (PassiveItem item in Owner.passiveItems) TrySpawnCompanionForID(item.PickupObjectId, item);
            foreach (PlayerItem item in Owner.activeItems) TrySpawnCompanionForID(item.PickupObjectId, item);
            foreach (Gun item in Owner.inventory.AllGuns) TrySpawnCompanionForID(item.PickupObjectId, item);
            yield break;
        }
        public void TrySpawnCompanionForID(int id, PickupObject correspondingItem = null)
        {
            if (CompanionItemDictionary.ContainsKey(id)) { foreach (string guid in CompanionItemDictionary[id]) { SpawnNewCompanion(guid, correspondingItem); } }
        }
        private void SpawnNewCompanion(string guid, PickupObject correspondingItem = null)
        {
            GameObject spawnedCompanion = UnityEngine.Object.Instantiate<GameObject>(EnemyDatabase.GetOrLoadByGuid(guid).gameObject, Owner.transform.position, Quaternion.identity);
            LeadOfLifeCompanion leadOfLifeComponent = spawnedCompanion.GetOrAddComponent<LeadOfLifeCompanion>();
            extantCompanions.Add(leadOfLifeComponent);
            leadOfLifeComponent.Initialize(Owner);
            globalCompanionFirerateMultiplier *= leadOfLifeComponent.globalCompanionFirerateMultiplier;
            if (correspondingItem != null) leadOfLifeComponent.correspondingItem = correspondingItem;
            if (leadOfLifeComponent.specRigidbody) PhysicsEngine.Instance.RegisterOverlappingGhostCollisionExceptions(leadOfLifeComponent.specRigidbody, null, false);
        }
        private void DestroyAllCompanions()
        {
            globalCompanionFirerateMultiplier = 1;
            if (extantCompanions.Count <= 0) { return; }
            for (int i = extantCompanions.Count - 1; i >= 0; i--)
            {
                if (extantCompanions[i] && extantCompanions[i].gameObject) { UnityEngine.Object.Destroy(extantCompanions[i].gameObject); }
            }
            extantCompanions.Clear();
        }

        //Monitoring, to control the amount of companions at all times
        private int lastItems;
        public override void Update()
        {
            if (!Dungeon.IsGenerating && Owner)
            {
                int currentItems = (Owner.passiveItems.Count + Owner.activeItems.Count + Owner.inventory.AllGuns.Count);
                if (currentItems != lastItems)
                {
                    RecalculateCompanions();
                    lastItems = currentItems;
                }
            }
            base.Update();
        }
        private void OnNewFloor(PlayerController player) { RecalculateCompanions(); }

        //Pickup and Disable
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            RecalculateCompanions();
            player.OnNewFloorLoaded += this.OnNewFloor;
        }
        public override void DisableEffect(PlayerController player)
        {
            DestroyAllCompanions();
            player.OnNewFloorLoaded -= this.OnNewFloor;
            base.DisableEffect(player);
        }

        //Variables
        public float globalCompanionFirerateMultiplier = 1;
        public List<LeadOfLifeCompanion> extantCompanions = new List<LeadOfLifeCompanion>();
    }
    public class LeadOfLifeCompanionStats
    {
        public string guid;
        public GameObject prefab;
    }
}
