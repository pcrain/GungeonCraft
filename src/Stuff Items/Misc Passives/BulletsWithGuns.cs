using Alexandria.ItemAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NevernamedsItems
{
    public class BulletsWithGuns : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Bullets With Guns";
            string resourceName = "NevernamedsItems/Resources/bulletswithguns_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<BulletsWithGuns>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Bullets From Bullets";
            string longDesc = "Your bullets move slower, but they take that extra time to aim and shoot more bullets at enemies mid-air!" + "\n\n...this is getting a little ridiculous.";

            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.ProjectileSpeed, 0.6f, StatModifier.ModifyMethod.MULTIPLICATIVE);
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.Damage, 0.85f, StatModifier.ModifyMethod.MULTIPLICATIVE);
            item.quality = PickupObject.ItemQuality.B;
            item.SetTag("bullet_modifier");

            Projectile projectile = UnityEngine.Object.Instantiate(((Gun)ETGMod.Databases.Items[86]).DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            projectile.gameObject.AddComponent<BulletFromBulletWithGun>();
            projectile.baseData.damage *= 0.8f;
            projectile.AdditionalScaleMultiplier *= 0.5f;
            projectileToSpawn = projectile;

            Projectile projectile2 = UnityEngine.Object.Instantiate(((Gun)ETGMod.Databases.Items[377]).DefaultModule.projectiles[0]);
            projectile2.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile2.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile2);
            projectile2.gameObject.AddComponent<BulletFromBulletWithGun>();
            projectile2.baseData.damage *= 0.715f;
            projectile2.AdditionalScaleMultiplier *= 0.8f;
            swordProjectile = projectile2;
        }
        public class BulletFromBulletWithGun : MonoBehaviour { }
        public static Projectile projectileToSpawn;
        public static Projectile swordProjectile;
        public override void Pickup(PlayerController player)
        {
            player.PostProcessProjectile += this.PostProcessProj;
            player.PostProcessBeam += this.PostProcessBeam;
            base.Pickup(player);
        }
        private void PostProcessProj(Projectile bullet, float scaler)
        {
            if (bullet.GetComponent<BulletFromBulletWithGun>() == null)
            {
                if (bullet.GetComponent<SpawnProjModifier>() != null)
                {
                    SpawnProjModifier extantMod = bullet.gameObject.GetComponent<SpawnProjModifier>();
                    if (extantMod.spawnProjectilesInFlight)
                    {
                        extantMod.inFlightSpawnCooldown *= 0.5f;
                    }
                    else
                    {
                        extantMod.spawnProjectilesInFlight = true;
                        extantMod.inFlightAimAtEnemies = true;
                        extantMod.usesComplexSpawnInFlight = true;
                        extantMod.inFlightSpawnCooldown = 0.5f;
                        extantMod.PostprocessSpawnedProjectiles = true;
                        if (Owner.PlayerHasActiveSynergy("Bullets With Knives")) extantMod.projectileToSpawnInFlight = swordProjectile;
                        else extantMod.projectileToSpawnInFlight = projectileToSpawn;
                    }
                }
                else
                {
                    SpawnProjModifier mod = bullet.gameObject.GetOrAddComponent<SpawnProjModifier>();
                    mod.spawnProjectilesInFlight = true;
                    mod.inFlightAimAtEnemies = true;
                    mod.inFlightSpawnCooldown = 0.5f;
                    mod.usesComplexSpawnInFlight = true;
                    mod.PostprocessSpawnedProjectiles = true;
                    mod.numToSpawnInFlight = 1;
                    if (Owner.PlayerHasActiveSynergy("Bullets With Knives")) mod.projectileToSpawnInFlight = swordProjectile;
                   else mod.projectileToSpawnInFlight = projectileToSpawn;
                }
            }
        }
        private void PostProcessBeam(BeamController beam)
        {
        SpawnProjectileAtBeamPoint beamspawner =   beam.gameObject.GetOrAddComponent<SpawnProjectileAtBeamPoint>();
            beamspawner.addFromBulletWithGunComponent = true;
            beamspawner.doPostProcess = true;
            if (Owner.PlayerHasActiveSynergy("Bullets With Knives")) beamspawner.projectileToFire = swordProjectile;
            else beamspawner.projectileToFire = projectileToSpawn;
        }
        public override DebrisObject Drop(PlayerController player)
        {
            player.PostProcessProjectile -= this.PostProcessProj;
            player.PostProcessBeam -= this.PostProcessBeam;

            return base.Drop(player);
        }
        public override void OnDestroy()
        {
            if (Owner)
            {
                Owner.PostProcessProjectile -= this.PostProcessProj;
                Owner.PostProcessBeam -= this.PostProcessBeam;

            }
            base.OnDestroy();
        }
    }
}
