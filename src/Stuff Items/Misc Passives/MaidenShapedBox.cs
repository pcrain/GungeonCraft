using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using ItemAPI;
using SaveAPI;

namespace NevernamedsItems
{
    public class MaidenShapedBox : PassiveItem
    {
        public static void Init()
        {
            //The name of the item
            string itemName = "Maiden-Shaped Box";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it
            string resourceName = "NevernamedsItems/Resources/maidenshapedbox_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a PassiveItem component to the object
            var item = obj.AddComponent<MaidenShapedBox>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Singlehandedly Ruining This Game";
            string longDesc = "The itty bitty nanites contained within this peculiarly shaped container are specifically programmed to seek, destroy, and transmute Lead Maidens.\n\n" + "Whoever made this thing must have really hated Lead Maidens.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //Do this after ItemBuilder.AddSpriteToObject!
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Adds the actual passive effect to the item
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.Curse, 1f, StatModifier.ModifyMethod.ADDITIVE);

            //Set the rarity of the item
            item.quality = PickupObject.ItemQuality.B;

            item.SetupUnlockOnCustomFlag(CustomDungeonFlags.JAMMEDLEADMAIDEN_QUEST_REWARDED, true);
        }
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.PostProcessProjectile += this.PostProcessProjectile;
            player.PostProcessBeam += this.PostProcessBeam;
        }

        /*private void OnDealtDamage(PlayerController player, float amount, bool fatal, HealthHaver target)
        {
            AIActor targetEnemy = target.gameObject.GetComponent<AIActor>();
            string targetGuid = null;
            if (targetEnemy)
            {
                targetGuid = targetEnemy.EnemyGuid;
            }
            if (!string.IsNullOrEmpty(targetGuid) && listMaidens.Contains(targetGuid) && fatal == true)
            {
                ETGModConsole.Log("You just killed a Lead Maiden");
            }
            

        }*/
        private void PostProcessProjectile(Projectile sourceProjectile, float effectChanceScalar)
        {
            try
            {
                //ETGModConsole.Log("posting the process to the projectile in the mail");
                //ETGModConsole.Log($"Proj Null: {projectile == null} | OnHitEnemy null: {projectile?.OnHitEnemy == null}");
                sourceProjectile.OnHitEnemy += this.OnHitEnemy;
            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
            }
        }
        private void PostProcessBeam(BeamController sourceBeam)
        {
            try
            {
                sourceBeam.projectile.OnHitEnemy += this.OnHitEnemy;
            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
            }
        }


        public static List<string> listMaidens = new List<string>()
        {
            EnemyGuidDatabase.Entries["lead_maiden"],
            EnemyGuidDatabase.Entries["fridge_maiden"],
        };

        private void OnHitEnemy(Projectile arg1, SpeculativeRigidbody arg2, bool arg3)
        {
            string enemyGuid = arg2?.aiActor?.EnemyGuid;
            if (!string.IsNullOrEmpty(enemyGuid))
            {
                try
                {
                    //ETGModConsole.Log("This enemy's Guid is: " + enemyGuid);
                    foreach (var guid in listMaidens)
                    {
                        if (guid.Equals(enemyGuid))
                        {
                            if (GameStatsManager.Instance.IsRainbowRun == true)
                            {
                                SpawnMaidenRainbowLoot(arg2.aiActor.healthHaver);
                            }
                            else
                            {
                                SpawnMaidenLoot(arg2.aiActor.healthHaver);
                            }
                            InstaKill(arg2.aiActor.healthHaver);
                            return;
                        }
                    }
                }
                catch (Exception e)
                {
                    ETGModConsole.Log(e.Message);
                }
            }
        }

        public void SpawnMaidenLoot(HealthHaver target)
        {
            if (target.healthHaver.IsDead) return;
            int randomTierSelectionNumber = UnityEngine.Random.Range(1, 100);
            var itemQuality = PickupObject.ItemQuality.D;
            if (randomTierSelectionNumber <= 37) itemQuality = PickupObject.ItemQuality.D; //Make Tier D
            else if (randomTierSelectionNumber <= 67) itemQuality = PickupObject.ItemQuality.C; //Make Tier C
            else if (randomTierSelectionNumber <= 87) itemQuality = PickupObject.ItemQuality.B; //Make Tier B
            else if (randomTierSelectionNumber <= 98) itemQuality = PickupObject.ItemQuality.A; //Make Tier A               
            else if (randomTierSelectionNumber <= 100) itemQuality = PickupObject.ItemQuality.S; //Make Tier S
            GameManager.Instance.RewardManager.SpawnTotallyRandomItem(target.specRigidbody.UnitCenter, itemQuality, itemQuality);
        }

        public void SpawnMaidenRainbowLoot(HealthHaver target)
        {
            if (target.healthHaver.IsDead) return;
            int randomTierSelectionNumber = UnityEngine.Random.Range(1, 100);
            if (randomTierSelectionNumber <= 35)
            {
                //Spawn junk or a glass guon stone
                if (UnityEngine.Random.value > .50f)
                {
                    //Junk
                    LootEngine.SpawnItem(PickupObjectDatabase.GetById(127).gameObject, target.specRigidbody.UnitCenter, Vector2.zero, 1f, false, true, false);
                }
                else
                {
                    //Glass Guon
                    LootEngine.SpawnItem(PickupObjectDatabase.GetById(565).gameObject, target.specRigidbody.UnitCenter, Vector2.zero, 1f, false, true, false);
                }
            }
            else if (randomTierSelectionNumber <= 67)
            {
                //Spawn a Half Heart
                LootEngine.SpawnHealth(target.specRigidbody.UnitCenter, 1, null);
            }
            else if (randomTierSelectionNumber <= 87)
            {
                //Spawn a piece of Armour
                LootEngine.SpawnItem(PickupObjectDatabase.GetById(120).gameObject, target.specRigidbody.UnitCenter, Vector2.zero, 1f, false, true, false);
            }
            else if (randomTierSelectionNumber <= 96)
            {
                //Spawn a full heart
                LootEngine.SpawnHealth(target.specRigidbody.UnitCenter, 2, null);
            }
            else if (randomTierSelectionNumber <= 100)
            {
                //spawn 
                LootEngine.SpawnBowlerNote(GameManager.Instance.RewardManager.BowlerNoteBoss, target.specRigidbody.UnitCenter, target.aiActor.ParentRoom, true);
            }
        }

        public void InstaKill(HealthHaver target)
        {
            try
            {
                //ETGModConsole.Log("This poor bastard is gonna die");
                target.ApplyDamage(1E+07f, Vector2.zero, "Erasure", CoreDamageTypes.None, DamageCategory.Unstoppable, true, null, false);
            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
            }
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject debrisObject = base.Drop(player);
            player.PostProcessProjectile -= this.PostProcessProjectile;
            player.PostProcessBeam -= this.PostProcessBeam;
            return debrisObject;
        }
        public override void OnDestroy()
        {
            if (Owner)
            {
                Owner.PostProcessProjectile -= this.PostProcessProjectile;
                Owner.PostProcessBeam -= this.PostProcessBeam;
            }
            base.OnDestroy();
        }

    }
}
