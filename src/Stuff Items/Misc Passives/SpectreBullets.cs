using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using UnityEngine;
using Alexandria.ItemAPI;

namespace NevernamedsItems
{
    public class SpectreBullets : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Spectre Bullets";
            string resourceName = "NevernamedsItems/Resources/spectrebullets_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<SpectreBullets>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "SpoOOOooOOoky!";
            string longDesc = "These terrifying rounds are modelled after a spirit that shouldn’t even exist.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.B;
            item.SetTag("bullet_modifier");
        }
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.PostProcessProjectile += this.PostProcessProjectile;
        }
        private void PostProcessProjectile(Projectile sourceProjectile, float effectChanceScalar)
        {
            float activationChance = 0.4f;
            float num = activationChance * effectChanceScalar;
            if (UnityEngine.Random.value < num)
            {
                sourceProjectile.OnHitEnemy = (Action<Projectile, SpeculativeRigidbody, bool>)Delegate.Combine(sourceProjectile.OnHitEnemy, new Action<Projectile, SpeculativeRigidbody, bool>(this.AddFearEffect));
            }
        }
        private void AddFearEffect(Projectile arg1, SpeculativeRigidbody arg2, bool arg3)
        {
            if (arg2 != null && arg2.aiActor != null && Owner != null)
            {
                if (arg2 != null && arg2.healthHaver.IsAlive)
                {
                    if (arg2.aiActor.EnemyGuid != "465da2bb086a4a88a803f79fe3a27677" && arg2.aiActor.EnemyGuid != "05b8afe0b6cc4fffa9dc6036fa24c8ec")
                    {
                        StartCoroutine(HandleFear(Owner, arg2));
                    }
                }
            }
        }        
        private IEnumerator HandleFear(PlayerController user, SpeculativeRigidbody enemy)
        {
            if (this.fleeData == null || this.fleeData.Player != Owner)
            {
                this.fleeData = new FleePlayerData();
                this.fleeData.Player = Owner;
                this.fleeData.StartDistance *= 2f;
            }
            if (enemy.aiActor.behaviorSpeculator != null)
            {
                enemy.aiActor.behaviorSpeculator.FleePlayerData = this.fleeData;
                FleePlayerData fleePlayerData = new FleePlayerData();
                yield return new WaitForSeconds(7f);
                enemy.aiActor.behaviorSpeculator.FleePlayerData.Player = null;
            }
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject debrisObject = base.Drop(player);
            player.PostProcessProjectile -= this.PostProcessProjectile;
            return debrisObject;
        }
        public override void OnDestroy()
        {
            if (Owner) Owner.PostProcessProjectile -= this.PostProcessProjectile;
            base.OnDestroy();
        }
        private FleePlayerData fleeData;
        //private float duration = 7f;
    }
}
