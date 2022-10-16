using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using ItemAPI;
using SaveAPI;

namespace NevernamedsItems
{
    class ChanceKinEffigy : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Chance Effigy";
            string resourceName = "NevernamedsItems/Resources/chanceeffigy_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<ChanceKinEffigy>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Guns Upon A Time";
            string longDesc = "Chance Kin drop bonus supplies." + "\n\nHailing from the same ludicrous sect who forged the Keybullet Effigy. Their religious rites, while inclusive of Chance Kin, rarely focus on them as they are perceived as lesser spirits.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.D;
            ChanceEffigyID = item.PickupObjectId;
            item.SetupUnlockOnCustomFlag(CustomDungeonFlags.KILLEDJAMMEDCHANCEKIN, true);
        }
        public static int ChanceEffigyID;
        private void OnKilledEnemy(PlayerController player, HealthHaver enemy)
        {
            if (Owner && enemy.aiActor && (enemy.aiActor.EnemyGuid == "699cd24270af4cd183d671090d8323a1" || enemy.aiActor.EnemyGuid == "a446c626b56d4166915a4e29869737fd"))
            {
                if (Owner.PlayerHasActiveSynergy("Luck of the Quickdraw"))
                {
                    StatModifier dmgup = new StatModifier()
                    {
                        amount = 1.1f,
                        statToBoost = PlayerStats.StatType.Damage,
                        modifyType = StatModifier.ModifyMethod.MULTIPLICATIVE
                    };
                    Owner.ownerlessStatModifiers.Add(dmgup);
                    Owner.stats.RecalculateStats(Owner, false, false);
                }
            }
        }
        private void OnPreSpawn(AIActor actor)
        {
            if (Owner && actor && (actor.EnemyGuid == "699cd24270af4cd183d671090d8323a1" || actor.EnemyGuid == "a446c626b56d4166915a4e29869737fd"))
            {
                actor.AdditionalSafeItemDrops.Add(PickupObjectDatabase.GetById(BraveUtility.RandomElement(lootIDlist)));
            }
        }
        private void OnEnteredCombat()
        {
            if (Owner && UnityEngine.Random.value <= 0.015f)
            {
                var KeyBulletKin = EnemyDatabase.GetOrLoadByGuid("a446c626b56d4166915a4e29869737fd");
                IntVector2? spawnPos = Owner.CurrentRoom.GetRandomVisibleClearSpot(1, 1);
                if (spawnPos != null)
                {
                    AIActor TargetActor = AIActor.Spawn(KeyBulletKin.aiActor, spawnPos.Value, GameManager.Instance.Dungeon.data.GetAbsoluteRoomFromPosition(spawnPos.Value), true, AIActor.AwakenAnimationType.Default, true);
                    PhysicsEngine.Instance.RegisterOverlappingGhostCollisionExceptions(TargetActor.specRigidbody, null, false);
                    TargetActor.HandleReinforcementFallIntoRoom(0f);
                }
            }
        }
        public static List<int> lootIDlist = new List<int>()
        {
            78, //Ammo
            600, //Spread Ammo
            565, //Glass Guon Stone
            73, //Half Heart
            85, //Heart
            120, //Armor
        };

        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            ETGMod.AIActor.OnPreStart += this.OnPreSpawn;
            player.OnKilledEnemyContext += this.OnKilledEnemy;
            player.OnEnteredCombat += this.OnEnteredCombat;
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject debrisObject = base.Drop(player);
            player.OnKilledEnemyContext -= this.OnKilledEnemy;
            player.OnEnteredCombat -= this.OnEnteredCombat;
            ETGMod.AIActor.OnPreStart -= this.OnPreSpawn;
            return debrisObject;
        }
        public override void OnDestroy()
        {
            if (Owner)
            {
                Owner.OnKilledEnemyContext -= this.OnKilledEnemy;
                Owner.OnEnteredCombat -= this.OnEnteredCombat;
            }
            ETGMod.AIActor.OnPreStart -= this.OnPreSpawn;
            base.OnDestroy();
        }
    }
}
