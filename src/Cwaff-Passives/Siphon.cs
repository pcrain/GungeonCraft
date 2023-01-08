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
        public static string longDescription  = "(Immunity to all negative goops; status effect applied to all shots based on goop)";

        private PlayerController owner;
        private bool doAnyEffect = false;
        private bool doElectrify = false;
        private bool doFreeze    = false;
        private bool doFire      = false; //immunity: omitb mr fahrenheit
        private bool doGreenFire = false; //immunity: omitb mr fahrenheit
        private bool doPoison    = false; //immunity: omitb chemical burn
        private bool doLockdown  = false;
        private bool doCharm     = false;
        private bool doCheese    = false;
        private bool doInstakill = false;

        private GoopStatus curGoopType  = GoopStatus.None;
        private GoopStatus lastGoopType = GoopStatus.None;

        private enum GoopStatus
        {
            None,       //No Goop
            Electrify,  //Water Goop
            Freeze,     //Ice Goop
            Fire,       //Fire Goop
            GreenFire,  //Oil Goop
            Poison,     //Poison Goop
            Lockdown,   //Web Goop
            Charm,      //Charm Goop
            Instakill,  //Cheese / Curse Goop
        }

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

        private void PostProcessProjectile(Projectile sourceProjectile, float effectChanceScalar)
        {
             if (!this.doAnyEffect)
                return;
             if (this.doElectrify)
             {
                ETGModConsole.Log("would electrify");
             }
             if (this.doFreeze)
             {
                ETGModConsole.Log("would freeze");
             }
             if (this.doFire)
             {
                ETGModConsole.Log("would fire");
             }
             if (this.doGreenFire)
             {
                ETGModConsole.Log("would green fire");
             }
             if (this.doPoison)
             {
                ETGModConsole.Log("would poison");
             }
             if (this.doLockdown)
             {
                ETGModConsole.Log("would lockdown");
             }
             if (this.doCharm)
             {
                ETGModConsole.Log("would charm");
             }
             if (this.doCheese)
             {
                ETGModConsole.Log("would cheese");
             }
             if (this.doInstakill)
             {
                ETGModConsole.Log("would instakill");
             }
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

            lastGoopType = curGoopType;
            curGoopType = GoopStatus.None;

            // List<DeadlyDeadlyGoopManager> roomGoops = this.owner.GetAbsoluteParentRoom().RoomGoops;
            // if (roomGoops == null)
            //     return;

            // DeadlyDeadlyGoopManager currentGoopManager = null;
            // for (int i = 0; i < roomGoops.Count; i++)
            // {
            //     if (roomGoops[i].IsPositionInGoop(this.owner.specRigidbody.UnitCenter))
            //     {
            //         currentGoopManager = roomGoops[i];

            //         // IntVector2 key = (this.owner.specRigidbody.UnitCenter / GOOP_GRID_SIZE).ToIntVector2(VectorConversions.Floor);
            //         // GoopPositionData gpos = null;
            //         // if (m_goopedCells.TryGetValue(key, out gpos) && gpos.remainingLifespan > goopDefinition.fadePeriod)
            //         // {

            //         // }
            //         // else {
            //         //     gpos = null;
            //         // }

            //         break;
            //     }
            // }
            // if (currentGoopManager == null)
            //     return;

            // GoopDefinition currentGoopDef = currentGoopManager.goopDefinition;
            // // GoopPositionData g;
            // if (currentGoopDef.AppliesCheese || currentGoopDef.CheeseModifierEffect != null)
            //     curGoopType = GoopStatus.Instakill;
            // else if (currentGoopDef.AppliesSpeedModifier || currentGoopDef.AppliesSpeedModifierContinuously)
            //     curGoopType = GoopStatus.Lockdown;
            // else if (currentGoopDef.UsesGreenFire)
            //     curGoopType = GoopStatus.GreenFire;
            // else if (currentGoopDef.CanBeIgnited || currentGoopDef.SelfIgnites || currentGoopDef.fireEffect != null)
            //     curGoopType = GoopStatus.Fire;
            // else if (currentGoopDef.CanBeFrozen)
            //     curGoopType = GoopStatus.Freeze;
            // else if (currentGoopDef.AppliesDamageOverTime || currentGoopDef.damagesPlayers)
            //     curGoopType = GoopStatus.Poison;
            // else if (currentGoopDef.AppliesCharm)
            //     curGoopType = GoopStatus.Charm;

            this.doAnyEffect = false;

            RoomHandler absoluteRoomFromPosition = this.owner.GetAbsoluteParentRoom();
            List<DeadlyDeadlyGoopManager> roomGoops = absoluteRoomFromPosition.RoomGoops;
            if (roomGoops == null)
                return;

            DeadlyDeadlyGoopManager currentGoopManager = null;
            for (int i = 0; i < roomGoops.Count; i++)
            {
                bool isOverGoop = roomGoops[i].IsPositionInGoop(this.owner.specRigidbody.UnitCenter);
                if (isOverGoop)
                {
                    currentGoopManager = roomGoops[i];
                    break;
                }
            }
            if (currentGoopManager == null)
                return;

            GoopDefinition currentGoopDef = currentGoopManager.goopDefinition;
            CellVisualData.CellFloorType cellFloorType = GameManager.Instance.Dungeon.GetFloorTypeFromPosition(
                this.owner.specRigidbody.UnitBottomCenter);

            this.doAnyEffect = true;
            this.doCharm     = (currentGoopDef.AppliesCharm && currentGoopDef.CharmModifierEffect != null);
            this.doCheese    = (currentGoopDef.AppliesCheese && currentGoopDef.CheeseModifierEffect != null);
            this.doPoison    = currentGoopDef.damagesEnemies || (currentGoopDef.AppliesDamageOverTime && currentGoopDef.HealthModifierEffect != null);
            this.doLockdown  = ((currentGoopDef.AppliesSpeedModifier || currentGoopDef.AppliesSpeedModifierContinuously) && currentGoopDef.SpeedModifierEffect != null);
            this.doFire      = (currentGoopManager.IsPositionOnFire(this.owner.specRigidbody.UnitCenter));
            this.doGreenFire = this.doFire && currentGoopDef.UsesGreenFire;
            this.doFreeze    = (cellFloorType == CellVisualData.CellFloorType.Ice || currentGoopManager.IsPositionFrozen(this.owner.specRigidbody.UnitCenter));
            this.doInstakill = currentGoopDef.DrainsAmmo || (this.owner.CurrentCurseMeterValue > 0f && this.owner.CurseIsDecaying); // standing in ammo drain goop or curse goop

            // lazy immunity to most goop effects
            this.owner.CurrentFireMeterValue   = Mathf.Min(0.01f,this.owner.CurrentFireMeterValue);
            this.owner.CurrentPoisonMeterValue = Mathf.Min(0.01f,this.owner.CurrentPoisonMeterValue);
            this.owner.CurrentDrainMeterValue  = Mathf.Min(0.01f,this.owner.CurrentDrainMeterValue);
            this.owner.CurrentCurseMeterValue  = Mathf.Min(0.01f,this.owner.CurrentCurseMeterValue);
        }
    }
}

