using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;

using UnityEngine;
using MonoMod.RuntimeDetour;

using Gungeon;
using Dungeonator;
using Alexandria.ItemAPI;
using Alexandria.Misc;
using SaveAPI;

namespace CwaffingTheGungy
{
    class InsurancePolicy : PlayerItem
    {
        public static string ItemName         = "Insurance Policy";
        public static string SpritePath       = "insurance_policy_icon";
        public static string ShortDescription = "TBD";
        public static string LongDescription  = "TBD";

        private const float _MAX_DIST = 5f;

        internal static Chest _InsuranceChestPrefab = null;
        internal static GameObject _InsuranceSparklePrefab = null;

        private static int _InsurancePolicyId;
        private static List<int> _InsuredItems = new();
        private static string _InsuranceFile;

        public static void Init()
        {
            PlayerItem item = Lazy.SetupActive<InsurancePolicy>(ItemName, SpritePath, ShortDescription, LongDescription);
            item.quality      = PickupObject.ItemQuality.A;
            item.consumable   = false;
            item.CanBeDropped = true;
            item.SetCooldownType(ItemBuilder.CooldownType.Timed, 2f);

            _InsuranceChestPrefab = GameManager.Instance.RewardManager.GetTargetChestPrefab(PickupObject.ItemQuality.B).gameObject.ClonePrefab().GetComponent<Chest>();
                _InsuranceChestPrefab.groundHitDelay = 0.10f;
                _InsuranceChestPrefab.groundHitDelay = 0.40f;
                _InsuranceChestPrefab.spawnAnimName = _InsuranceChestPrefab.sprite.SetUpAnimation("insurance_chest_appear", 11);
                    _InsuranceChestPrefab.spriteAnimator.GetClipByName("insurance_chest_appear").frames[0].triggerEvent = true;
                    _InsuranceChestPrefab.spriteAnimator.GetClipByName("insurance_chest_appear").frames[0].eventAudio = "Play_OBJ_smallchest_spawn_01";
                _InsuranceChestPrefab.openAnimName  = _InsuranceChestPrefab.sprite.SetUpAnimation("insurance_chest_open", 12);
                _InsuranceChestPrefab.breakAnimName = _InsuranceChestPrefab.sprite.SetUpAnimation("insurance_chest_break", 11);
                _InsuranceChestPrefab.sprite.SetUpAnimation("insurance_chest_idle", 11);
                _InsuranceChestPrefab.IsLocked = false; // can't get lock renderer to attach properly after adjusting appearance animation
                _InsuranceChestPrefab.GetComponent<MajorBreakable>().HitPoints = float.MaxValue; // insurance chest should be unbreakable

            _InsuranceSparklePrefab = VFX.RegisterVFXObject("InsuranceSparkle", ResMap.Get("insurance_policy_icon"),
                fps: 1, loops: true, anchor: tk2dBaseSprite.Anchor.MiddleCenter, emissivePower: 1f);

            _InsurancePolicyId = item.PickupObjectId;
            _InsuranceFile     = Path.Combine(SaveManager.SavePath,"insurance.csv");

            CwaffEvents.OnFirstFloorFullyLoaded += InsuranceCheck;
        }

        public static void InsuranceCheck()
        {
            GameManager.Instance.StartCoroutine(InsuranceCheck_CR());
        }

        public static IEnumerator InsuranceCheck_CR()
        {
            PlayerController p1 = GameManager.Instance.PrimaryPlayer;
            while (!p1.AcceptingAnyInput)
                yield return null;

            LoadInsuredItems();
            ClearInsuredItemsFile();
            if (_InsuredItems.Count() == 0)
                yield break;

            bool success;
            Chest chest = Chest.Spawn(_InsuranceChestPrefab, GameManager.Instance.PrimaryPlayer.CurrentRoom.GetCenteredVisibleClearSpot(2, 2, out success));
            chest.m_isMimic = false;
            chest.forceContentIds = new(_InsuredItems);
            _InsuredItems.Clear();
        }

        public override void DoEffect(PlayerController user) ///
        {
            PickupObject nearestPickup = null;
            float nearestDist = _MAX_DIST;
            foreach (DebrisObject debris in StaticReferenceManager.AllDebris)
            {
                if (!debris.IsPickupObject)
                    continue;
                if (debris.GetComponentInChildren<PickupObject>() is not PickupObject pickup)
                    continue;
                if (pickup.GetComponent<Insured>())
                    continue;

                float pickupDist = (debris.sprite.WorldCenter - user.sprite.WorldCenter).magnitude;
                if (pickupDist >= nearestDist)
                    continue;

                nearestPickup = pickup;
                nearestDist   = pickupDist;
            }
            if (!nearestPickup)
                return;

            nearestPickup.gameObject.GetOrAddComponent<Insured>();
            ETGModConsole.Log($"insuring {nearestPickup.DisplayName}");
            _InsuredItems.Add(nearestPickup.PickupObjectId);
            SaveInsuredItems();
        }

        internal static void SaveInsuredItems()
        {
            using (var file = File.CreateText(_InsuranceFile))
            {
                bool first = true;
                foreach(int itemId in _InsuredItems)
                {
                    file.Write($"{(first ? "" : ",")}{itemId}");
                    first = false;
                }
            }
        }

        internal static void LoadInsuredItems()
        {
            _InsuredItems.Clear();
            if (!File.Exists(_InsuranceFile))
                return;
            try
            {
                string[] itemIds = File.ReadAllLines(_InsuranceFile)[0].Split(',');
                foreach(string itemId in itemIds)
                    _InsuredItems.Add(Int32.Parse(itemId));
            }
            catch (Exception)
            {
                _InsuredItems.Clear(); // if there's any sort of parse error, give up immediately
            }
        }

        internal static void ClearInsuredItemsFile()
        {
            if (File.Exists(_InsuranceFile))
                File.Delete(_InsuranceFile);
        }
    }

    // NOTE: this is apparently recreated every time the item is picked up...why?
    public class Insured : MonoBehaviour
    {
        private PickupObject _pickup;
        private int _pickupId;

        // these need to be public because Guns create copies of themselves when picked up, and
        //   private fields don't serialize and get reset
        public bool dropped = false;
        public GameObject vfx = null;

        private void Start()
        {
            this._pickup = base.GetComponent<PickupObject>();
            this._pickupId = this._pickup.PickupObjectId;
            this.dropped = true;
            OnDrop();
        }

        private void LateUpdate()
        {
            // if (this._dropped)
            //     DoInsuranceParticles();

            bool dropped = !GameManager.Instance.AnyPlayerHasPickupID(this._pickupId);
            if (this.dropped == dropped)
                return; // cached state is the same

            if (dropped)
                OnDrop();
            else
                OnPickup();
            this.dropped = dropped;
        }

        private void DoInsuranceParticles()
        {
            if (UnityEngine.Random.value > 0.1f)
                return;
            SpawnManager.SpawnVFX(InsurancePolicy._InsuranceSparklePrefab,
                this._pickup.sprite.WorldCenter + Lazy.RandomVector(0.5f), Lazy.RandomEulerZ());
        }

        private void OnDrop()
        {
            if (this.vfx != null)
                UnityEngine.Object.Destroy(this.vfx);
            ETGModConsole.Log($"spawning vfx");
            this.vfx = SpawnManager.SpawnVFX(InsurancePolicy._InsuranceSparklePrefab, this._pickup.sprite.WorldTopCenter + new Vector2(0f, 0.5f), Quaternion.identity);
            this.vfx.transform.parent = this._pickup.gameObject.transform;
            this.vfx.SetAlphaImmediate(0.5f);
            // AkSoundEngine.PostEvent("zenkai_aura_sound", this._pickup.gameObject);
        }

        private void OnPickup()
        {
            if (this.vfx != null)
            {
                UnityEngine.Object.Destroy(this.vfx);
                this.vfx = null;
            }
        }
    }
}
