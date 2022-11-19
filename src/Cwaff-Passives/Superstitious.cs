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

        public static void Init()
        {
            PickupObject item = Lazy.SetupItem<Superstitious>(passiveName, spritePath, shortDescription, longDescription, "cg");
            item.quality      = PickupObject.ItemQuality.C;

            els.Add(new HUDElement("Coolness","","CwaffingTheGungy/Resources/HUD/Coolness.png"));
            els.Add(new HUDElement("Curse","","CwaffingTheGungy/Resources/HUD/Curse.png"));
            els.Add(new HUDElement("Basic2","basic text",null));
        }

        public override void Pickup(PlayerController player)
        {
            this.owner = player;
            foreach (HUDElement el in els)
            {
                el.updater = HUDUpdater;
                el.Activate();
            }
            base.Pickup(player);
        }

        public override DebrisObject Drop(PlayerController player)
        {
            this.owner = null;
            foreach (HUDElement el in els)
            {
                el.updater = null;
                el.Activate();
            }
            return base.Drop(player);
        }

        private bool HUDUpdater(HUDElement self)
        {
            if (this.owner != null)
                self.text.Text = this.owner.carriedConsumables.Currency.ToString();
            else
                self.text.Text = BraveTime.DeltaTime.ToString();
            return true;
        }
    }
}

