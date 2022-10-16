using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Alexandria.ItemAPI;
using Dungeonator;
using System.Collections;
using System.Diagnostics;
using Alexandria.Misc;

namespace NevernamedsItems
{
    public class StickPlayerToDebug : MonoBehaviour
    {
        public StickPlayerToDebug()
        {
            Obj = base.GetComponent<SpeculativeRigidbody>();
        }
      
        private void Update()
        {
            player.specRigidbody.Position = new Position(Obj.Position);
        }
        public PlayerController player;
        private SpeculativeRigidbody Obj;
    }
    public class CameraOverrideTarget : MonoBehaviour
    {
        public CameraOverrideTarget()
        {
            startOverrideOnStart = false;
            duration = -1;
            timer = -1;
        }
        private void OnDestroy()
        {
            GameManager.Instance.MainCameraController.SetManualControl(false, true);
        }
        private void Start()
        {
            targetObject = base.GetComponent<SpeculativeRigidbody>();
            if (startOverrideOnStart) BeginOverride();
        }
        private void Update()
        {
            if (targetObject) GameManager.Instance.MainCameraController.OverridePosition = targetObject.UnitCenter;
            if (duration > 0)
            {
                if (timer <= 0)
                {
                    EndOverride();
                }
                else
                {
                    timer -= BraveTime.DeltaTime;
                }
            }
        }
        public void BeginOverride()
        {
            GameManager.Instance.MainCameraController.SetManualControl(true, false);
            if (duration > 0) timer = duration;
        }
        public void EndOverride()
        {
            GameManager.Instance.MainCameraController.SetManualControl(false, true);
        }
        public bool startOverrideOnStart;
        private SpeculativeRigidbody targetObject;
        public float duration;
        private float timer;
    }
    public static class MiscToolbox
    {

        public static void SpawnShield(PlayerController user, Vector3 location)
        {
            Gun currentGun = user.CurrentGun;
            GameObject shieldObj = (PickupObjectDatabase.GetById(380) as Gun).ObjectToInstantiateOnReload.gameObject;
            GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(shieldObj, location, Quaternion.identity);

            SingleSpawnableGunPlacedObject @interface = gameObject2.GetInterface<SingleSpawnableGunPlacedObject>();
            BreakableShieldController component = gameObject2.GetComponent<BreakableShieldController>();
            if (gameObject2)
            {
                @interface.Initialize(currentGun);
                component.Initialize(currentGun);
            }
            if (location != Vector3.zero)
            {
                component.transform.position = location;
                component.specRigidbody.Reinitialize();
            }
        }


        public static RoomHandler GetAbsoluteRoomFromProjectile(Projectile bullet)
        {
            Vector2 bulletPosition = bullet.sprite.WorldCenter;
            IntVector2 bulletPositionIntVector2 = bulletPosition.ToIntVector2();
            return GameManager.Instance.Dungeon.data.GetAbsoluteRoomFromPosition(bulletPositionIntVector2);
        }



        public static float Vector2ToDegree(Vector2 p_vector2)
        {
            if (p_vector2.x < 0) { return 360 - (Mathf.Atan2(p_vector2.x, p_vector2.y) * Mathf.Rad2Deg * -1); }
            else { return Mathf.Atan2(p_vector2.x, p_vector2.y) * Mathf.Rad2Deg; }
        }
        public static void ApplyFear(this AIActor enemy, PlayerController player, float fearLength, float fearStartDistance, float fearStopDistance)
        {
            EnemyFeared inflictedFear = enemy.gameObject.AddComponent<EnemyFeared>();
            inflictedFear.player = player;
            inflictedFear.fearLength = fearLength;
            inflictedFear.fearStartDistance = fearStartDistance;
            inflictedFear.fearStopDistance = fearStopDistance;
        }
    }
    public class EnemyFeared : BraveBehaviour
    {
        private void Start()
        {
            this.enemy = base.aiActor;
            if (this.player != null && this.enemy != null)
            {
                StartCoroutine("HandleFear");
            }
        }
        private IEnumerator HandleFear()
        {
            if (this.enemy.behaviorSpeculator != null)
            {
                //ETGModConsole.Log("Fear Triggered");
                FleePlayerData fleedata = new FleePlayerData
                {
                    StartDistance = this.fearStartDistance,
                    StopDistance = this.fearStopDistance,
                    Player = this.player,
                };
                this.enemy.behaviorSpeculator.FleePlayerData = fleedata;

                yield return new WaitForSeconds(fearLength);

                this.enemy.behaviorSpeculator.FleePlayerData = null;
            }
        }
        public PlayerController player;
        private AIActor enemy;
        public float fearLength;
        public float fearStartDistance;
        public float fearStopDistance;
    }
    public class StaticCoroutine : MonoBehaviour
    {
        private static StaticCoroutine m_instance;

        // OnDestroy is called when the MonoBehaviour will be destroyed.
        // Coroutines are not stopped when a MonoBehaviour is disabled, but only when it is definitely destroyed.
        private void OnDestroy()
        { m_instance.StopAllCoroutines(); }

        // OnApplicationQuit is called on all game objects before the application is closed.
        // In the editor it is called when the user stops playmode.
        private void OnApplicationQuit()
        { m_instance.StopAllCoroutines(); }

        // Build will attempt to retrieve the class-wide instance, returning it when available.
        // If no instance exists, attempt to find another StaticCoroutine that exists.
        // If no StaticCoroutines are present, create a dedicated StaticCoroutine object.
        private static StaticCoroutine Build()
        {
            if (m_instance != null)
            { return m_instance; }

            m_instance = (StaticCoroutine)FindObjectOfType(typeof(StaticCoroutine));

            if (m_instance != null)
            { return m_instance; }

            GameObject instanceObject = new GameObject("StaticCoroutine");
            instanceObject.AddComponent<StaticCoroutine>();
            m_instance = instanceObject.GetComponent<StaticCoroutine>();

            if (m_instance != null)
            { return m_instance; }



            ETGModConsole.Log("STATIC COROUTINE: Build did not generate a replacement instance. Method Failed!");

            return null;
        }
        //public static Coroutine Start(IEnumerator routine)
        //{ return Build().StartCoroutine(routine); }

        // Overloaded Static Coroutine Methods which use Unity's default Coroutines.
        // Polymorphism applied for best compatibility with the standard engine.
        public static void Start(string methodName)
        { Build().StartCoroutine(methodName); }
        public static void Start(string methodName, object value)
        { Build().StartCoroutine(methodName, value); }
        public static Coroutine Start(IEnumerator routine)
        { return Build().StartCoroutine(routine); }
    }
    public class CustomEnemyTagsSystem : MonoBehaviour
    {
        public CustomEnemyTagsSystem()
        {
            isKalibersEyeMinion = false;
            ignoreForGoodMimic = false;
            isGundertaleFriendly = false;
        }

        public bool isKalibersEyeMinion;
        public bool isGundertaleFriendly;
        public bool ignoreForGoodMimic;
    }
    public class AdvancedTransformGunSynergyProcessor : MonoBehaviour
    {
        public AdvancedTransformGunSynergyProcessor()
        {
            this.NonSynergyGunId = -1;
            this.SynergyGunId = -1;
        }

        private void Awake()
        {
            this.m_gun = base.GetComponent<Gun>();
        }

        private void Update()
        {
            if (Dungeon.IsGenerating || Dungeon.ShouldAttemptToLoadFromMidgameSave)
            {
                return;
            }
            if (this.m_gun && this.m_gun.CurrentOwner is PlayerController)
            {
                PlayerController playerController = this.m_gun.CurrentOwner as PlayerController;
                if (!this.m_gun.enabled)
                {
                    return;
                }
                if (playerController.PlayerHasActiveSynergy(this.SynergyToCheck) && !this.m_transformed)
                {
                    this.m_transformed = true;
                    this.m_gun.TransformToTargetGun(PickupObjectDatabase.GetById(this.SynergyGunId) as Gun);
                    if (this.ShouldResetAmmoAfterTransformation)
                    {
                        this.m_gun.ammo = this.ResetAmmoCount;
                    }
                }
                else if (!playerController.PlayerHasActiveSynergy(this.SynergyToCheck) && this.m_transformed)
                {
                    this.m_transformed = false;
                    this.m_gun.TransformToTargetGun(PickupObjectDatabase.GetById(this.NonSynergyGunId) as Gun);
                    if (this.ShouldResetAmmoAfterTransformation)
                    {
                        this.m_gun.ammo = this.ResetAmmoCount;
                    }
                }
            }
            else if (this.m_gun && !this.m_gun.CurrentOwner && this.m_transformed)
            {
                this.m_transformed = false;
                this.m_gun.TransformToTargetGun(PickupObjectDatabase.GetById(this.NonSynergyGunId) as Gun);
                if (this.ShouldResetAmmoAfterTransformation)
                {
                    this.m_gun.ammo = this.ResetAmmoCount;
                }
            }
            this.ShouldResetAmmoAfterTransformation = false;
        }

        public string SynergyToCheck;
        public int NonSynergyGunId;
        public int SynergyGunId;
        private Gun m_gun;
        private bool m_transformed;
        public bool ShouldResetAmmoAfterTransformation;
        public int ResetAmmoCount;
    }
    public class PermaCharmBulletBehaviour : MonoBehaviour
    {
        public PermaCharmBulletBehaviour()
        {
            this.tintColour = ExtendedColours.pink;
            this.useSpecialTint = true;
            this.procChance = 1;
        }
        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            if (useSpecialTint)
            {
                m_projectile.AdjustPlayerProjectileTint(tintColour, 2);
            }
            m_projectile.OnHitEnemy += this.OnHitEnemy;
        }
        private void OnHitEnemy(Projectile bullet, SpeculativeRigidbody enemy, bool fatal)
        {
            if (UnityEngine.Random.value <= procChance)
            {
                if (enemy && enemy.aiActor) enemy.aiActor.ApplyEffect(GameManager.Instance.Dungeon.sharedSettingsPrefab.DefaultPermanentCharmEffect, 1f, null);
            }
        }
        private Projectile m_projectile;
        public Color tintColour;
        public bool useSpecialTint;
        public float procChance;
    }
    public class AreaFearBulletModifier : MonoBehaviour
    {
        public AreaFearBulletModifier()
        {
            this.tintColour = ExtendedColours.pink;
            this.useSpecialTint = true;
            this.procChance = 1;
            this.effectRadius = 5;
            this.fearLength = 1f;
            this.fearStartDistance = 5f;
            this.fearStopDistance = 7f;
        }
        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            if (m_projectile.Owner is PlayerController) this.player = m_projectile.Owner as PlayerController;
            if (useSpecialTint)
            {
                m_projectile.AdjustPlayerProjectileTint(tintColour, 2);
            }
            m_projectile.OnHitEnemy += this.OnHitEnemy;

        }
        private void OnHitEnemy(Projectile bullet, SpeculativeRigidbody enemy, bool fatal)
        {
            //ETGModConsole.Log("OnHitEnemy Activated");
            if (UnityEngine.Random.value <= procChance)
            {
                //ETGModConsole.Log("Proc Chance Passed");

                List<AIActor> activeEnemies = enemy.aiActor.GetAbsoluteParentRoom().GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
                if (activeEnemies != null)
                {
                    //ETGModConsole.Log("Active Enemies was not Null");

                    for (int i = 0; i < activeEnemies.Count; i++)
                    {
                        //ETGModConsole.Log("Found Valid Enemy --------------");

                        AIActor aiactor = activeEnemies[i];
                        if (aiactor.IsNormalEnemy)
                        {
                            //ETGModConsole.Log("Valid enemy is normal enemy");

                            float num = Vector2.Distance(enemy.specRigidbody.UnitCenter, aiactor.CenterPosition);
                            if (num <= effectRadius)
                            {
                                aiactor.ApplyFear(player, fearLength, fearStartDistance, fearStopDistance);
                            }
                        }
                    }
                }
            }
        }

        private PlayerController player;
        private Projectile m_projectile;
        public Color tintColour;
        public bool useSpecialTint;
        public float procChance;
        public float fearStartDistance;
        public float fearStopDistance;
        public float fearLength;
        public float effectRadius;
    }

    public class PlayerProjectileTeleportModifier : BraveBehaviour
    {
        public PlayerProjectileTeleportModifier()
        {
            this.trigger = PlayerProjectileTeleportModifier.TeleportTrigger.AngleToTarget;
            this.minAngleToTeleport = 70f;
            this.distToTeleport = 3f;
            this.type = PlayerProjectileTeleportModifier.TeleportType.BackToSpawn;
            this.behindTargetDistance = 5f;
        }
        //[DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public event Action OnTeleport;
        private bool ShowMMinAngleToTeleport() { return this.trigger == PlayerProjectileTeleportModifier.TeleportTrigger.AngleToTarget; }
        private bool ShowDistToTeleport() { return this.trigger == PlayerProjectileTeleportModifier.TeleportTrigger.DistanceFromTarget; }
        private bool ShowBehindTargetDistance() { return this.type == PlayerProjectileTeleportModifier.TeleportType.BehindTarget; }
        public void Start()
        {
            if (!base.sprite) { base.sprite = base.GetComponentInChildren<tk2dSprite>(); }
            if (base.projectile && base.projectile.Owner is AIActor) { this.m_targetRigidbody = (base.projectile.Owner as AIActor).TargetRigidbody; }
            else if (base.projectile && base.projectile.Owner is PlayerController)
            {
                AIActor enemy = MiscToolbox.GetAbsoluteRoomFromProjectile(base.projectile).GetRandomActiveEnemy(false);
                this.m_targetRigidbody = enemy.specRigidbody;
            }
            if (!this.m_targetRigidbody)
            {
                base.enabled = false;
                return;
            }
            this.m_startingPos = base.transform.position;
        }
        public void Update()
        {
            if (this.m_isTeleporting) { return; }
            if (this.m_cooldown > 0f)
            {
                this.m_cooldown -= BraveTime.DeltaTime;
                return;
            }
            if (this.numTeleports > 0 && this.ShouldTeleport()) { base.StartCoroutine(this.DoTeleport()); }
        }
        public override void OnDestroy()
        {
            base.StopAllCoroutines();
            base.OnDestroy();
        }
        private bool ShouldTeleport()
        {
            if (m_targetRigidbody)
            {
                Vector2 unitCenter = this.m_targetRigidbody.GetUnitCenter(ColliderType.HitBox);
                if (this.trigger == PlayerProjectileTeleportModifier.TeleportTrigger.AngleToTarget)
                {
                    float a = (unitCenter - base.specRigidbody.UnitCenter).ToAngle();
                    float b = base.specRigidbody.Velocity.ToAngle();
                    return BraveMathCollege.AbsAngleBetween(a, b) > this.minAngleToTeleport;
                }
                return this.trigger == PlayerProjectileTeleportModifier.TeleportTrigger.DistanceFromTarget && Vector2.Distance(unitCenter, base.specRigidbody.UnitCenter) < this.distToTeleport;
            }
            return false;
        }
        private Vector2 GetTeleportPosition()
        {
            if (this.type == PlayerProjectileTeleportModifier.TeleportType.BackToSpawn)
            {
                return this.m_startingPos;
            }
            if (this.type == PlayerProjectileTeleportModifier.TeleportType.BehindTarget && this.m_targetRigidbody && this.m_targetRigidbody.gameActor)
            {
                Vector2 unitCenter = this.m_targetRigidbody.GetUnitCenter(ColliderType.HitBox);
                float facingDirection = this.m_targetRigidbody.gameActor.FacingDirection;
                Dungeon dungeon = GameManager.Instance.Dungeon;
                for (int i = 0; i < 18; i++)
                {
                    Vector2 vector = unitCenter + BraveMathCollege.DegreesToVector(facingDirection + 180f + (float)(i * 20), this.behindTargetDistance);
                    if (!dungeon.CellExists(vector) || !dungeon.data.isWall((int)vector.x, (int)vector.y)) { return vector; }
                    vector = unitCenter + BraveMathCollege.DegreesToVector(facingDirection + 180f + (float)(i * -20), this.behindTargetDistance);
                    if (!dungeon.CellExists(vector) || !dungeon.data.isWall((int)vector.x, (int)vector.y)) { return vector; }
                }
            }
            return this.m_startingPos;
        }
        private IEnumerator DoTeleport()
        {
            VFXPool vfxpool = this.teleportVfx;
            Vector3 position = this.specRigidbody.UnitCenter;
            Transform transform = this.transform;
            vfxpool.SpawnAtPosition(position, 0f, transform, null, null, null, false, null, null, false);
            if (this.teleportPauseTime > 0f)
            {
                this.m_isTeleporting = true;
                this.sprite.renderer.enabled = false;
                this.projectile.enabled = false;
                this.specRigidbody.enabled = false;
                if (this.projectile.braveBulletScript)
                {
                    this.projectile.braveBulletScript.enabled = false;
                }
                yield return new WaitForSeconds(this.teleportPauseTime);
                if (!this || !this.m_targetRigidbody)
                {
                    yield break;
                }
                this.m_isTeleporting = false;
                this.sprite.renderer.enabled = true;
                this.projectile.enabled = true;
                this.specRigidbody.enabled = true;
                if (this.projectile.braveBulletScript)
                {
                    this.projectile.braveBulletScript.enabled = true;
                }
            }
            Vector2 newPosition = this.GetTeleportPosition();
            this.transform.position = newPosition;
            this.specRigidbody.Reinitialize();
            VFXPool vfxpool2 = this.teleportVfx;
            position = this.specRigidbody.UnitCenter;
            transform = this.transform;
            vfxpool2.SpawnAtPosition(position, 0f, transform, null, null, null, false, null, null, false);
            Vector2 firingCenter = this.specRigidbody.UnitCenter;
            Vector2 targetCenter = this.m_targetRigidbody.specRigidbody.GetUnitCenter(ColliderType.HitBox);
            PlayerController targetPlayer = this.m_targetRigidbody.gameActor as PlayerController;
            if (this.leadAmount > 0f && targetPlayer)
            {
                Vector2 targetVelocity = (!targetPlayer) ? this.m_targetRigidbody.Velocity : targetPlayer.AverageVelocity;
                Vector2 predictedPosition = BraveMathCollege.GetPredictedPosition(targetCenter, targetVelocity, firingCenter, this.projectile.Speed);
                targetCenter = Vector2.Lerp(targetCenter, predictedPosition, this.leadAmount);
            }
            this.projectile.SendInDirection(targetCenter - firingCenter, true, true);
            if (this.projectile.braveBulletScript && this.projectile.braveBulletScript.bullet != null)
            {
                this.projectile.braveBulletScript.bullet.Position = newPosition;
                this.projectile.braveBulletScript.bullet.Direction = (targetCenter - newPosition).ToAngle();
            }
            this.numTeleports--;
            this.m_cooldown = this.teleportCooldown;
            if (this.OnTeleport != null)
            {
                this.OnTeleport();
            }
            yield break;
        }
        public PlayerProjectileTeleportModifier.TeleportTrigger trigger;
        [ShowInInspectorIf("ShowMMinAngleToTeleport", true)]
        public float minAngleToTeleport;
        [ShowInInspectorIf("ShowDistToTeleport", true)]
        public float distToTeleport;
        public PlayerProjectileTeleportModifier.TeleportType type;
        [ShowInInspectorIf("ShowBehindTargetDistance", true)]
        public float behindTargetDistance;
        public int numTeleports;
        public float teleportPauseTime;
        public float leadAmount;
        public float teleportCooldown;
        public VFXPool teleportVfx;
        private SpeculativeRigidbody m_targetRigidbody;
        private Vector3 m_startingPos;
        private bool m_isTeleporting;
        private float m_cooldown;
        public enum TeleportTrigger
        {
            AngleToTarget = 10,
            DistanceFromTarget = 20
        }
        public enum TeleportType
        {
            BackToSpawn = 10,
            BehindTarget = 20
        }
    }
    public static class SpecialDrop //Can drop starter items
    {
        public static DebrisObject DropItem(PlayerController player, PickupObject thingum, bool deregisterGuns)
        {
            PassiveItem item = thingum.GetComponent<PassiveItem>();
            PlayerItem active = thingum.GetComponent<PlayerItem>();
            Gun gun = thingum.GetComponent<Gun>();
            if (item != null)
            {
                if (player.passiveItems.Contains(item))
                {
                    //ETGModConsole.Log("Started the drop");
                    player.passiveItems.Remove(item);
                    //ETGModConsole.Log("Successful Remove");

                    GameUIRoot.Instance.RemovePassiveItemFromDock(item);
                    //ETGModConsole.Log("RemovedFromDock");

                    DebrisObject debrisObject = item.Drop(player);
                    //ETGModConsole.Log("Successfully Dropped");

                    player.stats.RecalculateStats(player, false, false);
                    DontDontDestroyOnLoad(debrisObject.gameObject);
                    //ETGModConsole.Log("we did it");

                    return debrisObject;
                }
                ETGModConsole.Log("Failed to drop item because the player doesn't have it?");
                return null;
            }
            else if (active != null)
            {
                if (player.activeItems.Contains(active))
                {
                    player.activeItems.Remove(active);
                    DebrisObject debrisObject = active.Drop(player);
                    player.stats.RecalculateStats(player, false, false);
                    DontDontDestroyOnLoad(debrisObject.gameObject);
                    return debrisObject;
                }
                ETGModConsole.Log("Failed to drop item because the player doesn't have it?");
                return null;
            }
            else if (gun != null)
            {
                if (player.inventory.AllGuns.Contains(gun))
                {
                    player.inventory.RemoveGunFromInventory(gun);

                    DebrisObject debrisObject = LootEngine.DropItemWithoutInstantiating(gun.gameObject, player.LockedApproximateSpriteCenter, player.unadjustedAimPoint - player.LockedApproximateSpriteCenter, 4f, true, false, false, false);

                    if (deregisterGuns)
                    {
                        Component tempgun = null;
                        foreach (Component component in debrisObject.GetComponentsInChildren<Component>()) { if (component is Gun) { tempgun = component; } }
                        if (tempgun != null) { UnityEngine.Object.Destroy(tempgun); }
                    }


                    player.stats.RecalculateStats(player, false, false);
                    DontDontDestroyOnLoad(debrisObject.gameObject);
                    return debrisObject;
                }
                ETGModConsole.Log("Failed to drop item because the player doesn't have it?");
                return null;
            }
            return null;
        }
        private static void DontDontDestroyOnLoad(GameObject target)
        {
            if (target && GameManager.Instance.Dungeon && target.transform.parent == null)
            {
                target.transform.parent = GameManager.Instance.Dungeon.transform;
                target.transform.parent = null;
            }
        }
    }

    public class KeyBulletBehaviour : MonoBehaviour
    {
        public KeyBulletBehaviour()
        {
            this.tintColour = Color.grey;
            this.useSpecialTint = true;
            this.procChance = 1;
        }
        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            if (useSpecialTint)
            {
                m_projectile.AdjustPlayerProjectileTint(tintColour, 2);
            }
            m_projectile.specRigidbody.OnRigidbodyCollision += this.OnHitChest;
        }
        private void OnHitChest(CollisionData rigidbodyCollision)
        {
            SpeculativeRigidbody hitObject = rigidbodyCollision.OtherRigidbody;
            Chest chestComponent = hitObject.GetComponent<Chest>();
            if (chestComponent != null)
            {
                if (UnityEngine.Random.value <= procChance && !chestComponent.IsTruthChest)
                {
                    chestComponent.ForceUnlock();
                }
            }
        }
        private Projectile m_projectile;
        public Color tintColour;
        public bool useSpecialTint;
        public float procChance;
    }
    
    
    public class SimpleFreezingBulletBehaviour : MonoBehaviour
    {
        public SimpleFreezingBulletBehaviour()
        {
            this.tintColour = ExtendedColours.skyblue;
            this.useSpecialTint = true;
            this.procChance = 1;
        }
        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            if (useSpecialTint)
            {
                m_projectile.AdjustPlayerProjectileTint(tintColour, 2);
            }
            m_projectile.OnHitEnemy += this.OnHitEnemy;
        }
        private void OnHitEnemy(Projectile bullet, SpeculativeRigidbody enemy, bool fatal)
        {
            if (UnityEngine.Random.value <= procChance)
            {
                BulletStatusEffectItem component = PickupObjectDatabase.GetById(278).GetComponent<BulletStatusEffectItem>();
                GameActorFreezeEffect freezeModifierEffect = component.FreezeModifierEffect;
                if (enemy.healthHaver.IsBoss) ApplyDirectStatusEffects.ApplyDirectFreeze(enemy.gameActor, freezeModifierEffect.duration, freezeAmountForBosses, freezeModifierEffect.UnfreezeDamagePercent, freezeModifierEffect.TintColor, freezeModifierEffect.DeathTintColor, EffectResistanceType.Freeze, "NNs Freeze", true, true);
                else ApplyDirectStatusEffects.ApplyDirectFreeze(enemy.gameActor, freezeModifierEffect.duration, freezeAmount, freezeModifierEffect.UnfreezeDamagePercent, freezeModifierEffect.TintColor, freezeModifierEffect.DeathTintColor, EffectResistanceType.Freeze, "NNs Freeze", true, true);
            }
        }
        private Projectile m_projectile;
        public Color tintColour;
        public bool useSpecialTint;
        public int procChance;
        public int freezeAmount;
        public int freezeAmountForBosses;
    }

    public class ExtremelySimpleStatusEffectBulletBehaviour : MonoBehaviour
    {
        public ExtremelySimpleStatusEffectBulletBehaviour()
        {
            this.tintColour = Color.red;
            this.useSpecialTint = false;
            this.onFiredProcChance = 1;
            this.onHitProcChance = 1;
            this.fireEffect = StaticStatusEffects.hotLeadEffect;
            this.usesFireEffect = false;
            this.usesCharmEffect = false;
            this.usesPoisonEffect = false;
            this.usesSpeedEffect = false;
            this.speedEffect = StaticStatusEffects.tripleCrossbowSlowEffect;
            this.poisonEffect = StaticStatusEffects.irradiatedLeadEffect;
            this.charmEffect = StaticStatusEffects.charmingRoundsEffect;
        }
        public GameActorFireEffect fireEffect;
        public bool usesFireEffect;
        public bool usesCharmEffect;
        public GameActorCharmEffect charmEffect;
        public GameActorHealthEffect poisonEffect;
        public bool usesPoisonEffect;
        public bool usesSpeedEffect;
        public GameActorSpeedEffect speedEffect;
        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            if (UnityEngine.Random.value <= onFiredProcChance)
            {
                if (useSpecialTint)
                {
                    m_projectile.AdjustPlayerProjectileTint(tintColour, 2);
                }
                m_projectile.OnHitEnemy += this.OnHitEnemy;
            }
        }
        private void OnHitEnemy(Projectile bullet, SpeculativeRigidbody enemy, bool fatal)
        {
            if (enemy != null && enemy.gameActor != null && enemy.healthHaver != null && enemy.healthHaver.IsAlive)
            {
                if (UnityEngine.Random.value <= onHitProcChance)
                {
                    if (usesFireEffect) { enemy.gameActor.ApplyEffect(this.fireEffect, 1f, null); }
                    if (usesPoisonEffect) { enemy.gameActor.ApplyEffect(this.poisonEffect, 1f, null); }
                    if (usesCharmEffect) { enemy.gameActor.ApplyEffect(this.charmEffect, 1f, null); }
                    if (usesSpeedEffect) { enemy.gameActor.ApplyEffect(this.speedEffect, 1f, null); }
                }
            }
        }
        private Projectile m_projectile;
        public Color tintColour;
        public bool useSpecialTint;
        public float onFiredProcChance;
        public float onHitProcChance;
    }
    public class InstantTeleportToPlayerCursorBehav : MonoBehaviour
    {
        public InstantTeleportToPlayerCursorBehav()
        {
            this.procChance = 1;
        }
        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            if (m_projectile.Owner is PlayerController) this.owner = m_projectile.Owner as PlayerController;
            if (owner && UnityEngine.Random.value <= procChance)
            {
                m_projectile.specRigidbody.Position = new Position(owner.GetCursorPosition(30));
            }
        }
        private Projectile m_projectile;
        private PlayerController owner;
        public float procChance;
    }
    public class ExtremelySimplePoisonBulletBehaviour : MonoBehaviour
    {
        public ExtremelySimplePoisonBulletBehaviour()
        {
            this.tintColour = Color.green;
            this.useSpecialTint = true;
            this.procChance = 1;
        }
        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            if (useSpecialTint)
            {
                m_projectile.AdjustPlayerProjectileTint(tintColour, 2);
            }
            m_projectile.OnHitEnemy += this.OnHitEnemy;
        }
        private void OnHitEnemy(Projectile bullet, SpeculativeRigidbody enemy, bool fatal)
        {
            if (enemy && enemy.gameActor && enemy.healthHaver)
            {
                if (UnityEngine.Random.value <= procChance)
                {
                    GameActorHealthEffect irradiatedLeadEffect = Gungeon.Game.Items["irradiated_lead"].GetComponent<BulletStatusEffectItem>().HealthModifierEffect;
                    GameActorHealthEffect poisonToApply = new GameActorHealthEffect
                    {
                        duration = irradiatedLeadEffect.duration,
                        DamagePerSecondToEnemies = irradiatedLeadEffect.DamagePerSecondToEnemies,
                        TintColor = tintColour,
                        DeathTintColor = tintColour,
                        effectIdentifier = irradiatedLeadEffect.effectIdentifier,
                        AppliesTint = true,
                        AppliesDeathTint = true,
                        resistanceType = EffectResistanceType.Poison,

                        //Eh
                        OverheadVFX = irradiatedLeadEffect.OverheadVFX,
                        AffectsEnemies = true,
                        AffectsPlayers = false,
                        AppliesOutlineTint = false,
                        ignitesGoops = false,
                        OutlineTintColor = tintColour,
                        PlaysVFXOnActor = false,
                    };
                    if (duration > 0) poisonToApply.duration = duration;
                    enemy.gameActor.ApplyEffect(poisonToApply, 1f, null);
                    //ETGModConsole.Log("Applied poison");
                }
            }
            else
            {
                ETGModConsole.Log("Target could not be poisoned");
            }
        }

        private Projectile m_projectile;
        public Color tintColour;
        public bool useSpecialTint;
        public int procChance;
        public int duration;
    }
    public class ApplyDirectStatusEffects //----------------------------------------------------------------------------------------------------------------------------
    {
        public static void ApplyDirectFreeze(GameActor target, float duration, float freezeAmount, float damageToDealOnUnfreeze, Color tintColour, Color deathTintColour, EffectResistanceType resistanceType, string identifier, bool tintsEnemy, bool tintsCorpse)
        {
            try
            {
                //ETGModConsole.Log("Attempted to apply direct freeze");
                GameActorFreezeEffect freezeModifierEffect = PickupObjectDatabase.GetById(278).GetComponent<BulletStatusEffectItem>().FreezeModifierEffect;
                GameActorFreezeEffect freezeToApply = new GameActorFreezeEffect
                {
                    duration = duration,
                    TintColor = tintColour,
                    DeathTintColor = deathTintColour,
                    effectIdentifier = identifier,
                    AppliesTint = tintsEnemy,
                    AppliesDeathTint = tintsCorpse,
                    resistanceType = resistanceType,
                    FreezeAmount = freezeAmount,
                    UnfreezeDamagePercent = damageToDealOnUnfreeze,
                    crystalNum = freezeModifierEffect.crystalNum,
                    crystalRot = freezeModifierEffect.crystalRot,
                    crystalVariation = freezeModifierEffect.crystalVariation,
                    FreezeCrystals = freezeModifierEffect.FreezeCrystals,
                    debrisAngleVariance = freezeModifierEffect.debrisAngleVariance,
                    debrisMaxForce = freezeModifierEffect.debrisMaxForce,
                    debrisMinForce = freezeModifierEffect.debrisMinForce,
                    OverheadVFX = freezeModifierEffect.OverheadVFX,
                    vfxExplosion = freezeModifierEffect.vfxExplosion,
                    stackMode = freezeModifierEffect.stackMode,
                    maxStackedDuration = freezeModifierEffect.maxStackedDuration,
                    AffectsEnemies = true,
                    AffectsPlayers = false,
                    AppliesOutlineTint = false,
                    OutlineTintColor = tintColour,
                    PlaysVFXOnActor = true,
                };
                target.ApplyEffect(freezeToApply, 1f, null);

            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
                ETGModConsole.Log(e.StackTrace);
            }
        }
        public static void ApplyDirectPoison(GameActor target, float duration, float dps, Color tintColour, Color deathTintColour, EffectResistanceType resistanceType, string identifier, bool tintsEnemy, bool tintsCorpse)
        {
            GameActorHealthEffect irradiatedLeadEffect = Gungeon.Game.Items["irradiated_lead"].GetComponent<BulletStatusEffectItem>().HealthModifierEffect;
            GameActorHealthEffect poisonToApply = new GameActorHealthEffect
            {
                duration = duration,
                DamagePerSecondToEnemies = dps,
                TintColor = tintColour,
                DeathTintColor = deathTintColour,
                effectIdentifier = identifier,
                AppliesTint = tintsEnemy,
                AppliesDeathTint = tintsCorpse,
                resistanceType = resistanceType,

                //Eh
                OverheadVFX = irradiatedLeadEffect.OverheadVFX,
                AffectsEnemies = true,
                AffectsPlayers = false,
                AppliesOutlineTint = false,
                ignitesGoops = false,
                OutlineTintColor = tintColour,
                PlaysVFXOnActor = false,
            };
            if (target && target.aiActor && target.healthHaver && target.healthHaver.IsAlive)
            {
                target.ApplyEffect(poisonToApply, 1f, null);
            }
        }
        public static void ApplyDirectSlow(GameActor target, float duration, float speedMultiplier, Color tintColour, Color deathTintColour, EffectResistanceType resistanceType, string identifier, bool tintsEnemy, bool tintsCorpse)
        {
            Gun gun = ETGMod.Databases.Items["triple_crossbow"] as Gun;
            GameActorSpeedEffect gameActorSpeedEffect = gun.DefaultModule.projectiles[0].speedEffect;
            GameActorSpeedEffect speedToApply = new GameActorSpeedEffect
            {
                duration = duration,
                TintColor = tintColour,
                DeathTintColor = deathTintColour,
                effectIdentifier = identifier,
                AppliesTint = tintsEnemy,
                AppliesDeathTint = tintsCorpse,
                resistanceType = resistanceType,
                SpeedMultiplier = speedMultiplier,

                //Eh
                OverheadVFX = gameActorSpeedEffect.OverheadVFX,
                AffectsEnemies = true,
                AffectsPlayers = false,
                AppliesOutlineTint = false,
                OutlineTintColor = tintColour,
                PlaysVFXOnActor = false,
            };
            if (target && target.aiActor && target.healthHaver && target.healthHaver.IsAlive)
            {
                target.ApplyEffect(speedToApply, 1f, null);
            }
        }
        public static void ApplyDirectFire(GameActor target, float duration, float dps, Color tintColour, Color deathTintColour, EffectResistanceType resistanceType, string identifier, bool tintsEnemy, bool tintsCorpse)
        {
            GameActorFireEffect fireToApply = new GameActorFireEffect
            {
                duration = duration,
                DamagePerSecondToEnemies = dps,
                TintColor = tintColour,
                DeathTintColor = deathTintColour,
                effectIdentifier = identifier,
                AppliesTint = tintsEnemy,
                AppliesDeathTint = tintsCorpse,
                resistanceType = resistanceType,

                //Eh
                OverheadVFX = StaticStatusEffects.hotLeadEffect.OverheadVFX,
                AffectsEnemies = true,
                AffectsPlayers = false,
                AppliesOutlineTint = false,
                ignitesGoops = StaticStatusEffects.hotLeadEffect.ignitesGoops,
                OutlineTintColor = tintColour,
                PlaysVFXOnActor = StaticStatusEffects.hotLeadEffect.PlaysVFXOnActor,
            };
            if (target && target.aiActor && target.healthHaver && target.healthHaver.IsAlive)
            {
                target.ApplyEffect(fireToApply, 1f, null);
            }
        }
    }
    public class SpawnObjectManager : MonoBehaviour //----------------------------------------------------------------------------------------------------------------------------
    {
        public static void SpawnObject(GameObject thingToSpawn, Vector3 convertedVector, GameObject SpawnVFX, bool correctForWalls = false)
        {
            Vector2 Vector2Position = convertedVector;

            GameObject newObject = Instantiate(thingToSpawn, convertedVector, Quaternion.identity);

            SpeculativeRigidbody ObjectSpecRigidBody = newObject.GetComponentInChildren<SpeculativeRigidbody>();
            Component[] componentsInChildren = newObject.GetComponentsInChildren(typeof(IPlayerInteractable));
            for (int i = 0; i < componentsInChildren.Length; i++)
            {
                IPlayerInteractable interactable = componentsInChildren[i] as IPlayerInteractable;
                if (interactable != null)
                {
                    newObject.transform.position.GetAbsoluteRoom().RegisterInteractable(interactable);
                }
            }
            Component[] componentsInChildren2 = newObject.GetComponentsInChildren(typeof(IPlaceConfigurable));
            for (int i = 0; i < componentsInChildren2.Length; i++)
            {
                IPlaceConfigurable placeConfigurable = componentsInChildren2[i] as IPlaceConfigurable;
                if (placeConfigurable != null)
                {
                    placeConfigurable.ConfigureOnPlacement(GameManager.Instance.Dungeon.data.GetAbsoluteRoomFromPosition(Vector2Position.ToIntVector2()));
                }
            }
            /* FlippableCover component7 = newObject.GetComponentInChildren<FlippableCover>();
             component7.transform.position.XY().GetAbsoluteRoom().RegisterInteractable(component7);
             component7.ConfigureOnPlacement(component7.transform.position.XY().GetAbsoluteRoom());*/

            ObjectSpecRigidBody.Initialize();
            PhysicsEngine.Instance.RegisterOverlappingGhostCollisionExceptions(ObjectSpecRigidBody, null, false);

            if (SpawnVFX != null)
            {
                UnityEngine.Object.Instantiate<GameObject>(SpawnVFX, ObjectSpecRigidBody.sprite.WorldCenter, Quaternion.identity);
            }
            if (correctForWalls) CorrectForWalls(newObject);
        }
        private static void CorrectForWalls(GameObject portal)
        {
            SpeculativeRigidbody rigidbody = portal.GetComponent<SpeculativeRigidbody>();
            if (rigidbody)
            {
                bool flag = PhysicsEngine.Instance.OverlapCast(rigidbody, null, true, false, null, null, false, null, null, new SpeculativeRigidbody[0]);
                if (flag)
                {
                    Vector2 vector = portal.transform.position.XY();
                    IntVector2[] cardinalsAndOrdinals = IntVector2.CardinalsAndOrdinals;
                    int num = 0;
                    int num2 = 1;
                    for (; ; )
                    {
                        for (int i = 0; i < cardinalsAndOrdinals.Length; i++)
                        {
                            portal.transform.position = vector + PhysicsEngine.PixelToUnit(cardinalsAndOrdinals[i] * num2);
                            rigidbody.Reinitialize();
                            if (!PhysicsEngine.Instance.OverlapCast(rigidbody, null, true, false, null, null, false, null, null, new SpeculativeRigidbody[0]))
                            {
                                return;
                            }
                        }
                        num2++;
                        num++;
                        if (num > 200)
                        {
                            goto Block_4;
                        }
                    }
                //return;
                Block_4:
                    UnityEngine.Debug.LogError("FREEZE AVERTED!  TELL RUBEL!  (you're welcome) 147");
                    return;
                }
            }
        }
    }
    public class CompanionisedEnemyBulletModifiers : BraveBehaviour //----------------------------------------------------------------------------------------------
    {
        public CompanionisedEnemyBulletModifiers()
        {
            this.scaleDamage = false;
            this.scaleSize = false;
            this.scaleSpeed = false;
            this.doPostProcess = false;
            this.baseBulletDamage = 10f;
            this.TintBullets = false;
            this.TintColor = Color.grey;
            this.jammedDamageMultiplier = 2f;
        }
        public void Start()
        {
            enemy = base.aiActor;
            AIBulletBank bulletBank2 = enemy.bulletBank;
            foreach (AIBulletBank.Entry bullet in bulletBank2.Bullets)
            {
                bullet.BulletObject.GetComponent<Projectile>().BulletScriptSettings.preventPooling = true;
            }
            if (enemy.aiShooter != null)
            {
                AIShooter aiShooter = enemy.aiShooter;
                aiShooter.PostProcessProjectile = (Action<Projectile>)Delegate.Combine(aiShooter.PostProcessProjectile, new Action<Projectile>(this.PostProcessSpawnedEnemyProjectiles));
            }

            if (enemy.bulletBank != null)
            {
                AIBulletBank bulletBank = enemy.bulletBank;
                bulletBank.OnProjectileCreated = (Action<Projectile>)Delegate.Combine(bulletBank.OnProjectileCreated, new Action<Projectile>(this.PostProcessSpawnedEnemyProjectiles));
            }
        }
        private void PostProcessSpawnedEnemyProjectiles(Projectile proj)
        {
            if (TintBullets) { proj.AdjustPlayerProjectileTint(this.TintColor, 1); }
            if (enemy != null)
            {
                if (enemy.aiActor != null)
                {
                    proj.baseData.damage = baseBulletDamage;
                    if (enemyOwner != null)
                    {
                        //ETGModConsole.Log("Companionise: enemyOwner is not null");
                        if (scaleDamage) proj.baseData.damage *= enemyOwner.stats.GetStatValue(PlayerStats.StatType.Damage);
                        if (scaleSize)
                        {
                            proj.RuntimeUpdateScale(enemyOwner.stats.GetStatValue(PlayerStats.StatType.PlayerBulletScale));
                        }
                        if (scaleSpeed)
                        {
                            proj.baseData.speed *= enemyOwner.stats.GetStatValue(PlayerStats.StatType.ProjectileSpeed);
                            proj.UpdateSpeed();
                        }
                        //ETGModConsole.Log("Damage: " + proj.baseData.damage);
                        if (doPostProcess) enemyOwner.DoPostProcessProjectile(proj);
                    }
                    if (enemy.aiActor.IsBlackPhantom) { proj.baseData.damage = baseBulletDamage * jammedDamageMultiplier; }
                }
            }
            else { ETGModConsole.Log("Shooter is NULL"); }
        }

        private AIActor enemy;
        public PlayerController enemyOwner;
        public bool scaleDamage;
        public bool scaleSize;
        public bool scaleSpeed;
        public bool doPostProcess;
        public float baseBulletDamage;
        public float jammedDamageMultiplier;
        public bool TintBullets;
        public Color TintColor;

    }
    public class EraseFromExistenceOnRoomClear : BraveBehaviour //----------------------------------------------------------------------------------------------
    {
        public void Start()
        {
            RoomHandler parentRoom = base.aiActor.ParentRoom;
            parentRoom.OnEnemiesCleared = (Action)Delegate.Combine(parentRoom.OnEnemiesCleared, new Action(this.RoomCleared));
        }
        public override void OnDestroy()
        {
            if (base.aiActor && base.aiActor.ParentRoom != null)
            {
                RoomHandler parentRoom = base.aiActor.ParentRoom;
                parentRoom.OnEnemiesCleared = (Action)Delegate.Remove(parentRoom.OnEnemiesCleared, new Action(this.RoomCleared));
            }
            base.OnDestroy();
        }
        private void RoomCleared()
        {
            Invoke("DoEliminate", Delay);
        }
        private void DoEliminate()
        {
            if (base.aiActor != null)
            {
                if (this.preventExplodeOnDeath)
                {
                    ExplodeOnDeath component = base.GetComponent<ExplodeOnDeath>();
                    if (component)
                    {
                        component.enabled = false;
                    }
                }
                base.healthHaver.PreventAllDamage = false;
                Exploder.DoDefaultExplosion(aiActor.sprite.WorldCenter, Vector2.zero);
                base.aiActor.EraseFromExistenceWithRewards(false);
            }
        }
        public bool preventExplodeOnDeath;
        public bool forceExplodeOnDeath;
        public float Delay;
    }
    public static class AlterItemStats //----------------------------------------------------------------------------------------------------------------------
    {
        public static void AddStatToActive(PlayerItem item, PlayerStats.StatType statType, float amount, StatModifier.ModifyMethod method = StatModifier.ModifyMethod.ADDITIVE)
        {
            StatModifier modifier = new StatModifier
            {
                amount = amount,
                statToBoost = statType,
                modifyType = method
            };

            if (item.passiveStatModifiers == null)
                item.passiveStatModifiers = new StatModifier[] { modifier };
            else
                item.passiveStatModifiers = item.passiveStatModifiers.Concat(new StatModifier[] { modifier }).ToArray();
        }
        public static void AddStatToPassive(PassiveItem item, PlayerStats.StatType statType, float amount, StatModifier.ModifyMethod method = StatModifier.ModifyMethod.ADDITIVE)
        {
            StatModifier modifier = new StatModifier
            {
                amount = amount,
                statToBoost = statType,
                modifyType = method
            };

            if (item.passiveStatModifiers == null)
                item.passiveStatModifiers = new StatModifier[] { modifier };
            else
                item.passiveStatModifiers = item.passiveStatModifiers.Concat(new StatModifier[] { modifier }).ToArray();
        }
        public static void RemoveStatFromPassive(PassiveItem item, PlayerStats.StatType statType)
        {
            var newModifiers = new List<StatModifier>();
            for (int i = 0; i < item.passiveStatModifiers.Length; i++)
            {
                if (item.passiveStatModifiers[i].statToBoost != statType)
                    newModifiers.Add(item.passiveStatModifiers[i]);
            }
            item.passiveStatModifiers = newModifiers.ToArray();
        }
        public static void RemoveStatFromActive(PlayerItem item, PlayerStats.StatType statType)
        {
            var newModifiers = new List<StatModifier>();
            for (int i = 0; i < item.passiveStatModifiers.Length; i++)
            {
                if (item.passiveStatModifiers[i].statToBoost != statType)
                    newModifiers.Add(item.passiveStatModifiers[i]);
            }
            item.passiveStatModifiers = newModifiers.ToArray();
        }
    }
    public static class AnimateBullet//----------------------------------------------------------------------------------------------
    {
        public static List<T> ConstructListOfSameValues<T>(T value, int length)
        {
            List<T> list = new List<T>();
            for (int i = 0; i < length; i++)
            {
                list.Add(value);
            }
            return list;
        }
        public static void AnimateProjectile(this Projectile proj, List<string> names, int fps, bool loops, List<IntVector2> pixelSizes, List<bool> lighteneds, List<tk2dBaseSprite.Anchor> anchors, List<bool> anchorsChangeColliders,
            List<bool> fixesScales, List<Vector3?> manualOffsets, List<IntVector2?> overrideColliderPixelSizes, List<IntVector2?> overrideColliderOffsets, List<Projectile> overrideProjectilesToCopyFrom)
        {
            tk2dSpriteAnimationClip clip = new tk2dSpriteAnimationClip();
            clip.name = "idle";
            clip.fps = fps;
            List<tk2dSpriteAnimationFrame> frames = new List<tk2dSpriteAnimationFrame>();
            for (int i = 0; i < names.Count; i++)
            {
                string name = names[i];
                IntVector2 pixelSize = pixelSizes[i];
                IntVector2? overrideColliderPixelSize = overrideColliderPixelSizes[i];
                IntVector2? overrideColliderOffset = overrideColliderOffsets[i];
                Vector3? manualOffset = manualOffsets[i];
                bool anchorChangesCollider = anchorsChangeColliders[i];
                bool fixesScale = fixesScales[i];
                if (!manualOffset.HasValue)
                {
                    manualOffset = new Vector2?(Vector2.zero);
                }
                tk2dBaseSprite.Anchor anchor = anchors[i];
                bool lightened = lighteneds[i];
                Projectile overrideProjectileToCopyFrom = overrideProjectilesToCopyFrom[i];
                tk2dSpriteAnimationFrame frame = new tk2dSpriteAnimationFrame();
                frame.spriteId = ETGMod.Databases.Items.ProjectileCollection.inst.GetSpriteIdByName(name);
                frame.spriteCollection = ETGMod.Databases.Items.ProjectileCollection;
                frames.Add(frame);
                int? overrideColliderPixelWidth = null;
                int? overrideColliderPixelHeight = null;
                if (overrideColliderPixelSize.HasValue)
                {
                    overrideColliderPixelWidth = overrideColliderPixelSize.Value.x;
                    overrideColliderPixelHeight = overrideColliderPixelSize.Value.y;
                }
                int? overrideColliderOffsetX = null;
                int? overrideColliderOffsetY = null;
                if (overrideColliderOffset.HasValue)
                {
                    overrideColliderOffsetX = overrideColliderOffset.Value.x;
                    overrideColliderOffsetY = overrideColliderOffset.Value.y;
                }
                tk2dSpriteDefinition def = GunTools.SetupDefinitionForProjectileSprite(name, frame.spriteId, pixelSize.x, pixelSize.y, lightened, overrideColliderPixelWidth, overrideColliderPixelHeight, overrideColliderOffsetX, overrideColliderOffsetY,
                    overrideProjectileToCopyFrom);
                def.ConstructOffsetsFromAnchor(anchor, def.position3, fixesScale, anchorChangesCollider);
                def.position0 += manualOffset.Value;
                def.position1 += manualOffset.Value;
                def.position2 += manualOffset.Value;
                def.position3 += manualOffset.Value;
                if (i == 0)
                {
                    proj.GetAnySprite().SetSprite(frame.spriteCollection, frame.spriteId);
                }
            }
            clip.wrapMode = loops ? tk2dSpriteAnimationClip.WrapMode.Loop : tk2dSpriteAnimationClip.WrapMode.Once;
            clip.frames = frames.ToArray();
            if (proj.sprite.spriteAnimator == null)
            {
                proj.sprite.spriteAnimator = proj.sprite.gameObject.AddComponent<tk2dSpriteAnimator>();
            }
            proj.sprite.spriteAnimator.playAutomatically = true;
            bool flag = proj.sprite.spriteAnimator.Library == null;
            if (flag)
            {
                proj.sprite.spriteAnimator.Library = proj.sprite.spriteAnimator.gameObject.AddComponent<tk2dSpriteAnimation>();
                proj.sprite.spriteAnimator.Library.clips = new tk2dSpriteAnimationClip[0];
                proj.sprite.spriteAnimator.Library.enabled = true;
            }
            proj.sprite.spriteAnimator.Library.clips = proj.sprite.spriteAnimator.Library.clips.Concat(new tk2dSpriteAnimationClip[] { clip }).ToArray();
            proj.sprite.spriteAnimator.DefaultClipId = proj.sprite.spriteAnimator.Library.GetClipIdByName("idle");
            proj.sprite.spriteAnimator.deferNextStartClip = false;
        }
    }
    public class AmmoBasedFormeChanger : MonoBehaviour
    {
        public AmmoBasedFormeChanger()
        {
            highestAmmoGunID = -1;
            higherAmmoGunID = -1;
            baseGunID = -1;
            lowerAmmoGunID = -1;
            lowestAmmoGunID = -1;

            highestAmmoAmount = -1;
            higherAmmoAmount = -1;
            lowerAmmoAmount = -1;
            lowestAmmoAmount = -1;
        }

        private void Awake()
        {
            this.m_gun = base.GetComponent<Gun>();
        }

        private void Update()
        {
            if (Dungeon.IsGenerating || Dungeon.ShouldAttemptToLoadFromMidgameSave)
            {
                return;
            }
            if (this.m_gun && this.m_gun.CurrentOwner is PlayerController)
            {
                PlayerController playerController = this.m_gun.CurrentOwner as PlayerController;
                if (!this.m_gun.enabled)
                {
                    return;
                }
                int currentAmmo = this.m_gun.CurrentAmmo;
                if (currentAmmo != cachedAmmo)
                {
                    DetermineForme(currentAmmo);
                    cachedAmmo = currentAmmo;
                }
            }
        }
        private void DetermineForme(int currentAmmo)
        {
            if (highestAmmoGunID != -1 && currentAmmo > highestAmmoAmount)
            {
                if (Forme != "Highest")
                {
                    this.m_gun.TransformToTargetGun(PickupObjectDatabase.GetById(this.highestAmmoGunID) as Gun);
                    Forme = "Highest";

                }
            }
            else if (higherAmmoGunID != -1 && currentAmmo > higherAmmoAmount && (highestAmmoGunID == -1 || currentAmmo < highestAmmoAmount))
            {
                if (Forme != "Higher")
                {
                    this.m_gun.TransformToTargetGun(PickupObjectDatabase.GetById(this.higherAmmoGunID) as Gun);
                    Forme = "Higher";

                }
            }
            else if (lowerAmmoGunID != -1 && currentAmmo < lowerAmmoAmount && (lowestAmmoGunID == -1 || currentAmmo > lowestAmmoAmount))
            {
                if (Forme != "Lower")
                {
                    this.m_gun.TransformToTargetGun(PickupObjectDatabase.GetById(this.lowerAmmoGunID) as Gun);
                    Forme = "Lower";

                }
            }
            else if (lowestAmmoGunID != -1 && currentAmmo < lowestAmmoAmount)
            {
                if (Forme != "Lowest")
                {
                    this.m_gun.TransformToTargetGun(PickupObjectDatabase.GetById(this.lowestAmmoGunID) as Gun);
                    Forme = "Lowest";
                }
            }
            else if (baseGunID != -1 && (currentAmmo > lowerAmmoAmount || lowerAmmoAmount == -1) && (currentAmmo < higherAmmoAmount || higherAmmoAmount == -1))
            {
                if (Forme != "Normal")
                {
                    this.m_gun.TransformToTargetGun(PickupObjectDatabase.GetById(this.baseGunID) as Gun);
                    Forme = "Normal";
                }
            }
            else
            {
                ETGModConsole.Log("That's weird, this message shouldn't be seen unless I fucked up. Send this to Nevernamed and tell him he needs to fix his Ammo Based Forme Switcher.");
            }


        }
        private int cachedAmmo;



        private Gun m_gun;
        //IDS
        public int baseGunID;
        public int higherAmmoGunID;
        public int highestAmmoGunID;
        public int lowerAmmoGunID;
        public int lowestAmmoGunID;

        //AMMO MEASUREMENTS
        public int highestAmmoAmount;
        public int higherAmmoAmount;
        public int lowerAmmoAmount;
        public int lowestAmmoAmount;

        private string Forme;
    }
    public class TextBubble
    {
        public static void DoAmbientTalk(Transform baseTransform, Vector3 offset, string stringKey, float duration)
        {
            TextBoxManager.ShowTextBox(baseTransform.position + offset, baseTransform, duration, stringKey, string.Empty, false, TextBoxManager.BoxSlideOrientation.NO_ADJUSTMENT, false, false);
        }
    }
    class LoadHelper
    {
        public static UnityEngine.Object LoadAssetFromAnywhere(string path)
        {
            UnityEngine.Object obj = null;
            foreach (string name in BundlePrereqs)
            {
                try
                {
                    obj = ResourceManager.LoadAssetBundle(name).LoadAsset(path);
                }
                catch
                {
                }
                if (obj != null)
                {
                    break;
                }
            }
            if (obj == null)
            {
                //obj = Toolbox.specialeverything.LoadAsset<UnityEngine.Object>(path);
            }
            return obj;
        }
        public static T LoadAssetFromAnywhere<T>(string path) where T : UnityEngine.Object
        {
            T obj = null;
            foreach (string name in BundlePrereqs)
            {
                try
                {
                    obj = ResourceManager.LoadAssetBundle(name).LoadAsset<T>(path);
                }
                catch
                {
                }
                if (obj != null)
                {
                    break;
                }
            }
            if (obj == null)
            {
                //obj = Toolbox.specialeverything.LoadAsset<T>(path);
            }
            return obj;
        }
        public static List<T> Find<T>(string toFind) where T : UnityEngine.Object
        {
            List<T> objects = new List<T>();
            foreach (string name in BundlePrereqs)
            {
                try
                {
                    foreach (string str in ResourceManager.LoadAssetBundle(name).GetAllAssetNames())
                    {
                        if (str.ToLower().Contains(toFind))
                        {
                            if (ResourceManager.LoadAssetBundle(name).LoadAsset(str).GetType() == typeof(T) && !objects.Contains(ResourceManager.LoadAssetBundle(name).LoadAsset<T>(str)))
                            {
                                objects.Add(ResourceManager.LoadAssetBundle(name).LoadAsset<T>(str));
                            }
                        }
                    }
                }
                catch
                {
                }
            }
            return objects;
        }
        public static List<UnityEngine.Object> Find(string toFind)
        {
            List<UnityEngine.Object> objects = new List<UnityEngine.Object>();
            foreach (string name in BundlePrereqs)
            {
                try
                {
                    foreach (string str in ResourceManager.LoadAssetBundle(name).GetAllAssetNames())
                    {
                        if (str.ToLower().Contains(toFind))
                        {
                            if (!objects.Contains(ResourceManager.LoadAssetBundle(name).LoadAsset(str)))
                            {
                                objects.Add(ResourceManager.LoadAssetBundle(name).LoadAsset(str));
                            }
                        }
                    }
                }
                catch
                {
                }
            }
            return objects;
        }
        static LoadHelper()
        {
            LoadHelper.BundlePrereqs = new string[]
            {
                "brave_resources_001",
                "dungeon_scene_001",
                "encounters_base_001",
                "enemies_base_001",
                "flows_base_001",
                "foyer_001",
                "foyer_002",
                "foyer_003",
                "shared_auto_001",
                "shared_auto_002",
                "shared_base_001",
                "dungeons/base_bullethell",
                "dungeons/base_castle",
                "dungeons/base_catacombs",
                "dungeons/base_cathedral",
                "dungeons/base_forge",
                "dungeons/base_foyer",
                "dungeons/base_gungeon",
                "dungeons/base_mines",
                "dungeons/base_nakatomi",
                "dungeons/base_resourcefulrat",
                "dungeons/base_sewer",
                "dungeons/base_tutorial",
                "dungeons/finalscenario_bullet",
                "dungeons/finalscenario_convict",
                "dungeons/finalscenario_coop",
                "dungeons/finalscenario_guide",
                "dungeons/finalscenario_pilot",
                "dungeons/finalscenario_robot",
                "dungeons/finalscenario_soldier"
            };
        }
        private static string[] BundlePrereqs;
    }
    public class AdjustGunPosition
    {
        public static List<tk2dSpriteAnimationClip> GetGunAnimationClips(Gun gun)
        {
            List<tk2dSpriteAnimationClip> clips = new List<tk2dSpriteAnimationClip>();
            if (!string.IsNullOrEmpty(gun.shootAnimation) && gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation) != null)
            {
                clips.Add(gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation));
            }
            if (!string.IsNullOrEmpty(gun.reloadAnimation) && gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.reloadAnimation) != null)
            {
                clips.Add(gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.reloadAnimation));
            }
            if (!string.IsNullOrEmpty(gun.emptyReloadAnimation) && gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.emptyReloadAnimation) != null)
            {
                clips.Add(gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.emptyReloadAnimation));
            }
            if (!string.IsNullOrEmpty(gun.idleAnimation) && gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.idleAnimation) != null)
            {
                clips.Add(gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.idleAnimation));
            }
            if (!string.IsNullOrEmpty(gun.chargeAnimation) && gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.chargeAnimation) != null)
            {
                clips.Add(gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.chargeAnimation));
            }
            if (!string.IsNullOrEmpty(gun.dischargeAnimation) && gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.dischargeAnimation) != null)
            {
                clips.Add(gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.dischargeAnimation));
            }
            if (!string.IsNullOrEmpty(gun.emptyAnimation) && gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.emptyAnimation) != null)
            {
                clips.Add(gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.emptyAnimation));
            }
            if (!string.IsNullOrEmpty(gun.introAnimation) && gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.introAnimation) != null)
            {
                clips.Add(gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.introAnimation));
            }
            if (!string.IsNullOrEmpty(gun.finalShootAnimation) && gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.finalShootAnimation) != null)
            {
                clips.Add(gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.finalShootAnimation));
            }
            if (!string.IsNullOrEmpty(gun.enemyPreFireAnimation) && gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.enemyPreFireAnimation) != null)
            {
                clips.Add(gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.enemyPreFireAnimation));
            }
            if (!string.IsNullOrEmpty(gun.outOfAmmoAnimation) && gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.outOfAmmoAnimation) != null)
            {
                clips.Add(gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.outOfAmmoAnimation));
            }
            if (!string.IsNullOrEmpty(gun.criticalFireAnimation) && gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.criticalFireAnimation) != null)
            {
                clips.Add(gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.criticalFireAnimation));
            }
            if (!string.IsNullOrEmpty(gun.dodgeAnimation) && gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.dodgeAnimation) != null)
            {
                clips.Add(gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.dodgeAnimation));
            }
            return clips;
        }
    }
}
