using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Reflection;

using UnityEngine;
using MonoMod.RuntimeDetour;

using ItemAPI;
using Gungeon;
using Dungeonator;

namespace CwaffingTheGungy
{
    public class ZoolandersDiary : PassiveItem
    {
        public static string passiveName      = "Zoolander's Diary";
        public static string spritePath       = "CwaffingTheGungy/Resources/ItemSprites/zoolander_icon";
        public static string shortDescription = "Ambiturner No More";
        public static string longDescription  = "(3x damage when shooting right; 1/3 damage when aiming left)";

        private PlayerController owner;

        public static void Init()
        {
            PickupObject item = Lazy.SetupItem<ZoolandersDiary>(passiveName, spritePath, shortDescription, longDescription, "cg");
            item.quality      = PickupObject.ItemQuality.C;
        }

        public override void Pickup(PlayerController player)
        {
            this.owner = player;
            player.PostProcessProjectile += this.PostProcessProjectile;
            base.Pickup(player);
        }

        public override DebrisObject Drop(PlayerController player)
        {
            this.owner = null;
            player.PostProcessProjectile -= this.PostProcessProjectile;
            return base.Drop(player);
        }

        private void PostProcessProjectile(Projectile proj, float effectChanceScalar)
        {
            if (this.owner == null)
                return;

            float gunAngle = BraveMathCollege.ClampAngle180(this.owner.m_currentGunAngle);
            if (Math.Abs(gunAngle) < 45)
                proj.baseData.damage *= 3;
            else if (Math.Abs(gunAngle) > 135)
                proj.baseData.damage /= 3;
        }
    }
}
