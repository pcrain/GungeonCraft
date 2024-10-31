namespace CwaffingTheGungy;

public interface ICwaffItem
{
  public void OnFirstPickup(PlayerController player);
}

public abstract class CwaffPassive : PassiveItem, ICwaffItem
{
  public override void Pickup(PlayerController player)
  {
    if (!this.m_pickedUpThisRun) // must come before base.Pickup()
      OnFirstPickup(player);
    base.Pickup(player);
  }

  public virtual void OnFirstPickup(PlayerController player)
  {

  }
}

public abstract class CwaffActive: PlayerItem, ICwaffItem
{
  public override void Pickup(PlayerController player)
  {
    if (!this.m_pickedUpThisRun) // must come before base.Pickup()
      OnFirstPickup(player);
    base.Pickup(player);
  }

  public virtual void OnFirstPickup(PlayerController player)
  {

  }
}

public abstract class CwaffGun: GunBehaviour, ICwaffItem, IGunInheritable/*, ILevelLoadedListener*/
{
  private const string _DEFAULT_BARREL_OFFSET = "__default";
  private static Dictionary<string, Dictionary<string, List<Vector3>>> _BarrelOffsetCache = new();

  private bool                              _hasReloaded               = true;  // whether we have finished reloading
  private bool                              _usesDynamicBarrelPosition = false; // whether the gun uses dynamic barrel offsets
  private Dictionary<string, List<Vector3>> _barrelOffsets             = null;  // list of dynamic barrel offsets for each of a gun's animations
  private Vector3                           _defaultBarrelOffset       = Vector3.zero; // the default barrel offset for guns with dynamic offsets
  private ModuleShootData                   _cachedShootData           = null; // cached firing data for getting info on extant beams, etc.

  protected float                           _spinupRemaining           = 0.0f; // the remaining time a gun must spin up before firing

  public  bool                              hideAmmo                   = false;  // whether our ammo display is visible
  public  bool                              suppressReloadLabel        = false;  // whether to suppress reload label when out of ammo
  public  float                             percentSpeedWhileCharging  = 1.0f;   // max relative speed the player can move while charging the gun
  public  bool                              preventRollingWhenCharging = false;  // whether holding the gun prevents the player from dodge rolling
  public  float                             spinupTime                 = 0.0f;   // the amount of time it takes an automatic weapon to start firing
  public  string                            spinupSound                = null;   // the sound to play while an automatic gun is spinning up
  public  bool                              continuousFireAnimation    = false;  // if true, don't restart shooting animation when firing bullet

  public static void SetUpDynamicBarrelOffsets(Gun gun)
  {
    var d = _BarrelOffsetCache[gun.GetUnmodifiedDisplayName()] = new();
    //WARNING: can't do idle animation since it breaks loading with trimmed sprites
    SetUpDefaultDynamicBarrelOffset(d, gun);
    SetUpDynamicBarrelOffsetsForAnimation(d, gun, gun.chargeAnimation);
    SetUpDynamicBarrelOffsetsForAnimation(d, gun, gun.reloadAnimation);
    SetUpDynamicBarrelOffsetsForAnimation(d, gun, gun.shootAnimation);
  }

  private static void SetUpDefaultDynamicBarrelOffset(Dictionary<string, List<Vector3>> d, Gun gun)
  {
    d[_DEFAULT_BARREL_OFFSET] = new(){ gun.barrelOffset.localPosition };
  }

  private static void SetUpDynamicBarrelOffsetsForAnimation(Dictionary<string, List<Vector3>> d, Gun gun, string anim)
  {
    if (!string.IsNullOrEmpty(anim))
      d[anim] = gun.GetBarrelOffsetsForAnimation(anim);
  }

  public override void OnPlayerPickup(PlayerController player)
  {
    if (!this.EverPickedUp)
      OnFirstPickup(player);
    base.OnPlayerPickup(player);

    player.GunChanged -= OnGunsChanged;
    player.GunChanged += OnGunsChanged;

    // Load dynamic barrel offsets if we have any registered
    if (_BarrelOffsetCache.TryGetValue(this.gun.GetUnmodifiedDisplayName(), out var barrelOffsets))
    {
      this._usesDynamicBarrelPosition = true;
      this._barrelOffsets             = barrelOffsets;
      this._defaultBarrelOffset       = barrelOffsets[_DEFAULT_BARREL_OFFSET][0];
    }
  }

  /// <summary>Called the first time a gun is picked up by a player during a run</summary>
  public virtual void OnFirstPickup(PlayerController player)
  {
    //NOTE: fix a vanilla issue where guns' associatedItemChanceMods data is ignored
    if (this.gun.associatedItemChanceMods != null)
      foreach(LootModData mod in this.gun.associatedItemChanceMods)
        player.lootModData.Add(mod);
  }

  /// <summary>
  /// OnGunsChanged() is called when the player changes the current gun.
  /// </summary>
  /// <param name="previous">The previous current gun.</param>
  /// <param name="current">The new current gun.</param>
  /// <param name="newGun">True if the gun was changed because player picked up a new gun.</param>
  public void OnGunsChanged(Gun previous, Gun current, bool newGun)
  {
    if (previous != this.gun && current == this.gun)
        this.OnSwitchedToThisGun();
    if (previous == this.gun && current != this.gun)
        this.OnSwitchedAwayFromThisGun();
  }

  /// <summary>
  /// OnSwitchedToThisGun() when the player switches to this behaviour's affected gun.
  /// </summary>
  public virtual void OnSwitchedToThisGun()
  {
    this.gun.PreventNormalFireAudio = true;
    if (gun.spriteAnimator.GetClipByName(gun.shootAnimation) is tk2dSpriteAnimationClip clip)
      this.gun.OverrideNormalFireAudioEvent = clip.frames[0].eventAudio;
  }

  /// <summary>
  /// OnSwitchedToThisGun() when the player switches away from this behaviour's affected gun.
  /// </summary>
  public virtual void OnSwitchedAwayFromThisGun()
  {
    foreach (CwaffReticle ret in base.gameObject.GetComponents<CwaffReticle>())
      ret.HideImmediately();
  }

  public override void OnDroppedByPlayer(PlayerController player)
  {
      base.OnDroppedByPlayer(player);
      foreach (CwaffReticle ret in base.gameObject.GetComponents<CwaffReticle>())
        ret.HideImmediately();
  }

  public override void Update()
  {
    if (this.gun && !this.gun.IsReloading)
        this._hasReloaded = true;
    if (this._usesDynamicBarrelPosition)
      AdjustBarrelPosition();
  }

  private void AdjustBarrelPosition()
  {
    tk2dSpriteAnimator animator = this.gun.spriteAnimator;
    if (this._barrelOffsets.TryGetValue(animator.currentClip.name, out List<Vector3> offsets))
      this.gun.barrelOffset.localPosition = offsets[animator.CurrentFrame];
    else
      this.gun.barrelOffset.localPosition = this._defaultBarrelOffset;
    if (this.gun.sprite.FlipY)
      this.gun.barrelOffset.localPosition = this.gun.barrelOffset.localPosition.WithY(-this.gun.barrelOffset.localPosition.y);
  }

  public override void OnReloadPressed(PlayerController player, Gun gun, bool manual)
  {
    if (this._hasReloaded && gun.IsReloading)
    {
      OnActualReload(player, gun, manual);
      this._hasReloaded = false;
    }
    if (player.AcceptingNonMotionInput && !gun.IsReloading && manual && (gun.ClipShotsRemaining >= Mathf.Min(gun.ClipCapacity, gun.CurrentAmmo)))
    {
      OnFullClipReload(player, gun);
    }
  }

  /// <summary>Called when the player actually initiates an ammo-repleneshing reload</summary>
  public virtual void OnActualReload(PlayerController player, Gun gun, bool manual)
  {
  }

  /// <summary>Called when the player manually initiates a reload with a full clip</summary>
  public virtual void OnFullClipReload(PlayerController player, Gun gun)
  {
  }

  /// <summary>Determines the fire rate of the gun.</summary>
  public virtual float GetDynamicFireRate() => 1.0f;

  /// <summary>Use Natascha's custom rate of fire spinup code</summary>
  /// <remarks>Only works if GainsRateOfFireAsContinueAttack is true</remarks>
  [HarmonyPatch(typeof(Gun), nameof(Gun.HandleModuleCooldown), MethodType.Enumerator)]
  private class DynamicSpinupPatch
  {
      [HarmonyILManipulator]
      private static void DynamicSpinupIL(ILContext il, MethodBase original)
      {
          ILCursor cursor = new ILCursor(il);
          Type ot = original.DeclaringType;

          if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchAdd())) // immediately after the first add is where we're looking for
              return;

          cursor.Emit(OpCodes.Ldarg_0);  // load enumerator type
          cursor.Emit(OpCodes.Ldfld, ot.GetEnumeratorField("$this")); // load actual "$this" field
          cursor.CallPrivate(typeof(DynamicSpinupPatch), nameof(ModifyRateOfFire));
      }

      private static float ModifyRateOfFire(float oldFireRate, Gun gun)
      {
          return oldFireRate * ((gun.GetComponent<CwaffGun>() is CwaffGun cg) ? cg.GetDynamicFireRate() : 1f);
      }
  }

  /// <summary>Query whether the gun is currently spinning up</summary>
  protected bool IsSpinningUp() => _spinupRemaining < spinupTime && _spinupRemaining > 0;
  /// <summary>Query whether the gun has been spun up</summary>

  protected bool IsSpunUp() => _spinupRemaining <= 0;

  /// <summary>Determines an additional dynamic accuracy multiplier for the gun.</summary>
  public virtual float GetDynamicAccuracy() => 1.0f;

  // NOTE: called by patch in CwaffPatches
  internal static float ModifyAccuracy(float oldSpread, Gun gun)
  {
      return oldSpread * ((gun.GetComponent<CwaffGun>() is CwaffGun cg) ? cg.GetDynamicAccuracy() : 1f);
  }

  // public void BraveOnLevelWasLoaded()
  // {
  // }

  protected void ClearCachedShootData() => this._cachedShootData = null;

  protected BeamController GetExtantBeam()
  {
      if (this._cachedShootData == null)
      {
          if (!this.gun || !this.gun.IsFiring || this.gun.m_moduleData == null || this.gun.DefaultModule == null)
              return null;
          if (!this.gun.m_moduleData.TryGetValue(this.gun.DefaultModule, out ModuleShootData data))
              return null;
          this._cachedShootData = data;
      }
      return this._cachedShootData.beam;
  }

  /// <summary>Completely hides a gun's ammo like blasphemy</summary>
  [HarmonyPatch(typeof(GameUIAmmoController), nameof(GameUIAmmoController.UpdateUIGun))]
  private class GameUIAmmoControllerUpdateUIGunPatch
  {
      [HarmonyILManipulator]
      private static void GameUIAmmoControllerUpdateUIGunIL(ILContext il)
      {
          ILCursor cursor = new ILCursor(il);
          if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<Gun>("IsHeroSword")))
              return;

          cursor.Emit(OpCodes.Ldloc_0);
          cursor.CallPrivate(typeof(GameUIAmmoControllerUpdateUIGunPatch), nameof(CheckHideAmmo));
      }

      private static bool CheckHideAmmo(bool oldValue, Gun gun)
      {
        if (oldValue)
          return true;
        if (gun.gameObject.GetComponent<CwaffGun>() is not CwaffGun cg)
          return false;
        return cg.hideAmmo;
      }
  }

  /// <summary>Thrown guns don't count as dropped or destroyed, leading to bugs with certain guns that handle cleanup when dropped, so handle it manually</summary>
  [HarmonyPatch(typeof(Gun), nameof(Gun.ThrowGun))]
  private class ThrowGunCountsAsDroppedGunPatch
  {
      static void Prefix(Gun __instance)
      {
        if ((__instance.GetComponent<CwaffGun>() is CwaffGun cg) && (cg.PlayerOwner is PlayerController player))
          cg.OnDroppedByPlayer(player);
      }
  }

  /// <summary>Patch for detecting whether a secondary reload button is pressed</summary>
  [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.HandlePlayerInput))]
  private class SecondaryReloadPatch
  {
      [HarmonyILManipulator]
      private static void SecondaryReloadIL(ILContext il)
      {
          ILCursor cursor = new ILCursor(il);
          if (!cursor.TryGotoNext(MoveType.After,
              instr => instr.MatchLdfld<GungeonActions>("ReloadAction"),
              instr => instr.MatchCallvirt<InControl.OneAxisInputControl>("get_WasPressed")))
              return;
          cursor.Emit(OpCodes.Ldarg_0);
          cursor.CallPrivate(typeof(SecondaryReloadPatch), nameof(CheckSecondaryReload));
      }

      private static bool CheckSecondaryReload(bool oldValue, PlayerController player)
      {
        return oldValue || player.SecondaryReloadPressed();
      }
  }

  /// <summary>Patch to prevent dodge rolls when holding a specific gun</summary>
  [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.HandleStartDodgeRoll))]
  private class PlayerControllerHandleStartDodgeRollPatch
  {
      static bool Prefix(PlayerController __instance, Vector2 direction, ref bool __result)
      {
          if (__instance.CurrentGun is not Gun gun)
            return true; // call the original method
          if (!gun.IsCharging)
            return true; // call the original method
          if (gun.GetComponent<CwaffGun>() is not CwaffGun cg)
            return true; // call the original method
          if (!cg.preventRollingWhenCharging)
            return true; // call the original method
          __result = false; // change the original result
          return false;    // skip the original method
      }
  }

  /// <summary>Patch to prevent movement when holding a specific gun</summary>
  [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.AdjustInputVector))]
  private class PlayerControllerAdjustInputVectorPatch
  {
      static void Postfix(PlayerController __instance, Vector2 rawInput, float cardinalMagnetAngle, float ordinalMagnetAngle, ref Vector2 __result)
      {
          if (__instance.CurrentGun is not Gun gun)
            return;
          if (gun.GetComponent<CwaffGun>() is not CwaffGun cg)
            return;
          if (!gun.IsCharging && cg._spinupRemaining == cg.spinupTime)
            return;
          __result = cg.percentSpeedWhileCharging * __result; // change the original result
      }
  }

  /// <summary>Patch to prevent guns from being thrown</summary>
  [HarmonyPatch(typeof(Gun), nameof(Gun.PrepGunForThrow))]
  private class GunPrepGunForThrowPatch
  {
      static bool Prefix(Gun __instance)
      {
          return !__instance.gameObject.GetComponent<Unthrowable>();
      }
  }

  /// <summary>Patch to prevent guns from flashing the reload label</summary>
  [HarmonyPatch(typeof(GameUIRoot), nameof(GameUIRoot.InformNeedsReload))]
  private class GameUIRootInformNeedsReloadPatch
  {
      static bool Prefix(GameUIRoot __instance, PlayerController attachPlayer, Vector3 offset, float customDuration, string customKey)
      {
        if (attachPlayer && attachPlayer.CurrentGun is Gun gun && gun.gameObject.GetComponent<CwaffGun>() is CwaffGun cg)
          return !cg.suppressReloadLabel; // skip the original method if we are suppressing reload labels
        return true;     // call the original method
      }
  }

  /// <summary>Patch to allow the ammo display to show more than 125 shots for non-beam weapons</summary>
  [HarmonyPatch(typeof(GameUIAmmoController), nameof(GameUIAmmoController.UpdateAmmoUIForModule))]
  private class ShowMoreThan125ShotsPatch
  {
      [HarmonyILManipulator]
      private static void ShowMoreThan125ShotsIL(ILContext il)
      {
          ILCursor cursor = new ILCursor(il);
          if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcI4(125)))
            return;

          cursor.Emit(OpCodes.Ldarg, 8); // load currentGun
          cursor.CallPrivate(typeof(ShowMoreThan125ShotsPatch), nameof(CheckGun));
      }

      private static int CheckGun(int oldCount, Gun gun)
      {
        if (gun && gun.gameObject.GetComponent<CwaffGun>())
          return 500;
        return oldCount;
      }
  }

  // REFACTOR: this should allow individual guns to opt out and be relocated to Alexandria eventually
  /// <summary>Patch to prevent duct tape from duct taping multiple modules at once</summary>
  [HarmonyPatch(typeof(DuctTapeItem), nameof(DuctTapeItem.CombineVolleys))]
  private class DuctTapeItemCombineVolleysPatch
  {
      [HarmonyILManipulator]
      private static void DuctTapeItemCombineVolleysIL(ILContext il)
      {
          ILCursor cursor = new ILCursor(il);
          ILLabel loopEnd = null;
          for (int i = 0; i < 2; ++i) // we're interested in the target of the SECOND occurrence of brfalse
            if (!cursor.TryGotoNext(MoveType.After,
              instr => instr.MatchLdfld<ProjectileModule>("runtimeGuid"),
              instr => instr.MatchBrfalse(out loopEnd) ))
                return;

          cursor.Index = 0;
          if (!cursor.TryGotoNext(MoveType.Before,
            instr => instr.MatchCallvirt<Gun>("get_RawSourceVolley"),
            instr => instr.MatchLdfld<ProjectileVolleyData>("projectiles") ))
              return;

          // arg1 == mergeGun is already on the stack and needs to be restored after branch
          cursor.Emit(OpCodes.Ldloc_1); // load i
          cursor.CallPrivate(typeof(DuctTapeItemCombineVolleysPatch), nameof(ShouldDuctTapeModule));
          cursor.Emit(OpCodes.Brfalse, loopEnd); // skip the current loop iteration
          cursor.Emit(OpCodes.Ldarg_1); // restore mergeGun on stack if we don't skip the current loop iteration
      }

      private static bool ShouldDuctTapeModule(Gun gun, int i)
      {
        if (!gun.Volley || !gun.Volley.ModulesAreTiers || gun.CurrentStrengthTier == i)
          return true;
        if (!gun.gameObject.GetComponent<CwaffGun>())
          return true; // don't affect vanilla guns
        return false;
      }
  }

  /// <summary>Allow automatic guns to have a spin up time</summary>
  [HarmonyPatch]
  private class GunHandleInitialGunShootPatch
  {
      [HarmonyPatch(typeof(Gun), nameof(Gun.HandleInitialGunShoot), typeof(ProjectileModule), typeof(ProjectileData), typeof(GameObject))]
      [HarmonyPatch(typeof(Gun), nameof(Gun.HandleInitialGunShoot), typeof(ProjectileVolleyData), typeof(ProjectileData), typeof(GameObject))]
      [HarmonyILManipulator]
      private static void GunHandleInitialGunShootIL(ILContext il)
      {
          ILCursor cursor = new ILCursor(il);
          if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<ModuleShootData>(nameof(ModuleShootData.onCooldown))))
            return;
          cursor.Emit(OpCodes.Ldarg_0);
          cursor.CallPrivate(typeof(GunHandleInitialGunShootPatch), nameof(HandleAutomaticGunWindup));
      }

      private static bool HandleAutomaticGunWindup(bool wasOnCooldown, Gun gun)
      {
        if (wasOnCooldown)
          return true;
        if (gun.CurrentOwner is not PlayerController player)
          return false;
        if (gun.gameObject.GetComponent<CwaffGun>() is not CwaffGun cg)
          return false;
        if (cg.spinupTime <= 0.0f)
          return false; // don't have spin up
        BraveInput currentBraveInput = BraveInput.GetInstanceForPlayer(player.PlayerIDX);
        if (currentBraveInput.GetButtonDown(GungeonActions.GungeonActionType.Shoot))
        {
          // System.Console.WriteLine($"consuming fire");
          currentBraveInput.ConsumeButtonDown(GungeonActions.GungeonActionType.Shoot);
        }
        cg._spinupRemaining = Mathf.Max(cg._spinupRemaining - BraveTime.DeltaTime, 0f);
        if (!string.IsNullOrEmpty(cg.spinupSound))
          cg.LoopSoundIf(cg._spinupRemaining > 0, cg.spinupSound);
        // System.Console.WriteLine($"on cooldown? {cg._spinupRemaining > 0}");
        return cg._spinupRemaining > 0;
      }
  }

  /// <summary>Reset spinup for automatic guns</summary>
  [HarmonyPatch]
  private class PlayerControllerHandleGunFiringInternalPatch
  {
      [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.HandleGunFiringInternal))]
      [HarmonyPrefix]
      static void HandleGunFiringInternalPrefix(PlayerController __instance, Gun targetGun, BraveInput currentBraveInput, bool isSecondary)
      {
        if (!targetGun || targetGun.gameObject.GetComponent<CwaffGun>() is not CwaffGun cg)
          return;

        if (currentBraveInput.GetButtonUp(GungeonActions.GungeonActionType.Shoot))
        {
          currentBraveInput.ConsumeButtonUp(GungeonActions.GungeonActionType.Shoot);
          // System.Console.WriteLine($"resetting spinup");
          cg._spinupRemaining = cg.spinupTime;
        }
      }

      [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.HandleGunFiringInternal))]
      [HarmonyILManipulator]
      private static void PlayerControllerHandleGunFiringInternalIL(ILContext il)
      {
          ILCursor cursor = new ILCursor(il);
          if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt<BraveInput>(nameof(BraveInput.GetButtonDown))))
            return;
          cursor.Emit(OpCodes.Ldarg_1); // Gun
          cursor.CallPrivate(typeof(PlayerControllerHandleGunFiringInternalPatch), nameof(HandleAutomaticGunWindup));
      }

      private static bool HandleAutomaticGunWindup(bool wasFiring, Gun gun)
      {
        if (!gun || gun.gameObject.GetComponent<CwaffGun>() is not CwaffGun cg || cg.spinupTime <= 0.0f)
          return wasFiring; // no adjustment
        if (gun.CurrentOwner is not PlayerController player)
          return wasFiring; // no adjustment

        BraveInput currentBraveInput = BraveInput.GetInstanceForPlayer(player.PlayerIDX);
        if (currentBraveInput.GetButtonDown(GungeonActions.GungeonActionType.Shoot))
        {
          // System.Console.WriteLine($"consuming fire");
          currentBraveInput.ConsumeButtonDown(GungeonActions.GungeonActionType.Shoot);
        }

        bool isFiring = wasFiring || currentBraveInput.GetButton(GungeonActions.GungeonActionType.Shoot);
        if (!isFiring)
          cg._spinupRemaining = cg.spinupTime;
        return isFiring;
      }
  }

  /// <summary>Allow guns to not reset their shooting animation with every bullet fired</summary>
  [HarmonyPatch]
  private class GunShootAnimationPatch
  {
      [HarmonyPatch(typeof(Gun), nameof(Gun.HandleShootAnimation))]
      [HarmonyILManipulator]
      private static void GunShootAnimationIL(ILContext il)
      {
          ILCursor cursor = new ILCursor(il);
          if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcI4(1)))
            return;
          cursor.Emit(OpCodes.Ldarg_0);
          cursor.CallPrivate(typeof(GunShootAnimationPatch), nameof(CheckShouldRestartShootAnimation));
      }

      private static bool CheckShouldRestartShootAnimation(bool shouldRestart, Gun gun)
      {
          if (gun.gameObject.GetComponent<CwaffGun>() is CwaffGun cg)
            return !cg.continuousFireAnimation;
          return shouldRestart;
      }
  }
}

public abstract class CwaffBlankModificationItem: BlankModificationItem, ICwaffItem
{
  // public abstract string ItemName         { get; }
  // public abstract string ShortDescription { get; }
  // public abstract string LongDescription  { get; }
  // public abstract string Lore             { get; }

  public virtual void OnFirstPickup(PlayerController player)
  {

  }
}

public abstract class CwaffDodgeRollItem : CustomDodgeRollItem, ICwaffItem
{
  public virtual void OnFirstPickup(PlayerController player)
  {

  }
}

public abstract class CwaffCompanion : CompanionItem, ICwaffItem
{
  public override void Pickup(PlayerController player)
  {
    if (!this.m_pickedUpThisRun) // must come before base.Pickup()
      OnFirstPickup(player);
    base.Pickup(player);
  }

  public virtual void OnFirstPickup(PlayerController player)
  {

  }
}

public interface ICustomBlankDoer
{
  public void OnCustomBlankedProjectile(Projectile p);

  [HarmonyPatch(typeof(SilencerInstance), nameof(SilencerInstance.ProcessBlankModificationItemAdditionalEffects))]
  private static class AmmoAmmoletProcessBlankModificationPatch
  {
      static void Postfix(SilencerInstance __instance, BlankModificationItem bmi, Vector2 centerPoint, PlayerController user)
      {
          if (bmi is not ICustomBlankDoer customBlankDoer)
              return;

          __instance.UsesCustomProjectileCallback = true;
          __instance.OnCustomBlankedProjectile += customBlankDoer.OnCustomBlankedProjectile;
      }
  }
}

/// <summary>Dummy class to prevent guns from being thrown</summary>
public class Unthrowable : MonoBehaviour {}
