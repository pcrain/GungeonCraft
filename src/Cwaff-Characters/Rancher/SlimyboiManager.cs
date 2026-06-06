namespace CwaffingTheGungy;

/// <summary>Class for managing Slime spawns throughout a run</summary>
public class SlimyboiManager : MonoBehaviour
{
  //NOTE: use _Instance where possible so we don't actually create a SlimyboiManager if we don't have one
  private static SlimyboiManager _Instance = null;
  // NOTE: needs to be static so it can happen at room processing time
  private static Dictionary<RoomHandler, List<TrapController>> _TrapMap;

  private HashSet<RoomHandler> _processedRooms;
  private List<SlimyboiController> _allActiveSlimes;
  private ReadOnlyCollection<SlimyboiController> _readOnlyActiveSlimes;

  public static ReadOnlyCollection<SlimyboiController> ActiveSlimes => Instance._readOnlyActiveSlimes;

  public static SlimyboiManager Instance
  {
    get
    {
      if (_Instance)
        return _Instance;

      _Instance = GameManager.Instance.gameObject.AddComponent<SlimyboiManager>();
      _Instance._processedRooms = new();
      _Instance._allActiveSlimes = new();
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
    Lazy.DebugConsoleLog($"clearing traps");
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

  private static IEnumerator SpawnSlimesEnumerator(List<SlimyboiType> slimes, PlayerController player = null, RoomHandler room = null)
  {
    const float SPAWN_GAP = 0.3f;

    if (!_Instance || slimes == null || slimes.Count == 0)
      yield break;

    foreach (SlimyboiType sloim in slimes)
    {
      SpawnSingleSlime(sloim, room);
      yield return new WaitForSeconds(SPAWN_GAP);
    }

    slimes.Clear();
    yield break;
  }

  private static void SpawnSlimes(List<SlimyboiType> slimes, PlayerController player = null, RoomHandler room = null)
  {
    _Instance.StartCoroutine(SpawnSlimesEnumerator(slimes, player, room));
  }

  private static List<SlimyboiType> DetermineRoomSlimesToSpawn(PlayerController player, RoomHandler room, bool combatEnded)
  {
    List <SlimyboiType> sloims = null;
    if (!combatEnded) // non combat room
    {
      if (room.area.PrototypeRoomCategory == PrototypeDungeonRoom.RoomCategory.SECRET) // secret room
        return [SlimyboiType.Quantum, SlimyboiType.Quantum];
      if (room.area.PrototypeRoomCategory == PrototypeDungeonRoom.RoomCategory.REWARD) // chest room
        return Enumerable.Repeat(SlimyboiType.Phosphor, 2).ToList(); // TODO: more slimes depending on chest tier
      if (room.IsShop) // shop room
      {
        bool isBelloShop = false;
        foreach (BaseShopController shop in StaticReferenceManager.AllShops)
          if (shop.m_room == room && shop.IsMainShopkeep)
            isBelloShop = true;
        return Enumerable.Repeat(SlimyboiType.Tabby, isBelloShop ? 4 : 2).ToList();
      }

      // probably a trap room, scan for traps
      sloims = new();
      float pitCoverage = room.GetPitCoverage();
      int numSpikeTraps = CountSpikeTraps(room);
      if (pitCoverage > 0.0f)
        sloims.Add(SlimyboiType.Dervish);
      if (pitCoverage > 0.2f)
        sloims.Add(SlimyboiType.Dervish);
      if (numSpikeTraps > 0)
        sloims.Add(SlimyboiType.Crystal);
      if (numSpikeTraps > 5)
        sloims.Add(SlimyboiType.Crystal);

      return sloims;
    }

    sloims = new();
    // combat room
    if (room.area.PrototypeRoomCategory == PrototypeDungeonRoom.RoomCategory.BOSS)
    {
      sloims.Add(SlimyboiType.Gold);
      if (!room.PlayerHasTakenDamageInThisRoom)
        sloims.Add(SlimyboiType.Gold);
    }
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
    if (!_Instance || room == null || room.area == null || (!combatEnded && room.EverHadEnemies) || _Instance._processedRooms.Contains(room))
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
    SpawnSlimes([SlimyboiType.Hunter, SlimyboiType.Hunter], player);
  }

  public static void OnBlankCollected(SilencerItem blank, PlayerController player)
  {
    SpawnSlimes([SlimyboiType.Tangle, SlimyboiType.Tangle], player);
  }

  public static void OnAnyHealthHaverDie(HealthHaver hh)
  {
    if (hh.aiActor is not AIActor enemy || hh.lastIncurredDamageSource != StringTableManager.GetEnemiesString("#EXPLOSION"))
      return;
    if (enemy.gameObject.GetComponent<SlimyboiController>())
      return; // slimes themselves shouldn't spawn Boom slimes when killed by explosions
    SpawnSlimes([SlimyboiType.Boom]);
  }

  public static void OnAnyPlayerCollectedHealth(HealthPickup health, PlayerController player)
  {
    if (health.armorAmount > 0)
      SpawnSlimes([SlimyboiType.Rock], player: player);
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
      SpawnSlimes(Enumerable.Repeat(SlimyboiType.Lucky, slimesToSpawn).ToList(), player: player);
  }

  public static void HandleTableFlip(FlippableCover table)
  {
    if (!_Instance)
      return;
    SpawnSlimes([SlimyboiType.Saber], room: table.transform.position.GetAbsoluteRoom());
  }

  public static void OnMinorBreakableShattered(MinorBreakable breakable)
  {
    if (!_Instance)
      return;
    // if (breakable.explodesOnBreak) //TODO: undecided if we want this or not
    // {
    //   SpawnSlimes([SlimyboiType.Boom], room: breakable.transform.position.GetAbsoluteRoom());
    //   return;
    // }
    if (!breakable.goopsOnBreak || breakable.goopType is not GoopDefinition def)
      return;
    if (def.AppliesDamageOverTime)
      SpawnSlimes([SlimyboiType.Rad], room: breakable.transform.position.GetAbsoluteRoom());
    else if (def.UsesGreenFire)
      SpawnSlimes([SlimyboiType.Fire, SlimyboiType.Fire], room: breakable.transform.position.GetAbsoluteRoom());
    else if (def.isOily)
      SpawnSlimes([SlimyboiType.Fire], room: breakable.transform.position.GetAbsoluteRoom());
    else if (def.CanBeElectrified || def.CanBeFrozen)
      SpawnSlimes([SlimyboiType.Puddle, SlimyboiType.Puddle, SlimyboiType.Puddle], room: breakable.transform.position.GetAbsoluteRoom());
  }

  public static void OnWillPickUpAnyPassive(PlayerController player, PickupObject pickup)
  {
    if (pickup is IounStoneOrbitalItem guon && !guon.m_pickedUpThisRun && (guon.BreaksUponContact || guon.BreaksUponOwnerDamage))
      SpawnSlimes([SlimyboiType.Mosaic], player: player, room: player.CurrentRoom);
  }
}
