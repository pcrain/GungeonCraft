namespace CwaffingTheGungy;

// Version of TrailController that doesn't rely on rigid bodies or projectiles, only a sprite
public class SpriteTrailController : BraveBehaviour
{
  private class Bone
  {
    public float posX;

    public Vector2 normal;

    public bool IsAnimating;

    public float AnimationTimer;

    public readonly float HeightOffset;

    public bool Hide = false;

    public Vector2 pos { get; set; }

    public Bone(Vector2 pos, float posX, float heightOffset)
    {
      this.pos = pos;
      this.posX = posX;
      HeightOffset = heightOffset;
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

  [HideInInspector]
  public bool FlipUvsY;

  public bool rampHeight;

  public float rampStartHeight = 2f;

  public float rampTime = 1f;

  public Vector2 boneSpawnOffset;

  public bool UsesDispersalParticles;

  [ShowInInspectorIf("UsesDispersalParticles", false)]
  public float DispersalDensity = 3f;

  [ShowInInspectorIf("UsesDispersalParticles", false)]
  public float DispersalMinCoherency = 0.2f;

  [ShowInInspectorIf("UsesDispersalParticles", false)]
  public float DispersalMaxCoherency = 1f;

  [ShowInInspectorIf("UsesDispersalParticles", false)]
  public GameObject DispersalParticleSystemPrefab;

  private tk2dBaseSprite parent_sprite;

  private tk2dTiledSprite trail_sprite;

  private tk2dSpriteAnimationClip m_startAnimationClip;

  private tk2dSpriteAnimationClip m_animationClip;

  private int m_spriteSubtileWidth;

  private readonly LinkedList<Bone> m_bones = new LinkedList<Bone>();

  private ParticleSystem m_dispersalParticles;

  private Vector2 m_minBonePosition;

  private Vector2 m_maxBonePosition;

  private bool m_isDirty;

  private float m_globalTimer;

  private float m_rampTimer;

  private float m_maxPosX;

  private bool trailActive = true;

  private const int c_bonePixelLength = 4;

  private const float c_boneUnitLength = 0.25f;

  private const float c_trailHeightOffset = -0.5f;

  public void Start()
  {
    parent_sprite = base.transform.parent.gameObject.GetComponent<tk2dBaseSprite>();
    base.transform.parent = SpawnManager.Instance.VFX;
    base.transform.rotation = Quaternion.identity;
    base.transform.position = Vector3.zero;
    base.gameObject.SetLayerRecursively(LayerMask.NameToLayer("FG_Critical"));
    trail_sprite = GetComponent<tk2dTiledSprite>();
    trail_sprite.OverrideGetTiledSpriteGeomDesc = GetTiledSpriteGeomDesc;
    trail_sprite.OverrideSetTiledSpriteGeom = SetTiledSpriteGeom;
    tk2dSpriteDefinition currentSpriteDef = trail_sprite.GetCurrentSpriteDef();
    m_spriteSubtileWidth = Mathf.RoundToInt(currentSpriteDef.untrimmedBoundsDataExtents.x / currentSpriteDef.texelSize.x) / 4;
    float heightOffset = ((!rampHeight) ? 0f : rampStartHeight);
    m_bones.AddLast(new Bone(parent_sprite.WorldCenter + boneSpawnOffset, 0f, heightOffset));
    m_bones.AddLast(new Bone(parent_sprite.WorldCenter + boneSpawnOffset, 0f, heightOffset));

    if (usesStartAnimation)
      m_startAnimationClip = base.spriteAnimator.GetClipByName(startAnimation);
    if (usesAnimation)
      m_animationClip = base.spriteAnimator.GetClipByName(animation);
    if ((usesStartAnimation || usesAnimation) && usesCascadeTimer)
      m_bones.First.Value.IsAnimating = true;
    if (UsesDispersalParticles)
      m_dispersalParticles = GlobalDispersalParticleManager.GetSystemForPrefab(DispersalParticleSystemPrefab);
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
    m_rampTimer += BraveTime.DeltaTime;
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
            }
            flag = true;
            m_isDirty = true;
          }
        }
        if (linkedListNode != null && !linkedListNode.Value.IsAnimating)
        {
          if (usesGlobalTimer && m_globalTimer > globalTimer)
          {
            linkedListNode.Value.IsAnimating = true;
            linkedListNode.Value.AnimationTimer = m_globalTimer - globalTimer;
            DoDispersalParticles(linkedListNode, num2);
            m_isDirty = true;
          }
          if (usesCascadeTimer && (linkedListNode == m_bones.First || lastNodeAnimationTimer >= cascadeTimer))
          {
            linkedListNode.Value.IsAnimating = true;
            lastNodeAnimationTimer = 0f;
            DoDispersalParticles(linkedListNode, num2);
            m_isDirty = true;
          }
          if (usesSoftMaxLength && m_maxPosX - linkedListNode.Value.posX > softMaxLength)
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
    if (destroyOnEmpty && m_bones.Count == 0)
    {
      UnityEngine.Object.Destroy(base.gameObject);
    }
  }

  public void LateUpdate()
  {
    HandleExtension(parent_sprite.WorldCenter);
    UpdateIfDirty();
  }

  private void UpdateIfDirty()
  {
    if (!m_isDirty)
      return;

    m_minBonePosition = new Vector2(float.MaxValue, float.MaxValue);
    m_maxBonePosition = new Vector2(float.MinValue, float.MinValue);
    for (LinkedListNode<Bone> linkedListNode = m_bones.First; linkedListNode != null; linkedListNode = linkedListNode.Next)
    {
      m_minBonePosition = Vector2.Min(m_minBonePosition, linkedListNode.Value.pos);
      m_maxBonePosition = Vector2.Max(m_maxBonePosition, linkedListNode.Value.pos);
    }
    base.transform.position = new Vector3(m_minBonePosition.x, m_minBonePosition.y, m_minBonePosition.y + -0.5f);
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
      float heightOffset = ((!rampHeight) ? 0f : rampStartHeight);
      m_bones.AddLast(new Bone(parent_sprite.WorldCenter + boneSpawnOffset, m_maxPosX, heightOffset));
      m_bones.AddLast(new Bone(parent_sprite.WorldCenter + boneSpawnOffset, m_maxPosX, heightOffset));
    }
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
      float heightOffset = ((!rampHeight) ? 0f : Mathf.Lerp(rampStartHeight, 0f, m_rampTimer / rampTime));
      vector3.Normalize();
      while (num7 > 0f)
      {
        if (num7 < 0.25f)
        {
          m_bones.AddLast(new Bone(newPos, m_bones.Last.Value.posX + num7, heightOffset));
          break;
        }
        pos += vector3 * 0.25f;
        m_bones.AddLast(new Bone(pos, m_bones.Last.Value.posX + 0.25f, heightOffset));
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
    int num = Mathf.RoundToInt(spriteDef.untrimmedBoundsDataExtents.x / spriteDef.texelSize.x);
    int num2 = num / 4;
    int num3 = Mathf.Max(m_bones.Count - 1, 0);
    int num4 = Mathf.CeilToInt((float)num3 / (float)num2);
    boundsCenter = (m_minBonePosition + m_maxBonePosition) / 2f;
    boundsExtents = (m_maxBonePosition - m_minBonePosition) / 2f;
    LinkedListNode<Bone> linkedListNode = m_bones.First;
    int num5 = 0;
    for (int i = 0; i < num4; i++)
    {
      int num6 = 0;
      int num7 = num2 - 1;
      if (i == num4 - 1 && num3 % num2 != 0)
      {
        num7 = num3 % num2 - 1;
      }
      tk2dSpriteDefinition tk2dSpriteDefinition2 = spriteDef;
      if (usesStartAnimation && i == 0)
      {
        int num8 = Mathf.Clamp(Mathf.FloorToInt(linkedListNode.Value.AnimationTimer * m_startAnimationClip.fps), 0, m_startAnimationClip.frames.Length - 1);
        tk2dSpriteDefinition2 = trail_sprite.Collection.spriteDefinitions[m_startAnimationClip.frames[num8].spriteId];
      }
      else if (usesAnimation && linkedListNode.Value.IsAnimating)
      {
        int num9 = Mathf.Min((int)(linkedListNode.Value.AnimationTimer * m_animationClip.fps), m_animationClip.frames.Length - 1);
        tk2dSpriteDefinition2 = trail_sprite.Collection.spriteDefinitions[m_animationClip.frames[num9].spriteId];
      }
      float num10 = 0f;
      for (int j = num6; j <= num7; j++)
      {
        float num11 = 1f;
        if (i == num4 - 1 && j == num7)
        {
          num11 = Vector2.Distance(linkedListNode.Next.Value.pos, linkedListNode.Value.pos);
        }
        int num12 = offset + num5;
        pos[num12++] = linkedListNode.Value.pos + linkedListNode.Value.normal * tk2dSpriteDefinition2.position0.y - m_minBonePosition;
        pos[num12++] = linkedListNode.Next.Value.pos + linkedListNode.Next.Value.normal * tk2dSpriteDefinition2.position1.y - m_minBonePosition;
        pos[num12++] = linkedListNode.Value.pos + linkedListNode.Value.normal * tk2dSpriteDefinition2.position2.y - m_minBonePosition;
        pos[num12++] = linkedListNode.Next.Value.pos + linkedListNode.Next.Value.normal * tk2dSpriteDefinition2.position3.y - m_minBonePosition;
        num12 = offset + num5;
        pos[num12++] += new Vector3(0f, 0f, 0f - linkedListNode.Value.HeightOffset);
        pos[num12++] += new Vector3(0f, 0f, 0f - linkedListNode.Next.Value.HeightOffset);
        pos[num12++] += new Vector3(0f, 0f, 0f - linkedListNode.Value.HeightOffset);
        pos[num12++] += new Vector3(0f, 0f, 0f - linkedListNode.Next.Value.HeightOffset);
        Vector2 vector = Vector2.Lerp(tk2dSpriteDefinition2.uvs[0], tk2dSpriteDefinition2.uvs[1], num10);
        Vector2 vector2 = Vector2.Lerp(tk2dSpriteDefinition2.uvs[2], tk2dSpriteDefinition2.uvs[3], num10 + num11 / (float)num2);
        num12 = offset + num5;
        if (tk2dSpriteDefinition2.flipped == tk2dSpriteDefinition.FlipMode.Tk2d)
        {
          uv[num12++] = new Vector2(vector.x, vector.y);
          uv[num12++] = new Vector2(vector.x, vector2.y);
          uv[num12++] = new Vector2(vector2.x, vector.y);
          uv[num12++] = new Vector2(vector2.x, vector2.y);
        }
        else if (tk2dSpriteDefinition2.flipped == tk2dSpriteDefinition.FlipMode.TPackerCW)
        {
          uv[num12++] = new Vector2(vector.x, vector.y);
          uv[num12++] = new Vector2(vector2.x, vector.y);
          uv[num12++] = new Vector2(vector.x, vector2.y);
          uv[num12++] = new Vector2(vector2.x, vector2.y);
        }
        else
        {
          uv[num12++] = new Vector2(vector.x, vector.y);
          uv[num12++] = new Vector2(vector2.x, vector.y);
          uv[num12++] = new Vector2(vector.x, vector2.y);
          uv[num12++] = new Vector2(vector2.x, vector2.y);
        }
        if (linkedListNode.Value.Hide)
        {
          uv[num12 - 4] = Vector2.zero;
          uv[num12 - 3] = Vector2.zero;
          uv[num12 - 2] = Vector2.zero;
          uv[num12 - 1] = Vector2.zero;
        }
        if (FlipUvsY)
        {
          Vector2 vector3 = uv[num12 - 4];
          uv[num12 - 4] = uv[num12 - 2];
          uv[num12 - 2] = vector3;
          vector3 = uv[num12 - 3];
          uv[num12 - 3] = uv[num12 - 1];
          uv[num12 - 1] = vector3;
        }
        num5 += 4;
        num10 += num11 / (float)m_spriteSubtileWidth;
        if (linkedListNode != null)
        {
          linkedListNode = linkedListNode.Next;
        }
      }
    }
  }
}
