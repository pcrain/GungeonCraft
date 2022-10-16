using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using ItemAPI;
using MonoMod.RuntimeDetour;
using System.Reflection;

namespace NevernamedsItems
{
    public class SpecialGoopBehaviours : MonoBehaviour
    {
        public SpecialGoopBehaviours()
        {
            this.ForcesEnemiesToFall = false;
        }
        public bool ForcesEnemiesToFall;      
    }
    public class DoGoopEffectHook
    {
        public static void Init()
        {
            Hook doCustomGoopEffectsHook = new Hook(
                    typeof(DeadlyDeadlyGoopManager).GetMethod("DoGoopEffect", BindingFlags.Instance | BindingFlags.Public),
                    typeof(DoGoopEffectHook).GetMethod("DoCustomGoopEffects")
                );
        }
        public static void DoCustomGoopEffects(Action<DeadlyDeadlyGoopManager, GameActor, IntVector2> orig, DeadlyDeadlyGoopManager self, GameActor actor, IntVector2 goopPosition)
        {
            orig(self, actor, goopPosition);
            try
            {
                
                SpecialGoopBehaviours specialBehaviours = self.gameObject.GetComponent<SpecialGoopBehaviours>();
                if (specialBehaviours != null)
                {
                    if (actor && !(actor.aiAnimator.IsPlaying("spawn") && !actor.aiAnimator.IsPlaying("awaken")))
                    {

                    if (specialBehaviours.ForcesEnemiesToFall == true)
                    {
                        if (!actor.healthHaver.IsBoss)
                        {
                            actor.ForceFall();
                        }
                    }
                    }
                }
            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
                ETGModConsole.Log(e.StackTrace);
            }
        }
    }
}
