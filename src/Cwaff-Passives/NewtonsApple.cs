namespace CwaffingTheGungy;

public class NewtonsApple : CwaffPassive
{
    public static string ItemName         = "Newton's Apple";
    public static string ShortDescription = "Gravity's Just a Theory";
    public static string LongDescription  = "Grounds all airborne enemies, making them susceptible to various goops, traps, and pits.";
    public static string Lore             = "TBD";

    public static void Init()
    {
        PassiveItem item   = Lazy.SetupPassive<NewtonsApple>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality       = ItemQuality.D;
    }

    public override void Pickup(PlayerController player)
    {
        base.Pickup(player);
        PassiveItem.IncrementFlag(player, typeof(NewtonsApple));
    }

    public override void DisableEffect(PlayerController player)
    {
        base.DisableEffect(player);
        if (player)
            PassiveItem.DecrementFlag(player, typeof(NewtonsApple));
    }

    [HarmonyPatch]
    private static class NewtonsApplePatches
    {
      private static bool AffectedByNewtonsApple(GameActor actor)
      {
        if (!PassiveItem.IsFlagSetAtAll(typeof(NewtonsApple)))
          return false;
        if (actor is not AIActor enemy)
          return false;
        if (enemy.CompanionOwner)
          return false; // don't touch companions
        return true;
      }

      [HarmonyPatch(typeof(GameActor), nameof(GameActor.QueryGroundedFrame))]
      [HarmonyPrefix]
      private static bool GameActorQueryGroundedFramePatch(GameActor __instance, ref bool __result)
      {
        if (!AffectedByNewtonsApple(__instance))
          return true; // call original method
        __result = true;
        return false; // skip original method
      }

      [HarmonyPatch(typeof(GameActor), nameof(GameActor.IsFlying), MethodType.Getter)]
      [HarmonyPrefix]
      private static bool GameActorIsFlyingPatch(GameActor __instance, ref bool __result)
      {
        if (!AffectedByNewtonsApple(__instance))
          return true; // call original method
        __result = false;
        return false;    // skip the original method
      }
    }
}
