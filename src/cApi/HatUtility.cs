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

        public static void SetupConsoleCommands()
        {
            try {
                ETGModConsole.Commands.AddGroup("capi");
                ETGModConsole.Commands.GetGroup("capi").AddUnit("sethat", new Action<string[]>(SetHat), HatAutoCompletionSettings);
            } catch(Exception e)
            {
                ETGModConsole.Log("Hatutility broke heres why:" + e);
            }
        }

		private static void SetHat(string[] args)
		{
            if (args == null || args[0] == "none")
            {
                PlayerController playa = GameManager.Instance.PrimaryPlayer;
                HatController HatCont = playa.GetComponent<HatController>();
                if (HatCont)
                {
                    if (HatCont.CurrentHat != null)
                    {
                        HatCont.RemoveCurrentHat();
                        ETGModConsole.Log("Hat Removed", false);
                    }
                    else ETGModConsole.Log("No Hat to remove!", false);
                }
            }
            else
            {
                string processedHatName = args[0];

                if (Hatabase.Hats.ContainsKey(processedHatName))
                {
                    PlayerController playa = GameManager.Instance.PrimaryPlayer;
                    HatController HatCont = playa.GetComponent<HatController>();
                    if (HatCont)
                    {
                        HatCont.SetHat(Hatabase.Hats[processedHatName]);
                        ETGModConsole.Log("Hat set to: " + processedHatName, false);
                    }
                    else ETGModConsole.Log("<size=100><color=#ff0000ff>Error: No HatController found.</color></size>", false);
                }
                else ETGModConsole.Log("<size=100><color=#ff0000ff>Error: Hat '</color></size>" + processedHatName + "<size=100><color=#ff0000ff>' not found in Hatabase</color></size>", false);
            }
        }

		public static void SetupHatSprites(List<string> spritePaths, GameObject hatObj, int fps, Vector2? hatSize = null)
        {
            if (hatObj.GetComponent<Hat>() is not Hat hatness)
                return;

            string collectionName = hatness.hatName.Replace(" ", "_");
            tk2dSpriteCollectionData HatSpriteCollection = SpriteBuilder.ConstructCollection(hatObj, (collectionName + "_Collection"));
            var callingASM = Assembly.GetCallingAssembly();
            int spriteID = SpriteBuilder.AddSpriteToCollection(spritePaths[0], HatSpriteCollection, callingASM);
            tk2dSprite hatBaseSprite = hatObj.GetOrAddComponent<tk2dSprite>();
            hatBaseSprite.SetSprite(HatSpriteCollection, spriteID);
            tk2dSpriteDefinition def = hatBaseSprite.GetCurrentSpriteDef();
            def.colliderVertices = new Vector3[]{
                Vector3.zero,
                hatSize.HasValue ? (0.0625f * hatSize.Value) : def.position3
            };
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
                    hatness.hatDirectionality = Hat.HatDirectionality.TWOWAYHORIZONTAL;
            }
            else if (NorthAnimation.Count == 0)
                hatness.hatDirectionality = Hat.HatDirectionality.NONE;
            else if (EastAnimation.Count == 0 || WestAnimation.Count == 0)
                hatness.hatDirectionality = Hat.HatDirectionality.TWOWAYVERTICAL;
            else if (NorthEastAnimation.Count == 0 || NorthWestAnimation.Count == 0)
                hatness.hatDirectionality = Hat.HatDirectionality.FOURWAY;
            else
                hatness.hatDirectionality = Hat.HatDirectionality.SIXWAY;
            ETGModConsole.Log($"made hat {spritePaths[0]} with direction {hatness.hatDirectionality}");

            //SET UP THE ANIMATOR AND THE ANIMATION
            tk2dSpriteAnimator animator = hatObj.GetOrAddComponent<tk2dSpriteAnimator>();
            tk2dSpriteAnimation animation = hatObj.GetOrAddComponent<tk2dSpriteAnimation>();
            animation.clips = new tk2dSpriteAnimationClip[0];
            animator.Library = animation;

            animation.AddHatAnimation(animationName: "hat_south",     spriteNames: SouthAnimation,     fps: fps, collection: HatSpriteCollection, callingASM: callingASM, def: def);
            animation.AddHatAnimation(animationName: "hat_north",     spriteNames: NorthAnimation,     fps: fps, collection: HatSpriteCollection, callingASM: callingASM, def: def);
            animation.AddHatAnimation(animationName: "hat_west",      spriteNames: WestAnimation,      fps: fps, collection: HatSpriteCollection, callingASM: callingASM, def: def);
            animation.AddHatAnimation(animationName: "hat_east",      spriteNames: EastAnimation,      fps: fps, collection: HatSpriteCollection, callingASM: callingASM, def: def);
            animation.AddHatAnimation(animationName: "hat_northeast", spriteNames: NorthEastAnimation, fps: fps, collection: HatSpriteCollection, callingASM: callingASM, def: def);
            animation.AddHatAnimation(animationName: "hat_northwest", spriteNames: NorthWestAnimation, fps: fps, collection: HatSpriteCollection, callingASM: callingASM, def: def);
        }

        private static void AddHatAnimation(this tk2dSpriteAnimation animation, string animationName, List<string> spriteNames, int fps, tk2dSpriteCollectionData collection,
            Assembly callingASM, tk2dSpriteDefinition def)
        {
            if (spriteNames == null || spriteNames.Count == 0)
                return; // nothing to do

            tk2dSpriteAnimationClip clip = new tk2dSpriteAnimationClip() { name = animationName, frames = new tk2dSpriteAnimationFrame[spriteNames.Count], fps = fps };
            List<tk2dSpriteAnimationFrame> frames = new List<tk2dSpriteAnimationFrame>();
            for (int i = 0; i < spriteNames.Count; ++i)
            {
                string path = spriteNames[i];
                int frameSpriteId = SpriteBuilder.AddSpriteToCollection(path, collection, callingASM);
                tk2dSpriteDefinition frameDef = collection.spriteDefinitions[frameSpriteId];
                frameDef.colliderVertices = def.colliderVertices;
                frameDef.ConstructOffsetsFromAnchor(tk2dBaseSprite.Anchor.LowerCenter);
                clip.frames[i] = new tk2dSpriteAnimationFrame { spriteId = frameSpriteId, spriteCollection = collection };
            }
            animation.clips = animation.clips.Concat(new tk2dSpriteAnimationClip[] { clip }).ToArray();
        }

        public static void AddHatToDatabase(GameObject hatObj)
        {
            if (hatObj.GetComponent<Hat>() is Hat hatComponent)
            {
                Hatabase.Hats.Add(hatComponent.hatName.ToLower().Replace(" ","_"), hatComponent);
                ETGModConsole.Log("Hat '" + hatComponent.hatName + "' correctly added to Hatabase!", true);
            }
            //NOTE: should be restored once integrated into Alexandria
            // foreach (var obj in InfiniteRoom.objects)
            // {
            //     UnityEngine.GameObject.Destroy(obj);
            // }
            // InfiniteRoom.objects.Clear();
            // InfiniteRoom.Init();
        }
    }
}

//NOTE: these should already be in Alexandria and can probably be deleted from here once it's merged into Alexandria
static class ExtensionMethods {
    public static void MakeOffset(this tk2dSpriteDefinition def, Vector3 offset, bool changesCollider = false)
    {
        def.position0 += offset;
        def.position1 += offset;
        def.position2 += offset;
        def.position3 += offset;
        def.boundsDataCenter += offset;
        def.boundsDataExtents += offset;
        def.untrimmedBoundsDataCenter += offset;
        def.untrimmedBoundsDataExtents += offset;
        if (def.colliderVertices != null && def.colliderVertices.Length > 0 && changesCollider)
            def.colliderVertices[0] += offset;
    }

    public static void ConstructOffsetsFromAnchor(this tk2dSpriteDefinition def, tk2dBaseSprite.Anchor anchor, Vector2? scale = null, bool fixesScale = false, bool changesCollider = true)
    {
        if (!scale.HasValue)
        {
            scale = new Vector2?(def.position3);
        }
        if (fixesScale)
        {
            Vector2 fixedScale = scale.Value - def.position0.XY();
            scale = new Vector2?(fixedScale);
        }
        float xOffset = 0;
        if (anchor == tk2dBaseSprite.Anchor.LowerCenter || anchor == tk2dBaseSprite.Anchor.MiddleCenter || anchor == tk2dBaseSprite.Anchor.UpperCenter)
        {
            xOffset = -(scale.Value.x / 2f);
        }
        else if (anchor == tk2dBaseSprite.Anchor.LowerRight || anchor == tk2dBaseSprite.Anchor.MiddleRight || anchor == tk2dBaseSprite.Anchor.UpperRight)
        {
            xOffset = -scale.Value.x;
        }
        float yOffset = 0;
        if (anchor == tk2dBaseSprite.Anchor.MiddleLeft || anchor == tk2dBaseSprite.Anchor.MiddleCenter || anchor == tk2dBaseSprite.Anchor.MiddleLeft)
        {
            yOffset = -(scale.Value.y / 2f);
        }
        else if (anchor == tk2dBaseSprite.Anchor.UpperLeft || anchor == tk2dBaseSprite.Anchor.UpperCenter || anchor == tk2dBaseSprite.Anchor.UpperRight)
        {
            yOffset = -scale.Value.y;
        }
        def.MakeOffset(new Vector2(xOffset, yOffset), false);
        if (changesCollider && def.colliderVertices != null && def.colliderVertices.Length > 0)
        {
            float colliderXOffset = 0;
            if (anchor == tk2dBaseSprite.Anchor.LowerLeft || anchor == tk2dBaseSprite.Anchor.MiddleLeft || anchor == tk2dBaseSprite.Anchor.UpperLeft)
            {
                colliderXOffset = (scale.Value.x / 2f);
            }
            else if (anchor == tk2dBaseSprite.Anchor.LowerRight || anchor == tk2dBaseSprite.Anchor.MiddleRight || anchor == tk2dBaseSprite.Anchor.UpperRight)
            {
                colliderXOffset = -(scale.Value.x / 2f);
            }
            float colliderYOffset = 0;
            if (anchor == tk2dBaseSprite.Anchor.LowerLeft || anchor == tk2dBaseSprite.Anchor.LowerCenter || anchor == tk2dBaseSprite.Anchor.LowerRight)
            {
                colliderYOffset = (scale.Value.y / 2f);
            }
            else if (anchor == tk2dBaseSprite.Anchor.UpperLeft || anchor == tk2dBaseSprite.Anchor.UpperCenter || anchor == tk2dBaseSprite.Anchor.UpperRight)
            {
                colliderYOffset = -(scale.Value.y / 2f);
            }
            def.colliderVertices[0] += new Vector3(colliderXOffset, colliderYOffset, 0);
        }
    }
}
