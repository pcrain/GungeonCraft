namespace CwaffingTheGungy;

/// <summary>Convenience class for easily creating and placing dfLabels in world space.</summary>
public static class EasyLabel
{
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

  public static void Place(this dfLabel label, Vector2 pos, float rot = 0f, TextAlignment? align = null)
  {
      rot = rot.Clamp180();
      Vector2 finalPos = pos;
      float uiScale = Pixelator.Instance.ScaleTileScale / Pixelator.Instance.CurrentTileScale; // 1.33, usually
      float adj = label.PixelsToUnits() / uiScale; // PixelsToUnits() == 1 / 303.75 == 16/9 * 2/1080
      if (Mathf.Abs(rot) > 90f)
      {
          float fontSizeToPixels = uiScale / C.PIXELS_PER_CELL;
          rot = (rot + 180f).Clamp180();
          //NOTE: need to adjust position of bottom-aligned text
          //HACK: 0.5 seems to be the magic number for this font size here, idk how to arrive at this answer computationally though...
          //NOTE: label.Font.LineHeight == 40, label.TextScale == 0.6, label.Size.Y == 24, label.PixelsToUnits() == (1 / 303.75)
          //NOTE: df magic pixel scale = 1 / 64 == 1 / C.PIXELS_PER_CELL
          finalPos += (rot - 90f).ToVector(label.Size.y * fontSizeToPixels);  //WARN: guessing at the math here...
      }
      label.transform.position = dfFollowObject.ConvertWorldSpaces(
          finalPos,
          GameManager.Instance.MainCameraController.Camera,
          GameUIRoot.Instance.m_manager.RenderCamera).WithZ(0f).QuantizeFloor(adj);
      label.transform.localRotation = rot.EulerZ();
      if (align.HasValue)
      {
        TextAlignment alignment = label.TextAlignment = align.Value;
        if (alignment == TextAlignment.Left)
          label.Pivot = dfPivotPoint.MiddleLeft;
        else if (alignment == TextAlignment.Right)
          label.Pivot = dfPivotPoint.MiddleRight;
        else
          label.Pivot = dfPivotPoint.MiddleCenter;
      }
      label.IsVisible = true;
      LabelExt le = label.gameObject.GetComponent<LabelExt>();
      le.lastPos = pos;
      le.lastRot = rot;
  }
}

public class LabelExt : MonoBehaviour
{
    public Vector2 lastPos;
    public float lastRot;
}
