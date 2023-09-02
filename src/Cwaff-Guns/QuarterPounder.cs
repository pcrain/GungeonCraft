using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using UnityEngine;
using MonoMod.RuntimeDetour;

using Gungeon;
using Dungeonator;
using Alexandria.ItemAPI;
using Alexandria.Misc;

namespace CwaffingTheGungy
{
    public class QuarterPounder : AdvancedGunBehavior
    {
        public static string ItemName         = "Quarter Pounder";
        public static string SpriteName       = "quarter_pounder";
        public static string ProjectileName   = "38_special";
        public static string ShortDescription = "Pay Per Pew";
        public static string LongDescription  = "(shoots money O:)";

        // internal static tk2dSpriteAnimationClip _BulletSprite;
        internal static float                   _BaseCooldownTime = 0.4f;
        internal static int                     _FireAnimationFrames = 8;

        private const float _NATASHA_PROJECTILE_SCALE = 0.5f;
        private float _speedmult               = 1.0f;

        public static void Add()
        {
            Gun gun = Lazy.SetupGun(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription);
                gun.gunSwitchGroup                    = (ItemHelper.Get(Items.GunslingersAshes) as Gun).gunSwitchGroup;
                gun.DefaultModule.ammoCost            = 1;
                gun.DefaultModule.shootStyle          = ProjectileModule.ShootStyle.Automatic;
                gun.DefaultModule.sequenceStyle       = ProjectileModule.ProjectileSequenceStyle.Random;
                gun.reloadTime                        = 1.1f;
                gun.DefaultModule.angleVariance       = 15.0f;
                gun.DefaultModule.cooldownTime        = _BaseCooldownTime;
                gun.DefaultModule.numberOfShotsInClip = 2500;
                gun.quality                           = PickupObject.ItemQuality.D;
                gun.SetBaseMaxAmmo(2500);
                gun.CurrentAmmo = 2500;

            var comp = gun.gameObject.AddComponent<QuarterPounder>();
            Projectile projectile = Lazy.PrefabProjectileFromGun(gun);
        }

        protected override void OnPickedUpByPlayer(PlayerController player)
        {
            ETGModConsole.Log($"picking up gun");
            base.OnPickedUpByPlayer(player);

            ETGModConsole.Log($"setting up shader");
            // Shader s = Shader.Find("CG/PretzelCustom");
            Shader s = Shader.Find("PretzelCustom");
            // Shader s = Shader.Find("Invisible");
            if (!s)
            {
                ETGModConsole.Log($"failed to set up shader");
                return;
            }

            Material m = player.sprite.renderer.material;
                m.shader = s;
                m.SetFloat("_PhantomGradientScale", 0.75f);
                m.SetFloat("_PhantomContrastPower", 1.3f);
            ETGModConsole.Log($"set up shader");
        }

        protected override void OnPostDroppedByPlayer(PlayerController player)
        {
            base.OnPostDroppedByPlayer(player);
        }
    }
}
