using UnityEngine;
using ItemAPI;
using Dungeonator;
using System.Collections;
using System;
using MonoMod.RuntimeDetour;
using System.Reflection;
using Gungeon;
using System.Collections.Generic;


namespace NevernamedsItems
{
    public class IvoryAmmolet : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Ivory Ammolet";
            string resourceName = "NevernamedsItems/Resources/ivoryammolet_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<IvoryAmmolet>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Tiny Blanks";
            string longDesc = "Makes your blanks weaker, but gives you more of them." + "\n\nCarved from rare Dragun ivory.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.C;
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.AdditionalBlanksPerFloor, 2f, StatModifier.ModifyMethod.ADDITIVE);

            item.AddToSubShop(ItemBuilder.ShopType.OldRed);

            IvoryAmmoletID = item.PickupObjectId;

            BlankHook = new Hook(
            typeof(SilencerInstance).GetMethod("TriggerSilencer", BindingFlags.Instance | BindingFlags.Public),
            typeof(IvoryAmmolet).GetMethod("BlankModHook", BindingFlags.Instance | BindingFlags.Public),
            typeof(SilencerInstance)
            );

            blankPickupHook = new Hook(
                   typeof(SilencerItem).GetMethod("Pickup", BindingFlags.Instance | BindingFlags.Public),
                   typeof(IvoryAmmolet).GetMethod("blankPickupHookMethod")

                   );
        }
        private static Hook blankPickupHook;
        public override void Pickup(PlayerController player)
        {
            if (!m_pickedUpThisRun)
            {
                LootEngine.GivePrefabToPlayer(PickupObjectDatabase.GetById(224).gameObject, player);
            }
            base.Pickup(player);
        }

        public static int IvoryAmmoletID;

        private static Hook BlankHook;
        public delegate void Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 t10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15);
        public void BlankModHook(Action<SilencerInstance, Vector2, float, float, GameObject, float, float, float, float, float, float, float, PlayerController, bool, bool> orig, SilencerInstance silencer, Vector2 pos, float expandSpeed, float maxRadius, GameObject vfx, float distIntensity, float distRadius, float pushForce, float pushRadius, float knockbackForce, float knockbackRadius, float additionalTimeAtMaxRadius, PlayerController user, bool breaksWalls = true, bool skipBreakables = true)
        {
            if (silencer && !silencer.ForceNoDamage && user != null)
            {
                if (user.HasPickupID(IvoryAmmoletID))
                {
                    GameObject smallVFX = (GameObject)ResourceCache.Acquire("Global VFX/BlankVFX_Ghost");
                    orig(silencer, pos, expandSpeed * 0.5f, maxRadius / 5, smallVFX, 0, 0, pushForce / 16, pushRadius / 3, knockbackForce * 0.5f, knockbackRadius / 3f, additionalTimeAtMaxRadius * 0.5f, user, breaksWalls, skipBreakables);
                }
                else
                {
                    orig(silencer, pos, expandSpeed, maxRadius, vfx, distIntensity, distRadius, pushForce, pushRadius, knockbackForce, knockbackRadius, additionalTimeAtMaxRadius, user, breaksWalls, skipBreakables);
                }
            }
            else
            {
                Debug.Log("Ivory Ammolet Silencer Hook: DEFAULTED TO BASE. Silencer: " + (silencer == null) + "Owner: " + (user == null) + " ForceNoDMG: " + (silencer.ForceNoDamage));
                orig(silencer, pos, expandSpeed, maxRadius, vfx, distIntensity, distRadius, pushForce, pushRadius, knockbackForce, knockbackRadius, additionalTimeAtMaxRadius, user, breaksWalls, skipBreakables);
            }
        }
        public static void blankPickupHookMethod(Action<SilencerItem, PlayerController> orig, SilencerItem self, PlayerController player)
        {
            orig(self, player);
            if (self && player)
            {
                if (player.HasPickupID(IvoryAmmoletID))
                {
                    player.Blanks += 2;
                }
            }
        }
    }
}

