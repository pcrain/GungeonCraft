namespace CwaffingTheGungy;

public static class CwaffTweaks
{
    public static void Init()
    {
        //Make Wolf pettable
        string[] defaultCompanions = new string[] {
            "wolf",
            // "junkan","turkey","baby_good_mimic","baby_good_shelleton","super_space_turtle",
            // "r2g2","blank_companions_ring","badge","pig","chicken_flute",
        };
        foreach(string c in defaultCompanions)
        {
            if (C.DEBUG_BUILD)
                ETGModConsole.Log("Making "+c+" pettable");
            Gungeon.Game.Items[c].GetComponent<CompanionItem>().MakePettable(
                ResMap.Get("wolf_pet"),
                ResMap.Get("wolf_pet_left"));
        }
    }

    private static DirectionalAnimation GetDogPettingAnimation() =>
        EnemyDatabase.GetOrLoadByGuid(ItemHelper.Get(Items.Dog).GetComponent<CompanionItem>().CompanionGuid)
            .aiAnimator.OtherAnimations.Find(n => n.name == "pet").anim;

    public static void MakePettable(this CompanionItem ci, List<string> pettingAnimation = null, List<string> pettingAnimationLeft = null)
    {
        // Get the companion controller for the companion
        CompanionController cc =
            EnemyDatabase.GetOrLoadByGuid(ci.CompanionGuid).gameObject.GetOrAddComponent<CompanionController>();

        // Make it pettable
        cc.CanBePet = true;

        // Give it a petting animation
        if (pettingAnimation != null)
        {
            List<int> animIndicesRight = AtlasHelper.AddSpritesToCollection(pettingAnimation, cc.sprite.Collection).AsRange();
            tk2dSpriteAnimationClip anim_right = SpriteBuilder.AddAnimation(cc.sprite.spriteAnimator, cc.sprite.Collection, animIndicesRight,
                "pet_right", tk2dSpriteAnimationClip.WrapMode.Loop);
            anim_right.fps = 8f;

            List<int> animIndicesLeft = AtlasHelper.AddSpritesToCollection(pettingAnimationLeft, cc.sprite.Collection).AsRange();
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
        // else //...or just copy it from the dog while testing
        // {
        //     cc.sprite.aiAnimator.OtherAnimations.Add(new AIAnimator.NamedDirectionalAnimation {
        //         name = "pet",
        //         anim = GetDogPettingAnimation(),
        //     });
        // }


        // wolfyboiai.animationAudioEvents = doggoai.animationAudioEvents;
    }
}
