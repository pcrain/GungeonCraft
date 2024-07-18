namespace CwaffingTheGungy;

using static Femtobyte.HoldType;
using static Femtobyte.HoldSize;

/* TODO:
  Technical:
    - targeting reticle for placement
    - no overlaps when placing
    - removing objects from slots
    - make placeable objects limited
    - adding objects to slots
        - chests
        - tables
        - barrels
        - minor pickups (hearts, armor, ammo)
    - (maybe) prevent re-digitizing materialized objects
    - (maybe) multislot spanning items

  Presentational:
    - interaction delay after placement
    - better info on objects
    - switch shader from screen to some kind of overlay
    - better sounds
    - gun animations
    - gun shaders
    - projectile animations
    - projectile shaders
    - save serialization
*/

public class Femtobyte : CwaffGun
{
    public static string ItemName         = "Femtobyte";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    private const int _MAX_SLOTS = 12;

    internal static string _EmptyUI       = $"{C.MOD_PREFIX}:_SlotEmptyUI";
    internal static string _EmptyActiveUI = $"{C.MOD_PREFIX}:_SlotEmptyActiveUI";
    internal static string _FullUI        = $"{C.MOD_PREFIX}:_SlotFullUI";
    internal static string _FullActiveUI  = $"{C.MOD_PREFIX}:_SlotFullActiveUI";

    public enum HoldType { EMPTY, TABLE, BARREL, TRAP, CHEST, ENEMY, PICKUP }
    public enum HoldSize { SMALL, MEDIUM, LARGE, HUGE }

    public class DigitizedObject
    {
        public HoldType   type          = EMPTY;
        public int        slotSpan      = 1;

        public GameObject prefab        = null;

        public List<int>  contents      = null;
        public bool       locked        = false;

        public string     enemyGuid     = null;
        public bool       jammed        = false;

        public int        pickupID      = -1;
    }

    internal static readonly Dictionary<string, GameObject> _NameToPrefabMap = new(){
        // Traps
        {"trap_spinning_log_vertical_resizable",   ExoticObjects.Spinning_Log_Vertical },
        {"trap_spinning_log_vertical_gungeon_2x5", ExoticObjects.Spinning_Log_Vertical }, // technically not the same
        {"trap_spinning_log_vertical_gungeon_2x4", ExoticObjects.Spinning_Log_Vertical }, // technically not the same
        {"trap_spinning_log_vertical_gungeon_2x8", ExoticObjects.Spinning_Log_Vertical }, // technically not the same
        {"trap_spinning_log_horizontal_resizable", ExoticObjects.Spinning_Log_Horizontal },
        {"trap_sawblade_omni_gungeon_2x2",         ExoticObjects.SawBlade },
        {"minecart",                               ExoticObjects.Minecart },
        {"minecart_turret",                        ExoticObjects.TurretMinecart },
        {"skullfirespinner",                       ExoticObjects.FireBarTrap },
        {"flamepipe_spraysdown",                   ExoticObjects.FlamePipeNorth },
        {"flamepipe_spraysleft",                   ExoticObjects.FlamePipeEast },
        {"flamepipe_spraysright",                  ExoticObjects.FlamePipeWest },
        {"forge_hammer",                           LoadHelper.LoadAssetFromAnywhere<GameObject>("Forge_Hammer") },

        // Barrels
        {"red barrel"  ,                           LoadHelper.LoadAssetFromAnywhere<GameObject>("Red Barrel") },
        {"red drum",                               LoadHelper.LoadAssetFromAnywhere<GameObject>("Red Drum") },
        {"blue drum",                              LoadHelper.LoadAssetFromAnywhere<GameObject>("Blue Drum") },
        {"purple drum",                            LoadHelper.LoadAssetFromAnywhere<GameObject>("Purple Drum") },
        {"yellow drum",                            LoadHelper.LoadAssetFromAnywhere<GameObject>("Yellow Drum") },

        // Chests
        {"chest_wood_two_items",                   GameManager.Instance.RewardManager.D_Chest.gameObject },
        {"chest_silver",                           GameManager.Instance.RewardManager.C_Chest.gameObject },
        {"chest_green",                            GameManager.Instance.RewardManager.B_Chest.gameObject },
        {"chest_red",                              GameManager.Instance.RewardManager.A_Chest.gameObject },
        {"chest_black",                            GameManager.Instance.RewardManager.S_Chest.gameObject },
        {"chest_rainbow",                          GameManager.Instance.RewardManager.Rainbow_Chest.gameObject },
        {"chest_synergy",                          GameManager.Instance.RewardManager.Synergy_Chest.gameObject },
        {"truthchest",                             LoadHelper.LoadAssetFromAnywhere<GameObject>("TruthChest") },
        {"chest_rat",                              LoadHelper.LoadAssetFromAnywhere<GameObject>("Chest_Rat") },

        // Tables
        { "folding_table_vertical",                ItemHelper.Get(Items.PortableTableDevice).GetComponent<FoldingTableItem>().TableToSpawn.gameObject },
        { "kingofthehillbox",                      LoadHelper.LoadAssetFromAnywhere<GameObject>("_ChallengeManager")
                                                     .GetComponent<ChallengeManager>().PossibleChallenges[21].challenge.gameObject
                                                     .GetComponent<ZoneControlChallengeModifier>().BoxPlaceable.variantTiers[0].nonDatabasePlaceable },
        { "table_horizontal_steel",                ExoticObjects.SteelTableHorizontal },
        { "table_vertical_steel",                  ExoticObjects.SteelTableVertical },
        { "coffin_horizontal",                     LoadHelper.LoadAssetFromAnywhere<GameObject>("coffin_horizontal") },
        { "coffin_vertical",                       LoadHelper.LoadAssetFromAnywhere<GameObject>("coffin_vertical") },
        { "table_horizontal",                      LoadHelper.LoadAssetFromAnywhere<GameObject>("table_horizontal") },
        { "table_vertical",                        LoadHelper.LoadAssetFromAnywhere<GameObject>("table_vertical") },
        { "table_horizontal_stone",                LoadHelper.LoadAssetFromAnywhere<GameObject>("table_horizontal_stone") },
        { "table_vertical_stone",                  LoadHelper.LoadAssetFromAnywhere<GameObject>("table_vertical_stone") },
    };

    public List<DigitizedObject> digitizedObjects = Enumerable.Repeat<DigitizedObject>(default, _MAX_SLOTS).ToList();
    internal int _currentSlot = 0;

    public static void Init()
    {
        Gun gun = Lazy.SetupGun<Femtobyte>(ItemName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.C, gunClass: GunClass.RIFLE, reloadTime: 0.0f, ammo: 9999, canGainAmmo: false, infiniteAmmo: true,
                shootFps: 24, reloadFps: 16, modulesAreTiers: true, fireAudio: "fire_coin_sound", banFromBlessedRuns: true);
            //NOTE: modulesAreTiers with no 2nd module lets use switch to tier 1 to do cool alternate stuff without firing projectiles

        gun.InitProjectile(GunData.New(clipSize: -1, angleVariance: 15.0f, shootStyle: ShootStyle.SemiAutomatic, damage: 20.0f, speed: 44.0f,
          sprite: "femtobyte_projectile", fps: 2, anchor: Anchor.MiddleCenter)).Attach<FemtobyteProjectile>();

        foreach (var kvpair in _NameToPrefabMap)
            if (kvpair.Value == null)
                ETGModConsole.Log($"  failed to load prefab for {kvpair.Key}");

        gun.gameObject.AddComponent<FemtobyteAmmoDisplay>();
    }

    private static bool IsWhiteListedTrap(GameObject bodyObject, out GameObject trapPrefab)
    {
        string name = bodyObject.name.Replace("(Clone)","").TrimEnd().ToLowerInvariant();
        ETGModConsole.Log($"looking up {name} in trap whitelist");
        if (!_NameToPrefabMap.TryGetValue(name, out trapPrefab))
            trapPrefab = null;
        return trapPrefab != null;
    }

    private bool DigitizeEnemy(AIActor enemy)
    {
        if (enemy.healthHaver is not HealthHaver hh || hh.IsDead || hh.IsBoss || hh.IsSubboss)
            return false;
        CwaffShaders.Digitize(enemy.sprite);
        enemy.EraseFromExistenceWithRewards();
        return true;
    }

    private bool DigitizePickup(PickupObject pickup)
    {
        CwaffShaders.Digitize(pickup.sprite);
        UnityEngine.Object.Destroy(pickup.gameObject);
        return true;
    }

    private bool DigitizeChest(Chest chest)
    {
        if (chest.IsOpen || chest.IsBroken)
            return false;
        CwaffShaders.Digitize(chest.sprite);
        UnityEngine.Object.Destroy(chest.gameObject);
        return true;
    }

    private bool DigitizeBarrel(KickableObject barrel)
    {
        CwaffShaders.Digitize(barrel.sprite);
        UnityEngine.Object.Destroy(barrel.gameObject);
        return true;
    }

    private bool DigitizeTable(FlippableCover table)
    {
        digitizedObjects.Add(new(){
            type          = TABLE,
            prefab        = ExoticObjects.SteelTableHorizontal, //TODO: use actual prefab
            slotSpan      = 1,
        });
        CwaffShaders.Digitize(table.sprite);
        UnityEngine.Object.Destroy(table.gameObject);
        return true;
    }

    private bool DigitizeTrap(GameObject trapPrefab, SpeculativeRigidbody body)
    {
        ETGModConsole.Log($"  got prefab {trapPrefab.name}");
        if (body.sprite is tk2dSlicedSprite sliced)
            CwaffShaders.Digitize<tk2dSlicedSprite>(sliced);
        else
            CwaffShaders.Digitize(body.sprite);
        UnityEngine.Object.Destroy(body.gameObject);
        return true;
    }

    public bool TryToDigitize(SpeculativeRigidbody body)
    {
        DigitizedObject d = this.digitizedObjects[this._currentSlot];
        if (d != null && d.type != EMPTY)
            return false; // can't digitize if we don't have an available slot

        GameObject bodyObject = body.gameObject;
        if (bodyObject.GetComponent<AIActor>() is AIActor enemy)
            return DigitizeEnemy(enemy);
        if (bodyObject.GetComponent<PickupObject>() is PickupObject pickup)
            return DigitizePickup(pickup);
        if (bodyObject.GetComponent<Chest>() is Chest chest)
            return DigitizeChest(chest);
        if (bodyObject.GetComponent<KickableObject>() is KickableObject barrel)
            return DigitizeBarrel(barrel);
        if (bodyObject.transform.parent is Transform tp && tp.gameObject.GetComponent<FlippableCover>() is FlippableCover table)
            return DigitizeTable(table);

        //TODO: traps are trickier to place where they didn't originally belong, so finish this later
        // if (IsWhiteListedTrap(bodyObject, out GameObject trapPrefab))
        //     return DigitizeTrap(trapPrefab, body);

        return false;
    }

    private void OnTriedToInitiateAttack(PlayerController player)
    {
        if (this.gun.CurrentStrengthTier == 0)
            return; // empty slot, do normal firing stuff
        BraveInput.GetInstanceForPlayer(player.PlayerIDX).ConsumeButtonDown(GungeonActions.GungeonActionType.Shoot);
        PlaceDigitizedObject();
    }

    public string GetTitleForCurrentSlot()
    {
        DigitizedObject d = this.digitizedObjects[this._currentSlot];
        if (d == null)
            return "Empty";
        switch (d.type)
        {
            case EMPTY:  return "Empty";
            case CHEST:  return "Chest";
            case ENEMY:  return "Enemy";
            case PICKUP: return "Pickup";
            case TRAP:   return "Trap";
            case BARREL: return "Barrel";
            case TABLE:  return "Table";
        }
        return "Unknown";
    }

    bool _DidDebugSetup = false;
    public override void OnPlayerPickup(PlayerController player)
    {
        base.OnPlayerPickup(player);
        player.OnTriedToInitiateAttack += this.OnTriedToInitiateAttack;
        UpdateStrengthTier();
        if (!C.DEBUG_BUILD || _DidDebugSetup)
            return;

        _DidDebugSetup = true;
        int i = 0;
        this.digitizedObjects[i++] = new(){ type = TABLE, prefab = ExoticObjects.SteelTableHorizontal };
        this.digitizedObjects[i++] = new(){ type = BARREL, prefab = _NameToPrefabMap["red barrel"] };
        this.digitizedObjects[i++] = new(){ type = BARREL, prefab = _NameToPrefabMap["blue drum"] };
        this.digitizedObjects[i++] = new(){ type = CHEST, prefab = _NameToPrefabMap["chest_silver"], contents = [(int)Items.Akey47], locked = true };
        UpdateStrengthTier();
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        player.OnTriedToInitiateAttack -= this.OnTriedToInitiateAttack;
        base.OnDroppedByPlayer(player);
    }

    public override void OnDestroy()
    {
        if (this.PlayerOwner)
            this.PlayerOwner.OnTriedToInitiateAttack -= this.OnTriedToInitiateAttack;
        base.OnDestroy();
    }

    // TODO: update mtgapi so that this actually works
    public override void OnFirstPickup(PlayerController player)
    {
    }

    private void UpdateStrengthTier()
    {
        DigitizedObject d = this.digitizedObjects[this._currentSlot];
        this.gun.CurrentStrengthTier = (d == null || d.type == EMPTY) ? 0 : 1;
    }

    public override void OnReloadPressed(PlayerController player, Gun gun, bool manualReload)
    {
        if (!manualReload)
            return;

        this._currentSlot = (this._currentSlot + 1) % _MAX_SLOTS;
        UpdateStrengthTier();
        player.gameObject.Play("replicant_select_sound");
    }

    private void PlaceDigitizedObject()
    {
        Vector2 placePoint = this.PlayerOwner.unadjustedAimPoint;
        RoomHandler room = placePoint.GetAbsoluteRoom();
        if (room == null)
            return;
        if (digitizedObjects.Count != _MAX_SLOTS)
        {
            Lazy.RuntimeWarn("Digitized Object list is smaller than it should be; this should never happen");
            return;
        }
        if (digitizedObjects[this._currentSlot] is not DigitizedObject d || d.type == EMPTY)
            return;

        ETGModConsole.Log($"spawning in {d.prefab.name} at cursor position {placePoint}");
        switch (d.type)
        {
            case TABLE:
                GameObject table = UnityEngine.Object.Instantiate(d.prefab, placePoint, Quaternion.identity);
                tk2dSprite tableSprite = table.GetComponentInChildren<tk2dSprite>();
                tableSprite.PlaceAtPositionByAnchor(placePoint, Anchor.MiddleCenter);
                SpeculativeRigidbody tableBody = table.GetComponentInChildren<SpeculativeRigidbody>();
                FlippableCover cover = table.GetComponent<FlippableCover>();
                room.RegisterInteractable(cover);
                cover.ConfigureOnPlacement(room);
                tableBody.Initialize();
                PhysicsEngine.Instance.RegisterOverlappingGhostCollisionExceptions(tableBody, null, false);
                CwaffShaders.Materialize(tableSprite);
                break;
            case BARREL:
                GameObject barrel = UnityEngine.Object.Instantiate(d.prefab, placePoint, Quaternion.identity);
                tk2dSprite barrelSprite = barrel.GetComponentInChildren<tk2dSprite>();
                barrelSprite.PlaceAtPositionByAnchor(placePoint, Anchor.MiddleCenter);
                SpeculativeRigidbody barrelBody = barrel.GetComponentInChildren<SpeculativeRigidbody>();
                KickableObject kickable = barrel.GetComponentInChildren<KickableObject>();
                room.RegisterInteractable(kickable);
                kickable.ConfigureOnPlacement(room);
                barrelBody.Initialize();
                PhysicsEngine.Instance.RegisterOverlappingGhostCollisionExceptions(barrelBody, null, false);
                CwaffShaders.Materialize(barrelSprite);
                break;
            case CHEST:
                GameObject chestObject = UnityEngine.Object.Instantiate(d.prefab, placePoint, Quaternion.identity);
                Chest chest = chestObject.GetComponent<Chest>();
                chest.Initialize();
                tk2dBaseSprite chestSprite = chest.sprite;
                chestSprite.UpdateZDepth();
                chestSprite.PlaceAtPositionByAnchor(placePoint, Anchor.MiddleCenter);
                chest.MimicGuid = null;
                chest.m_room = room;
                chest.IsLocked = d.locked;
                if (chest.LockAnimator != null)
                    chest.LockAnimator.GetComponent<Renderer>().enabled = chest.IsLocked;
                chest.contents = null;
                if (d.contents != null && d.contents.Count > 0)
                {
                    chest.contents = new(d.contents.Count);
                    foreach (int id in d.contents)
                        chest.contents.Add(PickupObjectDatabase.GetById(id));
                }
                room.RegisterInteractable(chest);
                PhysicsEngine.Instance.RegisterOverlappingGhostCollisionExceptions(chest.specRigidbody);
                CwaffShaders.Materialize(chestSprite);
                break;
            default:
                break;
        }
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        if (projectile.gameObject.GetComponent<FemtobyteProjectile>() is not FemtobyteProjectile fp)
            return;
        fp.Setup(this);
    }

    private class FemtobyteAmmoDisplay : CustomAmmoDisplay
    {
        private static StringBuilder _SB = new StringBuilder("", 1000);

        private Femtobyte _femto;
        private PlayerController _owner;

        private void Start()
        {
            Gun gun     = base.GetComponent<Gun>();
            this._femto = gun.GetComponent<Femtobyte>();
            this._owner = gun.CurrentOwner as PlayerController;
        }

        public override bool DoCustomAmmoDisplay(GameUIAmmoController uic)
        {
            if (!this._owner)
                return false;

            uic.SetAmmoCountLabelColor(Color.white);
            uic.GunAmmoCountLabel.AutoHeight = true; // enable multiline text
            uic.GunAmmoCountLabel.ProcessMarkup = true; // enable multicolor text

            _SB.Length = 0;
            _SB.Append(this._femto.GetTitleForCurrentSlot());
            _SB.Append("\n\n");
            for (int i = 0; i < _MAX_SLOTS; ++i)
            {
                if (i == _MAX_SLOTS / 2)
                    _SB.Append("\n");
                DigitizedObject d = this._femto.digitizedObjects[i];
                bool used = d != null && d.type != EMPTY;
                if (used)
                    _SB.AppendFormat("[sprite \"{0}\"]", i == this._femto._currentSlot ? Femtobyte._FullActiveUI : Femtobyte._FullUI);
                else
                    _SB.AppendFormat("[sprite \"{0}\"]", i == this._femto._currentSlot ? Femtobyte._EmptyActiveUI : Femtobyte._EmptyUI);
            }
            uic.GunAmmoCountLabel.Text = _SB.ToString();
            return true;
        }
    }
}

public class FemtobyteProjectile : MonoBehaviour
{
    private Projectile _projectile;
    private PlayerController _owner;
    private Femtobyte _femtobyte;

    public void Setup(Femtobyte femtobyte)
    {
        this._projectile = base.GetComponent<Projectile>();
        this._owner = this._projectile.Owner as PlayerController;
        this._femtobyte = femtobyte;
        this._projectile.specRigidbody.OnPreRigidbodyCollision += this.OnPreRigidbodyCollision;
        this._projectile.specRigidbody.AddCollisionLayerOverride(CollisionMask.LayerToMask(CollisionLayer.PlayerHitBox));
        this._projectile.specRigidbody.AddCollisionLayerOverride(CollisionMask.LayerToMask(CollisionLayer.Trap));
    }

    private void OnPreRigidbodyCollision(SpeculativeRigidbody body, PixelCollider myCollider, SpeculativeRigidbody otherBody, PixelCollider otherCollider)
    {
        if (!this._owner || !this._projectile || !this._femtobyte || !otherBody)
            return;
        if (!this._femtobyte.TryToDigitize(otherBody))
            return;
        this._projectile.DieInAir(false, false, false, false);
        PhysicsEngine.SkipCollision = true;
    }

    private void Update()
    {
        if (!this._owner || !this._projectile || !this._femtobyte)
            return;
        TryToCollideWithPickups();
    }

    private void TryToCollideWithPickups()
    {
        IPlayerInteractable nearestIxable = this._owner.CurrentRoom.GetNearestInteractable(this._projectile.SafeCenter, 1f, this._owner);
        if (nearestIxable is not PickupObject pickup || pickup.IsBeingEyedByRat)
            return;
        if (pickup.GetComponent<SpeculativeRigidbody>() is not SpeculativeRigidbody body)
            return;
        if (!this._femtobyte.TryToDigitize(body))
            return;
        this._projectile.DieInAir(false, false, false, false);
    }
}
