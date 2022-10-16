using Brave.BulletScript;
using Gungeon;
using ItemAPI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnemyAPI;
using UnityEngine;
using Alexandria.Misc;

namespace NevernamedsItems
{
    public class GunjurerSlamPlayerScript : Script
    {
        public float aimDirection;
        public float overrideSpeed = 10;

        public override IEnumerator Top()
        {
            base.EndOnBlank = false;
            switch (UnityEngine.Random.Range(0, 4))
            {
                case 0:
                    this.FireX();
                    break;
                case 1:
                    this.FireSquare();
                    break;
                case 2:
                    this.FireTriangle();
                    break;
                case 3:
                    this.FireCircle();
                    break;
            }
            yield break;
        }
        private void FireX()
        {
            Vector2 start = new Vector2(2f, 0f).Rotate(45f);
            Vector2 start2 = new Vector2(2f, 0f).Rotate(135f);
            Vector2 end = new Vector2(2f, 0f).Rotate(225f);
            Vector2 end2 = new Vector2(2f, 0f).Rotate(-45f);
            this.FireExpandingLine(start, end, 11);
            this.FireExpandingLine(start2, end2, 11);
        }
        private void FireSquare()
        {
            Vector2 vector = new Vector2(2f, 0f).Rotate(45f);
            Vector2 vector2 = new Vector2(2f, 0f).Rotate(135f);
            Vector2 vector3 = new Vector2(2f, 0f).Rotate(225f);
            Vector2 vector4 = new Vector2(2f, 0f).Rotate(-45f);
            this.FireExpandingLine(vector, vector2, 9);
            this.FireExpandingLine(vector2, vector3, 9);
            this.FireExpandingLine(vector3, vector4, 9);
            this.FireExpandingLine(vector4, vector, 9);
        }
        private void FireTriangle()
        {
            Vector2 vector = new Vector2(2f, 0f).Rotate(90f);
            Vector2 vector2 = new Vector2(2f, 0f).Rotate(210f);
            Vector2 vector3 = new Vector2(2f, 0f).Rotate(330f);
            this.FireExpandingLine(vector, vector2, 10);
            this.FireExpandingLine(vector2, vector3, 10);
            this.FireExpandingLine(vector3, vector, 10);
        }
        private void FireCircle()
        {
            for (int i = 0; i < 36; i++)
            {
                base.Fire(new GunjurerSlamPlayerScript.ExpandingBullet(this, new Vector2(2f, 0f).Rotate((float)i / 35f * 360f)));
            }
        }
        private void FireExpandingLine(Vector2 start, Vector2 end, int numBullets)
        {
            for (int i = 0; i < numBullets; i++)
            {
                base.Fire(new GunjurerSlamPlayerScript.ExpandingBullet(this, Vector2.Lerp(start, end, (float)i / ((float)numBullets - 1f))));
            }
        }
        public const float Radius = 2f;
        public const int GrowTime = 15;
        public const float RotationSpeed = 180f;
        public const float BulletSpeed = 10f;
        public class ExpandingBullet : Bullet
        {
            public ExpandingBullet(GunjurerSlamPlayerScript parent, Vector2 offset) : base(null, false, false, false)
            {
                this.m_parent = parent;
                this.m_offset = offset;
            }
            public override IEnumerator Top()
            {

                base.ManualControl = true;
                Vector2 centerPosition = base.Position;
                for (int i = 0; i < 15; i++)
                {
                    base.UpdateVelocity();
                    centerPosition += this.Velocity / 60f;
                    Vector2 actualOffset = Vector2.Lerp(Vector2.zero, this.m_offset, (float)i / 14f);
                    actualOffset = actualOffset.Rotate(3f * (float)i);
                    base.Position = centerPosition + actualOffset;
                    yield return base.Wait(1);
                }
                this.Direction = this.m_parent.aimDirection;
                this.Speed = 10;//this.m_parent.overrideSpeed;
                for (int j = 0; j < 300; j++)
                {
                    base.UpdateVelocity();
                    centerPosition += this.Velocity / 60f;
                    base.Position = centerPosition + this.m_offset.Rotate(3f * (float)(15 + j));
                    yield return base.Wait(1);
                }
                base.Vanish(false);
                yield break;
            }

            private GunjurerSlamPlayerScript m_parent;
            private Vector2 m_offset;
        }
    }

    public class SpawnGunjurerBulletScriptOnSpawn : MonoBehaviour
    {
        public SpawnGunjurerBulletScriptOnSpawn()
        {
        }
        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            BulletScriptSource source = this.m_projectile.gameObject.GetOrAddComponent<BulletScriptSource>();
            this.m_projectile.gameObject.AddComponent<BulletSourceKiller>();
            var bulletScriptSelected = new CustomBulletScriptSelector(typeof(GunjurerSlamPlayerScript));
            AIBulletBank bulletBank = DataCloners.CopyAIBulletBank(EnemyDatabase.GetOrLoadByGuid("206405acad4d4c33aac6717d184dc8d4").bulletBank);
            bulletBank.OnProjectileCreated += this.OnBulletSpawned;
            foreach (AIBulletBank.Entry bullet in bulletBank.Bullets)
            {
                bullet.BulletObject.GetComponent<Projectile>().BulletScriptSettings.preventPooling = true;
            }
            source.BulletManager = bulletBank;
            source.BulletScript = bulletScriptSelected;
            source.Initialize();//to fire the script once
            GunjurerSlamPlayerScript spawnedScript = source.RootBullet as GunjurerSlamPlayerScript;
            spawnedScript.aimDirection = this.m_projectile.Direction.ToAngle();
            /*if (this.m_projectile.ProjectilePlayerOwner() != null)
            {
                spawnedScript.overrideSpeed *= this.m_projectile.ProjectilePlayerOwner().stats.GetStatValue(PlayerStats.StatType.ProjectileSpeed);
            }*/
        }
        private void OnBulletSpawned(Projectile projectile)
        {
            if (projectile)
            {
                projectile.collidesWithPlayer = false;
                projectile.collidesWithEnemies = true;
                projectile.UpdateCollisionMask();
                if (this.m_projectile && this.m_projectile.Owner != null)
                {
                    projectile.Owner = this.m_projectile.Owner;
                    if (this.m_projectile.ProjectilePlayerOwner() != null)
                    {
                        PlayerController player = this.m_projectile.ProjectilePlayerOwner();

                        projectile.AdjustPlayerProjectileTint(ExtendedColours.honeyYellow, 1);

                        projectile.baseData.damage = 3;
                        projectile.baseData.damage *= player.stats.GetStatValue(PlayerStats.StatType.Damage);
                        projectile.baseData.speed *= player.stats.GetStatValue(PlayerStats.StatType.ProjectileSpeed);
                        projectile.baseData.range *= player.stats.GetStatValue(PlayerStats.StatType.RangeMultiplier);
                        projectile.BossDamageMultiplier *= player.stats.GetStatValue(PlayerStats.StatType.DamageToBosses);
                        projectile.RuntimeUpdateScale(player.stats.GetStatValue(PlayerStats.StatType.PlayerBulletScale));
                        projectile.UpdateSpeed();
                        player.DoPostProcessProjectile(projectile);
                    }
                }
            }
        }
        private Projectile m_projectile;
    }
    public class GunjurersStaff : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Gunjurer's Staff", "gunjurerswand");
            Game.Items.Rename("outdated_gun_mods:gunjurer's_staff", "nn:gunjurers_staff");
            var behav = gun.gameObject.AddComponent<GunjurersStaff>();
            behav.preventNormalFireAudio = true;
            behav.preventNormalReloadAudio = true;
            behav.overrideNormalReloadAudio = "Play_ENM_wizardred_chant_01";
            gun.SetShortDescription("Do you believe in magic?");
            gun.SetLongDescription("The lost wand of an Apprentice Gunjurer, cruelly slain by a Gungeoneer while out of his mentor's sight for but a moment...");

            gun.SetupSprite(null, "gunjurerswand_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 10);
            gun.SetAnimationFPS(gun.chargeAnimation, 4);

            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(35) as Gun).gunSwitchGroup;
            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);

            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.chargeAnimation).wrapMode = tk2dSpriteAnimationClip.WrapMode.LoopSection;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.chargeAnimation).loopStart = 2;

            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).frames[0].eventAudio = "Play_ENM_wizardred_shoot_01";
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).frames[0].triggerEvent = true;

            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.chargeAnimation).frames[0].eventAudio = "Play_ENM_wizard_charge_01";
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.chargeAnimation).frames[0].triggerEvent = true;

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Charged;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Ordered;
            gun.muzzleFlashEffects.type = VFXPoolType.None;
            gun.reloadTime = 1.5f;
            gun.DefaultModule.cooldownTime = 0.5f;
            gun.DefaultModule.numberOfShotsInClip = 3;
            gun.barrelOffset.transform.localPosition = new Vector3(1.31f, 0.5f, 0f);
            gun.SetBaseMaxAmmo(45);
            gun.ammo = 45;
            gun.gunClass = GunClass.CHARGE;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            projectile.baseData.damage = 0f;
            projectile.baseData.force = 0f;
            projectile.baseData.speed = 0.001f;
            NoCollideBehaviour nocollide = projectile.gameObject.AddComponent<NoCollideBehaviour>();
            nocollide.worksOnEnemies = true;
            nocollide.worksOnProjectiles = true;
            projectile.sprite.renderer.enabled = false;
            BulletLifeTimer timer = projectile.gameObject.AddComponent<BulletLifeTimer>();
            timer.eraseInsteadOfDie = true;
            timer.secondsTillDeath = 1.5f;
            projectile.specRigidbody.CollideWithTileMap = false;

            SpawnGunjurerBulletScriptOnSpawn script = projectile.gameObject.AddComponent<SpawnGunjurerBulletScriptOnSpawn>();

            ProjectileModule.ChargeProjectile chargeProj = new ProjectileModule.ChargeProjectile
            {
                Projectile = projectile,
                ChargeTime = 0.8f,
            };
            gun.DefaultModule.chargeProjectiles = new List<ProjectileModule.ChargeProjectile> { chargeProj };

            gun.quality = PickupObject.ItemQuality.A; //B
            ETGMod.Databases.Items.Add(gun, null, "ANY");
            GunjurersStaffID = gun.PickupObjectId;
        }
        public static int GunjurersStaffID;
        public GunjurersStaff()
        {

        }
    }
}
