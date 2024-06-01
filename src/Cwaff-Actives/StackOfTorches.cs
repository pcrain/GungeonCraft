namespace CwaffingTheGungy;

public class StackOfTorches : CwaffActive
{
    public static string ItemName         = "Stack of Torches";
    public static string ShortDescription = "Back in the Mines";
    public static string LongDescription  = "Places a torch in front of the player, which has several effects: 1) Placing a torch guarantees at most one additional wave of enemies will spawn in the current room. 2) The first four torches placed in a room have a 25%, 50%, 75%, and 100% chance of preventing ALL additional waves of enemies from spawning. 3) Each placed torch has a 60% chance of brightening up a room under the darkness effect. 4) Each placed torch increases the chance of finding treasure on room clear by 5%, with 20 torches guaranteeing a chest.";
    public static string Lore             = "An absolute staple in any adventurer's inventory. These state-of-the-art torches come pre-bundled, pre-lit, pre-mounted, and pre-used, having been yanked straight off the Gungeon's walls. Their lack of resilience towards bullets is matched only by their embarrassingly high susceptibility to singular drops of water. Even so, their warm, radiant glow provides a reassuring sense of safety.";

    private const float _MAX_WALL_DIST           = 1.6f;
    private const float _CHANCE_TO_END_DARK_ROOM = 0.60f;
    private const float _REWARD_CHANCE_PER_TORCH = 0.05f;

    private static GameObject _TorchPrefab       = null;
    private static GameObject _TorchPurplePrefab = null;
    private static GameObject _TorchBluePrefab   = null;
    private static GameObject _TorchSidePrefab   = null;
    private static GameObject _SconcePrefab      = null;
    // private static GameObject _LanternPrefab  = null;
    private static List<GameObject> _Torches     = null;
    private static Dictionary<RoomHandler, int> _TorchesInRoom = new();

    private PlayerController _owner              = null;

    public static void Init()
    {
        PlayerItem item = Lazy.SetupActive<StackOfTorches>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality    = ItemQuality.D;
        item.AddToSubShop(ModdedShopType.Rusty);

        ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.None, 0.1f);
        item.consumable   = true;
        item.numberOfUses = 64;
        item.CanBeDropped = true;

        _TorchPrefab       = Dissect.FindDefaultResource("DefaultTorch");
        _TorchPurplePrefab = Dissect.FindDefaultResource("DefaultTorchPurple");
        _TorchBluePrefab   = Dissect.FindDefaultResource("DefaultTorchBlue");
        _TorchSidePrefab   = Dissect.FindDefaultResource("DefaultTorchSide");
        _SconcePrefab      = Dissect.FindDefaultResource("Sconce_Light");
        // _LanternPrefab = ResourceCache.Acquire("Global Prefabs/Shrine_Lantern") as GameObject;
        _Torches = new(){
            _TorchPrefab,
            _TorchPurplePrefab,
            _TorchBluePrefab,
            _TorchSidePrefab,
            // _LanternPrefab,
        };
    }

    private static void SpawnAdditionalRoomRewardBasedOnTorchCount(RoomHandler room)
    {
        if (!_TorchesInRoom.ContainsKey(room))
            return;

        float rewardChance = _REWARD_CHANCE_PER_TORCH * _TorchesInRoom[room];
        if (UnityEngine.Random.value > rewardChance)
            return;

        IntVector2 bestRewardLocation = room.GetBestRewardLocation(new IntVector2(2, 1));
        if (GameStatsManager.Instance.IsRainbowRun)
            LootEngine.SpawnBowlerNote(GameManager.Instance.RewardManager.BowlerNoteChest, bestRewardLocation.ToCenterVector2(), room, true);
        else
            room.SpawnRoomRewardChest(null, bestRewardLocation)?.ForceUnlock();
    }

    public override void DoEffect(PlayerController user)
    {
        // Do some calculations to figure out whether we should place our torch on the floor or on the wall
        float gunAngle    = user.m_currentGunAngle;
        Vector2 target    = Raycast.ToNearestWall(user.sprite.WorldCenter, gunAngle);
        Vector2 delta     = (target - user.sprite.WorldCenter);
        bool placeOnFloor = delta.magnitude > _MAX_WALL_DIST;

        // Actually create the torch
        if (placeOnFloor)
        {
            _Torches.ChooseRandom().Instantiate(position: user.sprite.WorldCenter + _MAX_WALL_DIST * delta.normalized, anchor: Anchor.MiddleCenter);
            base.gameObject.Play("mc_torch_place");
        }
        else
        {
            _SconcePrefab.Instantiate(position: target, anchor: Anchor.LowerCenter);
            base.gameObject.Play("mc_lantern_place");
        }

        // Nothing else to do if current room is invalid
        if (user.CurrentRoom is not RoomHandler room)
            return;

        // Count up the number of torches we've placed in the current room
        if (!_TorchesInRoom.ContainsKey(room))
            _TorchesInRoom[room] = 0;
        int roomTorches = ++_TorchesInRoom[room];

        // Once we've placed at least one torch, we should have at most one remaining wave of reinforcements
        int numReinforcements = (room.remainingReinforcementLayers?.Count ?? 0);
        while(numReinforcements > 1)
            room.remainingReinforcementLayers.RemoveAt(--numReinforcements);

        // It should take anywhere between 1-4 total torches to remove the remaining reinforcement waves for a room
        if (numReinforcements > 0 && (0.25f * roomTorches) >= UnityEngine.Random.value)
            room.ClearReinforcementLayers();

        // Each torch should also have a 25% chance or so to remove the darkness effect
        if (room.IsDarkAndTerrifying && UnityEngine.Random.value <= _CHANCE_TO_END_DARK_ROOM)
            room.EndTerrifyingDarkRoom();

        // Finally, the first placed torch should add an event handler to possibly spawn treasure on room clear
        if (roomTorches == 1)
            room.OnEnemiesCleared += () => SpawnAdditionalRoomRewardBasedOnTorchCount(room);
    }
}
