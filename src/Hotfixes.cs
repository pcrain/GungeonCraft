namespace CwaffingTheGungy;

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
