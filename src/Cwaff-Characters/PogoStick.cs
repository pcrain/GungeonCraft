namespace CwaffingTheGungy;

using System;
using static PogoDodgeRoll;

public class PogoStick : CwaffDodgeRollActiveItem
{
    public static string ItemName         = "Pogo Stick";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    public static GameObject PogoPrefab = null;

    private const string POGO_ATTACH_POINT = "Pogo Attach Point";

    internal PlayerController _owner = null;
    internal State _state {
        get {
            return this._dodgeRoller ? this._dodgeRoller._state : State.INACTIVE;
        }
        set {
            if (this._dodgeRoller)
                this._dodgeRoller._state = value;
        }
    }

    private bool _active = false;
    private GameObject _attachedPogo = null;
    private tk2dSprite _attachedPogoSprite = null;
    private float _bounceTimer = 0.0f;
    private PogoDodgeRoll _dodgeRoller = null;

    public static void Init()
    {
        PlayerItem item  = Lazy.SetupActive<PogoStick>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.EXCLUDED;
        item.consumable = false;
        item.SetCooldownType(ItemBuilder.CooldownType.Timed, 2.0f);
        item.gameObject.AddComponent<PogoDodgeRoll>();

        PogoPrefab = VFX.Create("pogo_stick_vfx", anchor: Anchor.LowerCenter);
    }

    public override CustomDodgeRoll CustomDodgeRoll()
    {
        if (!this._dodgeRoller)
        {
            this._dodgeRoller = this.gameObject.GetComponent<PogoDodgeRoll>();
            this._dodgeRoller.IsEnabled = false;
        }
        return this._dodgeRoller;
    }

    public override void Pickup(PlayerController player)
    {
        this._owner = player;
        if (!this._dodgeRoller)
            this._dodgeRoller = this.gameObject.GetComponent<PogoDodgeRoll>();
        this._dodgeRoller.IsEnabled = false;
        base.Pickup(player);
    }

    public override void OnPreDrop(PlayerController player)
    {
        Deactivate();
        this._owner = null;
        base.OnPreDrop(player);
    }

    public override void OnDestroy()
    {
        Deactivate();
        base.OnDestroy();
    }

    public override void DoEffect(PlayerController player)
    {
        base.DoEffect(player);

        this._owner = player;
        this._active = !this._active;
        if (!this._dodgeRoller)
        {
            this._dodgeRoller = this.gameObject.GetComponent<PogoDodgeRoll>();
            this._dodgeRoller.IsEnabled = false;
        }
        if (this._active)
            Activate(player);
        else
            Deactivate();
    }

    private void Activate(PlayerController player)
    {
        player.sprite.SpriteChanged += HandlePlayerSpriteChanged;
        this._attachedPogo = UnityEngine.Object.Instantiate(PogoPrefab, player.sprite.transform);
        HandlePlayerSpriteChanged(player.sprite);
        this._attachedPogoSprite = this._attachedPogo.GetComponent<tk2dSprite>();
        this._attachedPogoSprite.usesOverrideMaterial = true;
        this._attachedPogoSprite.renderer.material.shader = ShaderCache.Acquire("Brave/PlayerShader");
        SpriteOutlineManager.AddOutlineToSprite(this._attachedPogoSprite, Color.black);
        player.SetIsFlying(true, ItemName, adjustShadow: false);
        player.AdditionalCanDodgeRollWhileFlying.AddOverride(ItemName);
        this._bounceTimer = 0.0f;
        this._dodgeRoller.IsEnabled = true;
    }

    private void Deactivate()
    {
        if (this._owner is not PlayerController player)
            return;

        player.sprite.SpriteChanged -= HandlePlayerSpriteChanged;
        player.sprite.transform.localPosition = player.sprite.transform.localPosition.WithY(0f);
        if (this._attachedPogo)
        {
            this._attachedPogo.transform.parent = null;
            UnityEngine.Object.Destroy(this._attachedPogo);
        }
        this._attachedPogo = null;
        this._attachedPogoSprite = null;
        player.SetIsFlying(false, ItemName, adjustShadow: false);
        player.AdditionalCanDodgeRollWhileFlying.RemoveOverride(ItemName);
        this._dodgeRoller.IsEnabled = false;
    }

    #if DEBUG
        private dfLabel _debugLabel = new();
    #endif
    private void LateUpdate()
    {
        #if DEBUG
        if (this._owner)
        {
            if (!this._debugLabel)
                this._debugLabel = Sextant.MakeNewLabel();
            this._debugLabel.Color = Color.cyan;
            this._debugLabel.Text = $"{this._state}";
            Sextant.PlaceLabel(this._debugLabel, this._owner.sprite.WorldTopCenter + new Vector2(0f, 1f), 0f);
        }
        #endif
        // base.Update();
        if (!this._active || !this._owner || !this._attachedPogo || !this._attachedPogoSprite)
            return;

        UpdatePogo(this._owner.sprite);
        float newY = this._owner.sprite.transform.localPosition.y;
        bool movingDown = newY < this._lastY;
        this._lastY = newY;
        if (movingDown == this._lastMovingDown)
            return;

        this._lastMovingDown = movingDown;
        if (movingDown)
            return;

        this._owner.gameObject.Play("rogo_dodge_sound");
        if (this._state == State.WAITING)
        {
            this._state = State.CHARGING;
            this._bounceTimer = 0.0f;
            this._phase = 0.0f;
        }
    }

    private static readonly Vector2 _OFFSET = new Vector2(0, -8/16f);
    private void HandlePlayerSpriteChanged(tk2dBaseSprite newPlayerSprite)
    {
        UpdatePogo(newPlayerSprite, updateTimer: false);
    }

    private float _phase = 0.0f;
    private float _lastY = 0.0f;
    private bool _lastMovingDown = true;
    private void UpdatePogo(tk2dBaseSprite playerSprite, bool updateTimer = true)
    {
        // const float NORTH_DEPTH   =  0.7f;  // works for idle animations but not jetpack animation (probably because we're flying)
        // const float SOUTH_DEPTH   = -0.7f;  // works for idle animations but not jetpack animation (probably because we're flying)
        const float NORTH_DEPTH   =  1.5f;
        // const float SOUTH_DEPTH   = -0.7f;
        const float SOUTH_DEPTH   = -0.3f;
        const float BOUNCE_HEIGHT = 0.25f;
        const float BOUNCE_FREQ   = 7.0f;
        if (!this._owner || !this._attachedPogo)
            return;
        // System.Console.WriteLine($"pogo was at {this._attachedPogo.transform.position} (local: {this._attachedPogo.transform.localPosition})");
        if (updateTimer && this._state < State.CHARGING)
        {
            this._bounceTimer += BraveTime.DeltaTime;
            this._phase = Mathf.Abs(Mathf.Sin(BOUNCE_FREQ * this._bounceTimer));
        }
        float newY = BOUNCE_HEIGHT * this._phase;

        if (this._state != State.BOUNCING)
            playerSprite.transform.localPosition = playerSprite.transform.localPosition.WithY(newY);
        bool facingSouth = (this._owner.m_currentGunAngle > 155f || this._owner.m_currentGunAngle < 25f);
        Vector2 basePos = playerSprite.WorldBottomCenter.Quantize(0.0625f, VectorConversions.Floor);
        string playerSpriteName = playerSprite.CurrentSprite.name;
        if (playerSprite.FlipX && !playerSpriteName.Contains("front") && !playerSpriteName.Contains("back"))
            basePos += new Vector2(-1/16f, 0f); //HACK: one pixel off when facing left
        this._attachedPogo.transform.position = (basePos + _OFFSET).ToVector3ZisY(facingSouth ? SOUTH_DEPTH : NORTH_DEPTH);
        // System.Console.WriteLine($"  now at {this._attachedPogo.transform.position} (local: {this._attachedPogo.transform.localPosition}) (scale: {newPlayerSprite.scale})");
    }

    // [HarmonyPatch]
    // private class PogoAnimationPatch
    // {
    //     [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.GetBaseAnimationName))]
    //     static void Postfix(PlayerController __instance, Vector2 v, float gunAngle, bool invertThresholds, bool forceTwoHands, ref string __result)
    //     {
    //         // if (__instance.GetComponent<Caffeination>() is not Caffeination caff || caff._state != Caffeination.State.CAFFEINATED)
    //         //     return true;

    //         // __result = GetCaffeinatedAnimationName(__instance, v, gunAngle, invertThresholds, forceTwoHands);
    //         // return false;  // skip the original check
    //     }
    // }

    /// <summary>Make gun line up with character while using Pogo Stick</summary>
    [HarmonyPatch]
    private static class PlayerControllerHandleGunAttachPointInternalPatch
    {
        [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.HandleGunAttachPointInternal))]
        static void Postfix(PlayerController __instance, Gun targetGun, bool isSecondary)
        {
            Transform t = __instance.gunAttachPoint;
            t.localPosition = t.localPosition.WithY(t.localPosition.y + __instance.sprite.transform.localPosition.y);
        }
    }

    /// <summary>Slow down while we're bouncing</summary>
    [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.AdjustInputVector))]
    private class PlayerControllerAdjustInputVectorPatch
    {
        static void Postfix(PlayerController __instance, Vector2 rawInput, float cardinalMagnetAngle, float ordinalMagnetAngle, ref Vector2 __result)
        {
            if (__instance.GetActive<PogoStick>() is not PogoStick pogo)
              return;
          if (pogo._active && pogo._lastMovingDown)
              __result *= pogo._phase;
        }
    }
}

public class PogoDodgeRoll : CustomDodgeRoll
{
    internal enum State
    {
        INACTIVE, // dodge roll is not currently being attempted
        WAITING,  // waiting for pogo bounce to finish
        CHARGING, // building power for jump
        BOUNCING, // airborne after releasing charge (invulnerable and uninterruptible)
        LANDING,  // landing on the ground after falling down
    }

    // public override float bufferWindow       => 0.5f;
    public override bool  dodgesProjectiles => false; // we have our own projectile collision handling
    public override Priority priority       => Priority.Exclusive; // starting item, needs exclusive priority while active
    public override bool  canDodgeInPlace   => true;
    public override bool  lockedDirection   => false; // we're allowed to aim, just not to move

    internal State _state = State.INACTIVE;

    private PogoStick _pogo = null;
    private PlayerController _pogoOwner = null;
    private Geometry _chargeRadius = null;
    private Geometry _chargeTarget = null;
    private StatModifier _noSpeed = StatType.MovementSpeed.Mult(0f);

    private void EnsurePogo()
    {
        if (this._pogo && this._pogoOwner)
            return;
        this._pogo = base.gameObject.GetComponent<PogoStick>();
        if (this._pogo)
            this._pogoOwner = this._pogo._owner;
    }

    private static float GetHeight(float startingHeight, float velocity, float gravity, float time)
    {
        return startingHeight + velocity * time - 0.5f * gravity * time * time;
    }

    private static float GetHeight(float velocity, float gravity, float time)
    {
        return GetHeight(0f, velocity, gravity, time);
    }

    protected override IEnumerator ContinueDodgeRoll()
    {
      #region Initialization
        EnsurePogo();
        if (!this._pogo || !this._pogoOwner)
        {
            Lazy.DebugWarn($"no pogo D:");
            // ForceVanillaDodgeRoll(direction);
            yield break;
        }
        if (this._state != State.INACTIVE)
        {
            Lazy.DebugWarn("pogo roll already active");
            yield break;
        }
        if (!this._chargeRadius)
            this._chargeRadius = new GameObject().AddComponent<Geometry>();
        if (!this._chargeTarget)
            this._chargeTarget = new GameObject().AddComponent<Geometry>();
      #endregion

      #region Wait for pogo bounce to finish
        this._state = State.WAITING;
        while (this._state == State.WAITING)
            yield return null;
      #endregion

      #region Begin charge phase
        const float MAX_BOUNCE_RADIUS = 8f;
        const float MIN_BOUNCE_RADIUS = 1f;
        const float MAX_CHARGE_TIME = 1f;

        this._owner.lockedDodgeRollDirection = Vector2.zero; // avoids some animation glitches
        this._owner.OnReceivedDamage += this.OnReceivedDamage;
        this._owner.ownerlessStatModifiers.AddUnique(this._noSpeed);
        this._owner.stats.RecalculateStats(this._owner);
        float radius = 0f;
        Vector2 target = this._owner.CenterPosition;
        for (float elapsed = 0f; this._dodgeButtonHeld; elapsed += BraveTime.DeltaTime)
        {
            // System.Console.WriteLine($"  charged for {elapsed} seconds");
            radius = MAX_BOUNCE_RADIUS * Mathf.Clamp01(elapsed / MAX_CHARGE_TIME);
            target = this._owner.CenterPosition +  radius * (this._owner.unadjustedAimPoint.XY() - this._owner.sprite.WorldCenter).normalized;
            this._chargeRadius.Setup(Geometry.Shape.FILLEDCIRCLE, Color.blue.WithAlpha(0.1f), pos: this._owner.CenterPosition, radius: radius);
            this._chargeTarget.Setup(Geometry.Shape.FILLEDCIRCLE, Color.red.WithAlpha(0.2f), pos: target, radius: 1f);
            yield return null;
        }

        if (radius < MIN_BOUNCE_RADIUS)
            yield break;
      #endregion

      #region Begin bouncing phase
        const float GRAVITY = 100f;
        const float VELOCITY = 25f;
        const float BOUNCE_TIME = (2f * VELOCITY) / GRAVITY;
        this._state = State.BOUNCING;
        this._owner.specRigidbody.CollideWithOthers = false;
        Vector2 startingPos = this._owner.transform.position;
        target -= Vector3.Scale(this._owner.sprite.GetRelativePositionFromAnchor(Anchor.LowerCenter), this._owner.sprite.scale).XY();
        Vector2 velocity = (target - startingPos) / BOUNCE_TIME;
        KnockbackDoer kb = this._owner.knockbackDoer;
        int pogoBounceKnockback = kb.ApplyContinuousKnockback(velocity.normalized, velocity.magnitude * 0.1f * kb.weight);
        Transform spriteTransform = this._owner.sprite.transform;
        base.gameObject.Play("rogo_charge_bounce_sound");
        for (float elapsed = 0f; elapsed < BOUNCE_TIME; elapsed += BraveTime.DeltaTime)
        {
            float height = GetHeight(VELOCITY, GRAVITY, elapsed);
            float percentDone = elapsed / BOUNCE_TIME;
            spriteTransform.localPosition = spriteTransform.localPosition.WithY(height);
            yield return null;
        }
        kb.EndContinuousKnockback(pogoBounceKnockback);
      #endregion

        // System.Console.WriteLine($"end of continue dodge roll");
        yield break;
    }

    private void OnReceivedDamage(PlayerController controller)
    {
        this.FinishDodgeRoll();
    }

    protected override void FinishDodgeRoll(bool aborted = false)
    {
        // System.Console.WriteLine($"finished dodge roll");
        Transform spriteTransform = this._owner.sprite.transform;
        spriteTransform.localPosition = spriteTransform.localPosition.WithY(0f);
        this._owner.OnReceivedDamage -= this.OnReceivedDamage;
        this._owner.specRigidbody.CollideWithOthers = true;
        this._owner.ownerlessStatModifiers.TryRemove(this._noSpeed);
        this._owner.stats.RecalculateStats(this._owner);
        this._chargeRadius._meshRenderer.enabled = false;
        this._chargeTarget._meshRenderer.enabled = false;
        this._state = State.INACTIVE;
    }

    private void OnDestroy()
    {
        if (this._chargeRadius)
            UnityEngine.Object.Destroy(this._chargeRadius.gameObject);
    }
}
