namespace CwaffingTheGungy;

using System;
using static ShmuppyCompanion.ShmuppyMovementBehavior.State;

public class ChewToy : CwaffCompanion
{
    public static string ItemName         = "Chew Toy";
    public static string ShortDescription = "Bullet Bitten";
    public static string LongDescription  = "Spawns a friendly Shmuppy. While in combat, Shmuppy will distract and draw fire from a single enemy. Shmuppy is scared of bosses, and will play dead during boss fights.";
    public static string Lore             = "While normally completely loyal to the Bullet Kin, Shmuppies have an incredibly soft spot for their favorite chew toys, and with minimal effort, can be coaxed into joining any friendly Gungeoneer on their adventure. They seem to be a lot more easily distracted while by your side, but their heart is in the right place.";

    public static string CompanionName    = "Shmuppy";
    public static string EnemyDesc        = "Bullet Dog";
    public static string EnemyLongDesc    = "A valued companion in bullet society. While docile and physically harmless most of the time, their barks can disrupt the passage of fired bullets and thus are a helpful way to mildly disarm trespassing gungeoneers, hopefully long enough to be stopped by reinforcements.";

    public static AIActor ShmuppyEnemyPrefab = null;

    private static readonly string[] Directions = ["back", "back_right", "right", "front_right", "front", "front_left", "left", "back_left"];

    public static void Init()
    {
        PassiveItem item  = Lazy.SetupPassive<ChewToy>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.D;

        ShmuppyCompanion friend = item.InitCompanion<ShmuppyCompanion>(friendName: CompanionName, baseFps: 20,
            extraAnims: ["bark", "hurt", "death"/*, "pitfall"*/])
          .SetPettingOffsets(new Vector2(-0.875f, -0.25f), new Vector2(0.6875f, -0.25f));
        friend.gameObject.GetComponent<BehaviorSpeculator>().MovementBehaviors.Add(new ShmuppyCompanion.ShmuppyMovementBehavior());

        // init enemy variant
        ShmuppyEnemyPrefab = CompanionName.InitEnemy(health: 30, baseFps: 20, extraAnims: ["bark", "hurt", "death"/*, "pitfall"*/],
          shortDesc: EnemyDesc, longDesc: EnemyLongDesc);
        ShmuppyEnemyPrefab.gameObject.GetComponent<BehaviorSpeculator>().MovementBehaviors.Add(new ShmuppyCompanion.ShmuppyMovementBehavior());

        // init common stuff
        foreach (AIActor actor in (AIActor[])[friend.aiActor, ShmuppyEnemyPrefab])
        {
          if (actor.gameObject.GetComponent<KnockbackDoer>() is KnockbackDoer kbd)
            kbd.weight = 30f;
          tk2dSpriteAnimation anims = actor.gameObject.GetComponent<tk2dSpriteAnimation>();
          foreach (string s in Directions)
          {
              anims.GetClipByName("shmuppy_idle_"+s).fps = 8;
              anims.GetClipByName("shmuppy_bark_"+s).frames[2].AddSound("shmuppy_bark_sound");
          }
        }
    }

    [HarmonyPatch]
    private static class ShmuppyReinforcementPatch
    {
      // #if DEBUG
      // private const float _SPAWN_CHANCE = 1.00f;
      // #else
      private const float _SPAWN_CHANCE = 0.02f;
      // #endif
      private static bool _SpawnedThisRoom = false;
      private static RoomHandler _LastSpawnedRoom = null;

      [HarmonyPatch(typeof(AIActor), nameof(AIActor.OnEngaged))]
      [HarmonyPrefix]
      private static void AIActorOnEngagedPrefixPatch(AIActor __instance, bool isReinforcement, ref bool __state)
      {
          __state = false; // by default, do nothing
          if (!__instance || __instance.m_hasBeenEngaged || __instance.IsInReinforcementLayer || isReinforcement)
            return;
          if (__instance.EnemyGuid != Enemies.BulletKin)
            return;
          // #if !DEBUG
          if (UnityEngine.Random.value > _SPAWN_CHANCE)
            return;
          // #endif
          __state = true;
      }

      [HarmonyPatch(typeof(AIActor), nameof(AIActor.OnEngaged))]
      [HarmonyPostfix]
      private static void AIActorOnEngagedPostfixPatch(AIActor __instance, bool isReinforcement, ref bool __state)
      {
        if (!__state)
          return; // failed prefix patch checks
        if (!__instance || __instance.CenterPosition.GetAbsoluteRoom() is not RoomHandler room || room == null)
          return;
        if (_LastSpawnedRoom != room)
        {
          _LastSpawnedRoom = room;
          _SpawnedThisRoom = false;
        }
        if (_SpawnedThisRoom)
          return;

        AIActor.Spawn(
            prefabActor     : ShmuppyEnemyPrefab,
            position        : ShmuppyEnemyPrefab.RandomCellForEnemySpawn(room, spawnFarFromPlayer: true) ?? __instance.CenterPosition.ToIntVector2(),
            source          : room,
            correctForWalls : true,
            awakenAnimType  : AIActor.AwakenAnimationType.Spawn)
          .SpawnInInstantly(isReinforcement: false);
        _SpawnedThisRoom = true;
      }
    }
}

public class ShmuppyCompanion : CwaffCompanionController
{
    public class ShmuppyMovementBehavior : CwaffCompanionMovementBehaviorBase
    {
        internal enum State {
            IDLE,     // doing nothing
            FOLLOW,   // following its owner
            CHASE,    // chasing its target
            BARK,     // barking at its target to gain their attention
            DISTRACT, // running around its target once they're paying attention
            PLAYDEAD, // playing dead during a boss fight
        }

        private const float _SPEED         = 10.0f;
        private const float NEAR_OWNER     = 2.0f;
        private const float FAR_FROM_OWNER = 6.0f;

        private State _state = IDLE;
        private HealthHaver _hh = null;
        private UltraFortunesFavor _uff = null;

        public override void Start()
        {
            base.Start();
            this.retargetOnPathingFailure = true;
            m_aiActor.specRigidbody.CollideWithOthers = true; //NOTE: this doesn't work in CwaffCompanion.InitCompanion for some reason...investigate later
            m_aiActor.MovementSpeed = _SPEED;
            this._hh = m_aiActor.gameObject.GetComponent<HealthHaver>();
            if (!_isCompanion)
              m_aiActor.specRigidbody.OnPreRigidbodyCollision += this.OnPreRigidbodyCollision;
        }

        private void SetupUltraFortunesFavor()
        {
          if (this._uff)
            return;

          this._uff = m_aiActor.gameObject.AddComponent<UltraFortunesFavor>();
          this._uff.sparkOctantVFX      = CwaffCompanionAndEnemyBuilder.FortunesFavorVFX;
          this._uff.vfxOffset           = 0.625f;
          this._uff.bulletRadius        = 2f;
          this._uff.bulletSpeedModifier = 0.8f;
          this._uff.beamRadius          = 2f;
          this._uff.goopRadius          = 2f;
          this._uff.enabled             = true;
          this.m_aiActor.RegenerateCache();
        }

        private void DismissUltraFortunesFavor()
        {
          if (!this._uff)
            return;

          if (this._uff.m_bulletBlocker != null)
            base.m_aiActor.specRigidbody.PixelColliders.Remove(this._uff.m_bulletBlocker);
          if (this._uff.m_beamReflector != null)
            base.m_aiActor.specRigidbody.PixelColliders.Remove(this._uff.m_beamReflector);
          UnityEngine.Object.Destroy(this._uff);
          this._uff = null;
          this.m_aiActor.RegenerateCache();
        }

        private void OnPreRigidbodyCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
        {
          if (otherRigidbody.gameObject.GetComponent<PlayerController>())
            PhysicsEngine.SkipCollision = true;
        }

        private void ResetState()
        {
            this._state = IDLE;
            this.m_stateTimer = 0.5f;
            base.m_aiAnimator.EndAnimation();
            if (this._targetActor is AIActor enemy && enemy.OverrideTarget == this.m_aiActor.specRigidbody)
              enemy.OverrideTarget = null;
            DismissUltraFortunesFavor();
        }

        private AIActor FindNearbyBulletKin()
        {
            foreach(AIActor enemy in m_aiActor.CenterPosition.GetAllNearbyEnemies(radius: 16f, includeInvulnerable: true))
              if (enemy.AmmonomiconName().Contains("Bullet Kin"))
                return enemy;
            return null;
        }

        private void FollowOwner()
        {
            this._targetActor = _isCompanion ? m_companionController.m_owner : FindNearbyBulletKin();
            this._targetPos = this._targetActor ? this._targetActor.CenterPosition : m_aiActor.CenterPosition;
            this._state = NearTargetActor(FAR_FROM_OWNER) ? IDLE : FOLLOW;
            RepathToTarget();
        }

        protected override void OnWarp()
        {
            DetermineNewTarget();
        }

        protected override void TickMovement(ref Vector2 voluntaryVel, ref Vector2 involuntaryVel)
        {
            base.TickMovement(ref voluntaryVel, ref involuntaryVel);
            if (this._state != BARK)
              DismissUltraFortunesFavor();
            else if (this._state == BARK && this._targetActor)
            {
              float targetDir = (this._targetActor.CenterPosition - m_aiActor.CenterPosition).ToAngle();
              base.m_aiAnimator.FacingDirection = targetDir;
            }
            // m_aiActor.DebugNametag($"{this._state.ToString()}\n{this.m_stateTimer}\n{base.m_aiAnimator.spriteAnimator.CurrentClip.name}");
        }

        protected override bool IsTargetValid()
        {
            switch(this._state)
            {
                case IDLE:     return true;                  // end state manually
                case FOLLOW:   return this._targetActor;     // end state manually or when player is dead
                case CHASE:    return this._targetActor;     // end state manually or when enemy is dead
                case BARK:     return this._targetActor;     // end state manually or when enemy is dead
                case DISTRACT: return this._targetActor;     // end state manually or when enemy is dead
                case PLAYDEAD: return true;                  // end state manually
            }
            return false;
        }

        private void DetermineNewTargetAsCompanion()
        {
            this._allowPathing = true;
            ResetState();
            if (m_companionController.m_owner is not PlayerController owner)
              return;

            FollowOwner();
            if (!owner.IsInCombat)
              return;

            ReadOnlyCollection<AIActor> enemies = m_aiActor.CenterPosition.GetAllNearbyEnemies(radius: 16f);
            if (enemies.Count == 0 || enemies.ChooseRandom() is not AIActor enemy)
              return;

            this._targetActor = enemy;
            this._targetPos = this._targetActor.CenterPosition;
            this._state = CHASE;
        }

        private void DetermineNewTargetAsEnemy()
        {
            this._allowPathing = true;
            ResetState();

            PlayerController targetPlayer = GameManager.Instance.GetRandomActivePlayer();
            if (!targetPlayer)
              return;

            this._targetActor = targetPlayer;
            this._targetPos = this._targetActor.CenterPosition;
            this._state = CHASE;
        }

        protected override void DetermineNewTarget()
        {
            if (_isCompanion)
              DetermineNewTargetAsCompanion();
            else
              DetermineNewTargetAsEnemy();
        }

        protected override void UpdateStateAndTargetPosition()
        {
            if (this._targetActor && this._state != DISTRACT)
              this._targetPos = this._targetActor.CenterPosition;
        }

        protected override bool ReachedTarget()
        {
            if (_isCompanion && m_companionController && m_companionController.IsBeingPet)
              return false;

            switch(this._state)
            {
                case IDLE:     return true;                                  // idle state is always reached
                case FOLLOW:   return NearTargetActor(NEAR_OWNER);           // near target
                case CHASE:    return NearTargetActor(5f);                   // near target
                case BARK:     return NearTargetActor(3f) || StateExpired(); // near target or timeout
                case DISTRACT: return NearTargetPos(3f) || StateExpired();   // near target or timeout
                case PLAYDEAD: return !InCombat();                           // boss battle is over
            }
            return false;
        }

        private Vector2 GetRandomVisiblePointInRoom()
        {
            RoomHandler room;
            if (_isCompanion && m_companionController.m_owner is PlayerController player)
              room = player.CurrentRoom;
            else
              room = m_aiActor.CenterPosition.GetAbsoluteRoom();
            if (room != null)
              return room.GetRandomVisibleClearSpot(2, 2).ToVector2();
            return m_aiActor.CenterPosition;
        }

        protected override void OnReachedTarget()
        {
            switch(this._state)
            {
                case IDLE:
                {
                  DetermineNewTarget();
                  break;
                }
                case FOLLOW:
                {
                  ResetState();
                  break;
                }
                case CHASE:
                {
                  this._allowPathing = false;
                  m_aiActor.ClearPath();
                  if (this._targetActor && this._targetActor.gameObject.GetComponent<HealthHaver>() is HealthHaver hh)
                    if (hh.IsBoss || hh.IsSubboss)
                      {
                        this._state = PLAYDEAD;
                        base.m_aiAnimator.PlayUntilCancelled("death");
                        break;
                      }
                  this._state = BARK;
                  if (!this._isCompanion)
                    SetupUltraFortunesFavor();
                  this.m_stateTimer = 3.0f;
                  if (this._targetActor is AIActor enemy && enemy.OverrideTarget == null)
                    enemy.OverrideTarget = this.m_aiActor.specRigidbody;
                  if (this._targetActor)
                  {
                    float targetDir = (this._targetActor.CenterPosition - m_aiActor.CenterPosition).ToAngle();
                    base.m_aiAnimator.FacingDirection = targetDir;
                  }
                  base.m_aiAnimator.EndAnimation();
                  base.m_aiAnimator.PlayUntilCancelled("bark");
                  break;
                }
                case BARK:
                {
                  ResetState();
                  if (this._targetActor is AIActor enemy && enemy.OverrideTarget == null)
                    enemy.OverrideTarget = this.m_aiActor.specRigidbody;
                  this._allowPathing = true;
                  this._state = DISTRACT;
                  this.m_stateTimer = 5.0f;
                  this._targetPos = GetRandomVisiblePointInRoom();
                  break;
                }
                case DISTRACT:
                {
                  if (this.m_stateTimer <= 0.0f)
                  {
                    ResetState();
                    FollowOwner();
                    break;
                  }
                  this._targetPos = GetRandomVisiblePointInRoom();
                  break;
                }
                case PLAYDEAD:
                {
                  FollowOwner();
                  break;
                }
            }
        }
    }
}
