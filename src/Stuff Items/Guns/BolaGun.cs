using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using ItemAPI;
using SaveAPI;
using System.Reflection;
using Alexandria.Misc;

namespace NevernamedsItems
{
    public class BolaGun : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Bola Gun", "bolagun");
            Game.Items.Rename("outdated_gun_mods:bola_gun", "nn:bola_gun");
            var behav = gun.gameObject.AddComponent<BolaGun>();

            gun.SetShortDescription("Deathly Strands");
            gun.SetLongDescription("Fires swinging bolas connected by frazzling energy beams."+"\n\nThe wide swing of the bolas renders the weapon ineffective in narrow corridors.");

            gun.SetupSprite(null, "bolagun_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 12);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(150) as Gun).gunSwitchGroup;
            gun.gunClass = GunClass.RIFLE;
            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1.2f;
            gun.DefaultModule.cooldownTime = 0.3f;
            //gun.muzzleFlashEffects.type = VFXPoolType.None;
            gun.DefaultModule.numberOfShotsInClip = 7;
            gun.barrelOffset.transform.localPosition = new Vector3(2.56f, 0.81f, 0f);
            gun.SetBaseMaxAmmo(210);
            gun.carryPixelOffset = new IntVector2(10, -3);
            //Bola Stats
            Projectile projectile2 = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            projectile2.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile2.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile2);
            projectile2.baseData.speed *= 1f;
            projectile2.baseData.range *= 1f;
            projectile2.baseData.damage *= 2f;
            projectile2.SetProjectileSpriteRight("bolagun_projectile", 9, 9, true, tk2dBaseSprite.Anchor.MiddleCenter, 7, 7);

            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage = 7f;
            projectile.baseData.speed *= 0.5f;
            projectile.baseData.range *= 10f;

            BolaControlla controlla = projectile.gameObject.AddComponent<BolaControlla>();
            controlla.bolaPrefab = projectile2.gameObject;
            projectile.sprite.renderer.enabled = false;
            NoCollideBehaviour noCollide = projectile.gameObject.AddComponent<NoCollideBehaviour>();
            noCollide.worksOnEnemies = true;
            noCollide.worksOnProjectiles = true;

            projectile.transform.parent = gun.barrelOffset;

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("Bola Gun Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/bolagun_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/bolagun_clipempty");

            gun.quality = PickupObject.ItemQuality.A;
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            BolaGunID = gun.PickupObjectId;

            LinkVFX = FakePrefab.Clone(Game.Items["shock_rounds"].GetComponent<ComplexProjectileModifier>().ChainLightningVFX);
        }
        public static int BolaGunID;
        public static GameObject LinkVFX;
        public BolaGun()
        {

        }
    }
    public class BolaControlla : MonoBehaviour
    {

        public BolaControlla()
        {
            this.LinkVFXPrefab = BolaGun.LinkVFX;
        }
        private void OnDestroy()
        {
            Uncouple();
        }

        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            if (m_projectile.ProjectilePlayerOwner()) bolaOwna = m_projectile.ProjectilePlayerOwner();
            if (bolaProjectileA == null || bolaProjectileB == null)
            {
                bool invert = false;
                if (UnityEngine.Random.value <= 0.5f) invert = true;
                bolaProjectileA = CreateBolaProjectile(false, invert);
                bolaProjectileB = CreateBolaProjectile(true, invert);
            }
        }
        private IEnumerator LerpToMaxRadius(Projectile proj)
        {
            if (!proj || proj.OverrideMotionModule == null) yield break;
            if (proj.OverrideMotionModule is OrbitProjectileMotionModule)
            {
                OrbitProjectileMotionModule motionMod = proj.OverrideMotionModule as OrbitProjectileMotionModule;

                float elapsed = 0f;
                float duration = 0.5f;
                while (elapsed < duration)
                {
                    elapsed += m_projectile.LocalDeltaTime;
                    float t = elapsed / duration;
                    float currentRadius = Mathf.Lerp(0.1f, 3, t);

                    FieldInfo field = typeof(OrbitProjectileMotionModule).GetField("m_radius", BindingFlags.Instance | BindingFlags.NonPublic);
                    field.SetValue(motionMod, currentRadius);
                    yield return null;
                }
            }
            yield break;
        }
        private void Update()
        {
            if (this.LinkVFXPrefab == null)
            {
                this.LinkVFXPrefab = FakePrefab.Clone(Game.Items["shock_rounds"].GetComponent<ComplexProjectileModifier>().ChainLightningVFX);
            }
            if (bolaProjectileA && bolaProjectileB && this.extantLink == null)
            {
                tk2dTiledSprite component = SpawnManager.SpawnVFX(this.LinkVFXPrefab, false).GetComponent<tk2dTiledSprite>();
                this.extantLink = component;
            }

            if (bolaProjectileA == null || bolaProjectileB == null)
            {
                Uncouple();
                m_projectile.DieInAir();
            }

        }
        private void Uncouple()
        {
            if (extantLink != null)
            {
                SpawnManager.Despawn(extantLink.gameObject);
                extantLink = null;
            }
            if (bolaProjectileA)
            {
                if (bolaProjectileA.baseData.speed < 0)
                {
                    bolaProjectileA.baseData.speed *= -1;
                    bolaProjectileA.UpdateSpeed();
                }
                bolaProjectileA.OverrideMotionModule = null;
                BulletLifeTimer timer = bolaProjectileA.gameObject.AddComponent<BulletLifeTimer>();
                timer.secondsTillDeath = 30;
            }
            if (bolaProjectileB)
            {
                if (bolaProjectileB.baseData.speed < 0)
                {
                    bolaProjectileB.baseData.speed *= -1;
                    bolaProjectileB.UpdateSpeed();
                }
                bolaProjectileB.OverrideMotionModule = null;
                BulletLifeTimer timer = bolaProjectileB.gameObject.AddComponent<BulletLifeTimer>();
                timer.secondsTillDeath = 30;
            }
        }
        private void FixedUpdate()
        {
            if (bolaProjectileA && bolaProjectileB && this.extantLink != null)
            {
                UpdateLink(this.extantLink);
            }
        }
        private void UpdateLink(tk2dTiledSprite m_extantLink)
        {
            Vector2 unitCenter = bolaProjectileA.specRigidbody.UnitCenter;
            Vector2 unitCenter2 = bolaProjectileB.specRigidbody.UnitCenter;
            m_extantLink.transform.position = unitCenter;
            Vector2 vector = unitCenter2 - unitCenter;
            float num = BraveMathCollege.Atan2Degrees(vector.normalized);
            int num2 = Mathf.RoundToInt(vector.magnitude / 0.0625f);
            m_extantLink.dimensions = new Vector2((float)num2, m_extantLink.dimensions.y);
            m_extantLink.transform.rotation = Quaternion.Euler(0f, 0f, num);
            m_extantLink.UpdateZDepth();
            this.ApplyLinearDamage(unitCenter, unitCenter2);
        }
        private void ApplyLinearDamage(Vector2 p1, Vector2 p2)
        {
            float num = 1;
            for (int i = 0; i < StaticReferenceManager.AllEnemies.Count; i++)
            {
                AIActor aiactor = StaticReferenceManager.AllEnemies[i];
                if (aiactor && aiactor.HasBeenEngaged && aiactor.IsNormalEnemy && aiactor.specRigidbody)
                {
                    Vector2 zero = Vector2.zero;
                    if (BraveUtility.LineIntersectsAABB(p1, p2, aiactor.specRigidbody.HitboxPixelCollider.UnitBottomLeft, aiactor.specRigidbody.HitboxPixelCollider.UnitDimensions, out zero))
                    {
                        aiactor.healthHaver.ApplyDamage(num, Vector2.zero, "Bola Gun", CoreDamageTypes.Electric, DamageCategory.Normal, false, null, false);

                    }
                }
            }
        }
        private Projectile CreateBolaProjectile(bool isB = false, bool invert = false)
        {
            GameObject gameObject = SpawnManager.SpawnProjectile(bolaPrefab, m_projectile.specRigidbody.UnitCenter, Quaternion.Euler(0f, 0f, 0f), true);
            Projectile component = gameObject.GetComponent<Projectile>();
            if (component != null)
            {
                component.Owner = bolaOwna;
                component.Shooter = bolaOwna.specRigidbody;
                component.baseData.damage *= bolaOwna.stats.GetStatValue(PlayerStats.StatType.Damage);
                component.baseData.speed *= bolaOwna.stats.GetStatValue(PlayerStats.StatType.ProjectileSpeed);
                component.baseData.force *= bolaOwna.stats.GetStatValue(PlayerStats.StatType.KnockbackMultiplier);
                component.UpdateSpeed();

                if (invert)
                {
                    component.baseData.speed *= -1;
                    component.UpdateSpeed();
                }
                component.specRigidbody.CollideWithTileMap = false;
                component.pierceMinorBreakables = true;
                PierceProjModifier pierce = component.gameObject.GetOrAddComponent<PierceProjModifier>();
                pierce.penetration++;
                OrbitProjectileMotionModule orbitProjectileMotionModule = new OrbitProjectileMotionModule();

                orbitProjectileMotionModule.lifespan = 50;
                orbitProjectileMotionModule.MinRadius = 0.1f;
                orbitProjectileMotionModule.MaxRadius = 0.1f;
                orbitProjectileMotionModule.usesAlternateOrbitTarget = true;
                orbitProjectileMotionModule.OrbitGroup = -6;
                orbitProjectileMotionModule.alternateOrbitTarget = m_projectile.specRigidbody;
                if (isB) component.transform.localRotation = Quaternion.Euler(0f, 0f, component.transform.localRotation.z + 180);

                bolaOwna.DoPostProcessProjectile(component);

                if (component.OverrideMotionModule != null && component.OverrideMotionModule is HelixProjectileMotionModule)
                {
                    orbitProjectileMotionModule.StackHelix = true;
                    orbitProjectileMotionModule.ForceInvert = (component.OverrideMotionModule as HelixProjectileMotionModule).ForceInvert;
                }
                component.OverrideMotionModule = orbitProjectileMotionModule;
                StartCoroutine(LerpToMaxRadius(component));
                StartCoroutine(MakeProjectileSolid(component));
                return component;
            }
            return null;
        }
        private IEnumerator MakeProjectileSolid(Projectile projectile)
        {
            yield return new WaitForSeconds(0.2f);
            projectile.specRigidbody.CollideWithTileMap = true;
            projectile.specRigidbody.Reinitialize();
            yield break;
        }
        public GameObject bolaPrefab;
        public GameObject LinkVFXPrefab;


        private tk2dTiledSprite extantLink;
        private PlayerController bolaOwna;
        private Projectile m_projectile;
        private Projectile bolaProjectileA;
        private Projectile bolaProjectileB;
    }
}