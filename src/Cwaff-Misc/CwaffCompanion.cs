namespace CwaffingTheGungy;

using static DirectionalAnimation.DirectionType;

public abstract class CwaffCompanionController : CompanionController
{
}

public class CwaffCompanionMovementBehaviorBase : MovementBehaviorBase
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

  private int m_sequentialPathFails;

  public override void Start()
  {
    base.Start();
    m_companionController = m_gameObject.GetComponent<CompanionController>();
  }

  /// <summary>Return to to prevent the companion from warping.</summary>
  protected virtual bool PreventWarping() => false;
  /// <summary>Returns true iff the companion has reached its designated target</summary>
  protected virtual bool ReachedTarget() => false;
  /// <summary>Called every update as long as ReachedTarget() is true</summary>
  protected virtual void OnReachedTarget() {}
  /// <summary>Returns true iff the current target is valid</summary>
  protected virtual bool IsTargetValid() => false;
  /// <summary>Updates _targetPos and potentially _targetActor</summary>
  protected virtual void DetermineNewTarget() {}
  /// <summary>Called at the beginning of every normal Update cycle</summary>
  protected virtual void UpdateStateAndTargetPosition() {}
  /// <summary>Called immediately before warping.</summary>
  protected virtual void OnPreWarp() {}
  /// <summary>Called immediately after warping.</summary>
  protected virtual void OnWarp() {}
  /// <summary>Attempts to path over to _targetPos, calling DetermineNewTarget() as necessary on failure.</summary>
  protected virtual void RepathToTarget()  {
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
  /// <summary>Returns true iff the companion is offscreen and/or in a different room.</summary>
  private bool CheckOffscreenForWarpingPurposes()
  {
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
      if (!GameManager.HasInstance || GameManager.Instance.IsLoadingLevel || !m_companionController || !m_aiActor.CompanionOwner)
          return BehaviorResult.SkipAllRemainingBehaviors;

      if (GameManager.Instance.CurrentLevelOverrideState == GameManager.LevelOverrideState.END_TIMES)
      {
          m_aiActor.ClearPath();
          return BehaviorResult.SkipAllRemainingBehaviors;
      }

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

public static class CwaffCompanionBuilder
{
    public static Friend InitCompanion<Friend>(this PassiveItem item, string friendName = null, int baseFps = 4, List<string> extraAnims = null)
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
        friend.SetupAnimations(baseFps: baseFps, extraAnims: extraAnims);
        //NOTE: should use LowerLeft anchor if we have a SpeculativeRigidBody and MiddleCenter otherwise, but we can't know ahead of time
        friend.aiActor.ActorShadowOffset = new Vector3(friend.aiActor.sprite.GetRelativePositionFromAnchor(Anchor.LowerLeft).x, 0f, 0f);
        friend.companionID = CompanionController.CompanionIdentifier.NONE;

        cc.CompanionGuid = friend.aiActor.EnemyGuid;
        return friend;
    }

    private static void SetupAnimations(this CwaffCompanionController friend, int baseFps, List<string> extraAnims = null)
    {
        string name = friend.gameObject.name;

        tk2dSpriteCollectionData coll     = VFX.Collection;
        AIAnimator aiAnimator             = friend.gameObject.GetComponent<AIAnimator>();
        tk2dSpriteAnimator spriteAnimator = friend.gameObject.GetComponent<tk2dSpriteAnimator>();
        spriteAnimator.library            = friend.gameObject.AddComponent<tk2dSpriteAnimation>();
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
            friend.CanBePet = true; //TODO: fix m_petOffset after calling DoPet() with patch
        }
        if (coll.AutoAnimation(ref clips, $"{name}_fidget", fps: baseFps) is DirectionalAnimation fidgetAnim)
            aiAnimator.IdleFidgetAnimations = [fidgetAnim];
        foreach (string anim in extraAnims.EmptyIfNull())
        {
            bool loop = (anim != "pitfall");
            aiAnimator.OtherAnimations.Add(new(){name = anim, anim = coll.AutoAnimation(ref clips, $"{name}_{anim}", fps: baseFps, loop: loop)});
        }
        spriteAnimator.library.clips = clips.ToArray();
    }

    /// <summary>Sets up the appropriate directional animation from the available sprites with the given prefix</summary>
    private static DirectionalAnimation AutoAnimation(this tk2dSpriteCollectionData coll, ref List<tk2dSpriteAnimationClip> clips, string name, int fps, bool loop = true)
    {
        DirectionalAnimation.DirectionType dType = AutoDetectDirectionFromSpriteName(name);
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

    private static DirectionalAnimation.DirectionType AutoDetectDirectionFromSpriteName(string name)
    {
        if (ResMap.Has($"{name}_north_northeast"))
            return SixteenWay;
        if (ResMap.Has($"{name}_northeast"))
            return EightWayOrdinal;
        if (ResMap.Has($"{name}_north"))
            return FourWayCardinal;
        if (ResMap.Has($"{name}_front_right"))
        {
            if (ResMap.Has($"{name}_right"))
                return EightWay;
            if (ResMap.Has($"{name}_front"))
                return SixWay;
            return FourWay;
        }
        if (ResMap.Has($"{name}_right"))
            return TwoWayHorizontal;
        if (ResMap.Has($"{name}_front"))
            return TwoWayVertical;
        if (ResMap.Has(name))
            return Single;
        return None;
    }

    public static void MakeIntangible(this CwaffCompanionController friend)
    {
        friend.CanCrossPits = true;
        friend.aiActor.healthHaver.PreventAllDamage = true;
        friend.aiActor.CollisionDamage = 0f;
        friend.aiActor.specRigidbody.CollideWithOthers = false;
        friend.aiActor.specRigidbody.CollideWithTileMap = false;
        friend.aiActor.HasShadow = false;
    }
}
