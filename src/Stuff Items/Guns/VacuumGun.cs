using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using Alexandria.ItemAPI;
using SaveAPI;
using Alexandria.Misc;

namespace NevernamedsItems
{
    public class VacuumGun : GunBehaviour
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Vacuum Gun", "vacuumgun");
            Game.Items.Rename("outdated_gun_mods:vacuum_gun", "nn:vacuum_gun");
            gun.gameObject.AddComponent<VacuumGun>();
            gun.SetShortDescription("Ranged Weapon");
            gun.SetLongDescription("Pressing reload sucks up nearby blobs, and uses them as ammo. Cannot gain ammo by any other method." + "\n\nDesigned specifically to combat Blobulonian creatures, in the case of a potential re-emergence of the empire." + "\n\nZZZZZZZ");

            gun.SetupSprite(null, "vacuumgun_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 12);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(150) as Gun).gunSwitchGroup;

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Automatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.ammo = 0;
            gun.CanGainAmmo = false;
            gun.DefaultModule.cooldownTime = 0.3f;
            gun.DefaultModule.numberOfShotsInClip = 10000;
            gun.barrelOffset.transform.localPosition = new Vector3(1.75f, 0.81f, 0f);
            gun.SetBaseMaxAmmo(10000);
            gun.gunClass = GunClass.SILLY;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage = 30f;
            projectile.baseData.speed *= 1f;
            projectile.baseData.range *= 1f;
            projectile.SetProjectileSpriteRight("vacuumgun_projectile", 16, 14, false, tk2dBaseSprite.Anchor.MiddleCenter, 15, 13);
            GoopModifier gooper = projectile.gameObject.AddComponent<GoopModifier>();
            gooper.SpawnGoopInFlight = false;
            gooper.SpawnGoopOnCollision = true;
            gooper.CollisionSpawnRadius = 2;
            gooper.goopDefinition = EasyGoopDefinitions.BlobulonGoopDef;
            CustomImpactSoundBehav sound = projectile.gameObject.AddComponent<CustomImpactSoundBehav>();
            sound.ImpactSFX = "Play_BlobulonDeath";

            projectile.transform.parent = gun.barrelOffset;

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("Vacuum Gun Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/vacuumgun_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/vacuumgun_clipempty");

            gun.quality = PickupObject.ItemQuality.C;
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            gun.SetupUnlockOnCustomFlag(CustomDungeonFlags.PURCHASED_VACUUMGUN, true);
            gun.AddItemToGooptonMetaShop(16);
            gun.AddToSubShop(ItemBuilder.ShopType.Goopton);
            ID = gun.PickupObjectId;
        }
        public static int ID;
        public override void PostProcessProjectile(Projectile projectile)
        {
            if (projectile && projectile.ProjectilePlayerOwner())
            {
                List<int> possibleForms = new List<int>();
                if (projectile.ProjectilePlayerOwner().PlayerHasActiveSynergy("Poisbulonial"))
                {
                    possibleForms.Add(1);
                    projectile.statusEffectsToApply.Add(StaticStatusEffects.irradiatedLeadEffect);
                }
                if (projectile.ProjectilePlayerOwner().PlayerHasActiveSynergy("Blizzbulonial"))
                {
                    possibleForms.Add(2);
                    projectile.statusEffectsToApply.Add(StaticStatusEffects.frostBulletsEffect);
                }
                if (projectile.ProjectilePlayerOwner().PlayerHasActiveSynergy("Leadbulonial"))
                {
                    possibleForms.Add(3);
                    projectile.statusEffectsToApply.Add(StaticStatusEffects.hotLeadEffect);
                }
                if (projectile.ProjectilePlayerOwner().PlayerHasActiveSynergy("Poopulonial"))
                {
                    possibleForms.Add(4);
                    projectile.statusEffectsToApply.Add(StaticStatusEffects.tripleCrossbowSlowEffect);
                }
                if (possibleForms.Count > 0)
                {
                    int randomGoopForm = BraveUtility.RandomElement(possibleForms);
                    switch (randomGoopForm)
                    {
                        case 1:
                            projectile.gameObject.GetComponent<GoopModifier>().goopDefinition = EasyGoopDefinitions.PoisonDef;
                            projectile.damageTypes |= CoreDamageTypes.Poison;
                            projectile.AdjustPlayerProjectileTint(ExtendedColours.poisonGreen, 1);
                            break;
                        case 2:
                            projectile.gameObject.GetComponent<GoopModifier>().goopDefinition = EasyGoopDefinitions.WaterGoop;
                            projectile.damageTypes |= CoreDamageTypes.Ice;
                            projectile.AdjustPlayerProjectileTint(ExtendedColours.skyblue, 1);
                            break;
                        case 3:
                            projectile.gameObject.GetComponent<GoopModifier>().goopDefinition = EasyGoopDefinitions.FireDef;
                            projectile.damageTypes |= CoreDamageTypes.Fire;
                            projectile.AdjustPlayerProjectileTint(Color.grey, 1);
                            break;
                        case 4:
                            projectile.AdjustPlayerProjectileTint(ExtendedColours.brown, 1);
                            break;
                    }
                }
                if (projectile.ProjectilePlayerOwner().PlayerHasActiveSynergy("Bloodbulonial"))
                {
                    SpawnProjModifier spawnpoj = projectile.gameObject.AddComponent<SpawnProjModifier>();
                    spawnpoj.projectileToSpawnOnCollision = (PickupObjectDatabase.GetById(38) as Gun).DefaultModule.projectiles[0];
                    spawnpoj.PostprocessSpawnedProjectiles = true;
                    spawnpoj.numberToSpawnOnCollison = 10;
                    spawnpoj.spawnOnObjectCollisions = true;
                    spawnpoj.spawnProjecitlesOnDieInAir = true;
                    spawnpoj.spawnProjectilesInFlight = false;
                    spawnpoj.spawnProjectilesOnCollision = true;
                    spawnpoj.randomRadialStartAngle = true;
                }
            }
            base.PostProcessProjectile(projectile);
        }
        public override void OnReloadPressed(PlayerController player, Gun gun, bool bSOMETHING)
        {
            base.OnReloadPressed(player, gun, bSOMETHING);
            if (player.CurrentRoom != null)
            {
                player.CurrentRoom.ApplyActionToNearbyEnemies(player.CenterPosition, 8f, new Action<AIActor, float>(this.ProcessEnemy));
            }

        }
        private void ProcessEnemy(AIActor target, float distance)
        {
            if (target && target.healthHaver && !target.healthHaver.IsBoss && target.HasTag("blobulon"))
            {
                GameManager.Instance.Dungeon.StartCoroutine(this.HandleEnemySuck(target));
                target.EraseFromExistence(true);
                gun.ammo += Math.Max(Mathf.CeilToInt(target.healthHaver.GetMaxHealth() * 1.5f), 5);
            }
            else if (target.EnemyGuid == EnemyGuidDatabase.Entries["chicken"])
            {
                if (gun.CurrentOwner && (gun.CurrentOwner as PlayerController).PlayerHasActiveSynergy("Chickadoo"))
                {
                    GameManager.Instance.Dungeon.StartCoroutine(this.HandleEnemySuck(target));
                    target.EraseFromExistence(true);
                    gun.ammo += 5;
                }
            }
        }
        private IEnumerator HandleEnemySuck(AIActor target)
        {
            Transform copySprite = this.CreateEmptySprite(target);
            Vector3 startPosition = copySprite.transform.position;
            float elapsed = 0f;
            float duration = 0.5f;
            while (elapsed < duration)
            {
                elapsed += BraveTime.DeltaTime;
                if (gun && copySprite)
                {
                    Vector3 position = gun.PrimaryHandAttachPoint.position;
                    float t = elapsed / duration * (elapsed / duration);
                    copySprite.position = Vector3.Lerp(startPosition, position, t);
                    copySprite.rotation = Quaternion.Euler(0f, 0f, 360f * BraveTime.DeltaTime) * copySprite.rotation;
                    copySprite.localScale = Vector3.Lerp(Vector3.one, new Vector3(0.1f, 0.1f, 0.1f), t);
                }
                yield return null;
            }
            if (copySprite)
            {
                UnityEngine.Object.Destroy(copySprite.gameObject);
            }
            yield break;
        }
        private Transform CreateEmptySprite(AIActor target)
        {
            GameObject gameObject = new GameObject("suck image");
            gameObject.layer = target.gameObject.layer;
            tk2dSprite tk2dSprite = gameObject.AddComponent<tk2dSprite>();
            gameObject.transform.parent = SpawnManager.Instance.VFX;
            tk2dSprite.SetSprite(target.sprite.Collection, target.sprite.spriteId);
            tk2dSprite.transform.position = target.sprite.transform.position;
            GameObject gameObject2 = new GameObject("image parent");
            gameObject2.transform.position = tk2dSprite.WorldCenter;
            tk2dSprite.transform.parent = gameObject2.transform;
            if (target.optionalPalette != null)
            {
                tk2dSprite.renderer.material.SetTexture("_PaletteTex", target.optionalPalette);
            }
            return gameObject2.transform;
        }
        public VacuumGun()
        {

        }
        public string[] TargetEnemies;
        public float SuckRadius;
    }
}
