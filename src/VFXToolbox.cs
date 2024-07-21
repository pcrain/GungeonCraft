namespace CwaffingTheGungy;

public static class VFX
{
    private const int PIXELS_ABOVE_HEAD = 2;

    private static Dictionary<GameActor,List<GameObject>> extantSprites = new();

    public static Dictionary<string,GameObject> animations              = new();
    public static Dictionary<string,VFXPool> vfxpool                    = new();
    public static Dictionary<string,VFXComplex> vfxcomplex              = new();
    private static Dictionary<GameObject,VFXPool> vfxObjectToPoolMap    = new();

    public static readonly tk2dSpriteCollectionData Collection =
         SpriteBuilder.ConstructCollection(new GameObject().RegisterPrefab(false, false, true), $"{C.MOD_NAME}_VFX_Collection");

    public static GameObject LaserSightPrefab;
    public static GameObject MiniPickup;
    public static GameObject MasterySigil;
    public static GameObject BasicReticle;

    public static void Init()
    {
        LaserSightPrefab = LoadHelper.LoadAssetFromAnywhere("assets/resourcesbundle/global vfx/vfx_lasersight.prefab") as GameObject;
        MiniPickup = VFX.Create("mini_pickup", 12, loops: false, anchor: Anchor.MiddleCenter);
        MasterySigil = VFX.Create("mastery_sigil", fps: 2);
        BasicReticle = VFX.Create("basic_reticle", fps: 2);
    }

    /// <summary>Register an animation for a VFX object</summary>
    public static tk2dSpriteAnimationClip NewAnimation(this GameObject vfxObject, string animName, List<string> spritePaths, float fps = 2, bool loops = true, int loopStart = -1,
        Anchor anchor = Anchor.MiddleCenter, float emissivePower = -1, Color? emissiveColour = null)
    {
        tk2dSpriteAnimationClip clip = new tk2dSpriteAnimationClip() {
            name      = animName,
            fps       = fps,
            frames    = new tk2dSpriteAnimationFrame[spritePaths.Count],
            loopStart = loopStart,
            wrapMode  =
                (loopStart > 0) ? tk2dSpriteAnimationClip.WrapMode.LoopSection :
                loops           ? tk2dSpriteAnimationClip.WrapMode.Loop : tk2dSpriteAnimationClip.WrapMode.Once
        };

        Shader shader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTintableTiltedCutoutEmissive");
        tk2dSpriteDefinition defaultDef = Collection.spriteDefinitions[vfxObject.GetComponent<tk2dSprite>().spriteId];
        for (int i = 0; i < spritePaths.Count; i++)
        {
            int frameSpriteId             = Collection.GetSpriteIdByName(spritePaths[i]);
            tk2dSpriteDefinition frameDef = Collection.spriteDefinitions[frameSpriteId];
            frameDef.BetterConstructOffsetsFromAnchor(anchor);
            frameDef.colliderVertices = defaultDef.colliderVertices; //NOTE: this overrides any prespecified collider vertices, unsure we want this
            frameDef.material.shader = shader; //NOTE: materialInst is the same as material for all of our sprites, so we don't need to adjust it separately
            if (emissivePower > 0) {
                frameDef.material.SetFloat("_EmissivePower", emissivePower);
                frameDef.material.SetFloat("_EmissiveColorPower", 1.55f);
            }
            if (emissiveColour != null)
                frameDef.material.SetColor("_EmissiveColor", (Color)emissiveColour);
            clip.frames[i] = new tk2dSpriteAnimationFrame { spriteId = frameSpriteId, spriteCollection = Collection };
        }
        return clip;
    }

    public static void AddAnimation(this GameObject vfxObject, string animName, string baseSpriteName, float fps = 2, bool loops = true, int loopStart = -1,
        Anchor anchor = Anchor.MiddleCenter, float emissivePower = -1, Color? emissiveColour = null)
    {
        tk2dSpriteAnimationClip clip = vfxObject.NewAnimation(animName: animName, spritePaths: ResMap.Get(baseSpriteName), fps: fps, loops: loops,
            loopStart: loopStart, anchor: anchor, emissivePower: emissivePower, emissiveColour: emissiveColour);
        tk2dSpriteAnimation library = vfxObject.GetComponent<tk2dSpriteAnimation>();
        int oldSize = library.clips.Length;
        Array.Resize(ref library.clips, oldSize + 1);
        library.clips[oldSize] = clip;
    }

    /// <summary>
    /// Generically register a VFX as a GameObject (animated sprite), VFXComplex, or VFXPool
    /// </summary>
    public static void RegisterVFX(string name, List<string> spritePaths, float fps = 2, bool loops = true, int loopStart = -1, float scale = 1.0f, Anchor anchor = Anchor.MiddleCenter, IntVector2? dimensions = null, bool usesZHeight = false, float zHeightOffset = 0, bool persist = false, VFXAlignment alignment = VFXAlignment.NormalAligned, float emissivePower = -1, Color? emissiveColour = null, bool orphaned = false, bool attached = true)
    {
        if (animations.ContainsKey(name))
        {
            Lazy.DebugWarn($"  HEY! re-creating VFX {name} can cause scale / anchor conflicts. Please reuse the original VFX or use a different sprite for this VFX.");
            return; //NOTE: this causes issues whether we return early (poe souls) or not (uppskeruvel muzzle)...these issues really need to be handled as they come up
        }

        GameObject          vfxEffect = new GameObject(name).RegisterPrefab();
        tk2dSprite          sprite    = vfxEffect.AddComponent<tk2dSprite>();
        tk2dSpriteAnimator  animator  = vfxEffect.AddComponent<tk2dSpriteAnimator>();
        tk2dSpriteAnimation animation = vfxEffect.AddComponent<tk2dSpriteAnimation>();

        int spriteId = Collection.GetSpriteIdByName(spritePaths[0], -1);
        if (spriteId == -1)
        {
            Lazy.DebugWarn($"  HEY! Failed to get VFX for {name}, might be from the wrong collection");
            spriteId = 0;
        }

        tk2dSpriteDefinition defaultDef = Collection.spriteDefinitions[spriteId];
        if (dimensions is IntVector2 dims)
            defaultDef.colliderVertices = new Vector3[]{Vector3.zero, C.PIXEL_SIZE * dims.ToVector3()};
        else
            defaultDef.colliderVertices = new Vector3[]{Vector3.zero, defaultDef.position3}; //NOTE: the original code for this was wrong and probably unused
        sprite.SetSprite(Collection, spriteId);

        tk2dSpriteAnimationClip clip = vfxEffect.NewAnimation(animName: "start", spritePaths: spritePaths, fps: fps, loops: loops, loopStart: loopStart,
            anchor: anchor, emissivePower: emissivePower, emissiveColour: emissiveColour);
        Shader shader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTintableTiltedCutoutEmissive");
        if (emissivePower > 0) {
            sprite.renderer.material.shader = shader;
            sprite.renderer.material.SetFloat("_EmissivePower", emissivePower);
            sprite.renderer.material.SetFloat("_EmissiveColorPower", 1.55f);
        }
        if (emissiveColour != null)
            sprite.renderer.material.SetColor("_EmissiveColor", (Color)emissiveColour);

        animation.clips            = new tk2dSpriteAnimationClip[1]{clip};
        animator.Library           = animation;
        animator.playAutomatically = true;
        // animator.DefaultClipId     = 0; //NOTE: trivially true as long as we only have 1 clip
        if (!loops && !persist) //NOTE: looping sprites by definition never finish their animation, so this code just adds overhead
        {
            SpriteAnimatorKiller kill = vfxEffect.AddComponent<SpriteAnimatorKiller>();
            kill.animator             = animator;
            kill.fadeTime             = -1f;
            kill.delayDestructionTime = -1f;
        }
        if (scale != 1.0f)
            sprite.scale = new Vector3(scale, scale, scale);

        VFXObject vfxObject = new(){
            attached        = attached,
            orphaned        = orphaned,
            persistsOnDeath = persist,
            usesZHeight     = usesZHeight,
            zHeight         = zHeightOffset,
            alignment       = alignment,
            destructible    = false,
            effect          = vfxEffect
        };
        VFXComplex complex  = new(){ effects = new VFXObject[]{ vfxObject } };
        VFXPool pool        = new(){ effects = new VFXComplex[]{ complex }, type = VFXPoolType.All };

        vfxpool[name]    = pool;
        vfxcomplex[name] = complex;
        animations[name] = vfxEffect;
    }

    /// <summary>
    /// Register and return a VFXObject
    /// </summary>
    public static GameObject Create(string name, float fps = 2, bool loops = true, int loopStart = -1, float scale = 1.0f, Anchor anchor = Anchor.MiddleCenter, IntVector2? dimensions = null, bool usesZHeight = false, float zHeightOffset = 0, bool persist = false, VFXAlignment alignment = VFXAlignment.NormalAligned, float emissivePower = -1, Color? emissiveColour = null, bool orphaned = false, bool attached = true)
    {
        RegisterVFX(
            name           : name,
            spritePaths    : ResMap.Get(name),
            fps            : fps,
            loops          : loops,
            loopStart      : loopStart,
            scale          : scale,
            anchor         : anchor,
            dimensions     : dimensions,
            usesZHeight    : usesZHeight,
            zHeightOffset  : zHeightOffset,
            persist        : persist,
            alignment      : alignment,
            emissivePower  : emissivePower,
            emissiveColour : emissiveColour,
            orphaned       : orphaned,
            attached       : attached
            );
        return animations[name];
    }

    /// <summary>
    /// Register and return a VFXPool
    /// </summary>
    public static VFXPool CreatePool(string name, float fps = 2, bool loops = true, int loopStart = -1, float scale = 1.0f, Anchor anchor = Anchor.MiddleCenter, IntVector2? dimensions = null, bool usesZHeight = false, float zHeightOffset = 0, bool persist = false, VFXAlignment alignment = VFXAlignment.NormalAligned, float emissivePower = -1, Color? emissiveColour = null, bool orphaned = false, bool attached = true)
    {
        RegisterVFX(
            name           : name,
            spritePaths    : ResMap.Get(name),
            fps            : fps,
            loops          : loops,
            loopStart      : loopStart,
            scale          : scale,
            anchor         : anchor,
            dimensions     : dimensions,
            usesZHeight    : usesZHeight,
            zHeightOffset  : zHeightOffset,
            persist        : persist,
            alignment      : alignment,
            emissivePower  : emissivePower,
            emissiveColour : emissiveColour,
            orphaned       : orphaned,
            attached       : attached
            );
        return vfxpool[name];
    }

    /// <summary>
    /// Register and return a VFXComplex
    /// </summary>
    public static VFXComplex CreateComplex(string name, float fps = 2, bool loops = true, int loopStart = -1, float scale = 1.0f, Anchor anchor = Anchor.MiddleCenter, IntVector2? dimensions = null, bool usesZHeight = false, float zHeightOffset = 0, bool persist = false, VFXAlignment alignment = VFXAlignment.NormalAligned, float emissivePower = -1, Color? emissiveColour = null, bool orphaned = false, bool attached = true)
    {
        RegisterVFX(
            name           : name,
            spritePaths    : ResMap.Get(name),
            fps            : fps,
            loops          : loops,
            loopStart      : loopStart,
            scale          : scale,
            anchor         : anchor,
            dimensions     : dimensions,
            usesZHeight    : usesZHeight,
            zHeightOffset  : zHeightOffset,
            persist        : persist,
            alignment      : alignment,
            emissivePower  : emissivePower,
            emissiveColour : emissiveColour,
            orphaned       : orphaned,
            attached       : attached
            );
        return vfxcomplex[name];
    }

    public static void ShowOverheadVFX(this GameActor gunOwner, string name, float timeout)
    {
        gunOwner.StartCoroutine(ShowVFXCoroutine(gunOwner, name, timeout));
    }

    public static void ShowOverheadAnimatedVFX(this GameActor gunOwner, string name, float timeout)
    {
        gunOwner.StartCoroutine(ShowAnimatedVFXCoroutine(gunOwner, name, timeout));
    }

    /// <summary>
    /// Spawn prefabricated vfx, optionally locked relative to a gameobject's position
    /// </summary>
    public static void SpawnVFXPool(string name, Vector2 position, bool above = false, float degAngle = 0, GameObject relativeTo = null)
    {
        SpawnVFXPool(VFX.vfxpool[name], position, above, degAngle, relativeTo);
    }

    public static void SpawnVFXPool(VFXPool vfx, Vector2 position, bool above = false, float degAngle = 0, GameObject relativeTo = null)
    {
        Transform t = (relativeTo != null) ? relativeTo.transform : null;
        vfx.SpawnAtPosition(
            position.ToVector3ZisY(above ? -1f : 1f), /* -1 = above player sprite */
            degAngle, t, null, null, -0.05f);
    }

    public static void SpawnVFXPool(GameObject vfx, Vector2 position, bool above = false, float degAngle = 0, GameObject relativeTo = null)
    {

        SpawnVFXPool(CreatePoolFromVFXGameObject(vfx), position, above, degAngle, relativeTo);
    }

    public static VFXPool CreatePoolFromVFXGameObject(GameObject vfx)
    {
        if (!(vfxObjectToPoolMap.ContainsKey(vfx)))
        {
            VFXObject vfObj         = new VFXObject();
            vfObj.attached          = false;
            vfObj.persistsOnDeath   = false;
            vfObj.usesZHeight       = false;
            vfObj.zHeight           = 0;
            vfObj.alignment         = VFXAlignment.NormalAligned;
            vfObj.destructible      = false;
            vfObj.effect            = vfx;

            VFXComplex complex      = new VFXComplex();
            complex.effects         = new VFXObject[] { vfObj };

            VFXPool pool            = new VFXPool();
            pool.type               = VFXPoolType.All;
            pool.effects            = new VFXComplex[] { complex };

            vfxObjectToPoolMap[vfx] = pool;
        }
        return vfxObjectToPoolMap[vfx];
    }

    private static IEnumerator ShowVFXCoroutine(this GameActor gunOwner, string name, float timeout)
    {
        if (!(extantSprites.ContainsKey(gunOwner)))
            extantSprites[gunOwner] = new List<GameObject>();
        gunOwner.StopAllOverheadVFX();
        GameObject newSprite = new GameObject(name, new Type[] { typeof(tk2dSprite) }) { layer = 0 };
        // newSprite.transform.position = (gunOwner.transform.position + new Vector3(0.5f, 2));
        newSprite.transform.position = new Vector3(
            gunOwner.CenterPosition.x,
            gunOwner.sprite.WorldTopCenter.y + PIXELS_ABOVE_HEAD/C.PIXELS_PER_TILE);
        tk2dSprite overheadSprite = newSprite.AddComponent<tk2dSprite>();
        extantSprites[gunOwner].Add(newSprite);
        overheadSprite.SetSprite(Collection, Collection.GetSpriteIdByName(name));
        overheadSprite.PlaceAtPositionByAnchor(newSprite.transform.position, Anchor.LowerCenter);
        overheadSprite.transform.localPosition = overheadSprite.transform.localPosition.Quantize(0.0625f);
        newSprite.transform.parent = gunOwner.transform;
        if (overheadSprite)
        {
            gunOwner.sprite.AttachRenderer(overheadSprite);
            overheadSprite.depthUsesTrimmedBounds = true;
            overheadSprite.UpdateZDepth();
        }
        gunOwner.sprite.UpdateZDepth();
        if (timeout > 0)
        {
            yield return new WaitForSeconds(timeout);
            if (newSprite)
            {
                extantSprites[gunOwner].Remove(newSprite);
                UnityEngine.Object.Destroy(newSprite.gameObject);
            }
        }
        else {
            yield break;
        }
    }

    private static IEnumerator ShowAnimatedVFXCoroutine(this GameActor gunOwner, string name, float timeout)
    {
        if (!(extantSprites.ContainsKey(gunOwner)))
            extantSprites[gunOwner] = new List<GameObject>();
        gunOwner.StopAllOverheadVFX();

        GameObject newSprite = UnityEngine.Object.Instantiate(animations[name]);

        tk2dBaseSprite baseSprite = newSprite.GetComponent<tk2dBaseSprite>();
        newSprite.transform.parent = gunOwner.transform;
        newSprite.transform.position = new Vector3(
            gunOwner.CenterPosition.x,
            gunOwner.sprite.WorldTopCenter.y + PIXELS_ABOVE_HEAD/C.PIXELS_PER_TILE);

        extantSprites[gunOwner].Add(baseSprite.gameObject);

        Bounds bounds = gunOwner.sprite.GetBounds();
        Vector3 vector = gunOwner.transform.position + new Vector3((bounds.max.x + bounds.min.x) / 2f, bounds.max.y, 0f).Quantize(0.0625f);
        newSprite.transform.position = gunOwner.CenterPosition.ToVector3ZUp(0f).WithY(vector.y);
        baseSprite.HeightOffGround = 0.5f;

        gunOwner.sprite.AttachRenderer(baseSprite);

        if (timeout > 0)
        {
            yield return new WaitForSeconds(timeout);
            if (baseSprite)
            {
                extantSprites[gunOwner].Remove(baseSprite.gameObject);
                UnityEngine.Object.Destroy(baseSprite.gameObject);
            }
        }
        else {
            yield break;
        }
    }

    public static void StopAllOverheadVFX(this GameActor gunOwner)
    {
        if (extantSprites[gunOwner].Count > 0)
        {
            for (int i = extantSprites[gunOwner].Count - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(extantSprites[gunOwner][i].gameObject);
            }
            extantSprites[gunOwner].Clear();
        }
    }

    // Blatantly stolen from Noonum
    public static GameObject CreateLaserSight(Vector2 position, float length, float width, float angle, Color? colour = null, float power = 0)
    {
        GameObject gameObject = SpawnManager.SpawnVFX(LaserSightPrefab, position, Quaternion.Euler(0, 0, angle));

        tk2dTiledSprite component2 = gameObject.GetComponent<tk2dTiledSprite>();
        float newWidth = 1f;
        if (width != -1) newWidth = width;
        component2.dimensions = new Vector2(length, newWidth);
        if (colour != null)
        {
            component2.usesOverrideMaterial = true;
            component2.sprite.renderer.material.shader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTintableTiltedCutoutEmissive");
            component2.sprite.renderer.material.SetColor("_OverrideColor", (Color)colour);
            component2.sprite.renderer.material.SetColor("_EmissiveColor", (Color)colour);
            component2.sprite.renderer.material.SetFloat("_EmissivePower", power);
            component2.sprite.renderer.material.SetFloat("_EmissiveColorPower", 1.55f);
        }
        return gameObject;
    }

    // Opacity management
    public static void SetAlpha(this Renderer renderer, float newAlpha = 1.0f)
    {
        // NOTE: might need to also make sure sprite has override material
        if (renderer.material.shader.name != "Brave/Internal/SimpleAlphaFadeUnlit")
        {
            if (renderer.gameObject.GetComponent<tk2dSprite>() is tk2dSprite sprite)
                sprite.usesOverrideMaterial = true;
            renderer.material.shader = ShaderCache.Acquire("Brave/Internal/SimpleAlphaFadeUnlit");
        }
        renderer.material.SetFloat("_Fade", newAlpha);

        // todo: these don't seem to be necessary or to work particularly well

        // if (renderer.sharedMaterial.shader.name != "Brave/Internal/SimpleAlphaFadeUnlit")
        //     renderer.sharedMaterial.shader = ShaderCache.Acquire("Brave/Internal/SimpleAlphaFadeUnlit");
        // renderer.sharedMaterial.SetFloat("_Fade", newAlpha);

        // foreach(Material m in renderer.sharedMaterials)
        // {
        //     if (m.shader.name != "Brave/Internal/SimpleAlphaFadeUnlit")
        //         m.shader = ShaderCache.Acquire("Brave/Internal/SimpleAlphaFadeUnlit");
        //     m.SetFloat("_Fade", newAlpha);
        // }
    }

    // Do a generic passive item activation effect above the player's head
    public static void DoGenericItemActivation(this PlayerController player, PickupObject item, bool playSound = true)
    {
        player.StartCoroutine(DoGenericItemActivation_CR(player, item, playSound));
    }

    public static IEnumerator DoGenericItemActivation_CR(PlayerController player, PickupObject item, bool playSound = true)
    {
        const float FADE_TIME  = 1.0f;
        const float BOB_RATE   = 1.0f * 2f * Mathf.PI;
        const float BOB_OFFSET = -0.5f;
        const float BOB_AMOUNT = 0.33f;
        const float SPIN_RATE  = 1.5f * 2f * Mathf.PI;

        if (playSound)
            player.gameObject.Play("minecraft_totem_pop_sound");

        tk2dSprite sprite = Lazy.SpriteObject(item.sprite.collection, item.sprite.spriteId);
            sprite.PlaceAtPositionByAnchor(player.sprite.WorldTopCenter - new Vector2(0, BOB_OFFSET), Anchor.LowerCenter);
            sprite.transform.parent = player.transform;

        for (float elapsed = 0f; elapsed < FADE_TIME; elapsed += BraveTime.DeltaTime)
        {
            float percentDone = elapsed / FADE_TIME;
            sprite.transform.localScale = new Vector2(Mathf.Cos(elapsed * SPIN_RATE), 1f);
            sprite.PlaceAtScaledPositionByAnchor(player.sprite.WorldTopCenter - new Vector2(0, BOB_OFFSET + BOB_AMOUNT * Mathf.Sin(elapsed * BOB_RATE)), Anchor.LowerCenter);
            sprite.gameObject.SetAlpha(1f - percentDone);
            yield return null;
        }

        UnityEngine.Object.Destroy(sprite.gameObject);
        yield break;
    }

    // yoinked and adapted from SomeBunny
    public static TrailController CreateTrailObject(string spriteName, int fps = -1, string startAnim = null, float timeTillAnimStart = -1,
        float cascadeTimer = -1, float softMaxLength = -1, bool destroyOnEmpty = false, GameObject dispersalPrefab = null)
    {
      try
      {
          List<string> animPaths = ResMap.Get(spriteName);
          string spritePath = animPaths[0];
          GameObject newTrailObject = new GameObject().RegisterPrefab();
          newTrailObject.name = "trailObject";

          int spriteID = ETGMod.Databases.Items.ProjectileCollection.GetSpriteIdByName(spritePath);
          tk2dTiledSprite tiledSprite = newTrailObject.GetOrAddComponent<tk2dTiledSprite>();

          tiledSprite.SetSprite(ETGMod.Databases.Items.ProjectileCollection, spriteID);
          tk2dSpriteDefinition def = tiledSprite.GetCurrentSpriteDef();
          def.colliderVertices = new Vector3[]{
              Vector3.zero,
              def.untrimmedBoundsDataExtents
          };

          tk2dSpriteAnimator animator = newTrailObject.GetOrAddComponent<tk2dSpriteAnimator>();
          tk2dSpriteAnimation animation = newTrailObject.GetOrAddComponent<tk2dSpriteAnimation>();
          animation.clips = new tk2dSpriteAnimationClip[0];
          animator.Library = animation;

          TrailController trail = newTrailObject.AddComponent<TrailController>();

          // ---------------- Sets up the animation for the main part of the trail
          if (animPaths != null)
          {
              SetupBeamPart(animation, animPaths, "trail_mid", fps, null, null, def.colliderVertices);
              trail.animation = "trail_mid";
              trail.usesAnimation = true;
          }
          else
          {
              def.BetterConstructOffsetsFromAnchor(Anchor.MiddleLeft); //NOTE: this is already done in SetupBeamPart(), and we don't want to do it twice
              trail.usesAnimation = false;
          }

          if (startAnim != null)
          {
              SetupBeamPart(animation, ResMap.Get(startAnim), "trail_start", fps, null, null, def.colliderVertices);
              trail.startAnimation = "trail_start";
              trail.usesStartAnimation = true;
          }
          else
              trail.usesStartAnimation = false;

          if (dispersalPrefab)
          {
            trail.UsesDispersalParticles = true;
            trail.DispersalParticleSystemPrefab = dispersalPrefab;
          }

          //Trail Variables
          if (softMaxLength > 0) { trail.usesSoftMaxLength = true; trail.softMaxLength = softMaxLength; }
          if (cascadeTimer > 0) { trail.usesCascadeTimer = true; trail.cascadeTimer = cascadeTimer; }
          if (timeTillAnimStart > 0) { trail.usesGlobalTimer = true; trail.globalTimer = timeTillAnimStart; }
          trail.destroyOnEmpty = destroyOnEmpty;
          return trail;
      }
      catch (Exception e)
      {
          ETGModConsole.Log(e.ToString());
          return null;
      }
    }

    // lazily copied from above, refactor later?
    public static SpriteTrailController CreateSpriteTrailObject(string spriteName, int fps = -1, string startAnim = null, float timeTillAnimStart = -1, float cascadeTimer = -1, float softMaxLength = -1, bool destroyOnEmpty = false)
    {
      try
      {
          List<string> animPaths = ResMap.Get(spriteName);
          string spritePath = animPaths[0];
          GameObject newTrailObject = new GameObject().RegisterPrefab();
          newTrailObject.name = "trailObject";

          int spriteID = ETGMod.Databases.Items.ProjectileCollection.GetSpriteIdByName(spritePath);
          tk2dTiledSprite tiledSprite = newTrailObject.GetOrAddComponent<tk2dTiledSprite>();

          tiledSprite.SetSprite(ETGMod.Databases.Items.ProjectileCollection, spriteID);
          tk2dSpriteDefinition def = tiledSprite.GetCurrentSpriteDef();
          def.colliderVertices = new Vector3[]{
              Vector3.zero,
              def.untrimmedBoundsDataExtents
          };

          tk2dSpriteAnimator animator = newTrailObject.GetOrAddComponent<tk2dSpriteAnimator>();
          tk2dSpriteAnimation animation = newTrailObject.GetOrAddComponent<tk2dSpriteAnimation>();
          animation.clips = new tk2dSpriteAnimationClip[0];
          animator.Library = animation;

          SpriteTrailController trail = newTrailObject.AddComponent<SpriteTrailController>();

          // ---------------- Sets up the animation for the main part of the trail
          if (animPaths != null)
          {
              SetupBeamPart(animation, animPaths, "trail_mid", fps, null, null, def.colliderVertices);
              trail.animation = "trail_mid";
              trail.usesAnimation = true;
          }
          else
          {
              def.BetterConstructOffsetsFromAnchor(Anchor.MiddleLeft); //NOTE: this is already done in SetupBeamPart(), and we don't want to do it twice
              trail.usesAnimation = false;
          }

          if (startAnim != null)
          {
              SetupBeamPart(animation, ResMap.Get(startAnim), "trail_start", fps, null, null, def.colliderVertices);
              trail.startAnimation = "trail_start";
              trail.usesStartAnimation = true;
          }
          else
              trail.usesStartAnimation = false;

          //Trail Variables
          if (softMaxLength > 0) { trail.usesSoftMaxLength = true; trail.softMaxLength = softMaxLength; }
          if (cascadeTimer > 0) { trail.usesCascadeTimer = true; trail.cascadeTimer = cascadeTimer; }
          if (timeTillAnimStart > 0) { trail.usesGlobalTimer = true; trail.globalTimer = timeTillAnimStart; }
          trail.destroyOnEmpty = destroyOnEmpty;
          return trail;
      }
      catch (Exception e)
      {
          ETGModConsole.Log(e.ToString());
          return null;
      }
    }

    // modification of GenerateBeamPrefab() from Planetside of Gunymede
    public static BasicBeamController FixedGenerateBeamPrefab(this Projectile projectile, string spritePath, Vector2 colliderDimensions, Vector2 colliderOffsets, List<string> beamAnimationPaths = null, int beamFPS = -1, List<string> impactVFXAnimationPaths = null, int beamImpactFPS = -1, Vector2? impactVFXColliderDimensions = null, Vector2? impactVFXColliderOffsets = null,
        List<string> endVFXAnimationPaths = null, int beamEndFPS = -1, Vector2? endVFXColliderDimensions = null, Vector2? endVFXColliderOffsets = null, List<string> startVFXAnimationPaths = null, int beamStartFPS = -1, Vector2? startVFXColliderDimensions = null, Vector2? startVFXColliderOffsets = null, bool glows = false,
        bool canTelegraph = false, List<string> beamTelegraphAnimationPaths = null, int beamtelegraphFPS = -1, List<string> beamStartTelegraphAnimationPaths = null, int beamStartTelegraphFPS = -1, List<string> beamEndTelegraphAnimationPaths = null, int beamEndTelegraphFPS = -1, float telegraphTime = 1,
        bool canDissipate = false, List<string> beamDissipateAnimationPaths = null, int beamDissipateFPS = -1, List<string> beamStartDissipateAnimationPaths = null, int beamStartDissipateFPS = -1, List<string> beamEndDissipateAnimationPaths = null, int beamEndDissipateFPS = -1, float dissipateTime = 1,
        List<string> chargeVFXAnimationPaths = null, int beamChargeFPS = -1, Vector2? chargeVFXColliderDimensions = null, Vector2? chargeVFXColliderOffsets = null, bool loopCharge = true)
    {
        try
        {
            if (projectile.specRigidbody) // modified: not all projectiles (esp. preexisting beams) have a specRigidbody component
                projectile.specRigidbody.CollideWithOthers = false;

            float convertedColliderX = colliderDimensions.x / 16f;
            float convertedColliderY = colliderDimensions.y / 16f;
            float convertedOffsetX = colliderOffsets.x / 16f;
            float convertedOffsetY = colliderOffsets.y / 16f;

            tk2dSpriteCollectionData collection = ETGMod.Databases.Items.ProjectileCollection;
            int spriteID = collection.GetSpriteIdByName(spritePath);
            tk2dTiledSprite tiledSprite = projectile.gameObject.GetOrAddComponent<tk2dTiledSprite>();

            tiledSprite.SetSprite(ETGMod.Databases.Items.ProjectileCollection, spriteID);
            tk2dSpriteDefinition def = tiledSprite.GetCurrentSpriteDef();
            def.colliderVertices = new Vector3[]{
                new Vector3(convertedOffsetX, convertedOffsetY, 0f),
                new Vector3(convertedColliderX, convertedColliderY, 0f)
            };

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
                    int frameSpriteId = collection.GetSpriteIdByName(path);
                    tk2dSpriteDefinition frameDef = collection.spriteDefinitions[frameSpriteId];
                    frameDef.BetterConstructOffsetsFromAnchor(Anchor.MiddleLeft);
                    frameDef.colliderVertices = def.colliderVertices;
                    frames.Add(new tk2dSpriteAnimationFrame { spriteId = frameSpriteId, spriteCollection = collection });
                }
                clip.frames = frames.ToArray();
                animation.clips = animation.clips.Concat(new tk2dSpriteAnimationClip[] { clip }).ToArray();
                beamController.beamAnimation = "beam_idle";
            }
            else
            {
                // construct an offset definition for the singular sprite
                def.BetterConstructOffsetsFromAnchor(Anchor.MiddleLeft);
            }

            //------------- Sets up the animation for the part of the beam that touches the wall
            if (endVFXAnimationPaths != null && endVFXColliderDimensions != null && endVFXColliderOffsets != null)
            {
                SetupBeamPart(animation, endVFXAnimationPaths, "beam_end", beamEndFPS, (Vector2)endVFXColliderDimensions, (Vector2)endVFXColliderOffsets);
                beamController.beamEndAnimation = "beam_end";
            }
            else
            {
                SetupBeamPart(animation, beamAnimationPaths, "beam_end", beamFPS, null, null, def.colliderVertices, shouldConstructOffsets: false);
                beamController.beamEndAnimation = "beam_end";
            }

            //---------------Sets up the animaton for the VFX that plays over top of the end of the beam where it hits stuff
            if (impactVFXAnimationPaths != null && impactVFXColliderDimensions != null && impactVFXColliderOffsets != null)
            {
                SetupBeamPart(animation, impactVFXAnimationPaths, "beam_impact", beamImpactFPS, (Vector2)impactVFXColliderDimensions, (Vector2)impactVFXColliderOffsets, anchorOverride: Anchor.MiddleCenter);
                beamController.impactAnimation = "beam_impact";
            }

            //---------------Sets up the animaton for the VFX that plays when the beam is charging
            if (chargeVFXAnimationPaths != null && chargeVFXColliderDimensions != null && chargeVFXColliderOffsets != null)
            {
                SetupBeamPart(animation, chargeVFXAnimationPaths, "beam_charge", beamChargeFPS, (Vector2)chargeVFXColliderDimensions, (Vector2)chargeVFXColliderOffsets, anchorOverride: Anchor.MiddleCenter,
                    wrapMode: loopCharge ? tk2dSpriteAnimationClip.WrapMode.Loop : tk2dSpriteAnimationClip.WrapMode.Once);
                beamController.chargeAnimation = "beam_charge";
            }

            //--------------Sets up the animation for the very start of the beam
            if (startVFXAnimationPaths != null && startVFXColliderDimensions != null && startVFXColliderOffsets != null)
            {
                SetupBeamPart(animation, startVFXAnimationPaths, "beam_start", beamStartFPS, (Vector2)startVFXColliderDimensions, (Vector2)startVFXColliderOffsets/*, anchorOverride: Anchor.MiddleCenter*/);
                beamController.beamStartAnimation = "beam_start";
            }
            else
            {
                SetupBeamPart(animation, beamAnimationPaths, "beam_start", beamFPS, null, null, def.colliderVertices, shouldConstructOffsets: false);
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

    internal static void SetupBeamPart(tk2dSpriteAnimation beamAnimation, List<string> animSpritePaths, string animationName, int fps, Vector2? colliderDimensions = null, Vector2? colliderOffsets = null, Vector3[] overrideVertices = null, tk2dSpriteAnimationClip.WrapMode wrapMode = tk2dSpriteAnimationClip.WrapMode.Loop, Anchor? anchorOverride = null, bool shouldConstructOffsets = true)
    {
        tk2dSpriteAnimationClip clip = new tk2dSpriteAnimationClip() { name = animationName, frames = new tk2dSpriteAnimationFrame[0], fps = fps };
        List<string> spritePaths = animSpritePaths;
        List<tk2dSpriteAnimationFrame> frames = new List<tk2dSpriteAnimationFrame>();
        clip.wrapMode = wrapMode;
        foreach (string path in spritePaths)
        {
            tk2dSpriteCollectionData collection = ETGMod.Databases.Items.ProjectileCollection;
            int frameSpriteId = collection.GetSpriteIdByName(path);
            tk2dSpriteDefinition frameDef = collection.spriteDefinitions[frameSpriteId];
            if (shouldConstructOffsets)
                frameDef.BetterConstructOffsetsFromAnchor(anchorOverride ?? Anchor.MiddleLeft);
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
                    Vector2 actualOffsets = (Vector2)colliderOffsets;
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

public class CwaffVFX
{
    // pools
    private static readonly LinkedList<CwaffVFX> _SpawnedVFX = new();
    private static readonly LinkedList<CwaffVFX> _DespawnedVFX = new();

    // locals
    private GameObject _vfx;
    private LinkedListNode<CwaffVFX> _node;
    private tk2dSprite _sprite;
    private tk2dSpriteAnimator _animator;
    private tk2dSpriteAnimation _library;

    private Vector3    _velocity      = Vector3.zero;
    private float      _curLifeTime   = 0.0f;
    private bool       _fadeOut       = false;
    private float      _fadeStartTime = 0.0f;
    private float      _fadeTotalTime = 0.0f;
    private float      _maxLifeTime   = 0.0f;
    private bool       _setup         = false;
    private bool       _fadeIn        = false;
    private float      _startScale    = 1.0f;
    private float      _endScale      = 1.0f;
    private bool       _changesScale  = false;
    private bool       _shouldDespawn      = false;

    private class CwaffVFXManager : MonoBehaviour
    {
        private void Update()
        {
            int numActive = _SpawnedVFX.Count;
            // ETGModConsole.Log($"updating {numActive} active VFX");
            LinkedListNode<CwaffVFX> current = _SpawnedVFX.First;
            LinkedListNode<CwaffVFX> next;
            for (int i = 0; i < numActive; ++i)
            {
                CwaffVFX c = current.Value;
                if (c == null)
                {
                    ETGModConsole.Log($"bad 1");
                    return;
                }
                next = current.Next;
                c.ManualUpdate();
                if (c._shouldDespawn)
                {
                    _SpawnedVFX.Remove(c._node);
                    _DespawnedVFX.AddLast(c._node);
                }
                current = next;
            }
        }
    }

    // private constructor, can't spawn manually
    private CwaffVFX()
    {
        this._vfx = new();
        UnityEngine.Object.DontDestroyOnLoad(this._vfx); // make our pooled vfx last forever
        this._node = null;
        this._sprite = this._vfx.AddComponent<tk2dSprite>();
        this._animator = this._vfx.AddComponent<tk2dSpriteAnimator>();
        this._library = this._vfx.AddComponent<tk2dSpriteAnimation>();

        this._animator.library = this._library;
        this._animator.playAutomatically = true;
    }

    public static void Spawn(GameObject prefab, Vector3 position, Quaternion? rotation = null,
        Vector2? velocity = null, float lifetime = 0, float? fadeOutTime = null, Transform parent = null, float emissivePower = 0, Color? emissiveColor = null,
        bool fadeIn = false, float startScale = 1.0f, float endScale = 1.0f, float? height = null, bool randomFrame = false)
    {
        // TODO: be smarter about this later
        GameManager.Instance.GetOrAddComponent<CwaffVFXManager>();

        CwaffVFX c;
        if (_DespawnedVFX.Count > 0)
        {
            c = _DespawnedVFX.Pop(); // reusing pooled vfx
        }
        else
        {
            ETGModConsole.Log($"creating new vfx {_SpawnedVFX.Count}");
            c = new();
        }
        if (c._node == null)
            c._node = _SpawnedVFX.AddLast(c);
        else
            _SpawnedVFX.AddLast(c._node);
        c._shouldDespawn = false;
        c.Setup(prefab, position, rotation ?? Quaternion.identity, velocity ?? Vector2.zero, lifetime, fadeOutTime, parent, emissivePower, emissiveColor, fadeIn, startScale, endScale, height, randomFrame);
    }

    private static void Despawn(CwaffVFX c)
    {
        c._shouldDespawn = true;
        c._setup = false;
        c._vfx.SetActive(false);
    }

    public static void SpawnBurst(GameObject prefab, int numToSpawn, Vector2 basePosition, float positionVariance = 0f, Vector2? baseVelocity = null, float minVelocity = 0f, float velocityVariance = 0f,
        FancyVFX.Vel velType = FancyVFX.Vel.Random, FancyVFX.Rot rotType = FancyVFX.Rot.None, float lifetime = 0, float? fadeOutTime = null, Transform parent = null, float emissivePower = 0,
        Color? emissiveColor = null, bool fadeIn = false, bool uniform = false, float startScale = 1.0f, float endScale = 1.0f, float? height = null, bool randomFrame = false)
    {
        Vector2 realBaseVelocity = baseVelocity ?? Vector2.zero;
        float baseAngle = Lazy.RandomAngle();
        for (int i = 0; i < numToSpawn; ++i)
        {
            float posOffsetAngle = uniform ? (baseAngle + 360f * ((float)i / numToSpawn)).Clamp360() : Lazy.RandomAngle();
            Vector2 finalpos = (positionVariance > 0)
                ? basePosition + posOffsetAngle.ToVector((uniform ? 1f : UnityEngine.Random.value) * positionVariance)
                : basePosition;
            Vector2 velocity = velType switch {
                FancyVFX.Vel.Random     => realBaseVelocity + Lazy.RandomAngle().ToVector(minVelocity + UnityEngine.Random.value * velocityVariance),
                FancyVFX.Vel.Radial     => realBaseVelocity + Lazy.RandomAngle().ToVector(minVelocity + velocityVariance),
                FancyVFX.Vel.Away       => realBaseVelocity + posOffsetAngle.ToVector(minVelocity + UnityEngine.Random.value * velocityVariance),
                FancyVFX.Vel.AwayRadial => realBaseVelocity + posOffsetAngle.ToVector(minVelocity + velocityVariance),
                _              => realBaseVelocity,
            };
            Quaternion rot = rotType switch {
                FancyVFX.Rot.Random   => UnityEngine.Random.Range(0f,360f).EulerZ(),
                FancyVFX.Rot.Position => posOffsetAngle.EulerZ(),
                FancyVFX.Rot.Velocity => velocity.EulerZ(),
                _            => Quaternion.identity,
                };
            CwaffVFX.Spawn(
                prefab        : prefab,
                position      : finalpos,
                rotation      : rot,
                velocity      : velocity,
                lifetime      : lifetime,
                fadeIn        : fadeIn,
                fadeOutTime   : fadeOutTime,
                emissivePower : emissivePower,
                emissiveColor : emissiveColor,
                parent        : parent,
                startScale    : startScale,
                endScale      : endScale,
                height        : height,
                randomFrame   : randomFrame
                );
        }
    }

    public void Setup(GameObject prefab, Vector3 position, Quaternion? rotation = null,
        Vector2? velocity = null, float lifetime = 0, float? fadeOutTime = null, Transform parent = null,
        float emissivePower = 0, Color? emissiveColor = null, bool fadeIn = false, float startScale = 1.0f, float endScale = 1.0f, float? height = null,
        bool randomFrame = false)
    {
        this._vfx.SetActive(true);
        this._vfx.transform.position = position;
        this._vfx.transform.localRotation = rotation ?? Quaternion.identity;

        tk2dSprite          prefabSprite  = prefab.GetComponent<tk2dSprite>();
        tk2dSpriteAnimator  prefabAnim    = prefab.GetComponent<tk2dSpriteAnimator>();
        tk2dSpriteAnimation prefabLibrary = prefab.GetComponent<tk2dSpriteAnimation>();
        this._sprite.SetSprite(prefabSprite.collection, prefabSprite.spriteId);
        this._library.clips = prefabLibrary.clips;

        // old stuff

        this._curLifeTime = 0.0f;
        this._fadeIn = fadeIn;

        this._velocity = velocity.HasValue ? (1.0f / C.PIXELS_PER_CELL) * velocity.Value.ToVector3ZisY(0) : Vector3.zero;
        this._maxLifeTime = (lifetime > 0) ? lifetime : 3600f;
        this._fadeOut = fadeOutTime.HasValue;
        if (this._fadeOut)
        {
            this._fadeTotalTime = fadeOutTime.Value;
            this._fadeStartTime = this._maxLifeTime - this._fadeTotalTime;
            // ETGModConsole.Log($"setup with fade from {this._fadeStartTime} to {this._fadeTotalTime} with fadeIn {fadeIn}");
        }
        if (height.HasValue)
        {
            this._sprite.HeightOffGround = height.Value;
            this._sprite.UpdateZDepth();
        }
        this._vfx.transform.parent = parent;

        this._sprite.scale = new Vector3(startScale, startScale, startScale);
        this._startScale   = startScale;
        this._endScale     = endScale;
        this._changesScale = this._startScale != this._endScale;

        if (emissivePower > 0)
        {
            // this._sprite.usesOverrideMaterial = true;
            Material m = this._sprite.renderer.material;
                m.shader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTintableTiltedCutoutEmissive");
                m.SetFloat("_EmissivePower", emissivePower);

            Color emitColor = emissiveColor ?? Color.white;
            // if (emissiveColor.HasValue)
            {
                m.SetFloat("_EmissiveColorPower", 1.55f);
                m.SetColor("_EmissiveColor", emitColor);
                m.SetColor("_OverrideColor", emitColor);
            }
        }
        else
        {
            //TODO: handle non-emissive case
        }

        if (randomFrame)
            this._animator.PickFrame();

        this._setup = true;
    }

    private void ManualUpdate()
    {
        if (!this._setup)
            return;

        this._curLifeTime += BraveTime.DeltaTime;
        float percentDone = this._curLifeTime / this._maxLifeTime;
        if (percentDone >= 1.0f)
        {
            Despawn(this);
            return;
        }

        this._sprite.transform.position += this._velocity * C.FPS * BraveTime.DeltaTime;
        if (this._changesScale)
        {
            float scale = Mathf.Lerp(this._startScale, this._endScale, percentDone);
            this._sprite.transform.localScale = new Vector3(scale, scale, 1.0f);
        }

        if (this._fadeOut && this._curLifeTime > this._fadeStartTime)
        {
            float alpha = (this._curLifeTime - this._fadeStartTime) / this._fadeTotalTime;
            if (!this._fadeIn)
                alpha = 1.0f - alpha;
            this._sprite.renderer.SetAlpha(alpha);
        }
    }
}

// Helper class for making movable / fadeable  VFX
public class FancyVFX : MonoBehaviour
{
    public tk2dBaseSprite sprite;

    private GameObject _vfx           = null;
    private Vector3    _velocity      = Vector3.zero;
    private float      _curLifeTime   = 0.0f;
    private bool       _fadeOut       = false;
    private float      _fadeStartTime = 0.0f;
    private float      _fadeTotalTime = 0.0f;
    private float      _maxLifeTime   = 0.0f;
    private bool       _setup         = false;
    private bool       _fadeIn        = false;
    private float      _startScale    = 1.0f;
    private float      _endScale      = 1.0f;
    private bool       _changesScale  = false;

    private void Start()
    {
        // ETGModConsole.Log($"created new fancy vfx {this.GetHashCode()}");
    }

    private void LateUpdate()
    {
        if (!this._setup)
            return;

        this._curLifeTime += BraveTime.DeltaTime;
        float percentDone = this._curLifeTime / this._maxLifeTime;
        if (percentDone >= 1.0f)
        {
            this._vfx.SafeDestroy();
            this._vfx = null;
            UnityEngine.Object.Destroy(this);
            return;
        }

        this.sprite.transform.position += this._velocity * C.FPS * BraveTime.DeltaTime;
        if (this._changesScale)
        {
            float scale = Mathf.Lerp(this._startScale, this._endScale, percentDone);
            this.sprite.transform.localScale = new Vector3(scale, scale, 1.0f);
        }

        if (this._fadeOut && this._curLifeTime > this._fadeStartTime)
        {
            float alpha = (this._curLifeTime - this._fadeStartTime) / this._fadeTotalTime;
            if (!this._fadeIn)
                alpha = 1.0f - alpha;
            // ETGModConsole.Log($"  setting alpha to {alpha}");
            this.sprite.renderer.SetAlpha(alpha);
        }
    }

    public void Setup(Vector2 velocity, float lifetime = 0, float? fadeOutTime = null, Transform parent = null,
        float emissivePower = 0, Color? emissiveColor = null, bool fadeIn = false, float startScale = 1.0f, float endScale = 1.0f, float? height = null,
        bool randomFrame = false)
    {
        this._vfx = base.gameObject;
        this.sprite = this._vfx.GetComponent<tk2dSprite>();
        this._curLifeTime = 0.0f;
        this._fadeIn = fadeIn;

        this._velocity = (1.0f / C.PIXELS_PER_CELL) * velocity.ToVector3ZisY(0);
        this._maxLifeTime = (lifetime > 0) ? lifetime : 3600f;
        this._fadeOut = fadeOutTime.HasValue;
        if (this._fadeOut)
        {
            this._fadeTotalTime = fadeOutTime.Value;
            this._fadeStartTime = this._maxLifeTime - this._fadeTotalTime;
            // ETGModConsole.Log($"setup with fade from {this._fadeStartTime} to {this._fadeTotalTime} with fadeIn {fadeIn}");
        }
        if (height.HasValue)
        {
            this.sprite.HeightOffGround = height.Value;
            this.sprite.UpdateZDepth();
        }
        this.transform.parent = parent;

        this._startScale   = startScale;
        this._endScale     = endScale;
        this._changesScale = this._startScale != this._endScale;

        if (emissivePower > 0)
        {
            // this._sprite.usesOverrideMaterial = true;
            Material m = this.sprite.renderer.material;
                m.shader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTintableTiltedCutoutEmissive");
                m.SetFloat("_EmissivePower", emissivePower);

            Color emitColor = emissiveColor ?? Color.white;
            // if (emissiveColor.HasValue)
            {
                m.SetFloat("_EmissiveColorPower", 1.55f);
                m.SetColor("_EmissiveColor", emitColor);
                m.SetColor("_OverrideColor", emitColor);
            }
        }

        if (randomFrame)
            this._vfx.PickFrame();

        this._setup = true;
    }

    /// <summary>Spawn a single FancyVFX from a normal SpawnManager.SpawnVFX</summary>
    /// <param name="prefab">Prefab for the VFX we want to spawn</param>
    /// <param name="position">Position at which the VFX is spawned</param>
    /// <param name="rotation">Rotation of the VFX sprite.</param>
    /// <param name="velocity">Velocity with which the VFX is launched</param>
    /// <param name="lifetime">Time before VFX automatically despawn. Set to 0 for no automatic despawning.</param>
    /// <param name="fadeOutTime">Time before VFX fade out to 0 alpha. If greater than lifetime, VFX will spawn in partially faded. Disabled if null.</param>
    /// <param name="parent">If non-null, VFX will automatically move with the parent transform.</param>
    /// <param name="emissivePower">Emissive power of the VFX. Ignored if fadeOutTime is non-null.</param>
    /// <param name="emissiveColor">Emissive color of the VFX. Ignored if fadeOutTime is non-null.</param>
    /// <param name="fadeIn">If true, VFX will fade in instead of fading out.</param>
    /// <param name="startScale">Starting scale of the VFX sprite.</param>
    /// <param name="endScale">Ending scale of the VFX sprite.</param>
    /// <param name="height">Height of the VFX above the ground. Positive = in front of most things, negative = behind most things.</param>
    /// <param name="randomFrame">If true, animation frames are treated as separate VFX, and one is selected at random.</param>
    public static FancyVFX Spawn(GameObject prefab, Vector3 position, Quaternion? rotation = null,
        Vector2? velocity = null, float lifetime = 0, float? fadeOutTime = null, Transform parent = null, float emissivePower = 0, Color? emissiveColor = null,
        bool fadeIn = false, float startScale = 1.0f, float endScale = 1.0f, float? height = null, bool randomFrame = false)
    {
        GameObject v = SpawnManager.SpawnVFX(prefab, position, rotation ?? Quaternion.identity, ignoresPools: false);
        FancyVFX fv = v.AddComponent<FancyVFX>();
        fv.Setup(velocity ?? Vector2.zero, lifetime, fadeOutTime, parent, emissivePower, emissiveColor, fadeIn, startScale, endScale, height, randomFrame);
        return fv;
    }

    public enum Rot
    {
        None,     // do note rotate the VFX
        Random,   // rotate the VFX randomly
        Position, // rotation matches the VFX's position relative to the base position
        Velocity, // rotation matches the VFX's velocity
    }

    public enum Vel
    {
        Random,       // base velocity is augmented by a random vector with magnitude between 0 and velocityVariance
        Radial,       // base velocity is augmented by a random vector with magnitude of exactly velocityVariance
        Away,         // base velocity is augmented by a vector away from position with magnitude between 0 and velocityVariance
        AwayRadial,   // base velocity is augmented by a vector away from position with magnitude of exactly velocityVariance
    }

    /// <summary>Spawn a burst of FancyVFX</summary>
    /// <param name="prefab">Prefab for the VFX we want to spawn</param>
    /// <param name="numToSpawn">Number of VFX to spawn</param>
    /// <param name="basePosition">Anchor position from which all VFX are spawned relative to</param>
    /// <param name="positionVariance">Maximum distance from the anchor position from which VFX will spawn</param>
    /// <param name="baseVelocity">Anchor velocity for which all VFX are launched relative to</param>
    /// <param name="minVelocity">Minimum magnitude of velocity for each individual VFX</param>
    /// <param name="velocityVariance">Maximum magnitude of deviance for each individual VFX from the baseVelocity</param>
    /// <param name="velType">Relation between baseVelocity and velocityVariance. Possible values:<br/><br/>
    ///   Random: base velocity is augmented by a random vector with magnitude between 0 and velocityVariance<br/>
    ///   Radial: base velocity is augmented by a random vector with magnitude of exactly velocityVariance<br/>
    ///   Away: base velocity is augmented by a vector away from position with magnitude between 0 and velocityVariance<br/>
    ///   AwayRadial base velocity is augmented by a vector away from position with magnitude of exactly velocityVariance<br/>
    /// </param>
    /// <param name="rotType">How the VFX are rotated. Possible values:<br/><br/>
    ///   None: do note rotate the VFX<br/>
    ///   Random: rotate the VFX randomly<br/>
    ///   Position rotation matches the VFX's position relative to the base position<br/>
    ///   Velocity rotation matches the VFX's velocity<br/>
    /// </param>
    /// <param name="lifetime">Time before VFX automatically despawn. Set to 0 for no automatic despawning.</param>
    /// <param name="fadeOutTime">Time before VFX fade out to 0 alpha. If greater than lifetime, VFX will spawn in partially faded. Disabled if null.</param>
    /// <param name="parent">If non-null, VFX will automatically move with the parent transform.</param>
    /// <param name="emissivePower">Emissive power of the VFX. Ignored if fadeOutTime is non-null.</param>
    /// <param name="emissiveColor">Emissive color of the VFX. Ignored if fadeOutTime is non-null.</param>
    /// <param name="fadeIn">If true, VFX will fade in instead of fading out.</param>
    /// <param name="uniform">If true, VFX will spawn with uniform angles around basePosition with magnitude positionVariance.</param>
    /// <param name="startScale">Starting scale of the VFX sprite.</param>
    /// <param name="endScale">Ending scale of the VFX sprite.</param>
    /// <param name="height">Height of the VFX above the ground. Positive = in front of most things, negative = behind most things.</param>
    /// <param name="randomFrame">If true, animation frames are treated as separate VFX, and one is selected at random.</param>
    public static void SpawnBurst(GameObject prefab, int numToSpawn, Vector2 basePosition, float positionVariance = 0f, Vector2? baseVelocity = null, float minVelocity = 0f, float velocityVariance = 0f,
        Vel velType = Vel.Random, Rot rotType = Rot.None, float lifetime = 0, float? fadeOutTime = null, Transform parent = null, float emissivePower = 0,
        Color? emissiveColor = null, bool fadeIn = false, bool uniform = false, float startScale = 1.0f, float endScale = 1.0f, float? height = null, bool randomFrame = false)
    {
        Vector2 realBaseVelocity = baseVelocity ?? Vector2.zero;
        float baseAngle = Lazy.RandomAngle();
        for (int i = 0; i < numToSpawn; ++i)
        {
            float posOffsetAngle = uniform ? (baseAngle + 360f * ((float)i / numToSpawn)).Clamp360() : Lazy.RandomAngle();
            Vector2 finalpos = (positionVariance > 0)
                ? basePosition + posOffsetAngle.ToVector((uniform ? 1f : UnityEngine.Random.value) * positionVariance)
                : basePosition;
            Vector2 velocity = velType switch {
                Vel.Random     => realBaseVelocity + Lazy.RandomAngle().ToVector(minVelocity + UnityEngine.Random.value * velocityVariance),
                Vel.Radial     => realBaseVelocity + Lazy.RandomAngle().ToVector(minVelocity + velocityVariance),
                Vel.Away       => realBaseVelocity + posOffsetAngle.ToVector(minVelocity + UnityEngine.Random.value * velocityVariance),
                Vel.AwayRadial => realBaseVelocity + posOffsetAngle.ToVector(minVelocity + velocityVariance),
                _              => realBaseVelocity,
            };
            Quaternion rot = rotType switch {
                Rot.Random   => UnityEngine.Random.Range(0f,360f).EulerZ(),
                Rot.Position => posOffsetAngle.EulerZ(),
                Rot.Velocity => velocity.EulerZ(),
                _            => Quaternion.identity,
                };
            FancyVFX.Spawn(
                prefab        : prefab,
                position      : finalpos,
                rotation      : rot,
                velocity      : velocity,
                lifetime      : lifetime,
                fadeIn        : fadeIn,
                fadeOutTime   : fadeOutTime,
                emissivePower : emissivePower,
                emissiveColor : emissiveColor,
                parent        : parent,
                startScale    : startScale,
                endScale      : endScale,
                height        : height,
                randomFrame   : randomFrame
                );
        }
    }

    // Make a new FancyVFX from a normal SpawnManager.SpawnVFX, ignoring pools (necessary for adding custom components)
    public static FancyVFX SpawnUnpooled(GameObject prefab, Vector3 position, Quaternion? rotation = null,
        Vector2? velocity = null, float lifetime = 0, float? fadeOutTime = null, Transform parent = null, float emissivePower = 0, Color? emissiveColor = null, bool fadeIn = false)
    {
        GameObject v = SpawnManager.SpawnVFX(prefab, position, rotation ?? Quaternion.identity, ignoresPools: true);
        FancyVFX fv = v.AddComponent<FancyVFX>();
        fv.Setup(velocity ?? Vector2.zero, lifetime, fadeOutTime, parent, emissivePower, emissiveColor, fadeIn);
        return fv;
    }

    // Make a new FancyVFX from a GameObject's current sprite, frame, position, etc. (does not have a spriteanimator, use with caution)
    public static FancyVFX FromCurrentFrame(tk2dBaseSprite osprite)
    {
        if (!osprite)
            return null;

        // GameObject g = UnityEngine.Object.Instantiate(new GameObject(), osprite.WorldCenter, osprite.transform.rotation);
        tk2dBaseSprite sprite = osprite.DuplicateInWorld();

        FancyVFX fv = sprite.AddComponent<FancyVFX>();
            fv.sprite = sprite;
        return fv;
    }

    private const float _MIN_SCALE      = 0.4f; // minimum scale our pickup can shrink down to
    private const float _VANISH_PERCENT = 0.5f; // percent of the way through the wrap animation the pickup should vanish

    // Make the FancyVFX arc smoothly from its current position to a target position
    public void ArcTowards(float animLength, tk2dBaseSprite targetSprite, bool useBottom = false, float minScale = _MIN_SCALE, float vanishPercent = _VANISH_PERCENT)
    {
        // Setup the VFX object for the pickup
        this.Setup(velocity: Vector2.zero, lifetime: animLength * vanishPercent, fadeOutTime: animLength * vanishPercent, fadeIn: false);

        // Do the actual arcing
        this.StartCoroutine(ArcTowards_CR(
          animLength: animLength, targetSprite: targetSprite, useBottom: useBottom, minScale: minScale, vanishPercent: vanishPercent
          ));
    }

    private IEnumerator ArcTowards_CR(float animLength, tk2dBaseSprite targetSprite, bool useBottom, float minScale, float vanishPercent)
    {
        // Suck the pickup into the present and wait for the animation to play out
        Vector2 startPosition = this.sprite.WorldCenter;
        float loopLength      = animLength * vanishPercent;
        for (float elapsed = 0f; elapsed < loopLength; elapsed += BraveTime.DeltaTime)
        {
            if (!this)
                break;

            float percentDone                = Mathf.Clamp01(elapsed / loopLength);
            float cubicLerp                  = Ease.OutCubic(percentDone);
            Vector2 extraOffset              = new Vector2(0f, 2f * Mathf.Sin(Mathf.PI * cubicLerp));
            Vector2 curPosition              = extraOffset + Vector2.Lerp(startPosition, useBottom ? targetSprite.WorldBottomCenter : targetSprite.WorldCenter, cubicLerp);
            float scale                      = 1f - ((1f - minScale) * cubicLerp);
            this.sprite.transform.localScale = new Vector3(scale, scale, 1f);
            this.sprite.PlaceAtScaledPositionByAnchor(curPosition, Anchor.MiddleCenter);
            yield return null;
        }
        yield break;
    }
}

// Helper class for doing orbital effects around an AIActor
public class OrbitalEffect : MonoBehaviour
{
    private const float _ORBIT_RPS = 0.5f;
    private const float _ORBIT_SPR = 1.0f / _ORBIT_RPS; // seconds per rotation

    private AIActor          _enemy       = null;
    private List<GameObject> _orbitals    = null;
    private int              _numOrbitals = 0;
    private float            _enemyGirth  = 0.0f;
    private float            _enemyHeight = 0.0f;
    private float            _orbitTimer  = 0.0f;
    private float            _orbitalGap  = 0.0f;
    private float            _rps         = 0.0f;
    private float            _spr         = 0.0f;
    private bool             _isEmissive  = false;
    private bool             _overhead    = false;
    private bool             _didSetup    = false;
    private bool             _rotates     = false;
    private bool             _flips       = false;
    private bool             _fades       = false;
    private float            _bobAmount   = 0.0f;

    public void SetupOrbitals(GameObject vfx, int numOrbitals = 3, float rps = 0.5f, bool isEmissive = false,
        bool isOverhead = false, bool rotates = true, bool flips = false, bool fades = false, float bobAmount = 0.0f)
    {
        this._enemy        = base.GetComponent<AIActor>();
        this._enemyGirth   = this._enemy.sprite.GetBounds().size.x / 2.0f; // get the x radius of the enemy's sprite
        this._enemyHeight  = this._enemy.sprite.GetBounds().size.y / 2.0f; // get the y radius of the enemy's sprite
        this._orbitTimer   = 0f;
        this._orbitals     = new();
        this._numOrbitals  = numOrbitals;
        this._orbitalGap   = 360.0f / (float)this._numOrbitals;
        this._isEmissive   = isEmissive;
        this._overhead     = isOverhead;
        this._rps          = rps;
        this._spr          = 1.0f / this._rps;
        this._rotates      = rotates;
        this._flips        = flips;
        this._fades        = fades;
        this._bobAmount    = bobAmount;

        // Spawn orbitals
        for (int i = 0; i < this._numOrbitals; ++i)
            this._orbitals.Add(SpawnManager.SpawnVFX(
                vfx, this._enemy.CenterPosition.ToVector3ZisY(-1), Quaternion.identity));
        UpdateOrbitals();

        this._didSetup = true;
    }

    public void ClearOrbitals()
    {
        foreach (GameObject g in this._orbitals)
            UnityEngine.Object.Destroy(g);
        this._orbitals.Clear();
        this._numOrbitals = 0;
        this._orbitalGap  = 360.0f;
    }

    public void AddOrbital(GameObject vfx)
    {
        this._orbitals.Add(SpawnManager.SpawnVFX(
            vfx, this._enemy.CenterPosition.ToVector3ZisY(-1), Quaternion.identity));
        this._numOrbitals += 1;
        this._orbitalGap   = 360.0f / (float)this._numOrbitals;
    }

    private void Update()
    {
        if (!this._didSetup)
            return;

        if (!this._enemy || !this._enemy.healthHaver || this._enemy.healthHaver.IsDead)
        {
            HandleEnemyDied();
            return;
        }

        this._orbitTimer += BraveTime.DeltaTime;
        if (this._orbitTimer > this._spr)
            this._orbitTimer -= this._spr;

        UpdateOrbitals();
    }

    private void OnDestroy()
    {
        HandleEnemyDied();
    }

    public void HandleEnemyDied()
    {
        if (this._orbitals == null)
            return;
        foreach (GameObject g in this._orbitals)
            UnityEngine.Object.Destroy(g);
        this._orbitals = null;
    }

    private float OverheadOffset()
    {
        return this._overhead ? this._enemyHeight : 0f;
    }

    private void UpdateOrbitals()
    {
        int i = 0;
        float orbitOffset = this._orbitTimer / this._spr;
        float z = C.PIXELS_PER_TILE * this._enemyGirth;

        float power = 2f * Mathf.Abs(Mathf.Sin(2.0f * Mathf.PI * orbitOffset));

        foreach (GameObject g in this._orbitals)
        {
            tk2dSprite sprite = g.GetComponent<tk2dSprite>();
            sprite.renderer.enabled = this._enemy.renderer.enabled;
            float angle = (this._orbitalGap * i + 360.0f * orbitOffset).Clamp360();
            float radAngle = angle / 57.29578f;
            Vector2 avec   = angle.ToVector();
            Vector2 offset = new Vector2(1.5f * this._enemyGirth * avec.x, 0.75f * this._enemyGirth * avec.y + OverheadOffset());
            if (this._bobAmount > 0)
                offset += new Vector2(0f, this._bobAmount * Mathf.Sin(radAngle));
            g.transform.position = (this._enemy.CenterPosition + offset).ToVector3ZisY(angle < 180 ? z : -z);
            if (this._rotates)
                g.transform.rotation = angle.EulerZ();
            if (this._flips)
                g.transform.localScale = new Vector3(-Mathf.Sin(radAngle), 1f, 1f);
            if (this._fades)
                sprite.SetAlpha((avec.y > 0) ? 0f : Mathf.Abs(Mathf.Sin(radAngle)));

            if (this._isEmissive)
                sprite.renderer.material.SetFloat("_EmissivePower", power);

            ++i;
        }
    }
}

// Fade in from complete transparency, emit light for a bit, then fade back out
public class GlowAndFadeOut : MonoBehaviour //NOTE: can't be used with pooled VFX
{
    private const float _MAX_EMIT = 200f;

    public void Setup(float fadeInTime, float glowInTime, float glowOutTime, float fadeOutTime, float maxEmit = _MAX_EMIT, bool destroy = true)
    {
        StartCoroutine(Top(fadeInTime, glowInTime, glowOutTime, fadeOutTime, maxEmit, destroy));
    }

    private IEnumerator Top(float fadeInTime, float glowInTime, float glowOutTime, float fadeOutTime, float maxEmit = _MAX_EMIT, bool destroy = true)
    {
        tk2dSprite sprite = base.GetComponent<tk2dSprite>();
        sprite.usesOverrideMaterial = true;

        for (float elapsed = 0f; elapsed < fadeInTime; elapsed += BraveTime.DeltaTime)
        {
            float percentDone = elapsed / fadeInTime;
            base.gameObject.SetAlpha(percentDone);
            yield return null;
        }

        sprite.renderer.material.shader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTintableTiltedCutoutEmissive");

        for (float elapsed = 0f; elapsed < glowInTime; elapsed += BraveTime.DeltaTime)
        {
            float percentLeft = 1f - elapsed / glowInTime;
            float quadraticEase = 1f - percentLeft * percentLeft;
            sprite.renderer.material.SetFloat("_EmissivePower", maxEmit * quadraticEase);
            yield return null;
        }

        for (float elapsed = 0f; elapsed < glowOutTime; elapsed += BraveTime.DeltaTime)
        {
            float percentDone = elapsed / glowOutTime;
            float quadraticEase = 1f - percentDone * percentDone;
            sprite.renderer.material.SetFloat("_EmissivePower", maxEmit * quadraticEase);
            yield return null;
        }

        for (float elapsed = 0f; elapsed < fadeOutTime; elapsed += BraveTime.DeltaTime)
        {
            float percentDone = elapsed / fadeOutTime;
            base.gameObject.SetAlpha(1f - percentDone);
            yield return null;
        }

        if (destroy)
            UnityEngine.Object.Destroy(base.gameObject);
        else
            base.gameObject.SetAlpha(1f);
        yield break;
    }
}
