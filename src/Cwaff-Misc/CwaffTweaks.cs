using ItemAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;  //debug
using System.ComponentModel;  //debug

using UnityEngine;


namespace CwaffingTheGungy
{
    public static class CwaffTweaks
    {
        private static DirectionalAnimation dogPettingAnimation;

        public static void Init()
        {
            GetDogPettingAnimation();

            string[] testPet = new string[] {
                "CwaffingTheGungy/Resources/Companions/Wolf/wolf_pet_001",
                "CwaffingTheGungy/Resources/Companions/Wolf/wolf_pet_002",
                "CwaffingTheGungy/Resources/Companions/Wolf/wolf_pet_003",
                "CwaffingTheGungy/Resources/Companions/Wolf/wolf_pet_004",
                "CwaffingTheGungy/Resources/Companions/Wolf/wolf_pet_005",
                "CwaffingTheGungy/Resources/Companions/Wolf/wolf_pet_006",
            };

            string[] testPet2 = new string[] {
                "CwaffingTheGungy/Resources/Companions/Wolf/wolf_pet_left_001",  //TODO: testing jankiness
                "CwaffingTheGungy/Resources/Companions/Wolf/wolf_pet_left_002",
                "CwaffingTheGungy/Resources/Companions/Wolf/wolf_pet_left_003",
                "CwaffingTheGungy/Resources/Companions/Wolf/wolf_pet_left_004",
                "CwaffingTheGungy/Resources/Companions/Wolf/wolf_pet_left_005",
                "CwaffingTheGungy/Resources/Companions/Wolf/wolf_pet_left_006",
            };

            //Make Wolf pettable
            string[] defaultCompanions = new string[] {
                "wolf","junkan","turkey","baby_good_mimic","baby_good_shelleton","super_space_turtle",
                "r2g2","blank_companions_ring","badge","pig","chicken_flute",
            };
            foreach(string c in defaultCompanions)
            {
                if (C.DEBUG_BUILD)
                    ETGModConsole.Log("Making "+c+" pettable");
                Gungeon.Game.Items[c].GetComponent<CompanionItem>().MakePettable(testPet,testPet2);
            }
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

        public static void MakePettable(this CompanionItem ci, string[] pettingAnimation = null, string[] pettingAnimationLeft = null)
        {
            // Get the companion controller for the companion
            CompanionController cc =
                EnemyDatabase.GetOrLoadByGuid(ci.CompanionGuid).gameObject.GetOrAddComponent<CompanionController>();

            // Make it pettable
            cc.CanBePet = true;

            // Give it a petting animation
            if (pettingAnimation != null)
            {
                List<int> animIndicesRight = new List<int>{};
                for (int i = 0; i < pettingAnimation.Length; i++)
                    animIndicesRight.Add(SpriteBuilder.AddSpriteToCollection(pettingAnimation[i], cc.sprite.Collection));
                tk2dSpriteAnimationClip anim_right = SpriteBuilder.AddAnimation(cc.sprite.spriteAnimator, cc.sprite.Collection, animIndicesRight,
                    "pet_right", tk2dSpriteAnimationClip.WrapMode.Loop);
                anim_right.fps = 8f;

               List<int> animIndicesLeft = new List<int>{};
                for (int i = 0; i < pettingAnimationLeft.Length; i++)
                    animIndicesLeft.Add(SpriteBuilder.AddSpriteToCollection(pettingAnimationLeft[i], cc.sprite.Collection));
                tk2dSpriteAnimationClip anim_left = SpriteBuilder.AddAnimation(cc.sprite.spriteAnimator, cc.sprite.Collection, animIndicesLeft,
                    "pet_left", tk2dSpriteAnimationClip.WrapMode.Loop);
                anim_left.fps = 8f;

                // BIG TODO: figure out why mirroring doesn't work for sprites loaded from the same path
                AIAnimator.NamedDirectionalAnimation newOtheranim = new AIAnimator.NamedDirectionalAnimation
                {
                    name = "pet",
                    anim = new DirectionalAnimation
                    {
                        Prefix = "pet",
                        Type = DirectionalAnimation.DirectionType.TwoWayHorizontal,
                        Flipped = new DirectionalAnimation.FlipType[]{
                            DirectionalAnimation.FlipType.None,
                            DirectionalAnimation.FlipType.None,
                            // DirectionalAnimation.FlipType.Mirror,
                            // DirectionalAnimation.FlipType.Mirror,
                        },
                        AnimNames = new string[2]{"pet_right","pet_left"},
                    }
                };

                if (cc.sprite.aiAnimator.OtherAnimations == null)
                    cc.sprite.aiAnimator.OtherAnimations = new List<AIAnimator.NamedDirectionalAnimation>();
                cc.sprite.aiAnimator.OtherAnimations.Add(newOtheranim);

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


