using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NevernamedsItems
{
    public class LeadOfLifeCompanion : CompanionController
    {
        public LeadOfLifeCompanion()
        {
            //Visual Variables
            overrideShader = null;

            //Attack Based Variables
            attackOnTimer = true;
            fireCooldown = 1.3f;
            requiresTargetToAttack = true;

            //Burst Fire Variables
            numberOfBurstAttacks = 1;
            burstFireCooldown = 0f;
            burstAttackBenefitsFromFirerate = true;

            //Misc
            globalCompanionFirerateMultiplier = 1;

            //Object Use
            objectToToss = null;
            objectSpawnChance = 0f;
            objectTossForce = 0f;
            tossedObjectBounces = false;

            //Active Item variables
            attacksOnActiveUse = false;
            activeItemIDToAttackOn = -1;

            //Stored attack info
            timesAttacked = 0;
            isAttacking = false;
        }
        public static LeadOfLifeCompanion AddToPrefab(GameObject prefab,
            int itemID,
            float moveSpeed = 5,
            List<MovementBehaviorBase> movementBehaviors = null
            )
        {
            LeadOfLifeCompanion addedInstance = prefab.AddComponent<LeadOfLifeCompanion>();
            addedInstance.aiActor.MovementSpeed = moveSpeed;
            addedInstance.tiedItemID = itemID;

            addedInstance.aiAnimator.GetDirectionalAnimation("idle").Prefix = "idle";
            if (addedInstance.aiAnimator.GetDirectionalAnimation("move") != null) addedInstance.aiAnimator.GetDirectionalAnimation("move").Prefix = "run";
            BehaviorSpeculator component = prefab.GetComponent<BehaviorSpeculator>();
            if (movementBehaviors == null) component.MovementBehaviors.AddRange(new List<MovementBehaviorBase>() { new CustomCompanionBehaviours.LeadOfLifeCompanionApproach(), new CompanionFollowPlayerBehavior { IdleAnimations = new string[] { "idle" } } });
            else component.MovementBehaviors.AddRange(movementBehaviors);

            return addedInstance;
        }
        //Triggers (IE: Timer, Active Item Use)
        public float timer;
        public override void Update()
        {
            if (base.specRigidbody && base.aiActor && Owner && base.transform)
            {
                if (ignitesGoop) DeadlyDeadlyGoopManager.IgniteGoopsCircle(base.specRigidbody.UnitBottomCenter, 1);
                if (Owner.IsInCombat && base.transform.position.GetAbsoluteRoom() == Owner.CurrentRoom)
                {
                    if (base.aiActor.OverrideTarget != null || !requiresTargetToAttack)
                    {
                        if (timer > 0)
                        {
                            if (!isAttacking) timer -= BraveTime.DeltaTime;
                        }
                        if (timer <= 0)
                        {
                            if (attackOnTimer) StartCoroutine(HandleAttackCoroutine());

                            //Handle firerate with modifiers
                            float originalFireCooldown = fireCooldown;
                            if (baseLeadOfLife && baseLeadOfLife.globalCompanionFirerateMultiplier != 1) originalFireCooldown /= baseLeadOfLife.globalCompanionFirerateMultiplier;
                            originalFireCooldown = ModifyCooldown(originalFireCooldown);

                            timer = originalFireCooldown;
                        }
                    }
                }
            }
            base.Update();
        }
        private void OnOwnerUsedActiveItem(PlayerController player, PlayerItem item)
        {
            OwnerUsedActiveItem(item);
            if (attacksOnActiveUse)
            {
                if (item.PickupObjectId == activeItemIDToAttackOn || activeItemIDToAttackOn == -1) StartCoroutine(HandleAttackCoroutine());
            }
        }
        private IEnumerator HandleAttackCoroutine()
        {
            isAttacking = true;
            for (int i = 0; i < numberOfBurstAttacks; i++)
            {
                Attack();
                if (burstFireCooldown > 0)
                {
                    float burstCooldown = burstFireCooldown;
                    if (burstAttackBenefitsFromFirerate)
                    {
                        if (baseLeadOfLife && baseLeadOfLife.globalCompanionFirerateMultiplier != 1) burstCooldown /= baseLeadOfLife.globalCompanionFirerateMultiplier;
                        burstCooldown = ModifyCooldown(burstCooldown);
                    }
                    yield return new WaitForSeconds(burstCooldown);
                }
            }
            isAttacking = false;
            yield break;
        }

        //Possible Activations
        public virtual void Attack()
        {
            if (objectSpawnChance > 0) SpawnObject();
        }
        private void SpawnObject()
        {
            if (UnityEngine.Random.value <= objectSpawnChance && base.sprite && objectToToss != null)
            {
                GameObject instanceObj = null;
                if (objectTossForce == 0)
                {
                    GameObject placedObject = UnityEngine.Object.Instantiate<GameObject>(objectToToss, base.sprite.WorldCenter, Quaternion.identity);
                    instanceObj = placedObject;
                    tk2dBaseSprite objectsprite = placedObject.GetComponent<tk2dBaseSprite>();
                    if (objectsprite != null)
                    {
                        objectsprite.PlaceAtPositionByAnchor(base.sprite.WorldCenter.ToVector3ZUp(objectsprite.transform.position.z), tk2dBaseSprite.Anchor.MiddleCenter);
                        if (objectsprite.specRigidbody != null)
                        {
                            objectsprite.specRigidbody.RegisterGhostCollisionException(Owner.specRigidbody);
                            objectsprite.specRigidbody.RegisterGhostCollisionException(base.specRigidbody);
                        }
                    }
                    placedObject.transform.position = placedObject.transform.position.Quantize(0.0625f);
                }
                else
                {
                    Vector3 angle = GetAngleToTarget();
                    Vector3 basePosition = base.sprite.WorldCenter;
                    if (angle.y > 0f)
                    {
                        basePosition += Vector3.up * 0.25f;
                    }
                    GameObject tossedObject = UnityEngine.Object.Instantiate<GameObject>(objectToToss, basePosition, Quaternion.identity);
                    tk2dBaseSprite tossedSprite = tossedObject.GetComponent<tk2dBaseSprite>();
                    if (tossedSprite)
                    {
                        tossedSprite.PlaceAtPositionByAnchor(basePosition, tk2dBaseSprite.Anchor.MiddleCenter);
                    }
                    instanceObj = tossedObject;

                    DebrisObject debrisObject = LootEngine.DropItemWithoutInstantiating(tossedObject, tossedObject.transform.position, angle, objectTossForce, false, false, true, false);
                    if (angle.y > 0f && debrisObject)
                    {
                        debrisObject.additionalHeightBoost = -1f;
                        if (debrisObject.sprite) debrisObject.sprite.UpdateZDepth();
                    }
                    debrisObject.IsAccurateDebris = true;
                    debrisObject.Priority = EphemeralObject.EphemeralPriority.Critical;
                    debrisObject.bounceCount = tossedObjectBounces ? 1 : 0;
                }
                if (instanceObj != null)
                {
                    SpawnObjectItem spawnedThingComp = instanceObj.GetComponentInChildren<SpawnObjectItem>();
                    if (spawnedThingComp) spawnedThingComp.SpawningPlayer = Owner;
                    PostSpawnObject(instanceObj);
                }
            }
        }
        private void OnRoomClearEffects(PlayerController guy)
        {
            if (base.specRigidbody)
            {
                timer = 1;
                OnRoomClear();
                if (spawnsCurrencyOnRoomClear) LootEngine.SpawnCurrency(base.specRigidbody.UnitCenter, UnityEngine.Random.Range(0, 4));
            }
        }

        //Utility
        public Vector2 GetAngleToTarget()
        {
            if (base.aiActor.OverrideTarget != null)
            {
                if (base.aiActor.OverrideTarget.sprite != null)
                {
                    return (base.aiActor.OverrideTarget.sprite.WorldCenter - base.specRigidbody.UnitCenter).normalized;
                }
                return (base.aiActor.OverrideTarget.UnitCenter - base.specRigidbody.UnitCenter).normalized;
            }
            else return Vector2.zero;
        }

        //Start and Destroy
        private void Start()
        {
            this.Owner = this.m_owner;
            timer = 1;
            if (Owner)
            {
                foreach (PassiveItem item in Owner.passiveItems)
                {
                    if (item && item.GetComponent<LeadOfLife>())
                    {
                        if (item.GetComponent<LeadOfLife>().extantCompanions.Count > 0)
                        {
                            if (item.GetComponent<LeadOfLife>().extantCompanions.Contains(this))
                            {
                                baseLeadOfLife = item.GetComponent<LeadOfLife>();
                            }
                        }
                    }
                }
                Owner.OnRoomClearEvent += this.OnRoomClearEffects;
                Owner.OnUsedPlayerItem += this.OnOwnerUsedActiveItem;
                if (!string.IsNullOrEmpty(overrideShader)) this.sprite.renderer.material.shader = ShaderCache.Acquire(overrideShader);
            }
        }
        public override void OnDestroy()
        {
            if (Owner)
            {
                Owner.OnRoomClearEvent -= this.OnRoomClearEffects;
                Owner.OnUsedPlayerItem -= this.OnOwnerUsedActiveItem;
            }
            base.OnDestroy();
        }

        //Virtual methods for extension
        public virtual float ModifyCooldown(float originalCooldown) { return originalCooldown; }
        public virtual void PostSpawnObject(GameObject prop) { }
        public virtual void OwnerUsedActiveItem(PlayerItem active) { }
        public virtual void OnRoomClear() { }


        #region Variables

        public PlayerController Owner;
        public int tiedItemID;
        public LeadOfLife baseLeadOfLife;
        public PickupObject alternativeSpawner;
        public PickupObject correspondingItem;

        //Appearance Variables
        public string overrideShader;

        //Attack Based Variables
        public bool attackOnTimer;
        public bool requiresTargetToAttack;
        public bool attacksOnActiveUse;
        public int activeItemIDToAttackOn;
        public float fireCooldown;

        //Burst-Based Variables
        public int numberOfBurstAttacks;
        public float burstFireCooldown;
        public bool burstAttackBenefitsFromFirerate;

        //Stored Attack Info
        public int timesAttacked;
        public bool isAttacking;

        //Other
        public bool ignitesGoop;
        public float globalCompanionFirerateMultiplier;

        //Room Clear Stuff
        public bool spawnsCurrencyOnRoomClear;

        //Object Spawning related variables
        public float objectSpawnChance;
        public float objectTossForce;
        public GameObject objectToToss;
        public bool tossedObjectBounces;
        #endregion
    }
}
