using System;
using System.Collections.Generic;
using System.Linq;
using Gungeon;
using System.Text;

using UnityEngine;
using ItemAPI;

namespace NevernamedsItems
{
    public class CardinalsMitre : PassiveItem
    {
        public static void Init()
        {
            //The name of the item
            string itemName = "Cardinals Mitre";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it
            string resourceName = "NevernamedsItems/Resources/cardinalsmitre_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a PassiveItem component to the object
            var item = obj.AddComponent<CardinalsMitre>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Ex Cathedra";
            string longDesc = "Fires a homing bullet upon reloading." + "\n\nImbued with power by a Bishop of the True Gun, hats like these are worn by Kaliber-devout Cardinals.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //Do this after ItemBuilder.AddSpriteToObject!
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");


            //Set the rarity of the item
            item.quality = PickupObject.ItemQuality.C;

            //MITRE PROJECTILES
            MitreProjectile = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(86) as Gun).DefaultModule.projectiles[0]);
            MitreProjectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(MitreProjectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(MitreProjectile);
            MitreProjectile.baseData.damage *= 5f;
            MitreProjectile.baseData.speed *= 0.4f;
            MitreProjectile.SetProjectileSpriteRight("mitreproj_1", 22, 22, true, tk2dBaseSprite.Anchor.MiddleCenter, 22, 22);

            HomingModifier homing = MitreProjectile.gameObject.AddComponent<HomingModifier>();
            homing.AngularVelocity = 80f;
            homing.HomingRadius = 1000f;

            MitreProjectile.AnimateProjectile(new List<string> {
                "mitreproj_1",
                "mitreproj_2",
                "mitreproj_3",
            }, 10, true, new List<IntVector2> {
                new IntVector2(22, 22), //1
                new IntVector2(20, 20), //2            
                new IntVector2(20, 20), //3
            }, AnimateBullet.ConstructListOfSameValues(true, 3), AnimateBullet.ConstructListOfSameValues(tk2dBaseSprite.Anchor.MiddleCenter, 3), AnimateBullet.ConstructListOfSameValues(true, 3), AnimateBullet.ConstructListOfSameValues(false, 3),
            AnimateBullet.ConstructListOfSameValues<Vector3?>(null, 3), AnimateBullet.ConstructListOfSameValues<IntVector2?>(null, 3), AnimateBullet.ConstructListOfSameValues<IntVector2?>(null, 3), AnimateBullet.ConstructListOfSameValues<Projectile>(null, 3));
        }
        public static Projectile MitreProjectile;
        bool canFire = true;
        private void HandleGunReloaded(PlayerController player, Gun playerGun)
        {
            if (playerGun.ClipShotsRemaining == 0)
            {
                FireBullet(player);
            }
        }
        private void FireBullet(PlayerController player)
        {
            if (canFire)
            {
                canFire = false;
                GameObject gameObject = SpawnManager.SpawnProjectile(MitreProjectile.gameObject, base.Owner.sprite.WorldCenter, Quaternion.Euler(0f, 0f, (base.Owner.CurrentGun == null) ? 0f : base.Owner.CurrentGun.CurrentAngle), true);
                Projectile component = gameObject.GetComponent<Projectile>();
                if (component != null)
                {
                    component.Owner = player;
                    component.Shooter = player.specRigidbody;
                    component.baseData.damage *= player.stats.GetStatValue(PlayerStats.StatType.Damage);
                    component.baseData.speed *= player.stats.GetStatValue(PlayerStats.StatType.ProjectileSpeed);
                    component.baseData.force *= player.stats.GetStatValue(PlayerStats.StatType.KnockbackMultiplier);
                    component.UpdateSpeed();
                    player.DoPostProcessProjectile(component);
                }

                Invoke("HandleCooldown", 0.7f);
            }
        }
        private void OnRollStarted(PlayerController player, Vector2 direction)
        {
            if (player.PlayerHasActiveSynergy("Holy Socks"))
            {
                FireBullet(player);
            }
        }
        private void HandleCooldown() { canFire = true; }
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.OnReloadedGun += this.HandleGunReloaded;
            player.OnRollStarted += this.OnRollStarted;
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject debrisObject = base.Drop(player);
            player.OnReloadedGun -= this.HandleGunReloaded;
            player.OnRollStarted -= this.OnRollStarted;

            return debrisObject;
        }
        public override void OnDestroy()
        {
            if (Owner)
            {
                Owner.OnReloadedGun -= this.HandleGunReloaded;
                Owner.OnRollStarted -= this.OnRollStarted;
            }

            base.OnDestroy();
        }
    }

}
