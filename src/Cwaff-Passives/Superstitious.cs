using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using ItemAPI;
using UnityEngine;
using MonoMod.RuntimeDetour;
using System.Reflection;

using ETGGUI;
using SGUI;

namespace CwaffingTheGungy
{
    public class Superstitious : PassiveItem
    {
        public static string passiveName      = "Superstitious";
        public static string spritePath       = "CwaffingTheGungy/Resources/NeoItemSprites/88888888_icon";
        public static string shortDescription = "Writings on the HUD";
        public static string longDescription  = "(6s and 7s)";

        private static HUDController hud => HUDController.Instance;
        private static List<HUDElement> els = new List<HUDElement>();

        private PlayerController owner;
        private StatModifier superstitionBuff = null;
        private int sixes, sevens;

        public static void Init()
        {
            PickupObject item = Lazy.SetupItem<Superstitious>(passiveName, spritePath, shortDescription, longDescription, "cg");
            item.quality      = PickupObject.ItemQuality.C;

            els.Add(new HUDElement("Coolness","","CwaffingTheGungy/Resources/HUD/Coolness.png"));
            // els.Add(new HUDElement("Curse","","CwaffingTheGungy/Resources/HUD/Curse.png"));
            // els.Add(new HUDElement("Basic2","basic text",null));
        }

        public override void Pickup(PlayerController player)
        {
            this.owner = player;
            foreach (HUDElement el in els)
            {
                el.updater = HUDUpdater;
                // el.updateFreq = 0.5f;
                el.Activate();
            }
            player.OnReloadedGun += this.HandleGunReloaded;
            base.Pickup(player);
        }

        public override DebrisObject Drop(PlayerController player)
        {
            this.owner = null;
            foreach (HUDElement el in els)
            {
                el.updater = null;
                el.Deactivate();
            }
            player.OnReloadedGun -= this.HandleGunReloaded;
            return base.Drop(player);
        }

        private void HandleGunReloaded(PlayerController player, Gun playerGun)
        {
            int statboost = this.sevens - this.sixes;

            if (player.ownerlessStatModifiers.Contains(superstitionBuff))
                player.ownerlessStatModifiers.Remove(superstitionBuff);

            superstitionBuff = new StatModifier();
            superstitionBuff.statToBoost = PlayerStats.StatType.Damage;
            float totalboost = Mathf.Pow(1.5f,Mathf.Abs(statboost));
            if (statboost < 0)
                totalboost = 1.0f/totalboost;
            superstitionBuff.amount = totalboost;
            superstitionBuff.modifyType = StatModifier.ModifyMethod.MULTIPLICATIVE;

            player.ownerlessStatModifiers.Add(superstitionBuff);
            player.stats.RecalculateStats(player);

            ETGModConsole.Log("Reloading with stat boost level "+totalboost);
        }

        public override void Update()
        {
            base.Update();
            string shells = this.owner.carriedConsumables.Currency.ToString();
            string ammo = this.owner.CurrentGun.InfiniteAmmo
                ? ""
                : this.owner.CurrentGun.CurrentAmmo.ToString()+this.owner.CurrentGun.AdjustedMaxAmmo.ToString();
            string keys = this.owner.carriedConsumables.KeyBullets.ToString();
            string blanks = this.owner.Blanks.ToString();
            string numbers = shells+ammo+keys+blanks;
            this.sixes = numbers.Count(x => x == '6');
            this.sevens = numbers.Count(x => x == '7');
        }

        private bool HUDUpdater(HUDElement self)
        {
            if (this.owner != null)
            {
                // self.text.Text = numbers+"->"+sixes.ToString()+","+sevens.ToString();
                self.text.Text = sixes.ToString()+","+sevens.ToString();
            }
            return true;
        }
    }
}
