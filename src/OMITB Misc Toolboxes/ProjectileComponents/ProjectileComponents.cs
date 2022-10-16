using Dungeonator;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Alexandria.ItemAPI;
using Alexandria.Misc;
using System.Text;
using UnityEngine;
using Alexandria.EnemyAPI;

namespace NevernamedsItems
{
    //CUSTOM PROJECTILE COMPONENTS

    public class CollideWithPlayerBehaviour : MonoBehaviour
    {
        public CollideWithPlayerBehaviour()
        {
        }

        public void Start()
        {
            try
            {
                this.m_projectile = base.GetComponent<Projectile>();

                this.m_projectile.allowSelfShooting = true;
                this.m_projectile.collidesWithEnemies = true;
                this.m_projectile.collidesWithPlayer = true;
                this.m_projectile.SetNewShooter(this.m_projectile.Shooter);
                this.m_projectile.UpdateCollisionMask();
            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
                ETGModConsole.Log(e.StackTrace);
            }
        }
        private Projectile m_projectile;
    } //Allows the projectile to hit players.
    public class HomeInOnPlayerModifyer : BraveBehaviour
    {
        private void Start()
        {
            if (!this.m_projectile)
            {
                this.m_projectile = base.GetComponent<Projectile>();
            }
            Projectile projectile = this.m_projectile;
            if (projectile.Owner && projectile.Owner is PlayerController)
            {
                projectile.ModifyVelocity = (Func<Vector2, Vector2>)Delegate.Combine(projectile.ModifyVelocity, new Func<Vector2, Vector2>(this.ModifyVelocity));
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
            float num = float.MaxValue;
            Vector2 vector2 = Vector2.zero;

            PlayerController target = m_projectile.Owner as PlayerController;

            Vector2 vector3 = (!base.sprite) ? base.transform.position.XY() : base.sprite.WorldCenter;

            Vector2 vector4 = target.CenterPosition - vector3;
            float sqrMagnitude = vector4.sqrMagnitude;
            if (sqrMagnitude < num)
            {
                vector2 = vector4;
                num = sqrMagnitude;

            }

            num = Mathf.Sqrt(num);
            if (num < this.HomingRadius)
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
                projectile.ModifyVelocity = (Func<Vector2, Vector2>)Delegate.Remove(projectile.ModifyVelocity, new Func<Vector2, Vector2>(this.ModifyVelocity));
            }
            base.OnDestroy();
        }

        public float HomingRadius = 2f;
        public float AngularVelocity = 180f;
        protected Projectile m_projectile;
    } //Causes the projectile to home in on the Playercontroller that fired it.
    public class NoCollideBehaviour : MonoBehaviour
    {
        public NoCollideBehaviour()
        {
            worksOnProjectiles = false;
            worksOnEnemies = true;
        }

        public void Start()
        {
            try
            {
                this.m_projectile = base.GetComponent<Projectile>();
                this.m_projectile.specRigidbody.OnPreRigidbodyCollision += this.HandlePreCollision;
            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
                ETGModConsole.Log(e.StackTrace);
            }
        }

        private void HandlePreCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
        {
            try
            {
                if (otherRigidbody)
                {
                    if (otherRigidbody.aiActor != null && otherRigidbody.healthHaver != null)
                    {
                        if (worksOnEnemies)
                        {
                            PhysicsEngine.SkipCollision = true;
                        }
                    }
                    else if (otherRigidbody.projectile != null && otherRigidbody.projectile.collidesWithProjectiles)
                    {
                        if (worksOnProjectiles)
                        {
                            PhysicsEngine.SkipCollision = true;
                        }
                    }
                    else
                    {
                        PhysicsEngine.SkipCollision = true;
                    }
                }
            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
                ETGModConsole.Log(e.StackTrace);
            }
        }
        private Projectile m_projectile;

        public bool worksOnEnemies = false;
        public bool worksOnProjectiles = false;
    } //Prevents the projectile from colliding with other rigid bodies.
    public class TickDamageBehaviour : MonoBehaviour
    {
        public TickDamageBehaviour()
        {
            damageSource = "Tick Damage";
            starterDamage = 2f;
            godHelpUsTick = 0.016f;
        }

        public void Start()
        {
            try
            {
                this.m_projectile = base.GetComponent<Projectile>();
                this.m_sprite = this.m_projectile.sprite;
                if (this.m_projectile.Owner is PlayerController)
                {
                    this.owner = this.m_projectile.Owner as PlayerController;
                }
                this.m_projectile.specRigidbody.OnPreRigidbodyCollision += this.HandlePreCollision;
                this.m_projectile.OnHitEnemy += this.OnHitEnemy;
                //this.m_projectile.OnPostUpdate += this.HandlePostUpdate;

            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
                ETGModConsole.Log(e.StackTrace);
            }
        }
        private PlayerController owner;
        public List<HealthHaver> FirstStrikeEnemies = new List<HealthHaver>() { };
        private void OnHitEnemy(Projectile bullet, SpeculativeRigidbody enemy, bool fatal)
        {
            if (enemy.healthHaver)
            {
                if (!FirstStrikeEnemies.Contains(enemy.healthHaver))
                {
                    FirstStrikeEnemies.Add(enemy.healthHaver);
                }
            }
        }
        private void Update()
        {
            if (godHelpUsTick >= 0)
            {
                godHelpUsTick -= BraveTime.DeltaTime;
            }
        }
        private void HandlePreCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
        {
            try
            {
                if (otherRigidbody.healthHaver != null && FirstStrikeEnemies.Contains(otherRigidbody.healthHaver))
                {
                    PlayerController playerness = otherRigidbody.gameObject.GetComponent<PlayerController>();
                    if (playerness == null)
                    {
                        if (godHelpUsTick <= 0)
                        {
                            float damageToDeal = starterDamage;
                            damageToDeal *= this.owner.stats.GetStatValue(PlayerStats.StatType.Damage);
                            if (otherRigidbody.healthHaver.IsBoss) damageToDeal *= this.owner.stats.GetStatValue(PlayerStats.StatType.DamageToBosses);
                            if (otherRigidbody.aiActor && otherRigidbody.aiActor.IsBlackPhantom) damageToDeal *= this.m_projectile.BlackPhantomDamageMultiplier;
                            otherRigidbody.healthHaver.ApplyDamage(damageToDeal, Vector2.zero, damageSource, CoreDamageTypes.None, DamageCategory.Unstoppable, true, null, true);
                            godHelpUsTick = 0.016f;
                        }
                        PhysicsEngine.SkipCollision = true;
                    }
                }
            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
                ETGModConsole.Log(e.StackTrace);
            }
        }
        private Projectile m_projectile;
        private tk2dBaseSprite m_sprite;
        public float EffectRadius = 1f;
        private float godHelpUsTick;
        public string damageSource;
        public float starterDamage;
    } //Causes the projectile to deal tick damage to enemies it overlaps. Requires SuperPierceProjectile.
    public class RandomProjectileStatsComponent : MonoBehaviour
    {
        public RandomProjectileStatsComponent()
        {
            this.randomDamage = false;
            this.highDMGPercent = 200;
            this.lowDMGPercent = 10;

            this.randomSpeed = false;
            this.highSpeedPercent = 200;
            this.lowSpeedPercent = 10;

            this.randomKnockback = false;
            this.highKnockbackPercent = 200;
            this.lowKnockbackPercent = 10;

            this.randomRange = false;
            this.highRangePercent = 200;
            this.lowRangePercent = 10;

            this.randomScale = false;
            this.highScalePercent = 200;
            this.lowScalePercent = 10;

            this.randomJammedDMG = false;
            this.highJammedDMGPercent = 200;
            this.lowJammedDMGPercent = 10;

            this.randomBossDMG = false;
            this.highBossDMGPercent = 200;
            this.lowBossDMGPercent = 10;

            this.scaleBasedOnDamage = false;
        }
        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();

            if (randomSpeed) this.m_projectile.baseData.speed *= (UnityEngine.Random.Range(lowSpeedPercent, highSpeedPercent + 1)) / 100f;
            if (randomKnockback) this.m_projectile.baseData.force *= (UnityEngine.Random.Range(lowKnockbackPercent, highKnockbackPercent + 1)) / 100f;
            if (randomRange) this.m_projectile.baseData.range *= (UnityEngine.Random.Range(lowRangePercent, highRangePercent + 1)) / 100f;
            if (randomBossDMG) this.m_projectile.BossDamageMultiplier *= (UnityEngine.Random.Range(lowBossDMGPercent, highBossDMGPercent + 1)) / 100f;
            if (randomJammedDMG) this.m_projectile.BlackPhantomDamageMultiplier *= (UnityEngine.Random.Range(lowJammedDMGPercent, highJammedDMGPercent + 1)) / 100f;
            float damageMult = (UnityEngine.Random.Range(lowDMGPercent, highDMGPercent + 1)) / 100f;
            if (randomDamage) this.m_projectile.baseData.damage *= damageMult;
            if (randomScale) this.m_projectile.RuntimeUpdateScale((UnityEngine.Random.Range(lowScalePercent, highScalePercent + 1)) / 100f);
            if (scaleBasedOnDamage) this.m_projectile.RuntimeUpdateScale(damageMult);
            this.m_projectile.UpdateSpeed();
        }
        private Projectile m_projectile;

        public bool randomDamage;
        public int highDMGPercent;
        public int lowDMGPercent;

        public bool randomSpeed;
        public int highSpeedPercent;
        public int lowSpeedPercent;

        public bool randomKnockback;
        public int highKnockbackPercent;
        public int lowKnockbackPercent;

        public bool randomRange;
        public int highRangePercent;
        public int lowRangePercent;

        public bool randomScale;
        public int highScalePercent;
        public int lowScalePercent;

        public bool randomBossDMG;
        public int highBossDMGPercent;
        public int lowBossDMGPercent;

        public bool randomJammedDMG;
        public int highJammedDMGPercent;
        public int lowJammedDMGPercent;

        public bool scaleBasedOnDamage;
    } //Randomises the stats of the attached projectile.
    public class RandomiseProjectileColourComponent : MonoBehaviour
    {
        public RandomiseProjectileColourComponent()
        {
            ListOfColours = new List<Color>
        {
            ExtendedColours.pink,
            Color.red,
            ExtendedColours.orange,
            Color.yellow,
            Color.green,
            Color.blue,
            ExtendedColours.purple,
            Color.cyan,
        };
            ApplyColourToHitEnemies = false;
            tintPriority = 1;
            paintballGun = false;
        }
        public static List<Color> ListOfColours;
        public bool ApplyColourToHitEnemies;
        public int tintPriority;
        public bool paintballGun;
        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            selectedColour = BraveUtility.RandomElement(ListOfColours);
            if (paintballGun && m_projectile.ProjectilePlayerOwner() && m_projectile.ProjectilePlayerOwner().PlayerHasActiveSynergy("Paint It Black")) selectedColour = Color.black;
            m_projectile.AdjustPlayerProjectileTint(selectedColour, tintPriority);
            if (ApplyColourToHitEnemies) m_projectile.OnHitEnemy += this.OnHitEnemy;
        }
        private Projectile m_projectile;
        private Color selectedColour;
        private void OnHitEnemy(Projectile bullet, SpeculativeRigidbody enemy, bool what)
        {
            GameActorHealthEffect tint = new GameActorHealthEffect()
            {
                TintColor = selectedColour,
                DeathTintColor = selectedColour,
                AppliesTint = true,
                AppliesDeathTint = true,
                AffectsEnemies = true,
                DamagePerSecondToEnemies = 0f,
                duration = 10000000,
                effectIdentifier = "ProjectileAppliedTint",
            };
            enemy.aiActor.ApplyEffect(tint);
        }
        private void Update()
        {

        }
    } //Randomises the colour of the projectile, and can make it apply the colour to enemies.
    public class PierceDeadActors : MonoBehaviour
    {
        public PierceDeadActors()
        {
        }
        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            this.m_projectile.specRigidbody.OnPreRigidbodyCollision += this.PreCollision;
        }
        private void PreCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
        {
            if (myRigidbody != null && otherRigidbody != null)
            {
                if (otherRigidbody.healthHaver != null && otherRigidbody.healthHaver.IsDead)
                {
                    PhysicsEngine.SkipCollision = true;
                }
            }
        }
        private Projectile m_projectile;
    } //Causes the projectiles to piece enemies that have already died, but still have a hitbox.
    public class BulletLifeTimer : MonoBehaviour
    {
        public BulletLifeTimer()
        {
            this.secondsTillDeath = 1;
            this.eraseInsteadOfDie = false;
        }
        private void Start()
        {
            timer = secondsTillDeath;
            this.m_projectile = base.GetComponent<Projectile>();

        }
        private void FixedUpdate()
        {
            if (this.m_projectile != null)
            {
                if (timer > 0)
                {
                    timer -= BraveTime.DeltaTime;
                }
                if (timer <= 0)
                {
                    if (eraseInsteadOfDie) UnityEngine.Object.Destroy(this.m_projectile.gameObject);
                    else this.m_projectile.DieInAir();
                }
            }
        }
        public float secondsTillDeath;
        public bool eraseInsteadOfDie;
        private float timer;
        private Projectile m_projectile;
    } //Gives the projectile a time-based lifespan, after which it will die.
    public class InstantDestroyProjOnSpawn : MonoBehaviour
    {
        public InstantDestroyProjOnSpawn()
        {
            chance = 1;
        }
        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            if (UnityEngine.Random.value <= chance)
            {
                UnityEngine.Object.DestroyImmediate(this.m_projectile.gameObject);
            }
        }
        public float chance;
        private Projectile m_projectile;
    } //Prevents the projectile from spawning.
    public class AdvancedMirrorProjectileModifier : MonoBehaviour
    {
        public bool tintsBullets;
        public Color tintColour;
        public bool projectileSurvives;
        public bool postProcessReflectedBullets;
        public bool allowSurvivalIfPiercing;
        public float baseReflectedDMG;
        public float baseRelfectedSpeed;
        public float baseReflectedSpread;
        public float baseReflectedScaleMod;
        public bool retarget;
        public string sfx;
        public int maxMirrors;
        public bool RapidRiposteWeebshitSynergy;
        public AdvancedMirrorProjectileModifier()
        {
            this.tintsBullets = false;
            this.tintColour = Color.white;
            this.projectileSurvives = false;
            this.postProcessReflectedBullets = false;
            this.allowSurvivalIfPiercing = true;
            this.baseReflectedDMG = 15;
            this.baseRelfectedSpeed = 10;
            this.baseReflectedSpread = 0;
            this.baseReflectedScaleMod = 1;
            this.retarget = true;
            this.sfx = null;
            this.maxMirrors = -1;
            this.TimesMirrored = 0;
            this.RapidRiposteWeebshitSynergy = false;
        }
        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            if (tintsBullets) { this.m_projectile.AdjustPlayerProjectileTint(tintColour, 2, 0f); }
            this.m_projectile.collidesWithProjectiles = true;
            this.m_projectile.UpdateCollisionMask();
            SpeculativeRigidbody specRigidbody = this.m_projectile.specRigidbody;
            specRigidbody.OnPreRigidbodyCollision += this.HandlePreCollision;
        }
        private void HandlePreCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
        {
            if (otherRigidbody && otherRigidbody.projectile)
            {
                if (TimesMirrored < maxMirrors || maxMirrors == -1)
                {
                    if (otherRigidbody.projectile.Owner is AIActor)
                    {
                        if (!projectileSurvives)
                        {
                            if (allowSurvivalIfPiercing)
                            {
                                PierceProjModifier piercing = myRigidbody.projectile.GetComponent<PierceProjModifier>();
                                if (piercing != null)
                                {
                                    if (piercing.penetration > 0) piercing.penetration -= 1;
                                    else myRigidbody.projectile.DieInAir(false, true, true, false);
                                }
                                else myRigidbody.projectile.DieInAir(false, true, true, false);
                            }
                            else myRigidbody.projectile.DieInAir(false, true, true, false);
                        }
                        TimesMirrored++;
                        otherRigidbody.projectile.ReflectBullet(retarget, myRigidbody.projectile.Owner, baseRelfectedSpeed, postProcessReflectedBullets, baseReflectedScaleMod, baseReflectedDMG, baseReflectedSpread, sfx);
                        if (RapidRiposteWeebshitSynergy) otherRigidbody.projectile.OnHitEnemy += this.HandleGunie;
                    }
                }
                PhysicsEngine.SkipCollision = true;
            }
        }
        private void HandleGunie(Projectile proj, SpeculativeRigidbody enemy, bool fatal)
        {
            if (!fatal && enemy && enemy.aiActor && this.m_projectile.Owner && this.m_projectile.Owner is PlayerController)
            {
                if (UnityEngine.Random.value <= 0.5f)
                {
                    enemy.aiActor.DoGeniePunch((this.m_projectile.Owner as PlayerController));
                }
            }
        }
        private int TimesMirrored = 0;
        private Projectile m_projectile;
    } //Causes the projectile to reflect bullets, mirror bullets style, but with more variety.
    public class RandomRoomPosBehaviour : MonoBehaviour
    {
        public RandomRoomPosBehaviour()
        {
        }
        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            if (this.m_projectile.GetAbsoluteRoom() != null)
            {
                IntVector2 newPosition = this.m_projectile.GetAbsoluteRoom().GetRandomVisibleClearSpot(3, 3);
                this.m_projectile.transform.position = newPosition.ToVector3();
                this.m_projectile.specRigidbody.Reinitialize();
            }
        }
        private Projectile m_projectile;
    } //Teleports the bullet to a random position in the room on spawn.
    public class GravitronBulletsBehaviour : MonoBehaviour
    {
        public GravitronBulletsBehaviour()
        {
            this.orbitalLifespan = 15;
            this.cappedOrbiters = 20;
            this.orbitersCollideWithTilemap = false;
            this.orbitalGroup = 3;

            this.resetTravelledDistanceOnOrbit = true;
            this.alterProjRangeOnOrbit = true;
            this.baseProjectileRangePrioritisedIfLarger = true;
            this.resetSpeedIfOverCappedValueOnOrbit = true;
            this.speedCap = 50;
            this.speedResetValue = 20;
            this.targetRange = 500;
            this.damageMultOnOrbitStart = 2;

            this.minOrbitalRadius = 2f;
            this.maxOrbitalRadius = 5f;

            this.hasAlreadyOrbited = false;
        }
        public void Start()
        {
            try
            {
                this.m_projectile = base.GetComponent<Projectile>();

                bool canDo = true;
                if (this.m_projectile is InstantDamageOneEnemyProjectile) canDo = false;
                if (this.m_projectile is InstantlyDamageAllProjectile) canDo = false;
                if (this.m_projectile.GetComponent<ArtfulDodgerProjectileController>()) canDo = false;


                if (canDo)
                {
                    PierceProjModifier orAddComponent = this.m_projectile.gameObject.GetOrAddComponent<PierceProjModifier>();
                    orAddComponent.penetration = Mathf.Max(orAddComponent.penetration, 20);
                    orAddComponent.penetratesBreakables = true;
                    this.m_projectile.OnHitEnemy += this.HandleStartOrbit;
                }
            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
                ETGModConsole.Log(e.StackTrace);
            }
        }
        private void Update()
        {
            if (m_projectile && m_projectile.OverrideMotionModule != null)
            {
                if (m_projectile.OverrideMotionModule is OrbitProjectileMotionModule)
                {
                    OrbitProjectileMotionModule mod = (m_projectile.OverrideMotionModule as OrbitProjectileMotionModule);
                    if (!mod.alternateOrbitTarget && mod.usesAlternateOrbitTarget)
                    {
                        m_projectile.DieInAir();
                    }
                }
            }
        }
        private void HandleStartOrbit(Projectile proj, SpeculativeRigidbody enemy, bool fatal)
        {
            if (!fatal && !this.hasAlreadyOrbited)
            {
                int orbitersInGroup = OrbitProjectileMotionModule.GetOrbitersInGroup(this.orbitalGroup);
                if (orbitersInGroup >= cappedOrbiters)
                {
                    //return;
                }
                proj.specRigidbody.CollideWithTileMap = this.orbitersCollideWithTilemap;
                if (resetTravelledDistanceOnOrbit) proj.ResetDistance();
                if (alterProjRangeOnOrbit)
                {
                    if (baseProjectileRangePrioritisedIfLarger) proj.baseData.range = Mathf.Max(proj.baseData.range, this.targetRange);
                    else proj.baseData.range = this.targetRange;
                }
                if (resetSpeedIfOverCappedValueOnOrbit && proj.baseData.speed > speedCap)
                {
                    proj.baseData.speed = speedResetValue;
                    proj.UpdateSpeed();
                }
                proj.baseData.damage *= this.damageMultOnOrbitStart;

                OrbitProjectileMotionModule orbitProjectileMotionModule = new OrbitProjectileMotionModule();
                orbitProjectileMotionModule.lifespan = this.orbitalLifespan;
                orbitProjectileMotionModule.MinRadius = this.minOrbitalRadius;
                orbitProjectileMotionModule.MaxRadius = this.maxOrbitalRadius;
                orbitProjectileMotionModule.usesAlternateOrbitTarget = true;
                orbitProjectileMotionModule.OrbitGroup = this.orbitalGroup;
                orbitProjectileMotionModule.alternateOrbitTarget = enemy;
                if (proj.OverrideMotionModule != null && proj.OverrideMotionModule is HelixProjectileMotionModule)
                {
                    orbitProjectileMotionModule.StackHelix = true;
                    orbitProjectileMotionModule.ForceInvert = (proj.OverrideMotionModule as HelixProjectileMotionModule).ForceInvert;
                }
                proj.OverrideMotionModule = orbitProjectileMotionModule;
                this.hasAlreadyOrbited = true;
            }
        }
        private bool hasAlreadyOrbited;
        private Projectile m_projectile;
        public bool orbitersCollideWithTilemap;
        public bool resetTravelledDistanceOnOrbit;
        public bool alterProjRangeOnOrbit;
        public float targetRange;
        public bool baseProjectileRangePrioritisedIfLarger;
        public bool resetSpeedIfOverCappedValueOnOrbit;
        public float speedCap;
        public float speedResetValue;
        public int cappedOrbiters;
        public float orbitalLifespan;
        public int orbitalGroup;
        public float minOrbitalRadius;
        public float maxOrbitalRadius;
        public float damageMultOnOrbitStart;
    } //Causes the Bullets to orbit enemies they hit.
      //Allows for much easier transmogrification (has a more complex list where each entry can have it's own chance and variables)
    public class SimpleRandomTransmogrifyComponent : MonoBehaviour
    {
        public SimpleRandomTransmogrifyComponent()
        {
            chaosPalette = false;
        }
        private void Start()
        {
            self = base.GetComponent<Projectile>();
            if (self) self.OnHitEnemy += this.OnHitEnemy;
            if (chaosPalette) RandomStringList = MagickeCauldron.GenerateChaosPalette();
        }
        private void OnHitEnemy(Projectile bullet, SpeculativeRigidbody enemy, bool fatal)
        {
            if (bullet && enemy && enemy.aiActor && enemy.healthHaver && !fatal && !enemy.healthHaver.IsBoss)
            {
                if (enemy.healthHaver.IsDead) return;
                // if (enemy.healthHaver.GetCurrentHealth() < self.ReturnRealDamageWithModifiers(enemy.healthHaver))
                // {
                //    return;
                //}
                if (RandomStringList.Count > 0)
                {
                    string target = BraveUtility.RandomElement(RandomStringList);
                    enemy.aiActor.AdvancedTransmogrify(
                        EnemyDatabase.GetOrLoadByGuid(target),
                        (GameObject)ResourceCache.Acquire("Global VFX/VFX_Item_Spawn_Poof"),
                        "Play_ENM_wizardred_appear_01",
                        false,
                        null,
                        null,
                        true,
                        true,
                        maintainHPPercent,
                        true,
                        false,
                        false
                        );
                }
            }
        }
        //Bools for Random Transmog 
        public bool maintainHPPercent;
        public List<string> RandomStringList = new List<string>();
        private Projectile self;
        public bool chaosPalette;
    } //Allows for much easier transmogrification (Picks a random GUID from a List)


    public class SpawnEnemyOnBulletSpawn : MonoBehaviour
    {
        public SpawnEnemyOnBulletSpawn()
        {
            this.procChance = 1;
            this.deleteProjAfterSpawn = true;
            this.companioniseEnemy = true;
            this.ignoreSpawnedEnemyForGoodMimic = true;
            this.killSpawnedEnemyOnRoomClear = true;
            this.doPostProcessOnEnemyBullets = true;
            this.scaleEnemyDamage = true;
            this.scaleEnemyProjSize = true;
            this.scaleEnemyProjSpeed = true;
            this.enemyBulletDamage = 10f;
        }
        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            if (this.m_projectile.Owner is PlayerController) { this.projOwner = this.m_projectile.Owner as PlayerController; }
            GameManager.Instance.StartCoroutine(handleSpawn());
        }
        private IEnumerator handleSpawn()
        {
            yield return null;
            if (UnityEngine.Random.value <= this.procChance)
            {
                if (guidToSpawn != null)
                {
                    var enemyToSpawn = EnemyDatabase.GetOrLoadByGuid(guidToSpawn);
                    var position = this.m_projectile.specRigidbody.UnitCenter;
                    Instantiate<GameObject>(EasyVFXDatabase.SpiratTeleportVFX, position, Quaternion.identity);

                    AIActor TargetActor = AIActor.Spawn(enemyToSpawn, position, GameManager.Instance.Dungeon.data.GetAbsoluteRoomFromPosition(position.ToIntVector2()), true, AIActor.AwakenAnimationType.Default, true);

                    PhysicsEngine.Instance.RegisterOverlappingGhostCollisionExceptions(TargetActor.specRigidbody, null, false);

                    if (ignoreSpawnedEnemyForGoodMimic)
                    {
                        CustomEnemyTagsSystem tags = TargetActor.gameObject.GetOrAddComponent<CustomEnemyTagsSystem>();
                        tags.ignoreForGoodMimic = true;
                    }

                    if (companioniseEnemy && this.projOwner != null)
                    {
                        CompanionController orAddComponent = TargetActor.gameObject.GetOrAddComponent<CompanionController>();
                        orAddComponent.companionID = CompanionController.CompanionIdentifier.NONE;
                        orAddComponent.Initialize(this.projOwner);

                        CompanionisedEnemyBulletModifiers companionisedBullets = TargetActor.gameObject.GetOrAddComponent<CompanionisedEnemyBulletModifiers>();
                        companionisedBullets.jammedDamageMultiplier = 2f;
                        companionisedBullets.TintBullets = true;
                        companionisedBullets.TintColor = ExtendedColours.honeyYellow;
                        companionisedBullets.baseBulletDamage = enemyBulletDamage;
                        companionisedBullets.scaleDamage = this.scaleEnemyDamage;
                        companionisedBullets.doPostProcess = this.doPostProcessOnEnemyBullets;
                        companionisedBullets.scaleSize = this.scaleEnemyProjSize;
                        companionisedBullets.scaleSpeed = this.scaleEnemyProjSpeed;
                        companionisedBullets.enemyOwner = this.projOwner;
                    }

                    if (killSpawnedEnemyOnRoomClear)
                    {
                        TargetActor.gameObject.AddComponent<KillOnRoomClear>();
                    }

                    TargetActor.IsHarmlessEnemy = true;
                    TargetActor.IgnoreForRoomClear = true;
                    TargetActor.StartCoroutine(PostSpawn(TargetActor, knockbackAmountAwayFromOwner, m_projectile.Direction));
                    if (TargetActor.gameObject.GetComponent<SpawnEnemyOnDeath>())
                    {
                        Destroy(TargetActor.gameObject.GetComponent<SpawnEnemyOnDeath>());
                    }
                    if (deleteProjAfterSpawn) { Destroy(this.m_projectile.gameObject); }
                }
            }
        }
        private IEnumerator PostSpawn(AIActor spawnedEnemy, float knockbackAway, Vector2 dir)
        {
            yield return null;
            if (knockbackAway > 0)
            {
                if (spawnedEnemy.knockbackDoer)
                {
                    spawnedEnemy.knockbackDoer.ApplyKnockback(dir, knockbackAway);
                }
            }
            yield break;
        }
        public float knockbackAmountAwayFromOwner;
        private Projectile m_projectile;
        private PlayerController projOwner;
        public float procChance;
        public float enemyBulletDamage;
        public bool companioniseEnemy;
        public bool killSpawnedEnemyOnRoomClear;
        public bool deleteProjAfterSpawn;
        public bool ignoreSpawnedEnemyForGoodMimic;
        public string guidToSpawn;
        public bool scaleEnemyDamage;
        public bool scaleEnemyProjSize;
        public bool scaleEnemyProjSpeed;
        public bool doPostProcessOnEnemyBullets;
    } //Causes the projectile to spawn an enemy upon it's creation.
    public class MaintainDamageOnPierce : MonoBehaviour
    {
        public MaintainDamageOnPierce()
        {
            damageMultOnPierce = 1;
        }
        public void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            if (m_projectile)
            {
                this.m_projectile.specRigidbody.OnPreRigidbodyCollision += this.HandlePierce;
            }
        }
        private void HandlePierce(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
        {
            FieldInfo field = typeof(Projectile).GetField("m_hasPierced", BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(myRigidbody.projectile, false);
            m_projectile.baseData.damage *= damageMultOnPierce;
        }
        public float damageMultOnPierce;
        private Projectile m_projectile;
    } //Prevents projectiles from losing damage when they pierce an enemy, and can even increase it!
    public class DamageSecretWalls : MonoBehaviour
    {
        public DamageSecretWalls()
        {
            damageToDeal = 1E+10f;
        }
        public void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            if (m_projectile)
            {
                this.m_projectile.specRigidbody.OnPreRigidbodyCollision += this.HandlePierce;
            }
        }
        private void HandlePierce(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
        {
            if (otherRigidbody.GetComponent<MajorBreakable>() != null)
            {
                if (otherRigidbody.GetComponent<MajorBreakable>().IsSecretDoor)
                {
                    otherRigidbody.GetComponent<MajorBreakable>().ApplyDamage(damageToDeal, Vector2.zero, false, false, true);
                }
            }
        }
        public float damageToDeal;
        private Projectile m_projectile;
    } //Makes the projectile damage secret room walls
    public class AntimatterBulletsModifier : MonoBehaviour
    {

        public AntimatterBulletsModifier()
        {

        }

        public ExplosionData explosionData;
        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            this.m_projectile.collidesWithProjectiles = true;
            this.m_projectile.UpdateCollisionMask();
            SpeculativeRigidbody specRigidbody = this.m_projectile.specRigidbody;
            specRigidbody.OnPreRigidbodyCollision += this.HandlePreCollision;
        }
        private void HandlePreCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
        {
            if (otherRigidbody && otherRigidbody.projectile)
            {
                if (otherRigidbody.projectile.Owner is AIActor)
                {
                    if (otherRigidbody.projectile != lastCollidedProjectile)
                    {
                        if (explosionData != null)
                        {
                            Exploder.Explode(m_projectile.specRigidbody.UnitCenter, explosionData, Vector2.zero, null, true);
                        }
                        lastCollidedProjectile = otherRigidbody.projectile;
                    }
                }
                PhysicsEngine.SkipCollision = true;
            }
        }
        private Projectile m_projectile;
        private Projectile lastCollidedProjectile;
    } //Makes the projectile explode when intersecting enemy bullets
    public class BlockEnemyProjectilesMod : MonoBehaviour
    {
        public bool projectileSurvives;
        public BlockEnemyProjectilesMod()
        {
            this.projectileSurvives = false;
        }
        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            this.m_projectile.collidesWithProjectiles = true;
            this.m_projectile.UpdateCollisionMask();
            SpeculativeRigidbody specRigidbody = this.m_projectile.specRigidbody;
            specRigidbody.OnPreRigidbodyCollision += this.HandlePreCollision;
        }
        private void HandlePreCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
        {
            if (otherRigidbody && otherRigidbody.projectile)
            {
                if (otherRigidbody.projectile.Owner is AIActor)
                {
                    otherRigidbody.projectile.DieInAir(false, true, true, false);
                }
                if (
                    !projectileSurvives) myRigidbody.projectile.DieInAir(false, true, true, false);
                PhysicsEngine.SkipCollision = true;
            }
        }

        private Projectile m_projectile;
    } //Causes the projectile to destroy enemy projectiles
    public class SelectiveDamageMult : MonoBehaviour
    {
        public float multiplier;
        public bool multOnFlyingEnemies;
        public bool multOnStunnedEnemies;
        public SelectiveDamageMult()
        {
            this.multiplier = 1;
            this.multOnFlyingEnemies = false;
            this.multOnStunnedEnemies = false;
        }
        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            this.m_projectile.specRigidbody.OnPreRigidbodyCollision += this.HandlePreCollision;

        }
        private void HandlePreCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
        {
            if (otherRigidbody && otherRigidbody.healthHaver && otherRigidbody.healthHaver.aiActor)
            {
                Projectile me = myRigidbody.projectile;
                if (multOnFlyingEnemies && otherRigidbody.aiActor.IsFlying) DoMult(me);
                if (multOnStunnedEnemies && otherRigidbody.behaviorSpeculator.IsStunned) DoMult(me);
            }
        }
        private void DoMult(Projectile self)
        {
            self.baseData.damage *= multiplier;
            GameManager.Instance.StartCoroutine(this.ChangeProjectileDamage(self.projectile));
        }
        private IEnumerator ChangeProjectileDamage(Projectile bullet)
        {
            yield return new WaitForSeconds(0.1f);
            if (bullet != null)
            {
                bullet.baseData.damage /= multiplier;
            }
            yield break;
        }

        private Projectile m_projectile;
    } //Doubles the damage under certain criteria about the target
    public class ProjectileSplitController : MonoBehaviour
    {
        public ProjectileSplitController()
        {
            distanceTillSplit = 7.5f;
            splitAngles = 35;
            amtToSplitTo = 0;
            dmgMultAfterSplit = 0.66f;
            sizeMultAfterSplit = 0.8f;
            removeComponentAfterUse = true;
        }
        private void Start()
        {
            parentProjectile = base.GetComponent<Projectile>();
            parentOwner = parentProjectile.ProjectilePlayerOwner();
        }
        private void Update()
        {
            if (parentProjectile != null && distanceBasedSplit && !hasSplit)
            {
                if (parentProjectile.GetElapsedDistance() > distanceTillSplit)
                {
                    SplitProjectile();
                }
            }
        }
        private void SplitProjectile()
        {
            float ProjectileInterval = splitAngles / ((float)amtToSplitTo - 1);
            float currentAngle = parentProjectile.Direction.ToAngle();
            float startAngle = currentAngle + (splitAngles * 0.5f);
            int iteration = 0;
            for (int i = 0; i < amtToSplitTo; i++)
            {
                float finalAngle = startAngle - (ProjectileInterval * iteration);

                GameObject newBulletOBJ = FakePrefab.Clone(parentProjectile.gameObject);
                GameObject spawnedBulletOBJ = SpawnManager.SpawnProjectile(newBulletOBJ, parentProjectile.sprite.WorldCenter, Quaternion.Euler(0f, 0f, finalAngle), true);
                Projectile component = spawnedBulletOBJ.GetComponent<Projectile>();
                if (component != null)
                {
                    component.Owner = parentOwner;
                    component.Shooter = parentOwner.specRigidbody;
                    component.baseData.damage *= dmgMultAfterSplit;
                    component.RuntimeUpdateScale(sizeMultAfterSplit);
                    ProjectileSplitController split2 = component.gameObject.GetComponent<ProjectileSplitController>();
                    if (split2 && removeComponentAfterUse)
                    {
                        UnityEngine.Object.Destroy(split2);
                    }
                }

                iteration++;
            }
            hasSplit = true;
            UnityEngine.Object.Destroy(parentProjectile.gameObject);
        }
        private Projectile parentProjectile;
        private PlayerController parentOwner;
        private bool hasSplit;

        //Publics
        public bool distanceBasedSplit;
        public float distanceTillSplit;
        public bool splitOnEnemy;
        public float splitAngles;
        public int amtToSplitTo;

        public float dmgMultAfterSplit;
        public float sizeMultAfterSplit;

        public bool removeComponentAfterUse;
    } //Causes projectiles to split
    public class EnemyScaleUpdaterMod : MonoBehaviour
    {
        public EnemyScaleUpdaterMod()
        {
            targetScale = new Vector2(1, 1);
            multiplyExisting = true;
            addScaleEffectsToEnemy = false;
        }
        public void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            if (m_projectile)
            {
                this.m_projectile.OnHitEnemy += this.OnHitEnemy;
            }
        }
        private void OnHitEnemy(Projectile bullet, SpeculativeRigidbody enemy, bool fatal)
        {
            if (enemy && enemy.aiActor && enemy.healthHaver)
            {
                if (!fatal)
                {
                    if (addScaleEffectsToEnemy)
                    {
                        enemy.gameObject.GetOrAddComponent<SpecialSizeStatModification>();
                    }

                    float targetX = targetScale.x;
                    float targetY = targetScale.y;
                    if (multiplyExisting)
                    {
                        targetX = enemy.aiActor.EnemyScale.x * targetScale.x;
                        targetY = enemy.aiActor.EnemyScale.y * targetScale.y;
                    }
                    float maxCap = 2;
                    float minCap = 0.4f;
                    if (enemy.healthHaver.IsBoss) { maxCap = 1.4f; minCap = 0.8f; }
                    targetX = Mathf.Min(targetX, maxCap);
                    targetX = Mathf.Max(targetX, minCap);
                    targetY = Mathf.Min(targetY, maxCap);
                    targetY = Mathf.Max(targetY, minCap);

                    int cachedLayer2 = enemy.gameObject.layer;
                    int cachedOutlineLayer2 = cachedLayer2;
                    enemy.gameObject.layer = LayerMask.NameToLayer("Unpixelated");
                    cachedOutlineLayer2 = SpriteOutlineManager.ChangeOutlineLayer(enemy.sprite, LayerMask.NameToLayer("Unpixelated"));

                    enemy.aiActor.EnemyScale = new Vector2(targetX, targetY);

                    enemy.aiActor.gameObject.layer = cachedLayer2;
                    SpriteOutlineManager.ChangeOutlineLayer(enemy.sprite, cachedOutlineLayer2);

                    enemy.aiActor.specRigidbody.Reinitialize();
                    enemy.aiActor.DoCorrectForWalls();
                }
            }
        }
        public Vector2 targetScale;
        public bool multiplyExisting;
        public bool addScaleEffectsToEnemy;
        private Projectile m_projectile;
    } //Updates the scale of the affected enemy
    public class SpawnGameObjectOnDestructionMod : MonoBehaviour
    {
        public SpawnGameObjectOnDestructionMod()
        {
            chanceToSpawn = 1;
        }
        public void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            if (m_projectile)
            {
                this.m_projectile.OnDestruction += this.OnDestroy;
            }
        }
        private void OnDestroy(Projectile self)
        {
            if (UnityEngine.Random.value <= chanceToSpawn)
            {
                if (objectsToPickFrom.Count > 0)
                {
                    GameObject objToSpawn = BraveUtility.RandomElement(objectsToPickFrom);
                    SpawnObjectManager.SpawnObject(objToSpawn, m_projectile.specRigidbody.UnitCenter, null, true);
                }
            }
        }
        public float chanceToSpawn;
        public List<GameObject> objectsToPickFrom = new List<GameObject>();
        private Projectile m_projectile;
    } //Spawns a gameobject when the projectile is destroyed
    public class SpawnEnemyOnDestructionMod : MonoBehaviour
    {
        public SpawnEnemyOnDestructionMod()
        {
            pickRandom = true;
            companionise = true;
        }
        public void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            if (m_projectile)
            {
                this.m_projectile.OnDestruction += this.OnDestroy;
            }
        }
        private void OnDestroy(Projectile self)
        {
            string objToSpawn = BraveUtility.RandomElement(EnemiesToSpawn);
            PlayerController player = m_projectile.ProjectilePlayerOwner();
            var Kin = EnemyDatabase.GetOrLoadByGuid(objToSpawn);

            CompanionisedEnemyUtility.SpawnCompanionisedEnemy(player, objToSpawn, m_projectile.specRigidbody.UnitCenter.ToIntVector2(), false, Color.red, 5, 2, false, true);
        }
        public List<string> EnemiesToSpawn = new List<string>();
        public bool pickRandom;
        public bool companionise;
        private Projectile m_projectile;
    } //Spawns a companionised enemy on destruction
    public class EraseEnemyBehav : MonoBehaviour
    {
        public EraseEnemyBehav()
        {
            bossMode = BossInteraction.DAMAGE;
            bonusBossDMG = 500;
        }
        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            this.m_projectile.OnHitEnemy += this.OnHitEnemy;

        }
        private void OnHitEnemy(Projectile self, SpeculativeRigidbody enemy, bool fatal)
        {
            if (enemy && enemy.aiActor && enemy.aiActor.healthHaver)
            {
                if (enemy.aiActor.healthHaver.IsBoss && bossMode != BossInteraction.ERASE)
                {
                    if (bossMode == BossInteraction.IGNORE) return;
                    else
                    {
                        enemy.aiActor.healthHaver.ApplyDamage(bonusBossDMG, Vector2.zero, "Erase", CoreDamageTypes.Void, DamageCategory.Unstoppable);
                    }
                }
                else
                {
                    enemy.aiActor.EraseFromExistenceWithRewards();
                    if (doSparks)
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            GlobalSparksDoer.DoRandomParticleBurst(3, enemy.specRigidbody.UnitCenter, enemy.specRigidbody.UnitCenter, new Vector3(UnityEngine.Random.Range(0, 4), UnityEngine.Random.Range(0, 4)), 360, 0, null, null, null, GlobalSparksDoer.SparksType.SOLID_SPARKLES);
                        }
                    }
                }
            }
        }
        private Projectile m_projectile;
        public BossInteraction bossMode;
        public float bonusBossDMG;
        public bool doSparks;
        public enum BossInteraction
        {
            ERASE,
            DAMAGE,
            IGNORE,
        }
    } //Erases enemies the bullet hits
    public class EnemyBulletSpeedSapperMod : MonoBehaviour
    {

        public EnemyBulletSpeedSapperMod()
        {

        }

        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            this.m_projectile.collidesWithProjectiles = true;
            this.m_projectile.UpdateCollisionMask();
            SpeculativeRigidbody specRigidbody = this.m_projectile.specRigidbody;
            specRigidbody.OnPreRigidbodyCollision += this.HandlePreCollision;
        }
        private void HandlePreCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
        {
            if (otherRigidbody && otherRigidbody.projectile)
            {
                if (otherRigidbody.projectile.Owner is AIActor)
                {
                    if (otherRigidbody.projectile != lastCollidedProjectile)
                    {
                        otherRigidbody.projectile.RemoveBulletScriptControl();
                        otherRigidbody.projectile.baseData.speed *= 0.5f;
                        otherRigidbody.projectile.UpdateSpeed();

                        this.m_projectile.baseData.speed *= 1.2f;
                        this.m_projectile.UpdateSpeed();
                        if (m_projectile.ProjectilePlayerOwner() && m_projectile.ProjectilePlayerOwner().PlayerHasActiveSynergy("Accelent"))
                        {
                            var ddgm = DeadlyDeadlyGoopManager.GetGoopManagerForGoopType(EasyGoopDefinitions.FireDef);
                            ddgm.AddGoopCircle(m_projectile.sprite.WorldCenter, 0.5f);
                        }
                        lastCollidedProjectile = otherRigidbody.projectile;
                    }
                }
                PhysicsEngine.SkipCollision = true;
            }
        }
        private Projectile m_projectile;
        private Projectile lastCollidedProjectile;
    } //The bullet will steal the speed from enemy bullets
    public class BeamBulletsBehaviour : MonoBehaviour
    {
        public BeamBulletsBehaviour()
        {
            beamToFire = LaserBullets.SimpleRedBeam;
            firetype = FireType.PLUS;
        }
        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            if (this.m_projectile.Owner && this.m_projectile.Owner is PlayerController)
            {
                this.m_owner = this.m_projectile.Owner as PlayerController;
            }
            Invoke("BeginBeamFire", 0.1f);
        }
        private void BeginBeamFire()
        {
            if (firetype == FireType.FORWARDS)
            {
                BeamController beam = BeamAPI.FreeFireBeamFromAnywhere(beamToFire, this.m_owner, this.m_projectile.gameObject, Vector2.zero, 0, 1000, true, true);
                Projectile beamprojcomponent = beam.GetComponent<Projectile>();
            }
            if (firetype == FireType.BACKWARDS)
            {
                BeamController beam = BeamAPI.FreeFireBeamFromAnywhere(beamToFire, this.m_owner, this.m_projectile.gameObject, Vector2.zero, 180, 1000, true, true, 180);
                Projectile beamprojcomponent = beam.GetComponent<Projectile>();
            }
            if (firetype == FireType.CROSS || firetype == FireType.STAR)
            {
                //NorthEast
                BeamController beam = BeamAPI.FreeFireBeamFromAnywhere(beamToFire, this.m_owner, this.m_projectile.gameObject, Vector2.zero, 45, 1000, true);
                Projectile beamprojcomponent = beam.GetComponent<Projectile>();
                beamprojcomponent.baseData.damage *= m_owner.stats.GetStatValue(PlayerStats.StatType.Damage);
                //SouthEast
                BeamController beam2 = BeamAPI.FreeFireBeamFromAnywhere(beamToFire, this.m_owner, this.m_projectile.gameObject, Vector2.zero, 135, 1000, true);
                Projectile beamprojcomponent2 = beam2.GetComponent<Projectile>();
                beamprojcomponent2.baseData.damage *= m_owner.stats.GetStatValue(PlayerStats.StatType.Damage);
                //SouthWest
                BeamController beam3 = BeamAPI.FreeFireBeamFromAnywhere(beamToFire, this.m_owner, this.m_projectile.gameObject, Vector2.zero, -45, 1000, true);
                Projectile beamprojcomponent3 = beam3.GetComponent<Projectile>();
                beamprojcomponent3.baseData.damage *= m_owner.stats.GetStatValue(PlayerStats.StatType.Damage);
                //NorthWest
                BeamController beam4 = BeamAPI.FreeFireBeamFromAnywhere(beamToFire, this.m_owner, this.m_projectile.gameObject, Vector2.zero, -135, 1000, true);
                Projectile beamprojcomponent4 = beam4.GetComponent<Projectile>();
                beamprojcomponent4.baseData.damage *= m_owner.stats.GetStatValue(PlayerStats.StatType.Damage);
            }
            if (firetype == FireType.PLUS || firetype == FireType.STAR)
            {
                //Right
                BeamController beam = BeamAPI.FreeFireBeamFromAnywhere(beamToFire, this.m_owner, this.m_projectile.gameObject, Vector2.zero, 0, 1000, true);
                Projectile beamprojcomponent = beam.GetComponent<Projectile>();
                beamprojcomponent.baseData.damage *= m_owner.stats.GetStatValue(PlayerStats.StatType.Damage);
                //Up
                BeamController beam2 = BeamAPI.FreeFireBeamFromAnywhere(beamToFire, this.m_owner, this.m_projectile.gameObject, Vector2.zero, 90, 1000, true);
                Projectile beamprojcomponent2 = beam2.GetComponent<Projectile>();
                beamprojcomponent2.baseData.damage *= m_owner.stats.GetStatValue(PlayerStats.StatType.Damage);
                //Left
                BeamController beam3 = BeamAPI.FreeFireBeamFromAnywhere(beamToFire, this.m_owner, this.m_projectile.gameObject, Vector2.zero, 180, 1000, true);
                Projectile beamprojcomponent3 = beam3.GetComponent<Projectile>();
                beamprojcomponent3.baseData.damage *= m_owner.stats.GetStatValue(PlayerStats.StatType.Damage);
                //Down
                BeamController beam4 = BeamAPI.FreeFireBeamFromAnywhere(beamToFire, this.m_owner, this.m_projectile.gameObject, Vector2.zero, -90, 1000, true);
                Projectile beamprojcomponent4 = beam4.GetComponent<Projectile>();
                beamprojcomponent4.baseData.damage *= m_owner.stats.GetStatValue(PlayerStats.StatType.Damage);
            }
        }
        private void Update()
        {

        }

        private Projectile m_projectile;
        private PlayerController m_owner;
        public Projectile beamToFire;
        public FireType firetype;
        public enum FireType
        {
            PLUS,
            CROSS,
            STAR,
            FORWARDS,
            BACKWARDS
        }
    }
    public class CustomImpactSoundBehav : MonoBehaviour
    {
        public CustomImpactSoundBehav()
        {
        }
        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            this.m_projectile.OnDestruction += this.onDestroy;
        }
        private void onDestroy(Projectile self)
        {
            if (!string.IsNullOrEmpty(ImpactSFX)) { AkSoundEngine.PostEvent(ImpactSFX, m_projectile.gameObject); }
        }
        public string ImpactSFX;
        private Projectile m_projectile;
    }
    public class PrefabStatusEffectsToApply : MonoBehaviour
    {
        public PrefabStatusEffectsToApply()
        {
        }
        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            this.m_projectile.statusEffectsToApply.AddRange(effects);
        }
        public List<GameActorEffect> effects;
        private Projectile m_projectile;
    }
    public class ScaleChangeOverTimeModifier : MonoBehaviour
    {
        public ScaleChangeOverTimeModifier()
        {
            timeToChangeOver = 1;
            ScaleToChangeTo = 0.1f;
            destroyAfterChange = false;
            timeExtendedByRangeMultiplier = true;
            suppressDeathFXIfdestroyed = true;
            scaleMultAffectsDamage = false;
        }
        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            initialScale = m_projectile.AdditionalScaleMultiplier;
            StartCoroutine(DoShrink());
        }
        private IEnumerator DoShrink()
        {
            float realTime = timeToChangeOver;
            float x = m_projectile.sprite.scale.x;
            float startDMG = m_projectile.baseData.damage;

            if (timeExtendedByRangeMultiplier && m_projectile.ProjectilePlayerOwner()) realTime *= m_projectile.ProjectilePlayerOwner().stats.GetStatValue(PlayerStats.StatType.RangeMultiplier);
            float elapsed = 0f;
            while (elapsed < realTime)
            {
                elapsed += BraveTime.DeltaTime;
                float t = Mathf.Clamp01(elapsed / realTime);
                float scalemodifier = Mathf.Lerp(initialScale, ScaleToChangeTo, t);

                //Let's try and do damage stuff
                if (scaleMultAffectsDamage)
                {
                    float percentageScaleThisFrame = scalemodifier / initialScale;
                    m_projectile.baseData.damage = (startDMG * percentageScaleThisFrame);
                }

                m_projectile.AdditionalScaleMultiplier = scalemodifier;
                m_projectile.sprite.scale = new Vector3(x * scalemodifier, x * scalemodifier, x * scalemodifier);

                if (m_projectile.specRigidbody != null)
                {
                    m_projectile.specRigidbody.UpdateCollidersOnScale = true;
                }

                yield return null;
            }
            if (destroyAfterChange)
            {
                m_projectile.DieInAir(suppressDeathFXIfdestroyed);
            }
        }
        private Projectile m_projectile;
        private float initialScale;
        public float timeToChangeOver;
        public float ScaleToChangeTo;
        public bool destroyAfterChange;
        public bool timeExtendedByRangeMultiplier;
        public bool suppressDeathFXIfdestroyed;
        public bool scaleMultAffectsDamage;
    } //Causes the projectile's scale to change over time
    public class SpecialProjectileIdentifier : MonoBehaviour
    {
        //This shit is to make sure that stuff like OnPreFireProjectileModifier doesn't accidentally catch duct taped bullets in it's switcheroo.
        public SpecialProjectileIdentifier()
        {
            SpecialIdentifier = "NULL";
        }
        public string SpecialIdentifier;
    }
    public class OwnerConnectLightningModifier : MonoBehaviour
    {
        public GameObject linkPrefab;
        public float DamagePerTick;
        private GameActor owner;
        private tk2dTiledSprite extantLink;
        private Projectile self;
        public OwnerConnectLightningModifier()
        {
            linkPrefab = St4ke.LinkVFXPrefab;
            DamagePerTick = 2f;
        }
        private void OnDestroy()
        {
            if (extantLink)
            {
                SpawnManager.Despawn(extantLink.gameObject);
            }
        }
        private void Start()
        {
            self = base.GetComponent<Projectile>();
            if (self)
            {
                if (self.Owner) owner = self.Owner;
            }
        }
        private void Update()
        {
            if (self && owner && this.extantLink == null)
            {
                tk2dTiledSprite component = SpawnManager.SpawnVFX(linkPrefab, false).GetComponent<tk2dTiledSprite>();
                this.extantLink = component;
            }
            else if (self && owner && this.extantLink != null)
            {
                UpdateLink(owner, this.extantLink);
            }
            else if (extantLink != null)
            {
                SpawnManager.Despawn(extantLink.gameObject);
                extantLink = null;
            }
        }
        private void UpdateLink(GameActor target, tk2dTiledSprite m_extantLink)
        {
            Vector2 unitCenter = self.specRigidbody.UnitCenter;
            Vector2 unitCenter2 = target.specRigidbody.HitboxPixelCollider.UnitCenter;
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
            if (owner is PlayerController)
            {
                float damage = DamagePerTick;
                damage *= self.ProjectilePlayerOwner().stats.GetStatValue(PlayerStats.StatType.Damage);
                for (int i = 0; i < StaticReferenceManager.AllEnemies.Count; i++)
                {
                    AIActor aiactor = StaticReferenceManager.AllEnemies[i];
                    if (!this.m_damagedEnemies.Contains(aiactor))
                    {
                        if (aiactor && aiactor.HasBeenEngaged && aiactor.IsNormalEnemy && aiactor.specRigidbody && aiactor.healthHaver)
                        {
                            if (aiactor.healthHaver.IsBoss) damage *= self.ProjectilePlayerOwner().stats.GetStatValue(PlayerStats.StatType.DamageToBosses);
                            Vector2 zero = Vector2.zero;
                            if (BraveUtility.LineIntersectsAABB(p1, p2, aiactor.specRigidbody.HitboxPixelCollider.UnitBottomLeft, aiactor.specRigidbody.HitboxPixelCollider.UnitDimensions, out zero))
                            {
                                aiactor.healthHaver.ApplyDamage(DamagePerTick, Vector2.zero, "Chain Lightning", CoreDamageTypes.Electric, DamageCategory.Normal, false, null, false);
                                GameManager.Instance.StartCoroutine(this.HandleDamageCooldown(aiactor));
                            }
                        }
                    }
                }
            }
            else if (owner is AIActor)
            {
                if (GameManager.Instance.PrimaryPlayer != null)
                {
                    PlayerController player1 = GameManager.Instance.PrimaryPlayer;
                    Vector2 zero = Vector2.zero;
                    if (BraveUtility.LineIntersectsAABB(p1, p2, player1.specRigidbody.HitboxPixelCollider.UnitBottomLeft, player1.specRigidbody.HitboxPixelCollider.UnitDimensions, out zero))
                    {
                        if (player1.healthHaver && player1.healthHaver.IsVulnerable && !player1.IsEthereal && !player1.IsGhost)
                        {
                            string damageSource = "Electricity";
                            if (owner.encounterTrackable) damageSource = owner.encounterTrackable.GetModifiedDisplayName();
                            if (self.IsBlackBullet) player1.healthHaver.ApplyDamage(1f, Vector2.zero, damageSource, CoreDamageTypes.Electric, DamageCategory.BlackBullet, true);
                            else player1.healthHaver.ApplyDamage(0.5f, Vector2.zero, damageSource, CoreDamageTypes.Electric, DamageCategory.Normal, false);
                        }
                    }
                }
                if (GameManager.Instance.SecondaryPlayer != null)
                {
                    PlayerController player2 = GameManager.Instance.SecondaryPlayer;
                    Vector2 zero = Vector2.zero;
                    if (BraveUtility.LineIntersectsAABB(p1, p2, player2.specRigidbody.HitboxPixelCollider.UnitBottomLeft, player2.specRigidbody.HitboxPixelCollider.UnitDimensions, out zero))
                    {
                        if (player2.healthHaver && player2.healthHaver.IsVulnerable && !player2.IsEthereal && !player2.IsGhost)
                        {
                            string damageSource = "Electricity";
                            if (owner.encounterTrackable) damageSource = owner.encounterTrackable.GetModifiedDisplayName();
                            if (self.IsBlackBullet) player2.healthHaver.ApplyDamage(1f, Vector2.zero, damageSource, CoreDamageTypes.Electric, DamageCategory.BlackBullet, true);
                            else player2.healthHaver.ApplyDamage(0.5f, Vector2.zero, damageSource, CoreDamageTypes.Electric, DamageCategory.Normal, false);
                        }
                    }
                }
            }
        }
        private HashSet<AIActor> m_damagedEnemies = new HashSet<AIActor>();

        private IEnumerator HandleDamageCooldown(AIActor damagedTarget)
        {
            this.m_damagedEnemies.Add(damagedTarget);
            yield return new WaitForSeconds(0.1f);
            this.m_damagedEnemies.Remove(damagedTarget);
            yield break;
        }
    }
    public class SneakyShotgunComponent : MonoBehaviour
    {
        public SneakyShotgunComponent()
        {
            scaleOffOwnerAccuracy = true;
            eraseSource = true;
            numToFire = 5;
            projPrefabToFire = (PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0];
            postProcess = true;
            doVelocityRandomiser = true;
            angleVariance = 40;
            scaleMult = 1;
            damageMult = 1;
            overrideProjectileSynergy = null;
            synergyProjectilePrefab = null;
        }
        public bool scaleOffOwnerAccuracy;
        public bool eraseSource;
        public float angleVariance;
        public int numToFire;
        public Projectile projPrefabToFire;
        public string overrideProjectileSynergy;
        public Projectile synergyProjectilePrefab;
        public bool postProcess;
        public bool doVelocityRandomiser;
        public float damageMult;
        public float scaleMult;
        private void Start()
        {
            self = base.GetComponent<Projectile>();
            StartCoroutine(handleShotgunBlast());
        }
        private Projectile self;
        private IEnumerator handleShotgunBlast()
        {
            yield return null;
            GameObject prefabtouse = projPrefabToFire.gameObject;
            if (!string.IsNullOrEmpty(overrideProjectileSynergy) && synergyProjectilePrefab != null && self.ProjectilePlayerOwner())
            {
                if (self.ProjectilePlayerOwner().PlayerHasActiveSynergy(overrideProjectileSynergy))
                {
                    prefabtouse = synergyProjectilePrefab.gameObject;
                }
            }
            for (int i = 0; i < numToFire; i++)
            {
                PlayerController accuracyOwner = null;
                if (scaleOffOwnerAccuracy && self.ProjectilePlayerOwner()) accuracyOwner = self.ProjectilePlayerOwner();
                float angle = ProjSpawnHelper.GetAccuracyAngled(self.Direction.ToAngle(), angleVariance, accuracyOwner);
                GameObject spawnObj = SpawnManager.SpawnProjectile(prefabtouse, self.transform.position, Quaternion.Euler(new Vector3(0f, 0f, angle)));
                Projectile component = spawnObj.GetComponent<Projectile>();
                if (component != null)
                {
                    component.Owner = self.Owner;
                    component.Shooter = self.Shooter;
                    if (doVelocityRandomiser) component.baseData.speed *= (1f + UnityEngine.Random.Range(-5f, 5f) / 100f);
                    component.UpdateSpeed();
                    component.baseData.damage *= damageMult;
                    component.baseData.force *= damageMult;
                    component.RuntimeUpdateScale(scaleMult);
                    if (postProcess && self.ProjectilePlayerOwner()) self.ProjectilePlayerOwner().DoPostProcessProjectile(component);
                }
            }
            if (eraseSource)
            {
                UnityEngine.Object.Destroy(self.gameObject);
            }
            yield break;
        }

    }
    

    //PASSIVE ITEM EFFECTS AS COMPONENTS
    public class AngryBulletsProjectileBehaviour : MonoBehaviour
    {
        public AngryBulletsProjectileBehaviour()
        {
            this.ApplyRandomBounceOffEnemy = true;
            this.ChanceToSeekEnemyOnBounce = 0.5f;
            this.ActivationsPerSecond = 1f;
            this.MinActivationChance = 0.05f;
        }

        public void Start()
        {
            try
            {
                this.m_projectile = base.GetComponent<Projectile>();
                this.m_projectile.OnHitEnemy += this.HandleProjectileHitEnemy;
            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
                ETGModConsole.Log(e.StackTrace);
            }
        }
        private void HandleProjectileHitEnemy(Projectile obj, SpeculativeRigidbody enemy, bool killed)
        {
            if (this.ApplyRandomBounceOffEnemy)
            {
                PierceProjModifier orAddComponent = obj.gameObject.GetOrAddComponent<PierceProjModifier>();
                orAddComponent.penetratesBreakables = true;
                orAddComponent.penetration++;
                HomingModifier component = obj.gameObject.GetComponent<HomingModifier>();
                if (component)
                {
                    component.AngularVelocity *= 0.75f;
                }
                Vector2 dirVec = UnityEngine.Random.insideUnitCircle;
                float num = this.ChanceToSeekEnemyOnBounce;
                Gun possibleSourceGun = obj.PossibleSourceGun;
                if (this.NormalizeAcrossFireRate && possibleSourceGun)
                {
                    float num2 = 1f / possibleSourceGun.DefaultModule.cooldownTime;
                    if (possibleSourceGun.Volley != null && possibleSourceGun.Volley.UsesShotgunStyleVelocityRandomizer)
                    {
                        num2 *= (float)Mathf.Max(1, possibleSourceGun.Volley.projectiles.Count);
                    }
                    num = Mathf.Clamp01(this.ActivationsPerSecond / num2);
                    num = Mathf.Max(this.MinActivationChance, num);
                }
                if (UnityEngine.Random.value < num && enemy.aiActor)
                {
                    Func<AIActor, bool> isValid = (AIActor a) => a && a.HasBeenEngaged && a.healthHaver && a.healthHaver.IsVulnerable;
                    AIActor closestToPosition = BraveUtility.GetClosestToPosition<AIActor>(enemy.aiActor.ParentRoom.GetActiveEnemies(RoomHandler.ActiveEnemyType.All), enemy.UnitCenter, isValid, new AIActor[]
                    {
                    enemy.aiActor
                    });
                    if (closestToPosition)
                    {
                        dirVec = closestToPosition.CenterPosition - obj.transform.position.XY();
                    }
                }
                obj.SendInDirection(dirVec, false, true);
            }
        }
        private Projectile m_projectile;
        public bool ApplyRandomBounceOffEnemy;
        public float ChanceToSeekEnemyOnBounce;
        public bool NormalizeAcrossFireRate;
        public float ActivationsPerSecond;
        public float MinActivationChance;
    } //The Angry Bullets Effect as a Projectile Component.

    public class RemoteBulletsProjectileBehaviour : MonoBehaviour
    {
        public RemoteBulletsProjectileBehaviour()
        {
            this.trackingSpeed = Gungeon.Game.Items["remote_bullets"].GetComponent<GuidedBulletsPassiveItem>().trackingSpeed;
            this.trackingCurve = Gungeon.Game.Items["remote_bullets"].GetComponent<GuidedBulletsPassiveItem>().trackingCurve;
            this.trackingTime = Gungeon.Game.Items["remote_bullets"].GetComponent<GuidedBulletsPassiveItem>().trackingTime;
        }

        public void Start()
        {
            try
            {
                this.m_projectile = base.GetComponent<Projectile>();
                this.m_projectile.PreMoveModifiers += this.PreMoveProjectileModifier;
            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
                ETGModConsole.Log(e.StackTrace);
            }
        }
        private void PreMoveProjectileModifier(Projectile p)
        {
            if (p && p.Owner is PlayerController)
            {
                BraveInput instanceForPlayer = BraveInput.GetInstanceForPlayer((p.Owner as PlayerController).PlayerIDX);
                if (instanceForPlayer == null) return;
                Vector2 vector = Vector2.zero;
                if (instanceForPlayer.IsKeyboardAndMouse(false))
                {
                    vector = (p.Owner as PlayerController).unadjustedAimPoint.XY() - p.specRigidbody.UnitCenter;
                }
                else
                {
                    if (instanceForPlayer.ActiveActions == null)
                    {
                        return;
                    }
                    vector = instanceForPlayer.ActiveActions.Aim.Vector;
                }
                float num = vector.ToAngle();
                float num2 = BraveMathCollege.Atan2Degrees(p.Direction);
                float num3 = 0f;
                if (p.ElapsedTime < this.trackingTime)
                {
                    num3 = this.trackingCurve.Evaluate(p.ElapsedTime / this.trackingTime) * this.trackingSpeed;
                }
                float num4 = Mathf.MoveTowardsAngle(num2, num, num3 * BraveTime.DeltaTime);
                Vector2 vector2 = Quaternion.Euler(0f, 0f, Mathf.DeltaAngle(num2, num4)) * p.Direction;
                if (p is HelixProjectile)
                {
                    HelixProjectile helixProjectile = p as HelixProjectile;
                    helixProjectile.AdjustRightVector(Mathf.DeltaAngle(num2, num4));
                }
                if (p.OverrideMotionModule != null)
                {
                    p.OverrideMotionModule.AdjustRightVector(Mathf.DeltaAngle(num2, num4));
                }
                p.Direction = vector2.normalized;
                if (p.shouldRotate)
                {
                    p.transform.eulerAngles = new Vector3(0f, 0f, p.Direction.ToAngle());
                }
            }
        }
        private Projectile m_projectile;
        public float trackingSpeed = 45f;
        public float trackingTime = 6f;
        [CurveRange(0f, 0f, 1f, 1f)]
        public AnimationCurve trackingCurve;
    } //The Remote Bullets effect as a Projectile Component.
    public class OrbitalBulletsBehaviour : MonoBehaviour
    {
        public OrbitalBulletsBehaviour()
        {
            this.orbitalLifespan = 15;
            this.cappedOrbiters = 20;
            this.orbitersCollideWithTilemap = false;
            this.orbitalGroup = -1;

            this.resetTravelledDistanceOnOrbit = true;
            this.alterProjRangeOnOrbit = true;
            this.baseProjectileRangePrioritisedIfLarger = true;
            this.resetSpeedIfOverCappedValueOnOrbit = true;
            this.speedCap = 50;
            this.speedResetValue = 20;
            this.targetRange = 500;

            this.minOrbitalRadius = 2f;
            this.maxOrbitalRadius = 5f;

            this.usesOverrideCenter = false;
            this.overrideCenter = null;
        }
        public void Start()
        {
            try
            {
                this.m_projectile = base.GetComponent<Projectile>();

                bool canDo = true;
                if (this.m_projectile is InstantDamageOneEnemyProjectile) canDo = false;
                if (this.m_projectile is InstantlyDamageAllProjectile) canDo = false;
                if (this.m_projectile.GetComponent<ArtfulDodgerProjectileController>()) canDo = false;

                if (canDo)
                {
                    BounceProjModifier orAddComponent = this.m_projectile.gameObject.GetOrAddComponent<BounceProjModifier>();
                    orAddComponent.numberOfBounces = Mathf.Max(orAddComponent.numberOfBounces, 1);
                    orAddComponent.onlyBounceOffTiles = true;
                    BounceProjModifier bounceProjModifier = orAddComponent;
                    bounceProjModifier.OnBounceContext += this.HandleStartOrbit;
                }
            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
                ETGModConsole.Log(e.StackTrace);
            }
        }
        private void HandleStartOrbit(BounceProjModifier mod, SpeculativeRigidbody srb)
        {
            int orbitersInGroup = OrbitProjectileMotionModule.GetOrbitersInGroup(this.orbitalGroup);
            if (orbitersInGroup >= cappedOrbiters)
            {
                return;
            }
            mod.projectile.specRigidbody.CollideWithTileMap = this.orbitersCollideWithTilemap;
            if (resetTravelledDistanceOnOrbit) mod.projectile.ResetDistance();
            if (alterProjRangeOnOrbit)
            {
                if (baseProjectileRangePrioritisedIfLarger) mod.projectile.baseData.range = Mathf.Max(mod.projectile.baseData.range, this.targetRange);
                else mod.projectile.baseData.range = this.targetRange;
            }
            if (resetSpeedIfOverCappedValueOnOrbit && mod.projectile.baseData.speed > speedCap)
            {
                mod.projectile.baseData.speed = speedResetValue;
                mod.projectile.UpdateSpeed();
            }

            OrbitProjectileMotionModule orbitProjectileMotionModule = new OrbitProjectileMotionModule();
            orbitProjectileMotionModule.lifespan = this.orbitalLifespan;
            orbitProjectileMotionModule.MinRadius = this.minOrbitalRadius;
            orbitProjectileMotionModule.MaxRadius = this.maxOrbitalRadius;
            orbitProjectileMotionModule.OrbitGroup = this.orbitalGroup;
            orbitProjectileMotionModule.usesAlternateOrbitTarget = this.usesOverrideCenter;
            orbitProjectileMotionModule.alternateOrbitTarget = this.overrideCenter;
            if (mod.projectile.OverrideMotionModule != null && mod.projectile.OverrideMotionModule is HelixProjectileMotionModule)
            {
                orbitProjectileMotionModule.StackHelix = true;
                orbitProjectileMotionModule.ForceInvert = (mod.projectile.OverrideMotionModule as HelixProjectileMotionModule).ForceInvert;
            }
            mod.projectile.OverrideMotionModule = orbitProjectileMotionModule;
        }
        private Projectile m_projectile;
        public bool orbitersCollideWithTilemap;
        public bool resetTravelledDistanceOnOrbit;
        public bool alterProjRangeOnOrbit;
        public float targetRange;
        public bool baseProjectileRangePrioritisedIfLarger;
        public bool resetSpeedIfOverCappedValueOnOrbit;
        public float speedCap;
        public float speedResetValue;
        public int cappedOrbiters;
        public float orbitalLifespan;
        public int orbitalGroup;
        public float minOrbitalRadius;
        public float maxOrbitalRadius;
        public bool usesOverrideCenter;
        public SpeculativeRigidbody overrideCenter;
    } //Orbital Bullets as a Projectile Component.
    public class BulletIsFromBeam : MonoBehaviour
    {

    }
    public class ChaosBulletsModifierComp : MonoBehaviour
    {
        public ChaosBulletsModifierComp()
        {
            //BASIC STATS
            effectScaler = 1;
            chanceToAddPierce = 0.1f;
            chanceToAddBounce = 0.1f;
            chanceToAddFat = 0.1f;
            minFatScale = 1.25f;
            maxFatScale = 1.75f;
            usesVelocityModificationCurve = true;
            VelocityModificationCurve = Gungeon.Game.Items["chaos_bullets"].GetComponent<ChaosBulletsItem>().VelocityModificationCurve;
            chanceOfActivatingStatusEffect = 0.1f;

            //--------------------------------MODIFIERS
            //SPEED
            speedModifierWeight = 2;
            speedModifierTintColour = Gungeon.Game.Items["chaos_bullets"].GetComponent<ChaosBulletsItem>().SpeedTintColor;
            speedModifierEffect = Gungeon.Game.Items["chaos_bullets"].GetComponent<ChaosBulletsItem>().SpeedModifierEffect;
            //POISON
            poisonModifierWeight = 2;
            poisonModifierTintColour = Gungeon.Game.Items["chaos_bullets"].GetComponent<ChaosBulletsItem>().PoisonTintColor;
            poisonModifierEffect = Gungeon.Game.Items["chaos_bullets"].GetComponent<ChaosBulletsItem>().HealthModifierEffect;
            //FIRE
            fireModifierWeight = 2;
            fireModifierTintColour = Gungeon.Game.Items["chaos_bullets"].GetComponent<ChaosBulletsItem>().FireTintColor;
            fireModifierEffect = Gungeon.Game.Items["chaos_bullets"].GetComponent<ChaosBulletsItem>().FireModifierEffect;
            //CHARM
            charmModifierWeight = 1;
            charmModifierTintColour = Gungeon.Game.Items["chaos_bullets"].GetComponent<ChaosBulletsItem>().CharmTintColor;
            charmModifierEffect = Gungeon.Game.Items["chaos_bullets"].GetComponent<ChaosBulletsItem>().CharmModifierEffect;
            //FREEZE
            freezeModifierWeight = 1;
            freezeModifierTintColour = Gungeon.Game.Items["chaos_bullets"].GetComponent<ChaosBulletsItem>().FreezeTintColor;
            freezeModifierEffect = Gungeon.Game.Items["chaos_bullets"].GetComponent<ChaosBulletsItem>().FreezeModifierEffect;
            //TRANSMOG
            transmogModifierWeight = 0.05f;
            transmogModifierTintColour = Gungeon.Game.Items["chaos_bullets"].GetComponent<ChaosBulletsItem>().TransmogrifyTintColor;
            transmogTargetGuid = "76bc43539fc24648bff4568c75c686d1";
        }

        public void Start()
        {
            try
            {
                this.m_projectile = base.GetComponent<Projectile>();
                DoEffects(this.m_projectile);
            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
                ETGModConsole.Log(e.StackTrace);
            }
        }
        private void DoEffects(Projectile projectile)
        {
            if (usesVelocityModificationCurve)
            {
                projectile.baseData.speed *= this.VelocityModificationCurve.Evaluate(UnityEngine.Random.value);
            }
            int num = 0;
            while (UnityEngine.Random.value < this.chanceToAddBounce && num < 10)
            {
                num++;
                BounceProjModifier bounceProjModifier = projectile.GetComponent<BounceProjModifier>();
                if (bounceProjModifier == null)
                {
                    bounceProjModifier = projectile.gameObject.AddComponent<BounceProjModifier>();
                    bounceProjModifier.numberOfBounces = 1;
                }
                else
                {
                    bounceProjModifier.numberOfBounces++;
                }
            }
            num = 0;
            while (UnityEngine.Random.value < this.chanceToAddPierce && num < 10)
            {
                num++;
                PierceProjModifier pierceProjModifier = projectile.GetComponent<PierceProjModifier>();
                if (pierceProjModifier == null)
                {
                    pierceProjModifier = projectile.gameObject.AddComponent<PierceProjModifier>();
                    pierceProjModifier.penetration = 2;
                    pierceProjModifier.penetratesBreakables = true;
                    pierceProjModifier.BeastModeLevel = PierceProjModifier.BeastModeStatus.NOT_BEAST_MODE;
                }
                else
                {
                    pierceProjModifier.penetration += 2;
                }
            }
            if (UnityEngine.Random.value < this.chanceToAddFat)
            { projectile.AdditionalScaleMultiplier *= UnityEngine.Random.Range(this.minFatScale, this.maxFatScale); }

            if (this.chanceOfActivatingStatusEffect < 1f)
            {
                this.chanceOfActivatingStatusEffect *= effectScaler;
            }
            if (UnityEngine.Random.value < chanceOfActivatingStatusEffect)
            {
                Color targetTintColor = Color.white;
                float combinedWeights = this.speedModifierWeight + this.poisonModifierWeight + this.freezeModifierWeight + this.charmModifierWeight + this.fireModifierWeight + this.transmogModifierWeight;
                float selectedWeight = combinedWeights * UnityEngine.Random.value;
                if (selectedWeight < this.speedModifierWeight)
                {
                    //SPEED
                    targetTintColor = this.speedModifierTintColour;
                    projectile.statusEffectsToApply.Add(this.speedModifierEffect);
                }
                else if (selectedWeight < this.speedModifierWeight + this.poisonModifierWeight)
                {
                    //POISON
                    targetTintColor = this.poisonModifierTintColour;
                    projectile.statusEffectsToApply.Add(this.poisonModifierEffect);
                }
                else if (selectedWeight < this.speedModifierWeight + this.poisonModifierWeight + this.freezeModifierWeight)
                {
                    //FREEZE
                    targetTintColor = this.freezeModifierTintColour;
                    projectile.statusEffectsToApply.Add(freezeModifierEffect);
                }
                else if (selectedWeight < this.speedModifierWeight + this.poisonModifierWeight + this.freezeModifierWeight + this.charmModifierWeight)
                {
                    //CHARM
                    targetTintColor = this.charmModifierTintColour;
                    projectile.statusEffectsToApply.Add(this.charmModifierEffect);
                }
                else if (selectedWeight < this.speedModifierWeight + this.poisonModifierWeight + this.freezeModifierWeight + this.charmModifierWeight + this.fireModifierWeight)
                {
                    //FIRE
                    targetTintColor = this.fireModifierTintColour;
                    projectile.statusEffectsToApply.Add(this.fireModifierEffect);
                }
                else if (selectedWeight < this.speedModifierWeight + this.poisonModifierWeight + this.freezeModifierWeight + this.charmModifierWeight + this.fireModifierWeight + this.transmogModifierWeight)
                {
                    //TRANSMOGRIFY
                    targetTintColor = this.transmogModifierTintColour;
                    projectile.CanTransmogrify = true;
                    projectile.ChanceToTransmogrify = 1f;
                    projectile.TransmogrifyTargetGuids = new string[1];
                    projectile.TransmogrifyTargetGuids[0] = this.transmogTargetGuid;
                }

                projectile.AdjustPlayerProjectileTint(targetTintColor, 6, 0f);

            }
        }

        private Projectile m_projectile;
        //Non status effect things
        public float effectScaler;
        public float chanceToAddPierce;
        public float chanceToAddBounce;
        public float chanceToAddFat;
        public float maxFatScale;
        public float minFatScale;
        //Velocity
        public bool usesVelocityModificationCurve;
        public AnimationCurve VelocityModificationCurve;
        //Effect weights
        public float chanceOfActivatingStatusEffect;
        //Speed
        public float speedModifierWeight;
        public GameActorSpeedEffect speedModifierEffect;
        public Color speedModifierTintColour;
        //Poison
        public float poisonModifierWeight;
        public GameActorHealthEffect poisonModifierEffect;
        public Color poisonModifierTintColour;
        //Fire
        public float fireModifierWeight;
        public GameActorFireEffect fireModifierEffect;
        public Color fireModifierTintColour;
        //Charm
        public float charmModifierWeight;
        public GameActorCharmEffect charmModifierEffect;
        public Color charmModifierTintColour;
        //Freeze
        public float freezeModifierWeight;
        public GameActorFreezeEffect freezeModifierEffect;
        public Color freezeModifierTintColour;
        //Transmog
        public float transmogModifierWeight;
        public string transmogTargetGuid;
        public Color transmogModifierTintColour;

    } //Chaos Bullets as a Projectile Component.
    public class BloodthirstyBulletsComp : MonoBehaviour
    {
        public BloodthirstyBulletsComp()
        {
            this.jamChance = 0.4f;
            this.nonJamDamageMult = 15;
        }

        public void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            this.m_projectile.OnHitEnemy += this.OnHitEnemy;
        }
        private void OnHitEnemy(Projectile arg1, SpeculativeRigidbody arg2, bool arg3)
        {
            if (arg2 != null && arg1.ProjectilePlayerOwner() && arg2.aiActor != null)
            {
                if (AllJammedState.AllJammedActive == false)
                {
                    if (arg2.aiActor.IsBlackPhantom) return;
                    else if (arg1.ProjectilePlayerOwner().CurrentGun.PickupObjectId == 17)
                    {
                        arg2.aiActor.healthHaver.ApplyDamage(2.2f, Vector2.zero, "BonusDamage", CoreDamageTypes.None, DamageCategory.Normal, true, null, false);
                    }
                    else if (!arg2.healthHaver.IsDead && !arg2.healthHaver.IsBoss)
                    {
                        float procChance = UnityEngine.Random.value;
                        if (UnityEngine.Random.value <= this.jamChance)
                        {
                            arg2.aiActor.BecomeBlackPhantom();
                        }
                        else
                        {
                            arg2.aiActor.healthHaver.ApplyDamage(arg1.baseData.damage * nonJamDamageMult, Vector2.zero, "BonusDamage", CoreDamageTypes.None, DamageCategory.Normal, true, null, false);
                        }
                    }
                }
            }
        }
        private Projectile m_projectile;
        public float jamChance;
        public float nonJamDamageMult;
    } //The Bloodthirsty Bullets Effect as a Projectile Component.




    //TECHNICALLY NOT NEW COMPONENTS, BUT NEW TYPES OF PROJECTILE
    public class SuperPierceProjectile : Projectile
    {
        public SuperPierceProjectile()
        {
            this.shouldRemoveCooldown = true;
        }
        public bool shouldRemoveCooldown;
        public override void OnRigidbodyCollision(CollisionData rigidbodyCollision)
        {
            base.OnRigidbodyCollision(rigidbodyCollision);
            if (shouldRemoveCooldown)
            {
                //remove the cooldown
                base.specRigidbody.DeregisterTemporaryCollisionException(rigidbodyCollision.OtherRigidbody);
                rigidbodyCollision.OtherRigidbody.DeregisterTemporaryCollisionException(base.specRigidbody);
            }
        }
    } //A projectile with no cooldown on how many times it can hit an enemy per second.

}
