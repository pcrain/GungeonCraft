using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using UnityEngine;
using Gungeon;
using ItemAPI;

namespace CwaffingTheGungy
{
    public class C // constants
    {
        public const float PIXELS_PER_TILE = 16f;
    }
    public class Lazy
    {
        /// <summary>
        /// Perform basic initialization for a new gun definition.
        /// </summary>
        public static Gun InitGunFromStrings(
          string gunName, string spriteName, string projectileName, string shortDescription, string longDescription)
        {
            string baseGunName = gunName.Replace(" ", "_").ToLower();  //get sane gun name for commands

            Gun gun = ETGMod.Databases.Items.NewGun(gunName, spriteName);  //create a new gun using specified sprite name
            Game.Items.Rename("outdated_gun_mods:"+baseGunName, "cg:"+baseGunName);  //rename the gun for commands
            gun.encounterTrackable.EncounterGuid = baseGunName+"-"+spriteName; //create a unique guid for the gun
            gun.SetShortDescription(shortDescription); //set the gun's short description
            gun.SetLongDescription(longDescription); //set the gun's long description
            gun.SetupSprite(null, spriteName+"_idle_001", 8); //set the gun's ammonomicon sprit
            int projectileId = 0;
            if (int.TryParse(projectileName, out projectileId))
                gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(projectileId) as Gun, true, false); //set the gun's default projectile to inherit
            else
                gun.AddProjectileModuleFrom(projectileName, true, false); //set the gun's default projectile to inherit
            ETGMod.Databases.Items.Add(gun, false, "ANY");  //register the gun in the EtG database
            IDs.Guns[baseGunName] = gun.PickupObjectId; //register gun in my ID database

            return gun;
        }

        /// <summary>
        /// Perform basic initialization for a new projectile definition.
        /// </summary>
        public static Projectile PrefabProjectileFromGun(Gun gun, bool setGunDefaultProjectile = true)
        {
            //actually instantiate the projectile
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false); //make sure the projectile isn't an active game object
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);  //mark the projectile as a prefab
            UnityEngine.Object.DontDestroyOnLoad(projectile); //make sure the projectile isn't destroyed when loaded as a prefab
            if (setGunDefaultProjectile)
                gun.DefaultModule.projectiles[0] = projectile; //reset the gun's default projectile
            return projectile;
        }

        /// <summary>
        /// Perform basic initialization for a copied projectile definition.
        /// </summary>
        public static Projectile PrefabProjectileFromExistingProjectile(Projectile baseProjectile)
        {
            //actually instantiate the projectile
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(baseProjectile);
            projectile.gameObject.SetActive(false); //make sure the projectile isn't an active game object
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);  //mark the projectile as a prefab
            UnityEngine.Object.DontDestroyOnLoad(projectile); //make sure the projectile isn't destroyed when loaded as a prefab
            return projectile;
        }

        /// <summary>
        /// Post a custom item pickup notification to the bottom of the screen
        /// </summary>
        public static void CustomNotification(string header, string text)
        {
            var sprite = GameUIRoot.Instance.notificationController.notificationObjectSprite;
            GameUIRoot.Instance.notificationController.DoCustomNotification(
                header,
                text,
                sprite.Collection,
                sprite.spriteId,
                UINotificationController.NotificationColor.PURPLE,
                false,
                false);
        }

        /// <summary>
        /// Calculate a vector from a given angle in degrees
        /// </summary>
        public static Vector2 AngleToVector(float angleInDegrees, float magnitude = 1)
        {
            Vector2 offset = new Vector2(
                Mathf.Cos(angleInDegrees*Mathf.PI/180),Mathf.Sin(angleInDegrees*Mathf.PI/180));
            return magnitude*offset;
        }
    }
}

