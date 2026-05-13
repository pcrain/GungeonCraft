namespace CwaffingTheGungy;

/// <summary>Class for creating curvy beam-like sprites without beams</summary>
public class CwaffBezierMesh : MonoBehaviour
{
  public tk2dSpriteAnimationClip animation;
  public Vector2 startPos;
  public Vector2 endPos;

  private tk2dTiledSprite m_sprite;
  private CwaffBoneManager _boneManager;

  private Vector3 mainBezierPoint1;
  private Vector3 mainBezierPoint2;
  private Vector3 mainBezierPoint3;
  private Vector3 mainBezierPoint4;

  private const int _BEZIER_CURVE_SEGMENTS = 20;
  private const int c_bonePixelLength = 4;
  private const float c_boneUnitLength = 0.25f;
  private const float c_trailHeightOffset = 0.5f;

  public static CwaffBezierMesh Create(tk2dSpriteAnimationClip animation, Vector2 startPos, Vector2 endPos, string name = null)
  {
      CwaffBezierMesh mesh = new GameObject(name ?? "new CwaffBezierMesh", typeof(CwaffBezierMesh)).GetComponent<CwaffBezierMesh>();
      mesh.m_sprite        = mesh.gameObject.GetOrAddComponent<tk2dTiledSprite>();
      mesh.animation       = animation;
      mesh.startPos        = startPos;
      mesh.endPos          = endPos;
      return mesh;
  }

  private void Start()
  {
    this._boneManager = base.gameObject.AddComponent<CwaffBoneManager>();
    this._boneManager.Setup(animation: animation);
  }

  private static readonly Quaternion _Rot90 = Quaternion.Euler(0f, 0f, 90f);
  private void Update()
  {
    this._boneManager.UpdateTimers();
    this._boneManager.ReturnAllBones();
    DrawMainBezierCurve(startPos, startPos + Vector2.down, endPos + Vector2.up, endPos);
    this._boneManager.RecomputeNormals();
  }

  private void LateUpdate()
  {
    m_sprite.HeightOffGround = 0.5f;
    this._boneManager.ManualLateUpdate();
  }

  private void DrawBezierCurve(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
  {
    Vector3 curveStart = BraveMathCollege.CalculateBezierPoint(0f, p0, p1, p2, p3);
    float approxLength = 0f;
    for (int i = 1; i <= _BEZIER_CURVE_SEGMENTS; i++)
    {
      Vector2 curveEnd = BraveMathCollege.CalculateBezierPoint((float)i / _BEZIER_CURVE_SEGMENTS, p0, p1, p2, p3);
      approxLength += Vector2.Distance(curveStart, curveEnd);
      curveStart = curveEnd;
    }
    float approxPixelLength = c_bonePixelLength * approxLength;
    curveStart = BraveMathCollege.CalculateBezierPoint(0f, p0, p1, p2, p3);
    if (!this._boneManager.HasBones())
      this._boneManager.RentBone(curveStart);
    for (int j = 1; j <= approxPixelLength; j++)
      this._boneManager.RentBone(BraveMathCollege.CalculateBezierPoint(j / approxPixelLength, p0, p1, p2, p3));
  }

  private void DrawMainBezierCurve(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
  {
    DrawBezierCurve(p0, p1, p2, p3);
    mainBezierPoint1 = p0;
    mainBezierPoint2 = p1;
    mainBezierPoint3 = p2;
    mainBezierPoint4 = p3;
  }

  public Vector2 GetPointOnMainBezier(float t)
  {
    return BraveMathCollege.CalculateBezierPoint(t, mainBezierPoint1, mainBezierPoint2, mainBezierPoint3, mainBezierPoint4);
  }
}
