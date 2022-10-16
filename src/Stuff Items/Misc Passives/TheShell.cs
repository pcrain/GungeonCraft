using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Alexandria.ItemAPI;
using MonoMod.RuntimeDetour;
using System.Reflection;

namespace NevernamedsItems
{
    public class TheShell : PassiveItem
    {
        public static void Init()
        {
            string itemName = "The Shell";
            string resourceName = "NevernamedsItems/Resources/BulletModifiers/theshell_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<TheShell>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "The First";
            string longDesc = "This is the first shotgun shell ever to be created, by the great Gunsmith Geddian" + "\n\nHas an affinity with all shotguns.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.B;
            item.SetTag("bullet_modifier");
        }
        public override void Pickup(PlayerController player)
        {
            player.GunChanged += GunChanged;
            base.Pickup(player);
            Recalc();
        }
        public override DebrisObject Drop(PlayerController player)
        {
            player.GunChanged -= GunChanged;
            return base.Drop(player);
        }
        public override void OnDestroy()
        {
            if (Owner)
            {
                Owner.GunChanged -= GunChanged;
            }
            base.OnDestroy();
        }
        private void Recalc()
        {
            AlterItemStats.RemoveStatFromPassive(this, PlayerStats.StatType.Damage);
            float multiplier = 1;
            float amtPerMod = 0.05f;
            if (Owner.PlayerHasActiveSynergy("ShellllehS llehSShell")) amtPerMod = 0.07f;
            if (Owner.PlayerHasActiveSynergy("Shoot Your Shot")) amtPerMod *= 3;
                if (Owner && Owner.CurrentGun)
            {
                if (Owner.CurrentGun.Volley && (Owner.CurrentGun.Volley.projectiles != null))
                {
                    multiplier += (Owner.CurrentGun.Volley.projectiles.Count * amtPerMod);
                }
                else if (Owner.CurrentGun.DefaultModule != null)
                {
                    multiplier += amtPerMod;
                }
                multiplier = Mathf.Min(multiplier, 3);
            }
            AlterItemStats.AddStatToPassive(this, PlayerStats.StatType.Damage, multiplier, StatModifier.ModifyMethod.MULTIPLICATIVE);
            Owner.stats.RecalculateStats(Owner, false, false);
        }
        private void GunChanged(Gun gun, Gun gun2, bool idk)
        {
            Recalc();
        }
    }
}
