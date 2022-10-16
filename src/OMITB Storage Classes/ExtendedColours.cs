using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NevernamedsItems
{
    public class ExtendedColours
    {
        //New Colours
        public static Color carrionRed = new Color(176 / 255f, 4f / 255f, 4f / 255f);
        public static Color pink = new Color(242f / 255f, 116f / 255f, 225f / 255f);
        public static Color paleYellow = new Color(242f / 255f, 238f / 255f, 148f / 255f);
        public static Color lime = new Color(111f / 255f, 252f / 255f, 3f / 255f);
        public static Color brown = new Color(122f / 255f, 71f / 255f, 16f / 255f);
        public static Color orange = new Color(240f / 255f, 160f / 255f, 22f / 255f);
        public static Color vibrantOrange = new Color(255f / 255f, 144f / 255f, 41f / 255f);
        public static Color purple = new Color(171f / 255f, 22f / 255f, 240f / 255f);
        public static Color skyblue = new Color(130f / 255f, 230f / 255f, 2255f / 255f);
        public static Color honeyYellow = new Color(255f / 255f, 180f / 255f, 18f / 255f);
        public static Color maroon = new Color(105f / 255f, 7f / 255f, 9f / 255f);
        public static Color veryDarkRed = new Color(71f / 255f, 4f / 255f, 3f / 255f);
        public static Color plaguePurple = new Color(242f / 255f, 161f / 255f, 255f / 255f);
        public static Color darkBrown = new Color(74f / 255f, 22f / 255f, 5f / 255f);

        //Colours  taken from Status effects
        public static Color freezeBlue = StaticStatusEffects.chaosBulletsFreeze.TintColor;
        public static Color poisonGreen = StaticStatusEffects.irradiatedLeadEffect.TintColor;
        public static Color charmPink = StaticStatusEffects.charmingRoundsEffect.TintColor;

        //Colours taken from items
        public static Color cursedBulletsPurple = Gungeon.Game.Items["cursed_bullets"].GetComponent<ScalingStatBoostItem>().TintColor;
        public static Color gildedBulletsGold = Gungeon.Game.Items["gilded_bullets"].GetComponent<ScalingStatBoostItem>().TintColor;
        public static Color silvedBulletsSilver = Gungeon.Game.Items["silver_bullets"].GetComponent<SilverBulletsPassiveItem>().TintColor;
        public static Color frostBulletsTint = Gungeon.Game.Items["frost_bullets"].GetComponent<BulletStatusEffectItem>().TintColor;
        public static Color shadowBulletsBlue = new Color(0.35f, 0.25f, 0.65f, 1f);

    }
}
