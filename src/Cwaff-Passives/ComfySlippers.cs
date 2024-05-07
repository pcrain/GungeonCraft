namespace CwaffingTheGungy;

public class ComfySlippers : CwaffPassive
{
    public static string ItemName         = "Comfy Slippers";
    public static string ShortDescription = "Furry Foot Friends";
    public static string LongDescription  = "Increases movement speed and dodge roll speed while on carpeted surfaces.";
    public static string Lore             = "The fluffiest and fuzziest footwear that stray casings you picked up off of the ground can buy. On top of being fashionable and adorable as all get-out, the padded insoles and added arch support reduce ankle strain and let you glide effortlessly across suitably soft floors. As an added bonus, your new slipper buddies provide some much-needed companionship on those cold, musty Gungeon nights, and will always be there to remind you that looking down at your feet while scurrying through the Gungeon is rather dangerous and probably isn't a fantastic idea and WATCH OUT FOR THAT PITF--.";

    internal const float _MOVEMENT_BOOST                = 2.0f;
    internal const float _DODGE_BOOST                   = 0.2f;

    internal static StatModifier[] _ComfyBuffs          = null;

    private CellVisualData.CellFloorType _lastFloorType = CellVisualData.CellFloorType.Stone;

    public static void Init()
    {
        PickupObject item = Lazy.SetupPassive<ComfySlippers>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.D;
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

    public override void OnDestroy()
    {
        this._lastFloorType = CellVisualData.CellFloorType.Stone;
        SetComfiness(false);
        base.OnDestroy();
    }

    public override void Update()
    {
        base.Update();
        if (!this.Owner || !this.Owner.specRigidbody)
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
