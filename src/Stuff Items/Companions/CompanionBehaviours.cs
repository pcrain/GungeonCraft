using Alexandria.Misc;
using Dungeonator;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NevernamedsItems
{
    public class CustomCompanionBehaviours
    {
        public class SimpleCompanionMeleeAttack : AttackBehaviorBase
        {
            public override BehaviorResult Update()
            {
                base.DecrementTimer(ref this.attackTimer, false);
                if (Owner == null)
                {
                    if (this.m_aiActor && this.m_aiActor.CompanionOwner)
                    {
                        Owner = this.m_aiActor.CompanionOwner;
                    }
                    else
                    {
                        Owner = GameManager.Instance.BestActivePlayer;
                    }
                }

                BehaviorResult result;
                if (this.m_aiActor && this.m_aiActor.OverrideTarget)
                {
                    SpeculativeRigidbody overrideTarget = this.m_aiActor.OverrideTarget;
                    this.isInRange = (Vector2.Distance(this.m_aiActor.specRigidbody.UnitCenter, overrideTarget.UnitCenter) <= this.DesiredDistance);
                    if (isInRange)
                    {

                        if (overrideTarget != null && this.attackTimer == 0)
                        {
                            if (overrideTarget.healthHaver) overrideTarget.healthHaver.ApplyDamage(this.DamagePerTick, this.m_aiActor.transform.position.CalculateVectorBetween(overrideTarget.transform.position), "Companion Melee", CoreDamageTypes.None, DamageCategory.Normal, false, null, false);
                            if ((targetKnockbackAmount) != 0 && (overrideTarget.knockbackDoer != null)) overrideTarget.knockbackDoer.ApplyKnockback(this.m_aiActor.transform.position.CalculateVectorBetween(overrideTarget.transform.position), targetKnockbackAmount);
                            if ((selfKnockbackAmount) != 0 && (this.m_aiActor.knockbackDoer != null)) this.m_aiActor.knockbackDoer.ApplyKnockback(overrideTarget.transform.position.CalculateVectorBetween(this.m_aiActor.transform.position), selfKnockbackAmount);
                            ETGModConsole.Log("ATTACKED!");
                            this.attackTimer = this.TickDelay;
                            result = BehaviorResult.SkipAllRemainingBehaviors;
                        }
                        else result = BehaviorResult.Continue;
                    }
                    else result = BehaviorResult.Continue;
                }
                else result = BehaviorResult.Continue;
                return result;
            }

            public override float GetMaxRange()
            {
                return 5f;
            }
            public override float GetMinReadyRange()
            {
                return 5f;
            }
            public override bool IsReady()
            {
                AIActor aiActor = this.m_aiActor;
                bool flag;
                if (aiActor == null) flag = true;
                else
                {
                    SpeculativeRigidbody targetRigidbody = aiActor.TargetRigidbody;
                    Vector2? vector = (targetRigidbody != null) ? new Vector2?(targetRigidbody.UnitCenter) : null;
                    flag = (vector == null);
                }
                bool flag2 = flag;
                return !flag2 && Vector2.Distance(this.m_aiActor.specRigidbody.UnitCenter, this.m_aiActor.TargetRigidbody.UnitCenter) <= this.GetMinReadyRange();
            }
            public bool findTarget = true;
            public float selfKnockbackAmount;
            public float targetKnockbackAmount;
            private PlayerController Owner;
            public float DamagePerTick = 5;
            public float TickDelay = 1;
            public float DesiredDistance = 3;
            private float attackTimer;
            private bool isInRange;

        }
        public class SimpleCompanionApproach : MovementBehaviorBase
        {
            public override void Init(GameObject gameObject, AIActor aiActor, AIShooter aiShooter) { base.Init(gameObject, aiActor, aiShooter); }
            public override void Upkeep()
            {
                base.Upkeep();
                base.DecrementTimer(ref this.repathTimer, false);
            }
            public override BehaviorResult Update()
            {
                if (Owner == null)
                {
                    if (this.m_aiActor && this.m_aiActor.CompanionOwner)
                    {
                        Owner = this.m_aiActor.CompanionOwner;
                    }
                    else
                    {
                        Owner = GameManager.Instance.BestActivePlayer;
                    }
                }
                SpeculativeRigidbody overrideTarget = this.m_aiActor.OverrideTarget;
                BehaviorResult result;
                if (this.repathTimer > 0f)
                {
                    result = ((overrideTarget == null || (overrideTarget.healthHaver && overrideTarget.healthHaver.IsDead)) ? BehaviorResult.Continue : BehaviorResult.SkipRemainingClassBehaviors);
                }
                else
                {
                    if (overrideTarget == null || (overrideTarget.healthHaver && overrideTarget.healthHaver.IsDead))
                    {
                        this.PickNewTarget();
                        result = BehaviorResult.Continue;
                    }
                    else
                    {
                        this.isInRange = (Vector2.Distance(this.m_aiActor.specRigidbody.UnitCenter, overrideTarget.UnitCenter) <= this.DesiredDistance);
                        if (overrideTarget != null && !this.isInRange)
                        {
                            this.m_aiActor.PathfindToPosition(overrideTarget.UnitCenter, null, true, null, null, null, false);
                            this.repathTimer = this.PathInterval;
                            result = BehaviorResult.SkipRemainingClassBehaviors;
                        }
                        else
                        {
                            if (overrideTarget != null && this.repathTimer >= 0f)
                            {
                                this.m_aiActor.ClearPath();
                                this.repathTimer = -1f;
                            }
                            result = BehaviorResult.Continue;
                        }
                    }
                }
                return result;
            }
            private void PickNewTarget()
            {
                if (this.m_aiActor != null)
                {
                    if (this.Owner == null)
                    {
                        if (this.m_aiActor && this.m_aiActor.CompanionOwner)
                        {
                            Owner = this.m_aiActor.CompanionOwner;
                        }
                        else
                        {
                            Owner = GameManager.Instance.BestActivePlayer;
                        }
                    }
                    this.Owner.CurrentRoom.GetActiveEnemies(RoomHandler.ActiveEnemyType.RoomClear, ref this.roomEnemies);
                    for (int i = 0; i < this.roomEnemies.Count; i++)
                    {
                        AIActor aiactor = this.roomEnemies[i];
                        if (aiactor.IsHarmlessEnemy || !aiactor.IsNormalEnemy || aiactor.healthHaver.IsDead || aiactor == this.m_aiActor || aiactor.EnemyGuid == "ba928393c8ed47819c2c5f593100a5bc")
                        { this.roomEnemies.Remove(aiactor); }
                    }
                    if (this.roomEnemies.Count == 0) { this.m_aiActor.OverrideTarget = null; }
                    else
                    {
                        AIActor aiActor = this.m_aiActor;
                        AIActor aiactor2 = this.roomEnemies[UnityEngine.Random.Range(0, this.roomEnemies.Count)];
                        aiActor.OverrideTarget = ((aiactor2 != null) ? aiactor2.specRigidbody : null);
                    }
                }
            }
            
            public float PathInterval = 0.25f;
            public float DesiredDistance = 5f;
            private float repathTimer;
            private List<AIActor> roomEnemies = new List<AIActor>();
            private bool isInRange;
            private PlayerController Owner;
        }
        public class PottyCompanionApproach : MovementBehaviorBase
        {
            public override void Init(GameObject gameObject, AIActor aiActor, AIShooter aiShooter) { base.Init(gameObject, aiActor, aiShooter); }
            public override void Upkeep()
            {
                base.Upkeep();
                base.DecrementTimer(ref this.repathTimer, false);
            }
            public override BehaviorResult Update()
            {
                SpeculativeRigidbody overrideTarget = this.m_aiActor.OverrideTarget;
                BehaviorResult result;
                if (this.repathTimer > 0f)
                {
                    result = ((overrideTarget == null || (overrideTarget.healthHaver && overrideTarget.healthHaver.IsDead)) ? BehaviorResult.Continue : BehaviorResult.SkipRemainingClassBehaviors);
                }
                else
                {
                    if (overrideTarget == null || (overrideTarget.healthHaver && overrideTarget.healthHaver.IsDead))
                    {
                        this.PickNewTarget();
                        result = BehaviorResult.Continue;
                    }
                    else
                    {
                        this.isInRange = (Vector2.Distance(this.m_aiActor.specRigidbody.UnitCenter, overrideTarget.UnitCenter) <= this.DesiredDistance);
                        if (overrideTarget != null && !this.isInRange)
                        {
                            this.m_aiActor.PathfindToPosition(overrideTarget.UnitCenter, null, true, null, null, null, false);
                            this.repathTimer = this.PathInterval;
                            result = BehaviorResult.SkipRemainingClassBehaviors;
                        }
                        else
                        {
                            if (overrideTarget != null && this.repathTimer >= 0f)
                            {
                                this.m_aiActor.ClearPath();
                                this.repathTimer = -1f;
                            }
                            result = BehaviorResult.Continue;
                        }
                    }
                }
                return result;
            }
            private void PickNewTarget()
            {
                if (this.m_aiActor != null)
                {
                    if (this.Owner == null)
                    {
                        this.Owner = this.m_aiActor.GetComponent<Potty.PottyCompanionBehaviour>().Owner;
                    }
                    this.Owner.CurrentRoom.GetActiveEnemies(RoomHandler.ActiveEnemyType.RoomClear, ref this.roomEnemies);
                    for (int i = 0; i < this.roomEnemies.Count; i++)
                    {
                        AIActor aiactor = this.roomEnemies[i];
                        if (aiactor.IsHarmlessEnemy || !aiactor.IsNormalEnemy || aiactor.healthHaver.IsDead || aiactor == this.m_aiActor || aiactor.EnemyGuid == "ba928393c8ed47819c2c5f593100a5bc")
                        { this.roomEnemies.Remove(aiactor); }
                    }
                    if (this.roomEnemies.Count == 0) { this.m_aiActor.OverrideTarget = null; }
                    else
                    {
                        AIActor aiActor = this.m_aiActor;
                        AIActor aiactor2 = this.roomEnemies[UnityEngine.Random.Range(0, this.roomEnemies.Count)];
                        aiActor.OverrideTarget = ((aiactor2 != null) ? aiactor2.specRigidbody : null);
                    }
                }
            }
            public float PathInterval = 0.25f;
            public float DesiredDistance = 3f;
            private float repathTimer;
            private List<AIActor> roomEnemies = new List<AIActor>();
            private bool isInRange;
            private PlayerController Owner;
        }
        public class LeadOfLifeCompanionApproach : MovementBehaviorBase
        {
            public override void Init(GameObject gameObject, AIActor aiActor, AIShooter aiShooter) { base.Init(gameObject, aiActor, aiShooter); }
            public override void Upkeep()
            {
                base.Upkeep();
                base.DecrementTimer(ref this.repathTimer, false);
                base.DecrementTimer(ref this.attackTimer, false);
            }
            public override BehaviorResult Update()
            {
                SpeculativeRigidbody overrideTarget = this.m_aiActor.OverrideTarget;
                BehaviorResult result;
                if (this.repathTimer > 0f)
                {
                    result = ((overrideTarget == null || (overrideTarget.healthHaver && overrideTarget.healthHaver.IsDead)) ? BehaviorResult.Continue : BehaviorResult.SkipRemainingClassBehaviors);
                }
                else
                {
                    if (overrideTarget == null || (overrideTarget.healthHaver && overrideTarget.healthHaver.IsDead))
                    {
                        this.PickNewTarget();
                        result = BehaviorResult.Continue;
                    }
                    else
                    {
                        this.isInRange = (Vector2.Distance(this.m_aiActor.specRigidbody.UnitCenter, overrideTarget.UnitCenter) <= this.DesiredDistance);
                        if (overrideTarget != null && !this.isInRange)
                        {
                            this.m_aiActor.PathfindToPosition(overrideTarget.UnitCenter, null, true, null, null, null, false);
                            this.repathTimer = this.PathInterval;
                            result = BehaviorResult.SkipRemainingClassBehaviors;
                        }
                        else
                        {
                            if (isZombieBullets)
                            {
                                if (overrideTarget != null && this.attackTimer == 0)
                                {
                                    overrideTarget.healthHaver.ApplyDamage(5f, this.m_aiActor.specRigidbody.Velocity, "Zombie Bullets!", CoreDamageTypes.None, DamageCategory.Normal, false, null, false);
                                    this.attackTimer = 0.5f;
                                }
                            }
                            if (overrideTarget != null && this.repathTimer >= 0f)
                            {
                                this.m_aiActor.ClearPath();
                                this.repathTimer = -1f;
                            }
                            result = BehaviorResult.Continue;
                        }
                    }
                }
                return result;
            }
            private void PickNewTarget()
            {
                if (this.m_aiActor != null)
                {
                    if (this.Owner == null)
                    {
                        this.Owner = this.m_aiActor.GetComponent<LeadOfLifeCompanion>().Owner;
                    }
                    this.Owner.CurrentRoom.GetActiveEnemies(RoomHandler.ActiveEnemyType.RoomClear, ref this.roomEnemies);
                    for (int i = 0; i < this.roomEnemies.Count; i++)
                    {
                        AIActor aiactor = this.roomEnemies[i];
                        if (aiactor.IsHarmlessEnemy || !aiactor.IsNormalEnemy || aiactor.healthHaver.IsDead || aiactor == this.m_aiActor || aiactor.EnemyGuid == "ba928393c8ed47819c2c5f593100a5bc")
                        { this.roomEnemies.Remove(aiactor); }
                    }
                    if (this.roomEnemies.Count == 0) { this.m_aiActor.OverrideTarget = null; }
                    else
                    {
                        AIActor aiActor = this.m_aiActor;
                        AIActor aiactor2 = this.roomEnemies[UnityEngine.Random.Range(0, this.roomEnemies.Count)];
                        aiActor.OverrideTarget = ((aiactor2 != null) ? aiactor2.specRigidbody : null);
                    }
                }
            }
            public float PathInterval = 0.25f;
            public float DesiredDistance = 5f;
            public bool isZombieBullets = false;

            private float attackTimer;
            private float repathTimer;
            private List<AIActor> roomEnemies = new List<AIActor>();
            private bool isInRange;
            private PlayerController Owner;
        }
        public class PeanutAttackBehaviour : AttackBehaviorBase
        {
            public override void Destroy() { base.Destroy(); }
            public override void Init(GameObject gameObject, AIActor aiActor, AIShooter aiShooter)
            {
                base.Init(gameObject, aiActor, aiShooter);
                this.Owner = this.m_aiActor.GetComponent<Peanut.PeanutCompanionBehaviour>().Owner;
            }
            public override BehaviorResult Update()
            {
                if (this.attackTimer > 0f && this.isAttacking) { base.DecrementTimer(ref this.attackTimer, false); }
                else
                {
                    if (this.attackCooldownTimer > 0f && !this.isAttacking) { base.DecrementTimer(ref this.attackCooldownTimer, false); }
                }
                BehaviorResult result;
                if ((!this.IsReady() || this.attackCooldownTimer > 0f || this.attackTimer == 0f || this.m_aiActor.TargetRigidbody == null) && this.isAttacking)
                {
                    this.StopAttacking();
                    result = BehaviorResult.Continue;
                }
                else
                {
                    if (this.IsReady() && this.attackCooldownTimer == 0f && !this.isAttacking)
                    {
                        this.attackTimer = this.attackDuration;
                        this.isAttacking = true;
                        this.m_aiActor.StartCoroutine(Attack());
                        result = BehaviorResult.SkipAllRemainingBehaviors;
                    }
                    else result = BehaviorResult.Continue;
                }
                return result;
            }
            private void StopAttacking()
            {
                this.isAttacking = false;
                this.attackTimer = 0f;
                this.attackCooldownTimer = this.attackCooldown;
            }
            public AIActor GetNearestEnemy(List<AIActor> activeEnemies, Vector2 position, out float nearestDistance, string[] filter)
            {
                AIActor aiactor = null;
                nearestDistance = float.MaxValue;
                AIActor result;
                if (activeEnemies == null) result = null;
                else
                {
                    for (int i = 0; i < activeEnemies.Count; i++)
                    {
                        AIActor aiactor2 = activeEnemies[i];
                        if (aiactor2.healthHaver && aiactor2.healthHaver.IsVulnerable)
                        {
                            if (!aiactor2.healthHaver.IsDead)
                            {
                                if (filter == null || !filter.Contains(aiactor2.EnemyGuid))
                                {
                                    float num = Vector2.Distance(position, aiactor2.CenterPosition);
                                    if (num < nearestDistance)
                                    {
                                        nearestDistance = num;
                                        aiactor = aiactor2;
                                    }
                                }
                            }
                        }
                    }
                    result = aiactor;
                }
                return result;
            }
            private IEnumerator Attack()
            {
                if (this.Owner == null) this.Owner = this.m_aiActor.GetComponent<Peanut.PeanutCompanionBehaviour>().Owner;
                float num = -1f;
                List<AIActor> activeEnemies = this.Owner.CurrentRoom.GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
                if (!(activeEnemies == null | activeEnemies.Count <= 0))
                {
                    AIActor nearestEnemy = this.GetNearestEnemy(activeEnemies, this.m_aiActor.sprite.WorldCenter, out num, null);
                    if (nearestEnemy && num < 10f)
                    {
                        if (this.IsInRange(nearestEnemy))
                        {
                            if (!nearestEnemy.IsHarmlessEnemy && nearestEnemy.IsNormalEnemy && !nearestEnemy.healthHaver.IsDead && nearestEnemy != this.m_aiActor)
                            {
                                //Determine angle of target
                                Vector2 unitCenter = this.m_aiActor.specRigidbody.UnitCenter;
                                Vector2 unitCenter2 = nearestEnemy.specRigidbody.HitboxPixelCollider.UnitCenter;
                                float z = BraveMathCollege.Atan2Degrees((unitCenter2 - unitCenter).normalized);
                                z = (float)Math.Round(z, 1);

                                //Setup Variables
                                Vector2 vectorToSpawn = Vector2.zero;
                                string AnimationName = null;
                                float modifiedAngle = 0;

                                //Determine snapped angle of target
                                if (z.IsBetweenRange(-22.5f, 22.5f)) { modifiedAngle = 0; AnimationName = "attack_east"; vectorToSpawn = new Vector2(1, 0); }
                                else if (z.IsBetweenRange(22.6f, 67.5f))
                                {
                                    modifiedAngle = 45;
                                    if (z.IsBetweenRange(22.6f, 45f)) { AnimationName = "attack_east"; vectorToSpawn = new Vector2(1, 0); }
                                    else { AnimationName = "attack_north"; vectorToSpawn = new Vector2(0, 1); }
                                }
                                else if (z.IsBetweenRange(67.6f, 112.5f)) { modifiedAngle = 90; AnimationName = "attack_north"; vectorToSpawn = new Vector2(0, 1); }
                                else if (z.IsBetweenRange(112.6f, 157.5f))
                                {
                                    modifiedAngle = 135;
                                    if (z.IsBetweenRange(112.6f, 135f)) { AnimationName = "attack_north"; vectorToSpawn = new Vector2(0, 1); }
                                    else { AnimationName = "attack_west"; vectorToSpawn = new Vector2(-1, 0); }
                                }
                                else if (z.IsBetweenRange(157.6f, 180f) || z.IsBetweenRange(-157.6f, -180f)) { modifiedAngle = 180; AnimationName = "attack_west"; vectorToSpawn = new Vector2(-1, 0); }
                                else if (z.IsBetweenRange(-67.5f, -22.5f))
                                {
                                    modifiedAngle = 315;
                                    if (z.IsBetweenRange(-45, -22.5f)) { AnimationName = "attack_east"; vectorToSpawn = new Vector2(1, 0); }
                                    else { AnimationName = "attack_south"; vectorToSpawn = new Vector2(0, -1); }
                                }
                                else if (z.IsBetweenRange(-112.5f, -67.6f)) { modifiedAngle = 270; AnimationName = "attack_south"; vectorToSpawn = new Vector2(0, -1); }
                                else if (z.IsBetweenRange(-157.5f, -112.6f))
                                {
                                    modifiedAngle = 225;
                                    if (z.IsBetweenRange(-157.5f, -136f)) { AnimationName = "attack_west"; vectorToSpawn = new Vector2(-1, 0); }
                                    else { AnimationName = "attack_south"; vectorToSpawn = new Vector2(0, -1); }
                                }
                                else
                                {
                                    ETGModConsole.Log("Peanut attempted to attack in an unrecognised direction, and gave up." + "\nDirection: " + z + "\nIf you see this error, report it to the mod creator with a Screenshot of this message, please.");
                                    yield break;
                                }

                                //Determine offset and play animation
                                if (this.m_aiActor.aiAnimator)
                                { this.m_aiActor.aiAnimator.PlayUntilFinished(AnimationName, false, null, -1f, false); }

                                //Delay
                                yield return new WaitForSeconds(delayBetweenAttackAndProjectileSpawn);

                                //Spawn projectiles
                                float startRotation = -30;
                                for (int i = 0; i < 7; i++)
                                {
                                    float actualDirection = modifiedAngle + startRotation;
                                    bool shouldDoDMGUp = true;
                                    Projectile projectile = ((Gun)ETGMod.Databases.Items[197]).DefaultModule.projectiles[0];
                                    if (Owner.PlayerHasActiveSynergy("Pealadin") && UnityEngine.Random.value <= 0.1f)
                                    {
                                        projectile = ((Gun)ETGMod.Databases.Items[674]).DefaultModule.projectiles[0];
                                        shouldDoDMGUp = false;
                                    }
                                    GameObject gameObject = SpawnManager.SpawnProjectile(projectile.gameObject, (this.m_aiActor.sprite.WorldCenter + vectorToSpawn), Quaternion.Euler(0f, 0f, actualDirection), true);
                                    Projectile component = gameObject.GetComponent<Projectile>();
                                    gameObject.AddComponent<PierceDeadActors>();
                                    if (component != null)
                                    {
                                        component.Owner = Owner;
                                        component.Shooter = this.m_aiActor.specRigidbody;
                                        component.collidesWithPlayer = false;
                                        if (shouldDoDMGUp) component.baseData.damage *= 2;
                                        component.ApplyCompanionModifierToBullet(Owner);
                                        component.baseData.damage *= Owner.stats.GetStatValue(PlayerStats.StatType.Damage);
                                        component.baseData.range *= Owner.stats.GetStatValue(PlayerStats.StatType.RangeMultiplier);
                                        component.baseData.speed *= Owner.stats.GetStatValue(PlayerStats.StatType.ProjectileSpeed);
                                        component.baseData.force *= Owner.stats.GetStatValue(PlayerStats.StatType.KnockbackMultiplier);
                                        component.BossDamageMultiplier *= Owner.stats.GetStatValue(PlayerStats.StatType.DamageToBosses);
                                        component.RuntimeUpdateScale(Owner.stats.GetStatValue(PlayerStats.StatType.PlayerBulletScale));
                                        Owner.DoPostProcessProjectile(component);
                                    }
                                    startRotation += 10f;
                                }
                            }
                        }
                    }
                }
                yield break;
            }
            public override float GetMaxRange()
            {
                return 20f;
            }
            public override float GetMinReadyRange()
            {
                return 15f;
            }
            public override bool IsReady()
            {
                AIActor aiActor = this.m_aiActor;
                bool flag;
                if (aiActor == null)
                {
                    flag = true;
                }
                else
                {
                    SpeculativeRigidbody targetRigidbody = aiActor.TargetRigidbody;
                    Vector2? vector = (targetRigidbody != null) ? new Vector2?(targetRigidbody.UnitCenter) : null;
                    flag = (vector == null);
                }
                bool flag2 = flag;
                return !flag2 && Vector2.Distance(this.m_aiActor.specRigidbody.UnitCenter, this.m_aiActor.TargetRigidbody.UnitCenter) <= this.GetMinReadyRange();
            }
            public bool IsInRange(AIActor enemy)
            {
                bool flag;
                if (enemy == null) flag = true;
                else
                {
                    SpeculativeRigidbody specRigidbody = enemy.specRigidbody;
                    Vector2? vector = (specRigidbody != null) ? new Vector2?(specRigidbody.UnitCenter) : null;
                    flag = (vector == null);
                }
                return !flag && Vector2.Distance(this.m_aiActor.specRigidbody.UnitCenter, enemy.specRigidbody.UnitCenter) <= this.GetMinReadyRange();
            }
            private bool isAttacking;
            private float attackCooldown = 4f;
            private float attackDuration = 0.5f;
            private float attackTimer;
            private float attackCooldownTimer;
            private float delayBetweenAttackAndProjectileSpawn = 0.2f;
            private PlayerController Owner;
            private List<AIActor> roomEnemies = new List<AIActor>();
        }
        public class ChromaGunDroneApproach : MovementBehaviorBase
        {
            public override void Init(GameObject gameObject, AIActor aiActor, AIShooter aiShooter) { base.Init(gameObject, aiActor, aiShooter); }
            public override void Upkeep()
            {
                base.Upkeep();
                base.DecrementTimer(ref this.repathTimer, false);
            }
            private bool Stealthed = false;
            public override BehaviorResult Update()
            {
                if (Owner == null)
                {
                    if (this.m_aiActor && this.m_aiActor.CompanionOwner)
                    {
                        Owner = this.m_aiActor.CompanionOwner;
                    }
                    else
                    {
                        Owner = GameManager.Instance.BestActivePlayer;
                    }
                }
                SpeculativeRigidbody overrideTarget = this.m_aiActor.OverrideTarget;
                BehaviorResult result;
                if ((overrideTarget == null) && (Stealthed == false))
                {
                    this.m_aiActor.PlayEffectOnActor(EasyVFXDatabase.BloodiedScarfPoofVFX, Vector3.zero, false, true, false);
                    this.m_aiActor.ToggleRenderers(false);
                    this.m_aiActor.ToggleShadowVisiblity(false);
                    Stealthed = true;
                }
                else if ((overrideTarget != null) && (Stealthed == true))
                {
                    this.m_aiActor.PlayEffectOnActor(EasyVFXDatabase.BloodiedScarfPoofVFX, Vector3.zero, false, true, false);
                    this.m_aiActor.sprite.renderer.enabled = true;
                    this.m_aiActor.ToggleRenderers(true);
                    this.m_aiActor.ToggleShadowVisiblity(true);
                    Stealthed = false;
                }
                if (this.repathTimer > 0f)
                {
                    result = ((overrideTarget == null) ? BehaviorResult.Continue : BehaviorResult.SkipRemainingClassBehaviors);
                }
                else
                {
                    if (overrideTarget == null || (overrideTarget.healthHaver && overrideTarget.healthHaver.IsDead))
                    {
                        this.PickNewTarget();
                        result = BehaviorResult.Continue;
                    }
                    else
                    {
                        if (overrideTarget.GetComponent<ChromaGun.ChromaGunColoured>() == null || (overrideTarget.GetComponent<ChromaGun.ChromaGunColoured>().ColourType != droneColour)) { overrideTarget = null; this.PickNewTarget(); result = BehaviorResult.Continue; }
                        else
                        {
                            this.isInRange = (Vector2.Distance(this.m_aiActor.specRigidbody.UnitCenter, overrideTarget.UnitCenter) <= this.DesiredDistance);
                            if (overrideTarget != null && !this.isInRange)
                            {
                                this.m_aiActor.PathfindToPosition(overrideTarget.UnitCenter, null, true, null, null, null, false);
                                this.repathTimer = this.PathInterval;
                                result = BehaviorResult.SkipRemainingClassBehaviors;
                            }
                            else
                            {
                                if (overrideTarget != null && this.repathTimer >= 0f)
                                {
                                    this.m_aiActor.ClearPath();
                                    this.repathTimer = -1f;
                                }
                                result = BehaviorResult.Continue;
                            }
                        }
                    }
                }
                return result;
            }
            private void PickNewTarget()
            {
                if (this.m_aiActor != null)
                {
                    if (this.Owner == null)
                    {
                        if (this.m_aiActor && this.m_aiActor.CompanionOwner)
                        {
                            Owner = this.m_aiActor.CompanionOwner;
                        }
                        else
                        {
                            Owner = GameManager.Instance.BestActivePlayer;
                        }
                    }
                    this.Owner.CurrentRoom.GetActiveEnemies(RoomHandler.ActiveEnemyType.RoomClear, ref this.roomEnemies);
                    for (int i = 0; i < this.roomEnemies.Count; i++)
                    {
                        AIActor aiactor = this.roomEnemies[i];
                        if (aiactor.IsHarmlessEnemy || !aiactor.IsNormalEnemy || aiactor.healthHaver.IsDead || aiactor == this.m_aiActor || aiactor.EnemyGuid == "ba928393c8ed47819c2c5f593100a5bc")
                        { this.roomEnemies.Remove(aiactor); }                     
                        if (aiactor.GetComponent<ChromaGun.ChromaGunColoured>() == null) { this.roomEnemies.Remove(aiactor); }
                        else if (aiactor.GetComponent<ChromaGun.ChromaGunColoured>().ColourType != droneColour) { this.roomEnemies.Remove(aiactor); }
                    }
                    if (this.roomEnemies.Count == 0) { this.m_aiActor.OverrideTarget = null; }
                    else
                    {
                        AIActor aiActor = this.m_aiActor;
                        AIActor aiactor2 = this.roomEnemies[UnityEngine.Random.Range(0, this.roomEnemies.Count)];
                        aiActor.OverrideTarget = ((aiactor2 != null) ? aiactor2.specRigidbody : null);
                    }
                }
            }
            public ChromaGun.ColourType droneColour;
            
            public float PathInterval = 0.25f;
            public float DesiredDistance = 5f;
            private float repathTimer;
            private List<AIActor> roomEnemies = new List<AIActor>();
            private bool isInRange;
            private PlayerController Owner;
        }
    }
}
