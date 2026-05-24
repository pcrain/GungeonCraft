namespace CwaffingTheGungy;

/// <summary>Class for managing bones for various tile sprites and meshes.</summary>
public class CwaffBone
{
  private static readonly LinkedList<CwaffBone> _BonePool = new();
  private static int _BonesCreated = 0;

  public Vector2 pos;
  public Vector2 normal;

  internal static LinkedListNode<CwaffBone> Rent(Vector2 pos)
  {
    if (_BonePool.Count == 0)
      _BonePool.AddLast(new CwaffBone());

    LinkedListNode<CwaffBone> node = _BonePool.Last;
    _BonePool.RemoveLast();

    CwaffBone bone   = node.Value;
    bone.pos    = pos;
    bone.normal = default;

    return node;
  }

  internal static void Return(LinkedListNode<CwaffBone> bone)
  {
    _BonePool.AddLast(bone);
    // Lazy.DebugConsoleLog($"returned {_BonePool.Count}/{_BonesCreated} bones");
  }

  internal static void ReturnAll(ref LinkedList<CwaffBone> bones)
  {
    if (bones == null)
      return;
    while (bones.Count > 0)
    {
      LinkedListNode<CwaffBone> bone = bones.Last;
      bones.RemoveLast();
      _BonePool.AddLast(bone);
    }
    // Lazy.DebugConsoleLog($"returned {_BonePool.Count}/{_BonesCreated} bones");
  }

  private CwaffBone() // can only be created by Rent
  {
    ++_BonesCreated;
    // Lazy.DebugConsoleLog($"rented bone {_BonesCreated}");
  }
}

/// <summary>Class for managing a list of bones and the relevant sprites.</summary>
public class CwaffBoneManager : BraveBehaviour
{
  private const int _SUBTILE_PIXEL_LENGTH = 4; // each subtile is 4 pixels long

  private LinkedList<CwaffBone> _bones = new LinkedList<CwaffBone>();
  private tk2dTiledSprite _sprite = null;
  private int _spriteSubtileWidth;
  private Vector2 _minBonePosition;
  private Vector2 _maxBonePosition;
  private float _globalTimer;
  private tk2dSpriteAnimationClip _animation;
  private tk2dSpriteAnimationClip _startAnimation;
  private float _projectileScale = 1f;

  public void Setup(tk2dSpriteAnimationClip animation, tk2dSpriteAnimationClip startAnimation = null, float projectileScale = 1f)
  {
    base.transform.rotation = Quaternion.identity;
    base.transform.position = Vector3.zero;
    this._animation = animation;
    this._startAnimation = (startAnimation != null) ? startAnimation : animation;
    this._projectileScale = projectileScale;

    _sprite = this.GetOrAddComponent<tk2dTiledSprite>();
    _sprite.collection = animation.frames[0].spriteCollection;
    _sprite.spriteId = animation.frames[0].spriteId;
    _sprite.OverrideGetTiledSpriteGeomDesc = GetTiledSpriteGeomDesc;
    if (startAnimation != null)
    {
      _sprite.OverrideSetTiledSpriteGeom = SetStartAnimatedTiledSpriteGeom;
    }
    else
    {
      _sprite.OverrideSetTiledSpriteGeom = SetTiledSpriteGeom;
    }
    tk2dSpriteDefinition currentSpriteDef = _sprite.collection.spriteDefinitions[_sprite.spriteId];
    _spriteSubtileWidth = Mathf.RoundToInt(currentSpriteDef.untrimmedBoundsDataExtents.x / currentSpriteDef.texelSize.x) / 4;
  }

  public void UpdateTimers()
  {
    _globalTimer += BraveTime.DeltaTime;
  }

  public void ManualLateUpdate()
  {
    float minX = float.MaxValue;
    float maxX = float.MinValue;
    float minY = float.MaxValue;
    float maxY = float.MinValue;
    for (LinkedListNode<CwaffBone> n = _bones.First; n != null; n = n.Next)
    {
      Vector2 pos = n.Value.pos;
      if (pos.x < minX) minX = pos.x;
      if (pos.x > maxX) maxX = pos.x;
      if (pos.y < minY) minY = pos.y;
      if (pos.y > maxY) maxY = pos.y;
    }
    _minBonePosition = new Vector2(minX, minY);
    _maxBonePosition = new Vector2(maxX, maxY);
    base.transform.position = new Vector3(minX, minY);
    _sprite.ForceBuild();
    _sprite.UpdateZDepth();
  }

  public void UpdateAnimations()
  {
    UpdateTimers();
    _sprite.ForceBuild();
    _sprite.UpdateZDepth();
  }

  private static readonly Quaternion _Rot90 = Quaternion.Euler(0f, 0f, 90f);
  public void RecomputeNormals()
  {
    for (LinkedListNode<CwaffBone> n = _bones.First; n != _bones.Last; n = n.Next)
      n.Value.normal = (_Rot90 * (n.Next.Value.pos - n.Value.pos)).normalized;
    if (_bones.Count > 1)
      _bones.Last.Value.normal = _bones.Last.Previous.Value.normal;
  }

  public void ReturnAllBones()
  {
    CwaffBone.ReturnAll(ref _bones);
  }

  public void RentBone(Vector2 pos)
  {
    _bones.AddLast(CwaffBone.Rent(pos));
  }

  public bool HasBones()
  {
    return _bones.Count > 0;
  }

  public int BoneCount()
  {
    return _bones.Count;
  }

  //TODO: use for other meshes besides CwaffRopeMesh
  public void ReplaceBones(List<Vector2> points)
  {
    int nbones = this._bones.Count;
    int npoints = points.Count;
    LinkedListNode<CwaffBone> nextBone = this._bones.First;
    for (int i = 0; i < points.Count; ++i)
    {
      if (nextBone == null)
      {
        _bones.AddLast(CwaffBone.Rent(points[i]));
        continue;
      }
      nextBone.Value.pos = points[i];
      nextBone = nextBone.Next;
    }
    while (nbones-- > npoints)
    {
      nextBone = this._bones.Last;
      this._bones.RemoveLast();
      CwaffBone.Return(nextBone);
    }
  }

  public override void OnDestroy()
  {
    base.OnDestroy();
    CwaffBone.ReturnAll(ref _bones);
  }

  private void GetTiledSpriteGeomDesc(out int numVertices, out int numIndices, tk2dSpriteDefinition spriteDef, Vector2 dimensions)
  {
    int segments = Mathf.Max(_bones.Count - 1, 0);
    numVertices = segments * 4;
    numIndices = segments * 6;
  }

  private void SetTiledSpriteGeom(Vector3[] pos, Vector2[] uv, int offset, out Vector3 boundsCenter, out Vector3 boundsExtents, tk2dSpriteDefinition spriteDef, Vector3 scale, Vector2 dimensions, tk2dBaseSprite.Anchor anchor, float colliderOffsetZ, float colliderExtentZ)
  {
    boundsCenter = 0.5f * (_maxBonePosition + _minBonePosition);
    boundsExtents = 0.5f * (_maxBonePosition - _minBonePosition);
    if (_bones.Count < 2)
      return;

    // System.Diagnostics.Stopwatch geomWatch = System.Diagnostics.Stopwatch.StartNew();

    int spritePixelLength              = Mathf.RoundToInt(spriteDef.untrimmedBoundsDataExtents.x / spriteDef.texelSize.x);
    int numSubtilesInSprite            = spritePixelLength / _SUBTILE_PIXEL_LENGTH;
    int lastBoneIndex                  = _bones.Count - 1;
    int lastSubtileIndex               = numSubtilesInSprite - 1;
    int totalSpritesToDraw             = Mathf.CeilToInt((float)lastBoneIndex / (float)numSubtilesInSprite);
    int animationFrame                 = Mathf.FloorToInt(Mathf.Repeat(_globalTimer * _animation.fps, _animation.frames.Length));
    tk2dSpriteAnimationFrame frame     = _animation.frames[animationFrame];
    tk2dSpriteDefinition segmentSprite = frame.spriteCollection.spriteDefinitions[frame.spriteId];

    // precompute some common variables
    float invSubW   = 1f / _spriteSubtileWidth;
    float ssy0      = segmentSprite.position0.y;
    float ssy1      = segmentSprite.position1.y;
    float ssy2      = segmentSprite.position2.y;
    float ssy3      = segmentSprite.position3.y;
    Vector2 ssuv0   = segmentSprite.uvs[0];
    Vector2 ssuv1   = segmentSprite.uvs[1];
    Vector2 ssuv2   = segmentSprite.uvs[2];
    Vector2 ssuv3   = segmentSprite.uvs[3];
    float ssuv0x    = ssuv0.x;
    float ssuv2x    = ssuv2.x;
    float ssuv0y    = ssuv0.y;
    float ssuv2y    = ssuv2.y;
    float ssuvdxMin = ssuv1.x - ssuv0x;
    float ssuvdxMax = ssuv3.x - ssuv2x;
    float ssuvdyMin = ssuv1.y - ssuv0y;
    float ssuvdyMax = ssuv3.y - ssuv2y;
    float minBoneX  = _minBonePosition.x;
    float minBoneY  = _minBonePosition.y;
    float subSize   = 1f / numSubtilesInSprite;

    // handle actual vertex updating logic
    LinkedListNode<CwaffBone> bone = _bones.First;
    CwaffBone nextBone = bone.Value;
    float nextBoneX    = nextBone.pos.x - minBoneX;
    float nextBoneY    = nextBone.pos.y - minBoneY;
    float nextNormX    = nextBone.normal.x;
    float nextNormY    = nextBone.normal.y;
    CwaffBone curBone;
    float curBoneX;
    float curBoneY;
    float curNormX;
    float curNormY;
    unsafe
    {
      fixed (Vector3* posPtr = pos)
      fixed (Vector2* uvPtr = uv)
      {
        Vector3* ppos = posPtr;
        Vector2* puv  = uvPtr;
        for (int i = 0; i < totalSpritesToDraw; i++)
        {
          if (i == totalSpritesToDraw - 1 && lastBoneIndex % numSubtilesInSprite != 0)
            lastSubtileIndex = lastBoneIndex % numSubtilesInSprite - 1;
          float numSpritesDrawn = 0f;
          float nextWorldX1 = (nextBoneX + (nextNormX * ssy0));
          float nextWorldY1 = (nextBoneY + (nextNormY * ssy0));
          float nextWorldX2 = (nextBoneX + (nextNormX * ssy2));
          float nextWorldY2 = (nextBoneY + (nextNormY * ssy2));
          for (int j = 0; j <= lastSubtileIndex; j++)
          {
            curBone   = nextBone;
            curBoneX  = nextBoneX;
            curBoneY  = nextBoneY;
            curNormX  = nextNormX;
            curNormY  = nextNormY;
            bone      = bone.Next;
            nextBone  = bone.Value;
            nextBoneX = nextBone.pos.x - minBoneX;
            nextBoneY = nextBone.pos.y - minBoneY;
            nextNormX = nextBone.normal.x;
            nextNormY = nextBone.normal.y;

            if (i == totalSpritesToDraw - 1 && j == lastSubtileIndex) // only happens on final iteration
            {
              float dx = nextBoneX - curBoneX;
              float dy = nextBoneY - curBoneY;
              subSize = Mathf.Sqrt(dx * dx + dy * dy) / numSubtilesInSprite;
            }

            float minUVx = ssuv0x + ssuvdxMin * numSpritesDrawn;
            float minUVy = ssuv0y + ssuvdyMin * numSpritesDrawn;
            float maxT   = numSpritesDrawn + subSize;
            float maxUVx = ssuv2x + ssuvdxMax * maxT;
            float maxUVy = ssuv2y + ssuvdyMax * maxT;

            ppos->x = nextWorldX1;
            ppos->y = nextWorldY1;
            ppos->z = 0;
            ++ppos;
            puv->x  = minUVx;
            puv->y  = minUVy;
            ++puv;

            ppos->x = nextWorldX1 = (nextBoneX + (nextNormX * ssy1));
            ppos->y = nextWorldY1 = (nextBoneY + (nextNormY * ssy1));
            ppos->z = 0;
            ++ppos;
            puv->x  = maxUVx;
            puv->y  = minUVy;
            ++puv;

            ppos->x = nextWorldX2;
            ppos->y = nextWorldY2;
            ppos->z = 0;
            ++ppos;
            puv->x  = minUVx;
            puv->y  = maxUVy;
            ++puv;

            ppos->x = nextWorldX2 = (nextBoneX + (nextNormX * ssy3));
            ppos->y = nextWorldY2 = (nextBoneY + (nextNormY * ssy3));
            ppos->z = 0;
            ++ppos;
            puv->x  = maxUVx;
            puv->y  = maxUVy;
            ++puv;

            //NOTE: observation: the only time fractionOfSubtileToDraw isn't exactly 1 is in the very last iteration where numSpritesDrawn isn't used anyway
            // numSpritesDrawn += fractionOfSubtileToDraw / _spriteSubtileWidth;
            numSpritesDrawn += invSubW;
          }
        }
      }
    }
    // geomWatch.Stop(); System.Console.WriteLine($"    {geomWatch.ElapsedTicks,6} ticks geom for {totalSpritesToDraw} sprites ({(geomWatch.ElapsedTicks / totalSpritesToDraw)} avg)");
  }

  public void SetStartAnimatedTiledSpriteGeom(Vector3[] pos, Vector2[] uv, int offset, out Vector3 boundsCenter, out Vector3 boundsExtents, tk2dSpriteDefinition spriteDef, Vector3 scale, Vector2 dimensions, tk2dBaseSprite.Anchor anchor, float colliderOffsetZ, float colliderExtentZ)
  {
    int spritePixelLength = Mathf.RoundToInt(spriteDef.untrimmedBoundsDataExtents.x / spriteDef.texelSize.x);
    int numSubtilesInSprite = spritePixelLength / _SUBTILE_PIXEL_LENGTH;
    int lastBoneIndex = Mathf.Max(_bones.Count - 1, 0);
    int totalSpritesToDraw = Mathf.CeilToInt((float)lastBoneIndex / (float)numSubtilesInSprite);
    boundsCenter = 0.5f * (_minBonePosition + _maxBonePosition);
    boundsExtents = 0.5f * (_maxBonePosition - _minBonePosition);
    LinkedListNode<CwaffBone> bone = _bones.First;
    int verticesDrawn = 0;
    int animationFrame = Mathf.FloorToInt(Mathf.Repeat(_globalTimer * _animation.fps, _animation.frames.Length));
    for (int i = 0; i < totalSpritesToDraw; i++)
    {
      int lastSubtileIndex = numSubtilesInSprite - 1;
      if (i == totalSpritesToDraw - 1 && lastBoneIndex % numSubtilesInSprite != 0)
        lastSubtileIndex = lastBoneIndex % numSubtilesInSprite - 1;
      tk2dSpriteDefinition segmentSprite = spriteDef;
      if (i == 0)
      {
        int startAnimationFrame = Mathf.FloorToInt(Mathf.Repeat(_globalTimer * _startAnimation.fps, _startAnimation.frames.Length));
        segmentSprite = _sprite.Collection.spriteDefinitions[_startAnimation.frames[startAnimationFrame].spriteId];
      }
      else
        segmentSprite = _sprite.Collection.spriteDefinitions[_animation.frames[animationFrame].spriteId];
      float numSpritesDrawn = 0f;
      for (int j = 0; j <= lastSubtileIndex; j++)
      {
        float fractionOfSubtileToDraw = 1f;
        CwaffBone curBone = bone.Value;
        CwaffBone nextBone = bone.Next.Value;
        if (i == totalSpritesToDraw - 1 && j == lastSubtileIndex)
          fractionOfSubtileToDraw = Vector2.Distance(nextBone.pos, curBone.pos);
        int uvCurrent = offset + verticesDrawn;
        pos[uvCurrent++] = (curBone.pos  + curBone.normal  * (segmentSprite.position0.y * _projectileScale) - _minBonePosition).ToVector3ZUp(0f);
        pos[uvCurrent++] = (nextBone.pos + nextBone.normal * (segmentSprite.position1.y * _projectileScale) - _minBonePosition).ToVector3ZUp(0f);
        pos[uvCurrent++] = (curBone.pos  + curBone.normal  * (segmentSprite.position2.y * _projectileScale) - _minBonePosition).ToVector3ZUp(0f);
        pos[uvCurrent++] = (nextBone.pos + nextBone.normal * (segmentSprite.position3.y * _projectileScale) - _minBonePosition).ToVector3ZUp(0f);
        Vector2 minUV = Vector2.Lerp(segmentSprite.uvs[0], segmentSprite.uvs[1], numSpritesDrawn);
        Vector2 maxUV = Vector2.Lerp(segmentSprite.uvs[2], segmentSprite.uvs[3], numSpritesDrawn + fractionOfSubtileToDraw / numSubtilesInSprite);
        uvCurrent = offset + verticesDrawn;
        uv[uvCurrent++] = minUV;
        uv[uvCurrent++] = new Vector2(maxUV.x, minUV.y);
        uv[uvCurrent++] = new Vector2(minUV.x, maxUV.y);
        uv[uvCurrent++] = maxUV;
        verticesDrawn += 4;
        numSpritesDrawn += fractionOfSubtileToDraw / _spriteSubtileWidth;
        bone = bone.Next;
      }
    }
  }
}
