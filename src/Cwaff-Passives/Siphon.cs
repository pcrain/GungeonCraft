using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Reflection;

using UnityEngine;

using ItemAPI;

namespace CwaffingTheGungy
{
    public class Siphon : PassiveItem
    {
        public static string PassiveName      = "Siphon";
        public static string SpritePath       = "CwaffingTheGungy/Resources/ItemSprites/88888888_icon";
        public static string ShortDescription = "Super Gooper";
        public static string LongDescription  = "Immunity to all negative goops; projectiles fired while standing in goops spread during flight";

        // private enum GoopStatus
        // {
        //     None,       //No Goop
        //     Electrify,  //Water Goop
        //     Freeze,     //Ice Goop
        //     Fire,       //Fire Goop
        //     GreenFire,  //Oil Goop
        //     Poison,     //Poison Goop
        //     Lockdown,   //Web Goop
        //     Charm,      //Charm Goop
        //     Instakill,  //Cheese / Curse Goop
        // }

        public static void Init()
        {
            PickupObject item = Lazy.SetupItem<Siphon>(PassiveName, SpritePath, ShortDescription, LongDescription, "cg");
            item.quality      = PickupObject.ItemQuality.B;
        }

        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.PostProcessProjectile += this.PostProcessProjectile;
        }

        private void PostProcessProjectile(Projectile proj, float effectChanceScalar)
        {
            if (this.Owner == null)
                return;

            List<DeadlyDeadlyGoopManager> roomGoops = this.Owner.GetAbsoluteParentRoom().RoomGoops;
            if (roomGoops == null)
                return;

            DeadlyDeadlyGoopManager currentGoopManager = null;
            Vector2 pos = this.Owner.specRigidbody.UnitCenter;
            for (int i = 0; i < roomGoops.Count; i++)
            {
                if (!roomGoops[i].IsPositionInGoop(pos))
                    continue;
                currentGoopManager = roomGoops[i];
                break;
            }
            if (currentGoopManager == null)
                return;

            GoopModifier goopmod = proj.gameObject.GetOrAddComponent<GoopModifier>();
                goopmod.goopDefinition         = currentGoopManager.goopDefinition;
                goopmod.SpawnGoopOnCollision   = true;
                goopmod.CollisionSpawnRadius   = 1f;
                goopmod.SpawnGoopInFlight      = true;
                goopmod.InFlightSpawnRadius    = 0.4f;
                goopmod.InFlightSpawnFrequency = 0.01f;
        }

        public override DebrisObject Drop(PlayerController player)
        {
            player.PostProcessProjectile -= this.PostProcessProjectile;
            return base.Drop(player);
        }

        public override void Update()
        {
            base.Update();

            if (!this.Owner)
                return;

            // lazy pseudo-immunity to most goop effects
            this.Owner.CurrentFireMeterValue   = Mathf.Min(0.01f,this.Owner.CurrentFireMeterValue);
            this.Owner.CurrentPoisonMeterValue = Mathf.Min(0.01f,this.Owner.CurrentPoisonMeterValue);
            this.Owner.CurrentDrainMeterValue  = Mathf.Min(0.01f,this.Owner.CurrentDrainMeterValue);
            this.Owner.CurrentCurseMeterValue  = Mathf.Min(0.01f,this.Owner.CurrentCurseMeterValue);
        }
    }
}
