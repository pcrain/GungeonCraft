namespace CwaffingTheGungy;

[HarmonyPatch]
internal static class ArmisticePatches
{
  private class DidArmisticeDialogue : MonoBehaviour {}

  private static bool StartArmisticeDialogue(TalkDoerLite talker, bool didBulletPickup)
  {
      if (talker.gameObject.GetComponent<DidArmisticeDialogue>())
        return false;
      talker.gameObject.AddComponent<DidArmisticeDialogue>();
      List<string> questionDialogue = didBulletPickup
        ? new(){
          "...hey... ",
          "You've already killed your past, right?",
          "Why do you need this bullet?",
          "Do you still have lingering regrets?" }
        : new(){
          "...hey... ",
          "You're not here to kill your past, right?",
          "Why do you need this bullet?",
          "Are you seeking something else?"
        };
      talker.AddNewDialogState("pastRegrets",
        dialogue: questionDialogue,
        yesPrompt : didBulletPickup ? "A few." : "No.", // NOTE: confusing naming...
        yesState  : "affirmative",
        noPrompt  : didBulletPickup ? "None at all." : "Yes.",
        noState   : "negative");
      talker.AddNewDialogState("affirmative", new(){"I see.", "Off you go then."});
      talker.AddNewDialogState("negative",
        customAction: () => {
          if (!didBulletPickup)
            GameManager.Instance.BestActivePlayer.AcquirePassiveItemPrefabDirectly(Items.BulletThatCanKillThePast.AsPassive());
          CwaffRunData.Instance.noPastRegrets = true;
          Lazy.DebugLog($"CwaffRunData.Instance.noPastRegrets = {CwaffRunData.Instance.noPastRegrets}");
        },
        dialogue: new(){
          "Interesting... ",
          "The {wj}gun{w} and {wj}bullet{w} give those with regrets a chance to change their past.",
          "If you truly have no regrets, what will happen when you fire the {wj}gun{w}?",
          "... ",
          "Off you go then.",
        });
      talker.StartDialog("pastRegrets");
      return true;
  }

  /// <summary>Add dynamic dialog to Blacksmith if preconditions are met.</summary>
  [HarmonyPatch(typeof(BulletThatCanKillThePast), nameof(BulletThatCanKillThePast.Pickup))]
  [HarmonyPrefix]
  private static void BulletThatCanKillThePastPickupPatch(BulletThatCanKillThePast __instance, PlayerController player)
  {
    if (!GungeonFlags.BOSSKILLED_LICH.Get())
      return;
    // if (!GungeonFlags.HAS_ATTEMPTED_RESOURCEFUL_RAT.Get())
    //   return;
    if (!CharacterSpecificGungeonFlags.KILLED_PAST.Get())
      return;
    if (__instance.m_pickedUp)
      return;
    if (player.CurrentRoom is not RoomHandler room)
      return;
    foreach (var ix in room.GetRoomInteractables())
    {
      if (ix is not TalkDoerLite talker || !talker || talker.name != "NPC_Blacksmith")
        continue;
      StartArmisticeDialogue(talker, didBulletPickup: true);
      break;
    }
  }

  /// <summary>Add dynamic dialog to Blacksmith for characters that can't normally pick up the BTCKTP</summary>
  [HarmonyPatch(typeof(TalkDoerLite), nameof(TalkDoerLite.Interact))]
  [HarmonyPrefix]
  private static bool TalkDoerLiteInteractPatch(TalkDoerLite __instance, PlayerController interactor)
  {
    if (__instance.name != "NPC_Blacksmith" || !interactor.AllowAlternateArmisticeAccess() || !StartArmisticeDialogue(__instance, didBulletPickup: false))
      return true;
    return false;
  }

  /// <summary>Allow the GTCKTP to take us to the secret area</summary>
  [HarmonyPatch(typeof(ArkController), nameof(ArkController.HandleClockhair), MethodType.Enumerator)]
  [HarmonyILManipulator]
  private static void ArkControllerHandleClockhairPatchIL(ILContext il, MethodBase original)
  {
      ILCursor cursor = new ILCursor(il);

      FieldInfo didShootHellTrigger = original.DeclaringType.GetEnumeratorField("didShootHellTrigger");
      if (!cursor.TryGotoNext(MoveType.After,
        instr => instr.MatchLdarg(0),
        instr => instr.MatchLdarg(0),
        instr => instr.MatchLdfld(original.DeclaringType.FullName, didShootHellTrigger.Name)
        ))
        return;

      // do quick fadeout if we are going to the secret area
      cursor.CallPrivate(typeof(ArmisticePatches), nameof(NoPastRegrets));

      // add new branch to go to our new floor
      ILLabel nextPastCheck = null;
      ILLabel afterAllPastChecks = null;
      if (!cursor.TryGotoNext(MoveType.Before,
        instr => instr.MatchBr(out afterAllPastChecks),
        instr => instr.MatchLdarg(0),
        instr => instr.MatchLdfld(original.DeclaringType.FullName, original.DeclaringType.GetEnumeratorField("shotPlayer").Name),
        instr => instr.MatchLdfld<PlayerController>("characterIdentity"),
        instr => instr.MatchLdcI4((int)PlayableCharacters.CoopCultist),
        instr => instr.MatchBneUn(out nextPastCheck)
        ))
        return;

      if (!cursor.TryGotoNext(MoveType.AfterLabel, instr => instr.MatchLdarg(0)))
        return;

      cursor.Emit(OpCodes.Ldarg_0); // load enumerator type
      cursor.Emit(OpCodes.Ldfld, original.DeclaringType.GetEnumeratorField("$this")); // load actual "$this" field
      cursor.CallPrivate(typeof(ArmisticePatches), nameof(CheckIfShouldGoToNoRegretsPast));
      cursor.Emit(OpCodes.Brtrue, afterAllPastChecks);
  }

  private static bool AllowAlternateArmisticeAccess(this PlayerController player)
  {
    if (!GungeonFlags.BOSSKILLED_LICH.Get())
      return false; // nobody should be able to access Armistice until the Lich has been defeated
    if (player.characterIdentity == PlayableCharacters.Gunslinger)
      return false; // Gunslinger cannot access Armistice for lore reasons
    if (player.characterIdentity == PlayableCharacters.Eevee)
      return GungeonFlags.GUNSLINGER_UNLOCKED.Get(); // Paradox can access Armistice if Gunslinger is unlocked
    if ((int)player.characterIdentity < (int)PlayableCharacters.Eevee)
      return false; // vanilla characters with pasts have to pick up the BTCKTP with no regrets
    if (player.gameObject.GetComponent<CustomCharacter>() is not CustomCharacter cc)
      return true; // non CharAPI custom characters without custom pasts get free access
    return !cc.hasPast; // only CharAPI characters without custom pasts get free access, all others require the BTCKTP route
  }

  private static bool NoPastRegrets(bool oldValue) {
    return oldValue || CwaffRunData.Instance.noPastRegrets;
  }

  private static bool CheckIfShouldGoToNoRegretsPast(ArkController ark)
  {
      if (!CwaffRunData.Instance.noPastRegrets)
        return false;

      Lazy.DebugConsoleLog($"  secrets O:");

      for (int i = 0; i < GameManager.Instance.AllPlayers.Length; i++)
      {
        PlayerController pc = GameManager.Instance.AllPlayers[i];
        if (!pc.healthHaver.IsAlive)
          continue;

        pc.IsVisible = true;
        pc.ClearInputOverride("ark");
        pc.ClearAllInputOverrides();
      }

      GameManager.Instance.LoadCustomLevel(ArmisticeDungeon.INTERNAL_NAME);
      GameUIRoot.Instance.ToggleUICamera(false);
      return true;
  }

  [HarmonyPatch(typeof(RoomHandler), nameof(RoomHandler.SealRoom))]
  [HarmonyPrefix]
  private static void MaybeSpawnMoreEnemies(RoomHandler __instance, ref bool __state)
  {
    __state = !__instance.m_isSealed; // __state should be true iff the room is actually transitioning to sealed
  }

  private static readonly List<Tuple<string, int>> WeightedEnemies = [
    new(Enemies.BulletKin, 60),
    new(Enemies.BandanaBulletKin, 30),
    new(Enemies.VeteranBulletKin, 10),
    new(Enemies.MutantBulletKin, 7),
    new(Enemies.SniperShell, 10),
    new(Enemies.Professional, 5),
    new(Enemies.Hollowpoint, 12),
    new(Enemies.Blobulon, 20),
    new(Enemies.Poisbulon, 9),
    new(Enemies.Gigi, 15),
    new(Enemies.Beadie, 15),
    new(Enemies.Cardinal, 10),
    new(Enemies.GunNut, 8),
    new(Enemies.SpectralGunNut, 5),
    new(Enemies.Gunzookie, 30),
    new(Enemies.Gunzockie, 10),
    new(Enemies.Revolvenant, 3),
    new(Enemies.Det, 11),
    new(Enemies.DynamiteKin, 21),
    new(Enemies.GrenadeKin, 21),
    new(Enemies.Bookllet, 30),
    new(Enemies.BlueBookllet, 10),
    new(Enemies.GreenBookllet, 8),
    new(Enemies.RedShotgunKin, 15),
    new(Enemies.BlueShotgunKin, 15),
    new(Enemies.ApprenticeGunjurer, 50),
    new(Enemies.Gunjurer, 10),
    new(Enemies.HighGunjurer, 5),
    new(Enemies.LoreGunjurer, 5),
    new(Enemies.Wizbang, 5),
    new(Enemies.LeadMaiden, 2),
  ];

  [HarmonyPatch(typeof(RoomHandler), nameof(RoomHandler.SealRoom))]
  [HarmonyPostfix]
  private static void SpawnMoreEnemies(RoomHandler __instance, bool __state)
  {
    if (!__state)
      return; // room wasn't actually sealed

    // #if !DEBUG
    if (GameManager.Instance.Dungeon.tileIndices.tilesetId != GlobalDungeonData.ValidTilesets.HELLGEON)
      return;
    if (!CwaffRunData.Instance.scrambledBulletHell)
      return;
    // #endif

    const int MinEnemiesToSpawn = 3;
    const int MaxEnemiesToSpawn = 10;
    PlayerController user = GameManager.Instance.BestActivePlayer;
    RoomHandler room = user.CurrentRoom;

    if (room == null || room.area.PrototypeRoomCategory == PrototypeDungeonRoom.RoomCategory.BOSS)
      return;

    IntVector2 targetCenter = room.GetCenteredVisibleClearSpot(2, 2, out bool success);
    if (!success)
    {
      Lazy.DebugWarn("could not find a flood fill spot");
      return;
    }

    FloodFillUtility.PreprocessContiguousCells(room, targetCenter);
    int num = 0;
    int NumEnemiesToSpawn = Mathf.Clamp(Mathf.Min(room.area.dimensions.x, room.area.dimensions.y) / 2, MinEnemiesToSpawn, MaxEnemiesToSpawn);
    Lazy.DebugLog($"spawning {NumEnemiesToSpawn} enemies");
    for (int i = 0; i < NumEnemiesToSpawn; i++)
    {
      string guid = WeightedEnemies.WeightedRandom();
      AIActor enemyPrefab = EnemyDatabase.GetOrLoadByGuid(guid);
      bool checkContiguous = true;
      Pathfinding.CellValidator cellValidator = (IntVector2 c) => {
        if (checkContiguous && !FloodFillUtility.WasFilled(c))
          return false;
        for (int k = 0; k < enemyPrefab.Clearance.x; k++)
        {
          for (int l = 0; l < enemyPrefab.Clearance.y; l++)
          {
            if (GameManager.Instance.Dungeon.data.isTopWall(c.x + k, c.y + l))
              return false;
            if (IntVector2.Distance(targetCenter, c.x + k, c.y + l) < 4f)
              return false;
            // if (IntVector2.Distance(targetCenter, c.x + k, c.y + l) > 20f)
            //   return false;
          }
        }
        return true;
      };
      checkContiguous = true;
      IntVector2? randomAvailableCell = room.GetRandomAvailableCell(enemyPrefab.Clearance, enemyPrefab.PathableTiles, canPassOccupied: false, cellValidator);
      if (!randomAvailableCell.HasValue)
      {
        checkContiguous = false;
        randomAvailableCell = room.GetRandomAvailableCell(enemyPrefab.Clearance, enemyPrefab.PathableTiles, canPassOccupied: false, cellValidator);
      }
      if (randomAvailableCell.HasValue)
      {
        AIActor aIActor = AIActor.Spawn(enemyPrefab, randomAvailableCell.Value, room, correctForWalls: true);
        Lazy.DebugLog($"  spawning a {aIActor.AmmonomiconName()}");
        num++;
        aIActor.HandleReinforcementFallIntoRoom();
      }
    }
    if (num <= 0)
      return;
    if (room.area.runtimePrototypeData != null)
    {
      bool flag2 = false;
      for (int j = 0; j < room.area.runtimePrototypeData.roomEvents.Count; j++)
      {
        RoomEventDefinition roomEventDefinition = room.area.runtimePrototypeData.roomEvents[j];
        if (roomEventDefinition.condition == RoomEventTriggerCondition.ON_ENEMIES_CLEARED && roomEventDefinition.action == RoomEventTriggerAction.UNSEAL_ROOM)
          flag2 = true;
      }
      if (!flag2)
        room.area.runtimePrototypeData.roomEvents.Add(new RoomEventDefinition(RoomEventTriggerCondition.ON_ENEMIES_CLEARED, RoomEventTriggerAction.UNSEAL_ROOM));
    }
    room.SealRoom();
  }
}
