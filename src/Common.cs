using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.ComponentModel;  //debug
using System.IO;
using System.Runtime.InteropServices;

using UnityEngine;
using MonoMod.RuntimeDetour;

using Gungeon;
using ItemAPI;

using Dungeonator;

// using Alexandria.ItemAPI;
using Alexandria.Misc;

namespace CwaffingTheGungy
{
    public class C // constants
    {
        public const float PIXELS_PER_TILE = 16f;
    }

    public class IDs // global IDs for this mod's guns and items
    {
        public static Dictionary<string, int> Pickups  { get; set; } = new Dictionary<string, int>();
        public static Dictionary<string, int> Guns     { get; set; } = new Dictionary<string, int>();
        public static Dictionary<string, int> Actives  { get; set; } = new Dictionary<string, int>();
        public static Dictionary<string, int> Passives { get; set; } = new Dictionary<string, int>();
    }

    public static class Lazy // all-purpose helper methods for being a lazy dumdum
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
            IDs.Guns[baseGunName] = gun.PickupObjectId; //register gun in gun ID database
            IDs.Pickups[baseGunName] = gun.PickupObjectId; //register gun in pickup ID database

            ETGModConsole.Log("Lazy Initialized Gun: "+baseGunName);
            return gun;
        }

        /// <summary>
        /// Perform basic initialization for a new projectile definition.
        /// </summary>
        public static Projectile PrefabProjectileFromGun(Gun gun = null, bool setGunDefaultProjectile = true)
        {
            gun ??= PickupObjectDatabase.GetById(86) as Gun; // default to marine sidearm
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
            IDs.Passives[baseItemName] = item.PickupObjectId; //register item in passive ID database
            IDs.Pickups[baseItemName] = item.PickupObjectId; //register item in pickup ID database

            ETGModConsole.Log("Lazy Initialized Passive: "+baseItemName);
            return item;
        }

        /// <summary>
        /// Perform basic initialization for a new active item definition.
        /// </summary>
        public static PlayerItem SetupActive<T>(string itemName, string spritePath, string shortDescription, string longDescription, string idPool = "ItemAPI")
            where T : PlayerItem
        {
            GameObject obj = new GameObject(itemName);
            PlayerItem item = obj.AddComponent<T>();
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
            IDs.Actives[baseItemName] = item.PickupObjectId; //register item in active ID database
            IDs.Pickups[baseItemName] = item.PickupObjectId; //register item in pickup ID database

            ETGModConsole.Log("Lazy Initialized Active: "+baseItemName);
            return item;
        }

        /// <summary>
        /// Create a basic list of named directional animations given a list of animation names previously setup with SpriteBuilder.AddSpriteToCollection
        /// </summary>
        public static List<AIAnimator.NamedDirectionalAnimation> EasyNamedDirectionalAnimations(string[] animNameList)
        {
            var theList = new List<AIAnimator.NamedDirectionalAnimation>();
            for(int i = 0; i < animNameList.Count(); ++i)
            {
                string anim = animNameList[i];
                theList.Add(new AIAnimator.NamedDirectionalAnimation() {
                    name = anim,
                    anim = new DirectionalAnimation() {
                        Type = DirectionalAnimation.DirectionType.Single,
                        Prefix = anim,
                        AnimNames = new string[] {anim},
                        Flipped = new DirectionalAnimation.FlipType[]{DirectionalAnimation.FlipType.None}
                    }
                });
            }
            return theList;
        }

        // Stolen from NN, seems important for StandardAPI
        public static bool PlayerHasActiveSynergy(this PlayerController player, string synergyNameToCheck)
        {
            foreach (int index in player.ActiveExtraSynergies)
            {
                AdvancedSynergyEntry synergy = GameManager.Instance.SynergyManager.synergies[index];
                if (synergy.NameKey == synergyNameToCheck)
                {
                    return true;
                }
            }
            return false;
        }
    }

    public static class Dissect // reflection helper methods for being a lazy dumdum
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

    public static class ReflectionHelpers // reflection helpers ultimately stolen from apache
    {
        public static T ReflectGetField<T>(Type classType, string fieldName, object o = null) {
            FieldInfo field = classType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | ((o != null) ? BindingFlags.Instance : BindingFlags.Static));
            return (T)field.GetValue(o);
        }

        public static void ReflectSetField<T>(Type classType, string fieldName, T value, object o = null) {
            FieldInfo field = classType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | ((o != null) ? BindingFlags.Instance : BindingFlags.Static));
            field.SetValue(o, value);
        }
    }

    public class SpawnObjectManager : MonoBehaviour // stolen from nn
    {
        public static GameObject SpawnObject(GameObject thingToSpawn, Vector3 convertedVector, GameObject SpawnVFX = null, bool correctForWalls = false)
        {
            Vector2 Vector2Position = convertedVector;

            GameObject newObject = Instantiate(thingToSpawn, convertedVector, Quaternion.identity);

            SpeculativeRigidbody ObjectSpecRigidBody = newObject.GetComponentInChildren<SpeculativeRigidbody>();
            UnityEngine.Component[] componentsInChildren = newObject.GetComponentsInChildren(typeof(IPlayerInteractable));
            for (int i = 0; i < componentsInChildren.Length; i++)
            {
                // ETGModConsole.Log(" == "+componentsInChildren[i].GetType());
                IPlayerInteractable interactable = componentsInChildren[i] as IPlayerInteractable;
                if (interactable != null)
                {
                    newObject.transform.position.GetAbsoluteRoom().RegisterInteractable(interactable);
                }
            }
            UnityEngine.Component[] componentsInChildren2 = newObject.GetComponentsInChildren(typeof(IPlaceConfigurable));
            for (int i = 0; i < componentsInChildren2.Length; i++)
            {
                IPlaceConfigurable placeConfigurable = componentsInChildren2[i] as IPlaceConfigurable;
                if (placeConfigurable != null)
                {
                    placeConfigurable.ConfigureOnPlacement(GameManager.Instance.Dungeon.data.GetAbsoluteRoomFromPosition(Vector2Position.ToIntVector2()));
                }
            }
            /* FlippableCover component7 = newObject.GetComponentInChildren<FlippableCover>();
             component7.transform.position.XY().GetAbsoluteRoom().RegisterInteractable(component7);
             component7.ConfigureOnPlacement(component7.transform.position.XY().GetAbsoluteRoom());*/

            ObjectSpecRigidBody.Initialize();
            PhysicsEngine.Instance.RegisterOverlappingGhostCollisionExceptions(ObjectSpecRigidBody, null, false);

            if (SpawnVFX != null)
            {
                UnityEngine.Object.Instantiate<GameObject>(SpawnVFX, ObjectSpecRigidBody.sprite.WorldCenter, Quaternion.identity);
            }
            if (correctForWalls) CorrectForWalls(newObject);

            return newObject;
        }
        private static void CorrectForWalls(GameObject portal)
        {
            SpeculativeRigidbody rigidbody = portal.GetComponent<SpeculativeRigidbody>();
            if (rigidbody)
            {
                bool flag = PhysicsEngine.Instance.OverlapCast(rigidbody, null, true, false, null, null, false, null, null, new SpeculativeRigidbody[0]);
                if (flag)
                {
                    Vector2 vector = portal.transform.position.XY();
                    IntVector2[] cardinalsAndOrdinals = IntVector2.CardinalsAndOrdinals;
                    int num = 0;
                    int num2 = 1;
                    for (; ; )
                    {
                        for (int i = 0; i < cardinalsAndOrdinals.Length; i++)
                        {
                            portal.transform.position = vector + PhysicsEngine.PixelToUnit(cardinalsAndOrdinals[i] * num2);
                            rigidbody.Reinitialize();
                            if (!PhysicsEngine.Instance.OverlapCast(rigidbody, null, true, false, null, null, false, null, null, new SpeculativeRigidbody[0]))
                            {
                                return;
                            }
                        }
                        num2++;
                        num++;
                        if (num > 200)
                        {
                            goto Block_4;
                        }
                    }
                //return;
                Block_4:
                    UnityEngine.Debug.LogError("FREEZE AVERTED!  TELL RUBEL!  (you're welcome) 147");
                    return;
                }
            }
        }
    }

    public static class PlayerToolsSetup  // hooks and stuff for PlayerControllers on game start
    {
        public static Hook playerStartHook;

        public static void Init()
        {
            playerStartHook = new Hook(
                typeof(PlayerController).GetMethod("Start", BindingFlags.Public | BindingFlags.Instance),
                typeof(PlayerToolsSetup).GetMethod("DoSetup"));
        }
        public static void DoSetup(Action<PlayerController> action, PlayerController player)
        {
            action(player);
            if (player.GetComponent<HatController>() == null) player.gameObject.AddComponent<HatController>();
        }
    }

    public class AudioResourceLoader // example audio resource loading class and functions
    {

        public static void InitAudio() { AutoloadFromAssembly(Assembly.GetExecutingAssembly(), "CwaffingTheGungy"); }

        public static void AutoloadFromAssembly(Assembly assembly, string prefix)
        {
            bool flag = assembly == null;
            if (flag) { throw new ArgumentNullException("assembly", "Assembly cannot be null."); }
            bool flag2 = prefix == null;
            if (flag2) { throw new ArgumentNullException("prefix", "Prefix name cannot be null."); }
            prefix = prefix.Trim();
            bool flag3 = prefix == "";
            if (flag3) { throw new ArgumentException("Prefix name cannot be an empty (or whitespace only) string.", "prefix"); }
            List<string> list = new List<string>(assembly.GetManifestResourceNames());
            for (int i = 0; i < list.Count; i++)
            {
                string text = list[i];
                string text2 = text;
                text2 = text2.Replace('/', Path.DirectorySeparatorChar);
                text2 = text2.Replace('\\', Path.DirectorySeparatorChar);
                bool flag4 = text2.IndexOf(prefix) != 0;
                if (!flag4)
                {
                    text2 = text2.Substring(text2.IndexOf(prefix) + prefix.Length);
                    bool flag5 = text2.LastIndexOf(".bnk") != text2.Length - ".bnk".Length;
                    if (!flag5)
                    {
                        text2 = text2.Substring(0, text2.Length - ".bnk".Length);
                        bool flag6 = text2.IndexOf(Path.DirectorySeparatorChar) == 0;
                        if (flag6) { text2 = text2.Substring(1); }
                        text2 = prefix + ":" + text2;
                        // Console.WriteLine(string.Format("{0}: Soundbank found, attempting to autoload: name='{1}' resource='{2}'", "hi", text2, text));
                        using (Stream manifestResourceStream = assembly.GetManifestResourceStream(text))
                        {
                            LoadSoundbankFromStream(manifestResourceStream, text2);
                        }
                    }
                }
            }
        }

        private static void LoadSoundbankFromStream(Stream stream, string name)
        {
            byte[] array = StreamToByteArray(stream);
            IntPtr intPtr = Marshal.AllocHGlobal(array.Length);
            try
            {
                Marshal.Copy(array, 0, intPtr, array.Length);
                uint num;
                AKRESULT akresult = AkSoundEngine.LoadAndDecodeBankFromMemory(intPtr, (uint)array.Length, false, name, false, out num);
                // Console.WriteLine(string.Format("Result of soundbank load: {0}.", akresult));
            }
            finally
            {
                Marshal.FreeHGlobal(intPtr);
            }
        }

        public static byte[] StreamToByteArray(Stream input)
        {
            byte[] array = new byte[16384];
            byte[] result;
            using (MemoryStream memoryStream = new MemoryStream())
            {
                int count;
                while ((count = input.Read(array, 0, array.Length)) > 0) { memoryStream.Write(array, 0, count); }
                result = memoryStream.ToArray();
            }
            return result;
        }
    }
}

