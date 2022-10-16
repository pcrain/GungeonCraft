using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using Dungeonator;
using Alexandria.ItemAPI;
using Alexandria.Misc;

namespace NevernamedsItems
{
    public class Ferrobolt : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Ferrobolt", "ferrobolt");
            Game.Items.Rename("outdated_gun_mods:ferrobolt", "nn:ferrobolt");
            var behav = gun.gameObject.AddComponent<Ferrobolt>();
            behav.preventNormalFireAudio = true;
            gun.SetShortDescription("Law of Attraction");
            gun.SetLongDescription("Fires alternating monopolar electromagnetic blasts."+"\n\nUpon the discovery of the first monopole, the tech was immediately weaponised.");

            gun.SetupSprite(null, "ferrobolt_idle_001", 8);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(150) as Gun).gunSwitchGroup;

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.doesScreenShake = true;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Charged;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Ordered;
            gun.reloadTime = 0.5f;
            gun.DefaultModule.cooldownTime = 0.2f;
            gun.DefaultModule.angleVariance = 0;
            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(23) as Gun).muzzleFlashEffects;

            gun.DefaultModule.numberOfShotsInClip = 8;
            gun.barrelOffset.transform.localPosition = new Vector3(35f / 16f, 7f / 16f, 0f);
            gun.SetBaseMaxAmmo(100);
            gun.ammo = 100;
            gun.gunClass = GunClass.CHARGE;
            gun.SetAnimationFPS(gun.chargeAnimation, 8);
            gun.SetAnimationFPS(gun.reloadAnimation, 10);
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.chargeAnimation).wrapMode = tk2dSpriteAnimationClip.WrapMode.LoopSection;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.chargeAnimation).loopStart = 2;

            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).frames[0].eventAudio = "Play_wpn_chargelaser_shot_01";
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).frames[0].triggerEvent = true;

            //BULLET STATS
            Projectile projectile = gun.DefaultModule.projectiles[0].gameObject.InstantiateAndFakeprefab().GetComponent<Projectile>();
            projectile.baseData.damage = 0f;
            projectile.baseData.force = 0f;

            NoCollideBehaviour nocollide = projectile.gameObject.AddComponent<NoCollideBehaviour>();
            nocollide.worksOnEnemies = true;
            nocollide.worksOnProjectiles = true;

            BounceProjModifier bounce = projectile.gameObject.AddComponent<BounceProjModifier>();
            bounce.numberOfBounces = 100;

            FerroboltOrbController orb = projectile.gameObject.AddComponent<FerroboltOrbController>();

            SlowDownOverTimeModifier slowDown = projectile.gameObject.AddComponent<SlowDownOverTimeModifier>();
            slowDown.timeToSlowOver = 0.5f;
            slowDown.killAfterCompleteStop = true;
            slowDown.timeTillKillAfterCompleteStop = 12f;

            projectile.hitEffects.overrideMidairDeathVFX = (PickupObjectDatabase.GetById(57) as Gun).DefaultModule.projectiles[0].hitEffects.overrideMidairDeathVFX;
            projectile.hitEffects.alwaysUseMidair = true;
            projectile.AnimateProjectile(new List<string> {
                "ferrobolt_orb_001",
                "ferrobolt_orb_002",
                "ferrobolt_orb_003",
                "ferrobolt_orb_004",
            }, 10, true, new List<IntVector2> {
                 new IntVector2(14, 14), //1
                 new IntVector2(14, 14), //1
                 new IntVector2(14, 14), //1
                 new IntVector2(14, 14), //1
            },
            AnimateBullet.ConstructListOfSameValues(false, 4),
            AnimateBullet.ConstructListOfSameValues(tk2dBaseSprite.Anchor.MiddleCenter, 4),
            AnimateBullet.ConstructListOfSameValues(true, 4),
            AnimateBullet.ConstructListOfSameValues(false, 4),
            AnimateBullet.ConstructListOfSameValues<Vector3?>(null, 4),
            AnimateBullet.ConstructListOfSameValues<IntVector2?>(new IntVector2(8, 8), 4),
            AnimateBullet.ConstructListOfSameValues<IntVector2?>(null, 4),
            AnimateBullet.ConstructListOfSameValues<Projectile>(null, 4));

            projectile.SetProjectileSpriteRight("ferrobolt_orb_001", 14, 14, true, tk2dBaseSprite.Anchor.MiddleCenter, 8, 8);

            ProjectileModule.ChargeProjectile chargeProj = new ProjectileModule.ChargeProjectile
            {
                Projectile = projectile,
                ChargeTime = 0.5f,
            };
            gun.DefaultModule.chargeProjectiles = new List<ProjectileModule.ChargeProjectile> { chargeProj };

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("Ferrobolt Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/ferrobolt_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/ferrobolt_clipempty");

            gun.quality = PickupObject.ItemQuality.C;
            ETGMod.Databases.Items.Add(gun, false, "ANY");
            ID = gun.PickupObjectId;


            Projectile bolt = (PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0].gameObject.InstantiateAndFakeprefab().GetComponent<Projectile>();
            bolt.baseData.damage = 30f;
            bolt.baseData.speed *= 2f;
            bolt.baseData.force *= 2f;
            PierceProjModifier piercing = bolt.gameObject.AddComponent<PierceProjModifier>();
            piercing.penetration = 100;
            bolt.pierceMinorBreakables = true;
            bolt.SetProjectileSpriteRight("ferrobolt_bolt_001", 19, 8, true, tk2dBaseSprite.Anchor.MiddleCenter, 16, 6);
            bolt.hitEffects.overrideMidairDeathVFX = (PickupObjectDatabase.GetById(57) as Gun).DefaultModule.chargeProjectiles[0].Projectile.hitEffects.overrideMidairDeathVFX;
            bolt.hitEffects.alwaysUseMidair = true;
            bolt.gameObject.AddComponent<FerroboltBoltController>();
            launchProj = bolt;
        }
        public static int ID;
        public static Projectile launchProj;
        public static bool shouldLaunchBolt;
    }
    public class FerroboltOrbController : MonoBehaviour
    {
        private Projectile self;
        private PlayerController owner;
        private void Start()
        {
            self = base.GetComponent<Projectile>();
            owner = self.ProjectilePlayerOwner();
            StartCoroutine(CheckNshit());
        }
        private IEnumerator CheckNshit()
        {
            yield return null;
            if (Ferrobolt.shouldLaunchBolt)
            {
                Ferrobolt.shouldLaunchBolt = false;
                GameObject gameObject = SpawnManager.SpawnProjectile(Ferrobolt.launchProj.gameObject, self.sprite.WorldCenter, Quaternion.Euler(0f, 0f, self.Direction.ToAngle()), true);
                Projectile component = gameObject.GetComponent<Projectile>();
                if (component != null)
                {
                    component.Owner = owner;
                    component.Shooter = owner.specRigidbody;
                    component.baseData.damage *= owner.stats.GetStatValue(PlayerStats.StatType.Damage);
                    component.baseData.force *= owner.stats.GetStatValue(PlayerStats.StatType.KnockbackMultiplier);
                    component.baseData.range *= owner.stats.GetStatValue(PlayerStats.StatType.RangeMultiplier);
                    component.baseData.speed *= owner.stats.GetStatValue(PlayerStats.StatType.ProjectileSpeed);
                    component.BossDamageMultiplier *= owner.stats.GetStatValue(PlayerStats.StatType.DamageToBosses);
                    owner.DoPostProcessProjectile(component);
                }
                UnityEngine.Object.Destroy(base.gameObject);
            }
            else
            {
                Ferrobolt.shouldLaunchBolt = true;
            }
            yield break;
        }
    }
    public class FerroboltBoltController : BraveBehaviour
    {
        private void Start()
        {
            if (!this.m_projectile)
            {
                this.m_projectile = base.GetComponent<Projectile>();
            }
            Projectile projectile = this.m_projectile;
            projectile.ModifyVelocity += this.ModifyVelocity;
        }
        private void Update()
        {
            foreach (Projectile proj in StaticReferenceManager.AllProjectiles)
            {
                if (proj && proj.Owner != null)
                {
                    if (proj.Owner is PlayerController)
                    {
                        if (proj.GetComponent<FerroboltOrbController>())
                        {
                            if (Vector2.Distance(proj.sprite.WorldCenter, m_projectile.sprite.WorldCenter) < 1)
                            {
                                ExplosionData boom = DataCloners.CopyExplosionData(StaticExplosionDatas.explosiveRoundsExplosion);
                                boom.ignoreList.Add(proj.Owner.specRigidbody);
                                Exploder.Explode(proj.sprite.WorldCenter, boom, Vector2.zero);
                                proj.DieInAir();
                                m_projectile.DieInAir();
                            }
                        }
                    }
                }
            }
        }
        public void AssignProjectile(Projectile source)
        {
            this.m_projectile = source;
        }
        private Vector2 ModifyVelocity(Vector2 inVel)
        {
            Vector2 vector = inVel;
            RoomHandler absoluteRoomFromPosition = GameManager.Instance.Dungeon.data.GetAbsoluteRoomFromPosition(this.m_projectile.LastPosition.IntXY(VectorConversions.Floor));

            List<Projectile> ValidProjectilesInRoom = new List<Projectile>();
            foreach (Projectile proj in StaticReferenceManager.AllProjectiles)
            {
                if (proj && proj.gameObject)
                {
                    RoomHandler checkProjRoom = GameManager.Instance.Dungeon.data.GetAbsoluteRoomFromPosition(proj.LastPosition.IntXY(VectorConversions.Floor));
                    if (checkProjRoom == absoluteRoomFromPosition)
                    {
                        if (proj.GetComponent<FerroboltOrbController>())
                        {
                            ValidProjectilesInRoom.Add(proj);
                        }
                    }
                }
            }

            if (ValidProjectilesInRoom == null || ValidProjectilesInRoom.Count == 0)
            {
                m_projectile.specRigidbody.CollideWithTileMap = true;
                m_projectile.UpdateCollisionMask();
                return inVel;
            }
            m_projectile.specRigidbody.CollideWithTileMap = false;
            m_projectile.UpdateCollisionMask();


            float num = float.MaxValue;
            Vector2 vector2 = Vector2.zero;
            Projectile grabbedProj = null;
            Vector2 vector3 = (!base.sprite) ? base.transform.position.XY() : base.sprite.WorldCenter;

            for (int i = 0; i < ValidProjectilesInRoom.Count; i++)
            {
                Projectile promnj = ValidProjectilesInRoom[i];
                if (promnj)
                {
                    Vector2 vector4 = promnj.sprite.WorldCenter - vector3;
                    float sqrMagnitude = vector4.sqrMagnitude;
                    if (sqrMagnitude < num)
                    {
                        vector2 = vector4;
                        num = sqrMagnitude;
                        grabbedProj = promnj;
                    }
                }
            }
            num = Mathf.Sqrt(num);

            if (num < this.HomingRadius && grabbedProj != null)
            {
                float num2 = 1f - num / this.HomingRadius;
                float num3 = vector2.ToAngle();
                float num4 = inVel.ToAngle();
                float num5 = this.AngularVelocity * num2 * this.m_projectile.LocalDeltaTime;
                float num6 = Mathf.MoveTowardsAngle(num4, num3, num5);
                if (this.m_projectile is HelixProjectile)
                {
                    float angleDiff = num6 - num4;
                    (this.m_projectile as HelixProjectile).AdjustRightVector(angleDiff);
                }
                else
                {
                    if (this.m_projectile.shouldRotate)
                    {
                        base.transform.rotation = Quaternion.Euler(0f, 0f, num6);
                    }
                    vector = BraveMathCollege.DegreesToVector(num6, inVel.magnitude);
                }
                if (this.m_projectile.OverrideMotionModule != null)
                {
                    this.m_projectile.OverrideMotionModule.AdjustRightVector(num6 - num4);
                }
            }
            if (vector == Vector2.zero || float.IsNaN(vector.x) || float.IsNaN(vector.y))
            {
                return inVel;
            }
            return vector;
        }
        public override void OnDestroy()
        {
            if (this.m_projectile)
            {
                Projectile projectile = this.m_projectile;
                projectile.ModifyVelocity -= this.ModifyVelocity;
            }
            base.OnDestroy();
        }
        public float HomingRadius = 200f;
        public float AngularVelocity = 4000f;
        protected Projectile m_projectile;
    }
}
