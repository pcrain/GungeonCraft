namespace CwaffingTheGungy;

public static class VFX
{
    private const int PIXELS_ABOVE_HEAD = 2;

    private static GameObject VFXScapegoat = new();
    private static tk2dSpriteCollectionData OverheadVFXCollection;
    private static Dictionary<GameActor,List<GameObject>> extantSprites = new();

    public static Dictionary<string,int> sprites = new();
    public static Dictionary<string,GameObject> animations = new();
    public static Dictionary<string,VFXPool> vfxpool = new();
    public static Dictionary<string,VFXComplex> vfxcomplex = new();
    private static Dictionary<GameObject,VFXPool> vfxObjectToPoolMap = new();

    public static GameObject LaserSightPrefab;
    public static GameObject MiniPickup;

    public static tk2dSpriteCollectionData SpriteCollection
    {
        get { return OverheadVFXCollection; }
    }

    public static void Init()
    {
        // Initialize VFX collections
        #region VFX Initialization
            OverheadVFXCollection = SpriteBuilder.ConstructCollection(VFXScapegoat, "OverheadVFX_Collection");
            UnityEngine.Object.DontDestroyOnLoad(VFXScapegoat);
            UnityEngine.Object.DontDestroyOnLoad(OverheadVFXCollection);
        #endregion

        #region Shared Assets
            LaserSightPrefab = LoadHelper.LoadAssetFromAnywhere("assets/resourcesbundle/global vfx/vfx_lasersight.prefab") as GameObject;
        #endregion

        #region Shared VFX
            // Shared by Sans and potentially future reticle users
            RegisterSprite("CwaffingTheGungy/Resources/MiscVFX/reticle_white");
            RegisterSprite("CwaffingTheGungy/Resources/MiscVFX/reticle_orange");
            RegisterSprite("CwaffingTheGungy/Resources/MiscVFX/reticle_blue");
            RegisterSprite("CwaffingTheGungy/Resources/MiscVFX/fancy_line");
            RegisterSprite("CwaffingTheGungy/Resources/MiscVFX/whip_segment");
            RegisterSprite("CwaffingTheGungy/Resources/MiscVFX/whip_segment_base");
            // Shared by Blackjack and possibly future auto-pickup items
            MiniPickup = VFX.Create("mini_pickup", 12, loops: false, anchor: Anchor.MiddleCenter);
        #endregion
    }

    /// <summary>
    /// Register a single-frame static sprite
    /// </summary>
    public static void RegisterSprite(string path)
    {
        sprites[path.Substring(path.LastIndexOf("/")+1)] = SpriteBuilder.AddSpriteToCollection(path, OverheadVFXCollection);
    }

    /// <summary>
    /// Generically register a VFX as a GameObject (animated sprite), VFXComplex, or VFXPool
    /// </summary>
    public static void RegisterVFX(string name, List<string> spritePaths, float fps, bool loops = true, int loopStart = -1, float scale = 1.0f, Anchor anchor = Anchor.MiddleCenter, IntVector2? dimensions = null, bool usesZHeight = false, float zHeightOffset = 0, bool persist = false, VFXAlignment alignment = VFXAlignment.NormalAligned, float emissivePower = -1, Color? emissiveColour = null, bool orphaned = false, bool attached = true)
    {
        // GameObject Obj     = new GameObject(name);
        GameObject Obj     = SpriteBuilder.SpriteFromResource(spritePaths[0], new GameObject(name));
        VFXComplex complex = new VFXComplex();
        VFXObject vfObj    = new VFXObject();
        VFXPool pool       = new VFXPool();
        pool.type          = VFXPoolType.All;
        Obj.RegisterPrefab();

        tk2dBaseSprite baseSprite = Obj.GetComponent<tk2dBaseSprite>();
        tk2dSpriteDefinition baseDef = baseSprite.GetCurrentSpriteDef();
        baseDef.ConstructOffsetsFromAnchor(
            Anchor.LowerCenter,
            baseDef.position3);

        int spriteID = SpriteBuilder.AddSpriteToCollection(spritePaths[0], OverheadVFXCollection);

        tk2dSprite sprite = Obj.GetOrAddComponent<tk2dSprite>();
        sprite.SetSprite(OverheadVFXCollection, spriteID);
        tk2dSpriteDefinition defaultDef = sprite.GetCurrentSpriteDef();

        if (dimensions is IntVector2 dims)
        {
            defaultDef.colliderVertices = new Vector3[]{
                      new Vector3(0f, 0f, 0f),
                      new Vector3((dims.x / C.PIXELS_PER_TILE), (dims.y / C.PIXELS_PER_TILE), 0f)
                  };
        }
        else
        {
            defaultDef.colliderVertices = new Vector3[]{
                      new Vector3(0f, 0f, 0f),
                      new Vector3(
                        baseSprite.GetCurrentSpriteDef().position3.x / C.PIXELS_PER_TILE,
                        baseSprite.GetCurrentSpriteDef().position3.y / C.PIXELS_PER_TILE,
                        0f)
                  };
        }

        tk2dSpriteAnimator animator           = Obj.GetOrAddComponent<tk2dSpriteAnimator>();
        tk2dSpriteAnimation animation         = Obj.GetOrAddComponent<tk2dSpriteAnimation>();
        animation.clips                       = new tk2dSpriteAnimationClip[0];
        animator.Library                      = animation;
        tk2dSpriteAnimationClip clip          = new tk2dSpriteAnimationClip() { name = "start", frames = new tk2dSpriteAnimationFrame[0], fps = fps };
        List<tk2dSpriteAnimationFrame> frames = new List<tk2dSpriteAnimationFrame>();
        for (int i = 0; i < spritePaths.Count; i++)
        {
            int frameSpriteId                   = SpriteBuilder.AddSpriteToCollection(spritePaths[i], OverheadVFXCollection);
            tk2dSpriteDefinition frameDef       = OverheadVFXCollection.spriteDefinitions[frameSpriteId];
            frameDef.ConstructOffsetsFromAnchor(anchor);
            frameDef.colliderVertices = defaultDef.colliderVertices;
            frameDef.material.shader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTintableTiltedCutoutEmissive");
            frameDef.materialInst.shader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTintableTiltedCutoutEmissive");
            if (emissivePower > 0) {
                frameDef.material.SetFloat("_EmissivePower", emissivePower);
                frameDef.material.SetFloat("_EmissiveColorPower", 1.55f);
                frameDef.materialInst.SetFloat("_EmissivePower", emissivePower);
                frameDef.materialInst.SetFloat("_EmissiveColorPower", 1.55f);
            }
            if (emissiveColour != null)
            {
                frameDef.material.SetColor("_EmissiveColor", (Color)emissiveColour);
                frameDef.materialInst.SetColor("_EmissiveColor", (Color)emissiveColour);
            }
            frames.Add(new tk2dSpriteAnimationFrame { spriteId = frameSpriteId, spriteCollection = OverheadVFXCollection });
        }
        if (emissivePower > 0) {
            sprite.renderer.material.shader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTintableTiltedCutoutEmissive");
            sprite.renderer.material.SetFloat("_EmissivePower", emissivePower);
            sprite.renderer.material.SetFloat("_EmissiveColorPower", 1.55f);
        }
        if (emissiveColour != null)
            sprite.renderer.material.SetColor("_EmissiveColor", (Color)emissiveColour);
        clip.frames     = frames.ToArray();
        if (loopStart > 0)
        {
            clip.wrapMode  = tk2dSpriteAnimationClip.WrapMode.LoopSection;
            clip.loopStart = loopStart;
        }
        else
            clip.wrapMode   = loops ? tk2dSpriteAnimationClip.WrapMode.Loop : tk2dSpriteAnimationClip.WrapMode.Once;
        animation.clips = animation.clips.Concat(new tk2dSpriteAnimationClip[] { clip }).ToArray();
        if (!persist)
        {
            SpriteAnimatorKiller kill = animator.gameObject.AddComponent<SpriteAnimatorKiller>();
            kill.fadeTime = -1f;
            kill.animator = animator;
            kill.delayDestructionTime = -1f;
        }
        animator.playAutomatically = true;
        animator.DefaultClipId     = animator.GetClipIdByName("start");
        vfObj.attached             = attached;
        vfObj.orphaned             = orphaned;
        vfObj.persistsOnDeath      = persist;
        vfObj.usesZHeight          = usesZHeight;
        vfObj.zHeight              = zHeightOffset;
        vfObj.alignment            = alignment;
        vfObj.destructible         = false;

        if (scale != 1.0f)
            sprite.scale = new Vector3(scale, scale, scale);

        vfObj.effect               = Obj;
        complex.effects            = new VFXObject[] { vfObj };
        pool.effects               = new VFXComplex[] { complex };

        vfxpool[name]    = pool;
        vfxcomplex[name] = complex;
        animations[name] = Obj;
    }

    /// <summary>
    /// Register and return a VFXObject
    /// </summary>
    public static GameObject Create(string name, float fps, bool loops = true, int loopStart = -1, float scale = 1.0f, Anchor anchor = Anchor.MiddleCenter, IntVector2? dimensions = null, bool usesZHeight = false, float zHeightOffset = 0, bool persist = false, VFXAlignment alignment = VFXAlignment.NormalAligned, float emissivePower = -1, Color? emissiveColour = null, bool orphaned = false, bool attached = true)
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
    public static VFXPool CreatePool(string name, float fps, bool loops = true, int loopStart = -1, float scale = 1.0f, Anchor anchor = Anchor.MiddleCenter, IntVector2? dimensions = null, bool usesZHeight = false, float zHeightOffset = 0, bool persist = false, VFXAlignment alignment = VFXAlignment.NormalAligned, float emissivePower = -1, Color? emissiveColour = null, bool orphaned = false, bool attached = true)
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
    public static VFXComplex CreateComplex(string name, float fps, bool loops = true, int loopStart = -1, float scale = 1.0f, Anchor anchor = Anchor.MiddleCenter, IntVector2? dimensions = null, bool usesZHeight = false, float zHeightOffset = 0, bool persist = false, VFXAlignment alignment = VFXAlignment.NormalAligned, float emissivePower = -1, Color? emissiveColour = null, bool orphaned = false, bool attached = true)
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
            gunOwner.sprite.WorldCenter.x,
            gunOwner.sprite.WorldTopCenter.y + PIXELS_ABOVE_HEAD/C.PIXELS_PER_TILE);
        tk2dSprite overheadSprite = newSprite.AddComponent<tk2dSprite>();
        extantSprites[gunOwner].Add(newSprite);
        overheadSprite.SetSprite(OverheadVFXCollection, sprites[name]);
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

        GameObject newSprite = UnityEngine.Object.Instantiate<GameObject>(animations[name]);

        tk2dBaseSprite baseSprite = newSprite.GetComponent<tk2dBaseSprite>();
        newSprite.transform.parent = gunOwner.transform;
        newSprite.transform.position = new Vector3(
            gunOwner.sprite.WorldCenter.x,
            gunOwner.sprite.WorldTopCenter.y + PIXELS_ABOVE_HEAD/C.PIXELS_PER_TILE);

        extantSprites[gunOwner].Add(baseSprite.gameObject);

        Bounds bounds = gunOwner.sprite.GetBounds();
        Vector3 vector = gunOwner.transform.position + new Vector3((bounds.max.x + bounds.min.x) / 2f, bounds.max.y, 0f).Quantize(0.0625f);
        newSprite.transform.position = gunOwner.sprite.WorldCenter.ToVector3ZUp(0f).WithY(vector.y);
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
            AkSoundEngine.PostEvent("minecraft_totem_pop_sound", player.gameObject);

        GameObject g = UnityEngine.Object.Instantiate(new GameObject(), player.sprite.WorldCenter, Quaternion.identity);
        tk2dSprite sprite = g.AddComponent<tk2dSprite>();
            sprite.SetSprite(item.sprite.collection, item.sprite.spriteId);
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

        UnityEngine.Object.Destroy(sprite);
        UnityEngine.Object.Destroy(g);
        yield break;
    }

    // yoinked from SomeBunny
    public static TrailController CreateTrailObject(string spritePath, Vector2 colliderDimensions, Vector2 colliderOffsets, List<string> animPaths = null, int animFPS = -1, List<string> startAnimPaths = null, int startAnimFPS = -1, float timeTillAnimStart = -1, float cascadeTimer = -1, float softMaxLength = -1, bool destroyOnEmpty = false)
    {
      try
      {
          // GameObject newTrailObject = UnityEngine.Object.Instantiate(new GameObject()).RegisterPrefab();
          GameObject newTrailObject = new GameObject().RegisterPrefab();
          // FakePrefab.InstantiateAndFakeprefab(newTrailObject);
          newTrailObject.name = "trailObject";
          float convertedColliderX = colliderDimensions.x / 16f;
          float convertedColliderY = colliderDimensions.y / 16f;
          float convertedOffsetX = colliderOffsets.x / 16f;
          float convertedOffsetY = colliderOffsets.y / 16f;

          int spriteID = SpriteBuilder.AddSpriteToCollection(spritePath, ETGMod.Databases.Items.ProjectileCollection);
          tk2dTiledSprite tiledSprite = newTrailObject.GetOrAddComponent<tk2dTiledSprite>();

          tiledSprite.SetSprite(ETGMod.Databases.Items.ProjectileCollection, spriteID);
          tk2dSpriteDefinition def = tiledSprite.GetCurrentSpriteDef();
          def.colliderVertices = new Vector3[]{
              new Vector3(convertedOffsetX, convertedOffsetY, 0f),
              new Vector3(convertedColliderX, convertedColliderY, 0f)
          };

          def.ConstructOffsetsFromAnchor(Anchor.MiddleLeft);

          tk2dSpriteAnimator animator = newTrailObject.GetOrAddComponent<tk2dSpriteAnimator>();
          tk2dSpriteAnimation animation = newTrailObject.GetOrAddComponent<tk2dSpriteAnimation>();
          animation.clips = new tk2dSpriteAnimationClip[0];
          animator.Library = animation;

          TrailController trail = newTrailObject.AddComponent<TrailController>();

          // ---------------- Sets up the animation for the main part of the trail
          if (animPaths != null)
          {
              BeamHelpers.SetupBeamPart(animation, animPaths, "trail_mid", animFPS, null, null, def.colliderVertices);
              trail.animation = "trail_mid";
              trail.usesAnimation = true;
          }
          else
              trail.usesAnimation = false;

          if (startAnimPaths != null)
          {
              BeamHelpers.SetupBeamPart(animation, startAnimPaths, "trail_start", startAnimFPS, null, null, def.colliderVertices);
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

    // lazily copied from above, refactor later?
    public static SpriteTrailController CreateSpriteTrailObject(string spritePath, Vector2 colliderDimensions, Vector2 colliderOffsets, List<string> animPaths = null, int animFPS = -1, List<string> startAnimPaths = null, int startAnimFPS = -1, float timeTillAnimStart = -1, float cascadeTimer = -1, float softMaxLength = -1, bool destroyOnEmpty = false)
    {
      try
      {
          // GameObject newTrailObject = UnityEngine.Object.Instantiate(new GameObject()).RegisterPrefab();
          GameObject newTrailObject = new GameObject().RegisterPrefab();
          // FakePrefab.InstantiateAndFakeprefab(newTrailObject);
          newTrailObject.name = "trailObject";
          float convertedColliderX = colliderDimensions.x / 16f;
          float convertedColliderY = colliderDimensions.y / 16f;
          float convertedOffsetX = colliderOffsets.x / 16f;
          float convertedOffsetY = colliderOffsets.y / 16f;

          int spriteID = SpriteBuilder.AddSpriteToCollection(spritePath, ETGMod.Databases.Items.ProjectileCollection);
          tk2dTiledSprite tiledSprite = newTrailObject.GetOrAddComponent<tk2dTiledSprite>();

          tiledSprite.SetSprite(ETGMod.Databases.Items.ProjectileCollection, spriteID);
          tk2dSpriteDefinition def = tiledSprite.GetCurrentSpriteDef();
          def.colliderVertices = new Vector3[]{
              new Vector3(convertedOffsetX, convertedOffsetY, 0f),
              new Vector3(convertedColliderX, convertedColliderY, 0f)
          };

          def.ConstructOffsetsFromAnchor(Anchor.MiddleLeft);

          tk2dSpriteAnimator animator = newTrailObject.GetOrAddComponent<tk2dSpriteAnimator>();
          tk2dSpriteAnimation animation = newTrailObject.GetOrAddComponent<tk2dSpriteAnimation>();
          animation.clips = new tk2dSpriteAnimationClip[0];
          animator.Library = animation;

          SpriteTrailController trail = newTrailObject.AddComponent<SpriteTrailController>();

          // ---------------- Sets up the animation for the main part of the trail
          if (animPaths != null)
          {
              BeamHelpers.SetupBeamPart(animation, animPaths, "trail_mid", animFPS, null, null, def.colliderVertices);
              trail.animation = "trail_mid";
              trail.usesAnimation = true;
          }
          else
              trail.usesAnimation = false;

          if (startAnimPaths != null)
          {
              BeamHelpers.SetupBeamPart(animation, startAnimPaths, "trail_start", startAnimFPS, null, null, def.colliderVertices);
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
}

// Helper class for making movable / fadeable  VFX
public class FancyVFX : MonoBehaviour
{
    public tk2dSprite sprite;

    private GameObject _vfx           = null;
    private Vector3    _velocity      = Vector3.zero;
    private float      _curLifeTime   = 0.0f;
    private bool       _fadeOut       = false;
    private float      _fadeStartTime = 0.0f;
    private float      _fadeTotalTime = 0.0f;
    private float      _maxLifeTime   = 0.0f;
    private bool       _setup         = false;
    private bool       _fadeIn        = false;

    private void Start()
    {
        // ETGModConsole.Log($"created new fancy vfx {this.GetHashCode()}");
    }

    private void LateUpdate()
    {
        if (!this._setup)
            return;

        if (!this._vfx)
        {
            UnityEngine.Object.Destroy(this);
            return;
        }
        this._curLifeTime += BraveTime.DeltaTime;
        if (this._curLifeTime > this._maxLifeTime)
        {
            if (this._vfx)
                UnityEngine.Object.Destroy(this._vfx);
            UnityEngine.Object.Destroy(this);
            return;
        }

        this.sprite.transform.position += this._velocity;

        if (this._fadeOut && this._curLifeTime > this._fadeStartTime)
        {
            float alpha = (this._curLifeTime - this._fadeStartTime) / this._fadeTotalTime;
            if (!this._fadeIn)
                alpha = 1.0f - alpha;
            // ETGModConsole.Log($"  setting alpha to {}");
            this.sprite.renderer.SetAlpha(alpha);
        }
    }

    // todo: fading and emission are not simultaneously compatible
    public void Setup(Vector2 velocity, float lifetime = 0, float? fadeOutTime = null, Transform parent = null, float emissivePower = 0, Color? emissiveColor = null, bool fadeIn = false)
    {
        this._vfx = base.gameObject;
        this.sprite = this._vfx.GetComponent<tk2dSprite>();
        this._curLifeTime = 0.0f;
        this._fadeIn = fadeIn;

        this._velocity = (1.0f / C.PIXELS_PER_CELL) * velocity.ToVector3ZisY(0);
        this._maxLifeTime = lifetime;
        this._fadeOut = fadeOutTime.HasValue;
        if (this._fadeOut)
        {
            this._fadeTotalTime = fadeOutTime.Value;
            this._fadeStartTime = this._maxLifeTime - this._fadeTotalTime;
        }
        this.transform.parent = parent;

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

        this._setup = true;
    }

    // Make a new FancyVFX from a normal SpawnManager.SpawnVFX
    public static FancyVFX Spawn(GameObject prefab, Vector3 position, Quaternion? rotation = null,
        Vector2? velocity = null, float lifetime = 0, float? fadeOutTime = null, Transform parent = null, float emissivePower = 0, Color? emissiveColor = null, bool fadeIn = false)
    {
        GameObject v = SpawnManager.SpawnVFX(prefab, position, rotation ?? Quaternion.identity, ignoresPools: false);
        FancyVFX fv = v.AddComponent<FancyVFX>();
        fv.Setup(velocity ?? Vector2.zero, lifetime, fadeOutTime, parent, emissivePower, emissiveColor, fadeIn);
        return fv;
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

        GameObject g = UnityEngine.Object.Instantiate(new GameObject(), osprite.WorldCenter, osprite.transform.rotation);
        tk2dSprite sprite = g.AddComponent<tk2dSprite>();
            sprite.SetSprite(osprite.collection, osprite.spriteId);
            sprite.PlaceAtPositionByAnchor(osprite.WorldCenter, Anchor.MiddleCenter);

        FancyVFX fv = g.AddComponent<FancyVFX>();
            fv.sprite = sprite;
        return fv;
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

    public void SetupOrbitals(GameObject vfx, int numOrbitals = 3, float rps = 0.5f, bool isEmissive = false, bool isOverhead = false)
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

        // Spawn orbitals
        for (int i = 0; i < this._numOrbitals; ++i)
            this._orbitals.Add(SpawnManager.SpawnVFX(
                vfx, this._enemy.sprite.WorldCenter.ToVector3ZisY(-1), Quaternion.identity));
        UpdateOrbitals();

        this._didSetup = true;
    }

    public void AddOrbital(GameObject vfx)
    {
        this._orbitals.Add(SpawnManager.SpawnVFX(
            vfx, this._enemy.sprite.WorldCenter.ToVector3ZisY(-1), Quaternion.identity));
        this._numOrbitals += 1;
        this._orbitalGap   = 360.0f / (float)this._numOrbitals;
    }

    private void Update()
    {
        if (!this._didSetup)
            return;

        if (this._enemy?.healthHaver?.IsDead ?? true)
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
            Vector2 avec   = angle.ToVector();
            Vector2 offset = new Vector2(1.5f * this._enemyGirth * avec.x, 0.75f * this._enemyGirth * avec.y + OverheadOffset());
            g.transform.position = (this._enemy.sprite.WorldCenter + offset).ToVector3ZisY(angle < 180 ? z : -z);
            g.transform.rotation = angle.EulerZ();

            if (this._isEmissive)
                sprite.renderer.material.SetFloat("_EmissivePower", power);

            ++i;
        }
    }
}

// Fade in from complete transparency, emit light for a bit, then fade back out
public class GlowAndFadeOut : MonoBehaviour
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
