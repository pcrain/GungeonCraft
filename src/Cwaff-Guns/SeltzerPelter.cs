using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using UnityEngine;
using MonoMod.RuntimeDetour;

using Gungeon;
using Dungeonator;
using Alexandria.ItemAPI;
using Alexandria.Misc;

namespace CwaffingTheGungy
{
    public class SeltzerPelter : AdvancedGunBehavior
    {
        public static string ItemName         = "Seltzer Pelter";
        public static string SpriteName       = "seltzer_pelter";
        public static string ProjectileName   = "38_special";
        public static string ShortDescription = "TBD";
        public static string LongDescription  = "TBD";

        internal static tk2dSpriteAnimationClip _BulletSprite;

        public static void Add()
        {
            Gun gun = Lazy.SetupGun<SeltzerPelter>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription);
                gun.SetAttributes(quality: PickupObject.ItemQuality.B, gunClass: GunClass.CHARGE, reloadTime: 1.2f, ammo: 800);

            ProjectileModule mod = gun.DefaultModule;
                mod.ammoCost            = 1;
                mod.shootStyle          = ProjectileModule.ShootStyle.SemiAutomatic;
                mod.sequenceStyle       = ProjectileModule.ProjectileSequenceStyle.Random;
                mod.cooldownTime        = 0.75f;
                mod.numberOfShotsInClip = 4;

            _BulletSprite = AnimateBullet.CreateProjectileAnimation(
                ResMap.Get("can_projectile_a").Base(),
                12, true, new IntVector2(16, 12),
                false, tk2dBaseSprite.Anchor.MiddleCenter,
                anchorsChangeColliders: false/*true*/,
                fixesScales: true,
                overrideColliderPixelSize: new IntVector2(2, 2) // prevent uneven colliders from glitching into walls
                );

            Projectile projectile = Lazy.PrefabProjectileFromGun(gun);
                projectile.AddDefaultAnimation(_BulletSprite);
                projectile.transform.parent = gun.barrelOffset;
                projectile.baseData.range = 999f;
                // projectile.shouldRotate = true;
                projectile.gameObject.AddComponent<SeltzerProjectile>();
        }
    }

    public class SeltzerProjectile : MonoBehaviour
    {
        private Projectile _projectile;
        private PlayerController _owner;
        private BounceProjModifier  _bounce        = null;
        private BasicBeamController _beam = null;
        private float _rotationRate = 0f;

        private void Start()
        {
            this._projectile = base.GetComponent<Projectile>();
            this._owner = this._projectile.Owner as PlayerController;
            this._projectile.DestroyMode = Projectile.ProjectileDestroyMode.BecomeDebris;
            this._projectile.shouldRotate = false; // prevent automatic rotation after creation

            this._bounce = this._projectile.gameObject.GetOrAddComponent<BounceProjModifier>();
                this._bounce.numberOfBounces     = 9999;
                this._bounce.chanceToDieOnBounce = 0f;
                this._bounce.onlyBounceOffTiles  = false;
                this._bounce.ExplodeOnEnemyBounce = false;
                this._bounce.bouncesTrackEnemies = true;
                this._bounce.bounceTrackRadius = 3f;
                this._bounce.OnBounce += this.StartSprayingSoda;
        }

        private void StartSprayingSoda()
        {
            this._bounce.OnBounce -= this.StartSprayingSoda;
            this._bounce.OnBounce += this.DisconnectBeamOnBounce;
            this._projectile.baseData.speed *= 0.5f;
            this._projectile.UpdateSpeed();

            // From FreeFireBeam()
            GameObject theBeamPrefab = (ItemHelper.Get(Items.MegaDouser) as Gun).DefaultModule.projectiles[0].gameObject;
            GameObject theBeamObject = SpawnManager.SpawnProjectile(theBeamPrefab, this._projectile.sprite.WorldCenter, Quaternion.identity);

            Projectile proj = theBeamObject.GetComponent<Projectile>();
                proj.Owner = this._owner;
                proj.baseData.range = 3f;
                proj.baseData.speed = 20f;
                // proj.baseData.speed = 20f;

            this._beam = theBeamObject.GetComponent<BasicBeamController>();
                this._beam.chargeDelay     = 0f;
                this._beam.usesChargeDelay = false;
                this._beam.HitsPlayers     = false;
                this._beam.HitsEnemies     = true;
                this._beam.Owner           = this._owner;
                this._beam.Origin          = this._projectile.sprite.WorldCenter;
                this._beam.Direction       = -this._projectile.sprite.transform.rotation.z.ToVector();
                this._beam.boneType        = BasicBeamController.BeamBoneType.Projectile;
                // this._beam.boneType        = BasicBeamController.BeamBoneType.Straight;
                // this._beam.TileType        = BasicBeamController.BeamTileType.GrowAtBeginning;
                this._beam.TileType        = BasicBeamController.BeamTileType.Flowing;
                this._beam.endType         = BasicBeamController.BeamEndType.Persist;
                // this._beam.endType         = BasicBeamController.BeamEndType.Vanish;
                // this._beam.endType         = BasicBeamController.BeamEndType.Dissipate; // doesn't work great without dissipate animation
                // this._beam.dissipateTime   = 0.5f;

            this._projectile.OnDestruction += this.DestroyBeam;
            this._beam.StartCoroutine(SpraySoda_CR(this, this._beam, this._projectile));
        }

        private void DisconnectBeamOnBounce()
        {
            this._beam.SeparateBeam(this._beam.m_bones.First, this._beam.Origin, this._beam.m_bones.First.Value.PosX);
            UpdateRotationRate();
        }

        private void UpdateRotationRate()
        {
            this._rotationRate = UnityEngine.Random.Range(-5f, 5f);
        }

        private void DestroyBeam(Projectile p)
        {
            this._beam?.CeaseAttack();
        }

        private const float SPRAY_TIME = 3f;
        private const float SPIN_TIME  = 5f;
        private const float ACCEL      = 50f;
        private const float _AIR_DRAG  = 0.20f;
        private static IEnumerator SpraySoda_CR(SeltzerProjectile seltzer, BasicBeamController beam, Projectile p)
        {
            yield return null;
            float startAngle = p.LastVelocity.ToAngle();
            float curAngle = startAngle;
            seltzer.UpdateRotationRate();

            #region The Ballistics
                for (float elapsed = 0f; elapsed < SPRAY_TIME; elapsed += BraveTime.DeltaTime)
                {
                    if (!p.isActiveAndEnabled || p.HasDiedInAir)
                        break;
                    Vector2 oldSpeed = p.LastVelocity;
                    curAngle += seltzer._rotationRate;
                    Vector2 newSpeed = oldSpeed + curAngle.ToVector(ACCEL * BraveTime.DeltaTime);
                    p.baseData.speed = newSpeed.magnitude;
                    p.SendInDirection(newSpeed, false, false);
                    p.UpdateSpeed();
                    p.SetRotation(curAngle);
                    beam.Origin = p.sprite.WorldCenter;
                    // beam.Direction = -this._projectile.sprite.transform.rotation.z.ToVector();
                    beam.Direction = -curAngle.ToVector();
                    beam.LateUpdatePosition(beam.Origin);
                    yield return null;
                }
            #endregion

            #region The Rapid Spin
                float rotIncrease = 5f * Mathf.Sign(seltzer._rotationRate);
                for (float elapsed = 0f; elapsed < SPIN_TIME; elapsed += BraveTime.DeltaTime)
                {
                    if (!p.isActiveAndEnabled || p.HasDiedInAir)
                        break;
                    if (p.baseData.speed > 0.1f)
                    {
                        p.baseData.speed *= Mathf.Pow(_AIR_DRAG, BraveTime.DeltaTime);
                        p.UpdateSpeed();
                    }
                    seltzer._rotationRate += rotIncrease * BraveTime.DeltaTime;
                    curAngle += seltzer._rotationRate;
                    p.SetRotation(curAngle);
                    beam.Origin = p.sprite.WorldCenter;
                    beam.Direction = -curAngle.ToVector();
                    beam.LateUpdatePosition(beam.Origin);
                    yield return null;
                }
            #endregion

            #region Die Down
                beam.CeaseAttack();
                p?.DieInAir();
            #endregion

            yield break;
        }

        private void Update()
        {

        }
    }
}
