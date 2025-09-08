namespace CwaffingTheGungy;

/// <summary>Subclass of ShootBehavior that allows the boss to reposition before attacking</summary>
public class MoveAndShootBehavior : ShootBehavior
{
  //NOTE: duplicate of base game method besides switching state immediately to CwaffShootBehaviorState.Relocating
  public override BehaviorResult Update()
  {
    if (!IsReady())
      return BehaviorResult.Continue;
    if (RequiresTarget && m_behaviorSpeculator.TargetRigidbody == null)
      return BehaviorResult.Continue;
    if (UseVfx && !string.IsNullOrEmpty(Vfx))
      m_aiAnimator.PlayVfx(Vfx);
    if (!m_gameObject.activeSelf)
    {
      m_gameObject.SetActive(true);
      m_beganInactive = true;
    }
    if ((bool)m_behaviorSpeculator.TargetRigidbody)
      m_cachedTargetCenter = m_behaviorSpeculator.TargetRigidbody.GetUnitCenter(ColliderType.HitBox);
    if (ClearGoop)
      SetGoopClearing(true);
    state = CwaffShootBehaviorState.Relocating; // TODO: refactor later to patch original method
    PrepareToRelocate();
    if (MoveSpeedModifier != 1f)
    {
      m_cachedMovementSpeed = m_aiActor.MovementSpeed;
      m_aiActor.MovementSpeed *= MoveSpeedModifier;
    }
    if (LockFacingDirection)
    {
      m_aiAnimator.FacingDirection = (m_behaviorSpeculator.TargetRigidbody.GetUnitCenter(ColliderType.HitBox) - m_specRigidbody.GetUnitCenter(ColliderType.HitBox)).ToAngle();
      m_aiAnimator.LockFacingDirection = true;
    }
    if (PreventTargetSwitching && (bool)m_aiActor)
      m_aiActor.SuppressTargetSwitch = true;
    m_updateEveryFrame = true;
    if (OverrideBaseAnims && (bool)m_aiAnimator)
    {
      if (!string.IsNullOrEmpty(OverrideIdleAnim))
        m_aiAnimator.OverrideIdleAnimation = OverrideIdleAnim;
      if (!string.IsNullOrEmpty(OverrideMoveAnim))
        m_aiAnimator.OverrideMoveAnimation = OverrideMoveAnim;
    }
    if (StopDuring == StopType.None || StopDuring == StopType.TellOnly)
      return BehaviorResult.RunContinuousInClass;
    return BehaviorResult.RunContinuous;
  }

  public override ContinuousBehaviorResult ContinuousUpdate()
  {
    if (this.state != CwaffShootBehaviorState.Relocating)
      return base.ContinuousUpdate();
    if (m_behaviorSpeculator.TargetRigidbody)
      m_cachedTargetCenter = m_behaviorSpeculator.TargetRigidbody.GetUnitCenter(ColliderType.HitBox);
    if (!Relocate())
      return ContinuousBehaviorResult.Continue;

    if (!string.IsNullOrEmpty(ChargeAnimation))
    {
      m_aiAnimator.PlayUntilFinished(ChargeAnimation, true);
      state = State.WaitingForCharge;
    }
    else if (!string.IsNullOrEmpty(TellAnimation))
    {
      if (!string.IsNullOrEmpty(TellAnimation))
        m_aiAnimator.PlayUntilCancelled(TellAnimation, true);
      else
        m_aiAnimator.PlayUntilFinished(TellAnimation, true);
      state = State.WaitingForTell;
      if (HideGun && (bool)m_aiShooter)
        m_aiShooter.ToggleGunAndHandRenderers(false, "ShootBulletScript");
    }
    else
      Fire();

    return ContinuousBehaviorResult.Continue;
  }

  /// <summary>Performs setup for calling Relocate() in future frames.</summary>
  protected internal virtual void PrepareToRelocate(Vector2? overridePos = null) {}

  /// <summary>Returns true if we're in position to attack, false otherwise.</summary>
  protected internal virtual bool Relocate() => true;
}
