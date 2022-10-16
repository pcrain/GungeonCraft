using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using UnityEngine;
using MonoMod.RuntimeDetour;
using Object = UnityEngine.Object;

namespace ItemAPI
{
    public static class FakePrefabHooks
    {
        public static void Init()
        {
            //Since for some reason generic method hooks don't want to work. Hopefully this is the only other way to get items!
            Hook silentItemAcquireHook = new Hook(
                typeof(PlayerController).GetMethod("AcquirePassiveItemPrefabDirectly"),
                typeof(FakePrefabHooks).GetMethod("AcquirePassiveItemPrefabDirectly")
            );

            //Same deal but for passives
            Hook activePickupHook = new Hook(
                typeof(PlayerItem).GetMethod("Pickup"),
                typeof(FakePrefabHooks).GetMethod("ActivePickup")
            );

            Hook instantiateOPI = new Hook(
                typeof(Object).GetMethod("Instantiate", new Type[]{
                    typeof(Object),
                    typeof(Transform),
                    typeof(bool),
                }),
                typeof(FakePrefabHooks).GetMethod("InstantiateOPI")
            );

            Hook instantiateOP = new Hook(
                typeof(Object).GetMethod("Instantiate", new Type[]{
                    typeof(Object),
                    typeof(Transform),
                }),
                typeof(FakePrefabHooks).GetMethod("InstantiateOP")
            );

            Hook instantiateO = new Hook(
                typeof(Object).GetMethod("orig_Instantiate", new Type[]{
                    typeof(Object),
                 }),
                typeof(FakePrefabHooks).GetMethod("InstantiateO")
            );

            Hook instantiateOPR = new Hook(
                typeof(Object).GetMethod("Instantiate", new Type[]{
                    typeof(Object),
                    typeof(Vector3),
                    typeof(Quaternion),
                }),
                typeof(FakePrefabHooks).GetMethod("InstantiateOPR")
            );

            Hook instantiateOPRP = new Hook(
                typeof(Object).GetMethod("Instantiate", new Type[]{
                    typeof(Object),
                    typeof(Vector3),
                    typeof(Quaternion),
                    typeof(Transform),
                }),
                typeof(FakePrefabHooks).GetMethod("InstantiateOPRP")
            );
        }

        public static void AcquirePassiveItemPrefabDirectly(Action<PlayerController, PassiveItem> orig, PlayerController self, PassiveItem item)
        {
            bool isFake = FakePrefab.IsFakePrefab(item.gameObject);
            if (isFake)
                item.gameObject.SetActive(true);

            orig(self, item);

            if (isFake)
                item.gameObject.SetActive(false);
        }

        public static void ActivePickup(Action<PlayerItem, PlayerController> orig, PlayerItem self, PlayerController player)
        {
            bool isFake = FakePrefab.IsFakePrefab(self.gameObject);
            if (isFake)
                self.gameObject.SetActive(true);

            orig(self, player);

            if (isFake)
                self.gameObject.SetActive(false);
        }

        public static Object InstantiateOPI(Func<Object, Transform, bool, Object> orig, Object original, Transform parent, bool instantiateInWorldSpace)
        {
            return FakePrefab.Instantiate(original, orig(original, parent, instantiateInWorldSpace));
        }

        public static Object InstantiateOP(Func<Object, Transform, Object> orig, Object original, Transform parent)
        {
            return FakePrefab.Instantiate(original, orig(original, parent));
        }

        public static Object InstantiateO(Func<Object, Object> orig, Object original)
        {
            return FakePrefab.Instantiate(original, orig(original));
        }

        public static Object InstantiateOPR(Func<Object, Vector3, Quaternion, Object> orig, Object original, Vector3 position, Quaternion rotation)
        {
            return FakePrefab.Instantiate(original, orig(original, position, rotation));
        }

        public delegate TResult Func<T1, T2, T3, T4, T5, out TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
        public static Object InstantiateOPRP(Func<Object, Vector3, Quaternion, Transform, Object> orig, Object original, Vector3 position, Quaternion rotation, Transform parent)
        {
            return FakePrefab.Instantiate(original, orig(original, position, rotation, parent));
        }

    }
}
