namespace CwaffingTheGungy;

public class Sextant : CwaffGun
{
    public static string ItemName         = "Sextant";
    public static string ShortDescription = "Deadly Precision";
    public static string LongDescription  = "Fires a calculated, single-target shot that deals more damage the longer Sextant is trained on its target before firing. When completely locked on, fires a critical shot that heavily damages bosses and instantly kills non-boss enemies. Reloading toggles focus mode, which slows down movement speed to assist with aiming.";
    public static string Lore             = "An essential instrument among mariners for centuries, repurposed against all odds as a deadly weapon. Holding it in your hands evokes painful memories of 10th grade trigonometry lessons. Should you ever be allowed a few seconds of respite in combat, you're certain this device will let you hit your enemies physically and psychologically *exactly* where it hurts most.";

    private const float _PHASE_TIME = 0.40f;
    private const float _SOUND_GAP  = _PHASE_TIME / 4f;
    private const int _MAX_PHASE    = 6;

    internal static readonly bool _UseUnicodeFont = true;
    internal static GameObject _MathSymbols = null;
    internal static CwaffTrailController _SextantTrailPrefab = null;

    private dfLabel _shotAngleLabel     = null;
    private dfLabel _spreadLabel        = null;
    private dfLabel _shotDistanceLabel  = null;
    private dfLabel _reboundAngleLabel  = null;
    private dfLabel _widthLabel         = null;
    private dfLabel _heightLabel        = null;
    private dfLabel _damageLabel        = null;
    private List<dfLabel> _labels       = new();

    private Geometry _aimAngleArc       = null;
    private Geometry _perfectShot       = null;
    private Geometry _reboundShot       = null;
    private Geometry _reboundArc        = null;
    private Geometry _leftBaseSpread    = null;
    private Geometry _rightBaseSpread   = null;
    private Geometry _leftFocus         = null;
    private Geometry _rightFocus        = null;
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
    private float _cooldownTimer        = 0.0f;
    private float _slidingAngleWindow   = 0.0f;
    private float _slidingMagWindow     = 0.0f;
    private float _timeFocusing         = 0.0f;
    private PixelCollider _lastCollider = null;
    private AIActor _targetActor         = null;
    private Vector2 _lastNormal         = Vector2.zero;
    private Vector2? _lastContact       = null;
    private Vector2? _lastRebound       = null;

    private bool _focused = false;

    public bool canDoCrit = false;

    public static void Init()
    {
        Lazy.SetupGun<Sextant>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.B, gunClass: GunClass.RIFLE, reloadTime: 0.9f, ammo: 162, shootFps: 14, reloadFps: 4,
            muzzleFrom: Items.Mailbox, fireAudio: "sextant_shoot_sound", carryOffset: new IntVector2(6, 0))
          .InitSpecialProjectile<PrecisionProjectile>(GunData.New(sprite: null, clipSize: 1, cooldown: 0.0f, shootStyle: ShootStyle.SemiAutomatic,
            damage: 50.0f, speed: 25f, range: 18f, force: 12f, invisibleProjectile: true, customClip: true));

        _MathSymbols = VFX.Create("math_symbols", loops: false);

        _SextantTrailPrefab = CwaffTrailController.Convert(Items.Awp.AsGun().DefaultModule.projectiles[0].gameObject
          .GetComponentInChildren<TrailController>());
            _SextantTrailPrefab.usesStartAnimation = false;
            _SextantTrailPrefab.globalTimer = 0.0f;
            _SextantTrailPrefab.cascadeTimer = 0.02f;
    }

    private void Start()
    {
        this._shapes.Add(this._perfectShot = new GameObject().AddComponent<Geometry>());
        this._shapes.Add(this._reboundShot = new GameObject().AddComponent<Geometry>());
        this._shapes.Add(this._reboundArc = new GameObject().AddComponent<Geometry>());
        this._shapes.Add(this._leftBaseSpread = new GameObject().AddComponent<Geometry>());
        this._shapes.Add(this._rightBaseSpread = new GameObject().AddComponent<Geometry>());
        this._shapes.Add(this._leftFocus = new GameObject().AddComponent<Geometry>());
        this._shapes.Add(this._rightFocus = new GameObject().AddComponent<Geometry>());
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
        this._labels.Add(this._damageLabel = MakeNewLabel());
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        base.OnSwitchedAwayFromThisGun();
        ResetCalculations();
        SetFocus(false);
    }

    public override void OnSwitchedToThisGun()
    {
        base.OnSwitchedToThisGun();
        ResetCalculations();
        SetFocus(false);
    }

    public override void OnPlayerPickup(PlayerController player)
    {
        base.OnPlayerPickup(player);
        SetFocus(false);
        player.OnRollStarted += this.OnDodgeRoll;
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        base.OnDroppedByPlayer(player);
        player.OnRollStarted -= this.OnDodgeRoll;
        SetFocus(false);
    }

    public override void OnDestroy()
    {
        if (this.PlayerOwner)
        {
            this.PlayerOwner.OnRollStarted -= this.OnDodgeRoll;
            SetFocus(false);
        }
        base.OnDestroy();
    }

    public override void OnFullClipReload(PlayerController player, Gun gun)
    {
        base.OnFullClipReload(player, gun);
        if (!gun.IsReloading && !player.IsDodgeRolling && player.AcceptingNonMotionInput)
        {
            SetFocus(!this._focused);
            base.gameObject.Play(this._focused ? "sextant_focus_sound" : "sextant_unfocus_sound");
        }
    }

    private void ForceFireGun()
    {
        if (this.PlayerOwner is not PlayerController pc)
            return;
        if (pc.CurrentGun != this.gun)
            return;
        pc.forceFireDown = true;
    }

    private void SetFocus(bool focus)
    {
        if (focus == this._focused)
            return;

        this._focused = focus;
        this.gun.RemoveStatFromGun(StatType.MovementSpeed);
        this.gun.AddStatToGun(StatType.MovementSpeed.Mult(focus ? 0.25f : 1.0f));
        this.PlayerOwner.stats.RecalculateStats(this.PlayerOwner);
    }

    private void ResetCalculations(bool postShotCooldown = false)
    {
        this.canDoCrit = false;
        this._drawTimer = 0.0f;
        this._soundTimer = 0.0f;
        this._freezeTimer = 0.0f;
        this._slidingAngleWindow = 0.0f;
        this._slidingMagWindow = 0.0f;
        this._timeFocusing = 0.0f;
        this._phase = 0;
        if (postShotCooldown)
            this._cooldownTimer = Mathf.Max(this._cooldownTimer, this.gun.AdjustedReloadTime);
        else
        {
            this._cooldownTimer = 0.0f;
            foreach (Geometry g in this._shapes)
                g._meshRenderer.enabled = false;
            foreach (dfLabel label in this._labels)
            {
                label.Opacity = 0.0f;
                label.IsVisible = false;
            }
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
        SetFocus(false);
    }

    private void OnDodgeRoll(PlayerController player, Vector2 dirVec)
    {
        SetFocus(false);
    }

    private float GetDamageMultFromFocusTime()
    {
        return Mathf.Clamp01((/*1f + Mathf.Floor*/(this._drawTimer / _PHASE_TIME)) / this._maxDrawablePhase);
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
            // CwaffTrailController.Spawn(SubtractorBeam._GreenTrailPrefab, start, end);
            CwaffTrailController.Spawn(_SextantTrailPrefab, start, end);
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

    public bool Untargetable(SpeculativeRigidbody body)
    {
        bool targetable = body && body.aiActor is AIActor e && e.isActiveAndEnabled && e.healthHaver is HealthHaver hh && hh.IsAlive;
        return !targetable;
    }

    private float[] phaseCompetion = new float[_MAX_PHASE];
    public override void Update()
    {
        const float AIM_CIRCLE_MAG = 3f;
        const float MAX_PIXEL_MAG_CHANGE_PER_FRAME = 40f;
        const float MAX_ANGLE_CHANGE_PER_SECOND = 48f;
        const float FREEZE_TIME = 0.1f;  // time to pause redrawing when recalculating

        base.Update();
        float dtime = BraveTime.DeltaTime;
        if (dtime == 0.0f)
            return;
        if (this.PlayerOwner is not PlayerController pc)
            return;
        pc.forceFireDown = false;

        if (!this.gun.IsReloading && this.gun.ClipShotsRemaining < Mathf.Min(this.gun.ClipCapacity, this.gun.CurrentAmmo))
            this.gun.Reload(); // force reload while we're not at max clip capacity

        if (this.gun.IsReloading || this._cooldownTimer > 0)
        {
            this._cooldownTimer = Mathf.Max(this._cooldownTimer - dtime, 0f);
            float fadeoutDelta = dtime / this.gun.AdjustedReloadTime;
            float fadeoutAbs = this._cooldownTimer / this.gun.AdjustedReloadTime;
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
        //NOTE: this method has a small discrepancy from the cursor on mouse and a large discrepancy on controller. using unadjusted aim point for now
        // float baseShotAngle = (gun.gunAngle + gun.m_moduleData[mod].alternateAngleSign * mod.angleFromAim).Clamp360();
        float baseShotAngle = (pc.unadjustedAimPoint.XY() - barrelPos).ToAngle();
        //NOTE: only CollisionLayer.EnemyHitBox should be needed in theory, but Gatling Gull and possibly some other have some weird collision...
        int enemyMask = CollisionMask.LayerToMask(CollisionLayer.EnemyHitBox, CollisionLayer.EnemyCollider, CollisionLayer.BulletBlocker);
        int wallMask = CollisionMask.LayerToMask(CollisionLayer.HighObstacle);
        Vector2 targetContact = Vector2.zero;
        Vector2 targetNormal = Vector2.zero;
        Vector2 shotVector = baseShotAngle.ToVector();
        Vector2 reboundVector = shotVector;
        float distanceToTarget = 5f;
        PixelCollider bodyCollider = null;
        float reboundMag = 1f;

        Color aimColor     = Color.red;
        Color focusColor   = ExtendedColours.pink;
        Color spreadColor  = ExtendedColours.vibrantOrange;
        Color magColor     = Color.green;
        Color reboundColor = Color.cyan;
        Color bboxColor    = Color.yellow;
        Color lockonColor  = Color.magenta;
        if (this.canDoCrit)
        {
            Color critColor = Color.Lerp(Color.white, Color.gray, Mathf.Abs(Mathf.Sin(10f * BraveTime.ScaledTimeSinceStartup)));
            aimColor     = critColor;
            focusColor   = critColor;
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

            if (PhysicsEngine.Instance.Raycast(barrelPos, shotVector.Rotate(1f), 999f, out result, true, false, wallMask))
            {
                //NOTE: rotate our angle a bit and verify the wall normal matches. if it doesn't, rotate it again and use it as the tiebreaker
                //      this is an attempt to get around an annoying bug when hitting the corner of a wall
                if (result.Normal != targetNormal)
                {
                    if (PhysicsEngine.Instance.Raycast(barrelPos, shotVector.Rotate(-1f), 999f, out result, true, false, wallMask))
                        targetNormal = result.Normal;
                }
            }
        }

        bool hitLivingEnemyDirectly = PhysicsEngine.Instance.Raycast(barrelPos, shotVector, 999f, out result, false, true, enemyMask, rigidbodyExcluder: Untargetable);
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
            bool secondContact = PhysicsEngine.Instance.Raycast(targetContact, reboundVector, 999f, out result, false, true, enemyMask, rigidbodyExcluder: Untargetable);
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
        this._slidingMagWindow = Mathf.Max(0f, this._slidingMagWindow + magChange - dtime * MAX_PIXEL_MAG_CHANGE_PER_FRAME);
        if (!this._targetActor || this._slidingAngleWindow > MAX_ANGLE_CHANGE_PER_SECOND)
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
        else if (magChange > MAX_PIXEL_MAG_CHANGE_PER_FRAME)
        {
            maxPhase = 2;
            this._slidingMagWindow = 0f;
        }
        // else if (this._lastNormal != targetNormal) //NOTE: this causes more problems than it's worth, so disabling this particular check for nows
        // {
        //     System.Console.WriteLine($"max phase 3");
        //     maxPhase = 3;
        // }
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

        // phase 1a: aim angle
        curPhaseCompletion = phaseCompetion[currentPhase++];
        this._aimAngleArc.Setup(Geometry.Shape.CIRCLE, aimColor, pos: barrelPos, radius: AIM_CIRCLE_MAG, angle: baseShotAngle.Clamp360(), arc: 180f * curPhaseCompletion);
        if (_UseUnicodeFont)
            this._shotAngleLabel.Text = $"θ={Mathf.RoundToInt(pc.m_currentGunAngle.Clamp180())}°";
        else
            this._shotAngleLabel.Text = $"{Mathf.RoundToInt(pc.m_currentGunAngle.Clamp180())} deg";
        this._shotAngleLabel.Color = aimColor;
        this._shotAngleLabel.Opacity = curPhaseCompletion;
        PlaceLabel(this._shotAngleLabel, barrelPos + baseShotAngle.ToVector(AIM_CIRCLE_MAG + 0.125f) + (baseShotAngle - 90f).ToVector(1.5f), baseShotAngle - 90f);

        // phase 1b: aim angle
        this._timeFocusing = Mathf.Clamp(this._timeFocusing + (this._focused ? dtime : -dtime), 0f, _PHASE_TIME);
        float focusCompletion = Mathf.Clamp01(this._timeFocusing / _PHASE_TIME);
        Vector2 focusPoint = barrelPos + baseShotAngle.ToVector(AIM_CIRCLE_MAG);
        Vector2 leftPoint = barrelPos + (baseShotAngle + 90f).ToVector(AIM_CIRCLE_MAG);
        Vector2 rightPoint = barrelPos + (baseShotAngle - 90f).ToVector(AIM_CIRCLE_MAG);
        this._leftFocus.Setup(Geometry.Shape.LINE, focusColor, pos: focusPoint, pos2: Vector2.Lerp(focusPoint, leftPoint, focusCompletion));
        this._rightFocus.Setup(Geometry.Shape.LINE, focusColor, pos: focusPoint, pos2: Vector2.Lerp(focusPoint, rightPoint, focusCompletion));

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

        if (this._targetActor && this._targetActor.sprite)
        {
            // phase 5: bounding box
            curPhaseCompletion = phaseCompetion[currentPhase++];
            Bounds spriteBounds = this._targetActor.sprite.GetBounds();
            // Rect bounds = new Rect(bodyCollider.UnitBottomLeft, bodyCollider.UnitDimensions).Inset(-0.5f);
            Rect bounds = new Rect(this._targetActor.sprite.WorldBottomLeft, spriteBounds.size).Inset(-0.5f);
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

            // phase 6b: damage estimate
            HealthHaver targetHH = this._targetActor.healthHaver;
            float damageMult = GetDamageMultFromFocusTime();
            float damageEstimate = damageMult * this.gun.DefaultModule.projectiles[0].baseData.damage * pc.stats.GetStatValue(PlayerStats.StatType.Damage);
            if (targetHH.IsBoss)
                damageEstimate *= pc.stats.GetStatValue(PlayerStats.StatType.DamageToBosses);
            if ((this.canDoCrit || damageEstimate >= targetHH.GetCurrentHealth()) && this.Mastered)
                ForceFireGun();

            // if (this.canDoCrit)
            // {
            //     if (targetHH.IsBoss)
            //         damageEstimate = Mathf.Max(damageEstimate, PrecisionProjectile.PERCENT_BOSS_DAMAGE_ON_CRIT * targetHH.AdjustedMaxHealth);
            //     else
            //         damageEstimate = targetHH.AdjustedMaxHealth;
            // }
            // float percentDamageEstimate = Mathf.FloorToInt(100f * Mathf.Clamp01(damageEstimate / targetHH.AdjustedMaxHealth));
            // this._damageLabel.Text = $"{percentDamageEstimate}% dmg";
            // this._damageLabel.Color = lockonColor;
            // this._damageLabel.Opacity = damageMult;
            // PlaceLabel(this._damageLabel, bounds.max + new Vector2(0.5f, 0.5f), 0f);
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
            this._damageLabel.IsVisible = false;
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
    public const float PERCENT_BOSS_DAMAGE_ON_CRIT = 0.2f;

    public AIActor target = null;
    public bool isCrit = false;

    public override void Move()
    {
        if (target && target.IsNormalEnemy && target.healthHaver && !target.IsGone)
        {
            if (isCrit)
            {
                //NOTE: friction must be applied BEFORE damage or m_currentMinFriction prevents it from working
                StickyFrictionManager.Instance.RegisterCustomStickyFriction(0.75f, 0f, true);
                if (target.healthHaver.IsBoss || target.healthHaver.IsSubboss)
                    target.healthHaver.ApplyDamage(
                        Mathf.Max(PERCENT_BOSS_DAMAGE_ON_CRIT * target.healthHaver.AdjustedMaxHealth, baseData.damage),
                        target.CenterPosition - base.transform.position.XY(), "Sextant", ignoreDamageCaps: true);
                else
                {
                    target.DuplicateInWorldAsMesh().Dissipate(time: 1.5f, amplitude: 5f, progressive: true);
                    target.EraseFromExistenceWithRewards();
                }
                base.gameObject.Play("sextant_critical_hit_sound");
            }
            else
                target.healthHaver.ApplyDamage(baseData.damage, target.CenterPosition - base.transform.position.XY(), "Sextant");
        }
        DieInAir(true, false, false, false);
    }
}
