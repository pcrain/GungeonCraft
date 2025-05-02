namespace CwaffingTheGungy;

public class PogoStick : CwaffActive
{
    public static string ItemName         = "Pogo Stick";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    public static GameObject PogoPrefab = null;

    private const string POGO_ATTACH_POINT = "Pogo Attach Point";

    private bool _active = false;
    private PlayerController _owner = null;
    private GameObject _attachedPogo = null;
    private tk2dSprite _attachedPogoSprite = null;
    private float _bounceTimer = 0.0f;

    public static void Init()
    {
        PlayerItem item  = Lazy.SetupActive<PogoStick>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.EXCLUDED;
        item.consumable = false;
        item.SetCooldownType(ItemBuilder.CooldownType.Timed, 2.0f);

        PogoPrefab = VFX.Create("pogo_stick_vfx", anchor: Anchor.LowerCenter);
    }

    public override void Pickup(PlayerController player)
    {
        this._owner = player;
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
        this._bounceTimer = 0.0f;
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
    }

    private void LateUpdate()
    {
        // base.Update();
        if (!this._owner || !this._attachedPogo || !this._attachedPogoSprite)
            return;

        UpdateSprite(this._owner.sprite);
        float newY = this._owner.sprite.transform.localPosition.y;
        bool movingDown = newY < this._lastY;
        if (movingDown != this._lastMovingDown)
        {
            this._lastMovingDown = movingDown;
            if (!movingDown)
                this._owner.gameObject.Play("rogo_dodge_sound");
        }
        this._lastY = newY;
    }

    private static readonly Vector2 _OFFSET = new Vector2(0, -8/16f);
    private void HandlePlayerSpriteChanged(tk2dBaseSprite newPlayerSprite)
    {
        UpdateSprite(newPlayerSprite, updateTimer: false);
    }

    private float _phase = 0.0f;
    private float _lastY = 0.0f;
    private bool _lastMovingDown = true;
    private void UpdateSprite(tk2dBaseSprite playerSprite, bool updateTimer = true)
    {
        // const float NORTH_DEPTH   =  0.7f;  // works for idle animations but not jetpack animation (probably because we're flying)
        // const float SOUTH_DEPTH   = -0.7f;  // works for idle animations but not jetpack animation (probably because we're flying)
        const float NORTH_DEPTH   =  1.5f;
        const float SOUTH_DEPTH   = -0.7f;
        const float BOUNCE_HEIGHT = 0.25f;
        const float BOUNCE_FREQ   = 7.0f;
        if (!this._owner || !this._attachedPogo)
            return;
        // System.Console.WriteLine($"pogo was at {this._attachedPogo.transform.position} (local: {this._attachedPogo.transform.localPosition})");
        if (updateTimer)
        {
            this._bounceTimer += BraveTime.DeltaTime;
            this._phase = Mathf.Abs(Mathf.Sin(BOUNCE_FREQ * this._bounceTimer));
        }
        float newY = BOUNCE_HEIGHT * this._phase;

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

