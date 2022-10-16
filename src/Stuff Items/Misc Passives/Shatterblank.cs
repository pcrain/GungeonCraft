using UnityEngine;
using ItemAPI;
using Dungeonator;
using System.Collections;
using System;
using MonoMod.RuntimeDetour;
using System.Reflection;
using Gungeon;
using System.Collections.Generic;
using SaveAPI;

namespace NevernamedsItems
{
    public class Shatterblank : BlankModificationItem
    {
        public static void Init()
        {
            string itemName = "Shatterblank";
            string resourceName = "NevernamedsItems/Resources/shatterblank_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<Shatterblank>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Fragmentation";
            string longDesc = "Blanks release dangerous shrapnel." + "\n\nThis artefact was originally part of a brittle Ammolet, before the whole thing was shattered into a thousand tiny pieces.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");


            item.quality = PickupObject.ItemQuality.D;
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.AdditionalBlanksPerFloor, 1f, StatModifier.ModifyMethod.ADDITIVE);
            item.AddToSubShop(ItemBuilder.ShopType.OldRed);
            ShatterblankID = item.PickupObjectId;

            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            projectile.baseData.damage = 6f;
            projectile.baseData.range *= 4f;
            BounceProjModifier bounce = projectile.gameObject.AddComponent<BounceProjModifier>();
            bounce.numberOfBounces += 3;
            shatterProj = projectile;
        }

        private static int ShatterblankID;

        public static Projectile shatterProj;

        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
        }

        private static Hook BlankHook = new Hook(
    typeof(SilencerInstance).GetMethod("ProcessBlankModificationItemAdditionalEffects", BindingFlags.Instance | BindingFlags.NonPublic),
    typeof(Shatterblank).GetMethod("BlankModHook", BindingFlags.Instance | BindingFlags.Public),
    typeof(SilencerInstance)
);

        public void BlankModHook(Action<SilencerInstance, BlankModificationItem, Vector2, PlayerController> orig, SilencerInstance silencer, BlankModificationItem bmi, Vector2 centerPoint, PlayerController user)
        {
            orig(silencer, bmi, centerPoint, user);

            if (user.HasPickupID(ShatterblankID))
            {
                float degrees = 0;
                for (int i = 0; i < 15; i++)
                {
                    GameObject proj = shatterProj.gameObject;
                    bool needsToAddBounce = false;
                    bool isBeamInstead = false;
                    if (user.PlayerHasActiveSynergy("Frag Mental"))
                    {
                        if (user.CurrentGun && UnityEngine.Random.value <= 0.2f)
                        {
                            if (user.CurrentGun.DefaultModule.shootStyle == ProjectileModule.ShootStyle.Charged)
                            {
                                for (int j = 0; j < user.CurrentGun.DefaultModule.chargeProjectiles.Count; j++)
                                {
                                    if (user.CurrentGun.DefaultModule.chargeProjectiles[j] != null && user.CurrentGun.DefaultModule.chargeProjectiles[j].Projectile != null)
                                    {
                                        proj = user.CurrentGun.DefaultModule.chargeProjectiles[j].Projectile.gameObject;
                                        break;
                                    }
                                }
                            }
                            else if (user.CurrentGun.DefaultModule.shootStyle == ProjectileModule.ShootStyle.Beam)
                            {
                                proj = user.CurrentGun.DefaultModule.projectiles[0].gameObject;
                                isBeamInstead = true;
                            }
                            else
                            {
                                proj = user.CurrentGun.DefaultModule.projectiles[0].gameObject;
                            }
                            needsToAddBounce = true;
                        }
                    }
                    if (isBeamInstead)
                    {
                        BeamController beam = BeamController.FreeFireBeam(proj.GetComponent<Projectile>(), user, degrees, 3, true);
                        if (beam && beam.GetComponent<Projectile>())
                        {
                            Projectile component = beam.GetComponent<Projectile>();
                            if (user.stats.GetStatValue(PlayerStats.StatType.Damage) >= 1) component.baseData.damage *= user.stats.GetStatValue(PlayerStats.StatType.Damage);
                            component.baseData.range *= user.stats.GetStatValue(PlayerStats.StatType.RangeMultiplier);
                            component.baseData.speed *= user.stats.GetStatValue(PlayerStats.StatType.ProjectileSpeed);
                            component.UpdateSpeed();
                            component.baseData.range *= 4f;
                            BounceProjModifier bounce = component.gameObject.AddComponent<BounceProjModifier>();
                            bounce.numberOfBounces += 3;

                        }
                    }
                    else
                    {
                        GameObject gameObject = SpawnManager.SpawnProjectile(proj, centerPoint, Quaternion.Euler(0, 0, degrees), true);
                        Projectile component = gameObject.GetComponent<Projectile>();
                        if (component != null)
                        {
                            component.TreatedAsNonProjectileForChallenge = true;
                            component.Owner = user;
                            component.Shooter = user.specRigidbody;
                            user.DoPostProcessProjectile(component);
                            if (user.stats.GetStatValue(PlayerStats.StatType.Damage) >= 1) component.baseData.damage *= user.stats.GetStatValue(PlayerStats.StatType.Damage);
                            component.baseData.range *= user.stats.GetStatValue(PlayerStats.StatType.RangeMultiplier);
                            component.baseData.speed *= user.stats.GetStatValue(PlayerStats.StatType.ProjectileSpeed);
                            component.UpdateSpeed();
                            if (needsToAddBounce)
                            {
                                component.baseData.range *= 4f;
                                BounceProjModifier bounce = component.gameObject.AddComponent<BounceProjModifier>();
                                bounce.numberOfBounces += 3;
                            }
                        }
                    }
                    degrees += 24;
                }
            }
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject debrisObject = base.Drop(player);
            return debrisObject;
        }
    }
}
