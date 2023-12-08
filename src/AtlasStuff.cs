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
