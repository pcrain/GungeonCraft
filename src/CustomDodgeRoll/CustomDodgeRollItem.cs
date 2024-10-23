namespace CwaffingTheGungy;

public class CustomDodgeRollItem : PassiveItem
{
  /// <summary>The CustomDodgeRoll, if any, this item grants while held</summary>
  public virtual CustomDodgeRoll CustomDodgeRoll() => null;

  /// <summary>The number of extra midair dodge rolls this item grants</summary>
  public virtual int ExtraMidairDodgeRolls() => 0;
}
