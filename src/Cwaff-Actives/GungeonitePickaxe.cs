namespace CwaffingTheGungy;

public class GungeonitePickaxe : PlayerItem
{
    public static string ItemName         = "Gungeonite Pickaxe";
    public static string SpritePath       = "gungeonite_pickaxe_icon";
    public static string ShortDescription = "So We Back in the Mines";
    public static string LongDescription  = "TBD";

    internal static VFXPool _VFXDustPoof;
    private static int _PickaxeId;

    private const float _MAX_DIST = 5f;

    // my compiler REALLY doesn't like Actions with 6 parameter types, so declaring this separately;
    public delegate void FloorEdgeBorderDelegate(TK2DDungeonAssembler assembler, CellData cellData, Dungeon dungeon, tk2dTileMap map, int x, int y);

    public static void Init()
    {
        PlayerItem item = Lazy.SetupActive<GungeonitePickaxe>(ItemName, SpritePath, ShortDescription, LongDescription);
        item.quality      = PickupObject.ItemQuality.D;
        item.consumable   = false;
        item.CanBeDropped = true;
        item.SetCooldownType(ItemBuilder.CooldownType.Timed, 0.5f);

        _VFXDustPoof = (ItemHelper.Get(Items.Drill) as PlayerItem).GetComponent<PaydayDrillItem>().VFXDustPoof;

        _PickaxeId = item.PickupObjectId;

        // new Hook(
        //     typeof(TK2DDungeonAssembler).GetMethod("BuildFloorEdgeBorderTiles", BindingFlags.Instance | BindingFlags.NonPublic),
        //     typeof(GungeonitePickaxe).GetMethod("BuildFloorEdgeBorderTilesSanityCheck", BindingFlags.Static | BindingFlags.NonPublic)
        //     );
    }

    private static void BuildFloorEdgeBorderTilesSanityCheck(FloorEdgeBorderDelegate orig, TK2DDungeonAssembler assembler, CellData current, Dungeon d, tk2dTileMap map, int ix, int iy)
    {
        // if (assembler == null)
        //     ETGModConsole.Log($"NULL ASSEMBLER");
        // if (current == null)
        //     ETGModConsole.Log($"NULL CELLDATA");
        // if (map == null)
        //     ETGModConsole.Log($"NULL TILEMAP");
        // // if (d.roomMaterialDefinitions[current.cellVisualData.roomVisualTypeIndex].roomFloorBorderGrid == null)
        // //     ETGModConsole.Log($"NULL BORDER GRID");
        if (GameManager.Instance.AnyPlayerHasPickupID(_PickaxeId))
            MyBuildFloorEdgeBorderTiles(assembler, current, d, map, ix, iy);
        else
            orig(assembler, current, d, map, ix, iy);
    }

    private static void MyBuildFloorEdgeBorderTiles(TK2DDungeonAssembler assembler, CellData current, Dungeon d, tk2dTileMap map, int ix, int iy)
    {
        if (current.type != CellType.FLOOR && !d.data.isFaceWallLower(ix, iy))
        {
            return;
        }
        TileIndexGrid tileIndexGrid = d.roomMaterialDefinitions[current.cellVisualData.roomVisualTypeIndex].roomFloorBorderGrid;
        if (d.tileIndices.tilesetId == GlobalDungeonData.ValidTilesets.WESTGEON && current.cellVisualData.IsFacewallForInteriorTransition)
        {
            tileIndexGrid = d.roomMaterialDefinitions[current.cellVisualData.InteriorTransitionIndex].exteriorFacadeBorderGrid;
        }
        if (!(tileIndexGrid != null))
        {
            return;
        }
        if (current.diagonalWallType == DiagonalWallType.NONE || !d.data.isFaceWallLower(ix, iy))
        {
            List<CellData> cellNeighbors = d.data.GetCellNeighbors(current, true);
            bool[] array = new bool[8];
            for (int i = 0; i < array.Length; i++)
            {
                if (cellNeighbors[i] != null)
                {
                    array[i] = cellNeighbors[i].type == CellType.WALL && !d.data.isTopWall(cellNeighbors[i].position.x, cellNeighbors[i].position.y + 1) && cellNeighbors[i].diagonalWallType == DiagonalWallType.NONE;
                    ETGModConsole.Log($"  starting neighbor at {cellNeighbors[i].position}");
                    ETGModConsole.Log($"    checking neighbor's neighbor at {cellNeighbors[i].position + IntVector2.Up}");
                    bool flag = cellNeighbors[i].isSecretRoomCell || (d.data[cellNeighbors[i].position + IntVector2.Up].IsTopWall() && d.data[cellNeighbors[i].position + IntVector2.Up].isSecretRoomCell);
                    ETGModConsole.Log($"  finishing neighbor {cellNeighbors[i].position}");
                    array[i] = array[i] || flag != current.isSecretRoomCell;
                }
            }
            int indexGivenEightSides = tileIndexGrid.GetIndexGivenEightSides(array);
            if (indexGivenEightSides != -1)
            {
                map.Layers[GlobalDungeonData.decalLayerIndex].SetTile(current.positionInTilemap.x, current.positionInTilemap.y, indexGivenEightSides);
            }
        }
        else
        {
            int indexByWeight = tileIndexGrid.quadNubs.GetIndexByWeight();
            if (indexByWeight != -1)
            {
                map.Layers[GlobalDungeonData.decalLayerIndex].SetTile(current.positionInTilemap.x, current.positionInTilemap.y, indexByWeight);
            }
        }
        // ETGModConsole.Log($"    DONE");
    }

    private static void AddNeighboringWalls(Dungeon d, RoomHandler r, IntVector2 pos, int timesToRecurse = 1)
    {
        List<IntVector2> neighborDirs = new();
            neighborDirs.Add(IntVector2.Right);
            neighborDirs.Add(IntVector2.UpRight);
            neighborDirs.Add(IntVector2.Up);
            neighborDirs.Add(IntVector2.UpLeft);
            neighborDirs.Add(IntVector2.Left);
            neighborDirs.Add(IntVector2.DownLeft);
            neighborDirs.Add(IntVector2.Down);
            neighborDirs.Add(IntVector2.DownRight);

        // tk2dTileMap tilemap = null;
        CellData baseCell = d.data[pos];
        if (baseCell == null)
        {
            ETGModConsole.Log($"SHOULD NEVER HAPPEN");
            return;
        }
        bool cachedDataCleared = false;
        foreach (IntVector2 neighborDir in neighborDirs)
        {
            IntVector2 neighbor = pos + neighborDir;
            CellData neighborData = d.data[neighbor];
            if (neighborData != null)
                continue;

            ETGModConsole.Log($"  placing new wall at {neighbor}");

            if (!cachedDataCleared)
            {
                cachedDataCleared = true;
                d.data.ClearCachedCellData();
            }

            CellData newCell = new CellData(neighbor);
            d.data.cellData[neighbor.x][neighbor.y] = newCell;
            d.data[neighbor] = newCell;
            r.Cells.Add(neighbor);
            r.CellsWithoutExits.Add(neighbor);
            r.RawCells.Add(neighbor);

            newCell.cellVisualData.CopyFrom(baseCell.cellVisualData);
            newCell.positionInTilemap = baseCell.positionInTilemap + neighborDir;
            newCell.parentArea = baseCell.parentArea;
            newCell.parentRoom = r;
            newCell.nearestRoom = r;
            newCell.occlusionData.overrideOcclusion = true;

            r.RuntimeStampCellComplex(neighbor.x, neighbor.y, CellType.WALL, DiagonalWallType.NONE);
            d.ConstructWallAtPosition(neighbor.x, neighbor.y, deferRebuild: true);
            if (timesToRecurse > 0)
                AddNeighboringWalls(d, r, neighbor, timesToRecurse - 1);
            // tilemap = d.ConstructWallAtPosition(neighbor.x, neighbor.y, deferRebuild: true);
        }
    }

    public override void DoEffect(PlayerController user)
    {
        ETGModConsole.Log($"");
        Dungeon d = GameManager.Instance.Dungeon;
        Vector2 ppos = user.transform.PositionVector2();
        Vector2 unitBottomCenter = user.specRigidbody.PrimaryPixelCollider.UnitBottomCenter;
        IntVector2 truepos = unitBottomCenter.ToIntVector2(VectorConversions.Floor);
        if (!GameManager.Instance.Dungeon.data.CheckInBoundsAndValid(truepos))
        {
            ETGModConsole.Log($"we're out of bounds!");
            return;
        }

        CellData cellData = d.data[truepos];
        if (cellData == null)
        {
            ETGModConsole.Log($"our current cell doesn't exist!");
            return;
        }

        RoomHandler r = user.CurrentRoom;
        if (r == null)
        {
            ETGModConsole.Log($"not in a room!");
            return;
        }

        float gunAngle = user.m_currentGunAngle.Clamp180();
        IntVector2 facingPos = truepos;
        if (Mathf.Abs(gunAngle) < 45f)
            facingPos += IntVector2.Right;
        else if (Mathf.Abs(gunAngle) > 135f)
            facingPos += IntVector2.Left;
        else
            facingPos += gunAngle > 0 ? IntVector2.Up : IntVector2.Down;
        ETGModConsole.Log($"we are in a cell at {cellData.position} of type {cellData.type}");
        // foreach (CellData cellNeighbor in d.data.GetCellNeighbors(cellData))
        //     ETGModConsole.Log($"  neighbor at {cellNeighbor.position} is type {cellNeighbor.type} {(cellNeighbor.position == facingPos ? " (facing)" : "")}");

        if (!GameManager.Instance.Dungeon.data.CheckInBoundsAndValid(facingPos))
        {
            ETGModConsole.Log($"  out of bounds!");
            return;
        }

        CellData facingCellData = d.data[facingPos];
        if (facingCellData == null)
        {
            ETGModConsole.Log($"  null facing cell data!");
            return;
        }
        if (facingCellData.type != CellType.WALL)
        {
            ETGModConsole.Log($"  facing a non-wall!");
            return;
        }

        ETGModConsole.Log($"  facing cell at {facingCellData.position} is type {facingCellData.type}");

        AddNeighboringWalls(d, r, facingPos, timesToRecurse: 3);
        foreach (CellData cellNeighbor in d.data.GetCellNeighbors(facingCellData, getDiagonals: true))
            ETGModConsole.Log($"  NEW neighbor at {cellNeighbor.position} is type {cellNeighbor.type}");

        facingCellData.breakable = true;
        facingCellData.occlusionData.overrideOcclusion = true;
        facingCellData.occlusionData.cellOcclusionDirty = true;
        d.DestroyWallAtPosition(facingPos.x, facingPos.y, deferRebuild: false);
        _VFXDustPoof.SpawnAtPosition(facingPos.ToCenterVector3(facingPos.y));

        r.Cells.Add(facingPos);
        r.CellsWithoutExits.Add(facingPos);
        r.RawCells.Add(facingPos);
        Pixelator.Instance.MarkOcclusionDirty();
        Pixelator.Instance.ProcessOcclusionChange(r.Epicenter, 1f, r, false);

        AkSoundEngine.PostEvent("Play_OBJ_rock_break_01", GameManager.Instance.gameObject);
        AkSoundEngine.PostEvent("Play_OBJ_stone_crumble_01", GameManager.Instance.gameObject);

    }
}
