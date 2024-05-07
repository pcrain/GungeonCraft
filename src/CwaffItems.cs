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

public abstract class CwaffGun: AdvancedGunBehavior, ICwaffItem
{
  // public abstract string ItemName         { get; }
  // public abstract string ShortDescription { get; }
  // public abstract string LongDescription  { get; }
  // public abstract string Lore             { get; }
}

public abstract class CwaffBlankModificationItem: BlankModificationItem, ICwaffItem
{
  // public abstract string ItemName         { get; }
  // public abstract string ShortDescription { get; }
  // public abstract string LongDescription  { get; }
  // public abstract string Lore             { get; }
}
