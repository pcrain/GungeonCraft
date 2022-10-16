using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Alexandria.ItemAPI;
using UnityEngine;
using MonoMod.RuntimeDetour;
using System.Reflection;
using SaveAPI;

namespace NevernamedsItems
{
    public class LockdownBullets : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Lockdown Bullets";
            string resourceName = "NevernamedsItems/Resources/lockdownbullets_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<LockdownBullets>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Clapped In Irons";
            string longDesc = "Chance to lock enemies in place."+"\n\nComissioned by an elderly Gungeoneer whose reaction times weren't enough to keep up with all those fast young whippersnapper bullet kin.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.C;
            item.SetTag("bullet_modifier");
            item.SetupUnlockOnCustomFlag(CustomDungeonFlags.PURCHASED_LOCKDOWNBULLETS, true);
            item.AddItemToDougMetaShop(20);
        }

        private void PostProcessProjectile(Projectile sourceProjectile, float effectChanceScalar)
        {
            
            float procChance = 0.12f;
            //ETGModConsole.Log("Scaler: " + effectChanceScalar);
            procChance *= effectChanceScalar;
            bool DoFirstShotOverrideSynergy = (Owner.CurrentGun.LastShotIndex == 0) && Owner.PlayerHasActiveSynergy("Added Effect - Lockdown");
            bool DoLastShotOverrideSynergy = (Owner.CurrentGun.LastShotIndex == Owner.CurrentGun.ClipCapacity - 1) && Owner.PlayerHasActiveSynergy("Moonstone Weapon");
            //ETGModConsole.Log("PostScaleChance: " + procChance);
            try
            {
                if (UnityEngine.Random.value <= procChance || DoFirstShotOverrideSynergy || DoLastShotOverrideSynergy)
                {
                    ApplyLockdownBulletBehaviour orAddComponent = sourceProjectile.gameObject.GetOrAddComponent<ApplyLockdownBulletBehaviour>();
                    orAddComponent.duration = 4f;
                    orAddComponent.useSpecialBulletTint = true;
                    orAddComponent.bulletTintColour = Color.grey;
                    orAddComponent.TintEnemy = true;
                    orAddComponent.procChance = 1;
                }
            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
            }
        }
        private void PostProcessBeam(BeamController beam, SpeculativeRigidbody hitRigidBody, float tickrate)
        {
            float procChance = 0.12f;
            GameActor gameActor = hitRigidBody.gameActor;
            if (!gameActor)
            {
                return;
            }
            if (UnityEngine.Random.value < BraveMathCollege.SliceProbability(procChance, tickrate))
            {
                ApplyLockdown.ApplyDirectLockdown(hitRigidBody.gameActor, 4, Color.grey, Color.grey, EffectResistanceType.None, "Lockdown", true, false);
            }
        }

        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject debrisObject = base.Drop(player);
            player.PostProcessProjectile -= this.PostProcessProjectile;
            player.PostProcessBeamTick -= this.PostProcessBeam;


            return debrisObject;
        }
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.PostProcessProjectile += this.PostProcessProjectile;
            player.PostProcessBeamTick += this.PostProcessBeam;
        }
        public override void OnDestroy()
        {
            if (Owner)
            {
                Owner.PostProcessBeamTick -= this.PostProcessBeam;
                Owner.PostProcessProjectile -= this.PostProcessProjectile;
            }
            base.OnDestroy();
        }
    }
}
