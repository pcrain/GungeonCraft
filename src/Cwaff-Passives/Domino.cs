namespace CwaffingTheGungy;

/* TODO:
    - optmimize laggy startup
*/

public class Domino : CwaffPassive
{
    public static string ItemName         = "Domino";
    public static string ShortDescription = "Free Delivery";
    public static string LongDescription  = "Upon clearing a floor of enemies, adds an optional pizza delivery event marker to the floor exit room. Triggering the event grants a Pizza Peel for delivering as many pizzas as possible to Bullet Kin on the way back to the floor entrance. Casings are awarded upon reaching the event marker in the floor entrance based on the amount of pizzas delivered, while an additional casing multiplier or chest reward may be granted for high delivery rates. No rewards are granted if the event marker is not reached before the timer runs out.";
    public static string Lore             = "Pizza delivery in the Gungeon is rather awkward. Bullet Kin are known to immensely enjoy their pizza; however, Gungeon protocol prohibits them from ordering pizza while on duty. Bullet Kin have consequently been known to occasionally aid adventurers in clearing out floors for the sole purpose of clocking out to enjoy their pizza time earlier.";

    public static void Init()
    {
        PassiveItem item  = Lazy.SetupPassive<Domino>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.C;

        PizzaPeel.Init();
        MoneyGun.Init();
        PizzaGun.Init();
    }

    public override void Pickup(PlayerController player)
    {
        base.Pickup(player);
        player.OnRoomClearEvent += OnRoomClear;
        CwaffEvents.OnChangedRooms += PizzaTimeController.MaybeSpawnPizzaAtExit;
        GameManager.Instance.OnNewLevelFullyLoaded += PizzaTimeController.OnNewLevelFullyLoaded;
        CwaffEvents.OnCleanStart += PizzaTimeController.OnNewLevelFullyLoaded; // no need to remove this when the item is dropped
    }

    public override void DisableEffect(PlayerController player)
    {
        base.DisableEffect(player);
        if (player)
            player.OnRoomClearEvent -= OnRoomClear;
        CwaffEvents.OnChangedRooms -= PizzaTimeController.MaybeSpawnPizzaAtExit;
        GameManager.Instance.OnNewLevelFullyLoaded -= PizzaTimeController.OnNewLevelFullyLoaded;
    }

    private static void OnRoomClear(PlayerController player)
    {
        PizzaTimeController.MaybeSpawnPizzaAtExit(player, null, null);
    }
}

public class PizzaPeel : CwaffGun
{
    public static string ItemName         = "Pizza Peel";
    public static string ShortDescription = "Pizza Time";
    public static string LongDescription  = "Delivers fresh, hot pizza slices to hungry bullet kin.";
    public static string Lore             = "";

    private const int _SHOOT_FPS = 30;

    internal static GameObject _CashVFX = null;
    internal static GameObject _PizzaSliceVFX = null;
    internal static GameObject _PizzaBox = null;
    internal static readonly List<string> _IdleAnimations = new(5);
    internal static readonly List<string> _FireAnimations = new(4);

    public static void Init()
    {
        Lazy.SetupGun<PizzaPeel>(ItemName, ShortDescription, LongDescription, Lore, hideFromAmmonomicon: true)
          .SetAttributes(quality: ItemQuality.SPECIAL, gunClass: GunClass.RIFLE, reloadTime: 1.5f, ammo: 600, shootFps: _SHOOT_FPS, reloadFps: 4,
            muzzleFrom: Items.Mailbox, smoothReload: 0.1f, infiniteAmmo: true)
          .SetReloadAudio("pizza_flip_sound", 0, 8, 16, 20, 24, 28, 32)
          .Attach<PizzaPeelAmmoDisplay>()
          .AssignGun(out Gun gun)
          .InitProjectile(GunData.New(sprite: "pizza_projectile", clipSize: 4, cooldown: 0.06f, shootStyle: ShootStyle.SemiAutomatic,
            damage: 0.0f, speed: 25f, range: 18f, force: 12f, hitEnemySound: null, hitWallSound: null, pierceBreakables: true))
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

        _CashVFX = VFX.Create("cold_hard_cash_vfx", fps: 18, emissivePower: 1, emissiveColour: Color.green);
        _PizzaSliceVFX = VFX.Create("pizza_slice_vfx");
        _PizzaBox = VFX.Create("pizza_box");
        SpeculativeRigidbody body = _PizzaBox.GetOrAddComponent<SpeculativeRigidbody>();
        body.CanBePushed          = true;
        body.PixelColliders       = new List<PixelCollider>(){new(){
          ColliderGenerationMode = PixelCollider.PixelColliderGeneration.Manual,
          ManualOffsetX          = -13,
          ManualOffsetY          = -8,
          ManualWidth            = 26,
          ManualHeight           = 16,
          CollisionLayer         = CollisionLayer.HighObstacle,
          Enabled                = true,
          IsTrigger              = false,
        }};
        _PizzaBox.AddComponent<PizzaBox>();
    }

    public override void OnPlayerPickup(PlayerController player)
    {
        base.OnPlayerPickup(player);
        UpdateAnimations();
        this.gun.spriteAnimator.StopAndResetFrameToDefault();
        this.gun.spriteAnimator.Play(this.gun.idleAnimation);
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
        uic.GunAmmoCountLabel.Text = $"{time}[sprite \"stopwatch_ui\"]\n{cur}/{max}[sprite \"pizza_ui\"]";
        return true;
    }
}

public class PizzaBox : MonoBehaviour
{
    private const float _PARTICLE_RATE = 0.025f;

    private Vector2 _basePos;
    private float _nextParticle = 0f;

    public bool endsEvent = false;

    private void Start()
    {
        base.gameObject.GetComponent<SpeculativeRigidbody>().OnPreRigidbodyCollision += this.MaybeStartEvent;
        this._basePos = base.transform.position;
    }

    private void Update()
    {
        float now = BraveTime.ScaledTimeSinceStartup;
        float yOff = (3f / 16f) * Mathf.Sin(3f * now);
        Vector2 newPos = this._basePos + new Vector2(0f, yOff);;
        base.transform.position = newPos;
        if (this._nextParticle > now)
            return;

        CwaffVFX.Spawn(
            prefab         : VFX.SinglePixel,
            position       : newPos,
            velocity       : 2f * Vector2.up + Lazy.RandomVector(1f),
            lifetime       : 0.5f,
            emissivePower  : 3000f,
            overrideColor  : Color.red,
            emitColorPower : 8f,
            height         : 5f
          );
        this._nextParticle = now + _PARTICLE_RATE;
    }

    private void MaybeStartEvent(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
    {
        if (!otherRigidbody || otherRigidbody.gameObject.GetComponent<PlayerController>() is not PlayerController pc)
            return;

        CwaffVFX.SpawnBurst(
            prefab           : PizzaPeel._PizzaSliceVFX,
            numToSpawn       : 20,
            basePosition     : pc.CenterPosition,
            positionVariance : 1f,
            minVelocity      : 6f,
            velocityVariance : 2f,
            velType          : CwaffVFX.Vel.AwayRadial,
            rotType          : CwaffVFX.Rot.Random,
            lifetime         : 0.8f,
            fadeOutTime      : 0.25f,
            uniform          : true
          );

        if (endsEvent)
            PizzaTimeController.HandleDeliverySuccess();
        else
            PizzaTimeController.StartPizzaTime(pc);
        UnityEngine.Object.Destroy(base.gameObject);
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

        this._projectile.specRigidbody.OnPreRigidbodyCollision += this.OnPreRigidbodyCollision;
        this._projectile.OnHitEnemy += this.DoPizzaChecks;
    }

    private void OnPreRigidbodyCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
    {
        if (otherRigidbody && otherRigidbody.gameObject.GetComponent<WantsPizza>() is WantsPizza p && p.hasPizza)
            PhysicsEngine.SkipCollision = true; // quality of life to make sure overlapping enemies can all get pizza
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
        GameObject vfx = SpawnManager.SpawnVFX(PizzaPeel._CashVFX, enemy.sprite.WorldTopCenter + new Vector2(0f, 1f), Quaternion.identity, ignoresPools: true);
            vfx.GetComponent<tk2dSprite>().HeightOffGround = 1f;
            vfx.transform.parent = enemy.sprite.transform;
            vfx.AddComponent<GlowAndFadeOut>().Setup(
                fadeInTime: 0.25f, glowInTime: 0.50f, holdTime: 0.0f, glowOutTime: 0.50f, fadeOutTime: 0.25f, maxEmit: 50f, destroy: true);

        enemy.gameObject.Play("got_some_cash");

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
    internal static bool _PizzaTimeAttemptedThisFloor = false;
    internal static Gun _ExtantGun = null;
    internal static PlayerController _DeliveryBoi = null;
    internal static int _CurDeliveries = 0;
    internal static int _MaxDeliveries = 0;
    internal static bool _ScannedRoomsThisFloor = false;
    internal static float _PizzaEventTimer = 0f;
    internal static RoomHandler _ElevatorRoom = null;
    internal static RoomHandler _StartRoom = null;

    private static PizzaTimeController _Instance = null;
    private static int _MoneyItem = -1;
    private static readonly List<RoomHandler> _DeliveryRooms = new();
    private static readonly LinkedList<RoomHandler> _OccupiedRooms = new();
    private static readonly LinkedList<AIActor> _Hungrybois = new();
    private static bool _PlayingMusic = false;
    private static PizzaBox _GoalBox = null;

    internal static void MaybeSpawnPizzaAtExit(PlayerController player, RoomHandler oldRoom, RoomHandler newRoom)
    {
        if (_PizzaTimeHappening || _PizzaTimeAttemptedThisFloor)
            return;
        if (!CanStartPizzaTime(player))
        {
            // Lazy.DebugLog($"need to clear {_OccupiedRooms.Count} for pizza time");
            return;
        }
        // Lazy.DebugLog($"enabling pizza time event!");
        _PizzaTimeAttemptedThisFloor = true;
        if (_ElevatorRoom == null)
        {
            // Lazy.DebugLog($"...if we had an elevator room");
            return;
        }
        if (_StartRoom == null)
        {
            // Lazy.DebugLog($"...if we had a start room");
            return;
        }
        PizzaPeel._PizzaBox.Instantiate(position: _ElevatorRoom.GetBestRewardLocation(new IntVector2(1, 1)).ToVector2())
          .GetComponent<PizzaBox>().endsEvent = false;
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
        GameManager.Instance.DungeonMusicController.NotifyEnteredNewRoom(_StartRoom);
    }

    private static readonly string[] _ChestNames = ["Brown", "Blue", "Green", "Red", "Black"];

    public static void HandleDeliverySuccess()
    {
        // Lazy.DebugLog($"pizza delivered :D");

        int baseQuality = Lazy.GetFloorIndex() switch {
            >= 5f => 3, // B tier
            >= 3f => 2, // C tier
            >= 1f => 1, // D tier
            _     => 2, // C tier default
        };

        float completion = (float)_CurDeliveries / (float)_MaxDeliveries;
        int casings = _CurDeliveries;
        switch (completion)
        {
            case >= 1.0f:
                baseQuality += 1;  // upgrade another tier for perfect delivery
                break;
            case >= 0.9f:
                break; // base chest reward for 90% delivery
            case >= 0.5f:
                baseQuality = 0; // no chest reward under 90% delivery
                break;
            case < 0.5f:
                casings /= 2; // casing penalty for under 50% delivery
                break;
        }

        IntVector2 pos = _DeliveryBoi.CurrentRoom.GetCenteredVisibleClearSpot(2, 2, out bool success);

        if (baseQuality > 0)
        {
            if (UnityEngine.Random.value > 0.3f)
                baseQuality += 1; // 30% chance to upgrade quality

            GameObject rewardPrefab = _DeliveryBoi.GetRandomChestRewardOfQuality((ItemQuality)baseQuality);
            Chest chest = Lazy.SpawnChestWithSpecificItem(
              pickup: rewardPrefab.GetComponent<PickupObject>(),
              position: pos);
        }
        LootEngine.SpawnCurrency(pos.ToVector2(), casings, false, null, null);

        string report = $"{Mathf.RoundToInt(100f * completion)}% of pizzas delivered\n\n----------\n\nReward:\n- {casings} casings";
        if (baseQuality > 0)
            report += $"\n- {_ChestNames[baseQuality - 1]} Chest";
        CustomNoteDoer.CreateNote(pos.ToVector2() + new Vector2(0, 1f), report);

        EndPizzaTime();
    }

    public static void HandleDeliveryFailure()
    {
        // Lazy.DebugLog($"no pizza D:");
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
            List<Tuple<IntVector2, float>> spawnPositions = room.GetGoodSpotsInternal(2, 2);
            if (spawnPositions.Count > numEnemiesInRoom)
                spawnPositions.Shuffle();
            else if (spawnPositions.Count < numEnemiesInRoom)
                numEnemiesInRoom = spawnPositions.Count;
            for (int i = 0; i < numEnemiesInRoom; ++i)
            {
                IntVector2 clearSpot = spawnPositions[i].First;
                AIActor enemy = AIActor.Spawn(kinPrefab, clearSpot, room);
                enemy.IgnoreForRoomClear = true;
                enemy.CollisionDamage = 0f;
                // enemy.specRigidbody.CorrectForWalls(andRigidBodies: true);
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
        // Lazy.DebugLog($"spawned in {_Hungrybois.Count} bullet kin awaiting pizza delivery");
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

    private static void CalculateEventTimer()
    {
        _PizzaEventTimer = _Hungrybois.Count * (GameManager.IsTurboMode ? 3f : 4f);
        float floorIndex = Lazy.GetFloorIndex();
        if (floorIndex >= 1f)
            _PizzaEventTimer *= (0.8f + 0.2f * floorIndex);
        else
            _PizzaEventTimer *= 1.3f; // unsure what to do on custom floors, this seems fairish
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
        _PizzaTimeAttemptedThisFloor = false;
        _ElevatorRoom = null;
        _StartRoom = null;
    }

    private static bool RoomStillHasEnemies(RoomHandler room)
    {
        if (room.remainingReinforcementLayers != null && room.remainingReinforcementLayers.Count > 0)
            return true;
        if (room.activeEnemies == null)
            return false;
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
            if (room == null)
                continue;
            if (room.area.PrototypeRoomCategory == PrototypeDungeonRoom.RoomCategory.ENTRANCE)
                _StartRoom = room;
            if (room.area.PrototypeRoomCategory == PrototypeDungeonRoom.RoomCategory.EXIT)
                _ElevatorRoom = room;
            if (RoomStillHasEnemies(room))
                _OccupiedRooms.AddLast(room);
            if (room.IsStandardRoom && room.EverHadEnemies)
                _DeliveryRooms.Add(room);
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
        _Instance.gameObject.Play("pizza_event_start_sound");
        _PizzaTimeHappening = true; // prevent player from teleporting and some other stuff
        _DeliveryBoi = deliveryboi;
        _DeliveryBoi.OverrideHat(CwaffHats._PizzaHat, doPoof: true);
        GivePizzaPeel(deliveryboi); // give player the PizzaPeel and gun lock them
        SpawnHungryBulletKins(); // add bullet kin to a bunch of rooms
        CalculateEventTimer(); // figure out how long to give the player to complete the event
        //TODO: maybe disable map?
        DoPizzaTimeMusic();

        _GoalBox = PizzaPeel._PizzaBox.Instantiate(position: _StartRoom.GetBestRewardLocation(new IntVector2(1, 1)).ToVector2())
          .GetComponent<PizzaBox>();
        _GoalBox.endsEvent = true;
    }

    public static void EndPizzaTime(bool floorEnded = false)
    {
        // Lazy.DebugLog($"delivered {_CurDeliveries} / {_MaxDeliveries} pizzas");
        _Instance.gameObject.Play("pizza_event_start_sound");
        StopPizzaTimeMusic();
        if (!floorEnded)
            DespawnHungryBulletKins();
        DestroyPizzaPeel(_DeliveryBoi);
        _DeliveryBoi.ClearHatOverride(CwaffHats._PizzaHat, doPoof: true);
        _DeliveryBoi = null;
        _PizzaTimeHappening = false;
        if (_GoalBox)
        {
            UnityEngine.Object.Destroy(_GoalBox.gameObject);
            _GoalBox = null;
        }
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

    /// <summary>Force increased speed out of combat durring pizza event</summary>
    [HarmonyPatch]
    private static class IncreaseSpeedOOCDuringPizzaEventPatch
    {
        [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.Update))]
        [HarmonyILManipulator]
        private static void PlayerControllerUpdateIL(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<GameOptions>(nameof(GameOptions.IncreaseSpeedOutOfCombat))))
                return;
            cursor.CallPrivate(typeof(IncreaseSpeedOOCDuringPizzaEventPatch), nameof(ForceOOCSpeedIncrease));
        }

        private static bool ForceOOCSpeedIncrease(bool origValue)
        {
            return origValue || _PizzaTimeHappening;
        }
    }
}
