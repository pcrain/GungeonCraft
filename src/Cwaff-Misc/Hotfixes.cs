namespace CwaffingTheGungy;

// Fixes ignoreDamageCaps not working on beams
[HarmonyPatch(typeof(BasicBeamController), nameof(BasicBeamController.FrameUpdate))]
public static class BasicBeamControllerFrameUpdatePatch
{
    [HarmonyILManipulator]
    private static void BasicBeamControllerFrameUpdateIL(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);
        for (int i = 0; i < 3; ++i) // move right before the 3rd ApplyDamage() call
            if (!cursor.TryGotoNext(i == 2 ? MoveType.Before : MoveType.After,
              instr => instr.MatchCallvirt<HealthHaver>(nameof(HealthHaver.ApplyDamage))))
                return;

        if (!cursor.TryGotoPrev(MoveType.After, instr => instr.MatchLdcI4(0))) // hardcoded false for ignoreDamageCaps
            return;

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.CallPrivate(typeof(BasicBeamControllerFrameUpdatePatch), nameof(BeamShouldIgnoreDamageCaps));
    }

    private static bool BeamShouldIgnoreDamageCaps(bool origVal, BasicBeamController beam)
    {
        return origVal || (beam.projectile && beam.projectile.ignoreDamageCaps);
    }
}

// Fixes UI armor sprites slowly shifting offscreen whenever they're changed
//   (doesn't crop up in Vanilla due to armor sprite never changing)
[HarmonyPatch(typeof(GameUIHeartController), nameof(GameUIHeartController.UpdateHealth))]
public static class ArmorUIOffsetFix
{
    [HarmonyILManipulator]
    private static void ArmorUIOffsetFixIL(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);
        // 3 total instances, all of them need to be patched
        while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt<GameUIRoot>(nameof(GameUIRoot.GetMotionGroupParent))))
        {
            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<Pixelator>(nameof(Pixelator.CurrentTileScale))))
                return;
            // motionGroupParent.Width -= 0f;
            cursor.CallPrivate(typeof(ArmorUIOffsetFix), nameof(Zero));

            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<Pixelator>(nameof(Pixelator.CurrentTileScale))))
                return;
            // motionGroupParent.Height -= 0f;
            cursor.CallPrivate(typeof(ArmorUIOffsetFix), nameof(Zero));
        }
        return;
    }

    private static float Zero(float originalValue) => 0f;
}

// Fixes vanilla bug where if ModulesAreTiers is true, inactive burst modules contribute towards m_midBurstFire checks,
//   causing infinite firing after letting go of mouse
//   (doesn't crop up in vanilla due to vanilla ModulesAreTiers guns not having infinite burst sizes)
[HarmonyPatch(typeof(Gun), nameof(Gun.ContinueAttack))]
public static class ModulesAreTiersBurstFirePatch
{
    [HarmonyILManipulator]
    private static void ModulesAreTiersBurstFirePatchIL(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);
        if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<ProjectileModule>("burstShotCount")))
            return;

        cursor.Emit(OpCodes.Ldarg_0); // Gun instance
        cursor.Emit(OpCodes.Ldloc_S, (byte)4); // V_4 == projectileModule
        cursor.Emit(OpCodes.Ldloc_3); // V_3 == i (loop iterator)
        cursor.CallPrivate(typeof(ModulesAreTiersBurstFirePatch), nameof(IsCurrentBurstModule));
    }

    private static int IsCurrentBurstModule(int unadjustBurstShotCount, Gun gun, ProjectileModule mod, int i)
    {
      if (!gun.Volley.ModulesAreTiers)
        return unadjustBurstShotCount;
      if (gun.m_currentStrengthTier == ((mod.CloneSourceIndex < 0) ? i : mod.CloneSourceIndex))
        return unadjustBurstShotCount;
      return 0;
    }
}

// Prevent victory / death screen from displaying fake items (i.e., items suppressed from inventory)
public static class AmmonomiconPageRendererHotfix
{
    [HarmonyPatch(typeof(AmmonomiconPageRenderer), nameof(AmmonomiconPageRenderer.InitializeDeathPageRight))]
    private class SuppressFakeItemOnVictoryScreenPatch
    {
        [HarmonyILManipulator]
        private static void VictoryScreenFakeItemIL(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            ILLabel passiveLoopEndLabel = cursor.DefineLabel();

            // Actually do the logic for suppressing the item if necessary
            if (!cursor.TryGotoNext(MoveType.After,
                instr => instr.MatchLdarg(0), // this == AmmonomiconPageRenderer instance
                instr => instr.MatchLdloc((byte)6), // V_6 == playerController
                instr => instr.MatchLdfld<PlayerController>("passiveItems")
                ))
                return;
            cursor.Emit(OpCodes.Ldloc_S, (byte)12); // V_12 == m == iterator over passive items
            cursor.CallPrivate(typeof(SuppressFakeItemOnVictoryScreenPatch), nameof(ShouldSuppressItemFromVictoryScreen));
            cursor.Emit(OpCodes.Brtrue, passiveLoopEndLabel);
            // if we don't branch, repopulate the stack
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldloc_S, (byte)6);
            cursor.Emit(OpCodes.Ldfld, typeof(PlayerController).GetField("passiveItems", BindingFlags.Instance | BindingFlags.Public));

            // Mark the beginning of the loop since the compiler puts it later in the IL code
            if (!cursor.TryGotoNext(MoveType.Before,
                instr => instr.MatchLdloc((byte)12), // V_12 == m == iterator over passive items
                instr => instr.MatchLdcI4(1) // increment iterator
                ))
                return;
            cursor.MarkLabel(passiveLoopEndLabel);
        }

        // need to pass in AmmonomiconPageRenderer because it's the first instruction at the top of the loop and IL gets very messy otherwise
        private static bool ShouldSuppressItemFromVictoryScreen(AmmonomiconPageRenderer renderer, List<PassiveItem> items, int index)
        {
            return items[index].encounterTrackable && items[index].encounterTrackable.SuppressInInventory;
        }
    }
}

// Fix GungeonCraft guns with large idle frames having weird offsets from chests
// NOTE: can technically remove the GetComponent<CwaffGun> check to fix vanilla items like Casey, but don't want to break other mods that
//       rely on the existing behavior
public static class BadItemOffsetsFromChestHotfix
{
    [HarmonyPatch(typeof(Chest), nameof(Chest.PresentItem), MethodType.Enumerator)]
    private class BadItemOffsetsFromChestPatch
    {
        [HarmonyILManipulator]
        private static void BadItemOffsetsFromChestIL(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            for (int i = 0; i < 2; ++i) // interested in 2nd occurrence of store
                if (!cursor.TryGotoNext((i == 1) ? MoveType.Before : MoveType.After, instr => instr.MatchStloc((byte)7)))
                    return; // V_7 == offset vector for chest sprite

            cursor.Emit(OpCodes.Ldloc_S, (byte)3); // V_3 == the pickup object
            cursor.Emit(OpCodes.Ldloc_S, (byte)8); // V_8 == sprite for our chest prize
            cursor.CallPrivate(typeof(BadItemOffsetsFromChestPatch), nameof(DetermineActualOffset));
        }

        private static Vector3 DetermineActualOffset(Vector3 original, PickupObject p, tk2dSprite s)
        {
            if (p.gameObject.GetComponent<CwaffGun>())
                return -s.GetRelativePositionFromAnchor(Anchor.LowerCenter);
            return original;
        }
    }
}

/// <summary>Our guns show up funny in synergy notifications unless we use trimmed sprites, so fix that here by using the ammonomicon sprite instead</summary>
[HarmonyPatch(typeof(UINotificationController), nameof(UINotificationController.SetupSynergySprite))]
public static class SetupSynergySpritePatch
{
    static void Postfix(UINotificationController __instance, tk2dSpriteCollectionData collection, int spriteId)
    {
        string spriteName = collection.spriteDefinitions[spriteId].name;
        if (!spriteName.EndsWith("_idle_001"))
            return;
        string trimmedSpriteName = spriteName.Replace("_idle_001", "_ammonomicon");
        tk2dSpriteCollectionData ammonomiconCollection = AmmonomiconController.ForceInstance.EncounterIconCollection;
        int trimmedId = ammonomiconCollection.GetSpriteIdByName(trimmedSpriteName, defaultValue: -1);
        if (trimmedId != -1)
            __instance.notificationSynergySprite.SetSprite(ammonomiconCollection, trimmedId);
    }
}

// Temporary hotfix until I can figure out why the Dragun fight crashes with PSOG
public static class DragunFightHotfix
{
    [HarmonyPatch(typeof(BossTriggerZone), nameof(BossTriggerZone.OnTriggerCollision))]
    private class BossTriggerZoneNullDereferencePatch
    {
        [HarmonyILManipulator]
        private static void BossTriggerZoneNullDereferenceFixIL(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            // Sanity check the trigger zone itself
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.Emit(OpCodes.Ldarg_2);
            cursor.Emit(OpCodes.Ldarg_3);
            cursor.CallPrivate(typeof(DragunFightHotfix), nameof(BossTriggerZoneSanityCheck));

            // Sanity check the healthhaver to make sure it's not a boss without an ObjectVisibilityManager
            if (cursor.TryGotoNext(MoveType.Before, instr => instr.MatchCallvirt<HealthHaver>("get_IsBoss")))
                cursor.CallPrivate(typeof(DragunFightHotfix), nameof(HealthHaverSanityCheck));
            return;
        }
    }

    private static void BossTriggerZoneSanityCheck(BossTriggerZone zone, SpeculativeRigidbody otherRigidbody, SpeculativeRigidbody myRigidbody, CollisionData collisionData)
    {
        if (zone != null && collisionData.OtherPixelCollider != null && StaticReferenceManager.AllHealthHavers != null)
            return; // nothing went wrong

        Debug.Log($"BossTriggerZone is valid? {zone != null}");
        Debug.Log($"collisionData.OtherPixelCollider is valid? {collisionData.OtherPixelCollider != null}");
        Debug.Log($"StaticReferenceManager.AllHealthHavers is valid? {StaticReferenceManager.AllHealthHavers != null}");
    }

    internal static bool _SanityCheckFailed = false;
    private static HealthHaver HealthHaverSanityCheck(HealthHaver hh)
    {
        if (!hh)
        {
            Debug.Log($" healthhaver is invalid at boss trigger zone, tell pretzel");
            return hh;
        }
        if (!hh || !hh.aiActor)
            return hh;
        if (!hh.IsBoss || (hh.GetComponent<GenericIntroDoer>() is not GenericIntroDoer gid))
            return hh;
        if (gid.triggerType != GenericIntroDoer.TriggerType.BossTriggerZone)
            return hh;

        Debug.Log($" healthhaver {hh.aiActor.name} / {hh.aiActor.ActorName} / {hh.aiActor.OverrideDisplayName}");
        Debug.Log($"  is boss with GenericIntroDoer and BossTriggerZone type");
        if (gid.GetComponent<ObjectVisibilityManager>() is not ObjectVisibilityManager ovm)
        {
            gid.gameObject.GetOrAddComponent<ObjectVisibilityManager>(); // bandages the issue so the fight should still be playable
            Debug.LogError($"     DOES NOT HAVE ObjectVisibilityManager, TELL PRETZEL");
            if (!_SanityCheckFailed)
            {
                Lazy.CustomNotification("DRAGUN FIGHT BROKEN BY",$"{hh.aiActor.name}/{hh.aiActor.ActorName}/{hh.aiActor.OverrideDisplayName}");
                _SanityCheckFailed = true;
            }
        }
        return hh;
    }
}
