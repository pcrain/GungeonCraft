namespace CwaffingTheGungy;

public static class BetterAtlas
{
    public static void AddUISpriteBatch(List<string> pathsAndNames)
    {
      Assembly assembly = Assembly.GetCallingAssembly();
      List<Texture2D> textures = new();
      List<string> names = new();
      for (int i = 0; i < pathsAndNames.Count; i += 2)
      {
        textures.Add(ResourceExtractor.GetTextureFromResource(pathsAndNames[i], assembly));
        names.Add(pathsAndNames[i+1]);
      }
      GameUIRoot.Instance.ConversationBar.portraitSprite.Atlas.AddMultipleItemsToAtlas(textures, names);
    }

    /// <summary>
    /// Builds and adds multiple new <see cref="dfAtlas.ItemInfo"/>s to <paramref name="atlas"/> with the textures in <paramref name="texes"/> and the names in <paramref name="names"/>.
    /// </summary>
    /// <param name="atlas">The <see cref="dfAtlas"/> to add the new <see cref="dfAtlas.ItemInfo"/> to.</param>
    /// <param name="texes">List of textures to put in the new <see cref="dfAtlas.ItemInfo"/>.</param>
    /// <param name="name">List of name for the new textures <see cref="dfAtlas.ItemInfo"/>. If a name is <see langword="null"/>, it will default to <paramref name="tex"/>'s name.</param>
    /// <returns>The built <see cref="dfAtlas.ItemInfo"/>.</returns>
    public static List<dfAtlas.ItemInfo> AddMultipleItemsToAtlas(this dfAtlas atlas, List<Texture2D> texes, List<string> names)
    {
        if (texes.Count != names.Count)
          return null;
        List<dfAtlas.ItemInfo> items = new();
        int totalWidth = 0;
        int maxHeight = 0;
        foreach (Texture2D tex in texes)
        {
          totalWidth += tex.width;
          maxHeight = Mathf.Max(maxHeight, tex.height);
        }
        // Find a region with enough horizontal space to contain all of the next textures side by side
        Rect baseRegion = atlas.FindFirstValidEmptySpace(new IntVector2(totalWidth, maxHeight));
        int cumulativeWidth = 0;
        for (int i = 0; i < texes.Count; ++i)
        {
          Texture2D tex = texes[i];
          string name = names[i];
          if (string.IsNullOrEmpty(name))
          {
              name = tex.name;
          }
          if (atlas[name] != null)
          {
              items.Add(atlas[name]);
              continue;
          }
          dfAtlas.ItemInfo item = new dfAtlas.ItemInfo
          {
              border = new RectOffset(),
              deleted = false,
              name = name,
              region = new Rect(
                (float)baseRegion.x + ((float)cumulativeWidth / atlas.Texture.width),
                (float)baseRegion.y,
                (float)tex.width / atlas.Texture.width,
                (float)tex.height / atlas.Texture.height),
              rotated = false,
              sizeInPixels = new Vector2(tex.width, tex.height),
              texture = tex,
              textureGUID = name
          };
          cumulativeWidth += tex.width;
          int startPointX = Mathf.RoundToInt(item.region.x * atlas.Texture.width);
          int startPointY = Mathf.RoundToInt(item.region.y * atlas.Texture.height);
          atlas.Texture.SetPixels(startPointX, startPointY, tex.width, tex.height, tex.GetPixels());
          atlas.Texture.Apply();
          atlas.AddItem(item);
        }

        return items;
    }
}

/// <summary>Class for setting up sprites from textures packed with cheetah</summary>
public static class PackerHelper
{
  private class SpriteInfo
  {
    public Texture2D atlas = null;
    public int x           = 0;
    public int y           = 0;
    public int w           = 0;
    public int h           = 0;
  }

  internal static Dictionary<string, tk2dSpriteDefinition> _PackedTextures = new();

  /// <summary>Construct a tk2dSpriteDefinition from a segment of a packed texture</summary>
  public static tk2dSpriteDefinition SpriteDefFromSegment(this Texture2D texture, string spriteName, int x, int y, int w, int h)
  {
    Material material = new Material(ShaderCache.Acquire(PlayerController.DefaultShaderName));
    material.mainTexture = texture;
    float xx = 0f;
    float yy = 0f;
    float ww = (float)w / 16f;
    float hh = (float)h / 16f;
    tk2dSpriteDefinition def = new tk2dSpriteDefinition
    {
        name = spriteName,
        normals = new Vector3[]
        {
          new Vector3(0f, 0f, -1f),
          new Vector3(0f, 0f, -1f),
          new Vector3(0f, 0f, -1f),
          new Vector3(0f, 0f, -1f)
        },
        tangents = new Vector4[]
        {
          new Vector4(1f, 0f, 0f, 1f),
          new Vector4(1f, 0f, 0f, 1f),
          new Vector4(1f, 0f, 0f, 1f),
          new Vector4(1f, 0f, 0f, 1f)
        },
        texelSize = new Vector2(0.0625f, 0.0625f),
        extractRegion = false,
        regionX = 0,
        regionY = 0,
        regionW = 0,
        regionH = 0,
        flipped = tk2dSpriteDefinition.FlipMode.None,
        complexGeometry = false,
        physicsEngine = tk2dSpriteDefinition.PhysicsEngine.Physics3D,
        colliderType = tk2dSpriteDefinition.ColliderType.Box,
        collisionLayer = CollisionLayer.HighObstacle,
        position0 = new Vector3(xx,      yy,      0f),
        position1 = new Vector3(xx + ww, yy,      0f),
        position2 = new Vector3(xx,      yy + hh, 0f),
        position3 = new Vector3(xx + ww, yy + hh, 0f),
        material = material,
        materialInst = material,
        materialId = 0,
        uvs = new Vector2[]
        {  // texture is flipped vertically in memory
          new Vector2((float) x      / (float)texture.width, 1f - (float)(y + h) / (float)texture.height),
          new Vector2((float)(x + w) / (float)texture.width, 1f - (float)(y + h) / (float)texture.height),
          new Vector2((float) x      / (float)texture.width, 1f - (float) y      / (float)texture.height),
          new Vector2((float)(x + w) / (float)texture.width, 1f - (float) y      / (float)texture.height),
        },
        boundsDataCenter           = new Vector3(ww / 2f, hh / 2f, 0f),
        boundsDataExtents          = new Vector3(ww,      hh, 0f),
        untrimmedBoundsDataCenter  = new Vector3(ww / 2f, hh / 2f, 0f),
        untrimmedBoundsDataExtents = new Vector3(ww,      hh, 0f)
    };
    return def;
  }

  /// <summary>Retrieve a tk2dSprite by name</summary>
  public static tk2dSpriteDefinition NamedSpriteInPackedTexture(string s)
  {
    return _PackedTextures.TryGetValue(s.Split('/').Last(), out tk2dSpriteDefinition value) ? value : null;
  }

  // internal static tk2dSpriteCollectionData _WeaponCollection = ItemHelper.Get(Items.Ak47).sprite.Collection;
  internal static tk2dSpriteCollectionData _WeaponCollection = ETGMod.Databases.Items.WeaponCollection;
  internal static tk2dSpriteCollectionData _AmmonomiconCollection = AmmonomiconController.ForceInstance.EncounterIconCollection;

  /// <summary>Load a packed texture from a resource string</summary>
  public static void LoadPackedTextureResource(string textureResourcePath, string metaDataResourcePath)
  {
    Assembly asmb = Assembly.GetCallingAssembly();
    Texture2D atlas = ResourceExtractor.GetTextureFromResource(textureResourcePath, asmb);
    if (atlas == null)
      return;
    if (C.DEBUG_BUILD)
      ETGModConsole.Log($"extracted texture {textureResourcePath}");
    if (atlas.width != 1024 || atlas.height != 1024)
      ETGModConsole.Log($"D:D:D:");
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
        // _PackedTextures[spriteName] = new SpriteInfo{atlas = atlas, x = x, y = y, w = w, h = h};
        tk2dSpriteDefinition def = _PackedTextures[spriteName] = atlas.SpriteDefFromSegment(spriteName, x, y, w, h);
        // ETGModConsole.Log($"loaded packed {w}x{h} sprite {spriteName} at {x},{y}");

        if (collName == "ProjectileCollection")
        {
          SpriteBuilder.AddSpriteToCollection(def, ETGMod.Databases.Items.ProjectileCollection);
          continue;
        }

        if (collName == "Ammonomicon Encounter Icon Collection")
        {
          SpriteBuilder.AddSpriteToCollection(def, _AmmonomiconCollection);
          continue;
        }

        // everything from here onward only applies to weapon collection
        if (collName != "WeaponCollection")
          continue;

        int id = SpriteBuilder.AddSpriteToCollection(def, _WeaponCollection);
        // ETGModConsole.Log($"added {spriteName} to weapons");
        string json = $"CwaffingTheGungy.Resources.{collName}.{spriteName}.json";

        using var jstream = asmb.GetManifestResourceStream(json);
        if (jstream == null) // should only happen for _trimmed sprites
        {
          // ETGModConsole.Log($"could not find resource {json}");
          continue;
        }
        AssetSpriteData frameData = default;
        try
        {
            frameData = JSONHelper.ReadJSON<AssetSpriteData>(jstream);
        }
        catch
        {
          ETGModConsole.Log("Error: invalid json at project path " + json);
          jstream.Dispose();
          continue;
        }
        // ETGModConsole.Log($"setting attach points for {id} == {spriteName}");
        _WeaponCollection.SetAttachPoints(id, frameData.attachPoints);
        if (_WeaponCollection.inst != _WeaponCollection)
            _WeaponCollection.inst.SetAttachPoints(id, frameData.attachPoints);
      }
    }
  }

  internal static tk2dSpriteCollectionData itemCollection = PickupObjectDatabase.GetById(155).sprite.Collection;

  /// <summary>Patched version of Alexandria's SpriteFromResource</summary>
  [HarmonyPatch(typeof(SpriteBuilder), nameof(SpriteBuilder.SpriteFromResource))]
  private class SpriteFromResourcePatch
  {
    public static bool Prefix(string spriteName, GameObject obj, Assembly assembly, ref GameObject __result)
    {
        // ETGModConsole.Log($"CALLING PATCHED SpriteFromResource for {spriteName}");
        if (obj == null)
          obj = new GameObject();

        tk2dSprite sprite;
        sprite = obj.AddComponent<tk2dSprite>();

        int id = SpriteBuilder.AddSpriteToCollection(PackerHelper.NamedSpriteInPackedTexture(spriteName), itemCollection);
        sprite.SetSprite(itemCollection, id);
        sprite.SortingOrder = 0;
        sprite.IsPerpendicular = true;

        obj.GetComponent<BraveBehaviour>().sprite = sprite;

        __result = obj;
        return false; // skip original method
    }
  }

  /// <summary>Patched version of Alexandria's AddSpriteToCollection(string, ...)</summary>
  [HarmonyPatch(typeof(SpriteBuilder), nameof(SpriteBuilder.AddSpriteToCollection), typeof(string), typeof(tk2dSpriteCollectionData), /*typeof(string), */typeof(Assembly))]
  private class AddSpriteToCollectionPatch
  {
    public static bool Prefix(string resourcePath, tk2dSpriteCollectionData collection, /*string name, */Assembly assembly, ref int __result)
    {
        // ETGModConsole.Log($"CALLING PATCHED AddSpriteToCollection for {resourcePath}");
        __result = SpriteBuilder.AddSpriteToCollection(PackerHelper.NamedSpriteInPackedTexture(resourcePath), collection);
        return false; // skip original method
    }
  }
}
