namespace CwaffingTheGungy;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class tk2dMeshSprite : tk2dBaseSprite
{
  private static int m_shaderEmissivePowerID = -1;
  private static int m_shaderEmissiveColorPowerID = -1;
  private static int m_shaderEmissiveColorID = -1;
  private static int m_shaderThresholdID = -1;

  private static readonly Vector4                    _TangentVec    = new Vector4(1f, 0f, 0f, 1f);
  private static readonly Dictionary<int, Vector4[]> _TangentCache  = new();
  private static readonly Dictionary<int, Vector3[]> _NormalCache   = new();
  private static readonly Dictionary<int, int[]>     _TriangleCache = new();
  private static readonly Dictionary<int, int[]>     _PointCache    = new();

  private Mesh mesh;
  private Vector3[] meshVertices;
  private Vector3[] meshNormals;
  private Vector4[] meshTangents;
  private Color32[] meshColors;
  private Vector2[] meshUVs;
  private int[] meshTriangles;
  private MeshFilter m_filter;
  private int meshX = 2;
  private int meshY = 2;
  private int numVertices = 4;
  private bool setup = false;
  private bool isPointMesh = false;

  public Texture2D optionalPalette;
  public bool ApplyEmissivePropertyBlock;

  private static Vector4[] TangentsForMeshOfSize(int x, int y)
  {
    int index = (x << 16) + y;
    if (_TangentCache.TryGetValue(index, out Vector4[] cached))
      return cached;
    return _TangentCache[index] = Enumerable.Repeat<Vector4>(_TangentVec, x * y).ToArray();
  }

  // built left to right, bottom to top
  private static Vector3[] NormalsForMeshOfSize(int x, int y)
  {
    int index = (x << 16) + y;
    if (_NormalCache.TryGetValue(index, out Vector3[] cached))
      return cached;

    Vector3[] normals = new Vector3[x * y];
    int n = 0;
    for (int j = 1; j <= y; ++j)
    {
      float ycomp = (j == 1) ? -1f : ((j == y) ? 1f : 0f);
      for (int i = 1; i <= x; ++i)
      {
        float xcomp = (i == 1) ? -1f : ((i == x) ? 1f : 0f);
        normals[n++] = new Vector3(xcomp, ycomp, -1f);
      }
    }
    return _NormalCache[index] = normals;
  }

  // built left to right, bottom to top
  private static int[] TrianglesForMeshOfSize(int x, int y)
  {
    int index = (x << 16) + y;
    if (_TriangleCache.TryGetValue(index, out int[] cached))
      return cached;

    int xQuads = x - 1;
    int yQuads = y - 1;
    int nPoints = xQuads * yQuads * 6;

    int[] triangles = new int[nPoints];

    int n = 0;
    for (int j = 0; j < yQuads; ++j)
    {
      int y1 = j * x;
      int y2 = y1 + x;
      for (int i = 0; i < xQuads; ++i)
      {
        triangles[n + 0] = y1 + i;
        triangles[n + 1] = y2 + i + 1;
        triangles[n + 2] = y1 + i + 1;
        triangles[n + 3] = y2 + i;
        triangles[n + 4] = triangles[n + 1];
        triangles[n + 5] = triangles[n];
        n += 6;
      }
    }

    return _TriangleCache[index] = triangles;
  }

  // built left to right, bottom to top
  private static int[] PointsForMeshOfSize(int x, int y)
  {
    int index = (x << 16) + y;
    if (_PointCache.TryGetValue(index, out int[] cached))
      return cached;

    int nPoints = x * y;
    int[] points = new int[nPoints];
    for (int i = 0; i < nPoints; ++i)
      points[i] = i;

    return _PointCache[index] = points;
  }

  public void ResizeMesh(int xSize = -1, int ySize = -1, bool usePointMesh = false, bool build = true)
  {
    if (xSize < 2)
      xSize = 2;
    if (ySize < 2)
      ySize = xSize;

    meshX         = xSize;
    meshY         = ySize;
    isPointMesh   = usePointMesh;
    numVertices   = xSize * ySize;
    meshTangents  = TangentsForMeshOfSize(meshX, meshY);
    meshNormals   = NormalsForMeshOfSize(meshX, meshY);
    if (usePointMesh)
      meshTriangles = PointsForMeshOfSize(meshX, meshY);
    else
      meshTriangles = TrianglesForMeshOfSize(meshX, meshY);
    meshVertices  = new Vector3[numVertices];
    meshColors    = new Color32[numVertices];
    meshUVs       = new Vector2[numVertices];
    setup         = true;
    if (build)
      ForceBuild();
  }

  private new void Awake()
  {
    base.Awake();
    mesh = new Mesh();
    mesh.MarkDynamic();
    mesh.hideFlags = HideFlags.DontSave;
    m_filter = GetComponent<MeshFilter>();
    m_filter.mesh = mesh;
    if (!setup)
      ResizeMesh();
    if (base.Collection)
    {
      if (_spriteId < 0 || _spriteId >= base.Collection.Count)
        _spriteId = 0;
      Build();
    }
  }

  public override void OnDestroy()
  {
    base.OnDestroy();
    if (mesh)
      UnityEngine.Object.Destroy(mesh);
    if (meshColliderMesh)
      UnityEngine.Object.Destroy(meshColliderMesh);
  }

  protected void SetColorsMesh(Color32[] dest)
  {
    Color color = _color;
    if (collectionInst.premultipliedAlpha)
    {
      color.r *= color.a;
      color.g *= color.a;
      color.b *= color.a;
    }
    Color32 c32 = color;
    for (int i = 0; i < numVertices; i++)
      dest[i] = c32;
  }

  public override void Build()
  {
    BuildInternal(updateColors: true);
    UpdateMaterial();
    CreateCollider();
  }

  public override void UpdateGeometry()
  {
    UpdateGeometryImpl();
  }

  public override void UpdateColors()
  {
    UpdateColorsImpl();
  }

  public override void UpdateVertices()
  {
    UpdateVerticesImpl();
  }

  // built left to right, bottom to top, unless the UVs are rotated, then bottom to top -> left to right
  private static void ComputeUVs(Vector2[] uvs, tk2dSpriteDefinition def, int meshX, int meshY)
  {
    // If the x coordinate of the first two UVs match, we're using a rotated sprite
    bool isRotated = (def.uvs[0].x == def.uvs[1].x);

    Vector2 bl = def.uvs[0];
    Vector2 br = def.uvs[1];
    Vector2 tl = def.uvs[2];
    Vector2 tr = def.uvs[3];

    float xMax = meshX - 1f;
    float yMax = meshY - 1f;

    int n = 0;
    if (!isRotated)
      for (int j = 0; j < meshY; ++j)
      {
        float yOff = Mathf.Lerp(bl.y, tr.y, j / yMax);
        for (int i = 0; i < meshX; ++i)
        {
          float xOff = Mathf.Lerp(bl.x, tr.x, i / xMax);
          uvs[n++] = new Vector2(xOff, yOff);
        }
      }
    else
      for (int j = 0; j < meshY; ++j)
      {
        float xOff = Mathf.Lerp(bl.x, tr.x, j / yMax);
        for (int i = 0; i < meshX; ++i)
        {
          float yOff = Mathf.Lerp(bl.y, tr.y, i / xMax);
          uvs[n++] = new Vector2(xOff, yOff);
        }
      }
  }

  // built left to right, bottom to top
  private void SetPositionsMesh(Vector3[] positions, tk2dSpriteDefinition def)
  {
    if (m_transform == null)
      m_transform = base.transform;

    Vector3 bl = Vector3.Scale(def.position0, _scale);
    Vector3 br = Vector3.Scale(def.position1, _scale);
    Vector3 tl = Vector3.Scale(def.position2, _scale);
    Vector3 tr = Vector3.Scale(def.position3, _scale);

    float xMax = meshX; // needs to be a float
    float yMax = meshY; // needs to be a float
    float z = tr.z;

    int n = 0;
    for (int j = 0; j < meshY; ++j)
    {
      float yOff = Mathf.Lerp(bl.y, tr.y, (j + 0.5f) / yMax); // +0.5 -> build from center of pixel
      for (int i = 0; i < meshX; ++i)
      {
        float xOff = Mathf.Lerp(bl.x, tr.x, (i + 0.5f) / xMax); // +0.5 -> build from center of pixel
        positions[n++] = new Vector3(xOff, yOff, z);
      }
    }

    if (!ShouldDoTilt)
      return;

    for (int i = 0; i < numVertices; i++)
    {
      float y = (m_transform.rotation * Vector3.Scale(positions[i], m_transform.lossyScale)).y;
      positions[i].z += (IsPerpendicular ? -y : y);
    }
  }

  private void BuildInternal(bool updateColors)
  {
    if (!setup)
      ResizeMesh();
    if (mesh == null)
    {
      mesh = new Mesh();
      mesh.MarkDynamic();
      mesh.hideFlags = HideFlags.DontSave;
      GetComponent<MeshFilter>().mesh = mesh;
    }

    tk2dSpriteDefinition def = collectionInst.spriteDefinitions[base.spriteId];
    SetPositionsMesh(meshVertices, def);
    ComputeUVs(meshUVs, def, meshX, meshY);

    mesh.vertices = meshVertices;
    mesh.normals = meshNormals;
    mesh.tangents = meshTangents;
    if (isPointMesh)
      mesh.SetIndices(meshTriangles, MeshTopology.Points, 0);
    else
      mesh.triangles = meshTriangles;
    mesh.uv = meshUVs;
    mesh.bounds = tk2dBaseSprite.AdjustedMeshBounds(GetBounds(), renderLayer);

    if (updateColors)
      UpdateColorsImpl();
  }

  protected void UpdateColorsImpl()
  {
    if (mesh != null && meshColors != null && meshColors.Length != 0)
    {
      SetColorsMesh(meshColors);
      mesh.colors32 = meshColors;
    }
  }

  protected void UpdateVerticesImpl()
  {
    if (!collectionInst || collectionInst.spriteDefinitions == null)
      return;

    BuildInternal(updateColors: false);
  }

  protected void UpdateGeometryImpl()
  {
    BuildInternal(updateColors: true);
  }

  protected void CopyPropertyBlock(Material source, Material dest)
  {
    if (dest.HasProperty(m_shaderEmissivePowerID) && source.HasProperty(m_shaderEmissivePowerID))
      dest.SetFloat(m_shaderEmissivePowerID, source.GetFloat(m_shaderEmissivePowerID));
    if (dest.HasProperty(m_shaderEmissiveColorPowerID) && source.HasProperty(m_shaderEmissiveColorPowerID))
      dest.SetFloat(m_shaderEmissiveColorPowerID, source.GetFloat(m_shaderEmissiveColorPowerID));
    if (dest.HasProperty(m_shaderEmissiveColorID) && source.HasProperty(m_shaderEmissiveColorID))
      dest.SetColor(m_shaderEmissiveColorID, source.GetColor(m_shaderEmissiveColorID));
    if (dest.HasProperty(m_shaderThresholdID) && source.HasProperty(m_shaderThresholdID))
      dest.SetFloat(m_shaderThresholdID, source.GetFloat(m_shaderThresholdID));
  }

  public override void UpdateMaterial()
  {
    if (!base.renderer)
      return;

    if (m_shaderEmissiveColorID == -1)
    {
      m_shaderEmissivePowerID = Shader.PropertyToID("_EmissivePower");
      m_shaderEmissiveColorPowerID = Shader.PropertyToID("_EmissiveColorPower");
      m_shaderEmissiveColorID = Shader.PropertyToID("_EmissiveColor");
      m_shaderThresholdID = Shader.PropertyToID("_EmissiveThresholdSensitivity");
    }
    if (OverrideMaterialMode != 0 && base.renderer.sharedMaterial != null)
    {
      if (OverrideMaterialMode == SpriteMaterialOverrideMode.OVERRIDE_MATERIAL_SIMPLE)
      {
        Material materialInst = collectionInst.spriteDefinitions[base.spriteId].materialInst;
        Material sharedMaterial = base.renderer.sharedMaterial;
        if (sharedMaterial != materialInst)
        {
          sharedMaterial.mainTexture = materialInst.mainTexture;
          if (ApplyEmissivePropertyBlock)
          {
            CopyPropertyBlock(materialInst, sharedMaterial);
          }
        }
        return;
      }
      if (OverrideMaterialMode == SpriteMaterialOverrideMode.OVERRIDE_MATERIAL_COMPLEX)
        return;
    }
    if (base.renderer.sharedMaterial != collectionInst.spriteDefinitions[base.spriteId].materialInst)
      base.renderer.material = collectionInst.spriteDefinitions[base.spriteId].materialInst;
  }

  public override int GetCurrentVertexCount()
  {
    return (meshVertices != null) ? meshVertices.Length : 0;
  }

  public override void ForceBuild()
  {
    if ((bool)this)
    {
      base.ForceBuild();
      GetComponent<MeshFilter>().mesh = mesh;
    }
  }

  public override void ReshapeBounds(Vector3 dMin, Vector3 dMax)
  {
    tk2dSpriteDefinition currentSprite = base.CurrentSprite;
    Vector3 vector = Vector3.Scale(currentSprite.untrimmedBoundsDataCenter - 0.5f * currentSprite.untrimmedBoundsDataExtents, _scale);
    Vector3 vector2 = Vector3.Scale(currentSprite.untrimmedBoundsDataExtents, _scale);
    Vector3 vector3 = vector2 + dMax - dMin;
    vector3.x /= currentSprite.untrimmedBoundsDataExtents.x;
    vector3.y /= currentSprite.untrimmedBoundsDataExtents.y;
    Vector3 vector4 = new Vector3((!Mathf.Approximately(_scale.x, 0f)) ? (vector.x * vector3.x / _scale.x) : 0f, (!Mathf.Approximately(_scale.y, 0f)) ? (vector.y * vector3.y / _scale.y) : 0f);
    Vector3 position = vector + dMin - vector4;
    position.z = 0f;
    base.transform.position = base.transform.TransformPoint(position);
    base.scale = new Vector3(vector3.x, vector3.y, _scale.z);
  }
}
