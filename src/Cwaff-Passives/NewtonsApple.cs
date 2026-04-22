namespace CwaffingTheGungy;

public class NewtonsApple : CwaffPassive
{
    public static string ItemName         = "Newton's Apple";
    public static string ShortDescription = "Doesn't Fall Far from the Tree";
    public static string LongDescription  = "Grounds all airborne enemies, making them susceptible to various goops, traps, and pits.";
    public static string Lore             = "Before an apple helped Isaac Newton invents gravity, everyone was burdened with the powers of flight and levitation. After gravity eliminated this burden from all but a select few, scientists got to work synthesizing even stronger gravity apples to alleviate the burden of flight from the rest of the population.";

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
        // enemy.PathableTiles |= CellTypes.PIT; // NOTE: probably don't want grounded enemies to pathfind over pits, so this doesn't do anything useful i think?
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
