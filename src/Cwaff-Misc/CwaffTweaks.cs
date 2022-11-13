using ItemAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

using System.ComponentModel;  //debug
using System.Reflection;  //debug

namespace CwaffingTheGungy
{
    public static class CwaffTweaks
    {
        private static DirectionalAnimation dogPettingAnimation;

        public static void Init()
        {
            GetDogPettingAnimation();

            //Make Wolf pettable
            ETGModConsole.Log("Making wolfyboi pettable");
            Gungeon.Game.Items["wolf"].GetComponent<CompanionItem>().MakePettable(
                spritePaths);
        }

        private static void GetDogPettingAnimation()
        {
            CompanionItem doggo = Gungeon.Game.Items["dog"].GetComponent<CompanionItem>();
            AIActor doggoai = EnemyDatabase.GetOrLoadByGuid(doggo.CompanionGuid);
            CompanionController doggocontroller =
                doggoai.gameObject.GetOrAddComponent<CompanionController>();
            foreach(AIAnimator.NamedDirectionalAnimation n in doggocontroller.sprite.aiAnimator.OtherAnimations)
            {
                if (n.name != "pet")
                    continue;
                dogPettingAnimation = n.anim;
                break;
            }
        }

        private static string[] spritePaths = new string[]
        {
            "CwaffingTheGungy/Resources/Companions/DroneCompanion/drone_idle_001", //0
            "CwaffingTheGungy/Resources/Companions/DroneCompanion/drone_idle_002", //1
            "CwaffingTheGungy/Resources/Companions/DroneCompanion/drone_idle_003", //2
            "CwaffingTheGungy/Resources/Companions/DroneCompanion/drone_idle_004", //3
            "CwaffingTheGungy/Resources/Companions/DroneCompanion/drone_idle_005", //4
            "CwaffingTheGungy/Resources/Companions/DroneCompanion/drone_idle_006", //5

        };
        public static void MakePettable(this CompanionItem ci, string[] pettingAnimation = null)
        {
            // Get the companion controller for the companion
            CompanionController cc =
                EnemyDatabase.GetOrLoadByGuid(ci.CompanionGuid).gameObject.GetOrAddComponent<CompanionController>();

            // Make it pettable
            cc.CanBePet = true;

            // Give it a petting animation
            if (pettingAnimation != null)
            {
                List<int> animIndices = new List<int>{};
                for (int i = 0; i < pettingAnimation.Length; i++)
                    animIndices.Add(SpriteBuilder.AddSpriteToCollection(pettingAnimation[i], cc.sprite.Collection));
                SpriteBuilder.AddAnimation(cc.sprite.spriteAnimator, cc.sprite.Collection, animIndices,
                    "pet", tk2dSpriteAnimationClip.WrapMode.Loop).fps = 8f;
            }
            else //...or just copy it from the dog while testing
            {
                cc.sprite.aiAnimator.OtherAnimations.Add(new AIAnimator.NamedDirectionalAnimation {
                    name = "pet",
                    anim = dogPettingAnimation,
                });
            }


            // wolfyboiai.animationAudioEvents = doggoai.animationAudioEvents;
        }
    }
}


