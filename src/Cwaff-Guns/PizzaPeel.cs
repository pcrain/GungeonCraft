
namespace CwaffingTheGungy;

public class PizzaPeel : CwaffGun
{
    public static string ItemName         = "Pizza Peel";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    private const int _SHOOT_FPS = 30;

    internal static readonly List<string> _IdleAnimations = new(5);
    internal static readonly List<string> _FireAnimations = new(4);

    public static void Init()
    {
        Lazy.SetupGun<PizzaPeel>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.B, gunClass: GunClass.RIFLE, reloadTime: 1.5f, ammo: 600, shootFps: _SHOOT_FPS, reloadFps: 4,
            muzzleFrom: Items.Mailbox, smoothReload: 0.1f, infiniteAmmo: true)
          .SetReloadAudio("pizza_flip_sound", 0, 8, 16, 20, 24, 28, 32)
          .AssignGun(out Gun gun)
          .InitProjectile(GunData.New(sprite: "pizza_projectile", clipSize: 4, cooldown: 0.18f, shootStyle: ShootStyle.SemiAutomatic,
            damage: 9.0f, speed: 25f, range: 18f, force: 12f, hitEnemySound: null, hitWallSound: null));
          // .Attach<PizzaPeelProjectile>()

        for (int i = 0; i < 5; ++i)
        {
            _IdleAnimations.Add(gun.QuickUpdateGunAnimation($"{i}_slice_idle"));
            if (i < 4)
            {
                _FireAnimations.Add(gun.QuickUpdateGunAnimation($"{i}_slice_fire", fps: _SHOOT_FPS, returnToIdle: true));
                gun.SetGunAudio(_FireAnimations[i], "pizza_flip_sound");
            }
        }

        gun.idleAnimation = _IdleAnimations[0];
        gun.shootAnimation = _FireAnimations[0];

        #if DEBUG
        Commands._OnDebugKeyPressed -= StartDebugPizzaTime;
        Commands._OnDebugKeyPressed += StartDebugPizzaTime;
        #endif
    }

    private static void StartDebugPizzaTime()
    {
        Lazy.DebugLog($"staring pizza time!");
        PizzaTimeController.StartPizzaTime(GameManager.Instance.PrimaryPlayer);
    }

    public override void OnPlayerPickup(PlayerController player)
    {
        base.OnPlayerPickup(player);
        UpdateAnimations();
        this.gun.spriteAnimator.StopAndResetFrameToDefault();
        this.gun.spriteAnimator.Play(this.gun.idleAnimation);
        CwaffEvents.OnChangedRooms += PizzaTimeController.MaybeSpawnPizzaAtExit;
        GameManager.Instance.OnNewLevelFullyLoaded += PizzaTimeController.OnNewLevelFullyLoaded;
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        base.OnDroppedByPlayer(player);
        CwaffEvents.OnChangedRooms -= PizzaTimeController.MaybeSpawnPizzaAtExit;
        GameManager.Instance.OnNewLevelFullyLoaded -= PizzaTimeController.OnNewLevelFullyLoaded;
    }

    public override void OnDestroy()
    {
        CwaffEvents.OnChangedRooms -= PizzaTimeController.MaybeSpawnPizzaAtExit;
        GameManager.Instance.OnNewLevelFullyLoaded -= PizzaTimeController.OnNewLevelFullyLoaded;
        base.OnDestroy();
    }

    public override void OnSwitchedToThisGun()
    {
        base.OnSwitchedToThisGun();
        UpdateAnimations();
        this.gun.spriteAnimator.StopAndResetFrameToDefault();
        this.gun.spriteAnimator.Play(this.gun.idleAnimation);
    }

    private void UpdateAnimations()
    {
        if (this.gun.IsReloading)
        {
            this.gun.idleAnimation = _IdleAnimations[4];
            this.gun.shootAnimation = _FireAnimations[3];
        }
        else
        {
            this.gun.idleAnimation = _IdleAnimations[Mathf.Clamp(this.gun.ClipShotsRemaining, 0, 4)];
            this.gun.shootAnimation = _FireAnimations[Mathf.Clamp(this.gun.ClipShotsRemaining - 1, 0, 3)];
        }
        this.gun.spriteAnimator.defaultClipId = this.gun.spriteAnimator.GetClipIdByName(this.gun.idleAnimation);
    }

    public override void Update()
    {
        base.Update();
        if (!this.PlayerOwner || !this.PlayerOwner.AcceptingNonMotionInput)
            return;

        UpdateAnimations();
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        UpdateAnimations();
    }
}

// public class PizzaPeelProjectile : MonoBehaviour
// {
//     private Projectile _projectile;
//     private PlayerController _owner;
//     private PizzaPeel _gun;

//     private void Start()
//     {
//         this._projectile = base.GetComponent<Projectile>();
//         this._owner = this._projectile.Owner as PlayerController;
//         if (!this._owner)
//             return;

//         if (this._owner.CurrentGun is Gun gun)
//             this._gun = gun.gameObject.GetComponent<PizzaPeel>();

//         // enter extra startup code here
//     }

//     private void Update()
//     {
//       // enter update code here
//     }

//     private void OnDestroy()
//     {
//       // enter destroy code here
//     }
// }


public class PizzaTimeController : MonoBehaviour
{
    private const int _MIN_DELIVERY_ROOMS = 5;
    private const string _PIZZA_TIME_OVERRIDE = "Pizza Time";

    internal static bool _PizzaTimeHappening = false;
    internal static Gun _ExtantGun = null;
    internal static PlayerController _DeliveryBoi = null;

    private static PizzaTimeController _Instance = null;
    private static readonly List<RoomHandler> _DeliveryRooms = new();
    private static readonly LinkedList<RoomHandler> _OccupiedRooms = new();
    private static readonly LinkedList<AIActor> _Hungrybois = new();
    private static bool _ScannedRoomsThisFloor = false;
    private static float _PizzaEventEndTime = 0f;
    private static int _MaxDeliveries = 0;
    private static int _CurDeliveries = 0;

    internal static void MaybeSpawnPizzaAtExit(PlayerController player, RoomHandler oldRoom, RoomHandler newRoom)
    {
        // TODO: spawn pizza in exit room to start pizza time event...doing it with a debug key for now
        // throw new NotImplementedException();
    }

    private static void DoPizzaTimeMusic()
    {
        //TODO: implement this
        // throw new NotImplementedException();
    }

    private static void StopPizzaTimeMusic()
    {
        //TODO: implement this
        // throw new NotImplementedException();
    }

    public static void HandleDeliverySuccess()
    {
        Lazy.DebugLog($"pizza delivered :D");
        //TODO: handle proper rewards
    }

    public static void HandleDeliveryFailure()
    {
        Lazy.DebugLog($"no pizza D:");
        //TODO: handle proper failure message
    }

    private static void SpawnHungryBulletKins()
    {
        AIActor kinPrefab = EnemyDatabase.GetOrLoadByGuid(Enemies.BulletKin);
        foreach (RoomHandler room in _DeliveryRooms)
        {
            IntVector2 centeredVisibleClearSpot = room.GetCenteredVisibleClearSpot(3, 3, out bool success, true);
            if (!success)
            {
                Lazy.DebugLog($"  failed to spawn enemy in room {room.area.PrototypeRoomName}");
                continue;
            }
            AIActor enemy = AIActor.Spawn(kinPrefab, centeredVisibleClearSpot, room);
            enemy.IgnoreForRoomClear = true;
            enemy.ParentRoom.ResetEnemyHPPercentage();
            enemy.ApplyEffect(LibraryCardtridge._CharmEffect);
            Lazy.DebugLog($"spawned kin in room {room.area.PrototypeRoomName}");
            _Hungrybois.AddLast(enemy);
        }
        _MaxDeliveries = _Hungrybois.Count;
        Lazy.DebugLog($"spawned in {_Hungrybois.Count} bullet kin awaiting pizza delivery");
    }

    private static void DespawnHungryBulletKins()
    {
        RoomHandler currentRoom = _DeliveryBoi ? _DeliveryBoi.CurrentRoom : null;
        int numKin = _Hungrybois.Count;
        for (int i = 0; i < numKin; ++i)
        {
            AIActor boi = _Hungrybois.First.Value;
            _Hungrybois.RemoveFirst();
            if (!boi || !boi.gameObject)
                continue;
            if (currentRoom != null || boi.ParentRoom != currentRoom)
                UnityEngine.Object.Destroy(boi.gameObject);
            else
                boi.EraseFromExistence(true);
        }
    }

    private static void DetermineEventEndTime()
    {
        _PizzaEventEndTime = BraveTime.ScaledTimeSinceStartup + 90f;
        //TODO: can maybe make this more dynamic later
    }

    internal static void OnNewLevelFullyLoaded()
    {
        if (_PizzaTimeHappening)
            EndPizzaTime(floorEnded: true);
        _DeliveryRooms.Clear();
        _OccupiedRooms.Clear();
        _Hungrybois.Clear();
        _CurDeliveries = 0;
        _MaxDeliveries = 0;
        _ScannedRoomsThisFloor = false;
    }

    private static bool RoomStillHasEnemies(RoomHandler room)
    {
        if (room.remainingReinforcementLayers != null && room.remainingReinforcementLayers.Count > 0)
            return true;
        for (int i = 0; i < room.activeEnemies.Count; i++)
            if (!room.activeEnemies[i].IgnoreForRoomClear)
                return true;
        return false;
    }

    private static void ScanRooms()
    {
        List<RoomHandler> rooms = GameManager.Instance.Dungeon.data.rooms;
        for (int i = 0; i < rooms.Count; i++)
        {
            RoomHandler room = rooms[i];
            if (room == null || !room.IsStandardRoom || !room.EverHadEnemies)
                continue;
            _DeliveryRooms.Add(room);
            if (RoomStillHasEnemies(room))
                _OccupiedRooms.AddLast(room);
        }
    }

    private static bool AnyRoomsStillOccupied()
    {
        int nRooms = _OccupiedRooms.Count;
        for (int i = 0; i < nRooms; ++i)
        {
            LinkedListNode<RoomHandler> room = _OccupiedRooms.First;
            _OccupiedRooms.RemoveFirst();
            if (RoomStillHasEnemies(room.Value))
                _OccupiedRooms.AddLast(room);
        }
        return _OccupiedRooms.Count > 0;
    }

    private static bool AllRoomsCleared()
    {
        if (!_ScannedRoomsThisFloor)
            ScanRooms();
        return _DeliveryRooms.Count > _MIN_DELIVERY_ROOMS && !AnyRoomsStillOccupied();
    }

    public static bool CanStartPizzaTime(PlayerController deliveryboi)
    {
        if (_PizzaTimeHappening)
            return false;
        if (!deliveryboi || deliveryboi.IsGunLocked)
            return false;
        if (!AllRoomsCleared())
            return false;
        return true;
    }

    public static void StartPizzaTime(PlayerController deliveryboi)
    {
        _Instance = new GameObject().AddComponent<PizzaTimeController>();
        _PizzaTimeHappening = true; // prevent player from teleporting and some other stuff
        _DeliveryBoi = deliveryboi;
        GivePizzaPeel(deliveryboi); // give player the PizzaPeel and gun lock them
        SpawnHungryBulletKins(); //TODO: add bullet kin to a bunch of rooms
        DetermineEventEndTime(); //TODO: figure out how long to give the player to complete the event
        //TODO: maybe disable map?
        DoPizzaTimeMusic();
    }

    public static void EndPizzaTime(bool floorEnded = false)
    {
        Lazy.DebugLog($"delivered {_CurDeliveries} / {_MaxDeliveries} pizzas");
        StopPizzaTimeMusic();
        if (!floorEnded)
            DespawnHungryBulletKins();
        DestroyPizzaPeel(_DeliveryBoi);
        _DeliveryBoi = null;
        _PizzaTimeHappening = false;
        UnityEngine.Object.Destroy(_Instance.gameObject);
        _Instance = null;
    }

    private static void GivePizzaPeel(PlayerController deliveryboi)
    {
        deliveryboi.inventory.GunChangeForgiveness = true;
        Gun pizzaGun = Lazy.Pickup<PizzaPeel>() as Gun;
        _ExtantGun = deliveryboi.inventory.AddGunToInventory(pizzaGun, true);
        _ExtantGun.CanBeDropped = false;
        _ExtantGun.CanBeSold = false;
        deliveryboi.inventory.GunLocked.SetOverride(_PIZZA_TIME_OVERRIDE, true);
    }

    private static void DestroyPizzaPeel(PlayerController deliveryboi)
    {
        deliveryboi.inventory.GunLocked.RemoveOverride(_PIZZA_TIME_OVERRIDE);
        deliveryboi.inventory.GunChangeForgiveness = false;
        if (_ExtantGun)
        {
            deliveryboi.inventory.DestroyGun(_ExtantGun);
            _ExtantGun = null;
        }
    }

    private void Update()
    {
        if (BraveTime.ScaledTimeSinceStartup < _PizzaEventEndTime)
            return;
        HandleDeliveryFailure();
        EndPizzaTime();
    }

    //TODO: verify this doesn't break any other code that hooks AttemptTeleportToRoom()
    /// <summary>Prevent teleportation while pizza time is active.</summary>
    [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.AttemptTeleportToRoom))]
    private static class PlayerControllerAttemptTeleportToRoomPatch
    {
        static bool Prefix(PlayerController __instance)
        {
            return !_PizzaTimeHappening; // call the original method only if we're not in pizza time
        }
    }
}

