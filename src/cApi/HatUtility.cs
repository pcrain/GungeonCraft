using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Gungeon;
using Dungeonator;
using System.Reflection;
using Alexandria.ItemAPI;
using System.Collections;
using System.Globalization;

namespace Alexandria.cAPI
{
    public static class HatUtility
    {
        private static AutocompletionSettings HatAutoCompletionSettings = new AutocompletionSettings(delegate (string input) {
            return Hatabase.Hats.Keys.Where(key => key.AutocompletionMatch(input.ToLower())).ToArray();
        });

        internal static void SetupConsoleCommands()
        {
            ETGModConsole.Commands.AddGroup("capi");
            ETGModConsole.Commands.GetGroup("capi").AddUnit("sethat", new Action<string[]>(SetHat1), HatAutoCompletionSettings);
            ETGModConsole.Commands.GetGroup("capi").AddUnit("2sethat", new Action<string[]>(SetHat2), HatAutoCompletionSettings);
            ETGModConsole.Commands.GetGroup("capi").AddUnit("reload_offsets", new Action<string[]>(ReloadHatOffsets), HatAutoCompletionSettings);
        }

        private static void SetHat1(string[] args) => SetHat(args, GameManager.Instance.PrimaryPlayer);
        private static void SetHat2(string[] args) => SetHat(args, GameManager.Instance.SecondaryPlayer);
        private static void ReloadHatOffsets(string[] args) => LazyLoadModdedHatData(force: true);

		private static void SetHat(string[] args, PlayerController playa)
		{
            if (!playa || playa.GetComponent<HatController>() is not HatController HatCont)
            {
                ETGModConsole.Log("<size=100><color=#ff0000ff>Error: No HatController found.</color></size>", false);
                return;
            }

            if (args == null || args.Length == 0 || string.IsNullOrEmpty(args[0]))
            {
                if (HatCont.RemoveCurrentHat())
                    ETGModConsole.Log("Hat Removed", false);
                else
                    ETGModConsole.Log("No Hat to remove!", false);
                return;
            }

            string processedHatName = args[0];
            if (Hatabase.Hats.TryGetValue(processedHatName, out Hat hat))
            {
                HatCont.SetHat(hat);
                ETGModConsole.Log("Hat set to: " + processedHatName, false);
            }
            else
                ETGModConsole.Log("<size=100><color=#ff0000ff>Error: Hat '</color></size>" + processedHatName + "<size=100><color=#ff0000ff>' not found in Hatabase</color></size>", false);
        }

        /// <summary>Set up a new custom hat and register it in the hatabase</summary>
        /// <param name="name">
        /// The name of the hat as displayed in the hat room. Mandatory parameter.
        /// </param>
        /// <param name="spritePaths">
        /// A list of sprite paths for the hat. Sprite paths must end with [direction]_###.png, where ### is a three digit number starting with 001 and [direction]
        ///  is one of "south", "north", "east", "west", "northeast", and "northwest". Hat directionality and animations are set up automatically depending on
        ///  the list of sprite paths passed during set up. Mandatory parameter.
        /// </param>
        /// <param name="pixelOffset">
        /// The pixel offset of the hat relative to the default hat / eyewear position. Positve x is right, negative x is left, positive y is up, negative y is down.
        ///  Defaults to (0,0).
        /// </param>
        /// <param name="fps">
        /// The frame rate of the hat's animations, if present. Defaults to 4.
        /// </param>
        /// <param name="attachLevel">
        /// Where the hat is positioned relatived to the player. HEAD_TOP positions the hat relative to the player's head, and EYE_LEVEL positions the hat relative
        ///  to the player's eyes. Defaults to HEAD_TOP.
        /// </param>
        /// <param name="depthType">
        /// The poisitioning of the hat relative to the camera.
        ///   ALWAYS_IN_FRONT makes the hat render closer to the camera than the player regardless of facing direction.
        ///   ALWAYS_BEHIND makes the hat render farther from the camera than the player regardless of facing direction.
        ///   BEHIND_WHEN_FACING_BACK makes the hat render closer to the camera when the player is facing forward, and farther from the camera when the player is facing backward.
        ///   IN_FRONT_WHEN_FACING_BACK makes the hat render farther from the camera when the player is facing forward, and closer to the camera when the player is facing backward.
        /// Defaults to ALWAYS_IN_FRONT.
        /// </param>
        /// <param name="hatRollReaction">
        /// How the hat reacts to dodge rolls.
        ///   FLIP makes the hat flip above the player for the duration of the dodge roll.
        ///   VANISH makes the hat completely disappear while the player is dodge rolling.
        ///   NONE continues to render the hat as normal while the player is dodge rolling.
        /// Defaults to FLIP.
        /// </param>
        /// <param name="flipStartedSound">
        /// An optional sound to play when the hat flips at the beginning of a dodge roll. Has no effect unless hatRollReaction is FLIP.
        /// </param>
        /// <param name="flipEndedSound">
        /// An optional sound to play when the hat lands at the end of a dodge roll. Has no effect unless hatRollReaction is FLIP.
        /// </param>
        /// <param name="flipSpeed">
        /// The number of full rotations the hat will make during a dodge roll. Can be fractional (e.g., 1.5f). Setting to 0 makes the hat go straight up and down.
        ///  Has no effect unless hatRollReaction is FLIP. Defaults to 1f.
        /// </param>
        /// <param name="flipHeight">
        /// A multiplier for how high the hat will flip relative to the default flip height. Has no effect unless hatRollReaction is FLIP. Defaults to 1f.
        /// </param>
        /// <param name="flipHorizontalWithPlayer">
        /// Whether the hat sprite will flip horizontally with the player's sprite when facing left. Defaults to automatic (null), which is true for non-directional and
        ///  north-south hats, and false for east-west, 4-way, and 6-way hats.
        /// </param>
        /// <param name="excludeFromHatRoom">
        /// If true, this hat will not show up in the hat room, and can only be accessed via code and console commands. Defaults to false.
        /// </param>
        /// <param name="unlockFlags">
        /// An optional list of GungeonFlags required to unlock this hat. Can be given custom flags using ExtendedEnums.
        /// </param>
        /// <param name="unlockPrereqs">
        /// An optional list of DungeonPrerequisite required to unlock this hat. Can be given custom prerequisites using ExtendedEnums.
        /// </param>
        /// <param name="unlockHint">
        /// An optional unlock hint to display when interacting with the hat's pedestal in the hat room while the hat is locked. Has no effect if the hat is not an
        ///  unlockable hat or if the hat is excluded from the hat room.
        /// </param>
        /// <param name="showSilhouetteWhenLocked">
        /// If true, a silhouette of the hat will appear above its pedestal in the hat room while the hat is locked; if false, the pedestal will simply be empty while
        ///  the hat is locked. Has no effect if the hat is not an unlockable hat or if the hat is excluded from the hat room. Defaults to false.
        /// </param>
        public static Hat SetupHat(
            string name, List<string> spritePaths, IntVector2? pixelOffset = null, int fps = 4,
            Hat.HatAttachLevel attachLevel = Hat.HatAttachLevel.HEAD_TOP, Hat.HatDepthType depthType = Hat.HatDepthType.ALWAYS_IN_FRONT,
            Hat.HatRollReaction hatRollReaction = Hat.HatRollReaction.FLIP, string flipStartedSound = null, string flipEndedSound = null,
            float flipSpeed = 1f, float flipHeight = 1f, bool? flipHorizontalWithPlayer = null, bool excludeFromHatRoom = false,
            List<GungeonFlags> unlockFlags = null, List<DungeonPrerequisite> unlockPrereqs = null, string unlockHint = null, bool showSilhouetteWhenLocked = false
            )
        {
            Hat hat = UnityEngine.Object.Instantiate(new GameObject()).AddComponent<Hat>();
            UnityEngine.Object.DontDestroyOnLoad(hat.gameObject);

            hat.hatName = name;
            hat.hatOffset = 0.0625f * ((pixelOffset ?? IntVector2.Zero).ToVector2());
            hat.attachLevel = attachLevel;
            hat.hatDepthType = depthType;
            hat.hatRollReaction = hatRollReaction;
            hat.flipStartedSound = flipStartedSound;
            hat.flipEndedSound = flipEndedSound;
            hat.flipSpeedMultiplier = flipSpeed;
            hat.flipHeightMultiplier = flipHeight;
            hat.goldenPedestal = false; // TODO: might need to add this back in later
            hat.unlockHint = unlockHint;
            hat.showSilhouetteWhenLocked = showSilhouetteWhenLocked;

            if (unlockFlags != null)
                foreach(GungeonFlags flag in unlockFlags)
                    hat.AddUnlockOnFlag(flag);
            if (unlockPrereqs != null)
                foreach(DungeonPrerequisite prereq in unlockPrereqs)
                    hat.AddUnlockPrerequisite(prereq);

            hat.SetupHatSprites(spritePaths: spritePaths, fps: fps);
            hat.flipHorizontalWithPlayer = flipHorizontalWithPlayer ??
                (hat.hatDirectionality == Hat.HatDirectionality.NONE || hat.hatDirectionality == Hat.HatDirectionality.TWO_WAY_VERTICAL);

            AddHatToDatabase(hat, excludeFromHatRoom: excludeFromHatRoom);
            return hat;
        }

        /// <summary>Set up default hat offsets for a custom character</summary>
        /// <param name="characterObjectName">
        /// The name of the player prefab, as accessed by `prefabObject.name`. Will usually be "PlayerXXXX(Clone)".
        /// </param>
        /// <param name="defaultHeadXOffset">Default head-top hat pixel x-offset for the character.></param>
        /// <param name="defaultHeadYOffset">Default head-top hat pixel y-offset for the character.></param>
        /// <param name="defaultEyeXOffset">Default eye-level hat pixel x-offset for the character.></param>
        /// <param name="defaultEyeYOffset">Default eye-level hat pixel y-offset for the character.></param>
        public static void SetupHatOffsets(string characterObjectName, int defaultHeadXOffset, int defaultHeadYOffset, int defaultEyeXOffset, int defaultEyeYOffset)
        {
            Hatabase.HeadLevel[characterObjectName] = new Vector2(0.0625f * defaultHeadXOffset, 0.0625f * defaultHeadYOffset);
            Hatabase.EyeLevel[characterObjectName] = new Vector2(0.0625f * defaultEyeXOffset, 0.0625f * defaultEyeYOffset);
            Hatabase.ModdedHeadFrameOffsets[characterObjectName] = new();
            Hatabase.ModdedEyeFrameOffsets[characterObjectName] = new();
        }

        /// <summary>Create additional frame-specific hat offsets for a custom character</summary>
        /// <param name="characterObjectName">
        /// The name of the player prefab, as accessed by `prefabObject.name`. Will usually be "PlayerXXXX(Clone)".
        /// </param>
        /// <param name="animationFrameName">
        /// The name of the frame of animation whose offset should be adjusted.
        /// </param>
        /// <param name="headXOffset">Head-top hat pixel x-offset for the animation frame.></param>
        /// <param name="headYOffset">Head-top hat pixel y-offset for the animation frame.></param>
        /// <param name="eyeXOffset">Eye-level hat pixel x-offset for the animation frame.></param>
        /// <param name="eyeYOffset">Eye-level hat pixel y-offset for the animation frame.></param>
        public static void AddHatOffset(string characterObjectName, string animationFrameName, int headXOffset, int headYOffset, int? eyeXOffset = null, int? eyeYOffset = null)
        {
            Hatabase.ModdedHeadFrameOffsets[characterObjectName][animationFrameName] =
                new Hatabase.FrameOffset(headXOffset, headYOffset);
            Hatabase.ModdedEyeFrameOffsets[characterObjectName][animationFrameName] =
                new Hatabase.FrameOffset(eyeXOffset ?? headXOffset, eyeYOffset ?? headYOffset);
        }

        private static tk2dSpriteCollectionData HatSpriteCollection = null;
		private static void SetupHatSprites(this Hat hat, List<string> spritePaths, int fps)
        {
            GameObject hatObj = hat.gameObject;

            HatSpriteCollection ??= SpriteBuilder.ConstructCollection(new GameObject(), "HatCollection");
            Assembly callingASM = Assembly.GetCallingAssembly();
            int spriteID = SpriteBuilder.AddSpriteToCollection(spritePaths[0], HatSpriteCollection, callingASM);
            tk2dSprite hatBaseSprite = hatObj.GetOrAddComponent<tk2dSprite>();
            hatBaseSprite.SetSprite(HatSpriteCollection, spriteID);
            tk2dSpriteDefinition def = hatBaseSprite.GetCurrentSpriteDef();
            def.colliderVertices = new Vector3[]{ Vector3.zero, def.position3 };
            hatBaseSprite.PlaceAtPositionByAnchor(hatObj.transform.position, tk2dBaseSprite.Anchor.LowerCenter);
            hatBaseSprite.depthUsesTrimmedBounds = true;
            hatBaseSprite.IsPerpendicular = true;
            hatBaseSprite.UpdateZDepth();
            hatBaseSprite.HeightOffGround = 0.2f;

            List<string> SouthAnimation = spritePaths.Where(path => path.ToLower().Contains("_south_")).ToList();
            List<string> NorthAnimation = spritePaths.Where(path => path.ToLower().Contains("_north_")).ToList();
            List<string> EastAnimation = spritePaths.Where(path => path.ToLower().Contains("_east_")).ToList();
            List<string> WestAnimation = spritePaths.Where(path => path.ToLower().Contains("_west_")).ToList();
            List<string> NorthWestAnimation = spritePaths.Where(path => path.ToLower().Contains("_northwest_")).ToList();
            List<string> NorthEastAnimation = spritePaths.Where(path => path.ToLower().Contains("_northeast_")).ToList();

            if (SouthAnimation.Count == 0)
            {
                if (EastAnimation.Count == 0 || WestAnimation.Count == 0)
                    throw new Exception("Hat Does Not Have Proper Animations");
                else
                    hat.hatDirectionality = Hat.HatDirectionality.TWO_WAY_HORIZONTAL;
            }
            else if (NorthAnimation.Count == 0)
                hat.hatDirectionality = Hat.HatDirectionality.NONE;
            else if (EastAnimation.Count == 0 || WestAnimation.Count == 0)
                hat.hatDirectionality = Hat.HatDirectionality.TWO_WAY_VERTICAL;
            else if (NorthEastAnimation.Count == 0 || NorthWestAnimation.Count == 0)
                hat.hatDirectionality = Hat.HatDirectionality.FOUR_WAY;
            else
                hat.hatDirectionality = Hat.HatDirectionality.SIX_WAY;

            //SET UP THE ANIMATOR AND THE ANIMATION
            tk2dSpriteAnimation animation = hatObj.GetOrAddComponent<tk2dSpriteAnimation>();
            animation.clips = new tk2dSpriteAnimationClip[0];
            hatObj.GetOrAddComponent<tk2dSpriteAnimator>().Library = animation;

            // use the same offset for every sprite for a hat to avoid alignment jankiness
            Vector2 lowerCenterOffset = new Vector2(-def.untrimmedBoundsDataCenter.x, 0);
            animation.AddHatAnimation(animName: "hat_south",     spriteNames: SouthAnimation,     fps: fps, callingASM: callingASM, def: def, offset: lowerCenterOffset);
            animation.AddHatAnimation(animName: "hat_north",     spriteNames: NorthAnimation,     fps: fps, callingASM: callingASM, def: def, offset: lowerCenterOffset);
            animation.AddHatAnimation(animName: "hat_west",      spriteNames: WestAnimation,      fps: fps, callingASM: callingASM, def: def, offset: lowerCenterOffset);
            animation.AddHatAnimation(animName: "hat_east",      spriteNames: EastAnimation,      fps: fps, callingASM: callingASM, def: def, offset: lowerCenterOffset);
            animation.AddHatAnimation(animName: "hat_northeast", spriteNames: NorthEastAnimation, fps: fps, callingASM: callingASM, def: def, offset: lowerCenterOffset);
            animation.AddHatAnimation(animName: "hat_northwest", spriteNames: NorthWestAnimation, fps: fps, callingASM: callingASM, def: def, offset: lowerCenterOffset);
        }

        private static void AddHatAnimation(this tk2dSpriteAnimation animation, string animName, List<string> spriteNames, int fps,
            Assembly callingASM, tk2dSpriteDefinition def, Vector2 offset)
        {
            if (spriteNames == null || spriteNames.Count == 0)
                return; // nothing to do

            tk2dSpriteAnimationClip clip = new tk2dSpriteAnimationClip() { name = animName, frames = new tk2dSpriteAnimationFrame[spriteNames.Count], fps = fps };
            List<tk2dSpriteAnimationFrame> frames = new List<tk2dSpriteAnimationFrame>();
            for (int i = 0; i < spriteNames.Count; ++i)
            {
                string path = spriteNames[i];
                int frameSpriteId = SpriteBuilder.AddSpriteToCollection(path, HatSpriteCollection, callingASM);
                tk2dSpriteDefinition frameDef = HatSpriteCollection.spriteDefinitions[frameSpriteId];
                frameDef.colliderVertices = def.colliderVertices;
                frameDef.AdjustOffset(offset);
                clip.frames[i] = new tk2dSpriteAnimationFrame { spriteId = frameSpriteId, spriteCollection = HatSpriteCollection };
            }
            animation.clips = animation.clips.Concat(new tk2dSpriteAnimationClip[] { clip }).ToArray();
        }

        private static void AdjustOffset(this tk2dSpriteDefinition def, Vector3 offset)
        {
            def.position0 += offset;
            def.position1 += offset;
            def.position2 += offset;
            def.position3 += offset;
            def.boundsDataCenter += offset;
            def.boundsDataExtents += offset;
            def.untrimmedBoundsDataCenter += offset;
            def.untrimmedBoundsDataExtents += offset;
        }

        private static void AddHatToDatabase(Hat hat, bool excludeFromHatRoom)
        {
            Hatabase.Hats[hat.hatName.GetDatabaseFriendlyHatName()] = hat;
            if (!excludeFromHatRoom)
                Hatabase.HatRoomHats.Add(hat);
        }

        /// <summary>Converts a hat's display name to the format it's stored in within the hat database</summary>
        public static string GetDatabaseFriendlyHatName(this string hatName)
        {
            return hatName.ToLower().Replace(" ","_");
        }

        /// <summary>Retrieve's the player's current hat, if they're wearing one</summary>
        public static Hat CurrentHat(this PlayerController player)
        {
            if (!player || player.GetComponent<HatController>() is not HatController hc)
                return null;
            return hc.CurrentHat;
        }

        private static bool loadedModdedHatData = false;
        internal static void LazyLoadModdedHatData(bool force = false)
        {
            if (loadedModdedHatData && !force)
                return;

            string[] hatDataPaths = Directory.GetFiles(BepInEx.Paths.PluginPath, "hatdata.txt", SearchOption.AllDirectories);
            foreach (string hatData in hatDataPaths)
            {
                string dir = Path.GetDirectoryName(hatData);
                string charData = Path.Combine(dir, "characterdata.txt");
                if (!File.Exists(charData))
                    continue;
                // ETGModConsole.Log($"found hatdata in {dir}");
                Dictionary<string, Hatabase.FrameOffset> headOffsets = null;
                Dictionary<string, Hatabase.FrameOffset> eyeOffsets = null;
                string charName = null;
                string charObjName = null;
                foreach (string line in File.ReadAllLines(charData))
                {
                    if (!line.Contains("name short:"))
                        continue;
                    charName = line.Split(':')[1].Trim();
                    // ETGModConsole.Log($"found character {charName}");
                    charObjName = $"Player{charName}(Clone)";
                    if (!Hatabase.ModdedHeadFrameOffsets.TryGetValue(charObjName, out headOffsets))
                        headOffsets = Hatabase.ModdedHeadFrameOffsets[charObjName] = new();
                    if (!Hatabase.ModdedEyeFrameOffsets.TryGetValue(charObjName, out eyeOffsets))
                        eyeOffsets = Hatabase.ModdedEyeFrameOffsets[charObjName] = new();
                    break;
                }
                if (charName == null)
                {
                    Debug.Log($"failed to find characterdata.txt in {dir}");
                    continue;
                }
                foreach (string line in File.ReadAllLines(hatData))
                {
                    string[] fields = Regex.Replace(line, @"\s+", " ").Split(' ');
                    if (fields.Length == 0 || fields[0].Length == 0 || fields[0][0] == '#')
                        continue;
                    try
                    {
                        if (fields.Length < 3)
                            continue;
                        string frame = fields[0];
                        int headX = Int32.Parse(fields[1]);
                        int headY = Int32.Parse(fields[2]);

                        int eyeX = headX;
                        int eyeY = headY;
                        if (fields.Length >= 5)
                        {
                            eyeX = Int32.Parse(fields[3]);
                            eyeY = Int32.Parse(fields[4]);
                        }

                        if (frame == "DEFAULT")
                        {
                            Hatabase.HeadLevel[charObjName] = new Vector2(0.0625f * headX, 0.0625f * headY);
                            Hatabase.EyeLevel[charObjName] = new Vector2(0.0625f * eyeX, 0.0625f * eyeY);
                        }
                        else
                        {
                            headOffsets[frame] = new Hatabase.FrameOffset(headX, headY);
                            eyeOffsets[frame] = new Hatabase.FrameOffset(eyeX, eyeY);
                        }
                    }
                    catch (Exception)
                    {
                        continue; // failed to parse line, nothing to do
                    }
                }
                Debug.Log($"loaded {headOffsets.Keys.Count} custom hat offsets for {charName}");
            }

            loadedModdedHatData = true;
        }
    }
}
