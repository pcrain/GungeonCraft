namespace CwaffingTheGungy;

/// <summary>Class for setting up sprites from textures packed with cheetah</summary>
public static class AtlasHelper
{
    internal static Mutex _AddSpriteMutex = new(); // adding more than one sprite at once seems to causes issues, so protect it

    internal static Dictionary<string, tk2dSpriteDefinition> _PackedTextures = new();
    private static readonly Vector2 _TexelSize = new Vector2(0.0625f, 0.0625f);

    /// <summary>Batches UI sprite additions from an alternating list of texture paths and sprite names</summary>
    public static void AddUISpriteBatch(List<string> pathsAndNames)
    {
      Assembly assembly = Assembly.GetCallingAssembly();
      List<tk2dSpriteDefinition> defs = new();
      List<string> names = new();
      for (int i = 0; i < pathsAndNames.Count; i += 2)
      {
        defs.Add(AtlasHelper._PackedTextures[pathsAndNames[i]]);
        names.Add(pathsAndNames[i+1]);
      }
      GameUIRoot.Instance.ConversationBar.portraitSprite.Atlas.AddMultipleItemsToAtlas(defs, names);
    }

    /// <summary>
    /// Builds and adds multiple new <see cref="dfAtlas.ItemInfo"/>s to <paramref name="atlas"/> with the textures in <paramref name="defs"/> and the names in <paramref name="names"/>.
    /// </summary>
    /// <param name="atlas">The <see cref="dfAtlas"/> to add the new <see cref="dfAtlas.ItemInfo"/> to.</param>
    /// <param name="defs">List of textures to put in the new <see cref="dfAtlas.ItemInfo"/>.</param>
    /// <param name="name">List of name for the new textures <see cref="dfAtlas.ItemInfo"/>. If a name is <see langword="null"/>, it will default to <paramref name="tex"/>'s name.</param>
    /// <returns>The built <see cref="dfAtlas.ItemInfo"/>.</returns>
    public static List<dfAtlas.ItemInfo> AddMultipleItemsToAtlas(this dfAtlas atlas, List<tk2dSpriteDefinition> defs, List<string> names)
    {
        if (defs.Count != names.Count)
          return null;
        List<dfAtlas.ItemInfo> items = new();
        int totalWidth = 0;
        int maxHeight = 0;
        foreach (tk2dSpriteDefinition def in defs)
        {
          totalWidth += (int)(C.PIXELS_PER_TILE * def.untrimmedBoundsDataExtents.x);
          maxHeight = Mathf.Max(maxHeight, (int)(C.PIXELS_PER_TILE * def.untrimmedBoundsDataExtents.y));
        }
        // Find a region with enough horizontal space to contain all of the next textures side by side
        Rect baseRegion = atlas.FindFirstValidEmptySpace(new IntVector2(totalWidth, maxHeight));
        int cumulativeWidth = 0;
        for (int i = 0; i < defs.Count; ++i)
        {
          tk2dSpriteDefinition def = defs[i];
          string name = names[i];
          if (string.IsNullOrEmpty(name))
          {
              name = def.name;
          }
          if (atlas[name] != null)
          {
              items.Add(atlas[name]);
              continue;
          }
          Texture2D tex   = def.material.mainTexture as Texture2D;
          Vector2 texPos  = new Vector2(tex.width * def.uvs[0].x, tex.height * def.uvs[0].y);
          Vector2 texSize = C.PIXELS_PER_TILE * def.untrimmedBoundsDataExtents;
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
          int startPointX = Mathf.RoundToInt(item.region.x * atlas.Texture.width);
          int startPointY = Mathf.RoundToInt(item.region.y * atlas.Texture.height);
          atlas.Texture.SetPixels(startPointX, startPointY, (int)texSize.x, (int)texSize.y,
            item.texture.GetPixels((int)texPos.x, (int)texPos.y, (int)texSize.x, (int)texSize.y));
          atlas.Texture.Apply();
          atlas.AddItem(item);
        }

        return items;
    }

  /// <summary>Construct a tk2dSpriteDefinition from a segment of a packed texture</summary>
  public static tk2dSpriteDefinition SpriteDefFromSegment(this Texture2D texture, string spriteName, int x, int y, int w, int h, int ox, int oy, int ow, int oh)
  {
    Material material    = new Material(ShaderCache.Acquire(PlayerController.DefaultShaderName));
    material.mainTexture = texture;
    float xmin           = (float) x      / (float)texture.width;
    float xmax           = (float)(x + w) / (float)texture.width;
    float ymin           = 1f - (float)(y + h) / (float)texture.height;
    float ymax           = 1f - (float) y      / (float)texture.height;
    Vector3 offset       = C.PIXEL_SIZE * new Vector3(ox, oh - oy - h, 0f); //NOTE: texture is flipped vertically in memory
    Vector3 extents      = C.PIXEL_SIZE * new Vector3(w, h, 0f);
    Vector3 trueExtents  = C.PIXEL_SIZE * new Vector3(ow, oh, 0f);
    Vector3 trueCenter   = 0.5f * trueExtents;

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
        position1                  = offset + extents.WithY(0f),
        position2                  = offset + extents.WithX(0f),
        position3                  = offset + extents,
        boundsDataExtents          = trueExtents,
        boundsDataCenter           = trueCenter,
        untrimmedBoundsDataExtents = trueExtents,
        untrimmedBoundsDataCenter  = trueCenter,
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

  internal static tk2dSpriteCollectionData _WeaponCollection = ETGMod.Databases.Items.WeaponCollection;
  internal static tk2dSpriteCollectionData _AmmonomiconCollection = AmmonomiconController.ForceInstance.EncounterIconCollection;

  /// <summary>Load a packed texture from a resource string</summary>
  public static void LoadPackedTextureResource(Texture2D atlas, Dictionary<string, tk2dSpriteDefinition.AttachPoint[]> attachPoints, string metaDataResourcePath)
  {
    Assembly asmb = Assembly.GetCallingAssembly();

    List<tk2dSpriteDefinition> projectileSprites = new();
    List<tk2dSpriteDefinition> ammonomiconSprites = new();
    List<tk2dSpriteDefinition> weaponSprites = new();
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
        string collName   = pathName.First();
        string spriteName = pathName.Last().Split('.').First();  // trim off path and extension
        int x = Int32.Parse(tokens[1]);
        int y = Int32.Parse(tokens[2]);
        int w = Int32.Parse(tokens[3]);
        int h = Int32.Parse(tokens[4]);
        int ox = Int32.Parse(tokens[5]);
        int oy = Int32.Parse(tokens[6]);
        int ow = Int32.Parse(tokens[7]);
        int oh = Int32.Parse(tokens[8]);
        tk2dSpriteDefinition def = _PackedTextures[spriteName] = atlas.SpriteDefFromSegment(spriteName, x, y, w, h, ox, oy, ow, oh);

        //NOTE: we don't need to use SafeAddSpriteToCollection() since this is all happening on the main thread
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
      }
    }

    AddSpritesToCollection(newDefs: projectileSprites,  collection: ETGMod.Databases.Items.ProjectileCollection);
    AddSpritesToCollection(newDefs: ammonomiconSprites, collection: _AmmonomiconCollection);
    AddSpritesToCollection(newDefs: weaponSprites,      collection: _WeaponCollection, attachPoints: weaponAttachPoints);
  }

  /// <summary>Helper method for adding multiple sprites to a collection at once</summary>
  private static void AddSpritesToCollection(List<tk2dSpriteDefinition> newDefs, tk2dSpriteCollectionData collection, List<tk2dSpriteDefinition.AttachPoint[]> attachPoints = null)
  {
      if (collection.spriteNameLookupDict == null)
          collection.InitDictionary();

      //Add definition to collection
      int oldLength = collection.spriteDefinitions.Length;
      Array.Resize(ref collection.spriteDefinitions, oldLength + newDefs.Count);
      int i = oldLength;
      foreach (tk2dSpriteDefinition def in newDefs)
      {
        collection.spriteDefinitions[i] = def;
        collection.spriteNameLookupDict[def.name] = i;
        i++;
      }

      // Add attach points if they're available
      if (attachPoints == null)
        return;
      for (int j = 0; j < attachPoints.Count; ++j)
      {
        tk2dSpriteDefinition.AttachPoint[] aps = attachPoints[j];
        if (aps == null)
          continue;
        collection.SetAttachPoints(oldLength + j, aps);
        if (collection.inst != collection)
            collection.inst.SetAttachPoints(oldLength + j, aps);
        // ETGModConsole.Log($"setting attach points for {id} == {spriteName}");
      }
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
  public static string AddCustomAmmoType(string name, string ammoTypeSpritePath, string ammoBackgroundSpritePath)
  {
      tk2dSpriteDefinition fgTexture = _PackedTextures[ammoTypeSpritePath];
      tk2dSpriteDefinition bgTexture = _PackedTextures[ammoBackgroundSpritePath];

      GameObject fgSpriteObject = new GameObject("sprite fg").RegisterPrefab();
      GameObject bgSpriteObject = new GameObject("sprite bg").RegisterPrefab();

      GameUIAmmoType uiammotype = new GameUIAmmoType {
          ammoBarBG      = bgSpriteObject.SetupDfSpriteFromDef<dfTiledSprite>(bgTexture, ShaderCache.Acquire("Daikon Forge/Default UI Shader")),
          ammoBarFG      = fgSpriteObject.SetupDfSpriteFromDef<dfTiledSprite>(fgTexture, ShaderCache.Acquire("Daikon Forge/Default UI Shader")),
          ammoType       = GameUIAmmoType.AmmoType.CUSTOM,
          customAmmoType = name
      };
      Alexandria.ItemAPI.CustomClipAmmoTypeToolbox.addedAmmoTypes.Add(uiammotype);
      foreach (GameUIAmmoController uiammocontroller in GameUIRoot.Instance.ammoControllers)
          Alexandria.ItemAPI.CustomClipAmmoTypeToolbox.Add(ref uiammocontroller.ammoTypes, uiammotype);
      return name;
  }

  /// <summary>Modification of Alexandria method using our own packed textures</summary>
  public static T SetupDfSpriteFromDef<T>(this GameObject obj, tk2dSpriteDefinition def, Shader shader) where T : dfSprite
  {
      T sprite = obj.GetOrAddComponent<T>();
      dfAtlas atlas = obj.GetOrAddComponent<dfAtlas>();
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
      sprite.Atlas = atlas;
      sprite.SpriteName = "main_sprite";
      return sprite;
  }

  /// <summary>Thread-safe wrapper around SpriteBuilder.AddSpriteToCollection()</summary>
  public static int SafeAddSpriteToCollection(string resourcePath, tk2dSpriteCollectionData collection)
  {
    _AddSpriteMutex.WaitOne();
    int result = SpriteBuilder.AddSpriteToCollection(resourcePath, collection);
    _AddSpriteMutex.ReleaseMutex();
    return result;
  }

  /// <summary>Thread-safe wrapper around SpriteBuilder.AddSpriteToCollection()</summary>
  public static int SafeAddSpriteToCollection(tk2dSpriteDefinition def, tk2dSpriteCollectionData collection)
  {
    _AddSpriteMutex.WaitOne();
    int result = SpriteBuilder.AddSpriteToCollection(def, collection);
    _AddSpriteMutex.ReleaseMutex();
    return result;
  }

  /// <summary>Thread-safe wrapper around SpriteBuilder.AddToAmmonomicon()</summary>
  public static int SafeAddToAmmonomicon(tk2dSpriteDefinition spriteDefinition, string prefix = "")
  {
    _AddSpriteMutex.WaitOne();
    int result = SpriteBuilder.AddToAmmonomicon(spriteDefinition, prefix);
    _AddSpriteMutex.ReleaseMutex();
    return result;
  }

  internal static tk2dSpriteCollectionData itemCollection = PickupObjectDatabase.GetById(155).sprite.Collection;

  /// <summary>Manually initialize some Harmony patches we need very early on to enable threaded setup</summary>
  public static void InitSetupPatches(Harmony harmony)
  {
      BindingFlags anyFlags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

      // Safe shared resource access
      MethodInfo threadSafePrefix = typeof(AtlasHelper.ThreadSafeUnityStuffPatch).GetMethod(
        "Prefix", bindingAttr: BindingFlags.Static | BindingFlags.Public);
      MethodInfo threadSafePostfix = typeof(AtlasHelper.ThreadSafeUnityStuffPatch).GetMethod(
        "Postfix", bindingAttr: BindingFlags.Static | BindingFlags.Public);
      harmony.Patch(typeof(tk2dSpriteAnimation).GetMethod("GetClipByName", bindingAttr: anyFlags),
        prefix: new HarmonyMethod(threadSafePrefix), postfix:  new HarmonyMethod(threadSafePostfix));
      harmony.Patch(typeof(tk2dSpriteAnimation).GetMethod("GetClipById", bindingAttr: anyFlags),
        prefix: new HarmonyMethod(threadSafePrefix), postfix:  new HarmonyMethod(threadSafePostfix));
      harmony.Patch(typeof(tk2dSpriteAnimation).GetMethod("GetClipIdByName", types: new[]{typeof(string)}),
        prefix: new HarmonyMethod(threadSafePrefix), postfix:  new HarmonyMethod(threadSafePostfix));
      harmony.Patch(typeof(tk2dSpriteAnimation).GetMethod("GetClipIdByName", types: new[]{typeof(tk2dSpriteAnimationClip)}),
        prefix: new HarmonyMethod(threadSafePrefix), postfix:  new HarmonyMethod(threadSafePostfix));
      harmony.Patch(typeof(GameObject).GetMethod("SetActive", bindingAttr: anyFlags),
        prefix: new HarmonyMethod(threadSafePrefix), postfix:  new HarmonyMethod(threadSafePostfix));
      harmony.Patch(typeof(GunExt).GetMethod("UpdateAnimation", bindingAttr: anyFlags),
        prefix: new HarmonyMethod(threadSafePrefix), postfix:  new HarmonyMethod(threadSafePostfix));
      harmony.Patch(typeof(EnemyDatabase).GetMethod("GetOrLoadByGuid", bindingAttr: anyFlags),
        prefix: new HarmonyMethod(threadSafePrefix), postfix:  new HarmonyMethod(threadSafePostfix));

      // Load sprites from our own atlases
      harmony.Patch(typeof(SpriteBuilder).GetMethod("SpriteFromResource", bindingAttr: anyFlags),
        prefix: new HarmonyMethod(typeof(SpriteFromResourcePatch).GetMethod("Prefix", bindingAttr: anyFlags)));

      // Add sprites to collections from our own atlases
      harmony.Patch(typeof(SpriteBuilder).GetMethod("AddSpriteToCollection", types: new[]{typeof(string), typeof(tk2dSpriteCollectionData), typeof(Assembly)}),
        prefix: new HarmonyMethod(typeof(AddSpriteToCollectionPatch).GetMethod("Prefix", bindingAttr: anyFlags)));
  }

  /// <summary>Patched version of Alexandria's SpriteFromResource (manually added through InitSetupPatches())</summary>
  private class SpriteFromResourcePatch
  {
    public static bool Prefix(string spriteName, GameObject obj, Assembly assembly, ref GameObject __result)
    {
        if (C._ModSetupFinished)
          return true; // call original method

        // System.Console.WriteLine($"CALLING PATCHED SpriteFromResource for {spriteName}");
        if (obj == null)
          obj = new GameObject();

        tk2dSprite sprite;
        sprite = obj.AddComponent<tk2dSprite>();

        int id = AtlasHelper.SafeAddSpriteToCollection(AtlasHelper.NamedSpriteInPackedTexture(spriteName), itemCollection);
        sprite.SetSprite(itemCollection, id);
        sprite.SortingOrder = 0;
        sprite.IsPerpendicular = true;

        obj.GetComponent<BraveBehaviour>().sprite = sprite;

        __result = obj;
        return false; // skip original method
    }
  }

  /// <summary>Patched version of Alexandria's AddSpriteToCollection(string, ...) (manually added through InitSetupPatches())</summary>
  private class AddSpriteToCollectionPatch
  {
    public static bool Prefix(string resourcePath, tk2dSpriteCollectionData collection, /*string name, */Assembly assembly, ref int __result)
    {
        if (C._ModSetupFinished)
          return true; // call original method

        // ETGModConsole.Log($"CALLING PATCHED AddSpriteToCollection for {resourcePath}");
        __result = AtlasHelper.SafeAddSpriteToCollection(AtlasHelper.NamedSpriteInPackedTexture(resourcePath), collection);
        return false; // skip original method
    }
  }

  /// <summary>Patched, thread-safe versions of various sensitive functions (manually added through InitSetupPatches())</summary>
  private class ThreadSafeUnityStuffPatch
  {
    public static void Prefix()
    {
        if (!C._ModSetupFinished)
          _AddSpriteMutex.WaitOne();
    }
    public static void Postfix()
    {
        if (!C._ModSetupFinished)
          _AddSpriteMutex.ReleaseMutex();
    }
  }
}