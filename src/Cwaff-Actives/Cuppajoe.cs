namespace CwaffingTheGungy;

public class Cuppajoe : PlayerItem
{
    public static string ItemName         = "Cuppajoe";
    public static string ShortDescription = "Not A Morning Person";
    public static string LongDescription  = "Dramatically increases rate of fire, reload speed, movement speed, and dodge roll speed for 12 seconds, but dramatically decreases these stats for 8 seconds afterwards.";
    public static string Lore             = "Coffee is something of a miracle beverage, letting you move faster, react quicker, focus harder, aim better, think better, learn better, practice more effectively, earn more money, heal all your illnesses, find true love, cure cancer, achieve world peace, end world hunger, open your third eye, see the future, reach nirvana, rule the galaxy, observe the multiverse...and it tastes good. Coffee's great isn't it!? Have another cup!!";

    private Caffeination _caffeine = null;
    private PlayerController _owner = null;

    public static void Init()
    {
        PlayerItem item = Lazy.SetupActive<Cuppajoe>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.D;
        item.consumable   = false;
        item.CanBeDropped = true;
        item.SetCooldownType(ItemBuilder.CooldownType.Timed, Caffeination._CRASH_TIME);
    }

    public override void Pickup(PlayerController player)
    {
        this._owner = player;
        this._caffeine = player.gameObject.GetOrAddComponent<Caffeination>();
        base.Pickup(player);
    }

    public override void OnPreDrop(PlayerController player)
    {
        this._owner = null;
        this._caffeine = null;
        base.OnPreDrop(player);
    }

    public override bool CanBeUsed(PlayerController user)
    {
        return !this._caffeine || (this._caffeine._state == Caffeination.State.NEUTRAL);
    }

    public override void DoEffect(PlayerController user)
    {
        this._caffeine.AnotherCup();
        this.m_activeDuration  = Caffeination._BOOST_TIME;
        this.m_activeElapsed   = 0f;
        this.IsCurrentlyActive = true;
    }

    public override void Update()
    {
        base.Update();
        if (!this._owner)
            return;

        if (this.IsCurrentlyActive && (this.m_activeElapsed >= this.m_activeDuration))
        {
            this.IsCurrentlyActive = false;
            this.CurrentTimeCooldown = Caffeination._CRASH_TIME;
        }
    }
}

internal class Caffeination : MonoBehaviour
{
    internal enum State
    {
        NEUTRAL,
        CAFFEINATED,
        CRASHED,
    }

    internal const float _BOOST_TIME = 12f;
    internal const float _CRASH_TIME = 8f;

    private PlayerController _owner         = null;
    private StatModifier[]   _caffeineBuffs = null;
    private StatModifier[]   _crashNerfs    = null;

    internal State _state = State.NEUTRAL;

    private void Start()
    {
        this._owner = base.GetComponent<PlayerController>();
        this._caffeineBuffs = new[] {
            new StatModifier(){
                amount      = 1.50f,
                modifyType  = StatModifier.ModifyMethod.MULTIPLICATIVE,
                statToBoost = PlayerStats.StatType.RateOfFire,
            },
            new StatModifier(){
                amount      = 1.50f,
                modifyType  = StatModifier.ModifyMethod.MULTIPLICATIVE,
                statToBoost = PlayerStats.StatType.DodgeRollSpeedMultiplier,
            },
            new StatModifier(){
                amount      = 1.50f,
                modifyType  = StatModifier.ModifyMethod.MULTIPLICATIVE,
                statToBoost = PlayerStats.StatType.MovementSpeed,
            },
            new StatModifier(){
                amount      = 0.75f,
                modifyType  = StatModifier.ModifyMethod.MULTIPLICATIVE,
                statToBoost = PlayerStats.StatType.ReloadSpeed,
            },
        };
        this._crashNerfs = new[] {
            new StatModifier(){
                amount      = 0.65f,
                modifyType  = StatModifier.ModifyMethod.MULTIPLICATIVE,
                statToBoost = PlayerStats.StatType.RateOfFire,
            },
            new StatModifier(){
                amount      = 0.75f,
                modifyType  = StatModifier.ModifyMethod.MULTIPLICATIVE,
                statToBoost = PlayerStats.StatType.DodgeRollSpeedMultiplier,
            },
            new StatModifier(){
                amount      = 0.65f,
                modifyType  = StatModifier.ModifyMethod.MULTIPLICATIVE,
                statToBoost = PlayerStats.StatType.MovementSpeed,
            },
            new StatModifier(){
                amount      = 1.5f,
                modifyType  = StatModifier.ModifyMethod.MULTIPLICATIVE,
                statToBoost = PlayerStats.StatType.ReloadSpeed,
            },
        };
    }

    private void Update()
    {
        float animSpeed = 1.0f;
        if (this._state == Caffeination.State.CAFFEINATED)
            animSpeed = (this._owner.specRigidbody.Velocity.sqrMagnitude > 1f) ? 4f : 2.5f;
        else if (this._state == Caffeination.State.CRASHED)
            animSpeed = 0.5f;

        this._owner.spriteAnimator.ClipFps = this._owner.spriteAnimator.CurrentClip.fps * animSpeed;
    }

    public void AnotherCup()
    {
        if (this._state == State.NEUTRAL)
            this._owner.StartCoroutine(AnotherCup_CR());
    }

    private IEnumerator AnotherCup_CR()
    {
        this._owner.gameObject.Play("coffee_drink_sound");
        this._state = State.CAFFEINATED;
        foreach (StatModifier stat in this._caffeineBuffs)
            this._owner.ownerlessStatModifiers.Add(stat);
        this._owner.stats.RecalculateStats(this._owner);
        yield return new WaitForSeconds(_BOOST_TIME);

        this._state = State.CRASHED;
        foreach (StatModifier stat in this._caffeineBuffs)
            this._owner.ownerlessStatModifiers.Remove(stat);
        foreach (StatModifier stat in this._crashNerfs)
            this._owner.ownerlessStatModifiers.Add(stat);
        this._owner.stats.RecalculateStats(this._owner);
        yield return new WaitForSeconds(_CRASH_TIME);

        this._state = State.NEUTRAL;
        foreach (StatModifier stat in this._crashNerfs)
            this._owner.ownerlessStatModifiers.Remove(stat);
        this._owner.stats.RecalculateStats(this._owner);
    }


    [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.GetBaseAnimationName))]
    private class CuppajoeAnimationPatch
    {
        static bool Prefix(PlayerController __instance, Vector2 v, float gunAngle, bool invertThresholds, bool forceTwoHands, ref string __result)
        {
            if (__instance.GetComponent<Caffeination>()?._state != Caffeination.State.CAFFEINATED)
                return true;

            __result = GetCaffeinatedAnimationName(__instance, v, gunAngle, invertThresholds, forceTwoHands);
            return false;  // skip the original check
        }
    }

    // Base game's GetBaseAnimationName() function, with all idle animations replace with running animations
    private static string GetCaffeinatedAnimationName(PlayerController pc, Vector2 v, float gunAngle, bool invertThresholds = false, bool forceTwoHands = false)
    {
      string empty = string.Empty;
      bool hasGun = pc.CurrentGun != null;
      if (hasGun && pc.CurrentGun.Handedness == GunHandedness.NoHanded)
        forceTwoHands = true;
      if (GameManager.Instance.CurrentLevelOverrideState == GameManager.LevelOverrideState.END_TIMES)
        hasGun = false;
      float num = 155f;
      float num2 = 25f;
      if (invertThresholds)
      {
        num = -155f;
        num2 -= 50f;
      }
      float num3 = 120f;
      float num4 = 60f;
      float num5 = -60f;
      float num6 = -120f;
      bool facingUp = gunAngle <= num && gunAngle >= num2;
      if (invertThresholds)
        facingUp = gunAngle <= num || gunAngle >= num2;
      if (pc.IsGhost)
      {
        if (facingUp)
        {
          if (gunAngle < num3 && gunAngle >= num4)
          {
            empty = "ghost_idle_back";
          }
          else
          {
            float num7 = 105f;
            empty = ((!(Mathf.Abs(gunAngle) > num7)) ? "ghost_idle_back_right" : "ghost_idle_back_left");
          }
        }
        else if (gunAngle <= num5 && gunAngle >= num6)
        {
          empty = "ghost_idle_front";
        }
        else
        {
          float num8 = 105f;
          empty = ((!(Mathf.Abs(gunAngle) > num8)) ? "ghost_idle_right" : "ghost_idle_left");
        }
      }
      else if (pc.IsFlying)
      {
        empty = (facingUp ? ((!(gunAngle < num3) || !(gunAngle >= num4)) ? "jetpack_right_bw" : "jetpack_up") : ((!(gunAngle <= num5) || !(gunAngle >= num6)) ? ((!pc.RenderBodyHand) ? "jetpack_right" : "jetpack_right_hand") : ((!pc.RenderBodyHand) ? "jetpack_down" : "jetpack_down_hand")));
      }
      else if (v == Vector2.zero || pc.IsStationary)
      {
        if (pc.IsPetting)
        {
          empty = "pet";
        }
        else if (facingUp)
        {
          if (gunAngle < num3 && gunAngle >= num4)
          {
            string text = (((forceTwoHands || !hasGun) && !pc.ForceHandless) ? "run_right_twohands" : ((!pc.RenderBodyHand) ? "run_right" : "run_right_hand"));
            empty = text;
          }
          else
          {
            string text2 = (((!forceTwoHands && hasGun) || pc.ForceHandless) ? "run_right" : "run_right_twohands");
            empty = text2;
          }
        }
        else if (gunAngle <= num5 && gunAngle >= num6)
        {
          string text3 = (((forceTwoHands || !hasGun) && !pc.ForceHandless) ? "run_down_twohands" : ((!pc.RenderBodyHand) ? "run_down" : "run_down_hand"));
          empty = text3;
        }
        else
        {
          string text4 = (((forceTwoHands || !hasGun) && !pc.ForceHandless) ? "run_right_twohands" : ((!pc.RenderBodyHand) ? "run_right" : "run_right_hand"));
          empty = text4;
        }
      }
      else if (facingUp)
      {
        string text5 = (((!forceTwoHands && hasGun) || pc.ForceHandless) ? "run_right_bw" : "run_right_bw_twohands");
        if (gunAngle < num3 && gunAngle >= num4)
        {
          text5 = (((forceTwoHands || !hasGun) && !pc.ForceHandless) ? "run_up_twohands" : ((!pc.RenderBodyHand) ? "run_up" : "run_up_hand"));
        }
        empty = text5;
      }
      else
      {
        string text6 = "run_right";
        if (gunAngle <= num5 && gunAngle >= num6)
        {
          text6 = "run_down";
        }
        if ((forceTwoHands || !hasGun) && !pc.ForceHandless)
        {
          text6 += "_twohands";
        }
        else if (pc.RenderBodyHand)
        {
          text6 += "_hand";
        }
        empty = text6;
      }
      if (pc.UseArmorlessAnim && !pc.IsGhost)
      {
        empty += "_armorless";
      }
      return empty;
    }
}
