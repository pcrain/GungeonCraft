namespace CwaffingTheGungy;

public static class AtlasFixer
{
    public static string BetterAddCustomCurrencyType(string ammoTypeSpritePath, string name, Assembly assembly = null)
    {
        return GameUIRoot.Instance.ConversationBar.portraitSprite.Atlas.FastAddNewItemToAtlas(ResourceExtractor.GetTextureFromResource(ammoTypeSpritePath, assembly ?? Assembly.GetCallingAssembly()), name).name;
    }

    /// <summary>
    /// Builds and adds a new <see cref="dfAtlas.ItemInfo"/> to <paramref name="atlas"/> with the texture of <paramref name="tex"/> and the name of <paramref name="name"/>.
    /// </summary>
    /// <param name="atlas">The <see cref="dfAtlas"/> to add the new <see cref="dfAtlas.ItemInfo"/> to.</param>
    /// <param name="tex">The texture of the new <see cref="dfAtlas.ItemInfo"/>.</param>
    /// <param name="name">The name of the new <see cref="dfAtlas.ItemInfo"/>. If <see langword="null"/>, it will default to <paramref name="tex"/>'s name.</param>
    /// <returns>The built <see cref="dfAtlas.ItemInfo"/>.</returns>
    public static dfAtlas.ItemInfo FastAddNewItemToAtlas(this dfAtlas atlas, Texture2D tex, string name = null)
    {
        if (string.IsNullOrEmpty(name))
        {
            name = tex.name;
        }
        if (atlas[name] != null)
        {
            return atlas[name];
        }
        dfAtlas.ItemInfo item = new dfAtlas.ItemInfo
        {
            border = new RectOffset(),
            deleted = false,
            name = name,
            region = atlas.FastFindFirstValidEmptySpace(new IntVector2(tex.width, tex.height)),
            rotated = false,
            sizeInPixels = new Vector2(tex.width, tex.height),
            texture = tex,
            textureGUID = name
        };
        int startPointX = Mathf.RoundToInt(item.region.x * atlas.Texture.width);
        int startPointY = Mathf.RoundToInt(item.region.y * atlas.Texture.height);
        int width = Mathf.RoundToInt(item.region.xMax * atlas.Texture.width) - startPointX;
        int height = Mathf.RoundToInt(item.region.yMax * atlas.Texture.height) - startPointY;
        atlas.Texture.SetPixels(startPointX, startPointY,
          width,
          height,
          tex.GetPixels(0, 0, width, height)
          );
        atlas.Texture.Apply();
        atlas.AddItem(item);

        return item;
    }

    /// <summary>
    /// Gets the first empty space in <paramref name="atlas"/> that has at least the size of <paramref name="pixelScale"/>.
    /// </summary>
    /// <param name="atlas">The <see cref="dfAtlas"/> to find the empty space in.</param>
    /// <param name="pixelScale">The required size of the empty space.</param>
    /// <returns>The rect of the empty space divided by the atlas texture's size.</returns>
    public static Rect FastFindFirstValidEmptySpace(this dfAtlas atlas, IntVector2 pixelScale)
    {




        if (atlas == null || atlas.Texture == null || !atlas.Texture.IsReadable())
        {
            return new Rect(0f, 0f, 0f, 0f);
        }
        Vector2Int point = new Vector2Int(0, 0);
        int pointIndex = -1;
        List<RectInt> rects = atlas.GetPixelRegions();


        while (true)
        {
            bool shouldContinue = false;
            foreach (RectInt rint in rects)
            {

                if (rint.DoseOverlap(new RectInt(point, pixelScale.ToVector2Int())))
                {
                    shouldContinue = true;
                    pointIndex++;
                    if (pointIndex >= rects.Count)
                    {
                        return new Rect(0f, 0f, 0f, 0f);
                    }
                    point = rects[pointIndex].max + Vector2Int.one;
                    if (point.x > atlas.Texture.width || point.y > atlas.Texture.height)
                    {
                        atlas.FastResizeAtlas(new IntVector2(atlas.Texture.width * 2, atlas.Texture.height * 2));
                    }
                    break;
                }
                bool shouldBreak = false;
                foreach (RectInt rint2 in rects)
                {
                    RectInt currentRect = new RectInt(point, pixelScale.ToVector2Int());
                    if (rint2.x < currentRect.x || rint2.y < currentRect.y)
                    {
                        continue;
                    }
                    else
                    {
                        if (currentRect.DoseOverlap(rint2))
                        {
                            shouldContinue = true;
                            shouldBreak = true;
                            pointIndex++;
                            if (pointIndex >= rects.Count)
                            {
                                return new Rect(0f, 0f, 0f, 0f);
                            }
                            point = rects[pointIndex].max + Vector2Int.one;
                            if (point.x > atlas.Texture.width || point.y > atlas.Texture.height)
                            {
                                atlas.FastResizeAtlas(new IntVector2(atlas.Texture.width * 2, atlas.Texture.height * 2));
                            }
                            break;
                        }
                    }
                }
                if (shouldBreak)
                {
                    break;
                }
            }
            if (shouldContinue)
            {
                continue;
            }
            RectInt currentRect2 = new RectInt(point, pixelScale.ToVector2Int());
            if (currentRect2.xMax > atlas.Texture.width || currentRect2.yMax > atlas.Texture.height)
            {
                atlas.FastResizeAtlas(new IntVector2(atlas.Texture.width * 2, atlas.Texture.height * 2));
            }
            break;
        }
        RectInt currentRect3 = new RectInt(point, pixelScale.ToVector2Int());
        Rect rect = new Rect((float)currentRect3.x / atlas.Texture.width, (float)currentRect3.y / atlas.Texture.height, (float)currentRect3.width / atlas.Texture.width, (float)currentRect3.height / atlas.Texture.height);
        return rect;
    }

        /// <summary>
    /// Resizes <paramref name="atlas"/> and all of it's <see cref="dfAtlas.ItemInfo"/>s.
    /// </summary>
    /// <param name="atlas">The <see cref="dfAtlas"/> to resize/</param>
    /// <param name="newDimensions"><paramref name="atlas"/>'s new size.</param>
    public static void FastResizeAtlas(this dfAtlas atlas, IntVector2 newDimensions)
        {
            Texture2D tex = atlas.Texture;
            if (!tex.IsReadable())
            {
                return;
            }
            if (tex.width == newDimensions.x && tex.height == newDimensions.y)
            {
                return;
            }
            foreach (dfAtlas.ItemInfo item in atlas.Items)
            {
                if (item.region != null)
                {
                    item.region.x = (item.region.x * tex.width) / newDimensions.x;
                    item.region.y = (item.region.y * tex.height) / newDimensions.y;
                    item.region.width = (item.region.width * tex.width) / newDimensions.x;
                    item.region.height = (item.region.height * tex.height) / newDimensions.y;
                }
            }
            tex.FastResizeBetter(newDimensions.x, newDimensions.y);
            atlas.Material.SetTexture("_MainTex", tex);
        }




        /// <summary>
    /// Resizes <paramref name="tex"/> without it losing it's pixel information.
    /// </summary>
    /// <param name="tex">The <see cref="Texture2D"/> to resize.</param>
    /// <param name="width">The <paramref name="tex"/>'s new width.</param>
    /// <param name="height">The <paramref name="tex"/>'s new height.</param>
    /// <returns></returns>
    public static bool FastResizeBetter(this Texture2D tex, int width, int height, bool center = false)
    {
        if (tex.IsReadable())
        {
            Color[][] pixels = new Color[Math.Min(tex.width, width)][];

            Texture2D newTex = new(width, height);

            int value = center ? 1 : 0;
            newTex.SetPixels(value, value, tex.width - 2 * value, tex.height - 2 * value, tex.GetPixels());

            bool result = tex.Resize(width, height);
            tex.SetPixels(newTex.GetPixels());

            // for (int x = value; x < tex.width - value; x++)
            // {
            //     for (int y = value; y < tex.height - value; y++)
            //     {
            //         bool isInOrigTex = false;
            //         if (x - value < pixels.Length)
            //         {
            //             if (y - value < pixels[x - value].Length)
            //             {
            //                 isInOrigTex = true;
            //                 tex.SetPixel(x, y, pixels[x - value][y - value]);
            //             }
            //         }
            //         if (!isInOrigTex)
            //         {
            //             tex.SetPixel(x, y, Color.clear);
            //         }
            //     }
            // }

            // for (int x = 0; x < tex.width; x++)
            // {
            //     for (int y = 0; y < tex.height; y++)
            //     {

            //         if (tex.GetPixel(x, y) == new Color32(205, 205, 205, 205))
            //         {
            //             tex.SetPixel(x, y, Color.clear);
            //         }

            //     }
            // }

            tex.Apply();
            return result;
        }
        return tex.Resize(width, height);
    }
}
