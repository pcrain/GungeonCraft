
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

    private const float _PHASE_TIME = 0.35f;
    private const float _SOUND_GAP = _PHASE_TIME / 4f;
    private const int _MAX_PHASE = 6;

    internal static readonly bool _UseUnicodeFont = true;
    internal static GameObject _MathSymbols = null;

    private dfLabel _shotAngleLabel     = null;
    private dfLabel _spreadLabel        = null;
    private dfLabel _shotDistanceLabel  = null;
    private dfLabel _reboundAngleLabel  = null;
    private dfLabel _widthLabel         = null;
    private dfLabel _heightLabel        = null;
    private List<dfLabel> _labels       = new();

    private Geometry _aimAngleArc       = null;
    private Geometry _perfectShot       = null;
    private Geometry _reboundShot       = null;
    private Geometry _reboundArc        = null;
    private Geometry _leftBaseSpread    = null;
    private Geometry _rightBaseSpread   = null;
    private Geometry _leftAdjSpread     = null;
    private Geometry _rightAdjSpread    = null;
    private Geometry _topBbox           = null;
    private Geometry _bottomBbox        = null;
    private Geometry _leftBbox          = null;
    private Geometry _rightBbox         = null;
    private Geometry _weakPointL        = null;
    private Geometry _weakPointR        = null;
    private Geometry _weakPointT        = null;
    private Geometry _weakPointB        = null;
    private Geometry _weakPointArcL     = null;
    private Geometry _weakPointArcR     = null;
    private Geometry _weakPointArcT     = null;
    private Geometry _weakPointArcB     = null;
    private List<Geometry> _shapes      = new();

    private float _drawTimer            = 0.0f;
    private float _soundTimer           = 0.0f;
    private int _phase                  = 0;
    private int _maxDrawablePhase       = 0;
    private float _lastAimAngle         = 0.0f;
    private float _lastSpread           = 0.0f;
    private float _lastTargetMag        = 0.0f;
    private float _freezeTimer          = 0.0f;
    private float _cooldownTimer          = 0.0f;
    private float _slidingAngleWindow   = 0.0f;
    private float _slidingMagWindow     = 0.0f;
    private PixelCollider _lastCollider = null;
    private AIActor _targetActor         = null;
    private Vector2 _lastNormal         = Vector2.zero;
    private Vector2? _lastContact       = null;
    private Vector2? _lastRebound       = null;

    public bool canDoCrit = false;

    public static void Init()
    {
        Lazy.SetupGun<Sextant>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.B, gunClass: GunClass.RIFLE, reloadTime: 0.9f, ammo: 100, shootFps: 14, reloadFps: 4,
            /*muzzleFrom: Items.Mailbox, */ fireAudio: "sextant_shoot_sound"/*, reloadAudio: "paintball_reload_sound"*/)
          .InitSpecialProjectile<PrecisionProjectile>(GunData.New(sprite: null, clipSize: -1, cooldown: 0.9f, shootStyle: ShootStyle.SemiAutomatic,
            damage: 50.0f, speed: 25f, range: 18f, force: 12f, invisibleProjectile: true/*, customClip: true*/));

        _MathSymbols = VFX.Create("math_symbols", loops: false);
    }

    private void Start()
    {
        this._shapes.Add(this._perfectShot = new GameObject().AddComponent<Geometry>());
        this._shapes.Add(this._reboundShot = new GameObject().AddComponent<Geometry>());
        this._shapes.Add(this._reboundArc = new GameObject().AddComponent<Geometry>());
        this._shapes.Add(this._leftBaseSpread = new GameObject().AddComponent<Geometry>());
        this._shapes.Add(this._rightBaseSpread = new GameObject().AddComponent<Geometry>());
        this._shapes.Add(this._leftAdjSpread = new GameObject().AddComponent<Geometry>());
        this._shapes.Add(this._rightAdjSpread = new GameObject().AddComponent<Geometry>());
        this._shapes.Add(this._aimAngleArc = new GameObject().AddComponent<Geometry>());
        this._shapes.Add(this._topBbox = new GameObject().AddComponent<Geometry>());
        this._shapes.Add(this._bottomBbox = new GameObject().AddComponent<Geometry>());
        this._shapes.Add(this._leftBbox = new GameObject().AddComponent<Geometry>());
        this._shapes.Add(this._rightBbox = new GameObject().AddComponent<Geometry>());
        this._shapes.Add(this._weakPointL = new GameObject().AddComponent<Geometry>());
        this._shapes.Add(this._weakPointR = new GameObject().AddComponent<Geometry>());
        this._shapes.Add(this._weakPointT = new GameObject().AddComponent<Geometry>());
        this._shapes.Add(this._weakPointB = new GameObject().AddComponent<Geometry>());
        this._shapes.Add(this._weakPointArcL = new GameObject().AddComponent<Geometry>());
        this._shapes.Add(this._weakPointArcR = new GameObject().AddComponent<Geometry>());
        this._shapes.Add(this._weakPointArcT = new GameObject().AddComponent<Geometry>());
        this._shapes.Add(this._weakPointArcB = new GameObject().AddComponent<Geometry>());

        this._labels.Add(this._shotAngleLabel = MakeNewLabel());
        this._labels.Add(this._spreadLabel = MakeNewLabel());
        this._labels.Add(this._shotDistanceLabel = MakeNewLabel());
        this._labels.Add(this._reboundAngleLabel = MakeNewLabel());
        this._labels.Add(this._widthLabel = MakeNewLabel());
        this._labels.Add(this._heightLabel = MakeNewLabel());
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        base.OnSwitchedAwayFromThisGun();
        ResetCalculations();
    }

    public override void OnSwitchedToThisGun()
    {
        base.OnSwitchedToThisGun();
        ResetCalculations();
    }

    private void ResetCalculations(bool postShotCooldown = false)
    {
        this.canDoCrit = false;
        this._drawTimer = 0.0f;
        this._soundTimer = 0.0f;
        this._freezeTimer = 0.0f;
        this._slidingAngleWindow = 0.0f;
        this._slidingMagWindow = 0.0f;
        this._phase = 0;
        if (postShotCooldown)
            this._cooldownTimer = Mathf.Max(this._cooldownTimer, this.gun.DefaultModule.cooldownTime);
        else
        {
            this._cooldownTimer = 0.0f;
            foreach (Geometry g in this._shapes)
                g._meshRenderer.enabled = false;
            foreach (dfLabel label in this._labels)
                label.IsVisible = false;
        }
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        if (projectile is not PrecisionProjectile pp)
            return;

        pp.isCrit = this.canDoCrit;
        pp.target = this._targetActor;
        pp.baseData.damage *= GetDamageMultFromFocusTime();
        if ((this._lastRebound ?? this._lastContact) is Vector2 impactPos)
        {
            float velMag = this.canDoCrit ? 5f : 2f;
            // shot comes from lastContact if we're rebounding, or from barrel otherwise
            Vector2 baseVel = (this._lastRebound.HasValue ? this._lastContact.Value : this.gun.barrelOffset.position) - impactPos;
            CwaffVFX.SpawnBurst(prefab: _MathSymbols, numToSpawn: this.canDoCrit ? 20 : 6, basePosition: impactPos,
                positionVariance: 1f, baseVelocity: velMag * baseVel.normalized, velocityVariance: velMag - 1f, velType: CwaffVFX.Vel.Radial,
                lifetime: 0.5f, fadeOutTime: 0.5f, randomFrame: true);
        }
        DoTrailParticles(this.canDoCrit);
    }

    public override void OnPostFired(PlayerController player, Gun gun)
    {
        base.OnPostFired(player, gun);
        ResetCalculations(postShotCooldown: true);
        float velMag = 3f;
        CwaffVFX.SpawnBurst(prefab: _MathSymbols, numToSpawn: 8, basePosition: gun.barrelOffset.position,
            positionVariance: 1f, baseVelocity: gun.gunAngle.ToVector(velMag), velocityVariance: velMag - 1f, velType: CwaffVFX.Vel.Radial,
            lifetime: 0.5f, fadeOutTime: 0.5f, randomFrame: true);
    }

    private float GetDamageMultFromFocusTime()
    {
        return Mathf.Clamp01((1f + Mathf.Floor(this._drawTimer / _PHASE_TIME)) / this._maxDrawablePhase);
    }

    public void DoTrailParticles(bool isCrit)
    {
        const float TARGET_GAP = 1.5f;
        const float TARGET_CRIT_GAP = 0.3f;

        if (!this._lastContact.HasValue)
            return;

        float gapMag = (isCrit ? TARGET_CRIT_GAP : TARGET_GAP);
        for (int n = 0; n < 2; ++n)
        {
            if (n == 1 && !this._lastRebound.HasValue)
                return;
            Vector2 start = n == 0 ? this.gun.barrelOffset.position : this._lastContact.Value;
            Vector2 end = n == 0 ? this._lastContact.Value : this._lastRebound.Value;
            Vector2 delta = end - start;
            Vector2 dnorm = delta.normalized;
            Vector2 gap = gapMag * dnorm;
            Vector2 perp = dnorm.Rotate(90f);
            int numParticles = Mathf.FloorToInt(delta.magnitude / gapMag);
            for (int i = 0; i < numParticles; ++i)
              CwaffVFX.Spawn(prefab: _MathSymbols, position: start + i * gap,
                velocity: (UnityEngine.Random.Range(1f, 4f) *  (Lazy.CoinFlip() ? perp : -perp)).Rotate(UnityEngine.Random.Range(1f, 10f)),
                lifetime: 0.5f, fadeOutTime: 0.5f, randomFrame: true);
        }
    }

    public bool OnlyLivingEnemies(SpeculativeRigidbody body)
    {
        return !(body && body.aiActor && body.aiActor.IsHostile());
    }

    private float[] phaseCompetion = new float[_MAX_PHASE];
    public override void Update()
    {
        const float AIM_CIRCLE_MAG = 3f;
        const float MAX_PIXEL_MAG_CHANGE_PER_SECOND = 64f;
        const float MAX_ANGLE_CHANGE_PER_SECOND = 48f;
        const float FREEZE_TIME = 0.1f;  // time to pause redrawing when recalculating

        base.Update();
        float dtime = BraveTime.DeltaTime;
        if (dtime == 0.0f)
            return;
        if (this.PlayerOwner is not PlayerController pc)
            return;

        if (this._cooldownTimer > 0)
        {
            this._cooldownTimer = Mathf.Max(this._cooldownTimer - dtime, 0f);
            float fadeoutDelta = dtime / this.gun.DefaultModule.cooldownTime;
            float fadeoutAbs = this._cooldownTimer / this.gun.DefaultModule.cooldownTime;
            foreach (Geometry g in this._shapes)
                g._meshRenderer.material.SetColor("_OverrideColor", g.color.WithAlpha(fadeoutAbs));
            foreach (dfLabel label in this._labels)
            {
                label.Opacity = Mathf.Max(label.Opacity - fadeoutDelta, 0f);
                StabilizeLabel(label);
            }
            return;
        }

        this._freezeTimer = Mathf.Max(this._freezeTimer - dtime, 0f);
        if (this._freezeTimer == 0f)
        {
            this._drawTimer += dtime;
            this._soundTimer += dtime;
            if (this._soundTimer >= _SOUND_GAP && this._drawTimer <= this._maxDrawablePhase * _PHASE_TIME + 0.05f)
            {
                this._soundTimer -= _SOUND_GAP;
                base.gameObject.Play("sextant_calculate_sound");
            }
        }

        // variable setup
        float uiScale = Pixelator.Instance.ScaleTileScale / Pixelator.Instance.CurrentTileScale; // 1.33, usually
        float fontSizeToPixels = uiScale / C.PIXELS_PER_CELL;
        Gun gun = pc.CurrentGun;
        ProjectileModule mod = gun.DefaultModule;
        float accMult = pc.stats.GetStatValue(PlayerStats.StatType.Accuracy);
        Vector2 basePos = pc.sprite.WorldBottomCenter;
        Vector2 barrelPos = gun.barrelOffset.position + gun.gunAngle.EulerZ() * mod.positionOffset;
        float spread = mod.angleVariance * accMult;
        float baseShotAngle = (gun.gunAngle + gun.m_moduleData[mod].alternateAngleSign * mod.angleFromAim).Clamp360();
        int enemyMask = CollisionMask.LayerToMask(CollisionLayer.EnemyHitBox);
        int wallMask = CollisionMask.LayerToMask(CollisionLayer.HighObstacle);
        Vector2 targetContact = Vector2.zero;
        Vector2 targetNormal = Vector2.zero;
        Vector2 shotVector = baseShotAngle.ToVector();
        float distanceToTarget = 5f;
        PixelCollider bodyCollider = null;
        Vector2 reboundVector = baseShotAngle.ToVector();
        float reboundMag = 1f;

        Color aimColor     = Color.red;
        Color spreadColor  = ExtendedColours.vibrantOrange;
        Color magColor     = Color.green;
        Color reboundColor = Color.cyan;
        Color bboxColor    = Color.yellow;
        Color lockonColor  = Color.magenta;
        if (this.canDoCrit)
        {
            Color critColor = Color.Lerp(Color.white, Color.gray, Mathf.Abs(Mathf.Sin(10f * BraveTime.ScaledTimeSinceStartup)));
            aimColor     = critColor;
            spreadColor  = critColor;
            magColor     = critColor;
            reboundColor = critColor;
            bboxColor    = critColor;
            lockonColor  = critColor;
        }

        // calculations
        RaycastResult result;
        AIActor lastTargetActor = this._targetActor;
        this._targetActor = null;
        this._lastContact = null;
        this._lastRebound = null;
        bool canDoCritThisFrame = false;

        bool hitWall = PhysicsEngine.Instance.Raycast(barrelPos, shotVector, 999f, out result, true, false, wallMask);
        if (hitWall && result.Normal != Vector2.zero)
        {
            distanceToTarget = result.Distance;
            targetContact = result.Contact;
            targetNormal = result.Normal;

            //NOTE: rotate our angle a bit and verify the wall normal matches. if it doesn't, rotate it again and use it as the tiebreaker
            //      this is an attempt to get around an annoying bug when hitting the corner of a wall
            if (PhysicsEngine.Instance.Raycast(barrelPos, shotVector.Rotate(1f), 999f, out result, true, false, enemyMask, null, false))
            {
                if (result.Normal != targetNormal)
                {
                    if (PhysicsEngine.Instance.Raycast(barrelPos, shotVector.Rotate(-1f), 999f, out result, true, false, enemyMask, null, false))
                        targetNormal = result.Normal;
                }
            }
        }

        bool hitLivingEnemyDirectly = PhysicsEngine.Instance.Raycast(barrelPos, shotVector, 999f, out result, false, true, enemyMask, rigidbodyExcluder: OnlyLivingEnemies);
        if (hitLivingEnemyDirectly && hitWall && result.Distance > distanceToTarget)
            hitLivingEnemyDirectly = false; // wall interrupted our trajectory
        if (hitLivingEnemyDirectly)
        {
            hitWall = false; // enemy interrupted our trajectory
            distanceToTarget = result.Distance;
            targetContact = result.Contact;
            targetNormal = result.Normal;
            bodyCollider = result.OtherPixelCollider;
            this._targetActor = result.SpeculativeRigidbody.gameActor as AIActor;
        }

        if (targetNormal.x != 0)
            reboundVector = reboundVector.WithX(-reboundVector.x);
        if (targetNormal.y != 0)
            reboundVector = reboundVector.WithY(-reboundVector.y);
        if (hitWall)
        {
            bool secondContact = PhysicsEngine.Instance.Raycast(targetContact, reboundVector, 999f, out result, false, true, enemyMask, rigidbodyExcluder: OnlyLivingEnemies);
            if (secondContact)
            {
                reboundMag = result.Distance;
                bodyCollider = result.OtherPixelCollider;
                this._targetActor = result.SpeculativeRigidbody.gameActor as AIActor;
                this._lastRebound = this._targetActor.CenterPosition;
            }
        }

        RaycastResult.Pool.Free(ref result);
        this._lastContact = targetContact;

        // check if we need to reset any timers
        int maxPhase;
        float angleChange = this._lastAimAngle.AbsAngleTo(gun.gunAngle);
        this._slidingAngleWindow = Mathf.Max(0f, this._slidingAngleWindow + angleChange - dtime * MAX_ANGLE_CHANGE_PER_SECOND);
        float magChange = C.PIXELS_PER_TILE * Mathf.Abs(this._lastTargetMag - distanceToTarget);
        this._slidingMagWindow = Mathf.Max(0f, this._slidingMagWindow + magChange - dtime * MAX_PIXEL_MAG_CHANGE_PER_SECOND);
        if (!this._targetActor || !this._targetActor.IsHostile() || this._slidingAngleWindow > MAX_ANGLE_CHANGE_PER_SECOND)
        {
            maxPhase = 0;
            this._slidingAngleWindow = 0f;
            this._slidingMagWindow = 0f;
        }
        else if (this._lastSpread != spread)
        {
            maxPhase = 1;
            this._slidingMagWindow = 0f;
        }
        else if (this._slidingMagWindow > MAX_PIXEL_MAG_CHANGE_PER_SECOND)
        {
            maxPhase = 2;
            this._slidingMagWindow = 0f;
        }
        else if (this._lastNormal != targetNormal)
            maxPhase = 3;
        else if (lastTargetActor != this._targetActor)
            maxPhase = 4;
        else
            maxPhase = _MAX_PHASE;
        if (maxPhase < _MAX_PHASE)
        {
            float maxTimer = maxPhase * _PHASE_TIME;
            if (this._drawTimer > maxTimer)
            {
                this._drawTimer = maxTimer;
                this._soundTimer = 0.0f;
                this._freezeTimer = FREEZE_TIME;
            }
        }
        this._lastAimAngle  = gun.gunAngle;
        this._lastSpread    = spread;
        this._lastTargetMag = distanceToTarget;
        this._lastNormal    = targetNormal;
        this._lastCollider  = bodyCollider;

        // check phase completion
        for (int i = 0; i < _MAX_PHASE; ++i)
            phaseCompetion[i] = Mathf.Clamp01(this._drawTimer / _PHASE_TIME - i);
        int currentPhase = 0;
        float curPhaseCompletion;

        // phase 1: aim angle
        curPhaseCompletion = phaseCompetion[currentPhase++];
        this._aimAngleArc.Setup(Geometry.Shape.CIRCLE, aimColor, pos: barrelPos, radius: AIM_CIRCLE_MAG, angle: baseShotAngle.Clamp360(), arc: 180f * curPhaseCompletion);
        if (_UseUnicodeFont)
            this._shotAngleLabel.Text = $"θ={Mathf.RoundToInt(pc.m_currentGunAngle.Clamp180())}°";
        else
            this._shotAngleLabel.Text = $"{Mathf.RoundToInt(pc.m_currentGunAngle.Clamp180())} deg";
        this._shotAngleLabel.Color = aimColor;
        this._shotAngleLabel.Opacity = curPhaseCompletion;
        PlaceLabel(this._shotAngleLabel, barrelPos + baseShotAngle.ToVector(AIM_CIRCLE_MAG + 0.125f) + (baseShotAngle - 90f).ToVector(1.5f), baseShotAngle - 90f);

        // phase 2: aim spread
        curPhaseCompletion = phaseCompetion[currentPhase++];
        float approxSpread = spread * curPhaseCompletion;
        this._leftAdjSpread.Setup(Geometry.Shape.DASHEDLINE, spreadColor, pos: barrelPos, radius: distanceToTarget * curPhaseCompletion, angle: (baseShotAngle - approxSpread).Clamp360());
        this._rightAdjSpread.Setup(Geometry.Shape.DASHEDLINE, spreadColor, pos: barrelPos, radius: distanceToTarget * curPhaseCompletion, angle: (baseShotAngle + approxSpread).Clamp360());
        if (_UseUnicodeFont)
            this._spreadLabel.Text = $"±{approxSpread:0.0}°";
        else
            this._spreadLabel.Text = $"+/-{approxSpread:0.0} deg";
        this._spreadLabel.Color = spreadColor;
        this._spreadLabel.Opacity = curPhaseCompletion;
        PlaceLabel(this._spreadLabel, barrelPos + baseShotAngle.ToVector(AIM_CIRCLE_MAG + 0.625f) + (baseShotAngle - 90f).ToVector(1.75f), baseShotAngle - 90f);

        // phase 3: aim distance
        curPhaseCompletion = phaseCompetion[currentPhase++];
        this._perfectShot.Setup(Geometry.Shape.LINE, magColor, pos: barrelPos, radius: distanceToTarget * curPhaseCompletion, angle: baseShotAngle.Clamp360());
        if (_UseUnicodeFont)
            this._shotDistanceLabel.Text = $"Δ={Mathf.RoundToInt(C.PIXELS_PER_TILE * distanceToTarget * curPhaseCompletion)}";
        else
            this._shotDistanceLabel.Text = $"dx={Mathf.RoundToInt(C.PIXELS_PER_TILE * distanceToTarget * curPhaseCompletion)}";
        this._shotDistanceLabel.Color = magColor;
        this._shotDistanceLabel.Opacity = curPhaseCompletion;
        PlaceLabel(this._shotDistanceLabel,
          barrelPos + baseShotAngle.ToVector(0.5f * distanceToTarget * curPhaseCompletion) + (baseShotAngle - 90f).ToVector(-0.25f), baseShotAngle);

        // phase 4: rebound distance and angle
        if (hitWall)
        {
            curPhaseCompletion = phaseCompetion[currentPhase++];
            float reboundAngle = reboundVector.ToAngle();
            this._reboundShot.Setup(Geometry.Shape.LINE, reboundColor, pos: targetContact, radius: reboundMag * curPhaseCompletion, angle: reboundAngle);

            float reboundArcDiameter = Mathf.Min(2f, 0.5f * distanceToTarget);
            float reboundArcRadius = 0.5f * reboundArcDiameter;
            Vector2 reboundArcCenter = targetContact + reboundArcRadius * targetNormal;
            float reboundTheta = 2f * (baseShotAngle + 180f).AbsAngleTo(reboundAngle);
            this._reboundArc.Setup(Geometry.Shape.CIRCLE, reboundColor, pos: reboundArcCenter, radius: reboundArcRadius,
              angle: targetNormal.ToAngle(), arc: reboundTheta * curPhaseCompletion);

            if (_UseUnicodeFont)
                this._reboundAngleLabel.Text = $"∠{Mathf.RoundToInt(0.5f * reboundTheta * curPhaseCompletion)}°";
            else
                this._reboundAngleLabel.Text = $"{Mathf.RoundToInt(0.5f * reboundTheta * curPhaseCompletion)} deg";
            this._reboundAngleLabel.Color = reboundColor;
            this._reboundAngleLabel.Opacity = curPhaseCompletion;
            PlaceLabel(this._reboundAngleLabel, targetContact + (reboundArcDiameter + 0.125f) * targetNormal, targetNormal.ToAngle() - 90f);
        }
        else
        {
            this._reboundShot._meshRenderer.enabled = false;
            this._reboundArc._meshRenderer.enabled = false;
            this._reboundAngleLabel.IsVisible = false;
        }

        if (bodyCollider != null)
        {
            // phase 5: bounding box
            curPhaseCompletion = phaseCompetion[currentPhase++];
            Rect bounds = new Rect(bodyCollider.UnitBottomLeft, bodyCollider.UnitDimensions).Inset(-0.5f);
            Vector2 center = bounds.center;
            Vector2 tl = center + curPhaseCompletion * (new Vector2(bounds.xMin, bounds.yMax) - center);
            Vector2 bl = center + curPhaseCompletion * (new Vector2(bounds.xMin, bounds.yMin) - center);
            Vector2 tr = center + curPhaseCompletion * (new Vector2(bounds.xMax, bounds.yMax) - center);
            Vector2 br = center + curPhaseCompletion * (new Vector2(bounds.xMax, bounds.yMin) - center);
            this._topBbox.Setup(Geometry.Shape.DASHEDLINE, bboxColor, pos: tl, pos2: tr);
            this._bottomBbox.Setup(Geometry.Shape.DASHEDLINE, bboxColor, pos: bl, pos2: br);
            this._leftBbox.Setup(Geometry.Shape.DASHEDLINE, bboxColor, pos: tl, pos2: bl);
            this._rightBbox.Setup(Geometry.Shape.DASHEDLINE, bboxColor, pos: tr, pos2: br);

            this._widthLabel.Text = $"w={Mathf.RoundToInt(C.PIXELS_PER_TILE * (tr.x - tl.x))}";
            this._widthLabel.Color = bboxColor;
            this._widthLabel.Opacity = curPhaseCompletion;
            PlaceLabel(this._widthLabel, 0.5f * (tr + tl) + new Vector2(0f, 0.25f), 0f);

            this._heightLabel.Text = $"h={Mathf.RoundToInt(C.PIXELS_PER_TILE * (tr.y - br.y))}";
            this._heightLabel.Color = bboxColor;
            this._heightLabel.Opacity = curPhaseCompletion;
            //HACK: horrendous math since I can't get labels to be anything other than bottom-center aligned...fix later maybe
            PlaceLabel(this._heightLabel, 0.5f * (tr + br) + new Vector2(
                this._heightLabel.Size.x * 0.5f * fontSizeToPixels + 0.25f, -this._heightLabel.Size.y * 0.5f * fontSizeToPixels),
              0f);

            // phase 6: weak point
            curPhaseCompletion = phaseCompetion[currentPhase++];
            Vector2 l = 0.5f * (tl + bl);
            Vector2 r = 0.5f * (tr + br);
            Vector2 t = 0.5f * (tl + tr);
            Vector2 b = 0.5f * (bl + br);
            float radius = 0.5f * Mathf.Min(r.x - l.x, t.y - b.y);
            float arcLength = 90f * curPhaseCompletion;
            this._weakPointL.Setup(Geometry.Shape.DASHEDLINE, lockonColor, pos: l, pos2: l + curPhaseCompletion * (center - l));
            this._weakPointR.Setup(Geometry.Shape.DASHEDLINE, lockonColor, pos: r, pos2: r + curPhaseCompletion * (center - r));
            this._weakPointT.Setup(Geometry.Shape.DASHEDLINE, lockonColor, pos: t, pos2: t + curPhaseCompletion * (center - t));
            this._weakPointB.Setup(Geometry.Shape.DASHEDLINE, lockonColor, pos: b, pos2: b + curPhaseCompletion * (center - b));
            this._weakPointArcR.Setup(Geometry.Shape.CIRCLE, lockonColor, pos: center, radius: radius, angle:  45f, arc: arcLength);
            this._weakPointArcT.Setup(Geometry.Shape.CIRCLE, lockonColor, pos: center, radius: radius, angle: 135f, arc: arcLength);
            this._weakPointArcL.Setup(Geometry.Shape.CIRCLE, lockonColor, pos: center, radius: radius, angle: 225f, arc: arcLength);
            this._weakPointArcB.Setup(Geometry.Shape.CIRCLE, lockonColor, pos: center, radius: radius, angle: 315f, arc: arcLength);

            canDoCritThisFrame = (curPhaseCompletion >= 1.0f);
        }
        else
        {
            this._topBbox._meshRenderer.enabled = false;
            this._bottomBbox._meshRenderer.enabled = false;
            this._leftBbox._meshRenderer.enabled = false;
            this._rightBbox._meshRenderer.enabled = false;
            this._weakPointL._meshRenderer.enabled = false;
            this._weakPointR._meshRenderer.enabled = false;
            this._weakPointT._meshRenderer.enabled = false;
            this._weakPointB._meshRenderer.enabled = false;
            this._weakPointArcL._meshRenderer.enabled = false;
            this._weakPointArcR._meshRenderer.enabled = false;
            this._weakPointArcT._meshRenderer.enabled = false;
            this._weakPointArcB._meshRenderer.enabled = false;
            this._widthLabel.IsVisible = false;
            this._heightLabel.IsVisible = false;
        }

        this._maxDrawablePhase = currentPhase;
        if (canDoCritThisFrame && !this.canDoCrit)
            base.gameObject.Play("sextant_crit_ready_sound");
        this.canDoCrit = canDoCritThisFrame;
    }

    internal class LabelExt : MonoBehaviour
    {
        public Vector2 lastPos;
        public float lastRot;
    }

    private static dfLabel MakeNewLabel()
    {
        dfLabel label = UnityEngine.Object.Instantiate(GameUIRoot.Instance.p_needsReloadLabel.gameObject, GameUIRoot.Instance.transform).GetComponent<dfLabel>();
        if (_UseUnicodeFont)
        {
            label.Font = (ResourceCache.Acquire("Alternate Fonts/JackeyFont12_DF") as GameObject).GetComponent<dfFont>();
            label.Atlas = (label.Font as dfFont).Atlas;
            label.TextScale = 2.0f;
        }
        label.transform.localScale = Vector3.one / GameUIRoot.GameUIScalar;
        label.Anchor = dfAnchorStyle.CenterVertical | dfAnchorStyle.CenterHorizontal;
        label.TextAlignment = TextAlignment.Center;
        label.VerticalAlignment = dfVerticalAlignment.Middle;
        label.Opacity = 1f;
        label.Text = string.Empty;
        label.gameObject.SetActive(true);
        // label.enabled = true;
        label.IsVisible = true;
        label.gameObject.AddComponent<LabelExt>();
        return label;
    }

    private static void StabilizeLabel(dfLabel label)
    {
        LabelExt le = label.gameObject.GetComponent<LabelExt>();
        PlaceLabel(label, le.lastPos, le.lastRot);
    }

    private static void PlaceLabel(dfLabel label, Vector2 pos, float rot)
    {
        rot = rot.Clamp180();
        Vector2 finalPos = pos;
        float uiScale = Pixelator.Instance.ScaleTileScale / Pixelator.Instance.CurrentTileScale; // 1.33, usually
        float fontSizeToPixels = uiScale / C.PIXELS_PER_CELL;
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
            finalPos += (rot - 90f).ToVector(label.Size.y * fontSizeToPixels);  //WARN: guessing at the math here...
        }
        label.transform.position = dfFollowObject.ConvertWorldSpaces(
            finalPos,
            GameManager.Instance.MainCameraController.Camera,
            GameUIRoot.Instance.m_manager.RenderCamera).WithZ(0f);
        label.transform.position = label.transform.position.QuantizeFloor(adj);
        label.transform.localRotation = rot.EulerZ();
        label.IsVisible = true;
        LabelExt le = label.gameObject.GetComponent<LabelExt>();
        le.lastPos = pos;
        le.lastRot = rot;
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
            this.arc      = arc    ?? this.arc;
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
        this._meshRenderer.enabled = true;
    }

    private void OnDestroy()
    {
        if (this._meshObject)
            UnityEngine.Object.Destroy(this._meshObject);
    }
}

public class PrecisionProjectile : Projectile
{
    public AIActor target = null;
    public bool isCrit = false;

    public override void Move()
    {
        if (target && target.IsNormalEnemy && target.healthHaver && !target.IsGone)
        {
            if (isCrit)
            {
                if (target.healthHaver.IsBoss || target.healthHaver.IsSubboss)
                    target.healthHaver.ApplyDamage(0.2f * target.healthHaver.AdjustedMaxHealth, target.CenterPosition - base.transform.position.XY(), "Sextant", ignoreDamageCaps: true);
                else
                    target.healthHaver.ApplyDamage(float.MaxValue, target.CenterPosition - base.transform.position.XY(), "Sextant", ignoreDamageCaps: true);
                base.gameObject.Play("sextant_critical_hit_sound");
            }
            else
                target.healthHaver.ApplyDamage(baseData.damage, target.CenterPosition - base.transform.position.XY(), "Sextant");
        }
        DieInAir(true, false, false, false);
    }
}
