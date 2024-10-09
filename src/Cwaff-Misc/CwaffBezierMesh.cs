namespace CwaffingTheGungy;

/// <summary>Class for creating curvy beam-like sprites without beams</summary>
public class CwaffBezierMesh : MonoBehaviour
{
  private class Bone
  {
    private static int _Counter = 0;

    public Vector2 pos;

    public Vector2 normal;

    private static int _BonesCreated = 0;

    private static readonly LinkedList<Bone> _BonePool = new();

    internal static LinkedListNode<Bone> Rent(Vector2 pos)
    {
      if (_BonePool.Count == 0)
        _BonePool.AddLast(new Bone());

      LinkedListNode<Bone> node = _BonePool.Last;
      _BonePool.RemoveLast();

      Bone bone = node.Value;
      bone.pos    = pos;
      bone.normal = default;

      return node;
    }

    internal static void Return(LinkedListNode<Bone> bone)
    {
      _BonePool.AddLast(bone);
      // System.Console.WriteLine($"returned {_BonePool.Count}/{_BonesCreated} bones");
    }

    internal static void ReturnAll(ref LinkedList<Bone> bones)
    {
      if (bones == null)
        return;
      while (bones.Count > 0)
      {
        LinkedListNode<Bone> bone = bones.Last;
        bones.RemoveLast();
        _BonePool.AddLast(bone);
      }
      // System.Console.WriteLine($"returned {_BonePool.Count}/{_BonesCreated} bones");
    }

    private Bone() // can only be created by Rent
    {
      ++_BonesCreated;
    }
  }

  public tk2dSpriteAnimationClip animation;
  public Vector2 startPos;
  public Vector2 endPos;

  private tk2dTiledSprite m_sprite;
  private int m_spriteSubtileWidth;
  private LinkedList<Bone> m_bones = new LinkedList<Bone>();
  private Vector2 m_minBonePosition;
  private Vector2 m_maxBonePosition;
  private float m_globalTimer;

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
      mesh.animation       = animation;
      mesh.startPos        = startPos;
      mesh.endPos          = endPos;
      return mesh;
  }

  private void Start()
  {
    // base.transform.parent = SpawnManager.Instance.VFX;
    base.transform.rotation = Quaternion.identity;
    base.transform.position = Vector3.zero;
    m_sprite = this.GetOrAddComponent<tk2dTiledSprite>();
    m_sprite.collection = animation.frames[0].spriteCollection;
    m_sprite.spriteId = animation.frames[0].spriteId;
    m_sprite.OverrideGetTiledSpriteGeomDesc = GetTiledSpriteGeomDesc;
    m_sprite.OverrideSetTiledSpriteGeom = SetTiledSpriteGeom;
    tk2dSpriteDefinition currentSpriteDef = m_sprite.collection.spriteDefinitions[m_sprite.spriteId];
    m_spriteSubtileWidth = Mathf.RoundToInt(currentSpriteDef.untrimmedBoundsDataExtents.x / currentSpriteDef.texelSize.x) / 4;
  }

  private void Update()
  {
    m_globalTimer += BraveTime.DeltaTime;
    Bone.ReturnAll(ref m_bones);
    DrawMainBezierCurve(startPos, startPos + Vector2.down, endPos + Vector2.up, endPos);
    LinkedListNode<Bone> linkedListNode = m_bones.First;
    while (linkedListNode != null && linkedListNode != m_bones.Last)
    {
      linkedListNode.Value.normal = (Quaternion.Euler(0f, 0f, 90f) * (linkedListNode.Next.Value.pos - linkedListNode.Value.pos)).normalized;
      linkedListNode = linkedListNode.Next;
    }
    if (m_bones.Count > 0)
      m_bones.Last.Value.normal = m_bones.Last.Previous.Value.normal;
  }

  private void LateUpdate()
  {
    m_minBonePosition = new Vector2(float.MaxValue, float.MaxValue);
    m_maxBonePosition = new Vector2(float.MinValue, float.MinValue);
    for (LinkedListNode<Bone> linkedListNode = m_bones.First; linkedListNode != null; linkedListNode = linkedListNode.Next)
    {
      m_minBonePosition = Vector2.Min(m_minBonePosition, linkedListNode.Value.pos);
      m_maxBonePosition = Vector2.Max(m_maxBonePosition, linkedListNode.Value.pos);
    }
    Vector2 vector = new Vector2(m_minBonePosition.x, m_minBonePosition.y) - base.transform.position.XY();
    base.transform.position = new Vector3(m_minBonePosition.x, m_minBonePosition.y);
    m_sprite.HeightOffGround = 0.5f;
    m_sprite.ForceBuild();
    m_sprite.UpdateZDepth();
  }

  private void OnDestroy()
  {
    Bone.ReturnAll(ref m_bones);
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
    if (m_bones.Count == 0)
      m_bones.AddLast(Bone.Rent(curveStart));
    for (int j = 1; j <= approxPixelLength; j++)
      m_bones.AddLast(Bone.Rent(BraveMathCollege.CalculateBezierPoint(j / approxPixelLength, p0, p1, p2, p3)));
  }

  private void DrawMainBezierCurve(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
  {
    DrawBezierCurve(p0, p1, p2, p3);
    mainBezierPoint1 = p0;
    mainBezierPoint2 = p1;
    mainBezierPoint3 = p2;
    mainBezierPoint4 = p3;
  }

  private Vector2 GetPointOnMainBezier(float t)
  {
    return BraveMathCollege.CalculateBezierPoint(t, mainBezierPoint1, mainBezierPoint2, mainBezierPoint3, mainBezierPoint4);
  }

  private void GetTiledSpriteGeomDesc(out int numVertices, out int numIndices, tk2dSpriteDefinition spriteDef, Vector2 dimensions)
  {
    int segments = Mathf.Max(m_bones.Count - 1, 0);
    numVertices = segments * 4;
    numIndices = segments * 6;
  }

  private void SetTiledSpriteGeom(Vector3[] pos, Vector2[] uv, int offset, out Vector3 boundsCenter, out Vector3 boundsExtents, tk2dSpriteDefinition spriteDef, Vector3 scale, Vector2 dimensions, tk2dBaseSprite.Anchor anchor, float colliderOffsetZ, float colliderExtentZ)
  {
    int spritePixelLength = Mathf.RoundToInt(spriteDef.untrimmedBoundsDataExtents.x / spriteDef.texelSize.x);
    int numSubtilesInSprite = spritePixelLength / 4;
    int lastBoneIndex = Mathf.Max(m_bones.Count - 1, 0);
    int totalSpritesToDraw = Mathf.CeilToInt((float)lastBoneIndex / (float)numSubtilesInSprite);
    boundsCenter = (m_minBonePosition + m_maxBonePosition) / 2f;
    boundsExtents = (m_maxBonePosition - m_minBonePosition) / 2f;
    LinkedListNode<Bone> linkedListNode = m_bones.First;
    int verticesDrawn = 0;
    int animationFrame = Mathf.FloorToInt(Mathf.Repeat(m_globalTimer * animation.fps, animation.frames.Length));
    tk2dSpriteAnimationFrame frame = animation.frames[animationFrame];
    tk2dSpriteDefinition segmentSprite = frame.spriteCollection.spriteDefinitions[frame.spriteId];
    for (int i = 0; i < totalSpritesToDraw; i++)
    {
      int lastSubtileIndex = numSubtilesInSprite - 1;
      if (i == totalSpritesToDraw - 1 && lastBoneIndex % numSubtilesInSprite != 0)
        lastSubtileIndex = lastBoneIndex % numSubtilesInSprite - 1;
      float numSpritesDrawn = 0f;
      for (int j = 0; j <= lastSubtileIndex; j++)
      {
        float fractionOfSubtileToDraw = 1f;
        if (i == totalSpritesToDraw - 1 && j == lastSubtileIndex)
          fractionOfSubtileToDraw = Vector2.Distance(linkedListNode.Next.Value.pos, linkedListNode.Value.pos);
        int uvCurrent = offset + verticesDrawn;
        Bone curBone = linkedListNode.Value;
        Bone nextBone = linkedListNode.Next.Value;
        pos[uvCurrent++] = (curBone.pos  + curBone.normal  * segmentSprite.position0.y - m_minBonePosition).ToVector3ZUp(/*40*/0f);
        pos[uvCurrent++] = (nextBone.pos + nextBone.normal * segmentSprite.position1.y - m_minBonePosition).ToVector3ZUp(/*40*/0f);
        pos[uvCurrent++] = (curBone.pos  + curBone.normal  * segmentSprite.position2.y - m_minBonePosition).ToVector3ZUp(/*40*/0f);
        pos[uvCurrent++] = (nextBone.pos + nextBone.normal * segmentSprite.position3.y - m_minBonePosition).ToVector3ZUp(/*40*/0f);
        Vector2 minUV = Vector2.Lerp(segmentSprite.uvs[0], segmentSprite.uvs[1], numSpritesDrawn);
        Vector2 maxUV = Vector2.Lerp(segmentSprite.uvs[2], segmentSprite.uvs[3], numSpritesDrawn + fractionOfSubtileToDraw / numSubtilesInSprite);
        uvCurrent = offset + verticesDrawn;
        uv[uvCurrent++] = minUV;
        uv[uvCurrent++] = new Vector2(maxUV.x, minUV.y);
        uv[uvCurrent++] = new Vector2(minUV.x, maxUV.y);
        uv[uvCurrent++] = maxUV;
        verticesDrawn += 4;
        numSpritesDrawn += fractionOfSubtileToDraw / m_spriteSubtileWidth;
        if (linkedListNode != null)
          linkedListNode = linkedListNode.Next;
      }
    }
  }
}
