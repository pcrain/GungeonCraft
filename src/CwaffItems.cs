namespace CwaffingTheGungy;

public interface ICwaffItem
{
  // public string ItemName          { get; }
  // public string ShortDescription  { get; }
  // public string LongDescription   { get; }
  // public string Lore              { get; }
}

public static class CwaffItem
{
  // public static string Name<T>() where T : ICwaffItem, new() { return new T().ItemName; }
}

public abstract class CwaffPassive : PassiveItem, ICwaffItem
{
  // public abstract string ItemName         { get; }
  // public abstract string ShortDescription { get; }
  // public abstract string LongDescription  { get; }
  // public abstract string Lore             { get; }
}

public abstract class CwaffActive: PlayerItem, ICwaffItem
{
  // public abstract string ItemName         { get; }
  // public abstract string ShortDescription { get; }
  // public abstract string LongDescription  { get; }
  // public abstract string Lore             { get; }
}

public abstract class CwaffGun: GunBehaviour, ICwaffItem
{
  // public abstract string ItemName         { get; }
  // public abstract string ShortDescription { get; }
  // public abstract string LongDescription  { get; }
  // public abstract string Lore             { get; }
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
    string fireAudio = gun.spriteAnimator.GetClipByName(gun.shootAnimation).frames[0].eventAudio;
    if (!string.IsNullOrEmpty(fireAudio))
    {
      // if (C.DEBUG_BUILD)
      //   ETGModConsole.Log($"custom fire audio initialized for {this.gun.EncounterNameOrDisplayName}");
      this.gun.OverrideNormalFireAudioEvent = fireAudio;
    }
  }

  /// <summary>
  /// OnSwitchedToThisGun() when the player switches away from this behaviour's affected gun.
  /// </summary>
  public virtual void OnSwitchedAwayFromThisGun()
  {
  }
}

public abstract class CwaffBlankModificationItem: BlankModificationItem, ICwaffItem
{
  // public abstract string ItemName         { get; }
  // public abstract string ShortDescription { get; }
  // public abstract string LongDescription  { get; }
  // public abstract string Lore             { get; }
}
