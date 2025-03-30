namespace CwaffingTheGungy;

/* TODO:
    - make active item for actually triggering the pizza time event
    - add rewards for finishing the event
    - add other vfx for the event
*/

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
          .Attach<PizzaPeelAmmoDisplay>()
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
        if (!PizzaTimeController._ScannedRoomsThisFloor)
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

public class PizzaPeelAmmoDisplay : CustomAmmoDisplay
{
    private Gun _gun;
    private PizzaPeel _peel;
    private PlayerController _owner;
    private void Start()
    {
        this._gun = base.GetComponent<Gun>();
        this._peel = this._gun.GetComponent<PizzaPeel>();
        this._owner = this._gun.CurrentOwner as PlayerController;
    }

    public override bool DoCustomAmmoDisplay(GameUIAmmoController uic)
    {
        if (!this._owner)
            return false;

        int max = PizzaTimeController._MaxDeliveries;
        int cur = PizzaTimeController._CurDeliveries;
        int time = Mathf.CeilToInt(PizzaTimeController._PizzaEventTimer);
        uic.GunAmmoCountLabel.Text = $"[sprite \"stopwatch_ui\"] {time}\n[sprite \"pizza_ui\"] {cur}/{max}";
        return true;
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
            // enemy.aiShooter.ForceGunOnTop = true;
            bs.AttackBehaviors   = new();
            bs.OverrideBehaviors = new();
            bs.OtherBehaviors    = new();
            bs.TargetBehaviors   = new();
            bs.MovementBehaviors = new();
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
          .SetAttributes(quality: ItemQuality.SPECIAL, gunClass: GunClass.SILLY, reloadTime: 1.2f, ammo: 999, infiniteAmmo: true)
          .DefaultModule.projectiles = new(){ Lazy.NoProjectile() };
    }

    public override void Update()
    {
        base.Update();
        this.gun.OverrideAngleSnap = 180f;
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

    private AIActor _enemy = null;
    private HealthHaver _hh = null;
    private GameObject _threatArrow = null;

    private void Start()
    {
        this._enemy = base.gameObject.GetComponent<AIActor>();
    }

    private void Update()
    {
        if (!this._enemy)
            return;
        if (!this._hh)
        {
            this._hh = base.gameObject.GetComponent<HealthHaver>();
            if (!this._hh)
                return;
        }
        this._hh.IsVulnerable = false;
        this._hh.TriggerInvulnerabilityPeriod(999999f);
        HandleIndicators();
    }

    private void OnDestroy()
    {
        if (this._threatArrow)
            UnityEngine.Object.Destroy(this._threatArrow);
    }

    private void HandleIndicators()
    {
        if ((PizzaTimeController._MaxDeliveries - PizzaTimeController._CurDeliveries) > PizzaTimeController._INDICATOR_THRESHOLD)
            return;

        if (!_threatArrow)
        {
            _threatArrow = (GameObject)UnityEngine.Object.Instantiate(BraveResources.Load("Global VFX/Alert_Arrow"));
            _threatArrow.SetLayerRecursively(LayerMask.NameToLayer("Unoccluded"));
        }
        tk2dBaseSprite extantArrowSprite = _threatArrow.GetComponent<tk2dBaseSprite>();
        if (this.hasPizza)
        {
            extantArrowSprite.renderer.enabled = false;
            return;
        }
        extantArrowSprite.HeightOffGround = 8f;
        extantArrowSprite.UpdateZDepth();
        if (extantArrowSprite.GetCurrentSpriteDef().name == "blankframe")
            return;

        Vector2 center = PizzaTimeController._DeliveryBoi.CenterPosition;
        Vector2 target = this._enemy.CenterPosition;
        Vector2 result = Vector2.zero;
        if (!BraveMathCollege.LineSegmentRectangleIntersection(center, target, GameManager.Instance.MainCameraController.MinVisiblePoint, GameManager.Instance.MainCameraController.MaxVisiblePoint, ref result))
        {
            extantArrowSprite.renderer.enabled = false;
            return;
        }
        extantArrowSprite.renderer.enabled = true;

        float atan = Mathf.RoundToInt(BraveMathCollege.Atan2Degrees(target - center) / 5f) * 5;
        Quaternion rot = Quaternion.Euler(0f, 0f, atan);
        Vector2 v = rot * Vector2.right;
        result -= v.normalized * 0.5f;
        _threatArrow.transform.position = result.ToVector3ZUp().Quantize(0.0625f);
        _threatArrow.transform.localRotation = rot;
    }
}

public class PizzaTimeController : MonoBehaviour
{
    private const int _MIN_DELIVERY_ROOMS = 5;
    private const string _PIZZA_TIME_OVERRIDE = "Pizza Time";

    internal const int _INDICATOR_THRESHOLD = 6;

    internal static bool _PizzaTimeHappening = false;
    internal static Gun _ExtantGun = null;
    internal static PlayerController _DeliveryBoi = null;
    internal static int _CurDeliveries = 0;
    internal static int _MaxDeliveries = 0;
    internal static bool _ScannedRoomsThisFloor = false;
    internal static float _PizzaEventTimer = 0f;

    private static PizzaTimeController _Instance = null;
    private static int _MoneyItem = -1;
    private static readonly List<RoomHandler> _DeliveryRooms = new();
    private static readonly LinkedList<RoomHandler> _OccupiedRooms = new();
    private static readonly LinkedList<AIActor> _Hungrybois = new();
    private static bool _PlayingMusic = false;

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
        if (_MoneyItem < 0)
            _MoneyItem = Lazy.PickupId<MoneyGun>();

        AIActor kinPrefab = EnemyDatabase.GetOrLoadByGuid(Enemies.BulletKin);
        foreach (RoomHandler room in _DeliveryRooms)
        {
            int numEnemiesInRoom = UnityEngine.Random.Range(1, 5);
            for (int i = 0; i < numEnemiesInRoom; ++i)
            {
                IntVector2 clearSpot = room.GetRandomVisibleClearSpot(5, 5);
                if (clearSpot == IntVector2.Zero)
                    continue;

                AIActor enemy = AIActor.Spawn(kinPrefab, clearSpot, room);
                enemy.IgnoreForRoomClear = true;
                enemy.CollisionDamage = 0f;
                enemy.specRigidbody.CorrectForWalls(andRigidBodies: true);
                enemy.ParentRoom.ResetEnemyHPPercentage();
                enemy.gameObject.AddComponent<WantsPizza>();
                enemy.ReplaceGun((Items)_MoneyItem);
                if (enemy.gameObject.GetComponent<BehaviorSpeculator>() is BehaviorSpeculator bs)
                {
                    bs.AttackBehaviors.Clear();
                    bs.OverrideBehaviors.Clear();
                    bs.OtherBehaviors.Clear();
                    bs.MovementBehaviors.Clear();
                    // bs.FullyRefreshBehaviors(); //NOTE: unnecessary since the behaviors haven't even been set up yet
                }
                enemy.HasDonePlayerEnterCheck = true;
                enemy.OnEngaged(true);

                _Hungrybois.AddLast(enemy);
            }
        }
        _CurDeliveries = 0;
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
        bool oocSpeed = GameManager.Options.IncreaseSpeedOutOfCombat;
        bool turbo = GameManager.IsTurboMode;
        if (turbo && oocSpeed)
            _PizzaEventTimer = 90f;
        else if (turbo || oocSpeed)
            _PizzaEventTimer = 120f;
        else
            _PizzaEventTimer = 150f;
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
        _ScannedRoomsThisFloor = true;
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
        _PizzaEventTimer -= BraveTime.DeltaTime;
        if (_PizzaEventTimer > 0)
            return;
        _PizzaEventTimer = 0;
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
