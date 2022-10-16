using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using UnityEngine;
using MonoMod.RuntimeDetour;
using Object = UnityEngine.Object;
using ItemAPI;

namespace GungeonAPI
{
    public static class ShrineFakePrefabHooks
    {
        public static void Init()
        {
            try
            {
                /*
                MethodInfo genericInstantiate = typeof(Object).GetMethods()
                             .Where(m => m.Name == "Instantiate")
                             .Select(m => new
                             {
                                 Method = m,
                                 Params = m.GetParameters(),
                                 Args = m.GetGenericArguments()
                             })
                             .Where(x => x.Params.Length == 1
                                         && x.Args.Length == 1
                                         && x.Params[0].ParameterType == x.Args[0])
                             .Select(x => x.Method)
                             .First();
                Hook genericInstantiateHook = new Hook(
                    genericInstantiate,
                    typeof(FakePrefabHooks).GetMethod("InstantiateGeneric")
                );
                */
                Hook instantiateOPI = new Hook(
                    typeof(Object).GetMethod("Instantiate", new Type[]{
                    typeof(Object),
                    typeof(Transform),
                    typeof(bool),
                    }),
                    typeof(ShrineFakePrefabHooks).GetMethod("InstantiateOPI")
                );

                Hook instantiateOP = new Hook(
                    typeof(Object).GetMethod("Instantiate", new Type[]{
                    typeof(Object),
                    typeof(Transform),
                    }),
                    typeof(ShrineFakePrefabHooks).GetMethod("InstantiateOP")
                );

                Hook instantiateO = new Hook(
                    typeof(Object).GetMethod("Instantiate", new Type[]{
                    typeof(Object),
                     }),
                    typeof(ShrineFakePrefabHooks).GetMethod("InstantiateO")
                );

                Hook instantiateOPR = new Hook(
                    typeof(Object).GetMethod("Instantiate", new Type[]{
                    typeof(Object),
                    typeof(Vector3),
                    typeof(Quaternion),
                    }),
                    typeof(ShrineFakePrefabHooks).GetMethod("InstantiateOPR")
                );

                Hook instantiateOPRP = new Hook(
                    typeof(Object).GetMethod("Instantiate", new Type[]{
                    typeof(Object),
                    typeof(Vector3),
                    typeof(Quaternion),
                    typeof(Transform),
                    }),
                    typeof(ShrineFakePrefabHooks).GetMethod("InstantiateOPRP")
                );
            }
            catch (Exception e)
            {
                Tools.PrintException(e);
            }
        }

        public static T InstantiateGeneric<T>(Object original) where T : Object
        {
            return (T)ShrineFakePrefab.Instantiate(original, Object.Instantiate(original));
        }

        public static Object InstantiateOPI(Func<Object, Transform, bool, Object> orig, Object original, Transform parent, bool instantiateInWorldSpace)
        {
            return ShrineFakePrefab.Instantiate(original, orig(original, parent, instantiateInWorldSpace));
        }

        public static Object InstantiateOP(Func<Object, Transform, Object> orig, Object original, Transform parent)
        {
            return ShrineFakePrefab.Instantiate(original, orig(original, parent));
        }

        public static Object InstantiateO(Func<Object, Object> orig, Object original)
        {
            return ShrineFakePrefab.Instantiate(original, orig(original));
        }

        public static Object InstantiateOPR(Func<Object, Vector3, Quaternion, Object> orig, Object original, Vector3 position, Quaternion rotation)
        {
            return ShrineFakePrefab.Instantiate(original, orig(original, position, rotation));
        }

        public delegate TResult Func<T1, T2, T3, T4, T5, out TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
        public static Object InstantiateOPRP(Func<Object, Vector3, Quaternion, Transform, Object> orig, Object original, Vector3 position, Quaternion rotation, Transform parent)
        {
            return ShrineFakePrefab.Instantiate(original, orig(original, position, rotation, parent));
        }

    }
}