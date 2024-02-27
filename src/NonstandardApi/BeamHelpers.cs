namespace CwaffingTheGungy;

public static class BeamHelpers
{
  // modification of GenerateBeamPrefab() from Planetside of Gunymede
  public static BasicBeamController FixedGenerateBeamPrefab(this Projectile projectile, string spritePath, Vector2 colliderDimensions, Vector2 colliderOffsets, List<string> beamAnimationPaths = null, int beamFPS = -1, List<string> impactVFXAnimationPaths = null, int beamImpactFPS = -1, Vector2? impactVFXColliderDimensions = null, Vector2? impactVFXColliderOffsets = null, List<string> endVFXAnimationPaths = null, int beamEndFPS = -1, Vector2? endVFXColliderDimensions = null, Vector2? endVFXColliderOffsets = null, List<string> muzzleVFXAnimationPaths = null, int beamMuzzleFPS = -1, Vector2? muzzleVFXColliderDimensions = null, Vector2? muzzleVFXColliderOffsets = null, bool glows = false,
       bool canTelegraph = false, List<string> beamTelegraphAnimationPaths = null, int beamtelegraphFPS = -1, List<string> beamStartTelegraphAnimationPaths = null, int beamStartTelegraphFPS = -1, List<string> beamEndTelegraphAnimationPaths = null, int beamEndTelegraphFPS = -1, float telegraphTime = 1,
        bool canDissipate = false, List<string> beamDissipateAnimationPaths = null, int beamDissipateFPS = -1, List<string> beamStartDissipateAnimationPaths = null, int beamStartDissipateFPS = -1, List<string> beamEndDissipateAnimationPaths = null, int beamEndDissipateFPS = -1, float dissipateTime = 1)

    {
        try
        {
            if (projectile.specRigidbody) // modified: not all projectiles (esp. preexisting beams) have a specRigidbody component
                projectile.specRigidbody.CollideWithOthers = false;

            float convertedColliderX = colliderDimensions.x / 16f;
            float convertedColliderY = colliderDimensions.y / 16f;
            float convertedOffsetX = colliderOffsets.x / 16f;
            float convertedOffsetY = colliderOffsets.y / 16f;

            int spriteID = PackerHelper.AddSpriteToCollection(spritePath, ETGMod.Databases.Items.ProjectileCollection);
            tk2dTiledSprite tiledSprite = projectile.gameObject.GetOrAddComponent<tk2dTiledSprite>();


            tiledSprite.SetSprite(ETGMod.Databases.Items.ProjectileCollection, spriteID);
            tk2dSpriteDefinition def = tiledSprite.GetCurrentSpriteDef();
            def.colliderVertices = new Vector3[]{
                new Vector3(convertedOffsetX, convertedOffsetY, 0f),
                new Vector3(convertedColliderX, convertedColliderY, 0f)
            };

            def.ConstructOffsetsFromAnchor(Anchor.MiddleLeft);

            //tiledSprite.anchor = Anchor.MiddleCenter;
            tk2dSpriteAnimator animator = projectile.gameObject.GetOrAddComponent<tk2dSpriteAnimator>();
            tk2dSpriteAnimation animation = projectile.gameObject.GetOrAddComponent<tk2dSpriteAnimation>();
            animation.clips = new tk2dSpriteAnimationClip[0];
            animator.Library = animation;
            UnityEngine.Object.Destroy(projectile.GetComponentInChildren<tk2dSprite>());
            projectile.sprite = tiledSprite;

            BasicBeamController beamController = projectile.gameObject.GetOrAddComponent<BasicBeamController>();

            //---------------- Sets up the animation for the main part of the beam
            if (beamAnimationPaths != null)
            {
                tk2dSpriteAnimationClip clip = new tk2dSpriteAnimationClip() { name = "beam_idle", frames = new tk2dSpriteAnimationFrame[0], fps = beamFPS };
                List<string> spritePaths = beamAnimationPaths;

                List<tk2dSpriteAnimationFrame> frames = new List<tk2dSpriteAnimationFrame>();
                foreach (string path in spritePaths)
                {
                    tk2dSpriteCollectionData collection = ETGMod.Databases.Items.ProjectileCollection;
                    int frameSpriteId = PackerHelper.AddSpriteToCollection(path, collection);
                    tk2dSpriteDefinition frameDef = collection.spriteDefinitions[frameSpriteId];
                    frameDef.ConstructOffsetsFromAnchor(Anchor.MiddleLeft);
                    frameDef.colliderVertices = def.colliderVertices;
                    frames.Add(new tk2dSpriteAnimationFrame { spriteId = frameSpriteId, spriteCollection = collection });
                }
                clip.frames = frames.ToArray();
                animation.clips = animation.clips.Concat(new tk2dSpriteAnimationClip[] { clip }).ToArray();
                beamController.beamAnimation = "beam_idle";
            }

            //------------- Sets up the animation for the part of the beam that touches the wall
            if (endVFXAnimationPaths != null && endVFXColliderDimensions != null && endVFXColliderOffsets != null)
            {
                SetupBeamPart(animation, endVFXAnimationPaths, "beam_end", beamEndFPS, (Vector2)endVFXColliderDimensions, (Vector2)endVFXColliderOffsets);
                beamController.beamEndAnimation = "beam_end";
            }
            else
            {
                SetupBeamPart(animation, beamAnimationPaths, "beam_end", beamFPS, null, null, def.colliderVertices);
                beamController.beamEndAnimation = "beam_end";
            }

            //---------------Sets up the animaton for the VFX that plays over top of the end of the beam where it hits stuff
            if (impactVFXAnimationPaths != null && impactVFXColliderDimensions != null && impactVFXColliderOffsets != null)
            {
                SetupBeamPart(animation, impactVFXAnimationPaths, "beam_impact", beamImpactFPS, (Vector2)impactVFXColliderDimensions, (Vector2)impactVFXColliderOffsets, anchorOverride: Anchor.MiddleCenter);
                beamController.impactAnimation = "beam_impact";
            }

            //--------------Sets up the animation for the very start of the beam
            if (muzzleVFXAnimationPaths != null && muzzleVFXColliderDimensions != null && muzzleVFXColliderOffsets != null)
            {
                SetupBeamPart(animation, muzzleVFXAnimationPaths, "beam_start", beamMuzzleFPS, (Vector2)muzzleVFXColliderDimensions, (Vector2)muzzleVFXColliderOffsets, anchorOverride: Anchor.MiddleCenter);
                beamController.beamStartAnimation = "beam_start";
            }
            else
            {
                SetupBeamPart(animation, beamAnimationPaths, "beam_start", beamFPS, null, null, def.colliderVertices);
                beamController.beamStartAnimation = "beam_start";
            }


            if (canTelegraph == true)
            {
                beamController.usesTelegraph = true;
                beamController.telegraphAnimations = new BasicBeamController.TelegraphAnims();

                if (beamStartTelegraphAnimationPaths != null)
                {
                    SetupBeamPart(animation, beamStartTelegraphAnimationPaths, "beam_telegraph_start", beamStartTelegraphFPS, new Vector2(0, 0), new Vector2(0, 0));
                    beamController.telegraphAnimations.beamStartAnimation = "beam_telegraph_start";
                }
                if (beamTelegraphAnimationPaths != null)
                {
                    SetupBeamPart(animation, beamTelegraphAnimationPaths, "beam_telegraph_middle", beamtelegraphFPS, new Vector2(0, 0), new Vector2(0, 0));
                    beamController.telegraphAnimations.beamAnimation = "beam_telegraph_middle";
                }
                if (beamEndTelegraphAnimationPaths != null)
                {
                    SetupBeamPart(animation, beamEndTelegraphAnimationPaths, "beam_telegraph_end", beamEndTelegraphFPS, new Vector2(0,0), new Vector2(0, 0));
                    beamController.telegraphAnimations.beamEndAnimation = "beam_telegraph_end";
                }
                beamController.telegraphTime = telegraphTime;
            }
            if (canDissipate == true)
            {
                beamController.endType = BasicBeamController.BeamEndType.Dissipate;
                beamController.dissipateAnimations = new BasicBeamController.TelegraphAnims();
                if (beamStartTelegraphAnimationPaths != null)
                {
                    SetupBeamPart(animation, beamStartDissipateAnimationPaths, "beam_dissipate_start", beamStartDissipateFPS, new Vector2(0, 0), new Vector2(0, 0));
                    beamController.dissipateAnimations.beamStartAnimation = "beam_dissipate_start";
                }
                if (beamTelegraphAnimationPaths != null)
                {
                    SetupBeamPart(animation, beamDissipateAnimationPaths, "beam_dissipate_middle", beamDissipateFPS, new Vector2(0, 0), new Vector2(0, 0));
                    beamController.dissipateAnimations.beamAnimation = "beam_dissipate_middle";
                }
                if (beamEndTelegraphAnimationPaths != null)
                {
                    SetupBeamPart(animation, beamEndDissipateAnimationPaths, "beam_dissipate_end", beamEndDissipateFPS, new Vector2(0, 0), new Vector2(0, 0));
                    beamController.dissipateAnimations.beamEndAnimation = "beam_dissipate_end";
                }
                beamController.dissipateTime = dissipateTime;

            }

            // if (glows)
            // {
            //     EmmisiveBeams emission = projectile.gameObject.GetOrAddComponent<EmmisiveBeams>();
            //     //emission

            // }
            return beamController;
        }
        catch (Exception e)
        {
            ETGModConsole.Log(e.ToString());
            return null;
        }

    }
    internal static void SetupBeamPart(tk2dSpriteAnimation beamAnimation, List<string> animSpritePaths, string animationName, int fps, Vector2? colliderDimensions = null, Vector2? colliderOffsets = null, Vector3[] overrideVertices = null, tk2dSpriteAnimationClip.WrapMode wrapMode = tk2dSpriteAnimationClip.WrapMode.Loop, Anchor? anchorOverride = null)
    {
        tk2dSpriteAnimationClip clip = new tk2dSpriteAnimationClip() { name = animationName, frames = new tk2dSpriteAnimationFrame[0], fps = fps };
        List<string> spritePaths = animSpritePaths;
        List<tk2dSpriteAnimationFrame> frames = new List<tk2dSpriteAnimationFrame>();
        clip.wrapMode = wrapMode;
        foreach (string path in spritePaths)
        {
            tk2dSpriteCollectionData collection = ETGMod.Databases.Items.ProjectileCollection;
            int frameSpriteId = PackerHelper.AddSpriteToCollection(path, collection);
            tk2dSpriteDefinition frameDef = collection.spriteDefinitions[frameSpriteId];
            frameDef.ConstructOffsetsFromAnchor(anchorOverride ?? Anchor.MiddleLeft);
            if (overrideVertices != null)
            {
                frameDef.colliderVertices = overrideVertices;
            }
            else
            {
                if (colliderDimensions == null || colliderOffsets == null)
                {
                    ETGModConsole.Log("<size=100><color=#ff0000ff>BEAM ERROR: colliderDimensions or colliderOffsets was null with no override vertices!</color></size>", false);
                }
                else
                {
                    Vector2 actualDimensions = (Vector2)colliderDimensions;
                    Vector2 actualOffsets = (Vector2)colliderDimensions;
                    frameDef.colliderVertices = new Vector3[]{
                        new Vector3(actualOffsets.x / 16, actualOffsets.y / 16, 0f),
                        new Vector3(actualDimensions.x / 16, actualDimensions.y / 16, 0f)
                    };
                }
            }
            frames.Add(new tk2dSpriteAnimationFrame { spriteId = frameSpriteId, spriteCollection = collection });
        }
        clip.frames = frames.ToArray();
        beamAnimation.clips = beamAnimation.clips.Concat(new tk2dSpriteAnimationClip[] { clip }).ToArray();
    }
}
