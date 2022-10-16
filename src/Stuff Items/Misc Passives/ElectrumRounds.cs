using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Alexandria.ItemAPI;
using UnityEngine;
using MonoMod.RuntimeDetour;
using System.Reflection;
using SaveAPI;
using Gungeon;

namespace NevernamedsItems
{
    public class ElectrumRounds : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Electrum Rounds";
            string resourceName = "NevernamedsItems/Resources/NeoItemSprites/electrumrounds_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<ElectrumRounds>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Zip Zop Zap";
            string longDesc = "Fast, penetrative bullets made of gold and silver alloy. Highly conductive, it maintains a powerful electric bond with it's home holster.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.S;
            item.SetTag("bullet_modifier");
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.ProjectileSpeed, 1.7f, StatModifier.ModifyMethod.MULTIPLICATIVE);
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.AdditionalShotPiercing, 1, StatModifier.ModifyMethod.ADDITIVE);

            linkVFX = FakePrefab.Clone(PickupObjectDatabase.GetById(298).GetComponent<ComplexProjectileModifier>().ChainLightningVFX);
            FakePrefab.MarkAsFakePrefab(linkVFX);
            UnityEngine.Object.DontDestroyOnLoad(linkVFX);
            item.SetupUnlockOnCustomFlag(CustomDungeonFlags.ADVDRAGUN_KILLED_ROBOT, true);
        }
        public static GameObject linkVFX;
        private void PostProcessProjectile(Projectile sourceProjectile, float effectChanceScalar)
        {
            OwnerConnectLightningModifier litening = sourceProjectile.gameObject.AddComponent<OwnerConnectLightningModifier>();
            litening.linkPrefab = linkVFX;
        }

        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject debrisObject = base.Drop(player);
            player.PostProcessProjectile -= this.PostProcessProjectile;


            return debrisObject;
        }
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.PostProcessProjectile += this.PostProcessProjectile;
        }
        public override void OnDestroy()
        {
            if (Owner)
            {
                Owner.PostProcessProjectile -= this.PostProcessProjectile;
            }
            base.OnDestroy();
        }
    }
}
