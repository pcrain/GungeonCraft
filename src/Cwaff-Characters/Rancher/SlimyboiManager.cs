namespace CwaffingTheGungy;

/// <summary>Class for managing Slime spawns throughout a run</summary>
public class SlimyboiManager : MonoBehaviour
{
  //NOTE: use _Instance where possible so we don't actually create a SlimyboiManager if we don't have one
  private static SlimyboiManager _Instance = null;
  // NOTE: needs to be static so it can happen at room processing time
  private static Dictionary<RoomHandler, List<TrapController>> _TrapMap;

  private int _enemiesExplodedThisRoom;
  private HashSet<RoomHandler> _processedRooms;
  private List<SlimyboiController> _allActiveSlimes;
  private List<float> _enemyKillTimes;
  private ReadOnlyCollection<SlimyboiController> _readOnlyActiveSlimes;

  public static ReadOnlyCollection<SlimyboiController> ActiveSlimes => Instance._readOnlyActiveSlimes;

  public static SlimyboiManager Instance
  {
    get
    {
      if (_Instance)
        return _Instance;

      _Instance = GameManager.Instance.gameObject.AddComponent<SlimyboiManager>();
      _Instance._enemiesExplodedThisRoom = 0;
      _Instance._processedRooms = new();
      _Instance._allActiveSlimes = new();
      _Instance._enemyKillTimes = new();
      _Instance._readOnlyActiveSlimes = new ReadOnlyCollection<SlimyboiController>(_Instance._allActiveSlimes);

      _TrapMap = new();

      CwaffEvents.OnCleanStart -= OnCleanStart;
      CwaffEvents.OnCleanStart += OnCleanStart;
      CwaffEvents.OnChangedRooms -= OnChangedRooms;
      CwaffEvents.OnChangedRooms += OnChangedRooms;
      CwaffEvents.OnNewFloorFullyLoaded -= OnNewFloorFullyLoaded;
      CwaffEvents.OnNewFloorFullyLoaded += OnNewFloorFullyLoaded;

      #if DEBUG
        Commands._OnDebugKeyPressed -= DebugSlimeSpawn;
        Commands._OnDebugKeyPressed += DebugSlimeSpawn;
      #endif

      return _Instance;
    }
  }

  private void OnDestroy()
  {
    // not sure if we need anything here
  }

  public static void EnsureInstance() { var _ = Instance; }

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
  private static void OnNewFloorFullyLoaded()
  {
    _TrapMap.Clear();
    if (_Instance)
      _Instance._processedRooms.Clear();
  }

  internal static void DebugSlimeSpawn() => SpawnSingleSlime(SlimyboiType.Pink);

  internal static void RegisterTrap(TrapController trap, RoomHandler room)
  {
    if (room == null)
      return;
    if (!_TrapMap.TryGetValue(room, out List<TrapController> roomTraps))
      roomTraps = _TrapMap[room] = new();
    roomTraps.Add(trap);
    Lazy.DebugConsoleLog($" added trap {trap.gameObject.name} to room {room.area.prototypeRoom.name}");
  }

  private static void SpawnSingleSlime(SlimyboiType sloim, RoomHandler room = null, IntVector2? spawnPos = null, PlayerController player = null)
  {
      if (room == null && spawnPos.HasValue)
        room = spawnPos.Value.ToVector2().GetAbsoluteRoom();
      if (room == null)
        room = (player ? player : GameManager.Instance.BestActivePlayer).CurrentRoom;
      AIActor slimePrefab = sloim.Data().prefab;
      AIActor slimeActor = AIActor.Spawn(
        prefabActor     : slimePrefab,
        position        : spawnPos ?? slimePrefab.RandomCellForEnemySpawn(room) ?? (player ? player : GameManager.Instance.BestActivePlayer).CenterPosition.ToIntVector2(),
        source          : room,
        awakenAnimType  : AIActor.AwakenAnimationType.Spawn,
        correctForWalls : true);
      slimeActor.SpawnInInstantly(isReinforcement: true);
      SlimyboiController slime = slimeActor.gameObject.GetComponent<SlimyboiController>();
      slime.HandleRoomSpawn();
  }

  private static IEnumerator SpawnSlimesEnumerator(List<SlimyboiType> slimes, PlayerController player = null, RoomHandler room = null, Vector2? spawnPos = null)
  {
    const float SPAWN_GAP = 0.3f;

    if (!_Instance || slimes == null || slimes.Count == 0)
      yield break;

    if (room == null && spawnPos.HasValue)
      room = spawnPos.Value.GetAbsoluteRoom();
    if (room == null)
      room = (player ? player : GameManager.Instance.BestActivePlayer).CurrentRoom;
    bool centeredSpawn = spawnPos.HasValue && room != null;
    IntVector2 spawnCenter = spawnPos.HasValue ? spawnPos.Value.ToIntVector2() : default;
    foreach (SlimyboiType sloim in slimes)
    {
      if (centeredSpawn && sloim.Data().prefab.RandomCellForEnemySpawn(room, targetCenter: spawnCenter, minDist: 0f, maxDist: 4f) is IntVector2 pos)
        SpawnSingleSlime(sloim, room, spawnPos: pos);
      else
        SpawnSingleSlime(sloim, room);
      yield return new WaitForSeconds(SPAWN_GAP);
    }

    slimes.Clear();
    yield break;
  }

  private static void SpawnSlimes(List<SlimyboiType> slimes, PlayerController player = null, RoomHandler room = null, Vector2? pos = null)
  {
    _Instance.StartCoroutine(SpawnSlimesEnumerator(slimes, player, room, pos));
  }

  private static List<SlimyboiType> DetermineRoomSlimesToSpawn(PlayerController player, RoomHandler room, bool wasCombatEncounter)
  {
    List<SlimyboiType> sloims = new();
    if (!wasCombatEncounter) // non combat room
    {
      if (room.area.PrototypeRoomCategory == PrototypeDungeonRoom.RoomCategory.SECRET) // secret room
        sloims.AddMultiple(SlimyboiType.Quantum, 2);
      else if (room.area.PrototypeRoomCategory == PrototypeDungeonRoom.RoomCategory.REWARD) // chest room
        sloims.AddMultiple(SlimyboiType.Phosphor, 2); // TODO: more slimes depending on chest tier
      else if (room.IsShop) // shop room
      {
        bool isBelloShop = false;
        foreach (BaseShopController shop in StaticReferenceManager.AllShops)
          if (shop.m_room == room && shop.IsMainShopkeep)
            isBelloShop = true;
        sloims.AddMultiple(SlimyboiType.Tabby, isBelloShop ? 4 : 2);
      }
    }

    // scan for traps
    float pitCoverage = room.GetPitCoverage();
    int numSpikeTraps = CountSpikeTraps(room);
    int oldSlimeCount = sloims.Count;
    if (pitCoverage > 0.0f)
      sloims.Add(SlimyboiType.Dervish);
    if (pitCoverage > 0.2f)
      sloims.Add(SlimyboiType.Dervish);
    if (numSpikeTraps > 0)
      sloims.Add(SlimyboiType.Crystal);
    if (numSpikeTraps > 5)
      sloims.Add(SlimyboiType.Crystal);
    if (!wasCombatEncounter)
    {
      if (room.area.PrototypeRoomCategory == PrototypeDungeonRoom.RoomCategory.NORMAL && room.area.PrototypeRoomNormalSubcategory == PrototypeDungeonRoom.RoomNormalSubCategory.TRAP)
      {
        int trapSlimesAdded = sloims.Count - oldSlimeCount;
        if (trapSlimesAdded < 2)
          sloims.AddMultiple(SlimyboiType.Honey, trapSlimesAdded - 2); // add honey slimes to trap rooms without spikes or pits
      }
      return sloims; // nothing else to do in non-combat rooms
    }

    // check boss clears
    if (room.area.PrototypeRoomCategory == PrototypeDungeonRoom.RoomCategory.BOSS)
    {
      sloims.Add(SlimyboiType.Gold);
      if (!room.PlayerHasTakenDamageInThisRoom)
        sloims.Add(SlimyboiType.Gold);
    }
    // check if 3 enemies were killed in a second
    for (int i = _Instance._enemyKillTimes.Count - 1; i >= 2; --i)
    {
      if ((_Instance._enemyKillTimes[i] - _Instance._enemyKillTimes[i - 2]) > 1.0f)
        continue;
      sloims.Add(SlimyboiType.Quicksilver);
      break;
    }
    _Instance._enemyKillTimes.Clear();
    // check exploded enemy count
    if (_Instance._enemiesExplodedThisRoom > 0)
    {
      sloims.AddMultiple(SlimyboiType.Boom, _Instance._enemiesExplodedThisRoom);
      _Instance._enemiesExplodedThisRoom = 0;
    }
    // handle standard combat room clears
    int pinkSlimesToSpawn = 1;
    if (room.area.prototypeRoom != null && room.area.prototypeRoom.additionalObjectLayers != null)
      pinkSlimesToSpawn += room.area.prototypeRoom.additionalObjectLayers.Count;
    if (!room.PlayerHasTakenDamageInThisRoom)
      pinkSlimesToSpawn += 1;
    // TODO: spawn more slimes if player is REALLY low
    for (int i = 0; i < pinkSlimesToSpawn; ++i)
      sloims.Add(SlimyboiType.Pink);
    return sloims;
  }

  private static int CountSpikeTraps(RoomHandler room)
  {
    if (!_Instance || room == null || !_TrapMap.TryGetValue(room, out List<TrapController> roomTraps))
      return 0;
    int spikeCount = 0;
    foreach (TrapController trap in roomTraps)
    {
      if (trap is BasicTrapController basicTrap && basicTrap.TrapSwitchState == "spikes")
        ++spikeCount;
      else if (trap is PathingTrapController pathTrap)
      {
        string trapName = trap.gameObject.name;
        if (trapName.Contains("sawblade"))
          spikeCount += 2;
        else if (trapName.Contains("spinning_log"))
          spikeCount += 5;
      }
    }
    return spikeCount;
  }

  private static void HandleSlimeSpawns(PlayerController player, RoomHandler room, bool combatEnded)
  {
    const float GLITCH_SLIME_CHANCE = 0.005f;
    if (!_Instance || room == null || room.area == null || (!combatEnded && room.IsUnclearedCombatRoom()) || _Instance._processedRooms.Contains(room))
      return;

    _Instance._processedRooms.Add(room);
    List<SlimyboiType> sloims = DetermineRoomSlimesToSpawn(player, room, combatEnded);
    if (UnityEngine.Random.value < GLITCH_SLIME_CHANCE)
      sloims.Add(SlimyboiType.Glitch);
    SpawnSlimes(sloims, player, room);
  }

  public static void OnCombatRoomClear(PlayerController player)
  {
    HandleSlimeSpawns(player, player.CurrentRoom, combatEnded: true);
  }

  public static void OnChangedRooms(PlayerController player, RoomHandler oldRoom, RoomHandler newRoom)
  {
    HandleSlimeSpawns(player, newRoom, combatEnded: false);
  }

  public static void OnAmmoCollected(AmmoPickup ammo, PlayerController player)
  {
    SpawnSlimes([SlimyboiType.Hunter, SlimyboiType.Hunter], player: player, pos: player.CenterPosition);
  }

  public static void OnBlankCollected(SilencerItem blank, PlayerController player)
  {
    SpawnSlimes([SlimyboiType.Tangle, SlimyboiType.Tangle], player: player, pos: player.CenterPosition);
  }

  public static void OnAnyHealthHaverDie(HealthHaver hh)
  {
    if (hh.aiActor is not AIActor enemy || enemy.gameObject.GetComponent<SlimyboiController>())
      return; // slimes themselves shouldn't count towards kills

    _Instance._enemyKillTimes.Add(BraveTime.ScaledTimeSinceStartup);
    if (hh.lastIncurredDamageSource == StringTableManager.GetEnemiesString("#EXPLOSION"))
      ++_Instance._enemiesExplodedThisRoom;
  }

  public static void OnAnyPlayerCollectedHealth(HealthPickup health, PlayerController player)
  {
    if (health.armorAmount > 0)
      SpawnSlimes([SlimyboiType.Rock], player: player, pos: player.CenterPosition);
  }

  public static void OnWillPickUpCurrency(PlayerController player, CurrencyPickup currency)
  {
    const float LUCKY_CHANCE_PER_CASING = 0.01f;
    if (currency.IsMetaCurrency)
      return;
    int slimesToSpawn = 0;
    for (int i = currency.currencyValue; i > 0; --i)
      if (UnityEngine.Random.value <= LUCKY_CHANCE_PER_CASING)
        ++slimesToSpawn;
    if (slimesToSpawn > 0)
      SpawnSlimes(Enumerable.Repeat(SlimyboiType.Lucky, slimesToSpawn).ToList(), player: player, pos: player.CenterPosition);
  }

  public static void HandleTableFlip(FlippableCover table)
  {
    if (!_Instance)
      return;
    SpawnSlimes([SlimyboiType.Saber], pos: table.transform.position);
  }

  public static void OnMinorBreakableShattered(MinorBreakable breakable)
  {
    if (!_Instance)
      return;
    if (!breakable.goopsOnBreak || breakable.goopType is not GoopDefinition def)
      return;
    if (def.AppliesDamageOverTime)
      SpawnSlimes([SlimyboiType.Rad], pos: breakable.transform.position);
    else if (def.UsesGreenFire)
      SpawnSlimes([SlimyboiType.Fire, SlimyboiType.Fire], pos: breakable.transform.position);
    else if (def.isOily)
      SpawnSlimes([SlimyboiType.Fire], pos: breakable.transform.position);
    else if (def.CanBeElectrified || def.CanBeFrozen)
      SpawnSlimes([SlimyboiType.Puddle, SlimyboiType.Puddle, SlimyboiType.Puddle], pos: breakable.transform.position);
  }

  public static void OnWillPickUpAnyPassive(PlayerController player, PickupObject pickup)
  {
    if (pickup is IounStoneOrbitalItem guon && !guon.m_pickedUpThisRun && (guon.BreaksUponContact || guon.BreaksUponOwnerDamage))
      SpawnSlimes([SlimyboiType.Mosaic], player: player, pos: player.CenterPosition);
  }
}
