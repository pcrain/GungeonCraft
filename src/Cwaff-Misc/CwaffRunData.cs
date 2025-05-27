namespace CwaffingTheGungy;

/// <summary>Class for saving various extra info for runs, serialized for mid-run saves</summary>
public class CwaffRunData : FakeItem
{
    public string btcktfEnemyGuid = string.Empty;
    public bool shouldReturnToPreviousFloor = false;
    public string nameOfPreviousFloor = null;
    public List<int>[] glassGunIds = [new(), new()];

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

        data.Add(glassGunIds[0].Count);
        for (int i = 0; i < glassGunIds[0].Count; ++i)
            data.Add(glassGunIds[0][i]);

        data.Add(glassGunIds[1].Count);
        for (int i = 0; i < glassGunIds[1].Count; ++i)
            data.Add(glassGunIds[1][i]);
    }

    public override void MidGameDeserialize(List<object> data)
    {
        Lazy.DebugLog($"deserializing extra mid run data");
        _Instance = this;
        base.MidGameDeserialize(data);
        int i = 0;

        btcktfEnemyGuid = (string)data[i++];
        Lazy.DebugLog($"  memorialized enemy is {btcktfEnemyGuid}");

        int p1NumGlassGuns = (int)data[i++];
        glassGunIds[0] = new List<int>();
        for (int gunNum = 0; gunNum < p1NumGlassGuns; ++gunNum)
            glassGunIds[0].Add((int)data[i++]);
        if (p1NumGlassGuns > 0 && GameManager.Instance.PrimaryPlayer is PlayerController p1)
            GlassAmmoBox.RestoreMidGameData(p1);

        int p2NumGlassGuns = (int)data[i++];
        glassGunIds[1] = new List<int>();
        for (int gunNum = 0; gunNum < p2NumGlassGuns; ++gunNum)
            glassGunIds[1].Add((int)data[i++]);
        if (p2NumGlassGuns > 0 && GameManager.Instance.SecondaryPlayer is PlayerController p2)
            GlassAmmoBox.RestoreMidGameData(p2);

        _Instance = this;
        this._deserialized = true;
    }
}
