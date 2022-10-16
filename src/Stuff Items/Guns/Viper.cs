using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using ItemAPI;

namespace NevernamedsItems
{
    public class Viper : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Viper", "viper");
            Game.Items.Rename("outdated_gun_mods:viper", "nn:viper");
      var behav =      gun.gameObject.AddComponent<Viper>();
            behav.overrideNormalFireAudio = "Play_ENM_snake_shot_01";
            behav.preventNormalFireAudio = true;
            gun.SetShortDescription("Death Throws");
            gun.SetLongDescription("A futuristic plasma blaster modeled after a historic flintlock." + "\n\nIt's initial bite has a deadly followup.");

            gun.SetupSprite(null, "viper_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 10);

            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(32) as Gun).gunSwitchGroup;
            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.DefaultModule.cooldownTime = 0.3f;
            gun.DefaultModule.angleVariance = 5;
            gun.DefaultModule.numberOfShotsInClip = 4;
            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(334) as Gun).muzzleFlashEffects;
            gun.barrelOffset.transform.localPosition = new Vector3(1.81f, 0.43f, 0f);
            gun.SetBaseMaxAmmo(200);
            gun.ammo = 200;
            gun.gunClass = GunClass.PISTOL;
            //SECONDARY BULLETS
            Projectile secondaryProj = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(86) as Gun).DefaultModule.projectiles[0]);
            secondaryProj.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(secondaryProj.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(secondaryProj);
            secondaryProj.baseData.damage *= 1.2f;
            secondaryProj.baseData.force *= 2f;
            secondaryProj.SetProjectileSpriteRight("vipersecondary_projectile", 6, 6, true, tk2dBaseSprite.Anchor.MiddleCenter, 4, 4);


            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            projectile.baseData.damage *= 2f;
            projectile.baseData.force *= 1f;
            projectile.baseData.speed *= 2f;
            projectile.baseData.range *= 2f;
            ViperProjModifier mod = projectile.gameObject.GetOrAddComponent<ViperProjModifier>();
            mod.projToSpawn = secondaryProj;
            PierceProjModifier pierce = projectile.gameObject.GetOrAddComponent<PierceProjModifier>();
            pierce.penetration++;
            pierce.penetratesBreakables = true;
            projectile.SetProjectileSpriteRight("vipermain_projectile", 15, 10, true, tk2dBaseSprite.Anchor.MiddleCenter, 11, 6);

            gun.DefaultModule.projectiles[0] = projectile;

            gun.quality = PickupObject.ItemQuality.B;
            ETGMod.Databases.Items.Add(gun, false, "ANY");
        }
        public Viper()
        {

        }
        public override void PostProcessProjectile(Projectile projectile)
        {
            if (projectile.Owner && projectile.Owner is PlayerController && (projectile.Owner as PlayerController).PlayerHasActiveSynergy("Lightning Fast Strike"))
            {
                ViperProjModifier mod = projectile.GetComponent<ViperProjModifier>();
                if (mod != null)
                {
                    mod.DistanceBetweenPositions = 1;
                }
            }
            base.PostProcessProjectile(projectile);
        }
    }
    public class ViperProjModifier : MonoBehaviour
    {
        public ViperProjModifier()
        {
            this.DistanceBetweenPositions = 2;
        }
        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            if (this.m_projectile.Owner && this.m_projectile.Owner is PlayerController) this.Owner = this.m_projectile.Owner as PlayerController;
            this.m_projectile.OnDestruction += this.Death;
        }
        private List<Vector3> cachedPositions = new List<Vector3>();
        private float LastCheckedDistance = 0;
        public int DistanceBetweenPositions;
        private void Update()
        {
            if (this.m_projectile && this.m_projectile.GetElapsedDistance() > LastCheckedDistance + DistanceBetweenPositions)
            {
                cachedPositions.Add(new Vector3(m_projectile.transform.position.x, m_projectile.transform.position.y, m_projectile.Direction.ToAngle()));
                LastCheckedDistance = this.m_projectile.GetElapsedDistance();
            }

        }
        private void Death(Projectile bullet)
        {
            if (cachedPositions.Count > 0)
            {
                GameManager.Instance.Dungeon.StartCoroutine(HandleDeathSpawns());
            }
        }
        private IEnumerator HandleDeathSpawns()
        {
            bool isLeft = true;
            foreach (Vector3 vector in cachedPositions)
            {
                float rotation = vector.z;
                if (isLeft) { rotation -= 90; isLeft = false; }
                else { rotation += 90; isLeft = true; }
                UnityEngine.Object.Instantiate<GameObject>(EasyVFXDatabase.RedLaserCircleVFX, new Vector3(vector.x, vector.y), Quaternion.identity);
                GameObject obj = SpawnManager.SpawnProjectile(projToSpawn.gameObject, new Vector3(vector.x, vector.y, 0), Quaternion.Euler(0, 0, rotation));
                Projectile component = obj.GetComponent<Projectile>();
                if (component != null)
                {
                    component.Owner = Owner;
                    component.Shooter = Owner.specRigidbody;
                    component.collidesWithPlayer = false;

                    component.baseData.damage *= Owner.stats.GetStatValue(PlayerStats.StatType.Damage);
                    component.baseData.range *= Owner.stats.GetStatValue(PlayerStats.StatType.RangeMultiplier);
                    component.baseData.force *= Owner.stats.GetStatValue(PlayerStats.StatType.KnockbackMultiplier);
                    component.baseData.speed *= Owner.stats.GetStatValue(PlayerStats.StatType.ProjectileSpeed);
                    component.AdditionalScaleMultiplier *= Owner.stats.GetStatValue(PlayerStats.StatType.PlayerBulletScale);
                    Owner.DoPostProcessProjectile(component);

                    if (Owner.PlayerHasActiveSynergy("Sniper Viper"))
                    {
                        component.baseData.speed *= 2;
                        PierceProjModifier piercing = component.gameObject.GetOrAddComponent<PierceProjModifier>();
                        piercing.penetration++;
                        piercing.penetratesBreakables = true;
                    }
                }
                yield return null;
            }
            yield break;
        }

        private Projectile m_projectile;
        private PlayerController Owner;
        public Projectile projToSpawn;
    }
}

