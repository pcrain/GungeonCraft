namespace CwaffingTheGungy;

/// <summary>Class for setting up sprites from textures packed with cheetah</summary>
internal static class AtlasHelper
{
  private static readonly Vector2 _TexelSize = new Vector2(0.0625f, 0.0625f);
  private static readonly List<TempHarmonyPrefix> _TemporaryPatches = new();
  private static readonly HashSet<string> _CreatedAmmoTypes = new();

  internal static Dictionary<string, tk2dSpriteDefinition> _PackedTextures = new();

  /// <summary>Batches UI sprite additions from a list of sprite definitions</summary>
  public static void AddUISpriteBatch(List<tk2dSpriteDefinition> defs)
  {
    foreach (tk2dSpriteDefinition def in defs)
      ToolsCharApi.AddUISprite(def);
  }

  /// <summary>
  /// Builds and adds multiple new <see cref="dfAtlas.ItemInfo"/>s to <paramref name="atlas"/> with the textures in <paramref name="defs"/> and the names in <paramref name="names"/>.
  /// </summary>
  /// <param name="atlas">The <see cref="dfAtlas"/> to add the new <see cref="dfAtlas.ItemInfo"/> to.</param>
  /// <param name="defs">List of textures to put in the new <see cref="dfAtlas.ItemInfo"/>.</param>
  /// <returns>The built <see cref="dfAtlas.ItemInfo"/>.</returns>
  [ObsoleteAttribute("This method is obsolete and exists for future API reference only.", false)]
  private static List<dfAtlas.ItemInfo> AddMultipleItemsToAtlas(this dfAtlas atlas, List<tk2dSpriteDefinition> defs)
  {
      List<dfAtlas.ItemInfo> items = new();
      int totalWidth = 0;
      int maxHeight = 0;
      foreach (tk2dSpriteDefinition def in defs)
      {
        totalWidth += (int)(C.PIXELS_PER_TILE * def.untrimmedBoundsDataExtents.x);
        maxHeight = Mathf.Max(maxHeight, (int)(C.PIXELS_PER_TILE * def.untrimmedBoundsDataExtents.y));
      }
      // Find a region with enough horizontal space to contain all of the next textures side by side
      Rect baseRegion = ToolsCharApi.FindFirstValidEmptySpace(atlas, new IntVector2(totalWidth, maxHeight));
      int cumulativeWidth = 0;
      for (int i = 0; i < defs.Count; ++i)
      {
        tk2dSpriteDefinition def = defs[i];
        string name = def.name;
        if (atlas[name] != null)
        {
            items.Add(atlas[name]);
            continue;
        }
        Texture2D tex   = def.material.mainTexture as Texture2D;
        IntVector2 texOffset = (C.PIXELS_PER_TILE * def.position0.XY()).ToIntVector2();
        Vector2 texPos  = new Vector2(tex.width * def.uvs[0].x, tex.height * def.uvs[0].y);
        Vector2 texSize = C.PIXELS_PER_TILE * def.untrimmedBoundsDataExtents;
        Vector2 croppedTexSize = new Vector2(tex.width * def.uvs[3].x, tex.height * def.uvs[3].y) - texPos;
        dfAtlas.ItemInfo item = new dfAtlas.ItemInfo
        {
            border = new RectOffset(),
            deleted = false,
            name = name,
            region = new Rect(
              (float)baseRegion.x + ((float)cumulativeWidth / atlas.Texture.width),
              (float)baseRegion.y,
              (float)texSize.x / atlas.Texture.width,
              (float)texSize.y / atlas.Texture.height),
            rotated = false,
            sizeInPixels = texSize,
            texture = def.material.mainTexture as Texture2D,
            textureGUID = name
        };
        cumulativeWidth += (int)texSize.x;
        int startPointX = texOffset.x + Mathf.RoundToInt(item.region.x * atlas.Texture.width);
        int startPointY = texOffset.y + Mathf.RoundToInt(item.region.y * atlas.Texture.height);
        atlas.Texture.SetPixels(startPointX, startPointY, (int)croppedTexSize.x, (int)croppedTexSize.y,
          item.texture.GetPixels((int)texPos.x, (int)texPos.y, (int)croppedTexSize.x, (int)croppedTexSize.y));
        atlas.AddItem(item);
      }
      atlas.Texture.Apply();

      return items;
  }

  /// <summary>Construct a tk2dSpriteDefinition from a segment of a packed texture</summary>
  public static tk2dSpriteDefinition SpriteDefFromSegment(this Texture2D texture, string spriteName, int x, int y, int w, int h, int ox, int oy, int ow, int oh)
  {
    Material material    = new Material(ShaderCache.Acquire(PlayerController.DefaultShaderName));
    material.mainTexture = texture;
    float xmin           =      (float) x      / (float)texture.width;
    float xmax           =      (float)(x + w) / (float)texture.width;
    float ymin           = 1f - (float)(y + h) / (float)texture.height;
    float ymax           = 1f - (float) y      / (float)texture.height;
    // NOTE: POSITIVE y is up on the screen but NEGATIVE y is up in atlas
    Vector3 offset       = C.PIXEL_SIZE * new Vector3(ox, oh - h - oy, 0f); //NOTE: texture is flipped vertically in memory
    Vector3 extents      = C.PIXEL_SIZE * new Vector3(w, h, 0f);
    Vector3 trueExtents  = C.PIXEL_SIZE * new Vector3(ow, oh, 0f);

    tk2dSpriteDefinition def = new tk2dSpriteDefinition
    {
        name                       = spriteName,
        texelSize                  = _TexelSize,
        flipped                    = tk2dSpriteDefinition.FlipMode.None,
        physicsEngine              = tk2dSpriteDefinition.PhysicsEngine.Physics3D,
        colliderType               = tk2dSpriteDefinition.ColliderType.None,
        collisionLayer             = CollisionLayer.HighObstacle,
        material                   = material,
        materialInst               = material,
        position0                  = offset,
        position1                  = offset + new Vector3(extents.x, 0f, 0f),
        position2                  = offset + new Vector3(0f, extents.y, 0f),
        position3                  = offset + extents,
        boundsDataExtents          = extents,
        boundsDataCenter           = offset + 0.5f * extents,
        untrimmedBoundsDataExtents = trueExtents,
        untrimmedBoundsDataCenter  = 0.5f * trueExtents,
        uvs = new Vector2[]
        { //NOTE: texture is flipped vertically in memory
          new Vector2(xmin, ymin),
          new Vector2(xmax, ymin),
          new Vector2(xmin, ymax),
          new Vector2(xmax, ymax),
        },
    };
    return def;
  }

  /// <summary>Retrieve a tk2dSprite by name</summary>
  public static tk2dSpriteDefinition NamedSpriteInPackedTexture(string s)
  {
    return _PackedTextures.TryGetValue(s.Split('/').Last(), out tk2dSpriteDefinition value) ? value : null;
  }

  /// <summary>Load a packed texture from a resource string</summary>
  public static void LoadPackedTextureResource(Texture2D atlas, Dictionary<string, tk2dSpriteDefinition.AttachPoint[]> attachPoints, string metaDataResourcePath)
  {
    Assembly asmb = Assembly.GetCallingAssembly();

    List<tk2dSpriteDefinition> projectileSprites                = new();
    List<tk2dSpriteDefinition> ammonomiconSprites               = new();
    List<tk2dSpriteDefinition> itemSprites                      = new();
    List<tk2dSpriteDefinition> weaponSprites                    = new();
    List<tk2dSpriteDefinition> uiSprites                        = new();
    List<tk2dSpriteDefinition> characterSprites                 = new();
    List<tk2dSpriteDefinition> punchoutSprites                  = new();
    List<tk2dSpriteDefinition> miscSprites                      = new();
    List<tk2dSpriteDefinition.AttachPoint[]> weaponAttachPoints = new();

    using (Stream stream = asmb.GetManifestResourceStream(metaDataResourcePath))
    using (StreamReader reader = new StreamReader(stream))
    {
      string line = null;
      while ((line = reader.ReadLine()) != null)
      {
        string[] tokens = line.Split('\t');
        if (tokens.Length < 9)
          continue; // first line, skip it since it doesn't have relevant information
        string[] pathName = tokens[0].Split('/'); // trim off path and extension
        string collName   = pathName[0];
        string spriteName = pathName[pathName.Length - 1].Split('.')[0];  // trim off path and extension
        tk2dSpriteDefinition def = _PackedTextures[spriteName] = atlas.SpriteDefFromSegment(
            spriteName : spriteName,
            x          : Int32.Parse(tokens[1]),
            y          : Int32.Parse(tokens[2]),
            w          : Int32.Parse(tokens[3]),
            h          : Int32.Parse(tokens[4]),
            ox         : Int32.Parse(tokens[5]),
            oy         : Int32.Parse(tokens[6]),
            ow         : Int32.Parse(tokens[7]),
            oh         : Int32.Parse(tokens[8]));

        if (collName == "ProjectileCollection")
        {
          projectileSprites.Add(def);
          continue;
        }

        if (collName == "Ammonomicon Encounter Icon Collection")
        {
          ammonomiconSprites.Add(def);
          continue;
        }

        if (collName == "ItemSprites")
        {
          itemSprites.Add(def);
          ammonomiconSprites.Add(def); //NOTE: all items also need to be added to the Ammonomicon
          continue;
        }

        if (collName == "UISprites")
        {
          uiSprites.Add(def);
          continue;
        }

        if (collName == "Characters")
        {
          characterSprites.Add(def);
          continue;
        }

        if (collName == "Punchout")
        {
          punchoutSprites.Add(def);
          continue;
        }

        // everything from here onward only applies to weapon collection
        if (collName == "WeaponCollection")
        {
          weaponSprites.Add(def);
          if (attachPoints.TryGetValue(spriteName, out tk2dSpriteDefinition.AttachPoint[] aps))
            weaponAttachPoints.Add(aps);
          else
            weaponAttachPoints.Add(null);
          continue;
        }

        miscSprites.Add(def);
      }
    }

    tk2dSpriteCollectionData punchoutCollection = ResourceManager.LoadAssetBundle("enemies_base_001")
      .LoadAsset<GameObject>("MetalGearRat").GetComponent<AIActor>().GetComponent<MetalGearRatDeathController>()
      .PunchoutMinigamePrefab.GetComponent<PunchoutController>().Player.gameObject.GetComponent<tk2dSpriteAnimator>().Library
      .clips[0].frames[0].spriteCollection;

    AddSpritesToCollection(newDefs: projectileSprites,  collection: ETGMod.Databases.Items.ProjectileCollection);
    AddSpritesToCollection(newDefs: ammonomiconSprites, collection: AmmonomiconController.ForceInstance.EncounterIconCollection);
    AddSpritesToCollection(newDefs: itemSprites,        collection: ETGMod.Databases.Items.ItemCollection);
    AddSpritesToCollection(newDefs: weaponSprites,      collection: ETGMod.Databases.Items.WeaponCollection, attachPoints: weaponAttachPoints);
    AddSpritesToCollection(newDefs: characterSprites,   collection: CwaffCharacter.Collection);
    AddSpritesToCollection(newDefs: punchoutSprites,    collection: punchoutCollection);
    AddSpritesToCollection(newDefs: miscSprites,        collection: VFX.Collection); // NOTE: all miscellaneous sprites go into the VFX collection
    AddSpritesToCollection(newDefs: uiSprites,          collection: ((GameObject)ResourceCache.Acquire("ControllerButtonSprite")).GetComponent<tk2dBaseSprite>().Collection);
    AddUISpriteBatch(uiSprites);
  }

  /// <summary>Helper method for adding multiple sprites to a collection at once. Returns the id of the first sprite added and the number of sprites added.</summary>
  private static IntVector2 AddSpritesToCollection(List<tk2dSpriteDefinition> newDefs, tk2dSpriteCollectionData collection,
    List<tk2dSpriteDefinition.AttachPoint[]> attachPoints = null, bool copyMaterialSettings = false)
  {
      if (collection.spriteNameLookupDict == null)
          collection.InitDictionary();

      Material baseMat  = null;
      Shader baseShader = null;
      if (copyMaterialSettings && collection.spriteDefinitions.Length > 0)
      {
        baseMat = collection.spriteDefinitions[0].material;
        baseShader = baseMat.shader;
      }

      //Add definition to collection
      int oldLength = collection.spriteDefinitions.Length;
      int n = newDefs.Count;
      Array.Resize(ref collection.spriteDefinitions, oldLength + n);
      for (int i = 0; i < n; ++i)
      {
        tk2dSpriteDefinition def = newDefs[i];
        if (baseMat)
          def.CopyMaterialProps(baseMat);
        int newPos = oldLength + i;
        collection.spriteDefinitions[newPos] = def;
        collection.spriteNameLookupDict[def.name] = newPos;
      }
      if (attachPoints == null)
        return new IntVector2(oldLength, newDefs.Count);

      // Add attach points if they're available
      for (int j = 0; j < attachPoints.Count; ++j)
      {
        tk2dSpriteDefinition.AttachPoint[] aps = attachPoints[j];
        if (aps == null)
          continue;
        int spriteId = oldLength + j;
        collection.SpriteIDsWithAttachPoints.Add(spriteId);
        collection.SpriteDefinedAttachPoints.Add(new AttachPointData(aps));
        if (collection.inst != collection)
        {
            collection.inst.SpriteIDsWithAttachPoints.Add(spriteId);
            collection.inst.SpriteDefinedAttachPoints.Add(new AttachPointData(aps));
        }
        // ETGModConsole.Log($"setting attach points for {id} == {spriteName}");
      }

      return new IntVector2(oldLength, n);
  }

  /// <summary>Helper method for adding multiple sprites to a collection at once (public path-based version). Returns the id of the first sprite added and the number of sprites added.</summary>
  public static IntVector2 AddSpritesToCollection(List<string> paths, tk2dSpriteCollectionData collection, bool copyMaterialSettings = false)
  {
    int n = paths.Count;
    List<tk2dSpriteDefinition> defs = new(n);
    for (int i = 0; i < n; ++i)
      defs.Add(_PackedTextures[paths[i]]);
    return AddSpritesToCollection(defs, collection, copyMaterialSettings: copyMaterialSettings);
  }

  public static Dictionary<string, tk2dSpriteDefinition.AttachPoint[]> ReadAttachPointsFromTSV(Assembly asmb, string tsvPath)
  {
    Dictionary<string, tk2dSpriteDefinition.AttachPoint[]> attachPointDict = new();

    using (Stream stream = asmb.GetManifestResourceStream(tsvPath))
    using (StreamReader reader = new StreamReader(stream))
    {
      string line = null;
      while ((line = reader.ReadLine()) != null)
      {
        string[] tokens = line.Split('\t');
        tk2dSpriteDefinition.AttachPoint[] aps = attachPointDict[tokens[0]] = new tk2dSpriteDefinition.AttachPoint[(tokens.Length - 1) / 3];
        int index = 0;
        for (int i = 1; i < tokens.Length; i += 3)
        {
          aps[index] = new tk2dSpriteDefinition.AttachPoint(){
            name = tokens[i],
            position = new Vector3(float.Parse(tokens[i+1]), float.Parse(tokens[i+2]), 0f)
          };
          ++index;
        }
      }
    }

    return attachPointDict;
  }

  /// <summary>Modification of Alexandria method using our own packed textures</summary>
  public static string GetOrAddCustomAmmoType(string name, string ammoTypeSpritePath, string ammoBackgroundSpritePath)
  {
      if (_CreatedAmmoTypes.Contains(name))
        return name;
      GameUIAmmoType uiammotype = new GameUIAmmoType {
          ammoBarBG      = new GameObject("sprite bg").RegisterPrefab().SetupDfSpriteFromDef(
            _PackedTextures[ammoBackgroundSpritePath], ShaderCache.Acquire("Daikon Forge/Default UI Shader")),
          ammoBarFG      = new GameObject("sprite fg").RegisterPrefab().SetupDfSpriteFromDef(
            _PackedTextures[ammoTypeSpritePath], ShaderCache.Acquire("Daikon Forge/Default UI Shader")),
          ammoType       = GameUIAmmoType.AmmoType.CUSTOM,
          customAmmoType = name
      };
      Alexandria.ItemAPI.CustomClipAmmoTypeToolbox.addedAmmoTypes.Add(uiammotype);
      foreach (GameUIAmmoController uiammocontroller in GameUIRoot.Instance.ammoControllers)
          Alexandria.ItemAPI.CustomClipAmmoTypeToolbox.Add(ref uiammocontroller.ammoTypes, uiammotype);
      _CreatedAmmoTypes.Add(name);
      return name;
  }

  /// <summary>Modification of Alexandria method using our own packed textures</summary>
  public static dfTiledSprite SetupDfSpriteFromDef(this GameObject obj, tk2dSpriteDefinition def, Shader shader)
  {
      dfAtlas atlas = obj.AddComponent<dfAtlas>();
      atlas.Material = new Material(shader);
      atlas.Material.mainTexture = def.material.mainTexture;
      atlas.Items.Clear();
      dfAtlas.ItemInfo info = new dfAtlas.ItemInfo
      {
          border       = new RectOffset(),
          deleted      = false,
          name         = "main_sprite",
          region       = new Rect(def.uvs[0], def.uvs[3] - def.uvs[0]),
          rotated      = false,
          sizeInPixels = (C.PIXELS_PER_TILE * def.untrimmedBoundsDataExtents.XY()),
          texture      = null,
          textureGUID  = "main_sprite"
      };
      atlas.AddItem(info);
      dfTiledSprite sprite = obj.AddComponent<dfTiledSprite>();
      sprite.Atlas = atlas;
      sprite.SpriteName = "main_sprite";
      return sprite;
  }

  private class TempHarmonyPrefix
  {
    private Harmony _harmony;
    public MethodInfo original;
    public MethodInfo patch;
    public TempHarmonyPrefix(Harmony harmony, MethodInfo original, MethodInfo patch)
    {
      this._harmony = harmony;
      this.original = original;
      this.patch    = patch;
      harmony.Patch(original, prefix: new HarmonyMethod(patch));
    }
    public void Unpatch()
    {
      this._harmony.Unpatch(this.original, this.patch);
    }
  }

  /// <summary>Manually initialize some Harmony patches we need very early on to enable threaded setup</summary>
  internal static void InitSetupPatches(Harmony harmony)
  {
      BindingFlags anyFlags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

      // Load sprites from our own atlases
      _TemporaryPatches.Add(new(harmony,
        typeof(SpriteBuilder).GetMethod("SpriteFromResource", bindingAttr: anyFlags),
        typeof(SpriteFromResourcePatch).GetMethod("Prefix", bindingAttr: anyFlags)));

      // Add sprites to collections from our own atlases
      _TemporaryPatches.Add(new(harmony,
        typeof(SpriteBuilder).GetMethod("AddSpriteToCollection", types: new[]{typeof(string), typeof(tk2dSpriteCollectionData), typeof(Assembly)}),
        typeof(AddSpriteToCollectionPatch).GetMethod("Prefix", bindingAttr: anyFlags)));

      // Prevent MTG API's sprite replacement stuff from triggering overzealously
      Assembly MtGAssembly = typeof(GunBehaviour).Assembly;
      _TemporaryPatches.Add(new(harmony,
        MtGAssembly.GetType("HarmonyPatches").GetMethod("AddMissingReplacements", bindingAttr: anyFlags, binder: null, callConvention: CallingConventions.Any, types: new[]{typeof(tk2dSpriteCollectionData)}, modifiers: null),
        typeof(AtlasHelper).GetMethod("False", bindingAttr: anyFlags)));
  }

  private static bool False() => false; // skip method unconditionally

  internal static void RemoveSetupPatches(Harmony harmony)
  {
    for (int i = _TemporaryPatches.Count - 1; i >= 0; --i)
      _TemporaryPatches[i].Unpatch();
    _TemporaryPatches.Clear();
    // Lazy.DebugLog("  Temporary setup patches removed");
  }

  // NOTE: used by BossBuilder.BuildPrefab() and CompanionBuilder.BuildPrefab()
  /// <summary>Patched version of Alexandria's SpriteFromResource (manually added through InitSetupPatches())</summary>
  private class SpriteFromResourcePatch
  {
    private static tk2dSpriteCollectionData itemCollection = null;

    public static bool Prefix(string spriteName, GameObject obj, Assembly assembly, ref GameObject __result)
    {
        // System.Console.WriteLine($"CALLING PATCHED SpriteFromResource for {spriteName}");
        if (obj == null)
          obj = new GameObject();

        tk2dSprite sprite = obj.AddComponent<tk2dSprite>();

        itemCollection ??= PickupObjectDatabase.GetById(155).sprite.Collection;
        int id = SpriteBuilder.AddSpriteToCollection(AtlasHelper.NamedSpriteInPackedTexture(spriteName), itemCollection);
        sprite.SetSprite(itemCollection, id);
        sprite.SortingOrder = 0;
        sprite.IsPerpendicular = true;

        obj.GetComponent<BraveBehaviour>().sprite = sprite;  //TODO: probably completely unnecessary

        __result = obj;
        return false; // skip original method
    }
  }

  // NOTE: used extensively for hat setup
  /// <summary>Patched version of Alexandria's AddSpriteToCollection(string, ...) (manually added through InitSetupPatches())</summary>
  private class AddSpriteToCollectionPatch
  {
    public static bool Prefix(string resourcePath, tk2dSpriteCollectionData collection, /*string name, */Assembly assembly, ref int __result)
    {
        // System.Console.WriteLine($"CALLING PATCHED AddSpriteToCollection for {resourcePath}");
        __result = SpriteBuilder.AddSpriteToCollection(AtlasHelper.NamedSpriteInPackedTexture(resourcePath), collection);
        return false; // skip original method
    }
  }
}
