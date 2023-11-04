namespace CwaffingTheGungy;

public class HatUtility
{
    public static void NecessarySetup()
    {
        ETGModConsole.Commands.AddGroup("nn", delegate (string[] args)
        {
            ETGModConsole.Log("<size=100><color=#ff0000ff>Please specify a command. Type 'nn help' for a list of commands.</color></size>", false);
        });
        ETGModConsole.Commands.GetGroup("nn").AddUnit("sethat", delegate (string[] args)
        {
            if (args == null)
                ETGModConsole.Log("<size=100><color=#ff0000ff>Please Specify a Hat</color></size>", false);
            else
            {
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
                    string processedHatName = args[0].Replace("_", " ");
                    processedHatName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(processedHatName.ToLower());

                    if (HatLibrary.Hats.ContainsKey(processedHatName))
                    {
                        PlayerController playa = GameManager.Instance.PrimaryPlayer;
                        HatController HatCont = playa.GetComponent<HatController>();
                        if (HatCont)
                        {
                            HatCont.SetHat(HatLibrary.Hats[processedHatName]);
                            ETGModConsole.Log("Hat set to: " + processedHatName, false);
                        }
                        else ETGModConsole.Log("<size=100><color=#ff0000ff>Error: No HatController found.</color></size>", false);
                    }
                    else ETGModConsole.Log("<size=100><color=#ff0000ff>Error: Hat '</color></size>" + processedHatName + "<size=100><color=#ff0000ff>' not found in Hatabase</color></size>", false);
                }
            }
        });
    }
    public static void SetupHatSprites(List<string> spritePaths, GameObject hatObj, int fps, Vector2 hatSize)
    {
        Hat hatness = hatObj.GetComponent<Hat>();
        if (hatness)
        {

            string collectionName = hatness.hatName;
            collectionName = collectionName.Replace(" ", "_");
            tk2dSpriteCollectionData HatSpriteCollection = SpriteBuilder.ConstructCollection(hatObj, (collectionName + "_Collection"));

            int spriteID = SpriteBuilder.AddSpriteToCollection(spritePaths[0], HatSpriteCollection);
            tk2dSprite hatBaseSprite = hatObj.GetOrAddComponent<tk2dSprite>();
            hatBaseSprite.SetSprite(HatSpriteCollection, spriteID);
            tk2dSpriteDefinition def = hatBaseSprite.GetCurrentSpriteDef();
            def.colliderVertices = new Vector3[]{
                new Vector3(0f, 0f, 0f),
                new Vector3((hatSize.x / 16), (hatSize.y / 16), 0f)
            };
            hatBaseSprite.PlaceAtPositionByAnchor(hatObj.transform.position, tk2dBaseSprite.Anchor.LowerCenter);
            hatBaseSprite.depthUsesTrimmedBounds = true;
            hatBaseSprite.IsPerpendicular = true;
            hatBaseSprite.UpdateZDepth();
            hatBaseSprite.HeightOffGround = 0.2f;

            List<string> SouthAnimation = new List<string>();
            List<string> NorthAnimation = new List<string>();
            List<string> EastAnimation = new List<string>();
            List<string> WestAnimation = new List<string>();
            List<string> NorthWestAnimation = new List<string>();
            List<string> NorthEastAnimation = new List<string>();
            List<string> SouthEastAnimation = new List<string>();
            List<string> SouthWestAnimation = new List<string>();

            switch (hatness.hatDirectionality)
            {
                case Hat.HatDirectionality.EIGHTWAY:
                    foreach (string path in spritePaths)
                    {
                        if (path.ToLower().Contains("_north_")) NorthAnimation.Add(path);
                        if (path.ToLower().Contains("_south_")) SouthAnimation.Add(path);
                        if (path.ToLower().Contains("_west_")) WestAnimation.Add(path);
                        if (path.ToLower().Contains("_east_")) EastAnimation.Add(path);
                        if (path.ToLower().Contains("_northwest_")) NorthWestAnimation.Add(path);
                        if (path.ToLower().Contains("_northeast_")) NorthEastAnimation.Add(path);
                        if (path.ToLower().Contains("_southwest_")) SouthWestAnimation.Add(path);
                        if (path.ToLower().Contains("_southeast_")) SouthEastAnimation.Add(path);
                    }
                    break;
                case Hat.HatDirectionality.SIXWAY:
                    foreach (string path in spritePaths)
                    {
                        if (path.ToLower().Contains("_north_")) NorthAnimation.Add(path);
                        if (path.ToLower().Contains("_south_")) SouthAnimation.Add(path);
                        if (path.ToLower().Contains("_west_")) WestAnimation.Add(path);
                        if (path.ToLower().Contains("_east_")) EastAnimation.Add(path);
                        if (path.ToLower().Contains("_northwest_")) NorthWestAnimation.Add(path);
                        if (path.ToLower().Contains("_northeast_")) NorthEastAnimation.Add(path);
                    }
                    break;
                case Hat.HatDirectionality.FOURWAY:
                    foreach (string path in spritePaths)
                    {
                        if (path.ToLower().Contains("_west_")) WestAnimation.Add(path);
                        if (path.ToLower().Contains("_east_")) EastAnimation.Add(path);
                        if (path.ToLower().Contains("_north_")) NorthAnimation.Add(path);
                        if (path.ToLower().Contains("_south_")) SouthAnimation.Add(path);
                    }
                    break;
                case Hat.HatDirectionality.TWOWAYHORIZONTAL:
                    foreach (string path in spritePaths)
                    {
                        if (path.ToLower().Contains("_west_")) WestAnimation.Add(path);
                        if (path.ToLower().Contains("_east_")) EastAnimation.Add(path);
                    }
                    break;
                case Hat.HatDirectionality.TWOWAYVERTICAL:
                    foreach (string path in spritePaths)
                    {
                        if (path.ToLower().Contains("_north_")) NorthAnimation.Add(path);
                        if (path.ToLower().Contains("_south_")) SouthAnimation.Add(path);
                    }
                    break;
                case Hat.HatDirectionality.NONE:
                    foreach (string path in spritePaths) if (path.ToLower().Contains("_south_")) SouthAnimation.Add(path);
                    break;
            }

            //SET UP THE ANIMATOR AND THE ANIMATION
            tk2dSpriteAnimator animator = hatObj.GetOrAddComponent<tk2dSpriteAnimator>();
            tk2dSpriteAnimation animation = hatObj.GetOrAddComponent<tk2dSpriteAnimation>();
            animation.clips = new tk2dSpriteAnimationClip[0];
            animator.Library = animation;

            //SOUTH ANIMATION
            if (SouthAnimation != null)
            {
                tk2dSpriteAnimationClip clip = new tk2dSpriteAnimationClip() { name = "hat_south", frames = new tk2dSpriteAnimationFrame[0], fps = fps };

                List<tk2dSpriteAnimationFrame> frames = new List<tk2dSpriteAnimationFrame>();
                foreach (string path in SouthAnimation)
                {
                    tk2dSpriteCollectionData collection = HatSpriteCollection;
                    int frameSpriteId = SpriteBuilder.AddSpriteToCollection(path, collection);
                    tk2dSpriteDefinition frameDef = collection.spriteDefinitions[frameSpriteId];
                    frameDef.colliderVertices = def.colliderVertices;
                    frameDef.ConstructOffsetsFromAnchor(tk2dBaseSprite.Anchor.LowerCenter);
                    frames.Add(new tk2dSpriteAnimationFrame { spriteId = frameSpriteId, spriteCollection = collection });
                }
                clip.frames = frames.ToArray();
                animation.clips = animation.clips.Concat(new tk2dSpriteAnimationClip[] { clip }).ToArray();
            }
            //NORTH ANIMATIONS
            if (NorthAnimation != null)
            {
                tk2dSpriteAnimationClip clip = new tk2dSpriteAnimationClip() { name = "hat_north", frames = new tk2dSpriteAnimationFrame[0], fps = fps };

                List<tk2dSpriteAnimationFrame> frames = new List<tk2dSpriteAnimationFrame>();
                foreach (string path in NorthAnimation)
                {
                    tk2dSpriteCollectionData collection = HatSpriteCollection;
                    int frameSpriteId = SpriteBuilder.AddSpriteToCollection(path, collection);
                    tk2dSpriteDefinition frameDef = collection.spriteDefinitions[frameSpriteId];
                    frameDef.colliderVertices = def.colliderVertices;
                    frameDef.ConstructOffsetsFromAnchor(tk2dBaseSprite.Anchor.LowerCenter);
                    frames.Add(new tk2dSpriteAnimationFrame { spriteId = frameSpriteId, spriteCollection = collection });
                }
                clip.frames = frames.ToArray();
                animation.clips = animation.clips.Concat(new tk2dSpriteAnimationClip[] { clip }).ToArray();
            }
            //WEST ANIMATIONS
            if (WestAnimation != null)
            {
                tk2dSpriteAnimationClip clip = new tk2dSpriteAnimationClip() { name = "hat_west", frames = new tk2dSpriteAnimationFrame[0], fps = fps };

                List<tk2dSpriteAnimationFrame> frames = new List<tk2dSpriteAnimationFrame>();
                foreach (string path in WestAnimation)
                {
                    tk2dSpriteCollectionData collection = HatSpriteCollection;
                    int frameSpriteId = SpriteBuilder.AddSpriteToCollection(path, collection);
                    tk2dSpriteDefinition frameDef = collection.spriteDefinitions[frameSpriteId];
                    frameDef.colliderVertices = def.colliderVertices;
                    frameDef.ConstructOffsetsFromAnchor(tk2dBaseSprite.Anchor.LowerCenter);
                    frames.Add(new tk2dSpriteAnimationFrame { spriteId = frameSpriteId, spriteCollection = collection });
                }
                clip.frames = frames.ToArray();
                animation.clips = animation.clips.Concat(new tk2dSpriteAnimationClip[] { clip }).ToArray();
            }
            //EAST ANIMATIONS
            if (EastAnimation != null)
            {
                tk2dSpriteAnimationClip clip = new tk2dSpriteAnimationClip() { name = "hat_east", frames = new tk2dSpriteAnimationFrame[0], fps = fps };

                List<tk2dSpriteAnimationFrame> frames = new List<tk2dSpriteAnimationFrame>();
                foreach (string path in EastAnimation)
                {
                    tk2dSpriteCollectionData collection = HatSpriteCollection;
                    int frameSpriteId = SpriteBuilder.AddSpriteToCollection(path, collection);
                    tk2dSpriteDefinition frameDef = collection.spriteDefinitions[frameSpriteId];
                    frameDef.colliderVertices = def.colliderVertices;
                    frameDef.ConstructOffsetsFromAnchor(tk2dBaseSprite.Anchor.LowerCenter);
                    frames.Add(new tk2dSpriteAnimationFrame { spriteId = frameSpriteId, spriteCollection = collection });
                }
                clip.frames = frames.ToArray();
                animation.clips = animation.clips.Concat(new tk2dSpriteAnimationClip[] { clip }).ToArray();
            }
            //NORTHEAST
            if (NorthEastAnimation != null)
            {
                tk2dSpriteAnimationClip clip = new tk2dSpriteAnimationClip() { name = "hat_northeast", frames = new tk2dSpriteAnimationFrame[0], fps = fps };

                List<tk2dSpriteAnimationFrame> frames = new List<tk2dSpriteAnimationFrame>();
                foreach (string path in NorthEastAnimation)
                {
                    tk2dSpriteCollectionData collection = HatSpriteCollection;
                    int frameSpriteId = SpriteBuilder.AddSpriteToCollection(path, collection);
                    tk2dSpriteDefinition frameDef = collection.spriteDefinitions[frameSpriteId];
                    frameDef.colliderVertices = def.colliderVertices;
                    frameDef.ConstructOffsetsFromAnchor(tk2dBaseSprite.Anchor.LowerCenter);
                    frames.Add(new tk2dSpriteAnimationFrame { spriteId = frameSpriteId, spriteCollection = collection });
                }
                clip.frames = frames.ToArray();
                animation.clips = animation.clips.Concat(new tk2dSpriteAnimationClip[] { clip }).ToArray();
            }
            //NORTHWEST ANIMATION
            if (NorthWestAnimation != null)
            {
                tk2dSpriteAnimationClip clip = new tk2dSpriteAnimationClip() { name = "hat_northwest", frames = new tk2dSpriteAnimationFrame[0], fps = fps };

                List<tk2dSpriteAnimationFrame> frames = new List<tk2dSpriteAnimationFrame>();
                foreach (string path in NorthWestAnimation)
                {
                    tk2dSpriteCollectionData collection = HatSpriteCollection;
                    int frameSpriteId = SpriteBuilder.AddSpriteToCollection(path, collection);
                    tk2dSpriteDefinition frameDef = collection.spriteDefinitions[frameSpriteId];
                    frameDef.colliderVertices = def.colliderVertices;
                    frameDef.ConstructOffsetsFromAnchor(tk2dBaseSprite.Anchor.LowerCenter);
                    frames.Add(new tk2dSpriteAnimationFrame { spriteId = frameSpriteId, spriteCollection = collection });
                }
                clip.frames = frames.ToArray();
                animation.clips = animation.clips.Concat(new tk2dSpriteAnimationClip[] { clip }).ToArray();
            }
            //SOUTHWEST ANIMATION
            if (SouthWestAnimation != null)
            {
                tk2dSpriteAnimationClip clip = new tk2dSpriteAnimationClip() { name = "hat_southwest", frames = new tk2dSpriteAnimationFrame[0], fps = fps };

                List<tk2dSpriteAnimationFrame> frames = new List<tk2dSpriteAnimationFrame>();
                foreach (string path in SouthWestAnimation)
                {
                    tk2dSpriteCollectionData collection = HatSpriteCollection;
                    int frameSpriteId = SpriteBuilder.AddSpriteToCollection(path, collection);
                    tk2dSpriteDefinition frameDef = collection.spriteDefinitions[frameSpriteId];
                    frameDef.colliderVertices = def.colliderVertices;
                    frameDef.ConstructOffsetsFromAnchor(tk2dBaseSprite.Anchor.LowerCenter);
                    frames.Add(new tk2dSpriteAnimationFrame { spriteId = frameSpriteId, spriteCollection = collection });
                }
                clip.frames = frames.ToArray();
                animation.clips = animation.clips.Concat(new tk2dSpriteAnimationClip[] { clip }).ToArray();
            }
            //SOUTHEAST ANIMATION
            if (SouthEastAnimation != null)
            {
                tk2dSpriteAnimationClip clip = new tk2dSpriteAnimationClip() { name = "hat_southeast", frames = new tk2dSpriteAnimationFrame[0], fps = fps };

                List<tk2dSpriteAnimationFrame> frames = new List<tk2dSpriteAnimationFrame>();
                foreach (string path in SouthEastAnimation)
                {
                    tk2dSpriteCollectionData collection = HatSpriteCollection;
                    int frameSpriteId = SpriteBuilder.AddSpriteToCollection(path, collection);
                    tk2dSpriteDefinition frameDef = collection.spriteDefinitions[frameSpriteId];
                    frameDef.colliderVertices = def.colliderVertices;
                    frameDef.ConstructOffsetsFromAnchor(tk2dBaseSprite.Anchor.LowerCenter);
                    frames.Add(new tk2dSpriteAnimationFrame { spriteId = frameSpriteId, spriteCollection = collection });
                }
                clip.frames = frames.ToArray();
                animation.clips = animation.clips.Concat(new tk2dSpriteAnimationClip[] { clip }).ToArray();
            }
        }
    }
    public static void AddHatToDatabase(GameObject hatObj)
    {
        Hat hatComponent = hatObj.GetComponent<Hat>();
        if (hatComponent != null)
        {
            HatLibrary.Hats.Add(hatComponent.hatName, hatComponent);
            Debug.Log("Hat '" + hatComponent.hatName + "' correctly added to Hatabase!");
        }
    }
}
