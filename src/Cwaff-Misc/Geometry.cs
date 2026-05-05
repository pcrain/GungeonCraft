namespace CwaffingTheGungy;

/// <summary>Public API for drawing various custom geometry meshes.</summary>
public partial class Geometry : MonoBehaviour
{
    /// <summary>Shapes we're capable of drawing</summary>
    public enum Shape
    {
        NONE,
        FILLEDCIRCLE,
        CIRCLE,
        RING,
        DASHEDLINE,
        LINE,
        RECTANGLE,
    }

    public Shape shape       { get; private set; } = Shape.NONE;
    public Color color       { get; private set; } = default;
    public Vector2 pos       { get; private set; } = default;
    public float radius      { get; private set; } = 1f;
    public float radiusInner { get; private set; } = 0.5f;
    public float angle       { get; private set; } = 0f;
    public float arc         { get; private set; } = 360f;

    /// <summary>Creates a new Geometry object.</summary>
    /// <param name="shape">The shape of the geometry to render. Cannot be changed after initial Setup().</param>
    public static Geometry Create(Shape shape)
    {
      if (shape == Shape.NONE)
      {
          UnityEngine.Debug.LogError($"must set shape of Geometry on creation");
          return null;
      }
      Geometry g = new GameObject().AddComponent<Geometry>();
      g.shape = shape;
      g.CreateMesh();
      return g;
    }

    /// <summary>Requests that a Geometry object be drawn on the GUI background layer. Recommended if drawing in screen space.</summary>
    public Geometry UseGUILayer()
    {
      base.gameObject.SetLayerRecursively(LayerMask.NameToLayer("GUI"));
      return this;
    }

    /// <summary>Sets up and enables the renderer for a Geometry object in world coordinates.</summary>
    /// <param name="color">The color of the geometry to render.</param>
    /// <param name="pos">The origin of the geometry. Corresponds to the center for circle-like shapes, a corner for rectangle-like shapes, and an endpoint for line-like shapes.</param>
    /// <param name="pos2">A second point uniquely definining the geometry. Corresponds to a point on the perimeter for circle-like shapes, the corner opposite of the origin for rectangle-like shapes, and a second endpoint for line-like shapes.</param>
    /// <param name="radius">The radius for circle-like shapes.</param>
    /// <param name="angle">The angle for circle-like shapes (only meaningful when arc is less than 360 degrees).</param>
    /// <param name="arc">The angle a circle-like shape subtends (used for drawing cones / wedges).</param>
    /// <param name="radiusInner">The inner radius for ring-like shapes.</param>
    /// <param name="useScreenSpace">If true, coordinates are taken to be screen space instead of world space. (0,0) is the bottom-left of the screen, (1,1) is the top-right.</param>
    public Geometry Place(Color? color = null, Vector2? pos = null, Vector2? pos2 = null, float? radius = null, float? angle = null, float? arc = null, float? radiusInner = null, bool useScreenSpace = false)
    {
        if (useScreenSpace)
        {
          CameraController mcc = GameManager.Instance.MainCameraController;
          if (pos is Vector2 posv)
            pos = new Vector2(
              Mathf.Lerp(mcc.m_cachedMinPos.x, mcc.m_cachedMaxPos.x, posv.x),
              Mathf.Lerp(mcc.m_cachedMinPos.y, mcc.m_cachedMaxPos.y, posv.y));
          if (pos2 is Vector2 pos2v)
            pos2 = new Vector2(
              Mathf.Lerp(mcc.m_cachedMinPos.x, mcc.m_cachedMaxPos.x, pos2v.x),
              Mathf.Lerp(mcc.m_cachedMinPos.y, mcc.m_cachedMaxPos.y, pos2v.y));
          if (radius is float radiusv)
            radius = Mathf.Clamp01(radiusv) * (mcc.m_cachedMaxPos.x - mcc.m_cachedMinPos.x);
          if (radiusInner is float radiusInnerv)
            radiusInner = Mathf.Clamp01(radiusInnerv) * (mcc.m_cachedMaxPos.x - mcc.m_cachedMinPos.x);
        }

        this.color  = color  ?? this.color;
        this.pos    = pos    ?? this.pos;
        if (pos2.HasValue)
        {
            Vector2 delta = pos2.Value - this.pos;
            this.radius   = delta.magnitude;
            this.angle    = delta.ToAngle();
            this.arc      = arc    ?? this.arc;
        }
        else
        {
            this.radius = radius ?? this.radius;
            this.angle  = angle  ?? this.angle;
            this.arc    = arc    ?? this.arc;
            this.radiusInner = radiusInner ?? this.radiusInner;
        }
        if (color.HasValue)
            this._meshRenderer.material.SetColor(_OverrideColorId, this.color);
        if (!this._didSetup || pos.HasValue || pos2.HasValue || radius.HasValue || radiusInner.HasValue)
            RebuildMeshes();
        this._didSetup = true;
        this._meshRenderer.enabled = true;
        return this;
    }

    /// <summary>Disables the renderer for this Geometry. Can be re-enabled by calling Setup() with no arguments.</summary>
    public Geometry Disable()
    {
      if (this._meshRenderer)
        this._meshRenderer.enabled = false;
      return this;
    }
}

public static class GeometryHelpers
{
  /// <summary>Convenience method for creating Geometry directly from a Geometry Shape.</summary>
  public static Geometry Create(this Geometry.Shape shape)
  {
    return Geometry.Create(shape);
  }
}

// Private API
public partial class Geometry : MonoBehaviour
{
    private const int _CIRCLE_SEGMENTS = 100;
    private const int _MAX_LINE_SEGMENTS = 100;
    private const int _MAX_LINE_VERTICES = _MAX_LINE_SEGMENTS * 2;
    private const float _MIN_SEG_LEN = 0.2f;

    private static readonly Vector2 _WayOffscreen = new Vector2(100000f, 100000f);
    private static readonly int _OverrideColorId = Shader.PropertyToID("_OverrideColor");

    private bool _didSetup = false;
    private GameObject _meshObject = null;
    private Mesh _mesh = null;
    private Vector3[] _vertices = null;
    private MeshRenderer _meshRenderer = null;

    private void Awake()
    {
        this._mesh = new Mesh();
        this._meshObject = new GameObject("debug_circle", typeof(MeshFilter), typeof(MeshRenderer));
        this._meshObject.SetLayerRecursively(LayerMask.NameToLayer("Unoccluded"));
        this._meshObject.GetComponent<MeshFilter>().mesh = this._mesh;
        this._meshRenderer = this._meshObject.GetComponent<MeshRenderer>();
    }

    private void CreateMesh()
    {
        switch (this.shape)
        {
            case Shape.FILLEDCIRCLE:
                this._vertices = new Vector3[_CIRCLE_SEGMENTS + 2];
                int[] triangles = new int[3 * _CIRCLE_SEGMENTS];
                for (int i = 0; i < _CIRCLE_SEGMENTS; i++) //NOTE: triangle fan
                {
                    triangles[i * 3]     = 0;
                    triangles[i * 3 + 1] = i + 1;
                    triangles[i * 3 + 2] = i + 2;
                }
                this._mesh.vertices = this._vertices;
                this._mesh.uv = new Vector2[this._vertices.Length];
                this._mesh.triangles = triangles;
                break;
            case Shape.RING:
                this._vertices = new Vector3[_CIRCLE_SEGMENTS * 2 + 2];
                int[] ringTriangles = new int[6 * _CIRCLE_SEGMENTS];
                int n = 0;
                for (int i = 0; i < _CIRCLE_SEGMENTS * 2; i += 2) //NOTE: need _CIRCLE_SEGMENTS quads
                {
                    ringTriangles[n++] = i + 0;
                    ringTriangles[n++] = i + 1;
                    ringTriangles[n++] = i + 2;
                    ringTriangles[n++] = i + 1;
                    ringTriangles[n++] = i + 2;
                    ringTriangles[n++] = i + 3;
                }
                this._mesh.vertices = this._vertices;
                this._mesh.uv = new Vector2[this._vertices.Length];
                this._mesh.triangles = ringTriangles;
                break;
            case Shape.CIRCLE:
                this._vertices = new Vector3[_CIRCLE_SEGMENTS + 1];
                int[] segments = new int[2 * _CIRCLE_SEGMENTS];
                for (int i = 0; i < _CIRCLE_SEGMENTS; i++)
                {
                    segments[i * 2]     = i;
                    segments[i * 2 + 1] = i + 1;
                }
                this._mesh.vertices = this._vertices;
                this._mesh.uv = new Vector2[this._vertices.Length];
                this._mesh.SetIndices(segments, MeshTopology.Lines, 0);
                break;
            case Shape.DASHEDLINE:
                this._vertices = new Vector3[2 * _MAX_LINE_SEGMENTS];
                int[] segmentsB = new int[2 * _MAX_LINE_SEGMENTS];
                for (int i = 0; i < 2 * _MAX_LINE_SEGMENTS; i++)
                    segmentsB[i] = i;
                this._mesh.vertices = this._vertices;
                this._mesh.uv = new Vector2[this._vertices.Length];
                this._mesh.SetIndices(segmentsB, MeshTopology.Lines, 0);
                break;
            case Shape.LINE:
                this._vertices = new Vector3[2];
                int[] segment = new int[2];
                segment[0] = 0;
                segment[1] = 1;
                this._mesh.vertices = this._vertices;
                this._mesh.uv = new Vector2[this._vertices.Length];
                this._mesh.SetIndices(segment, MeshTopology.Lines, 0);
                break;
            case Shape.RECTANGLE:
                this._vertices = new Vector3[4];
                this._mesh.vertices = this._vertices;
                this._mesh.uv = new Vector2[this._vertices.Length];
                this._mesh.SetIndices(new int[6]{0, 1, 2, 1, 2, 3}, MeshTopology.Triangles, 0);
                break;
            default:
                break;
        }

        Material mat = this._meshRenderer.material = BraveResources.Load("Global VFX/WhiteMaterial", ".mat") as Material;
        mat.shader = ShaderCache.Acquire("tk2d/BlendVertexColorAlphaTintableTilted");
        mat.SetColor(_OverrideColorId, this.color);
    }

    private void RebuildMeshes()
    {
        Vector3 basePos = this.pos;
        switch (this.shape)
        {
            case Shape.FILLEDCIRCLE:
                this._vertices[0] = basePos;
                float startF = (this.angle - 0.5f * this.arc).Clamp360();
                float gapF = this.arc / (_CIRCLE_SEGMENTS - 1);
                for (int i = 0; i <= _CIRCLE_SEGMENTS; ++i)
                    this._vertices[i + 1] = basePos + (startF + i * gapF).ToVector3(this.radius);
                break;
            case Shape.RING:
                for (int i = 0; i <= _CIRCLE_SEGMENTS; ++i)
                {
                    float angle = (i * (360f / _CIRCLE_SEGMENTS));
                    this._vertices[2 * i + 0] = basePos + angle.ToVector3(this.radius);
                    this._vertices[2 * i + 1] = basePos + angle.ToVector3(this.radiusInner);
                }
                break;
            case Shape.CIRCLE:
                float start = (this.angle - 0.5f * this.arc).Clamp360();
                float gap = this.arc / (_CIRCLE_SEGMENTS - 1);
                for (int i = 0; i <= _CIRCLE_SEGMENTS; ++i)
                    this._vertices[i] = basePos + (start + i * gap).ToVector3(this.radius);
                break;
            case Shape.DASHEDLINE:
                float vertexSpacing = Mathf.Max(_MIN_SEG_LEN, this.radius / (_MAX_LINE_VERTICES - 1));
                float verticesNeeded = this.radius / vertexSpacing;
                int maxVertexToDraw = Mathf.FloorToInt(verticesNeeded);
                if (maxVertexToDraw % 2 != 1)  // start and end with a line segment
                    --maxVertexToDraw;
                float offset = 0.5f * (verticesNeeded - maxVertexToDraw);
                for (int i = 0; i <= maxVertexToDraw; ++i)
                    this._vertices[i] = basePos + this.angle.ToVector3((offset + i) * vertexSpacing);
                for (int i = maxVertexToDraw + 1; i < _MAX_LINE_VERTICES; ++i)
                    this._vertices[i] = _WayOffscreen;
                break;
            case Shape.LINE:
                this._vertices[0] = basePos;
                this._vertices[1] = basePos + this.angle.ToVector3(this.radius);
                break;
            case Shape.RECTANGLE:
                this._vertices[0] = basePos;
                this._vertices[3] = basePos + this.angle.ToVector3(this.radius);
                this._vertices[1] = new Vector3(this._vertices[3].x, basePos.y);
                this._vertices[2] = new Vector3(basePos.x, this._vertices[3].y);
                break;
            default:
                break;
        }
        this._mesh.vertices = this._vertices; // necessary to actually trigger an update for some reason
        this._mesh.RecalculateBounds();
        if (this.shape == Shape.FILLEDCIRCLE)
            this._mesh.RecalculateNormals();
    }

    private void OnDestroy()
    {
        if (this._meshObject)
            UnityEngine.Object.Destroy(this._meshObject);
    }
}
