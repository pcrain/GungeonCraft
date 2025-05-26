namespace CwaffingTheGungy;

// Version of TrailController that doesn't rely on rigid bodies or projectiles and pools bones for memory efficiency
public class CwaffTrailController : BraveBehaviour
{
  private class Bone
  {
    public float posX;
    public Vector2 normal;
    public bool IsAnimating;
    public float AnimationTimer;
    public bool Hide;
    public Vector2 pos;

    private static int _BonesCreated = 0;
    private static readonly LinkedList<Bone> _BonePool = new();

    internal static LinkedListNode<Bone> Rent(Vector2 pos, float posX)
    {
      if (_BonePool.Count == 0)
        _BonePool.AddLast(new Bone());

      LinkedListNode<Bone> node = _BonePool.Last;
      _BonePool.RemoveLast();

      Bone bone = node.Value;
      bone.pos            = pos;
      bone.posX           = posX;

      bone.normal         = default;
      bone.IsAnimating    = default;
      bone.AnimationTimer = default;
      bone.Hide           = default;

      return node;
    }

    internal static void Return(LinkedListNode<Bone> bone)
    {
      _BonePool.AddLast(bone);
      // System.Console.WriteLine($"returned {_BonePool.Count}/{_BonesCreated} bones");
    }

    private Bone() // can only be created by Rent
    {
      ++_BonesCreated;
    }
  }

  public bool usesStartAnimation;

  public string startAnimation;

  public bool usesAnimation;

  public string animation;

  [TogglesProperty("cascadeTimer", "Cascade Timer")]
  public bool usesCascadeTimer;

  [HideInInspector]
  public float cascadeTimer;

  [TogglesProperty("softMaxLength", "Soft Max Length")]
  public bool usesSoftMaxLength;

  [HideInInspector]
  public float softMaxLength;

  [TogglesProperty("globalTimer", "Global Timer")]
  public bool usesGlobalTimer;

  [HideInInspector]
  public float globalTimer;

  public bool destroyOnEmpty = true;

  public bool awaitAllTimers = false; // if true, await for both global and cascade timers

  public Vector2 boneSpawnOffset;

  public bool useBody = true; // if true, use position of SpeculativeRigidBody if available (false == use sprite)

  public bool UsesDispersalParticles;

  [ShowInInspectorIf("UsesDispersalParticles", false)]
  public float DispersalDensity = 3f;

  [ShowInInspectorIf("UsesDispersalParticles", false)]
  public float DispersalMinCoherency = 0.2f;

  [ShowInInspectorIf("UsesDispersalParticles", false)]
  public float DispersalMaxCoherency = 1f;

  [ShowInInspectorIf("UsesDispersalParticles", false)]
  public GameObject DispersalParticleSystemPrefab;

  public bool converted = false; // whether we were converted from a vanilla TrailController

  private SpeculativeRigidbody body;

  private tk2dBaseSprite parent_sprite;

  private tk2dTiledSprite trail_sprite;

  private tk2dSpriteAnimationClip m_startAnimationClip;

  private tk2dSpriteAnimationClip m_animationClip;

  private float m_projectileScale = 1f;

  private int m_spriteSubtileWidth;

  private readonly LinkedList<Bone> m_bones = new LinkedList<Bone>();

  private ParticleSystem m_dispersalParticles;

  private Vector2 m_minBonePosition;

  private Vector2 m_maxBonePosition;

  private bool m_isDirty;

  private float m_globalTimer;

  private float m_maxPosX;

  private bool trailActive = true;

  private bool disconnected = false;

  private bool setup = false;

  private const int c_bonePixelLength = 4;

  private const float c_boneUnitLength = 0.25f;

  private const float c_trailHeightOffset = -0.5f;

  private Vector2 TruePosition
  {
    get
    {
      if (this.body)
        return this.body.Position.UnitPosition;
      if (parent_sprite)
        return parent_sprite.WorldCenter;
      return base.transform.position;
    }
  }

  /// <summary>Spawn a standalone disconnected trail between two points.</summary>
  public static void Spawn(CwaffTrailController prefab, Vector2 start, Vector2 end)
  {
    CwaffTrailController ctc = UnityEngine.Object.Instantiate(prefab.gameObject, start, (end - start).EulerZ()).GetComponent<CwaffTrailController>();
    ctc.transform.localScale = new Vector3(1f, 1f, 1f);
    ctc.Setup();
    ctc.HandleExtension(end);
    ctc.disconnected = true;
    ctc.destroyOnEmpty = true;
  }

  /// <summary>Convert a vanilla TrailController to a CwaffTrailController.</summary>
  public static CwaffTrailController Convert(TrailController tc)
  {
    CwaffTrailController ctc = tc.gameObject.ClonePrefab().AddComponent<CwaffTrailController>();
    UnityEngine.Object.Destroy(ctc.gameObject.GetComponent<TrailController>());  // remove TrailController component from original prefab

    ctc.usesStartAnimation            = tc.usesStartAnimation;
    ctc.startAnimation                = tc.startAnimation;
    ctc.usesAnimation                 = tc.usesAnimation;
    ctc.animation                     = tc.animation;
    ctc.usesCascadeTimer              = tc.usesCascadeTimer;
    ctc.cascadeTimer                  = tc.cascadeTimer;
    ctc.usesSoftMaxLength             = tc.usesSoftMaxLength;
    ctc.softMaxLength                 = tc.softMaxLength;
    ctc.usesGlobalTimer               = tc.usesGlobalTimer;
    ctc.globalTimer                   = tc.globalTimer;
    ctc.destroyOnEmpty                = tc.destroyOnEmpty;
    ctc.boneSpawnOffset               = tc.boneSpawnOffset;
    ctc.UsesDispersalParticles        = tc.UsesDispersalParticles;
    ctc.DispersalDensity              = tc.DispersalDensity;
    ctc.DispersalMinCoherency         = tc.DispersalMinCoherency;
    ctc.DispersalMaxCoherency         = tc.DispersalMaxCoherency;
    ctc.DispersalParticleSystemPrefab = tc.DispersalParticleSystemPrefab;

    ctc.converted = true;
    return ctc;
  }

  public void Start()
  {
    Setup();
  }

  private void Setup()
  {
    if (setup)
      return;

    if (base.transform.parent is Transform parentTransform)
    {
      if (this.useBody)
      {
        this.body = parentTransform.gameObject.GetComponent<SpeculativeRigidbody>();
        if (this.body)
          this.body.Initialize();
      }
      parent_sprite = parentTransform.gameObject.GetComponent<tk2dBaseSprite>();
      base.transform.position = Vector3.zero; // only set if we have a parent
      base.transform.rotation = Quaternion.identity;
    }
    // base.transform.parent = SpawnManager.Instance.VFX; //NOTE: need to be parented to our projectiles so we get destroyed properly
    if (this.body && this.body.projectile && this.body.projectile.Owner is PlayerController pc)
    {
      m_projectileScale = pc.BulletScaleModifier;
      this.body.projectile.OnDestruction += this.OnProjectileDestruction;
    }
    base.gameObject.SetLayerRecursively(LayerMask.NameToLayer("FG_Critical"));
    trail_sprite = GetComponent<tk2dTiledSprite>();
    trail_sprite.OverrideGetTiledSpriteGeomDesc = GetTiledSpriteGeomDesc;
    trail_sprite.OverrideSetTiledSpriteGeom = SetTiledSpriteGeom;
    tk2dSpriteDefinition currentSpriteDef = trail_sprite.GetCurrentSpriteDef();
    m_spriteSubtileWidth = Mathf.RoundToInt(currentSpriteDef.untrimmedBoundsDataExtents.x / currentSpriteDef.texelSize.x) / 4;
    m_bones.AddLast(Bone.Rent(TruePosition + boneSpawnOffset, 0f));
    m_bones.AddLast(Bone.Rent(TruePosition + boneSpawnOffset, 0f));

    if (usesStartAnimation)
      m_startAnimationClip = base.spriteAnimator.GetClipByName(startAnimation);
    if (usesAnimation)
      m_animationClip = base.spriteAnimator.GetClipByName(animation);
    if ((usesStartAnimation || usesAnimation) && usesCascadeTimer)
      m_bones.First.Value.IsAnimating = true;
    if (UsesDispersalParticles)
      m_dispersalParticles = GlobalDispersalParticleManager.GetSystemForPrefab(DispersalParticleSystemPrefab);
    if (this.body)
    {
      this.body.OnCollision += this.UpdateOnCollision;
      this.body.OnPostRigidbodyMovement += this.PostRigidbodyMovement;
    }
    this.setup = true;
  }

  private void OnProjectileDestruction(Projectile obj)
  {
      DisconnectFromSpecRigidbody();
  }

  public void Toggle(bool enable = true)
  {
    this.trailActive = enable;
  }

  public void Update()
  {
    int num = Mathf.RoundToInt(trail_sprite.GetCurrentSpriteDef().untrimmedBoundsDataExtents.x / trail_sprite.GetCurrentSpriteDef().texelSize.x);
    int num2 = num / 4;
    m_globalTimer += BraveTime.DeltaTime;
    if (usesAnimation)
    {
      LinkedListNode<Bone> linkedListNode = m_bones.First;
      float lastNodeAnimationTimer = 0f;
      while (linkedListNode != null)
      {
        bool flag = false;
        if (linkedListNode.Value.IsAnimating)
        {
          tk2dSpriteAnimationClip tk2dSpriteAnimationClip2 = ((!usesStartAnimation || linkedListNode != m_bones.First) ? m_animationClip : m_startAnimationClip);
          linkedListNode.Value.AnimationTimer += BraveTime.DeltaTime;
          lastNodeAnimationTimer = linkedListNode.Value.AnimationTimer;
          int num4 = Mathf.FloorToInt((linkedListNode.Value.AnimationTimer - BraveTime.DeltaTime) * tk2dSpriteAnimationClip2.fps);
          int num5 = Mathf.FloorToInt(linkedListNode.Value.AnimationTimer * tk2dSpriteAnimationClip2.fps);
          if (num5 != num4)
          {
            m_isDirty = true;
          }
          if (linkedListNode.Value.AnimationTimer > (float)tk2dSpriteAnimationClip2.frames.Length / tk2dSpriteAnimationClip2.fps)
          {
            if (usesStartAnimation && linkedListNode == m_bones.First)
            {
              usesStartAnimation = false;
            }
            for (int i = 0; i < num2; i++)
            {
              if (linkedListNode == null)
              {
                break;
              }
              LinkedListNode<Bone> node = linkedListNode;
              linkedListNode = linkedListNode.Next;
              m_bones.Remove(node);
              Bone.Return(node);
            }
            flag = true;
            m_isDirty = true;
          }
        }
        if (linkedListNode != null && !linkedListNode.Value.IsAnimating)
        {
          bool allReady = usesGlobalTimer || usesCascadeTimer || usesSoftMaxLength;
          bool anyReady = false;
          if (usesGlobalTimer)
          {
            allReady &= (m_globalTimer > globalTimer);
            anyReady |= (m_globalTimer > globalTimer);
          }
          if (usesCascadeTimer)
          {
            allReady &= (linkedListNode == m_bones.First || lastNodeAnimationTimer >= cascadeTimer);
            anyReady |= (linkedListNode == m_bones.First || lastNodeAnimationTimer >= cascadeTimer);
          }
          if (usesSoftMaxLength)
          {
            allReady &= (m_maxPosX - linkedListNode.Value.posX > softMaxLength);
            anyReady |= (m_maxPosX - linkedListNode.Value.posX > softMaxLength);
          }
          if (awaitAllTimers ? allReady : anyReady)
          {
            linkedListNode.Value.IsAnimating = true;
            lastNodeAnimationTimer = 0f;
            DoDispersalParticles(linkedListNode, num2);
            m_isDirty = true;
          }
        }
        if (flag || linkedListNode == null)
        {
          continue;
        }
        for (int j = 0; j < num2; j++)
        {
          if (linkedListNode == null)
          {
            break;
          }
          linkedListNode = linkedListNode.Next;
        }
      }
    }
    if (destroyOnEmpty && m_bones.Count == 0) //NOTE: patched from original method to keep alive even when we have no bones
    {
      UnityEngine.Object.Destroy(base.gameObject);
    }
  }

  public void LateUpdate()
  {
    base.transform.rotation = Quaternion.identity; //NOTE: if we're parented to a projectile, prevent our bones from rotating
    if (!this.body && !this.disconnected)
      HandleExtension(TruePosition); //NOTE: this will never get called otherwise if we don't have a rigid body
    UpdateIfDirty();
  }

  public override void OnDestroy()
  {
    if (this.body)
    {
      this.body.OnCollision -= UpdateOnCollision;
      this.body.OnPostRigidbodyMovement -= PostRigidbodyMovement;
      if (this.body.projectile)
        this.body.projectile.OnDestruction -= this.OnProjectileDestruction;
    }
    while (m_bones.Count > 0)
    {
      LinkedListNode<Bone> node = m_bones.Last;
      m_bones.RemoveLast();
      Bone.Return(node);
    }
    base.OnDestroy();
  }

  public void DisconnectFromSpecRigidbody()
  {
    if (!this.body)
      return;
    this.body.OnCollision -= UpdateOnCollision;
    this.body.OnPostRigidbodyMovement -= PostRigidbodyMovement;
    this.body = null;
    base.transform.parent = null; // make sure we don't keep transforming with whatever rigid body we were attached to
    this.destroyOnEmpty = true; // make sure our bones get returned to the pool
    this.disconnected = true; // don't call HandleExtension() any more
  }

  private void UpdateOnCollision(CollisionData obj)
  {
    Vector2 specRigidbodyPosition = this.body.Position.UnitPosition + PhysicsEngine.PixelToUnit(obj.NewPixelsToMove);
    HandleExtension(specRigidbodyPosition);
    m_bones.Last.Value.Hide = true;
    m_bones.AddLast(Bone.Rent(m_bones.Last.Value.pos, m_bones.Last.Value.posX));
    m_bones.AddLast(Bone.Rent(m_bones.Last.Value.pos, m_bones.Last.Value.posX));
  }

  private void PostRigidbodyMovement(SpeculativeRigidbody rigidbody, Vector2 unitDelta, IntVector2 pixelDelta)
  {
    HandleExtension(this.body.Position.UnitPosition);
    UpdateIfDirty();
  }

  private void UpdateIfDirty()
  {
    if (!m_isDirty)
      return;

    float minX = float.MaxValue;
    float maxX = float.MinValue;
    float minY = float.MaxValue;
    float maxY = float.MinValue;
    for (LinkedListNode<Bone> linkedListNode = m_bones.First; linkedListNode != null; linkedListNode = linkedListNode.Next)
    {
      Vector2 pos = linkedListNode.Value.pos;
      if (pos.x < minX) minX = pos.x;
      if (pos.x > maxX) maxX = pos.x;
      if (pos.y < minY) minY = pos.y;
      if (pos.y > maxY) maxY = pos.y;
    }
    m_minBonePosition = new Vector2(minX, minY);
    m_maxBonePosition = new Vector2(maxX, maxY);
    base.transform.position = new Vector3(minX, minY, minY - 0.5f);
    trail_sprite.ForceBuild();
    trail_sprite.UpdateZDepth();
    m_isDirty = false;
  }

  private void HandleExtension(Vector2 toPosition)
  {
    if (!this.trailActive)
      return;

    if (!destroyOnEmpty && m_bones.Count == 0)
    {
      m_bones.AddLast(Bone.Rent(TruePosition + boneSpawnOffset, m_maxPosX));
      m_bones.AddLast(Bone.Rent(TruePosition + boneSpawnOffset, m_maxPosX));
    }
    if (this.body && this.body.projectile && this.body.projectile.OverrideTrailPoint.HasValue)
      toPosition = this.body.projectile.OverrideTrailPoint.Value;
    ExtendBonesTo(toPosition + boneSpawnOffset);
    m_isDirty = true;
  }

  private void ExtendBonesTo(Vector2 newPos)
  {
    if (m_bones == null || m_bones.Last == null || m_bones.Last.Value == null || m_bones.Last.Previous == null || m_bones.Last.Previous.Value == null)
    {
      return;
    }
    Vector2 vector = newPos - m_bones.Last.Value.pos;
    Vector2 v = m_bones.Last.Value.pos - m_bones.Last.Previous.Value.pos;
    float magnitude = v.magnitude;
    LinkedListNode<Bone> previous = m_bones.Last.Previous;
    float num = Vector3.Distance(newPos, m_bones.Last.Previous.Value.pos);
    if (num < 0.25f) // minimum move distance to create a trail
    {
      m_bones.Last.Value.pos = newPos;
      m_bones.Last.Value.posX = m_bones.Last.Previous.Value.posX + num;
    }
    else
    {
      if (Mathf.Approximately(magnitude, 0f))
      {
        m_bones.Last.Value.pos = m_bones.Last.Previous.Value.pos + vector.normalized * 0.25f;
        m_bones.Last.Value.posX = m_bones.Last.Previous.Value.posX + 0.25f;
      }
      else
      {
        float num2 = 0.25f;
        float num3 = magnitude;
        float f = BraveMathCollege.ClampAnglePi(Mathf.Atan2(vector.y, vector.x) - Mathf.Atan2(0f - v.y, 0f - v.x));
        float num4 = Mathf.Abs(f);
        float num5 = Mathf.Asin(num3 / num2 * Mathf.Sin(num4));
        float num6 = (float)Math.PI - num5 - num4;
        Vector2 vector2 = v.Rotate(Mathf.Sign(f) * (0f - num6) * 57.29578f);
        m_bones.Last.Value.pos = m_bones.Last.Previous.Value.pos + vector2.normalized * 0.25f;
        m_bones.Last.Value.posX = m_bones.Last.Previous.Value.posX + 0.25f;
      }
      Vector2 pos = m_bones.Last.Value.pos;
      Vector2 vector3 = newPos - pos;
      float num7 = vector3.magnitude;
      vector3.Normalize();
      while (num7 > 0f)
      {
        if (num7 < 0.25f)
        {
          m_bones.AddLast(Bone.Rent(newPos, m_bones.Last.Value.posX + num7));
          break;
        }
        pos += vector3 * 0.25f;
        m_bones.AddLast(Bone.Rent(pos, m_bones.Last.Value.posX + 0.25f));
        num7 -= 0.25f;
        if (usesGlobalTimer && m_globalTimer > globalTimer)
        {
          m_bones.Last.Value.AnimationTimer = m_globalTimer - globalTimer;
        }
      }
    }
    m_maxPosX = m_bones.Last.Value.posX;
    LinkedListNode<Bone> linkedListNode = previous;
    while (linkedListNode != null && linkedListNode.Next != null)
    {
      linkedListNode.Value.normal = (Quaternion.Euler(0f, 0f, 90f) * (linkedListNode.Next.Value.pos - linkedListNode.Value.pos)).normalized;
      linkedListNode = linkedListNode.Next;
    }
    m_bones.Last.Value.normal = m_bones.Last.Previous.Value.normal;
    m_isDirty = true;
  }

  private void DoDispersalParticles(LinkedListNode<Bone> boneNode, int subtilesPerTile)
  {
    if (!UsesDispersalParticles || boneNode.Value == null || boneNode.Next == null || boneNode.Next.Value == null)
    {
      return;
    }
    Vector3 vector = boneNode.Value.pos.ToVector3ZUp(boneNode.Value.pos.y);
    LinkedListNode<Bone> linkedListNode = boneNode;
    for (int i = 0; i < subtilesPerTile; i++)
    {
      if (linkedListNode.Next == null)
      {
        break;
      }
      linkedListNode = linkedListNode.Next;
    }
    Vector3 vector2 = linkedListNode.Value.pos.ToVector3ZUp(linkedListNode.Value.pos.y);
    int num = Mathf.Max(Mathf.CeilToInt(Vector2.Distance(vector.XY(), vector2.XY()) * DispersalDensity), 1);
    for (int j = 0; j < num; j++)
    {
      float t = (float)j / (float)num;
      Vector3 position = Vector3.Lerp(vector, vector2, t);
      position += Vector3.back;
      float num2 = Mathf.PerlinNoise(position.x / 3f, position.y / 3f);
      Vector3 a = Quaternion.Euler(0f, 0f, num2 * 360f) * Vector3.right;
      Vector3 vector3 = Vector3.Lerp(a, UnityEngine.Random.insideUnitSphere, UnityEngine.Random.Range(DispersalMinCoherency, DispersalMaxCoherency));
      ParticleSystem.EmitParams emitParams = default(ParticleSystem.EmitParams);
      #pragma warning disable 0618 //disable deprecation warnings for a bit
      emitParams.position = position;
      emitParams.velocity = vector3 * m_dispersalParticles.startSpeed;
      emitParams.startSize = m_dispersalParticles.startSize;
      emitParams.startLifetime = m_dispersalParticles.startLifetime;
      emitParams.startColor = m_dispersalParticles.startColor;
      #pragma warning restore 0618
      ParticleSystem.EmitParams emitParams2 = emitParams;
      m_dispersalParticles.Emit(emitParams2, 1);
    }
  }

  public void GetTiledSpriteGeomDesc(out int numVertices, out int numIndices, tk2dSpriteDefinition spriteDef, Vector2 dimensions)
  {
    int num = Mathf.Max(m_bones.Count - 1, 0);
    numVertices = num * 4;
    numIndices = num * 6;
  }

  public void SetTiledSpriteGeom(Vector3[] pos, Vector2[] uv, int offset, out Vector3 boundsCenter, out Vector3 boundsExtents, tk2dSpriteDefinition spriteDef, Vector3 scale, Vector2 dimensions, tk2dBaseSprite.Anchor anchor, float colliderOffsetZ, float colliderExtentZ)
  {
    int trailPixelLength = Mathf.RoundToInt(spriteDef.untrimmedBoundsDataExtents.x / spriteDef.texelSize.x);
    int num2 = trailPixelLength / 4;
    int lastBoneIndex = Mathf.Max(m_bones.Count - 1, 0);
    int num4 = Mathf.CeilToInt((float)lastBoneIndex / (float)num2);
    boundsCenter = (m_minBonePosition + m_maxBonePosition) / 2f;
    boundsExtents = (m_maxBonePosition - m_minBonePosition) / 2f;
    LinkedListNode<Bone> linkedListNode = m_bones.First;
    int uvIndex = offset;
    tk2dSpriteDefinition[] defs = trail_sprite.Collection.spriteDefinitions;
    float invSubtile =  1f / (float)m_spriteSubtileWidth;
    for (int i = 0; i < num4; i++)
    {
      int num7 = num2 - 1;
      if (i == num4 - 1 && lastBoneIndex % num2 != 0)
        num7 = lastBoneIndex % num2 - 1;
      tk2dSpriteDefinition segmentSprite = spriteDef;
      Bone bone = linkedListNode.Value;
      if (usesStartAnimation && i == 0)
      {
        int startAnimationFrame = Mathf.Clamp(Mathf.FloorToInt(bone.AnimationTimer * m_startAnimationClip.fps), 0, m_startAnimationClip.frames.Length - 1);
        segmentSprite = defs[m_startAnimationClip.frames[startAnimationFrame].spriteId];
      }
      else if (usesAnimation && bone.IsAnimating)
      {
        int animationFrame = Mathf.Min((int)(bone.AnimationTimer * m_animationClip.fps), m_animationClip.frames.Length - 1);
        segmentSprite = defs[m_animationClip.frames[animationFrame].spriteId];
      }
      float num10 = 0f;
      float ymin = segmentSprite.position0.y * m_projectileScale;
      float ymax = segmentSprite.position3.y * m_projectileScale;
      for (int j = 0; j <= num7; j++)
      {
        Bone nextBone = linkedListNode.Next.Value;
        float num11 = 1f;
        if (i == num4 - 1 && j == num7)
          num11 = Vector2.Distance(nextBone.pos, bone.pos);
        //NOTE: we can safely reuse positional indices from previous bones if we know the sprite hasn't changed
        pos[uvIndex]     = (j > 0) ? pos[uvIndex - 3] : bone.pos + (bone.normal * ymin) - m_minBonePosition;
        pos[uvIndex + 1] = nextBone.pos + (nextBone.normal * ymin) - m_minBonePosition;
        pos[uvIndex + 2] = (j > 0) ? pos[uvIndex - 1] : bone.pos + (bone.normal * ymax) - m_minBonePosition;
        pos[uvIndex + 3] = nextBone.pos + (nextBone.normal * ymax) - m_minBonePosition;
        Vector2 vector = Vector2.Lerp(segmentSprite.uvs[0], segmentSprite.uvs[1], num10);
        Vector2 vector2 = Vector2.Lerp(segmentSprite.uvs[2], segmentSprite.uvs[3], num10 + num11 / (float)num2);

        if (bone.Hide)
        {
          uv[uvIndex    ] = Vector2.zero;
          uv[uvIndex + 1] = Vector2.zero;
          uv[uvIndex + 2] = Vector2.zero;
          uv[uvIndex + 3] = Vector2.zero;
        }
        else if (!converted || segmentSprite.flipped == tk2dSpriteDefinition.FlipMode.None)
        { //NOTE: we don't use tk2d flip modes, so only worry about the other flip cases for converted TrailControllers
          uv[uvIndex    ] = new Vector2(vector.x, vector.y);
          uv[uvIndex + 1] = new Vector2(vector2.x, vector.y);
          uv[uvIndex + 2] = new Vector2(vector.x, vector2.y);
          uv[uvIndex + 3] = new Vector2(vector2.x, vector2.y);
        }
        else if (segmentSprite.flipped == tk2dSpriteDefinition.FlipMode.Tk2d)
        {
          uv[uvIndex    ] = new Vector2(vector.x, vector.y);
          uv[uvIndex + 1] = new Vector2(vector.x, vector2.y);
          uv[uvIndex + 2] = new Vector2(vector2.x, vector.y);
          uv[uvIndex + 3] = new Vector2(vector2.x, vector2.y);
        }
        else if (segmentSprite.flipped == tk2dSpriteDefinition.FlipMode.TPackerCW)
        {
          uv[uvIndex    ] =  new Vector2(vector.x, vector.y);
          uv[uvIndex + 1] =  new Vector2(vector2.x, vector.y);
          uv[uvIndex + 2] =  new Vector2(vector.x, vector2.y);
          uv[uvIndex + 3] =  new Vector2(vector2.x, vector2.y);
        }

        uvIndex += 4;
        num10 += invSubtile;
        linkedListNode = linkedListNode.Next;
        bone = nextBone;
      }
    }
  }
}
