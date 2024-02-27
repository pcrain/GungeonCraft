namespace CwaffingTheGungy;

public static class Extensions
{
  /// <summary>destroy a game object after a fixed amount of time, with optional fadeout</summary>
  public class Expiration : MonoBehaviour
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

  /// <summary>Add an expiration timer to a GameObject</summary>
  public static void ExpireIn(this GameObject self, float seconds, float fadeFor = 0f, float startAlpha = 1f, bool shrink = false)
  {
    self.GetOrAddComponent<Expiration>().ExpireIn(seconds, fadeFor, startAlpha, shrink);
  }

  /// <summary>Check if a rectangle contains a point</summary>
  public static bool Contains(this Rect self, Vector2 point)
  {
    return (point.x >= self.xMin && point.x <= self.xMax && point.y >= self.yMin && point.y <= self.yMax);
  }

  /// <summary>Insets the borders of a rectangle by a specified amount on each side</summary>
  public static Rect Inset(this Rect self, float topInset, float rightInset, float bottomInset, float leftInset)
  {
    // ETGModConsole.Log($"  old bounds are {self.xMin},{self.yMin} to {self.xMax},{self.yMax}");
    Rect r = new Rect(self.x + leftInset, self.y + bottomInset, self.width - leftInset - rightInset, self.height - bottomInset - topInset);
    // ETGModConsole.Log($"  new bounds are {r.xMin},{r.yMin} to {r.xMax},{r.yMax}");
    return r;
  }

  /// <summary>Insets the borders of a rectangle by a specified amount on each axis</summary>
  public static Rect Inset(this Rect self, float xInset, float yInset)
  {
    return self.Inset(yInset,xInset,yInset,xInset);
  }

  /// <summary>Insets the borders of a rectangle by a specified amount on all sides</summary>
  public static Rect Inset(this Rect self, float inset)
  {
    return self.Inset(inset,inset,inset,inset);
  }

  /// <summary>Gets the center of a rectangle</summary>
  public static Vector2 Center(this Rect self)
  {
    return new Vector2(self.xMin + self.width / 2, self.yMin + self.height / 2);
  }

  /// <summary>Get a random point on the perimeter of a rectangle</summary>
  public static Vector2 RandomPointOnPerimeter(this Rect self)
    { return self.PointOnPerimeter(UnityEngine.Random.Range(0.0f,1.0f)); }

  /// <summary>Get a given point on the perimeter of a rectangle scales from 0 to 1 (counterclockwise from bottom-left)</summary>
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

  /// <summary>Given an angle and wall Rect, determine the intersection point from a ray cast from self to theWall</summary>
  public static Vector2 RaycastToWall(this Vector2 self, float angle, Rect theWall)
  {
    Vector2 intersection = Vector2.positiveInfinity;
    if(!BraveMathCollege.LineSegmentRectangleIntersection(self + 1000f * angle.ToVector(), self, theWall.position, theWall.position + theWall.size, ref intersection))
    {
      // ETGModConsole.Log("no intersection found");
    }
    return intersection;
  }

  /// <summary>Add a named bullet from a named enemy to a bullet bank</summary>
  public static void AddBulletFromEnemy(this AIBulletBank self, string enemyGuid, string bulletName)
  {
    self.Bullets.Add(EnemyDatabase.GetOrLoadByGuid(enemyGuid).bulletBank.GetBullet(bulletName));
  }

  /// <summary>Register a game object as a prefab</summary>
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

  /// <summary>Register a game object as a prefab, with generic support</summary>
  public static T RegisterPrefab<T>(this T self, bool deactivate = true, bool markFake = true, bool dontUnload = true)
    where T : Component
  {
    self.gameObject.RegisterPrefab(deactivate, markFake, dontUnload);
    return self;
  }

  /// <summary>Instantiate a prefab and clone it as a new prefab</summary>
  public static GameObject ClonePrefab(this GameObject self, bool deactivate = true, bool markFake = true, bool dontUnload = true)
  {
    return UnityEngine.Object.Instantiate(self).RegisterPrefab(deactivate, markFake, dontUnload).gameObject;
  }

  /// <summary>Instantiate a prefab and clone it as a new prefab, with generic support</summary>
  public static T ClonePrefab<T>(this T self, bool deactivate = true, bool markFake = true, bool dontUnload = true)
    where T : Component
  {
    return UnityEngine.Object.Instantiate<T>(self).RegisterPrefab<T>(deactivate, markFake, dontUnload);
  }

  /// <summary>Convert degrees to a Vector2 angle</summary>
  public static Vector2 ToVector(this float self, float magnitude = 1f)
  {
    return magnitude * (Vector2)(Quaternion.Euler(0f, 0f, self) * Vector2.right);
  }

  /// <summary>Rotate a Vector2 by specified number of degrees</summary>
  public static Vector2 Rotate(this Vector2 self, float rotation)
  {
    return (Vector2)(Quaternion.Euler(0f, 0f, rotation) * self);
  }

  /// <summary>Clamp a floating point number between -absoluteMax and absoluteMax</summary>
  public static float ClampAbsolute(this float self, float absoluteMax)
  {
    return (Mathf.Abs(self) <= absoluteMax) ? self : Mathf.Sign(self)*absoluteMax;
  }

  /// <summary>Clamp a floating point angle in degrees to [-180,180]</summary>
  public static float Clamp180(this float self)
  {
    return BraveMathCollege.ClampAngle180(self);
  }

  /// <summary>Clamp a floating point angle in degrees to [0,360]</summary>
  public static float Clamp360(this float self)
  {
    return BraveMathCollege.ClampAngle360(self);
  }

  /// <summary>Determine the relative angle (in degrees) between two angles</summary>
  public static float RelAngleTo(this float angle, float other)
  {
    return (other - angle).Clamp180();
  }

  /// <summary>Determine the absolute angle (in degrees) between two angles</summary>
  public static float AbsAngleTo(this float angle, float other)
  {
    return Mathf.Abs((other - angle).Clamp180());
  }

  /// <summary>Determine whether an angle is within a degree tolerance of a floating point angle</summary>
  public static bool IsNearAngle(this float angle, float other, float tolerance)
  {
    return angle.AbsAngleTo(other) <= tolerance;
  }

  /// <summary>Determine whether a Vector is within a degree tolerance of a floating point angle</summary>
  public static bool IsNearAngle(this Vector2 v, float angle, float tolerance)
  {
    return v.ToAngle().IsNearAngle(angle, tolerance);
  }

  /// <summary>Get a bullet's direction to the primary player</summary>
  public static float DirToNearestPlayer(this Bullet self)
  {
    return (GameManager.Instance.GetPlayerClosestToPoint(self.Position).CenterPosition - self.Position).ToAngle();
  }

  /// <summary>Get a bullet's current velocity (because Velocity doesn't work)</summary>
  public static Vector2 RealVelocity(this Bullet self)
  {
    return (self.Speed / C.PIXELS_PER_CELL) * self.Direction.ToVector();
  }

  /// <summary>Get a Quaternion representing an angle rotated on the Z axis</summary>
  public static Quaternion EulerZ(this float self)
  {
    return Quaternion.Euler(0f, 0f, self);
  }

  /// <summary>Get a Quaternion representing a vector rotated on the Z axis</summary>
  public static Quaternion EulerZ(this Vector2 self)
  {
    return Quaternion.Euler(0f, 0f, BraveMathCollege.Atan2Degrees(self));
  }

  /// <summary>Add custom firing audio to a gun</summary>
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

  /// <summary>Add custom reloading audio to a gun</summary>
  public static void SetReloadAudio<T>(this T agun, string audioEventName = null)
    where T : Alexandria.ItemAPI.AdvancedGunBehavior
  {
    agun.preventNormalReloadAudio  = true;
    if (audioEventName is not null)
      agun.overrideNormalReloadAudio = audioEventName;
  }

  /// <summary>Loop a gun's animation</summary>
  public static void LoopAnimation(this Gun gun, string animationName, int loopStart)
  {
    gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(animationName).wrapMode = tk2dSpriteAnimationClip.WrapMode.LoopSection;
    gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(animationName).loopStart = loopStart;
  }

  /// <summary>Set a projectile's horizontal impact VFX</summary>
  public static Projectile SetHorizontalImpactVFX(this Projectile p, VFXPool vfx)
  {
    p.hitEffects.tileMapHorizontal = vfx;  // necessary
    p.hitEffects.deathTileMapHorizontal = vfx; // optional
    return p;
  }

  /// <summary>Set a gun's horizontal impact VFX</summary>
  public static void SetHorizontalImpactVFX(this Gun gun, VFXPool vfx)
  {
    foreach (ProjectileModule mod in gun.Volley.projectiles)
      foreach (Projectile p in mod.projectiles)
        p.SetHorizontalImpactVFX(vfx);
  }

  /// <summary>Set a projectile's vertical impact VFX</summary>
  public static Projectile SetVerticalImpactVFX(this Projectile p, VFXPool vfx)
  {
    p.hitEffects.tileMapVertical = vfx;  // necessary
    p.hitEffects.deathTileMapVertical = vfx; // optional
    return p;
  }

  /// <summary>Set a gun's vertical impact VFX</summary>
  public static void SetVerticalImpactVFX(this Gun gun, VFXPool vfx)
  {
    foreach (ProjectileModule mod in gun.Volley.projectiles)
      foreach (Projectile p in mod.projectiles)
        p.SetVerticalImpactVFX(vfx);
  }

  /// <summary>Set a projectile's enemy impact VFX</summary>
  public static Projectile SetEnemyImpactVFX(this Projectile p, VFXPool vfx)
  {
    p.hitEffects.enemy = vfx;  // necessary
    p.hitEffects.deathEnemy = vfx; // optional
    return p;
  }

  /// <summary>Set a gun's enemy impact VFX</summary>
  public static void SetEnemyImpactVFX(this Gun gun, VFXPool vfx)
  {
    foreach (ProjectileModule mod in gun.Volley.projectiles)
      foreach (Projectile p in mod.projectiles)
        p.SetEnemyImpactVFX(vfx);
  }

  /// <summary>Set a projectile's midair impact / death VFX</summary>
  public static Projectile SetAirImpactVFX(this Projectile p, VFXPool vfx)
  {
    p.hitEffects.suppressMidairDeathVfx = false;
    p.hitEffects.overrideMidairDeathVFX = vfx.effects[0].effects[0].effect;
    return p;
  }

  /// <summary>Set a gun's midair impact / death VFX</summary>
  public static void SetAirImpactVFX(this Gun gun, VFXPool vfx)
  {
    foreach (ProjectileModule mod in gun.Volley.projectiles)
      foreach (Projectile p in mod.projectiles)
        p.SetAirImpactVFX(vfx);
  }

  /// <summary>Set a projectile's impact VFX across the board</summary>
  public static Projectile SetAllImpactVFX(this Projectile p, VFXPool vfx)
  {
    p.SetHorizontalImpactVFX(vfx);
    p.SetVerticalImpactVFX(vfx);
    p.SetEnemyImpactVFX(vfx);
    p.SetAirImpactVFX(vfx);
    return p;
  }

  /// <summary>Set a gun's impact VFX across the board</summary>
  public static void SetAllImpactVFX(this Gun gun, VFXPool vfx)
  {
    gun.DefaultModule.projectiles[0].SetAllImpactVFX(vfx);
  }

  /// <summary>Check if an enemy is hostile</summary>
  public static bool IsHostile(this AIActor e, bool canBeDead = false, bool canBeNeutral = false)
  {
    HealthHaver h = e?.healthHaver;
    return e && !e.IsGone && (canBeNeutral || !e.IsHarmlessEnemy) && h && (canBeDead || (h.IsAlive && !h.IsDead)) && !h.isPlayerCharacter;
  }

  /// <summary>Check if an enemy is hostile and a non-boss</summary>
  public static bool IsHostileAndNotABoss(this AIActor e, bool canBeDead = false, bool canBeNeutral = false)
  {
    HealthHaver h = e?.healthHaver;
    return e && !e.IsGone && (canBeNeutral || !e.IsHarmlessEnemy) && h && !h.IsBoss && !h.IsSubboss &&  (canBeDead || (h.IsAlive && !h.IsDead)) && !h.isPlayerCharacter;
  }

  /// <summary>Check if an enemy is a boss</summary>
  public static bool IsABoss(this AIActor e, bool canBeDead = false)
  {
    HealthHaver h = e?.healthHaver;
    return e && !e.IsGone && h && (h.IsBoss || h.IsSubboss) && (canBeDead || (h.IsAlive && !h.IsDead));
  }

  /// <summary>Set the Alpha of a GameObject's sprite</summary>
  public static void SetAlpha(this GameObject g, float a)
  {
    g.GetComponent<Renderer>()?.SetAlpha(a);
  }

  /// <summary>Set the Alpha of a Component's sprite (attached to the base component)</summary>
  public static void SetAlpha(this Component c, float a)
  {
    c.GetComponent<Renderer>()?.SetAlpha(a);
  }

  /// <summary>Set the Alpha of a GameObject's sprite immediately and avoid the 1-frame opacity delay upon creation</summary>
  public static void SetAlphaImmediate(this GameObject g, float a)
  {
    g.GetComponent<Renderer>()?.SetAlpha(a);
    g.GetComponent<tk2dSpriteAnimator>()?.LateUpdate();
  }

  /// <summary>Set the Alpha of a Component's sprite immediately and avoid the 1-frame opacity delay upon creation</summary>
  public static void SetAlphaImmediate(this Component c, float a)
  {
    c.GetComponent<Renderer>()?.SetAlpha(a);
    c.GetComponent<tk2dSpriteAnimator>()?.LateUpdate();
  }

  /// <summary>Add emissiveness to a game object</summary>
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

  /// <summary>Randomly add or subtract an amount from an angle</summary>
  public static float AddRandomSpread(this float angle, float spread)
  {
    return angle + (Lazy.CoinFlip() ? 1f : -1f) * spread * UnityEngine.Random.value;
  }

  /// <summary>Get a list including all of a players' passives, actives, and guns</summary>
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

  /// <summary>Get a passive item owned by the player</summary>
  public static T GetPassive<T>(this PlayerController p) where T : PassiveItem
    => p.passiveItems.Find(item => item is T) as T;

  /// <summary>Get an active item owned by the player</summary>
  public static T GetActive<T>(this PlayerController p)  where T : PlayerItem
    => p.activeItems.Find(item => item is T) as T;

  /// <summary>Get a gun owned by the player</summary>
  public static T GetGun<T>(this PlayerController p)     where T : Gun
    => p.inventory.AllGuns.Find(item => item is T) as T;

  /// <summary>Clamps a float between two numbers (default 0 and 1)</summary>
  public static float Clamp(this float f, float min = 0f, float max = 1f)
  {
    return (f < min) ? min : ((f > max) ? max : f);
  }

  /// <summary>Check if a player is one hit from death</summary>
  public static bool IsOneHitFromDeath(this PlayerController player)
  {
    if (player.ForceZeroHealthState)
      return player.healthHaver.Armor == 1;
    return player.healthHaver.GetCurrentHealth() == 0.5f;
  }

  /// <summary>Check if a player is in a boss room</summary>
  public static bool InBossRoom(this PlayerController player)
  {
      return player.GetAbsoluteParentRoom().area.PrototypeRoomCategory == PrototypeDungeonRoom.RoomCategory.BOSS;
  }

  /// <summary>https://forum.unity.com/threads/clever-way-to-shuffle-a-list-t-in-one-line-of-c-code.241052/</summary>
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

  /// <summary>Copy and shuffle a list</summary>
  public static List<T> CopyAndShuffle<T>(this List<T> list)
  {
    List<T> shuffled = new();
    foreach (T item in list)
      shuffled.Add(item);
    shuffled.Shuffle();
    return shuffled;
  }

  /// <summary>Get a numerical quality for a PickupObject</summary>
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

  /// <summary>Get the highest quality item in a list</summary>
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

  /// <summary>Select a random element from an array</summary>
  public static T ChooseRandom<T>(this T[] source)
  {
      if (source.Length == 0)
        return default(T);
      return source[UnityEngine.Random.Range(0,source.Length)];
  }

  /// <summary>Select a random element from a list</summary>
  public static T ChooseRandom<T>(this List<T> source)
  {
      if (source == null || source.Count == 0)
        return default(T);
      return source[UnityEngine.Random.Range(0,source.Count)];
  }

  /// <summary>Select a random element from an enum</summary>
  public static T ChooseRandom<T>() where T : Enum
  {
      var v = Enum.GetValues(typeof (T));
      return (T) v.GetValue(UnityEngine.Random.Range(0,v.Length));
  }

  /// <summary>Check if enemies are actively spawning in a room</summary>
  public static bool NewWaveOfEnemiesIsSpawning(this RoomHandler room)
  {
    foreach (AIActor enemy in room.GetActiveEnemies(RoomHandler.ActiveEnemyType.All))
      if (!enemy.isActiveAndEnabled || !enemy.IsValid || !enemy.HasBeenAwoken)
        return true;
    return false;
  }

  /// <summary>Check if we have line of sight to a target from start without walls interfering</summary>
  public static bool HasLineOfSight(this Vector2 start, Vector2 target)
  {
    Vector2 dirVec = target - start;
    RaycastResult collision;
    bool collided = PhysicsEngine.Instance.Raycast(start, dirVec, dirVec.magnitude, out collision, true, false);
    RaycastResult.Pool.Free(ref collision);
    return !collided;
  }

  /// <summary>Clear a gun's default audio events</summary>
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

  /// <summary>Set an audio event for a specific frame of a gun's animation</summary>
  public static void SetGunAudio(this Gun gun, string name = null, string audio = "", int frame = 0)
  {
    tk2dSpriteAnimationFrame aframe = gun.spriteAnimator.GetClipByName(name).frames[frame];
    aframe.triggerEvent = !string.IsNullOrEmpty(audio);
    aframe.eventAudio = audio;
  }

  /// <summary>Set an audio event for several frames of a gun's animation</summary>
  public static void SetGunAudio(this Gun gun, string name = null, string audio = "", params int[] frames)
  {
    foreach (int frame in frames)
      gun.SetGunAudio(name: name, audio: audio, frame: frame);
  }

  /// <summary>needs to use Alexandria version because fireaudio overrides are not serialized</summary>
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
  /// <summary>private const int _ROOM_PIXEL_FUDGE_FACTOR = 16;</summary>
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

  /// <summary>Check if a PixelColider is fully within a rectangle</summary>
  public static bool FullyWithin(this PixelCollider self, Rect other)
  {
    return new Rect(self.MinX, self.MinY, self.Dimensions.X, self.Dimensions.Y).FullyWithin(other);
  }

  /// <summary>Check if a rectangle is fully within another rectangle</summary>
  public static bool FullyWithin(this Rect self, Rect other)
  {
    bool xwithin = self.xMin > other.xMin && self.xMax < other.xMax;
    bool ywithin = self.yMin > other.yMin && self.yMax < other.yMax;
    return xwithin && ywithin;
  }

  /// <summary>Set some basic attributes for each gun</summary>
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

  /// <summary>Create a prefab trail and add it to a prefab projectile</summary>
  public static TrailController AddTrailToProjectilePrefab(this Projectile target, string spritePath, Vector2 colliderDimensions, Vector2 colliderOffsets, List<string> animPaths = null, int animFPS = -1, List<string> startAnimPaths = null, int startAnimFPS = -1, float timeTillAnimStart = -1, float cascadeTimer = -1, float softMaxLength = -1, bool destroyOnEmpty = false)
  {
      TrailController trail = VFX.CreateTrailObject(
        spritePath, colliderDimensions, colliderOffsets, animPaths, animFPS, startAnimPaths, startAnimFPS, timeTillAnimStart, cascadeTimer, softMaxLength, destroyOnEmpty);
      trail.gameObject.SetActive(true); // parent projectile is deactivated, so we want to re-activate ourselves so we display correctly when the projectile becomes active
      trail.gameObject.transform.parent = target.transform;
      return trail;
  }

  /// <summary>Add an existing prefab trail to a prefab projectile</summary>
  public static TrailController AddTrailToProjectilePrefab(this Projectile target, TrailController trail)
  {
      trail.gameObject.SetActive(true); // parent projectile is deactivated, so we want to re-activate ourselves so we display correctly when the projectile becomes active
      trail.gameObject.transform.parent = target.transform;
      return trail;
  }

  /// <summary>Add an existing prefab trail to a projectile instance</summary>
  public static TrailController AddTrailToProjectileInstance(this Projectile target, TrailController trail)
  {
      GameObject instantiatedTrail = UnityEngine.Object.Instantiate(trail.gameObject);
      instantiatedTrail.transform.parent = target.transform;
      return instantiatedTrail.GetComponent<TrailController>();
  }

  /// <summary>Add an existing prefab trail to a sprite instance</summary>
  public static SpriteTrailController AddTrailToSpriteInstance(this tk2dBaseSprite target, SpriteTrailController trail)
  {
      GameObject instantiatedTrail = UnityEngine.Object.Instantiate(trail.gameObject);
      instantiatedTrail.transform.parent = target.transform;
      return instantiatedTrail.GetComponent<SpriteTrailController>();
  }

  /// <summary>Set the rotation of a projectile manually</summary>
  public static void SetRotation(this Projectile p, float angle)
  {
    p.m_transform.eulerAngles = new Vector3(0f, 0f, angle);
  }

  /// <summary>Add a new animation to the same collection as a reference sprite</summary>
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

  /// <summary>Same as PlaceAtPositionByAnchor(), but adjusted for sprite's current scale</summary>
  public static void PlaceAtScaledPositionByAnchor(this tk2dBaseSprite sprite, Vector3 position, Anchor anchor)
  {
      Vector2 scale = sprite.transform.localScale.XY();
      Vector2 anchorPos = sprite.GetRelativePositionFromAnchor(anchor);
      Vector2 relativePositionFromAnchor = new Vector2(scale.x * anchorPos.x, scale.y * anchorPos.y);
      // Vector2 relativePositionFromAnchor = Vector2.Cross(sprite.transform.localScale.XY(), sprite.GetRelativePositionFromAnchor(anchor));
      sprite.transform.position = position - relativePositionFromAnchor.ToVector3ZUp();
  }

  /// <summary>Same as PlaceAtPositionByAnchor(), but adjusted for sprite's current scale and rotation</summary>
  public static void PlaceAtRotatedPositionByAnchor(this tk2dBaseSprite sprite, Vector3 position, Anchor anchor)
  {
      Vector2 scale = sprite.transform.localScale.XY();
      Vector2 anchorPos = sprite.GetRelativePositionFromAnchor(anchor);
      Vector2 relativePositionFromAnchor = sprite.transform.rotation * new Vector2(scale.x * anchorPos.x, scale.y * anchorPos.y);
      // Vector2 relativePositionFromAnchor = Vector2.Cross(sprite.transform.localScale.XY(), sprite.GetRelativePositionFromAnchor(anchor));
      sprite.transform.position = position - relativePositionFromAnchor.ToVector3ZUp();
  }

  /// <summary>Remove and return last element from list</summary>
  public static T Pop<T>(this List<T> items)
  {
    T item = items[items.Count - 1];
    items.RemoveAt(items.Count - 1);
    return item;
  }

  /// <summary>Clear out all old behaviors for a BehaviorSpeculator and restart everything</summary>
  public static void FullyRefreshBehaviors(this BehaviorSpeculator self)
  {
    self.m_behaviors.Clear();
    if (self.aiActor == null)
      ETGModConsole.Log($"  null actor!");
    self.RefreshBehaviors();
  }

  /// <summary>Set up custom ammo types from default resource paths</summary>
  public static void SetupCustomAmmoClip(this ProjectileModule mod, GunBuildData b)
  {
      string clipname    = b.gun.EncounterNameOrDisplayName.SafeName();
      // if (C.DEBUG_BUILD)
      //   ETGModConsole.Log($"  getting clip {$"{clipname}_clip"}");
      mod.ammoType       = GameUIAmmoType.AmmoType.CUSTOM;
      mod.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType($"{clipname}_clip", ResMap.Get($"{clipname}_clipfull")[0], ResMap.Get($"{clipname}_clipempty")[0]);
  }

  /// <summary>Check if a player will die from next hit</summary>
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

  /// <summary>Remove a shader from a gameObject</summary>
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

  /// <summary>Add a shader to a gameObject, and return the material for that shader</summary>
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

  /// <summary>Check if a goop position is electrificed</summary>
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

  /// <summary>Returns a singleton of an empty IEnumerable when the collection being extended is empty</summary>
  public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T> enumerable)
  {
    return enumerable ?? Enumerable.Empty<T>();
  }

  internal static  tk2dSpriteCollectionData _GunCollection = null;

  /// <summary>Get a list of barrel offsets for a gun's animation</summary>
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

  /// <summary>Returns true if a projectile was fired from a gun without depleting ammo</summary>
  public static bool FiredForFree(this Projectile proj, Gun gun, ProjectileModule mod)
  {
    return (mod.ammoCost == 0 || gun.InfiniteAmmo || gun.LocalInfiniteAmmo /*|| gun.CanGainAmmo*/ || ((proj.Owner as PlayerController)?.InfiniteAmmo?.Value ?? false));
  }

  /// <summary>Add a component to a projectile's GameObject and return the component</summary>
  public static T AddComponent<T>(this Projectile projectile) where T : MonoBehaviour
  {
    return projectile.gameObject.AddComponent<T>();
  }

  /// <summary>Returns or adds a component to a projectile's GameObject and return the component</summary>
  public static T GetOrAddComponent<T>(this Projectile projectile) where T : MonoBehaviour
  {
    return projectile.gameObject.GetOrAddComponent<T>();
  }

  /// <summary>Get the internal sprite name for each gun (keep in parity with SetupItem())</summary>
  public static string InternalName(this Gun gun)
  {
    return gun.gunName.Replace("-", "").Replace(".", "").Replace(" ", "_").ToLower(); // keep in parity with SetupItem()
  }

  /// <summary>Get the internal sprite name for each gun (keep in parity with SetupItem())</summary>
  public static string InternalSpriteName(this Gun gun)
  {
    return gun.InternalName().Replace("'",""); // keep in parity with SetupItem()
  }

  /// <summary>Get the internal name of an item / gun corresponding to its sprite</summary>
  public static string SafeName(this string s)
  {
    return s.Replace(" ", "_").Replace("'","").Replace(".","").ToLower();
  }

  /// <summary>Set the FPS for a gun's idle animation (including the fixed idle animation, if available)</summary>
  public static void SetIdleAnimationFPS(this Gun gun, int fps)
  {
    gun.SetAnimationFPS($"{gun.InternalSpriteName()}_idle", fps);
    gun.SetAnimationFPS($"{gun.InternalSpriteName()}_{LargeGunAnimationHotfix._TRIM_ANIMATION}", fps);
  }

  /// <summary>Force a gun to render on top of the player (call this in LateUpdate())</summary>
  public static void RenderInFrontOfPlayer(this Gun gun)
  {
    if (gun.CurrentOwner is not PlayerController pc)
      return;
    if (pc.m_currentGunAngle >= 25f && pc.m_currentGunAngle <= 155f)
      return;

    gun.GetSprite().HeightOffGround = 0.075f;
    gun.GetSprite().UpdateZDepth();
  }

  /// <summary>Set an animated projectile to play a singular frame</summary>
  public static void SetFrame(this Projectile projectile, int frame)
  {
      projectile.spriteAnimator.deferNextStartClip = true;
      projectile.spriteAnimator.SetFrame(frame);
      projectile.spriteAnimator.Stop();
  }

  /// <summary>Add strings to the global string database</summary>
  public static void SetupDBStrings(this string key, List<string> values)
  {
    StringDBTable table = ETGMod.Databases.Strings.Core;
    foreach (string v in values)
      table.AddComplex(key, v);
  }

  /// <summary>Convert a list of pickup ids to an evenly-weighted loot table</summary>
  public static GenericLootTable ToLootTable(this List<int> ids)
  {
    GenericLootTable loot = FancyShopBuilder.CreateLootTable();
    foreach (int id in ids)
        loot.AddItemToPool(id);
    return loot;
  }

  /// <summary>Shift all vectors in a list by a different vector</summary>
  public static List<Vector3> ShiftAll(this IEnumerable<Vector3> vecList, Vector3 shift)
  {
    List<Vector3> vecs = new();
    foreach (Vector3 v in vecList)
      vecs.Add(v + shift);
    return vecs;
  }

  /// <summary>Find a custom shop item currently under consideration by player</summary>
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

  /// <summary>Pseudo-homing behavior</summary>
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

  /// <summary>Get Debris objects within a cone of vision from some reference position, optionally checking at most limit debris</summary>
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

  /// <summary>Get impact VFX from a specific Gun as a VFXPool</summary>
  public static VFXPool EnemyImpactPool(this Items item, int proj = 0)
    => (ItemHelper.Get(item) as Gun).DefaultModule.projectiles[proj].hitEffects.enemy;
  public static VFXPool HorizontalImpactPool(this Items item, int proj = 0)
    => (ItemHelper.Get(item) as Gun).DefaultModule.projectiles[proj].hitEffects.tileMapHorizontal;
  public static VFXPool VerticalImpactPool(this Items item, int proj = 0)
    => (ItemHelper.Get(item) as Gun).DefaultModule.projectiles[proj].hitEffects.tileMapVertical;

  /// <summary>Get impact VFX from a specific Gun as a GameObject</summary>
  public static GameObject EnemyImpactVFX(this Items item, int proj = 0)
    => (ItemHelper.Get(item) as Gun).DefaultModule.projectiles[proj].hitEffects.enemy.effects[0].effects[0].effect;
  public static GameObject HorizontalImpactVFX(this Items item, int proj = 0)
    => (ItemHelper.Get(item) as Gun).DefaultModule.projectiles[proj].hitEffects.tileMapHorizontal.effects[0].effects[0].effect;
  public static GameObject VerticalImpactVFX(this Items item, int proj = 0)
    => (ItemHelper.Get(item) as Gun).DefaultModule.projectiles[proj].hitEffects.tileMapVertical.effects[0].effects[0].effect;
  public static GameObject AirImpactVFX(this Items item, int proj = 0)
    => (ItemHelper.Get(item) as Gun).DefaultModule.projectiles[proj].hitEffects.overrideMidairDeathVFX;

  /// <summary>Append a string to all strings in a list</summary>
  public static List<string> AppendAll(this List<string> strings, string suffix)
  {
    List<string> newStrings = new();
    foreach (string s in strings)
      newStrings.Add(s+suffix);
    return newStrings;
  }

  /// <summary>Destroy a GameObject if it is non-null</summary>
  public static void SafeDestroy(this GameObject g)
  {
    if (g) UnityEngine.Object.Destroy(g);
  }

  /// <summary>Destroy a Component if it is non-null</summary>
  public static void SafeDestroy<T>(this T c) where T : Component
  {
    if (c) UnityEngine.Object.Destroy(c);
  }

  /// <summary>Select a pickup id from a weighted list</summary>
  public static int GetWeightedPickupID(this List<IntVector2> weights)
  {
    int targetWeight = UnityEngine.Random.Range(0, weights.Sum(item => item.y));
    foreach (IntVector2 weight in weights)
      if ((targetWeight -= weight.y) < 0)
        return weight.x;
    return 0;
  }

  /// <summary>Get the first element of a list if possible, returning null otherwise</summary>
  public static T SafeFirst<T>(this List<T> c)
  {
    return ((c?.Count ?? 0) == 0) ? default(T) : c[0];
  }

  /// <summary>Get the first element of a list if possible, returning null otherwise</summary>
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

  private static readonly SpeculativeRigidbody[] _NoRigidBodies = Enumerable.Empty<SpeculativeRigidbody>().ToArray();
  /// <summary>Determine whether a SpeculativeRigidBodsy is inside a wall</summary>
  public static bool InsideWall(this SpeculativeRigidbody body)
  {
    return PhysicsEngine.Instance.OverlapCast(
      rigidbody              : body,
      overlappingCollisions  : null,
      collideWithTiles       : true,
      collideWithRigidbodies : false,
      overrideCollisionMask  : null,
      ignoreCollisionMask    : null,
      collideWithTriggers    : false,
      overridePosition       : null,
      rigidbodyExcluder      : null,
      ignoreList             : _NoRigidBodies
      );
  }

  /// <summary>Move a SpeculativeRigidBody from start towards target in steps increments, stopping if we hit a wall. Returns true iff we reach our target without a wall collision.</summary>
  public static bool MoveTowardsTargetOrWall(this SpeculativeRigidbody body, Vector2 start, Vector2 target, int steps = 10)
  {
      Vector2 delta        = (target - start);
      Vector2 step         = delta / (float)steps;
      Vector2 lastCheckPos = start;
      tk2dSprite sprite = body.GetComponent<tk2dSprite>();
      for (int i = 0; i < steps; ++i)
      {
          Vector2 checkPos = start + (i * step);
          sprite.PlaceAtPositionByAnchor(checkPos, Anchor.MiddleCenter);
          body.Reinitialize();
          if (body.InsideWall())
          {
              body.gameObject.transform.position = lastCheckPos;
              body.Reinitialize();
              return false;
          }
          lastCheckPos = checkPos;
      }
      return true;
  }

  /// <summary>End a BehaviorSpeculator's stun and reset it to a new, potentially smaller value</summary>
  public static void ResetStun(this BehaviorSpeculator bs, float duration, bool createVFX = true)
  {
      bs.EndStun();
      bs.Stun(duration: duration, createVFX: createVFX);
  }

  /// <summary>Get the player's current damage multiplier</summary>
  public static float DamageMult(this PlayerController p) => p.stats.GetStatValue(PlayerStats.StatType.Damage);

  /// <summary>Get the player's current accuracy (spread) multiplier</summary>
  public static float AccuracyMult(this PlayerController p) => p.stats.GetStatValue(PlayerStats.StatType.Accuracy);

  /// <summary>returns true if sprite a overlaps sprite b in the world</summary>
  public static bool Overlaps(this tk2dBaseSprite a, tk2dBaseSprite b)
  {
      return IntVector2.AABBOverlap(
        posA        : a.sprite.WorldBottomLeft.ToIntVector2(),
        dimensionsA : (a.sprite.WorldTopRight - a.sprite.WorldBottomLeft).ToIntVector2(),
        posB        : b.sprite.WorldBottomLeft.ToIntVector2(),
        dimensionsB : (b.sprite.WorldTopRight - b.sprite.WorldBottomLeft).ToIntVector2()
        );
  }

  /// <summary>
  /// Given a floating point amount (e.g., 45.71), use the fractional component (e.g., .71) as the odds to return the ceiling of the
  /// amount (e.g., 48), returning the floor of the amount (e.g., 47) otherwise
  /// </summary>
  public static int RoundWeighted(this float amount)
  {
      return (UnityEngine.Random.value <= (amount - Math.Truncate(amount))
          ? Mathf.CeilToInt(amount)
          : Mathf.FloorToInt(amount));
  }

  /// <summary>
  /// Perform basic initialization of beam sprites for a projectile, override the beam controller's existing sprites if they exist
  /// </summary>
  public static BasicBeamController SetupBeamSprites(this Projectile projectile, string spriteName, int fps, Vector2 dims, Vector2? impactDims = null, int impactFps = -1)
  {
      // Fix breakage with GenerateBeamPrefab() expecting a non-null specrigidbody
      projectile.specRigidbody = projectile.gameObject.GetOrAddComponent<SpeculativeRigidbody>();

      // Unnecessary to delete these
      // UnityEngine.Object.Destroy(projectile.GetComponentInChildren<tk2dSpriteAnimation>());
      // UnityEngine.Object.Destroy(projectile.GetComponentInChildren<tk2dTiledSprite>());
      // UnityEngine.Object.Destroy(projectile.GetComponentInChildren<tk2dSpriteAnimator>());
      // UnityEngine.Object.Destroy(projectile.GetComponentInChildren<BasicBeamController>());

      // Compute beam offsets from middle-left of sprite
      Vector2 offsets = new Vector2(0, Mathf.Ceil(dims.y / 2f));
      // Compute impact offsets from true center of sprite
      Vector2? impactOffsets = impactDims.HasValue ? new Vector2(Mathf.Ceil(impactDims.Value.x / 2f), Mathf.Ceil(impactDims.Value.y / 2f)) : null;

      // Create the beam itself using our resource map lookup
      BasicBeamController beamComp = projectile.FixedGenerateBeamPrefab(
          spritePath                  : ResMap.Get($"{spriteName}_mid")[0],
          colliderDimensions          : dims,
          colliderOffsets             : offsets,
          beamAnimationPaths          : ResMap.Get($"{spriteName}_mid"),
          beamFPS                     : fps,
          //Impact
          impactVFXAnimationPaths     : ResMap.Get($"{spriteName}_impact", quietFailure: true),
          beamImpactFPS               : (impactFps > 0) ? impactFps : fps,
          impactVFXColliderDimensions : impactDims,
          impactVFXColliderOffsets    : impactOffsets,
          //End
          endVFXAnimationPaths        : ResMap.Get($"{spriteName}_end", quietFailure: true),
          beamEndFPS                  : fps,
          endVFXColliderDimensions    : dims,
          endVFXColliderOffsets       : offsets,
          //Beginning
          muzzleVFXAnimationPaths     : ResMap.Get($"{spriteName}_start", quietFailure: true),
          beamMuzzleFPS               : fps,
          muzzleVFXColliderDimensions : dims,
          muzzleVFXColliderOffsets    : offsets //,
          //Other Variables
          // glowAmount                  : 0f,
          // emissivecolouramt           : 0f
          );


      // fix some more animation glitches (don't consistently work, check and enable on a case by case basis)
      // beamComp.usesChargeDelay = false;
      // beamComp.muzzleAnimation = "beam_start;
      // beamComp.beamStartAnimation = null;
      // beamComp.chargeAnimation = null;
      // beamComp.rotateChargeAnimation = true;
      // projectile.shouldRotate = true;
      // projectile.shouldFlipVertically = true;

      return beamComp;
  }

  /// <summary>
  /// Get attach points for an animation clip
  /// </summary>
  public static tk2dSpriteDefinition.AttachPoint[] AttachPointsForClip(this Gun gun, string clipName)
  {
      Lazy._GunSpriteCollection ??= gun.sprite.collection; // need to initialize at least once
      tk2dSpriteAnimationClip clip = gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(clipName);
      if (clip == null)
          return null;
      int spriteid = clip.frames[0].spriteId;
      int attachIndex = Lazy._GunSpriteCollection.SpriteIDsWithAttachPoints.IndexOf(spriteid);
      if (attachIndex == -1)
      {
        ETGModConsole.Log($"failed to find attach points for {clipName} == {spriteid}");
        return null;
      }
      return Lazy._GunSpriteCollection.SpriteDefinedAttachPoints[attachIndex].attachPoints;
  }

  /// <summary>Instantiate a prefab with the specified position and rotation, optionally anchoring its sprite and clamping it to the pixel grid</summary>
  public static GameObject Instantiate(this GameObject original, Vector3? position = null, Quaternion? rotation = null, Anchor? anchor = null, bool quantize = true)
  {
    Vector3 pos = position ?? Vector3.zero;
    GameObject g = UnityEngine.Object.Instantiate(original: original, position: pos, rotation: rotation ?? Quaternion.identity);
    if (anchor.HasValue && g.GetComponent<tk2dBaseSprite>() is tk2dBaseSprite sprite)
    {
      sprite.PlaceAtPositionByAnchor(pos, anchor.Value);
      if (quantize)
        sprite.transform.position = sprite.transform.position.Quantize(0.0625f);
      g.GetComponent<SpeculativeRigidbody>()?.Reinitialize();
    }
    return g;
  }

  /// <summary>Pick and pause on a frame (random if frame == -1) from a tk2dSpriteAnimator</summary>
  public static void PickFrame(this tk2dSpriteAnimator animator, int frame = -1)
  {
    tk2dSpriteAnimationFrame[] frames = animator.currentClip.frames;
    // animator.deferNextStartClip = false;
    animator.SetSprite(
      spriteCollection: frames[0].spriteCollection,
      spriteId: (frame >= 0) ? frame : frames[UnityEngine.Random.Range(0, frames.Count())].spriteId);
    animator.Pause(); // stop animating immediately after creation so we can stick with our initial sprite
  }

  /// <summary>Pick and pause on a frame (random if frame == -1) from a gameObject's tk2dSpriteAnimator</summary>
  public static void PickFrame(this GameObject g, int frame = -1) => g.GetComponent<tk2dSpriteAnimator>().PickFrame(frame);

  /// <summary>Pick and pause on a frame (random if frame == -1) from a component's tk2dSpriteAnimator</summary>
  public static void PickFrame(this Component c, int frame = -1) => c.GetComponent<tk2dSpriteAnimator>().PickFrame(frame);

  /// <summary>Pick and pause on a frame (random if frame == -1) from a projectiles's tk2dSpriteAnimator</summary>
  /// <remarks>Potentially the same as the Projectile.SetFrame() extension</remarks>
  public static void PickFrame(this Projectile p, int frame = -1) => p.spriteAnimator.PickFrame(frame);

  /// <summary>Play a sound on a GameObject</summary>
  public static void Play(this GameObject g, string sound)
  {
    AkSoundEngine.PostEvent(sound, g);
  }

  /// <summary>Play a sound on a GameObject, stopping any instances of the sound on that object.</summary>
  public static void PlayOnce(this GameObject g, string sound)
  {
    AkSoundEngine.PostEvent(sound+"_stop", g);
    AkSoundEngine.PostEvent(sound, g);
  }

  /// <summary>Play a sound on a GameObject, stopping any instances of the sound globally.</summary>
  public static void PlayUnique(this GameObject g, string sound)
  {
    AkSoundEngine.PostEvent(sound+"_stop_all", g);
    AkSoundEngine.PostEvent(sound, g);
  }

  /// <summary>Returns a vector with the smaller component set to 0.</summary>
  public static Vector2 LargerComponent(this Vector2 v)  => (Mathf.Abs(v.x) > Mathf.Abs(v.y)) ? v.WithY(0) : v.WithX(0);
  /// <summary>Returns a vector with the larger component set to 0.</summary>
  public static Vector2 SmallerComponent(this Vector2 v) => (Mathf.Abs(v.x) < Mathf.Abs(v.y)) ? v.WithY(0) : v.WithX(0);

  /// <summary>Get the first matching gun in the Player's inventory</summary>
  public static GunType FindGun<GunType>(this PlayerController p) where GunType : MonoBehaviour
  {
    foreach (Gun gun in p.inventory.AllGuns)
      if (gun.GetComponent<GunType>() is GunType g)
        return g;
    return null;
  }

  /// <summary>Get the Gun behavior for the first matching gun in the Player's inventory</summary>
  public static Gun FindBaseGun<GunType>(this PlayerController p) where GunType : MonoBehaviour
  {
    foreach (Gun gun in p.inventory.AllGuns)
      if (gun.GetComponent<GunType>() is GunType g)
        return gun;
    return null;
  }

  /// <summary>Set the speed of a projectile and update the cached baseData.speed</summary>
  public static void SetSpeed(this Projectile p, float newSpeed)
  {
    p.baseData.speed = newSpeed;
    p.UpdateSpeed();
  }

  /// <summary>Multiply the speed of a projectile and update the cached baseData.speed</summary>
  public static void MultiplySpeed(this Projectile p, float factor)
  {
    p.baseData.speed *= factor;
    p.UpdateSpeed();
  }

  /// <summary>Accelerates a projectile and update the cached baseData.speed</summary>
  public static void Accelerate(this Projectile p, float accel)
  {
    p.baseData.speed += accel * p.LocalDeltaTime;
    p.UpdateSpeed();
  }

  /// <summary>Apply friction to a projectile and update the cached baseData.speed</summary>
  public static void ApplyFriction(this Projectile p, float friction)
  {
    p.baseData.speed *= (float)Lazy.FastPow(friction, p.LocalDeltaTime * C.FPS);
    p.UpdateSpeed();
  }

  /// <summary>Forces all ProjectileModules in a gun to reload with the DefaultModule</summary>
  public static void SynchronizeReloadAcrossAllModules(this Gun gun)
  {  //TODO: compare this to ChargeProjectile's DepleteAmmo() / IncrementModuleFireCountAndMarkReload()
      if (!gun.m_moduleData[gun.DefaultModule].needsReload)
        return;
      foreach (ProjectileModule mod in gun.Volley.projectiles)
          gun.m_moduleData[mod].needsReload = true;
  }

  /// <summary>Return a position near a position that bobs up and down relative to the scaled time since startup</summary>
  public static Vector2 HoverAt(this Vector2 pos, float amplitude = 1f, float frequency = 6.28f, float offset = 0.0f, float phase = 0.0f)
    => new Vector2(pos.x, pos.y + offset + amplitude * Mathf.Sin(phase + frequency * BraveTime.ScaledTimeSinceStartup));

  /// <summary>Return a position near a position that bobs up and down relative to the scaled time since startup (Vector3 version)</summary>
  public static Vector3 HoverAt(this Vector3 pos, float amplitude = 1f, float frequency = 6.28f, float offset = 0.0f, float phase = 0.0f)
    => pos.XY().HoverAt(amplitude: amplitude, frequency: frequency, offset: offset, phase: phase).ToVector3ZisY();

  /// <summary>Set up a SpeculativeRigidBody for a VFX sprite based on the sprite's dimensions, FlipX status, and Anchor</summary>
  public static SpeculativeRigidbody AutoRigidBody(this GameObject g, Anchor anchor, bool canBePushed = false)
  {
    SpeculativeRigidbody body = g.AddComponent<SpeculativeRigidbody>();

    tk2dBaseSprite sprite     = g.GetComponent<tk2dBaseSprite>();
    IntVector2 spriteSize     = (C.PIXELS_PER_TILE * sprite.GetBounds().size.XY()).ToIntVector2();
    IntVector2 rawOffsets     = tk2dSpriteGeomGen.GetAnchorOffset(anchor, spriteSize.x, spriteSize.y).ToIntVector2();
    IntVector2 spriteOffsets = rawOffsets - spriteSize;
    if (!sprite.FlipX)  // NOTE: VFX sprites are set up by default with a LowerCenter anchor, so these shenanigans are necessary to get the body right
      spriteOffsets = spriteOffsets.WithX(-rawOffsets.x);
    body.PixelColliders       = new List<PixelCollider>(){new(){
      ColliderGenerationMode = PixelCollider.PixelColliderGeneration.Manual,
      ManualOffsetX          = spriteOffsets.x,
      ManualOffsetY          = spriteOffsets.y,
      ManualWidth            = spriteSize.x,
      ManualHeight           = spriteSize.y,
      CollisionLayer         = CollisionLayer.HighObstacle,
      Enabled                = true,
      IsTrigger              = false,
    }};
    body.CanBePushed = canBePushed;

    return body;
  }

  /// <summary>Get the current charge level of a ProjectileModule. Returns -1 if no charged projectile is currently valid.</summary>
  public static int GetChargeLevel(this Gun gun, ProjectileModule mod = null)
  {
    mod ??= gun.DefaultModule;
    if (mod.chargeProjectiles == null)
      return -1;
    return mod.chargeProjectiles.IndexOf(mod.GetChargeProjectile(gun.m_moduleData[mod].chargeTime));
  }
}
