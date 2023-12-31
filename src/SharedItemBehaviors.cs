﻿namespace CwaffingTheGungy;

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

public class Expiration : MonoBehaviour  // kill projectile after a fixed amount of time
{
    public float expirationTimer = 1f;

    // dummy component
    private void Start()
    {
        Projectile p = base.GetComponent<Projectile>();
        if (expirationTimer == 0f)
            Expire();
        else
            Invoke("Expire", expirationTimer);
    }

    private void Expire()
    {
        this.gameObject.GetComponent<Projectile>()?.DieInAir(true,false,false,true);
    }
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

    // Manualy Set
    private float _arcAngle                = 0;
    private float _maxSecsToReachTarget    = 3.0f;
    private float _minSpeed                = 5.0f;
    private bool  _hasBeenSetup            = false;

    public void Setup(float? arcAngle = null, float? maxSecsToReachTarget = null, float? minSpeed = null)
    {
        this._projectile = base.GetComponent<Projectile>();
        if (this._projectile.Owner is not PlayerController pc)
            return;
        this._owner = pc;

        this._targetAngle  = this._owner.CurrentGun.CurrentAngle;
        this._targetPos    = Raycast.ToNearestWallOrEnemyOrObject(
            this._owner.sprite.WorldCenter,
            this._targetAngle);

        if (arcAngle.HasValue)
            this._arcAngle = arcAngle.Value;
        if (maxSecsToReachTarget.HasValue)
            this._maxSecsToReachTarget = maxSecsToReachTarget.Value;
        if (minSpeed.HasValue)
            this._minSpeed = minSpeed.Value;

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
        float distanceToTarget = Vector2.Distance(curpos,this._targetPos);
        this._projectile.baseData.speed = Mathf.Max(distanceToTarget / _maxSecsToReachTarget, _minSpeed);
        this._actualTimeToReachTarget = distanceToTarget / this._projectile.baseData.speed;
        this._projectile.UpdateSpeed();
        this._projectile.SendInDirection(BraveMathCollege.DegreesToVector(this._targetAngle-this._arcAngle), true);
    }

    private void Update()
    {
        if (!_hasBeenSetup)
            return;

        float deltatime = BraveTime.DeltaTime;
        this._lifetime += deltatime;
        float percentDoneTurning = this._lifetime / this._actualTimeToReachTarget;
        if (percentDoneTurning > 1.0f)
            return;

        float inflection = (2.0f*percentDoneTurning) - 1.0f;
        float newAngle = this._targetAngle + inflection * this._arcAngle;
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
    public static tk2dSpriteAnimationClip CreateProjectileAnimation(List<string> names, int fps, bool loops, List<float> pixelScales, List<bool> lighteneds, List<Anchor> anchors, List<bool> anchorsChangeColliders,
        List<bool> fixesScales, List<Vector3?> manualOffsets, List<IntVector2?> overrideColliderPixelSizes, List<IntVector2?> overrideColliderOffsets, List<Projectile> overrideProjectilesToCopyFrom)
    {
        if (_KnownClips.Contains(names[0]))
        {
            Lazy.DebugWarn($"  HEY! re-creating projectile sprite {names[0]}. If this is intentional, please reuse the original clip, don't create it twice.");
            return null;
        }
        tk2dSpriteAnimationClip clip = new tk2dSpriteAnimationClip();
        clip.name = names[0]+"_clip";
        clip.fps = fps;
        List<tk2dSpriteAnimationFrame> frames = new List<tk2dSpriteAnimationFrame>();
        for (int i = 0; i < names.Count; i++)
        {
            string name = names[i];
            float pixelScale = pixelScales[i];
            IntVector2? overrideColliderPixelSize = overrideColliderPixelSizes[i];
            IntVector2? overrideColliderOffset = overrideColliderOffsets[i];
            bool anchorChangesCollider = anchorsChangeColliders[i];
            bool fixesScale = fixesScales[i];
            Anchor anchor = anchors[i];
            bool lightened = lighteneds[i];
            Projectile overrideProjectileToCopyFrom = overrideProjectilesToCopyFrom[i];
            tk2dSpriteAnimationFrame frame = new tk2dSpriteAnimationFrame();
            frame.spriteCollection = ETGMod.Databases.Items.ProjectileCollection;
            frame.spriteId = frame.spriteCollection.inst.GetSpriteIdByName(name);
            frames.Add(frame);
            IntVector2 truePixelSize = (C.PIXELS_PER_TILE * frame.spriteCollection.GetSpriteDefinition(name).position3.XY()).ToIntVector2();
            IntVector2 pixelSize = new IntVector2((int)(pixelScale * truePixelSize.x), (int)(pixelScale * truePixelSize.y));
            int? overrideColliderPixelWidth = null;
            int? overrideColliderPixelHeight = null;
            if (overrideColliderPixelSize.HasValue)
            {
                overrideColliderPixelWidth = overrideColliderPixelSize.Value.x;
                overrideColliderPixelHeight = overrideColliderPixelSize.Value.y;
            }
            int? overrideColliderOffsetX = null;
            int? overrideColliderOffsetY = null;
            if (overrideColliderOffset.HasValue)
            {
                overrideColliderOffsetX = overrideColliderOffset.Value.x;
                overrideColliderOffsetY = overrideColliderOffset.Value.y;
            }
            tk2dSpriteDefinition def = GunTools.SetupDefinitionForProjectileSprite(name, frame.spriteId, pixelSize.x, pixelSize.y, lightened, overrideColliderPixelWidth, overrideColliderPixelHeight, overrideColliderOffsetX, overrideColliderOffsetY,
                overrideProjectileToCopyFrom);
            def.ConstructOffsetsFromAnchor(anchor, def.position3, fixesScale, anchorChangesCollider);
            if (manualOffsets[i].HasValue)
            {
                Vector3 manualOffset = manualOffsets[i].Value;
                def.position0 += manualOffset;
                def.position1 += manualOffset;
                def.position2 += manualOffset;
                def.position3 += manualOffset;
            }
        }
        clip.wrapMode = loops ? tk2dSpriteAnimationClip.WrapMode.Loop : tk2dSpriteAnimationClip.WrapMode.Once;
        clip.frames = frames.ToArray();
        // Lazy.DebugLog($"  created clip {clip.name} with id {clip.frames[0].spriteId}");
        _KnownClips.Add(names[0]);
        return clip;
    }

    public static tk2dSpriteAnimationClip Create(string name, int fps = 2, Anchor anchor = Anchor.MiddleCenter, float scale = 1.0f, bool anchorsChangeColliders = true,
        bool fixesScales = true, Vector3? manualOffsets = null, IntVector2? overrideColliderPixelSizes = null, IntVector2? overrideColliderOffsets = null, Projectile overrideProjectilesToCopyFrom = null)
    {
        List<string> names = ResMap.Get(name).Base();
        int n = names.Count;
        return CreateProjectileAnimation(
            names                         : names,
            fps                           : fps,
            loops                         : true,
            pixelScales                   : Enumerable.Repeat(scale,n).ToList(),
            lighteneds                    : Enumerable.Repeat(false/*lighteneds*/,n).ToList(),
            anchors                       : Enumerable.Repeat(anchor,n).ToList(),
            anchorsChangeColliders        : Enumerable.Repeat(anchorsChangeColliders,n).ToList(),
            fixesScales                   : Enumerable.Repeat(fixesScales,n).ToList(),
            manualOffsets                 : Enumerable.Repeat<Vector3?>(manualOffsets,n).ToList(),
            overrideColliderPixelSizes    : Enumerable.Repeat<IntVector2?>(overrideColliderPixelSizes,n).ToList(),
            overrideColliderOffsets       : Enumerable.Repeat<IntVector2?>(overrideColliderOffsets,n).ToList(),
            overrideProjectilesToCopyFrom : Enumerable.Repeat<Projectile>(overrideProjectilesToCopyFrom,n).ToList());
    }

    public static tk2dSpriteAnimationClip Create(ref tk2dSpriteAnimationClip refClip, string name, int fps = 2, Anchor anchor = Anchor.MiddleCenter, float scale = 1.0f, bool anchorsChangeColliders = true,
        bool fixesScales = true, Vector3? manualOffsets = null, IntVector2? overrideColliderPixelSizes = null, IntVector2? overrideColliderOffsets = null, Projectile overrideProjectilesToCopyFrom = null)
    {
        return refClip = AnimatedBullet.Create(name: name, fps: fps, anchor: anchor, scale: scale, anchorsChangeColliders: anchorsChangeColliders,
            fixesScales: fixesScales, manualOffsets: manualOffsets, overrideColliderPixelSizes: overrideColliderPixelSizes,
            overrideColliderOffsets: overrideColliderOffsets, overrideProjectilesToCopyFrom: overrideProjectilesToCopyFrom);
    }

    public static void SetAnimation(this Projectile proj, tk2dSpriteAnimationClip clip, int frame = 0)
    {
        proj.sprite.spriteAnimator.currentClip = clip;
        proj.sprite.spriteAnimator.PlayFromFrame(frame);
    }

    public static void AddAnimation(this Projectile proj, tk2dSpriteAnimationClip clip)
    {
        if (proj.sprite.spriteAnimator == null)
        {
            proj.sprite.spriteAnimator = proj.sprite.gameObject.AddComponent<tk2dSpriteAnimator>();
        }
        proj.sprite.spriteAnimator.playAutomatically = true;
        if (proj.sprite.spriteAnimator.Library == null)
        {
            proj.sprite.spriteAnimator.Library = proj.sprite.spriteAnimator.gameObject.AddComponent<tk2dSpriteAnimation>();
            proj.sprite.spriteAnimator.Library.clips = new tk2dSpriteAnimationClip[0];
            proj.sprite.spriteAnimator.Library.enabled = true;
        }

        proj.sprite.spriteAnimator.Library.clips = proj.sprite.spriteAnimator.Library.clips.Concat(new tk2dSpriteAnimationClip[] { clip }).ToArray();
        proj.sprite.spriteAnimator.deferNextStartClip = false;
    }

    public static void AddDefaultAnimation(this Projectile proj, tk2dSpriteAnimationClip clip, int frame = 0)
    {
        proj.AddAnimation(clip);
        proj.SetAnimation(clip, frame);
    }

    public static void AddDefaultAnimation(this Projectile proj, GunBuildData b, int frame = 0)
    {
        if (string.IsNullOrEmpty(b.sprite))
            return; // if we haven't specified a sprite, make this a no-op
        proj.AddDefaultAnimation(AnimatedBullet.Create(
            name: b.sprite, fps: b.fps, anchor: b.anchor, scale: b.scale, anchorsChangeColliders: b.anchorsChangeColliders, fixesScales: b.fixesScales,
            manualOffsets: b.manualOffsets, overrideColliderPixelSizes: b.overrideColliderPixelSizes, overrideColliderOffsets: b.overrideColliderOffsets,
            overrideProjectilesToCopyFrom: b.overrideProjectilesToCopyFrom), frame: frame);
    }
}

public class EasyTrailBullet : BraveBehaviour // stolen from NN
{
    public EasyTrailBullet()
    {
        //=====
        this.TrailPos = new Vector3(0, 0, 0);
        //======
        this.BaseColor = Color.red;
        this.StartColor = Color.red;
        this.EndColor = Color.white;
        //======
        this.LifeTime = 1f;
        //======
        this.StartWidth = 1;
        this.EndWidth = 0;

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
    public void Start()
    {
        proj = base.projectile;
        {
            tro = base.projectile.gameObject.AddChild("trail object");
            tro.transform.position = base.projectile.transform.position;
            tro.transform.localPosition = TrailPos;

            tr = tro.AddComponent<TrailRenderer>();
            tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            tr.receiveShadows = false;
            mat = new Material(Shader.Find("Sprites/Default"));
            mat.mainTexture = _gradTexture;
            tr.material = mat;
            tr.minVertexDistance = 0.1f;
            //======
            mat.SetColor("_Color", BaseColor);
            tr.startColor = StartColor;
            tr.endColor = EndColor;
            //======
            tr.time = LifeTime;
            //======
            tr.startWidth = StartWidth;
            tr.endWidth = EndWidth;
        }

    }
    public void UpdateTrail()
    {
        if (!tro)
            return;
        tro.transform.localPosition = TrailPos;
        mat.SetColor("_Color", BaseColor);
        tr.startColor = StartColor;
        tr.endColor = EndColor;
        //======
        tr.time = LifeTime;
        //======
        tr.startWidth = StartWidth;
        tr.endWidth = EndWidth;
    }

    public void Enable() => tr.enabled = true;
    public void Disable() => tr.enabled = false;

    public Texture _gradTexture;
    private Projectile proj;
    private GameObject tro;
    private TrailRenderer tr;
    private Material mat;

    public Vector2 TrailPos;
    public Color BaseColor;
    public Color StartColor;
    public Color EndColor;
    public float LifeTime;
    public float StartWidth;
    public float EndWidth;
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
        if (!string.IsNullOrEmpty( slashParameters.soundEvent) && owner != null && owner.gameObject != null) AkSoundEngine.PostEvent(slashParameters.soundEvent, owner.gameObject);
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
                Vector2 projectileCenter = projectile2.sprite.WorldCenter;
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
                if (minorBreakable?.specRigidbody)
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
            List<AIActor> activeEnemies = roomHandler.GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
            if (activeEnemies == null) return;

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

public class SlashData : ScriptableObject // stolen from NN
{
    public bool doVFX = true;
    public VFXPool VFX = (ItemHelper.Get(Items.Blasphemy) as Gun).muzzleFlashEffects;
    public bool doHitVFX = true;
    public VFXPool hitVFX = (ItemHelper.Get(Items.Blasphemy) as Gun).DefaultModule.projectiles[0].hitEffects.enemy;
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
                    sproot.renderer.material.SetFloat("_EmissivePower", EmissivePower);
                    sproot.renderer.material.SetFloat("_EmissiveColorPower", EmissiveColorPower);
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
    public float DamagePerTick;
    public float disownTimer = -1f;
    public float fadeTimer = -1f;
    private GameActor owner;
    private tk2dTiledSprite extantLink;
    private Projectile self;
    private bool makeGlowy = false;
    private bool fading = false;
    public OwnerConnectLightningModifier()
    {
        // linkPrefab = St4ke.LinkVFXPrefab;
        DamagePerTick = 2f;
    }
    private void OnDestroy()
    {
        if (extantLink)
        {
            SpawnManager.Despawn(extantLink.gameObject);
        }
    }
    private void Start()
    {
        self = base.GetComponent<Projectile>();
        if (self)
        {
            if (self.Owner) owner = self.Owner;
        }
    }
    public void MakeGlowy()
    {
        this.makeGlowy = true;
    }
    private void Update()
    {
        if (disownTimer > 0)
        {
            disownTimer -= BraveTime.DeltaTime;
            if (disownTimer <= 0)
                owner = null;
        }
        if (self && owner)
        {
            if (this.extantLink == null)
            {
                tk2dTiledSprite component = SpawnManager.SpawnVFX(linkPrefab, false).GetComponent<tk2dTiledSprite>();
                this.extantLink = component;

                if (makeGlowy)
                {
                    Shader glowshader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTiltedCutoutEmissive");
                    // this.extantLink.usesOverrideMaterial = true;
                    this.extantLink.renderer.material.shader = glowshader;
                    this.extantLink.renderer.material.DisableKeyword("BRIGHTNESS_CLAMP_ON");
                    this.extantLink.renderer.material.EnableKeyword("BRIGHTNESS_CLAMP_OFF");
                    this.extantLink.renderer.material.SetFloat("_EmissivePower", 100.0f);
                    this.extantLink.renderer.material.SetFloat("_EmissiveColorPower", 1.55f);
                    this.extantLink.renderer.material.SetColor("_EmissiveColor", ExtendedColours.paleYellow);
                    this.extantLink.color = ExtendedColours.paleYellow;

                    // foreach (tk2dSpriteDefinition frameDef in component.sprite.collection.spriteDefinitions)
                    // {
                    //     frameDef.material.EnableKeyword("BRIGHTNESS_CLAMP_ON");
                    //     frameDef.material.SetFloat("_EmissivePower", 10.0f);
                    //     frameDef.material.SetFloat("_EmissiveColorPower", 1.55f);
                    //     frameDef.material.SetColor("_EmissiveColor", ExtendedColours.paleYellow);
                    //     frameDef.materialInst.EnableKeyword("BRIGHTNESS_CLAMP_ON");
                    //     frameDef.materialInst.SetFloat("_EmissivePower", 10.0f);
                    //     frameDef.materialInst.SetFloat("_EmissiveColorPower", 1.55f);
                    //     frameDef.materialInst.SetColor("_EmissiveColor", ExtendedColours.paleYellow);
                    // }

                    // this.extantLink.renderer.materialInst.EnableKeyword("BRIGHTNESS_CLAMP_ON");
                    // this.extantLink.renderer.materialInst.DisableKeyword("BRIGHTNESS_CLAMP_OFF");
                    // this.extantLink.renderer.materialInst.SetFloat("_EmissivePower", 100.0f);
                    // this.extantLink.renderer.materialInst.SetFloat("_EmissiveColorPower", 1.55f);
                    // this.extantLink.renderer.materialInst.SetColor("_EmissiveColor", ExtendedColours.paleYellow);
                }
            }
            else
                UpdateLink(owner, this.extantLink);
        }
        else if (!owner && !fading)
        {
            fading = true;
            if (fadeTimer > 0)
                StartCoroutine(FadeOut());
            else
                DestroyExtantLink();
        }
        else if (!self)
        {
            DestroyExtantLink();
        }
    }
    private void DestroyExtantLink()
    {
        if (extantLink != null)
            SpawnManager.Despawn(extantLink.gameObject);
        extantLink = null;
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
                this.extantLink.renderer.material.SetFloat("_EmissivePower", 100.0f*(timer/halftimer));
                this.extantLink.renderer.material.SetFloat("_EmissiveColorPower", 1.55f*(timer/halftimer));
            }
            yield return null;
        }
        DestroyExtantLink();
        yield break;
    }
    private void UpdateLink(GameActor target, tk2dTiledSprite m_extantLink)
    {
        Vector2 unitCenter = self.specRigidbody.UnitCenter;
        Vector2 unitCenter2 = target.specRigidbody.HitboxPixelCollider.UnitCenter;
        m_extantLink.transform.position = unitCenter;
        Vector2 vector = unitCenter2 - unitCenter;
        float num = BraveMathCollege.Atan2Degrees(vector.normalized);
        int num2 = Mathf.RoundToInt(vector.magnitude / 0.0625f);
        m_extantLink.dimensions = new Vector2((float)num2, m_extantLink.dimensions.y);
        m_extantLink.transform.rotation = Quaternion.Euler(0f, 0f, num);
        m_extantLink.UpdateZDepth();
        this.ApplyLinearDamage(unitCenter, unitCenter2);
    }
    private void ApplyLinearDamage(Vector2 p1, Vector2 p2)
    {
        if (owner is PlayerController)
        {
            float damage = DamagePerTick;
            damage *= self.ProjectilePlayerOwner().stats.GetStatValue(PlayerStats.StatType.Damage);
            for (int i = 0; i < StaticReferenceManager.AllEnemies.Count; i++)
            {
                AIActor aiactor = StaticReferenceManager.AllEnemies[i];
                if (!this.m_damagedEnemies.Contains(aiactor))
                {
                    if (aiactor && aiactor.HasBeenEngaged && aiactor.IsNormalEnemy && aiactor.specRigidbody && aiactor.healthHaver)
                    {
                        if (aiactor.healthHaver.IsBoss) damage *= self.ProjectilePlayerOwner().stats.GetStatValue(PlayerStats.StatType.DamageToBosses);
                        Vector2 zero = Vector2.zero;
                        if (BraveUtility.LineIntersectsAABB(p1, p2, aiactor.specRigidbody.HitboxPixelCollider.UnitBottomLeft, aiactor.specRigidbody.HitboxPixelCollider.UnitDimensions, out zero))
                        {
                            aiactor.healthHaver.ApplyDamage(DamagePerTick, Vector2.zero, "Chain Lightning", CoreDamageTypes.Electric, DamageCategory.Normal, false, null, false);
                            GameManager.Instance.StartCoroutine(this.HandleDamageCooldown(aiactor));
                        }
                    }
                }
            }
        }
        else if (owner is AIActor)
        {
            if (GameManager.Instance.PrimaryPlayer != null)
            {
                PlayerController player1 = GameManager.Instance.PrimaryPlayer;
                Vector2 zero = Vector2.zero;
                if (BraveUtility.LineIntersectsAABB(p1, p2, player1.specRigidbody.HitboxPixelCollider.UnitBottomLeft, player1.specRigidbody.HitboxPixelCollider.UnitDimensions, out zero))
                {
                    if (player1.healthHaver && player1.healthHaver.IsVulnerable && !player1.IsEthereal && !player1.IsGhost)
                    {
                        string damageSource = "Electricity";
                        if (owner.encounterTrackable) damageSource = owner.encounterTrackable.GetModifiedDisplayName();
                        if (self.IsBlackBullet) player1.healthHaver.ApplyDamage(1f, Vector2.zero, damageSource, CoreDamageTypes.Electric, DamageCategory.BlackBullet, true);
                        else player1.healthHaver.ApplyDamage(0.5f, Vector2.zero, damageSource, CoreDamageTypes.Electric, DamageCategory.Normal, false);
                    }
                }
            }
            if (GameManager.Instance.SecondaryPlayer != null)
            {
                PlayerController player2 = GameManager.Instance.SecondaryPlayer;
                Vector2 zero = Vector2.zero;
                if (BraveUtility.LineIntersectsAABB(p1, p2, player2.specRigidbody.HitboxPixelCollider.UnitBottomLeft, player2.specRigidbody.HitboxPixelCollider.UnitDimensions, out zero))
                {
                    if (player2.healthHaver && player2.healthHaver.IsVulnerable && !player2.IsEthereal && !player2.IsGhost)
                    {
                        string damageSource = "Electricity";
                        if (owner.encounterTrackable) damageSource = owner.encounterTrackable.GetModifiedDisplayName();
                        if (self.IsBlackBullet) player2.healthHaver.ApplyDamage(1f, Vector2.zero, damageSource, CoreDamageTypes.Electric, DamageCategory.BlackBullet, true);
                        else player2.healthHaver.ApplyDamage(0.5f, Vector2.zero, damageSource, CoreDamageTypes.Electric, DamageCategory.Normal, false);
                    }
                }
            }
        }
    }
    private HashSet<AIActor> m_damagedEnemies = new HashSet<AIActor>();

    private IEnumerator HandleDamageCooldown(AIActor damagedTarget)
    {
        this.m_damagedEnemies.Add(damagedTarget);
        yield return new WaitForSeconds(0.1f);
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
        GameObject obj = UnityEngine.Object.Instantiate(new GameObject(), player.sprite.WorldBottomCenter, Quaternion.identity);
        tk2dSprite sprite = obj.AddComponent<tk2dSprite>();

        tk2dSpriteAnimationFrame frame = player.spriteAnimator.CurrentClip.frames[player.spriteAnimator.CurrentFrame];
        sprite.SetSprite(frame.spriteCollection, frame.spriteId);
        sprite.FlipX = player.sprite.FlipX;
        obj.GetComponent<BraveBehaviour>().sprite = sprite;

        sprite.PlaceAtPositionByAnchor(
            player.sprite.transform.position,
            sprite.FlipX ? Anchor.LowerRight : Anchor.LowerLeft);

        obj.GetComponent<BraveBehaviour>().StartCoroutine(Fade(obj,_LIFETIME));
    }

    private static IEnumerator Fade(GameObject obj, float fadeTime, float flickerRate = 0.05f)
    {
        tk2dSprite sprite = obj.GetComponent<tk2dSprite>();
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
        UnityEngine.Object.Destroy(obj);
        yield break;
    }
}

public static class CustomNoteDoer
{
    private static NoteDoer prefab = null;

    public static void Init()
    {
        GameObject noteSpriteObject = SpriteBuilder.SpriteFromResource(ResMap.Get("note_icon")[0], null);
            FakePrefab.MarkAsFakePrefab(noteSpriteObject);
            tk2dSprite noteSprite = noteSpriteObject.GetComponent<tk2dSprite>();

        GameObject noteItem = new GameObject("Custom Note Item");
        tk2dSprite noteSpriteComp = noteItem.GetOrAddComponent<tk2dSprite>();
            noteSpriteComp.SetSprite(noteSprite.Collection, noteSprite.spriteId);
            noteSpriteComp.PlaceAtPositionByAnchor(noteItem.transform.position, Anchor.LowerCenter);
        prefab = noteItem.AddComponent<NoteDoer>();
        prefab.gameObject.SetActive(false);
        FakePrefab.MarkAsFakePrefab(prefab.gameObject);
        UnityEngine.Object.DontDestroyOnLoad(prefab);
    }

    public static NoteDoer CreateNote(Vector2 position, string formattedNoteText, NoteDoer.NoteBackgroundType background = NoteDoer.NoteBackgroundType.NOTE, bool destroyOnRead = true, bool poofIn = true, tk2dSprite customSprite = null)
    {
        NoteDoer noteDoer = UnityEngine.Object.Instantiate(
            prefab.gameObject,
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

public class Nametag : MonoBehaviour
{
    private Text _nametag; // Reference to the Text component.
    private AIActor _actor;
    private GameObject _canvasGo;
    private GameObject _textGo;

    private static int _NumNames = 0;
    private static Font _Font;

    public void Setup()
    {
        _Font ??= Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
        this._actor = base.GetComponent<AIActor>();

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
        this._nametag.alignment = TextAnchor.UpperCenter;
        this._nametag.color     = Color.green;
        this._nametag.gameObject.AddComponent<Outline>().effectColor = Color.black;

        // Provide Text position and size using RectTransform.
        RectTransform rectTransform;
        rectTransform = this._nametag.GetComponent<RectTransform>();
        rectTransform.localPosition = new Vector3(0, 0, 0);
        rectTransform.sizeDelta = new Vector2(500, 100); // make this big enough to fit a pretty big name

        this._actor.healthHaver.OnPreDeath += (_) => HandleEnemyDied();

        UpdateWhileParentAlive();  // fixes rendering over the player instead of the enemy on the first frame
    }

    public void SetName(string name)
    {
        this._nametag.text = name;
    }

    internal bool UpdateWhileParentAlive()
    {
        if (this._actor?.healthHaver?.IsDead ?? true)
        {
            HandleEnemyDied();
            return false;
        }

        Vector3 screenPos = Camera.main.WorldToScreenPoint(this._actor.sprite.WorldTopCenter);
        this._nametag.transform.position = screenPos;
        return true;
    }

    private void HandleEnemyDied()
    {
        UnityEngine.Object.Destroy(this._canvasGo);
        UnityEngine.Object.Destroy(this._textGo);
        UnityEngine.Object.Destroy(this);
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
