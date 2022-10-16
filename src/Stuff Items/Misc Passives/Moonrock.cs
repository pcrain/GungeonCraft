using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using ItemAPI;
using UnityEngine;
using MonoMod.RuntimeDetour;
using System.Reflection;
using Alexandria.Misc;

namespace NevernamedsItems
{
    public class Moonrock : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Moonrock";
            string resourceName = "NevernamedsItems/Resources/moonrock_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<Moonrock>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Little Orbiters";
            string longDesc = "Causes small chunks of space debris to orbit your shots."+ "\n\nRound and round and round and round and round and round and round and round and round and round and round and round and round and round and round and round and round and...";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.B;

            moonrockProjectile = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(86) as Gun).DefaultModule.projectiles[0]);
            moonrockProjectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(moonrockProjectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(moonrockProjectile);
            moonrockProjectile.baseData.damage = 5f;
            moonrockProjectile.baseData.speed *= 0.5f;
            moonrockProjectile.SetProjectileSpriteRight("moonrock_proj", 4, 4, false, tk2dBaseSprite.Anchor.MiddleCenter, 4, 4);
        }
        public static Projectile moonrockProjectile;
        private void PostProcessProjectile(Projectile sourceProjectile, float effectChanceScalar)
        {
            if (sourceProjectile.GetComponent<MoonrockProjectile>()) return;
            if (sourceProjectile is InstantDamageOneEnemyProjectile) return;
            if (sourceProjectile is InstantlyDamageAllProjectile) return;
            if (sourceProjectile.GetComponent<ArtfulDodgerProjectileController>()) return;

            PlayerController owner = sourceProjectile.ProjectilePlayerOwner();

            int amtOfOrbiters = UnityEngine.Random.Range(0, 4);
            if (amtOfOrbiters > 0)
            {
                for (int i = 0; i < amtOfOrbiters; i++)
                {
                    GameObject gameObject = SpawnManager.SpawnProjectile(moonrockProjectile.gameObject, sourceProjectile.sprite.WorldCenter, Quaternion.Euler(0f, 0f, (base.Owner.CurrentGun == null) ? 0f : base.Owner.CurrentGun.CurrentAngle), true);
                    Projectile component = gameObject.GetComponent<Projectile>();
                    if (component != null)
                    {
                        component.Owner = owner;
                        component.Shooter = owner.specRigidbody;
                        component.baseData.damage *= owner.stats.GetStatValue(PlayerStats.StatType.Damage);
                        component.baseData.speed *= owner.stats.GetStatValue(PlayerStats.StatType.ProjectileSpeed);
                        component.baseData.force *= owner.stats.GetStatValue(PlayerStats.StatType.KnockbackMultiplier);
                        component.UpdateSpeed();
         
                        component.specRigidbody.CollideWithTileMap = true;

                        BulletLifeTimer timer = component.gameObject.GetOrAddComponent<BulletLifeTimer>();
                        timer.secondsTillDeath = 30f;

                        OrbitProjectileMotionModule orbitProjectileMotionModule = new OrbitProjectileMotionModule();
                        orbitProjectileMotionModule.lifespan = 50;
                        orbitProjectileMotionModule.MinRadius = 0.5f;
                        orbitProjectileMotionModule.MaxRadius = 2;
                        orbitProjectileMotionModule.usesAlternateOrbitTarget = true;
                        orbitProjectileMotionModule.OrbitGroup = -5;
                        orbitProjectileMotionModule.alternateOrbitTarget = sourceProjectile.specRigidbody;
                        if (component.OverrideMotionModule != null && component.OverrideMotionModule is HelixProjectileMotionModule)
                        {
                            orbitProjectileMotionModule.StackHelix = true;
                            orbitProjectileMotionModule.ForceInvert = (component.OverrideMotionModule as HelixProjectileMotionModule).ForceInvert;
                        }
                        component.OverrideMotionModule = orbitProjectileMotionModule;

                        component.gameObject.GetOrAddComponent<MoonrockProjectile>();

                        owner.DoPostProcessProjectile(component);
                    }
                }
            }

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
    class MoonrockProjectile : MonoBehaviour { }
}
