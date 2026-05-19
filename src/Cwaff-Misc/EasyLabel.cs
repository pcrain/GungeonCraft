namespace CwaffingTheGungy;

/// <summary>Convenience class for easily creating and placing dfLabels in world space.</summary>
public static class EasyLabel
{
  private static int _LastCamFrameCount = 0; // laste frame for which we've cached the conversion matrix
  private static Matrix4x4 _ConversionMatrix = default; // conversion matrix for quickly translating between coordinate spaces

  /// <summary>Creates a dfLabel that can be easily rendered on screen at will.</summary>
  /// <param name="unicode">Whether to use a unicode font or an ASCII font. Don't set to false unless you know what you're doing.</param>
  /// <param name="outline">Whether the font should be drawn with an outline or not.</param>
  /// <param name="align">How the text should be aligned relative to its placed position.</param>
  public static dfLabel Create(bool unicode = true, bool outline = false, TextAlignment align = TextAlignment.Center)
  {
      dfLabel label = UnityEngine.Object.Instantiate(GameUIRoot.Instance.p_needsReloadLabel.gameObject, GameUIRoot.Instance.transform).GetComponent<dfLabel>();
      if (unicode)
      {
          label.Font = (ResourceCache.Acquire("Alternate Fonts/JackeyFont12_DF") as GameObject).GetComponent<dfFont>();
          label.Atlas = (label.Font as dfFont).Atlas;
          label.TextScale = 2.0f;
      }
      label.transform.localScale = Vector3.one / GameUIRoot.GameUIScalar;
      label.TextAlignment = align;
      if (align == TextAlignment.Left)
        label.Pivot = dfPivotPoint.BottomLeft;
      else if (align == TextAlignment.Right)
        label.Pivot = dfPivotPoint.BottomRight;
      else
        label.Pivot = dfPivotPoint.BottomCenter;
      label.VerticalAlignment = dfVerticalAlignment.Middle;
      label.Opacity = 1f;
      label.Text = string.Empty;
      label.gameObject.SetActive(true);
      label.IsVisible = true;
      label.ProcessMarkup = true;
      label.Color = Color.white;
      label.WordWrap = true;
      if (outline)
      {
        label.Outline = true;
        label.OutlineSize = 4;
        label.OutlineColor = Color.black;
      }
      label.gameObject.AddComponent<LabelExt>();
      return label;
  }

  /// <summary>Places a dfLabel created via EasyLabel.Create() and enables rendering for it.</summary>
  /// <param name="label">The dfLabel being placed.</param>
  /// <param name="pos">Where to place the label in world space, if useScreenSpace is false, or in screen space, if useScreenSpace is true.</param>
  /// <param name="rot">Counterclockwise rotation of the label, in degrees.</param>
  /// <param name="useScreenSpace">If true, pos coordinates are taken to be screen space instead of world space. (0,0) is the bottom-left of the screen, (1,1) is the top-right.</param>
  public static void Place(this dfLabel label, Vector2 pos, float rot = 0f, bool useScreenSpace = false)
  {
      rot = BraveMathCollege.ClampAngle180(rot);
      Vector2 finalPos = pos;
      float uiScale = Pixelator.Instance.ScaleTileScale / Pixelator.Instance.CurrentTileScale; // 1.33, usually
      float adj = label.PixelsToUnits() / uiScale; // PixelsToUnits() == 1 / 303.75 == 16/9 * 2/1080
      if (Mathf.Abs(rot) > 90f)
      {
          float fontSizeToPixels = uiScale / C.PIXELS_PER_CELL;
          rot = BraveMathCollege.ClampAngle180(rot + 180f);
          //NOTE: need to adjust position of bottom-aligned text
          //HACK: 0.5 seems to be the magic number for this font size here, idk how to arrive at this answer computationally though...
          //NOTE: label.Font.LineHeight == 40, label.TextScale == 0.6, label.Size.Y == 24, label.PixelsToUnits() == (1 / 303.75)
          //NOTE: df magic pixel scale = 1 / 64 == 1 / C.PIXELS_PER_CELL
          finalPos += BraveMathCollege.DegreesToVector(rot - 90f, label.Size.y * fontSizeToPixels);  //WARN: guessing at the math here...
      }
      CameraController mcc = GameManager.Instance.MainCameraController;
      if (useScreenSpace)
      {
        finalPos = new Vector2(
          Mathf.Lerp(mcc.m_cachedMinPos.x, mcc.m_cachedMaxPos.x, finalPos.x),
          Mathf.Lerp(mcc.m_cachedMinPos.y, mcc.m_cachedMaxPos.y, finalPos.y));
      }
      int frameCount = Time.frameCount;
      if (_LastCamFrameCount != frameCount)
      {
        // compute conversion matrix for this frame
        _LastCamFrameCount = frameCount;
        Camera mainCam = mcc.Camera;
        Camera renderCam = GameUIRoot.Instance.m_manager.RenderCamera;
        Matrix4x4 inVP = mainCam.projectionMatrix * mainCam.worldToCameraMatrix;
        Matrix4x4 outVP = renderCam.projectionMatrix * renderCam.worldToCameraMatrix;
        _ConversionMatrix = outVP.inverse * inVP;
      }
      Vector4 v = _ConversionMatrix * new Vector4(finalPos.x, finalPos.y, 0f, 1f);
      // Lazy.DebugConsoleLog($"converted {finalPos.x},{finalPos.y} to {v.x},{v.y}");
      label.transform.position = new Vector3(v.x, v.y, 0f).QuantizeFloor(adj);

      label.transform.localRotation = Quaternion.Euler(0f, 0f, rot);
      label.IsVisible = true;
      LabelExt le = label.gameObject.GetComponent<LabelExt>();
      le.lastPos = pos;
      le.lastRot = rot;
  }

  /// <summary>Prevents a dfLabel from rendering. Does NOT destroy it. Automatically re-enabled when calling Place().</summary>
  public static void Disable(this dfLabel label)
  {
    label.IsVisible = false;
  }
}

public class LabelExt : MonoBehaviour
{
    public Vector2 lastPos;
    public float lastRot;
}
