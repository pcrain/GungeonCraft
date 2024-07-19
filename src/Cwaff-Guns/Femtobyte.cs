namespace CwaffingTheGungy;

using static Femtobyte.HoldType;
using static Femtobyte.HoldSize;

/* TODO:
  Technical:
    - save serialization

  Presentational:
    - better sounds
*/

public class Femtobyte : CwaffGun
{
    public static string ItemName         = "Femtobyte";
    public static string ShortDescription = "Digital Storage";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    private const int _MAX_SLOTS = 6;

    internal const string _EmptyUI       = $"{C.MOD_PREFIX}:_SlotEmptyUI";
    internal const string _EmptyActiveUI = $"{C.MOD_PREFIX}:_SlotEmptyActiveUI";
    internal const string _FullUI        = $"{C.MOD_PREFIX}:_SlotFullUI";
    internal const string _FullActiveUI  = $"{C.MOD_PREFIX}:_SlotFullActiveUI";
    internal static GameObject _ImpactBits = null;

    public enum HoldType { EMPTY, TABLE, BARREL, SPECIAL, CHEST, ENEMY, PICKUP }
    public enum HoldSize { SMALL, MEDIUM, LARGE, HUGE }
    public class PrefabData
    {
        public string name;
        public GameObject prefab;
        public PrefabData(string name, GameObject prefab)
        {
            this.name = name;
            this.prefab = prefab;
        }
    }

    public class DigitizedObject
    {
        public HoldType   type          = EMPTY;
        public int        slotSpan      = 1;

        public PrefabData data          = null;

        public List<int>  contents      = null;
        public bool       locked        = false;
        public bool       glitched      = false;
        public bool       rainbow       = false;

        public string     enemyGuid     = null;
        public bool       jammed        = false;

        public int        pickupID      = -1;

        public static DigitizedObject FromPickup(PickupObject pickup)
        {
            return new(){
                type      = PICKUP,
                pickupID  = pickup.PickupObjectId,
                data      = new(pickup.EncounterNameOrDisplayName, PickupObjectDatabase.GetById(pickup.PickupObjectId).gameObject),
            };
        }
    }

    internal static readonly Dictionary<string, PrefabData> _NameToPrefabMap = new(){
        // Janky Traps (don't work sensibly, cause visual glitches or worse)
        // {"minecart",                               new("Minecart", ExoticObjects.Minecart) },
        // {"minecart_turret",                        new("Minecart Turret", ExoticObjects.TurretMinecart) },
        // {"trap_spinning_log_vertical_resizable",   new("Spike Log V", ExoticObjects.Spinning_Log_Vertical) },
        // {"trap_spinning_log_vertical_gungeon_2x5", ExoticObjects.Spinning_Log_Vertical }, // technically not the same
        // {"trap_spinning_log_vertical_gungeon_2x4", ExoticObjects.Spinning_Log_Vertical }, // technically not the same
        // {"trap_spinning_log_vertical_gungeon_2x8", ExoticObjects.Spinning_Log_Vertical }, // technically not the same
        // {"trap_spinning_log_horizontal_resizable", ExoticObjects.Spinning_Log_Horizontal },

        // Special
        {"npc_gunbermuncher",                      new("Muncher", LoadHelper.LoadAssetFromAnywhere<SharedInjectionData>("Base Shared Injection Data")
                                                     .AttachedInjectionData[2].InjectionData[0].exactRoom.placedObjects[11].nonenemyBehaviour.gameObject) },
        {"npc_gunbermuncher_evil",                 new("Evil Muncher", GameManager.Instance.GlobalInjectionData.entries[3]
                                                     .injectionData.InjectionData[5].exactRoom.placedObjects[0].nonenemyBehaviour.gameObject) },

        // Traps
        {"trap_sawblade_omni_gungeon_2x2",         new("Sawblade", ExoticObjects.SawBlade) },
        {"skullfirespinner",                       new("Skull Fire Trap", ExoticObjects.FireBarTrap) },
        {"flamepipe_spraysdown",                   new("Flame Pipe N", ExoticObjects.FlamePipeNorth) },
        {"flamepipe_spraysleft",                   new("Flame Pipe E", ExoticObjects.FlamePipeEast) },
        {"flamepipe_spraysright",                  new("Flame Pipe W", ExoticObjects.FlamePipeWest) },
        {"forge_hammer",                           new("Forge Hammer", LoadHelper.LoadAssetFromAnywhere<GameObject>("Forge_Hammer")) },
        {"brazier",                                new("Brazier", LoadHelper.LoadAssetFromAnywhere<SharedInjectionData>("Base Shared Injection Data")
                                                     .InjectionData[1].roomTable.includedRooms.elements[6].room.placedObjects[4].placeableContents
                                                     .variantTiers[0].nonDatabasePlaceable) },

        // Barrels
        {"red barrel"  ,                           new("Exposive Barrel", LoadHelper.LoadAssetFromAnywhere<GameObject>("Red Barrel")) },
        {"red drum",                               new("Exposive Drum", LoadHelper.LoadAssetFromAnywhere<GameObject>("Red Drum")) },
        {"blue drum",                              new("Water Drum", LoadHelper.LoadAssetFromAnywhere<GameObject>("Blue Drum")) },
        {"purple drum",                            new("Oil Drum", LoadHelper.LoadAssetFromAnywhere<GameObject>("Purple Drum")) },
        {"yellow drum",                            new("Poison Drum", LoadHelper.LoadAssetFromAnywhere<GameObject>("Yellow Drum")) },

        // Chests
        {"chest_wood_two_items",                   new("Brown Chest", GameManager.Instance.RewardManager.D_Chest.gameObject) },
        {"chest_silver",                           new("Blue Chest", GameManager.Instance.RewardManager.C_Chest.gameObject) },
        {"chest_green",                            new("Green Chest", GameManager.Instance.RewardManager.B_Chest.gameObject) },
        {"chest_red",                              new("Red Chest", GameManager.Instance.RewardManager.A_Chest.gameObject) },
        {"chest_black",                            new("Black Chest", GameManager.Instance.RewardManager.S_Chest.gameObject) },
        {"chest_rainbow",                          new("Rainbow Chest", GameManager.Instance.RewardManager.Rainbow_Chest.gameObject) },
        {"chest_synergy",                          new("Synergy Chest", GameManager.Instance.RewardManager.Synergy_Chest.gameObject) },
        {"truthchest",                             new("Albern's Chest", LoadHelper.LoadAssetFromAnywhere<GameObject>("TruthChest")) },
        {"chest_rat",                              new("Rat Chest", LoadHelper.LoadAssetFromAnywhere<GameObject>("Chest_Rat")) },

        // Tables
        { "folding_table_vertical",                new("Folding Table", ItemHelper.Get(Items.PortableTableDevice).GetComponent<FoldingTableItem>().TableToSpawn.gameObject) },
        { "kingofthehillbox",                      new("KotH Table", LoadHelper.LoadAssetFromAnywhere<GameObject>("_ChallengeManager")
                                                     .GetComponent<ChallengeManager>().PossibleChallenges[21].challenge.gameObject
                                                     .GetComponent<ZoneControlChallengeModifier>().BoxPlaceable.variantTiers[0].nonDatabasePlaceable) },
        { "table_horizontal_steel",                new("Steel Table H", ExoticObjects.SteelTableHorizontal) },
        { "table_vertical_steel",                  new("Steel Table V", ExoticObjects.SteelTableVertical) },
        { "coffin_horizontal",                     new("Coffin H", LoadHelper.LoadAssetFromAnywhere<GameObject>("coffin_horizontal")) },
        { "coffin_vertical",                       new("Coffin V", LoadHelper.LoadAssetFromAnywhere<GameObject>("coffin_vertical")) },
        { "table_horizontal",                      new("Wood Table H", LoadHelper.LoadAssetFromAnywhere<GameObject>("table_horizontal")) },
        { "table_vertical",                        new("Wood Table V", LoadHelper.LoadAssetFromAnywhere<GameObject>("table_vertical")) },
        { "table_horizontal_stone",                new("Stone Table H", LoadHelper.LoadAssetFromAnywhere<GameObject>("table_horizontal_stone")) },
        { "table_vertical_stone",                  new("Stone Table V", LoadHelper.LoadAssetFromAnywhere<GameObject>("table_vertical_stone")) },
    };

    public List<DigitizedObject> digitizedObjects = Enumerable.Repeat<DigitizedObject>(default, _MAX_SLOTS).ToList();
    private tk2dBaseSprite _placementPhantom = null;
    private Material _placementMaterial = null;
    internal int _currentSlot = 0;
    internal bool _displayNameDirty = false;
    private bool _suppressNextClick = false;

    public static void Init()
    {
        Gun gun = Lazy.SetupGun<Femtobyte>(ItemName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.C, gunClass: CwaffGunClass.UTILITY, reloadTime: 0.0f, ammo: 9999, canGainAmmo: false,
                infiniteAmmo: true, shootFps: 24, reloadFps: 16, modulesAreTiers: true, fireAudio: "fire_coin_sound", banFromBlessedRuns: true);
            //NOTE: modulesAreTiers with no 2nd module lets use switch to tier 1 to do cool alternate stuff without firing projectiles

        Projectile proj = gun.InitProjectile(GunData.New(clipSize: -1, angleVariance: 2.0f, shootStyle: ShootStyle.SemiAutomatic, damage: 7.5f, speed: 90.0f,
          cooldown: 0.4f, sprite: "femtobyte_projectile", fps: 2, anchor: Anchor.MiddleCenter)).Attach<FemtobyteProjectile>();
        TrailController tc = proj.AddTrailToProjectilePrefab("femtobyte_beam", fps: 10, cascadeTimer: C.FRAME, softMaxLength: 1f, destroyOnEmpty: false);
            tc.gameObject.AddComponent<TrailControllerHotfix.Fix>(); //NOTE: high speed projectiles don't always collide with walls cleanly in vanilla, so patch that

        _ImpactBits = VFX.Create("femtobyte_projectile_vfx", fps: 1, loops: false, anchor: Anchor.MiddleCenter);

        gun.gameObject.AddComponent<FemtobyteAmmoDisplay>();

        // foreach (var kvpair in _NameToPrefabMap)
        //     if (kvpair.Value == null || kvpair.Value.prefab == null)
        //         ETGModConsole.Log($"  failed to load prefab for {kvpair.Key}");
    }

    private static bool IsWhiteListedPrefab(GameObject bodyObject, out PrefabData trapPrefab)
    {
        string name = bodyObject.name.Replace("(Clone)","").TrimEnd().ToLowerInvariant();
        // ETGModConsole.Log($"looking up {name} in prefab whitelist");
        if (!_NameToPrefabMap.TryGetValue(name, out trapPrefab))
            trapPrefab = null;
        return trapPrefab != null;
    }

    private bool DigitizeEnemy(AIActor enemy, PrefabData data)
    {
        if (enemy.healthHaver is not HealthHaver hh || hh.IsDead || hh.IsBoss || hh.IsSubboss)
            return false;
        CwaffShaders.Digitize(enemy.sprite);
        enemy.EraseFromExistenceWithRewards();
        return true;
    }

    private bool DigitizePickup(PickupObject pickup, PrefabData data)
    {
        if (pickup.PickupObjectId < 0)
            return false;

        if (pickup is HealthPickup health)
            health.GetRidOfMinimapIcon();
        else if (pickup is AmmoPickup ammo)
            ammo.GetRidOfMinimapIcon();
        else if (pickup is KeyBulletPickup key)
            key.GetRidOfMinimapIcon();
        else if (pickup is SilencerItem blank)
            blank.GetRidOfMinimapIcon();
        else
            return false;

        SetCurrentSlot(DigitizedObject.FromPickup(pickup));
        CwaffShaders.Digitize(pickup.sprite);
        UnityEngine.Object.Destroy(pickup.gameObject);
        return true;
    }

    private bool DigitizeChest(Chest chest, PrefabData data)
    {
        if (chest.IsOpen || chest.IsBroken || chest.IsMimic)
            return false;
        if (data.prefab != null)
            SetCurrentSlot(new(){
                type      = CHEST,
                data      = data,
                locked    = chest.IsLocked,
                glitched  = chest.IsGlitched,
                rainbow   = chest.IsRainbowChest,
                contents  = chest.contents != null ? chest.contents.Select(p => p ? p.PickupObjectId : -1).ToList() : null,
            });
        CwaffShaders.Digitize(chest.sprite);
        if (chest.GetAbsoluteParentRoom() is RoomHandler room)
            room.DeregisterInteractable(chest);
        chest.DeregisterChestOnMinimap();
        UnityEngine.Object.Destroy(chest.gameObject);
        return true;
    }

    private bool DigitizeBarrel(KickableObject barrel, PrefabData data)
    {
        if (data.prefab != null)
            SetCurrentSlot(new(){
                type      = BARREL,
                data      = data,
            });
        CwaffShaders.Digitize(barrel.sprite);
        UnityEngine.Object.Destroy(barrel.gameObject);
        return true;
    }

    private bool DigitizeTable(FlippableCover table, PrefabData data)
    {
        if (data.prefab != null)
            SetCurrentSlot(new(){
                type      = TABLE,
                data      = data,
            });
        CwaffShaders.Digitize(table.sprite);
        UnityEngine.Object.Destroy(table.gameObject);
        return true;
    }

    private bool DigitizeSpecial(SpeculativeRigidbody body, PrefabData data)
    {
        if (data.prefab != null)
            SetCurrentSlot(new(){
                type      = SPECIAL,
                data      = data,
            });
        // ETGModConsole.Log($"  got prefab {data.name} == {data.prefab.name}");
        if (body.sprite is tk2dSlicedSprite sliced)
            CwaffShaders.Digitize<tk2dSlicedSprite>(sliced);
        else
            CwaffShaders.Digitize(body.sprite);
        UnityEngine.Object.Destroy(body.gameObject);
        return true;
    }

    private void SetCurrentSlot(DigitizedObject d)
    {
        this.digitizedObjects[this._currentSlot] = d;
        this.PlayerOwner.gameObject.Play("replicant_select_new_sound");
        UpdateCurrentSlot();
    }

    public bool TryToDigitize(GameObject target)
    {
        DigitizedObject d = this.digitizedObjects[this._currentSlot];
        if (d != null && d.type != EMPTY)
            return false; // can't digitize if we don't have an available slot

        SpeculativeRigidbody body = target.GetComponent<SpeculativeRigidbody>();
        if (!IsWhiteListedPrefab(target, out PrefabData data))
            data = null;
        if (target.GetComponent<AIActor>() is AIActor enemy)
            return DigitizeEnemy(enemy, data);
        if (target.GetComponent<PickupObject>() is PickupObject pickup)
            return DigitizePickup(pickup, data);
        if (target.GetComponent<Chest>() is Chest chest)
            return DigitizeChest(chest, data);
        if (target.GetComponent<KickableObject>() is KickableObject barrel)
            return DigitizeBarrel(barrel, data);
        if (target.transform.parent is Transform tp && tp.gameObject.GetComponent<FlippableCover>() is FlippableCover table)
            return DigitizeTable(table, data);
        if (data != null && data.prefab != null)
            return DigitizeSpecial(body, data); //TODO: traps are trickier to place where they didn't originally belong, so finish this later

        return false;
    }

    private void OnTriedToInitiateAttack(PlayerController player)
    {
        if (!player || player.CurrentGun != this.gun)
            return; // inactive, do normal firing stuff
        if (this.gun.CurrentStrengthTier == 0)
            return; // empty slot, do normal firing stuff
        Vector2 pos = player.unadjustedAimPoint;
        if (!this.CanPlacePhantom(pos))
            return; // invalid position
        PlaceDigitizedObject(pos);
        player.SuppressThisClick = true;
    }

    private bool CanPlacePhantom(Vector2 pos, DigitizedObject d = null)
    {
        d ??= this.digitizedObjects[this._currentSlot];
        if (d == null || d.data == null || d.data.prefab == null || d.data.prefab.GetComponentInChildren<tk2dSprite>() is not tk2dSprite sprite)
            return false;

        Vector2 radius = 0.5f * sprite.GetBounds().extents;
        foreach (ICollidableObject collidable in PhysicsEngine.Instance.GetOverlappingCollidableObjects(
            min : pos - radius, max : pos + radius, collideWithTiles : true, collideWithRigidbodies : true,
            layerMask : null, includeTriggers : false ))
        {
            if (collidable is not SpeculativeRigidbody body)
                return false;  // tile, no good
            GameObject go = body.gameObject;
            if (go.GetComponent<MajorBreakable>())
                return false;
            if (go.GetComponent<GameActor>())
                return false;
            if (!go.GetComponent<MinorBreakable>() && !go.GetComponent<DebrisObject>())
                return false;
            // safe for now, continue
        }
        return true;
    }

    public string GetTitleForCurrentSlot()
    {
        DigitizedObject d = this.digitizedObjects[this._currentSlot];
        if (d == null)
            return "Empty";
        if (d.data != null && !d.data.name.IsNullOrWhiteSpace())
            return d.data.name;
        switch (d.type)
        {
            case EMPTY:  return "Empty";
            case CHEST:  return "Chest";
            case ENEMY:  return "Enemy";
            case PICKUP: return "Pickup";
            case SPECIAL:   return "Trap";
            case BARREL: return "Barrel";
            case TABLE:  return "Table";
        }
        return "Unknown";
    }

    bool _DidDebugSetup = false;
    public override void OnPlayerPickup(PlayerController player)
    {
        base.OnPlayerPickup(player);
        AdjustGunShader(on: true);
        player.OnTriedToInitiateAttack += this.OnTriedToInitiateAttack;
        UpdateCurrentSlot();

        // if (!C.DEBUG_BUILD || _DidDebugSetup)
        //     return;
        // _DidDebugSetup = true;
        // int i = 0;
        // this.digitizedObjects[i++] = new(){ type = SPECIAL, data = _NameToPrefabMap["forge_hammer"] };
        // this.digitizedObjects[i++] = new(){ type = SPECIAL, data = _NameToPrefabMap["trap_sawblade_omni_gungeon_2x2"] };
        // this.digitizedObjects[i++] = new(){ type = SPECIAL, data = _NameToPrefabMap["skullfirespinner"] };
        // this.digitizedObjects[i++] = new(){ type = SPECIAL, data = _NameToPrefabMap["flamepipe_spraysdown"] };
        // this.digitizedObjects[i++] = DigitizedObject.FromPickup(Items.Ak47.AsGun());
        // this.digitizedObjects[i++] = new(){ type = SPECIAL, data = _NameToPrefabMap["brazier"] };
        // this.digitizedObjects[i++] = new(){ type = SPECIAL, data = _NameToPrefabMap["npc_gunbermuncher_evil"] };
        // this.digitizedObjects[i++] = new(){ type = TABLE,  data = _NameToPrefabMap["table_horizontal_steel"] };
        // this.digitizedObjects[i++] = new(){ type = BARREL, data = _NameToPrefabMap["red barrel"] };
        // this.digitizedObjects[i++] = new(){ type = BARREL, data = _NameToPrefabMap["blue drum"] };
        // this.digitizedObjects[i++] = new(){ type = CHEST,  data = _NameToPrefabMap["chest_silver"], contents = [(int)Items.Akey47], locked = true };
        // UpdateCurrentSlot();
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        AdjustGunShader(on: false);
        if (this._placementPhantom)
            UnityEngine.Object.Destroy(this._placementPhantom);
        player.OnTriedToInitiateAttack -= this.OnTriedToInitiateAttack;
        base.OnDroppedByPlayer(player);
    }

    public void AdjustGunShader(bool on)
    {
        Material m = this.gun.sprite.renderer.material;
        if (!on)
        {
            this.gun.sprite.usesOverrideMaterial = false;
            m.shader = ShaderCache.Acquire("Brave/PlayerShader");
            return;
        }
        this.gun.sprite.usesOverrideMaterial = true;
        m.shader = CwaffShaders.UnlitDigitizeShader;
        m.SetTexture("_BinaryTex", CwaffShaders.DigitizeTexture);
        m.SetFloat("_BinarizeProgress", 1.0f);
        m.SetFloat("_ColorizeProgress", 0.0f);
        m.SetFloat("_FadeProgress", 0.0f);
        m.SetFloat("_ScrollSpeed", 1.5f);
        m.SetFloat("_HScrollSpeed", 0.35f);
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        base.OnSwitchedAwayFromThisGun();
        if (this._placementPhantom)
            this._placementPhantom.gameObject.SetActive(false);
    }

    public override void OnDestroy()
    {
        if (this.PlayerOwner)
            this.PlayerOwner.OnTriedToInitiateAttack -= this.OnTriedToInitiateAttack;
        if (this._placementPhantom)
            UnityEngine.Object.Destroy(this._placementPhantom);
        base.OnDestroy();
    }

    private static readonly Color _Valid = Color.Lerp(Color.green, Color.black, 0.35f);
    private static readonly Color _Invalid = Color.Lerp(Color.red, Color.black, 0.35f);
    public override void Update()
    {
        base.Update();
        if (this.PlayerOwner is not PlayerController player)
            return;
        if (!this._placementPhantom)
        {
            this._placementPhantom = Lazy.SpriteObject(player.sprite.collection, player.sprite.spriteId);
            this._placementPhantom.usesOverrideMaterial = true;
            Material m = this._placementMaterial = this._placementPhantom.gameObject.GetComponent<Renderer>().material;
            // m.shader = CwaffShaders.DigitizeShader;
            m.shader = CwaffShaders.UnlitDigitizeShader;
            m.SetTexture("_BinaryTex", CwaffShaders.DigitizeTexture);
            m.SetFloat("_BinarizeProgress", 1.0f);
            m.SetFloat("_ColorizeProgress", 1.0f);
            m.SetFloat("_FadeProgress", 0.0f);
            m.SetFloat("_ScrollSpeed", -4.0f);
            m.SetFloat("_HScrollSpeed", 0.35f);
            m.SetColor("_Color", _Invalid);
        }
        if (this.gun.CurrentStrengthTier == 0)
        {
            this._placementPhantom.gameObject.SetActive(false);
            return;
        }

        DigitizedObject d = this.digitizedObjects[this._currentSlot];
        if (d == null || d.data == null || d.data.prefab == null)
        {
            this._placementPhantom.gameObject.SetActive(false);
            return;
        }

        tk2dSprite prefabSprite = d.data.prefab.GetComponentInChildren<tk2dSprite>();
        this._placementPhantom.gameObject.SetActive(true);
        this._placementPhantom.SetSprite(prefabSprite.collection, prefabSprite.spriteId);
        this._placementPhantom.PlaceAtPositionByAnchor(player.unadjustedAimPoint, Anchor.MiddleCenter);
        if (!this._placementMaterial)
            this._placementMaterial = this._placementPhantom.gameObject.GetComponent<Renderer>().material;
        this._placementMaterial.SetColor("_Color", this.CanPlacePhantom(player.unadjustedAimPoint, d) ? _Valid : _Invalid);
    }

    private void UpdateCurrentSlot()
    {
        DigitizedObject d = this.digitizedObjects[this._currentSlot];
        this.gun.CurrentStrengthTier = (d == null || d.type == EMPTY) ? 0 : 1;
        this._displayNameDirty = true;
    }

    public override void OnReloadPressed(PlayerController player, Gun gun, bool manualReload)
    {
        if (!manualReload)
            return;

        this._currentSlot = (this._currentSlot + 1) % _MAX_SLOTS;
        UpdateCurrentSlot();
        player.gameObject.Play("replicant_select_sound");
    }

    private void PlaceDigitizedObject(Vector2 placePoint)
    {
        RoomHandler room = placePoint.GetAbsoluteRoom();
        if (room == null)
            return;
        if (digitizedObjects.Count != _MAX_SLOTS)
        {
            Lazy.RuntimeWarn($"Digitized Object list is length {digitizedObjects.Count}, not what it should be; this should never happen");
            return;
        }
        if (digitizedObjects[this._currentSlot] is not DigitizedObject d || d.type == EMPTY)
            return;

        switch (d.type)
        {
            case PICKUP:
                GameObject pickup = LootEngine.SpawnItem(PickupObjectDatabase.GetById(d.pickupID).gameObject, placePoint, Vector2.zero, 0f, false, false, true).gameObject;
                tk2dSprite pickupSprite = pickup.GetComponentInChildren<tk2dSprite>();
                pickupSprite.PlaceAtPositionByAnchor(placePoint, Anchor.MiddleCenter);

                CwaffShaders.Materialize(pickupSprite);
                break;
            case TABLE:
                GameObject table = UnityEngine.Object.Instantiate(d.data.prefab, placePoint, Quaternion.identity);
                tk2dSprite tableSprite = table.GetComponentInChildren<tk2dSprite>();
                tableSprite.PlaceAtPositionByAnchor(placePoint, Anchor.MiddleCenter);
                SpeculativeRigidbody tableBody = table.GetComponentInChildren<SpeculativeRigidbody>();
                FlippableCover cover = table.GetComponent<FlippableCover>();
                room.RegisterInteractable(cover);
                cover.ConfigureOnPlacement(room);
                tableBody.Initialize();

                tableBody.CorrectForWalls();
                PhysicsEngine.Instance.RegisterOverlappingGhostCollisionExceptions(tableBody, null, false);
                CwaffShaders.Materialize(tableSprite);
                break;
            case BARREL:
                GameObject barrel = UnityEngine.Object.Instantiate(d.data.prefab, placePoint, Quaternion.identity);
                tk2dSprite barrelSprite = barrel.GetComponentInChildren<tk2dSprite>();
                barrelSprite.PlaceAtPositionByAnchor(placePoint, Anchor.MiddleCenter);
                SpeculativeRigidbody barrelBody = barrel.GetComponentInChildren<SpeculativeRigidbody>();
                KickableObject kickable = barrel.GetComponentInChildren<KickableObject>();
                room.RegisterInteractable(kickable);
                kickable.ConfigureOnPlacement(room);
                barrelBody.Initialize();

                barrelBody.CorrectForWalls();
                PhysicsEngine.Instance.RegisterOverlappingGhostCollisionExceptions(barrelBody, null, false);
                CwaffShaders.Materialize(barrelSprite);
                break;
            case CHEST:
                GameObject chestObject = UnityEngine.Object.Instantiate(d.data.prefab, placePoint, Quaternion.identity);
                Chest chest = chestObject.GetComponent<Chest>();
                tk2dBaseSprite chestSprite = chest.sprite;
                chestObject.transform.position -= chestSprite.GetRelativePositionFromAnchor(Anchor.MiddleCenter).ToVector3ZUp();
                chestSprite.UpdateZDepth();
                chest.Initialize();
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
                        if (id >= 0)
                            chest.contents.Add(PickupObjectDatabase.GetById(id));
                }
                room.RegisterInteractable(chest);
                chest.RegisterChestOnMinimap(room);
                if (d.glitched)
                    chest.BecomeGlitchChest();
                if (d.rainbow)
                    chest.BecomeRainbowChest();

                chest.specRigidbody.CorrectForWalls();
                PhysicsEngine.Instance.RegisterOverlappingGhostCollisionExceptions(chest.specRigidbody);
                CwaffShaders.Materialize(chestSprite);
                break;
            case SPECIAL:
                GameObject trap = UnityEngine.Object.Instantiate(d.data.prefab, placePoint, Quaternion.identity);
                tk2dSprite trapSprite = trap.GetComponentInChildren<tk2dSprite>();
                trapSprite.PlaceAtPositionByAnchor(placePoint, Anchor.MiddleCenter);
                if (trap.GetComponentInChildren<ForgeHammerController>() is ForgeHammerController forgeHammer)
                    forgeHammer.DeactivateOnEnemiesCleared = false;
                if (trap.GetComponentInChildren<IPlaceConfigurable>() is IPlaceConfigurable configurable)
                    configurable.ConfigureOnPlacement(room);
                if (trap.GetComponentInChildren<IPlayerInteractable>() is IPlayerInteractable interactable)
                    room.RegisterInteractable(interactable);
                if (trap.GetComponentInChildren<PathMover>() is PathMover pathMover)
                    pathMover.CreateDummyPath();
                if (trap.GetComponentInChildren<SpeculativeRigidbody>() is SpeculativeRigidbody trapBody)
                    trapBody.Reinitialize();
                if (trap.GetComponent<BraveBehaviour>() is BraveBehaviour bb)
                    bb.RegenerateCache();

                CwaffShaders.Materialize(trapSprite);
                break;
            default:
                break;
        }

        SpawnBitBurst(placePoint, 20);
        digitizedObjects[this._currentSlot] = null;
        UpdateCurrentSlot();
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        if (projectile.gameObject.GetComponent<FemtobyteProjectile>() is not FemtobyteProjectile fp)
            return;
        fp.Setup(this);
    }

    public static void SpawnBitBurst(Vector2 pos, int howMany)
    {
        FancyVFX.SpawnBurst(prefab: _ImpactBits, numToSpawn: howMany, basePosition: pos,
            positionVariance: 1f, baseVelocity: 10f * Vector2.up, velocityVariance: 5f, velType: FancyVFX.Vel.Radial,
            lifetime: 0.5f, fadeOutTime: 0.5f, randomFrame: true);
    }

    private class FemtobyteAmmoDisplay : CustomAmmoDisplay
    {
        private static StringBuilder _SB = new StringBuilder("", 1000);

        private Femtobyte _femto;
        private PlayerController _owner;
        private string _cachedDisplayName = null;

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

            if (this._femto._displayNameDirty || this._cachedDisplayName.IsNullOrWhiteSpace())
            {
                _SB.Length = 0;
                _SB.Append(this._femto.GetTitleForCurrentSlot());
                _SB.Append("\n");
                for (int i = 0; i < _MAX_SLOTS; ++i)
                {
                    DigitizedObject d = this._femto.digitizedObjects[i];
                    bool used = d != null && d.type != EMPTY;
                    if (used)
                        _SB.AppendFormat("[sprite \"{0}\"]", i == this._femto._currentSlot ? Femtobyte._FullActiveUI : Femtobyte._FullUI);
                    else
                        _SB.AppendFormat("[sprite \"{0}\"]", i == this._femto._currentSlot ? Femtobyte._EmptyActiveUI : Femtobyte._EmptyUI);
                }
                this._cachedDisplayName = _SB.ToString();
            }
            uic.GunAmmoCountLabel.Text = this._cachedDisplayName;
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
        this._projectile.specRigidbody.AddCollisionLayerOverride(CollisionMask.LayerToMask(CollisionLayer.Pickup));
        this._projectile.specRigidbody.AddCollisionLayerOverride(CollisionMask.LayerToMask(CollisionLayer.LowObstacle));
        this._projectile.OnHitEnemy += this.OnHitEnemy;
        this._projectile.OnWillKillEnemy += this.OnWillKillEnemy;

        TrailController tc = base.gameObject.GetComponentInChildren<TrailController>();
        tk2dBaseSprite trailSprite = tc.gameObject.GetComponent<tk2dBaseSprite>();
            trailSprite.usesOverrideMaterial = true;
            Material m = trailSprite.renderer.material;
            m.shader = CwaffShaders.DigitizeShader;
            m.SetTexture("_BinaryTex", CwaffShaders.DigitizeTexture);
            m.SetFloat("_BinarizeProgress", 1.0f);
            m.SetFloat("_ColorizeProgress", 1.0f);
            m.SetFloat("_FadeProgress", 0.0f);
            m.SetFloat("_ScrollSpeed", 3.5f);
            m.SetFloat("_HScrollSpeed", 0.25f);
            m.SetFloat("_Emission", 10f);
    }

    private void OnHitEnemy(Projectile p, SpeculativeRigidbody enemy, bool killed)
    {
        Femtobyte.SpawnBitBurst(enemy.UnitCenter, Mathf.Min((int)p.baseData.damage, 30));
    }

    private void OnWillKillEnemy(Projectile proj, SpeculativeRigidbody enemy)
    {
        if (enemy.GetComponent<HealthHaver>() is HealthHaver hh && !hh.IsBoss && !hh.IsSubboss)
        {
            Femtobyte.SpawnBitBurst(enemy.UnitCenter, 10);
            this._femtobyte.TryToDigitize(enemy.gameObject);
        }
    }

    private void OnPreRigidbodyCollision(SpeculativeRigidbody body, PixelCollider myCollider, SpeculativeRigidbody otherBody, PixelCollider otherCollider)
    {
        if (!this._owner || !this._projectile || !this._femtobyte || !otherBody)
            return;
        if (otherBody.GetComponent<AIActor>())
            return; // handled in OnWillKillEnemy
        if (otherBody.GetComponent<HealthHaver>() is HealthHaver hh && (hh.IsBoss || hh.IsSubboss))
            return; // handled in OnWillKillEnemy
        Vector2 otherPos = otherBody.UnitCenter;
        if (!this._femtobyte.TryToDigitize(otherBody.gameObject))
            return;
        Femtobyte.SpawnBitBurst(otherPos, 10);
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
        if (nearestIxable is PickupObject pp)
            ETGModConsole.Log($"near pickup {pp.EncounterNameOrDisplayName}");
        if (nearestIxable is not PickupObject pickup || pickup.IsBeingEyedByRat || !pickup.isActiveAndEnabled)
            return;
        Vector2 otherPos = pickup.GetComponent<tk2dBaseSprite>().WorldCenter;
        if (!this._femtobyte.TryToDigitize(pickup.gameObject))
            return;
        Femtobyte.SpawnBitBurst(otherPos, 5);
        this._projectile.DieInAir(false, false, false, false);
    }
}
