namespace CwaffingTheGungy;

public class Itemfinder : CwaffActive
{
    public static string ItemName         = "Itemfinder";
    public static string ShortDescription = "Scavenger Hunt";
    public static string LongDescription  = "Beeps when near hidden treasure. Using near hidden treasure uncovers an item or gun with varying rarity.";
    public static string Lore             = "Ox and Cadence commissioned the development of this handy little gadget for helping them find wares to sell at their shop. Their inventory has expanded considerably since they switched over from using a traditional metal detector, which had the unfortunate habit of going off around just about everything in the Gungeon. As this included the Gundead themselves, Ox and Cadence's medical expenses have also gone down considerably since foregoing the metal detector.";

    // Chance for getting at least 1, 2, 3, or 4 treasures per floor
    internal readonly float[] _TREASURE_CHANCES = {1.00f, 0.50f, 0.20f, 0.05f};
    // Chance each individual treasure is at least D, C, B, A, or S tier
    internal readonly float[] _QUALITY_CHANCES  = {1.00f, 0.60f, 0.27f, 0.09f, 0.03f};

    internal const float _CLOSE_DISTANCE = 3f; // Distance in tiles from an item location to be considered "close"
    internal const float _CLOSE_DISTANCE_SQR = _CLOSE_DISTANCE * _CLOSE_DISTANCE;
    internal const int   _MAX_CELL_DIST = 5;   // Max number of cells to look when looking for a good hiding location

    internal static List<RoomHandler> _RoomsOnFloor = null;
    internal static List<TreasureLocation> _Treasure = null;
    internal static float _LastSoundPlayTime = 0f;
    internal static int _NormalId;
    internal static int _BlinkId;

    public static void Init()
    {
        PlayerItem item = Lazy.SetupActive<Itemfinder>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.A;
        item.consumable   = false;
        item.SetCooldownType(ItemBuilder.CooldownType.Timed, 1f);

        _NormalId = item.sprite.spriteId;
        _BlinkId  = item.sprite.collection.GetSpriteIdByName("itemfinder_blink_icon");
    }

    public override void OnFirstPickup(PlayerController player)
    {
        base.OnFirstPickup(player);
        InitializeTreasureForFloor();
    }

    public override void Pickup(PlayerController player)
    {
        GameManager.Instance.OnNewLevelFullyLoaded -= InitializeTreasureForFloor; //TODO: look into whether this is necessary or not
        GameManager.Instance.OnNewLevelFullyLoaded += InitializeTreasureForFloor;
        base.Pickup(player);
    }

    public override void OnPreDrop(PlayerController player)
    {
        GameManager.Instance.OnNewLevelFullyLoaded -= InitializeTreasureForFloor;
        base.OnPreDrop(player);
    }

    public override void OnDestroy()
    {
        GameManager.Instance.OnNewLevelFullyLoaded -= InitializeTreasureForFloor;
        base.OnDestroy();
    }

    public override bool CanBeUsed(PlayerController user)
    {
        bool nearAnyTreasure = false;
        foreach (TreasureLocation t in _Treasure)
        {
            float sqrdist = (t.location - user.CenterPosition).sqrMagnitude;
            if (sqrdist < _CLOSE_DISTANCE_SQR)
            {
                nearAnyTreasure = true;
                break;
            }
        }
        if (nearAnyTreasure && (BraveTime.ScaledTimeSinceStartup - _LastSoundPlayTime) > 1f)
        {
            base.sprite.SetSprite(_BlinkId);
            user.gameObject.Play("itemfinder_sound");
            _LastSoundPlayTime = BraveTime.ScaledTimeSinceStartup;
        }
        if ((BraveTime.ScaledTimeSinceStartup - _LastSoundPlayTime) > 0.5f)
            base.sprite.SetSprite(_NormalId);
        return nearAnyTreasure && base.CanBeUsed(user);
    }

    public override void DoEffect(PlayerController user)
    {
        TreasureLocation nearestTreasure = null;
        float nearestSqrDist = _CLOSE_DISTANCE_SQR;
        foreach (TreasureLocation t in _Treasure)
        {
            float sqrdist = (t.location - user.CenterPosition).sqrMagnitude;
            if (sqrdist < nearestSqrDist)
            {
                nearestSqrDist = sqrdist;
                nearestTreasure = t;
            }
        }
        if (!nearestTreasure)
            return;

        LootEngine.SpawnItem(nearestTreasure.pickup.gameObject, nearestTreasure.location, Vector2.zero, 0f, true, true, false);
        user.gameObject.Play("itemfinder_get_item");

        _Treasure.Remove(nearestTreasure);
        UnityEngine.GameObject.Destroy(nearestTreasure.gameObject);
    }

    private void InitializeTreasureForFloor()
    {
        if (_Treasure != null)
            _Treasure.Clear();
        _Treasure = new();

        _RoomsOnFloor = GameManager.Instance.Dungeon.data.rooms.CopyAndShuffle();

        float treasureVal = UnityEngine.Random.value;
        for (int i = 0; i < _TREASURE_CHANCES.Length; ++i)
        {
            if (treasureVal > _TREASURE_CHANCES[i])
                break;
            ItemQuality q;
            float treasureQual = UnityEngine.Random.value;
            if      (treasureQual < _QUALITY_CHANCES[4]) q = ItemQuality.S;
            else if (treasureQual < _QUALITY_CHANCES[3]) q = ItemQuality.A;
            else if (treasureQual < _QUALITY_CHANCES[2]) q = ItemQuality.B;
            else if (treasureQual < _QUALITY_CHANCES[1]) q = ItemQuality.C;
            else                                         q = ItemQuality.D;
            // ETGModConsole.Log($"Seeding treasure {i+1} with quality {q}");
            InitializeSingleTreasureOfQuality(q);
        }
    }

    private bool IsViableTreasureRoom(RoomHandler room)
    {
        if (room == null || room.area == null)
            return false;
        if (room.area.PrototypeRoomCategory != PrototypeDungeonRoom.RoomCategory.NORMAL && room.area.PrototypeRoomCategory != PrototypeDungeonRoom.RoomCategory.HUB)
            return false;
        if (room.area.IsProceduralRoom || room.area.proceduralCells != null)
            return false;
        if (room.GetRandomVisibleClearSpot(_MAX_CELL_DIST, _MAX_CELL_DIST) == IntVector2.Zero)
            return false;
        return true;
    }

    private RoomHandler GetNextTreasureRoom()
    {
        for(int i = _RoomsOnFloor.Count - 1; i >= 0; --i)
        {
            RoomHandler room = _RoomsOnFloor[i];
            _RoomsOnFloor.RemoveAt(i);
            if (IsViableTreasureRoom(room))
                return room;
        }
        return null;
    }

    private void InitializeSingleTreasureOfQuality(ItemQuality quality)
    {
        RoomHandler room = GetNextTreasureRoom();
        if (room == null)
        {
            ETGModConsole.Log($"failed to initalize treasure D:");
            return;
        }

        IntVector2 spot = room.GetRandomVisibleClearSpot(_MAX_CELL_DIST, _MAX_CELL_DIST);
        if (spot == IntVector2.Zero)
        {
            ETGModConsole.Log($"failed to get a good spot for treasure D:");
            return;
        }

        // ETGModConsole.Log($"Adding treasure to room {room.GetRoomName()} -> {room.area.basePosition}");
        _Treasure.Add(TreasureLocation.Create(quality, spot.ToVector2()));
    }
}

public class TreasureLocation : MonoBehaviour
{
    public PickupObject pickup = null;
    public Vector2 location = Vector2.zero;

    public static TreasureLocation Create(ItemQuality quality, Vector2 location)
    {
        GameObject g = new GameObject();
            g.transform.position = location;
        TreasureLocation t = g.AddComponent<TreasureLocation>();
            t.pickup = LootEngine.GetItemOfTypeAndQuality<PickupObject>(quality, null, false);
            t.location = location;
        return t;
    }
}
