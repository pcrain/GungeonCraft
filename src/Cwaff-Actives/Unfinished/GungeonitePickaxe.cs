namespace CwaffingTheGungy;

using tk2dRuntime.TileMap;

public class GungeonitePickaxe : CwaffActive
{
    public static string ItemName         = "Gungeonite Pickaxe";
    public static string ShortDescription = "So We Back in the Mines";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    private const int _ADD_WALL_DEPTH = 4; // number of layers of extra walls to add when digging (to prevent tilemap rebuilder from panicking)

    internal static VFXPool _VFXDustPoof;
    internal static HashSet<IntVector2> _RebuiltChunks = new();

    private static readonly List<IntVector2> _NeighborDirs = new(){
        IntVector2.Right,IntVector2.UpRight,IntVector2.Up,IntVector2.UpLeft,IntVector2.Left,IntVector2.DownLeft,IntVector2.Down,IntVector2.DownRight};

    private const float _MAX_DIST = 5f;

    // my compiler REALLY doesn't like Actions with 6 parameter types, so declaring this separately;
    public delegate void FloorEdgeBorderDelegate(TK2DDungeonAssembler assembler, CellData cellData, Dungeon dungeon, tk2dTileMap map, int x, int y);
    public delegate void BuildForChunkDelegate(tk2dTileMap tileMap, SpriteChunk chunk, bool useColor, bool skipPrefabs, int baseX, int baseY, LayerInfo layerData);

    public static void Init()
    {
        PlayerItem item = Lazy.SetupActive<GungeonitePickaxe>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.D;
        item.consumable   = false;
        item.CanBeDropped = true;
        item.SetCooldownType(ItemBuilder.CooldownType.Timed, 0.5f);

        _VFXDustPoof = (ItemHelper.Get(Items.Drill) as PlayerItem).GetComponent<PaydayDrillItem>().VFXDustPoof;

        // new Hook(
        //     typeof(TK2DDungeonAssembler).GetMethod("BuildFloorEdgeBorderTiles", BindingFlags.Instance | BindingFlags.NonPublic),
        //     typeof(GungeonitePickaxe).GetMethod("BuildFloorEdgeBorderTilesSanityCheck", BindingFlags.Static | BindingFlags.NonPublic)
        //     );

        // new Hook(
        //     typeof(tk2dRuntime.TileMap.RenderMeshBuilder).GetMethod("BuildForChunk", BindingFlags.Static | BindingFlags.Public),
        //     typeof(GungeonitePickaxe).GetMethod("BuildForChunkSanityCheck", BindingFlags.Static | BindingFlags.NonPublic)
        //     );
    }

    private static int   _ChunksBuilt = 0;
    private static float _ChunkBuildMsTotal = 0;
    private static float _ChunkBuildMsMin = 0;
    private static float _ChunkBuildMsMax = 0;
    private static void BuildForChunkSanityCheck(BuildForChunkDelegate orig, tk2dTileMap tileMap, SpriteChunk chunk, bool useColor, bool skipPrefabs, int baseX, int baseY, LayerInfo layerData)
    {
        System.Diagnostics.Stopwatch tempWatch = System.Diagnostics.Stopwatch.StartNew();
        orig(tileMap, chunk, useColor, skipPrefabs, baseX, baseY, layerData);
        tempWatch.Stop();
        ++_ChunksBuilt;
        _ChunkBuildMsTotal += tempWatch.ElapsedMilliseconds;
        // ETGModConsole.Log($"part 1 finished in "+(tempWatch.ElapsedMilliseconds/1000.0f)+" seconds"); tempWatch = System.Diagnostics.Stopwatch.StartNew();
    }


    private static void BuildFloorEdgeBorderTilesSanityCheck(FloorEdgeBorderDelegate orig, TK2DDungeonAssembler assembler, CellData current, Dungeon d, tk2dTileMap map, int ix, int iy)
    {
        if (Lazy.AnyoneHasActive<GungeonitePickaxe>())
            MyBuildFloorEdgeBorderTiles(assembler, current, d, map, ix, iy);
        else
            orig(assembler, current, d, map, ix, iy);
    }

    private static void MyBuildFloorEdgeBorderTiles(TK2DDungeonAssembler assembler, CellData current, Dungeon d, tk2dTileMap map, int ix, int iy)
    {
        ETGModConsole.Log($"called BuildFloorEdgeBorderTiles() from starting position {current.position}");
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
    }

    private static void AddNeighboringWalls(Dungeon d, RoomHandler r, IntVector2 pos, int timesToRecurse, bool firstLayer = true)
    {
        CellData baseCell = d.data[pos];
        if (baseCell == null)
            return; // should never happen
        foreach (IntVector2 neighborDir in _NeighborDirs)
        {
            IntVector2 neighbor = pos + neighborDir;
            CellData neighborData = d.data[neighbor];
            if (neighborData == null)
            {
                // ETGModConsole.Log($"  placing new wall at {neighbor}");
                CellData newCell                        = new CellData(neighbor);
                d.data.cellData[neighbor.x][neighbor.y] = newCell;
                d.data[neighbor]                        = newCell;
                // r.Cells.Add(neighbor);
                // r.CellsWithoutExits.Add(neighbor);
                // r.RawCells.Add(neighbor);

                newCell.cellVisualData.CopyFrom(baseCell.cellVisualData);
                newCell.positionInTilemap               = baseCell.positionInTilemap + neighborDir;
                newCell.parentArea                      = null; //baseCell.parentArea;
                newCell.parentRoom                      = null;
                newCell.nearestRoom                     = baseCell.nearestRoom ?? baseCell.parentRoom;  // needed to prevent GoopChecks() from panicking
                newCell.occlusionData.overrideOcclusion = false; // true;

                // r.RuntimeStampCellComplex(neighbor.x, neighbor.y, CellType.WALL, DiagonalWallType.NONE);
                d.ConstructWallAtPosition(neighbor.x, neighbor.y, deferRebuild: true);
            }

            if (timesToRecurse > 0)
                AddNeighboringWalls(d, r, neighbor, timesToRecurse - 1, firstLayer: false);
        }
    }

    internal static float _LastUpdateTime = 0f;
    internal static IntVector2 _LastChunk = IntVector2.Zero;
    public override void Update()
    {
        base.Update();
        if (this.LastOwner is not PlayerController pc)
            return;

        IntVector2 truepos = pc.CenterPosition.ToIntVector2(VectorConversions.Floor);
        IntVector2 chunk = new IntVector2(Mathf.FloorToInt(truepos.x / 32f), Mathf.FloorToInt(truepos.y / 32f));
        if (chunk == _LastChunk)
            return;

        _LastChunk = chunk;

        tk2dTileMap tilemap = GameManager.Instance.Dungeon.MainTilemap;
        RebuildAdjacentChunks(tilemap, chunk.x, chunk.y);

        // if (pc.CurrentRoom == null)
        //     return;

        // float curTime = BraveTime.ScaledTimeSinceStartup;
        // if (_LastUpdateTime + 1f >= curTime)
        //     return;

        // _LastUpdateTime = curTime;
        // ETGModConsole.Log($"recomputing tilemap!");
        // Pixelator.Instance.MarkOcclusionDirty();
        // Pixelator.Instance.ProcessOcclusionChange(pc.CurrentRoom.Epicenter, 1f, pc.CurrentRoom, false);
        // GameManager.Instance.Dungeon.RebuildTilemap(pc.CurrentRoom.OverrideTilemap ?? GameManager.Instance.Dungeon.m_tilemap);
    }

    private static Coroutine _RebuildCoroutine = null;
    public static void RebuildAdjacentChunks(tk2dTileMap tilemap, int chunkX, int chunkY)
    {
        ETGModConsole.Log($"is main tilemap? {tilemap == GameManager.Instance.Dungeon.MainTilemap}");
        ETGModConsole.Log($"  rebuilding chunks around {chunkX} , {chunkY}");

        int minX = Math.Max(0, chunkX - 1);
        int minY = Math.Max(0, chunkY - 1);
        int maxX = Math.Min(tilemap.Layers[0].numColumns - 1, chunkX + 1);
        int maxY = Math.Min(tilemap.Layers[0].numRows - 1, chunkY + 1);

        int numLayers = tilemap.data.NumLayers;

        bool anythingToRebuild = false;
        for (int j = minY; j <= maxY; j++)
            for (int k = minX; k <= maxX; k++)
            {
                IntVector2 chunkIndex = new IntVector2(k, j);
                // if (_RebuiltChunks.Contains(chunkIndex))
                //     continue;
                // _RebuiltChunks.Add(chunkIndex);
                ETGModConsole.Log($"  marking chunk at {k},{j} as dirty");
                for (int layerId = 0; layerId < numLayers; layerId++)
                {
                    // ETGModConsole.Log($"    for layer {tilemap.data.Layers[layerId].name}");
                    tilemap.Layers[layerId].GetChunk(k, j).Dirty = true;
                }
                anythingToRebuild = true;
            }
        if (anythingToRebuild)
        {
            // if (_RebuildCoroutine != null)
            //     GameManager.Instance.StopCoroutine(_RebuildCoroutine);
            // _RebuildCoroutine = GameManager.Instance.StartCoroutine(tilemap.DeferredBuild(tk2dTileMap.BuildFlags.Default));
            tilemap.Build(tk2dTileMap.BuildFlags.Default);
        }
    }

    public override void DoEffect(PlayerController user)
    {
        ETGModConsole.Log($"built {_ChunksBuilt} in {_ChunkBuildMsTotal} ms == {_ChunkBuildMsTotal/_ChunksBuilt} ms / chunk");
        _ChunksBuilt = 0;
        _ChunkBuildMsTotal = 0;

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

        d.data.ClearCachedCellData(); // clear our cell cache, as its about to be very out of date
        AddNeighboringWalls(d, r, facingPos, timesToRecurse: _ADD_WALL_DEPTH, firstLayer: true);
        // foreach (CellData cellNeighbor in d.data.GetCellNeighbors(facingCellData, getDiagonals: true))
        //     ETGModConsole.Log($"  NEW neighbor at {cellNeighbor.position} is type {cellNeighbor.type}");

        facingCellData.breakable = true;
        facingCellData.occlusionData.overrideOcclusion = true;
        facingCellData.occlusionData.cellOcclusionDirty = true;
        tk2dTileMap tilemap = d.DestroyWallAtPosition(facingPos.x, facingPos.y, deferRebuild: true);
        _VFXDustPoof.SpawnAtPosition(facingPos.ToCenterVector3(facingPos.y));

        // r.Cells.Add(facingPos);
        // r.CellsWithoutExits.Add(facingPos);
        // r.RawCells.Add(facingPos);
        Pixelator.Instance.MarkOcclusionDirty();
        Pixelator.Instance.ProcessOcclusionChange(truepos /*r.Epicenter*/, 1f, r, false);
        if (tilemap)
        {
            tilemap.Build(tk2dTileMap.BuildFlags.ForceBuild);
            // _RebuiltChunks.Clear();
            // ETGModConsole.Log($"is main tilemap? {tilemap == GameManager.Instance.Dungeon.MainTilemap}");
            // int chunkX = Mathf.FloorToInt(facingPos.x / 32f);
            // int chunkY = Mathf.FloorToInt(facingPos.y / 32f);
            // RebuildAdjacentChunks(tilemap, chunkX, chunkY);
        }
        // Pixelator.Instance.MarkOcclusionDirty();
        // Pixelator.Instance.ProcessOcclusionChange(r.Epicenter, 1f, r, false);

        GameManager.Instance.gameObject.Play("Play_OBJ_rock_break_01");
        GameManager.Instance.gameObject.Play("Play_OBJ_stone_crumble_01");

        DeadlyDeadlyGoopManager.ReinitializeData();
    }

    public static void RebuildTilemapFixed(tk2dTileMap targetTilemap)
    {
        // ETGModConsole.Log($"starting offest = {RenderMeshBuilder.CurrentCellXOffset},{RenderMeshBuilder.CurrentCellYOffset}");
        // ETGModConsole.Log($"old position = {targetTilemap.renderData.transform.position}");
        RenderMeshBuilder.CurrentCellXOffset = Mathf.RoundToInt(targetTilemap.renderData.transform.position.x);
        RenderMeshBuilder.CurrentCellYOffset = Mathf.RoundToInt(targetTilemap.renderData.transform.position.y);
        targetTilemap.Build();
        targetTilemap.renderData.transform.position = new Vector3(RenderMeshBuilder.CurrentCellXOffset, RenderMeshBuilder.CurrentCellYOffset, RenderMeshBuilder.CurrentCellYOffset);
        // ETGModConsole.Log($"new position = {targetTilemap.renderData.transform.position}");
        RenderMeshBuilder.CurrentCellXOffset = 0;
        RenderMeshBuilder.CurrentCellYOffset = 0;
    }

    public static void MyBuild(tk2dTileMap tileMap)
    {
        IEnumerator enumerator = DeferredBuild(tileMap);
        while (enumerator.MoveNext())
        {
        }
    }

    public static IEnumerator DeferredBuild(tk2dTileMap tileMap)
    {
        if (!(tileMap.data != null) || !(tileMap.spriteCollection != null))
        {
            yield break;
        }
        if (tileMap.data.tilePrefabs == null)
        {
            tileMap.data.tilePrefabs = new GameObject[tileMap.SpriteCollectionInst.Count];
        }
        else if (tileMap.data.tilePrefabs.Length != tileMap.SpriteCollectionInst.Count)
        {
            Array.Resize(ref tileMap.data.tilePrefabs, tileMap.SpriteCollectionInst.Count);
        }
        BuilderUtil.InitDataStore(tileMap);
        if (tileMap.SpriteCollectionInst)
        {
            tileMap.SpriteCollectionInst.InitMaterialIds();
        }
        bool forceBuild = true;
        Dictionary<Layer, bool> layersActive = new Dictionary<Layer, bool>();
        if (tileMap.layers != null)
        {
            for (int i = 0; i < tileMap.layers.Length; i++)
            {
                Layer layer = tileMap.layers[i];
                if (layer != null && layer.gameObject != null)
                {
                    layersActive[layer] = layer.gameObject.activeSelf;
                }
            }
        }
        if (forceBuild)
        {
            tileMap.ClearSpawnedInstances();
        }
        BuilderUtil.CreateRenderData(tileMap, false, layersActive);
        SpriteChunk.s_roomChunks = new Dictionary<LayerInfo, List<SpriteChunk>>();
        if (Application.isPlaying && GameManager.Instance.Dungeon != null && GameManager.Instance.Dungeon.data != null && GameManager.Instance.Dungeon.MainTilemap == tileMap)
        {
            List<RoomHandler> rooms = GameManager.Instance.Dungeon.data.rooms;
            if (rooms != null && rooms.Count > 0)
            {
                for (int j = 0; j < tileMap.data.Layers.Length; j++)
                {
                    if (!tileMap.data.Layers[j].overrideChunkable)
                    {
                        continue;
                    }
                    for (int k = 0; k < rooms.Count; k++)
                    {
                        if (!SpriteChunk.s_roomChunks.ContainsKey(tileMap.data.Layers[j]))
                        {
                            SpriteChunk.s_roomChunks.Add(tileMap.data.Layers[j], new List<SpriteChunk>());
                        }
                        SpriteChunk spriteChunk = new SpriteChunk(
                            rooms[k].area.basePosition.x + tileMap.data.Layers[j].overrideChunkXOffset,
                            rooms[k].area.basePosition.y + tileMap.data.Layers[j].overrideChunkYOffset,
                            rooms[k].area.basePosition.x + rooms[k].area.dimensions.x + tileMap.data.Layers[j].overrideChunkXOffset,
                            rooms[k].area.basePosition.y + rooms[k].area.dimensions.y + tileMap.data.Layers[j].overrideChunkYOffset);
                        spriteChunk.roomReference = rooms[k];
                        string prototypeRoomName = rooms[k].area.PrototypeRoomName;
                        tileMap.Layers[j].CreateOverrideChunk(spriteChunk);
                        BuilderUtil.CreateOverrideChunkData(spriteChunk, tileMap, j, prototypeRoomName);
                        SpriteChunk.s_roomChunks[tileMap.data.Layers[j]].Add(spriteChunk);
                    }
                }
            }
        }
        forceBuild = false;
        IEnumerator BuildTracker = RenderMeshBuilder.Build(tileMap, false, forceBuild);
        while (BuildTracker.MoveNext())
        {
            yield return null;
        }
        forceBuild = false;
        if (tileMap.isGungeonTilemap)
        {
            BuilderUtil.SpawnAnimatedTiles(tileMap, forceBuild);
        }
        if (true)
        {
            tk2dSpriteDefinition firstValidDefinition = tileMap.SpriteCollectionInst.FirstValidDefinition;
            if (firstValidDefinition != null && firstValidDefinition.physicsEngine == tk2dSpriteDefinition.PhysicsEngine.Physics2D)
            {
                ColliderBuilder2D.Build(tileMap, forceBuild);
            }
            else
            {
                ColliderBuilder3D.Build(tileMap, forceBuild);
            }
            BuilderUtil.SpawnPrefabs(tileMap, forceBuild);
        }
        Layer[] array = tileMap.layers;
        foreach (Layer layer2 in array)
        {
            layer2.ClearDirtyFlag();
        }
        if (tileMap.colorChannel != null)
        {
            tileMap.colorChannel.ClearDirtyFlag();
        }
        if (tileMap.SpriteCollectionInst)
        {
            tileMap.spriteCollectionKey = tileMap.SpriteCollectionInst.buildKey;
        }
    }
}
