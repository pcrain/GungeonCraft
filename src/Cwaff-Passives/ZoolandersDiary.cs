using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using UnityEngine;
using MonoMod.RuntimeDetour;

using Gungeon;
using Dungeonator;
using Alexandria.ItemAPI;
using Alexandria.Misc;

namespace CwaffingTheGungy
{
    public class ZoolandersDiary : PassiveItem
    {
        public static string PassiveName      = "Zoolander's Diary";
        public static string SpritePath       = "CwaffingTheGungy/Resources/ItemSprites/zoolander_icon";
        public static string ShortDescription = "Ambiturner No More";
        public static string LongDescription  = "(3x damage when shooting right; 1/3 damage when aiming left)";

        public static void Init()
        {
            PickupObject item = Lazy.SetupPassive<ZoolandersDiary>(PassiveName, SpritePath, ShortDescription, LongDescription);
            item.quality      = PickupObject.ItemQuality.C;
        }

        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.PostProcessProjectile += this.PostProcessProjectile;
        }

        public override DebrisObject Drop(PlayerController player)
        {
            player.PostProcessProjectile -= this.PostProcessProjectile;
            return base.Drop(player);
        }

        private void PostProcessProjectile(Projectile proj, float effectChanceScalar)
        {
            if (this.Owner == null)
                return;

            float gunAngle = BraveMathCollege.ClampAngle180(this.Owner.m_currentGunAngle);
            if (Math.Abs(gunAngle) < 45)
                proj.baseData.damage *= 3;
            else if (Math.Abs(gunAngle) > 135)
                proj.baseData.damage /= 3;
        }
    }
}
