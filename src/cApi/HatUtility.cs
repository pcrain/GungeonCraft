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
        }

        private static void SetHat1(string[] args) => SetHat(args, GameManager.Instance.PrimaryPlayer);
        private static void SetHat2(string[] args) => SetHat(args, GameManager.Instance.SecondaryPlayer);

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

        public static Hat SetupHat(
            string name, List<string> spritePaths, IntVector2? pixelOffset = null, int fps = 4,
            Hat.HatAttachLevel attachLevel = Hat.HatAttachLevel.HEAD_TOP, Hat.HatDepthType depthType = Hat.HatDepthType.ALWAYS_IN_FRONT,
            Hat.HatRollReaction hatRollReaction = Hat.HatRollReaction.FLIP, string flipStartedSound = null, string flipEndedSound = null,
            float flipSpeed = 1f, float flipHeight = 1f, bool goldenPedestal = false, bool? flipHorizontalWithPlayer = null,
            List<GungeonFlags> unlockFlags = null, List<DungeonPrerequisite> unlockPrereqs = null, string unlockHint = null, bool showSilhouetteWhenLocked = false,
            bool excludeFromHatRoom = false
            )
        {
            Hat hat = UnityEngine.Object.Instantiate(new GameObject()).AddComponent<Hat>();
            hat.hatName = name;
            hat.hatOffset = 0.0625f * ((pixelOffset ?? IntVector2.Zero).ToVector2());
            hat.attachLevel = attachLevel;
            hat.hatDepthType = depthType;
            hat.hatRollReaction = hatRollReaction;
            hat.flipStartedSound = flipStartedSound;
            hat.flipEndedSound = flipEndedSound;
            hat.flipSpeedMultiplier = flipSpeed;
            hat.flipHeightMultiplier = flipHeight;
            hat.goldenPedestal = goldenPedestal;
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
    }
}
