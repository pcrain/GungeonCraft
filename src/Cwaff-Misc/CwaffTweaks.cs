namespace CwaffingTheGungy;

public static class CwaffTweaks
{
    public static void Init()
    {
        //Make Wolf pettable
        Gungeon.Game.Items["wolf"].GetComponent<CompanionItem>().MakePettable(
            ResMap.Get("wolf_pet"),
            ResMap.Get("wolf_pet_left"));

        // Other stuff
        JammedLies.Init();
        TheCake.Init();
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
                    Flipped = new DirectionalAnimation.FlipType[2],
                    AnimNames = new string[2]{"pet_right","pet_left"},
                }
            };

            cc.sprite.aiAnimator.OtherAnimations ??= new List<AIAnimator.NamedDirectionalAnimation>();
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

    [HarmonyPatch(typeof(Chest), nameof(Chest.ConfigureOnPlacement))]
    private class ChestConfigureLiesOnPlacementPatch
    {
        private const float _SPECIAL_LIES_CHANCE = 0.25f;

        private static void Postfix(Chest __instance)
        {
            if (__instance.overrideJunkId < 0)
                return;
            if (UnityEngine.Random.value > _SPECIAL_LIES_CHANCE)
                return;
            if (PlayerStats.GetTotalCurse() == 0)
                __instance.overrideJunkId = JammedLies._PickupId;
            else
                __instance.overrideJunkId = TheCake._PickupId;
        }
    }

    public class TheCake : CwaffPassive
    {
        public static string ItemName         = "The Cake";
        public static string ShortDescription = "Black Mesa Forest";
        public static string LongDescription  = "A tasty looking cake. For some reason, you want it gone and out of your sight.";
        public static string Lore             = "";

        internal static int _PickupId;

        public static void Init()
        {
            PassiveItem item = Lazy.SetupPassive<TheCake>(ItemName, ShortDescription, LongDescription, Lore, hideFromAmmonomicon: true);
            item.quality = ItemQuality.SPECIAL;
            item.ShouldBeExcludedFromShops = true;  // don't show up in shops
            _PickupId = item.PickupObjectId;
        }
    }

    public class JammedLies : CwaffPassive
    {
        public static string ItemName         = "Jammed Lies";
        public static string ShortDescription = "There Won't Be a Next Time";
        public static string LongDescription  = "You get the feeling you should probably dispose of this at your earliest convenience.";
        public static string Lore             = "";

        internal static int _PickupId;

        public static void Init()
        {
            PassiveItem item = Lazy.SetupPassive<JammedLies>(ItemName, ShortDescription, LongDescription, Lore, hideFromAmmonomicon: true);
            item.quality = ItemQuality.SPECIAL;
            item.ShouldBeExcludedFromShops = true;  // don't show up in shops
            item.passiveStatModifiers = [new StatModifier(){
                statToBoost = PlayerStats.StatType.Curse,
                amount      = 5,
                modifyType  = StatModifier.ModifyMethod.ADDITIVE}];
            _PickupId = item.PickupObjectId;
        }

        [HarmonyPatch(typeof(SellCellController), nameof(SellCellController.HandleSoldItem), MethodType.Enumerator)]
        private class SellJammedLiesPatch
        {
            [HarmonyILManipulator]
            private static void SellJammedLiesIL(ILContext il, MethodBase original)
            {
                ILCursor cursor = new ILCursor(il);
                Type ot = original.DeclaringType;
                string sellPriceFieldName = ot.GetEnumeratorFieldName("sellPrice");
                if (!cursor.TryGotoNext(MoveType.After, // move after the block forcing the price to 3 for excldued items
                    instr => instr.MatchLdcI4(3),
                    instr => instr.MatchStfld(ot, sellPriceFieldName)))
                    return;
                if (!cursor.TryGotoNext(MoveType.After, // move after the point where the sell price is next loaded
                    instr => instr.MatchLdfld(ot, sellPriceFieldName)))
                    return;

                FieldInfo sellPriceField = ot.GetField(sellPriceFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldfld, ot.GetField("targetItem", BindingFlags.Instance | BindingFlags.NonPublic));
                cursor.CallPrivate(typeof(SellJammedLiesPatch), nameof(AdjustPrice));
            }

            private static int AdjustPrice(int oldPrice, PickupObject p)
            {
                if (p is JammedLies)
                    return 666;
                return oldPrice;
            }
        }
    }
}

