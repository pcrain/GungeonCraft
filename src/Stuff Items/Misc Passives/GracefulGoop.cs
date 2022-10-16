using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dungeonator;
using UnityEngine;
using ItemAPI;
using SaveAPI;

namespace NevernamedsItems
{
    public class GracefulGoop : PassiveItem
    {
        public static void Init()
        {
            //The name of the item
            string itemName = "Graceful Goop";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it
            string resourceName = "NevernamedsItems/Resources/gracefulgoop_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a PassiveItem component to the object
            var item = obj.AddComponent<GracefulGoop>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "They Have Died... Inside";
            string longDesc = "Bullets trail poison." + "\n\nBrewed (and probably drunk) by a comedian who IS actually funny, no matter what he tells you.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //Do this after ItemBuilder.AddSpriteToObject!
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Adds the actual passive effect to the item
            item.AddToSubShop(ItemBuilder.ShopType.Goopton);


            //Set the rarity of the item
            item.quality = PickupObject.ItemQuality.B;
            AssetBundle assetBundle = ResourceManager.LoadAssetBundle("shared_auto_001");
            GracefulGoop.goopDefs = new List<GoopDefinition>();
            foreach (string text in GracefulGoop.goops)
            {
                GoopDefinition goopDefinition;
                try
                {
                    GameObject gameObject = assetBundle.LoadAsset(text) as GameObject;
                    goopDefinition = gameObject.GetComponent<GoopDefinition>();
                }
                catch
                {
                    goopDefinition = (assetBundle.LoadAsset(text) as GoopDefinition);
                }
                goopDefinition.name = text.Replace("assets/data/goops/", "").Replace(".asset", "");
                GracefulGoop.goopDefs.Add(goopDefinition);
            }
            List<GoopDefinition> list = GracefulGoop.goopDefs;

            item.SetupUnlockOnCustomFlag(CustomDungeonFlags.PURCHASED_GRACEFULGOOP, true);
            item.AddItemToGooptonMetaShop(25);
        }
        public void onFired(Projectile bullet, float eventchancescaler)
        {
            if (!Owner.HasPickupID(Gungeon.Game.Items["nn:tracer_rounds"].PickupObjectId))
            {
                TrailFireModifier mirrorProjectileModifier = bullet.gameObject.AddComponent<TrailFireModifier>();
                mirrorProjectileModifier.goopRadius = 0.5f;
                mirrorProjectileModifier.goopType = 1;
            }
            else if (Owner.HasPickupID(Gungeon.Game.Items["nn:tracer_rounds"].PickupObjectId))
            {
                if (UnityEngine.Random.value < 0.1f || Owner.HasPickupID(661))
                {
                    TrailFireModifier mirrorProjectileModifier = bullet.gameObject.AddComponent<TrailFireModifier>();
                    if (Owner.HasPickupID(275)) mirrorProjectileModifier.goopRadius = 1;
                    else mirrorProjectileModifier.goopRadius = 0.5f;
                    mirrorProjectileModifier.goopType = 0;
                    mirrorProjectileModifier.needsToUseGreenFire = true;
                }
                else
                {
                    TrailFireModifier mirrorProjectileModifier = bullet.gameObject.AddComponent<TrailFireModifier>();
                    mirrorProjectileModifier.goopRadius = 0.5f;
                    mirrorProjectileModifier.goopType = 1;
                }
            }
            if (Owner.CurrentGun.PickupObjectId == 481) spawnCameraFirePool();
            if (Owner.CurrentGun.PickupObjectId == 33) IsaacIsDeadLetsCrabDance();
        }
        private void IsaacIsDeadLetsCrabDance()
        {
            Owner.inventory.RemoveGunFromInventory(Owner.CurrentGun);
            IntVector2 bestRewardLocation = Owner.CurrentRoom.GetBestRewardLocation(IntVector2.One * 3, RoomHandler.RewardLocationStyle.PlayerCenter, true);
            Exploder.DoDefaultExplosion(Owner.sprite.WorldCenter, new Vector2());
            if (UnityEngine.Random.value < 0.5)
            {
                Chest Red_Chest = GameManager.Instance.RewardManager.A_Chest;
                Red_Chest.IsLocked = false;
                Chest.Spawn(Red_Chest, bestRewardLocation);
            }
            else if (UnityEngine.Random.value < 0.2)
            {
                Chest Black_Chest = GameManager.Instance.RewardManager.S_Chest;
                Black_Chest.IsLocked = true;
                Chest.Spawn(Black_Chest, bestRewardLocation);
            }
            else
            {
                Chest Synergy_Chest = GameManager.Instance.RewardManager.Synergy_Chest;
                Synergy_Chest.IsLocked = false;
                Chest.Spawn(Synergy_Chest, bestRewardLocation);
            }
        }
        private void spawnCameraFirePool()
        {
            if (!Owner.HasPickupID(Gungeon.Game.Items["nn:tracer_rounds"].PickupObjectId))
            {

                var ddgm = DeadlyDeadlyGoopManager.GetGoopManagerForGoopType(GracefulGoop.goopDefs[0]);
                ddgm.AddGoopCircle(Owner.sprite.WorldCenter, 10);
            }
        }
        private void onFiredBeam(BeamController sourceBeam)
        {

        }
        private DamageTypeModifier m_poisonImmunity;
        private void SpawnCasingSynergy(float damage, bool fatal, HealthHaver enemy)
        {
            if (enemy && enemy.aiActor && fatal)
            {
                if (Owner.HasPickupID(93) || Owner.HasPickupID(321) || Owner.HasPickupID(641) || Owner.HasPickupID(53) || Owner.HasPickupID(231) || Owner.HasPickupID(532) || Owner.HasPickupID(Gungeon.Game.Items["nn:golden_armour"].PickupObjectId) || Owner.HasPickupID(Gungeon.Game.Items["nn:gold_guon_stone"].PickupObjectId))
                {
                    LootEngine.SpawnItem(PickupObjectDatabase.GetById(68).gameObject, enemy.specRigidbody.UnitCenter, Vector2.zero, 1f, false, true, false);
                }
            }
        }
        public override void Pickup(PlayerController player)
        {
            player.PostProcessProjectile += this.onFired;
            player.OnAnyEnemyReceivedDamage += this.SpawnCasingSynergy;
            player.PostProcessBeam += this.onFiredBeam;
            this.m_poisonImmunity = new DamageTypeModifier();
            this.m_poisonImmunity.damageMultiplier = 0f;
            this.m_poisonImmunity.damageType = CoreDamageTypes.Poison;
            player.healthHaver.damageTypeModifiers.Add(this.m_poisonImmunity);
            base.Pickup(player);
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject result = base.Drop(player);
            player.OnAnyEnemyReceivedDamage -= this.SpawnCasingSynergy;
            player.PostProcessProjectile -= this.onFired;
            player.PostProcessBeam -= this.onFiredBeam;
            player.healthHaver.damageTypeModifiers.Remove(this.m_poisonImmunity);
            return result;
        }
        public override void OnDestroy()
        {
            if (Owner)
            {
                Owner.PostProcessProjectile -= this.onFired;
                Owner.PostProcessBeam -= this.onFiredBeam;
            }
            base.OnDestroy();
        }
        private static List<GoopDefinition> goopDefs;

        private static string[] goops = new string[]
        {
            "assets/data/goops/poison goop.asset",
        };
    }
}
