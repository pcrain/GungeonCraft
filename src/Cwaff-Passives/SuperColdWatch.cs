namespace CwaffingTheGungy;

public class SuperColdWatch : CwaffPassive
{
    public static string ItemName         = "Super Cold Watch";
    public static string ShortDescription = "Time Moves As You...Don't?";
    public static string LongDescription  = "All other objects gradually slow down as the player moves, down to a minimum of 1/8 their normal speed. Standing still or dodge rolling will reset time to its normal speed.";
    public static string Lore             = "A Super Hot Watch that was left to cool off in the Hollow for far too long. You feel an otherworldly coldness and stillness in the air around you as you hold it, comparable to being permanently stuck in the moment of complete silence after telling a poorly received joke at a funeral.";

    private const float _BUILDUP_TIME = 5.0f;
    private const float _MAX_TIMESCALE_REDUCTION = 0.875f;

    private bool _active = false;
    private float _activeTime = 0.0f;
    private float _effectStrength = 0.0f;

    private StatModifier[] _statModifiers = null;

    public static void Init()
    {
        PassiveItem item  = Lazy.SetupPassive<SuperColdWatch>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.S;
    }

    private void UpdateStats()
    {
      this._effectStrength = _MAX_TIMESCALE_REDUCTION * Ease.OutQuad(Mathf.Clamp01(this._activeTime / _BUILDUP_TIME));
      this._statModifiers ??= new[] {
        StatType.RateOfFire.Mult(1f),
        StatType.MovementSpeed.Mult(1f),
        StatType.ReloadSpeed.Mult(1f),
      };
      float statStrength = 1f - this._effectStrength;
      float invertedStatStrength = 1f / statStrength;
      this._statModifiers[0].amount = invertedStatStrength;
      this._statModifiers[1].amount = invertedStatStrength;
      this._statModifiers[2].amount = statStrength;
      this.passiveStatModifiers = this._statModifiers;
      if (this.m_owner)
      {
        this.m_owner.stats.RecalculateStatsWithoutRebuildingGunVolleys(this.m_owner);
        if (this.m_owner.spriteAnimator is tk2dSpriteAnimator animator)
          animator.OverrideTimeScale = invertedStatStrength;
      }
    }

    public override void Update()
    {
      base.Update();
      float now = Time.realtimeSinceStartup;
      bool shouldBeActive = m_pickedUp && !GameManager.Instance.IsLoadingLevel && m_owner
        && (m_owner.CurrentInputState == PlayerInputState.AllInput || m_owner.CurrentInputState == PlayerInputState.OnlyMovement)
        && !m_owner.IsFalling && !m_owner.IsDodgeRolling && m_owner.healthHaver && !m_owner.healthHaver.IsDead
        && m_owner.specRigidbody && m_owner.specRigidbody.Velocity.sqrMagnitude > 0.01f;
      if (!shouldBeActive)
      {
        if (!this._active)
          return;

        this._activeTime = 0.0f;
        this._effectStrength = 0.0f;
        BraveTime.ClearMultiplier(base.gameObject);
        UpdateStats();
        this._active = false;
        return;
      }

      this._activeTime += Time.deltaTime; // NOTE: don't use BraveTime here, we want the actual delta time
      UpdateStats();
      BraveTime.SetTimeScaleMultiplier(1f - this._effectStrength, base.gameObject);
      this._active = true;
    }

    public override void DisableEffect(PlayerController player)
    {
        base.DisableEffect(player);
        if (player && player.spriteAnimator is tk2dSpriteAnimator animator)
          animator.OverrideTimeScale = 1.0f;
    }
}
