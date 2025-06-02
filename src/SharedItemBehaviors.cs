namespace CwaffingTheGungy;

// invisible, collisionless projectile used for calculating other objects as if they were projectiles
public class FakeProjectileComponent : MonoBehaviour
{
    // dummy component
    private void Start()
    {
        Projectile p = base.GetComponent<Projectile>();
        p.sprite.renderer.enabled = false;
        p.damageTypes &= (~CoreDamageTypes.Electric);
        p.collidesWithEnemies = false;
        p.collidesWithProjectiles = false;
    }
}

/// <summary>Kill a projectile after a fixed amount of time</summary>
public class ProjectileExpiration : MonoBehaviour
{
    public float expirationTimer = 1f;

    private void Start()
    {
        if (expirationTimer > 0)
        {
            StartCoroutine(Expire(expirationTimer));
            return;
        }
        if (base.gameObject.GetComponent<Projectile>() is Projectile p)
            p.DieInAir(true,false,false,true);
    }

    private IEnumerator Expire(float expirationTimer)
    {
        yield return new WaitForSeconds(expirationTimer);
        if (base.gameObject.GetComponent<Projectile>() is Projectile p)
            p.DieInAir(true,false,false,true);
    }
}

  /// <summary>destroy a game object after a fixed amount of time, with optional fadeout</summary>
  public class Expiration : MonoBehaviour
  {
    public void ExpireIn(float seconds, float fadeFor = 0f, float startAlpha = 1f, bool shrink = false)
    {
      this.StartCoroutine(Expire(seconds, fadeFor, startAlpha, shrink));
    }

    private IEnumerator Expire(float seconds, float fadeFor = 0f, float startAlpha = 1f, bool shrink = false)
    {
      if (startAlpha < 1f)
        this.gameObject.SetAlphaImmediate(startAlpha);
      float startXScale = this.gameObject.transform.localScale.x;
      float startYScale = this.gameObject.transform.localScale.y;
      if (fadeFor == 0f)
      {
        yield return new WaitForSeconds(seconds);
        UnityEngine.Object.Destroy(this.gameObject);
        yield break;
      }

      float lifeLeft = seconds;
      while (lifeLeft > 0)
      {
        lifeLeft -= BraveTime.DeltaTime;
        float percentAlive = Mathf.Min(1f,lifeLeft / fadeFor);
        this.gameObject.SetAlpha(startAlpha * percentAlive);
        if (shrink)
        {
          this.gameObject.transform.localScale = new Vector3(percentAlive * startXScale, percentAlive * startYScale, 1.0f);
        }
        yield return null;
      }
      UnityEngine.Object.Destroy(this.gameObject);
      yield break;
    }
  }

/// <summary>Class for fake items that don't show up in inventory or ammonomicon, but can persistently update and get serialized during midgame saves</summary>
public class FakeItem : CwaffPassive
{
    public static void Create<T>() where T : FakeItem
    {
        T fake                            = Lazy.SetupFakeItem<T>();
        _Prefabs[typeof(T)]               = fake;
        _PrefabsById[fake.PickupObjectId] = fake;
    }

    public static FakeItem Get<T>() where T : FakeItem => _Prefabs[typeof(T)];
    public static FakeItem Get(int id) => _PrefabsById[id];
    private static Dictionary<Type, FakeItem> _Prefabs     = new();
    private static Dictionary<int, FakeItem>  _PrefabsById = new();
}

public class BulletLifeTimer : MonoBehaviour
{
    public BulletLifeTimer()
    {
        this.secondsTillDeath = 1;
        this.eraseInsteadOfDie = false;
    }
    private void Start()
    {
        timer = secondsTillDeath;
        this.m_projectile = base.GetComponent<Projectile>();

    }
    private void FixedUpdate()
    {
        if (this.m_projectile != null)
        {
            if (timer > 0)
            {
                timer -= BraveTime.DeltaTime;
            }
            if (timer <= 0)
            {
                if (eraseInsteadOfDie) UnityEngine.Object.Destroy(this.m_projectile.gameObject);
                else this.m_projectile.DieInAir();
            }
        }
    }
    public float secondsTillDeath;
    public bool eraseInsteadOfDie;
    private float timer;
    private Projectile m_projectile;
}

public class ArcTowardsTargetBehavior : MonoBehaviour
{
    // Computed
    private Projectile _projectile         = null;
    private PlayerController _owner        = null;
    private float _lifetime                = 0;
    private Vector2 _targetPos             = Vector2.zero;
    private float _targetAngle             = 0;
    private float _actualTimeToReachTarget = 0;
    private float _arcSpeed                = 0.0f;
    private bool  _hasBeenSetup            = false;

    // Manualy Set
    private float _arcAngle                = 0;
    private float _maxSecsToReachTarget    = 3.0f;
    private float _minSpeed                = 5.0f;

    public void Setup(float arcAngle, float maxSecsToReachTarget, float minSpeed)
    {
        this._projectile = base.GetComponent<Projectile>();
        if (this._projectile.Owner is not PlayerController pc)
            return;
        this._owner = pc;

        this._targetAngle  = this._projectile.Direction.ToAngle();
        this._targetPos    = Raycast.ToNearestWallOrEnemyOrObject(this._owner.CenterPosition, this._targetAngle);

        this._arcAngle = arcAngle;
        this._maxSecsToReachTarget = maxSecsToReachTarget;
        this._minSpeed = minSpeed;

        SetNewTarget(this._targetPos);
        this._hasBeenSetup = true;
    }

    public void SetNewTarget(Vector2 target)
    {
        this._lifetime = 0;
        this._targetPos = target;
        Vector2 curpos = this._projectile.specRigidbody.Position.GetPixelVector2();
        Vector2 delta  = (this._targetPos-curpos);
        this._targetAngle = delta.ToAngle();
        float distanceToTarget = Vector2.Distance(curpos, this._targetPos);
        this._projectile.SetSpeed(Mathf.Max(distanceToTarget / _maxSecsToReachTarget, _minSpeed));
        this._actualTimeToReachTarget = distanceToTarget / this._projectile.baseData.speed;
        this._arcSpeed = 2f * this._arcAngle / this._actualTimeToReachTarget;
        this._projectile.SendInDirection(BraveMathCollege.DegreesToVector(this._targetAngle-this._arcAngle), true);
    }

    private void Update()
    {
        if (!_hasBeenSetup)
            return;

        this._lifetime += BraveTime.DeltaTime;
        float percentDoneTurning = this._lifetime / this._actualTimeToReachTarget;
        if (percentDoneTurning > 1.0f)
            return;

        float oldAngle = this._projectile.Direction.ToAngle();
        float newAngle = (oldAngle + (this._arcSpeed * BraveTime.DeltaTime));

        if (this._projectile.OverrideMotionModule != null)
            this._projectile.OverrideMotionModule.AdjustRightVector(Mathf.DeltaAngle(oldAngle, newAngle));
        else
            this._projectile.SendInDirection(BraveMathCollege.DegreesToVector(newAngle), true);
    }
}

public class ProjectileSlashingBehaviour : MonoBehaviour  // stolen from NN
{
    public ProjectileSlashingBehaviour()
    {
        DestroyBaseAfterFirstSlash = false;
        timeBetweenSlashes = 1;
        SlashDamageUsesBaseProjectileDamage = true;
    }
    private void Start()
    {
        this.m_projectile = base.GetComponent<Projectile>();
        if (this.m_projectile.Owner && this.m_projectile.Owner is PlayerController) this.owner = this.m_projectile.Owner as PlayerController;
    }
    private void Update()
    {
        if (this.m_projectile)
        {
            if (timer > 0)
            {
                timer -= BraveTime.DeltaTime;
            }
            if (timer <= 0)
            {
                this.m_projectile.StartCoroutine(DoSlash(0, 0));
                if (doSpinAttack)
                {
                    this.m_projectile.StartCoroutine(DoSlash(90, 0.15f));
                    this.m_projectile.StartCoroutine(DoSlash(180, 0.30f));
                    this.m_projectile.StartCoroutine(DoSlash(-90, 0.45f));
                }
                timer = timeBetweenSlashes;
            }
        }
    }
    private IEnumerator DoSlash(float angle, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (slashParameters == null) { slashParameters = new SlashData(); ETGModConsole.Log("Made a new slashparam"); }


        Projectile proj = this.m_projectile;
        List<GameActorEffect> effects = new List<GameActorEffect>();
        effects.AddRange(proj.GetFullListOfStatusEffects(true));

        SlashData instSlash = SlashData.CloneSlashData(slashParameters);

        if (SlashDamageUsesBaseProjectileDamage)
        {
            instSlash.damage = this.m_projectile.baseData.damage;
            instSlash.bossDamageMult = this.m_projectile.BossDamageMultiplier;
            instSlash.jammedDamageMult = this.m_projectile.BlackPhantomDamageMultiplier;
            instSlash.enemyKnockbackForce = this.m_projectile.baseData.force;
        }
        instSlash.OnHitTarget += SlashHitTarget;

        SlashDoer.DoSwordSlash(this.m_projectile.specRigidbody.UnitCenter, (this.m_projectile.Direction.ToAngle() + angle), owner, instSlash);

        if (DestroyBaseAfterFirstSlash) StartCoroutine(Suicide());
        yield break;
    }
    private IEnumerator Suicide()
    {
        yield return null;
        UnityEngine.Object.Destroy(this.m_projectile.gameObject);
        yield break;
    }
    public virtual void SlashHitTarget(GameActor target, bool fatal)
    {

    }

    private float timer;
    public float timeBetweenSlashes;
    public bool doSpinAttack;
    public bool SlashDamageUsesBaseProjectileDamage;
    public bool DestroyBaseAfterFirstSlash;
    public SlashData slashParameters;
    private Projectile m_projectile;
    private PlayerController owner;
}

public static class AnimatedBullet // stolen and modified from NN
{
    private static int _ClipCounter = 0;
    private static HashSet<string> _KnownClips = new();
    public static tk2dSpriteAnimationClip Create( string name, int fps = 2, Anchor anchor = Anchor.MiddleCenter, float scale = 1.0f, bool anchorsChangeColliders = true,
        bool fixesScales = true, IntVector2? overrideColliderPixelSizes = null, IntVector2? overrideColliderOffsets = null)
    {
        List<string> names = ResMap.Get(name).Base();
        if (_KnownClips.Contains(names[0]))
        {
            Lazy.DebugWarn($"  HEY! re-creating projectile sprite {names[0]}. If this is intentional, please reuse the original clip, don't create it twice.");
            return null;
        }
        tk2dSpriteAnimationClip clip = new(){
            name     = names[0]+"_clip",
            fps      = fps,
            wrapMode = tk2dSpriteAnimationClip.WrapMode.Loop,
            frames   = new tk2dSpriteAnimationFrame[names.Count],
        };
        tk2dSpriteCollectionData coll = ETGMod.Databases.Items.ProjectileCollection;
        for (int i = 0; i < names.Count; i++)
        {
            int spriteId = coll.inst.GetSpriteIdByName(names[i]);
            clip.frames[i] = new(){ spriteCollection = coll, spriteId = spriteId };
            tk2dSpriteDefinition def = coll.inst.spriteDefinitions[spriteId];
            if (scale != 1.0f)
                def.ScaleBy(scale);
            //NOTE: set up default colliders, could maybe do at atlas build time but not all sprites need it, so doing it here for now on an as-needed basis
            def.colliderVertices = new Vector3[2]{
                overrideColliderOffsets.HasValue
                    ? C.PIXEL_SIZE * overrideColliderOffsets.Value.ToVector3()
                    : def.position0, // offset
                0.5f * (overrideColliderPixelSizes.HasValue
                    ? (C.PIXEL_SIZE * overrideColliderPixelSizes.Value.ToVector3())
                    : def.boundsDataExtents) // radius
            };
            def.BetterConstructOffsetsFromAnchor(anchor, fixesScales ? def.position3 : null, fixesScales, anchorsChangeColliders);
        }
        // Lazy.DebugLog($"  created clip {clip.name} with id {clip.frames[0].spriteId}");
        _KnownClips.Add(names[0]);
        return clip;
    }

    public static tk2dSpriteAnimationClip Create(ref tk2dSpriteAnimationClip refClip, string name, int fps = 2, Anchor anchor = Anchor.MiddleCenter, float scale = 1.0f, bool anchorsChangeColliders = true,
        bool fixesScales = true, IntVector2? overrideColliderPixelSizes = null, IntVector2? overrideColliderOffsets = null)
    {
        return refClip = AnimatedBullet.Create(name: name, fps: fps, anchor: anchor, scale: scale, anchorsChangeColliders: anchorsChangeColliders,
            fixesScales: fixesScales, overrideColliderPixelSizes: overrideColliderPixelSizes, overrideColliderOffsets: overrideColliderOffsets);
    }

    public static void SetAnimation(this Projectile proj, tk2dSpriteAnimationClip clip, int frame = 0)
    {
        tk2dSpriteAnimator animator = proj.sprite.spriteAnimator;
        animator.currentClip = clip;
        animator.PlayFromFrame(frame);
    }

    public static void AddClip(this tk2dSpriteAnimator animator, tk2dSpriteAnimationClip clip, bool overwriteExisting = false)
    {
        if (animator.Library == null)
        {
            animator.Library = animator.gameObject.GetOrAddComponent<tk2dSpriteAnimation>();
            animator.Library.clips = new tk2dSpriteAnimationClip[0];
            animator.Library.enabled = true;
        }

        if (overwriteExisting || animator.Library.clips == null)
            animator.Library.clips = new tk2dSpriteAnimationClip[] { clip };
        else
        {
            int nextIndex = animator.Library.clips.Length;
            Array.Resize(ref animator.Library.clips, nextIndex + 1);
            animator.Library.clips[nextIndex] = clip;
        }
    }

    public static void AddAnimation(this Projectile proj, tk2dSpriteAnimationClip clip, bool overwriteExisting = false)
    {
        if (!proj.sprite.spriteAnimator)
            proj.sprite.spriteAnimator = proj.sprite.gameObject.GetOrAddComponent<tk2dSpriteAnimator>();
        tk2dSpriteAnimator animator = proj.sprite.spriteAnimator;
        animator.AddClip(clip, overwriteExisting);
        animator.playAutomatically = true;
        animator.deferNextStartClip = false;
    }

    public static void AddDefaultAnimation(this Projectile proj, tk2dSpriteAnimationClip clip, int frame = 0, bool overwriteExisting = false)
    {
        proj.AddAnimation(clip, overwriteExisting: overwriteExisting);
        proj.SetAnimation(clip, frame);
    }

    public static void AddDefaultAnimation(this Projectile proj, GunData b, int frame = 0)
    {
        if (string.IsNullOrEmpty(b.sprite))
            return; // if we haven't specified a sprite, make this a no-op
        proj.AddDefaultAnimation(AnimatedBullet.Create(
            name: b.sprite, fps: b.fps, anchor: b.anchor, scale: b.scale, anchorsChangeColliders: b.anchorsChangeColliders, fixesScales: b.fixesScales,
            overrideColliderPixelSizes: b.overrideColliderPixelSizes, overrideColliderOffsets: b.overrideColliderOffsets),
          frame: frame, overwriteExisting: true);
    }
}

public class EasyTrailBullet : BraveBehaviour // adapted from NN
{
    private static readonly LinkedList<GameObject> _TrailPool = new();
    private static readonly LinkedList<GameObject> _UsedTrails = new();
    private static int _TrailsCreated = 0;

    private Projectile proj;
    private GameObject tro;
    private CustomTrailRenderer tr;
    private Material mat;

    public Vector2 TrailPos;
    public Color BaseColor;
    public Color StartColor;
    public Color EndColor;
    public float LifeTime;
    public float StartWidth;
    public float EndWidth;

    private static GameObject Rent(GameObject parent)
    {
      if (_TrailPool.Count == 0)
      {
        //NOTE: need to immediately parent new trail object to avoid visual glitches
        GameObject newTrail = parent.AddChild("trail object", typeof(CustomTrailRenderer));
        CustomTrailRenderer newTr = newTrail.GetComponent<CustomTrailRenderer>();
        newTr.minVertexDistance = 0.1f;
        newTr.material = new Material(Shader.Find("Sprites/Default"));
        newTr.material.mainTexture = null;
        newTr.colors = new Color[2];
        newTr.widths = new float[2];
        _TrailPool.AddLast(newTrail);
        ++_TrailsCreated;
      }

      LinkedListNode<GameObject> node = _TrailPool.Last;
      _TrailPool.RemoveLast();

      GameObject trail = node.Value; //BUG: null when switching floors
      node.Value = null;
      _UsedTrails.AddLast(node);

      trail.SetActive(true);
      CustomTrailRenderer ctr = trail.GetComponent<CustomTrailRenderer>();
      ctr.enabled = true;
      ctr.Clear();
      ctr.Reenable();
      return trail;
    }

    private static void Return(GameObject trail)
    {
      CustomTrailRenderer tr = trail.GetComponent<CustomTrailRenderer>();
      tr.Clear();

      trail.transform.parent = null;
      GameObject.DontDestroyOnLoad(trail);
      trail.SetActive(false);
      LinkedListNode<GameObject> node = _UsedTrails.Last;
      _UsedTrails.RemoveLast();
      node.Value = trail;
      _TrailPool.AddLast(node);
      // #if DEBUG
      // System.Console.WriteLine($"returned {_TrailPool.Count}/{_TrailsCreated} trails");
      // #endif
    }

    private EasyTrailBullet()
    {
        this.TrailPos   = Vector3.zero;
        this.BaseColor  = Color.red;
        this.StartColor = Color.red;
        this.EndColor   = Color.white;
        this.LifeTime   = 1f;
        this.StartWidth = 1;
        this.EndWidth   = 0;
    }

    /// <summary>
    /// Lets you add a trail to your projectile.
    /// </summary>
    /// <param name="TrailPos">Where the trail attaches its center-point to. You can input a custom Vector3 but its best to use the base preset. (Namely"projectile.transform.position;").</param>
    /// <param name="BaseColor">The Base Color of your trail.</param>
    /// <param name="StartColor">The Starting color of your trail.</param>
    /// <param name="EndColor">The End color of your trail. Having it different to the StartColor will make it transition from the Starting/Base Color to its End Color during its lifetime.</param>
    /// <param name="LifeTime">How long your trail lives for.</param>
    /// <param name="StartWidth">The Starting Width of your Trail.</param>
    /// <param name="EndWidth">The Ending Width of your Trail. Not sure why youd want it to be something other than 0, but the options there.</param>
    private void Start()
    {
        proj = base.projectile;

        tro = Rent(proj.gameObject);
        tro.transform.parent = proj.gameObject.transform;
        tro.transform.rotation = Quaternion.identity;
        tro.transform.position = proj.transform.position;
        tro.transform.localPosition = TrailPos;

        tr = tro.GetComponent<CustomTrailRenderer>();
        mat = tr.material;
        mat.SetColor(CwaffVFX._ColorId, BaseColor);
        tr.colors[0] = StartColor;
        tr.colors[1] = EndColor;
        tr.widths[0] = StartWidth;
        tr.widths[1] = EndWidth;
        tr.lifeTime = LifeTime;
    }

    public void Enable() => tr.enabled = true;
    public void Disable() => tr.enabled = false;

    private void LateUpdate()
    {
        if (tro)
            tro.transform.rotation = Quaternion.identity; // keep trail stable even when projectile rotates
    }

    public void UpdateTrail()
    {
        if (!tro)
            return;

        tro.transform.localPosition = TrailPos;
        mat.SetColor(CwaffVFX._ColorId, BaseColor);
        tr.colors[0] = StartColor;
        tr.colors[1] = EndColor;
        tr.widths[0] = StartWidth;
        tr.widths[1] = EndWidth;
        tr.lifeTime = LifeTime;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (tro)
            Return(tro);
    }
}

public static class SlashDoer // stolen from NN
{
    public enum ProjInteractMode
    {
        IGNORE,
        DESTROY,
        REFLECT,
        REFLECTANDPOSTPROCESS,
    }
    public static void DoSwordSlash(
        Vector2 position,
        float angle,
        GameActor owner,
        SlashData slashParameters,
        Transform parentTransform = null)
    {
        if (slashParameters.doVFX && slashParameters.VFX != null) slashParameters.VFX.SpawnAtPosition(position, angle, parentTransform, null, null, -0.05f);
        if (!string.IsNullOrEmpty( slashParameters.soundEvent) && owner != null && owner.gameObject != null) owner.gameObject.Play(slashParameters.soundEvent);
        GameManager.Instance.StartCoroutine(HandleSlash(position, angle, owner, slashParameters));
    }
    private static IEnumerator HandleSlash(Vector2 position, float angle, GameActor owner, SlashData slashParameters)
    {
        int slashId = Time.frameCount;
        List<SpeculativeRigidbody> alreadyHit = new List<SpeculativeRigidbody>();
        if (slashParameters.playerKnockbackForce != 0f && owner != null) owner.knockbackDoer.ApplyKnockback(BraveMathCollege.DegreesToVector(angle, 1f), slashParameters.playerKnockbackForce, 0.25f, false);
        float ela = 0f;
        while (ela < 0.2f)
        {
            ela += BraveTime.DeltaTime;
            HandleHeroSwordSlash(alreadyHit, position, angle, slashId, owner, slashParameters);
            yield return null;
        }
        yield break;
    }
    private static bool SlasherIsPlayerOrFriendly(GameActor slasher)
    {
        if (slasher is PlayerController) return true;
        if (slasher is AIActor)
        {
            if (slasher.GetComponent<CompanionController>()) return true;
            if (!slasher.aiActor.CanTargetPlayers && slasher.aiActor.CanTargetEnemies) return true;
        }
        return false;
    }
    private static bool ProjectileIsValid(Projectile proj, GameActor slashOwner)
    {
        if (proj)
        {
            if (slashOwner == null)
            {
                return false;
            }
            if (SlasherIsPlayerOrFriendly(slashOwner))
            {
                if ((proj.Owner && !(proj.Owner is PlayerController)) || proj.ForcePlayerBlankable) return true;
            }
            else if (slashOwner is AIActor)
            {
                if (proj.Owner && proj.Owner is PlayerController) return true;
            }
            else
            {
                if (proj.Owner) return true;
            }
        }

        return false;
    }
    private static bool ObjectWasHitBySlash(Vector2 ObjectPosition, Vector2 SlashPosition, float slashAngle, float SlashRange, float SlashDimensions)
    {
        if (Vector2.Distance(ObjectPosition, SlashPosition) < SlashRange)
        {
            float num7 = BraveMathCollege.Atan2Degrees(ObjectPosition - SlashPosition);
            float minRawAngle = Math.Min(SlashDimensions, -SlashDimensions);
            float maxRawAngle = Math.Max(SlashDimensions, -SlashDimensions);
            bool isInRange = false;
            float actualMaxAngle = slashAngle + maxRawAngle;
            float actualMinAngle = slashAngle + minRawAngle;

            if (num7.IsBetweenRange(actualMinAngle, actualMaxAngle)) isInRange = true;
            if (actualMaxAngle > 180)
            {
                float Overflow = actualMaxAngle - 180;
                if (num7.IsBetweenRange(-180, (-180 + Overflow))) isInRange = true;
            }
            if (actualMinAngle < -180)
            {
                float Underflow = actualMinAngle + 180;
                if (num7.IsBetweenRange((180 + Underflow), 180)) isInRange = true;
            }
            return isInRange;
        }
        return false;
    }
    private static void HandleHeroSwordSlash(List<SpeculativeRigidbody> alreadyHit, Vector2 arcOrigin, float slashAngle, int slashId, GameActor owner, SlashData slashParameters)
    {
        float degreesOfSlash = slashParameters.slashDegrees;
        float slashRange = slashParameters.slashRange;



        ReadOnlyCollection<Projectile> allProjectiles2 = StaticReferenceManager.AllProjectiles;
        for (int j = allProjectiles2.Count - 1; j >= 0; j--)
        {
            Projectile projectile2 = allProjectiles2[j];
            if (ProjectileIsValid(projectile2, owner))
            {
                Vector2 projectileCenter = projectile2.SafeCenter;
                if (ObjectWasHitBySlash(projectileCenter, arcOrigin, slashAngle, slashRange, degreesOfSlash))
                {
                    if (slashParameters.OnHitBullet != null) slashParameters.OnHitBullet(projectile2);
                    if (slashParameters.projInteractMode != ProjInteractMode.IGNORE || projectile2.collidesWithProjectiles)
                    {
                        if (slashParameters.projInteractMode == ProjInteractMode.DESTROY || slashParameters.projInteractMode == ProjInteractMode.IGNORE) projectile2.DieInAir(false, true, true, true);
                        else if (slashParameters.projInteractMode == ProjInteractMode.REFLECT || slashParameters.projInteractMode == ProjInteractMode.REFLECTANDPOSTPROCESS)
                        {
                            if (projectile2.Owner != null && projectile2.LastReflectedSlashId != slashId)
                            {
                                projectile2.ReflectBullet(true, owner, 5, (slashParameters.projInteractMode == ProjInteractMode.REFLECTANDPOSTPROCESS), 1, 5, 0, null);
                                projectile2.LastReflectedSlashId = slashId;
                            }
                        }
                    }
                }
            }
        }
        DealDamageToEnemiesInArc(owner, arcOrigin, slashAngle, slashRange, slashParameters, alreadyHit);

        if (slashParameters.damagesBreakables)
        {
            List<MinorBreakable> allMinorBreakables = StaticReferenceManager.AllMinorBreakables;
            for (int k = allMinorBreakables.Count - 1; k >= 0; k--)
            {
                MinorBreakable minorBreakable = allMinorBreakables[k];
                if (minorBreakable && minorBreakable.specRigidbody)
                {
                    if (!minorBreakable.IsBroken && minorBreakable.sprite)
                    {
                        if (ObjectWasHitBySlash(minorBreakable.sprite.WorldCenter, arcOrigin, slashAngle, slashRange, degreesOfSlash))
                        {
                            if (slashParameters.OnHitMinorBreakable != null) slashParameters.OnHitMinorBreakable(minorBreakable);
                            minorBreakable.Break();
                        }
                    }
                }
            }
            List<MajorBreakable> allMajorBreakables = StaticReferenceManager.AllMajorBreakables;
            for (int l = allMajorBreakables.Count - 1; l >= 0; l--)
            {
                MajorBreakable majorBreakable = allMajorBreakables[l];
                if (majorBreakable && majorBreakable.specRigidbody)
                {
                    if (!alreadyHit.Contains(majorBreakable.specRigidbody))
                    {
                        if (!majorBreakable.IsSecretDoor && !majorBreakable.IsDestroyed)
                        {
                            if (ObjectWasHitBySlash(majorBreakable.specRigidbody.UnitCenter, arcOrigin, slashAngle, slashRange, degreesOfSlash))
                            {
                                float num9 = slashParameters.damage;
                                if (majorBreakable.healthHaver)
                                {
                                    num9 *= 0.2f;
                                }
                                if (slashParameters.OnHitMajorBreakable != null) slashParameters.OnHitMajorBreakable(majorBreakable);
                                majorBreakable.ApplyDamage(num9, majorBreakable.specRigidbody.UnitCenter - arcOrigin, false, false, false);
                                alreadyHit.Add(majorBreakable.specRigidbody);
                            }
                        }
                    }
                }
            }
        }
    }
    private static void DealDamageToEnemiesInArc(GameActor owner, Vector2 arcOrigin, float arcAngle, float arcRadius, SlashData slashParameters, List<SpeculativeRigidbody> alreadyHit = null)
    {
        RoomHandler roomHandler = arcOrigin.GetAbsoluteRoom();
        if (roomHandler == null) return;
        if (SlasherIsPlayerOrFriendly(owner))
        {
            List<AIActor> activeEnemies = roomHandler.SafeGetEnemiesInRoom();
            for (int i = 0; i < activeEnemies.Count; i++)
            {
                AIActor aiactor = activeEnemies[i];
                if (!(aiactor && aiactor.specRigidbody && aiactor.IsNormalEnemy && !aiactor.IsGone && aiactor.healthHaver))
                    continue;
                if (alreadyHit != null && alreadyHit.Contains(aiactor.specRigidbody))
                    continue;
                for (int j = 0; j < aiactor.healthHaver.NumBodyRigidbodies; j++)
                {
                    SpeculativeRigidbody bodyRigidbody = aiactor.healthHaver.GetBodyRigidbody(j);
                    PixelCollider hitboxPixelCollider = bodyRigidbody.HitboxPixelCollider;
                    if (hitboxPixelCollider == null)
                        continue;

                    Vector2 vector = BraveMathCollege.ClosestPointOnRectangle(arcOrigin, hitboxPixelCollider.UnitBottomLeft, hitboxPixelCollider.UnitDimensions);
                    if (!ObjectWasHitBySlash(vector, arcOrigin, arcAngle, arcRadius, 90))
                        continue;

                    bool attackIsNotBlocked = true;
                    int rayMask = CollisionMask.LayerToMask(CollisionLayer.HighObstacle, CollisionLayer.BulletBlocker, CollisionLayer.BulletBreakable);
                    RaycastResult raycastResult;
                    if (PhysicsEngine.Instance.Raycast(arcOrigin, vector - arcOrigin, Vector2.Distance(vector, arcOrigin), out raycastResult, true, true, rayMask, null, false, null, null) && raycastResult.SpeculativeRigidbody != bodyRigidbody)
                        attackIsNotBlocked = false;
                    RaycastResult.Pool.Free(ref raycastResult);
                    if (!attackIsNotBlocked)
                        continue;

                    float damage = DealSwordDamageToEnemy(owner, aiactor, arcOrigin, vector, arcAngle, slashParameters);
                    if (alreadyHit != null)
                    {
                        if (alreadyHit.Count == 0)
                            StickyFrictionManager.Instance.RegisterSwordDamageStickyFriction(damage);
                        alreadyHit.Add(aiactor.specRigidbody);
                    }
                    break;
                }
            }
        }
        else
        {
            List<PlayerController> AllPlayers = new List<PlayerController>();
            if (GameManager.Instance.PrimaryPlayer) AllPlayers.Add(GameManager.Instance.PrimaryPlayer);
            if (GameManager.Instance.SecondaryPlayer) AllPlayers.Add(GameManager.Instance.SecondaryPlayer);
            for (int i = 0; i < AllPlayers.Count; i++)
            {
                PlayerController player = AllPlayers[i];
                if (!(player && player.specRigidbody && player.healthHaver && !player.IsGhost))
                    continue;

                if (alreadyHit != null && alreadyHit.Contains(player.specRigidbody))
                    continue;

                SpeculativeRigidbody bodyRigidbody = player.specRigidbody;
                PixelCollider hitboxPixelCollider = bodyRigidbody.HitboxPixelCollider;
                if (hitboxPixelCollider == null)
                    continue;

                Vector2 vector = BraveMathCollege.ClosestPointOnRectangle(arcOrigin, hitboxPixelCollider.UnitBottomLeft, hitboxPixelCollider.UnitDimensions);
                if (!ObjectWasHitBySlash(vector, arcOrigin, arcAngle, arcRadius, 90))
                    continue;

                bool attackIsNotBlocked = true;
                int rayMask = CollisionMask.LayerToMask(CollisionLayer.HighObstacle, CollisionLayer.BulletBlocker, CollisionLayer.BulletBreakable);
                RaycastResult raycastResult;
                if (PhysicsEngine.Instance.Raycast(arcOrigin, vector - arcOrigin, Vector2.Distance(vector, arcOrigin), out raycastResult, true, true, rayMask, null, false, null, null) && raycastResult.SpeculativeRigidbody != bodyRigidbody)
                    attackIsNotBlocked = false;
                RaycastResult.Pool.Free(ref raycastResult);
                if (!attackIsNotBlocked)
                    continue;

                float damage = DealSwordDamageToEnemy(owner, player, arcOrigin, vector, arcAngle, slashParameters);
                if (alreadyHit != null)
                {
                    if (alreadyHit.Count == 0)
                        StickyFrictionManager.Instance.RegisterSwordDamageStickyFriction(damage);
                    alreadyHit.Add(player.specRigidbody);
                }
                break;
            }

        }
    }
    private static float DealSwordDamageToEnemy(GameActor owner, GameActor targetEnemy, Vector2 arcOrigin, Vector2 contact, float angle, SlashData slashParameters)
    {
        if (targetEnemy.healthHaver)
        {
            float damageToDeal = slashParameters.damage;
            if (targetEnemy.healthHaver && targetEnemy.healthHaver.IsBoss) damageToDeal *= slashParameters.bossDamageMult;
            if ((targetEnemy is AIActor) && (targetEnemy as AIActor).IsBlackPhantom) damageToDeal *= slashParameters.jammedDamageMult;
            DamageCategory category = DamageCategory.Normal;
            if ((owner is AIActor) && (owner as AIActor).IsBlackPhantom) category = DamageCategory.BlackBullet;

            bool wasAlivePreviously = targetEnemy.healthHaver.IsAlive;
            //VFX
            if (slashParameters.doHitVFX && slashParameters.hitVFX != null)
            {
                slashParameters.hitVFX.SpawnAtPosition(new Vector3(contact.x, contact.y), 0, targetEnemy.transform);
            }
            targetEnemy.healthHaver.ApplyDamage(damageToDeal, contact - arcOrigin, owner.ActorName, CoreDamageTypes.None, category, false, null, false);

            bool fatal = false;
            if (wasAlivePreviously && targetEnemy.healthHaver.IsDead) fatal = true;

            if (slashParameters.OnHitTarget != null) slashParameters.OnHitTarget(targetEnemy, fatal);
        }
        if (targetEnemy.knockbackDoer)
        {
            targetEnemy.knockbackDoer.ApplyKnockback(contact - arcOrigin, slashParameters.enemyKnockbackForce, false);
        }
        if (slashParameters.statusEffects != null && slashParameters.statusEffects.Count > 0)
        {
            foreach (GameActorEffect effect in slashParameters.statusEffects)
            {
                targetEnemy.ApplyEffect(effect);
            }
        }
        return slashParameters.damage;
    }
}

public class SlashData // stolen from NN
{
    public bool doVFX = true;
    public VFXPool VFX = Items.Blasphemy.AsGun().muzzleFlashEffects;
    public bool doHitVFX = true;
    public VFXPool hitVFX = Items.Blasphemy.AsGun().DefaultModule.projectiles[0].hitEffects.enemy;
    public SlashDoer.ProjInteractMode projInteractMode = SlashDoer.ProjInteractMode.IGNORE;
    public float playerKnockbackForce = 5;
    public float enemyKnockbackForce = 10;
    public List<GameActorEffect> statusEffects = new List<GameActorEffect>();
    public float jammedDamageMult = 1;
    public float bossDamageMult = 1;
    public bool doOnSlash = true;
    public bool doPostProcessSlash = true;
    public float slashRange = 2.5f;
    public float slashDegrees = 90f;
    public float damage = 5f;
    public bool damagesBreakables = true;
    public string soundEvent = "Play_WPN_blasphemy_shot_01";
    public Action<GameActor, bool> OnHitTarget = null;
    public Action<Projectile> OnHitBullet = null;
    public Action<MinorBreakable> OnHitMinorBreakable = null;
    public Action<MajorBreakable> OnHitMajorBreakable = null;

    public static SlashData CloneSlashData(SlashData original)
    {
        SlashData newData = new SlashData();
        newData.doVFX = original.doVFX;
        newData.VFX = original.VFX;
        newData.doHitVFX = original.doHitVFX;
        newData.hitVFX = original.hitVFX;
        newData.projInteractMode = original.projInteractMode;
        newData.playerKnockbackForce = original.playerKnockbackForce;
        newData.enemyKnockbackForce = original.enemyKnockbackForce;
        newData.statusEffects = original.statusEffects;
        newData.jammedDamageMult = original.jammedDamageMult;
        newData.bossDamageMult = original.bossDamageMult;
        newData.doOnSlash = original.doOnSlash;
        newData.doPostProcessSlash = original.doPostProcessSlash;
        newData.slashRange = original.slashRange;
        newData.slashDegrees = original.slashDegrees;
        newData.damage = original.damage;
        newData.damagesBreakables = original.damagesBreakables;
        newData.soundEvent = original.soundEvent;
        newData.OnHitTarget = original.OnHitTarget;
        newData.OnHitBullet = original.OnHitBullet;
        newData.OnHitMinorBreakable = original.OnHitMinorBreakable;
        newData.OnHitMajorBreakable = original.OnHitMajorBreakable;
        return newData;
    }
}

public class EmissiveTrail : MonoBehaviour // stolen from NN
{
    public EmissiveTrail()
    {
        this.EmissivePower = 75;
        this.EmissiveColorPower = 1.55f;
        debugLogging = false;
    }
    public void Start()
    {
        Shader glowshader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTiltedCutoutEmissive");

        foreach (Transform transform in base.transform)
        {

                tk2dBaseSprite sproot = transform.GetComponent<tk2dBaseSprite>();
                if (sproot != null)
                {
                    if (debugLogging) Debug.Log($"Checks were passed for transform; {transform.name}");
                    sproot.usesOverrideMaterial = true;
                    sproot.renderer.material.shader = glowshader;
                    sproot.renderer.material.EnableKeyword("BRIGHTNESS_CLAMP_ON");
                    sproot.renderer.material.SetFloat(CwaffVFX._EmissivePowerId, EmissivePower);
                    sproot.renderer.material.SetFloat(CwaffVFX._EmissiveColorPowerId, EmissiveColorPower);
                }
                else
                {
                    if (debugLogging) Debug.Log("Sprite was null");
                }
        }
    }
    private List<string> TransformList = new List<string>()
    {
        "trailObject",
    };
    public float EmissivePower;
    public float EmissiveColorPower;
    public bool debugLogging;
}

public class OwnerConnectLightningModifier : MonoBehaviour // mostly stolen from NN
{
    public GameObject linkPrefab;
    public float baseDamage = 1f;
    public float disownTimer = -1f;
    public float fadeTimer = -1f;
    public float hitCooldown = 0.1f;
    public bool disowned = false;
    public Color color = ExtendedColours.paleYellow;
    public SpeculativeRigidbody targetBody;
    public bool shrinkFade = false;
    public float emissivePower = 100f;
    public GameActor owner;

    private tk2dTiledSprite extantLink;
    private Projectile proj;
    private bool makeGlowy = false;
    private bool destroyWithProjectile = false;
    private HashSet<AIActor> m_damagedEnemies = new HashSet<AIActor>();
    private float shrinkLength = 1f;

    public Vector2 originPos;
    public Vector2 targetPos;

    private void OnDestroy()
    {
        if (extantLink)
            UnityEngine.Object.Destroy(extantLink.gameObject);
    }
    private void Start()
    {
        proj = base.GetComponent<Projectile>();
        if (!proj)
            return;
        owner = proj.Owner;
        destroyWithProjectile = true;
    }
    public void MakeGlowy() => this.makeGlowy = true;
    private void Update()
    {
        if (destroyWithProjectile && !proj)
        {
            DestroyExtantLink();
            return;
        }
        if (!this.extantLink)
        {
            this.extantLink = UnityEngine.Object.Instantiate(linkPrefab).GetComponent<tk2dTiledSprite>();
            if (makeGlowy)
            {
                Shader glowshader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTiltedCutoutEmissive");
                this.extantLink.usesOverrideMaterial = true;
                Material mat = this.extantLink.renderer.material;
                mat.shader = glowshader;
                mat.DisableKeyword("BRIGHTNESS_CLAMP_ON");
                mat.EnableKeyword("BRIGHTNESS_CLAMP_OFF");
                mat.SetFloat(CwaffVFX._EmissivePowerId, emissivePower);
                mat.SetFloat(CwaffVFX._EmissiveColorPowerId, 1.55f);
                mat.SetColor(CwaffVFX._EmissiveColorId, color);
                this.extantLink.color = color;
            }
            if (disownTimer <= 0 && fadeTimer > 0)  // if we immediately have a fadeTimer > 0 and no disownTimer, start fading out immediately
                StartCoroutine(shrinkFade ? ShrinkOut() : FadeOut());
        }
        if (disownTimer > 0)
        {
            disownTimer -= BraveTime.DeltaTime;
            if (disownTimer <= 0)
            {
                owner = null;
                disowned = true;
                if (fadeTimer > 0)
                    StartCoroutine(shrinkFade ? ShrinkOut() : FadeOut());
                else
                    DestroyExtantLink();
            }
        }
        UpdateLink();
    }
    private void DestroyExtantLink()
    {
        if (!extantLink)
            return;
        UnityEngine.Object.Destroy(extantLink.gameObject);
        extantLink = null;
        if (proj)
            UnityEngine.Object.Destroy(this);
        else
            UnityEngine.Object.Destroy(base.gameObject);
    }

    private IEnumerator FadeOut()
    {
        float halftimer = fadeTimer / 2.0f;
        float timer = halftimer;
        bool halfway = false;
        while (timer > 0)
        {
            timer -= BraveTime.DeltaTime;
            if (timer < 0 && !halfway) //change from emissive to fading halfway through
            {
                halfway = true;
                // this.extantLink.usesOverrideMaterial = true;

                this.extantLink.renderer.material.shader = ShaderCache.Acquire("Brave/LitBlendUber");
                this.extantLink.renderer.material.SetFloat("_VertexColor", 1f);
                timer = halftimer;
            }
            if (halfway)
            {
                this.extantLink.color = this.extantLink.color.WithAlpha(timer/halftimer);
            }
            else {
                this.extantLink.renderer.material.SetFloat(CwaffVFX._EmissivePowerId, emissivePower*(timer/halftimer));
                this.extantLink.renderer.material.SetFloat(CwaffVFX._EmissiveColorPowerId, 1.55f*(timer/halftimer));
            }
            yield return null;
        }
        DestroyExtantLink();
        yield break;
    }

    private IEnumerator ShrinkOut()
    {
        for (float timer = fadeTimer; timer > 0; timer -= BraveTime.DeltaTime)
        {
            shrinkLength = timer / fadeTimer;
            yield return null;
        }
        DestroyExtantLink();
        yield break;
    }

    private void UpdateLink()
    {
        if (proj)
            originPos = proj.specRigidbody.UnitCenter;
        if (targetBody)
            targetPos = targetBody.HitboxPixelCollider.UnitCenter;
        else if (!disowned && owner)
            targetPos = owner.specRigidbody.HitboxPixelCollider.UnitCenter;
        this.extantLink.transform.position = originPos;
        Vector2 vector = targetPos - originPos;
        float angle = BraveMathCollege.Atan2Degrees(vector.normalized);
        int pixelLength = Mathf.RoundToInt(shrinkLength * vector.magnitude * 16f);
        this.extantLink.dimensions = new Vector2((float)pixelLength, this.extantLink.dimensions.y);
        this.extantLink.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        this.extantLink.UpdateZDepth();
        this.ApplyLinearDamage(originPos, targetPos);
    }

    private void ApplyLinearDamage(Vector2 p1, Vector2 p2)
    {
        Vector2 delta = (p2 - p1).normalized;
        if (owner is PlayerController playerOwner)
        {
            float damage = baseDamage * playerOwner.DamageMult();
            float bossMult = playerOwner.stats.GetStatValue(StatType.DamageToBosses);
            for (int i = 0; i < StaticReferenceManager.AllEnemies.Count; i++)
            {
                AIActor aiactor = StaticReferenceManager.AllEnemies[i];
                if (this.m_damagedEnemies.Contains(aiactor))
                    continue;
                if (!aiactor || !aiactor.HasBeenEngaged || !aiactor.IsNormalEnemy || !aiactor.specRigidbody || !aiactor.healthHaver)
                    continue;
                Vector2 ipos;
                PixelCollider col = aiactor.specRigidbody.HitboxPixelCollider;
                if (col == null || !BraveUtility.LineIntersectsAABB(p1, p2, col.UnitBottomLeft, col.UnitDimensions, out ipos))
                    continue;
                aiactor.healthHaver.ApplyDamage(damage * (aiactor.healthHaver.IsBoss ? bossMult : 1f), delta,
                    "Chain Lightning", CoreDamageTypes.Electric, DamageCategory.Normal, false, null, false);
                GameManager.Instance.StartCoroutine(this.HandleDamageCooldown(aiactor));
            }
        }
        else if (owner is AIActor)
        {
            foreach (PlayerController player in GameManager.Instance.AllPlayers)
            {
                if (!player)
                    continue;
                Vector2 ipos;
                PixelCollider pcol = player.specRigidbody.HitboxPixelCollider;
                if (!BraveUtility.LineIntersectsAABB(p1, p2, pcol.UnitBottomLeft, pcol.UnitDimensions, out ipos))
                    continue;
                if (!player.healthHaver || !player.healthHaver.IsVulnerable || player.IsEthereal || player.IsGhost)
                    continue;
                string damageSource = "Electricity";
                if (owner.encounterTrackable)
                    damageSource = owner.encounterTrackable.GetModifiedDisplayName();
                if (proj && proj.IsBlackBullet)
                    player.healthHaver.ApplyDamage(1f, delta, damageSource, CoreDamageTypes.Electric, DamageCategory.BlackBullet, false);
                else
                    player.healthHaver.ApplyDamage(0.5f, delta, damageSource, CoreDamageTypes.Electric, DamageCategory.Normal, false);
            }
        }
    }

    private IEnumerator HandleDamageCooldown(AIActor damagedTarget)
    {
        this.m_damagedEnemies.Add(damagedTarget);
        yield return new WaitForSeconds(hitCooldown);
        this.m_damagedEnemies.Remove(damagedTarget);
        yield break;
    }
}

public static class Raycast
{
    private static bool ExcludeAllButWallsAndEnemiesFromRaycasting(SpeculativeRigidbody s)
    {
        if (s.GetComponent<PlayerController>() != null)
            return true; //true == exclude players
        if (s.GetComponent<Projectile>() != null)
            return true; //true == exclude projectiles
        if (s.GetComponent<MinorBreakable>() != null)
            return true; //true == exclude minor breakables
        if (s.GetComponent<MajorBreakable>() != null)
            return true; //true == exclude major breakables
        if (s.GetComponent<FlippableCover>() != null)
            return true; //true == exclude tables
        return false; //false == don't exclude
    }

    private static bool ExcludeAllButWallsFromRaycasting(SpeculativeRigidbody s)
    {
        // if (s.GetComponent<AIActor>() != null)
        //     return true; //true == exclude enemies
        // return ExcludeAllButWallsAndEnemiesFromRaycasting(s);

        // TODO: fails to collide with some unexpected things, including statue in starting rom
        if (s.PrimaryPixelCollider.IsTileCollider)
            return false;
        return true;
    }

    public static Vector2 ToNearestWallOrEnemyOrObject(this Vector2 pos, float angle, float minDistance = 1)
    {
        RaycastResult hit;
        Vector2 contact;
        if (PhysicsEngine.Instance.Raycast(
          pos+BraveMathCollege.DegreesToVector(angle,minDistance), BraveMathCollege.DegreesToVector(angle), 200, out hit,
          rigidbodyExcluder: ExcludeAllButWallsAndEnemiesFromRaycasting))
            contact = hit.Contact;
        else
            contact = pos+BraveMathCollege.DegreesToVector(angle,minDistance);
        RaycastResult.Pool.Free(ref hit);
        return contact;
    }

    public static Vector2 ToNearestWallOrObject(this Vector2 pos, float angle, float minDistance = 1)
    {
        RaycastResult hit;
        Vector2 contact;
        if (PhysicsEngine.Instance.Raycast(
          pos+BraveMathCollege.DegreesToVector(angle,minDistance), BraveMathCollege.DegreesToVector(angle), 200, out hit,
          rigidbodyExcluder: ExcludeAllButWallsFromRaycasting))
            contact = hit.Contact;
        else
            contact = pos+BraveMathCollege.DegreesToVector(angle,minDistance);
        RaycastResult.Pool.Free(ref hit);
        return contact;
    }

    public static Vector2 ToNearestWall(this Vector2 pos, out Vector2 normal, float angle, float minDistance = 1)
    {
        RaycastResult hit;
        Vector2 contact;
        if (PhysicsEngine.Instance.Raycast(
          pos+BraveMathCollege.DegreesToVector(angle,minDistance), BraveMathCollege.DegreesToVector(angle), 200, out hit,
          collideWithRigidbodies: false))
        {
            contact = hit.Contact;
            normal = hit.Normal;
        }
        else
        {
            contact = pos+BraveMathCollege.DegreesToVector(angle,minDistance);
            normal = Vector2.zero;
        }
        RaycastResult.Pool.Free(ref hit);
        return contact;
    }

    public static Vector2 ToNearestWall(this Vector2 pos, float angle, float minDistance = 1)
    {
        Vector2 dummy;
        return pos.ToNearestWall(out dummy, angle, minDistance);
    }
}

public static class AfterImageHelpers
{
    private const float _LIFETIME = 0.5f;

    public static Color afterImageGray   = new Color( 32f / 255f,  32f / 255f,  32f / 255f);
    public static Color afterImageWhite  = new Color(255f / 255f, 255f / 255f, 255f / 255f);
    public static Color afterImageBlue   = new Color(160f / 255f, 160f / 255f, 255f / 255f);
    public static Color afterImageYellow = new Color(255f / 255f, 255f / 255f, 120f / 255f);

    public static void PlayerAfterImage(this PlayerController player)
    {
        if (player.spriteAnimator is not tk2dSpriteAnimator animator)
            return;
        if (animator.CurrentClip is not tk2dSpriteAnimationClip clip)
            return;
        if (clip.frames == null || clip.frames.Length == 0)
            return;
        tk2dSpriteAnimationFrame frame = clip.frames[animator.CurrentFrame % clip.frames.Length];

        tk2dSprite sprite = Lazy.SpriteObject(frame.spriteCollection, frame.spriteId);
        sprite.FlipX = player.sprite.FlipX;
        sprite.PlaceAtPositionByAnchor(
            player.sprite.transform.position,
            sprite.FlipX ? Anchor.LowerRight : Anchor.LowerLeft);

        sprite.StartCoroutine(Fade(sprite, _LIFETIME));
    }

    private static IEnumerator Fade(tk2dSprite sprite, float fadeTime, float flickerRate = 0.05f)
    {
        sprite.renderer.material.shader = ShaderCache.Acquire("Brave/LitBlendUber");
        sprite.renderer.material.SetFloat("_VertexColor", 1f);

        bool flickerOn = true;
        float flickerTimer = 0.0f;
        for (float timer = 0.0f; timer < fadeTime; )
        {
            timer += BraveTime.DeltaTime;
            flickerTimer += BraveTime.DeltaTime;
            if (flickerTimer > flickerRate)
            {
                flickerTimer = 0;
                flickerOn = !flickerOn;
            }
            sprite.color = afterImageYellow.WithAlpha(flickerOn ? 1.0f : 0.35f);
            yield return null;
        }
        UnityEngine.Object.Destroy(sprite.gameObject);
        yield break;
    }
}

public static class CustomNoteDoer
{
    private static NoteDoer _Prefab = null;

    public static void Init()
    {
        tk2dSprite noteSpriteComp = Lazy.SpriteObject(spriteColl: VFX.Collection, spriteId: VFX.Collection.GetSpriteIdByName("note_icon")).RegisterPrefab();
            noteSpriteComp.PlaceAtPositionByAnchor(noteSpriteComp.gameObject.transform.position, Anchor.LowerCenter);
        _Prefab = noteSpriteComp.AddComponent<NoteDoer>();
    }

    public static NoteDoer CreateNote(Vector2 position, string formattedNoteText, NoteDoer.NoteBackgroundType background = NoteDoer.NoteBackgroundType.NOTE, bool destroyOnRead = true, bool poofIn = true, tk2dSprite customSprite = null)
    {
        NoteDoer noteDoer = UnityEngine.Object.Instantiate(
            _Prefab.gameObject,
            position.ToVector3ZisY(-1f),
            Quaternion.identity).GetComponent<NoteDoer>();

        if (customSprite != null)
            noteDoer.sprite.SetSprite(customSprite.Collection,customSprite.spriteId);
        noteDoer.alreadyLocalized   = true;
        noteDoer.textboxSpawnPoint  = noteDoer.sprite.transform;
        noteDoer.stringKey          = formattedNoteText;
        noteDoer.noteBackgroundType = background;
        noteDoer.DestroyedOnFinish  = destroyOnRead;
        position.GetAbsoluteRoom().RegisterInteractable(noteDoer);

        if (poofIn)
            LootEngine.DoDefaultItemPoof(noteDoer.sprite.WorldCenter);

        return noteDoer;
    }
}

public class RotateIntoPositionBehavior : MonoBehaviour
{
    public Vector2 m_fulcrum;
    public float m_radius;
    public float m_start_angle;
    public float m_end_angle;
    public float m_rotate_time;

    private float timer;
    private float angle_delta;
    private bool has_been_init = false;

    public void Setup()
    {
        this.timer         = 0;
        this.angle_delta   = this.m_end_angle - this.m_start_angle;
        this.has_been_init = true;
        this.Relocate();
    }

    private void Update()
    {
        if ((!this.has_been_init) || (this.timer > m_rotate_time))
            return;
        this.timer += BraveTime.DeltaTime;
        if (this.timer > this.m_rotate_time)
            this.timer = this.m_rotate_time;
        this.Relocate();
    }

    private void Relocate()
    {
        float percentDone  = this.timer / this.m_rotate_time;
        float curAngle     = this.m_start_angle + (float)Math.Tanh(percentDone*Mathf.PI) * this.angle_delta;
        Vector2 curPos     = this.m_fulcrum + BraveMathCollege.DegreesToVector(curAngle, this.m_radius);
        base.gameObject.transform.position = curPos.ToVector3ZUp(base.gameObject.transform.position.z);
        base.gameObject.transform.rotation =
            Quaternion.Euler(0f, 0f, curAngle + (curAngle > 180 ? 180 : (-180)));
    }
}

//TODO: destroying these without breaking the game is rather hard...look into it later
public class Nametag : MonoBehaviour
{
    private Text _nametag; // Reference to the Text component.
    private GameActor _actor;
    private GameObject _canvasGo;
    private GameObject _textGo;

    private static int _NumNames = 0;
    private static Font _Font;

    public void Setup(TextAnchor anchor = TextAnchor.UpperCenter)
    {
        _Font ??= Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
        this._actor = base.GetComponent<GameActor>();

        // Create Canvas GameObject.
        this._canvasGo = new GameObject();
        this._canvasGo.name = "Canvas";
        Canvas canvas = this._canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
        this._canvasGo.AddComponent<CanvasScaler>();

        // Create the Text GameObject.
        this._textGo = new GameObject();
            this._textGo.transform.parent = _canvasGo.transform;

        // Set Text component properties.
        this._nametag           = this._textGo.AddComponent<Text>();
        this._nametag.font      = _Font;
        this._nametag.text      = "";
        this._nametag.fontSize  = 32;
        this._nametag.alignment = anchor;
        this._nametag.color     = Color.green;
        this._nametag.gameObject.AddComponent<Outline>().effectColor = Color.black;

        // Provide Text position and size using RectTransform.
        RectTransform rectTransform;
        rectTransform = this._nametag.GetComponent<RectTransform>();
        rectTransform.localPosition = new Vector3(0, 0, 0);
        rectTransform.sizeDelta = new Vector2(500, 300); // make this big enough to fit a pretty big name

        this._actor.healthHaver.OnPreDeath += HandleEnemyDied;

        UpdateWhileParentAlive();  // fixes rendering over the player instead of the enemy on the first frame
    }

    public void SetName(string name)
    {
        this._nametag.text = name;
    }

    internal bool UpdateWhileParentAlive()
    {
        if (!this._actor || !this._actor.healthHaver || this._actor.healthHaver.IsDead)
        {
            HandleEnemyDied(Vector2.zero);
            return false;
        }

        Vector3 screenPos = Camera.main.WorldToScreenPoint(this._actor.sprite ? this._actor.sprite.WorldTopCenter : this._actor.CenterPosition);
        this._nametag.transform.position = screenPos;
        return true;
    }

    private void HandleEnemyDied(Vector2 _)
    {
        UnityEngine.Object.Destroy(this._canvasGo);
        UnityEngine.Object.Destroy(this._textGo);
        UnityEngine.Object.Destroy(this);
    }

    private void OnDestroy()
    {
        if (this._canvasGo)
            UnityEngine.Object.Destroy(this._canvasGo);
        if (this._textGo)
            UnityEngine.Object.Destroy(this._textGo);
    }

    public void SetEnabled(bool v)
    {
        this._textGo.SetActive(v);
    }
}

// Modified from GrenadeProjectile()
public class FancyGrenadeProjectile : Projectile
{
    public float startingHeight   = 1f;
    public float startingVelocity = 0f;
    public float gravity          = 10f;
    public float minBounceAngle   = 0f;
    public float maxBounceAngle   = 0f;
    public Action OnBounce;

    private float m_currentHeight;
    private Vector3 m_current3DVelocity;

    public override void Start()
    {
        base.Start();
        m_currentHeight = startingHeight;
        m_current3DVelocity = (m_currentDirection * m_currentSpeed).ToVector3ZUp(startingVelocity);
    }

    public override void Move()
    {
        m_current3DVelocity.x  = m_currentDirection.x;
        m_current3DVelocity.y  = m_currentDirection.y;
        m_current3DVelocity.z += base.LocalDeltaTime * -gravity;
        float num              = m_currentHeight + m_current3DVelocity.z * base.LocalDeltaTime;
        bool bounced = num < 0f;
        if (bounced)
        {
            if (maxBounceAngle > 0)
            {
                m_current3DVelocity = (m_currentDirection.ToAngle() + BraveUtility.RandomSign() * UnityEngine.Random.Range(minBounceAngle,maxBounceAngle)
                    ).ToVector().ToVector3ZUp(-m_current3DVelocity.z);
            }
            else
                m_current3DVelocity.z = -m_current3DVelocity.z;
            num *= -1f;
        }
        m_currentHeight             = num;
        m_currentDirection          = m_current3DVelocity.XY();
        Vector2 vector              = m_current3DVelocity.XY().normalized * m_currentSpeed;
        base.specRigidbody.Velocity = new Vector2(vector.x, vector.y + m_current3DVelocity.z);
        base.LastVelocity           = m_current3DVelocity.XY();
        if (bounced)
            OnBounce();
    }

    public override void DoModifyVelocity()
    {
        if (ModifyVelocity == null)
            return;

        Vector2 arg = m_current3DVelocity.XY().normalized * m_currentSpeed;
        arg = ModifyVelocity(arg);
        base.specRigidbody.Velocity = new Vector2(arg.x, arg.y + m_current3DVelocity.z);
        if (arg.sqrMagnitude > 0f)
            m_currentDirection = arg.normalized;
    }

    public void Redirect(Vector2 direction)
    {
        m_currentDirection = direction;
    }
}

public class SkipNonProjectileCollisionsBehavior : MonoBehaviour
{
    private void Start()
    {
        if (base.GetComponent<SpeculativeRigidbody>() is not SpeculativeRigidbody body)
            return;
        body.OnPreRigidbodyCollision += this.OnPreRigidbodyCollision;
        body.OnPreTileCollision += this.OnPreTileCollision;
    }

    private void OnDestroy()
    {
        if (base.GetComponent<SpeculativeRigidbody>() is not SpeculativeRigidbody body)
            return;
        body.OnPreRigidbodyCollision -= this.OnPreRigidbodyCollision;
        body.OnPreTileCollision -= this.OnPreTileCollision;
    }

    private void OnPreRigidbodyCollision(SpeculativeRigidbody me, PixelCollider myPixelCollider, SpeculativeRigidbody other, PixelCollider otherPixelCollider)
    {
        if (!other.projectile)
            PhysicsEngine.SkipCollision = true;
    }

    private void OnPreTileCollision(SpeculativeRigidbody me, PixelCollider myPixelCollider, PhysicsEngine.Tile other, PixelCollider otherPixelCollider)
    {
        PhysicsEngine.SkipCollision = true;
    }
}

public static class MovingDistortionWave
{
    public static void DoMovingDistortionWave(this Transform parent, float distortionIntensity, float distortionRadius, float maxRadius, float duration)
    {
        Exploder component = new GameObject("temp_explosion_processor", typeof(Exploder)).GetComponent<Exploder>();
        component.StartCoroutine(DoMovingDistortionWaveLocal(parent, distortionIntensity, distortionRadius, maxRadius, duration));
    }

    private static Vector4 GetCenterPointInScreenUV(Vector2 centerPoint, float dIntensity, float dRadius)
    {
        Vector3 vector = GameManager.Instance.MainCameraController.Camera.WorldToViewportPoint(centerPoint.ToVector3ZUp());
        return new Vector4(vector.x, vector.y, dRadius, dIntensity);
    }

    private static IEnumerator DoMovingDistortionWaveLocal(Transform parent, float distortionIntensity, float distortionRadius, float maxRadius, float duration)
    {
        Material distMaterial = new Material(ShaderCache.Acquire("Brave/Internal/DistortionWave"));
        Vector2 center = (parent != null) ? parent.position : Vector2.zero;
        Vector4 distortionSettings2 = GetCenterPointInScreenUV(center, distortionIntensity, distortionRadius);
        distMaterial.SetVector("_WaveCenter", distortionSettings2);
        Pixelator.Instance.RegisterAdditionalRenderPass(distMaterial);
        float elapsed = 0f;
        while (elapsed < duration && (!BraveUtility.isLoadingLevel || !GameManager.Instance.IsLoadingLevel))
        {
            elapsed += BraveTime.DeltaTime;
            float t2 = elapsed / duration;
            t2 = BraveMathCollege.LinearToSmoothStepInterpolate(0f, 1f, t2);
            if (parent != null)
                center = parent.position;
            distortionSettings2 = GetCenterPointInScreenUV(center, distortionIntensity, distortionRadius);
            distortionSettings2.w = Mathf.Lerp(distortionSettings2.w, 0f, t2);
            distMaterial.SetVector("_WaveCenter", distortionSettings2);
            float currentRadius = Mathf.Lerp(0f, maxRadius, t2);
            distMaterial.SetFloat("_DistortProgress", currentRadius / maxRadius * (maxRadius / 33.75f));
            yield return null;
        }
        Pixelator.Instance.DeregisterAdditionalRenderPass(distMaterial);
        UnityEngine.Object.Destroy(distMaterial);
    }
}

public class DissipatingSpriteFragment : MonoBehaviour
{
    Vector2 _start     = Vector2.zero;
    Vector2 _target    = Vector2.zero;
    float _time        = 0f;
    float _lifetime    = 0f;
    float _delay       = 0f;
    bool _autoDestroy  = false;
    bool _setup        = false;
    tk2dSprite _sprite = null;

    public void Setup(Vector2 start, Vector2 target, float time, float delay = 0.0f, bool autoDestroy = false)
    {
        this._start       = start;
        this._target      = target;
        this._time        = time;
        this._sprite      = base.GetComponent<tk2dSprite>();
        this._delay       = delay;
        this._autoDestroy = autoDestroy;
        this._setup       = true;
    }

    private void Update()
    {
        if (!this._setup)
            return;

        this._lifetime += BraveTime.DeltaTime;
        if (this._delay > 0.0f)
        {
            if (this._lifetime < this._delay)
                return;
            this._lifetime -= this._delay;
            this._delay = 0.0f;
        }

        if (this._lifetime >= this._time)
        {
            if (this._autoDestroy)
                base.gameObject.SafeDestroy();
            else
                this._sprite.PlaceAtRotatedPositionByAnchor(this._target, Anchor.MiddleCenter);
            return;
        }

        float percentLeft = 1f - this._lifetime / this._time;
        float ease        = 1f - (percentLeft * percentLeft);
        Vector2 pos       = Vector2.Lerp(this._start, this._target, ease);
        this._sprite.PlaceAtRotatedPositionByAnchor(pos, Anchor.MiddleCenter);
    }
}

/// <summary>Allow guns to have a different carryPixelOffset when flipped</summary>
public class FlippedCarryPixelOffset : MonoBehaviour
{
    private bool _cachedFlipped = false;
    private bool _firstSpriteCheck = true;

    //NOTE: all public so they can be serialized
    public IntVector2 defaultCarryOffset;
    public IntVector2 defaultFlippedCarryOffset;
    public IntVector2[] carryOffsets        = new IntVector2[1 + (int)PlayableCharacters.Gunslinger];
    public IntVector2[] flippedCarryOffsets = new IntVector2[1 + (int)PlayableCharacters.Gunslinger];

    public static void AddTo(Gun gun, IntVector2 offset, IntVector2 flippedOffset,
        IntVector2? offsetPilot       = null, IntVector2? flippedOffsetPilot       = null,
        IntVector2? offsetConvict     = null, IntVector2? flippedOffsetConvict     = null,
        IntVector2? offsetRobot       = null, IntVector2? flippedOffsetRobot       = null,
        IntVector2? offsetNinja       = null, IntVector2? flippedOffsetNinja       = null,
        IntVector2? offsetCosmonaut   = null, IntVector2? flippedOffsetCosmonaut   = null,
        IntVector2? offsetSoldier     = null, IntVector2? flippedOffsetSoldier     = null,
        IntVector2? offsetGuide       = null, IntVector2? flippedOffsetGuide       = null,
        IntVector2? offsetCoopCultist = null, IntVector2? flippedOffsetCoopCultist = null,
        IntVector2? offsetBullet      = null, IntVector2? flippedOffsetBullet      = null,
        IntVector2? offsetEevee       = null, IntVector2? flippedOffsetEevee       = null,
        IntVector2? offsetGunslinger  = null, IntVector2? flippedOffsetGunslinger  = null
        )
    {
        FlippedCarryPixelOffset fixer   = gun.gameObject.AddComponent<FlippedCarryPixelOffset>();
        fixer.defaultCarryOffset        = offset;
        fixer.defaultFlippedCarryOffset = flippedOffset;

        fixer.carryOffsets[(int)PlayableCharacters.Pilot]       = offsetPilot       ?? offset;
        fixer.carryOffsets[(int)PlayableCharacters.Convict]     = offsetConvict     ?? offset;
        fixer.carryOffsets[(int)PlayableCharacters.Robot]       = offsetRobot       ?? offset;
        fixer.carryOffsets[(int)PlayableCharacters.Ninja]       = offsetNinja       ?? offset;
        fixer.carryOffsets[(int)PlayableCharacters.Cosmonaut]   = offsetCosmonaut   ?? offset;
        fixer.carryOffsets[(int)PlayableCharacters.Soldier]     = offsetSoldier     ?? offset;
        fixer.carryOffsets[(int)PlayableCharacters.Guide]       = offsetGuide       ?? offset;
        fixer.carryOffsets[(int)PlayableCharacters.CoopCultist] = offsetCoopCultist ?? offset;
        fixer.carryOffsets[(int)PlayableCharacters.Bullet]      = offsetBullet      ?? offset;
        fixer.carryOffsets[(int)PlayableCharacters.Eevee]       = offsetEevee       ?? offset;
        fixer.carryOffsets[(int)PlayableCharacters.Gunslinger]  = offsetGunslinger  ?? offset;

        fixer.flippedCarryOffsets[(int)PlayableCharacters.Pilot]       = flippedOffsetPilot       ?? flippedOffset;
        fixer.flippedCarryOffsets[(int)PlayableCharacters.Convict]     = flippedOffsetConvict     ?? flippedOffset;
        fixer.flippedCarryOffsets[(int)PlayableCharacters.Robot]       = flippedOffsetRobot       ?? flippedOffset;
        fixer.flippedCarryOffsets[(int)PlayableCharacters.Ninja]       = flippedOffsetNinja       ?? flippedOffset;
        fixer.flippedCarryOffsets[(int)PlayableCharacters.Cosmonaut]   = flippedOffsetCosmonaut   ?? flippedOffset;
        fixer.flippedCarryOffsets[(int)PlayableCharacters.Soldier]     = flippedOffsetSoldier     ?? flippedOffset;
        fixer.flippedCarryOffsets[(int)PlayableCharacters.Guide]       = flippedOffsetGuide       ?? flippedOffset;
        fixer.flippedCarryOffsets[(int)PlayableCharacters.CoopCultist] = flippedOffsetCoopCultist ?? flippedOffset;
        fixer.flippedCarryOffsets[(int)PlayableCharacters.Bullet]      = flippedOffsetBullet      ?? flippedOffset;
        fixer.flippedCarryOffsets[(int)PlayableCharacters.Eevee]       = flippedOffsetEevee       ?? flippedOffset;
        fixer.flippedCarryOffsets[(int)PlayableCharacters.Gunslinger]  = flippedOffsetGunslinger  ?? flippedOffset;
    }

    private IntVector2 GetOffset(PlayableCharacters identity, bool flipped)
    {
        int charId = (int)identity;
        if (charId <= (int)PlayableCharacters.Gunslinger)
            return flipped ? flippedCarryOffsets[charId] : carryOffsets[charId];
        return flipped ? defaultFlippedCarryOffset : defaultCarryOffset;
    }

    [HarmonyPatch(typeof(Gun), nameof(Gun.HandleSpriteFlip))]
    private class GunSpriteFlipFixerPatch // patch for automatically fixing carryPixelOffsets for flipped guns
    {
        static void Prefix(bool flipped, ref bool __state)
        {
            __state = flipped;  // we need to remember whether we were flipped before entering the original method
        }

        static void Postfix(Gun __instance, bool flipped, bool __state)
        {
            bool wasFlipped = __state;
            if (__instance.GetComponent<FlippedCarryPixelOffset>() is not FlippedCarryPixelOffset fixer)
                return;
            if (__instance.CurrentOwner is not PlayerController player)
                return;
            if (wasFlipped == fixer._cachedFlipped && !fixer._firstSpriteCheck)
                return;

            __instance.carryPixelOffset = fixer.GetOffset(player.characterIdentity, wasFlipped);
            fixer._cachedFlipped        = wasFlipped;
            fixer._firstSpriteCheck     = false;
        }
    }
}

/// <summary>Class to allow for easy manual movement and manipulation of projectiles</summary>
public class ManualMotionModule : ProjectileAndBeamMotionModule
{
    public override void Move(Projectile source, Transform projectileTransform, tk2dBaseSprite projectileSprite, SpeculativeRigidbody specRigidbody, ref float m_timeElapsed, ref Vector2 m_currentDirection, bool Inverted, bool shouldRotate)
    {
        // Vector2 vector = ((!projectileSprite) ? projectileTransform.position.XY() : projectileSprite.WorldCenter);
        // if (!m_initialized)
        // {
        //     m_initialized        = true;
        //     m_initialRightVector = ((!shouldRotate) ? m_currentDirection : projectileTransform.right.XY());
        //     m_initialUpVector    = ((!shouldRotate) ? (Quaternion.Euler(0f, 0f, 90f) * m_currentDirection) : projectileTransform.up);
        //     m_radius             = UnityEngine.Random.Range(MinRadius, MaxRadius);
        //     m_currentAngle       = m_initialRightVector.ToAngle();
        // }

        // m_timeElapsed        += BraveTime.DeltaTime;
        // float radius         = m_radius;
        // float num            = source.Speed * BraveTime.DeltaTime;
        // float num2           = num / ((float)Math.PI * 2f * radius) * 360f;
        // m_currentAngle       += num2;
        // Vector2 targetCenter = ((!usesAlternateOrbitTarget) ? source.Owner.CenterPosition : alternateOrbitTarget.UnitCenter);
        // Vector2 vector2      = targetCenter + (Quaternion.Euler(0f, 0f, m_currentAngle) * Vector2.right * radius).XY();
        // Vector2 velocity     = (vector2 - vector) / BraveTime.DeltaTime;
        // m_currentDirection   = velocity.normalized;
        // if (shouldRotate)
        // {
        //     float num7 = m_currentDirection.ToAngle();
        //     if (float.IsNaN(num7) || float.IsInfinity(num7))
        //         num7 = 0f;
        //     projectileTransform.localRotation = Quaternion.Euler(0f, 0f, num7);
        // }
        // specRigidbody.Velocity = velocity;
        // if (float.IsNaN(specRigidbody.Velocity.magnitude) || Mathf.Approximately(specRigidbody.Velocity.magnitude, 0f))
        //     source.DieInAir();
    }

    public override void UpdateDataOnBounce(float angleDiff)
    {
    }

    public override Vector2 GetBoneOffset(BasicBeamController.BeamBone bone, BeamController sourceBeam, bool inverted)
    {
        return Vector2.zero;
    }
}

/// <summary>Class for creating replicant enemies that temporarily fight on your side</summary>
public static class Replicant
{
    private static GameActorCharmEffect _CharmEffect = new(){
        AffectsPlayers   = false,
        AffectsEnemies   = true,
        effectIdentifier = "replicant",
        resistanceType   = 0,
        stackMode        = GameActorEffect.EffectStackingMode.Refresh,
        duration         = 36000f,
        };

    public static AIActor Create(string guid, Vector2 position, Action<tk2dBaseSprite> shaderFunc, bool hasCollision)
    {
        AIActor replicant = AIActor.Spawn(
            prefabActor     : EnemyDatabase.GetOrLoadByGuid(guid),
            position        : position.ToIntVector2(VectorConversions.Floor),
            source          : position.GetAbsoluteRoom(),
            correctForWalls : true, //NOTE: could possibly be false, Chain Gunners don't have good offsets when spawned like this
            awakenAnimType  : AIActor.AwakenAnimationType.Spawn
            );
        if (!replicant)
            return null;

        replicant.PreventBlackPhantom = true;
        replicant.SpawnInInstantly();
        replicant.sprite.PlaceAtPositionByAnchor(position, Anchor.MiddleCenter);
        replicant.specRigidbody.Initialize();
        replicant.specRigidbody.CollideWithTileMap = false;
        if (hasCollision)
        {
            if (GameManager.Instance.PrimaryPlayer is PlayerController p1 && p1.specRigidbody)
                replicant.specRigidbody.RegisterSpecificCollisionException(p1.specRigidbody);
            if (GameManager.Instance.CurrentGameType == GameManager.GameType.COOP_2_PLAYER)
                if (GameManager.Instance.SecondaryPlayer is PlayerController p2 && p2.specRigidbody)
                    replicant.specRigidbody.RegisterSpecificCollisionException(p2.specRigidbody);
        }
        else
        {
            replicant.specRigidbody.CollideWithOthers = false;
            replicant.specRigidbody.AddCollisionLayerIgnoreOverride(CollisionMask.LayerToMask(CollisionLayer.Projectile));
        }
        replicant.HitByEnemyBullets = false;
        replicant.IgnoreForRoomClear = true;
        replicant.IsHarmlessEnemy = true;
        replicant.ApplyEffect(_CharmEffect);
        if (replicant.GetComponent<SpawnEnemyOnDeath>() is SpawnEnemyOnDeath seod)
            seod.chanceToSpawn = 0.0f; // prevent enemies such as Blobulons from replicating on death
        if (replicant.healthHaver is HealthHaver hh)
            hh.PreventAllDamage = true; // can't be harmed normally (exceptions for, e.g., Pinhead or Nitra self-detonation)
        if (replicant.knockbackDoer is KnockbackDoer kb)
            kb.SetImmobile(true, "replicant"); // can't be knocked back

        replicant.ApplyShader(shaderFunc, true, true);
        return replicant;
    }
}

/// <summary>Class for holding 3 ints</summary>
public struct IntVector3 {
    public int x;
    public int y;
    public int z;

    public static readonly IntVector3 zero = new();
    public static readonly IntVector3 one  = new(1, 1, 1);

    public IntVector3(int x = 0, int y = 0, int z = 0)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }
}

/// <summary>Class to draw an animated targeting line between two points (currently unused, relocated from Maestro for code cleanup purposes)</summary>
// public class AnimatedTargetingLine
// {
//     private const float _MAX_SEGMENTS = 30;

//     private float          _segmentPhaseTime = 0.25f;
//     private float          _segmentSpacing   = 1.0f;
//     private GameObject     _lineSegmentVFX   = null;
//     private List<CwaffVFX> _targetLine       = new();
//     private float          _baseAlpha        = 1f;

//     public AnimatedTargetingLine(float segmentPhaseTime, float segmentSpacing, GameObject lineSegmentVFX, List<CwaffVFX> targetLine, float baseAlpha)
//     {
//         this._segmentPhaseTime = segmentPhaseTime;
//         this._segmentSpacing   = segmentSpacing;
//         this._lineSegmentVFX   = lineSegmentVFX;
//         this._targetLine       = targetLine;
//         this._baseAlpha        = baseAlpha;
//     }

//     private void UpdateTargetingLine(Vector2 start, Vector2 end)
//     {
//         Vector2 delta   = (end - start);
//         float mag       = delta.magnitude;
//         Vector2 dir     = delta / mag;
//         int numSegments = Mathf.FloorToInt(Mathf.Min(mag / this._segmentSpacing, _MAX_SEGMENTS));
//         float offset    = (BraveTime.ScaledTimeSinceStartup % this._segmentPhaseTime) / this._segmentPhaseTime;
//         for (int i = this._targetLine.Count; i < _MAX_SEGMENTS; ++i)
//         {
//             // CwaffVFX fv = CwaffVFX.Spawn(this._lineSegmentVFX, start/*, rotation: Lazy.RandomEulerZ()*/);
//             // fv.GetComponent<tk2dSpriteAnimator>().PlayFromFrame(i % 4);
//             // this._targetLine.Add(fv);
//         }
//         for (int i = 0; i < numSegments; ++i)
//         {
//             Vector2 pos = start + ((i + 1 - offset) * this._segmentSpacing * dir);
//             if (!this._targetLine[i])
//             {
//                 // CwaffVFX fv = CwaffVFX.Spawn(this._lineSegmentVFX, pos/*, rotation: Lazy.RandomEulerZ()*/);
//                 // fv.GetComponent<tk2dSpriteAnimator>().PlayFromFrame(i % 4);
//                 // this._targetLine[i] = fv;
//             }
//             tk2dBaseSprite sprite = this._targetLine[i].sprite;
//             sprite.renderer.enabled = true;
//             float alpha;
//             if (i == 0)
//                 alpha = 1f - offset;
//             else if (i == numSegments - 1)
//                 alpha = offset;
//             else
//                 alpha = 1f;
//             sprite.renderer.SetAlpha(alpha * this._baseAlpha);
//             sprite.transform.position = pos;
//         }
//         for (int i = numSegments; i < Mathf.Min(this._targetLine.Count, _MAX_SEGMENTS); ++i)
//             if (this._targetLine[i])
//                this._targetLine[i].sprite.renderer.enabled = false;
//     }
// }
