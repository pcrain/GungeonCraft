using System;
using System.Collections.Generic;
using System.Linq;
using Gungeon;
using System.Text;

using UnityEngine;
using ItemAPI;
using MonoMod.RuntimeDetour;
using System.Reflection;
using SaveAPI;

namespace NevernamedsItems
{
    public class NeutroniumAmmolet : BlankModificationItem
    {
        public static void Init()
        {       
            string itemName = "Neutronium Ammolet";
            string resourceName = "NevernamedsItems/Resources/Ammolets/neutroniumammolet_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<NeutroniumAmmolet>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Blanks Unravel";
            string longDesc = "An impossible element with no protons, created in the mantle of a neutron star."+"\n\nCrushes nearby spacetime when agitated by a blank.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.Curse, 1f, StatModifier.ModifyMethod.ADDITIVE);
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.AdditionalBlanksPerFloor, 1f, StatModifier.ModifyMethod.ADDITIVE);
            item.quality = PickupObject.ItemQuality.A;
            NeutroniumAmmoletID = item.PickupObjectId;
            item.AddToSubShop(ItemBuilder.ShopType.OldRed);

            item.SetupUnlockOnCustomFlag(CustomDungeonFlags.PURCHASED_NEUTRONIUMAMMOLET, true);
            item.AddItemToDougMetaShop(60);
        }
        private static int NeutroniumAmmoletID;
        private static Hook BlankHook = new Hook(
   typeof(SilencerInstance).GetMethod("ProcessBlankModificationItemAdditionalEffects", BindingFlags.Instance | BindingFlags.NonPublic),
   typeof(NeutroniumAmmolet).GetMethod("BlankModHook", BindingFlags.Instance | BindingFlags.Public),
   typeof(SilencerInstance));
        public void BlankModHook(Action<SilencerInstance, BlankModificationItem, Vector2, PlayerController> orig, SilencerInstance silencer, BlankModificationItem bmi, Vector2 centerPoint, PlayerController user)
        {
            orig(silencer, bmi, centerPoint, user);

                //ETGModConsole.Log("Blank hook ran");
            if (user.HasPickupID(NeutroniumAmmoletID))
            {
                //ETGModConsole.Log("HasID");
                Projectile projectile2 = ((Gun)ETGMod.Databases.Items["black_hole_gun"]).DefaultModule.projectiles[0];
                GameObject gameObject = SpawnManager.SpawnProjectile(projectile2.gameObject, centerPoint, Quaternion.identity, true);
                Projectile component = gameObject.GetComponent<Projectile>();
                if (component != null)
                {
                    component.Owner = user;
                    component.Shooter = user.specRigidbody;
                    component.baseData.speed = 0f;
                    component.baseData.range *= 100;
                    BulletLifeTimer timer = component.gameObject.AddComponent<BulletLifeTimer>();
                    timer.secondsTillDeath = 7f;
                    user.DoPostProcessProjectile(component);
                }
            }
        }
    }

}