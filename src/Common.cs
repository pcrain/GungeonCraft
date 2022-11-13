using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.ComponentModel;  //debug

using UnityEngine;
using Gungeon;
using ItemAPI;

namespace CwaffingTheGungy
{
    public class C // constants
    {
        public const float PIXELS_PER_TILE = 16f;
    }
    public static class Lazy
    {
        /// <summary>
        /// Perform basic initialization for a new gun definition.
        /// </summary>
        public static Gun InitGunFromStrings(
          string gunName, string spriteName, string projectileName, string shortDescription, string longDescription)
        {
            string newGunName  = gunName.Replace("'", "").Replace("-", "");  //get sane gun for item rename
            string baseGunName = newGunName.Replace(" ", "_").ToLower();  //get saner gun name for commands

            Gun gun = ETGMod.Databases.Items.NewGun(newGunName, spriteName);  //create a new gun using specified sprite name
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

            ETGModConsole.Log("Lazy Initialized Gun: "+baseGunName);
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

        /// <summary>
        /// Perform basic initialization for a new passive item definition. Stolen and modified from Noonum.
        /// </summary>
        public static PickupObject SetupItem<T>(string itemName, string spritePath, string shortDescription, string longDescription, string idPool = "ItemAPI")
            where T : PickupObject
        {
            GameObject obj = new GameObject(itemName);
            PickupObject item = obj.AddComponent<T>();
            ItemBuilder.AddSpriteToObject(itemName, spritePath, obj);

            item.encounterTrackable = null;

            ETGMod.Databases.Items.SetupItem(item, item.name);
            SpriteBuilder.AddToAmmonomicon(item.sprite.GetCurrentSpriteDef());
            item.encounterTrackable.journalData.AmmonomiconSprite = item.sprite.GetCurrentSpriteDef().name;

            item.SetName(item.name);
            item.SetShortDescription(shortDescription);
            item.SetLongDescription(longDescription);

            if (item is PlayerItem)
                (item as PlayerItem).consumable = false;

            string newItemName  = itemName.Replace("'", "").Replace("-", "");  //get sane item for item rename
            string baseItemName = newItemName.Replace(" ", "_").ToLower();  //get saner item name for commands
            Gungeon.Game.Items.Add(idPool + ":" + baseItemName, item);
            ETGMod.Databases.Items.Add(item);
            IDs.Passives[baseItemName] = item.PickupObjectId; //register item in my ID database

            ETGModConsole.Log("Lazy Initialized Passive: "+baseItemName);
            return item;
        }
    }
    public static class Dissect
    {
        public static void DumpComponents(this GameObject g)
        {
            foreach (var c in g.GetComponents(typeof(object)))
            {
                ETGModConsole.Log("  "+c.GetType().Name);
            }

        }

        public static void DumpFieldsAndProperties<T>(T o)
        {
            // Type type = o.GetType();
            Type type = typeof(T);
            foreach (var f in type.GetFields()) {
                Console.WriteLine(
                    String.Format("field {0} = {1}", f.Name, f.GetValue(o)));
            }
            foreach(PropertyDescriptor d in TypeDescriptor.GetProperties(o))
            {
                string name = d.Name;
                object value = d.GetValue(o);
                Console.WriteLine(" prop {0} = {1}", name, value);
            }
        }

        public static void CompareFieldsAndProperties<T>(T o1, T o2)
        {
            // Type type = o.GetType();
            Type type = typeof(T);
            foreach (var f in type.GetFields()) {
                if (f.GetValue(o1) == null)
                {
                    if (f.GetValue(o2) == null)
                        continue;
                }
                else if (f.GetValue(o2) != null && f.GetValue(o1).Equals(f.GetValue(o2)))
                    continue;
                Console.WriteLine(
                    String.Format("field {0} = {1} -> {2}", f.Name, f.GetValue(o1), f.GetValue(o2)));
            }
            foreach(PropertyDescriptor f in TypeDescriptor.GetProperties(o1))
            {
                if (f.GetValue(o1) == null)
                {
                    if (f.GetValue(o2) == null)
                        continue;
                }
                else if (f.GetValue(o2) != null && f.GetValue(o1).Equals(f.GetValue(o2)))
                    continue;
                string name = f.Name;
                Console.WriteLine(" prop {0} = {1} -> {2}", name, f.GetValue(o1), f.GetValue(o2));
            }
        }
    }
}

