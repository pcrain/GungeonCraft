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

  private static Dictionary<string, tk2dSpriteDefinition> _PackedTextures = new();

  /// <summary>Construct a tk2dSpriteDefinition from a segment of a packed texture</summary>
  public static tk2dSpriteDefinition SpriteDefFromSegment(this Texture2D texture, string spriteName, int x, int y, int w, int h)
  {
    Material material = new Material(ShaderCache.Acquire(PlayerController.DefaultShaderName));
    material.mainTexture = texture;
    float xx = (float)x/16f;
    float yy = (float)y/16f;
    float ww = (float)w / 16f;
    float hh = (float)h / 16f;
    tk2dSpriteDefinition tk2dSpriteDefinition = new tk2dSpriteDefinition
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
        colliderType = tk2dSpriteDefinition.ColliderType.None,
        collisionLayer = CollisionLayer.HighObstacle,
        position0 = new Vector3(xx, yy, 0f),
        position1 = new Vector3(xx + ww, yy, 0f),
        position2 = new Vector3(xx, yy + hh, 0f),
        position3 = new Vector3(xx + ww, yy + hh, 0f),
        material = material,
        materialInst = material,
        materialId = 0,
        uvs = new Vector2[]
        {
          new Vector2((float) x      / (float)texture.width, (float) y      / (float)texture.height),
          new Vector2((float)(x + w) / (float)texture.width, (float) y      / (float)texture.height),
          new Vector2((float) x      / (float)texture.width, (float)(y + h) / (float)texture.height),
          new Vector2((float)(x + w) / (float)texture.width, (float)(y + h) / (float)texture.height),
        },
        boundsDataCenter           = new Vector3(ww / 2f, hh / 2f, 0f),
        boundsDataExtents          = new Vector3(ww,      hh, 0f),
        untrimmedBoundsDataCenter  = new Vector3(ww / 2f, hh / 2f, 0f),
        untrimmedBoundsDataExtents = new Vector3(ww,      hh, 0f)
      };
    return tk2dSpriteDefinition;
  }

  /// <summary>Retrieve a tk2dSprite by name</summary>
  public static tk2dSpriteDefinition NamedSpriteInPackedTexture(string s)
  {
    return _PackedTextures.TryGetValue(s.Split('/').Last(), out tk2dSpriteDefinition value) ? value : null;
    // if (!_PackedTextures.TryGetValue(s, out SpriteInfo si))
    // {
    //   ETGModConsole.Log($"failed to retrieve sprite {s} from packed textures");
    //   return null;
    // }
    // return si.atlas.SpriteDefFromSegment(si.x, si.y, si.w, si.h);
  }

  /// <summary>Load a packed texture from a resource string</summary>
  public static void LoadPackedTextureResource(string textureResourcePath, string metaDataResourcePath)
  {
    Assembly asmb = Assembly.GetCallingAssembly();
    Texture2D atlas = ResourceExtractor.GetTextureFromResource(textureResourcePath, asmb);
    if (atlas == null)
      return;
    ETGModConsole.Log($"extracted texture {textureResourcePath}");
    using (Stream stream = asmb.GetManifestResourceStream(metaDataResourcePath))
    using (StreamReader reader = new StreamReader(stream))
    {
      string line = null;
      while ((line = reader.ReadLine()) != null)
      {
        string[] tokens = line.Split('\t');
        if (tokens.Length < 9)
          continue; // first line, skip it since it doesn't have relevant information
        string spriteName = tokens[0].Split('/').Last().Split('.').First();  // trim off path and extension
        int x = Int32.Parse(tokens[1]);
        int y = Int32.Parse(tokens[2]);
        int w = Int32.Parse(tokens[3]);
        int h = Int32.Parse(tokens[4]);
        // _PackedTextures[spriteName] = new SpriteInfo{atlas = atlas, x = x, y = y, w = w, h = h};
        _PackedTextures[spriteName] = atlas.SpriteDefFromSegment(spriteName, x, y, w, h);
        // ETGModConsole.Log($"loaded packed {w}x{h} sprite {spriteName} at {x},{y}");
      }
    }
  }
}
