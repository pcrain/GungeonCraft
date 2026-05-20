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
    internal static StatModifier[] _NoBoosts            = [];

    private CellVisualData.CellFloorType _lastFloorType = CellVisualData.CellFloorType.Stone;

    public static void Init()
    {
        PassiveItem item  = Lazy.SetupPassive<ComfySlippers>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.D;
        _ComfyBuffs       = [
            StatType.MovementSpeed.Add(_MOVEMENT_BOOST),
            StatType.DodgeRollSpeedMultiplier.Add(_DODGE_BOOST),
        ];
    }

    public override void DisableEffect(PlayerController player)
    {
        base.DisableEffect(player);
        if (!player)
            return;
        this._lastFloorType = CellVisualData.CellFloorType.Stone;
        SetComfiness(false);
    }

    public override void Update()
    {
        base.Update();
        if (!GameManager.HasInstance || GameManager.Instance.IsLoadingLevel || GameManager.IsReturningToBreach)
            return;
        if (!this.Owner || !this.Owner.specRigidbody)
            return;

        Vector2 footPosition = this.Owner.specRigidbody.UnitBottomCenter;
        if (this.Owner.HasSynergy(Synergy.BEDTIME_ROUTINE))
        {
          if (!SudsWave._ToothpasteGooper)
            SudsWave._ToothpasteGooper = DeadlyDeadlyGoopManager.GetGoopManagerForGoopType(EasyGoopDefinitions.ToothpasteGoop);
          if (SudsWave._ToothpasteGooper.m_goopedPositions.Contains(footPosition.WorldToGoopPosition()))
            SudsWave._ToothpasteGooper.AddGoopCircle(footPosition, 1f);
        }

        CellVisualData.CellFloorType cellFloorType = GameManager.Instance.Dungeon.GetFloorTypeFromPosition(footPosition);
        if (cellFloorType == this._lastFloorType)
            return;

        this._lastFloorType = cellFloorType;
        SetComfiness(cellFloorType == CellVisualData.CellFloorType.Carpet);
    }

    private void SetComfiness(bool comfy)
    {
        this.passiveStatModifiers = comfy ? _ComfyBuffs : _NoBoosts;
        this.Owner.stats.RecalculateStats(this.Owner);
    }
}
