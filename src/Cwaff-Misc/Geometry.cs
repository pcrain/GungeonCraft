namespace CwaffingTheGungy;

/// <summary>Class for drawing various custom geometry meshes </summary>
public class Geometry : MonoBehaviour
{
    public enum Shape
    {
        NONE,
        FILLEDCIRCLE,
        CIRCLE,
        RING,
        DASHEDLINE,
        LINE,
    }

    public Color color = default;
    public Vector2 pos = default;
    public float radius = 1f;
    public float radiusInner = 0.5f;
    public float angle = 0f;
    public float arc = 360f;

    private const int _CIRCLE_SEGMENTS = 100;
    private const int _MAX_LINE_SEGMENTS = 100;
    private const int _MAX_LINE_VERTICES = _MAX_LINE_SEGMENTS * 2;
    private const float _MIN_SEG_LEN = 0.2f;

    internal MeshRenderer _meshRenderer = null;

    private bool _didSetup = false;
    private GameObject _meshObject = new GameObject("debug_circle", typeof(MeshFilter), typeof(MeshRenderer));
    private Mesh _mesh = new();
    private Vector3[] _vertices;
    private Shape _shape = Shape.NONE;

    private void Awake()
    {
        this._meshObject.SetLayerRecursively(LayerMask.NameToLayer("Unoccluded"));
        this._meshObject.GetComponent<MeshFilter>().mesh = this._mesh;
        this._meshRenderer = this._meshObject.GetComponent<MeshRenderer>();
    }

    private void CreateMesh()
    {
        switch (this._shape)
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
            default:
                break;
        }

        Material mat = this._meshRenderer.material = BraveResources.Load("Global VFX/WhiteMaterial", ".mat") as Material;
        mat.shader = ShaderCache.Acquire("tk2d/BlendVertexColorAlphaTintableTilted");
        mat.SetColor("_OverrideColor", this.color);
    }

    // private static bool _PrintVertices = true;
    private static readonly Vector2 _WayOffscreen = new Vector2(1000f, 1000f);
    private void RebuildMeshes()
    {
        Vector3 basePos = this.pos;
        switch (this._shape)
        {
            case Shape.FILLEDCIRCLE:
                this._vertices[0] = basePos;
                for (int i = 0; i <= _CIRCLE_SEGMENTS; ++i)
                    this._vertices[i + 1] = basePos + (i * (360f / _CIRCLE_SEGMENTS)).ToVector3(this.radius);
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
            default:
                break;
        }
        this._mesh.vertices = this._vertices; // necessary to actually trigger an update for some reason
        this._mesh.RecalculateBounds();
        if (this._shape == Shape.FILLEDCIRCLE)
            this._mesh.RecalculateNormals();
    }

    public void Setup(Shape shape, Color? color = null, Vector2? pos = null, float? radius = null, float? angle = null, float? arc = null, Vector2? pos2 = null, float? radiusInner = null)
    {
        if (shape == Shape.NONE || (this._shape != Shape.NONE && this._shape != shape))
        {
            Lazy.DebugLog($"can't change shape of mesh!");
            return;
        }
        this._shape = shape;
        if (!this._didSetup)
            CreateMesh();

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
            this._meshRenderer.material.SetColor("_OverrideColor", this.color);
        if (!this._didSetup || pos.HasValue || pos2.HasValue || radius.HasValue || radiusInner.HasValue)
            RebuildMeshes();
        this._didSetup = true;
        this._meshRenderer.enabled = true;
    }

    private void OnDestroy()
    {
        if (this._meshObject)
            UnityEngine.Object.Destroy(this._meshObject);
    }
}
