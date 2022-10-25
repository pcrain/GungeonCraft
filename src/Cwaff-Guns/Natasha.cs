using System;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
// using ItemAPI;

using System.Collections.Generic;
using System.Linq;
using System.Text;

using Alexandria.Misc;

using MonoMod.RuntimeDetour;
using System.Reflection;

using Alexandria.ItemAPI;
// using SaveAPI;
// using Dungeonator;

namespace CwaffingTheGungy
{

    public class Natasha : AdvancedGunBehavior
    {
        private static float baseCooldownTime = 0.4f;
        private float speedmult               = 1.0f;

        public static void Add()
        {
            // Get yourself a new gun "base" first.
            // Let's just call it "Basic Gun", and use "jpxfrd" for all sprites and as "codename" All sprites must begin with the same word as the codename. For example, your firing sprite would be named "jpxfrd_fire_001".
            Gun gun = ETGMod.Databases.Items.NewGun("Natasha", "eldermagnum2");
            // "kp:basic_gun determines how you spawn in your gun through the console. You can change this command to whatever you want, as long as it follows the "name:itemname" template.
            Game.Items.Rename("outdated_gun_mods:natasha", "cg:natasha");
            var comp = gun.gameObject.AddComponent<Natasha>();
            //These two lines determines the description of your gun, ".SetShortDescription" being the description that appears when you pick up the gun and ".SetLongDescription" being the description in the Ammonomicon entry.
            gun.SetShortDescription("Fear no Man");
            gun.SetLongDescription("(Gets more powerful the longer you fire, but you slow down as well.)");
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
            gun.DefaultModule.shootStyle          = ProjectileModule.ShootStyle.Automatic;
            gun.DefaultModule.sequenceStyle       = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime                        = 1.1f;
            gun.DefaultModule.angleVariance       = 20.0f;
            gun.DefaultModule.cooldownTime        = baseCooldownTime;
            gun.DefaultModule.numberOfShotsInClip = 1000;
            gun.SetBaseMaxAmmo(1000);
            // Here we just set the quality of the gun and the "EncounterGuid", which is used by Gungeon to identify the gun.
            gun.quality = PickupObject.ItemQuality.D;
            gun.encounterTrackable.EncounterGuid = "heeeegyyyaaaaaaaaAAAAAAAAaahhh";
            //This block of code helps clone our projectile. Basically it makes it so things like Shadow Clone and Hip Holster keep the stats/sprite of your custom gun's projectiles.
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);

            projectile.gameObject.AddComponent<NatashaBullets>();

            // projectile.hitEffects.overrideMidairDeathVFX = EasyVFXDatabase.RedLaserCircleVFX;
            // projectile.hitEffects.alwaysUseMidair = true;

            gun.DefaultModule.projectiles[0] = projectile;
             //projectile.baseData allows you to modify the base properties of your projectile module.
            //In our case, our gun uses modified projectiles from the ak-47.
            //You can modify a good number of stats but for now, let's just modify the damage and speed.
            projectile.baseData.damage = 3f;
            // projectile.baseData.speed  = 1.7f;
            projectile.baseData.speed  = 20.0f;
            projectile.transform.parent = gun.barrelOffset;
            //This determines what sprite you want your projectile to use. Note this isn't necessary if you don't want to have a custom projectile sprite.
            //The x and y values determine the size of your custom projectile
            ETGMod.Databases.Items.Add(gun, false, "ANY");

        }

        public override void OnPostFired(PlayerController player, Gun gun)
        {
            base.OnPostFired(player, gun);
            if (gun && gun.GunPlayerOwner())
            {
                if (this.speedmult > 0.15f)
                {
                    this.speedmult *= 0.85f;
                    this.RecalculateGunStats();
                }
            }
        }

        public override void OnFinishAttack(PlayerController player, Gun gun)
        {
            this.speedmult = 1.0f;
            this.RecalculateGunStats();
        }

        protected override void OnPickedUpByPlayer(PlayerController player)
        {
            base.OnPickedUpByPlayer(player);
            player.OnRollStarted += this.OnDodgeRoll;
            // ETGModConsole.Log("picked up O:");
        }
        protected override void OnPostDroppedByPlayer(PlayerController player)
        {
            base.OnPostDroppedByPlayer(player);
            player.OnRollStarted -= this.OnDodgeRoll;
            // ETGModConsole.Log("put down ;-;");
        }

        private void OnDodgeRoll(PlayerController player, Vector2 dirVec)
        {
            this.speedmult = 1.0f;
            this.RecalculateGunStats();
            // ETGModConsole.Log("we rollin' O:");
        }

        private void RecalculateGunStats()
        {
            if (!(this.gun && this.gun.GunPlayerOwner()))
                return;
            PlayerController pc = this.gun.GunPlayerOwner();
            this.gun.RemoveStatFromGun(PlayerStats.StatType.MovementSpeed);
            this.gun.AddStatToGun(PlayerStats.StatType.MovementSpeed, this.speedmult, StatModifier.ModifyMethod.MULTIPLICATIVE);
            pc.stats.RecalculateStats(gun.GunPlayerOwner());
            this.gun.DefaultModule.cooldownTime = this.speedmult * baseCooldownTime;
        }

        //This block of code allows us to change the reload sounds.
        protected override void Update()
        {
            base.Update();
            if (gun && gun.GunPlayerOwner())
            {

            }
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
        }

        public override void OnSwitchedToThisGun()
        {
            base.OnSwitchedToThisGun();
        }

        public override void OnSwitchedAwayFromThisGun()
        {
            base.OnSwitchedAwayFromThisGun();
        }

        public Natasha() {

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

    public class NatashaBullets : MonoBehaviour
    {
        private const float NATASHA_PROJECTILE_SCALE  = 0.5f;
        private void Start()
        {
            Projectile self = base.GetComponent<Projectile>();
            self.RuntimeUpdateScale(NATASHA_PROJECTILE_SCALE);
        }
    }
}
