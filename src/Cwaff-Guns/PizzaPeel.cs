
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
            damage: 0.0f, speed: 25f, range: 18f, force: 12f, hitEnemySound: null, hitWallSound: null))
          .Attach<PizzaPeelProjectile>();

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

        MoneyGun.Init();
        PizzaGun.Init();

        #if DEBUG
        Commands._OnDebugKeyPressed -= StartDebugPizzaTime;
        Commands._OnDebugKeyPressed += StartDebugPizzaTime;
        #endif
    }

    private static void StartDebugPizzaTime()
    {
        GameManager.Instance.PrimaryPlayer.StartCoroutine(StartDebugPizzaTime_CR());
    }

    private static IEnumerator StartDebugPizzaTime_CR()
    {
        Lazy.DebugLog($"staring pizza time!");
        // nuke all rooms
        List<RoomHandler> rooms = GameManager.Instance.Dungeon.data.rooms;
        List<AIActor> enemiesToKill = new();
        for (int i = 0; i < rooms.Count; i++)
        {
            RoomHandler room = rooms[i];
            if (room == null)
                continue;
            room.ClearReinforcementLayers();
            room.GetActiveEnemies(RoomHandler.ActiveEnemyType.All, ref enemiesToKill);
            for (int k = 0; k < enemiesToKill.Count; k++)
                if (enemiesToKill[k])
                    enemiesToKill[k].enabled = true;
            yield return null;
            for (int j = 0; j < enemiesToKill.Count; j++)
                if (enemiesToKill[j])
                    UnityEngine.Object.Destroy(enemiesToKill[j].gameObject);
        }

        // actually do pizza time stuff
        PizzaTimeController.ScanRooms();
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

public class PizzaPeelProjectile : MonoBehaviour
{
    private static int _PizzaItem = -1;

    private Projectile _projectile;
    private PlayerController _owner;

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._owner = this._projectile.Owner as PlayerController;
        if (!this._owner)
            return;

        this._projectile.OnHitEnemy += this.DoPizzaChecks;
    }

    private void DoPizzaChecks(Projectile projectile, SpeculativeRigidbody rigidbody, bool arg3)
    {
        if (!rigidbody || rigidbody.gameObject.GetComponent<AIActor>() is not AIActor enemy)
            return;
        if (rigidbody.gameObject.GetComponent<WantsPizza>() is not WantsPizza p)
            return;
        if (p.hasPizza)
            return;

        p.hasPizza = true;
        if (_PizzaItem < 0)
            _PizzaItem = Lazy.PickupId<PizzaGun>();
        enemy.ReplaceGun((Items)_PizzaItem);
        if (enemy.EnemyGuid == Enemies.BulletKin)
            enemy.aiAnimator.OverrideIdleAnimation = "smile";

        if (enemy.aiShooter && enemy.aiShooter.behaviorSpeculator is BehaviorSpeculator bs)
        {
            bs.AttackBehaviors   = new();
            bs.OverrideBehaviors = new();
            bs.OtherBehaviors    = new();
            bs.FullyRefreshBehaviors();
        }

        // enemy.gameObject.AddComponent<HappyIceCreamHaver>();
        GameObject vfx = SpawnManager.SpawnVFX(IceCream._HeartVFX, enemy.sprite.WorldTopCenter + new Vector2(0f, 1f), Quaternion.identity, ignoresPools: true);
            tk2dSprite sprite = vfx.GetComponent<tk2dSprite>();
                sprite.HeightOffGround = 1f;
            vfx.transform.parent = enemy.sprite.transform;
            vfx.AddComponent<GlowAndFadeOut>().Setup(
                fadeInTime: 0.25f, glowInTime: 0.50f, holdTime: 0.0f, glowOutTime: 0.50f, fadeOutTime: 0.25f, maxEmit: 200f, destroy: true);

        enemy.gameObject.Play("ice_cream_shared");

        ++PizzaTimeController._CurDeliveries;
    }

    private void Update()
    {
      // enter update code here
    }

    private void OnDestroy()
    {
      // enter destroy code here
    }
}

public class MoneyGun : CwaffGun
{
    public static string ItemName         = "Money Gun";
    public static string ShortDescription = "O:";
    public static string LongDescription  = "$$$";
    public static string Lore             = "$$$$$";

    public static void Init()
    {
        Lazy.SetupGun<MoneyGun>(ItemName, ShortDescription, LongDescription, Lore, hideFromAmmonomicon: true)
          .SetAttributes(quality: ItemQuality.SPECIAL, gunClass: GunClass.SILLY, reloadTime: 1.2f, ammo: 999, infiniteAmmo: true, preventRotation: true)
          .DefaultModule.projectiles = new(){ Lazy.NoProjectile() };
    }
}

public class PizzaGun : CwaffGun
{
    public static string ItemName         = "Pizza Gun";
    public static string ShortDescription = "=D";
    public static string LongDescription  = "Pizza";
    public static string Lore             = "Peepaga, Peepaga";

    public static void Init()
    {
        Lazy.SetupGun<PizzaGun>(ItemName, ShortDescription, LongDescription, Lore, hideFromAmmonomicon: true)
          .SetAttributes(quality: ItemQuality.SPECIAL, gunClass: GunClass.SILLY, reloadTime: 1.2f, ammo: 999, infiniteAmmo: true, preventRotation: true)
          .DefaultModule.projectiles = new(){ Lazy.NoProjectile() };
    }
}

public class WantsPizza : MonoBehaviour {
    public bool hasPizza = false;
}

public class PizzaTimeController : MonoBehaviour
{
    private const int _MIN_DELIVERY_ROOMS = 5;
    private const string _PIZZA_TIME_OVERRIDE = "Pizza Time";

    internal static bool _PizzaTimeHappening = false;
    internal static Gun _ExtantGun = null;
    internal static PlayerController _DeliveryBoi = null;
    internal static int _CurDeliveries = 0;

    private static PizzaTimeController _Instance = null;
    private static int _MoneyItem = -1;
    private static readonly List<RoomHandler> _DeliveryRooms = new();
    private static readonly LinkedList<RoomHandler> _OccupiedRooms = new();
    private static readonly LinkedList<AIActor> _Hungrybois = new();
    private static bool _ScannedRoomsThisFloor = false;
    private static bool _PlayingMusic = false;
    private static float _PizzaEventEndTime = 0f;
    private static int _MaxDeliveries = 0;

    internal static void MaybeSpawnPizzaAtExit(PlayerController player, RoomHandler oldRoom, RoomHandler newRoom)
    {
        // TODO: spawn pizza in exit room to start pizza time event...doing it with a debug key for now
        // throw new NotImplementedException();
    }

    private static void DoPizzaTimeMusic()
    {
        // AkSoundEngine.PostEvent("Stop_SND_All", pc.gameObject);
        // AkSoundEngine.StopAll();
        AkSoundEngine.PostEvent("Stop_MUS_All", GameManager.Instance.gameObject);
        _PlayingMusic = true;
        //TODO: implement this
        // throw new NotImplementedException();
    }

    private static void StopPizzaTimeMusic()
    {
        GameManager.Instance.DungeonMusicController.ResetForNewFloor(GameManager.Instance.Dungeon);
        if (GameManager.Instance.BestActivePlayer.CurrentRoom is RoomHandler room)
            GameManager.Instance.DungeonMusicController.NotifyEnteredNewRoom(room);
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
            IntVector2 clearSpot = room.GetRandomVisibleClearSpot(5, 5);
            if (clearSpot == IntVector2.Zero)
                continue;
            AIActor enemy = AIActor.Spawn(kinPrefab, clearSpot, room);
            enemy.IgnoreForRoomClear = true;
            enemy.ParentRoom.ResetEnemyHPPercentage();
            // enemy.ApplyEffect(LibraryCardtridge._CharmEffect);
            enemy.gameObject.AddComponent<WantsPizza>();

            if (_MoneyItem < 0)
                _MoneyItem = Lazy.PickupId<MoneyGun>();
            enemy.ReplaceGun((Items)_MoneyItem);

            if (enemy.healthHaver is HealthHaver hh)
            {
                hh.IsVulnerable = false;
                hh.TriggerInvulnerabilityPeriod(999999f);
            }

            // Lazy.DebugLog($"spawned kin in room {room.area.PrototypeRoomName}");
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
            if (boi.gameObject.GetComponent<WantsPizza>() is WantsPizza p && p.hasPizza)
                continue;
            if (currentRoom != null && boi.CenterPosition.GetAbsoluteRoom() == currentRoom)
                boi.EraseFromExistence(true);
            else
                UnityEngine.Object.Destroy(boi.gameObject);
        }
        _Hungrybois.Clear();
    }

    //TODO: make this more dynamic later
    private static void DetermineEventEndTime()
    {
        #if DEBUG
        _PizzaEventEndTime = BraveTime.ScaledTimeSinceStartup + 20f;
        #else
        _PizzaEventEndTime = BraveTime.ScaledTimeSinceStartup + 90f;
        #endif
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

    internal static void ScanRooms()
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
        this.LoopSoundIf(_PlayingMusic, "pizza_time", loopPointMs: 9056, rewindAmountMs: 5318);
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
