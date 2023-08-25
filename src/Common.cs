using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.ComponentModel;  //debug
using System.IO;
using System.Runtime.InteropServices; // audio loading
using System.Text.RegularExpressions;

using UnityEngine;
using MonoMod.RuntimeDetour;

using Gungeon;
using Dungeonator;
using ItemAPI;

namespace CwaffingTheGungy
{
    public class C // constants
    {
        public const string MOD_PREFIX      = "cg";
        public const float  PIXELS_PER_TILE = 16f;
        public const float  PIXELS_PER_CELL = 64f;
        public const float  FPS             = 60f;
    }

    public class IDs // global IDs for this mod's guns and items
    {
        public static Dictionary<string, int>    Pickups       { get; set; } = new Dictionary<string, int>();
        public static Dictionary<string, int>    Guns          { get; set; } = new Dictionary<string, int>();
        public static Dictionary<string, int>    Actives       { get; set; } = new Dictionary<string, int>();
        public static Dictionary<string, int>    Passives      { get; set; } = new Dictionary<string, int>();
        public static Dictionary<string, string> InternalNames { get; set; } = new Dictionary<string, string>();
    }

    public static class ResMap // Resource map from PNG stem names to lists of paths to all PNGs with those names (i.e., animation frames)
    {
        private static Regex _NumberAtEnd = new Regex(@"^(.*?)(_?)([0-9]+)$",
          RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static Dictionary<string, List<string>> _ResMap = new ();

        // Gets a list of resources with numbered sprites from the resource's base name
        // Does not work with CreateProjectileAnimation(), which expects direct sprite names in the mod's "sprites" directory
        public static List<string> Get(string resource)
        {
            return _ResMap[resource];
        }

        // Builds a resource map from every PNG embedded in the assembly
        public static void Build()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            Dictionary<string, string[]> tempMap = new ();
            // Get the name of each PNG resource and stuff it into a sorted array by its index number
            foreach(string s in ResourceExtractor.GetResourceNames())
            {
                if (!s.EndsWithInvariant(".png"))
                    continue;
                string path = s.Replace('.','/').Substring(0, s.Length - 4);
                string[] tokens = path.Split('/');
                string baseName = tokens[tokens.Length - 1];
                MatchCollection matches = _NumberAtEnd.Matches(baseName);
                // If we aren't numbered at the end, we're just a singular sprite
                if (matches.Count == 0)
                {
                    if (!tempMap.ContainsKey(baseName))
                        tempMap[baseName] = new string[1];
                    tempMap[baseName][0] = path;
                    continue;
                }
                foreach (Match match in matches)
                {
                    string name = match.Groups[1].Value;
                    if (name.Length == 0)
                        continue; // don't allow 0-length keys
                    int index = Int32.Parse(match.Groups[3].Value);
                    if (index == 0)
                        continue; // don't allow 0 for an index
                    if (!tempMap.ContainsKey(name))
                        tempMap[name] = new string[index];
                    if (index > tempMap[name].Length)
                    {
                        string[] arr = tempMap[name];
                        Array.Resize(ref arr, index);
                        tempMap[name] = arr;
                    }
                    tempMap[name][index - 1] = path;
                }
            }

            // Convert our arrays to lists
            foreach(KeyValuePair<string, string[]> entry in tempMap)
                _ResMap[entry.Key] = new List<string>(entry.Value);

            // Hint to the GC we want to unload the tempMap
            tempMap = null;

            // Debug sanity check
            // foreach(KeyValuePair<string, List<string>> entry in _ResMap)
            // {
            //     ETGModConsole.Log($"{entry.Key} ->");
            //     foreach (string s in entry.Value)
            //         ETGModConsole.Log($"  {s}");
            // }
            watch.Stop();
            ETGModConsole.Log($"Built resource map in {watch.ElapsedMilliseconds} milliseconds");
        }
    }

    public static class Lazy // all-purpose helper methods for being a lazy dumdum
    {
        /// <summary>
        /// Perform basic initialization for a new passive, active, or gun item definition.
        /// </summary>
        public static TItemClass SetupItem<TItemClass, TItemSpecific>(string itemName, string spritePath, string projectileName, string shortDescription, string longDescription)
            where TItemClass : PickupObject   // must be PickupObject for passive items, PlayerItem for active items, or Gun for guns
            where TItemSpecific : TItemClass  // must be a subclass of TItemClass
        {
            string newItemName  = itemName.Replace("'", "").Replace("-", "").Replace(".", "");  //get sane gun for item rename
            string baseItemName = newItemName.Replace(" ", "_").ToLower();  //get saner gun name for commands
            IDs.InternalNames[itemName] = C.MOD_PREFIX+":"+baseItemName;

            TItemClass item;

            if (typeof(TItemClass) == typeof(Gun))
            {
                string spriteName = spritePath; // TODO: guns use names, regular items use full paths -- should be made uniform eventually
                Gun gun = ETGMod.Databases.Items.NewGun(newItemName, spriteName);  //create a new gun using specified sprite name
                Game.Items.Rename("outdated_gun_mods:"+baseItemName, IDs.InternalNames[itemName]);  //rename the gun for commands
                gun.SetupSprite(null, spriteName+"_idle_001", 8); //set the gun's ammonomicon sprite
                int projectileId = 0;
                if (int.TryParse(projectileName, out projectileId))
                    gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(projectileId) as Gun, true, true); //set the gun's default projectile to inherit
                else
                    gun.AddProjectileModuleFrom(projectileName, true, false); //set the gun's default projectile to inherit
                item = gun as TItemClass;
            }
            else
            {
                GameObject obj = new GameObject(itemName);
                item = obj.AddComponent<TItemSpecific>();
                ItemBuilder.AddSpriteToObject(itemName, spritePath, obj);

                ETGMod.Databases.Items.SetupItem(item, item.name);
                SpriteBuilder.AddToAmmonomicon(item.sprite.GetCurrentSpriteDef());

                Gungeon.Game.Items.Add(IDs.InternalNames[itemName], item);
            }

            item.encounterTrackable.EncounterGuid = C.MOD_PREFIX+"-"+baseItemName; //create a unique guid for the item
            item.encounterTrackable.journalData.AmmonomiconSprite = item.sprite.GetCurrentSpriteDef().name;
            item.SetShortDescription(shortDescription);
            item.SetLongDescription(longDescription);
            ETGMod.Databases.Items.Add(item);
            IDs.Pickups[baseItemName] = item.PickupObjectId; //register item in pickup ID database
            if (item is Gun)
            {
                IDs.Guns[baseItemName] = item.PickupObjectId; //register item in gun ID database
                ETGModConsole.Log("Lazy Initialized Gun: "+baseItemName);
            }
            else if (item is PlayerItem)
            {
                IDs.Actives[baseItemName] = item.PickupObjectId; //register item in active ID database
                ETGModConsole.Log("Lazy Initialized Active: "+baseItemName);
            }
            else
            {
                IDs.Passives[baseItemName] = item.PickupObjectId; //register item in passive ID database
                ETGModConsole.Log("Lazy Initialized Passive: "+baseItemName);
            }
            return item;
        }

        /// <summary>
        /// Perform basic initialization for a new passive item definition.
        /// </summary>
        public static PickupObject SetupPassive<T>(string itemName, string spritePath, string shortDescription, string longDescription)
            where T : PickupObject
        {
            return SetupItem<PickupObject, T>(itemName, spritePath, "", shortDescription, longDescription);
        }

        /// <summary>
        /// Perform basic initialization for a new active item definition.
        /// </summary>
        public static PlayerItem SetupActive<T>(string itemName, string spritePath, string shortDescription, string longDescription)
            where T : PlayerItem
        {
            return SetupItem<PlayerItem, T>(itemName, spritePath, "", shortDescription, longDescription);
        }

        /// <summary>
        /// Perform basic initialization for a new gun definition.
        /// </summary>
        public static Gun SetupGun(string gunName, string spritePath, string projectileName, string shortDescription, string longDescription)
        {
            return SetupItem<Gun, Gun>(gunName, spritePath, projectileName, shortDescription, longDescription);
        }

        /// <summary>
        /// Perform basic initialization for a new projectile definition.
        /// </summary>
        public static Projectile PrefabProjectileFromGun(Gun gun, bool setGunDefaultProjectile = true)
        {
            //actually instantiate the projectile
            Projectile projectile = gun.DefaultModule.projectiles[0].ClonePrefab<Projectile>();
            if (setGunDefaultProjectile)
                gun.DefaultModule.projectiles[0] = projectile; //reset the gun's default projectile
            return projectile;
        }

        /// <summary>
        /// Perform basic initialization for a copied projectile definition.
        /// </summary>
        public static Projectile PrefabProjectileFromExistingProjectile(Projectile baseProjectile)
        {
            return baseProjectile.ClonePrefab<Projectile>();
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

        // Stolen from NN
        public static bool PlayerHasActiveSynergy(this PlayerController player, string synergyNameToCheck)
        {
            foreach (int index in player.ActiveExtraSynergies)
            {
                if (GameManager.Instance.SynergyManager.synergies[index].NameKey == synergyNameToCheck)
                    return true;
            }
            return false;
        }

        // Select a random element from an array
        public static T ChooseRandom<T>(this T[] source)
        {
            return source[UnityEngine.Random.Range(0,source.Length)];
        }

        // Select a random element from a list
        public static T ChooseRandom<T>(this List<T> source)
        {
            return source[UnityEngine.Random.Range(0,source.Count)];
        }

        public static void MovePlayerTowardsPositionUntilHittingWall(PlayerController player, Vector2 position)
        {
            int num_steps = 100;

            Vector2 playerPos   = player.transform.position;
            Vector2 targetPos   = position;
            Vector2 deltaPos    = (targetPos - playerPos)/((float)(num_steps));
            Vector2 adjustedPos = Vector2.zero;

            // magic code that slowly moves the player out of walls
            for (int i = 0; i < num_steps; ++i)
            {
                player.transform.position = (playerPos + i * deltaPos).ToVector3ZisY();
                player.specRigidbody.Reinitialize();
                if (PhysicsEngine.Instance.OverlapCast(player.specRigidbody, null, true, false, null, null, false, null, null))
                {
                    player.transform.position = adjustedPos;
                    break;
                }
                adjustedPos = player.transform.position;
            }
        }

        public static string GetBaseIdleAnimationName(PlayerController p, float gunAngle)
        {
            string anim = string.Empty;
            bool hasgun = p.CurrentGun != null;
            bool invertThresholds = false;
            if (GameManager.Instance.CurrentLevelOverrideState == GameManager.LevelOverrideState.END_TIMES)
            {
                hasgun = false;
            }
            float num = 155f;
            float num2 = 25f;
            if (invertThresholds)
            {
                num = -155f;
                num2 = -25f;
            }
            float num3 = 120f;
            float num4 = 60f;
            float num5 = -60f;
            float num6 = -120f;
            bool flag2 = gunAngle <= num && gunAngle >= num2;
            if (invertThresholds)
                flag2 = gunAngle <= num || gunAngle >= num2;
            if (flag2)
            {
                if (gunAngle < num3 && gunAngle >= num4)
                    anim = (((!hasgun) && !p.ForceHandless) ? "_backward_twohands" : ((!p.RenderBodyHand) ? "_backward" : "_backward_hand"));
                else
                    anim = ((hasgun || p.ForceHandless) ? "_bw" : "_bw_twohands");
            }
            else if (gunAngle <= num5 && gunAngle >= num6)
                anim = (((!hasgun) && !p.ForceHandless) ? "_forward_twohands" : ((!p.RenderBodyHand) ? "_forward" : "_forward_hand"));
            else
                anim = (((!hasgun) && !p.ForceHandless) ? "_twohands" : ((!p.RenderBodyHand) ? "" : "_hand"));
            if (p.UseArmorlessAnim)
                anim += "_armorless";
            return "idle"+anim;
        }

        public static string GetBaseDodgeAnimationName(PlayerController p, Vector2 vector)
        {
            return ((!(Mathf.Abs(vector.x) < 0.1f)) ? (((!(vector.y > 0.1f)) ? "dodge_left" : "dodge_left_bw") + ((!p.UseArmorlessAnim) ? string.Empty : "_armorless")) : (((!(vector.y > 0.1f)) ? "dodge" : "dodge_bw") + ((!p.UseArmorlessAnim) ? string.Empty : "_armorless")));
        }

        // Get a random angle in range [-180,180]
        public static float RandomAngle()
        {
          return UnityEngine.Random.Range(-180f,180f);
        }

        // Get a random vector
        public static Vector2 RandomVector(float magnitude = 1f)
        {
          return magnitude * RandomAngle().ToVector();
        }

        // Get a random Quaternion rotated on the Z axis
        public static Quaternion RandomEulerZ()
        {
          return RandomAngle().EulerZ();
        }

        // Get a random boolean
        public static bool CoinFlip()
        {
          return UnityEngine.Random.Range(0,2) == 1;
        }

        public static Projectile GunDefaultProjectile(int gunid)
        {
            return (PickupObjectDatabase.GetById(gunid) as Gun).DefaultModule.projectiles[0];
        }
    }

    public static class Dissect // reflection helper methods for being a lazy dumdum
    {
        public static void DumpComponents(this GameObject g)
        {
            foreach (var c in g.GetComponents(typeof(object)))
                ETGModConsole.Log("  "+c.GetType().Name);

        }

        public static void DumpFieldsAndProperties<T>(T o)
        {
            // Type type = o.GetType();
            Type type = typeof(T);
            foreach (var f in type.GetFields())
                Console.WriteLine(String.Format("field {0} = {1}", f.Name, f.GetValue(o)));
            foreach(PropertyDescriptor d in TypeDescriptor.GetProperties(o))
                Console.WriteLine(" prop {0} = {1}", d.Name, d.GetValue(o));
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

        public static void PrintSpriteCollectionNames(tk2dSpriteCollectionData theCollection)
        {
            for (int i = 0; i < theCollection.spriteDefinitions.Length; ++i)
                ETGModConsole.Log(theCollection.spriteDefinitions[i].name);
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
                UnityEngine.Object.Instantiate<GameObject>(SpawnVFX, ObjectSpecRigidBody.sprite.WorldCenter, Quaternion.identity);
            if (correctForWalls) CorrectForWalls(newObject);

            return newObject;
        }
        private static void CorrectForWalls(GameObject portal)
        {
            SpeculativeRigidbody rigidbody = portal.GetComponent<SpeculativeRigidbody>();
            if (!rigidbody)
                return;

            bool flag = PhysicsEngine.Instance.OverlapCast(rigidbody, null, true, false, null, null, false, null, null, new SpeculativeRigidbody[0]);
            if (!flag)
                return;

            Vector2 vector = portal.transform.position.XY();
            IntVector2[] cardinalsAndOrdinals = IntVector2.CardinalsAndOrdinals;
            for (int num2 = 1; num2 <= 200; ++num2)
            {
                for (int i = 0; i < cardinalsAndOrdinals.Length; i++)
                {
                    portal.transform.position = vector + PhysicsEngine.PixelToUnit(cardinalsAndOrdinals[i] * num2);
                    rigidbody.Reinitialize();
                    if (!PhysicsEngine.Instance.OverlapCast(rigidbody, null, true, false, null, null, false, null, null, new SpeculativeRigidbody[0]))
                        return;
                }
            }
            UnityEngine.Debug.LogError("FREEZE AVERTED!  TELL RUBEL!  (you're welcome) 147");
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
                if (text2.IndexOf(prefix) != 0)
                    continue;

                text2 = text2.Substring(text2.IndexOf(prefix) + prefix.Length);
                if (text2.LastIndexOf(".bnk") != text2.Length - ".bnk".Length)
                    continue;

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

