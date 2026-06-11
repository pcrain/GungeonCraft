namespace CwaffingTheGungy;

[HarmonyPatch]
internal static class SlimyboiPatches
{
  /// <summary>Patches to make slime collision damage ignore boss damage caps</summary>
  private static bool _NextAttackIgnoresDamageCaps = false;
  private static void SlimyboiControllerIgnoreDamageCaps(AIActor actor)
  {
    if (actor && actor.gameObject.GetComponent<SlimyboiController>())
      _NextAttackIgnoresDamageCaps = true;
  }
  [HarmonyPatch(typeof(HealthHaver), nameof(HealthHaver.ApplyDamage))]
  [HarmonyPrefix]
  private static void HealthHaverApplyDamagePatch(HealthHaver __instance, float damage, Vector2 direction, string sourceName, CoreDamageTypes damageTypes, DamageCategory damageCategory, bool ignoreInvulnerabilityFrames, PixelCollider hitPixelCollider, ref bool ignoreDamageCaps)
  {
    if (_NextAttackIgnoresDamageCaps)
      ignoreDamageCaps = true;
    _NextAttackIgnoresDamageCaps = false;
  }
  [HarmonyPatch(typeof(AIActor), nameof(AIActor.OnCollision))]
  [HarmonyILManipulator]
  private static void AIActorOnCollisionPatchIL(ILContext il)
  {
      ILCursor cursor = new ILCursor(il);
      if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<AIActor>("CollisionDamageTypes")))
        return;
      if (!cursor.TryGotoNext(MoveType.Before, instr => instr.MatchCallvirt<HealthHaver>(nameof(HealthHaver.ApplyDamage))))
        return;

      cursor.Emit(OpCodes.Ldarg_0);
      cursor.CallPrivate(typeof(SlimyboiPatches), nameof(SlimyboiControllerIgnoreDamageCaps));
  }

  /// <summary>Patch to make player-owned beams not hit slimes.</summary>
  private static SpeculativeRigidbody[] _IgnoredBodiesPlusSlimes = new SpeculativeRigidbody[0];
  [HarmonyPatch(typeof(BeamController), nameof(BeamController.GetIgnoreRigidbodies))]
  [HarmonyPostfix]
  private static void BeamControllerGetIgnoreRigidbodiesPatch(BeamController __instance, ref SpeculativeRigidbody[] __result)
  {
      if (!SlimyboiManager.AnyActiveSlimes() || __instance.Owner is not PlayerController)
        return; // shortcut if no slimes are active or if the beam is not player-owned

      int numSlimes = SlimyboiManager.NumActiveSlimes();
      int numOtherBodies = __result.Length; // get the older number of ignored bodies
      int totalIgnoredBodies = numSlimes + numOtherBodies; // total ignored bodies is the old number + the number of slimes
      if (totalIgnoredBodies != _IgnoredBodiesPlusSlimes.Length)
        _IgnoredBodiesPlusSlimes = new SpeculativeRigidbody[totalIgnoredBodies]; // don't reallocate unless we absolutely need to
      int i;
      for (i = 0; i < numOtherBodies; ++i)
        _IgnoredBodiesPlusSlimes[i] = __result[i]; // copy the result array over
      foreach (SlimyboiController sloim in SlimyboiManager.ActiveSlimes)
        _IgnoredBodiesPlusSlimes[i++] = sloim ? sloim.specRigidbody : null; // add rigidbodies for the slimes in as necessary
      __result = _IgnoredBodiesPlusSlimes; // replace the result with the slimes
  }

  /// <summary>Patches to detect rooms that spawn with traps</summary>
  [HarmonyPatch(typeof(PathingTrapController), nameof(PathingTrapController.Start))]
  [HarmonyPostfix]
  private static void PathingTrapControllerStartPatch(PathingTrapController __instance)
  {
    SlimyboiManager.RegisterTrap(__instance, __instance.m_parentRoom);
  }
  [HarmonyPatch(typeof(BasicTrapController), nameof(BasicTrapController.Start))]
  [HarmonyPostfix]
  private static void BasicTrapControllerStartPatch(BasicTrapController __instance)
  {
    SlimyboiManager.RegisterTrap(__instance, __instance.m_parentRoom);
  }

  /// <summary>Patch to detect flipped tables</summary>
  [HarmonyPatch(typeof(FlippableCover), nameof(FlippableCover.Flip), typeof(DungeonData.Direction))]
  [HarmonyPostfix]
  private static void FlippableCoverFlipPatch(FlippableCover __instance, DungeonData.Direction flipDirection)
  {
    SlimyboiManager.HandleTableFlip(__instance);
  }

  /// <summary>Allow slimes to collide with the Bullet King's / Old King's Chancellor</summary>
  [HarmonyPatch(typeof(BulletKingToadieController), nameof(BulletKingToadieController.PreRigidbodyCollision))]
  [HarmonyPostfix]
  private static void BulletKingToadieControllerPreRigidbodyCollisionPatch(BulletKingToadieController __instance, SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
  {
    if (otherRigidbody.gameObject.GetComponent<SlimyboiController>())
      PhysicsEngine.SkipCollision = false;
  }

  // WARNING: this "fixes" the camera, but Wallmongerer's behavior is just to teleport down to the player as soon as it gets unstuck...
  //          unsure whether it's best to leave this in or not
  private static Vector2 _PreAdjustCameraPos = default;
  /// <summary>Fix camera drift as Wallmongerer gets pushed back.</summary>
  [HarmonyPatch(typeof(DemonWallMovementBehavior), nameof(DemonWallMovementBehavior.Update))]
  [HarmonyPrefix]
  private static void DemonWallMovementBehaviorUpdatePrefixPatch(DemonWallMovementBehavior __instance)
  {
    if (!SlimyboiManager.HasInstance || __instance.m_deltaTime <= 0f || __instance.m_demonWallController is not DemonWallController wall || !wall.IsCameraLocked)
      return;

    _PreAdjustCameraPos = GameManager.Instance.MainCameraController.OverridePosition;
  }
  [HarmonyPatch(typeof(DemonWallMovementBehavior), nameof(DemonWallMovementBehavior.Update))]
  [HarmonyPostfix]
  private static void DemonWallMovementBehaviorUpdatePostfixPatch(DemonWallMovementBehavior __instance)
  {
    if (!SlimyboiManager.HasInstance || __instance.m_deltaTime <= 0f || __instance.m_demonWallController is not DemonWallController wall || !wall.IsCameraLocked)
      return;

    CameraController mainCameraController = GameManager.Instance.MainCameraController;
    Vector2 offset = new Vector2(0f, wall.specRigidbody.HitboxPixelCollider.UnitDimensions.y - mainCameraController.Camera.orthographicSize + 0.5f);
    mainCameraController.OverridePosition = Lazy.SmoothestLerp(_PreAdjustCameraPos, wall.specRigidbody.UnitCenter + offset, 2.0f);
  }
}
