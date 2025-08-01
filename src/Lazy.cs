namespace CwaffingTheGungy;

using static GlobalDungeonData.ValidTilesets;

/// <summary>All-purpose helper methods for being a lazy dumdum</summary>
public static class Lazy
{
    internal static tk2dSpriteCollectionData _GunSpriteCollection = null;

    //NOTE: System.Diagnostics.Conditional can be used to strip out methods from release builds
    // https://stackoverflow.com/questions/49251621/will-c-sharp-compiler-strip-out-empty-methods
    /// <summary>Log with the console only in debug mode</summary>
    [System.Diagnostics.Conditional("DEBUG")]
    public static void DebugLog(object text)
    {
        ETGModConsole.Log(text);
    }

    /// <summary>Warn with the console only in debug mode</summary>
    [System.Diagnostics.Conditional("DEBUG")]
    public static void DebugWarn(string text)
    {
        ETGModConsole.Log($"<color=#ffffaaff>{text}</color>");
    }

    /// <summary>Warn with the console</summary>
    public static void RuntimeWarn(string text)
    {
        ETGModConsole.Log($"<color=#ffffaaff>{text}; tell Captain Pretzel</color>");
    }

    private static readonly Dictionary<Type, PickupObject> _CustomPickups = new();
    private static readonly Dictionary<Type, int> _CustomPickupIds = new();
    private static ProjectileModule _BaseModule = null;
    private static GenericLootTable _GunLoot = null;
    private static GenericLootTable _ItemLoot = null;
    /// <summary>Perform basic initialization for a new passive, active, or gun item definition.</summary>
    public static TItemClass SetupItem<TItemClass, TItemSpecific>(string itemName, string shortDescription, string longDescription, string lore,
      bool hideFromAmmonomicon = false, float weight = 1f)
        where TItemClass : PickupObject   // must be PickupObject for passive items, PlayerItem for active items, or Gun for guns
        where TItemSpecific : TItemClass  // must be a subclass of TItemClass
    {
        _GunLoot ??= GameManager.Instance.RewardManager.GunsLootTable;
        _ItemLoot ??= GameManager.Instance.RewardManager.ItemsLootTable;

        string baseItemName = itemName.InternalName();  //get saner gun name for commands
        string internalName = C.MOD_PREFIX+":"+baseItemName;
        string ammonomiconSprite;

        TItemClass item;
        bool isGun = typeof(TItemClass) == typeof(Gun);

        if (isGun)
        {
            _BaseModule ??= Items._38Special.AsGun().DefaultModule;

            GameObject prefabGun   = ItemHelper.Get(Items.PeaShooter).gameObject;
            prefabGun.SetActive(false); // prevent Awake() from being called on the new prefab and setting up audio events
            GameObject go          = UnityEngine.Object.Instantiate(prefabGun);
            prefabGun.SetActive(true);
            go.name                = baseItemName;
            ammonomiconSprite      = $"{baseItemName}_ammonomicon";

            Gun gun                = go.GetComponent<Gun>();
            gun.barrelOffset.transform.localScale = Vector3.one; //NOTE: Pea Shooter has a scale of 0.9, 0.9, 0.9, which messes up trails
            gun.gunName            = itemName;
            gun.gunSwitchGroup     = baseItemName;
            gun.modifiedVolley     = null;
            gun.singleModule       = null;
            gun.RawSourceVolley    = ScriptableObject.CreateInstance<ProjectileVolleyData>();
            gun.Volley.projectiles = new List<ProjectileModule>(){ProjectileModule.CreateClone(_BaseModule, inheritGuid: false)};
            gun.QuickUpdateGunAnimations(); // includes setting the default sprite

            item = gun as TItemClass;
        }
        else
        {
            GameObject obj    = new GameObject(itemName).RegisterPrefab();
            ammonomiconSprite = $"{baseItemName}_icon";

            tk2dSpriteCollectionData coll = ETGMod.Databases.Items.ItemCollection;
            tk2dSprite sprite = obj.AddComponent<tk2dSprite>();
            sprite.SetSprite(coll, coll.GetSpriteIdByName(ammonomiconSprite));
            sprite.SortingOrder = 0;
            sprite.IsPerpendicular = true;

            item = obj.AddComponent<TItemSpecific>();
        }

        ETGMod.Databases.Items.SetupItem(item, itemName);
        if (!hideFromAmmonomicon || C.DEBUG_BUILD) // only allow spawning hidden items through console in debug mode
            Gungeon.Game.Items.Add(internalName, item);

        item.itemName = itemName;
        item.encounterTrackable.journalData.AmmonomiconSprite = ammonomiconSprite;
        item.encounterTrackable.EncounterGuid = $"{C.MOD_PREFIX}-{baseItemName}"; //create a unique guid for the item
        item.SetShortDescription(shortDescription);
        item.SetLongDescription($"{longDescription}\n\n{lore}");
        ETGMod.Databases.Items.Add(item);
        GenericLootTable lootTable = (isGun ? _GunLoot : _ItemLoot);
        if (item.quality == ItemQuality.SPECIAL)
            lootTable.defaultItemDrops.elements.RemoveAt(lootTable.defaultItemDrops.elements.Count - 1);
        else if (weight != 1f)  // adjust loot pool weight if it's not the default
            lootTable.defaultItemDrops.elements.Last().weight = weight;

        if (hideFromAmmonomicon)
            item.encounterTrackable.journalData.SuppressInAmmonomicon = true;

        if (!isGun)
        {
            _CustomPickups[typeof(TItemSpecific)] = item; // register item in pickup by type database
            _CustomPickupIds[typeof(TItemSpecific)] = item.PickupObjectId; // register item in pickup id by type database
        }
        #if DEBUG
            if (!hideFromAmmonomicon)
            {
                if (item is Gun)
                    ETGModConsole.Log($"Lazy Initialized Gun: {baseItemName} ({item.DisplayName})");
                else if (item is PlayerItem)
                    ETGModConsole.Log($"Lazy Initialized Active: {baseItemName} ({item.DisplayName})");
                else
                    ETGModConsole.Log($"Lazy Initialized Passive: {baseItemName} ({item.DisplayName})");
            }
        #endif
        return item;
    }

    /// <summary>Perform basic initialization for a new fake passive items for update / synergy / save serialization purposes.</summary>
    public static TItemSpecific SetupFakeItem<TItemSpecific>() where TItemSpecific : FakeItem
    {
        string itemName = typeof(TItemSpecific).Name;
        string baseItemName = itemName.InternalName();  //get saner gun name for commands
        string internalName = C.MOD_PREFIX+":"+baseItemName;
        GameObject fakeItemObject = new GameObject(itemName).RegisterPrefab();

        tk2dSprite sprite = fakeItemObject.AddComponent<tk2dSprite>();
        sprite.collection = ETGMod.Databases.Items.ItemCollection;
        sprite.spriteId   = 0;

        TItemSpecific item = fakeItemObject.AddComponent<TItemSpecific>();
        ETGMod.Databases.Items.SetupItem(item, itemName);
        if (C.DEBUG_BUILD) // only allow spawning fake items through console in debug mode
            Gungeon.Game.Items.Add(internalName, item);

        item.itemName = itemName;
        item.encounterTrackable.EncounterGuid = $"{C.MOD_PREFIX}-{baseItemName}"; //create a unique guid for the item

        // ETGMod.Databases.Items.Add(item); //WARNING: can't use because it adds the item to default loot pools -> can spawn from synergy chests
        item.PickupObjectId = PickupObjectDatabase.Instance.Objects.Count;
        PickupObjectDatabase.Instance.Objects.Add(item);
        EncounterDatabase.Instance.Entries.Add(new(item.encounterTrackable)
        {
            myGuid = item.encounterTrackable.EncounterGuid,
            path = "Assets/Resources/ITEMDB:" + item.name + ".prefab",
        });

        item.encounterTrackable.journalData.SuppressInAmmonomicon = true; //don't show up in ammonomicon
        item.encounterTrackable.m_doNotificationOnEncounter = false; // don't display a notification when picked up
        item.quality = ItemQuality.SPECIAL;                   // don't show up as rewards
        item.ShouldBeExcludedFromShops               = true;  // don't show up in shops
        item.encounterTrackable.SuppressInInventory  = true;  // don't show up in inventory
        item.encounterTrackable.IgnoreDifferentiator = true;  // don't care how many times we encounter it
        item.CanBeDropped                            = false; // can't be dropped
        item.PersistsOnDeath                         = true;  // still can't be dropped
        item.CanBeSold                               = false; // can't be sold
        item.IgnoredByRat                            = true;  // can't be stolen
        item.ClearIgnoredByRatFlagOnPickup           = false; // still can't be stolen

        return item;
    }

    /// <summary>
    /// Perform basic initialization for a new passive item definition.
    /// </summary>
    public static PassiveItem SetupPassive<T>(string itemName, string shortDescription, string longDescription, string lore,
        bool hideFromAmmonomicon = false, float weight = 1f)
        where T : PassiveItem
    {
        return SetupItem<PassiveItem, T>(itemName, shortDescription, longDescription, lore,
          hideFromAmmonomicon: hideFromAmmonomicon, weight: weight);
    }

    /// <summary>
    /// Perform basic initialization for a new active item definition.
    /// </summary>
    public static PlayerItem SetupActive<T>(string itemName, string shortDescription, string longDescription, string lore,
        bool hideFromAmmonomicon = false, float weight = 1f)
        where T : PlayerItem
    {
        return SetupItem<PlayerItem, T>(itemName, shortDescription, longDescription, lore,
          hideFromAmmonomicon: hideFromAmmonomicon, weight: weight);
    }

    private static readonly List<Gun> _GunsToFinalize = new();
    /// <summary>
    /// Perform basic initialization for a new gun definition.
    /// </summary>
    public static Gun SetupGun<T>(string gunName, string shortDescription, string longDescription, string lore,
        bool hideFromAmmonomicon = false, float weight = 1f)
        where T : GunBehaviour
    {
        Gun gun = SetupItem<Gun, Gun>(gunName, shortDescription, longDescription, lore,
          hideFromAmmonomicon: hideFromAmmonomicon, weight: weight);
        gun.gameObject.AddComponent<T>();
        _CustomPickups[typeof(T)] = gun; // register gun in pickup by type database
        _CustomPickupIds[typeof(T)] = gun.PickupObjectId; // register gun in pickup id by type database
        _GunSpriteCollection ??= gun.sprite.collection; // need to initialize at least once

        #region Auto-setup barrelOffset from Casing attach point
            tk2dSpriteDefinition.AttachPoint[] aps = gun.AttachPointsForClip(gun.idleAnimation);
            if (aps != null)
                for (int i = 0; i < aps.Length; ++i)
                    if (aps[i].name == "Casing")
                        gun.barrelOffset.transform.localPosition = aps[i].position;
        #endregion

        #region Auto-play idle animation
            gun.spriteAnimator.DefaultClipId = gun.spriteAnimator.GetClipIdByName(gun.idleAnimation);
            gun.spriteAnimator.playAutomatically = true;
        #endregion

        _GunsToFinalize.Add(gun);
        return gun;
    }

    public static void FinalizeGuns()
    {
        foreach (Gun gun in _GunsToFinalize) // fix displayed shoot styles in ammonomicon
            EncounterDatabase.GetEntry(gun.encounterTrackable.EncounterGuid).shootStyleInt = (int)gun.DefaultModule.shootStyle;
        _GunsToFinalize.Clear();
    }

    /// <summary>Retrieve the pickup object associated with a given type</summary>
    public static PickupObject Pickup<T>() => _CustomPickups[typeof(T)];

    /// <summary>Retrieve the pickup object associated with a given type</summary>
    public static PickupObject Pickup(Type t) => _CustomPickups[t];

    /// <summary>Retrieve the pickup object id associated with a given type</summary>
    public static int PickupId<T>() => _CustomPickupIds[typeof(T)];

    /// <summary>Retrieve the pickup object id associated with a given type</summary>
    public static int PickupId(Type t) => _CustomPickupIds[t];

    /// <summary>
    /// Post a custom item pickup notification to the bottom of the screen
    /// </summary>
    public static void CustomNotification(string header, string text, tk2dBaseSprite sprite = null, UINotificationController.NotificationColor? color = null)
    {
        sprite ??= GameUIRoot.Instance.notificationController.notificationObjectSprite;
        GameUIRoot.Instance.notificationController.DoCustomNotification(
            header,
            text,
            sprite.Collection,
            sprite.spriteId,
            color ?? UINotificationController.NotificationColor.PURPLE,
            false,
            false);
    }

    /// <summary>
    /// Create a basic list of named directional animations given a list of animation names previously setup with PackerHelper.AddSpriteToCollection
    /// </summary>
    public static List<AIAnimator.NamedDirectionalAnimation> EasyNamedDirectionalAnimations(string[] animNameList)
    {
        var theList = new List<AIAnimator.NamedDirectionalAnimation>();
        for(int i = 0; i < animNameList.Length; ++i)
        {
            string anim = animNameList[i];
            theList.Add(new AIAnimator.NamedDirectionalAnimation() {
                name = anim,
                anim = new DirectionalAnimation() {
                    Type = DirectionalAnimation.DirectionType.Single,
                    Prefix = anim,
                    AnimNames = new string[] {anim},
                    Flipped = new DirectionalAnimation.FlipType[]{DirectionalAnimation.FlipType.None}
                }
            });
        }
        return theList;
    }

    /// <summary>Moves the player towards a wall in small increments, stopping if we would hit the wall. See also MoveTowardsTargetOrWall</summary>
    public static void MovePlayerTowardsPositionUntilHittingWall(PlayerController player, Vector2 position)
    {
        int num_steps = 100;

        Vector2 playerPos   = player.transform.position;
        Vector2 targetPos   = position;
        Vector2 deltaPos    = (targetPos - playerPos)/((float)(num_steps));
        Vector2 adjustedPos = Vector2.zero;

        // magic code that slowly moves the player out of walls
        for (int i = 0; i < num_steps; ++i)
        {
            player.transform.position = (playerPos + i * deltaPos).ToVector3ZisY();
            player.specRigidbody.Reinitialize();
            if (PhysicsEngine.Instance.OverlapCast(player.specRigidbody, null, true, false, null, null, false, null, null))
            {
                player.transform.position = adjustedPos;
                break;
            }
            adjustedPos = player.transform.position;
        }
    }

    /// <summary>Get the player's idle animation associated with the provided gun angle</summary>
    public static string GetBaseIdleAnimationName(PlayerController p, float gunAngle)
    {
        string anim = string.Empty;
        bool hasgun = p.CurrentGun != null;
        bool invertThresholds = false;
        if (GameManager.Instance.CurrentLevelOverrideState == GameManager.LevelOverrideState.END_TIMES)
        {
            hasgun = false;
        }
        float num = 155f;
        float num2 = 25f;
        if (invertThresholds)
        {
            num = -155f;
            num2 = -25f;
        }
        float num3 = 120f;
        float num4 = 60f;
        float num5 = -60f;
        float num6 = -120f;
        bool flag2 = gunAngle <= num && gunAngle >= num2;
        if (invertThresholds)
            flag2 = gunAngle <= num || gunAngle >= num2;
        if (flag2)
        {
            if (gunAngle < num3 && gunAngle >= num4)
                anim = (((!hasgun) && !p.ForceHandless) ? "_backward_twohands" : ((!p.RenderBodyHand) ? "_backward" : "_backward_hand"));
            else
                anim = ((hasgun || p.ForceHandless) ? "_bw" : "_bw_twohands");
        }
        else if (gunAngle <= num5 && gunAngle >= num6)
            anim = (((!hasgun) && !p.ForceHandless) ? "_forward_twohands" : ((!p.RenderBodyHand) ? "_forward" : "_forward_hand"));
        else
            anim = (((!hasgun) && !p.ForceHandless) ? "_twohands" : ((!p.RenderBodyHand) ? "" : "_hand"));
        if (p.UseArmorlessAnim)
            anim += "_armorless";
        return "idle"+anim;
    }

    /// <summary>Get the player's dodge animation associated with the provided angle vector</summary>
    public static string GetBaseDodgeAnimationName(PlayerController p, Vector2 vector)
    {
        return ((!(Mathf.Abs(vector.x) < 0.1f)) ? (((!(vector.y > 0.1f)) ? "dodge_left" : "dodge_left_bw") + ((!p.UseArmorlessAnim) ? string.Empty : "_armorless")) : (((!(vector.y > 0.1f)) ? "dodge" : "dodge_bw") + ((!p.UseArmorlessAnim) ? string.Empty : "_armorless")));
    }

    /// <summary>Get a random angle in range [-180,180]</summary>
    public static float RandomAngle()
    {
      return UnityEngine.Random.Range(-180f,180f);
    }

    /// <summary>Get a random vector with the specified magnitude</summary>
    public static Vector2 RandomVector(float magnitude = 1f)
    {
      return magnitude * RandomAngle().ToVector();
    }

    /// <summary>Get a random Quaternion rotated on the Z axis</summary>
    public static Quaternion RandomEulerZ()
    {
      return RandomAngle().EulerZ();
    }

    /// <summary>Get a random boolean</summary>
    public static bool CoinFlip()
    {
      return UnityEngine.Random.Range(0,2) == 1;
    }

    /// <summary>Get the defautl projectile for a gun by id</summary>
    public static Projectile GunDefaultProjectile(int gunid)
    {
        return (PickupObjectDatabase.GetById(gunid) as Gun).DefaultModule.projectiles[0];
    }

    /// <summary>Get an enemy's idle animation blended with a color of choice, with optional sheen</summary>
    public static Texture2D GetTexturedEnemyIdleAnimation(AIActor enemy, Color blendColor, float blendAmount, Color? sheenColor = null, float sheenWidth = 20.0f)
    {
        // Get the best idle sprite for the enemy
        tk2dSpriteDefinition bestIdleSprite = enemy.sprite.collection.spriteDefinitions[GetIdForBestIdleAnimation(enemy)];
        // If the x coordinate of the first two UVs match, we're using a rotated sprite
        bool isRotated = (bestIdleSprite.uvs[0].x == bestIdleSprite.uvs[1].x);
        // Remove the texture from the sprite sheet
        Texture2D spriteTexture = bestIdleSprite.DesheetTexture();
        // Create a new gold-blended texture for the sprite
        Texture2D goldTexture = new Texture2D(
            isRotated ? spriteTexture.height : spriteTexture.width,
            isRotated ? spriteTexture.width : spriteTexture.height);
        // Blend the pixels piece by piece
        for (int x = 0; x < goldTexture.width; x++)
        {
            for (int y = 0; y < goldTexture.height; y++)
            {
                Color pixelColor = spriteTexture.GetPixel(isRotated ? y : x, isRotated ? x : y);
                if (pixelColor.a > 0)
                {
                    // Blend opaque pixels with blendColor
                    pixelColor = Color.Lerp(pixelColor, blendColor, blendAmount);
                    // Add a diagonal white sheen
                    if (sheenColor.HasValue)
                        pixelColor = Color.Lerp(pixelColor, sheenColor.Value, Mathf.Sin( 6.28f * ( ( (x+y) % sheenWidth) / sheenWidth )));
                }
                goldTexture.SetPixel(x, y, pixelColor);
            }
        }
        return goldTexture;
    }

    internal static Dictionary<string, float> _SoundTimers = new();
    /// <summary>Play a sound on a GameObject until the GameObject is destroyed or the provided timer expires</summary>
    public static void PlaySoundUntilDeathOrTimeout(string soundName, GameObject source, float timer)
    {
        if (_SoundTimers.ContainsKey(soundName))
            _SoundTimers[soundName] = timer; // reset the timer
        else
            GameManager.Instance.StartCoroutine(PlaySoundUntilDeathOrTimeout_CR(soundName, source, timer)); // play the sound

        static IEnumerator PlaySoundUntilDeathOrTimeout_CR(string soundName, GameObject source, float timer)
        {
            _SoundTimers[soundName] = timer;
            source.Play(soundName);
            while (source != null && _SoundTimers[soundName] > 0)
            {
                _SoundTimers[soundName] -= BraveTime.DeltaTime;
                yield return null;
            }
            source.Play($"{soundName}_stop_all"); //TODO: probably better to use AkSoundEngine.StopPlayingID()
            _SoundTimers.Remove(soundName);
        }
    }


    /// <summary>Helper class for making sure looping sounds are cleaned up properly</summary>
    private class LoopingSoundHandler : MonoBehaviour
    {
        private const float _TIMEOUT = 0.1f;

        private class LoopingSoundData
        {
            public uint id;
            public float timer;
            public bool finishNaturally;
        }

        private static List<LoopingSoundData> _LoopTimers = new(16);

        private void Update()
        {
            for (int i = _LoopTimers.Count - 1; i >= 0; --i)
            {
                LoopingSoundData lsd = _LoopTimers[i];
                if (lsd.finishNaturally || (BraveTime.ScaledTimeSinceStartup - lsd.timer) < _TIMEOUT)
                    continue;
                AkSoundEngine.StopPlayingID(lsd.id);
                _LoopTimers.RemoveAt(i);
            }
        }

        public void NewSound(uint soundId, GameObject gameObject, bool finishNaturally)
        {
            _LoopTimers.Add(new(){
                id = AkSoundEngine.PostEvent(soundId, gameObject, in_uFlags: (uint)AkCallbackType.AK_EnableGetSourcePlayPosition),
                timer = BraveTime.ScaledTimeSinceStartup,
                finishNaturally = finishNaturally,
            });
        }

        public void RefreshSoundTimer(uint playingId)
        {
            for (int i = 0; i < _LoopTimers.Count; ++i)
                if (_LoopTimers[i].id == playingId)
                {
                    _LoopTimers[i].timer = BraveTime.ScaledTimeSinceStartup;
                    return;
                }
        }
    }

    private static readonly uint[] _PlayingIds = new uint[16]; //NOTE: hopefully safe to assume no more than 16 sounds are playing on the same object
    /// <summary>Loops a sound between two loop points if condition `play` is true, stops it otherwise</summary>
    public static void LoopSoundIf(this MonoBehaviour behav, bool play, string soundName, int loopPointMs = 0, int rewindAmountMs = 0, bool finishNaturally = false)
    {
        uint soundId = AkSoundEngine.GetIDFromString(soundName);
        uint count = (uint)_PlayingIds.Length;
        if (AkSoundEngine.GetPlayingIDsFromGameObject(behav.gameObject, ref count, _PlayingIds) != AKRESULT.AK_Success)
            return; // bad game object, bail out
        for (int i = 0; i < count; i++)
        {
            uint playingId = _PlayingIds[i];
            if (AkSoundEngine.GetEventIDFromPlayingID(playingId) != soundId)
                continue;
            if (!play)
            {
                if (!finishNaturally)
                    AkSoundEngine.StopPlayingID(playingId); // sound shouldn't be playing but is, so stop it now
                return;
            }
            GameManager.Instance.GetOrAddComponent<LoopingSoundHandler>().RefreshSoundTimer(playingId);
            AkSoundEngine.PostEvent(soundName + (GameManager.Instance.IsPaused ? "_pause" : "_resume"), behav.gameObject);
            if (loopPointMs > 0 && AkSoundEngine.GetSourcePlayPosition(playingId, out int pos) == AKRESULT.AK_Success && pos >= loopPointMs)
                AkSoundEngine.SeekOnEvent(soundId, behav.gameObject, pos - rewindAmountMs); // sound should be playing and is, so just check if we need to loop
            return;
        }
        if (play) // sound should be playing but isn't, so play it now
            GameManager.Instance.GetOrAddComponent<LoopingSoundHandler>().NewSound(soundId, behav.gameObject, finishNaturally);
    }

    /// <summary>Create some smoke VFX at the specified position</summary>
    public static void DoSmokeAt(Vector3 pos)
    {
        UnityEngine.Object.Instantiate(ResourceCache.Acquire("Global VFX/VFX_Item_Spawn_Poof") as GameObject)
            .GetComponent<tk2dBaseSprite>()
            .PlaceAtPositionByAnchor(pos, Anchor.MiddleCenter);
    }

    /// <summary>Create a pickup spawn poof VFX at the specified position</summary>
    public static void DoPickupAt(Vector3 pos, bool playSound = true)
    {
        GameObject original = (GameObject)ResourceCache.Acquire("Global VFX/VFX_Item_Pickup");
          GameObject gameObject = UnityEngine.Object.Instantiate(original);
            tk2dSprite sprite = gameObject.GetComponent<tk2dSprite>();
                sprite.PlaceAtPositionByAnchor(pos, Anchor.MiddleCenter);
                sprite.HeightOffGround = 6f;
                sprite.UpdateZDepth();
        if (playSound)
            gameObject.Play("Play_OBJ_item_pickup_01");
    }

    /// <summary>Create some debris from the current frame of the given sprite</summary>
    public static DebrisObject MakeDebrisFromSprite(tk2dBaseSprite sprite, Vector3 position, Vector2? initialVelocity = null, float? angularVelocity = null)
    {
        GameObject debrisObject = new GameObject("debrisboi");
            debrisObject.transform.position = position;
        debrisObject.AddComponent<tk2dSprite>().SetSprite(sprite.collection, sprite.spriteId);
        // SpeculativeRigidbody rigidbody = fakeDebris.AddComponent<SpeculativeRigidbody>();
        DebrisObject debris = debrisObject.AddComponent<DebrisObject>();
        if (initialVelocity.HasValue)
            debris.Trigger(initialVelocity.Value/*new Vector2(4f, 4f)*/, 1f, angularVelocity ?? 0f);
        return debris;
    }

    /// <summary>Get the current room of the game's best active player</summary>
    public static RoomHandler CurrentRoom()
    {
        return GameManager.Instance.BestActivePlayer.CurrentRoom;
    }

    private static Projectile _NullProjectilePrefab = null;
    /// <summary>Return a dummy projectile for instances where we need a projectile but don't want it to do anything</summary>
    public static Projectile NoProjectile()
    {
        if (_NullProjectilePrefab == null)
        {
            _NullProjectilePrefab = Items.Ak47.CloneProjectile(GunData.New(
              damage: 0.0f, force: 0.0f, speed: 1f, range: 1.0f, invisibleProjectile: true));
            _NullProjectilePrefab.isFakeBullet        = true;
            _NullProjectilePrefab.damageTypes         = CoreDamageTypes.None;
            _NullProjectilePrefab.collidesWithEnemies = false;
            _NullProjectilePrefab.collidesWithPlayer  = false;
            _NullProjectilePrefab.gameObject.AddComponent<ProjectileExpiration>().expirationTimer = 0f;
            _NullProjectilePrefab.gameObject.AddComponent<FakeProjectileComponent>();
        }
        return _NullProjectilePrefab;
    }

    private static List<AIActor> _TempEnemies = new();
    /// <summary>Determine whether any enemy is in an line between start and end (does not account for walls)</summary>
    public static bool AnyEnemyInLineOfSight(Vector2 start, Vector2 end, bool canBeNeutral = true, bool accountForWalls = false)
    {
        start.SafeGetEnemiesInRoom(ref _TempEnemies);
        foreach (AIActor enemy in _TempEnemies)
        {
            if (!enemy.IsHostile(canBeNeutral: canBeNeutral) || !enemy.specRigidbody)
                continue;
            PixelCollider collider = enemy.specRigidbody.HitboxPixelCollider;
            if (accountForWalls && !start.HasLineOfSight(collider.UnitCenter))
                continue;
            if (BraveUtility.LineIntersectsAABB(start, end, collider.UnitBottomLeft, collider.UnitDimensions, out Vector2 intersection))
                return true;
        }
        return false;
    }

    /// <summary>Determine whether any enemy is in an line between start and end</summary>
    public static AIActor NearestEnemyInLineOfSight(out Vector2 ipoint, Vector2 start, Vector2 end, bool canBeNeutral = true, bool accountForWalls = false)
    {
        AIActor nearest = null;
        float nearestSqrDist = float.MaxValue;
        start.SafeGetEnemiesInRoom(ref _TempEnemies);
        ipoint = Vector2.zero;
        foreach (AIActor enemy in _TempEnemies)
        {
            if (!enemy.IsHostile(canBeNeutral: canBeNeutral) || !enemy.specRigidbody)
                continue;
            PixelCollider collider = enemy.specRigidbody.HitboxPixelCollider;
            if (accountForWalls && !start.HasLineOfSight(collider.UnitCenter))
                continue;
            if (!BraveUtility.LineIntersectsAABB(start, end, collider.UnitBottomLeft, collider.UnitDimensions, out Vector2 intersection))
                continue;
            float sqrDist = (enemy.CenterPosition - start).sqrMagnitude;
            if (sqrDist > nearestSqrDist)
                continue;
            ipoint = intersection;
            nearestSqrDist = sqrDist;
            nearest = enemy;
        }
        return nearest;
    }

    /// <summary>Determine whether any enemy is in an line between start and end</summary>
    public static AIActor NearestEnemyInLineOfSight(Vector2 start, Vector2 end, bool canBeNeutral = true, bool accountForWalls = false)
    {
        return NearestEnemyInLineOfSight(out _, start, end, canBeNeutral, accountForWalls);
    }

    /// <summary>Determine the nearest enemy inside a cone of vision from position start within maxDeviation degree of coneAngle</summary>
    public static IEnumerable<AIActor> AllEnemiesWithinConeOfVision(Vector2 start, float coneAngle, float maxDeviation, float maxDistance = 100f, bool ignoreWalls = false)
    {
        float maxSqrDist = maxDistance * maxDistance;
        start.SafeGetEnemiesInRoom(ref _TempEnemies);
        foreach (AIActor enemy in _TempEnemies)
        {
            if (!enemy.IsHostile(canBeNeutral: true))
                continue;
            Vector2 tentativeTarget = enemy.CenterPosition;
            if (!ignoreWalls && !start.HasLineOfSight(tentativeTarget))
                continue;
            Vector2 delta        = (tentativeTarget - start);
            float angle          = delta.ToAngle().Clamp360();
            float angleDeviation = Mathf.Abs((coneAngle - angle).Clamp180());
            if (angleDeviation > maxDeviation)
                continue;
            float sqrDist        = delta.sqrMagnitude;
            if (sqrDist > maxSqrDist)
                continue;
            yield return enemy;
        }
        yield break;
    }

    /// <summary>Determine the nearest enemy inside a cone of vision from position start within maxDeviation degree of coneAngle</summary>
    public static AIActor NearestEnemyWithinConeOfVision(Vector2 start, float coneAngle, float maxDeviation, float maxDistance = 100f, bool useNearestAngleInsteadOfDistance = true, bool ignoreWalls = false)
    {
        float bestAngle    = maxDeviation;
        float maxSqrDist   = maxDistance * maxDistance;
        float bestSqrDist  = maxSqrDist;
        AIActor bestEnemy  = null;
        start.SafeGetEnemiesInRoom(ref _TempEnemies);
        foreach (AIActor enemy in _TempEnemies)
        {
            if (!enemy.IsHostile(canBeNeutral: true))
                continue;
            Vector2 tentativeTarget = enemy.CenterPosition;
            if (!ignoreWalls && !start.HasLineOfSight(tentativeTarget))
                continue;
            Vector2 delta        = (tentativeTarget - start);
            float angle          = delta.ToAngle().Clamp360();
            float angleDeviation = Mathf.Abs((coneAngle - angle).Clamp180());
            if (angleDeviation > maxDeviation)
                continue;
            float sqrDist        = delta.sqrMagnitude;
            if (sqrDist > maxSqrDist)
                continue;
            bool bestSoFar       = useNearestAngleInsteadOfDistance
                ? (angleDeviation < bestAngle)
                : (sqrDist < bestSqrDist);
            if (!bestSoFar)
                continue;
            bestEnemy   = enemy;
            bestAngle   = angleDeviation;
            bestSqrDist = sqrDist;
        }
        return bestEnemy;
    }

    /// <summary>Determine position of the nearest enemy inside a cone of vision from position start within maxDeviation degree of coneAngle</summary>
    public static Vector2? NearestEnemyPosWithinConeOfVision(Vector2 start, float coneAngle, float maxDeviation, float maxDistance = 100f, bool useNearestAngleInsteadOfDistance = true, bool ignoreWalls = false)
    {
        AIActor enemy = NearestEnemyWithinConeOfVision(start: start, coneAngle: coneAngle, maxDeviation: maxDeviation,
            useNearestAngleInsteadOfDistance: useNearestAngleInsteadOfDistance, ignoreWalls: ignoreWalls);
        return enemy ? enemy.CenterPosition : null;
    }

    /// <summary>Determine position of the nearest enemy to position start</summary>
    public static AIActor NearestEnemy(Vector2 start, bool useNearestAngleInsteadOfDistance = false, bool ignoreWalls = false)
    {
        return NearestEnemyWithinConeOfVision(start: start, coneAngle: 0f, maxDeviation: 360f,
            useNearestAngleInsteadOfDistance: useNearestAngleInsteadOfDistance, ignoreWalls: ignoreWalls);
    }

    /// <summary>Determine position of the nearest enemy to position start</summary>
    public static Vector2? NearestEnemyPos(Vector2 start, bool useNearestAngleInsteadOfDistance = false, bool ignoreWalls = false)
    {
        return NearestEnemyPosWithinConeOfVision(start: start, coneAngle: 0f, maxDeviation: 360f,
            useNearestAngleInsteadOfDistance: useNearestAngleInsteadOfDistance, ignoreWalls: ignoreWalls);
    }

    /// <summary>Determine all enemies within a radius of a point.</summary>
    public static void GetAllNearbyEnemies(ref List<AIActor> enemies, Vector2 center, float radius = -1f, bool ignoreWalls = false)
    {
        float sqrRadius = radius * radius;
        enemies.Clear();

        center.SafeGetEnemiesInRoom(ref _TempEnemies);
        foreach (AIActor enemy in _TempEnemies)
        {
            if (!enemy.IsHostile(canBeNeutral: true))
                continue;
            Vector2 tentativeTarget = enemy.CenterPosition;
            if ((radius > 0) && ((tentativeTarget - center).sqrMagnitude > sqrRadius))
                continue;
            if (ignoreWalls || center.HasLineOfSight(tentativeTarget))
                enemies.Add(enemy);
        }
    }

    /// <summary>Returns a list of all enemies within a radius of a point.</summary>
    public static List<AIActor> GetAllNearbyEnemies(Vector2 center, float radius = 100f, bool ignoreWalls = false)
    {
        GetAllNearbyEnemies(ref _TempEnemies, center, radius, ignoreWalls);
        return _TempEnemies;
    }

    /// <summary>Spawn a chest with a single guaranteed item inside of it</summary>
    public static Chest SpawnChestWithSpecificItem(PickupObject pickup, IntVector2 position, ItemQuality? overrideChestQuality = null, bool overrideJunk = false)
    {
      Chest chestPrefab =
        GameManager.Instance.RewardManager.GetTargetChestPrefab(overrideChestQuality ?? pickup.quality)
        ?? GameManager.Instance.RewardManager.GetTargetChestPrefab(ItemQuality.B);
      Chest chest = Chest.Spawn(chestPrefab, position);
      chest.forceContentIds = new(){pickup.PickupObjectId};
      if (overrideJunk)
        chest.overrideJunkId = pickup.PickupObjectId;
      if (position.ToVector2().GetAbsoluteRoom() is RoomHandler room)
        chest.RegisterChestOnMinimap(room);
      return chest;
    }

    // https://martin.ankerl.com/2007/10/04/optimized-pow-approximation-for-java-and-c-c/
    /// <summary>Compute a fast approximation for a^b</summary>
    public static double FastPow(double a, double b) {
        int tmp = (int)(BitConverter.DoubleToInt64Bits(a) >> 32);
        int tmp2 = (int)(b * (tmp - 1072632447) + 1072632447);
        return BitConverter.Int64BitsToDouble(((long)tmp2) << 32);
    }

    // https://martin.ankerl.com/2007/10/04/optimized-pow-approximation-for-java-and-c-c/
    /// <summary>Compute a fast approximation for a^0.5</summary>
    public static double FastSqrt(double a) {
        int tmp = (int)(BitConverter.DoubleToInt64Bits(a) >> 32);
        int tmp2 = (int)(0.5 * (tmp - 1072632447) + 1072632447);
        return BitConverter.Int64BitsToDouble(((long)tmp2) << 32);
    }

    /// <summary>Get a modded item by id, returning null if it doesn't exist</summary>
    public static PickupObject GetModdedItem(string itemName)
    {
        return Gungeon.Game.Items.GetSafe(itemName);
    }

    /// <summary>Returns log_2(f)</summary>
    public static float Log2(float f) => Mathf.Log(f, 2);

    /// <summary>Check whether a line intersects a circle with a given position and radius</summary>
    /// <remarks>See https://stackoverflow.com/questions/53173712/calculating-distance-of-point-to-linear-line</remarks>
    public static bool LineIntersectsCircle(Vector2 startp, Vector2 endp, Vector2 p, float radius)
    {
      // ChatGPT version: https://chat.openai.com/c/fe69576b-462a-4f49-a54e-d7462e42c6c7
      // Calculate direction vector of the line segment
      Vector2 d = endp - startp;
      // Calculate vectors between the circle center and the start/end points of the line segment
      Vector2 f = startp - p;
      Vector2 e = endp - p;
      // Calculate the squared length of the line segment
      float lengthSq = d.sqrMagnitude;
      // Project the circle center onto the line segment
      float t = Vector2.Dot(p - startp, d) / lengthSq;
      // Clamp t to be within the range [0,1] to ensure the projected point is on the line segment
      t = Mathf.Clamp01(t);
      // Calculate the closest point on the line segment to the circle center
      Vector2 closestPoint = startp + t * d;
      // Calculate the squared distance between the circle center and the closest point on the line segment
      float distanceSq = (p - closestPoint).sqrMagnitude;
      // Check if the squared distance is less than or equal to the squared radius
      return distanceSq <= radius * radius;
    }

    /// <summary>
    /// Given a ray `r` extending from start towards direction `dir`, returns the point `p` such that the segment between `target` and `p` is orthogonal to `r`.
    /// Returns `null` if no such point exists.
    /// </summary>
    public static Vector2? PointOrthognalTo(Vector2 start, Vector2 target, Vector2 dir, float projAmount = 1000f)
    {
        // Project a point outward from start in direction dir by amount projAmount
        Vector2 end = start + (projAmount * dir);

        // Project a line orthogonal to dir through our target
        Vector2 ortho = projAmount * dir.Rotate(degrees: 90);

        // Find the orthogonal intersection point, or return null if no such point exists
        Vector2 ipoint;
        if (!BraveUtility.LineIntersectsLine(start, end, target + ortho, target - ortho, out ipoint))
            return null;
        return ipoint;
    }

    private static Dictionary<string, tk2dSpriteDefinition> _FragmentDict = new();
    /// <summary>Breaks a sprite into edgeSize x edgeSize smaller sprite fragments, and returns a definition for the (x,y)th fragment </summary>
    public static tk2dSpriteDefinition GetSpriteFragment(tk2dSpriteDefinition orig, int x, int y, int edgeSize)
    {
        string fragmentName = $"{orig.name}_{x}_{y}_{edgeSize}";
        if (_FragmentDict.TryGetValue(fragmentName, out tk2dSpriteDefinition cachedDef))
            return cachedDef;

        // If the x coordinate of the first two UVs match, we're using a rotated sprite
        bool isRotated = (orig.uvs[0].x == orig.uvs[1].x);

        float fragSize     = 1f / (float)edgeSize;
        Vector3 opos       = orig.position0;
        Vector2 newExtents = fragSize * orig.boundsDataExtents;
        Vector2 newgap     = fragSize * (orig.uvs[3] - orig.uvs[0]);

        Vector2[] newUvs;
        if (isRotated) // math gets a little more complicated when individual fragment UVs and positions need to be rotated
        {
            int rotx       = y;
            int roty       = edgeSize - x - 1;
            Vector2 newmin = orig.uvs[0] + new Vector2(rotx       * newgap.x, roty       * newgap.y);
            Vector2 newmax = orig.uvs[0] + new Vector2((rotx + 1) * newgap.x, (roty + 1) * newgap.y);
            newUvs         = new Vector2[]
                { //NOTE: texture is flipped vertically in memory AND rotated horizontally in the atlas
                  new Vector2(newmin.x, newmax.y),
                  newmin,
                  newmax,
                  new Vector2(newmax.x, newmin.y),
                };
        }
        else
        {
            Vector2 newmin = orig.uvs[0] + new Vector2(x       * newgap.x, y       * newgap.y);
            Vector2 newmax = orig.uvs[0] + new Vector2((x + 1) * newgap.x, (y + 1) * newgap.y);
            newUvs         = new Vector2[]
                { //NOTE: texture is flipped vertically in memory
                  newmin,
                  new Vector2(newmax.x, newmin.y),
                  new Vector2(newmin.x, newmax.y),
                  newmax,
                };
        }

        tk2dSpriteDefinition def = new tk2dSpriteDefinition
        {
            name                       = fragmentName,
            texelSize                  = orig.texelSize,
            flipped                    = orig.flipped,
            physicsEngine              = orig.physicsEngine,
            colliderType               = orig.colliderType,
            collisionLayer             = orig.collisionLayer,
            material                   = orig.material,
            materialInst               = orig.materialInst,
            position0                  = opos + new Vector3(x     * newExtents.x, y       * newExtents.y, 0f),
            position1                  = opos + new Vector3((x+1) * newExtents.x, y       * newExtents.y, 0f),
            position2                  = opos + new Vector3(x     * newExtents.x, (y + 1) * newExtents.y, 0f),
            position3                  = opos + new Vector3((x+1) * newExtents.x, (y + 1) * newExtents.y, 0f),
            boundsDataExtents          = orig.boundsDataExtents/*.TransposeIf(isRotated)*/,
            boundsDataCenter           = orig.boundsDataCenter/*.TransposeIf(isRotated)*/,
            untrimmedBoundsDataExtents = orig.untrimmedBoundsDataExtents/*.TransposeIf(isRotated)*/,
            untrimmedBoundsDataCenter  = orig.untrimmedBoundsDataCenter/*.TransposeIf(isRotated)*/,
            uvs                        = newUvs,
        };
        _FragmentDict[fragmentName] = def;
        return def;
    }

    /// <summary>Returns a random color</summary>
    public static Color RandomColor()
    {
        return new Color(
            UnityEngine.Random.value,
            UnityEngine.Random.value,
            UnityEngine.Random.value
            );
    }

    /// <summary>Create a new GameObject with a specific sprite</summary>
    public static T SpriteObject<T>(tk2dSpriteCollectionData spriteColl, int spriteId) where T : tk2dBaseSprite
    {
        GameObject g = new();
        T sprite = g.AddComponent<T>();
        sprite.SetSprite(spriteColl, spriteId);
        return sprite;
    }

    /// <summary>Create a new GameObject with a specific sprite</summary>
    public static tk2dSprite SpriteObject(tk2dSpriteCollectionData spriteColl, int spriteId)
    {
        return SpriteObject<tk2dSprite>(spriteColl, spriteId);
    }

    /// <summary>Create a hovering gun from the player's current active gun</summary>
    public static void CreateHoveringGun(PlayerController player)
    {
        GameObject hg = UnityEngine.Object.Instantiate(ResourceCache.Acquire("Global Prefabs/HoveringGun") as GameObject, player.CenterPosition.ToVector3ZisY(), Quaternion.identity);
        hg.transform.parent = player.transform;
        HoveringGunController hgc = hg.GetComponent<HoveringGunController>();
        // hgc.ShootAudioEvent              = ShootAudioEvent;
        // hgc.OnEveryShotAudioEvent        = OnEveryShotAudioEvent;
        // hgc.FinishedShootingAudioEvent   = FinishedShootingAudioEvent;
        hgc.ConsumesTargetGunAmmo        = false;
        hgc.ChanceToConsumeTargetGunAmmo = 0f;
        hgc.Position                     = HoveringGunController.HoverPosition.CIRCULATE;
        hgc.Aim                          = HoveringGunController.AimType.PLAYER_AIM;
        hgc.Trigger                      = HoveringGunController.FireType.ON_FIRED_GUN;
        hgc.CooldownTime                 = 0.01f;
        hgc.ShootDuration                = 0f;
        hgc.OnlyOnEmptyReload            = false;
        hgc.Initialize(player.CurrentGun, player);
    }

    /// <summary>Combine multiple lists into one</summary>
    public static List<T> Combine<T>(params List<T>[] lists)
    {
        List<T> result = new();
        foreach (List<T> list in lists)
        {
            if (list == null)
                continue;
            foreach (T t in list) //TODO: could maybe use addrange
                result.Add(t);
        }
        return result;
    }

    /// <summary>Create a basic decoy object (adapted from MindControlEffect.cs)</summary>
    public static GameObject CreateDecoy(Vector3 position)
    {
        GameObject decoyObject = new GameObject("fake target");
        NonActor m_fakeActor = decoyObject.AddComponent<NonActor>();
        m_fakeActor.HasShadow = false;

        SpeculativeRigidbody body = decoyObject.AddComponent<SpeculativeRigidbody>();
        body.CollideWithTileMap = false;
        body.CollideWithOthers  = false;
        body.CanBeCarried       = false;
        body.CanBePushed        = false;
        body.CanCarry           = false;
        body.PixelColliders = new List<PixelCollider>(){new PixelCollider(){
            ColliderGenerationMode = PixelCollider.PixelColliderGeneration.Manual,
            CollisionLayer         = CollisionLayer.TileBlocker,
            ManualWidth            = 4,
            ManualHeight           = 4,
        }};

        decoyObject.transform.position = position;
        return decoyObject;
    }

    /// <summary>Check if any player has a passive item</summary>
    public static bool AnyoneHas<T>() where T : PassiveItem
    {
      if (GameManager.Instance.PrimaryPlayer is not PlayerController p1)
        return false;
      for (int i = 0; i < p1.passiveItems.Count; ++i)
        if (p1.passiveItems[i] is T)
          return true;
      if (GameManager.Instance.CurrentGameType != GameManager.GameType.COOP_2_PLAYER)
        return false;
      if (GameManager.Instance.SecondaryPlayer is not PlayerController p2)
        return false;
      for (int i = 0; i < p2.passiveItems.Count; ++i)
        if (p2.passiveItems[i] is T)
          return true;
      return false;
    }

    /// <summary>Check if any player has a passive item by id</summary>
    public static bool AnyoneHas(int id)
    {
      if (GameManager.Instance.PrimaryPlayer is not PlayerController p1)
        return false;
      for (int i = 0; i < p1.passiveItems.Count; ++i)
        if (p1.passiveItems[i].PickupObjectId == id)
          return true;
      if (GameManager.Instance.CurrentGameType != GameManager.GameType.COOP_2_PLAYER)
        return false;
      if (GameManager.Instance.SecondaryPlayer is not PlayerController p2)
        return false;
      for (int i = 0; i < p2.passiveItems.Count; ++i)
        if (p2.passiveItems[i].PickupObjectId == id)
          return true;
      return false;
    }

    /// <summary>Check if any player has an active item</summary>
    public static bool AnyoneHasActive<T>() where T : PlayerItem
    {
      if (GameManager.Instance.PrimaryPlayer is not PlayerController p1)
        return false;
      for (int i = 0; i < p1.activeItems.Count; ++i)
        if (p1.activeItems[i] is T)
          return true;
      if (GameManager.Instance.CurrentGameType != GameManager.GameType.COOP_2_PLAYER)
        return false;
      if (GameManager.Instance.SecondaryPlayer is not PlayerController p2)
        return false;
      for (int i = 0; i < p2.activeItems.Count; ++i)
        if (p2.activeItems[i] is T)
          return true;
      return false;
    }

    /// <summary>Check if any player has an active item by id</summary>
    public static bool AnyoneHasActive(int id)
    {
      if (GameManager.Instance.PrimaryPlayer is not PlayerController p1)
        return false;
      for (int i = 0; i < p1.activeItems.Count; ++i)
        if (p1.activeItems[i].PickupObjectId == id)
          return true;
      if (GameManager.Instance.CurrentGameType != GameManager.GameType.COOP_2_PLAYER)
        return false;
      if (GameManager.Instance.SecondaryPlayer is not PlayerController p2)
        return false;
      for (int i = 0; i < p2.activeItems.Count; ++i)
        if (p2.activeItems[i].PickupObjectId == id)
          return true;
      return false;
    }

    /// <summary>Check if any player has a gun</summary>
    public static bool AnyoneHasGun<T>() where T : Gun
    {
      if (GameManager.Instance.PrimaryPlayer is not PlayerController p1)
        return false;
      for (int i = 0; i < p1.inventory.AllGuns.Count; ++i)
        if (p1.inventory.AllGuns[i] is T)
          return true;
      if (GameManager.Instance.CurrentGameType != GameManager.GameType.COOP_2_PLAYER)
        return false;
      if (GameManager.Instance.SecondaryPlayer is not PlayerController p2)
        return false;
      for (int i = 0; i < p2.inventory.AllGuns.Count; ++i)
        if (p2.inventory.AllGuns[i] is T)
          return true;
      return false;
    }

    /// <summary>Check if any player has a gun by id</summary>
    public static bool AnyoneHasGun(int id)
    {
      if (GameManager.Instance.PrimaryPlayer is not PlayerController p1)
        return false;
      for (int i = 0; i < p1.inventory.AllGuns.Count; ++i)
        if (p1.inventory.AllGuns[i].PickupObjectId == id)
          return true;
      if (GameManager.Instance.CurrentGameType != GameManager.GameType.COOP_2_PLAYER)
        return false;
      if (GameManager.Instance.SecondaryPlayer is not PlayerController p2)
        return false;
      for (int i = 0; i < p2.inventory.AllGuns.Count; ++i)
        if (p2.inventory.AllGuns[i].PickupObjectId == id)
          return true;
      return false;
    }

    /// <summary>Check if any player has a synergy active</summary>
    public static bool AnyoneHasSynergy(Synergy synergy)
    {
      if (GameManager.Instance.PrimaryPlayer is not PlayerController p1)
        return false;
      if (p1.HasSynergy(synergy))
        return true;
      if (GameManager.Instance.CurrentGameType != GameManager.GameType.COOP_2_PLAYER)
        return false;
      if (GameManager.Instance.SecondaryPlayer is not PlayerController p2)
        return false;
      if (p2.HasSynergy(synergy))
        return true;
      return false;
    }

    /// <summary>Gets the best idle animation for the given enemy</summary>
    public static int GetIdForBestIdleAnimation(AIActor enemy)
    {
        int bestMatchStrength = 0;
        int bestSpriteId = -1;

        if (enemy.GetComponent<AIAnimator>() is AIAnimator animator
            && enemy.GetComponent<tk2dSpriteAnimator>() is tk2dSpriteAnimator spriteAnimator
            && animator.IdleAnimation.Type != DirectionalAnimation.DirectionType.None)
        {
            string idleName = animator.IdleAnimation.GetInfo(270f).name;
            // Lazy.DebugLog($"  found idle clip name {idleName}");
            tk2dSpriteAnimationClip idleClip = spriteAnimator.GetClipByName(idleName);
            if (idleClip != null && idleClip.frames != null && idleClip.frames.Length > 0)
                return idleClip.frames[0].spriteId;
        }

        tk2dSpriteDefinition[] defs = enemy.sprite.collection.spriteDefinitions;
        for (int i = 0; i < defs.Length; ++i)
        {
            tk2dSpriteDefinition sd = defs[i];
            int matchStrength = 0;
            if (sd.name.Contains("001"))
            {
                if (sd.name.Contains("idle_f"))
                    matchStrength = 5;
                else if (sd.name.Contains("idle_right") || sd.name.Contains("idle_r"))
                    matchStrength = 4;
                else if (sd.name.Contains("idle_left") || sd.name.Contains("idle_l"))
                    matchStrength = 3;
                else if (sd.name.Contains("idle") || sd.name.Contains("fire") || sd.name.Contains("run_right") || sd.name.Contains("right_run"))
                    matchStrength = 2;
                else if (sd.name.Contains("death") || sd.name.Contains("left") || sd.name.Contains("right"))
                    matchStrength = 1;
                if (matchStrength > bestMatchStrength)
                {
                  bestMatchStrength = matchStrength;
                  bestSpriteId = i;
                  if (bestMatchStrength == 5)
                    break;
                }
            }
        }

        if (bestSpriteId >= 0)
            return bestSpriteId;
        return enemy.sprite.collection.FirstValidDefinitionIndex;
    }


    /// <summary>Smoother Lerping over Deltatime, from "Lerp Smoothing is Broken": https://www.youtube.com/watch?v=LSNQuFEDOyQ</summary>
    public static float SmoothestLerp(float a, float b, float r)
    {
        return b + (a - b) * Mathf.Exp(-BraveTime.DeltaTime * r);
    }

    /// <summary>Vector2 version of above</summary>
    public static Vector2 SmoothestLerp(Vector2 a, Vector2 b, float r)
    {
        float exp = Mathf.Exp(-BraveTime.DeltaTime * r);
        return new Vector2(b.x + (a.x - b.x) * exp, b.y + (a.y - b.y) * exp);
    }

    private static GameObject _BlankVFXPrefab = null;
    /// <summary>Trigger a mini blank effect at a specific position (adapted from PlayerOrbitalFollower.cs)</summary>
    public static void DoMicroBlankAt(Vector2 pos, PlayerController user = null, float radius = 4f, float additionalTimeAtMaxRadius = 0.25f)
    {
        _BlankVFXPrefab ??= (GameObject)BraveResources.Load("Global VFX/BlankVFX_Ghost");
        GameObject gameObject = new GameObject("silencer");
        SilencerInstance silencerInstance = gameObject.AddComponent<SilencerInstance>();
        silencerInstance.TriggerSilencer(pos, 20f, radius, _BlankVFXPrefab, 0f, 3f, 3f, 3f, 30f, 3f, additionalTimeAtMaxRadius, user, false);
        gameObject.Play("Play_OBJ_silenceblank_small_01");
    }

    //TODO: this doesn't actually seem to work on the player, which is all we ever use it on..unsure why though
    /// <summary>Do a brief flash after taking damage [yoinked from HealthHaver.ApplyDamageDirectional()]</summary>
    public static void DoDamagedFlash(HealthHaver hh)
    {
        if (!(hh.flashesOnDamage && hh.spriteAnimator != null && !hh.m_isFlashing))
            return;

        if (hh.m_flashOnHitCoroutine != null)
        {
            hh.StopCoroutine(hh.m_flashOnHitCoroutine);
            hh.m_flashOnHitCoroutine = null;
        }
        if (hh.materialsToFlash == null)
        {
            hh.materialsToFlash = new List<Material>();
            hh.outlineMaterialsToFlash = new List<Material>();
            hh.sourceColors = new List<Color>();
        }
        if (hh.gameActor)
            for (int k = 0; k < hh.materialsToFlash.Count; k++)
                hh.materialsToFlash[k].SetColor(CwaffVFX._OverrideColorId, hh.gameActor.CurrentOverrideColor);
        if (hh.outlineMaterialsToFlash != null)
            for (int l = 0; l < hh.outlineMaterialsToFlash.Count; l++)
            {
                if (l >= hh.sourceColors.Count)
                    break;
                hh.outlineMaterialsToFlash[l].SetColor(CwaffVFX._OverrideColorId, hh.sourceColors[l]);
            }
        hh.m_flashOnHitCoroutine = hh.StartCoroutine(hh.FlashOnHit(DamageCategory.Normal, null));
    }

    /// <summary>Make a list of size n filled with default values for a type</summary>
    public static List<T> DefaultList<T>(int size)
    {
        return Enumerable.Repeat<T>(default, size).ToList();
    }

    /// <summary>Print a stat out</summary>
    public static void PrintStat(StatModifier stat)
    {
        ETGModConsole.Log($"  have {(stat.modifyType == StatModifier.ModifyMethod.MULTIPLICATIVE ? "mul" : "add")} stat {stat.statToBoost} by {stat.amount}");
    }

    /// <summary>Print a player's ownerless stats</summary>
    public static void PrintOwnerlessStats(PlayerController player)
    {
        foreach (StatModifier stat in player.ownerlessStatModifiers)
            PrintStat(stat);
    }

    /// <summary>Set up custom ammo types from default resource paths and returns it directly</summary>
    public static string SetupCustomAmmoClip(string clipname)
    {
        return AtlasHelper.GetOrAddCustomAmmoType($"{clipname}_clip", ResMap.Get($"{clipname}_clipfull")[0], ResMap.Get($"{clipname}_clipempty")[0]);
    }

    private const int _RANDOM_STRING_LENGTH = 10;
    private const int _MAX_CACHED_RANDOM_STRINGS = 100;
    private static List<string> _RandomStrings = new(_MAX_CACHED_RANDOM_STRINGS);
    /// <summary>Generate a random 10-character string from a GUID</summary>
    public static string GenRandomCorruptedString()
    {
        if (_RandomStrings.Count >= _MAX_CACHED_RANDOM_STRINGS)
            return _RandomStrings[UnityEngine.Random.Range(0, _MAX_CACHED_RANDOM_STRINGS)];
        string s = $"[color #dd6666]{Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Substring(0, _RANDOM_STRING_LENGTH)}[/color]";
        _RandomStrings.Add(s);
        return s;
    }

    /// <summary>Do an elastic collision between two speculative rigid bodies</summary>
    public static void DoElasticBounce(CollisionData collision)
    {
        Vector2 posDiff = collision.MyRigidbody.UnitCenter - collision.OtherRigidbody.UnitCenter;
        Vector2 v1 = collision.MyRigidbody.Velocity;
        Vector2 v2 = collision.OtherRigidbody.Velocity;
        float invDistNorm = 1f / Mathf.Max(0.1f, posDiff.sqrMagnitude);
        Vector2 newv1 = v1 - (Vector2.Dot(v1 - v2, posDiff) * invDistNorm) * posDiff;
        Vector2 newv2 = v2 - (Vector2.Dot(v2 - v1, -posDiff) * invDistNorm) * (-posDiff);

        float newSpeed = Mathf.Sqrt(Mathf.Max(v1.sqrMagnitude, v2.sqrMagnitude/*, newv1.sqrMagnitude, newv2.sqrMagnitude*/));
        PhysicsEngine.PostSliceVelocity = newSpeed * newv1.normalized;
    }

    /// <summary>Get the color from a palette corresponding to a red float scalar (calculations from decompiled shader)</summary>
    public static Color GetPaletteColor(Texture2D palette, float red)
    {
        const double INV_TEXEL_SIZE = 0.5;
        double x = red * 15.9375; // 255 / 16
        double texX = Math.Floor(x) * 0.0625 + 0.4;
        double texY = Math.Sign(x) * Math.Abs(x - Math.Truncate(x)) + 0.4;
        int pixX = (int)Math.Round(INV_TEXEL_SIZE * texX * palette.width);
        int pixY = (int)Math.Round(INV_TEXEL_SIZE * texY * palette.height);
        return palette.GetPixel(pixX, pixY);
    }

    private static readonly Dictionary<Texture, Texture2D> _ReadableTexes = new();
    /// <summary>Get the pixel colors for the best idle animation sprite for an enemy by GUID</summary>
    public static Color[] GetPixelColorsForEnemy(string guid)
    {
        AIActor prefab = EnemyDatabase.GetOrLoadByGuid(guid);
        // Lazy.DebugLog($"looking up pigment for {prefab.ActorName}");
        tk2dSpriteDefinition def = prefab.GetComponent<tk2dSprite>().collection.spriteDefinitions[Lazy.GetIdForBestIdleAnimation(prefab)];
        // Lazy.DebugLog($"  got def {def.name}");

        Texture mainTex = def.material.mainTexture;
        if (!_ReadableTexes.TryGetValue(mainTex, out Texture2D tex))
            tex = _ReadableTexes[mainTex] = (mainTex as Texture2D).GetRW();
        return tex.GetPixels(
            x           : Mathf.RoundToInt(def.uvs[0].x * tex.width),
            y           : Mathf.RoundToInt(def.uvs[0].y * tex.height),
            blockWidth  : Mathf.RoundToInt((def.uvs[3].x - def.uvs[0].x) * tex.width),
            blockHeight : Mathf.RoundToInt((def.uvs[3].y - def.uvs[0].y) * tex.height)
            );
    }

    /// <summary>Make a piece of debris decay over time</summary>
    public static IEnumerator DecayOverTime(DebrisObject debris, float time, bool shrink = false)
    {
        for (float lifeleft = time; lifeleft > 0; lifeleft -= BraveTime.DeltaTime)
        {
            if (shrink)
            {
                Vector2 oldCenter = debris.sprite.WorldCenter;
                debris.sprite.scale = (lifeleft / time) * Vector3.one;
                debris.sprite.PlaceAtScaledPositionByAnchor(oldCenter, Anchor.MiddleCenter);
            }
            else
                debris.sprite.renderer.SetAlpha(lifeleft / time);
            yield return null;
        }
        UnityEngine.Object.Destroy(debris.gameObject);
    }

    /// <summary>Create a mesh sprite object at the given position.</summary>
    public static tk2dMeshSprite CreateMeshSpriteObject(tk2dSpriteCollectionData collection, int spriteId, Vector2 pos, bool pointMesh = false, Texture2D optionalPalette = null)
    {
        GameObject g = new GameObject("mesh sprite object");
        g.transform.position = pos.Quantize(0.0625f);

        tk2dMeshSprite ss = g.AddComponent<tk2dMeshSprite>();
        ss.SetSprite(collection, spriteId);
        tk2dSpriteDefinition def = collection.spriteDefinitions[spriteId];
        int w = Mathf.RoundToInt(C.PIXELS_PER_TILE * def.boundsDataExtents.x);
        int h = Mathf.RoundToInt(C.PIXELS_PER_TILE * def.boundsDataExtents.y);
        ss.ResizeMesh(w, h, usePointMesh: pointMesh);
        ss.optionalPalette = optionalPalette;
        return ss;
    }

    /// <summary>Create a mesh sprite object at the given position using the given sprite as a reference.</summary>
    public static tk2dMeshSprite CreateMeshSpriteObject(tk2dBaseSprite s, Vector2 pos, bool pointMesh = false, Texture2D optionalPalette = null)
    {
        return CreateMeshSpriteObject(s.collection, s.spriteId, pos, pointMesh: pointMesh, optionalPalette: optionalPalette);
    }

    /// <summary>Dissipate a mesh sprite object over a period of time.</summary>
    public static void Dissipate(this tk2dMeshSprite ms, float time, float amplitude = 10f, bool progressive = false)
    {
        ms.StartCoroutine(Dissipate_CR(ms: ms, time: time, amplitude: amplitude, progressive: progressive));

        static IEnumerator Dissipate_CR(tk2dMeshSprite ms, float time, float amplitude = 10f, bool progressive = false)
        {
            Material mat = ms.renderer.material;
            mat.shader = CwaffShaders.ShatterShader;
            mat.SetFloat("_Progressive", progressive ? 1f : 0f);
            mat.SetFloat("_Amplitude", amplitude);
            mat.SetFloat("_RandomSeed", UnityEngine.Random.value);
            if (ms.optionalPalette != null)
            {
                mat.SetFloat("_UsePalette", 1f);
                mat.SetTexture("_PaletteTex", ms.optionalPalette);
            }

            for (float elapsed = 0f; elapsed < time; elapsed += BraveTime.DeltaTime)
            {
                float percentLeft = 1f - elapsed / time;
                mat.SetFloat(CwaffVFX._FadeId, 1f - percentLeft * percentLeft);
                yield return null;
            }
            UnityEngine.Object.Destroy(ms.gameObject);
            yield break;
        }
    }

    /// <summary>Get numerical index of current floor.</summary>
    public static float GetFloorIndex()
    {
        return GameManager.Instance.Dungeon.tileIndices.tilesetId switch {
            CASTLEGEON    => 1f,
            SEWERGEON     => 1.5f,
            GUNGEON       => 2f,
            CATHEDRALGEON => 2.5f,
            MINEGEON      => 3f,
            RATGEON       => 3.5f,
            CATACOMBGEON  => 4f,
            OFFICEGEON    => 4.5f,
            FORGEGEON     => 5f,
            HELLGEON      => 5.5f,
            _             => 0f,
        };
    }

  /// <summary>Returns the vector with the greater magnitude</summary>
  public static Vector2 MaxMagnitude(Vector2 a, Vector2 b)
  {
    return (a.sqrMagnitude > b.sqrMagnitude) ? a : b;
  }

  /// <summary>Add an item to an array</summary>
  public static void Append<T>(ref T[] array, T value)
  {
    int oldCount = array.Length;
    Array.Resize(ref array, oldCount + 1);
    array[oldCount] = value;
  }

  /// <summary>Register an easy placeable for use in Room Architect Tool</summary>
  public static T RegisterEasyRATPlaceable<T>(string guid) where T : MonoBehaviour
  {
    GameObject placeableObject = new GameObject(guid).RegisterPrefab();
    T t = placeableObject.AddComponent<T>(); // called after registering prefab so Start() isn't immediately called
    DungeonPlaceable placeable = BreakableAPIToolbox.GenerateDungeonPlaceable(new(){{placeableObject, 1f}});
    StaticReferences.StoredDungeonPlaceables.Add(guid, placeable);
    Alexandria.DungeonAPI.StaticReferences.customPlaceables.Add($"{C.MOD_PREFIX}:{guid}", placeable); // prepend our mod's prefix to the guid for RAT
    return t;
  }
}
