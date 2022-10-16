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
    public class PestiferousLead : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Pestiferous Lead";
            string resourceName = "NevernamedsItems/Resources/pestiferouslead_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<PestiferousLead>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "The Fester Pours";
            string longDesc = "These shells are loaded with a potent viral slurry, capable of quickly spreading through an enemy legion."+"\n\nFar removed from the ancient days of plague warfare, which typically involved corpses and catapults.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.A;
            item.SetTag("bullet_modifier");
            //Unlock
            item.SetupUnlockOnCustomFlag(CustomDungeonFlags.PURCHASED_PESTIFEROUSLEAD, true);
            item.AddItemToDougMetaShop(30);

        }

        private void PostProcessProjectile(Projectile sourceProjectile, float effectChanceScalar)
        {

            float procChance = 0.12f;
            procChance *= effectChanceScalar;
            bool DoFirstShotOverrideSynergy = (Owner.CurrentGun.LastShotIndex == 0) && Owner.PlayerHasActiveSynergy("Added Effect - Plague");
            bool DoLastShotOverrideSynergy = (Owner.CurrentGun.LastShotIndex == Owner.CurrentGun.ClipCapacity - 1) && Owner.PlayerHasActiveSynergy("Amethyst Weapon");
            try
            {
                if (UnityEngine.Random.value <= procChance || DoFirstShotOverrideSynergy || DoLastShotOverrideSynergy)
                {
                    sourceProjectile.AdjustPlayerProjectileTint(ExtendedColours.plaguePurple, 2);
                    sourceProjectile.statusEffectsToApply.Add(StaticStatusEffects.StandardPlagueEffect);
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
            beam.AdjustPlayerBeamTint(ExtendedColours.plaguePurple, 1, 0f);
            GameActor gameActor = hitRigidBody.gameActor;
            if (!gameActor)
            {
                return;
            }
            if (UnityEngine.Random.value < BraveMathCollege.SliceProbability(procChance, tickrate))
            {
                hitRigidBody.gameActor.ApplyEffect(StaticStatusEffects.StandardPlagueEffect);
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
