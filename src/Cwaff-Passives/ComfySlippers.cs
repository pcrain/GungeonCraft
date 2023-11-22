namespace CwaffingTheGungy;

public class ComfySlippers : PassiveItem
{
    public static string ItemName         = "Comfy Slippers";
    public static string SpritePath       = "comfy_slippers_icon";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";

    internal const float _MOVEMENT_BOOST                = 2.0f;
    internal const float _DODGE_BOOST                   = 0.2f;

    internal static StatModifier[] _ComfyBuffs          = null;

    private CellVisualData.CellFloorType _lastFloorType = CellVisualData.CellFloorType.Stone;

    public static void Init()
    {
        PickupObject item = Lazy.SetupPassive<ComfySlippers>(ItemName, SpritePath, ShortDescription, LongDescription);
        item.quality      = PickupObject.ItemQuality.D;
        _ComfyBuffs       = new[]{
            new StatModifier {
                amount      = _MOVEMENT_BOOST,
                statToBoost = PlayerStats.StatType.MovementSpeed,
                modifyType  = StatModifier.ModifyMethod.ADDITIVE},
            new StatModifier {
                amount      = _DODGE_BOOST,
                statToBoost = PlayerStats.StatType.DodgeRollSpeedMultiplier,
                modifyType  = StatModifier.ModifyMethod.ADDITIVE},
        };
    }

    public override DebrisObject Drop(PlayerController player)
    {
        this._lastFloorType = CellVisualData.CellFloorType.Stone;
        SetComfiness(false);
        return base.Drop(player);
    }

    public override void Update()
    {
        base.Update();
        if (!this.Owner?.specRigidbody)
            return;

        CellVisualData.CellFloorType cellFloorType = GameManager.Instance.Dungeon.GetFloorTypeFromPosition(this.Owner.specRigidbody.UnitBottomCenter);
        if (cellFloorType == this._lastFloorType)
            return;

        this._lastFloorType = cellFloorType;
        SetComfiness(cellFloorType == CellVisualData.CellFloorType.Carpet);
    }

    private void SetComfiness(bool comfy)
    {
        this.passiveStatModifiers = comfy ? _ComfyBuffs : null;
        this.Owner.stats.RecalculateStats(this.Owner);
    }
}
