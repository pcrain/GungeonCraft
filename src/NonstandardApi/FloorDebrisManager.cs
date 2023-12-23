// namespace CwaffingTheGungy;

// public static class FloorDebrisManager
// {
//   private const float _CHUNK_SIZE  = 32f;
//   private const float _CHUNK_UNIT  = 1f / _CHUNK_SIZE;
//   private const float _GRANULARITY = 32f;  // how many steps we split processing all debris into

//   private static float _currentStep = 0f;

//   private static HashSet<DebrisObject> _ProcessedFloorDebris = new();
//   // private static Dictionary<IntVector2,HashSet<DebrisObject>> _ProcessedChunkDebris = new();
//   private static Dictionary<IntVector2,LinkedList<DebrisObject>> _DebrisMap = new();

//   public static void InitForFloor()
//   {
//     _ProcessedFloorDebris.Clear();
//     // _ProcessedChunkDebris.Clear();
//     _DebrisMap.Clear();
//   }

//   private static void ProcessNewDebris()
//   {
//     for (int i = StaticReferenceManager.AllDebris.Count - 1; i > 0; ++i)
//     {
//       DebrisObject d = StaticReferenceManager.AllDebris[i];
//       if (_ProcessedFloorDebris.Contains(d))
//         break;

//       IntVector2 truepos = d.m_currentPosition.XY().ToIntVector2(VectorConversions.Floor);
//       IntVector2 chunk = new IntVector2(Mathf.FloorToInt(truepos.x * _CHUNK_UNIT), Mathf.FloorToInt(truepos.y * _CHUNK_UNIT));
//       // _ProcessedChunkDebris[chunk].Add(d);
//       _DebrisMap[chunk].AddLast(d);
//       _DebrisMap[chunk.WithX(chunk.x - 1)].AddLast(d);
//       _DebrisMap[chunk.WithX(chunk.x + 1)].AddLast(d);
//       _DebrisMap[chunk.WithY(chunk.y - 1)].AddLast(d);
//       _DebrisMap[chunk.WithY(chunk.y + 1)].AddLast(d);

//       _ProcessedFloorDebris.Add(d);
//     }
//   }

//   private static void ProcessExistingDebris()
//   {
//     int numDebris = StaticReferenceManager.AllDebris.Count;
//     int start = Mathf.FloorToInt(numDebris * (_currentStep / _GRANULARITY));
//     int end = Mathf.FloorToInt(numDebris * ((_currentStep + 1f) / _GRANULARITY));
//     for (int i = start; i < end; ++i)
//     {
//       DebrisObject d = StaticReferenceManager.AllDebris[i];

//       IntVector2 truepos = d.m_currentPosition.XY().ToIntVector2(VectorConversions.Floor);
//       IntVector2 chunk = new IntVector2(Mathf.FloorToInt(truepos.x * _CHUNK_UNIT), Mathf.FloorToInt(truepos.y * _CHUNK_UNIT));
//       // _ProcessedChunkDebris[chunk].Add(d);
//       _DebrisMap[chunk].AddLast(d);
//     }
//     _currentStep += 1f;
//     if (_currentStep >= _GRANULARITY)
//       _currentStep = 0f;
//   }

//   private static List<DebrisObject> GetNearbyDebris(this PlayerController player)
//   {
//       IntVector2 playerPos = player.CenterPosition.ToIntVector2(VectorConversions.Floor);
//       IntVector2 playerChunk = new IntVector2(Mathf.FloorToInt(playerPos.x * _CHUNK_UNIT), Mathf.FloorToInt(playerPos.y * _CHUNK_UNIT));

//       return null;
//   }

//   public static void Update()
//   {
//     ProcessNewDebris();
//     ProcessExistingDebris();
//   }
// }
