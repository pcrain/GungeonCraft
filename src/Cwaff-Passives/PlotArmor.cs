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
    public class PlotArmor : PassiveItem
    {
        public static string ItemName         = "Plot Armor";
        public static string SpritePath       = "CwaffingTheGungy/Resources/ItemSprites/plot_armor_icon";
        public static string ShortDescription = "Can't Die Yet";
        public static string LongDescription  = "(Gain enough armor before every boss fight to have 3 total armor, or 1 armor if you already have at least 3.)";

        internal const int _MIN_ARMOR = 3;

        private RoomHandler _lastVisitedRoom = null;

        public static void Init()
        {
            PickupObject item = Lazy.SetupPassive<PlotArmor>(ItemName, SpritePath, ShortDescription, LongDescription);
            item.quality      = PickupObject.ItemQuality.S;
        }

        public override void Update()
        {
            base.Update();
            RoomHandler room = this.Owner.CurrentRoom;
            if (!this.Owner || room == this._lastVisitedRoom)
                return;

            this._lastVisitedRoom = room;
            if (room.hasEverBeenVisited || room != ChestTeleporterItem.FindBossFoyer())
                return;

            int currentArmor = (int)this.Owner.healthHaver.Armor;
            if (this.Owner.ForceZeroHealthState)
                currentArmor -= 1; // Robot gets set to 4 armor
            int armorToGain = Mathf.Max(1, _MIN_ARMOR - currentArmor);
            this.Owner.StartCoroutine(SpawnSomeArmor(room, armorToGain));
        }

        private IEnumerator SpawnSomeArmor(RoomHandler room, int armorToGain)
        {
            for (int i = 0; i < armorToGain; ++i)
            {
                yield return new WaitForSeconds(0.33f);
                bool success;
                Vector2 armorSpot = room.GetCenteredVisibleClearSpot(2, 2, out success).ToVector2();
                if (!success)
                    armorSpot = this.Owner.sprite.WorldCenter;
                LootEngine.SpawnItem(ItemHelper.Get(Items.Armor).gameObject, armorSpot, Vector2.zero, 0f, true, true, false);
            }
        }
    }
}