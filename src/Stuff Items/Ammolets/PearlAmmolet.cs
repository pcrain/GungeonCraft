using UnityEngine;
using Alexandria.ItemAPI;
using Dungeonator;
using System.Collections;
using System;
using MonoMod.RuntimeDetour;
using System.Reflection;
using Gungeon;
using System.Collections.Generic;
using SaveAPI;
using Alexandria.Misc;

namespace NevernamedsItems
{
    public class PearlAmmolet : BlankModificationItem
    {
        public static void Init()
        {
            string itemName = "Pearl Ammolet";
            string resourceName = "NevernamedsItems/Resources/Ammolets/pearlammolet_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<PearlAmmolet>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Blanks Bubble";
            string longDesc = "Blanks convert enemy bullets into bubbles."+"\n\n Stolen from the Mother Clam, in a daring heist along the floor of a bottomless ocean.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");


            item.quality = PickupObject.ItemQuality.C;
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.AdditionalBlanksPerFloor, 1f, StatModifier.ModifyMethod.ADDITIVE);
            item.AddToSubShop(ItemBuilder.ShopType.OldRed);
            ID = item.PickupObjectId;
        }

        private static int ID;

        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
        }

        private static Hook BlankHook = new Hook(
    typeof(SilencerInstance).GetMethod("ProcessBlankModificationItemAdditionalEffects", BindingFlags.Instance | BindingFlags.NonPublic),
    typeof(PearlAmmolet).GetMethod("BlankModHook", BindingFlags.Instance | BindingFlags.Public),
    typeof(SilencerInstance)
);

        public void BlankModHook(Action<SilencerInstance, BlankModificationItem, Vector2, PlayerController> orig, SilencerInstance silencer, BlankModificationItem bmi, Vector2 centerPoint, PlayerController user)
        {
            orig(silencer, bmi, centerPoint, user);

            if (user.HasPickupID(ID))
            {
                silencer.UsesCustomProjectileCallback = true;
                silencer.OnCustomBlankedProjectile += OnBlankedProjectile;
            }
        }
        public static void OnBlankedProjectile(Projectile proj)
        {
            if (GameManager.Instance.PrimaryPlayer)
            {
                GameObject gameObject = SpawnManager.SpawnProjectile((PickupObjectDatabase.GetById(599) as Gun).DefaultModule.projectiles[0].gameObject, proj.transform.position + UnityEngine.Random.insideUnitCircle.ToVector3ZisY(0f), Quaternion.identity, true);
                Projectile component = gameObject.GetComponent<Projectile>();
                component.Owner = GameManager.Instance.PrimaryPlayer;
                component.Shooter = GameManager.Instance.PrimaryPlayer.specRigidbody;
                if (GameManager.Instance.AnyPlayerHasActiveSynergy("Bubble Blowing, Baby!")) { component.RuntimeUpdateScale(1.3f); component.baseData.damage *= 1.5f; }
                component.collidesWithPlayer = false;
                component.collidesWithEnemies = true;
                component.collidesWithProjectiles = false;
                // ETGModConsole.Log($"Direction({proj.Direction}) Angle({proj.Direction.ToAngle()})");
                component.StartCoroutine(OnBlankedDelay(component, proj.Direction));
                component.gameObject.AddComponent<DieWhenOwnerNotInRoom>();
            }
        }
        public static IEnumerator OnBlankedDelay(Projectile component, Vector2 direction)
        {
            yield return new WaitForEndOfFrame();
            if (component) component.SendInDirection(direction, true);
            yield break;
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject debrisObject = base.Drop(player);
            return debrisObject;
        }
    }
}