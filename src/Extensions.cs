namespace CwaffingTheGungy;

/// <summary>A centralized collection of generally-useful extensions methods</summary>
public static class Extensions
{
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
  public static GameObject RegisterPrefab(this GameObject self, bool deactivate = true, bool markFake = true, bool dontUnload = true, bool activate = false)
  {
    if (activate)
      self.gameObject.SetActive(true); //activate the object upon request
    else if (deactivate)
      self.gameObject.SetActive(false); //make sure we aren't an active game object
    if (markFake)
      FakePrefab.MarkAsFakePrefab(self.gameObject); //mark the object as a fake prefab
    if (dontUnload)
      UnityEngine.Object.DontDestroyOnLoad(self); //make sure the object isn't destroyed when loaded as a prefab
    return self;
  }

  /// <summary>Register a game object as a prefab, with generic support</summary>
  public static T RegisterPrefab<T>(this T self, bool deactivate = true, bool markFake = true, bool dontUnload = true) where T : Component
  {
    self.gameObject.RegisterPrefab(deactivate, markFake, dontUnload);
    return self;
  }

  /// <summary>Instantiate a prefab and clone it as a new prefab</summary>
  public static GameObject ClonePrefab(this GameObject self, bool deactivate = true, bool markFake = true, bool dontUnload = true)
  {
    return UnityEngine.Object.Instantiate(self).RegisterPrefab(deactivate, markFake, dontUnload).gameObject;
  }

  // WARNING: the old version of this method using Instantiate<T> (where T isn't GameObject) completely breaks if UnityEngine.CoreModule.MTGAPIPatcher.mm.dll is missing.
  // this should never happen if modded gungeon is installed properly, but has caused headache-inducing issues in the past, so be warned: https://github.com/pcrain/GungeonCraft/issues/8
  /// <summary>Instantiate a prefab and clone it as a new prefab, with generic support</summary>
  public static T ClonePrefab<T>(this T self, bool deactivate = true, bool markFake = true, bool dontUnload = true) where T : Component
  {
    return UnityEngine.Object.Instantiate(self.gameObject).RegisterPrefab(deactivate, markFake, dontUnload).GetComponent<T>();
  }

  /// <summary>Convert degrees to a Vector2 angle</summary>
  public static Vector2 ToVector(this float self, float magnitude = 1f)
  {
    return magnitude * (Vector2)(Quaternion.Euler(0f, 0f, self) * Vector2.right);
  }

  /// <summary>Convert degrees to a Vector3 angle</summary>
  public static Vector3 ToVector3(this float self, float magnitude = 1f)
  {
    return magnitude * (Quaternion.Euler(0f, 0f, self) * Vector3.right);
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

  /// <summary>Loop a gun's animation</summary>
  public static Gun LoopAnimation(this Gun gun, string animationName, int loopStart = 0)
  {
    gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(animationName).wrapMode = tk2dSpriteAnimationClip.WrapMode.LoopSection;
    gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(animationName).loopStart = loopStart;
    return gun;
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
  public static Projectile SetAirImpactVFX(this Projectile p, GameObject vfx, bool alwaysUseMidair = false)
  {
    p.hitEffects.suppressMidairDeathVfx = false;
    p.hitEffects.overrideMidairDeathVFX = vfx;
    p.hitEffects.alwaysUseMidair = alwaysUseMidair;
    return p;
  }

  /// <summary>Set a projectile's midair impact / death VFX</summary>
  public static Projectile SetAirImpactVFX(this Projectile p, VFXPool vfx, bool alwaysUseMidair = false)
  {
    p.hitEffects.suppressMidairDeathVfx = false;
    p.hitEffects.overrideMidairDeathVFX = vfx.effects[0].effects[0].effect;
    p.hitEffects.alwaysUseMidair = alwaysUseMidair;
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
    foreach (ProjectileModule mod in gun.Volley.projectiles)
      foreach (Projectile p in mod.projectiles)
        p.SetAllImpactVFX(vfx);
  }

  /// <summary>Copy a projectile's impact VFX from another projectile's across the board</summary>
  public static Projectile CopyAllImpactVFX(this Projectile p, Projectile otherP)
  {
    ProjectileImpactVFXPool v = p.hitEffects = new();
    ProjectileImpactVFXPool o = otherP.hitEffects;
    v.alwaysUseMidair               = o.alwaysUseMidair;
    v.tileMapHorizontal             = o.tileMapHorizontal;
    v.deathTileMapHorizontal        = o.deathTileMapHorizontal;
    v.tileMapVertical               = o.tileMapVertical;
    v.deathTileMapVertical          = o.deathTileMapVertical;
    v.enemy                         = o.enemy;
    v.deathEnemy                    = o.deathEnemy;
    v.suppressMidairDeathVfx        = o.suppressMidairDeathVfx;
    v.overrideMidairDeathVFX        = o.overrideMidairDeathVFX;
    v.midairInheritsRotation        = o.midairInheritsRotation;
    v.midairInheritsVelocity        = o.midairInheritsVelocity;
    v.midairInheritsFlip            = o.midairInheritsFlip;
    v.overrideMidairZHeight         = o.overrideMidairZHeight;
    v.overrideEarlyDeathVfx         = o.overrideEarlyDeathVfx;
    v.HasProjectileDeathVFX         = o.HasProjectileDeathVFX;
    v.CenterDeathVFXOnProjectile    = o.CenterDeathVFXOnProjectile;
    v.suppressHitEffectsIfOffscreen = o.suppressHitEffectsIfOffscreen;
    return p;
  }

  /// <summary>Copy a projectile's impact VFX across the board from another gun's default projectile (ItemHelper version)</summary>
  public static Projectile CopyAllImpactVFX(this Projectile p, Items other)
  {
    p.CopyAllImpactVFX((ItemHelper.Get(other) as Gun).DefaultModule.projectiles[0]);
    return p;
  }

  /// <summary>Check if an enemy is hostile</summary>
  public static bool IsHostile(this AIActor e, bool canBeDead = false, bool canBeNeutral = false)
  {
    if (!e)
      return false;
    HealthHaver h = e.healthHaver;
    return e && !e.IsGone && e.IsWorthShootingAt && (canBeNeutral || !e.IsHarmlessEnemy) && h && (canBeDead || h.IsAlive) && !h.isPlayerCharacter;
  }

  /// <summary>Check if an enemy is hostile and a non-boss</summary>
  public static bool IsHostileAndNotABoss(this AIActor e, bool canBeDead = false, bool canBeNeutral = false)
  {
    if (!e)
      return false;
    HealthHaver h = e.healthHaver;
    return e && !e.IsGone && e.IsWorthShootingAt && (canBeNeutral || !e.IsHarmlessEnemy) && h && !h.IsBoss && !h.IsSubboss &&  (canBeDead || h.IsAlive) && !h.isPlayerCharacter;
  }

  /// <summary>Check if an enemy is a boss</summary>
  public static bool IsABoss(this AIActor e, bool canBeDead = false)
  {
    if (!e)
      return false;
    HealthHaver h = e.healthHaver;
    return h && (h.IsBoss || h.IsSubboss) && (canBeDead || h.IsAlive);
  }

  /// <summary>Set the Alpha of a GameObject's sprite</summary>
  public static void SetAlpha(this GameObject g, float a)
  {
    if (g && g.GetComponent<Renderer>() is Renderer r)
      r.SetAlpha(a);
  }

  /// <summary>Set the Alpha of a Component's sprite (attached to the base component)</summary>
  public static void SetAlpha(this Component c, float a)
  {
    if (c && c.gameObject && c.gameObject.GetComponent<Renderer>() is Renderer r)
      r.SetAlpha(a);
  }

  /// <summary>Set the Alpha of a GameObject's sprite immediately and avoid the 1-frame opacity delay upon creation</summary>
  public static void SetAlphaImmediate(this GameObject g, float a)
  {
    if (!g || g.GetComponent<Renderer>() is not Renderer r)
      return;
    r.SetAlpha(a);
    if (g.GetComponent<tk2dSpriteAnimator>() is tk2dSpriteAnimator animator)
      animator.LateUpdate();
  }

  /// <summary>Set the Alpha of a Component's sprite immediately and avoid the 1-frame opacity delay upon creation</summary>
  public static void SetAlphaImmediate(this Component c, float a)
  {
    if (!c || !c.gameObject || c.gameObject.GetComponent<Renderer>() is not Renderer r)
      return;
    r.SetAlpha(a);
    if (c.gameObject.GetComponent<tk2dSpriteAnimator>() is tk2dSpriteAnimator animator)
      animator.LateUpdate();
  }

  /// <summary>Add emissiveness to a game object</summary>
  public static void SetGlowiness(this GameObject g, float a)
  {
    if (g.GetComponent<tk2dBaseSprite>() is tk2dBaseSprite sprite)
      sprite.SetGlowiness(a);
  }

  public static void SetGlowiness(this tk2dBaseSprite sprite, float glowAmount, Color? glowColor = null, Color? overrideColor = null, bool? clampBrightness = null)
  {
    sprite.usesOverrideMaterial = true;
    Material m = sprite.renderer.material;
    m.shader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTintableTiltedCutoutEmissive");
    if (clampBrightness.HasValue)
    {
      if (!clampBrightness.Value)
      {
        m.DisableKeyword("BRIGHTNESS_CLAMP_ON");
        m.EnableKeyword("BRIGHTNESS_CLAMP_OFF");
      }
      else
      {
        m.DisableKeyword("BRIGHTNESS_CLAMP_OFF");
        m.EnableKeyword("BRIGHTNESS_CLAMP_ON");
      }
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
  public static IEnumerable<PickupObject> AllItems(this PlayerController player)
  {
      if (!player)
          yield break;

      foreach(PickupObject item in player.passiveItems)
          yield return item;
      foreach(PickupObject item in player.activeItems)
          yield return item;
      foreach(PickupObject item in player.inventory.AllGuns)
          yield return item;
      yield break;
  }

  /// <summary>Get a passive item owned by the player</summary>
  public static bool HasPassive<T>(this PlayerController p) where T : PassiveItem
  {
    for (int i = 0; i < p.passiveItems.Count; ++i)
      if (p.passiveItems[i] is T)
        return true;
    return false;
  }

  /// <summary>Get a passive item owned by the player</summary>
  public static bool HasPassive(this PlayerController p, int id)
  {
    for (int i = 0; i < p.passiveItems.Count; ++i)
      if (p.passiveItems[i].PickupObjectId == id)
        return true;
    return false;
  }

  /// <summary>Get an active item owned by the player</summary>
  public static bool HasActive<T>(this PlayerController p) where T : PlayerItem
  {
    for (int i = 0; i < p.activeItems.Count; ++i)
      if (p.activeItems[i] is T)
        return true;
    return false;
  }

  /// <summary>Get an active item owned by the player</summary>
  public static bool HasActive(this PlayerController p, int id)
  {
    for (int i = 0; i < p.activeItems.Count; ++i)
      if (p.activeItems[i].PickupObjectId == id)
        return true;
    return false;
  }

  /// <summary>Get a gun owned by the player</summary>
  public static bool HasGun<T>(this PlayerController p) where T : Gun
  {
    for (int i = 0; i < p.inventory.AllGuns.Count; ++i)
      if (p.inventory.AllGuns[i] is T)
        return true;
    return false;
  }

  /// <summary>Get a gun owned by the player</summary>
  public static bool HasGun(this PlayerController p, int id)
  {
    for (int i = 0; i < p.inventory.AllGuns.Count; ++i)
      if (p.inventory.AllGuns[i].PickupObjectId == id)
        return true;
    return false;
  }

  /// <summary>Get a passive item owned by the player</summary>
  public static T GetPassive<T>(this PlayerController p) where T : PassiveItem
  {
    for (int i = 0; i < p.passiveItems.Count; ++i)
      if (p.passiveItems[i] is T t)
        return t;
    return null;
  }

  /// <summary>Get a passive item owned by the player by id</summary>
  public static PassiveItem GetPassive(this PlayerController p, int id)
  {
    for (int i = 0; i < p.passiveItems.Count; ++i)
      if (p.passiveItems[i].PickupObjectId == id)
        return p.passiveItems[i];
    return null;
  }

  /// <summary>Get an active item owned by the player</summary>
  public static T GetActive<T>(this PlayerController p) where T : PlayerItem
  {
    for (int i = 0; i < p.activeItems.Count; ++i)
      if (p.activeItems[i] is T t)
        return t;
    return null;
  }

  /// <summary>Get an active item owned by the player by id</summary>
  public static PlayerItem GetActive(this PlayerController p, int id)
  {
    for (int i = 0; i < p.activeItems.Count; ++i)
      if (p.activeItems[i].PickupObjectId == id)
        return p.activeItems[i];
    return null;
  }

  /// <summary>Get a gun owned by the player</summary>
  public static T GetGun<T>(this PlayerController p) where T : GunBehaviour
  {
    for (int i = 0; i < p.inventory.AllGuns.Count; ++i)
      if (p.inventory.AllGuns[i].gameObject.GetComponent<T>() is T t)
        return t;
    return null;
  }

  /// <summary>Get a gun owned by the player by id</summary>
  public static Gun GetGun(this PlayerController p, int id)
  {
    for (int i = 0; i < p.inventory.AllGuns.Count; ++i)
      if (p.inventory.AllGuns[i].PickupObjectId == id)
        return p.inventory.AllGuns[i];
    return null;
  }

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

  /// <summary>Fisher-Yates Shuffle: https://forum.unity.com/threads/clever-way-to-shuffle-a-list-t-in-one-line-of-c-code.241052/</summary>
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
      default:            return 0;
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
    foreach (AIActor enemy in room.SafeGetEnemiesInRoom())
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
  public static Gun ClearDefaultAudio(this Gun gun)
  {
    gun.gunSwitchGroup = Items.Banana.AsGun().gunSwitchGroup; // banana has silent reload and charge audio
    gun.PreventNormalFireAudio = true;
    gun.OverrideNormalFireAudioEvent = "";
    return gun;
  }

  /// <summary>Set an audio event for a specific frame of a gun's animation</summary>
  public static Gun SetGunAudio(this Gun gun, string name = null, string audio = "", int frame = 0)
  {
    tk2dSpriteAnimationFrame aframe = gun.spriteAnimator.GetClipByName(name).frames[frame];
    aframe.triggerEvent = !string.IsNullOrEmpty(audio);
    aframe.eventAudio = audio;
    return gun;
  }

  /// <summary>Set an audio event for several frames of a gun's animation</summary>
  public static Gun SetGunAudio(this Gun gun, string name = null, string audio = "", params int[] frames)
  {
    foreach (int frame in frames)
      gun.SetGunAudio(name: name, audio: audio, frame: frame);
    return gun;
  }

  /// <summary>needs to use Alexandria version because fireaudio overrides are not serialized</summary>
  public static Gun SetFireAudio(this Gun gun, string audio = "", int frame = 0)
  {
    return gun.SetGunAudio(name: gun.shootAnimation, audio: audio, frame: frame);
  }
  public static Gun SetFireAudio(this Gun gun, string audio = "", params int[] frames)
  {
    foreach (int frame in frames)
      gun.SetFireAudio(audio: audio, frame: frame);
    return gun;
  }
  public static Gun SetReloadAudio(this Gun gun, string audio = "", int frame = 0)
  {
    return gun.SetGunAudio(name: gun.reloadAnimation, audio: audio, frame: frame);
  }
  public static Gun SetReloadAudio(this Gun gun, string audio = "", params int[] frames)
  {
    foreach (int frame in frames)
      gun.SetGunAudio(name: gun.reloadAnimation, audio: audio, frame: frame);
    return gun;
  }
  public static Gun SetChargeAudio(this Gun gun, string audio = "", int frame = 0)
  {
    return gun.SetGunAudio(name: gun.chargeAnimation, audio: audio, frame: frame);
  }
  public static Gun SetChargeAudio(this Gun gun, string audio = "", params int[] frames)
  {
    foreach (int frame in frames)
      gun.SetGunAudio(name: gun.chargeAnimation, audio: audio, frame: frame);
    return gun;
  }
  public static Gun SetIdleAudio(this Gun gun, string audio = "", int frame = 0)
  {
    return gun.SetGunAudio(name: gun.idleAnimation, audio: audio, frame: frame);
  }
  public static Gun SetIdleAudio(this Gun gun, string audio = "", params int[] frames)
  {
    foreach (int frame in frames)
      gun.SetGunAudio(name: gun.idleAnimation, audio: audio, frame: frame);
    return gun;
  }
  public static Gun SetMuzzleVFX(this Gun gun, string resPath = null, float fps = 60, bool loops = false, float scale = 1.0f, Anchor anchor = Anchor.MiddleLeft, bool orphaned = false, float emissivePower = -1, bool continuous = false)
  {
    if (string.IsNullOrEmpty(resPath))
    {
      gun.muzzleFlashEffects = null; //.type = VFXPoolType.None;
      return gun;
    }

    gun.muzzleFlashEffects = VFX.CreatePool(resPath, fps: fps,
      loops: loops, scale: scale, anchor: anchor, alignment: VFXAlignment.Fixed, orphaned: orphaned, attached: true, emissivePower: emissivePower);
    gun.usesContinuousMuzzleFlash = continuous;
    return gun;
  }

  public static Gun SetMuzzleVFX(this Gun gun, Items gunToCopyFrom, bool onlyCopyBasicEffects = true)
  {
    Gun otherGun = gunToCopyFrom.AsGun();
    if (!otherGun)
      return gun;

    gun.muzzleFlashEffects = otherGun.muzzleFlashEffects;
    if (onlyCopyBasicEffects)
      return gun;

    gun.usesContinuousMuzzleFlash  = otherGun.usesContinuousMuzzleFlash;
    gun.finalMuzzleFlashEffects    = otherGun.finalMuzzleFlashEffects;
    gun.CriticalMuzzleFlashEffects = otherGun.CriticalMuzzleFlashEffects;
    return gun;
  }

  public static Gun SetCasing(this Gun gun, Items otherGun)
  {
    gun.shellCasing = otherGun.AsGun().shellCasing;
    return gun;
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
  public static Gun SetAttributes(this Gun gun, ItemQuality quality, GunClass gunClass, float reloadTime, int ammo,
    Items audioFrom = Items.Banana, bool defaultAudio = false, bool infiniteAmmo = false, bool canGainAmmo = true, bool canReloadNoMatterAmmo = false, bool? doesScreenShake = null,
    int? idleFps = null, int? shootFps = null, int? reloadFps = null, int? chargeFps = null, int? introFps = null, string fireAudio = null, string reloadAudio = null, string introAudio = null,
    int loopChargeAt = -1, int loopReloadAt = -1, int loopFireAt = -1, Items? muzzleFrom = null, bool modulesAreTiers = false, string muzzleVFX = null, int muzzleFps = 60,
    float muzzleScale = 1.0f, Anchor muzzleAnchor = Anchor.MiddleLeft, float muzzleEmission = -1f, IntVector2? carryOffset = null, bool preventRotation = false, float curse = 0f, bool continuousFire = false,
    bool dynamicBarrelOffsets = false, bool banFromBlessedRuns = false, bool rampUpFireRate = false, float rampUpFactor = 0f, bool suppressReloadAnim = false,
    GunHandedness handedness = GunHandedness.AutoDetect, bool autoPlay = true, bool attacksThroughWalls = false, bool suppressReloadLabel = false, float percentSpeedWhileCharging = 1.0f,
    bool onlyUsesIdleInWeaponBox = false, bool continuousFireAnimation = false, bool preventRollingWhenCharging = false, float percentSpeedWhileFiring = 1.0f, float smoothReload = -1f)
  {
    CwaffGun cg = gun.gameObject.GetComponent<CwaffGun>();

    gun.quality = quality;
    gun.reloadTime = reloadTime;
    gun.gunClass = gunClass;
    gun.SetBaseMaxAmmo(ammo);
    gun.CurrentAmmo = gun.GetBaseMaxAmmo(); // necessary iff gun basemaxammo > 1000

    gun.gunHandedness = handedness;
    gun.preventRotation = preventRotation;
    gun.gunSwitchGroup = audioFrom.AsGun().gunSwitchGroup;
    gun.InfiniteAmmo = infiniteAmmo;
    gun.CanGainAmmo = canGainAmmo;
    gun.CanReloadNoMatterAmmo = canReloadNoMatterAmmo;
    gun.Volley.ModulesAreTiers = modulesAreTiers;
    gun.GainsRateOfFireAsContinueAttack = rampUpFireRate;
    gun.CanAttackThroughObjects = attacksThroughWalls;
    gun.OnlyUsesIdleInWeaponBox = onlyUsesIdleInWeaponBox;
    if (rampUpFireRate)
      gun.RateOfFireMultiplierAdditionPerSecond = rampUpFactor;

    gun.doesScreenShake = doesScreenShake ?? gun.doesScreenShake;
    gun.spriteAnimator.playAutomatically = autoPlay;

    if (!defaultAudio)
      gun.ClearDefaultAudio();

    if (banFromBlessedRuns)
      gun.SetTag("exclude_blessed");

    if (continuousFire)
    {
      gun.usesContinuousFireAnimation = true;
      gun.LoopAnimation(gun.shootAnimation);
    }
    if (continuousFireAnimation)
      cg.continuousFireAnimation = true;
    if (smoothReload >= 0f) // smooth reload = synchronize reload animation speed with reload speed
    {
      cg.useSmoothReload = true;
      cg.smoothReloadOffset = smoothReload;
    }

    cg.percentSpeedWhileCharging = percentSpeedWhileCharging;
    cg.percentSpeedWhileFiring = percentSpeedWhileFiring;
    if (dynamicBarrelOffsets)
      CwaffGun.SetUpDynamicBarrelOffsets(gun);
    if (suppressReloadLabel)
      cg.suppressReloadLabel = true;
    if (preventRollingWhenCharging)
      cg.preventRollingWhenCharging = true;

    if (curse != 0f)
      gun.AddStatToGun(StatType.Curse.Add(curse));
    if (carryOffset.HasValue)
      gun.carryPixelOffset = carryOffset.Value;

    if (idleFps.HasValue)
      gun.SetAnimationFPS(gun.GetOriginalIdleAnimationName(), idleFps.Value);
    if (shootFps.HasValue)   gun.SetAnimationFPS(gun.shootAnimation, shootFps.Value);
    if (reloadFps is int reloadFpsValue)
    {
      if (reloadFpsValue == GunData.MATCH_ANIM)
        gun.SetAnimationFPS(gun.reloadAnimation, (int)(gun.spriteAnimator.GetClipByName(gun.reloadAnimation).frames.Length / gun.reloadTime));
      else
        gun.SetAnimationFPS(gun.reloadAnimation, reloadFpsValue);
    }
    if (chargeFps.HasValue)  gun.SetAnimationFPS(gun.chargeAnimation, chargeFps.Value);
    if (introFps.HasValue)   gun.SetAnimationFPS(gun.introAnimation, introFps.Value);
    if (fireAudio != null)   gun.SetFireAudio(fireAudio);  //NOTE: intentionally allowing empty strings here, just not null ones
    if (reloadAudio != null) gun.SetReloadAudio(reloadAudio);
    if (introAudio != null)  gun.SetGunAudio(gun.introAnimation, introAudio);
    if (loopChargeAt >= 0)   gun.LoopAnimation(gun.chargeAnimation, loopChargeAt);
    if (loopReloadAt >= 0)   gun.LoopAnimation(gun.reloadAnimation, loopReloadAt);
    if (loopFireAt >= 0)     gun.LoopAnimation(gun.shootAnimation, loopFireAt);

    if (muzzleFrom.HasValue)
      gun.SetMuzzleVFX(muzzleFrom.Value);
    else if (muzzleVFX == null)
      gun.SetMuzzleVFX();
    else
    {
      gun.SetMuzzleVFX(
        resPath       : muzzleVFX,
        fps           : muzzleFps,
        scale         : muzzleScale,
        anchor        : muzzleAnchor,
        emissivePower : muzzleEmission,
        loops         : false,
        orphaned      : false,
        continuous    : false
        );
    }

    if (suppressReloadAnim)
        gun.SuppressReloadAnimations();

    return gun;
  }

  /// <summary>Create a prefab trail, add it to a prefab projectile, and return the projectile.</summary>
  public static Projectile AttachTrail(this Projectile target, string spriteName, int fps = -1, string startAnim = null,
    float timeTillAnimStart = -1, float cascadeTimer = -1, float softMaxLength = -1, bool destroyOnEmpty = false, GameObject dispersalPrefab = null,
    Vector2? boneSpawnOffset = null)
  {
      CwaffTrailController trail = VFX.CreateSpriteTrailObject(
          spriteName         : spriteName,
          fps                : fps,
          startAnim          : startAnim,
          timeTillAnimStart  : timeTillAnimStart,
          cascadeTimer       : cascadeTimer,
          softMaxLength      : softMaxLength,
          destroyOnEmpty     : destroyOnEmpty,
          dispersalPrefab    : dispersalPrefab
          );
      trail.gameObject.SetActive(true); // parent projectile is deactivated, so we want to re-activate ourselves so we display correctly when the projectile becomes active
      trail.gameObject.transform.parent = target.transform;
      trail.boneSpawnOffset = boneSpawnOffset ?? Vector2.zero;
      return target;
  }

  /// <summary>Add an existing prefab trail to a BraveBehaviour instance and return the trail.</summary>
  public static CwaffTrailController AddTrail(this BraveBehaviour target, CwaffTrailController trail)
  {
      GameObject instantiatedTrail = UnityEngine.Object.Instantiate(trail.gameObject);
      instantiatedTrail.transform.parent = target.transform;
      return instantiatedTrail.GetComponent<CwaffTrailController>();
  }

  /// <summary>Set the rotation of a projectile manually</summary>
  public static void SetRotation(this Projectile p, float angle)
  {
    p.m_transform.eulerAngles = new Vector3(0f, 0f, angle);
  }

  /// <summary>Add a new animation to the same collection as a reference sprite</summary>
  public static string SetUpAnimation(this tk2dBaseSprite sprite, string animationName, float fps,
    tk2dSpriteAnimationClip.WrapMode wrapMode = tk2dSpriteAnimationClip.WrapMode.Once, bool copyMaterialSettings = false)
  {
    tk2dSpriteCollectionData collection = sprite.collection;
    tk2dSpriteDefinition referenceFrameDef = collection.spriteDefinitions[sprite.spriteId];
    tk2dSpriteAnimator anim = sprite.spriteAnimator;
    List<int> spriteIds = AtlasHelper.AddSpritesToCollection(ResMap.Get(animationName), collection).AsRange();
    if (copyMaterialSettings)
    {
      Material baseMat = referenceFrameDef.material;
      foreach (int fid in spriteIds)
        collection.spriteDefinitions[fid].CopyMaterialProps(baseMat);
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
    self.RefreshBehaviors();
  }

  /// <summary>Set up custom ammo types from default resource paths and adds it to a projectile module and returns the module.</summary>
  public static ProjectileModule SetupCustomAmmoClip(this ProjectileModule mod, GunData b)
  {
      mod.SetupCustomAmmoClip(b.gun.EncounterNameOrDisplayName.InternalName());
      return mod;
  }

  /// <summary>Set up custom ammo types from a specific resource path and adds it to a projectile module and returns the module.</summary>
  public static ProjectileModule SetupCustomAmmoClip(this ProjectileModule mod, string clipname)
  {
      mod.ammoType       = GameUIAmmoType.AmmoType.CUSTOM;
      mod.customAmmoType = AtlasHelper.GetOrAddCustomAmmoType($"{clipname}_clip", ResMap.Get($"{clipname}_clipfull")[0], ResMap.Get($"{clipname}_clipempty")[0]);
      return mod;
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
    if (!g || g.GetComponent<MeshRenderer>() is not MeshRenderer mr)
      return;
    Material[] sharedMaterials = mr.sharedMaterials;
    List<Material> list = new List<Material>();
    for (int i = 0; i < sharedMaterials.Length; i++)
    {
      if (sharedMaterials[i].shader != shader)
        list.Add(sharedMaterials[i]);
    }
    mr.sharedMaterials = list.ToArray();
  }

  /// <summary>Add a shader to a gameObject, and return the material for that shader</summary>
  public static Material GetOrAddShader(this GameObject g, Shader shader, bool atBeginning = true)
  {
    if (!g || g.GetComponent<MeshRenderer>() is not MeshRenderer component)
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


  /// <summary>Add a shader to a gameObject, and return the material for that shader</summary>
  public static Material AddShader(this Renderer renderer, Shader shader, bool atBeginning = true)
  {
    Material[] array = renderer.sharedMaterials;
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
    renderer.sharedMaterials = array;
    return material;
  }

  /// <summary>Check if a goop position is electrificed</summary>
  public static bool IsPositionElectrified(this DeadlyDeadlyGoopManager goopManager, Vector2 position)
  {
    IntVector2 key = (position / DeadlyDeadlyGoopManager.GOOP_GRID_SIZE).ToIntVector2(VectorConversions.Floor);
    if (goopManager.m_goopedCells.TryGetValue(key, out var value) && value.remainingLifespan > goopManager.goopDefinition.fadePeriod)
      return value.IsElectrified;
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
    _GunCollection ??= gun.sprite.collection;

    tk2dSpriteAnimationClip clip = gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(animationName);
    List<Vector3> offsets = new(clip.frames.Length);
    for (int i = 0; i < clip.frames.Length; ++i)
    {
        int attachIndex = _GunCollection.SpriteIDsWithAttachPoints.IndexOf(clip.frames[i].spriteId);
        foreach (tk2dSpriteDefinition.AttachPoint a in _GunCollection.SpriteDefinedAttachPoints[attachIndex].attachPoints)
            if (a.name == "Casing")
                offsets.Add(a.position);
    }

    return offsets;
  }

  /// <summary>Returns true if a projectile was fired from a gun without depleting ammo</summary>
  public static bool FiredForFreeOld(this Projectile proj, Gun gun, ProjectileModule mod)
  {
    if (proj.Owner is PlayerController pc && pc.InfiniteAmmo.Value)
      return true;
    return (mod.ammoCost == 0 || gun.InfiniteAmmo || gun.LocalInfiniteAmmo);
  }

  /// <summary>Add a component to an existing component's GameObject and return the component</summary>
  public static T AddComponent<T>(this Component component) where T : MonoBehaviour
  {
    return component.gameObject.AddComponent<T>();
  }

  /// <summary>Returns or adds a component to an existing component's GameObject and return the component</summary>
  public static T GetOrAddComponent<T>(this Component component) where T : MonoBehaviour
  {
    return (component.gameObject is not GameObject g) ? null : (g.GetComponent<T>() is not T t) ? g.AddComponent<T>() : t;
  }

  /// <summary>Get the internal name for a string</summary>
  public static string InternalName(this string s)
  {
    return s.Replace("-", "").Replace(".", "").Replace(":", "").Replace("'","").Replace(" ", "_").ToLower(); // keep in parity with SetupItem()
  }

  /// <summary>Get the internal sprite name for each gun (keep in parity with SetupItem())</summary>
  public static string InternalSpriteName(this Gun gun)
  {
    return gun.gunName.InternalName(); // keep in parity with SetupItem()
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
      if (!player || !shop || !shop.transform)
          return null;
      if (player.m_lastInteractionTarget is not IPlayerInteractable target)
          return null;
      foreach (Transform child in shop.transform)
      {
          if (!child || !child.gameObject)
              continue;
          if (child.gameObject.GetComponentsInChildren<CustomShopItemController>() is not CustomShopItemController[] shopItems)
              continue;
          if (shopItems.Length == 0)
              continue;
          if (shopItems[0] is not CustomShopItemController shopItem)
              continue;
          if (target.Equals(shopItem))
              return shopItem;
      }
      return null;
  }

  /// <summary>Pseudo-homing behavior</summary>
  public static Vector2 LerpDirectAndNaturalVelocity(this Vector2 position, Vector2 target, Vector2 naturalVelocity, float accel, float lerpFactor)
  {
      Vector2 towardsTarget = target - position;
      // Compute our natural velocity from accelerating towards our target
      Vector2 newNaturalVelocity = naturalVelocity + (accel * towardsTarget.normalized);
      // Compute a direct velocity from redirecting all of our momentum towards our target
      Vector2 newDirectVelocity = (naturalVelocity.magnitude + accel) * towardsTarget.normalized;
      // Take a weighted average
      return Lazy.SmoothestLerp(newDirectVelocity, newNaturalVelocity, lerpFactor);
  }

  /// <summary>Get Debris objects within a cone of vision from some reference position, optionally checking at most limit debris</summary>
  private static int _nextDebris = 0;
  public static IEnumerable<DebrisObject> DebrisWithinCone(this Vector2 start, float squareReach, float angle, float spread, int limit = -1, bool allowJunk = false)
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
            if (!allowJunk || !debris.IsPickupObject || (debris.GetComponent<PickupObject>().PickupObjectId != (int)Items.Junk))
              continue; // don't vacuum up important objects
          Vector2 debrisPos = debris.sprite ? debris.sprite.WorldCenter : debris.gameObject.transform.position.XY();
          Vector2 deltaVec = (debrisPos - start);
          if (deltaVec.sqrMagnitude > squareReach || !deltaVec.ToAngle().IsNearAngle(angle, spread))
              continue; // out of range
          yield return debris;
      }

      _nextDebris = last;
      yield break;
  }

  /// <summary>Get impact VFX from a specific Gun as a VFXPool</summary>
  public static VFXPool EnemyImpactPool(this Items item, int proj = 0)
    => item.AsGun().DefaultModule.projectiles[proj].hitEffects.enemy;
  public static VFXPool HorizontalImpactPool(this Items item, int proj = 0)
    => item.AsGun().DefaultModule.projectiles[proj].hitEffects.tileMapHorizontal;
  public static VFXPool VerticalImpactPool(this Items item, int proj = 0)
    => item.AsGun().DefaultModule.projectiles[proj].hitEffects.tileMapVertical;

  /// <summary>Get impact VFX from a specific Gun as a GameObject</summary>
  public static GameObject EnemyImpactVFX(this Items item, int proj = 0)
    => item.AsGun().DefaultModule.projectiles[proj].hitEffects.enemy.effects[0].effects[0].effect;
  public static GameObject HorizontalImpactVFX(this Items item, int proj = 0)
    => item.AsGun().DefaultModule.projectiles[proj].hitEffects.tileMapHorizontal.effects[0].effects[0].effect;
  public static GameObject VerticalImpactVFX(this Items item, int proj = 0)
    => item.AsGun().DefaultModule.projectiles[proj].hitEffects.tileMapVertical.effects[0].effects[0].effect;
  public static GameObject AirImpactVFX(this Items item, int proj = 0)
    => item.AsGun().DefaultModule.projectiles[proj].hitEffects.overrideMidairDeathVFX;

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

  /// <summary>Destroy all GameObjects in a less</summary>
  public static void SafeDestroyAll(this List<GameObject> objects)
  {
    for (int i = 0; i < objects.Count; ++i)
      objects[i].SafeDestroy();
    objects.Clear();
  }

  /// <summary>Select an item from a weighted list of (index, weight) pairs</summary>
  public static int WeightedRandom(this List<IntVector2> weights)
  {
    int targetWeight = UnityEngine.Random.Range(0, weights.Sum(item => item.y));
    foreach (IntVector2 weight in weights)
      if ((targetWeight -= weight.y) < 0)
        return weight.x;
    return 0;
  }

  /// <summary>Select an item from a weighted list of (index, weight) pairs</summary>
  public static int WeightedRandom(this List<Vector2> weights)
  {
    float targetWeight = UnityEngine.Random.Range(0, weights.Sum(item => item.y));
    foreach (Vector2 weight in weights)
      if ((targetWeight -= weight.y) < 0)
        return (int)weight.x;
    return 0;
  }

  /// <summary>Get the first element of a list if possible, returning null otherwise</summary>
  public static T SafeFirst<T>(this List<T> c)
  {
    return (c == null || c.Count == 0) ? default(T) : c[0];
  }

  /// <summary>Get the first element of a list if possible, returning null otherwise</summary>
  public static Projectile FirstValidChargeProjectile(this ProjectileModule mod)
  {
    List<ChargeProjectile> c = mod.chargeProjectiles;
    if (c == null)
      return null;
    foreach (ChargeProjectile cp in c)
    {
      if (cp.Projectile is Projectile p)
        return p;
    }
    return null;
  }

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
      ignoreList             : null
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
  public static float DamageMult(this PlayerController p) => p.stats.GetStatValue(StatType.Damage);

  /// <summary>Get the player's current accuracy (spread) multiplier</summary>
  public static float AccuracyMult(this PlayerController p) => p.stats.GetStatValue(StatType.Accuracy);

  /// <summary>Get the player's current projectile speed multiplier</summary>
  public static float ProjSpeedMult(this PlayerController p) => p.stats.GetStatValue(StatType.ProjectileSpeed);

  /// <summary>Get the player's current projectile knockback (force) multiplier</summary>
  public static float KnockbackMult(this PlayerController p) => p.stats.GetStatValue(StatType.KnockbackMultiplier);

  /// <summary>Get the player's current projectile range multiplier</summary>
  public static float RangeMult(this PlayerController p) => p.stats.GetStatValue(StatType.RangeMultiplier);

  /// <summary>Get the player's current gun charge rate multiplier</summary>
  public static float ChargeMult(this PlayerController p) => p.stats.GetStatValue(StatType.ChargeAmountMultiplier);

  /// <summary>Get the player's current curse level</summary>
  public static float Curse(this PlayerController p) => p.stats.GetStatValue(StatType.Curse);

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
  public static BasicBeamController SetupBeamSprites(this Projectile projectile, string spriteName, int fps, int impactFps = -1,
    int endFps = -1, int startFps = -1, int chargeFps = -1, int dissipateFps = -1, bool loopCharge = true)
  {
      // Fix breakage with GenerateBeamPrefab() expecting a non-null specrigidbody (no longer necessary with FixedGenerateBeamPrefab())
      // projectile.specRigidbody = projectile.gameObject.GetOrAddComponent<SpeculativeRigidbody>();

      // Unnecessary to delete these
      // UnityEngine.Object.Destroy(projectile.GetComponentInChildren<tk2dSpriteAnimation>());
      // UnityEngine.Object.Destroy(projectile.GetComponentInChildren<tk2dTiledSprite>());
      // UnityEngine.Object.Destroy(projectile.GetComponentInChildren<tk2dSpriteAnimator>());
      // UnityEngine.Object.Destroy(projectile.GetComponentInChildren<BasicBeamController>());

      // Create the beam itself using our resource map lookup
      //WARN: beam sprites must use the entire width / height of the canvas since bounding boxes are determined using trimmed bounds
      BasicBeamController beamComp = projectile.FixedGenerateBeamPrefab(
          beamAnimationPaths          : ResMap.Get($"{spriteName}_mid"),
          beamFPS                     : fps,
          //Impact
          impactVFXAnimationPaths     : ResMap.Get($"{spriteName}_impact", quietFailure: true),
          beamImpactFPS               : (impactFps > 0) ? impactFps : fps,
          //End
          endVFXAnimationPaths        : ResMap.Get($"{spriteName}_end", quietFailure: true),
          beamEndFPS                  : (endFps > 0) ? endFps : fps,
          //Beginning
          startVFXAnimationPaths      : ResMap.Get($"{spriteName}_start", quietFailure: true),
          beamStartFPS                : (startFps > 0) ? startFps : fps,
          //Charge
          chargeVFXAnimationPaths     : ResMap.Get($"{spriteName}_charge", quietFailure: true),
          beamChargeFPS               : (chargeFps > 0) ? chargeFps : fps,
          loopCharge                  : loopCharge,
          //Dissipate
          beamDissipateAnimationPaths : ResMap.Get($"{spriteName}_dissipate", quietFailure: true),
          beamDissipateFPS            : (dissipateFps > 0) ? dissipateFps : fps
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
  public static tk2dSpriteDefinition.AttachPoint[] AttachPointsForClip(this Gun gun, string clipName, int frame = 0)
  {
      Lazy._GunSpriteCollection ??= gun.sprite.collection; // need to initialize at least once
      tk2dSpriteAnimationClip clip = gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(clipName);
      if (clip == null)
          return null;
      int spriteid = clip.frames[frame].spriteId;
      int attachIndex = Lazy._GunSpriteCollection.SpriteIDsWithAttachPoints.IndexOf(spriteid);
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
      if (g.GetComponent<SpeculativeRigidbody>() is SpeculativeRigidbody body)
        body.Reinitialize();
    }
    return g;
  }

  /// <summary>Pick and pause on a frame (random if frame == -1) from a tk2dSpriteAnimator</summary>
  public static void PickFrame(this tk2dSpriteAnimator animator, int frame = -1)
  {
    tk2dSpriteAnimationFrame[] frames = (animator.currentClip ?? animator.DefaultClip).frames;
    // animator.deferNextStartClip = false;
    animator.playAutomatically = false; //NOTE: necessary so pooled CwaffVFX don't always switch back to the first frame when newly-generated
    animator.SetSprite(
      spriteCollection: frames[0].spriteCollection,
      spriteId: frames[(frame >= 0) ? frame : UnityEngine.Random.Range(0, frames.Length)].spriteId);
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
    //WARNING: can't use FastPow here as it isn't accurate enough (e.g., slows down RC Launcher projectiles at higher frame rates)
    // p.baseData.speed *= (float)Lazy.FastPow(friction, p.LocalDeltaTime * C.FPS);
    p.baseData.speed *= Mathf.Pow(friction, p.LocalDeltaTime * C.FPS);
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

  //WARNING: only works with our VFX and sprites, can't be used with basegame sprites or results in wonky hitboxes
  /// <summary>Set up a SpeculativeRigidBody for a VFX sprite based on the sprite's dimensions, FlipX status, and Anchor</summary>
  public static SpeculativeRigidbody AutoRigidBody(this GameObject g, Anchor anchor, CollisionLayer clayer = CollisionLayer.HighObstacle, bool canBePushed = false)
  {
    SpeculativeRigidbody body = g.GetOrAddComponent<SpeculativeRigidbody>();

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
      CollisionLayer         = clayer,
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

  /// <summary>Faster version of the MtG API equivalent using our ResMap()</summary>
  public static void QuickUpdateGunAnimations(this Gun gun)
  {
      tk2dSpriteCollectionData collection = ETGMod.Databases.Items.WeaponCollection;

      var clips = new List<tk2dSpriteAnimationClip>();

      gun.idleAnimation = gun.QuickUpdateAnimationAddClipsLater("idle", collection, clipsToAddLater: clips);
      gun.dodgeAnimation = gun.QuickUpdateAnimationAddClipsLater("dodge", collection, clipsToAddLater: clips);
      gun.introAnimation = gun.QuickUpdateAnimationAddClipsLater("intro", collection, true, clipsToAddLater: clips);
      gun.emptyAnimation = gun.QuickUpdateAnimationAddClipsLater("empty", collection, clipsToAddLater: clips);
      gun.shootAnimation = gun.QuickUpdateAnimationAddClipsLater("fire", collection, true, clipsToAddLater: clips);
      gun.reloadAnimation = gun.QuickUpdateAnimationAddClipsLater("reload", collection, true, clipsToAddLater: clips);
      gun.chargeAnimation = gun.QuickUpdateAnimationAddClipsLater("charge", collection, clipsToAddLater: clips);
      gun.outOfAmmoAnimation = gun.QuickUpdateAnimationAddClipsLater("out_of_ammo", collection, clipsToAddLater: clips);
      gun.dischargeAnimation = gun.QuickUpdateAnimationAddClipsLater("discharge", collection, clipsToAddLater: clips);
      gun.finalShootAnimation = gun.QuickUpdateAnimationAddClipsLater("final_fire", collection, true, clipsToAddLater: clips);
      gun.emptyReloadAnimation = gun.QuickUpdateAnimationAddClipsLater("empty_reload", collection, true, clipsToAddLater: clips);
      gun.criticalFireAnimation = gun.QuickUpdateAnimationAddClipsLater("critical_fire", collection, true, clipsToAddLater: clips);
      gun.enemyPreFireAnimation = gun.QuickUpdateAnimationAddClipsLater("enemy_pre_fire", collection, clipsToAddLater: clips);
      gun.alternateShootAnimation = gun.QuickUpdateAnimationAddClipsLater("alternate_shoot", collection, true, clipsToAddLater: clips);
      gun.alternateReloadAnimation = gun.QuickUpdateAnimationAddClipsLater("alternate_reload", collection, true, clipsToAddLater: clips);
      gun.alternateIdleAnimation = gun.QuickUpdateAnimationAddClipsLater("alternate_idle", collection, clipsToAddLater: clips);

      if(clips.Count == 0)
        return;

      // default sprite should be the first frame of the idle animation
      gun.DefaultSpriteID = clips[0].frames[0].spriteId;
      gun.GetSprite().SetSprite(collection, gun.DefaultSpriteID);

      Array.Resize(ref gun.spriteAnimator.Library.clips, gun.spriteAnimator.Library.clips.Length + clips.Count);
      for(int i = 0; i < clips.Count; i++)
          gun.spriteAnimator.Library.clips[gun.spriteAnimator.Library.clips.Length - clips.Count + i] = clips[i];
  }

  /// <summary>Faster version of the MtG API equivalent using our ResMap()</summary>
  public static string QuickUpdateAnimationAddClipsLater(this Gun gun, string name, tk2dSpriteCollectionData collection, bool returnToIdle = false, List<tk2dSpriteAnimationClip> clipsToAddLater = null)
  {
      string clipName = gun.name + "_" + name;
      if (ResMap.Get(clipName, true) is not List<string> frameNames)
        return null;

      tk2dSpriteAnimationClip clip = new(){
        name     = clipName,
        fps      = 15,
        wrapMode = returnToIdle ? tk2dSpriteAnimationClip.WrapMode.Once : default,
        frames   = collection.CreateAnimationFrames(frameNames),
      };

      if(clipsToAddLater == null)
      {
          Array.Resize(ref gun.spriteAnimator.Library.clips, gun.spriteAnimator.Library.clips.Length + 1);
          gun.spriteAnimator.Library.clips[gun.spriteAnimator.Library.clips.Length - 1] = clip;
      }
      else
          clipsToAddLater.Add(clip);

      return clipName;
  }

  /// <summary>Convert a list of resource names into a set of animation frames</summary>
  public static tk2dSpriteAnimationFrame[] CreateAnimationFrames(this tk2dSpriteCollectionData collection, List<string> frameNames)
  {
    tk2dSpriteAnimationFrame[] frames = new tk2dSpriteAnimationFrame[frameNames.Count];
    for (int i = 0; i < frames.Length; ++i)
      frames[i] = new(){
        spriteCollection = collection,
        spriteId         = collection.spriteNameLookupDict[frameNames[i]],
      };
    return frames;
  }

  /// <summary>Faster version of the MtG API equivalent using our ResMap()</summary>
  public static string QuickUpdateGunAnimation(this Gun gun, string name, tk2dSpriteCollectionData collection = null, bool returnToIdle = false, int fps = -1)
  {
      collection ??= ETGMod.Databases.Items.WeaponCollection;
      string clipName = QuickUpdateAnimationAddClipsLater(gun, name, collection, returnToIdle);
      if (fps >= 0)
        gun.SetAnimationFPS(clipName, fps);
      return clipName;
  }

  /// <summary>Fixed version fo Alexandria's ConstructOffsetsFromAnchor() to deal with atlas sprites correctly</summary>
  public static void BetterConstructOffsetsFromAnchor(this tk2dSpriteDefinition def, tk2dBaseSprite.Anchor anchor, Vector2? scale = null, bool fixesScale = false, bool changesCollider = true)
  {
      Vector2 scaling = scale ?? def.untrimmedBoundsDataExtents.XY();
      if (fixesScale)
          scaling -= def.position0.XY();

      float xOffset = 0;
      if (anchor == tk2dBaseSprite.Anchor.LowerCenter || anchor == tk2dBaseSprite.Anchor.MiddleCenter || anchor == tk2dBaseSprite.Anchor.UpperCenter)
          xOffset = -(0.5f * scaling.x);
      else if (anchor == tk2dBaseSprite.Anchor.LowerRight || anchor == tk2dBaseSprite.Anchor.MiddleRight || anchor == tk2dBaseSprite.Anchor.UpperRight)
          xOffset = -scaling.x;
      float yOffset = 0;
      if (anchor == tk2dBaseSprite.Anchor.MiddleLeft || anchor == tk2dBaseSprite.Anchor.MiddleCenter || anchor == tk2dBaseSprite.Anchor.MiddleLeft)
          yOffset = -(0.5f * scaling.y);
      else if (anchor == tk2dBaseSprite.Anchor.UpperLeft || anchor == tk2dBaseSprite.Anchor.UpperCenter || anchor == tk2dBaseSprite.Anchor.UpperRight)
          yOffset = -scaling.y;
      def.ShiftBy(new Vector3(xOffset, yOffset, 0f), false);
      if (changesCollider && def.colliderVertices != null && def.colliderVertices.Length > 0)
      {
          float colliderXOffset = 0;
          if (anchor == tk2dBaseSprite.Anchor.LowerLeft || anchor == tk2dBaseSprite.Anchor.MiddleLeft || anchor == tk2dBaseSprite.Anchor.UpperLeft)
              colliderXOffset = (scaling.x / 2f);
          else if (anchor == tk2dBaseSprite.Anchor.LowerRight || anchor == tk2dBaseSprite.Anchor.MiddleRight || anchor == tk2dBaseSprite.Anchor.UpperRight)
              colliderXOffset = -(scaling.x / 2f);
          float colliderYOffset = 0;
          if (anchor == tk2dBaseSprite.Anchor.LowerLeft || anchor == tk2dBaseSprite.Anchor.LowerCenter || anchor == tk2dBaseSprite.Anchor.LowerRight)
              colliderYOffset = (scaling.y / 2f);
          else if (anchor == tk2dBaseSprite.Anchor.UpperLeft || anchor == tk2dBaseSprite.Anchor.UpperCenter || anchor == tk2dBaseSprite.Anchor.UpperRight)
              colliderYOffset = -(scaling.y / 2f);
          def.colliderVertices[0] += new Vector3(colliderXOffset, colliderYOffset, 0);
      }
  }

  /// <summary>Scale a sprite definition by a factor.</summary>
  public static void ScaleBy(this tk2dSpriteDefinition def, float scale)
  {
    def.position0                  *= scale;
    def.position1                  *= scale;
    def.position2                  *= scale;
    def.position3                  *= scale;
    def.boundsDataCenter           *= scale;
    def.boundsDataExtents          *= scale;
    def.untrimmedBoundsDataCenter  *= scale;
    def.untrimmedBoundsDataExtents *= scale;
  }


  /// <summary>Shift a sprite definition by an offset.</summary>
  public static void ShiftBy(this tk2dSpriteDefinition def, Vector3 offset, bool changesCollider = false)
  {
      def.position0                  += offset;
      def.position1                  += offset;
      def.position2                  += offset;
      def.position3                  += offset;
      def.boundsDataCenter           += offset;
      // def.boundsDataExtents          += offset;
      def.untrimmedBoundsDataCenter  += offset;
      // def.untrimmedBoundsDataExtents += offset;
      if (def.colliderVertices != null && def.colliderVertices.Length > 0 && changesCollider)
          def.colliderVertices[0] += offset;
  }

  /// <summary>Gives a list of vec.y consecutive ints starting from vec.x</summary>
  public static List<int> AsRange(this IntVector2 vec)
  {
    int x = vec.x;
    List<int> ints = new(vec.y);
    for (int i = 0; i < vec.y; ++i)
      ints.Add(x++);
    return ints;
  }

  /// <summary>Disable a projectile's collision with players</summary>
  public static void StopCollidingWithPlayers(this Projectile p)
  {
      p.collidesWithPlayer = false;  // doesn't actually do anything directly, but semantically it's nice to set this
      foreach (PixelCollider pc in p.specRigidbody.PixelColliders) // actually does the heavy lifting
          pc.CollisionLayerIgnoreOverride |= CollisionMask.LayerToMask(CollisionLayer.PlayerHitBox);
  }

  /// <summary>Spawns an enemy in and skips the awakening animation + all other startup behaviors</summary>
  public static void SpawnInInstantly(this AIActor enemy)
  {
    enemy.HasDonePlayerEnterCheck = true;
    enemy.IsInReinforcementLayer = true;
    enemy.ToggleRenderers(true);
    enemy.OnEngaged(true);
    enemy.aiAnimator.EndAnimation();
  }

  /// <summary>Add a holographic shader to a sprite</summary>
  public static void MakeHolographic(this tk2dBaseSprite sprite, bool green = false)
  {
    sprite.usesOverrideMaterial = true;
    sprite.renderer.material.shader = ShaderCache.Acquire("Brave/Internal/HologramShader");
    if (green)
      sprite.renderer.material.SetFloat("_IsGreen", 1f);
  }

  /// <summary>Add a flipped carry offset to a gun</summary>
  public static Gun AddFlippedCarryPixelOffsets(this Gun gun, IntVector2 offset, IntVector2 flippedOffset,
      IntVector2? offsetPilot       = null, IntVector2? flippedOffsetPilot       = null,
      IntVector2? offsetConvict     = null, IntVector2? flippedOffsetConvict     = null,
      IntVector2? offsetRobot       = null, IntVector2? flippedOffsetRobot       = null,
      IntVector2? offsetNinja       = null, IntVector2? flippedOffsetNinja       = null,
      IntVector2? offsetCosmonaut   = null, IntVector2? flippedOffsetCosmonaut   = null,
      IntVector2? offsetSoldier     = null, IntVector2? flippedOffsetSoldier     = null,
      IntVector2? offsetGuide       = null, IntVector2? flippedOffsetGuide       = null,
      IntVector2? offsetCoopCultist = null, IntVector2? flippedOffsetCoopCultist = null,
      IntVector2? offsetBullet      = null, IntVector2? flippedOffsetBullet      = null,
      IntVector2? offsetEevee       = null, IntVector2? flippedOffsetEevee       = null,
      IntVector2? offsetGunslinger  = null, IntVector2? flippedOffsetGunslinger  = null)
  {
    gun.carryPixelOffset = offset;
    FlippedCarryPixelOffset.AddTo(gun: gun, offset: offset, flippedOffset: flippedOffset,
        offsetPilot:       offsetPilot,       flippedOffsetPilot:       flippedOffsetPilot,
        offsetConvict:     offsetConvict,     flippedOffsetConvict:     flippedOffsetConvict,
        offsetRobot:       offsetRobot,       flippedOffsetRobot:       flippedOffsetRobot,
        offsetNinja:       offsetNinja,       flippedOffsetNinja:       flippedOffsetNinja,
        offsetCosmonaut:   offsetCosmonaut,   flippedOffsetCosmonaut:   flippedOffsetCosmonaut,
        offsetSoldier:     offsetSoldier,     flippedOffsetSoldier:     flippedOffsetSoldier,
        offsetGuide:       offsetGuide,       flippedOffsetGuide:       flippedOffsetGuide,
        offsetCoopCultist: offsetCoopCultist, flippedOffsetCoopCultist: flippedOffsetCoopCultist,
        offsetBullet:      offsetBullet,      flippedOffsetBullet:      flippedOffsetBullet,
        offsetEevee:       offsetEevee,       flippedOffsetEevee:       flippedOffsetEevee,
        offsetGunslinger:  offsetGunslinger,  flippedOffsetGunslinger:  flippedOffsetGunslinger
      );
    return gun;
  }

  private static bool _SuppressNextPassivePickupSound = false;
  /// <summary>Patch to make secret items not play the "Play_OBJ_passive_get_01" sound when picked up</summary>
  [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.AcquirePassiveItem))]
  private class PlayerControllerAcquirePassiveItemPatch
  {
      [HarmonyILManipulator]
      private static void PlayerControllerAcquirePassiveItemIL(ILContext il)
      {
          ILCursor cursor = new ILCursor(il);
          if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdstr("Play_OBJ_passive_get_01")))
            return;
          cursor.CallPrivate(typeof(PlayerControllerAcquirePassiveItemPatch), nameof(AdjustPickupSound));
      }

      private static string AdjustPickupSound(string oldSound)
      {
        if (!_SuppressNextPassivePickupSound)
          return oldSound;
        _SuppressNextPassivePickupSound = false;
        return string.Empty;
      }
  }

  /// <summary>Acquire a fake item and put it in a player's inventory (generic version)</summary>
  public static T AcquireFakeItem<T>(this PlayerController player) where T : FakeItem
  {
    GameObject gameObject = UnityEngine.Object.Instantiate(FakeItem.Get<T>().gameObject);
    T fakePassive = gameObject.GetComponent<T>();
    EncounterTrackable trackable = fakePassive.GetComponent<EncounterTrackable>();
    if (trackable)
      trackable.DoNotificationOnEncounter = false;
    fakePassive.suppressPickupVFX = true;
    _SuppressNextPassivePickupSound = true;
    fakePassive.Pickup(player);
    return fakePassive;
  }

  /// <summary>Acquire a fake item and put it in a player's inventory (id version)</summary>
  public static FakeItem AcquireFakeItem(this PlayerController player, int id)
  {
    GameObject gameObject = UnityEngine.Object.Instantiate(FakeItem.Get(id).gameObject);
    FakeItem fakePassive = gameObject.GetComponent<FakeItem>();
    EncounterTrackable trackable = fakePassive.GetComponent<EncounterTrackable>();
    if (trackable)
      trackable.DoNotificationOnEncounter = false;
    fakePassive.suppressPickupVFX = true;
    _SuppressNextPassivePickupSound = true;
    fakePassive.Pickup(player);
    return fakePassive;
  }

  /// <summary>Acquire a passive item and put it in a player's inventory without notification or sound (id version)</summary>
  public static void AcquireSilently(this PlayerController player, int id)
  {
    _SuppressNextPassivePickupSound = true;
    player.AcquirePassiveItemPrefabDirectly(PickupObjectDatabase.GetById(id) as PassiveItem);
  }

  /// <summary>Spawn shrapnel from a projectile</summary>
  public static void SpawnShrapnel(this Projectile p, GameObject shrapnelVFX, int shrapnelCount = 10, float shrapnelMinVelocity = 20f, float shrapnelMaxVelocity = 25f, float shrapnelLifetime = 0.2f)
  {
      tk2dSpriteDefinition def = p.sprite.GetCurrentSpriteDef();
      Vector2 spriteSize       = def.position3 - def.position0;
      CwaffVFX.SpawnBurst(
          prefab           : shrapnelVFX,
          numToSpawn       : shrapnelCount,
          basePosition     : p.transform.position,
          positionVariance : 0.5f * Mathf.Min(spriteSize.x, spriteSize.y),
          minVelocity      : shrapnelMinVelocity,
          velocityVariance : shrapnelMaxVelocity - shrapnelMinVelocity,
          velType          : CwaffVFX.Vel.Away,
          rotType          : CwaffVFX.Rot.Random,
          lifetime         : shrapnelLifetime,
          fadeOutTime      : shrapnelLifetime
        );
  }

  /// <summary>Retrieves a field from within an enumerator</summary>
  private static Regex rx_enum_field = new Regex(@"^<?([^>]+)(>__[0-9]+)?$", RegexOptions.Compiled);
  public static FieldInfo GetEnumeratorField(this Type t, string s)
  {
      return AccessTools.GetDeclaredFields(t).Find(f => {
          // ETGModConsole.Log($"{f.Name}");
          foreach (Match match in rx_enum_field.Matches(f.Name))
          {
            // ETGModConsole.Log($"  {match.Groups[1].Value}");
            if (match.Groups[1].Value == s)
              return true;
          }
          return false;
      });
  }

  public static string GetEnumeratorFieldName(this Type t, string s)
  {
      return t.GetEnumeratorField(s).Name;
  }

  /// <summary>Yoinked from Spapi, need to refactor later</summary>
  // public static void EnumeratorSetField(this object obj, string name, object value) => obj.GetType().EnumeratorField(name).SetValue(obj, value);

  /// <summary>Declare a local variable in an ILManipulator</summary>
  public static VariableDefinition DeclareLocal<T>(this ILContext il)
  {
      VariableDefinition v = new VariableDefinition(il.Import(typeof(T)));
      il.Body.Variables.Add(v);
      return v;
  }

  /// <summary>Add an animation event to the tk2dSpriteAnimator of a GameObject</summary>
  public static void AddAnimationEvent(this GameObject g, Action<tk2dSpriteAnimator, tk2dSpriteAnimationClip, int> action, int frame, string sound)
  {
      if (g.GetComponent<tk2dSpriteAnimator>() is not tk2dSpriteAnimator anim)
      {
        Lazy.DebugWarn("Trying to play animation event on nonexistent animator");
        return;
      }
      if (action != null)
        anim.AnimationEventTriggered += action;
      tk2dSpriteAnimationFrame f = anim.DefaultClip.frames[frame];
        f.triggerEvent = true;
        f.eventAudio   = sound;
  }

  /// <summary>Add a sound effect to a frame of animation</summary>
  public static void AddSound(this tk2dSpriteAnimationFrame f, string sound)
  {
      f.triggerEvent = true;
      f.eventAudio   = sound;
  }

  /// <summary>Increases a player's curse</summary>
  public static void IncreaseCurse(this PlayerController player, float curse = 1f, bool updateStats = true)
  {
    player.ownerlessStatModifiers.Add(StatType.Curse.Add(curse));
    if (updateStats)
      player.stats.RecalculateStats(player);
  }

  /// <summary>Get the original idle animation name for a gun</summary>
  public static string GetOriginalIdleAnimationName(this Gun gun)
  {
    return $"{gun.InternalSpriteName()}_idle";
  }

  /// <summary>Get a Projectile's original direction for PostProcessProjectile purposes</summary>
  public static float OriginalDirection(this Projectile p)
  {
    return p.transform.right.XY().ToAngle();
  }

  /// <summary>Creates a GameObject with a sprite in the exact same location as an original sprite</summary>
  public static tk2dBaseSprite DuplicateInWorld(this tk2dBaseSprite osprite)
  {
    tk2dBaseSprite sprite = Lazy.SpriteObject(osprite.collection, osprite.spriteId);
    sprite.PlaceAtPositionByAnchor(osprite.WorldCenter, Anchor.MiddleCenter);
    sprite.HeightOffGround        = osprite.HeightOffGround;
    sprite.depthUsesTrimmedBounds = osprite.depthUsesTrimmedBounds;
    sprite.SortingOrder           = osprite.SortingOrder;
    sprite.renderLayer            = osprite.renderLayer;
    sprite.UpdateZDepth();
    return sprite;
  }

  /// <summary>Freezes a projectile and launches it with a short delay</summary>
  public static void FreezeAndLaunchWithDelay(this Projectile p, float delay, float speed, string sound = null)
  {
    p.StartCoroutine(FreezeAndLaunchWithDelay_CR(p, delay, speed, sound));
  }
  private static IEnumerator FreezeAndLaunchWithDelay_CR(Projectile p, float delay, float speed, string sound = null)
  {
    p.Speed = 0.001f;
    yield return new WaitForSeconds(delay);
    p.Speed = speed;
    if (sound != null)
      p.gameObject.Play(sound);
  }

  /// <summary>Returns the distance from a projectile to its owner. Returns -1f if the projectile has no owner.</summary>
  public static float DistanceToOwner(this Projectile p)
  {
    if (p.Owner)
      return (p.Owner.CenterPosition - p.SafeCenter).magnitude;
    return -1f;
  }

  /// <summary>Returns the angle from a projectile's owner to the projectile. Returns -1f if the projectile has no owner.</summary>
  public static float AngleFromOwner(this Projectile p)
  {
    if (p.Owner)
      return (p.SafeCenter - p.Owner.CenterPosition).ToAngle();
    return -1f;
  }

  /// <summary>Get an idle animation for a player spaced every 60 degrees</summary>
  public static string GetEvenlySpacedIdleAnimation(this PlayerController pc, float a)
  {
      string s;
      float absa = Mathf.Abs(a);
      if (absa < 60f || absa > 120f)
        s = (a > 0) ? "idle_bw" : "idle";
      else
        s = (a > 0) ? "idle_backward" : "idle_forward";
      if (pc.UseArmorlessAnim)
        s += "_armorless";
      return s;
  }

  /// <summary>Push a rigidbody out of walls, preventing movement outside the current room</summary>
  public static void CorrectForWalls(this SpeculativeRigidbody body, bool andRigidBodies = false)
  {
    if (!PhysicsEngine.Instance.OverlapCast(body, null, true, andRigidBodies, null, null, false, null, null))
      return;
    DungeonData dd = GameManager.Instance.Dungeon.data;
    Vector2 vector = body.transform.position.XY();
    IntVector2[] cardinalsAndOrdinals = IntVector2.CardinalsAndOrdinals;
    for (int pixels = 1; pixels <= 200; ++pixels)
      for (int i = 0; i < cardinalsAndOrdinals.Length; ++i)
      {
        Vector2 newPos = vector + PhysicsEngine.PixelToUnit(cardinalsAndOrdinals[i] * pixels);
        if (!dd.CheckInBoundsAndValid(newPos.ToIntVector2(VectorConversions.Floor)))
          continue;
        body.transform.position = newPos;
        body.Reinitialize();
        if (!PhysicsEngine.Instance.OverlapCast(body, null, true, andRigidBodies, null, null, false, null, null))
          return;
      }
    Debug.LogError("FREEZE AVERTED!  TELL CAPTAIN PRETZEL!  (you're welcome) 147");
  }

  /// <summary>Push a rigidbody out of a wall towards a specific direction, returning the number of pixels that were moved</summary>
  public static int PullOutOfWall(this SpeculativeRigidbody body, IntVector2 pushDirection)
  {
    if (!PhysicsEngine.Instance.OverlapCast(body, null, true, false, null, null, false, null, null))
      return 0;
    Vector2 vector = body.transform.position.XY();
    for (int pixels = 1; pixels <= 200; ++pixels)
    {
      body.transform.position = vector + PhysicsEngine.PixelToUnit(pushDirection * pixels);
      body.Reinitialize();
      if (!PhysicsEngine.Instance.OverlapCast(body, null, true, false, null, null, false, null, null))
        return pixels;
    }
    Debug.LogError("FREEZE AVERTED!  TELL CAPTAIN PRETZEL!  (you're welcome) 147");
    return -1;
  }

  /// <summary>Push a rigidbody into a wall in a specific direction, backs it out once pixel, and returns the number of pixels we moved</summary>
  public static int PushAgainstWalls(this SpeculativeRigidbody body, IntVector2 pushDirection)
  {
    if (PhysicsEngine.Instance.OverlapCast(body, null, true, false, null, null, false, null, null))
      return 0;
    Vector2 vector = body.transform.position.XY();
    for (int pixels = 1; pixels <= 64; ++pixels)
    {
      body.transform.position = vector + PhysicsEngine.PixelToUnit(pushDirection * pixels);
      body.Reinitialize();
      if (PhysicsEngine.Instance.OverlapCast(body, null, true, false, null, null, false, null, null))
      {
        --pixels;
        body.transform.position = vector + PhysicsEngine.PixelToUnit(pushDirection * pixels);
        body.Reinitialize();
        return pixels;
      }
    }
    Debug.LogError("FREEZE AVERTED!  TELL CAPTAIN PRETZEL!  (you're welcome) 148");
    return -1;
  }

  /// <summary>Check if a rigidbody is against a wall in a specific direction</summary>
  public static bool IsAgainstWall(this SpeculativeRigidbody body, IntVector2 pushDirection, int pixels = 1)
  {
    if (PhysicsEngine.Instance.OverlapCast(body, null, true, false, null, null, false, null, null))
      return true;
    Vector2 oldPos = body.transform.position.XY();
    body.transform.position = oldPos + PhysicsEngine.PixelToUnit(pushDirection * pixels);
    body.Reinitialize();
    bool result = PhysicsEngine.Instance.OverlapCast(body, null, true, false, null, null, false, null, null);
    body.transform.position = oldPos;
    body.Reinitialize();
    return result;
  }

  /// <summary>Gets the gun corresponding to an item from the pickup database</summary>
  public static Gun AsGun(this Items item) => ItemHelper.Get(item) as Gun;
  /// <summary>Gets the active item corresponding to an item from the pickup database</summary>
  public static PlayerItem AsActive(this Items item) => ItemHelper.Get(item) as PlayerItem;
  /// <summary>Gets the passive item corresponding to an item from the pickup database</summary>
  public static PassiveItem AsPassive(this Items item) => ItemHelper.Get(item) as PassiveItem;

  /// <summary>Adds a targeting reticle to the gun</summary>
  public static Gun AddReticle<T>(this Gun gun, GameObject reticleVFX, float reticleAlpha = 1f, float fadeInTime = 0f, float fadeOutTime = 0f, bool smoothLerp = false,
    float maxDistance = -1f, float controllerScale = 1f, float rotateSpeed = 0f, CwaffReticle.Visibility visibility = CwaffReticle.Visibility.DEFAULT,
    bool aimFromPlayerCenter = false, bool background = false) where T : CwaffReticle
  {
    T reticle                   = gun.gameObject.AddComponent<T>();
    reticle.reticleVFX          = reticleVFX;
    reticle.reticleAlpha        = reticleAlpha;
    reticle.fadeInTime          = fadeInTime;
    reticle.fadeOutTime         = fadeOutTime;
    reticle.smoothLerp          = smoothLerp;
    reticle.aimFromPlayerCenter = aimFromPlayerCenter;
    reticle.maxDistance         = maxDistance;
    reticle.controllerScale     = controllerScale;
    reticle.rotateSpeed         = rotateSpeed;
    reticle.visibility          = visibility;
    // reticle.targetObjFunc       = targetObjFunc; //NOTE: can't meaningfully be set / serialized in initialization
    // reticle.targetPosFunc       = targetPosFunc; //NOTE: can't meaningfully be set / serialized in initialization
    reticle.background          = background;
    return gun;
  }

  /// <summary>Check if a rigid body is the Oubilette entrance disguised as a wall, because it causes a lot of problems</summary>
  public static bool IsActuallyOubiletteEntranceRoom(this SpeculativeRigidbody body)
  {
    //NOTE: checking the name against "secret exit collider" is how vanilla gungeon blocks projectiles from the Oubilette entrance...rip
    return body.name.StartsWith("secret exit collider");
  }

  /// <summary>Returns true if the Player is at a location where they would effectively be stuck under normal circumstances</summary>
  /// <remarks>NOT IMPLEMENTED</remarks>
  public static bool IsEffectivelyOutOfBounds(this PlayerController player)
  {
    return false; //TODO: originally needed so Frisbee couldn't clip behind Bello's shop, but that was fixed...should still be implemented at some point
  }

  private static readonly List<AIActor> _NoEnemies = Enumerable.Empty<AIActor>().ToList();
  private static List<AIActor> _RefEnemies = new();
  /// <summary>Get all active enemies in a room, returning an empty list instead of null when the target is invalid</summary>
  public static List<AIActor> SafeGetEnemiesInRoom(this RoomHandler room)
  {
    if (room == null)
      return _NoEnemies;
    room.GetActiveEnemies(RoomHandler.ActiveEnemyType.All, ref _RefEnemies);
    return _RefEnemies;
  }

  /// <summary>Get all active enemies in a room given a Vector2 position, returning an empty list instead of null when the target is invalid</summary>
  public static List<AIActor> SafeGetEnemiesInRoom(this Vector2 pos)
  {
    if (pos.GetAbsoluteRoom() is not RoomHandler room)
      return _NoEnemies;
    room.GetActiveEnemies(RoomHandler.ActiveEnemyType.All, ref _RefEnemies);
    return _RefEnemies;
  }

  /// <summary>Get all active enemies in a room, returning an empty list instead of null when the target is invalid</summary>
  public static void SafeGetEnemiesInRoom(this RoomHandler room, ref List<AIActor> refList)
  {
    refList.Clear();
    if (room != null)
      room.GetActiveEnemies(RoomHandler.ActiveEnemyType.All, ref refList);
  }

  /// <summary>Get all active enemies in a room given a Vector2 position, returning an empty list instead of null when the target is invalid</summary>
  public static void SafeGetEnemiesInRoom(this Vector2 pos, ref List<AIActor> refList)
  {
    refList.Clear();
    if (pos.GetAbsoluteRoom() is RoomHandler room)
      room.GetActiveEnemies(RoomHandler.ActiveEnemyType.All, ref refList);
  }

  /// <summary>Adds an item to a list if it doesn't already contain it</summary>
  public static void AddUnique<T>(this List<T> list, T item)
  {
    if (!list.Contains(item))
      list.Add(item);
  }

  /// <summary>Removes an item from a list if it already contains it</summary>
  public static void TryRemove<T>(this List<T> list, T item)
  {
    if (list.Contains(item))
      list.Remove(item);
  }

  /// <summary>Get an appropriate angle for the current projectile</summary>
  public static float GetAngleForProjectile(this PlayerController player, ProjectileModule mod = null)
  {
      if (player.CurrentGun is not Gun gun)
          return player.m_overrideGunAngle ?? player.m_currentGunAngle;
      return gun.gunAngle + (mod ?? gun.DefaultModule).GetAngleForShot(1f, player.stats.GetStatValue(StatType.Accuracy));
  }

  /// <summary>Returns whether a projectile is on a collision path with an enemy, optionally including walls, pixel perfect collision, and outsetting our hitbox</summary>
  public static bool WouldCollideWithEnemy(this Projectile projectile, float angle, bool accountForWalls = true, bool pixelPerfect = false, int outset = 0)
  {
      Vector2 ppos = projectile.transform.position.XY();
      IntVector2 pixelDelta = accountForWalls
        ? PhysicsEngine.UnitToPixel(ppos.ToNearestWall(out Vector2 normal, angle, minDistance: 1) - ppos)
        : PhysicsEngine.UnitToPixel(angle.ToVector(20f));
      PixelCollider projectileCollider = projectile.specRigidbody.PrimaryPixelCollider;
      foreach (AIActor enemy in ppos.SafeGetEnemiesInRoom())
      {
          if (!enemy.IsHostile(canBeNeutral: true) || !enemy.specRigidbody)
              continue;
          PixelCollider collider = enemy.specRigidbody.HitboxPixelCollider;
          if (accountForWalls && !ppos.HasLineOfSight(collider.UnitCenter))
              continue; // avoid more expensive linear cast if possible
          if (projectileCollider.FastLinearCast(collider, pixelDelta, pixelPerfect: pixelPerfect, outset: outset))
            return true;
      }
      return false;
  }

  /// <summary>Returns whether a projectile is on a collision path with a player, optionally including walls, pixel perfect collision, and outsetting our hitbox</summary>
  public static bool WouldCollideWithPlayer(this Projectile projectile, float angle, bool accountForWalls = true, bool pixelPerfect = false, int outset = 0)
  {
      Vector2 ppos = projectile.transform.position.XY();
      IntVector2 pixelDelta = accountForWalls
        ? PhysicsEngine.UnitToPixel(ppos.ToNearestWall(out Vector2 normal, angle, minDistance: 1) - ppos)
        : PhysicsEngine.UnitToPixel(angle.ToVector(20f));
      PixelCollider projectileCollider = projectile.specRigidbody.PrimaryPixelCollider;
      foreach (PlayerController player in GameManager.Instance.AllPlayers)
      {
          if (!player || player.IsGhost)
            continue;
          PixelCollider collider = player.specRigidbody.HitboxPixelCollider;
          if (accountForWalls && !ppos.HasLineOfSight(collider.UnitCenter))
              continue; // avoid more expensive linear cast if possible
          if (projectileCollider.FastLinearCast(collider, pixelDelta, pixelPerfect: pixelPerfect, outset: outset))
            return true;
      }
      return false;
  }

  private static readonly List<PixelCollider.StepData> _Steps = new();
  /// <summary>Fast version of LinearCast that doesn't allocate a LinearCastResult, with optional imprecise collision and hitbox outset collision</summary>
  public static bool FastLinearCast(this PixelCollider myCollider, PixelCollider otherCollider, IntVector2 pixelsToMove, bool pixelPerfect = false, int outset = 0)
  {
      PhysicsEngine.PixelMovementGenerator(pixelsToMove, _Steps);
      return myCollider.FastLinearCast(otherCollider, pixelsToMove, _Steps, pixelPerfect, outset);
  }

  /// <summary>Fast version of LinearCast that doesn't allocate a LinearCastResult, with optional imprecise collision and hitbox outset collision</summary>
  public static bool FastLinearCast(this PixelCollider myCollider, PixelCollider otherCollider, IntVector2 pixelsToMove, List<PixelCollider.StepData> stepList, bool pixelPerfect = false, int outset = 0)
  {
      if (!myCollider.Enabled)
          return false;
      if (otherCollider.DirectionIgnorer != null && otherCollider.DirectionIgnorer(pixelsToMove))
          return false;

      IntVector2 myPos  = myCollider.m_position;
      IntVector2 myDims = myCollider.m_dimensions;
      if (outset > 0) // outset our hitbox dimensions a bit
      {
        myPos  -= new IntVector2(outset, outset);
        myDims += new IntVector2(2 * outset, 2 * outset);
      }

      IntVector2 totalDelta = IntVector2.Zero;
      IntVector2 posDelta   = otherCollider.m_position - myPos;
      for (int i = 0; i < stepList.Count; i++)
      {
          IntVector2 deltaPos = stepList[i].deltaPos;
          IntVector2 stepPos = myPos + totalDelta + deltaPos;
          if (!IntVector2.AABBOverlap(stepPos, myDims, otherCollider.Position, otherCollider.Dimensions))
          {
            totalDelta += deltaPos;
            continue;
          }
          if (!pixelPerfect)
            return true; // only care about overlapping rectangles

          IntVector2 minPos = IntVector2.Max(IntVector2.Zero, otherCollider.Position - stepPos);
          IntVector2 maxPos = IntVector2.Min(myDims - IntVector2.One, otherCollider.UpperRight - stepPos);
          for (int j = minPos.x; j <= maxPos.x; j++)
          {
              for (int k = minPos.y; k <= maxPos.y; k++)
              {
                  if (!myCollider.m_bestPixels[j, k])
                      continue;
                  IntVector2 pos = new IntVector2(j, k) - posDelta + totalDelta + deltaPos;
                  if (pos.x >= 0 && pos.x < otherCollider.Dimensions.x && pos.y >= 0 && pos.y < otherCollider.Dimensions.y && otherCollider[pos])
                    return true;
              }
          }
          totalDelta += deltaPos;
      }
      return false;
  }

  /// <summary>Determine if a projectile was fired for free</summary>
  public static bool FiredForFree(this Projectile p)
  {
    return p.gameObject.GetComponent<CwaffProjectile>() is CwaffProjectile c && c.firedForFree;
  }

  /// <summary>Check whether our secondary reload button was pressed</summary>
  public static bool SecondaryReloadPressed(this PlayerController player)
  {
    if (CwaffConfig._SecondaryReload == CwaffConfig.SecondaryReloadKey.None)
      return false;
    if (BraveInput.GetInstanceForPlayer(player.PlayerIDX).ActiveActions.Device is not InControl.InputDevice device)
      return false;
    if (CwaffConfig._SecondaryReload == CwaffConfig.SecondaryReloadKey.Left)
      return device.LeftStickButton.WasPressed;
    if (CwaffConfig._SecondaryReload == CwaffConfig.SecondaryReloadKey.Right)
      return device.RightStickButton.WasPressed;
    return false;
  }

  private static float _LastCameraCacheTime;
  private static Vector2 _CachedCameraMin;
  private static Vector2 _CachedCameraMax;

  /// <summary>Detect if a point is on screen</summary>
  public static bool OnScreen(this Vector2 pos)
  {
      // Conservatively compute the camera coordinates at most once per frame
      if (_LastCameraCacheTime != BraveTime.ScaledTimeSinceStartup)
      {
          _CachedCameraMin     = BraveUtility.ViewportToWorldpoint(Vector2.zero, ViewportType.Gameplay);
          _CachedCameraMax     = BraveUtility.ViewportToWorldpoint(Vector2.one, ViewportType.Gameplay);
          _LastCameraCacheTime = BraveTime.ScaledTimeSinceStartup;
      }
      if (pos.x < _CachedCameraMin.x || pos.x > _CachedCameraMax.x || pos.y < _CachedCameraMin.y || pos.y > _CachedCameraMax.y)
          return false;
      return true;
  }

  /// <summary>Detect if a point is on screen with a buffer</summary>
  public static bool OnScreen(this Vector2 pos, float leeway)
  {
      // Conservatively compute the camera coordinates at most once per frame
      if (_LastCameraCacheTime != BraveTime.ScaledTimeSinceStartup)
      {
          _CachedCameraMin     = BraveUtility.ViewportToWorldpoint(Vector2.zero, ViewportType.Gameplay);
          _CachedCameraMax     = BraveUtility.ViewportToWorldpoint(Vector2.one, ViewportType.Gameplay);
          _LastCameraCacheTime = BraveTime.ScaledTimeSinceStartup;
      }
      if (pos.x < _CachedCameraMin.x - leeway || pos.x > _CachedCameraMax.x + leeway || pos.y < _CachedCameraMin.y - leeway || pos.y > _CachedCameraMax.y + leeway)
          return false;
      return true;
  }

  /// <summary>Detect if a point is on screen, Vector3 version</summary>
  public static bool OnScreen(this Vector3 pos) => pos.XY().OnScreen();

  /// <summary>Detect if a point is on screen with a buffer, Vector3 version</summary>
  public static bool OnScreen(this Vector3 pos, float leeway) => pos.XY().OnScreen(leeway);

  /// <summary>Helper for flashing VFX briefly above the player's head</summary>
  public static void FlashVFXAbovePlayer(this PlayerController player, GameObject vfx, string sound = null, float time = 1.0f, bool glowAndFade = false, float glowAmount = 5f)
  {
      GameObject v = SpawnManager.SpawnVFX(vfx, player.sprite.WorldTopCenter + new Vector2(0f, 0.5f), Quaternion.identity);
      v.SetLayerRecursively(LayerMask.NameToLayer("Unoccluded"));
      v.transform.parent = player.transform;
      if (glowAndFade)
        v.AddComponent<GlowAndFadeOut>().Setup(
          fadeInTime: 0.15f, glowInTime: 0.20f, holdTime: time, glowOutTime: 0.20f, fadeOutTime: 0.15f, maxEmit: glowAmount, destroy: true);
      else
        v.ExpireIn(time);
      if (sound != null)
        player.gameObject.Play(sound);
  }

  /// <summary>Extend an enum from a string</summary>
  public static T ExtendEnum<T>(this string s) where T : System.Enum
  {
    return ETGModCompatibility.ExtendEnum<T>(C.MOD_PREFIX.ToUpper(), s);
  }

  /// <summary>Creates a stationary path for a pathmover object</summary>
  public static void CreateDummyPath(this PathMover pathMover)
  {
      Vector2 pos = pathMover.gameObject.transform.position;
      RoomHandler room = pos.GetAbsoluteRoom();
      IntVector2 posInRoom = (pos - room.area.basePosition.ToVector2()).ToIntVector2();
      SerializedPath serializedPath = new SerializedPath(posInRoom);
      serializedPath.AddPosition(posInRoom);
      serializedPath.wrapMode = SerializedPath.SerializedPathWrapMode.PingPong;
      SerializedPathNode pathNode;
      pathNode = serializedPath.nodes[0];
          pathNode.placement = SerializedPathNode.SerializedNodePlacement.Center;
          serializedPath.nodes[0] = pathNode;
      pathNode = serializedPath.nodes[1];
          pathNode.placement = SerializedPathNode.SerializedNodePlacement.Center;
          serializedPath.nodes[1] = pathNode;
      pathMover.RoomHandler = room;
      pathMover.Path = serializedPath;
      pathMover.PathStartNode = 0;
  }

  /// <summary>Return and remove the last element of a LinkedList</summary>
  public static T Pop<T>(this LinkedList<T> linked)
  {
    T t = linked.Last();
    linked.RemoveLast();
    return t;
  }

  // Make a sprite arc smoothly from its current position to a target position
  // minScale == minimum scale our pickup can shrink down to
  // vanishPercent == percent of the way through the wrap animation the pickup should vanish
  public static void ArcTowards(this tk2dBaseSprite sprite, float animLength, tk2dBaseSprite targetSprite, bool useBottom = false, float minScale = 0.4f, float vanishPercent = 0.5f)
  {
      sprite.StartCoroutine(ArcTowards_CR(sprite: sprite, animLength: animLength, targetSprite: targetSprite, useBottom: useBottom,
        minScale: minScale, vanishPercent: vanishPercent));
  }

  private static IEnumerator ArcTowards_CR(tk2dBaseSprite sprite, float animLength, tk2dBaseSprite targetSprite, bool useBottom, float minScale, float vanishPercent)
  {
      // Suck the pickup into the present and wait for the animation to play out
      Vector2 startPosition = sprite.WorldCenter;
      float loopLength      = animLength * vanishPercent;
      for (float elapsed = 0f; elapsed < loopLength; elapsed += BraveTime.DeltaTime)
      {
          if (!sprite)
              break;

          float percentDone                = Mathf.Clamp01(elapsed / loopLength);
          float cubicLerp                  = Ease.OutCubic(percentDone);
          Vector2 extraOffset              = new Vector2(0f, 2f * Mathf.Sin(Mathf.PI * cubicLerp));
          Vector2 curPosition              = extraOffset + Vector2.Lerp(startPosition, useBottom ? targetSprite.WorldBottomCenter : targetSprite.WorldCenter, cubicLerp);
          float scale                      = 1f - ((1f - minScale) * cubicLerp);
          sprite.transform.localScale = new Vector3(scale, scale, 1f);
          sprite.PlaceAtScaledPositionByAnchor(curPosition, Anchor.MiddleCenter);
          sprite.renderer.SetAlpha(1f - loopLength);
          yield return null;
      }
      UnityEngine.Object.Destroy(sprite.gameObject);
      yield break;
  }

  /// <summary>Unity null safe version of GetComponent</summary>
  public static T GetSafeComponent<T>(this GameObject g) where T : Component
    => (g && g.GetComponent<T>() is T t) ? t : null;

  /// <summary>Adds projectile modules to a gun when a synergy is active</summary>
  public static void AddSynergyModules(this Gun gun, Synergy s, params ProjectileModule[] modules)
  {
      gun.gameObject.AddComponent<VolleyModificationSynergyProcessor>().synergies =
        [new(){RequiredSynergy = s.Synergy(), AddsModules = true, ModulesToAdd = modules}];
  }

  /// <summary>Adds a final projectile to a gun when a synergy is active</summary>
  public static void AddSynergyFinalProjectile(this Gun gun, Synergy s, Projectile newFinal, string clipName, int num = 1)
  {
      gun.gameObject.AddComponent<VolleyModificationSynergyProcessor>().synergies =
        [new(){RequiredSynergy = s.Synergy(), SetsNumberFinalProjectiles = true, AddsNewFinalProjectile = true,
          NewFinalProjectile = newFinal, NewFinalProjectileAmmoType = Lazy.SetupCustomAmmoClip(clipName),
          NumberFinalProjectiles = num}];
  }

  /// <summary>Get a gun's display name without Gunderfury level or GungeonCraft mastery modifiers</summary>
  public static string GetUnmodifiedDisplayName(this Gun gun)
  {
    if (gun.encounterTrackable is not EncounterTrackable et)
      return string.Empty;
    if (et.m_journalData is not JournalEntry je)
      return string.Empty;
    return je.GetPrimaryDisplayName();
  }

  /// <summary>Jumps to the next instruction a specific number of times, and moves after the final instance</summary>
  public static bool JumpToNext(this ILCursor cursor, Func<Instruction, bool> match, int times = 1)
  {
    for(int i = 0; i < times; i++)
      if (!cursor.TryGotoNext(MoveType.After, match))
        return false;
    return true;
  }

  /// <summary>Jumps to the next instruction a specific number of times, and moves before the final instance</summary>
  public static bool JumpBeforeNext(this ILCursor cursor, Func<Instruction, bool> match, int times = 1)
  {
    for(int i = 0; i < times; i++)
      if (!cursor.TryGotoNext(MoveType.Before, match))
        return false;
    return true;
  }

  private static Dictionary<string, string> _EnemyNames = new();
  /// <summary>Get an enemy's Ammonomicon display name from their guid</summary>
  public static string AmmonomiconName(this string guid)
  {
    if (_EnemyNames.TryGetValue(guid, out string name))
      return name;
    if (EnemyDatabase.GetOrLoadByGuid(guid) is not AIActor enemy)
      return _EnemyNames[guid] = string.Empty;
    if (enemy.encounterTrackable is not EncounterTrackable trackable)
      return _EnemyNames[guid] = enemy.ActorName ?? string.Empty;
    if (trackable.journalData is not JournalEntry entry)
      return _EnemyNames[guid] = enemy.ActorName ?? string.Empty;
    if (string.IsNullOrEmpty(entry.PrimaryDisplayName))
      return _EnemyNames[guid] = enemy.ActorName ?? string.Empty;
    if (entry.PrimaryDisplayName[0] != '#')
      return _EnemyNames[guid] = entry.PrimaryDisplayName;
    return _EnemyNames[guid] = StringTableManager.GetEnemiesString(entry.PrimaryDisplayName);
  }

  /// <summary>Get an enemy's Ammonomicon display name from their gameObject</summary>
  public static string AmmonomiconName(this AIActor enemy) => enemy.EnemyGuid.AmmonomiconName();

  /// <summary>Apply a shader to an enemy using a specific shader function</summary>
  public static void ApplyShader(this AIActor enemy, Action<tk2dBaseSprite> shaderFunc, bool includeHands = true, bool includeGun = false)
  {
    shaderFunc(enemy.sprite);
    if (includeGun && enemy.CurrentGun is Gun gun)
      shaderFunc(gun.sprite);
    if (includeHands)
      for (int i = 0; i < enemy.transform.childCount; ++i)
        if (enemy.transform.GetChild(i).GetComponent<tk2dSprite>() is tk2dSprite sprite)
          shaderFunc(sprite);
    if (enemy.optionalPalette != null)
    {
        Material mat = enemy.sprite.renderer.material;
        if (mat.HasProperty("_PaletteTex"))
        {
          mat.SetFloat("_UsePalette", 1f);
          mat.SetTexture("_PaletteTex", enemy.optionalPalette);
        }
    }
  }

  /// <summary>Get the current goop data for a position in the world.</summary>
  public static GoopPositionData GoopData(this Vector2 pos)
  {
    IntVector2 key = (pos / DeadlyDeadlyGoopManager.GOOP_GRID_SIZE).ToIntVector2(VectorConversions.Floor);
    if (!DeadlyDeadlyGoopManager.allGoopPositionMap.TryGetValue(key, out DeadlyDeadlyGoopManager goopManager))
      return null;
    if (!goopManager.m_goopedCells.TryGetValue(key, out GoopPositionData data))
      return null;
    return data;
  }

  /// <summary>Get the current goop data for a position in the world, as well as the goop definition.</summary>
  public static GoopPositionData GoopData(this Vector2 pos, out GoopDefinition goopDef)
  {
    goopDef = null;
    IntVector2 key = (pos / DeadlyDeadlyGoopManager.GOOP_GRID_SIZE).ToIntVector2(VectorConversions.Floor);
    if (!DeadlyDeadlyGoopManager.allGoopPositionMap.TryGetValue(key, out DeadlyDeadlyGoopManager goopManager))
      return null;
    if (!goopManager.m_goopedCells.TryGetValue(key, out GoopPositionData data))
      return null;
    goopDef = goopManager.goopDefinition;
    return data;
  }

  private static readonly List<SpeculativeRigidbody> _NoIgnores = Enumerable.Empty<SpeculativeRigidbody>().ToList();
  /// <summary>Creates a new explosion</summary>
  public static ExplosionData With(this ExplosionData effect, float force = 100f, float debrisForce = 10f, float damage = 10f, float radius = 0.5f,
      bool preventPlayerForce = false, bool shake = true)
  {
      return new ExplosionData()
      {
          forceUseThisRadius     = true,
          pushRadius             = radius,
          damageRadius           = radius,
          damageToPlayer         = 0f,
          doDamage               = (damage > 0),
          damage                 = damage,
          doDestroyProjectiles   = false,
          doForce                = (force > 0 || debrisForce > 0),
          force                  = force,
          debrisForce            = debrisForce,
          preventPlayerForce     = preventPlayerForce,
          explosionDelay         = 0.01f,
          usesComprehensiveDelay = false,
          doScreenShake          = shake,
          playDefaultSFX         = true,
          ignoreList             = _NoIgnores,
          effect                 = effect.effect,
          ss                     = effect.ss,
      };
  }

  /// <summary>Creates a new explosion based on preexisting explosion data</summary>
  public static ExplosionData Clone(this ExplosionData effect)
  {
      return new ExplosionData()
      {
          forceUseThisRadius     = effect.forceUseThisRadius,
          pushRadius             = effect.pushRadius,
          damageRadius           = effect.damageRadius,
          damageToPlayer         = effect.damageToPlayer,
          doDamage               = effect.doDamage,
          damage                 = effect.damage,
          doDestroyProjectiles   = effect.doDestroyProjectiles,
          doForce                = effect.doForce,
          force                  = effect.force,
          debrisForce            = effect.debrisForce,
          preventPlayerForce     = effect.preventPlayerForce,
          explosionDelay         = effect.explosionDelay,
          usesComprehensiveDelay = effect.usesComprehensiveDelay,
          doScreenShake          = effect.doScreenShake,
          playDefaultSFX         = effect.playDefaultSFX,
          ignoreList             = effect.ignoreList,
          effect                 = effect.effect,
          ss                     = effect.ss,
      };
  }

  /// <summary>Set an audio event for a specific frame of a projectile's animation</summary>
  public static Projectile AudioEvent(this Projectile proj, string audio = "", int frame = 0)
  {
    tk2dSpriteAnimationFrame aframe = proj.sprite.spriteAnimator.DefaultClip.frames[frame];
    aframe.triggerEvent = !string.IsNullOrEmpty(audio);
    aframe.eventAudio = audio;
    return proj;
  }

  /// <summary>Given a non-increasing array of numbers, returns the first index i for which val is at least vals[i].</summary>
  public static int FirstGE<T>(this T[] vals, T val) where T : IComparable<T>
  {
    for (int i = 0; i < vals.Length; ++i)
      if (val.CompareTo(vals[i]) >= 0)
        return i;
    return vals.Length;
  }

  /// <summary>Given a non-increasing array of numbers, returns the first index i for which val is more than vals[i].</summary>
  public static int FirstGT<T>(this T[] vals, T val) where T : IComparable<T>
  {
    for (int i = 0; i < vals.Length; ++i)
      if (val.CompareTo(vals[i]) > 0)
        return i;
    return vals.Length;
  }

  /// <summary>Given a non-decreasing array of numbers, returns the first index i for which val is no more than vals[i].</summary>
  public static int FirstLE<T>(this T[] vals, T val) where T : IComparable<T>
  {
    for (int i = 0; i < vals.Length; ++i)
      if (val.CompareTo(vals[i]) <= 0)
        return i;
    return vals.Length;
  }

  /// <summary>Given a non-decreasing array of numbers, returns the first index i for which val is less than vals[i].</summary>
  public static int FirstLT<T>(this T[] vals, T val) where T : IComparable<T>
  {
    for (int i = 0; i < vals.Length; ++i)
      if (val.CompareTo(vals[i]) < 0)
        return i;
    return vals.Length;
  }

  /// <summary>Sets the owner of a projectile and, if it's a player, copy over projectile baseData from their stats</summary>
  public static void SetOwnerAndStats(this Projectile p, GameActor owner)
  {
    p.Owner = owner;
    p.SetNewShooter(owner.specRigidbody);
    if (owner is not PlayerController pc)
      return;
    p.baseData.damage *= pc.DamageMult();
    p.baseData.range *= pc.RangeMult();
    p.baseData.force *= pc.KnockbackMult();
    p.baseData.speed *= pc.ProjSpeedMult();
  }

  /// <summary>Check if a body has a collision layer override</summary>
  public static bool HasCollisionLayerOverride(this SpeculativeRigidbody body, int mask)
  {
    for (int i = 0; i < body.PixelColliders.Count; i++)
      if ((body.PixelColliders[i].CollisionLayerCollidableOverride & mask) == mask)
        return true;
    return false;
  }

  /// <summary>Check if a body has a collision layer ignore override</summary>
  public static bool HasCollisionLayerIgnoreOverride(this SpeculativeRigidbody body, int mask)
  {
    for (int i = 0; i < body.PixelColliders.Count; i++)
      if ((body.PixelColliders[i].CollisionLayerIgnoreOverride & mask) == mask)
        return true;
    return false;
  }

  /// <summary>Get the name of the default clip for a sprite animator</summary>
  public static string DefaultClipName(this tk2dSpriteAnimator animator)
  {
      if (!animator)
          return "no animator";
      if (animator.DefaultClip == null)
          return "no default clip";
      if (animator.DefaultClip.frames == null || animator.DefaultClip.frames.Length == 0)
          return "no default clip frames";
      tk2dSpriteAnimationFrame frame = animator.DefaultClip.frames[0];
      if (!frame.spriteCollection)
          return "no sprite collection";
      return frame.spriteCollection.spriteDefinitions[frame.spriteId].name;
  }

  //NOTE: this skews towards the bottom of the room for some reason
  /// <summary>Returns a random position in the GameActor's current room</summary>
  public static Vector2 RandomPosInCurrentRoom(this PlayerController player)
  {
    if (!player)
      player = GameManager.Instance.BestActivePlayer;
    RoomHandler room = player.CurrentRoom;
    if (room == null)
      return player.CenterPosition;
    return room.GetRandomVisibleClearSpot(2, 2).ToVector2();
  }

  /// <summary>Implementation of TryGetValue for ListDictionary</summary>
  public static bool TryGetValue<K, V>(this ListDictionary d, K key, out V value)
  {
    if (d.Contains(key))
    {
        value = (V)d[key];
        return true;
    }
    value = default;
    return false;
  }

  private static GameObject _MiniBlankVFX = null;
  /// <summary>Do a mini-blank effect with a custom color</summary>
  public static void DoColorfulMiniBlank(this PlayerController user, Color color, Vector2? position = null)
  {
      Vector2 pos = position ?? user.CenterPosition;
      SilencerInstance silencerInstance = new GameObject("silencer").AddComponent<SilencerInstance>();
      silencerInstance.TriggerSilencer(pos, 20f, 5f, null, 0f, 3f, 3f, 3f, 30f, 3f, 0.25f, user);

      _MiniBlankVFX ??= (GameObject)BraveResources.Load("Global VFX/BlankVFX_Ghost");
      GameObject blankVfx = UnityEngine.Object.Instantiate(_MiniBlankVFX, pos.ToVector3ZUp(pos.y), Quaternion.identity);
          tk2dSprite blankVfxSprite = blankVfx.GetComponentInChildren<tk2dSprite>();
          blankVfxSprite.usesOverrideMaterial = true;
          blankVfxSprite.renderer.material.shader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTintableTiltedCutoutEmissive");
          blankVfxSprite.renderer.material.SetColor("_OverrideColor", color.WithAlpha(0.25f));
      UnityEngine.Object.Destroy(blankVfx, 1f);

      user.DoVibration(Vibration.Time.Quick, Vibration.Strength.Medium);
  }

  private static GameObject _BlankVFX = null;
  /// <summary>Do a blank effect with a custom color</summary>
  public static void DoColorfulBlank(this PlayerController user, Color color, Vector2? position = null)
  // public void DoColorfulBlank(float overrideRadius = 25f, float overrideTimeAtMaxRadius = 0.5f, bool silent = false, bool breaksWalls = true, Vector2? overrideCenter = null, bool breaksObjects = true, float overrideForce = -1f)
  {
      Vector2 pos = position ?? user.CenterPosition;
      SilencerInstance silencerInstance = new GameObject("silencer").AddComponent<SilencerInstance>();
      silencerInstance.TriggerSilencer(pos, 50f, 25f, null, 0.15f, 0.2f, 50, 10, 140f, 15, 0.5f, user, true);

      _BlankVFX ??= (GameObject)BraveResources.Load("Global VFX/BlankVFX");
      GameObject blankVfx = UnityEngine.Object.Instantiate(_BlankVFX, pos.ToVector3ZUp(pos.y), Quaternion.identity);
          tk2dSprite blankVfxSprite = blankVfx.GetComponentInChildren<tk2dSprite>();
          blankVfxSprite.usesOverrideMaterial = true;
          blankVfxSprite.renderer.material.shader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTintableTiltedCutoutEmissive");
          blankVfxSprite.renderer.material.SetColor("_OverrideColor", color.WithAlpha(0.25f));
      UnityEngine.Object.Destroy(blankVfx, 1f);

      user.DoVibration(Vibration.Time.Quick, Vibration.Strength.Medium);
  }

  /// <summary>Add a component to a gameObject, perform setup if necessary, and return the gameObject</summary>
  public static GameObject Attach<T>(this GameObject go, Action<T> predicate = null, bool allowDuplicates = false) where T : MonoBehaviour
  {
    T component = allowDuplicates ? go.gameObject.AddComponent<T>() : go.gameObject.GetOrAddComponent<T>();
    if (predicate != null)
      predicate(component);
    return go;
  }

  /// <summary>Create an animation and add it to a sprite collection</summary>
  public static tk2dSpriteAnimationClip AddAnimation(this tk2dSpriteCollectionData coll, string spriteName, string animName = null, float fps = 4,
      int loopStart = 0, Anchor? adjustToAnchor = null)
  {
      if (ResMap.Get(spriteName, quietFailure: true) is not List<string> spritePaths)
          return null;

      tk2dSpriteAnimationClip clip = new tk2dSpriteAnimationClip() {
          name      = animName ?? spriteName,
          fps       = fps,
          frames    = new tk2dSpriteAnimationFrame[spritePaths.Count],
          loopStart = loopStart,
          wrapMode  =
              (loopStart > 0)  ? tk2dSpriteAnimationClip.WrapMode.LoopSection :
              (loopStart == 0) ? tk2dSpriteAnimationClip.WrapMode.Loop : tk2dSpriteAnimationClip.WrapMode.Once
      };


      bool adjustAnchor = adjustToAnchor.HasValue;
      Anchor anchor = adjustToAnchor ?? default;
      for (int i = 0; i < spritePaths.Count; i++)
      {
          int frameSpriteId = coll.GetSpriteIdByName(spritePaths[i]);
          if (adjustAnchor)
            coll.spriteDefinitions[frameSpriteId].BetterConstructOffsetsFromAnchor(anchor);
          clip.frames[i] = new() { spriteId = frameSpriteId, spriteCollection = coll };
      }
      return clip;
  }

  /// <summary>Runs actor.HandlePitChecks() logic to determine if an actor will definitely fall at their current position</summary>
  public static bool WillDefinitelyFall(this GameActor actor)
  {
    Rect source = default(Rect);
    source.min = PhysicsEngine.PixelToUnitMidpoint(actor.specRigidbody.PrimaryPixelCollider.LowerLeft);
    source.max = PhysicsEngine.PixelToUnitMidpoint(actor.specRigidbody.PrimaryPixelCollider.UpperRight);
    Rect rect = new Rect(source);
    actor.ModifyPitVectors(ref rect);
    Dungeon dungeon = GameManager.Instance.Dungeon;
    bool flag2 = dungeon.ShouldReallyFall(rect.min);
    bool flag3 = dungeon.ShouldReallyFall(new Vector3(rect.xMax, rect.yMin));
    bool flag4 = dungeon.ShouldReallyFall(new Vector3(rect.xMin, rect.yMax));
    bool flag5 = dungeon.ShouldReallyFall(rect.max);
    bool flag6 = dungeon.ShouldReallyFall(rect.center);
    bool overPitAtAll = flag2 || flag3 || flag4 || flag5 || flag6;
    if (!overPitAtAll)
      return false;

    flag2 |= dungeon.data.isWall((int)rect.xMin, (int)rect.yMin);
    flag3 |= dungeon.data.isWall((int)rect.xMax, (int)rect.yMin);
    flag4 |= dungeon.data.isWall((int)rect.xMin, (int)rect.yMax);
    flag5 |= dungeon.data.isWall((int)rect.xMax, (int)rect.yMax);
    flag6 |= dungeon.data.isWall((int)rect.center.x, (int)rect.center.y);
    return flag2 && flag3 && flag4 && flag5 && flag6;
  }

  /// <summary>Returns true iff pos is over or within one unit of a pit cell</summary>
  public static bool NearPit(this Vector2 pos)
  {
      DungeonData dd = GameManager.Instance.Dungeon.data;
      IntVector2 ipos = pos.ToIntVector2(VectorConversions.Floor);
      for (int x = ipos.x - 1; x <= ipos.x + 1; ++x)
      {
          for (int y = ipos.y - 1; y <= ipos.y + 1; ++y)
          {
              IntVector2 cpos = new(x, y);
              if (!dd.CheckInBoundsAndValid(cpos))
                  continue;
              if (dd[cpos] is not CellData cell)
                  continue;
              if (cell.type == CellType.PIT)
                  return true;
          }
      }

      return false;
  }

  public static bool InBounds(this Vector2 pos, bool wallsOk = false)
  {
    IntVector2 ipos = pos.ToIntVector2(VectorConversions.Floor);
    DungeonData dd = GameManager.Instance.Dungeon.data;
    if (ipos.x >= 0 && ipos.x < dd.Width && ipos.y >= 0 && ipos.y < dd.Height)
      return dd[ipos] != null && (wallsOk || dd[ipos].type != CellType.WALL);
    return false;
  }

  //NOTE: couldn't get this to work ):
  // public static void UpdateAmmonomiconSprite(this PickupObject item, int spriteId)
  // {
  //   int pickupID = item.PickupObjectId;
  //   string newName = item.sprite.collection.spriteDefinitions[spriteId].name;
  //   ETGModConsole.Log($"updating ammonomicon sprite");
  //   ETGModConsole.Log($"  old: {item.encounterTrackable.journalData.AmmonomiconSprite}");
  //   PickupObjectDatabase.GetById(item.PickupObjectId).encounterTrackable.journalData.AmmonomiconSprite = newName;
  //   PickupObjectDatabase.GetById(item.PickupObjectId).GetComponent<EncounterTrackable>().journalData.AmmonomiconSprite = newName;
  //   item.encounterTrackable.journalData.AmmonomiconSprite = newName;
  //   item.gameObject.GetComponent<EncounterTrackable>().journalData.AmmonomiconSprite = newName;
  //   ETGModConsole.Log($"  new: {item.encounterTrackable.journalData.AmmonomiconSprite}");

  //   var pages = AmmonomiconController.Instance.m_extantPageMap;
  //   if (pages.TryGetValue(AmmonomiconPageRenderer.PageType.EQUIPMENT_LEFT, out AmmonomiconPageRenderer ePage))
  //   {
  //     ETGModConsole.Log($"1");
  //     Transform transform = ePage.guiManager.transform.Find("Scroll Panel").Find("Scroll Panel");
  //     ETGModConsole.Log($"2");
  //     dfPanel component = transform.Find("Guns Panel").GetComponent<dfPanel>();
  //     ETGModConsole.Log($"3");
  //     dfPanel component5 = component.transform.GetChild(0).GetComponent<dfPanel>();
  //     ETGModConsole.Log($"4");
  //     dfPanel component6 = transform.Find("Active Items Panel").GetComponent<dfPanel>();
  //     ETGModConsole.Log($"5");
  //     component5 = component6.transform.GetChild(0).GetComponent<dfPanel>();
  //     ETGModConsole.Log($"6 with {component5.controls.Count} controls");
  //     foreach (dfControl control in component5.controls)
  //     {
  //       if (control is not dfButton button)
  //         continue;
  //       ETGModConsole.Log($" 7");
  //       if (button.GetComponent<AmmonomiconPokedexEntry>() is not AmmonomiconPokedexEntry dex)
  //         continue;
  //       ETGModConsole.Log($"  8");
  //       if (dex.pickupID != pickupID)
  //         continue;
  //       ETGModConsole.Log($"  9");
  //       dex.m_childSprite.spriteId = spriteId;
  //       dex.m_childSprite = null;
  //       ETGModConsole.Log($"  10");
  //       component6.PerformLayout();
  //       // ePage.RebuildRenderData();
  //       ePage.DoRefreshData();
  //       break;
  //     }
  //   }
  // }

  /// <summary>Get the default animation for a game object</summary>
  public static tk2dSpriteAnimationClip DefaultAnimation(this GameObject vfx)
  {
    return vfx.GetComponent<tk2dSpriteAnimator>().library.clips[0];
  }

  /// <summary>Convenience method for calling an internal / private static function with an ILCursor</summary>
  public static void CallPrivate(this ILCursor cursor, Type t, string name)
  {
    cursor.Emit(OpCodes.Call, t.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic));
  }

  /// <summary>Add a stat modifier directly to a gun's passive stat modifiers</summary>
  public static void AddStatToGun(this Gun item, StatModifier modifier)
  {
      item.passiveStatModifiers ??= [];
      Array.Resize(ref item.passiveStatModifiers, item.passiveStatModifiers.Length + 1);
      item.passiveStatModifiers[item.passiveStatModifiers.Length - 1] = modifier;
  }

  /// <summary>Set the speed for a gun's animation and return the Gun</summary>
  public static Gun UpdateAnimationFPS(this Gun gun, string animation, int fps)
  {
    gun.SetAnimationFPS(animation, fps);
    return gun;
  }

  /// <summary>Play an animation if it's not the current one</summary>
  public static void PlayIfNotPlaying(this tk2dSpriteAnimator animator, string anim)
  {
    if (!animator.IsPlaying(anim))
      animator.Play(anim);
  }

  /// <summary>Make an item increase the chance of finding another item.</summary>
  public static T IncreaseLootChance<T>(this T pickup, int pickupId, float multiplier) where T : PickupObject
  {
    pickup.associatedItemChanceMods ??= new LootModData[0];
    int oldLength = pickup.associatedItemChanceMods.Length;
    Array.Resize(ref pickup.associatedItemChanceMods, oldLength + 1);
    pickup.associatedItemChanceMods[oldLength] = new(){
      AssociatedPickupId = pickupId,
      DropRateMultiplier = multiplier,
    };
    return pickup;
  }

  private class UnresolvedLootData
  {
    public PickupObject pickup;
    public Type type;
    public float multiplier;
  }
  private static readonly List<UnresolvedLootData> _UnresolvedLootChances = new();
  /// <summary>Make an item increase the chance of finding another modded item.</summary>
  public static T IncreaseLootChance<T>(this T pickup, Type pickupType, float multiplier) where T : PickupObject
  {
    _UnresolvedLootChances.Add(new(){
      pickup = pickup,
      type = pickupType,
      multiplier = multiplier,
    }); // resolved later with call to Lazy.ResolveModdedLootChances()
    return pickup;
  }

  /// <summary>Finalize loot chance adjustments for modded items.</summary>
  internal static void ResolveModdedLootChances(this GameManager gm)
  {
    foreach (UnresolvedLootData u in _UnresolvedLootChances)
    {
      PickupObject pickup = u.pickup;
      pickup.associatedItemChanceMods ??= new LootModData[0];
      int oldLength = pickup.associatedItemChanceMods.Length;
      Array.Resize(ref pickup.associatedItemChanceMods, oldLength + 1);
      pickup.associatedItemChanceMods[oldLength] = new(){
        AssociatedPickupId = Lazy.PickupId(u.type),
        DropRateMultiplier = u.multiplier,
      };
      // Lazy.DebugLog($"adding loot chance x{u.multiplier} for {Lazy.PickupId(u.type)} == {Lazy.Pickup(u.type).DisplayName} when possessing {pickup.DisplayName}");
    }
  }

  /// <summary>Check whether a projectile is in front of the player or behind the player relative to its direction of travel. Returns true if the projectile is not moving.</summary>
  public static bool HeadingTowardPlayer(this Projectile proj, PlayerController player)
  {
    return proj.Direction == Vector2.zero || proj.Direction.ToAngle().IsNearAngle((player.CenterPosition - proj.SafeCenter).ToAngle(), 90f);
  }

  /// <summary>Get the percent of the way a given vector c is between two endpoints a and b (assumes all 3 points are roughly colinear)</summary>
  public static float LazyInverseLerp(this Vector2 c, Vector2 a, Vector2 b)
  {
    return Mathf.Sqrt((c-a).sqrMagnitude / (b-a).sqrMagnitude);
  }

  /// <summary>Creates chain lighting VFX from a normal VFX object</summary>
  public static GameObject MakeChainLightingVFX(this GameObject vfx)
  {
      GameObject prefab = Game.Items["shock_rounds"].GetComponent<ComplexProjectileModifier>().ChainLightningVFX.ClonePrefab(deactivate: false);
      prefab.GetComponent<tk2dSpriteAnimator>().Library = vfx.GetComponent<tk2dSpriteAnimator>().Library;
      prefab.GetComponent<tk2dSpriteAnimator>().DefaultClipId = vfx.GetComponent<tk2dSpriteAnimator>().DefaultClipId;
      prefab.GetComponent<tk2dTiledSprite>().SetSprite(vfx.DefaultAnimation().frames[0].spriteCollection, vfx.DefaultAnimation().frames[0].spriteId);
      return prefab;
  }

  /// <summary>Copy material properties and shader from another material (everything except the texture itself)</summary>
  public static tk2dSpriteDefinition CopyMaterialProps(this tk2dSpriteDefinition def, Material mat)
  {
    Texture oldTex = def.material.mainTexture;
    def.material.CopyPropertiesFromMaterial(mat);
    def.material.mainTexture = oldTex;
    def.material.shader = mat.shader;
    return def;
  }

  /// <summary>Get a random reward of the specified quality for a player, accounting for items the player already has.</summary>
  public static GameObject GetRandomChestRewardOfQuality(this PlayerController player, ItemQuality quality)
  {
      RewardManager rm = GameManager.Instance.RewardManager;
      return rm.GetItemForPlayer(player, Lazy.CoinFlip() ? rm.ItemsLootTable : rm.GunsLootTable, quality, null);
  }

  // TODO: in the future, might need to account for dual wields
  /// <summary>Determine whether a projectile is mastered.</summary>
  public static bool Mastered<T>(this Projectile proj) where T : CwaffGun
  {
    if (!proj)
      return false;
    if (proj.Owner is not PlayerController player)
      return false;
    if (player.CurrentGun is not Gun gun)
      return false;
    if (gun.gameObject.GetComponent<T>() is not T t)
      return false;
    return t.Mastered;
  }

  /// <summary>Reset piercing stastics for a projectile (to reset damage falloff)</summary>
  public static void ResetPiercing(this Projectile p, bool resetHitCount = true)
  {
    p.m_hasPierced = false;
    if (resetHitCount)
      p.m_healthHaverHitCount = 0;
  }
}

