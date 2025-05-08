namespace CwaffingTheGungy;

public static class CwaffLabel
{
  public static dfLabel MakeNewLabel(bool unicode = true, bool outline = false)
  {
      dfLabel label = UnityEngine.Object.Instantiate(GameUIRoot.Instance.p_needsReloadLabel.gameObject, GameUIRoot.Instance.transform).GetComponent<dfLabel>();
      if (unicode)
      {
          label.Font = (ResourceCache.Acquire("Alternate Fonts/JackeyFont12_DF") as GameObject).GetComponent<dfFont>();
          label.Atlas = (label.Font as dfFont).Atlas;
          label.TextScale = 2.0f;
      }
      label.transform.localScale = Vector3.one / GameUIRoot.GameUIScalar;
      label.Anchor = dfAnchorStyle.CenterVertical | dfAnchorStyle.CenterHorizontal;
      label.TextAlignment = TextAlignment.Center;
      label.VerticalAlignment = dfVerticalAlignment.Middle;
      label.Opacity = 1f;
      label.Text = string.Empty;
      label.gameObject.SetActive(true);
      label.IsVisible = true;
      label.ProcessMarkup = true;
      label.Color = Color.white;
      if (outline)
      {
        label.Outline = true;
        label.OutlineSize = 4;
        label.OutlineColor = Color.black;
      }
      label.gameObject.AddComponent<LabelExt>();
      return label;
  }

  public static void Place(this dfLabel label, Vector2 pos, float rot = 0f)
  {
      rot = rot.Clamp180();
      Vector2 finalPos = pos;
      float uiScale = Pixelator.Instance.ScaleTileScale / Pixelator.Instance.CurrentTileScale; // 1.33, usually
      float fontSizeToPixels = uiScale / C.PIXELS_PER_CELL;
      float adj = label.PixelsToUnits() / uiScale; // PixelsToUnits() == 1 / 303.75 == 16/9 * 2/1080
      if (Mathf.Abs(rot) > 90f)
      {
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
          GameUIRoot.Instance.m_manager.RenderCamera).WithZ(0f);
      label.transform.position = label.transform.position.QuantizeFloor(adj);
      label.transform.localRotation = rot.EulerZ();
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
