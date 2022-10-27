using Alexandria.Misc;
using Dungeonator;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using UnityEngine;

namespace CwaffingTheGungy
{
    public class SlashData :ScriptableObject
    {
        public static SlashData CloneSlashData(SlashData original)
        {
            SlashData newData = new SlashData();
            newData.doVFX = original.doVFX;
            newData.VFX = original.VFX;
            newData.doHitVFX = original.doHitVFX;
            newData.hitVFX = original.hitVFX;
            newData.projInteractMode = original.projInteractMode;
            newData.playerKnockbackForce = original.playerKnockbackForce;
            newData.enemyKnockbackForce = original.enemyKnockbackForce;
            newData.statusEffects = original.statusEffects;
            newData.jammedDamageMult = original.jammedDamageMult;
            newData.bossDamageMult = original.bossDamageMult;
            newData.doOnSlash = original.doOnSlash;
            newData.doPostProcessSlash = original.doPostProcessSlash;
            newData.slashRange = original.slashRange;
            newData.slashDegrees = original.slashDegrees;
            newData.damage = original.damage;
            newData.damagesBreakables = original.damagesBreakables;
            newData.soundEvent = original.soundEvent;
            newData.OnHitTarget = original.OnHitTarget;
            newData.OnHitBullet = original.OnHitBullet;
            newData.OnHitMinorBreakable = original.OnHitMinorBreakable;
            newData.OnHitMajorBreakable = original.OnHitMajorBreakable;
            return newData;
        }

        public bool doVFX = true;
        public VFXPool VFX = (PickupObjectDatabase.GetById(417) as Gun).muzzleFlashEffects;
        public bool doHitVFX = true;
        public VFXPool hitVFX = (PickupObjectDatabase.GetById(417) as Gun).DefaultModule.projectiles[0].hitEffects.enemy;
        public SlashDoer.ProjInteractMode projInteractMode = SlashDoer.ProjInteractMode.IGNORE;
        public float playerKnockbackForce = 5;
        public float enemyKnockbackForce = 10;
        public List<GameActorEffect> statusEffects = new List<GameActorEffect>();
        public float jammedDamageMult = 1;
        public float bossDamageMult = 1;
        public bool doOnSlash = true;
        public bool doPostProcessSlash = true;
        public float slashRange = 2.5f;
        public float slashDegrees = 90f;
        public float damage = 5f;
        public bool damagesBreakables = true;
        public string soundEvent = "Play_WPN_blasphemy_shot_01";
        public Action<GameActor, bool> OnHitTarget = null;
        public Action<Projectile> OnHitBullet = null;
        public Action<MinorBreakable> OnHitMinorBreakable = null;
        public Action<MajorBreakable> OnHitMajorBreakable = null;
    }
    public class SlashDoer
    {
        public static void DoSwordSlash(
            Vector2 position,
            float angle,
            GameActor owner,
            SlashData slashParameters,
            Transform parentTransform = null)
        {
            if (slashParameters.doVFX && slashParameters.VFX != null) slashParameters.VFX.SpawnAtPosition(position, angle, parentTransform, null, null, -0.05f);
            if (!string.IsNullOrEmpty( slashParameters.soundEvent) && owner != null && owner.gameObject != null) AkSoundEngine.PostEvent(slashParameters.soundEvent, owner.gameObject);
            GameManager.Instance.StartCoroutine(HandleSlash(position, angle, owner, slashParameters));
        }
        private static IEnumerator HandleSlash(Vector2 position, float angle, GameActor owner, SlashData slashParameters)
        {
            int slashId = Time.frameCount;
            List<SpeculativeRigidbody> alreadyHit = new List<SpeculativeRigidbody>();
            if (slashParameters.playerKnockbackForce != 0f && owner != null) owner.knockbackDoer.ApplyKnockback(BraveMathCollege.DegreesToVector(angle, 1f), slashParameters.playerKnockbackForce, 0.25f, false);
            float ela = 0f;
            while (ela < 0.2f)
            {
                ela += BraveTime.DeltaTime;
                HandleHeroSwordSlash(alreadyHit, position, angle, slashId, owner, slashParameters);
                yield return null;
            }
            yield break;
        }
        public enum ProjInteractMode
        {
            IGNORE,
            DESTROY,
            REFLECT,
            REFLECTANDPOSTPROCESS,
        }
        private static bool SlasherIsPlayerOrFriendly(GameActor slasher)
        {
            if (slasher is PlayerController) return true;
            if (slasher is AIActor)
            {
                if (slasher.GetComponent<CompanionController>()) return true;
                if (!slasher.aiActor.CanTargetPlayers && slasher.aiActor.CanTargetEnemies) return true;
            }
            return false;
        }
        private static bool ProjectileIsValid(Projectile proj, GameActor slashOwner)
        {
            if (proj)
            {
                if (slashOwner == null)
                {
                    return false;
                }
                if (SlasherIsPlayerOrFriendly(slashOwner))
                {
                    if ((proj.Owner && !(proj.Owner is PlayerController)) || proj.ForcePlayerBlankable) return true;
                }
                else if (slashOwner is AIActor)
                {
                    if (proj.Owner && proj.Owner is PlayerController) return true;
                }
                else
                {
                    if (proj.Owner) return true;
                }
            }

            return false;
        }
        private static bool ObjectWasHitBySlash(Vector2 ObjectPosition, Vector2 SlashPosition, float slashAngle, float SlashRange, float SlashDimensions)
        {
            if (Vector2.Distance(ObjectPosition, SlashPosition) < SlashRange)
            {
                float num7 = BraveMathCollege.Atan2Degrees(ObjectPosition - SlashPosition);
                float minRawAngle = Math.Min(SlashDimensions, -SlashDimensions);
                float maxRawAngle = Math.Max(SlashDimensions, -SlashDimensions);
                bool isInRange = false;
                float actualMaxAngle = slashAngle + maxRawAngle;
                float actualMinAngle = slashAngle + minRawAngle;

                if (num7.IsBetweenRange(actualMinAngle, actualMaxAngle)) isInRange = true;
                if (actualMaxAngle > 180)
                {
                    float Overflow = actualMaxAngle - 180;
                    if (num7.IsBetweenRange(-180, (-180 + Overflow))) isInRange = true;
                }
                if (actualMinAngle < -180)
                {
                    float Underflow = actualMinAngle + 180;
                    if (num7.IsBetweenRange((180 + Underflow), 180)) isInRange = true;
                }
                return isInRange;
            }
            return false;
        }
        private static void HandleHeroSwordSlash(List<SpeculativeRigidbody> alreadyHit, Vector2 arcOrigin, float slashAngle, int slashId, GameActor owner, SlashData slashParameters)
        {
            float degreesOfSlash = slashParameters.slashDegrees;
            float slashRange = slashParameters.slashRange;



            ReadOnlyCollection<Projectile> allProjectiles2 = StaticReferenceManager.AllProjectiles;
            for (int j = allProjectiles2.Count - 1; j >= 0; j--)
            {
                Projectile projectile2 = allProjectiles2[j];
                if (ProjectileIsValid(projectile2, owner))
                {
                    Vector2 projectileCenter = projectile2.sprite.WorldCenter;
                    if (ObjectWasHitBySlash(projectileCenter, arcOrigin, slashAngle, slashRange, degreesOfSlash))
                    {
                        if (slashParameters.OnHitBullet != null) slashParameters.OnHitBullet(projectile2);
                        if (slashParameters.projInteractMode != ProjInteractMode.IGNORE || projectile2.collidesWithProjectiles)
                        {
                            if (slashParameters.projInteractMode == ProjInteractMode.DESTROY || slashParameters.projInteractMode == ProjInteractMode.IGNORE) projectile2.DieInAir(false, true, true, true);
                            else if (slashParameters.projInteractMode == ProjInteractMode.REFLECT || slashParameters.projInteractMode == ProjInteractMode.REFLECTANDPOSTPROCESS)
                            {
                                if (projectile2.Owner != null && projectile2.LastReflectedSlashId != slashId)
                                {
                                    projectile2.ReflectBullet(true, owner, 5, (slashParameters.projInteractMode == ProjInteractMode.REFLECTANDPOSTPROCESS), 1, 5, 0, null);
                                    projectile2.LastReflectedSlashId = slashId;
                                }
                            }
                        }
                    }
                }
            }
            DealDamageToEnemiesInArc(owner, arcOrigin, slashAngle, slashRange, slashParameters, alreadyHit);

            if (slashParameters.damagesBreakables)
            {
                List<MinorBreakable> allMinorBreakables = StaticReferenceManager.AllMinorBreakables;
                for (int k = allMinorBreakables.Count - 1; k >= 0; k--)
                {
                    MinorBreakable minorBreakable = allMinorBreakables[k];
                    if (minorBreakable && minorBreakable.specRigidbody)
                    {
                        if (!minorBreakable.IsBroken && minorBreakable.sprite)
                        {
                            if (ObjectWasHitBySlash(minorBreakable.sprite.WorldCenter, arcOrigin, slashAngle, slashRange, degreesOfSlash))
                            {
                                if (slashParameters.OnHitMinorBreakable != null) slashParameters.OnHitMinorBreakable(minorBreakable);
                                minorBreakable.Break();
                            }
                        }
                    }
                }
                List<MajorBreakable> allMajorBreakables = StaticReferenceManager.AllMajorBreakables;
                for (int l = allMajorBreakables.Count - 1; l >= 0; l--)
                {
                    MajorBreakable majorBreakable = allMajorBreakables[l];
                    if (majorBreakable && majorBreakable.specRigidbody)
                    {
                        if (!alreadyHit.Contains(majorBreakable.specRigidbody))
                        {
                            if (!majorBreakable.IsSecretDoor && !majorBreakable.IsDestroyed)
                            {
                                if (ObjectWasHitBySlash(majorBreakable.specRigidbody.UnitCenter, arcOrigin, slashAngle, slashRange, degreesOfSlash))
                                {
                                    float num9 = slashParameters.damage;
                                    if (majorBreakable.healthHaver)
                                    {
                                        num9 *= 0.2f;
                                    }
                                    if (slashParameters.OnHitMajorBreakable != null) slashParameters.OnHitMajorBreakable(majorBreakable);
                                    majorBreakable.ApplyDamage(num9, majorBreakable.specRigidbody.UnitCenter - arcOrigin, false, false, false);
                                    alreadyHit.Add(majorBreakable.specRigidbody);
                                }
                            }
                        }
                    }
                }
            }
        }
        private static void DealDamageToEnemiesInArc(GameActor owner, Vector2 arcOrigin, float arcAngle, float arcRadius, SlashData slashParameters, List<SpeculativeRigidbody> alreadyHit = null)
        {
            RoomHandler roomHandler = arcOrigin.GetAbsoluteRoom();
            if (roomHandler == null) return;
            if (SlasherIsPlayerOrFriendly(owner))
            {
                List<AIActor> activeEnemies = roomHandler.GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
                if (activeEnemies == null) return;

                for (int i = 0; i < activeEnemies.Count; i++)
                {
                    AIActor aiactor = activeEnemies[i];
                    if (!(aiactor && aiactor.specRigidbody && aiactor.IsNormalEnemy && !aiactor.IsGone && aiactor.healthHaver))
                        continue;
                    if (alreadyHit != null && alreadyHit.Contains(aiactor.specRigidbody))
                        continue;
                    for (int j = 0; j < aiactor.healthHaver.NumBodyRigidbodies; j++)
                    {
                        SpeculativeRigidbody bodyRigidbody = aiactor.healthHaver.GetBodyRigidbody(j);
                        PixelCollider hitboxPixelCollider = bodyRigidbody.HitboxPixelCollider;
                        if (hitboxPixelCollider == null)
                            continue;

                        Vector2 vector = BraveMathCollege.ClosestPointOnRectangle(arcOrigin, hitboxPixelCollider.UnitBottomLeft, hitboxPixelCollider.UnitDimensions);
                        if (!ObjectWasHitBySlash(vector, arcOrigin, arcAngle, arcRadius, 90))
                            continue;

                        bool attackIsNotBlocked = true;
                        int rayMask = CollisionMask.LayerToMask(CollisionLayer.HighObstacle, CollisionLayer.BulletBlocker, CollisionLayer.BulletBreakable);
                        RaycastResult raycastResult;
                        if (PhysicsEngine.Instance.Raycast(arcOrigin, vector - arcOrigin, Vector2.Distance(vector, arcOrigin), out raycastResult, true, true, rayMask, null, false, null, null) && raycastResult.SpeculativeRigidbody != bodyRigidbody)
                            attackIsNotBlocked = false;
                        RaycastResult.Pool.Free(ref raycastResult);
                        if (!attackIsNotBlocked)
                            continue;

                        float damage = DealSwordDamageToEnemy(owner, aiactor, arcOrigin, vector, arcAngle, slashParameters);
                        if (alreadyHit != null)
                        {
                            if (alreadyHit.Count == 0)
                                StickyFrictionManager.Instance.RegisterSwordDamageStickyFriction(damage);
                            alreadyHit.Add(aiactor.specRigidbody);
                        }
                        break;
                    }
                }
            }
            else
            {
                List<PlayerController> AllPlayers = new List<PlayerController>();
                if (GameManager.Instance.PrimaryPlayer) AllPlayers.Add(GameManager.Instance.PrimaryPlayer);
                if (GameManager.Instance.SecondaryPlayer) AllPlayers.Add(GameManager.Instance.SecondaryPlayer);
                for (int i = 0; i < AllPlayers.Count; i++)
                {
                    PlayerController player = AllPlayers[i];
                    if (!(player && player.specRigidbody && player.healthHaver && !player.IsGhost))
                        continue;

                    if (alreadyHit != null && alreadyHit.Contains(player.specRigidbody))
                        continue;

                    SpeculativeRigidbody bodyRigidbody = player.specRigidbody;
                    PixelCollider hitboxPixelCollider = bodyRigidbody.HitboxPixelCollider;
                    if (hitboxPixelCollider == null)
                        continue;

                    Vector2 vector = BraveMathCollege.ClosestPointOnRectangle(arcOrigin, hitboxPixelCollider.UnitBottomLeft, hitboxPixelCollider.UnitDimensions);
                    if (!ObjectWasHitBySlash(vector, arcOrigin, arcAngle, arcRadius, 90))
                        continue;

                    bool attackIsNotBlocked = true;
                    int rayMask = CollisionMask.LayerToMask(CollisionLayer.HighObstacle, CollisionLayer.BulletBlocker, CollisionLayer.BulletBreakable);
                    RaycastResult raycastResult;
                    if (PhysicsEngine.Instance.Raycast(arcOrigin, vector - arcOrigin, Vector2.Distance(vector, arcOrigin), out raycastResult, true, true, rayMask, null, false, null, null) && raycastResult.SpeculativeRigidbody != bodyRigidbody)
                        attackIsNotBlocked = false;
                    RaycastResult.Pool.Free(ref raycastResult);
                    if (!attackIsNotBlocked)
                        continue;

                    float damage = DealSwordDamageToEnemy(owner, player, arcOrigin, vector, arcAngle, slashParameters);
                    if (alreadyHit != null)
                    {
                        if (alreadyHit.Count == 0)
                            StickyFrictionManager.Instance.RegisterSwordDamageStickyFriction(damage);
                        alreadyHit.Add(player.specRigidbody);
                    }
                    break;
                }

            }
        }
        private static float DealSwordDamageToEnemy(GameActor owner, GameActor targetEnemy, Vector2 arcOrigin, Vector2 contact, float angle, SlashData slashParameters)
        {
            if (targetEnemy.healthHaver)
            {
                float damageToDeal = slashParameters.damage;
                if (targetEnemy.healthHaver && targetEnemy.healthHaver.IsBoss) damageToDeal *= slashParameters.bossDamageMult;
                if ((targetEnemy is AIActor) && (targetEnemy as AIActor).IsBlackPhantom) damageToDeal *= slashParameters.jammedDamageMult;
                DamageCategory category = DamageCategory.Normal;
                if ((owner is AIActor) && (owner as AIActor).IsBlackPhantom) category = DamageCategory.BlackBullet;

                bool wasAlivePreviously = targetEnemy.healthHaver.IsAlive;
                //VFX
                if (slashParameters.doHitVFX && slashParameters.hitVFX != null)
                {
                    slashParameters.hitVFX.SpawnAtPosition(new Vector3(contact.x, contact.y), 0, targetEnemy.transform);
                }
                targetEnemy.healthHaver.ApplyDamage(damageToDeal, contact - arcOrigin, owner.ActorName, CoreDamageTypes.None, category, false, null, false);

                bool fatal = false;
                if (wasAlivePreviously && targetEnemy.healthHaver.IsDead) fatal = true;

                if (slashParameters.OnHitTarget != null) slashParameters.OnHitTarget(targetEnemy, fatal);
            }
            if (targetEnemy.knockbackDoer)
            {
                targetEnemy.knockbackDoer.ApplyKnockback(contact - arcOrigin, slashParameters.enemyKnockbackForce, false);
            }
            if (slashParameters.statusEffects != null && slashParameters.statusEffects.Count > 0)
            {
                foreach (GameActorEffect effect in slashParameters.statusEffects)
                {
                    targetEnemy.ApplyEffect(effect);
                }
            }
            return slashParameters.damage;
        }
    }
}
