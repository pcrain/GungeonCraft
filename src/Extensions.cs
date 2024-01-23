namespace CwaffingTheGungy;

public static class Extensions
{
  public class Expiration : MonoBehaviour  // destroy a game object after a fixed amount of time, with optional fadeout
  {
    public void ExpireIn(float seconds, float fadeFor = 0f, float startAlpha = 1f, bool shrink = false)
    {
      this.StartCoroutine(Expire(seconds, fadeFor, startAlpha, shrink));
    }

    private IEnumerator Expire(float seconds, float fadeFor = 0f, float startAlpha = 1f, bool shrink = false)
    {
      if (startAlpha < 1f)
        this.gameObject.SetAlphaImmediate(startAlpha);
      float startXScale = this.gameObject.transform.localScale.x;
      float startYScale = this.gameObject.transform.localScale.y;
      if (fadeFor == 0f)
      {
        yield return new WaitForSeconds(seconds);
        UnityEngine.Object.Destroy(this.gameObject);
        yield break;
      }

      float lifeLeft = seconds;
      while (lifeLeft > 0)
      {
        lifeLeft -= BraveTime.DeltaTime;
        float percentAlive = Mathf.Min(1f,lifeLeft / fadeFor);
        this.gameObject.SetAlpha(startAlpha * percentAlive);
        if (shrink)
        {
          this.gameObject.transform.localScale = new Vector3(percentAlive * startXScale, percentAlive * startYScale, 1.0f);
        }
        yield return null;
      }
      UnityEngine.Object.Destroy(this.gameObject);
      yield break;
    }
  }

  // Add an expiration timer to a GameObject
  public static void ExpireIn(this GameObject self, float seconds, float fadeFor = 0f, float startAlpha = 1f, bool shrink = false)
  {
    self.GetOrAddComponent<Expiration>().ExpireIn(seconds, fadeFor, startAlpha, shrink);
  }

  // Check if a rectangle contains a point
  public static bool Contains(this Rect self, Vector2 point)
  {
    return (point.x >= self.xMin && point.x <= self.xMax && point.y >= self.yMin && point.y <= self.yMax);
  }

  // Insets the borders of a rectangle by a specified amount on each side
  public static Rect Inset(this Rect self, float topInset, float rightInset, float bottomInset, float leftInset)
  {
    // ETGModConsole.Log($"  old bounds are {self.xMin},{self.yMin} to {self.xMax},{self.yMax}");
    Rect r = new Rect(self.x + leftInset, self.y + bottomInset, self.width - leftInset - rightInset, self.height - bottomInset - topInset);
    // ETGModConsole.Log($"  new bounds are {r.xMin},{r.yMin} to {r.xMax},{r.yMax}");
    return r;
  }

  // Insets the borders of a rectangle by a specified amount on each axis
  public static Rect Inset(this Rect self, float xInset, float yInset)
  {
    return self.Inset(yInset,xInset,yInset,xInset);
  }

  // Insets the borders of a rectangle by a specified amount on all sides
  public static Rect Inset(this Rect self, float inset)
  {
    return self.Inset(inset,inset,inset,inset);
  }

  // Gets the center of a rectangle
  public static Vector2 Center(this Rect self)
  {
    return new Vector2(self.xMin + self.width / 2, self.yMin + self.height / 2);
  }

  // Get a random point on the perimeter of a rectangle
  public static Vector2 RandomPointOnPerimeter(this Rect self)
    { return self.PointOnPerimeter(UnityEngine.Random.Range(0.0f,1.0f)); }

  // Get a given point on the perimeter of a rectangle scales from 0 to 1 (counterclockwise from bottom-left)
  public static Vector2 PointOnPerimeter(this Rect self, float t)
  {
    // ETGModConsole.Log($"bounds are {self.xMin},{self.yMin} to {self.xMax},{self.yMax}");
    float half  = self.width + self.height;
    float point = t*2.0f*half;
    Vector2 retPoint;
    if (point < self.width) // bottom edge
      retPoint = new Vector2(self.xMin + point, self.yMin);
    else if (point < half) // right edge
      retPoint = new Vector2(self.xMax, self.yMin + point-self.width);
    else if (point-half < self.width) // top edge
      retPoint = new Vector2(self.xMax - (point-half), self.yMax);
    else
      retPoint = new Vector2(self.xMin, self.yMax - (point-half-self.width)); // left edge
    // ETGModConsole.Log($"  chose point {retPoint.x},{retPoint.y}");
    return retPoint;
  }

  // Given an angle and wall Rect, determine the intersection point from a ray cast from self to theWall
  public static Vector2 RaycastToWall(this Vector2 self, float angle, Rect theWall)
  {
    Vector2 intersection = Vector2.positiveInfinity;
    if(!BraveMathCollege.LineSegmentRectangleIntersection(self + 1000f * angle.ToVector(), self, theWall.position, theWall.position + theWall.size, ref intersection))
    {
      // ETGModConsole.Log("no intersection found");
    }
    return intersection;
  }

  // Add a named bullet from a named enemy to a bullet bank
  public static void AddBulletFromEnemy(this AIBulletBank self, string enemyGuid, string bulletName)
  {
    self.Bullets.Add(EnemyDatabase.GetOrLoadByGuid(enemyGuid).bulletBank.GetBullet(bulletName));
  }

  // Register a game object as a prefab
  public static GameObject RegisterPrefab(this GameObject self, bool deactivate = true, bool markFake = true, bool dontUnload = true)
  {
    if (deactivate)
      self.gameObject.SetActive(false); //make sure we aren't an active game object
    if (markFake)
      FakePrefab.MarkAsFakePrefab(self.gameObject); //mark the object as a fake prefab
    if (dontUnload)
      UnityEngine.Object.DontDestroyOnLoad(self); //make sure the object isn't destroyed when loaded as a prefab
    return self;
  }

  // Register a game object as a prefab, with generic support
  public static T RegisterPrefab<T>(this T self, bool deactivate = true, bool markFake = true, bool dontUnload = true)
    where T : Component
  {
    self.gameObject.RegisterPrefab(deactivate, markFake, dontUnload);
    return self;
  }

  // Instantiate a prefab and clone it as a new prefab
  public static GameObject ClonePrefab(this GameObject self, bool deactivate = true, bool markFake = true, bool dontUnload = true)
  {
    return UnityEngine.Object.Instantiate(self).RegisterPrefab(deactivate, markFake, dontUnload).gameObject;
  }

  // Instantiate a prefab and clone it as a new prefab, with generic support
  public static T ClonePrefab<T>(this T self, bool deactivate = true, bool markFake = true, bool dontUnload = true)
    where T : Component
  {
    return UnityEngine.Object.Instantiate<T>(self).RegisterPrefab<T>(deactivate, markFake, dontUnload);
  }

  // Convert degrees to a Vector2 angle
  public static Vector2 ToVector(this float self, float magnitude = 1f)
  {
    return magnitude * (Vector2)(Quaternion.Euler(0f, 0f, self) * Vector2.right);
  }

  // Rotate a Vector2 by specified number of degrees
  public static Vector2 Rotate(this Vector2 self, float rotation)
  {
    return (Vector2)(Quaternion.Euler(0f, 0f, rotation) * self);
  }

  // Clamp a floating point number between -absoluteMax and absoluteMax
  public static float ClampAbsolute(this float self, float absoluteMax)
  {
    return (Mathf.Abs(self) <= absoluteMax) ? self : Mathf.Sign(self)*absoluteMax;
  }

  // Clamp a floating point angle in degrees to [-180,180]
  public static float Clamp180(this float self)
  {
    return BraveMathCollege.ClampAngle180(self);
  }

  // Clamp a floating point angle in degrees to [0,360]
  public static float Clamp360(this float self)
  {
    return BraveMathCollege.ClampAngle360(self);
  }

  // Determine the relative angle (in degrees) between two angles
  public static float RelAngleTo(this float angle, float other)
  {
    return (other - angle).Clamp180();
  }

  // Determine the absolute angle (in degrees) between two angles
  public static float AbsAngleTo(this float angle, float other)
  {
    return Mathf.Abs((other - angle).Clamp180());
  }

  // Determine whether an angle is within a degree tolerance of a floating point angle
  public static bool IsNearAngle(this float angle, float other, float tolerance)
  {
    return angle.AbsAngleTo(other) <= tolerance;
  }

  // Determine whether a Vector is within a degree tolerance of a floating point angle
  public static bool IsNearAngle(this Vector2 v, float angle, float tolerance)
  {
    return v.ToAngle().IsNearAngle(angle, tolerance);
  }

  // Get a bullet's direction to the primary player
  public static float DirToNearestPlayer(this Bullet self)
  {
    return (GameManager.Instance.GetPlayerClosestToPoint(self.Position).CenterPosition - self.Position).ToAngle();
  }

  // Get a bullet's current velocity (because Velocity doesn't work)
  public static Vector2 RealVelocity(this Bullet self)
  {
    return (self.Speed / C.PIXELS_PER_CELL) * self.Direction.ToVector();
  }

  // Get a Quaternion representing an angle rotated on the Z axis
  public static Quaternion EulerZ(this float self)
  {
    return Quaternion.Euler(0f, 0f, self);
  }

  // Get a Quaternion representing a vector rotated on the Z axis
  public static Quaternion EulerZ(this Vector2 self)
  {
    return Quaternion.Euler(0f, 0f, BraveMathCollege.Atan2Degrees(self));
  }

  // Add custom firing audio to a gun
  public static void SetFireAudio<T>(this T agun, string audioEventName = null)
    where T : Alexandria.ItemAPI.AdvancedGunBehavior
  {
    agun.preventNormalFireAudio = true;
    if (audioEventName is not null)
      agun.overrideNormalFireAudio = audioEventName;
    // agun.OverrideNormalFireAudioEvent = audioEventName;
    // agun.GetComponent<tk2dSpriteAnimator>().GetClipByName(agun.shootAnimation).frames[0].triggerEvent = true;
    // agun.GetComponent<tk2dSpriteAnimator>().GetClipByName(agun.shootAnimation).frames[0].eventAudio = audioEventName;
  }

  // Add custom reloading audio to a gun
  public static void SetReloadAudio<T>(this T agun, string audioEventName = null)
    where T : Alexandria.ItemAPI.AdvancedGunBehavior
  {
    agun.preventNormalReloadAudio  = true;
    if (audioEventName is not null)
      agun.overrideNormalReloadAudio = audioEventName;
  }

  // Loop a gun's animation
  public static void LoopAnimation(this Gun gun, string animationName, int loopStart)
  {
    gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(animationName).wrapMode = tk2dSpriteAnimationClip.WrapMode.LoopSection;
    gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(animationName).loopStart = loopStart;
  }

  // Set a projectile's horizontal impact VFX
  public static Projectile SetHorizontalImpactVFX(this Projectile p, VFXPool vfx)
  {
    p.hitEffects.tileMapHorizontal = vfx;  // necessary
    p.hitEffects.deathTileMapHorizontal = vfx; // optional
    return p;
  }

  // Set a gun's horizontal impact VFX
  public static void SetHorizontalImpactVFX(this Gun gun, VFXPool vfx)
  {
    foreach (ProjectileModule mod in gun.Volley.projectiles)
      foreach (Projectile p in mod.projectiles)
        p.SetHorizontalImpactVFX(vfx);
  }

  // Set a projectile's vertical impact VFX
  public static Projectile SetVerticalImpactVFX(this Projectile p, VFXPool vfx)
  {
    p.hitEffects.tileMapVertical = vfx;  // necessary
    p.hitEffects.deathTileMapVertical = vfx; // optional
    return p;
  }

  // Set a gun's vertical impact VFX
  public static void SetVerticalImpactVFX(this Gun gun, VFXPool vfx)
  {
    foreach (ProjectileModule mod in gun.Volley.projectiles)
      foreach (Projectile p in mod.projectiles)
        p.SetVerticalImpactVFX(vfx);
  }

  // Set a projectile's enemy impact VFX
  public static Projectile SetEnemyImpactVFX(this Projectile p, VFXPool vfx)
  {
    p.hitEffects.enemy = vfx;  // necessary
    p.hitEffects.deathEnemy = vfx; // optional
    return p;
  }

  // Set a gun's enemy impact VFX
  public static void SetEnemyImpactVFX(this Gun gun, VFXPool vfx)
  {
    foreach (ProjectileModule mod in gun.Volley.projectiles)
      foreach (Projectile p in mod.projectiles)
        p.SetEnemyImpactVFX(vfx);
  }

  // Set a projectile's midair impact / death VFX
  public static Projectile SetAirImpactVFX(this Projectile p, VFXPool vfx)
  {
    p.hitEffects.suppressMidairDeathVfx = false;
    p.hitEffects.overrideMidairDeathVFX = vfx.effects[0].effects[0].effect;
    return p;
  }

  // Set a gun's midair impact / death VFX
  public static void SetAirImpactVFX(this Gun gun, VFXPool vfx)
  {
    foreach (ProjectileModule mod in gun.Volley.projectiles)
      foreach (Projectile p in mod.projectiles)
        p.SetAirImpactVFX(vfx);
  }

  // Set a projectile's impact VFX across the board
  public static Projectile SetAllImpactVFX(this Projectile p, VFXPool vfx)
  {
    p.SetHorizontalImpactVFX(vfx);
    p.SetVerticalImpactVFX(vfx);
    p.SetEnemyImpactVFX(vfx);
    p.SetAirImpactVFX(vfx);
    return p;
  }

  // Set a gun's impact VFX across the board
  public static void SetAllImpactVFX(this Gun gun, VFXPool vfx)
  {
    gun.DefaultModule.projectiles[0].SetAllImpactVFX(vfx);
  }

  // Check if an enemy is hostile
  public static bool IsHostile(this AIActor e, bool canBeDead = false, bool canBeNeutral = false)
  {
    HealthHaver h = e?.healthHaver;
    return e && !e.IsGone && (canBeNeutral || !e.IsHarmlessEnemy) && h && (canBeDead || (h.IsAlive && !h.IsDead)) && !h.isPlayerCharacter;
  }

  // Check if an enemy is hostile and a non-boss
  public static bool IsHostileAndNotABoss(this AIActor e, bool canBeDead = false, bool canBeNeutral = false)
  {
    HealthHaver h = e?.healthHaver;
    return e && !e.IsGone && (canBeNeutral || !e.IsHarmlessEnemy) && h && !h.IsBoss && !h.IsSubboss &&  (canBeDead || (h.IsAlive && !h.IsDead)) && !h.isPlayerCharacter;
  }

  // Check if an enemy is a boss
  public static bool IsABoss(this AIActor e, bool canBeDead = false)
  {
    HealthHaver h = e?.healthHaver;
    return e && !e.IsGone && h && (h.IsBoss || h.IsSubboss) && (canBeDead || (h.IsAlive && !h.IsDead));
  }

  // Set the Alpha of a GameObject's sprite
  public static void SetAlpha(this GameObject g, float a)
  {
    g.GetComponent<Renderer>()?.SetAlpha(a);
  }

  // Set the Alpha of a Component's sprite (attached to the base component)
  public static void SetAlpha(this Component c, float a)
  {
    c.GetComponent<Renderer>()?.SetAlpha(a);
  }

  // Set the Alpha of a GameObject's sprite immediately and avoid the 1-frame opacity delay upon creation
  public static void SetAlphaImmediate(this GameObject g, float a)
  {
    g.GetComponent<Renderer>()?.SetAlpha(a);
    g.GetComponent<tk2dSpriteAnimator>()?.LateUpdate();
  }

  // Set the Alpha of a Component's sprite immediately and avoid the 1-frame opacity delay upon creation
  public static void SetAlphaImmediate(this Component c, float a)
  {
    c.GetComponent<Renderer>()?.SetAlpha(a);
    c.GetComponent<tk2dSpriteAnimator>()?.LateUpdate();
  }

  // Add emissiveness to a game object
  public static void SetGlowiness(this GameObject g, float a)
  {
    if (g.GetComponent<tk2dBaseSprite>() is not tk2dBaseSprite sprite)
      return;
    sprite.SetGlowiness(a);
  }

  public static void SetGlowiness(this tk2dBaseSprite sprite, float glowAmount, Color? glowColor = null, Color? overrideColor = null, bool clampBrightness = true)
  {
    sprite.usesOverrideMaterial = true;
    Material m = sprite.renderer.material;
    m.shader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTintableTiltedCutoutEmissive");
    if (!clampBrightness)
    {
      m.DisableKeyword("BRIGHTNESS_CLAMP_ON");
      m.EnableKeyword("BRIGHTNESS_CLAMP_OFF");
    }
    m.SetFloat("_EmissivePower", glowAmount);
    if (glowColor is Color c)
    {
      m.SetFloat("_EmissiveColorPower", 1.55f);
      m.SetColor("_EmissiveColor", c);
    }
    if (overrideColor is Color c2)
      m.SetColor("_OverrideColor", c2);
  }

  // Randomly add or subtract an amount from an angle
  public static float AddRandomSpread(this float angle, float spread)
  {
    return angle + (Lazy.CoinFlip() ? 1f : -1f) * spread * UnityEngine.Random.value;
  }

  // Get a list including all of a players' passives, actives, and guns
  public static List<PickupObject> AllItems(this PlayerController player)
  {
      List<PickupObject> allItems = new();
      foreach(PickupObject item in player.passiveItems)
          allItems.Add(item);
      foreach(PickupObject item in player.activeItems)
          allItems.Add(item);
      foreach(PickupObject item in player.inventory.AllGuns)
          allItems.Add(item);
      return allItems;
  }

  // Get a passive item owned by the player
  public static T GetPassive<T>(this PlayerController p) where T : PassiveItem
    => p.passiveItems.Find(item => item is T) as T;

  // Get an active item owned by the player
  public static T GetActive<T>(this PlayerController p)  where T : PlayerItem
    => p.activeItems.Find(item => item is T) as T;

  // Get a gun owned by the player
  public static T GetGun<T>(this PlayerController p)     where T : Gun
    => p.inventory.AllGuns.Find(item => item is T) as T;

  // Clamps a float between two numbers (default 0 and 1)
  public static float Clamp(this float f, float min = 0f, float max = 1f)
  {
    return (f < min) ? min : ((f > max) ? max : f);
  }

  // Check if a player is one hit from death
  public static bool IsOneHitFromDeath(this PlayerController player)
  {
    if (player.ForceZeroHealthState)
      return player.healthHaver.Armor == 1;
    return player.healthHaver.GetCurrentHealth() == 0.5f;
  }

  // Check if a player is in a boss room
  public static bool InBossRoom(this PlayerController player)
  {
      return player.GetAbsoluteParentRoom().area.PrototypeRoomCategory == PrototypeDungeonRoom.RoomCategory.BOSS;
  }

  // https://forum.unity.com/threads/clever-way-to-shuffle-a-list-t-in-one-line-of-c-code.241052/
  public static void Shuffle<T>(this IList<T> ts) {
      var count = ts.Count;
      var last = count - 1;
      for (var i = 0; i < last; ++i) {
          var r = UnityEngine.Random.Range(i, count);
          var tmp = ts[i];
          ts[i] = ts[r];
          ts[r] = tmp;
      }
  }

  // Copy and shuffle a list
  public static List<T> CopyAndShuffle<T>(this List<T> list)
  {
    List<T> shuffled = new();
    foreach (T item in list)
      shuffled.Add(item);
    shuffled.Shuffle();
    return shuffled;
  }

  // Get a numerical quality for a PickupObject
  public static int QualityGrade (this PickupObject pickup)
  {
    switch (pickup.quality)
    {
      case ItemQuality.S: return 5;
      case ItemQuality.A: return 4;
      case ItemQuality.B: return 3;
      case ItemQuality.C: return 2;
      case ItemQuality.D: return 1;
      default:                         return 0;
    }
  }

  // Get the highest quality item in a list
  public static PickupObject HighestQualityItem<T>(this List<T> pickups) where T : PickupObject
  {
    if (pickups == null || pickups.Count == 0)
      return null;
    PickupObject best = pickups[0];
    foreach (PickupObject p in pickups)
    {
      if (p.QualityGrade() > best.QualityGrade())
        best = p;
    }
    return best;
  }

  // Select a random element from an array
  public static T ChooseRandom<T>(this T[] source)
  {
      if (source.Length == 0)
        return default(T);
      return source[UnityEngine.Random.Range(0,source.Length)];
  }

  // Select a random element from a list
  public static T ChooseRandom<T>(this List<T> source)
  {
      if (source == null || source.Count == 0)
        return default(T);
      return source[UnityEngine.Random.Range(0,source.Count)];
  }

  // Select a random element from an enum
  public static T ChooseRandom<T>() where T : Enum
  {
      var v = Enum.GetValues(typeof (T));
      return (T) v.GetValue(UnityEngine.Random.Range(0,v.Length));
  }

  // Check if enemies are actively spawning in a room
  public static bool NewWaveOfEnemiesIsSpawning(this RoomHandler room)
  {
    foreach (AIActor enemy in room.GetActiveEnemies(RoomHandler.ActiveEnemyType.All))
      if (!enemy.isActiveAndEnabled || !enemy.IsValid || !enemy.HasBeenAwoken)
        return true;
    return false;
  }

  // Check if we have line of sight to a target from start without walls interfering
  public static bool HasLineOfSight(this Vector2 start, Vector2 target)
  {
    Vector2 dirVec = target - start;
    RaycastResult collision;
    bool collided = PhysicsEngine.Instance.Raycast(start, dirVec, dirVec.magnitude, out collision, true, false);
    RaycastResult.Pool.Free(ref collision);
    return !collided;
  }

  // Clear a gun's default audio events
  public static void ClearDefaultAudio(this Gun gun, bool useSilentGroup = true)
  {
    if (gun.GetComponent<Alexandria.ItemAPI.AdvancedGunBehavior>() is Alexandria.ItemAPI.AdvancedGunBehavior agun)
    {
      agun.SetFireAudio();
      agun.SetReloadAudio();
    }

    if (useSilentGroup)
      gun.gunSwitchGroup = (ItemHelper.Get(Items.Banana) as Gun).gunSwitchGroup; // banana has silent reload and charge audio

    gun.PreventNormalFireAudio = true;
    gun.OverrideNormalFireAudioEvent = "";

    if (gun.spriteAnimator.GetClipByName(gun.shootAnimation) is tk2dSpriteAnimationClip shootAnimation)
      foreach (tk2dSpriteAnimationFrame f in shootAnimation.frames)
      {
        f.triggerEvent = false;
        f.eventAudio   = "";
      }
    if (gun.spriteAnimator.GetClipByName(gun.chargeAnimation) is tk2dSpriteAnimationClip chargeAnimation)
      foreach (tk2dSpriteAnimationFrame f in chargeAnimation.frames)
      {
        f.triggerEvent = false;
        f.eventAudio   = "";
      }
    if (gun.spriteAnimator.GetClipByName(gun.reloadAnimation) is tk2dSpriteAnimationClip reloadAnimation)
      foreach (tk2dSpriteAnimationFrame f in reloadAnimation.frames)
      {
        f.triggerEvent = false;
        f.eventAudio   = "";
      }
  }

  // Set an audio event for a specific frame of a gun's animation
  public static void SetGunAudio(this Gun gun, string name = null, string audio = "", int frame = 0)
  {
    tk2dSpriteAnimationFrame aframe = gun.spriteAnimator.GetClipByName(name).frames[frame];
    aframe.triggerEvent = !string.IsNullOrEmpty(audio);
    aframe.eventAudio = audio;
  }
  //  doesn't work
  // public static void SetProjectileAudio(this Gun gun, string audio = "")
  // {
  //     gun.OnPostFired += (_, _) => {
  //       ETGModConsole.Log($"making a projectile!");
  //       AkSoundEngine.PostEvent(audio, gun.gameObject);
  //     };
  // }
  // needs to use Alexandria version because fireaudio overrides are not serialized
  public static void SetFireAudio(this Gun gun, string audio = "", int frame = 0)
  {
    gun.SetGunAudio(name: gun.shootAnimation, audio: audio, frame: frame);
    // gun.PreventNormalFireAudio = true;
    // gun.OverrideNormalFireAudioEvent = audio;

    if (gun.GetComponent<Alexandria.ItemAPI.AdvancedGunBehavior>() is Alexandria.ItemAPI.AdvancedGunBehavior agun)
    {
      agun.preventNormalFireAudio = true;
      agun.overrideNormalFireAudio = audio;
    }
  }
  public static void SetReloadAudio(this Gun gun, string audio = "", int frame = 0)
  {
    gun.SetGunAudio(name: gun.reloadAnimation, audio: audio, frame: frame);
  }
  public static void SetReloadAudio(this Gun gun, string audio = "", params int[] frames)
  {
    foreach (int frame in frames)
      gun.SetGunAudio(name: gun.reloadAnimation, audio: audio, frame: frame);
  }
  public static void SetChargeAudio(this Gun gun, string audio = "", int frame = 0)
  {
    gun.SetGunAudio(name: gun.chargeAnimation, audio: audio, frame: frame);
  }
  public static void SetChargeAudio(this Gun gun, string audio = "", params int[] frames)
  {
    foreach (int frame in frames)
      gun.SetGunAudio(name: gun.chargeAnimation, audio: audio, frame: frame);
  }
  public static void SetIdleAudio(this Gun gun, string audio = "", int frame = 0)
  {
    gun.SetGunAudio(name: gun.idleAnimation, audio: audio, frame: frame);
  }

  public static void SetMuzzleVFX(this Gun gun, string resPath = null, float fps = 60, bool loops = false, float scale = 1.0f, Anchor anchor = Anchor.MiddleLeft, bool orphaned = false, float emissivePower = -1, bool continuous = false)
  {
    if (string.IsNullOrEmpty(resPath))
      gun.muzzleFlashEffects = null; //.type = VFXPoolType.None;
    else
      gun.muzzleFlashEffects = VFX.CreatePool(resPath, fps: fps,
        loops: loops, scale: scale, anchor: anchor, alignment: VFXAlignment.Fixed, orphaned: orphaned, attached: true, emissivePower: emissivePower);
    gun.usesContinuousMuzzleFlash = continuous;
  }

  public static void SetMuzzleVFX(this Gun gun, Items gunToCopyFrom, bool onlyCopyBasicEffects = true)
  {
    Gun otherGun = ItemHelper.Get(gunToCopyFrom) as Gun;
    if (!otherGun)
      return;

    gun.muzzleFlashEffects = otherGun.muzzleFlashEffects;
    if (onlyCopyBasicEffects)
      return;

    gun.usesContinuousMuzzleFlash  = otherGun.usesContinuousMuzzleFlash;
    gun.finalMuzzleFlashEffects    = otherGun.finalMuzzleFlashEffects;
    gun.CriticalMuzzleFlashEffects = otherGun.CriticalMuzzleFlashEffects;
  }

  public static void SetCasing(this Gun gun, Items otherGun)
  {
    gun.shellCasing = (ItemHelper.Get(otherGun) as Gun).shellCasing;
  }


  public static bool Contains(this List<PassiveItem> items, int itemId)
  {
    foreach(PassiveItem item in items)
      if (item.PickupObjectId == itemId)
        return true;

    return false;
  }

  // Gets the actual rectangle corresponding to the the outermost walls of a room
  //   - Useful for boss fights
  //   - Useful for phasing checks
  // private const int _ROOM_PIXEL_FUDGE_FACTOR = 16;
  private const int _ROOM_PIXEL_FUDGE_FACTOR = 8;
  public static Rect GetRoomPixelBorder(this RoomHandler room)
  {
    Rect roomRect = room.GetBoundingRect();
    Rect roomPixelRect = new Rect(16 * roomRect.xMin, 16 * roomRect.yMin, 16 * roomRect.width, 16 * roomRect.height);
    return roomPixelRect.Inset(1, 1, _ROOM_PIXEL_FUDGE_FACTOR, 1).Inset(8);
  }

  public static bool FullyWithinRoom(this PlayerController pc, RoomHandler room = null)
  {
    return pc.specRigidbody.PixelColliders[0].FullyWithin((room ?? pc.CurrentRoom).GetRoomPixelBorder());
  }

  public static bool ForceConstrainToRoom(this PlayerController pc, RoomHandler room)
  {
    // ETGModConsole.Log($"position is {pc.specRigidbody.Position.PixelPosition}");

    Position oldPosition = pc.specRigidbody.Position;
    PixelCollider collider = pc.specRigidbody.PixelColliders[0];
    // ETGModConsole.Log($"bounds are {collider.Min} to {collider.Max}");
    Rect roomRect = room.GetRoomPixelBorder();

    bool adjusted = false;
    Vector2 vector = pc.transform.position.XY();

    if (collider.MinX < roomRect.xMin)
    {
      IntVector2 force = new IntVector2(1, 0) * (int)(roomRect.xMin - collider.MinX);
      // ETGModConsole.Log($"pushing right with force {force}");
      vector += PhysicsEngine.PixelToUnit(force);
      adjusted = true;
    }
    else if (collider.MaxX > roomRect.xMax)
    {
      IntVector2 force = new IntVector2(1, 0) * (int)(roomRect.xMax - collider.MaxX);
      // ETGModConsole.Log($"pushing left with force {force}");
      vector += PhysicsEngine.PixelToUnit(force);
      adjusted = true;
    }

    if (collider.MinY < roomRect.yMin)
    {
      IntVector2 force = new IntVector2(0, 1) * (int)(roomRect.yMin - collider.MinY);
      // ETGModConsole.Log($"pushing up with force {force}");
      vector += PhysicsEngine.PixelToUnit(force);
      adjusted = true;
    }
    else if (collider.MaxY > roomRect.yMax)
    {
      IntVector2 force = new IntVector2(0, 1) * (int)(roomRect.yMax - collider.MaxY);
      // ETGModConsole.Log($"pushing down with force {force}");
      vector += PhysicsEngine.PixelToUnit(force);
      adjusted = true;
    }

    if (adjusted)
    {
      pc.transform.position = vector;
      pc.specRigidbody.Reinitialize();
    }
    return adjusted;
  }

  // Check if a PixelColider is fully within a rectangle
  public static bool FullyWithin(this PixelCollider self, Rect other)
  {
    return new Rect(self.MinX, self.MinY, self.Dimensions.X, self.Dimensions.Y).FullyWithin(other);
  }

  // Check if a rectangle is fully within another rectangle
  public static bool FullyWithin(this Rect self, Rect other)
  {
    bool xwithin = self.xMin > other.xMin && self.xMax < other.xMax;
    bool ywithin = self.yMin > other.yMin && self.yMax < other.yMax;
    return xwithin && ywithin;
  }

  // Set some basic attributes for each gun
  public static void SetAttributes(this Gun gun, ItemQuality quality, GunClass gunClass, float reloadTime, int ammo,
    Items audioFrom = Items.Blasphemy, bool defaultAudio = false, bool infiniteAmmo = false, bool canGainAmmo = true, bool canReloadNoMatterAmmo = false, bool? doesScreenShake = null)
  {
    gun.quality = quality;
    gun.reloadTime = reloadTime;
    gun.gunClass = gunClass;
    gun.SetBaseMaxAmmo(ammo);
    gun.CurrentAmmo = gun.GetBaseMaxAmmo(); // necessary iff gun basemaxammo > 1000

    gun.gunSwitchGroup = (ItemHelper.Get(audioFrom) as Gun).gunSwitchGroup;
    gun.InfiniteAmmo = infiniteAmmo;
    gun.CanGainAmmo = canGainAmmo;
    gun.CanReloadNoMatterAmmo = canReloadNoMatterAmmo;

    gun.doesScreenShake = doesScreenShake ?? gun.doesScreenShake;

    if (!defaultAudio)
      gun.ClearDefaultAudio();
  }

  public static TrailController AddTrailToProjectilePrefab(this Projectile target, string spritePath, Vector2 colliderDimensions, Vector2 colliderOffsets, List<string> animPaths = null, int animFPS = -1, List<string> startAnimPaths = null, int startAnimFPS = -1, float timeTillAnimStart = -1, float cascadeTimer = -1, float softMaxLength = -1, bool destroyOnEmpty = false)
  {
      TrailController trail = VFX.CreateTrailObject(
        spritePath, colliderDimensions, colliderOffsets, animPaths, animFPS, startAnimPaths, startAnimFPS, timeTillAnimStart, cascadeTimer, softMaxLength, destroyOnEmpty);
      trail.gameObject.SetActive(true); // parent projectile is deactivated, so we want to re-activate ourselves so we display correctly when the projectile becomes active
      trail.gameObject.transform.parent = target.transform;
      return trail;
  }

  public static TrailController AddTrailToProjectilePrefab(this Projectile target, TrailController trail)
  {
      trail.gameObject.SetActive(true); // parent projectile is deactivated, so we want to re-activate ourselves so we display correctly when the projectile becomes active
      trail.gameObject.transform.parent = target.transform;
      return trail;
  }

  public static TrailController AddTrailToProjectileInstance(this Projectile target, TrailController trail)
  {
      GameObject instantiatedTrail = UnityEngine.Object.Instantiate(trail.gameObject);
      instantiatedTrail.transform.parent = target.transform;
      return instantiatedTrail.GetComponent<TrailController>();
  }

  public static SpriteTrailController AddTrailToSpriteInstance(this tk2dBaseSprite target, SpriteTrailController trail)
  {
      GameObject instantiatedTrail = UnityEngine.Object.Instantiate(trail.gameObject);
      instantiatedTrail.transform.parent = target.transform;
      return instantiatedTrail.GetComponent<SpriteTrailController>();
  }

  // Set the rotation of a projectile manually
  public static void SetRotation(this Projectile p, float angle)
  {
    p.m_transform.eulerAngles = new Vector3(0f, 0f, angle);
  }

  // Add a new animation to the same collection as a reference sprite
  public static string SetUpAnimation(this tk2dBaseSprite sprite, string animationName, float fps, tk2dSpriteAnimationClip.WrapMode wrapMode = tk2dSpriteAnimationClip.WrapMode.Once, bool copyShaders = false)
  {
    tk2dSpriteCollectionData collection = sprite.collection;
    tk2dSpriteDefinition referenceFrameDef = collection.spriteDefinitions[sprite.spriteId];
    tk2dSpriteAnimator anim = sprite.spriteAnimator;
    List<int> spriteIds = new();
    foreach (string spritePath in ResMap.Get(animationName))
    {
        int frameSpriteId = SpriteBuilder.AddSpriteToCollection(spritePath, collection);
        spriteIds.Add(frameSpriteId);
        if (copyShaders)
        {
          tk2dSpriteDefinition frameDef = collection.spriteDefinitions[frameSpriteId];
          frameDef.material.shader = referenceFrameDef.material.shader;
          // frameDef.materialInst.shader = referenceFrameDef.materialInst.shader;
        }
    }
    tk2dSpriteAnimationClip clip = SpriteBuilder.AddAnimation(anim, collection, spriteIds, animationName, wrapMode, fps);
    return animationName;
  }

  // Same as PlaceAtPositionByAnchor(), but adjusted for sprite's current scale
  public static void PlaceAtScaledPositionByAnchor(this tk2dBaseSprite sprite, Vector3 position, Anchor anchor)
  {
      Vector2 scale = sprite.transform.localScale.XY();
      Vector2 anchorPos = sprite.GetRelativePositionFromAnchor(anchor);
      Vector2 relativePositionFromAnchor = new Vector2(scale.x * anchorPos.x, scale.y * anchorPos.y);
      // Vector2 relativePositionFromAnchor = Vector2.Cross(sprite.transform.localScale.XY(), sprite.GetRelativePositionFromAnchor(anchor));
      sprite.transform.position = position - relativePositionFromAnchor.ToVector3ZUp();
  }

  // Same as PlaceAtPositionByAnchor(), but adjusted for sprite's current scale and rotation
  public static void PlaceAtRotatedPositionByAnchor(this tk2dBaseSprite sprite, Vector3 position, Anchor anchor)
  {
      Vector2 scale = sprite.transform.localScale.XY();
      Vector2 anchorPos = sprite.GetRelativePositionFromAnchor(anchor);
      Vector2 relativePositionFromAnchor = sprite.transform.rotation * new Vector2(scale.x * anchorPos.x, scale.y * anchorPos.y);
      // Vector2 relativePositionFromAnchor = Vector2.Cross(sprite.transform.localScale.XY(), sprite.GetRelativePositionFromAnchor(anchor));
      sprite.transform.position = position - relativePositionFromAnchor.ToVector3ZUp();
  }

  // Remove and return last element from list
  public static T Pop<T>(this List<T> items)
  {
    T item = items[items.Count - 1];
    items.RemoveAt(items.Count - 1);
    return item;
  }

  // Clear out all old behaviors for a BehaviorSpeculator and restart everything
  public static void FullyRefreshBehaviors(this BehaviorSpeculator self)
  {
    self.m_behaviors.Clear();
    self.RefreshBehaviors();
  }

  // Set up custom ammo types from default resource paths
  public static void SetupCustomAmmoClip(this ProjectileModule mod, GunBuildData b)
  {
      string clipname    = b.gun.EncounterNameOrDisplayName.SafeName();
      // if (C.DEBUG_BUILD)
      //   ETGModConsole.Log($"  getting clip {$"{clipname}_clip"}");
      mod.ammoType       = GameUIAmmoType.AmmoType.CUSTOM;
      mod.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType($"{clipname}_clip", ResMap.Get($"{clipname}_clipfull")[0], ResMap.Get($"{clipname}_clipempty")[0]);
  }

  // Check if a player will die from next hit
  public static bool PlayerWillDieFromHit(this HealthHaver hh, HealthHaver.ModifyDamageEventArgs data)
  {
    if (data == EventArgs.Empty || data.ModifiedDamage <= 0f || !hh.IsVulnerable)
      return false; // if we weren't going to take damage anyway, nothing to do

    if (hh.Armor > 1 || hh.GetCurrentHealth() > data.ModifiedDamage)
      return false; // no character is one hit from death in this situation

    if (hh.Armor == 1 && hh.GetCurrentHealth() > 0)
      return false; // we have both armor and health, so we are not the robot, and we are fine

    return true;
  }

  // Remove a shader from a gameObject
  public static void RemoveShader(this GameObject g, Shader shader)
  {
    if (g?.GetComponent<MeshRenderer>() is not MeshRenderer component)
      return;
    Material[] sharedMaterials = component.sharedMaterials;
    List<Material> list = new List<Material>();
    for (int i = 0; i < sharedMaterials.Length; i++)
    {
      if (sharedMaterials[i].shader != shader)
        list.Add(sharedMaterials[i]);
    }
    component.sharedMaterials = list.ToArray();
  }

  // Add a shader to a gameObject, and return the material for that shader
  public static Material GetOrAddShader(this GameObject g, Shader shader, bool atBeginning = true)
  {
    if (g?.GetComponent<MeshRenderer>() is not MeshRenderer component)
      return null;
    Material[] array = component.sharedMaterials;
    for (int i = 0; i < array.Length; i++)
      if (array[i].shader == shader)
        return array[i];
    Array.Resize(ref array, array.Length + 1);
    Material material = new Material(shader);
    material.SetTexture("_MainTex", array[0].GetTexture("_MainTex"));
    if (atBeginning)
    {
      for (int i = array.Length - 1; i > 1; --i)
        array[i] = array[i - 1];
      array[1] = material;
    }
    else
      array[array.Length - 1] = material;
    component.sharedMaterials = array;
    return material;
  }

  // Check if a goop position is electrificed
  public static bool IsPositionElectrified(this DeadlyDeadlyGoopManager goopManager, Vector2 position)
  {
    IntVector2 key = (position / DeadlyDeadlyGoopManager.GOOP_GRID_SIZE).ToIntVector2(VectorConversions.Floor);
    DeadlyDeadlyGoopManager.GoopPositionData value;
    if (goopManager.m_goopedCells.TryGetValue(key, out value) && value.remainingLifespan > goopManager.goopDefinition.fadePeriod)
    {
      return value.IsElectrified;
    }
    return false;
  }

  // Returns a singleton of an empty IEnumerable when the collection being extended is empty
  public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T> enumerable)
  {
    return enumerable ?? Enumerable.Empty<T>();
  }

  internal static  tk2dSpriteCollectionData _GunCollection = null;

  // Get a list of barrel offsets for a gun's animation
  public static List<Vector3> GetBarrelOffsetsForAnimation(this Gun gun, string animationName)
  {
    List<Vector3> offsets = new();
    _GunCollection ??= gun.sprite.collection;

    tk2dSpriteAnimationClip clip = gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(animationName);
    for (int i = 0; i < clip.frames.Count(); ++i)
    {
        int attachIndex = _GunCollection.SpriteIDsWithAttachPoints.IndexOf(clip.frames[i].spriteId);
        foreach (tk2dSpriteDefinition.AttachPoint a in _GunCollection.SpriteDefinedAttachPoints[attachIndex].attachPoints)
            if (a.name == "Casing")
                offsets.Add(a.position);
    }

    return offsets;
  }

  // Returns true if a projectile was fired from a gun without depleting ammo
  public static bool FiredForFree(this Projectile proj, Gun gun, ProjectileModule mod)
  {
    return (mod.ammoCost == 0 || gun.InfiniteAmmo || gun.LocalInfiniteAmmo /*|| gun.CanGainAmmo*/ || ((proj.Owner as PlayerController)?.InfiniteAmmo?.Value ?? false));
  }

  // Add a component to a projectile's GameObject and return the component
  public static T AddComponent<T>(this Projectile projectile) where T : MonoBehaviour
  {
    return projectile.gameObject.AddComponent<T>();
  }

  // Returns or adds a component to a projectile's GameObject and return the component
  public static T GetOrAddComponent<T>(this Projectile projectile) where T : MonoBehaviour
  {
    return projectile.gameObject.GetOrAddComponent<T>();
  }

  // Get the internal sprite name for each gun (keep in parity with SetupItem())
  public static string InternalName(this Gun gun)
  {
    return gun.gunName.Replace("-", "").Replace(".", "").Replace(" ", "_").ToLower(); // keep in parity with SetupItem()
  }

  // Get the internal sprite name for each gun (keep in parity with SetupItem())
  public static string InternalSpriteName(this Gun gun)
  {
    return gun.InternalName().Replace("'",""); // keep in parity with SetupItem()
  }

  // Get the internal name of an item / gun corresponding to its sprite
  public static string SafeName(this string s)
  {
    return s.Replace(" ", "_").Replace("'","").Replace(".","").ToLower();
  }

  // Set the FPS for a gun's idle animation (including the fixed idle animation, if available)
  public static void SetIdleAnimationFPS(this Gun gun, int fps)
  {
    gun.SetAnimationFPS($"{gun.InternalSpriteName()}_idle", fps);
    gun.SetAnimationFPS($"{gun.InternalSpriteName()}_{LargeGunAnimationHotfix._TRIM_ANIMATION}", fps);
  }

  // Force a gun to render on top of the player (call this in LateUpdate())
  public static void RenderInFrontOfPlayer(this Gun gun)
  {
    if (gun.CurrentOwner is not PlayerController pc)
      return;
    if (pc.m_currentGunAngle >= 25f && pc.m_currentGunAngle <= 155f)
      return;

    gun.GetSprite().HeightOffGround = 0.075f;
    gun.GetSprite().UpdateZDepth();
  }

  // Set an animated projectile to play a singular frame
  public static void SetFrame(this Projectile projectile, int frame)
  {
      projectile.spriteAnimator.deferNextStartClip = true;
      projectile.spriteAnimator.SetFrame(frame);
      projectile.spriteAnimator.Stop();
  }

  // Add strings to the global string database
  public static void SetupDBStrings(this string key, List<string> values)
  {
    StringDBTable table = ETGMod.Databases.Strings.Core;
    foreach (string v in values)
      table.AddComplex(key, v);
  }

  // Convert a list of pickup ids to an evenly-weighted loot table
  public static GenericLootTable ToLootTable(this List<int> ids)
  {
    GenericLootTable loot = FancyShopBuilder.CreateLootTable();
    foreach (int id in ids)
        loot.AddItemToPool(id);
    return loot;
  }

  // Shift all vectors in a list by a different vector
  public static List<Vector3> ShiftAll(this IEnumerable<Vector3> vecList, Vector3 shift)
  {
    List<Vector3> vecs = new();
    foreach (Vector3 v in vecList)
      vecs.Add(v + shift);
    return vecs;
  }

  // Find a custom shop item currently under consideration by player
  public static CustomShopItemController GetTargetedItemByPlayer(this CustomShopController shop, PlayerController player)
  {
      if (!shop?.transform)
        return null;
      foreach (Transform child in shop.transform)
      {
          CustomShopItemController[] shopItems =child?.gameObject?.GetComponentsInChildren<CustomShopItemController>();
          if ((shopItems?.Length ?? 0) == 0)
              continue;
          if (shopItems[0] is not CustomShopItemController shopItem)
              continue;
          if (player?.m_lastInteractionTarget?.Equals(shopItem) ?? false)
              return shopItem;
      }
      return null;
  }

  // Pseudo-homing behavior
  public static Vector2 LerpNaturalAndDirectVelocity(this Vector2 position, Vector2 target, Vector2 naturalVelocity, float accel, float lerpFactor)
  {
      Vector2 towardsTarget = target - position;
      // Compute our natural velocity from accelerating towards our target
      Vector2 newNaturalVelocity = naturalVelocity + (accel * towardsTarget.normalized);
      // Compute a direct velocity from redirecting all of our momentum towards our target
      Vector2 newDirectVelocity = (naturalVelocity.magnitude + accel) * towardsTarget.normalized;
      // Take a weighted average
      return Vector2.Lerp(newDirectVelocity, newNaturalVelocity, lerpFactor);
  }

  // Get Debris objects within a cone of vision from some reference position, optionally checking at most limit debris
  private static int _nextDebris = 0;
  public static IEnumerable<DebrisObject> DebrisWithinCone(this Vector2 start, float squareReach, float angle, float spread, int limit = -1)
  {
      int total = StaticReferenceManager.AllDebris.Count;
      if (total == 0)
        yield break;

      int next  = 0;
      int last  = total;
      if (limit > 0 && limit < total)
      {
        next = _nextDebris % total;
        last = (next + limit) % total;
      }

      float minAngle = angle - spread;
      float maxAngle = angle + spread;
      for (; next != last; ++next)
      {
          if (next == total)
          {
            next = 0;
            if (next == last)
              break;
          }

          DebrisObject debris = StaticReferenceManager.AllDebris[next];
          if (!debris || !debris.isActiveAndEnabled || !debris.HasBeenTriggered)
              continue; // not triggered yet
          if (debris.IsPickupObject || debris.Priority == EphemeralObject.EphemeralPriority.Critical)
              continue; // don't vacuum up important objects
          Vector2 deltaVec = (debris.gameObject.transform.position.XY() - start);
          if (deltaVec.sqrMagnitude > squareReach || !deltaVec.ToAngle().IsNearAngle(angle, spread))
              continue; // out of range
          yield return debris;
      }

      _nextDebris = last;
      yield break;
  }

  // Get impact VFX from a specific Gun as a VFXPool
  public static VFXPool EnemyImpactPool(this Items item, int proj = 0)
    => (ItemHelper.Get(item) as Gun).DefaultModule.projectiles[proj].hitEffects.enemy;
  public static VFXPool HorizontalImpactPool(this Items item, int proj = 0)
    => (ItemHelper.Get(item) as Gun).DefaultModule.projectiles[proj].hitEffects.tileMapHorizontal;
  public static VFXPool VerticalImpactPool(this Items item, int proj = 0)
    => (ItemHelper.Get(item) as Gun).DefaultModule.projectiles[proj].hitEffects.tileMapVertical;

  // Get impact VFX from a specific Gun as a GameObject
  public static GameObject EnemyImpactVFX(this Items item, int proj = 0)
    => (ItemHelper.Get(item) as Gun).DefaultModule.projectiles[proj].hitEffects.enemy.effects[0].effects[0].effect;
  public static GameObject HorizontalImpactVFX(this Items item, int proj = 0)
    => (ItemHelper.Get(item) as Gun).DefaultModule.projectiles[proj].hitEffects.tileMapHorizontal.effects[0].effects[0].effect;
  public static GameObject VerticalImpactVFX(this Items item, int proj = 0)
    => (ItemHelper.Get(item) as Gun).DefaultModule.projectiles[proj].hitEffects.tileMapVertical.effects[0].effects[0].effect;
  public static GameObject AirImpactVFX(this Items item, int proj = 0)
    => (ItemHelper.Get(item) as Gun).DefaultModule.projectiles[proj].hitEffects.overrideMidairDeathVFX;

  // Append a string to all strings in a list
  public static List<string> AppendAll(this List<string> strings, string suffix)
  {
    List<string> newStrings = new();
    foreach (string s in strings)
      newStrings.Add(s+suffix);
    return newStrings;
  }

  // Destroy a GameObject if it is non-null
  public static void SafeDestroy(this GameObject g)
  {
    if (g) UnityEngine.Object.Destroy(g);
  }

  // Destroy a Component if it is non-null
  public static void SafeDestroy<T>(this T c) where T : Component
  {
    if (c) UnityEngine.Object.Destroy(c);
  }

  // Select a pickup id from a weighted list
  public static int GetWeightedPickupID(this List<IntVector2> weights)
  {
    int targetWeight = UnityEngine.Random.Range(0, weights.Sum(item => item.y));
    foreach (IntVector2 weight in weights)
      if ((targetWeight -= weight.y) < 0)
        return weight.x;
    return 0;
  }

  // Get the first element of a list if possible, returning null otherwise
  public static T SafeFirst<T>(this List<T> c)
  {
    return ((c?.Count ?? 0) == 0) ? default(T) : c[0];
  }

  // Get the first element of a list if possible, returning null otherwise
  public static Projectile FirstValidChargeProjectile(this ProjectileModule mod)
  {
    List<ChargeProjectile> c = mod.chargeProjectiles;
    if ((c?.Count ?? 0) == 0)
      return null;
    foreach (ChargeProjectile cp in c)
    {
      if (cp.Projectile is Projectile p)
        return p;
    }
    return null;
  }
}
