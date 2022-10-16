using MonoMod.RuntimeDetour;
using SaveAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NevernamedsItems
{
    class MiscUnlockHooks
    {
        public static void InitHooks()
        {
            ShrineUseHook = new Hook(
                typeof(AdvancedShrineController).GetMethod("DoShrineEffect", BindingFlags.Instance | BindingFlags.NonPublic),
                typeof(MiscUnlockHooks).GetMethod("OnShrineUsed", BindingFlags.Static | BindingFlags.Public)
            );

            BelloAngerHook = new Hook(
                typeof(BaseShopController).GetMethod("OnProjectileCreated", BindingFlags.Instance | BindingFlags.NonPublic),
                typeof(MiscUnlockHooks).GetMethod("BelloFiredBullet", BindingFlags.Static | BindingFlags.Public)
            );
            //ETGModConsole.Log("Hooks initialised");

        }
        public static Hook ShrineUseHook;
        public static Hook BelloAngerHook;
        public static void OnShrineUsed(Action<AdvancedShrineController, PlayerController> orig, AdvancedShrineController self, PlayerController playa)
        {
            //ETGModConsole.Log(self.displayTextKey);
            if (self.displayTextKey == "#SHRINE_FALLEN_ANGEL_DISPLAY")
            {
                if (!SaveAPIManager.GetFlag(CustomDungeonFlags.USEDFALLENANGELSHRINE))
                {
                    SaveAPIManager.SetFlag(CustomDungeonFlags.USEDFALLENANGELSHRINE, true);
                }
            }
            orig(self, playa);
        }
        public static void BelloFiredBullet(Action<BaseShopController, Projectile> orig, BaseShopController self, Projectile firedShot)
        {
            orig(self, firedShot);
            if (!SaveAPIManager.GetFlag(CustomDungeonFlags.ANGERED_BELLO))
            {
                SaveAPIManager.SetFlag(CustomDungeonFlags.ANGERED_BELLO, true);
            }
        }
    }
}
