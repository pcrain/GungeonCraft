namespace CwaffingTheGungy;

// All 3 of Gungeon's list-shuffling implementations are flawed due to off by one errors, so we need to fix them
//   We can't hook generic methods directly, so focus on GenerationShuffle<int>(), which is the main issue for floor room generation
public static class RoomShuffleOffByOneHotfix
{
    public static void Init()
    {
        new ILHook(
            typeof(BraveUtility).GetMethod("GenerationShuffle", BindingFlags.Static | BindingFlags.Public).MakeGenericMethod(typeof(int)),
            GenerationShuffleFixIL
            );
    }

    private static void GenerationShuffleFixIL(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);
        // cursor.DumpILOnce("GenerationShuffleFixIL");

        // GenerationRandomRange(0, num) should be GenerationRandomRange(0, num + 1)
        if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdloc(0)))
            return;
        cursor.Emit(OpCodes.Ldc_I4_1);
        cursor.Emit(OpCodes.Add);

        // num > 1 should be num >= 1
        ILLabel forLabel = null;
        if (!cursor.TryGotoNext(MoveType.Before, instr => instr.MatchBgt(out forLabel)))
            return;
        cursor.Remove();
        cursor.Emit(OpCodes.Bge, forLabel);
    }
}

// Quick restart doesn't call PreprcoessRun(), so once-per-run rooms will never respawn until you return to the Breach
// GameManager.Instance.GlobalInjectionData.PreprocessRun();
public static class QuickRestartRoomCacheHotfix
{
    public static void Init()
    {
        new ILHook(
            typeof(GameManager).GetMethod("QuickRestart", BindingFlags.Instance | BindingFlags.Public),
            OnQuickRestartIL
            );
    }

    private static void OnQuickRestartIL(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);

        if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdstr("Quick Restarting...")))
            return;
        cursor.Index++; // skip over Debug.Log() call

        // if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchCall<GameManager>("FlushAudio")))
        //     return;

        cursor.Emit(OpCodes.Ldarg_0); // load the game manager
        cursor.Emit(OpCodes.Call, typeof(QuickRestartRoomCacheHotfix).GetMethod("ForcePreprocessRunForQuickStart", BindingFlags.Static | BindingFlags.NonPublic));
    }

    private static void ForcePreprocessRunForQuickStart(GameManager gm)
    {
        // ETGModConsole.Log($"  forcibly preprocessing run");
        if (!gm)
            return;
        gm.GlobalInjectionData.PreprocessRun();
    }
}

// Duct tape gun ids aren't serialized, so dropping them clears out the duct tape gun list and breaks save serialization
public static class DuctTapeSaveLoadHotfix
{
    public static void Init()
    {
        new Hook(
            typeof(Gun).GetMethod("CopyStateFrom", BindingFlags.Instance | BindingFlags.Public),
            typeof(DuctTapeSaveLoadHotfix).GetMethod("CopyStateFromHook", BindingFlags.Static | BindingFlags.NonPublic)
            );
    }

    private static void CopyStateFromHook(Action<Gun, Gun> orig, Gun gun, Gun other)
    {
        orig(gun, other);
        gun.DuctTapeMergedGunIDs = other.DuctTapeMergedGunIDs;
    }
}

// Fix guns with extremely large animations having enormous pickup ranges and appearing very weirdly on pedestals
public static class LargeGunAnimationHotfix
{
    internal const string _TRIM_ANIMATION = "idle_trimmed";

    public static void Init()
    {
        new ILHook(
            typeof(ShopItemController).GetMethod("InitializeInternal", BindingFlags.Instance | BindingFlags.NonPublic),
            OnInitializeVanillaShopItemIL
            );

        new ILHook(
            typeof(CustomShopItemController).GetMethod("InitializeInternal", BindingFlags.Instance | BindingFlags.Public),
            OnInitializeCustomShopItemIL
            );

        new ILHook(
            typeof(RewardPedestal).GetMethod("DetermineContents", BindingFlags.Instance | BindingFlags.NonPublic),
            OnDetermineContentsIL
            );

        new Hook(
            typeof(GunInventory).GetMethod("AddGunToInventory", BindingFlags.Instance | BindingFlags.Public),
            typeof(LargeGunAnimationHotfix).GetMethod("OnAddGunToInventory", BindingFlags.Static | BindingFlags.NonPublic)
            );

        new Hook(
            typeof(Gun).GetMethod("DropGun", BindingFlags.Instance | BindingFlags.Public),
            typeof(LargeGunAnimationHotfix).GetMethod("OnDropGun", BindingFlags.Static | BindingFlags.NonPublic)
            );
    }

    private static tk2dSpriteAnimationClip GetTrimmedIdleAnimation(this Gun gun)
    {
        return gun.spriteAnimator?.GetClipByName($"{gun.InternalSpriteName()}_{_TRIM_ANIMATION}");
    }

    // Make sure guns in vanilla shops are aligned properly
    private static void OnInitializeVanillaShopItemIL(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);
        // cursor.DumpILOnce("OnInitializeVanillaShopItemIL");

        if (!cursor.TryGotoNext(MoveType.Before,
          instr => instr.MatchStfld<ShopItemController>("UseOmnidirectionalItemFacing"),
          instr => instr.MatchLdarg(0),
          instr => instr.MatchCall<BraveBehaviour>("get_sprite"),
          instr => instr.MatchLdarg(0)
          ))
            return;

        // skip past UseOmnidirectionalItemFacing and loading the ShopItemController, then proceed as with custom shop items
        //   and finally replace the ShopItemController arg at the end
        cursor.Index += 2;
        cursor.Emit(OpCodes.Ldarg_1);  // PickupObject
        cursor.Emit(OpCodes.Call, typeof(LargeGunAnimationHotfix).GetMethod("FixVanillaShopItemSpriteIfNecessary", BindingFlags.Static | BindingFlags.NonPublic));
        cursor.Emit(OpCodes.Ldarg_0);  // ShopItemController
        // ETGModConsole.Log($"  prepatched vanilla shop!");
    }

    private static void FixVanillaShopItemSpriteIfNecessary(CustomShopItemController item, PickupObject pickup)
    {
        if (pickup.GetComponent<Gun>() is not Gun gun)
            return;

        tk2dSpriteAnimationClip idleClip = gun.GetTrimmedIdleAnimation();
        if (idleClip == null || idleClip.frames == null || idleClip.frames.Count() == 0)
            return;  // the idle animation clip is missing frames, so there's nothing to do

        item.sprite.SetSprite(idleClip.frames[0].spriteCollection, idleClip.frames[0].spriteId);
    }

    // Make sure guns in modded shops are aligned properly
    private static void OnInitializeCustomShopItemIL(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);
        // cursor.DumpILOnce("OnInitializeCustomShopItemIL");

        if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchStfld<CustomShopItemController>("UseOmnidirectionalItemFacing")))
            return;

        cursor.Emit(OpCodes.Ldarg_0);  // CustomShopItemController
        cursor.Emit(OpCodes.Ldarg_1);  // PickupObject
        cursor.Emit(OpCodes.Call, typeof(LargeGunAnimationHotfix).GetMethod("FixCustomShopItemSpriteIfNecessary", BindingFlags.Static | BindingFlags.NonPublic));
        // ETGModConsole.Log($"  prepatched custom shop!");
    }

    private static void FixCustomShopItemSpriteIfNecessary(CustomShopItemController item, PickupObject pickup)
    {
        if (pickup.GetComponent<Gun>() is not Gun gun)
            return;

        tk2dSpriteAnimationClip idleClip = gun.GetTrimmedIdleAnimation();
        if (idleClip == null || idleClip.frames == null || idleClip.frames.Count() == 0)
            return;  // the idle animation clip is missing frames, so there's nothing to do

        item.sprite.SetSprite(idleClip.frames[0].spriteCollection, idleClip.frames[0].spriteId);
    }

    // private static void DebugSetRewardPedestal(RewardPedestal reward)
    // {
    //     if (!C.DEBUG_BUILD)
    //         return;
    //     ETGModConsole.Log($"  SETTING REWARD PEDESTAL TO KNOWN BROKEN ITEM");
    //     reward.contents = PickupObjectDatabase.GetById(IDs.Pickups["racket_launcher"]);
    //     // reward.contents = PickupObjectDatabase.GetById(IDs.Pickups["outbreak"]);
    //     // reward.contents = PickupObjectDatabase.GetById(IDs.Pickups["jugglernaut"]);
    // }

    // Use the first frame of the gun's (potentially trimmed) idle animation as its reward pedestal sprite
    private static void FixRewardPedestalSpriteIfNecessary(RewardPedestal reward)
    {
        if (reward.contents.GetComponent<Gun>() is not Gun gun)
            return;  // we don't have a gun, so there's nothing to do
        if (!gun.idleAnimation.Contains(_TRIM_ANIMATION))
            return;  // the gun doesn't have a trimmed idle animation, so there's nothing to do

        tk2dSpriteAnimationClip idleClip = gun.GetTrimmedIdleAnimation();
        if (idleClip == null || idleClip.frames == null || idleClip.frames.Count() == 0)
            return;  // the idle animation clip is missing frames, so there's nothing to do

        // actually adjust the sprite to display properly on the reward pedestal
        reward.m_itemDisplaySprite.SetSprite(idleClip.frames[0].spriteCollection, idleClip.frames[0].spriteId);
    }

    private static void OnDetermineContentsIL(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);

        // if (C.DEBUG_BUILD)
        //     if (cursor.TryGotoNext(MoveType.Before, instr => instr.MatchLdstr("Display Sprite")))
        //     {
        //         cursor.Emit(OpCodes.Ldarg_0);
        //         cursor.Emit(OpCodes.Call, typeof(LargeGunAnimationHotfix).GetMethod("DebugSetRewardPedestal", BindingFlags.Static | BindingFlags.NonPublic));
        //     }

        if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchStfld<RewardPedestal>("m_itemDisplaySprite")))
            return;

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Call, typeof(LargeGunAnimationHotfix).GetMethod("FixRewardPedestalSpriteIfNecessary", BindingFlags.Static | BindingFlags.NonPublic));
    }

    private static Gun OnAddGunToInventory(Func<GunInventory, Gun, bool, Gun> orig, GunInventory inventory, Gun gun, bool makeActive)
    {
         if (!gun)
            return orig(inventory, gun, makeActive);

        string fixedIdleAnimation = $"{gun.InternalSpriteName()}_{_TRIM_ANIMATION}";
        if (gun.spriteAnimator.GetClipIdByName(fixedIdleAnimation) != -1)
        {
            gun.idleAnimation = $"{gun.InternalSpriteName()}_idle";  // restore the gun's original (untrimmed) idle animation when picked up
            gun.spriteAnimator.defaultClipId = gun.spriteAnimator.GetClipIdByName(gun.idleAnimation);
        }

        return orig(inventory, gun, makeActive);
    }

    private static DebrisObject OnDropGun(Func<Gun, float, DebrisObject> orig, Gun gun, float dropHeight)
    {
        DebrisObject debris = orig(gun, dropHeight);

        string fixedIdleAnimation = $"{gun.InternalSpriteName()}_{_TRIM_ANIMATION}";
        if (gun.spriteAnimator.GetClipIdByName(fixedIdleAnimation) != -1)
        {
            Vector2 center = gun.sprite.WorldCenter;
            gun.spriteAnimator.defaultClipId = gun.spriteAnimator.GetClipIdByName(fixedIdleAnimation);
            gun.spriteAnimator.Play(fixedIdleAnimation);  // play the gun's fixed (trimmed) idle animation when dropped
            gun.sprite.PlaceAtPositionByAnchor(center, Anchor.MiddleCenter);
        }
        return debris;
    }
}

// Fix softlock when one player dies in a payday drill room (can't get the bug to trigger consistently enough to test this, so disabling this for now)
public static class CoopDrillSoftlockHotfix
{
    // private static ILHook _CoopDrillSoftlockHotfixHook;

    public static void Init()
    {
        // _CoopDrillSoftlockHotfixHook = new ILHook(
        //     typeof(PaydayDrillItem).GetNestedType("<HandleTransitionToFallbackCombatRoom>c__Iterator1", BindingFlags.NonPublic | BindingFlags.Instance).GetMethod("MoveNext"),
        //     CoopDrillSoftlockHotfixHookIL
        //     );
    }

    private static void CoopDrillSoftlockHotfixHookIL(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);
        // cursor.DumpILOnce("CoopDrillSoftlockHotfixHook");

        if (!cursor.TryGotoNext(MoveType.After,
          instr => instr.MatchCallvirt<Chest>("ForceUnlock"),
          instr => instr.MatchLdarg(0)
          ))
            return; // failed to find what we need

        ETGModConsole.Log($"found!");

        // partial fix
        cursor.Remove(); // remove loading false into our bool
        cursor.Emit(OpCodes.Ldc_I4_1); // load true into the bool instead so we can immediately skip the loop

        // debugging
        // cursor.Emit(OpCodes.Call, typeof(CoopDrillSoftlockHotfix).GetMethod("SanityCheck"));

        return;
    }

    public static void SanityCheck()
    {
        ETGModConsole.Log($"sanity checking");
        for (int j = 0; j < GameManager.Instance.AllPlayers.Length; j++)
        {
            ETGModConsole.Log($"position for player {j+1}");
            ETGModConsole.Log($"{GameManager.Instance.AllPlayers[j].CenterPosition}");
        }
    }
}

// Fix player two not getting Turbo Mode speed buffs in Coop
public static class CoopTurboModeHotfix
{
    private static ILHook _CoopTurboModeFixHook;

    public static void Init()
    {
        _CoopTurboModeFixHook = new ILHook(
            typeof(PlayerController).GetMethod("UpdateTurboModeStats", BindingFlags.Instance | BindingFlags.NonPublic),
            CoopTurboModeFixHookIL
            );
    }

    private static void CoopTurboModeFixHookIL(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);
        // cursor.DumpILOnce("CoopTurboModeFixHookIL");

        if (!cursor.TryGotoNext(MoveType.After,
          instr => instr.MatchLdfld<PlayerController>("m_turboSpeedModifier"),
          instr => instr.OpCode == OpCodes.Callvirt  // can't match List<StatModified>::Add() for some reason
          ))
            return; // failed to find what we need

        // Recalculate stats after adjusting turbo speed modifier (mirrors IL code for other calls to stats.RecalculateStats())
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldfld, typeof(PlayerController).GetField("stats", BindingFlags.Instance | BindingFlags.Public));
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldc_I4_0);
        cursor.Emit(OpCodes.Ldc_I4_0);
        cursor.Emit(OpCodes.Callvirt, typeof(PlayerStats).GetMethod("RecalculateStats", BindingFlags.Instance | BindingFlags.Public));

        if (!cursor.TryGotoNext(MoveType.After,
          instr => instr.MatchLdfld<PlayerController>("m_turboRollSpeedModifier"),
          instr => instr.OpCode == OpCodes.Callvirt  // can't match List<StatModified>::Add() for some reason
          ))
            return; // failed to find what we need

        // Recalculate stats after adjusting turbo roll speed modifier (mirrors IL code for other calls to stats.RecalculateStats())
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldfld, typeof(PlayerController).GetField("stats", BindingFlags.Instance | BindingFlags.Public));
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldc_I4_0);
        cursor.Emit(OpCodes.Ldc_I4_0);
        cursor.Emit(OpCodes.Callvirt, typeof(PlayerStats).GetMethod("RecalculateStats", BindingFlags.Instance | BindingFlags.Public));

        return;
    }
}

// Temporary hotfix until I can figure out why the Dragun fight crashes with PSOG
public static class DragunFightHotfix
{
    private static ILHook _BossTriggerZoneNullDereferenceFixHook;

    public static void Init()
    {
        _BossTriggerZoneNullDereferenceFixHook = new ILHook(
            typeof(BossTriggerZone).GetMethod("OnTriggerCollision", BindingFlags.Instance | BindingFlags.NonPublic),
            BossTriggerZoneNullDereferenceFixIL
            );
    }

    public static void BossTriggerZoneSanityCheck(BossTriggerZone zone, SpeculativeRigidbody otherRigidbody, SpeculativeRigidbody myRigidbody, CollisionData collisionData)
    {
        if (zone != null && collisionData.OtherPixelCollider != null && StaticReferenceManager.AllHealthHavers != null)
            return; // nothing went wrong

        Debug.Log($"BossTriggerZone is valid? {zone != null}");
        Debug.Log($"collisionData.OtherPixelCollider is valid? {collisionData.OtherPixelCollider != null}");
        Debug.Log($"StaticReferenceManager.AllHealthHavers is valid? {StaticReferenceManager.AllHealthHavers != null}");
    }

    internal static bool _SanityCheckFailed = false;
    public static HealthHaver HealthHaverSanityCheck(HealthHaver hh)
    {
        if (!hh)
        {
            Debug.Log($" healthhaver is invalid at boss trigger zone, tell pretzel");
            return hh;
        }
        if (hh?.aiActor == null)
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

    private static void BossTriggerZoneNullDereferenceFixIL(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);

        // Sanity check the trigger zone itself
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldarg_1);
        cursor.Emit(OpCodes.Ldarg_2);
        cursor.Emit(OpCodes.Ldarg_3);
        cursor.Emit(OpCodes.Call, typeof(DragunFightHotfix).GetMethod("BossTriggerZoneSanityCheck"));

        // Sanity check the healthhaver to make sure it's not a boss without an ObjectVisibilityManager
        if (cursor.TryGotoNext(MoveType.Before, instr => instr.MatchCallvirt<HealthHaver>("get_IsBoss")))
            cursor.Emit(OpCodes.Call, typeof(DragunFightHotfix).GetMethod("HealthHaverSanityCheck"));
        return;
    }
}
