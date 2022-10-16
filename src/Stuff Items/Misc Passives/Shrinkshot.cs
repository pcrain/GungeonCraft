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
    public class Shrinkshot : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Shrinkshot";
            string resourceName = "NevernamedsItems/Resources/shrinkshot_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<Shrinkshot>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "A Person's A Person";
            string longDesc = "Chance to shrink enemies, allowing them to be stomped on."+"\n\nA portal accident.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.B;
            item.SetTag("bullet_modifier");
            //Unlock
            item.SetupUnlockOnCustomFlag(CustomDungeonFlags.PURCHASED_SHRINKSHOT, true);
            item.AddItemToDougMetaShop(40);

        }

        private void PostProcessProjectile(Projectile sourceProjectile, float effectChanceScalar)
        {

            float procChance = 0.09f;
            procChance *= effectChanceScalar;
            bool DoFirstShotOverrideSynergy = (Owner.CurrentGun.LastShotIndex == 0) && Owner.PlayerHasActiveSynergy("Added Effect - Shrink");
            bool DoLastShotOverrideSynergy = (Owner.CurrentGun.LastShotIndex == Owner.CurrentGun.ClipCapacity - 1) && Owner.PlayerHasActiveSynergy("Topaz Weapon");
            try
            {
                if (UnityEngine.Random.value <= procChance || DoFirstShotOverrideSynergy || DoLastShotOverrideSynergy)
                {
                    sourceProjectile.RuntimeUpdateScale(0.7f);
                    sourceProjectile.AdjustPlayerProjectileTint(ExtendedColours.vibrantOrange, 2);
                    sourceProjectile.statusEffectsToApply.Add(StatusEffectHelper.GenerateSizeEffect(10, new Vector2(0.4f, 0.4f)));
                }
            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
            }
        }
        private void PostProcessBeam(BeamController beam, SpeculativeRigidbody hitRigidBody, float tickrate)
        {
            float procChance = 0.09f;
            beam.AdjustPlayerBeamTint(ExtendedColours.vibrantOrange, 1, 0f);
            GameActor gameActor = hitRigidBody.gameActor;
            if (!gameActor)
            {
                return;
            }
            if (UnityEngine.Random.value < BraveMathCollege.SliceProbability(procChance, tickrate))
            {
                hitRigidBody.gameActor.ApplyEffect(StatusEffectHelper.GenerateSizeEffect(10, new Vector2(0.4f, 0.4f)));
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
