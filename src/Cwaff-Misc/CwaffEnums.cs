namespace CwaffingTheGungy;

public static class CwaffGunClass
{
    public static readonly GunClass UTILITY = "UTILITY".ExtendEnum<GunClass>();
}

public static class CwaffShootBehaviorState
{
    public static readonly ShootBehavior.State Relocating = "Relocating".ExtendEnum<ShootBehavior.State>();
}
