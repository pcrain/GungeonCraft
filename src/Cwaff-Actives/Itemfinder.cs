namespace CwaffingTheGungy;

public class Itemfinder : PlayerItem
{
    public static string ItemName         = "Itemfinder";
    public static string SpritePath       = "itemfinder_icon";
    public static string ShortDescription = "Scavenger Hunt";
    public static string LongDescription  = "Beeps when near hidden treasure. Using near hidden treasure uncovers an item or gun with varying rarity.\n\nOx and Cadence commissioned the development of this handy little gadget for helping them find wares to sell at their shop. Their inventory has expanded considerably since they switched over from using a traditional metal detector, which had the unfortunate habit of going off around just about everything in the Gungeon. As this included the Gundead themselves, Ox and Cadence's medical expenses have also gone down considerably since foregoing the metal detector.";

    // Chance for getting at least 0, 1, 2, 3, 4, or 5 treasures per floor
    internal readonly float[] _TREASURE_CHANCES = {1.00f, 1.00f, 1.00f, 0.50f, 0.20f, 0.05f};
    // Chance each individual treasure is at least D, C, B, A, or S tier
    internal readonly float[] _QUALITY_CHANCES  = {1.00f, 0.60f, 0.27f, 0.09f, 0.03f};

    internal const float _CLOSE_DISTANCE = 3f; // Distance in tiles from an item location to be considered "close"
    internal const int   _MAX_CELL_DIST = 5;   // Max number of cells to look when looking for a good hiding location

    internal static List<RoomHandler> _RoomsOnFloor = null;
    internal static List<TreasureLocation> _Treasure = null;
    internal static float _LastSoundPlayTime = 0f;

    public static void Init()
    {
        PlayerItem item = Lazy.SetupActive<Itemfinder>(ItemName, SpritePath, ShortDescription, LongDescription);
        item.quality      = PickupObject.ItemQuality.A;
        item.consumable   = false;
        item.CanBeDropped = true;
        item.SetCooldownType(ItemBuilder.CooldownType.Timed, 1f);
    }

    public override void Pickup(PlayerController player)
    {
        if (!this.m_pickedUpThisRun)
            InitializeTreasureForFloor();
        GameManager.Instance.OnNewLevelFullyLoaded += InitializeTreasureForFloor;
        base.Pickup(player);
    }

    public override void OnPreDrop(PlayerController player)
    {
        GameManager.Instance.OnNewLevelFullyLoaded -= InitializeTreasureForFloor;
        base.OnPreDrop(player);
    }

    public override bool CanBeUsed(PlayerController user)
    {
        bool nearAnyTreasure = false;
        foreach (TreasureLocation t in _Treasure)
        {
            float dist = (t.location - user.sprite.WorldCenter).magnitude;
            if (dist < _CLOSE_DISTANCE)
            {
                nearAnyTreasure = true;
                break;
            }
        }
        if (nearAnyTreasure && (BraveTime.ScaledTimeSinceStartup - _LastSoundPlayTime) > 1f)
        {
            AkSoundEngine.PostEvent("itemfinder_sound", user.gameObject);
            _LastSoundPlayTime = BraveTime.ScaledTimeSinceStartup;
        }
        return nearAnyTreasure && base.CanBeUsed(user);
    }

    public override void DoEffect(PlayerController user)
    {
        TreasureLocation nearestTreasure = null;
        float nearestDist = _CLOSE_DISTANCE;
        foreach (TreasureLocation t in _Treasure)
        {
            float dist = (t.location - user.sprite.WorldCenter).magnitude;
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearestTreasure = t;
            }
        }
        if (!nearestTreasure)
            return;

        LootEngine.SpawnItem(nearestTreasure.pickup.gameObject, nearestTreasure.location, Vector2.zero, 0f, true, true, false);
        AkSoundEngine.PostEvent("itemfinder_get_item", user.gameObject);

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
        for (int i = 0; i < _TREASURE_CHANCES.Count(); ++i)
        {
            if (treasureVal > _TREASURE_CHANCES[i])
                break;
            PickupObject.ItemQuality q;
            float treasureQual = UnityEngine.Random.value;
            if      (treasureQual < _QUALITY_CHANCES[4]) q = PickupObject.ItemQuality.S;
            else if (treasureQual < _QUALITY_CHANCES[3]) q = PickupObject.ItemQuality.A;
            else if (treasureQual < _QUALITY_CHANCES[2]) q = PickupObject.ItemQuality.B;
            else if (treasureQual < _QUALITY_CHANCES[1]) q = PickupObject.ItemQuality.C;
            else                                         q = PickupObject.ItemQuality.D;
            // ETGModConsole.Log($"Seeding treasure {i+1} with quality {q}");
            InitializeSingleTreasureOfQuality(q);
        }
    }

    private bool IsViableTreasureRoom(RoomHandler room)
    {
        if (room?.area == null)
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

    private void InitializeSingleTreasureOfQuality(PickupObject.ItemQuality quality)
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

    public static TreasureLocation Create(PickupObject.ItemQuality quality, Vector2 location)
    {
        GameObject g = new GameObject();
            g.transform.position = location;
        TreasureLocation t = g.AddComponent<TreasureLocation>();
            t.pickup = LootEngine.GetItemOfTypeAndQuality<PickupObject>(quality, null, false);
            t.location = location;
        return t;
    }
}
