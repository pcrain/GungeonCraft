using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Alexandria.ItemAPI;
using Alexandria.Misc;
using MonoMod.RuntimeDetour;
using System.Reflection;

namespace NevernamedsItems
{
    class PercussionCap : BlankModificationItem
    {
        public static void Init()
        {
            string itemName = "Percussion Cap";
            string resourceName = "NevernamedsItems/Resources/NeoItemSprites/bluecap_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<PercussionCap>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Blanks Enspore";
            string longDesc = "This mushroom cap responds to the resonant frequency of blanks, letting it know that it's time to release it's spores.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.C;
            item.AddToSubShop(ItemBuilder.ShopType.OldRed);
            ID = item.PickupObjectId;
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.AdditionalBlanksPerFloor, 1f, StatModifier.ModifyMethod.ADDITIVE);

            item.BlankStunTime = 0;

            new Hook(
    typeof(SilencerInstance).GetMethod("ProcessBlankModificationItemAdditionalEffects", BindingFlags.Instance | BindingFlags.NonPublic),
    typeof(PercussionCap).GetMethod("BlankModHook", BindingFlags.Instance | BindingFlags.Public),
    typeof(SilencerInstance)
);
        }
        public static int ID;

        public void BlankModHook(Action<SilencerInstance, BlankModificationItem, Vector2, PlayerController> orig, SilencerInstance silencer, BlankModificationItem bmi, Vector2 centerPoint, PlayerController user)
        {
            orig(silencer, bmi, centerPoint, user);

            if (bmi != null && bmi.PickupObjectId == ID && user != null)
            {
                AkSoundEngine.PostEvent("Play_ENM_mushroom_cloud_01", user.gameObject);
                for (int i = 0; i < 30; i++)
                {
                    GameObject toFire = (PickupObjectDatabase.GetById(FungoCannon.FungoCannonID) as Gun).DefaultModule.chargeProjectiles[0].Projectile.gameObject;
                    GameObject obj = UnityEngine.Object.Instantiate<GameObject>(toFire, centerPoint, Quaternion.Euler(new Vector3(0f, 0f, UnityEngine.Random.Range(0, 360))));
                    Projectile proj = obj.GetComponent<Projectile>();
                    if (proj)
                    {
                        proj.Owner = user;
                        proj.Shooter = user.specRigidbody;

                        proj.baseData.damage *= user.stats.GetStatValue(PlayerStats.StatType.Damage);
                        proj.baseData.speed *= user.stats.GetStatValue(PlayerStats.StatType.ProjectileSpeed);
                        proj.baseData.range *= user.stats.GetStatValue(PlayerStats.StatType.RangeMultiplier);
                        proj.baseData.force *= user.stats.GetStatValue(PlayerStats.StatType.KnockbackMultiplier);
                        proj.BossDamageMultiplier *= user.stats.GetStatValue(PlayerStats.StatType.DamageToBosses);
                        proj.UpdateSpeed();
                        if (user.PlayerHasActiveSynergy("Screamosynthesis") && UnityEngine.Random.value <= 0.07f)
                        {
                            ExplosiveModifier explode = proj.gameObject.AddComponent<ExplosiveModifier>();
                            explode.doExplosion = true;
                            explode.explosionData = StaticExplosionDatas.explosiveRoundsExplosion;
                        }
                        user.DoPostProcessProjectile(proj);
                    }
                }
            }
        }
    }
}

