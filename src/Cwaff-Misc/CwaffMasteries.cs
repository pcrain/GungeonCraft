namespace CwaffingTheGungy;

/* TODO:
    - only allow ritual once per run?
*/

public static class CwaffMasteries
{
    /// <summary>Update mastery ritual requirements whenever a gun is manually dropped by the player</summary>
    [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.ForceDropGun))]
    private class ManuallyDropGunPatch
    {
        static void Postfix(PlayerController __instance, Gun g, ref DebrisObject __result)
        {
          if (__instance && __instance.healthHaver && __instance.healthHaver.IsAlive)
            MasteryRitualComponent.PrepareDroppedItemForMasteryRitual(__result);
        }
    }

    /// <summary>Update mastery ritual requirements whenever a gun is picked up by the player</summary>
    [HarmonyPatch(typeof(GunInventory), nameof(GunInventory.AddGunToInventory))]
    private class PickUpGunPatch
    {
        static void Prefix(GunInventory __instance, Gun gun, bool makeActive)
        {
          if (gun.GetComponent<MasteryRitualComponent>() is not MasteryRitualComponent ritComp)
            return;

          ritComp.DisableEffects();
          MasteryRitualComponent._RitualGuns.TryRemove(ritComp);
          UnityEngine.Object.Destroy(ritComp);
        }
    }

    /// <summary>Attempt to complete the mastery ritual whenever the player uses a consumable blank</summary>
    [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.DoConsumableBlank))]
    private class UsedBlankPatch
    {
        static void Prefix(PlayerController __instance)
        {
            if (__instance.Blanks > 0)
              MasteryRitualComponent.UpdateMasteryRitualStatus(blankUser: __instance);
        }
    }

    /// <summary>Allow gun names displayed on the UI to process markup</summary>
    [HarmonyPatch(typeof(GameUIRoot), nameof(GameUIRoot.UpdateGunDataInternal))]
    private class AllowGunDisplayNameMarkupPatch
    {
        static void Prefix(GameUIRoot __instance, PlayerController targetPlayer, GunInventory inventory, int inventoryShift, GameUIAmmoController targetAmmoController, int labelTarget)
        {
          if (__instance.gunNameLabels == null)
            return;
          __instance.gunNameLabels[labelTarget].ProcessMarkup = true; // make sure we can process markup on "Mastered " text
          __instance.gunNameLabels[labelTarget].AutoHeight = true; // make sure we can have multiple lines
        }
    }
}

public class MasteryRitualComponent : MonoBehaviour
{
  private const float _BURN_TIME = 1.0f;
  private const float _SIGIL_SPIN_SPEED = 0.5f;
  private const float _PARTICLE_SPIN_SPEED = 1.4f;

  private static GameObject _CatalystNiceParticleSytem = null;
  private static GameObject _MasteryNiceParticleSytem = null;
  internal static List<MasteryRitualComponent> _RitualGuns = new();

  private Gun _gun = null;
  private ParticleSystem _ps = null;
  private GameObject _sigil = null;
  private float _spinSpeed = 1.0f;

  public static bool CheckRequirementsSatisfiedForMasteryRitual(out MasteryRitualComponent ritualTarget)
  {
    ritualTarget = null;

    if (GameManager.Instance.BestActivePlayer is not PlayerController player)
      return false;

    // Requirement #1: room must be valid and unsealed
    if (player.CurrentRoom is not RoomHandler room || room.IsSealed)
    {
      // Lazy.DebugLog($"Failed req #1: player must be in a real, unsealed room");
      return false; //
    }

    // Requirement #2: there must be exactly 4 guns on the floor in the current room, manually dropped by the player
    CleanUpRitualGunList(); // Housekeeping: make sure we don't have any invalid guns
    List<MasteryRitualComponent> roomGuns = new();
    foreach (MasteryRitualComponent ritComp in _RitualGuns)
      if (ritComp.transform.position.GetAbsoluteRoom() == room)
        roomGuns.Add(ritComp);
    if (roomGuns.Count != 4)
    {
      // Lazy.DebugLog($"Failed req #2: must be 4 manually-dropped guns in the room, have {roomGuns.Count}");
      return false;
    }

    // Requirement #3: 3 of the guns must form a triangle, and the 4th gun must be inside that triangle
    Vector2 p0 = roomGuns[0].gameObject.transform.position.XY();
    Vector2 p1 = roomGuns[1].gameObject.transform.position.XY();
    Vector2 p2 = roomGuns[2].gameObject.transform.position.XY();
    Vector2 p3 = roomGuns[3].gameObject.transform.position.XY();
    MasteryRitualComponent centerGun = null;
    if (PointInTriangle(p0, p1, p2, p3))
      centerGun = roomGuns[0];
    else if (PointInTriangle(p1, p0, p2, p3))
      centerGun = roomGuns[1];
    else if (PointInTriangle(p2, p1, p0, p3))
      centerGun = roomGuns[2];
    else if (PointInTriangle(p3, p1, p2, p0))
      centerGun = roomGuns[3];
    else
    {
      // Lazy.DebugLog($"Failed req #3: 3 of the guns must form a triangle, and the 4th gun must be in the middle");
      return false;
    }

    // Requirement #4: the middle gun must have a mastery
    if (centerGun.GetComponentInChildren<Gun>() is not Gun gun)
      return false; // should never happen, error state

    int masteryId = gun.MasteryTokenId();
    if (masteryId < 0)
    {
      // Lazy.DebugLog($"Failed req #4: center gun {gun.EncounterNameOrDisplayName} does not have a mastery available");
      return false;
    }

    // Requirement #5: the player must not already have the mastery for that gun
    if (Lazy.AnyoneHas(masteryId))
    {
      // Lazy.DebugLog($"Failed req #5: player already has mastery for {gun.EncounterNameOrDisplayName}");
      return false;
    }

    // Requirement #6: at least one of the guns being sacrificed must have a an equal or greater quality than the gun being mastered
    bool worthySacrifice = false;
    int neededQuality = gun.QualityGrade();
    foreach (MasteryRitualComponent comp in roomGuns)
    {
      if (comp == centerGun)
        continue;
      if (comp.GetComponent<Gun>() is not Gun sacrificialGun)
        break; // should never happen, error state
      if (sacrificialGun.QualityGrade() < neededQuality)
        continue;
      worthySacrifice = true;
      break;
    }
    if (!worthySacrifice)
    {
      // Lazy.DebugLog($"Failed req #6: one of the guns being sacrificed must have a quality equal to or greater than the gun being mastered");
      return false;
    }

    // Lazy.DebugLog($"All ritual requirements satisfied");
    ritualTarget = centerGun;
    return true;
  }

  public static void UpdateMasteryRitualStatus(PlayerController blankUser)
  {
    if (CheckRequirementsSatisfiedForMasteryRitual(out MasteryRitualComponent ritualTarget))
    {
      foreach (MasteryRitualComponent ritComp in _RitualGuns)
      {
        if (!blankUser)
        {
          ritComp.EnableEffects(ritComp == ritualTarget);
          continue;
        }
        ritComp.DisableEffects();
        if (ritComp == ritualTarget)
        {
          blankUser.AcquireMastery(ritComp.GetComponent<Gun>());
          if (ritComp.gameObject.GetComponent<CwaffGun>() is CwaffGun cg)
            cg.DoMasteryChecks(blankUser);
        }
        else
          ritComp.BurnAway();
      }
    }
    else
    {
      foreach (MasteryRitualComponent ritComp in _RitualGuns)
        if (ritComp)
          ritComp.DisableEffects();
    }
  }

  // from https://forum.unity.com/threads/point-in-triangle-code-c.42878/
  private static bool PointInTriangle(Vector2 p, Vector2 p0, Vector2 p1, Vector2 p2) {
      var a = .5f * (-p1.y * p2.x + p0.y * (-p1.x + p2.x) + p0.x * (p1.y - p2.y) + p1.x * p2.y);
      var sign = a < 0 ? -1 : 1;
      var s = (p0.y * p2.x - p0.x * p2.y + (p2.y - p0.y) * p.x + (p0.x - p2.x) * p.y) * sign;
      var t = (p0.x * p1.y - p0.y * p1.x + (p0.y - p1.y) * p.x + (p1.x - p0.x) * p.y) * sign;

      return s > 0 && t > 0 && (s + t) < 2 * a * sign;
  }

  // Remove old references from _RitualGuns
  private static void CleanUpRitualGunList()
  {
    List<MasteryRitualComponent> newRitualGuns = new();
    foreach (MasteryRitualComponent ritComp in _RitualGuns)
      if (ritComp)
        newRitualGuns.Add(ritComp);
    _RitualGuns = newRitualGuns;
  }

  public static void PrepareDroppedItemForMasteryRitual(DebrisObject pickup)
  {
    if (!pickup || pickup.GetComponentInChildren<Gun>() is not Gun gun)
      return;
    if (pickup.onGround)
      PrepareForMasteryRitual(pickup);
    else
      pickup.OnGrounded += PrepareForMasteryRitual;
  }

  private static void PrepareForMasteryRitual(DebrisObject pickup)
  {
    if (!pickup)
      return;

    pickup.OnGrounded -= PrepareForMasteryRitual;
    if (pickup.GetComponentInChildren<Gun>() is not Gun gun || !gun.gameObject)
      return; // rare, but has somehow happened at least once

    MasteryRitualComponent ritComp = gun.gameObject.GetOrAddComponent<MasteryRitualComponent>();
    _RitualGuns.Add(ritComp);
    UpdateMasteryRitualStatus(blankUser: null);
  }

  private static GameObject MakeNiceParticleSystem(Color particleColor, float arcSpeed)
  {
      GameObject psBasePrefab = Items.CombinedRifle.AsGun().alternateVolley.projectiles[0].projectiles[0].GetComponent<CombineEvaporateEffect>().ParticleSystemToSpawn;
      GameObject psnewPrefab = UnityEngine.Object.Instantiate(psBasePrefab).RegisterPrefab();
      //NOTE: look at CombineSparks.prefab for reference
      //NOTE: uses shader https://github.com/googlearchive/soundstagevr/blob/master/Assets/third_party/Sonic%20Ether/Shaders/SEParticlesAdditive.shader
      ParticleSystem ps = psnewPrefab.GetComponent<ParticleSystem>();
      // ETGModConsole.Log($"was using shader {psObj.GetComponent<ParticleSystemRenderer>().material.shader.name}");

      float absSpeed = Mathf.Abs(arcSpeed);

      ParticleSystem.MainModule main = ps.main;
      main.duration                = 3600f;
      main.startLifetime           = 0.15f + (1f / absSpeed); // slightly higher than one rotation
      main.startSpeed              = 0.25f;
      main.startSize               = 0.0625f;
      main.scalingMode             = ParticleSystemScalingMode.Local;
      main.startRotation           = 0f;
      main.startRotation3D         = false;
      main.startRotationMultiplier = 0f;
      main.maxParticles            = 200;
      main.startColor              = particleColor;

      ParticleSystem.ForceOverLifetimeModule force = ps.forceOverLifetime;
      force.y = 6f;
      force.z = 15f;

      ParticleSystem.RotationOverLifetimeModule rotl = ps.rotationOverLifetime;
      rotl.enabled = false;

      ParticleSystem.RotationBySpeedModule rots = ps.rotationBySpeed;
      rots.enabled = false;

      Gradient g = new Gradient();
      g.SetKeys(
          new GradientColorKey[] { new GradientColorKey(particleColor, 0.0f), new GradientColorKey(particleColor, 1.0f) },
          new GradientAlphaKey[] { new GradientAlphaKey(1f, 0.0f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1.0f) }
      );
      ParticleSystem.ColorOverLifetimeModule colm = ps.colorOverLifetime;
      colm.color = new ParticleSystem.MinMaxGradient(g); // looks jank

      ParticleSystem.EmissionModule em = ps.emission;
      em.rateOverTime = 30f * absSpeed;

      ParticleSystemRenderer psr = psnewPrefab.GetComponent<ParticleSystemRenderer>();
      psr.material.SetFloat("_InvFade", 3.0f);
      psr.material.SetFloat("_EmissionGain", 0.9f);
      psr.material.SetColor("_EmissionColor", particleColor);
      psr.material.SetColor("_DiffuseColor", particleColor);
      psr.sortingLayerName = "Foreground";

      ParticleSystem.ShapeModule shape = ps.shape;
      shape.randomDirectionAmount = 0f;
      shape.alignToDirection = true;

      // shape.scale = new Vector3(bounds.extents.x * 2f, bounds.extents.y * 2f, 0.125f);  //NOTE: for Box- / sprite-shaped emitters
      // shape.shapeType = ParticleSystemShapeType.Donut;
      shape.shapeType = ParticleSystemShapeType.Circle;
      if (shape.shapeType == ParticleSystemShapeType.Donut)
      {
        shape.scale           = new Vector3(1f, 1f, 1f);
        shape.radiusThickness = 0.0625f;
        shape.radiusMode      = ParticleSystemShapeMultiModeValue.Loop;
        shape.donutRadius     = 0.25f;
        shape.radius          = 0.25f + shape.donutRadius;
        shape.rotation        = new Vector3(0f, 0f, 0f);
        shape.arc             = 360f;
        shape.arcMode         = ParticleSystemShapeMultiModeValue.Loop;
      }
      else if (shape.shapeType == ParticleSystemShapeType.Circle)
      {
        shape.randomDirectionAmount = 0f;
        shape.alignToDirection = false;
        shape.scale           = Vector3.one;
        shape.radiusThickness = 0.25f;
        shape.radiusMode      = ParticleSystemShapeMultiModeValue.Loop;
        shape.radius          = 0.25f + shape.radiusThickness;
        shape.rotation        = Vector3.zero;
        shape.arc             = 360f;
        shape.arcMode         = ParticleSystemShapeMultiModeValue.Loop;
        shape.arcSpeed        = arcSpeed;
      }

      return psnewPrefab;
  }

  public void EnableEffects(bool isMasteryTarget)
  {
      if (!this.isActiveAndEnabled)
      {
        // Lazy.DebugWarn("Enabling effects on inactive ritual component");
        this.enabled = true;
      }
      if (!this._gun)
        this._gun = base.GetComponent<Gun>();
      if (!this._gun || !this._gun.sprite)
        return;

      _CatalystNiceParticleSytem ??= MakeNiceParticleSystem(new Color(0.75f, 0.75f, 0.5f), arcSpeed: _PARTICLE_SPIN_SPEED);
      _MasteryNiceParticleSytem  ??= MakeNiceParticleSystem(new Color(1.0f, 0.75f, 0.75f), arcSpeed: -_PARTICLE_SPIN_SPEED);

      DisableEffects();

      if (isMasteryTarget)
        base.gameObject.Play("mastery_ritual_activate_sound");
      this._spinSpeed = isMasteryTarget ? -_SIGIL_SPIN_SPEED : _SIGIL_SPIN_SPEED;

      GameObject psObj         = UnityEngine.Object.Instantiate(isMasteryTarget ? _MasteryNiceParticleSytem : _CatalystNiceParticleSytem);
      psObj.transform.position = this._gun.sprite.WorldCenter;
      psObj.transform.parent   = base.gameObject.transform;

      this._ps                         = psObj.GetComponent<ParticleSystem>();
      ParticleSystem.ShapeModule shape = this._ps.shape;
      Bounds bounds                    = this._gun.sprite.GetBounds();
      shape.radius                     = 0.25f + shape.radiusThickness + Mathf.Max(bounds.extents.x, bounds.extents.y);
      ParticleSystem.EmissionModule em = this._ps.emission;
      em.rateOverTime = 32f * shape.radius * _PARTICLE_SPIN_SPEED; // adjust for larger guns

      this._sigil = SpawnManager.SpawnVFX(VFX.MasterySigil, psObj.transform.position, Quaternion.identity, ignoresPools: true);
      this._sigil.SetAlphaImmediate(0.5f);
      this._sigil.transform.parent = base.gameObject.transform;
      tk2dSprite sigilSprite = this._sigil.GetComponent<tk2dSprite>();
      sigilSprite.HeightOffGround = -5f;
      sigilSprite.UpdateZDepth();
  }

  public void DisableEffects()
  {
    if (this._ps)
    {
      this._ps.Stop(true);
      this._ps.gameObject.transform.parent = null;
      this._ps.gameObject.ExpireIn(3f);
      this._ps = null;
    }
    if (this._sigil)
    {
      this._sigil.transform.parent = null;
      UnityEngine.Object.Destroy(this._sigil);
      this._sigil = null;
    }
  }

  private void Update()
  {
    if (!this._gun)
      this._gun = base.GetComponent<Gun>();
    if (!this._gun || this._gun.CurrentOwner != null)
    {
      // ETGModConsole.Log($"selfdestructing");
      UnityEngine.Object.Destroy(this);
      return;
    }
    if (this._sigil)
      this._sigil.transform.rotation = (this._spinSpeed * 360f * BraveTime.ScaledTimeSinceStartup).EulerZ();
  }

  private void OnDestroy()
  {
    DisableEffects();
    _RitualGuns.TryRemove(this);
    UpdateMasteryRitualStatus(blankUser: null);
  }

  private void Start()
  {
    this._gun = base.GetComponent<Gun>();
  }

  private void BurnAway()
  {
    if (base.GetComponent<Gun>() is Gun gun)
    {
      // Deregister ourselves as a room interactable if necessary
      RoomHandler room = base.gameObject.transform.position.GetAbsoluteRoom();
      if (room != null && room.IsRegistered(gun))
        room.DeregisterInteractable(gun);
      else
        RoomHandler.unassignedInteractableObjects.TryRemove(gun);

      gun.sprite.DuplicateInWorldAsMesh().Dissipate(time: 2.5f, amplitude: 5f, progressive: true);
    }

    // Destroy our gun component
    if (base.transform.parent is Transform parent)
      UnityEngine.Object.Destroy(parent.gameObject);
    else // shouldn't reach this
      UnityEngine.Object.Destroy(base.gameObject);
  }

  private static IEnumerator BurnAway_CR(tk2dBaseSprite sprite)
  {
      // Set up shaders
      // SpriteOutlineManager.RemoveOutlineFromSprite(sprite);
      sprite.renderer.material.DisableKeyword("TINTING_OFF");
      sprite.renderer.material.EnableKeyword("TINTING_ON");
      sprite.renderer.material.DisableKeyword("EMISSIVE_OFF");
      sprite.renderer.material.EnableKeyword("EMISSIVE_ON");
      sprite.renderer.material.DisableKeyword("BRIGHTNESS_CLAMP_ON");
      sprite.renderer.material.EnableKeyword("BRIGHTNESS_CLAMP_OFF");
      sprite.renderer.material.SetFloat("_EmissiveThresholdSensitivity", 5f);
      sprite.renderer.material.SetFloat(CwaffVFX._EmissiveColorPowerId, 1f);
      sprite.renderer.material.shader = ShaderCache.Acquire("Brave/LitCutoutUber");
      sprite.renderer.material.SetFloat(CwaffVFX._EmissivePowerId, 10f);

      // Fade away
      for (float elapsed = 0f; elapsed < _BURN_TIME; elapsed += BraveTime.DeltaTime)
      {
          float percentDone = elapsed / _BURN_TIME;
          sprite.renderer.material.SetFloat("_BurnAmount", percentDone);
          yield return null;
      }
      UnityEngine.Object.Destroy(sprite.gameObject);
      yield break;
  }
}
