// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.Linq;
// using System.Text;
// using System.Reflection;

// using UnityEngine;
// using MonoMod;
// using MonoMod.RuntimeDetour;
// using Gungeon;
// using Alexandria.Misc;
// using Alexandria.ItemAPI;

// namespace CwaffingTheGungy
// {
//     public class TestLightning : AdvancedGunBehavior
//     {
//         public static string gunName          = "TestLightning";
//         public static string spriteName       = "arccannon";
//         public static string projectileName   = "ak-47";
//         public static string shortDescription = "Bzzt";
//         public static string longDescription  = "(Zap)";

//         public static Projectile defaultProjectile;

//         public static void Add()
//         {
//             Gun gun = Lazy.InitGunFromStrings(gunName, spriteName, projectileName, shortDescription, longDescription);

//             gun.gunSwitchGroup                    = (ItemHelper.Get(Items.GunslingersAshes) as Gun).gunSwitchGroup;
//             gun.DefaultModule.ammoCost            = 1;
//             gun.DefaultModule.shootStyle          = ProjectileModule.ShootStyle.SemiAutomatic;
//             gun.DefaultModule.sequenceStyle       = ProjectileModule.ProjectileSequenceStyle.Random;
//             gun.reloadTime                        = 1.1f;
//             gun.DefaultModule.cooldownTime        = 0.1f;
//             gun.DefaultModule.numberOfShotsInClip = 20;
//             gun.quality                           = PickupObject.ItemQuality.D;
//             gun.SetBaseMaxAmmo(250);
//             gun.SetAnimationFPS(gun.shootAnimation, 24);

//             Projectile projectile       = Lazy.PrefabProjectileFromGun(gun);
//             projectile.baseData.damage  = 5f;
//             // projectile.baseData.speed   = 50.0f;
//             projectile.baseData.speed   = 0.001f;
//             projectile.transform.parent = gun.barrelOffset;

//             // UnityEngine.Object.Destroy(projectile.GetComponent<TrailController>());
//             // List<string> BeamAnimPaths = new List<string>()
//             // {
//             //     "CwaffingTheGungy/Resources/TrailSprites/bigarctrail_mid_001",
//             //     "CwaffingTheGungy/Resources/TrailSprites/bigarctrail_mid_002",
//             //     "CwaffingTheGungy/Resources/TrailSprites/bigarctrail_mid_003",
//             // };

//             // projectile.AddTrailToProjectile(
//             //     "CwaffingTheGungy/Resources/TrailSprites/bigarctrail_mid_001",
//             //     new Vector2(8, 7),
//             //     new Vector2(1, 1),
//             //     BeamAnimPaths, 20,
//             //     BeamAnimPaths, 20,
//             //     -1,
//             //     0.0001f,
//             //     -1,
//             //     true
//             //     );
//             // EmissiveTrail emis = projectile.gameObject.GetOrAddComponent<EmissiveTrail>();

//             defaultProjectile = projectile;
//         }
//     }
// }
