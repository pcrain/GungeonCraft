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

        private static HUDElement he, he2, he3;

        public static void Init()
        {
            PickupObject item = Lazy.SetupItem<Superstitious>(passiveName, spritePath, shortDescription, longDescription, "cg");
            item.quality      = PickupObject.ItemQuality.C;

            he = new SuperstitiousHUDElement("Coolness","","CwaffingTheGungy/Resources/HUD/Coolness.png");
            he2 = new SuperstitiousHUDElement("Curse","","CwaffingTheGungy/Resources/HUD/Curse.png");
            he3 = new HUDElement("Basic2","basic text",null);
        }

        public override void Pickup(PlayerController player)
        {
            he.Activate();
            he2.Activate();
            he3.Activate();
            base.Pickup(player);
        }

        public override DebrisObject Drop(PlayerController player)
        {
            he.Deactivate();
            he2.Deactivate();
            he3.Deactivate();
            return base.Drop(player);
        }
    }

    public class SuperstitiousHUDElement : HUDElement
    {
        public SuperstitiousHUDElement(string initText, string initIconPath, string initLabel)
            : base(initText, initIconPath, initLabel) {}

        public override void Update()
        {
            text.Text = BraveTime.DeltaTime.ToString();
            base.Update();
        }
    }
}

