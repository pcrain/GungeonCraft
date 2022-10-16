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
using Dungeonator;
using Alexandria.Misc;

namespace NevernamedsItems
{
    public class Seismograph : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Seismograph", "seismograph");
            Game.Items.Rename("outdated_gun_mods:seismograph", "nn:seismograph");
            var behav = gun.gameObject.AddComponent<Seismograph>();

            gun.SetShortDescription("Can Run Quake");
            gun.SetLongDescription("This gun is oddly good at predicting the seismic shaking of Gunymede's crust- it seems to always be fired moments before noticeable earthquakes.");

            gun.SetupSprite(null, "seismograph_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 16);
            gun.SetAnimationFPS(gun.idleAnimation, 16);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(601) as Gun).gunSwitchGroup;

            //GUN STATS
            gun.doesScreenShake = true;
            gun.gunScreenShake = StaticExplosionDatas.genericLargeExplosion.ss;
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Automatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1.3f;
            gun.DefaultModule.cooldownTime = 0.17f;
            gun.muzzleFlashEffects.type = VFXPoolType.None;
            gun.DefaultModule.numberOfShotsInClip = 10;
            gun.barrelOffset.transform.localPosition = new Vector3(25f / 16f, 17f / 16f, 0f);
            gun.SetBaseMaxAmmo(300);
            gun.gunClass = GunClass.PISTOL;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage = 4;



            projectile.pierceMinorBreakables = true;
            PierceProjModifier orAddComponent = projectile.gameObject.GetOrAddComponent<PierceProjModifier>();
            orAddComponent.penetratesBreakables = true;
            orAddComponent.penetration = 1;

            projectile.gameObject.AddComponent<Seismograph.EarthquakeProjectileMod>();



            gun.quality = PickupObject.ItemQuality.A;
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            ID = gun.PickupObjectId;
        }
        public static int ID;
        public class EarthquakeProjectileMod : MonoBehaviour
        {
            private void Start()
            {
                self = base.GetComponent<Projectile>();
                StartCoroutine(Quake());
            }
            private IEnumerator Quake()
            {
                yield return null;
                RoomHandler room = self.GetAbsoluteRoom();
                if (room != null)
                {
                    Exploder.DoRadialMinorBreakableBreak(base.transform.position, 20);
                    List<AIActor> enemies = room.GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
                    if (enemies != null && enemies.Count > 0)
                    {
                        for (int i = 0; i < enemies.Count; i++)
                        {
                            AIActor enemy = enemies[i];
                            if (enemy && enemy.healthHaver && enemy.healthHaver.IsAlive)
                            {
                                float knockback = 20;

                                float dmg = self.baseData.damage;

                                if (self.ProjectilePlayerOwner())
                                {
                                    knockback *= self.ProjectilePlayerOwner().stats.GetStatValue(PlayerStats.StatType.KnockbackMultiplier);
                                    if (enemy.healthHaver.IsBoss) dmg *= self.ProjectilePlayerOwner().stats.GetStatValue(PlayerStats.StatType.DamageToBosses);
                                    if (enemy.aiActor.IsBlackPhantom) dmg *= self.BlackPhantomDamageMultiplier;
                                }

                                enemy.healthHaver.ApplyDamage(dmg, UnityEngine.Random.insideUnitCircle, "Quake");
                                if (enemy.knockbackDoer) enemy.knockbackDoer.ApplyKnockback(UnityEngine.Random.insideUnitCircle, knockback, false);

                                List<GameActorEffect> effects = self.GetFullListOfStatusEffects();
                                if (effects.Count > 0) foreach (GameActorEffect effect in effects) enemy.ApplyEffect(effect);
                                if (enemy.behaviorSpeculator && self.AppliesStun && UnityEngine.Random.value <= self.StunApplyChance) enemy.behaviorSpeculator.Stun(self.AppliedStunDuration);

                            }
                        }
                    }
                }
                UnityEngine.Object.Destroy(self.gameObject);
                yield break;
            }
            private Projectile self;
            public float damageToDeal;
        }
    }
}
