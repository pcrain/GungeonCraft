using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace GungeonAPI
{
    class ReloadBreachShrineHooks
    {
        public static Hook breachshrinereloadhook;

        public static void Init()
        {
            breachshrinereloadhook = new Hook(typeof(Foyer).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic), typeof(ReloadBreachShrineHooks).GetMethod("ReloadBreachShrinesHook"));
        }
        private static bool hasInitialized;
        public static void ReloadBreachShrinesHook(Action<Foyer> orig, Foyer self1)
        {
            orig(self1);
            bool flag = ReloadBreachShrineHooks.hasInitialized;
            if (!flag)
            {
                {
                    ShrineFactory.PlaceBreachShrines();
                }
                ReloadBreachShrineHooks.hasInitialized = true;
            }
            ShrineFactory.PlaceBreachShrines();
        }
    }
}
