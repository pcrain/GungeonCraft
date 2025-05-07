namespace CwaffingTheGungy;

/// <summary>Class for saving various extra info for runs, serialized for mid-run saves</summary>
public class CwaffRunData : FakeItem
{
    public string btcktfEnemyGuid = string.Empty;
    public bool shouldReturnToPreviousFloor = false;
    public string nameOfPreviousFloor = null;

    private bool _deserialized = false;

    private static CwaffRunData _Instance = null;
    public static CwaffRunData Instance => _Instance;

    public static void Init()
    {
        CwaffEvents.OnCleanStart += OnCleanStart; // make sure this runs AFTER any deserialization
        CwaffEvents.OnFirstFloorOfRunFullyLoaded += ResetForNewRun; // make sure this runs AFTER any deserialization

        FakeItem.Create<CwaffRunData>();
    }

    private static void OnCleanStart()
    {
        _Instance = null;
    }

    private static void ResetForNewRun()
    {
        PlayerController p1 = GameManager.Instance.PrimaryPlayer;
        if (!p1)
        {
            Lazy.DebugWarn("we don't have a player at the beginning of a run...concerning");
            return;
        }
        if (p1.GetPassive<CwaffRunData>() is CwaffRunData extantData)
        {
            if (!extantData._deserialized)
                Lazy.DebugWarn("already found midrun data, but it's not deserialized...concerning");
            return; // already deserialized
        }

        CwaffRunData runData = p1.AcquireFakeItem<CwaffRunData>();
        _Instance = runData;
        // Lazy.DebugLog("granting player 1 mid run data storage");
    }

    public override void MidGameSerialize(List<object> data)
    {
        base.MidGameSerialize(data);
        data.Add(btcktfEnemyGuid);
    }

    public override void MidGameDeserialize(List<object> data)
    {
        Lazy.DebugLog($"deserializing extra mid run data");
        base.MidGameDeserialize(data);
        int i = 0;

        btcktfEnemyGuid = (string)data[i++];
        Lazy.DebugLog($"  memorialized enemy is {btcktfEnemyGuid}");

        _Instance = this;
        this._deserialized = true;
    }
}
