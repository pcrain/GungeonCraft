namespace CwaffingTheGungy;

public static class EnumExtenders
{
  /// <summary>Extend an enum from a string</summary>
  public static T ExtendEnum<T>(this string s, string guid = C.MOD_PREFIX) where T : System.Enum
  {
    return ETGModCompatibility.ExtendEnum<T>(guid.ToUpper(), s);
  }
}

public static class CwaffGunClass
{
    public static readonly GunClass UTILITY = "UTILITY".ExtendEnum<GunClass>();
}

public static class CwaffShootBehaviorState
{
    public static readonly ShootBehavior.State Relocating = "Relocating".ExtendEnum<ShootBehavior.State>();
}

public static class CwaffItemQuality
{
    public static readonly ItemQuality F = "F".ExtendEnum<ItemQuality>(guid: "Tier");
}
