using Alexandria.ItemAPI;
using MonoMod.RuntimeDetour;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Alexandria.EnemyAPI;

namespace NevernamedsItems
{
    class CompanionisedEnemyUtility
    {
        public static GameObject FriendlyVFX;
        public static void InitHooks()
        {
            DisplaceHook = new Hook(
    typeof(DisplaceBehavior).GetMethod("SpawnImage", BindingFlags.Instance | BindingFlags.NonPublic),
    typeof(CompanionisedEnemyUtility).GetMethod("DisplacedImageSpawnHook", BindingFlags.Instance | BindingFlags.NonPublic),
    typeof(DisplaceBehavior));


            List<string> FriendVFXPaths = new List<string>()
            {
                "NevernamedsItems/Resources/MiscVFX/friendlyoverhead_vfx_001",
                "NevernamedsItems/Resources/MiscVFX/friendlyoverhead_vfx_002",
                "NevernamedsItems/Resources/MiscVFX/friendlyoverhead_vfx_003",
                "NevernamedsItems/Resources/MiscVFX/friendlyoverhead_vfx_004",
                "NevernamedsItems/Resources/MiscVFX/friendlyoverhead_vfx_005"
            };
            GameObject friendly = VFXToolbox.CreateOverheadVFX(FriendVFXPaths, "FriendlyOverhead", 10);
            FriendlyVFX = friendly;
        }
        public static Hook DisplaceHook;
        private void DisplacedImageSpawnHook(Action<DisplaceBehavior> orig, DisplaceBehavior sourceBehaviour)
        {
            orig(sourceBehaviour);
            AIActor attkOwner = sourceBehaviour.GetAttackBehaviourOwner();
            if (attkOwner != null)
            {
                if (attkOwner.GetComponent<CustomEnemyTagsSystem>() != null && attkOwner.GetComponent<CustomEnemyTagsSystem>().isKalibersEyeMinion)
                {
                    AIActor image = OMITBReflectionHelpers.ReflectGetField<AIActor>(typeof(DisplaceBehavior), "m_image", sourceBehaviour);
                    if (image != null)
                    {
                        PhysicsEngine.Instance.RegisterOverlappingGhostCollisionExceptions(image.specRigidbody, null, false);

                        CustomEnemyTagsSystem tags = image.gameObject.GetOrAddComponent<CustomEnemyTagsSystem>();
                        tags.isKalibersEyeMinion = true;
                        tags.ignoreForGoodMimic = true;

                        if (attkOwner.CompanionOwner != null)
                        {
                            CompanionController orAddComponent = image.gameObject.GetOrAddComponent<CompanionController>();
                            orAddComponent.companionID = CompanionController.CompanionIdentifier.NONE;
                            orAddComponent.Initialize(attkOwner.CompanionOwner);
                        }

                        image.OverrideHitEnemies = true;
                        image.CollisionDamage = 0.5f;
                        image.CollisionDamageTypes |= CoreDamageTypes.Electric;

                        CompanionisedEnemyBulletModifiers companionisedBullets = image.gameObject.GetOrAddComponent<CompanionisedEnemyBulletModifiers>();
                        companionisedBullets.jammedDamageMultiplier = 2f;
                        companionisedBullets.TintBullets = true;
                        companionisedBullets.TintColor = ExtendedColours.honeyYellow;
                        companionisedBullets.baseBulletDamage = 10f;
                        companionisedBullets.scaleSpeed = true;
                        companionisedBullets.scaleDamage = true;
                        companionisedBullets.scaleSize = false;
                        companionisedBullets.doPostProcess = false;
                        if (attkOwner.CompanionOwner != null) companionisedBullets.enemyOwner = attkOwner.CompanionOwner;

                        image.ApplyEffect(GameManager.Instance.Dungeon.sharedSettingsPrefab.DefaultPermanentCharmEffect, 1f, null);

                        ContinualKillOnRoomClear contKill = image.gameObject.AddComponent<ContinualKillOnRoomClear>();
                        if (image.HasTag("multiple_phase_enemy"))
                        {
                            contKill.forceExplode = true;
                            contKill.eraseInsteadOfKill = true;
                        }

                        image.IsHarmlessEnemy = true;
                        image.RegisterOverrideColor(Color.grey, "Ressurection");
                        image.IgnoreForRoomClear = true;
                        if (image.gameObject.GetComponent<SpawnEnemyOnDeath>())
                        {
                            UnityEngine.Object.Destroy(image.gameObject.GetComponent<SpawnEnemyOnDeath>());
                        }
                    }
                }
            }
        }
        public static AIActor SpawnCompanionisedEnemy(PlayerController owner, string enemyGuid, IntVector2 position, bool doTint, Color enemyTint, int baseDMG, int jammedDMGMult, bool shouldBeJammed, bool doFriendlyOverhead)
        {
            var enemyToSpawn = EnemyDatabase.GetOrLoadByGuid(enemyGuid);
            UnityEngine.Object.Instantiate<GameObject>(EasyVFXDatabase.BloodiedScarfPoofVFX, position.ToVector3(), Quaternion.identity);
            AIActor TargetActor = AIActor.Spawn(enemyToSpawn, position, GameManager.Instance.Dungeon.data.GetAbsoluteRoomFromPosition(position), true, AIActor.AwakenAnimationType.Default, true);
            PhysicsEngine.Instance.RegisterOverlappingGhostCollisionExceptions(TargetActor.specRigidbody, null, false);

            CompanionController orAddComponent = TargetActor.gameObject.GetOrAddComponent<CompanionController>();
            orAddComponent.companionID = CompanionController.CompanionIdentifier.NONE;
            orAddComponent.Initialize(owner);

            if (shouldBeJammed == true)
            {
                TargetActor.BecomeBlackPhantom();
            }
            CompanionisedEnemyBulletModifiers companionisedBullets = TargetActor.gameObject.GetOrAddComponent<CompanionisedEnemyBulletModifiers>();
            companionisedBullets.jammedDamageMultiplier = jammedDMGMult;
            companionisedBullets.TintBullets = true;
            companionisedBullets.TintColor = ExtendedColours.honeyYellow;
            companionisedBullets.baseBulletDamage = baseDMG;

            TargetActor.ApplyEffect(GameManager.Instance.Dungeon.sharedSettingsPrefab.DefaultPermanentCharmEffect, 1f, null);
            TargetActor.gameObject.AddComponent<KillOnRoomClear>();
            ContinualKillOnRoomClear contKill = TargetActor.gameObject.AddComponent<ContinualKillOnRoomClear>();
            if (TargetActor.HasTag("multiple_phase_enemy"))
            {
                contKill.forceExplode = true;
                contKill.eraseInsteadOfKill = true;
            }
            TargetActor.IsHarmlessEnemy = true;
            if (doTint) TargetActor.RegisterOverrideColor(enemyTint, "CompanionisedEnemyTint");
            TargetActor.IgnoreForRoomClear = true;

            if (doFriendlyOverhead)
            {
                GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(FriendlyVFX);
                tk2dBaseSprite component = gameObject.GetComponent<tk2dBaseSprite>();
                gameObject.transform.parent = TargetActor.transform;
                if (TargetActor.healthHaver.IsBoss)
                {
                    gameObject.transform.position = TargetActor.specRigidbody.HitboxPixelCollider.UnitTopCenter;
                }
                else
                {
                    Bounds bounds = TargetActor.sprite.GetBounds();
                    Vector3 vector = TargetActor.transform.position + new Vector3((bounds.max.x + bounds.min.x) / 2f, bounds.max.y, 0f).Quantize(0.0625f);
                    gameObject.transform.position = TargetActor.sprite.WorldCenter.ToVector3ZUp(0f).WithY(vector.y);
                }
                component.HeightOffGround = 0.5f;
                TargetActor.sprite.AttachRenderer(component);
            }

            return TargetActor;
        }
    }
    public class ContinualKillOnRoomClear : MonoBehaviour
    {
        public ContinualKillOnRoomClear()
        {
            this.lengthOfNonCombatSurvival = 1f;
        }
        private void Start()
        {
            if (base.GetComponent<AIActor>()) this.self = base.GetComponent<AIActor>();
        }
        private void Update()
        {
            if (!GameManager.Instance.PrimaryPlayer.IsInCombat)
            {
                if (!ithasBegun)
                {
                    ithasBegun = true;
                    GameManager.Instance.StartCoroutine(Suicide());
                }
            }
        }
        private IEnumerator Suicide()
        {
            yield return new WaitForSeconds(lengthOfNonCombatSurvival);
            if (self & self.healthHaver && self.healthHaver.IsAlive)
            {
                ithasBegun = true;
                if (forceExplode && self.specRigidbody) Exploder.DoDefaultExplosion(self.specRigidbody.UnitCenter, Vector2.zero);
                if (eraseInsteadOfKill)
                {
                    self.EraseFromExistenceWithRewards();
                }
                else
                {
                    if (self.healthHaver)
                    {
                        self.healthHaver.ApplyDamage(float.MaxValue, Vector2.zero, "Erasure");
                    }
                }
            }
            yield break;
        }
        private bool ithasBegun = false;
        private AIActor self;
        public float lengthOfNonCombatSurvival;
        public bool eraseInsteadOfKill;
        public bool forceExplode;
    }
}
