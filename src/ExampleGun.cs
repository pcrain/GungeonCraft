/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace GunAPI
{
    class ExampleCannon : AdvancedGunBehavior
    {
        public static void Init()
        {
            //Sets up the gun's name, sprite prefix, short and long descriptions, and mod prefix.
            Gun gun = GunBuilder.BuildGun("Example Cannon", "exampl3", "This is an Example!", "This gun is used by professional gunsmithes to teach younger gunsmithes how to make guns.", "spapi");
            ExampleCannon behaviour = gun.gameObject.AddComponent<ExampleCannon>();
            //Determines whether or not the gun uses generic default sounds when shot.
            behaviour.preventNormalFireAudio = true;
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(338) as Gun).gunSwitchGroup;
            //Determines how fast the gun's shooting animation plays. Change the number at the end (16 in this case) to change it's FPS. 
            //Guns in base gungeon typically have an FPS of 12 to 16.
            GunExt.SetAnimationFPS(gun, gun.shootAnimation, 16);
            //Determines what kind of gun the gun is. The valid types are: 
                //Semiautomatic - The gun can be held to fire it, but fires much faster when spam-clicked.
                //Automatic - The gun fires automatically at it's fastest while fire is held down.
                //Burst - Upon being fired, the gun rapid-fires more bullets out of it's clip automatically.
                //Charge - Fire must be held down to charge up the gun, before releasing to fire.
                //Beam - The gun fires a continuous beam while fire is held. 
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Charged;
            //'AngleVariance' determines the gun's accuracy. A gun with '45' angle variance will fire it's bullets anywhere within 45 degrees of the aim direction.
            //A gun with 0 AngleVariance is perfectly accurate.
            gun.DefaultModule.angleVariance = 0;
            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.MEDIUM_BULLET;
            //Determines what bullet this gun's bullet is based off of. Guns use base game guns as bases for their bullets, before modifying the stats and appearance.
            //In this case, it's set to '38', which is the ID of the Magnum.
            Projectile projectile = ProjectileHandler.CloneProjectile((PickupObjectDatabase.GetById(38) as Gun).DefaultModule.projectiles[0]);
            //A full list of all gun and item numerical IDs can be found here: https://raw.githubusercontent.com/ModTheGungeon/ETGMod/master/Assembly-CSharp.Base.mm/Content/gungeon_id_map/items.txt
            gun.DefaultModule.chargeProjectiles.Add(GunBuilder.ConstructChargeProjectile(1, projectile));
            projectile.transform.parent = gun.barrelOffset;
            //Sets the damage and movement speed of the gun's bullet.
            projectile.baseData.damage = 66f;
            projectile.baseData.speed = 13;
            projectile.AnimateProjectile(new List<string> { "rad_b_projectile", "rad_u_projectile", "rad_l_projectile", "rad_l_projectile", "rad_e_projectile", "rad_t_projectile" }, 9, true,
                new List<IntVector2> { new IntVector2(8, 9), new IntVector2(8, 9), new IntVector2(6, 9), new IntVector2(6, 9), new IntVector2(8, 9), new IntVector2(8, 9) }, new List<bool> { true, true, true, true, true, true },
                new List<tk2dBaseSprite.Anchor> { tk2dBaseSprite.Anchor.MiddleCenter, tk2dBaseSprite.Anchor.MiddleCenter, tk2dBaseSprite.Anchor.MiddleCenter, tk2dBaseSprite.Anchor.MiddleCenter, tk2dBaseSprite.Anchor.MiddleCenter,
                    tk2dBaseSprite.Anchor.MiddleCenter }, new List<Vector3?> { Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero }, new List<IntVector2?> { null, null, null, null, null, null },
                new List<IntVector2?> { null, null, null, null, null, null }, new List<Projectile> { null, null, null, null, null, null });
            gun.clipObject = GunBuilder.CreateCustomClip("exampl_clip_001", 30, 9);
            gun.reloadClipLaunchFrame = 0;
            //Determines whether or not the gun uses generic default sounds when shot again, and also allows you to play a different sound upon firing instead.
            gun.PreventNormalFireAudio = true;
            gun.OverrideNormalFireAudioEvent = "Play_WPN_blasphemy_shot_01";
            //How fast the gun can fire. Most noticeable in Semiautomatic and Automatic weapons. Higher numbers means the gun has to wait longer after firing before it can fire again.
            gun.DefaultModule.cooldownTime = 1;
            //How many shots the gun has in it's clip.
            gun.DefaultModule.numberOfShotsInClip = 7;
            //How long the gun takes to reload. Higher numbers means a longer reload time. Check the Enter the Gungeon wiki to see base gun reload times if you want something to base it off.
            gun.reloadTime = 2;
            //The gun's maximum ammo.
            gun.SetBaseMaxAmmo(513);
            //What loot tier the gun is in. Set to EXCLUDED if you don't want the gun to appear in chests.
            gun.quality = PickupObject.ItemQuality.C;
            //Allows you to change where the bullets spawn from on the gun's sprite. The first two numbers (1.7 and 0.65 in this case) are the X and Y values.
            //To convert your gun's barrel position to usable information, take the X and Y pixel co-ordinate of your guns sprite on it's idle sprite or the first frame of it's firing animation (counting from the bottom left) and divide those numbers by 16.
            gun.barrelOffset.transform.localPosition = new Vector3(1.7f, 0.65f, 0f);
            gun.gunClass = GunClass.PISTOL;
            gun.AddCurrentGunDamageTypeModifier(CoreDamageTypes.Fire, 0f);
        }
    }
}*/
