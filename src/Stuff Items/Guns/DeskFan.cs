using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using ItemAPI;
using MonoMod.RuntimeDetour;
using System.Reflection;
using Dungeonator;
using Alexandria.Misc;

namespace NevernamedsItems
{
    public class DeskFan : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Desk Fan", "deskfan");
            Game.Items.Rename("outdated_gun_mods:desk_fan", "nn:desk_fan");
            gun.gameObject.AddComponent<DeskFan>();
            gun.SetShortDescription("Night Shift");
            gun.SetLongDescription("Pushes enemies away, and does slight damage." + "\n\nHides great and terrible secrets... maybe.");

            gun.SetupSprite(null, "deskfan_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 24);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(520) as Gun).gunSwitchGroup;

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);

            //GUN STATS
            gun.reloadTime = 0.8f;
            gun.barrelOffset.transform.localPosition = new Vector3(0.62f, 0.62f, 0f);
            gun.muzzleFlashEffects.type = VFXPoolType.None;
            gun.SetBaseMaxAmmo(700);
            gun.ammo = 700;
            gun.doesScreenShake = false;
            gun.gunClass = GunClass.SHITTY;

            gun.DefaultModule.angleVariance = 5;
            gun.DefaultModule.cooldownTime = 0.11f;
            gun.DefaultModule.numberOfShotsInClip = 30;
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Automatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;

            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage = 1f;
            projectile.baseData.speed *= 0.01f;
            projectile.baseData.force *= 1f;
            projectile.gameObject.GetOrAddComponent<DeskFanBlowey>();
            projectile.sprite.renderer.enabled = false;
            projectile.baseData.range *= 0.1f;


            projectile.transform.parent = gun.barrelOffset;


            gun.quality = PickupObject.ItemQuality.D;
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            DeskFanID = gun.PickupObjectId;

            Projectile projectile2 = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(520) as Gun).DefaultModule.projectiles[0]);
            projectile2.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile2.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile2);
            projectile2.baseData.damage = 10;
            DeskFanBlowey blowey = projectile2.gameObject.GetOrAddComponent<DeskFanBlowey>();
            blowey.deleteSelf = false;
            overrideGustyProj = projectile2;
        }
        public static int DeskFanID;
        public static Projectile overrideGustyProj;

        public override Projectile OnPreFireProjectileModifier(Gun gun, Projectile projectile, ProjectileModule mod)
        {
            if (gun.CurrentOwner is PlayerController)
            {
                if ((gun.CurrentOwner as PlayerController).PlayerHasActiveSynergy("Fresh Air") && (UnityEngine.Random.value < 0.1f))
                {
                    return overrideGustyProj;
                }
                else
                {
                    return projectile;
                }
            }
            else
            {
                return projectile;
            }
        }
        public DeskFan()
        {

        }
    }
    public class DeskFanBlowey : MonoBehaviour
    {
        public DeskFanBlowey()
        {
            this.deleteSelf = true;
            this.damageToDeal = 1;
        }
        private void Start()
        {
            self = base.GetComponent<Projectile>();
            StartCoroutine(DoBlowey());
        }
        private IEnumerator DoBlowey()
        {
            yield return null;
            RoomHandler room = self.GetAbsoluteRoom();
            if (room != null)
            {
                List<AIActor> enemies = room.GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
                for (int i = 0; i < enemies.Count; i++)
                {
                    AIActor enemy = enemies[i];
                    if (enemy && enemy.healthHaver && enemy.healthHaver.IsAlive)
                    {
                        if (enemy.sprite.WorldCenter.PositionBetweenRelativeValidAngles(self.specRigidbody.UnitCenter, self.Direction.ToAngle(), 1000000, 45f))
                        {
                            Vector2 dir = (enemy.sprite.WorldCenter - self.specRigidbody.UnitCenter).normalized;
                            float knockback = 10;
                            //Calculate Damage

                            float dmg = damageToDeal;
                            if (deleteSelf) dmg *= self.baseData.damage;
                            else dmg *= (self.baseData.damage / 10);
                            if (self.ProjectilePlayerOwner())
                            {
                                //dmg *= self.ProjectilePlayerOwner().stats.GetStatValue(PlayerStats.StatType.Damage);
                                knockback *= self.ProjectilePlayerOwner().stats.GetStatValue(PlayerStats.StatType.KnockbackMultiplier);
                                if (enemy.healthHaver.IsBoss) dmg *= self.ProjectilePlayerOwner().stats.GetStatValue(PlayerStats.StatType.DamageToBosses);
                                if (enemy.aiActor.IsBlackPhantom) dmg *= self.BlackPhantomDamageMultiplier;                                
                            }
                            enemy.healthHaver.ApplyDamage(dmg, (dir * -1), "Blowie");


                            if (enemy.knockbackDoer) enemy.knockbackDoer.ApplyKnockback(dir, knockback, false);
                            List<GameActorEffect> effects = self.GetFullListOfStatusEffects();
                            if (effects.Count > 0) foreach (GameActorEffect effect in effects) enemy.ApplyEffect(effect);
                            if (enemy.behaviorSpeculator && self.AppliesStun && UnityEngine.Random.value <= self.StunApplyChance) enemy.behaviorSpeculator.Stun(self.AppliedStunDuration);
                        }
                    }
                }
            }
            if (deleteSelf) UnityEngine.Object.Destroy(self.gameObject);
            yield break;
        }
        private Projectile self;
        public bool deleteSelf;
        public float damageToDeal;
    }
}