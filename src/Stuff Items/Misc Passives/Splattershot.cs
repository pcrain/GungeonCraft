using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Alexandria.ItemAPI;
using UnityEngine;
using MonoMod.RuntimeDetour;
using System.Reflection;
using Alexandria.Misc;

namespace NevernamedsItems
{
    public class Splattershot : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Splattershot";
            string resourceName = "NevernamedsItems/Resources/splattershot_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<Splattershot>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "SPLAT";
            string longDesc = "Bullets spend the beginning of their lives clustered close together, before splitting apart in a hearbreaking parable about the pain of growing up.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.B;
            item.SetTag("bullet_modifier");
        }

        private void PostProcessProjectile(Projectile sourceProjectile, float effectChanceScalar)
        {
            ProjectileSplitController splitCont = sourceProjectile.gameObject.GetOrAddComponent<ProjectileSplitController>();
            splitCont.distanceBasedSplit = true;
            splitCont.amtToSplitTo += 3;
            if (sourceProjectile.ProjectilePlayerOwner() && sourceProjectile.ProjectilePlayerOwner().PlayerHasActiveSynergy("3 + 1 = 4"))
            {
                splitCont.amtToSplitTo += 1;
            }
        }
        private void PostProcessBeam(BeamController beam)
        {
            if (beam && beam.gameObject && beam.projectile)
            {
                BeamSplittingModifier splitMod = beam.gameObject.GetOrAddComponent<BeamSplittingModifier>();
                splitMod.amtToSplitTo += 3;
                if (beam.projectile.ProjectilePlayerOwner() && beam.projectile.ProjectilePlayerOwner().PlayerHasActiveSynergy("3 + 1 = 4"))
                {
                    splitMod.amtToSplitTo += 1;
                }
            }
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject debrisObject = base.Drop(player);
            player.PostProcessProjectile -= this.PostProcessProjectile;
            player.PostProcessBeam -= this.PostProcessBeam;
            return debrisObject;
        }
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.PostProcessProjectile += this.PostProcessProjectile;
            player.PostProcessBeam += this.PostProcessBeam;

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