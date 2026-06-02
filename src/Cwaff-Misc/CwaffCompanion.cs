namespace CwaffingTheGungy;

using static DirectionalAnimation.DirectionType;

[HarmonyPatch]
public abstract class CwaffCompanionController : CompanionController
{
  [SerializeField]
  private Vector2 _petOffsetRight = Vector2.zero;
  [SerializeField]
  private Vector2 _petOffsetLeft  = Vector2.zero;

  internal void SetPettingOffsetsInternal(Vector2 right, Vector2? left = null)
  {
    this._petOffsetRight = right;
    this._petOffsetLeft = left ?? right;
  }

  [HarmonyPatch(typeof(CompanionController), nameof(CompanionController.DoPet))]
  [HarmonyPostfix]
  private static void PettingFixerPatch(CompanionController __instance, PlayerController player)
  {
    if (__instance is not CwaffCompanionController ccc)
      return;
    if (__instance.aiAnimator.FacingDirection < 90f)
      ccc.m_petOffset = ccc._petOffsetRight;
    else
      ccc.m_petOffset = ccc._petOffsetLeft;
  }
}

public abstract class CwaffCompanionMovementBehaviorBase : MovementBehaviorBase
{
  public float pathInterval = 0.25f; // amount of time between repathing
  public bool retargetOnPathingFailure = false; // if true, determine a new target after warping due to pathing failure
  public bool preventWarpingWhenOnscreen = false; // if true, don't warp when on screen, even if we're in a different room than our owner

  protected GameActor _targetActor; // actor we're currently targeting
  protected float m_repathTimer; // timer until next attempt at repathing
  protected float m_stateTimer; // timer until our current state has expired
  protected CompanionController m_companionController; // companion controller for this behavior
  protected Vector2 _targetPos; // position we're currently targeting
  protected bool _allowPathing = true; // whether we're currently allowed to path
  protected bool _isCompanion = false; // whether we're a companion or an enemy

  private int m_sequentialPathFails;

  public override void Start()
  {
    base.Start();
    m_companionController = m_gameObject.GetComponent<CompanionController>();
    _isCompanion = m_companionController != null;
    m_aiActor.MovementModifiers += this.TickMovement;
  }

  public override void Destroy()
  {
      if (m_aiActor)
          m_aiActor.MovementModifiers -= this.TickMovement;
      base.Destroy();
  }

  /// <summary>Called every frame while moving.</summary>
  protected virtual void TickMovement(ref Vector2 voluntaryVel, ref Vector2 involuntaryVel) {}
  /// <summary>Called every normal tick at the beginning of Update</summary>
  protected virtual void UpdateStateAndTargetPosition() {}
  /// <summary>Returns true iff the current target is valid. Update() will automatically call DetermineNewTarget() if this returns false.</summary>
  protected virtual bool IsTargetValid() => false;
  /// <summary>Returns true iff the companion has reached its designated target</summary>
  protected virtual bool ReachedTarget() => false;
  /// <summary>Called every update as long as ReachedTarget() is true</summary>
  protected virtual void OnReachedTarget() {}
  /// <summary>Updates _targetPos and potentially _targetActor</summary>
  protected virtual void DetermineNewTarget() {}
  /// <summary>Return true to prevent the companion from warping.</summary>
  protected virtual bool PreventWarping() => false;
  /// <summary>Called immediately before warping.</summary>
  protected virtual void OnPreWarp() {}
  /// <summary>Called immediately after warping.</summary>
  protected virtual void OnWarp() {}

  /// <summary>Attempts to path over to _targetPos, calling DetermineNewTarget() as necessary on failure.</summary>
  protected void RepathToTarget()  {
      // adjust relative to the center of our sprite
      Vector2 bottomLeft = m_aiActor.transform.position.XY();
      Vector2 center = m_aiActor.sprite.WorldCenter;
      Vector2 adjustedTarget = this._targetPos + (bottomLeft - center);

      if (m_repathTimer > 0f)
          return;

      m_repathTimer = pathInterval;
      if (m_companionController && m_companionController.IsBeingPet)
      {
          Vector2 petterPos = m_companionController.m_pettingDoer.specRigidbody.UnitCenter + m_companionController.m_petOffset;
          if (Vector2.Distance(petterPos, m_aiActor.specRigidbody.UnitCenter) < 0.08f)
              m_aiActor.ClearPath();
          else
              m_aiActor.PathfindToPosition(petterPos, petterPos);
      }
      else
          m_aiActor.PathfindToPosition(adjustedTarget);

      if (m_aiActor.Path == null)
      {
          m_sequentialPathFails = 0;
          return;
      }

      if (m_aiActor.Path.InaccurateLength > 50f)
      {
          m_aiActor.ClearPath();
          m_sequentialPathFails = 0;
          Warp(adjustedTarget);
          if (retargetOnPathingFailure)
            DetermineNewTarget();
      }
      else if (!m_aiActor.Path.WillReachFinalGoal && (++m_sequentialPathFails) > 3)
      {
          CellData cellData2 = GameManager.Instance.Dungeon.data[adjustedTarget.ToIntVector2(VectorConversions.Floor)];
          if (cellData2 != null && cellData2.IsPassable)
          {
              m_sequentialPathFails = 0;
              Warp(adjustedTarget);
              if (retargetOnPathingFailure)
                DetermineNewTarget();
          }
      }
      else
          m_sequentialPathFails = 0;
  }

  /// <summary>Warps the companion to a given position.</summary>
  protected void Warp(Vector2 pos)
  {
      OnPreWarp();
      m_aiActor.CompanionWarp(pos);
      OnWarp();
  }

  /// <summary>Checks if the companion is within a certain radius of its target position.</summary>
  protected bool NearTargetPos(float radius, bool checkInBounds = true)
  {
    if (checkInBounds && !m_aiActor.CenterPosition.InBounds())
      return false;
    return (this._targetPos - m_aiActor.CenterPosition).sqrMagnitude <= (radius * radius);
  }

  /// <summary>Checks if the companion is within a certain radius of its target actor.</summary>
  protected bool NearTargetActor(float radius, bool checkInBounds = true)
  {
    if (!this._targetActor || (checkInBounds && !m_aiActor.CenterPosition.InBounds()))
      return false;
    return (this._targetActor.CenterPosition - m_aiActor.CenterPosition).sqrMagnitude <= (radius * radius);
  }

  /// <summary>Checks if the companion has line of sight its target position.</summary>
  protected bool CanSeeTargetPos()
  {
    return m_aiActor.CenterPosition.HasLineOfSight(this._targetPos);
  }

  /// <summary>Checks if the companion has line of sight its target actor.</summary>
  protected bool CanSeeTargetActor()
  {
    return this._targetActor && m_aiActor.CenterPosition.HasLineOfSight(this._targetActor.CenterPosition);
  }

  /// <summary>Returns true iff the companion is over or within one unit of a pit cell.</summary>
  protected bool NearPit()
  {
    return m_aiActor.CenterPosition.NearPit();
  }

  /// <summary>Returns true iff the companion is in combat.</summary>
  protected bool InCombat()
  {
    return m_companionController && m_companionController.m_owner is PlayerController p && p.IsInCombat;
  }

  /// <summary>Determines the distance to _targetPos relative to the center of our sprite.</summary>
  protected Vector2 DeltaToTarget()
  {
      // adjust relative to the center of our sprite
      Vector2 bottomLeft = m_aiActor.transform.position.XY();
      Vector2 adjustedTarget = this._targetPos - m_aiActor.sprite.GetRelativePositionFromAnchor(Anchor.MiddleCenter);
      return adjustedTarget - bottomLeft;
  }

  /// <summary>Returns true if m_stateTimer <= 0.</summary>
  protected bool StateExpired() => m_stateTimer <= 0.0f;

  /// <summary>Returns true iff the companion is offscreen and/or in a different room.</summary>
  private bool CheckOffscreenForWarpingPurposes()
  {
      if (!m_companionController)
          return false;
      if (!preventWarpingWhenOnscreen)
          return m_companionController.InDifferentRoomThanOwner();
      Vector2 pos = m_aiActor.CenterPosition;
      if (GameManager.Instance.MainCameraController.PointIsVisible(pos, 0.4f))
          return false;
      RoomHandler ownerRoom = m_companionController.m_owner.CurrentRoom;
      return (ownerRoom == null || ownerRoom != pos.GetAbsoluteRoom());
  }

  /// <summary>Called every tick the behavior is active.</summary>
  public sealed override void Upkeep()
  {
      base.Upkeep();
      DecrementTimer(ref m_repathTimer);
      DecrementTimer(ref m_stateTimer);
  }

  /// <summary>Called after Upkeep() every tick the behavior is active and the companion is not stunned.</summary>
  public sealed override BehaviorResult Update()
  {
      if (!GameManager.HasInstance || GameManager.Instance.IsLoadingLevel)
          return BehaviorResult.SkipAllRemainingBehaviors;

      if (GameManager.Instance.CurrentLevelOverrideState == GameManager.LevelOverrideState.END_TIMES)
      {
          m_aiActor.ClearPath();
          return BehaviorResult.SkipAllRemainingBehaviors;
      }

      if (!IsTargetValid())
          DetermineNewTarget();
      UpdateStateAndTargetPosition();
      if (CheckOffscreenForWarpingPurposes() && !PreventWarping())
      {
          Warp(m_companionController.m_owner.CenterPosition);
          if (retargetOnPathingFailure)
            DetermineNewTarget();
      }
      else if (ReachedTarget())
          OnReachedTarget();
      else if (this._allowPathing)
          RepathToTarget();

      return BehaviorResult.SkipRemainingClassBehaviors;
  }
}

public static class CwaffCompanionAndEnemyBuilder
{
    public static GameObject FortunesFavorVFX = null;

    public static Friend InitCompanion<Friend>(this PassiveItem item, string friendName = null, int baseFps = 4, List<string> extraAnims = null, bool autoRigidBody = true)
        where Friend : CwaffCompanionController
    {
        if (item is not CwaffCompanion cc)
        {
            Lazy.RuntimeWarn("Trying to create a companion for an item that is not a CwaffCompanion");
            return null;
        }

        string name = (friendName ?? item.itemName).ToID();
        if (ResMap.Get($"{name}_idle") == null)
        {
            Lazy.RuntimeWarn($"All companions must have an idle_001 animation, but no \"{name}_idle_001\" sprite found");
            return null;
        }

        Friend friend = CompanionBuilder.BuildPrefab(name, $"{C.MOD_PREFIX}:{name}_companion", $"{name}_idle_001", IntVector2.Zero, IntVector2.One)
          .AddComponent<Friend>();
        AIActor actor = friend.gameObject.GetComponent<AIActor>();
        actor.SetupAnimations(baseFps: baseFps, extraAnims: extraAnims);
        //NOTE: should use LowerLeft anchor if we have a SpeculativeRigidBody and MiddleCenter otherwise
        if (autoRigidBody)
        {
          actor.gameObject.AutoRigidBody(CollisionLayer.EnemyCollider);
          actor.ActorShadowOffset = Vector2.zero;
        }
        else
          actor.ActorShadowOffset = new Vector3(actor.sprite.GetRelativePositionFromAnchor(Anchor.LowerLeft).x, 0f, 0f);
        friend.companionID = CompanionController.CompanionIdentifier.NONE;

        cc.CompanionGuid = actor.EnemyGuid;
        return friend;
    }

    public static AIActor InitEnemy(this string enemyName, int health, string shortDesc = null, string longDesc = null, int baseFps = 4, List<string> extraAnims = null,
      bool autoRigidBody = true, bool doCorpse = true)
    {
        string name = enemyName.ToID();
        if (ResMap.Get($"{name}_idle") == null)
        {
            Lazy.RuntimeWarn($"All enemies must have an idle_001 animation, but no \"{name}_idle_001\" sprite found");
            return null;
        }

        AIActor actor = EnemyBuilder.BuildPrefab(name, $"{C.MOD_PREFIX}:{name}_enemy", $"{name}_idle_001", IntVector2.Zero, IntVector2.One, false)
          .GetComponent<AIActor>();
        HealthHaver hh = actor.gameObject.GetComponent<HealthHaver>();
        hh.SetHealthMaximum(health);
        hh.FullHeal();
        actor.SetupAnimations(baseFps: baseFps, extraAnims: extraAnims);
        //NOTE: should use LowerLeft anchor if we have a SpeculativeRigidBody and MiddleCenter otherwise
        if (autoRigidBody)
        {
          actor.gameObject.AutoRigidBody((List<CollisionLayer>)[CollisionLayer.EnemyCollider, CollisionLayer.EnemyHitBox]);
          actor.ActorShadowOffset = Vector2.zero;
        }
        else
          actor.ActorShadowOffset = new Vector3(actor.sprite.GetRelativePositionFromAnchor(Anchor.LowerLeft).x, 0f, 0f);
        if (doCorpse)
          actor.CorpseObject = EnemyDatabase.GetOrLoadByGuid("01972dee89fc4404a5c408d50007dad5").CorpseObject;
        if (FortunesFavorVFX == null)
          FortunesFavorVFX = ResourceManager.LoadAssetBundle("shared_auto_001").LoadAsset<GameObject>("FortuneFavor_VFX_Spark");

        if (!string.IsNullOrEmpty(shortDesc) && !string.IsNullOrEmpty(longDesc))
          actor.SetupAmmonomiconEntry(shortDesc: shortDesc, longDesc: longDesc, EnemyName: enemyName);
        Game.Enemies.Add($"{C.MOD_PREFIX}:{name}", actor);
        return actor;
    }

    private static void SetupAnimations(this AIActor actor, int baseFps, List<string> extraAnims = null)
    {
        string name = actor.gameObject.name;
        CompanionController cc = actor.gameObject.GetComponent<CompanionController>();

        tk2dSpriteCollectionData coll     = VFX.Collection;
        AIAnimator aiAnimator             = actor.gameObject.GetComponent<AIAnimator>();
        tk2dSpriteAnimator spriteAnimator = actor.gameObject.GetComponent<tk2dSpriteAnimator>();
        spriteAnimator.library            = actor.gameObject.AddComponent<tk2dSpriteAnimation>();
        aiAnimator.OtherAnimations        = new();

        List<tk2dSpriteAnimationClip> clips = new();
        aiAnimator.IdleAnimation   = coll.AutoAnimation(ref clips, $"{name}_idle",   fps: baseFps);
        aiAnimator.MoveAnimation   = coll.AutoAnimation(ref clips, $"{name}_move",   fps: baseFps);
        aiAnimator.FlightAnimation = coll.AutoAnimation(ref clips, $"{name}_flight", fps: baseFps);
        aiAnimator.HitAnimation    = coll.AutoAnimation(ref clips, $"{name}_hit",    fps: baseFps);
        aiAnimator.TalkAnimation   = coll.AutoAnimation(ref clips, $"{name}_talk",   fps: baseFps);
        if (coll.AutoAnimation(ref clips, $"{name}_pet", fps: baseFps) is DirectionalAnimation petAnim)
        {
            aiAnimator.OtherAnimations.Add(new(){name = "pet", anim = petAnim});
            if (cc)
              cc.CanBePet = true;
        }
        if (coll.AutoAnimation(ref clips, $"{name}_fidget", fps: baseFps) is DirectionalAnimation fidgetAnim)
            aiAnimator.IdleFidgetAnimations = [fidgetAnim];
        foreach (string anim in extraAnims.EmptyIfNull())
        {
            bool loop = (anim != "pitfall") && (anim != "death");
            aiAnimator.OtherAnimations.Add(new(){name = anim, anim = coll.AutoAnimation(ref clips, $"{name}_{anim}", fps: baseFps, loop: loop)});
        }
        spriteAnimator.library.clips = clips.ToArray();
    }

    // stolen from Bunny, thanks Bunny
    private static void SetupAmmonomiconEntry(this AIActor enemy, string shortDesc, string longDesc, string EnemyName)
    {
        string lowerName = EnemyName.ToID();
        if (enemy.GetComponent<EncounterTrackable>() != null)
            UnityEngine.Object.Destroy(enemy.GetComponent<EncounterTrackable>());
        enemy.encounterTrackable = enemy.gameObject.AddComponent<EncounterTrackable>();
        enemy.encounterTrackable.journalData = new JournalEntry();
        enemy.encounterTrackable.EncounterGuid = enemy.EnemyGuid;
        enemy.encounterTrackable.prerequisites = new DungeonPrerequisite[0];
        enemy.encounterTrackable.journalData.SuppressKnownState = false;
        enemy.encounterTrackable.journalData.IsEnemy = true;
        enemy.encounterTrackable.journalData.SuppressInAmmonomicon = false;
        string ammonomiconIconName = $"{C.MOD_PREFIX}_{lowerName}_icon";
        AtlasHelper.AddSpritesToCollection([ammonomiconIconName], AmmonomiconController.ForceInstance.EncounterIconCollection);
        enemy.encounterTrackable.journalData.AmmonomiconSprite = ammonomiconIconName;
        enemy.encounterTrackable.journalData.enemyPortraitSprite = ResourceExtractor.GetTextureFromResource($"{C.MOD_INT_NAME}/Resources/{lowerName}_portrait.png");
        enemy.encounterTrackable.ProxyEncounterGuid = string.Empty;
        ETGMod.Databases.Strings.Enemies.Set("#" + EnemyName.ToUpper(), EnemyName);
        ETGMod.Databases.Strings.Enemies.Set("#" + shortDesc.ToUpper(), shortDesc);
        ETGMod.Databases.Strings.Enemies.Set("#" + longDesc.ToUpper(), longDesc);
        enemy.encounterTrackable.journalData.PrimaryDisplayName = "#" + EnemyName.ToUpper();
        enemy.encounterTrackable.journalData.NotificationPanelDescription = "#" + shortDesc.ToUpper();
        enemy.encounterTrackable.journalData.AmmonomiconFullEntry = "#" + longDesc.ToUpper();
        enemy.encounterTrackable.journalData.SuppressKnownState = false;

        EnemyDatabaseEntry item = new EnemyDatabaseEntry
        {
            myGuid = enemy.EnemyGuid,
            placeableWidth = 2,
            placeableHeight = 2,
            isNormalEnemy = true,
            path = enemy.EnemyGuid,
            isInBossTab = false,
            encounterGuid = enemy.EnemyGuid
        };
        EnemyDatabase.Instance.Entries.Add(item);
        EncounterDatabaseEntry encounterDatabaseEntry = new EncounterDatabaseEntry(enemy.encounterTrackable)
        {
            path = enemy.EnemyGuid,
            myGuid = enemy.EnemyGuid
        };
        EncounterDatabase.Instance.Entries.Add(encounterDatabaseEntry);
    }

    /// <summary>Sets up the appropriate directional animation from the available sprites with the given prefix</summary>
    public static DirectionalAnimation AutoAnimation(this tk2dSpriteCollectionData coll, ref List<tk2dSpriteAnimationClip> clips, string name, int fps, bool loop = true)
    {
        DirectionalAnimation.DirectionType dType = Lazy.AutoDetectDirectionFromSpriteName(name);
        if (dType == None)
        {
            // Lazy.RuntimeWarn($"failed to get animations for {name}");
            return null;
        }

        DirectionalAnimation.SingleAnimation[] sa = DirectionalAnimation.m_combined[(int)dType];
        int nanims = sa.Length;
        string[] animNames = new string[nanims];
        for (int i = 0; i < nanims; ++i)
        {
            string aname = string.IsNullOrEmpty(sa[i].suffix) ? name : $"{name}_{sa[i].suffix}";
            tk2dSpriteAnimationClip clip = coll.AddAnimation(aname, fps: fps, loopStart: loop ? 0 : -1);
            if (clip == null)
            {
                Lazy.RuntimeWarn($"  FAILED TO ADD CLIP {aname}");
                return null;
            }
            // Lazy.DebugLog($"  added clip {aname}");
            animNames[i] = aname;
            clips.Add(clip);
        }

        // ETGModConsole.Log($"found {animNames.Length} frames for {dType.ToString()} animation {name}");
        return new(){
            Type      = dType,
            Prefix    = name,
            AnimNames = animNames,
            Flipped   = new DirectionalAnimation.FlipType[nanims],
        };
    }

    public static T MakeIntangible<T>(this T friend) where T : CwaffCompanionController
    {
        friend.CanCrossPits = true;
        friend.aiActor.healthHaver.PreventAllDamage = true;
        friend.aiActor.CollisionDamage = 0f;
        friend.aiActor.specRigidbody.CollideWithOthers = false;
        friend.aiActor.specRigidbody.CollideWithTileMap = false;
        friend.aiActor.HasShadow = false;
        return friend;
    }

    public static T SetPettingOffsets<T>(this T self, Vector2 right, Vector2? left = null) where T : CwaffCompanionController
    {
      self.SetPettingOffsetsInternal(right, left);
      return self;
    }
}
