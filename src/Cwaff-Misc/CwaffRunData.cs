namespace CwaffingTheGungy;

/// <summary>Class for saving various extra info for runs, serialized for mid-run saves</summary>
public class CwaffRunData : FakeItem
{
    public string btcktfEnemyGuid = string.Empty;
    public bool shouldReturnToPreviousFloor = false;
    public string nameOfPreviousFloor = null;
    public List<int>[] glassGunIds = [new(), new()];
    public List<int>[] pristineGunIds = [new(), new()];

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

        for (int p = 0; p < 1; ++p)
        {
            // Glass Ammo Box Data
            data.Add(glassGunIds[p].Count);
            for (int i = 0; i < glassGunIds[p].Count; ++i)
                data.Add(glassGunIds[p][i]);

            // Display Pedestal Data
            data.Add(pristineGunIds[p].Count);
            for (int i = 0; i < pristineGunIds[p].Count; ++i)
                data.Add(pristineGunIds[p][i]);
        }
    }

    public override void MidGameDeserialize(List<object> data)
    {
        Lazy.DebugLog($"deserializing extra mid run data");
        _Instance = this;
        base.MidGameDeserialize(data);
        int i = 0;

        btcktfEnemyGuid = (string)data[i++];
        Lazy.DebugLog($"  memorialized enemy is {btcktfEnemyGuid}");

        for (int p = 0; p < 1; ++p)
        {
            PlayerController pp = GameManager.Instance.AllPlayers[p];
            // Glass Ammo Box Data
            int pGlassGuns = (int)data[i++];
            glassGunIds[p] = new List<int>();
            for (int gunNum = 0; gunNum < pGlassGuns; ++gunNum)
                glassGunIds[p].Add((int)data[i++]);
            if (pGlassGuns > 0 && pp)
                GlassAmmoBox.RestoreMidGameData(pp);

            // Display Pedestal Data
            int pPristineGuns = (int)data[i++];
            pristineGunIds[p] = new List<int>();
            for (int gunNum = 0; gunNum < pPristineGuns; ++gunNum)
                pristineGunIds[p].Add((int)data[i++]);
            if (pPristineGuns > 0 && pp)
                DisplayPedestal.RestoreMidGameData(pp);
        }

        _Instance = this;
        this._deserialized = true;
    }
}
