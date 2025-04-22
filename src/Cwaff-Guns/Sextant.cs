namespace CwaffingTheGungy;

/* TODO:
    - charging should decrease spread from 90 to 0, then increase damage from 8 to instakill
    - should measure
      - aim angle
      - distance to target
      - aim variance
      - rebound angle
      - chance to hit target
      - chance to kill target
      - target hitbox size
    - all measurements should be iteratively drawn as gun is charged (dotted lines?)
*/

public class Sextant : CwaffGun
{
    public static string ItemName         = "Sextant";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    private dfLabel _shotAngleLabel = null;
    private dfLabel _shotDistanceLabel = null;
    private dfLabel _reboundAngleLabel = null;

    private Geometry _aimAngleArc = null;
    private Geometry _perfectShot = null;
    private Geometry _reboundShot = null;
    private Geometry _reboundArc = null;
    private Geometry _leftBaseSpread = null;
    private Geometry _rightBaseSpread = null;
    private Geometry _leftAdjSpread = null;
    private Geometry _rightAdjSpread = null;
    private Geometry _topBbox = null;
    private Geometry _bottomBbox = null;
    private Geometry _leftBbox = null;
    private Geometry _rightBbox = null;

    public static void Init()
    {
        Lazy.SetupGun<Sextant>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.C, gunClass: GunClass.PISTOL, reloadTime: 0.9f, ammo: 600, shootFps: 14, reloadFps: 4,
            muzzleFrom: Items.Mailbox, fireAudio: "paintball_shoot_sound", reloadAudio: "paintball_reload_sound");
    }

    private void Start()
    {
        this._perfectShot = new GameObject().AddComponent<Geometry>();
        this._reboundShot = new GameObject().AddComponent<Geometry>();
        this._reboundArc = new GameObject().AddComponent<Geometry>();
        this._leftBaseSpread = new GameObject().AddComponent<Geometry>();
        this._rightBaseSpread = new GameObject().AddComponent<Geometry>();
        this._leftAdjSpread = new GameObject().AddComponent<Geometry>();
        this._rightAdjSpread = new GameObject().AddComponent<Geometry>();
        this._aimAngleArc = new GameObject().AddComponent<Geometry>();
        this._topBbox = new GameObject().AddComponent<Geometry>();
        this._bottomBbox = new GameObject().AddComponent<Geometry>();
        this._leftBbox = new GameObject().AddComponent<Geometry>();
        this._rightBbox = new GameObject().AddComponent<Geometry>();

        this._shotAngleLabel = MakeNewLabel();
        this._shotDistanceLabel = MakeNewLabel();
        this._reboundAngleLabel = MakeNewLabel();
    }

    public override void Update()
    {
        const float MAG = 3f;

        base.Update();
        if (BraveTime.DeltaTime == 0.0f)
            return;
        if (this.PlayerOwner is not PlayerController pc)
            return;

        Gun gun = pc.CurrentGun;
        ProjectileModule mod = gun.DefaultModule;
        float accMult = pc.stats.GetStatValue(PlayerStats.StatType.Accuracy);
        Vector2 basePos = pc.sprite.WorldBottomCenter;
        Vector2 barrelPos = gun.barrelOffset.position + gun.gunAngle.EulerZ() * mod.positionOffset;

        float spread = mod.angleVariance;
        float baseShotAngle = (gun.gunAngle + gun.m_moduleData[mod].alternateAngleSign * mod.angleFromAim).Clamp360();
        this._aimAngleArc.Setup(Geometry.Shape.CIRCLE, Color.red, pos: barrelPos, radius: MAG, angle: baseShotAngle.Clamp360(), arc: 180f);

        // int rayMask = CollisionMask.LayerToMask(CollisionLayer.HighObstacle/*, CollisionLayer.BulletBlocker, CollisionLayer.BulletBreakable*/);
        int rayMask = CollisionMask.LayerToMask(CollisionLayer.HighObstacle, CollisionLayer.EnemyHitBox);
        RaycastResult result;
        float distanceToWall = 5f;
        Vector2 wallContact = Vector2.zero;
        Vector2 wallNormal = Vector2.zero;
        Vector2 shotVector = baseShotAngle.ToVector();

        bool hitWallOrBody = PhysicsEngine.Instance.Raycast(barrelPos, shotVector, 999f, out result, true, true, rayMask, null, false);
        if (hitWallOrBody && result.Normal != Vector2.zero)
        {
            distanceToWall = result.Distance;
            wallContact = result.Contact;
            wallNormal = result.Normal;
            SpeculativeRigidbody body = result.SpeculativeRigidbody;

            if (body)
            {
                PixelCollider coll = result.OtherPixelCollider;
                Rect bounds = new Rect(coll.UnitBottomLeft, coll.UnitDimensions).Inset(-0.5f);
                Vector2 tl = new Vector2(bounds.xMin, bounds.yMax);
                Vector2 bl = new Vector2(bounds.xMin, bounds.yMin);
                Vector2 tr = new Vector2(bounds.xMax, bounds.yMax);
                Vector2 br = new Vector2(bounds.xMax, bounds.yMin);
                this._topBbox.Setup(Geometry.Shape.DASHEDLINE, Color.yellow, pos: tl, pos2: tr);
                this._bottomBbox.Setup(Geometry.Shape.DASHEDLINE, Color.yellow, pos: bl, pos2: br);
                this._leftBbox.Setup(Geometry.Shape.DASHEDLINE, Color.yellow, pos: tl, pos2: bl);
                this._rightBbox.Setup(Geometry.Shape.DASHEDLINE, Color.yellow, pos: tr, pos2: br);
            }
            else // hit a wall
            {
                //NOTE: rotate our angle a bit and verify the wall normal matches. if it doesn't, rotate it again and use it as the tiebreaker
                //      this is an attempt to get around an annoying bug when hitting the corner of a wall
                if (PhysicsEngine.Instance.Raycast(barrelPos, shotVector.Rotate(1f), 999f, out result, true, false, rayMask, null, false))
                {
                    if (result.Normal != wallNormal)
                    {
                        if (PhysicsEngine.Instance.Raycast(barrelPos, shotVector.Rotate(-1f), 999f, out result, true, false, rayMask, null, false))
                            wallNormal = result.Normal;
                    }
                }
            }
        }
        RaycastResult.Pool.Free(ref result);

        this._perfectShot.Setup(Geometry.Shape.LINE, Color.white, pos: barrelPos, radius: distanceToWall, angle: baseShotAngle.Clamp360());
        this._shotDistanceLabel.Text = $"dx={Mathf.RoundToInt(C.PIXELS_PER_TILE * distanceToWall)}";
        this._shotDistanceLabel.Color = Color.white;
        PlaceLabel(this._shotDistanceLabel,
          barrelPos + baseShotAngle.ToVector(0.5f * distanceToWall) + (baseShotAngle - 90f).ToVector(0.5f), baseShotAngle);

        if (hitWallOrBody)
        {
            Vector2 reboundVector = baseShotAngle.ToVector();
            if (wallNormal.x != 0)
                reboundVector = reboundVector.WithX(-reboundVector.x);
            if (wallNormal.y != 0)
                reboundVector = reboundVector.WithY(-reboundVector.y);
            float reboundAngle = reboundVector.ToAngle();
            this._reboundShot.Setup(Geometry.Shape.LINE, Color.blue, pos: wallContact, radius: distanceToWall, angle: reboundAngle);
            float reboundArcDiameter = Mathf.Min(2f, 0.5f * distanceToWall);
            float reboundArcRadius = 0.5f * reboundArcDiameter;
            Vector2 reboundArcCenter = wallContact + reboundArcRadius * wallNormal;
            float reboundTheta = 2f * (baseShotAngle + 180f).AbsAngleTo(reboundAngle);
            this._reboundArc.Setup(Geometry.Shape.CIRCLE, Color.cyan, pos: reboundArcCenter, radius: reboundArcRadius,
              angle: wallNormal.ToAngle(), arc: reboundTheta);

            this._reboundAngleLabel.Text = $"{Mathf.RoundToInt(0.5f * reboundTheta)} deg";
            this._reboundAngleLabel.Color = Color.cyan;
            PlaceLabel(this._reboundAngleLabel, wallContact + (reboundArcDiameter + 0.125f) * wallNormal, wallNormal.ToAngle() - 90f);
        }

        // if (spread > 0f)
        // {
        //     this._leftBaseSpread.Setup(Geometry.Shape.LINE, Color.yellow, pos: barrelPos, radius: distanceToWall, angle: (baseShotAngle - spread).Clamp360());
        //     this._rightBaseSpread.Setup(Geometry.Shape.LINE, Color.yellow, pos: barrelPos, radius: distanceToWall, angle: (baseShotAngle + spread).Clamp360());
        // }

        // if (accMult != 1f)
        {
            this._leftAdjSpread.Setup(Geometry.Shape.DASHEDLINE, Color.green, pos: barrelPos, radius: distanceToWall, angle: (baseShotAngle - spread * accMult).Clamp360());
            this._rightAdjSpread.Setup(Geometry.Shape.DASHEDLINE, Color.green, pos: barrelPos, radius: distanceToWall, angle: (baseShotAngle + spread * accMult).Clamp360());
        }

        this._shotAngleLabel.Text = $"{Mathf.RoundToInt(pc.m_currentGunAngle.Clamp180())} deg";
        this._shotAngleLabel.Color = Color.red;
        PlaceLabel(this._shotAngleLabel, barrelPos + baseShotAngle.ToVector(MAG + 0.125f) + (baseShotAngle - 90f).ToVector(1.25f), baseShotAngle - 90f);
    }

    private static dfLabel MakeNewLabel()
    {
        dfLabel label = UnityEngine.Object.Instantiate(GameUIRoot.Instance.p_needsReloadLabel.gameObject, GameUIRoot.Instance.transform).GetComponent<dfLabel>();
        label.transform.localScale = Vector3.one / GameUIRoot.GameUIScalar;
        label.Anchor = dfAnchorStyle.CenterVertical | dfAnchorStyle.CenterHorizontal;
        label.TextAlignment = TextAlignment.Center;
        label.VerticalAlignment = dfVerticalAlignment.Middle;
        label.Opacity = 1f;
        label.Text = string.Empty;
        label.gameObject.SetActive(true);
        label.enabled = true;
        label.IsVisible = true;
        return label;
    }

    private static void PlaceLabel(dfLabel label, Vector2 pos, float rot)
    {
        rot = rot.Clamp180();
        Vector2 finalPos = pos;
        float uiScale = Pixelator.Instance.ScaleTileScale / Pixelator.Instance.CurrentTileScale; // 1.33, usually
        // System.Console.WriteLine($"ui scale is {uiScale}");
        float adj = label.PixelsToUnits() / uiScale; // PixelsToUnits() == 1 / 303.75 == 16/9 * 2/1080
        // System.Console.WriteLine($"pixels -> units = {label.PixelsToUnits()}");
        // System.Console.WriteLine($"units -> pixels = {1f / label.PixelsToUnits()}");
        if (Mathf.Abs(rot) > 90f)
        {
            rot = (rot + 180f).Clamp180();
            //NOTE: need to adjust position of bottom-aligned text
            //HACK: 0.5 seems to be the magic number for this font size here, idk how to arrive at this answer computationally though...
            //NOTE: label.Font.LineHeight == 40, label.TextScale == 0.6, label.Size.Y == 24, label.PixelsToUnits() == (1 / 303.75)
            //NOTE: df magic pixel scale = 1 / 64 == 1 / C.PIXELS_PER_CELL
            finalPos += (rot - 90f).ToVector(label.Size.y * uiScale / C.PIXELS_PER_CELL);  //WARN: guessing at the math here...
        }
        label.transform.position = dfFollowObject.ConvertWorldSpaces(
            finalPos,
            GameManager.Instance.MainCameraController.Camera,
            GameUIRoot.Instance.m_manager.RenderCamera).WithZ(0f);
        label.transform.position = label.transform.position.QuantizeFloor(adj);
        label.transform.localRotation = rot.EulerZ();
    }
}

public class Geometry : MonoBehaviour
{
    public enum Shape
    {
        NONE,
        FILLEDCIRCLE,
        CIRCLE,
        DASHEDLINE,
        LINE,
    }

    public Color color = default;
    public Vector2 pos = default;
    public float radius = 1f;
    public float angle = 0f;
    public float arc = 360f;

    private const int _CIRCLE_SEGMENTS = 100;
    private const int _MAX_LINE_SEGMENTS = 30;
    private const int _MAX_LINE_VERTICES = _MAX_LINE_SEGMENTS * 2;
    private const float _MIN_SEG_LEN = 0.2f;

    private bool _didSetup = false;
    private GameObject _meshObject = null;
    private Mesh _mesh = null;
    private MeshRenderer _meshRenderer = null;
    private Vector3[] _vertices;
    private Shape _shape = Shape.NONE;

    private void CreateMesh()
    {
        this._meshObject = new GameObject("debug_circle");
        this._meshObject.SetLayerRecursively(LayerMask.NameToLayer("FG_Critical"));

        this._mesh = new Mesh();

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

        this._meshObject.AddComponent<MeshFilter>().mesh = this._mesh;

        this._meshRenderer = this._meshObject.AddComponent<MeshRenderer>();
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
                if (maxVertexToDraw < 2)
                    break;
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

    public void Setup(Shape shape, Color? color = null, Vector2? pos = null, float? radius = null, float? angle = null, float? arc = null, Vector2? pos2 = null)
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
        }
        else
        {
            this.radius = radius ?? this.radius;
            this.angle  = angle  ?? this.angle;
            this.arc    = arc    ?? this.arc;
        }
        if (color.HasValue)
            this._meshRenderer.material.SetColor("_OverrideColor", this.color);
        if (!this._didSetup || pos.HasValue || radius.HasValue)
            RebuildMeshes();
        this._didSetup = true;
    }

    private void OnDestroy()
    {
        if (this._meshObject)
            UnityEngine.Object.Destroy(this._meshObject);
    }
}
