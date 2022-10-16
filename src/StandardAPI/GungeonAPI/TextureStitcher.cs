using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
namespace GungeonAPI
{
    public static class TextureStitcher
    {
        public static Rect AddFaceCardToAtlas(Texture2D tex, Texture2D atlas, int index, Rect bounds)
        {
            int xCapacity = (int)(bounds.width / 34); //floor of width/spritesize
            int yCapacity = (int)(bounds.height / 34);

            int xIndex = index % xCapacity;
            int yIndex = index / xCapacity;

            if (xIndex >= xCapacity || yIndex >= yCapacity)
            {
                Tools.PrintError("Not enough room left on the Facecard Atlas for this facecard!");
                return Rect.zero;
            }

            int xOffset = (int)bounds.x + (xIndex * 34);
            int yOffset = (int)bounds.y + (yIndex * 34);

            for (int x = 0; x < tex.width; x++)
            {
                for (int y = 0; y < tex.height; y++)
                {
                    atlas.SetPixel(x + xOffset, y + yOffset, tex.GetPixel(x, y));
                }
            }
            atlas.Apply(false, false);
            return new Rect((float)xOffset / atlas.width, (float)yOffset / atlas.height, 34f / atlas.width, 34f / atlas.height);
        }

        public static Rect ReplaceFaceCardInAtlas(Texture2D tex, Texture2D atlas, Rect region)
        {
            int xOffset = (int)Mathf.Round(atlas.width * region.x);
            int yOffset = (int)Mathf.Round(atlas.width * region.y);
            for (int x = 0; x < tex.width; x++)
            {
                for (int y = 0; y < tex.height; y++)
                {
                    atlas.SetPixel(x + xOffset, y + yOffset, tex.GetPixel(x, y));
                }
            }
            atlas.Apply(false, false);
            return new Rect((float)xOffset / atlas.width, (float)yOffset / atlas.height, 34f / atlas.width, 34f / atlas.height);
        }

        public static Texture2D CropWhiteSpace(this Texture2D orig)
        {
            Rect bounds = orig.GetTrimmedBounds();
            Texture2D result = new Texture2D((int)bounds.width, (int)bounds.height);
            result.name = orig.name;

            for (int x = (int)bounds.x; x < bounds.x + bounds.width; x++)
            {
                for (int y = (int)bounds.y; y < bounds.y + bounds.height; y++)
                {
                    result.SetPixel(x - (int)bounds.x, y - (int)bounds.y, orig.GetPixel(x, y));
                }
            }
            result.Apply(false, false);
            return result;
        }

        public static Rect GetTrimmedBounds(this Texture2D t)
        {

            int xMin = t.width;
            int yMin = t.height;
            int xMax = 0;
            int yMax = 0;

            for (int x = 0; x < t.width; x++)
            {
                for (int y = 0; y < t.height; y++)
                {
                    if (t.GetPixel(x, y) != Color.clear)
                    {
                        if (x < xMin) xMin = x;
                        if (y < yMin) yMin = y;
                        if (x > xMax) xMax = x;
                        if (y > yMax) yMax = y;
                    }
                }
            }

            return new Rect(xMin, yMin, xMax - xMin + 1, yMax - yMin + 1);
        }

        public static readonly int padding = 1;
        public static Texture2D AddMargin(this Texture2D texture)
        {
            Texture2D result = new Texture2D(texture.width + (2 * padding), texture.height + (2 * padding));
            result.name = texture.name;
            result.filterMode = texture.filterMode;

            for (int x = 0; x < result.width; x++)
            {
                for (int y = 0; y < result.height; y++)
                {
                    result.SetPixel(x, y, Color.clear);
                }
            }

            for (int x = 0; x < texture.width; x++)
            {
                for (int y = 0; y < texture.height; y++)
                {
                    result.SetPixel(x + padding, y + padding, texture.GetPixel(x, y));
                }
            }

            result.Apply(false, false);

            return result;
        }

        public static Texture2D GetReadable(this Texture2D texture)
        {
            RenderTexture tmp = RenderTexture.GetTemporary(
                    texture.width,
                    texture.height,
                    0,
                    RenderTextureFormat.Default,
                    RenderTextureReadWrite.Linear);

            // Blit the pixels on texture to the RenderTexture
            Graphics.Blit(texture, tmp);

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = tmp;

            Texture2D output = new Texture2D(texture.width, texture.height);
            output.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
            output.Apply();
            RenderTexture.active = previous;

            return output;
        }

        public static Texture2D Rotated(this Texture2D texture, bool clockwise = false)
        {
            Color32[] original = texture.GetPixels32();
            Color32[] rotated = new Color32[original.Length];
            int w = texture.width;
            int h = texture.height;

            int iRotated, iOriginal;

            for (int j = 0; j < h; ++j)
            {
                for (int i = 0; i < w; ++i)
                {
                    iRotated = (i + 1) * h - j - 1;
                    iOriginal = clockwise ? original.Length - 1 - (j * w + i) : j * w + i;
                    rotated[iRotated] = original[iOriginal];
                }
            }

            Texture2D rotatedTexture = new Texture2D(h, w);
            rotatedTexture.SetPixels32(rotated);
            rotatedTexture.Apply();
            return rotatedTexture;
        }

        public static Texture2D Flipped(this Texture2D texture, bool horizontal = true)
        {
            int w = texture.width;
            int h = texture.height;

            Texture2D output = new Texture2D(w, h);
            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    output.SetPixel(i, j, texture.GetPixel(w - i - 1, j));
                }
            }
            output.Apply();
            return output;
        }

    }
}
