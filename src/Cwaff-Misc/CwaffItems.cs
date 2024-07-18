namespace CwaffingTheGungy;

public interface ICwaffItem
{
  public void OnFirstPickup(PlayerController player);
}

public abstract class CwaffPassive : PassiveItem, ICwaffItem
{
  public override void Pickup(PlayerController player)
  {
    if (!this.m_pickedUpThisRun) // must come before base.Pickup()
      OnFirstPickup(player);
    base.Pickup(player);
  }

  public virtual void OnFirstPickup(PlayerController player)
  {

  }
}

public abstract class CwaffActive: PlayerItem, ICwaffItem
{
  public override void Pickup(PlayerController player)
  {
    if (!this.m_pickedUpThisRun) // must come before base.Pickup()
      OnFirstPickup(player);
    base.Pickup(player);
  }

  public virtual void OnFirstPickup(PlayerController player)
  {

  }
}

public abstract class CwaffGun: GunBehaviour, ICwaffItem/*, ILevelLoadedListener*/
{
  private const string _DEFAULT_BARREL_OFFSET = "__default";
  private static Dictionary<string, Dictionary<string, List<Vector3>>> _BarrelOffsetCache = new();

  private bool                              _hasReloaded               = true;  // whether we have finished reloading
  private bool                              _usesDynamicBarrelPosition = false; // whether the gun uses dynamic barrel offsets
  private Dictionary<string, List<Vector3>> _barrelOffsets             = null;  // list of dynamic barrel offsets for each of a gun's animations
  private Vector3                           _defaultBarrelOffset       = Vector3.zero; // the default barrel offset for guns with dynamic offsets

  public static void SetUpDynamicBarrelOffsets(Gun gun)
  {
    var d = _BarrelOffsetCache[gun.DisplayName] = new();
    //WARNING: can't do idle animation since it breaks loading with trimmed sprites
    SetUpDefaultDynamicBarrelOffset(d, gun);
    SetUpDynamicBarrelOffsetsForAnimation(d, gun, gun.chargeAnimation);
    SetUpDynamicBarrelOffsetsForAnimation(d, gun, gun.reloadAnimation);
    SetUpDynamicBarrelOffsetsForAnimation(d, gun, gun.shootAnimation);
  }

  private static void SetUpDefaultDynamicBarrelOffset(Dictionary<string, List<Vector3>> d, Gun gun)
  {
    d[_DEFAULT_BARREL_OFFSET] = new(){ gun.barrelOffset.localPosition };
  }

  private static void SetUpDynamicBarrelOffsetsForAnimation(Dictionary<string, List<Vector3>> d, Gun gun, string anim)
  {
    if (!string.IsNullOrEmpty(anim))
      d[anim] = gun.GetBarrelOffsetsForAnimation(anim);
  }

  public override void OnPlayerPickup(PlayerController player)
  {
    ETGModConsole.Log($"ever picked up? {this.EverPickedUp}");
    if (!this.EverPickedUp) // must come before base.OnPlayerPickup()
      OnFirstPickup(player);
    base.OnPlayerPickup(player);

    player.GunChanged -= OnGunsChanged;
    player.GunChanged += OnGunsChanged;

    // Load dynamic barrel offsets if we have any registered
    if (_BarrelOffsetCache.TryGetValue(this.gun.DisplayName, out var barrelOffsets))
    {
      this._usesDynamicBarrelPosition = true;
      this._barrelOffsets             = barrelOffsets;
      this._defaultBarrelOffset       = barrelOffsets[_DEFAULT_BARREL_OFFSET][0];
    }
  }

  public virtual void OnFirstPickup(PlayerController player)
  {

  }

  /// <summary>
  /// OnGunsChanged() is called when the player changes the current gun.
  /// </summary>
  /// <param name="previous">The previous current gun.</param>
  /// <param name="current">The new current gun.</param>
  /// <param name="newGun">True if the gun was changed because player picked up a new gun.</param>
  public void OnGunsChanged(Gun previous, Gun current, bool newGun)
  {
    if (previous != this.gun && current == this.gun)
        this.OnSwitchedToThisGun();
    if (previous == this.gun && current != this.gun)
        this.OnSwitchedAwayFromThisGun();
  }

  /// <summary>
  /// OnSwitchedToThisGun() when the player switches to this behaviour's affected gun.
  /// </summary>
  public virtual void OnSwitchedToThisGun()
  {
    this.gun.PreventNormalFireAudio = true;
    if (gun.spriteAnimator.GetClipByName(gun.shootAnimation) is tk2dSpriteAnimationClip clip)
      this.gun.OverrideNormalFireAudioEvent = clip.frames[0].eventAudio;
  }

  /// <summary>
  /// OnSwitchedToThisGun() when the player switches away from this behaviour's affected gun.
  /// </summary>
  public virtual void OnSwitchedAwayFromThisGun()
  {
    foreach (CwaffReticle ret in base.gameObject.GetComponents<CwaffReticle>())
      ret.HideImmediately();
  }

  public override void OnDroppedByPlayer(PlayerController player)
  {
      base.OnDroppedByPlayer(player);
      foreach (CwaffReticle ret in base.gameObject.GetComponents<CwaffReticle>())
        ret.HideImmediately();
  }

  public override void Update()
  {
    if (this.gun && !this.gun.IsReloading)
        this._hasReloaded = true;
    if (this._usesDynamicBarrelPosition)
      AdjustBarrelPosition();
  }

  private void AdjustBarrelPosition()
  {
    tk2dSpriteAnimator animator = this.gun.spriteAnimator;
    if (this._barrelOffsets.TryGetValue(animator.currentClip.name, out List<Vector3> offsets))
      this.gun.barrelOffset.localPosition = offsets[animator.CurrentFrame];
    else
      this.gun.barrelOffset.localPosition = this._defaultBarrelOffset;
    if (this.gun.sprite.FlipY)
      this.gun.barrelOffset.localPosition = this.gun.barrelOffset.localPosition.WithY(-this.gun.barrelOffset.localPosition.y);
  }

  public override void OnReloadPressed(PlayerController player, Gun gun, bool manual)
  {
    if (this._hasReloaded && gun.IsReloading)
    {
      OnActualReload(player, gun, manual);
      this._hasReloaded = false;
    }
    if (player.AcceptingNonMotionInput && !gun.IsReloading && manual && (gun.ClipShotsRemaining >= gun.ClipCapacity))
    {
      OnFullClipReload(player, gun);
    }
  }

  /// <summary>Called when the player actually initiates an ammo-repleneshing reload</summary>
  public virtual void OnActualReload(PlayerController player, Gun gun, bool manual)
  {
  }

  /// <summary>Called when the player manually initiates a reload with a full clip</summary>
  public virtual void OnFullClipReload(PlayerController player, Gun gun)
  {
  }

  // public void BraveOnLevelWasLoaded()
  // {
  // }

  /// <summary>Thrown guns don't count as dropped or destroyed, leading to bugs with certain guns that handle cleanup when dropped, so handle it manually</summary>
  [HarmonyPatch(typeof(Gun), nameof(Gun.ThrowGun))]
  private class ThrowGunCountsAsDroppedGunPatch
  {
      static void Prefix(Gun __instance)
      {
        if ((__instance.GetComponent<CwaffGun>() is CwaffGun cg) && (cg.PlayerOwner is PlayerController player))
          cg.OnDroppedByPlayer(player);
      }
  }

  /// <summary>Patch for detecting whether a secondary reload button is pressed</summary>
  [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.HandlePlayerInput))]
  private class SecondaryReloadPatch
  {
      [HarmonyILManipulator]
      private static void SecondaryReloadIL(ILContext il)
      {
          ILCursor cursor = new ILCursor(il);
          if (!cursor.TryGotoNext(MoveType.After,
              instr => instr.MatchLdfld<GungeonActions>("ReloadAction"),
              instr => instr.MatchCallvirt<InControl.OneAxisInputControl>("get_WasPressed")))
              return;
          cursor.Emit(OpCodes.Ldarg_0);
          cursor.Emit(OpCodes.Call, typeof(SecondaryReloadPatch).GetMethod(nameof(SecondaryReloadPatch.CheckSecondaryReload), BindingFlags.Static | BindingFlags.NonPublic));
          return;
      }

      private static bool CheckSecondaryReload(bool oldValue, PlayerController player)
      {
        return oldValue || player.SecondaryReloadPressed();
      }
  }
}

public abstract class CwaffBlankModificationItem: BlankModificationItem, ICwaffItem
{
  // public abstract string ItemName         { get; }
  // public abstract string ShortDescription { get; }
  // public abstract string LongDescription  { get; }
  // public abstract string Lore             { get; }

  public virtual void OnFirstPickup(PlayerController player)
  {

  }
}
