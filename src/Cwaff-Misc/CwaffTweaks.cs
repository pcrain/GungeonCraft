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
            Gungeon.Game.Items["wolf"].GetComponent<CompanionItem>().MakePettable();
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
            "CwaffingTheGungy/Resources/Companions/DroneCompanion/drone_run_right_001", //6
            "CwaffingTheGungy/Resources/Companions/DroneCompanion/drone_run_right_002", //7
            "CwaffingTheGungy/Resources/Companions/DroneCompanion/drone_run_right_003", //8
            "CwaffingTheGungy/Resources/Companions/DroneCompanion/drone_run_left_001", //9
            "CwaffingTheGungy/Resources/Companions/DroneCompanion/drone_run_left_002", //10
            "CwaffingTheGungy/Resources/Companions/DroneCompanion/drone_run_left_003", //11
            "CwaffingTheGungy/Resources/Companions/DroneCompanion/drone_pet_001", //12

        };
        public static void MakePettable(this CompanionItem ci)
        {
            AIActor cai            = EnemyDatabase.GetOrLoadByGuid(ci.CompanionGuid);
            CompanionController cc = cai.gameObject.GetOrAddComponent<CompanionController>();
            cc.CanBePet            = true;
            AIAnimator can         = cc.sprite.aiAnimator;

            // can.OtherAnimations.Add(new AIAnimator.NamedDirectionalAnimation {
            //     name = "pet",
            //     anim = dogPettingAnimation,
            // });

            // tk2dSpriteCollectionData cta = cc.GetComponent<tk2dSpriteCollectionData>();
            // if(cta == null)
            //     cta = SpriteBuilder.ConstructCollection(cc.sprite.gameObject, ("wolf" + "_Pool"));

            int startindex = 0;
            for (int i = 0; i < spritePaths.Length; i++)
            {
                startindex = SpriteBuilder.AddSpriteToCollection(spritePaths[i], cc.sprite.Collection);
            }
            //Idling Animation
            SpriteBuilder.AddAnimation(cc.sprite.spriteAnimator, cc.sprite.Collection, new List<int>
            {
                startindex,
            }, "pet", tk2dSpriteAnimationClip.WrapMode.Loop).fps = 8f;

            // cai.gameObject.AddAnimation("pet", "CwaffingTheGungy/Resources/Companions/DroneCompanion/", 8,
            //     CompanionBuilder.AnimationType.Other);

            // CompanionBuilder.BuildAnimation(can, pet, spriteDirectory, fps);
            // wolfyboiai.animationAudioEvents = doggoai.animationAudioEvents;
        }
    }
}


