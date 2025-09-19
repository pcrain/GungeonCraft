namespace CwaffingTheGungy;

/// <summary>Class for saving various extra info for runs, serialized for mid-run saves</summary>
[HarmonyPatch]
public class CwaffRunData : FakeItem
{
    public string btcktfEnemyGuid = string.Empty;
    public bool noPastRegrets = false;
    public bool scrambledBulletHell = false;
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

        data.Add(noPastRegrets);
        data.Add(scrambledBulletHell);
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

        noPastRegrets = (bool)data[i++];
        scrambledBulletHell = (bool)data[i++];
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

            // Display Pedestal Data
            int pPristineGuns = (int)data[i++];
            pristineGunIds[p] = new List<int>();
            for (int gunNum = 0; gunNum < pPristineGuns; ++gunNum)
                pristineGunIds[p].Add((int)data[i++]);
        }

        _Instance = this;
        this._deserialized = true;
    }

    private void FinalizeDeserialization(PlayerController p1, PlayerController p2)
    {
        for (int p = 0; p < 1; ++p)
        {
            PlayerController pp = (p == 0) ? p1 : p2;
            if (!pp)
                continue;

            // Glass Ammo Box Data
            if (glassGunIds[p].Count > 0)
                GlassAmmoBox.RestoreMidGameData(pp);

            // Display Pedestal Data
            if (pristineGunIds[p].Count > 0)
                DisplayPedestal.RestoreMidGameData(pp);
        }
    }

    //NOTE: need to finish deserialization after both players' other items / guns are loaded in
    [HarmonyPatch(typeof(MidGameSaveData), nameof(MidGameSaveData.LoadDataFromMidGameSave))]
    [HarmonyPostfix]
    private static void MidGameSaveDataLoadDataFromMidGameSavePatch(MidGameSaveData __instance, PlayerController p1, PlayerController p2)
    {
        MidGameSaveData.IsInitializingPlayerData = true;
        CwaffRunData.Instance.FinalizeDeserialization(p1, p2);
        MidGameSaveData.IsInitializingPlayerData = false;
    }
}
