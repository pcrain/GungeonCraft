namespace CwaffingTheGungy;

public interface ICwaffItem
{
}

public static class CwaffItem
{
}

public abstract class CwaffPassive : PassiveItem, ICwaffItem
{
}

public abstract class CwaffActive: PlayerItem, ICwaffItem
{
}

public abstract class CwaffGun: GunBehaviour, ICwaffItem/*, ILevelLoadedListener*/
{
  public bool hasReloaded = true;

  public override void OnPlayerPickup(PlayerController player)
  {
    base.OnPlayerPickup(player);
    player.GunChanged -= OnGunsChanged;
    player.GunChanged += OnGunsChanged;
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
    this.gun.OverrideNormalFireAudioEvent = gun.spriteAnimator.GetClipByName(gun.shootAnimation).frames[0].eventAudio;
  }

  /// <summary>
  /// OnSwitchedToThisGun() when the player switches away from this behaviour's affected gun.
  /// </summary>
  public virtual void OnSwitchedAwayFromThisGun()
  {
  }

  public override void Update()
  {
    if (this.gun && !this.gun.IsReloading)
        this.hasReloaded = true;
  }

  public override void OnReloadPressed(PlayerController player, Gun gun, bool manual)
  {
    if (this.hasReloaded && gun.IsReloading)
    {
      OnActualReload(player, gun, manual);
      this.hasReloaded = false;
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

  public virtual void OnFullClipReload(PlayerController player, Gun gun)
  {

  }

  // public void BraveOnLevelWasLoaded()
  // {
  // }
}

public abstract class CwaffBlankModificationItem: BlankModificationItem, ICwaffItem
{
  // public abstract string ItemName         { get; }
  // public abstract string ShortDescription { get; }
  // public abstract string LongDescription  { get; }
  // public abstract string Lore             { get; }
}
