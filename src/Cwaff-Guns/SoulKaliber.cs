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
    public class SoulKaliber : AdvancedGunBehavior
    {
        public static string ItemName         = "Soul Kaliber";
        public static string SpriteName       = "ringer";
        public static string ProjectileName   = "ak-47";
        public static string ShortDescription = "Gundead or Alive";
        public static string LongDescription  = "(hitting an enemy gives them the soul link status effect, making all soul linked enemies take damage when any enemy is hit)";

        public static void Add()
        {
            Gun gun = Lazy.SetupGun(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription);

            var comp = gun.gameObject.AddComponent<SoulKaliber>();
            comp.preventNormalFireAudio = true;

            gun.muzzleFlashEffects.type           = VFXPoolType.None;
            gun.gunSwitchGroup                    = (ItemHelper.Get(Items.GunslingersAshes) as Gun).gunSwitchGroup;
            gun.DefaultModule.ammoCost            = 1;
            gun.DefaultModule.shootStyle          = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle       = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime                        = 1.1f;
            gun.DefaultModule.cooldownTime        = 0.1f;
            gun.DefaultModule.numberOfShotsInClip = 10;
            gun.quality                           = PickupObject.ItemQuality.D;
            gun.SetBaseMaxAmmo(250);
            gun.SetAnimationFPS(gun.shootAnimation, 24);

            Projectile projectile = Lazy.PrefabProjectileFromGun(gun);
                projectile.baseData.speed   = 30.0f;
                projectile.baseData.damage  = 1f;
                projectile.gameObject.AddComponent<SoulLinkProjectile>();
        }
    }

    public class SoulLinkProjectile : MonoBehaviour
    {
        private void Start()
        {
            base.GetComponent<Projectile>().OnHitEnemy += (Projectile _, SpeculativeRigidbody enemy, bool _) =>
                enemy.aiActor.gameObject.GetOrAddComponent<SoulLinkStatus>();
        }
    }
}
