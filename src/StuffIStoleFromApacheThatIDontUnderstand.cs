using Dungeonator;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using ItemAPI;
using tk2dRuntime.TileMap;
using Gungeon;
using MonoMod;
using Pathfinding;

namespace NevernamedsItems
{
    
     /*   public class FakePrefab : Component
        {
            internal static HashSet<GameObject> ExistingFakePrefabs = new HashSet<GameObject>();
            public static bool IsFakePrefab(UnityEngine.Object o)
            {
                bool flag = o is GameObject;
                bool result;
                if (flag)
                {
                    result = ExistingFakePrefabs.Contains((GameObject)o);
                }
                else
                {
                    bool flag2 = o is Component;
                    result = (flag2 && ExistingFakePrefabs.Contains(((Component)o).gameObject));
                }
                return result;
            }
            public static void MarkAsFakePrefab(GameObject obj) { ExistingFakePrefabs.Add(obj); }
            public static GameObject Clone(GameObject obj)
            {
                bool flag = IsFakePrefab(obj);
                bool activeSelf = obj.activeSelf;
                bool flag2 = activeSelf;
                if (flag2) { obj.SetActive(false); }
                GameObject gameObject = Instantiate(obj);
                bool flag3 = activeSelf;
                if (flag3) { obj.SetActive(true); }
                ExistingFakePrefabs.Add(gameObject);
                bool flag4 = flag;
                if (flag4) { }
                return gameObject;
            }
            public static UnityEngine.Object Instantiate(UnityEngine.Object o, UnityEngine.Object new_o)
            {
                try
                {
                    bool flag = o is GameObject && ExistingFakePrefabs.Contains((GameObject)o);
                    if (flag)
                    {                       
                        ((GameObject)new_o).SetActive(true);
                    }
                    else
                    {
                        bool flag2 = o is Component && ExistingFakePrefabs.Contains(((Component)o).gameObject);
                        if (flag2) { ((Component)new_o).gameObject.SetActive(true); }
                    }
                }
                catch (Exception ex)
                {
                Debug.LogError(ex);
                }
                return new_o;
            }
        }
    
    public class otherapacheshit
    {
        
        public static RoomHandler AddCustomRuntimeRoom(PrototypeDungeonRoom prototype, bool addRoomToMinimap = true, bool addTeleporter = true, bool isSecretRatExitRoom = false, Action<RoomHandler> postProcessCellData = null, DungeonData.LightGenerationStyle lightStyle = DungeonData.LightGenerationStyle.STANDARD)
        {
            Dungeon dungeon = GameManager.Instance.Dungeon;
            tk2dTileMap m_tilemap = dungeon.MainTilemap;

            if (m_tilemap == null)
            {
                ETGModConsole.Log("ERROR: TileMap object is null! Something seriously went wrong!");
                Debug.Log("ERROR: TileMap object is null! Something seriously went wrong!");
                return null;
            }

            TK2DDungeonAssembler assembler = new TK2DDungeonAssembler();
            assembler.Initialize(dungeon.tileIndices);

            IntVector2 basePosition = IntVector2.Zero;
            IntVector2 basePosition2 = new IntVector2(50, 50);
            int num = basePosition2.x;
            int num2 = basePosition2.y;
            IntVector2 intVector = new IntVector2(int.MaxValue, int.MaxValue);
            IntVector2 intVector2 = new IntVector2(int.MinValue, int.MinValue);
            intVector = IntVector2.Min(intVector, basePosition);
            intVector2 = IntVector2.Max(intVector2, basePosition + new IntVector2(prototype.Width, prototype.Height));
            IntVector2 a = intVector2 - intVector;
            IntVector2 b = IntVector2.Min(IntVector2.Zero, -1 * intVector);
            a += b;
            IntVector2 intVector3 = new IntVector2(dungeon.data.Width + num, num);
            int newWidth = dungeon.data.Width + num * 2 + a.x;
            int newHeight = Mathf.Max(dungeon.data.Height, a.y + num * 2);
            CellData[][] array = BraveUtility.MultidimensionalArrayResize(dungeon.data.cellData, dungeon.data.Width, dungeon.data.Height, newWidth, newHeight);
            dungeon.data.cellData = array;
            dungeon.data.ClearCachedCellData();
            IntVector2 d = new IntVector2(prototype.Width, prototype.Height);
            IntVector2 b2 = basePosition + b;
            IntVector2 intVector4 = intVector3 + b2;
            CellArea cellArea = new CellArea(intVector4, d, 0);
            cellArea.prototypeRoom = prototype;
            RoomHandler targetRoom = new RoomHandler(cellArea);
            for (int k = -num; k < d.x + num; k++)
            {
                for (int l = -num; l < d.y + num; l++)
                {
                    IntVector2 p = new IntVector2(k, l) + intVector4;
                    if ((k >= 0 && l >= 0 && k < d.x && l < d.y) || array[p.x][p.y] == null)
                    {
                        CellData cellData = new CellData(p, CellType.WALL);
                        cellData.positionInTilemap = cellData.positionInTilemap - intVector3 + new IntVector2(num2, num2);
                        cellData.parentArea = cellArea;
                        cellData.parentRoom = targetRoom;
                        cellData.nearestRoom = targetRoom;
                        cellData.distanceFromNearestRoom = 0f;
                        array[p.x][p.y] = cellData;
                    }
                }
            }
            dungeon.data.rooms.Add(targetRoom);
            try
            {
                targetRoom.WriteRoomData(dungeon.data);
            }
            catch (Exception)
            {
                ETGModConsole.Log("WARNING: Exception caused during WriteRoomData step on room: " + targetRoom.GetRoomName());
            }
            try
            {
                dungeon.data.GenerateLightsForRoom(dungeon.decoSettings, targetRoom, GameObject.Find("_Lights").transform, lightStyle);
            }
            catch (Exception)
            {
                ETGModConsole.Log("WARNING: Exception caused during GeernateLightsForRoom step on room: " + targetRoom.GetRoomName());
            }
            postProcessCellData?.Invoke(targetRoom);

            if (targetRoom.area.PrototypeRoomCategory == PrototypeDungeonRoom.RoomCategory.SECRET) { targetRoom.BuildSecretRoomCover(); }
            GameObject gameObject = (GameObject)UnityEngine.Object.Instantiate(BraveResources.Load("RuntimeTileMap", ".prefab"));
            tk2dTileMap component = gameObject.GetComponent<tk2dTileMap>();
            string str = UnityEngine.Random.Range(10000, 99999).ToString();
            gameObject.name = "Glitch_" + "RuntimeTilemap_" + str;
            component.renderData.name = "Glitch_" + "RuntimeTilemap_" + str + " Render Data";
            component.Editor__SpriteCollection = dungeon.tileIndices.dungeonCollection;
            try
            {
                TK2DDungeonAssembler.RuntimeResizeTileMap(component, a.x + num2 * 2, a.y + num2 * 2, m_tilemap.partitionSizeX, m_tilemap.partitionSizeY);
                IntVector2 intVector5 = new IntVector2(prototype.Width, prototype.Height);
                IntVector2 b3 = basePosition + b;
                IntVector2 intVector6 = intVector3 + b3;
                for (int num4 = -num2; num4 < intVector5.x + num2; num4++)
                {
                    for (int num5 = -num2; num5 < intVector5.y + num2 + 2; num5++)
                    {
                        assembler.BuildTileIndicesForCell(dungeon, component, intVector6.x + num4, intVector6.y + num5);
                    }
                }
                RenderMeshBuilder.CurrentCellXOffset = intVector3.x - num2;
                RenderMeshBuilder.CurrentCellYOffset = intVector3.y - num2;
                component.ForceBuild();
                RenderMeshBuilder.CurrentCellXOffset = 0;
                RenderMeshBuilder.CurrentCellYOffset = 0;
                component.renderData.transform.position = new Vector3(intVector3.x - num2, intVector3.y - num2, intVector3.y - num2);
            }
            catch (Exception ex)
            {
                ETGModConsole.Log("WARNING: Exception occured during RuntimeResizeTileMap / RenderMeshBuilder steps!");
                Debug.Log("WARNING: Exception occured during RuntimeResizeTileMap/RenderMeshBuilder steps!");
                Debug.LogException(ex);
            }
            targetRoom.OverrideTilemap = component;
            for (int num7 = 0; num7 < targetRoom.area.dimensions.x; num7++)
            {
                for (int num8 = 0; num8 < targetRoom.area.dimensions.y + 2; num8++)
                {
                    IntVector2 intVector7 = targetRoom.area.basePosition + new IntVector2(num7, num8);
                    if (dungeon.data.CheckInBoundsAndValid(intVector7))
                    {
                        CellData currentCell = dungeon.data[intVector7];
                        TK2DInteriorDecorator.PlaceLightDecorationForCell(dungeon, component, currentCell, intVector7);
                    }
                }
            }

            Pathfinder.Instance.InitializeRegion(dungeon.data, targetRoom.area.basePosition + new IntVector2(-3, -3), targetRoom.area.dimensions + new IntVector2(3, 3));

            if (prototype.usesProceduralDecoration && prototype.allowFloorDecoration)
            {
                TK2DInteriorDecorator decorator = new TK2DInteriorDecorator(assembler);
                decorator.HandleRoomDecoration(targetRoom, dungeon, m_tilemap);
            }
            targetRoom.PostGenerationCleanup();

            if (addRoomToMinimap)
            {
                targetRoom.visibility = RoomHandler.VisibilityStatus.VISITED;
               GameManager.Instance.StartCoroutine(Minimap.Instance.RevealMinimapRoomInternal(targetRoom, true, true, false));
                if (isSecretRatExitRoom) { targetRoom.visibility = RoomHandler.VisibilityStatus.OBSCURED; }
            }
            if (addTeleporter) { targetRoom.AddProceduralTeleporterToRoom(); }
            if (addRoomToMinimap) { Minimap.Instance.InitializeMinimap(dungeon.data); }
            DeadlyDeadlyGoopManager.ReinitializeData();
            return targetRoom;
        }
    }*/
    public class TeleportNonsense
    {
        public static bool CanBlinkToPoint(PlayerController Owner, Vector2 point)
        {
            bool flag = Owner.IsValidPlayerPosition(point);
            if (flag && Owner.CurrentRoom != null)
            {
                CellData cellData = GameManager.Instance.Dungeon.data[point.ToIntVector2(VectorConversions.Floor)];
                if (cellData == null) { return false; }
                RoomHandler nearestRoom = cellData.nearestRoom;
                if (cellData.type != CellType.FLOOR) { flag = false; }
                if (Owner.CurrentRoom.IsSealed && nearestRoom != Owner.CurrentRoom) { flag = false; }
                if (Owner.CurrentRoom.IsSealed && cellData.isExitCell) { flag = false; }
                if (nearestRoom.visibility == RoomHandler.VisibilityStatus.OBSCURED || nearestRoom.visibility == RoomHandler.VisibilityStatus.REOBSCURED) { flag = false; }
            }
            if (Owner.CurrentRoom == null) { flag = false; }
            if (Owner.IsDodgeRolling | Owner.IsFalling | Owner.IsCurrentlyCoopReviving | Owner.IsInMinecart | Owner.IsInputOverridden) { return false; }
            return flag;
        }
        public static bool PositionIsInBounds(PlayerController Owner, Vector2 point)
        {
            bool flag = true;
            if (Owner.CurrentRoom != null)
            {
                CellData cellData = GameManager.Instance.Dungeon.data[point.ToIntVector2(VectorConversions.Floor)];
                if (cellData == null) { return false; }
                RoomHandler nearestRoom = cellData.nearestRoom;
                if (cellData.type != CellType.FLOOR) { flag = false; }
                if (Owner.CurrentRoom.IsSealed && nearestRoom != Owner.CurrentRoom) { flag = false; }
                if (Owner.CurrentRoom.IsSealed && cellData.isExitCell) { flag = false; }
                if (nearestRoom.visibility == RoomHandler.VisibilityStatus.OBSCURED || nearestRoom.visibility == RoomHandler.VisibilityStatus.REOBSCURED) { flag = false; }
            }
            if (Owner.CurrentRoom == null) { flag = false; }
            if (Owner.IsDodgeRolling | Owner.IsFalling | Owner.IsCurrentlyCoopReviving | Owner.IsInMinecart | Owner.IsInputOverridden) { return false; }
            return flag;
        }
        public static Vector2 AdjustInputVector(Vector2 rawInput, float cardinalMagnetAngle, float ordinalMagnetAngle)
        {
            float num = BraveMathCollege.ClampAngle360(BraveMathCollege.Atan2Degrees(rawInput));
            float num2 = num % 90f;
            float num3 = (num + 45f) % 90f;
            float num4 = 0f;
            if (cardinalMagnetAngle > 0f)
            {
                if (num2 < cardinalMagnetAngle)
                {
                    num4 = -num2;
                }
                else if (num2 > 90f - cardinalMagnetAngle)
                {
                    num4 = 90f - num2;
                }
            }
            if (ordinalMagnetAngle > 0f)
            {
                if (num3 < ordinalMagnetAngle)
                {
                    num4 = -num3;
                }
                else if (num3 > 90f - ordinalMagnetAngle)
                {
                    num4 = 90f - num3;
                }
            }
            num += num4;
            return (Quaternion.Euler(0f, 0f, num) * Vector3.right).XY() * rawInput.magnitude;
        }
    }
    public class TeleportPlayerToCursorPosition : MonoBehaviour
    {
        private static Vector2 lockedDodgeRollDirection;
        public static BlinkPassiveItem m_BlinkPassive = PickupObjectDatabase.GetById(436).GetComponent<BlinkPassiveItem>();
        public GameObject BlinkpoofVfx = m_BlinkPassive.BlinkpoofVfx;
        public static void StartTeleport(PlayerController user, Vector2 newPosition)
        {
            user.healthHaver.TriggerInvulnerabilityPeriod(0.001f);
            user.DidUnstealthyAction();
            Vector2 clampedPosition = BraveMathCollege.ClampToBounds(newPosition, GameManager.Instance.MainCameraController.MinVisiblePoint, GameManager.Instance.MainCameraController.MaxVisiblePoint);
            BlinkToPoint(user, clampedPosition);
        }
        private static void BlinkToPoint(PlayerController Owner, Vector2 targetPoint)
        {

            lockedDodgeRollDirection = (targetPoint - Owner.specRigidbody.UnitCenter).normalized;

            Vector2 playerPos = Owner.transform.position;

            int x0 = (int)targetPoint.x, y0 = (int)targetPoint.y, x1 = (int)playerPos.x, y1 = (int)playerPos.y;
            int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
            int dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
            int err = dx + dy, e2; /* error value e_xy */
            int maxiterations = 600;
            while (maxiterations > 0)
            {  /* loop */

                if (x0 == x1 && y0 == y1) break;
                if (CanBlinkToPoint(new Vector2(x0, y0), Owner))
                {
                    StaticCoroutine.Start(HandleBlinkTeleport(Owner, new Vector2(x0, y0), lockedDodgeRollDirection));
                    return;
                }
                e2 = 2 * err;
                if (e2 >= dy) { err += dy; x0 += sx; } /* e_xy+e_x > 0 */
                if (e2 <= dx) { err += dx; y0 += sy; } /* e_xy+e_y < 0 */

                maxiterations--;
            }
        }
        static bool CanBlinkToPoint(Vector2 point, PlayerController owner)
        {
            RoomHandler CurrentRoom = owner.CurrentRoom;
            bool flag = owner.IsValidPlayerPosition(point);
            if (flag && CurrentRoom != null)
            {
                CellData cellData = GameManager.Instance.Dungeon.data[point.ToIntVector2(VectorConversions.Floor)];
                if (cellData == null)
                {
                    return false;
                }
                RoomHandler nearestRoom = cellData.nearestRoom;
                if (cellData.type != CellType.FLOOR)
                {
                    flag = false;
                }
                if (CurrentRoom.IsSealed && nearestRoom != CurrentRoom)
                {
                    flag = false;
                }
                if (CurrentRoom.IsSealed && cellData.isExitCell)
                {
                    flag = false;
                }
                if (nearestRoom.visibility == RoomHandler.VisibilityStatus.OBSCURED || nearestRoom.visibility == RoomHandler.VisibilityStatus.REOBSCURED)
                {
                    flag = false;
                }
            }
            if (CurrentRoom == null)
            {
                flag = false;
            }
            return flag;
        }
        private static IEnumerator HandleBlinkTeleport(PlayerController Owner, Vector2 targetPoint, Vector2 targetDirection)
        {

            //targetPoint = (targetPoint - new Vector2(0.30f, 0.125f));

            Owner.PlayEffectOnActor(EasyVFXDatabase.BloodiedScarfPoofVFX, Vector3.zero, false, true, false);

            AkSoundEngine.PostEvent("Play_ENM_wizardred_vanish_01", Owner.gameObject);
            List<AIActor> m_rollDamagedEnemies = OMITBReflectionHelpers.ReflectGetField<List<AIActor>>(typeof(PlayerController), "m_rollDamagedEnemies", Owner);
            if (m_rollDamagedEnemies != null)
            {
                m_rollDamagedEnemies.Clear();
                FieldInfo m_rollDamagedEnemiesClear = typeof(PlayerController).GetField("m_rollDamagedEnemies", BindingFlags.Instance | BindingFlags.NonPublic);
                m_rollDamagedEnemiesClear.SetValue(Owner, m_rollDamagedEnemies);
            }

            if (Owner.knockbackDoer) { Owner.knockbackDoer.ClearContinuousKnockbacks(); }
            Owner.IsEthereal = true;
            Owner.IsVisible = false;
            float RecoverySpeed = GameManager.Instance.MainCameraController.OverrideRecoverySpeed;
            bool IsLerping = GameManager.Instance.MainCameraController.IsLerping;
            yield return new WaitForSeconds(0.1f);
            GameManager.Instance.MainCameraController.OverrideRecoverySpeed = 80f;
            GameManager.Instance.MainCameraController.IsLerping = true;
            if (Owner.IsPrimaryPlayer)
            {
                GameManager.Instance.MainCameraController.UseOverridePlayerOnePosition = true;
                GameManager.Instance.MainCameraController.OverridePlayerOnePosition = targetPoint;
                yield return new WaitForSeconds(0.12f);
                Owner.specRigidbody.Velocity = Vector2.zero;
                Owner.specRigidbody.Position = new Position(targetPoint);
                GameManager.Instance.MainCameraController.UseOverridePlayerOnePosition = false;
            }
            else
            {
                GameManager.Instance.MainCameraController.UseOverridePlayerTwoPosition = true;
                GameManager.Instance.MainCameraController.OverridePlayerTwoPosition = targetPoint;
                yield return new WaitForSeconds(0.12f);
                Owner.specRigidbody.Velocity = Vector2.zero;
                Owner.specRigidbody.Position = new Position(targetPoint);
                GameManager.Instance.MainCameraController.UseOverridePlayerTwoPosition = false;
            }
            GameManager.Instance.MainCameraController.OverrideRecoverySpeed = RecoverySpeed;
            GameManager.Instance.MainCameraController.IsLerping = IsLerping;
            Owner.IsEthereal = false;
            Owner.IsVisible = true;
            Owner.PlayEffectOnActor(EasyVFXDatabase.BloodiedScarfPoofVFX, Vector3.zero, false, true, false);
            //m_CurrentlyBlinking = false;
            if (Owner.CurrentFireMeterValue <= 0f) { yield break; }
            Owner.CurrentFireMeterValue = Mathf.Max(0f, Owner.CurrentFireMeterValue -= 0.5f);
            if (Owner.CurrentFireMeterValue == 0f)
            {
                Owner.IsOnFire = false;
                yield break;
            }
            // yield return null;
            //CorrectForWalls(Owner);
            yield break;
        }
        public static void CorrectForWalls(PlayerController portal)
        {
            bool flag = PhysicsEngine.Instance.OverlapCast(portal.specRigidbody, null, true, false, null, null, false, null, null, new SpeculativeRigidbody[0]);
            if (flag)
            {
                Vector2 vector = portal.transform.position.XY();
                IntVector2[] cardinalsAndOrdinals = IntVector2.CardinalsAndOrdinals;
                int num = 0;
                int num2 = 1;
                for (; ; )
                {
                    for (int i = 0; i < cardinalsAndOrdinals.Length; i++)
                    {
                        //portal.transform.position = vector + PhysicsEngine.PixelToUnit(cardinalsAndOrdinals[i] * num2);
                        portal.specRigidbody.Position = new Position(vector + PhysicsEngine.PixelToUnit(cardinalsAndOrdinals[i] * num2));
                        portal.specRigidbody.Reinitialize();
                        if (!PhysicsEngine.Instance.OverlapCast(portal.specRigidbody, null, true, false, null, null, false, null, null, new SpeculativeRigidbody[0]))
                        {
                            return;
                        }
                    }
                    num2++;
                    num++;
                    if (num > 200)
                    {
                        goto Block_4;
                    }
                }
                //return;
            Block_4:
                Debug.LogError("FREEZE AVERTED!  TELL RUBEL!  (you're welcome) 147");
                return;
            }
        }
    }
}







