using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using MonoMod.RuntimeDetour;
using Brave.BulletScript;

using Dungeonator;
using ItemAPI;

namespace CwaffingTheGungy
{
  public static class Extensions
  {
    public class Expiration : MonoBehaviour  // destroy a game object after a fixed amount of time, with optional fadeout
    {
      public void ExpireIn(float seconds, float fadeFor = 0f, float startAlpha = 1f)
      {
        this.StartCoroutine(Expire(seconds, fadeFor, startAlpha));
      }

      private IEnumerator Expire(float seconds, float fadeFor = 0f, float startAlpha = 1f)
      {
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
          this.gameObject.SetAlpha(startAlpha * Mathf.Min(1f,lifeLeft / fadeFor));
          yield return null;
        }
        UnityEngine.Object.Destroy(this.gameObject);
        yield break;
      }
    }

    // Add an expiration timer to a GameObject
    public static void ExpireIn(this GameObject self, float seconds, float fadeFor = 0f, float startAlpha = 1f)
    {
      self.GetOrAddComponent<Expiration>().ExpireIn(seconds, fadeFor, startAlpha);
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

    // Determine whether a Vector is within a degree tolerance of a floating point angle
    public static bool IsNearAngle(this Vector2 v, float angle, float tolerance)
    {
      float vangle = v.ToAngle().Clamp360();
      float cangle = angle.Clamp360();
      float minangle = (cangle - tolerance).Clamp360();
      float maxangle = (cangle + tolerance).Clamp360();
      if (minangle < maxangle)
        return minangle < vangle && vangle < maxangle;
      return minangle < vangle || vangle < maxangle; // note the || operator
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
    public static void SetHorizontalImpactVFX(this Projectile p, VFXPool vfx)
    {
      p.hitEffects.tileMapHorizontal = vfx;  // necessary
      p.hitEffects.deathTileMapHorizontal = vfx; // optional
    }

    // Set a gun's horizontal impact VFX
    public static void SetHorizontalImpactVFX(this Gun gun, VFXPool vfx)
    {
      gun.DefaultModule.projectiles[0].SetHorizontalImpactVFX(vfx);
    }

    // Set a projectile's vertical impact VFX
    public static void SetVerticalImpactVFX(this Projectile p, VFXPool vfx)
    {
      p.hitEffects.tileMapVertical = vfx;  // necessary
      p.hitEffects.deathTileMapVertical = vfx; // optional
    }

    // Set a gun's vertical impact VFX
    public static void SetVerticalImpactVFX(this Gun gun, VFXPool vfx)
    {
      gun.DefaultModule.projectiles[0].SetVerticalImpactVFX(vfx);
    }

    // Set a projectile's enemy impact VFX
    public static void SetEnemyImpactVFX(this Projectile p, VFXPool vfx)
    {
      p.hitEffects.enemy = vfx;  // necessary
      p.hitEffects.deathEnemy = vfx; // optional
    }

    // Set a gun's enemy impact VFX
    public static void SetEnemyImpactVFX(this Gun gun, VFXPool vfx)
    {
      gun.DefaultModule.projectiles[0].SetEnemyImpactVFX(vfx);
    }

    // Set a projectile's midair impact / death VFX
    public static void SetAirImpactVFX(this Projectile p, VFXPool vfx)
    {
      p.hitEffects.suppressMidairDeathVfx = false;
      p.hitEffects.overrideMidairDeathVFX = vfx.effects[0].effects[0].effect;
    }

    // Set a gun's midair impact / death VFX
    public static void SetAirImpactVFX(this Gun gun, VFXPool vfx)
    {
      gun.DefaultModule.projectiles[0].SetAirImpactVFX(vfx);
    }

    // Check if an enemy is hostile
    public static bool IsHostile(this AIActor e, bool canBeDead = false)
    {
      HealthHaver h = e?.healthHaver;
      return e && !e.IsGone && !e.IsHarmlessEnemy && h && (canBeDead || (h.IsAlive && !h.IsDead)) && !h.isPlayerCharacter;
    }

    // Check if an enemy is hostile and a non-boss
    public static bool IsHostileAndNotABoss(this AIActor e, bool canBeDead = false)
    {
      HealthHaver h = e?.healthHaver;
      return e && !e.IsGone && !e.IsHarmlessEnemy && h && !h.IsBoss && !h.IsSubboss &&  (canBeDead || (h.IsAlive && !h.IsDead)) && !h.isPlayerCharacter;
    }

    // Set the Alpha of a GameObject's sprite
    public static void SetAlpha(this GameObject g, float a)
    {
      g.GetComponent<Renderer>()?.SetAlpha(a);
    }

    // Set the Alpha of a GameObject's sprite immediately and avoid the 1-frame opacity delay upon creation
    public static void SetAlphaImmediate(this GameObject g, float a)
    {
      g.GetComponent<Renderer>()?.SetAlpha(a);
      g.GetComponent<tk2dSpriteAnimator>()?.LateUpdate();
    }

    // Add emissiveness to a game object
    public static void SetGlowiness(this GameObject g, float a)
    {
      if (g.GetComponent<tk2dSprite>() is not tk2dSprite sprite)
        return;
      sprite.SetGlowiness(a);
    }

    public static void SetGlowiness(this tk2dSprite sprite, float a, Color? color = null, bool clampBrightness = true)
    {
      sprite.usesOverrideMaterial = true;
      Material m = sprite.renderer.material;
      m.shader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTintableTiltedCutoutEmissive");
      if (!clampBrightness)
      {
        m.DisableKeyword("BRIGHTNESS_CLAMP_ON");
        m.EnableKeyword("BRIGHTNESS_CLAMP_OFF");
      }
      m.SetFloat("_EmissivePower", a);
      if (color is Color c)
      {
        m.SetFloat("_EmissiveColorPower", 1.55f);
        m.SetColor("_EmissiveColor", c);
      }
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

    // Shuffle the elements in a list (or any other enumerable)
    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1) {
            n--;
            int k = UnityEngine.Random.Range(0, n);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
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
        case PickupObject.ItemQuality.S: return 5;
        case PickupObject.ItemQuality.A: return 4;
        case PickupObject.ItemQuality.B: return 3;
        case PickupObject.ItemQuality.C: return 2;
        case PickupObject.ItemQuality.D: return 1;
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
        gun.gunSwitchGroup = (ItemHelper.Get(Items.Blasphemy) as Gun).gunSwitchGroup; // quietest gun audio

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
    public static void SetChargeAudio(this Gun gun, string audio = "", int frame = 0)
    {
      gun.SetGunAudio(name: gun.chargeAnimation, audio: audio, frame: frame);
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
      return roomPixelRect.Inset(1, 1, _ROOM_PIXEL_FUDGE_FACTOR, 1);
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

    public static bool FullyWithin(this PixelCollider self, Rect other)
    {
      return new Rect(self.MinX, self.MinY, self.Dimensions.X, self.Dimensions.Y).FullyWithin(other);
    }

    public static bool FullyWithin(this Rect self, Rect other)
    {
      bool xwithin = self.xMin > other.xMin && self.xMax < other.xMax;
      bool ywithin = self.yMin > other.yMin && self.yMax < other.yMax;
      return xwithin && ywithin;
    }
  }
}
