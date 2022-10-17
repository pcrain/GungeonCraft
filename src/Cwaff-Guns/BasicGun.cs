using System;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using ItemAPI;

using System.Collections.Generic;
using System.Linq;
using System.Text;

// namespace CwaffingTheGungy
// {

//     public class ElderMagnum : AdvancedGunBehavior
//     {


//         public static void Add()
//         {

//             Gun gun = ETGMod.Databases.Items.NewGun("Elder Magnum", "eldermagnum2");

//             Game.Items.Rename("outdated_gun_mods:elder_magnum", "nn:elder_magnum");
//             var comp = gun.gameObject.AddComponent<ElderMagnum>();

//             gun.SetShortDescription("Guncestral");
//             gun.SetLongDescription("An ancient firearm, left to age in some safe over hundreds of years."+"\n\nWhoever owned this gun has probably been slinging since before your great grandpappy was born.");

//             gun.SetupSprite(null, "eldermagnum2_idle_001", 8);

//             gun.SetAnimationFPS(gun.shootAnimation, 14);
//             gun.gunSwitchGroup = (PickupObjectDatabase.GetById(198) as Gun).gunSwitchGroup;
//             gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(80) as Gun, true, false);

//             gun.DefaultModule.ammoCost = 1;
//             gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
//             gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
//             gun.reloadTime = 1.3f;
//             gun.DefaultModule.cooldownTime = 0.1f;
//             gun.DefaultModule.numberOfShotsInClip = 7;
//             gun.barrelOffset.transform.localPosition = new Vector3(1.5f, 0.81f, 0f);
//             gun.InfiniteAmmo = true;
//             gun.gunClass = GunClass.SHITTY;
//             Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
//             projectile.gameObject.SetActive(false);
//             FakePrefab.MarkAsFakePrefab(projectile.gameObject);
//             UnityEngine.Object.DontDestroyOnLoad(projectile);
//             // projectile.hitEffects.overrideMidairDeathVFX = EasyVFXDatabase.RedLaserCircleVFX;
//             projectile.hitEffects.alwaysUseMidair = true;
//             projectile.SetProjectileSpriteRight("eldermagnum_projectile", 5, 5, true, tk2dBaseSprite.Anchor.MiddleCenter, 4, 4);
//             gun.DefaultModule.projectiles[0] = projectile;

//             gun.quality = PickupObject.ItemQuality.EXCLUDED;
//             ETGMod.Databases.Items.Add(gun, false, "ANY");

//         }
//         public ElderMagnum()
//         {

//         }
//     }
// }

namespace CwaffingTheGungy
{

    public class BasicGun : AdvancedGunBehavior
    {
        public static void Add()
        {
            // Get yourself a new gun "base" first.
            // Let's just call it "Basic Gun", and use "jpxfrd" for all sprites and as "codename" All sprites must begin with the same word as the codename. For example, your firing sprite would be named "jpxfrd_fire_001".
            Gun gun = ETGMod.Databases.Items.NewGun("Basic Gun", "eldermagnum2");
            // "kp:basic_gun determines how you spawn in your gun through the console. You can change this command to whatever you want, as long as it follows the "name:itemname" template.
            Game.Items.Rename("outdated_gun_mods:basic_gun", "kp:basic_gun");
            var comp = gun.gameObject.AddComponent<BasicGun>();
            //These two lines determines the description of your gun, ".SetShortDescription" being the description that appears when you pick up the gun and ".SetLongDescription" being the description in the Ammonomicon entry.
            gun.SetShortDescription("Impressionable");
            gun.SetLongDescription("A gun left unfinished and abandoned by its creator. It still has great potential.");
            // This is required, unless you want to use the sprites of the base gun.
            // That, by default, is the pea shooter.
            // SetupSprite sets up the default gun sprite for the ammonomicon and the "gun get" popup.
            // WARNING: Add a copy of your default sprite to Ammonomicon Encounter Icon Collection!
            // That means, "sprites/Ammonomicon Encounter Icon Collection/defaultsprite.png" in your mod .zip. You can see an example of this with inside the mod folder.
            gun.SetupSprite(null, "eldermagnum2_idle_001", 8);
            // ETGMod automatically checks which animations are available.
            // The numbers next to "shootAnimation" determine the animation fps. You can also tweak the animation fps of the reload animation and idle animation using this method.
            gun.SetAnimationFPS(gun.shootAnimation, 24);

            // PAC: this needs to be added?
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(198) as Gun).gunSwitchGroup;

            // Every modded gun has base projectile it works with that is borrowed from other guns in the game.
            // The gun names are the names from the JSON dump! While most are the same, some guns named completely different things. If you need help finding gun names, ask a modder on the Gungeon discord.
            // gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(80) as Gun, true, false);
            gun.AddProjectileModuleFrom("ak-47", true, false);
             // Here we just take the default projectile module and change its settings how we want it to be.
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1.1f;
            gun.DefaultModule.cooldownTime = 0.1f;
            gun.DefaultModule.numberOfShotsInClip = 6;
            gun.SetBaseMaxAmmo(250);
            // Here we just set the quality of the gun and the "EncounterGuid", which is used by Gungeon to identify the gun.
            gun.quality = PickupObject.ItemQuality.D;
            gun.encounterTrackable.EncounterGuid = "why isn't this thing creating projectiles";
            //This block of code helps clone our projectile. Basically it makes it so things like Shadow Clone and Hip Holster keep the stats/sprite of your custom gun's projectiles.
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);

            // projectile.hitEffects.overrideMidairDeathVFX = EasyVFXDatabase.RedLaserCircleVFX;
            projectile.hitEffects.alwaysUseMidair = true;

            gun.DefaultModule.projectiles[0] = projectile;
             //projectile.baseData allows you to modify the base properties of your projectile module.
            //In our case, our gun uses modified projectiles from the ak-47.
            //You can modify a good number of stats but for now, let's just modify the damage and speed.
            projectile.baseData.damage = 5f;
            projectile.baseData.speed = 1.7f;
            projectile.transform.parent = gun.barrelOffset;
            //This determines what sprite you want your projectile to use. Note this isn't necessary if you don't want to have a custom projectile sprite.
            //The x and y values determine the size of your custom projectile
            // projectile.SetProjectileSpriteRight("build_projectile", 2, 2, null, null);
            // projectile.SetProjectileSpriteRight("build_projectile", 5, 5, true, tk2dBaseSprite.Anchor.MiddleCenter, 4, 4);
            projectile.SetProjectileSpriteRight("build_projectile", 5, 5, true, tk2dBaseSprite.Anchor.MiddleCenter, 4, 4);
            ETGMod.Databases.Items.Add(gun, false, "ANY");

        }

        public override void OnPostFired(PlayerController player, Gun gun)
        {
            //This determines what sound you want to play when you fire a gun.
            //Sounds names are based on the Gungeon sound dump, which can be found at EnterTheGungeon/Etg_Data/StreamingAssets/Audio/GeneratedSoundBanks/Windows/sfx.txt
            gun.PreventNormalFireAudio = true;
            AkSoundEngine.PostEvent("Play_WPN_smileyrevolver_shot_01", gameObject);
        }
        private bool HasReloaded;
        //This block of code allows us to change the reload sounds.
       protected new void Update()
        {
            if (gun.CurrentOwner)
            {

                if (!gun.PreventNormalFireAudio)
                {
                    this.gun.PreventNormalFireAudio = true;
                }
                if (!gun.IsReloading && !HasReloaded)
                {
                    this.HasReloaded = true;
                }
            }
        }

        public override void OnReloadPressed(PlayerController player, Gun gun, bool bSOMETHING)
        {
            if (gun.IsReloading && this.HasReloaded)
            {
                HasReloaded = false;
                AkSoundEngine.PostEvent("Stop_WPN_All", base.gameObject);
                base.OnReloadPressed(player, gun, bSOMETHING);
                AkSoundEngine.PostEvent("Play_WPN_SAA_reload_01", base.gameObject);
            }
        }

        public BasicGun() {

        }
        //Now add the Tools class to your project.
        //All that's left now is sprite stuff.
        //Your sprites should be organized, like how you see in the mod folder.
        //Every gun requires that you have a .json to match the sprites or else the gun won't spawn at all
        //.Json determines the hand sprites for your character. You can make a gun two handed by having both "SecondaryHand" and "PrimaryHand" in the .json file, which can be edited through Notepad or Visual Studios
        //By default this gun is a one-handed weapon
        //If you need a basic two handed .json. Just use the jpxfrd2.json.
        //And finally, don't forget to add your Gun to your ETGModule class!
    }
}
