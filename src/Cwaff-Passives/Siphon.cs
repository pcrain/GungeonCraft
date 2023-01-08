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
    public class Siphon : PassiveItem
    {
        public static string passiveName      = "Siphon";
        public static string spritePath       = "CwaffingTheGungy/Resources/ItemSprites/88888888_icon";
        public static string shortDescription = "Super Gooper";
        public static string longDescription  = "Immunity to all negative goops; projectiles fired while standing in goops spread during flight";

        private PlayerController owner;

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
            PickupObject item = Lazy.SetupItem<Siphon>(passiveName, spritePath, shortDescription, longDescription, "cg");
            item.quality      = PickupObject.ItemQuality.B;
        }

        public override void Pickup(PlayerController player)
        {
            this.owner = player;
            player.PostProcessProjectile += this.PostProcessProjectile;
            base.Pickup(player);
        }

        private void PostProcessProjectile(Projectile proj, float effectChanceScalar)
        {
            if (this.owner == null)
                return;

            List<DeadlyDeadlyGoopManager> roomGoops = this.owner.GetAbsoluteParentRoom().RoomGoops;
            if (roomGoops == null)
                return;

            DeadlyDeadlyGoopManager currentGoopManager = null;
            Vector2 pos = this.owner.specRigidbody.UnitCenter;
            for (int i = 0; i < roomGoops.Count; i++)
            {
                if (roomGoops[i].IsPositionInGoop(pos))
                {
                    currentGoopManager = roomGoops[i];
                    break;
                }
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
            this.owner = null;
            player.PostProcessProjectile -= this.PostProcessProjectile;
            return base.Drop(player);
        }

        public override void Update()
        {
            base.Update();

            if (!this.owner)
                return;

            // lazy pseudo-immunity to most goop effects
            this.owner.CurrentFireMeterValue   = Mathf.Min(0.01f,this.owner.CurrentFireMeterValue);
            this.owner.CurrentPoisonMeterValue = Mathf.Min(0.01f,this.owner.CurrentPoisonMeterValue);
            this.owner.CurrentDrainMeterValue  = Mathf.Min(0.01f,this.owner.CurrentDrainMeterValue);
            this.owner.CurrentCurseMeterValue  = Mathf.Min(0.01f,this.owner.CurrentCurseMeterValue);
        }
    }
}
