using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Alexandria.ItemAPI;

namespace NevernamedsItems
{
    public class Kevin : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Kevin";
            string resourceName = "NevernamedsItems/Resources/kevin_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<Kevin>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Long Live the Keving";
            string longDesc = "What may appear at first to be the skinned face of a Bullet Kin is in fact the flag of the Sovereign Nation of Kevin."+"\n\nIn your time of need, the Sovereign Nation of Kevin's sole resident (Kevin) will join you in battle. No matter how many times he gets knocked down, he'll just keep getting back up.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.D;
            item.AddToSubShop(ItemBuilder.ShopType.Cursula);

            List<string> mandatorySynergyItems = new List<string>() { "nn:kevin", "eyepatch" };
            CustomSynergies.Add("High Lord Kevin", mandatorySynergyItems);
            item.SetTag("non_companion_living_item");
            KevinID = item.PickupObjectId;
        }
        public static int KevinID;

        public GameActorCharmEffect charmEffect;
        private void SpawnKevin()
        {
            string kevinGuid;
            if (Owner.HasPickupID(118)) kevinGuid = "70216cae6c1346309d86d4a0b4603045";
            else kevinGuid = "01972dee89fc4404a5c408d50007dad5";
            PlayerController player = this.Owner;
            var Kevin = EnemyDatabase.GetOrLoadByGuid(kevinGuid);
            IntVector2? bestRewardLocation = Owner.CurrentRoom.GetRandomVisibleClearSpot(2, 2);
            AIActor TargetActor = AIActor.Spawn(Kevin.aiActor, bestRewardLocation.Value, GameManager.Instance.Dungeon.data.GetAbsoluteRoomFromPosition(bestRewardLocation.Value), true, AIActor.AwakenAnimationType.Default, true);
            //TargetActor.gameActor.ApplyEffect(this.charmEffect, 100f, null);
            TargetActor.CanTargetEnemies = true;
            TargetActor.CanTargetPlayers = false;
            PhysicsEngine.Instance.RegisterOverlappingGhostCollisionExceptions(TargetActor.specRigidbody, null, false);
            TargetActor.gameObject.AddComponent<KillOnRoomClear>();
            TargetActor.IsHarmlessEnemy = true;
            //TargetActor.healthHaver.bossHealthBar = HealthHaver.BossBarType.MainBar;
            TargetActor.IgnoreForRoomClear = true;
            TargetActor.HandleReinforcementFallIntoRoom(0f);
            TargetActor.gameObject.AddComponent<AIActorIsKevin>();
            //PRAISE BE OUR LORD AND SAVIOUR KEVIN. YOU WILL BOW TO HIM, OR YOU WILL DIE.
        }

        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.OnEnteredCombat += this.SpawnKevin;
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject result = base.Drop(player);
            player.OnEnteredCombat -= this.SpawnKevin;
            return result;
        }
        public override void OnDestroy()
        {
            if (Owner) Owner.OnEnteredCombat -= this.SpawnKevin;
            base.OnDestroy();
        }
    }
    class AIActorIsKevin : MonoBehaviour { }
}
