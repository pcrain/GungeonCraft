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
            List<string> ret = new List<string>();
            foreach (string key in Hatabase.Hats.Keys)
            {
                if (key.AutocompletionMatch(input.ToLower()))
                {
                    ret.Add(key);
                }
            }
            return ret.ToArray();
        });

        public static void NecessarySetup()
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
            if (args == null)
            {
                ETGModConsole.Log("<size=100><color=#ff0000ff>Please Specify a Hat</color></size>", false);
                return;
            }

            if (args[0] == "none")
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
            List<string> SouthEastAnimation = spritePaths.Where(path => path.ToLower().Contains("_southeast_")).ToList();
            List<string> SouthWestAnimation = spritePaths.Where(path => path.ToLower().Contains("_southwest_")).ToList();

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
            else if (SouthEastAnimation.Count == 0 || SouthWestAnimation.Count == 0)
                hatness.hatDirectionality = Hat.HatDirectionality.SIXWAY;
            else
                hatness.hatDirectionality = Hat.HatDirectionality.EIGHTWAY;
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
            animation.AddHatAnimation(animationName: "hat_southwest", spriteNames: SouthWestAnimation, fps: fps, collection: HatSpriteCollection, callingASM: callingASM, def: def);
            animation.AddHatAnimation(animationName: "hat_southeast", spriteNames: SouthEastAnimation, fps: fps, collection: HatSpriteCollection, callingASM: callingASM, def: def);
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
            // foreach (var obj in InfiniteRoom.objects)
            // {
            //     UnityEngine.GameObject.Destroy(obj);
            // }
            // InfiniteRoom.objects.Clear();
            // InfiniteRoom.Init();
        }
    }
}
