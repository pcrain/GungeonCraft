namespace CwaffingTheGungy;

public interface ICustomDodgeRollItem
{
  /// <summary>The CustomDodgeRoll, if any, this item grants while held</summary>
  public CustomDodgeRoll CustomDodgeRoll();

  /// <summary>The number of extra midair dodge rolls this item grants</summary>
  public int ExtraMidairDodgeRolls();
}
