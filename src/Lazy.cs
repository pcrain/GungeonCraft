namespace CwaffingTheGungy;

/// <summary>All-purpose helper methods for being a lazy dumdum</summary>
public static class Lazy
{
    internal static tk2dSpriteCollectionData _GunSpriteCollection = null;

    /// <summary>Log with the console only in debug mode</summary>
    public static void DebugLog(object text)
    {
    if (C.DEBUG_BUILD)
      ETGModConsole.Log(text);
    }

    /// <summary>Warn with the console only in debug mode</summary>
    public static void DebugWarn(string text)
    {
    if (C.DEBUG_BUILD)
      ETGModConsole.Log($"<color=#ffffaaff>{text}</color>");
    }

    /// <summary>Perform basic initialization for a new passive, active, or gun item definition.</summary>
    public static TItemClass SetupItem<TItemClass, TItemSpecific>(string itemName, string spritePath, string projectileName, string shortDescription, string longDescription, string lore, bool hideFromAmmonomicon = false)
        where TItemClass : PickupObject   // must be PickupObject for passive items, PlayerItem for active items, or Gun for guns
        where TItemSpecific : TItemClass  // must be a subclass of TItemClass
    {
        string newItemName  = itemName.Replace("-", "").Replace(".", "");  //get sane gun for item rename
        string baseItemName = newItemName.Replace(" ", "_").ToLower();  //get saner gun name for commands
        IDs.InternalNames[itemName] = C.MOD_PREFIX+":"+baseItemName;

        TItemClass item;

        if (typeof(TItemClass) == typeof(Gun))
        {
            string spriteName = spritePath; // TODO: guns use names, regular items use full paths -- should be made uniform eventually
            Gun gun = ETGMod.Databases.Items.NewGun(itemName, spriteName);  //create a new gun using specified sprite name
            Game.Items.Rename("outdated_gun_mods:"+baseItemName, IDs.InternalNames[itemName]);  //rename the gun for commands
            gun.SetupSprite(null, spriteName+"_idle_001"); //set the gun's ammonomicon sprite

            int projectileId = 0;
            if (int.TryParse(projectileName, out projectileId))
                gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(projectileId) as Gun, true, true); //set the gun's default projectile to inherit
            else
                gun.AddProjectileModuleFrom(projectileName, true, false); //set the gun's default projectile to inherit
            item = gun as TItemClass;
        }
        else
        {
            // Interpret paths with slashes as fully-qualified resource paths, and use our ResMap otherwise
            string spriteName = spritePath.Contains("/") ? spritePath : ResMap.Get(spritePath)[0];
            string altName = itemName.SafeName() + "_icon";
            if (spritePath != altName)
                ETGModConsole.Log($"  {spritePath} != {altName}");
            GameObject obj = new GameObject(itemName).RegisterPrefab();
            item = obj.AddComponent<TItemSpecific>();
            // ItemBuilder.AddSpriteToObject(itemName, spriteName, obj);
            tk2dSprite sprite = obj.AddComponent<tk2dSprite>();
            tk2dSpriteCollectionData coll = ItemHelper.Get(Items.AmmoSynthesizer).sprite.Collection;
            tk2dSpriteDefinition dd = PackerHelper.NamedSpriteInPackedTexture(spritePath);
            if (dd == null || coll == null)
                ETGModConsole.Log($"YIKES O_O_O_O");
            int spriteID = SpriteBuilder.AddSpriteToCollection(dd, coll);
            sprite.SetSprite(coll, spriteID);
            // SpriteBuilder.SpriteFromTexture(ETGMod.Assets.TextureMap[$"sprites/ItemSprites/{altName}"], spriteName, obj);

            ETGMod.Databases.Items.SetupItem(item, item.name);

            Gungeon.Game.Items.Add(IDs.InternalNames[itemName], item);
            SpriteBuilder.AddToAmmonomicon(item.sprite.GetCurrentSpriteDef());
            item.encounterTrackable.journalData.AmmonomiconSprite = item.sprite.GetCurrentSpriteDef().name;
        }

        item.itemName = itemName;
        item.encounterTrackable.EncounterGuid = C.MOD_PREFIX+"-"+baseItemName; //create a unique guid for the item
        item.SetShortDescription(shortDescription);
        item.SetLongDescription($"{longDescription}\n\n{lore}");
        ETGMod.Databases.Items.Add(item);

        if (hideFromAmmonomicon)
            item.gameObject.GetComponent<EncounterTrackable>().journalData.SuppressInAmmonomicon = true;

        IDs.Pickups[baseItemName] = item.PickupObjectId; //register item in pickup ID database
        if (item is Gun)
        {
            IDs.Guns[baseItemName] = item.PickupObjectId; //register item in gun ID database
            if (C.DEBUG_BUILD && !hideFromAmmonomicon)
                ETGModConsole.Log($"Lazy Initialized Gun: {baseItemName} ({item.DisplayName})");
        }
        else if (item is PlayerItem)
        {
            IDs.Actives[baseItemName] = item.PickupObjectId; //register item in active ID database
            if (C.DEBUG_BUILD && !hideFromAmmonomicon)
                ETGModConsole.Log($"Lazy Initialized Active: {baseItemName} ({item.DisplayName})");
        }
        else
        {
            IDs.Passives[baseItemName] = item.PickupObjectId; //register item in passive ID database
            if (C.DEBUG_BUILD && !hideFromAmmonomicon)
                ETGModConsole.Log($"Lazy Initialized Passive: {baseItemName} ({item.DisplayName})");
        }
        return item;
    }

    /// <summary>
    /// Perform basic initialization for a new passive item definition.
    /// </summary>
    public static PickupObject SetupPassive<T>(string itemName, string shortDescription, string longDescription, string lore, bool hideFromAmmonomicon = false)
        where T : PickupObject
    {
        return SetupItem<PickupObject, T>(itemName, $"{itemName.SafeName()}_icon", "", shortDescription, longDescription, lore, hideFromAmmonomicon: hideFromAmmonomicon);
    }

    /// <summary>
    /// Perform basic initialization for a new active item definition.
    /// </summary>
    public static PlayerItem SetupActive<T>(string itemName, string shortDescription, string longDescription, string lore, bool hideFromAmmonomicon = false)
        where T : PlayerItem
    {
        return SetupItem<PlayerItem, T>(itemName, $"{itemName.SafeName()}_icon", "", shortDescription, longDescription, lore, hideFromAmmonomicon: hideFromAmmonomicon);
    }

    /// <summary>
    /// Perform basic initialization for a new gun definition.
    /// </summary>
    public static Gun SetupGun<T>(string gunName, string projectileName, string shortDescription, string longDescription, string lore, bool hideFromAmmonomicon = false)
        where T : Alexandria.ItemAPI.AdvancedGunBehavior
    {
        Gun gun = SetupItem<Gun, Gun>(gunName, gunName.SafeName(), projectileName, shortDescription, longDescription, lore, hideFromAmmonomicon: hideFromAmmonomicon);
        gun.gameObject.AddComponent<T>();
        _GunSpriteCollection ??= gun.sprite.collection; // need to initialize at least once

        #region Auto-setup barrelOffset from Casing attach point
            foreach (tk2dSpriteDefinition.AttachPoint a in gun.AttachPointsForClip(gun.idleAnimation).EmptyIfNull())
                if (a.name == "Casing")
                    gun.barrelOffset.transform.localPosition = a.position;
        #endregion

        #region Set up trimmed idle sprites so we don't have wonky hitboxes for very large animations
            gun.UpdateAnimation(LargeGunAnimationHotfix._TRIM_ANIMATION, returnToIdle: true);
            string fixedIdleAnimation = $"{gun.InternalSpriteName()}_{LargeGunAnimationHotfix._TRIM_ANIMATION}";
            tk2dSpriteAnimationClip originalIdleClip = gun.spriteAnimator.GetClipByName(gun.idleAnimation);
            int fixedIdleAnimationClipId = gun.spriteAnimator.GetClipIdByName(fixedIdleAnimation);
            if (fixedIdleAnimationClipId != -1)
            {
                string originalIdleAnimation = gun.idleAnimation;
                int fixedIdleAnimationSpriteId = gun.spriteAnimator.GetClipByName(fixedIdleAnimation).frames[0].spriteId;

                // Fix sprite animator
                gun.SetAnimationFPS(fixedIdleAnimation, (int)originalIdleClip.fps);
                gun.LoopAnimation(fixedIdleAnimation, originalIdleClip.loopStart);
                gun.idleAnimation                = fixedIdleAnimation;
                gun.spriteAnimator.defaultClipId = fixedIdleAnimationClipId;

                // Fix pickup object sprite
                // gun.m_defaultSpriteID = fixedIdleAnimationSpriteId;
                // gun.GetComponent<PickupObject>().sprite.spriteId = fixedIdleAnimationSpriteId;
                // _GunSpriteCollection.SpriteIDsWithAttachPoints.Add(fixedIdleAnimationSpriteId);
                // _GunSpriteCollection.SpriteDefinedAttachPoints.Add(new AttachPointData(gun.AttachPointsForClip(originalIdleAnimation)));
            }
            else if (C.DEBUG_BUILD)
                ETGModConsole.Log($"  no fixed idle animation for {gunName}");
        #endregion

        #region Auto-play idle animation
            gun.spriteAnimator.DefaultClipId = gun.spriteAnimator.GetClipIdByName(gun.idleAnimation);
            gun.spriteAnimator.playAutomatically = true;
        #endregion

        return gun;
    }

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
    /// Create a basic list of named directional animations given a list of animation names previously setup with SpriteBuilder.AddSpriteToCollection
    /// </summary>
    public static List<AIAnimator.NamedDirectionalAnimation> EasyNamedDirectionalAnimations(string[] animNameList)
    {
        var theList = new List<AIAnimator.NamedDirectionalAnimation>();
        for(int i = 0; i < animNameList.Count(); ++i)
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
        tk2dSpriteDefinition bestIdleSprite = enemy.sprite.collection.spriteDefinitions[CwaffToolbox.GetIdForBestIdleAnimation(enemy)];
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
    }

    private static IEnumerator PlaySoundUntilDeathOrTimeout_CR(string soundName, GameObject source, float timer)
    {
        _SoundTimers[soundName] = timer;
        source.Play(soundName);
        while (source != null && _SoundTimers[soundName] > 0)
        {
            _SoundTimers[soundName] -= BraveTime.DeltaTime;
            yield return null;
        }
        source.Play($"{soundName}_stop_all");
        _SoundTimers.Remove(soundName);
    }

    /// <summary>Create some smoke VFX at the specified position</summary>
    public static void DoSmokeAt(Vector3 pos)
    {
        UnityEngine.Object.Instantiate(ResourceCache.Acquire("Global VFX/VFX_Item_Spawn_Poof") as GameObject)
            .GetComponent<tk2dBaseSprite>()
            .PlaceAtPositionByAnchor(pos, Anchor.MiddleCenter);
    }

    /// <summary>Create a pickup spawn poof VFX at the specified position</summary>
    public static void DoPickupAt(Vector3 pos)
    {
        GameObject original = (GameObject)ResourceCache.Acquire("Global VFX/VFX_Item_Pickup");
          GameObject gameObject = UnityEngine.Object.Instantiate(original);
            tk2dSprite sprite = gameObject.GetComponent<tk2dSprite>();
                sprite.PlaceAtPositionByAnchor(pos, Anchor.MiddleCenter);
                sprite.HeightOffGround = 6f;
                sprite.UpdateZDepth();
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
            _NullProjectilePrefab                     = Items.Ak47.CloneProjectile(new(damage: 0.0f, speed: 0.00001f, range: 1.0f));
            _NullProjectilePrefab.damageTypes         = CoreDamageTypes.None;
            _NullProjectilePrefab.collidesWithEnemies = false;
            _NullProjectilePrefab.collidesWithPlayer  = false;
            _NullProjectilePrefab.gameObject.AddComponent<Expiration>().expirationTimer = 0f;
        }
        return _NullProjectilePrefab;
    }

    /// <summary>Determine whether any enemy is in an line between start and end (does not account for walls)</summary>
    public static bool AnyEnemyInLineOfSight(Vector2 start, Vector2 end, bool canBeNeutral = true)
    {
        Vector2 intersection = Vector2.zero;
        foreach (AIActor enemy in start.GetAbsoluteRoom()?.GetActiveEnemies(RoomHandler.ActiveEnemyType.All).EmptyIfNull())
        {
            if (!enemy.IsHostile(canBeNeutral: canBeNeutral))
                continue;
            PixelCollider collider = enemy.specRigidbody.HitboxPixelCollider;
            if (BraveUtility.LineIntersectsAABB(start, end, collider.UnitBottomLeft, collider.UnitDimensions, out intersection))
                return true;
        }
        return false;
    }

    /// <summary>Determine the nearest enemy inside a cone of vision from position start within maxDeviation degree of coneAngle</summary>
    public static Vector2? NearestEnemyWithinConeOfVision(Vector2 start, float coneAngle, float maxDeviation, bool useNearestAngleInsteadOfDistance, bool ignoreWalls = false)
    {
        bool foundTarget   = false;
        float bestAngle    = maxDeviation;
        float bestDist     = 9999f;
        Vector2 bestTarget = Vector2.zero;
        foreach (AIActor enemy in start.GetAbsoluteRoom()?.GetActiveEnemies(RoomHandler.ActiveEnemyType.All).EmptyIfNull())
        {
            if (!enemy.IsHostile(canBeNeutral: true))
                continue;
            Vector2 tentativeTarget = enemy.sprite.WorldCenter;
            if (!ignoreWalls && !start.HasLineOfSight(tentativeTarget))
                continue;
            Vector2 delta        = (tentativeTarget - start);
            float angle          = delta.ToAngle().Clamp360();
            float angleDeviation = Mathf.Abs((coneAngle - angle).Clamp180());
            if (angleDeviation > maxDeviation)
                continue;
            float dist           = delta.magnitude;
            bool bestSoFar       = useNearestAngleInsteadOfDistance
                ? (angleDeviation < bestAngle)
                : (dist < bestDist);
            if (!bestSoFar)
                continue;
            foundTarget = true;
            bestTarget  = tentativeTarget;
            bestAngle   = angleDeviation;
            bestDist    = dist;
        }
        return foundTarget ? bestTarget : null;
    }

    /// <summary>Determine the nearest enemy to position start</summary>
    public static Vector2? NearestEnemy(Vector2 start, float coneAngle, bool useNearestAngleInsteadOfDistance = false, bool ignoreWalls = false)
    {
        return NearestEnemyWithinConeOfVision(start, coneAngle, 360f, useNearestAngleInsteadOfDistance, ignoreWalls);
    }

    /// <summary>Spawn a chest with a single guaranteed item inside of it</summary>
    public static Chest SpawnChestWithSpecificItem(PickupObject pickup, IntVector2 position, ItemQuality? overrideChestQuality = null)
    {
      Chest chestPrefab =
        GameManager.Instance.RewardManager.GetTargetChestPrefab(overrideChestQuality ?? pickup.quality)
        ?? GameManager.Instance.RewardManager.GetTargetChestPrefab(ItemQuality.B);
      Chest chest = Chest.Spawn(chestPrefab, position);
      chest.forceContentIds = new(){pickup.PickupObjectId};
      return chest;
    }

    // https://martin.ankerl.com/2007/10/04/optimized-pow-approximation-for-java-and-c-c/
    /// <summary>Compute a fast approximation for a^b</summary>
    public static double FastPow(double a, double b) {
        int tmp = (int)(BitConverter.DoubleToInt64Bits(a) >> 32);
        int tmp2 = (int)(b * (tmp - 1072632447) + 1072632447);
        return BitConverter.Int64BitsToDouble(((long)tmp2) << 32);
    }

    /// <summary>Set up a UI sprite from the provided resource paths</summary>
    // public static dfSprite SetupUISprite(List<string> resourcePaths)
    // {
    //     dfSprite uiSprite = CustomClipAmmoTypeToolbox.SetupDfSpriteFromTexture<dfSprite>(
    //         new GameObject().RegisterPrefab(),
    //         ResourceExtractor.GetTextureFromResource(resourcePaths[0] + ".png"),
    //         ShaderCache.Acquire("Daikon Forge/Default UI Shader"));
    //     return uiSprite;
    // }

    /// <summary>Get a modded item by id, returning null if it doesn't exit</summary>
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
}
