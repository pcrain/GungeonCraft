using System;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using ItemAPI;

using System.Collections.Generic;
using System.Linq;
using System.Text;

using Alexandria.Misc;

using MonoMod.RuntimeDetour;
using System.Reflection;


namespace CwaffingTheGungy
{

    public class RainCheck : AdvancedGunBehavior
    {
        public static void Add()
        {
            // Get yourself a new gun "base" first.
            // Let's just call it "Basic Gun", and use "jpxfrd" for all sprites and as "codename" All sprites must begin with the same word as the codename. For example, your firing sprite would be named "jpxfrd_fire_001".
            Gun gun = ETGMod.Databases.Items.NewGun("Rain Check", "eldermagnum2");
            // "kp:basic_gun determines how you spawn in your gun through the console. You can change this command to whatever you want, as long as it follows the "name:itemname" template.
            Game.Items.Rename("outdated_gun_mods:rain_check", "cg:rain_check");
            var comp = gun.gameObject.AddComponent<RainCheck>();
            //These two lines determines the description of your gun, ".SetShortDescription" being the description that appears when you pick up the gun and ".SetLongDescription" being the description in the Ammonomicon entry.
            gun.SetShortDescription("For a Rainy Day");
            gun.SetLongDescription("(Upon firing, bullets are delayed from moving until reloading, then move towards player. Switching away from this gun keeps bullets in stasis until switching back to this gun.)");
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
            gun.DefaultModule.ammoCost            = 1;
            gun.DefaultModule.shootStyle          = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle       = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime                        = 1.1f;
            gun.DefaultModule.cooldownTime        = 0.1f;
            gun.DefaultModule.numberOfShotsInClip = 20;
            gun.SetBaseMaxAmmo(250);
            // Here we just set the quality of the gun and the "EncounterGuid", which is used by Gungeon to identify the gun.
            gun.quality = PickupObject.ItemQuality.D;
            gun.encounterTrackable.EncounterGuid = "rain check rain check";
            //This block of code helps clone our projectile. Basically it makes it so things like Shadow Clone and Hip Holster keep the stats/sprite of your custom gun's projectiles.
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);

            projectile.gameObject.AddComponent<RainCheckBullets>();

            // projectile.hitEffects.overrideMidairDeathVFX = EasyVFXDatabase.RedLaserCircleVFX;
            // projectile.hitEffects.alwaysUseMidair = true;

            gun.DefaultModule.projectiles[0] = projectile;
             //projectile.baseData allows you to modify the base properties of your projectile module.
            //In our case, our gun uses modified projectiles from the ak-47.
            //You can modify a good number of stats but for now, let's just modify the damage and speed.
            projectile.baseData.damage = 5f;
            // projectile.baseData.speed  = 1.7f;
            projectile.baseData.speed  = 20.0f;
            projectile.transform.parent = gun.barrelOffset;
            //This determines what sprite you want your projectile to use. Note this isn't necessary if you don't want to have a custom projectile sprite.
            //The x and y values determine the size of your custom projectile
            // projectile.SetProjectileSpriteRight("build_projectile", 2, 2, null, null);
            // projectile.SetProjectileSpriteRight("build_projectile", 5, 5, true, tk2dBaseSprite.Anchor.MiddleCenter, 4, 4);
            // projectile.SetProjectileSpriteRight("build_projectile", 5, 5, true, tk2dBaseSprite.Anchor.MiddleCenter, 4, 4);
            ETGMod.Databases.Items.Add(gun, false, "ANY");

        }

        public override void OnPostFired(PlayerController player, Gun gun)
        {
            base.OnPostFired(player, gun);
            //This determines what sound you want to play when you fire a gun.
            //Sounds names are based on the Gungeon sound dump, which can be found at EnterTheGungeon/Etg_Data/StreamingAssets/Audio/GeneratedSoundBanks/Windows/sfx.txt
            // gun.PreventNormalFireAudio = true;
            // AkSoundEngine.PostEvent("Play_WPN_smileyrevolver_shot_01", gameObject);
        }

        //This block of code allows us to change the reload sounds.
        protected override void Update()
        {
            base.Update();
        }

        public override void OnReloadPressed(PlayerController player, Gun gun, bool manualReload)
        {
            base.OnReloadPressed(player, gun, manualReload);
            // if (gun.IsReloading && this.HasReloaded)
            // {
            //     HasReloaded = false;
            //     AkSoundEngine.PostEvent("Stop_WPN_All", base.gameObject);
            //     base.OnReloadPressed(player, gun, bSOMETHING);
            //     AkSoundEngine.PostEvent("Play_WPN_SAA_reload_01", base.gameObject);
            // }
        }

        public override void OnReload(PlayerController player, Gun gun)
        {
            base.OnReload(player, gun);
            LaunchAllBullets();
        }

        public override void OnSwitchedToThisGun()
        {
            base.OnSwitchedToThisGun();
            LaunchAllBullets();
        }

        public override void OnSwitchedAwayFromThisGun()
        {
            base.OnSwitchedAwayFromThisGun();
            PutAllBulletsInStasis();
        }


        private void LaunchAllBullets()
        {
            int num_found = 0;
            for (int i = 0; i < StaticReferenceManager.AllProjectiles.Count; i++)
            {
                Projectile projectile = StaticReferenceManager.AllProjectiles[i];
                if (projectile && projectile.Owner == gun.CurrentOwner && projectile.GetComponent<RainCheckBullets>())
                {
                    projectile.GetComponent<RainCheckBullets>().ForceMove(++num_found);
                }
            }
        }

        private void PutAllBulletsInStasis()
        {
            for (int i = 0; i < StaticReferenceManager.AllProjectiles.Count; i++)
            {
                Projectile projectile = StaticReferenceManager.AllProjectiles[i];
                if (projectile && projectile.Owner == gun.CurrentOwner && projectile.GetComponent<RainCheckBullets>())
                {
                    projectile.GetComponent<RainCheckBullets>().PutInStasis();
                }
            }
        }

        public RainCheck() {

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

    public class RainCheckBullets : MonoBehaviour
    {
        private const float RAINCHECK_MAX_TIMEOUT  = 10f;
        private const float RAINCHECK_LAUNCH_DELAY = 0.025f;

        private Projectile self;
        private PlayerController owner;
        private float initialSpeed;
        private float moveTimer;
        private bool launchSequenceStarted;
        private bool inStasis;
        private bool wasEverInStasis;
        private void Start()
        {
            this.self                  = base.GetComponent<Projectile>();
            this.owner                 = self.ProjectilePlayerOwner();
            this.initialSpeed          = self.baseData.speed;
            this.moveTimer             = RAINCHECK_MAX_TIMEOUT;
            this.launchSequenceStarted = false;
            this.inStasis              = false;
            this.wasEverInStasis       = false;

            self.baseData.speed = 0.1f;
            self.UpdateSpeed();

            // Reset the timers of all of our other RainCheckBullets, with a small delay
            int numRainProjectiles = 0;
            for (int i = 0; i < StaticReferenceManager.AllProjectiles.Count; i++)
            {
                Projectile projectile = StaticReferenceManager.AllProjectiles[i];
                if (projectile && projectile.Owner == self.Owner)
                {
                    var p = projectile.GetComponent<RainCheckBullets>();
                    if (p && !p.launchSequenceStarted)
                    {
                        p.moveTimer =
                            RAINCHECK_MAX_TIMEOUT - RAINCHECK_LAUNCH_DELAY * numRainProjectiles;
                        ++numRainProjectiles;
                    }
                }
            }

            StartCoroutine(DoSpeedChange());
        }

        private IEnumerator DoSpeedChange()
        {
            while (this.inStasis || this.moveTimer > 0)
            {
                this.moveTimer -= BraveTime.DeltaTime;
                if (!self) break;
                yield return null;
            }
            this.launchSequenceStarted = true;
            self.baseData.speed        = this.initialSpeed;
            if (this.owner)
            {
                Vector2 dirToPlayer = self.sprite.WorldCenter.CalculateVectorBetween(this.owner.sprite.WorldCenter);
                self.SendInDirection(dirToPlayer, true);
            }
            self.UpdateSpeed();
        }

        public void ForceMove(int index)
        {
            if (!this.launchSequenceStarted)
            {  //no resetting our timers after this function has been called once
                this.launchSequenceStarted = true;
                this.moveTimer             = index * RAINCHECK_LAUNCH_DELAY;
                this.inStasis              = false;
            }
        }

        public void PutInStasis()
        {
            if (!this.wasEverInStasis)
            {
                this.inStasis        = true;
                this.wasEverInStasis = true;
            }
        }
    }
}
