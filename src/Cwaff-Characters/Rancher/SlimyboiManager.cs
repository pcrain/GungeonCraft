namespace CwaffingTheGungy;

/// <summary>Class for managing Slime spawns throughout a run</summary>
public class SlimyboiManager : MonoBehaviour
{
  private static SlimyboiManager _Instance = null;

  private List<SlimyboiController> _allActiveSlimes;
  private ReadOnlyCollection<SlimyboiController> _readOnlyActiveSlimes;

  public static ReadOnlyCollection<SlimyboiController> ActiveSlimes
  {
    get
    {
      return _Instance._readOnlyActiveSlimes;
    }
  }

  public static SlimyboiManager Instance
  {
    get
    {
      if (!_Instance)
      {
        CwaffEvents.OnCleanStart -= OnCleanStart;
        CwaffEvents.OnCleanStart += OnCleanStart;
        #if DEBUG
          Commands._OnDebugKeyPressed -= DebugSlimeSpawn;
          Commands._OnDebugKeyPressed += DebugSlimeSpawn;
        #endif
        _Instance = GameManager.Instance.gameObject.AddComponent<SlimyboiManager>();
        _Instance._allActiveSlimes = new();
        _Instance._readOnlyActiveSlimes = new ReadOnlyCollection<SlimyboiController>(_Instance._allActiveSlimes);
      }
      return _Instance;
    }
  }

  public static void EnsureInstance() { var _ = Instance; }

  //NOTE: use _Instance so we don't actually create a SlimyboiManager if we don't have one

  private static void OnCleanStart()
  {
    if (_Instance)
      UnityEngine.Object.Destroy(_Instance);
    _Instance = null;
  }

  public static void RegisterSlime(SlimyboiController sloim)
  {
    if (_Instance)
      _Instance._allActiveSlimes.Add(sloim);
  }

  public static void DeregisterSlime(SlimyboiController sloim)
  {
    if (_Instance)
      _Instance._allActiveSlimes.Remove(sloim);
  }

  public static bool AnyActiveSlimes() => _Instance && _Instance._allActiveSlimes.Count > 0;
  public static int NumActiveSlimes() => _Instance ? _Instance._allActiveSlimes.Count : 0;

  public static void DebugSlimeSpawn()
  {
    RoomHandler room = GameManager.Instance.PrimaryPlayer.CurrentRoom;
    if (room == null)
      return;

    AIActor slimePrefab = SlimyboiType.Pink.Data().prefab;
    if (slimePrefab.RandomCellForEnemySpawn(room) is not IntVector2 spawnPos)
    {
      Lazy.DebugConsoleLog($"  failed to find good position to spawn slime");
      return;
    }

    AIActor slimeActor = AIActor.Spawn(
      prefabActor     : slimePrefab,
      position        : spawnPos,
      source          : room,
      awakenAnimType  : AIActor.AwakenAnimationType.Spawn,
      correctForWalls : true);
    slimeActor.SpawnInInstantly(isReinforcement: true);
    SlimyboiController slime = slimeActor.gameObject.GetComponent<SlimyboiController>();
    slime.HandleRoomSpawn();
  }
}
