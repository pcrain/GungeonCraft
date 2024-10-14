namespace CwaffingTheGungy;

/*
    - figure out projectile interpolation so it doesn't whiff when moving too fast
        (hack for now, just make projectile bigger)
    - add visuals for the chains
*/

public class SpinCycle : CwaffGun
{
    public static string ItemName         = "Spin Cycle";
    public static string ShortDescription = "Bring it Around Town";
    public static string LongDescription  = "(ball and chain)";
    public static string Lore             = "TBD";

    private static VFXPool _Vfx  = null;
    private static VFXPool _Vfx2 = null;
    private static Projectile _TheProtoBall;
    private static Projectile _TheProtoChain;

    private Projectile _theCurBall = null;
    private BasicBeamController _theCurChain = null;

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<SpinCycle>(ItemName, ShortDescription, LongDescription, Lore);
            gun.DefaultModule.ammoCost            = 1;
            gun.DefaultModule.shootStyle          = ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle       = ProjectileSequenceStyle.Random;
            gun.reloadTime                        = 5f;
            gun.DefaultModule.angleVariance       = 0f;
            gun.DefaultModule.numberOfShotsInClip = 1;
            gun.quality                           = ItemQuality.A;
            gun.InfiniteAmmo                      = true;
            gun.SetAnimationFPS(gun.shootAnimation, 0);

        Projectile ball = gun.InitFirstProjectile(GunData.New(damage: 100.0f, speed: 0.0001f, force: 100.0f, range: 9999f));
            ball.BulletScriptSettings.surviveTileCollisions      = true;
            ball.BulletScriptSettings.surviveRigidbodyCollisions = true;
            ball.PenetratesInternalWalls = true;
            ball.pierceMinorBreakables   = true;
        _TheProtoBall = ball;

        PierceProjModifier pierce    = ball.gameObject.GetOrAddComponent<PierceProjModifier>();
            pierce.penetration           = 100000;
            pierce.penetratesBreakables  = true;

        Projectile chain = ball.Clone(GunData.New(damage: 1.0f, speed: 100.0f, force: 1.0f));
            chain.BulletScriptSettings.surviveTileCollisions      = true;
            chain.BulletScriptSettings.surviveRigidbodyCollisions = true;
            chain.PenetratesInternalWalls                         = true;
            chain.pierceMinorBreakables                           = true;
            _TheProtoChain = chain;

        PierceProjModifier pierce2    = chain.gameObject.GetOrAddComponent<PierceProjModifier>();
            pierce2.penetration           = 100000;
            pierce2.penetratesBreakables  = true;

        BasicBeamController chainBeam = chain.GenerateBeamPrefab(
            $"{C.MOD_INT_NAME}/Resources/BeamSprites/alphabeam_mid_001",
            new Vector2(15, 7),
            new Vector2(0, 4),
            new() {
                $"{C.MOD_INT_NAME}/Resources/BeamSprites/alphabeam_mid_001",
                // $"{C.MOD_INT_NAME}/Resources/BeamSprites/alphabeam_mid_002",
                // $"{C.MOD_INT_NAME}/Resources/BeamSprites/alphabeam_mid_003",
                // $"{C.MOD_INT_NAME}/Resources/BeamSprites/alphabeam_mid_004",
            },
            13,glowAmount:100,emissivecolouramt:100
        );
            chainBeam.boneType                         = BasicBeamController.BeamBoneType.Projectile;
            chainBeam.interpolateStretchedBones        = true;
            chainBeam.ContinueBeamArtToWall            = true;

        _Vfx = VFX.CreatePoolFromVFXGameObject(Items.MagicLamp.AsGun().DefaultModule.projectiles[0].hitEffects.overrideMidairDeathVFX);
        _Vfx2 = Items.TearJerker.AsGun().muzzleFlashEffects;
    }

    private void SetupBallAndChain(PlayerController p)
    {
        _theCurBall = SpawnManager.SpawnProjectile(
          _TheProtoBall.gameObject, p.CenterPosition, Quaternion.Euler(0f, 0f, 0f), true
          ).GetComponent<Projectile>();
            _theCurBall.Owner = p;
            _theCurBall.Shooter = p.specRigidbody;
            _theCurBall.RuntimeUpdateScale(3.0f);

        if (tieProjectilePositionToBeam)
        {
            _theCurChain = BeamAPI.FreeFireBeamFromAnywhere(
                _TheProtoChain, p, p.gameObject,
                Vector2.zero, this.forcedDirection, 1000000.0f, true, true
                ).GetComponent<BasicBeamController>();
        }

        this.forcedDirection = p.FacingDirection;
        this.facingLast      = p.FacingDirection;
        this.curMomentum     = 0;
        p.m_overrideGunAngle = this.forcedDirection;
    }

    private void DestroyBallAndChain()
    {
        if (this._theCurBall)
            this._theCurBall.DieInAir(true,false,false,true);
        this._theCurBall = null;
        if (!tieProjectilePositionToBeam)
            return;
        if (this._theCurChain)
            this._theCurChain.DestroyBeam();
        this._theCurChain = null;
    }

    public override void OnSwitchedToThisGun()
    {
        SetupBallAndChain(this.gun.CurrentOwner as PlayerController);
        base.OnSwitchedToThisGun();
    }

    public override void OnPlayerPickup(PlayerController player)
    {
        base.OnPlayerPickup(player);
        player.OnRollStarted += this.OnDodgeRoll;
        if (this.gun == player.CurrentGun)
            SetupBallAndChain(player);
    }

    private void OnDodgeRoll(PlayerController player, Vector2 dirVec)
    {
        DestroyBallAndChain();
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        base.OnDroppedByPlayer(player);
        player.OnRollStarted -= this.OnDodgeRoll;
        DestroyBallAndChain();
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        DestroyBallAndChain();
        if (!this.PlayerOwner)
            return;
        this.PlayerOwner.m_overrideGunAngle = null;
        RecomputePlayerSpeed(this.PlayerOwner,1.0f);
        base.OnSwitchedAwayFromThisGun();
    }

    private const float _MAX_MOMENTUM           = 18f;  //3.0 rotations per second @60FPS
    private const float _BALL_WEIGHT            = 75f;  //full speed in 0.5 seconds @60FPS
    private const float _MIN_CHAIN_LENGTH       = 1.0f;
    private const float _MAX_CHAIN_LENGTH       = 6.0f;
    private const float _AIR_FRICTION           = 0.995f;
    private const bool _RELATIVE_TO_LAST_FACING = true;
    private const float _MAX_ACCEL              = _MAX_MOMENTUM/_BALL_WEIGHT;

    private const bool _INFLUENCE_PLAYER_MOMENTUM = true;
    private const float _MIN_PLAYER_MOMENTUM      = 0.5f;
    private const float _MAX_PLAYER_MOMENTUM      = 1.5f;
    private const float _PLAYER_MOMENTUM_DELTA    = _MAX_PLAYER_MOMENTUM - _MIN_PLAYER_MOMENTUM;
    private const int _MAX_CHAIN_SEGMENTS         = 5;
    private const float _MIN_GAP_BETWEEN_SEGMENTS = 1.4f;

    // Very important variable, determines behavior dramatically
    private static bool tieProjectilePositionToBeam = true;  //makes projectile hug walls since beams collide with them

    private float facingLast      = 0f;
    private float forcedDirection = 0f;
    private float curMomentum     = 0f;

    private void RecomputePlayerSpeed(PlayerController p, float speed)
    {
        this.gun.passiveStatModifiers = [StatType.MovementSpeed.Mult(speed)];
        p.stats.RecalculateStats(p, false, false);
    }

    public override void Update()
    {
        base.Update();
        if (this.gun.CurrentOwner is not PlayerController p)
            return;

        if (_theCurBall == null)
            SetupBallAndChain(p);

        // prevent the gun from normal firing entirely
        this.gun.RuntimeModuleData[this.gun.DefaultModule].onCooldown = true;

        // determine the angle delta between the reticle and our current forced direction
        float theta = (p.FacingDirection - (_RELATIVE_TO_LAST_FACING ? this.facingLast : this.forcedDirection)).Clamp180();
        this.facingLast = p.FacingDirection;

        // determine the actual change in momentum, then update momentum and direction accordingly
        float accel = Mathf.Sign(theta)*Mathf.Min(Math.Abs(theta)/_BALL_WEIGHT,_MAX_ACCEL);
        this.curMomentum = (this.curMomentum + accel).ClampAbsolute(_MAX_MOMENTUM)*_AIR_FRICTION;
        this.forcedDirection = (this.forcedDirection + this.curMomentum).Clamp360();

        // force player to face targeting reticle
        p.m_overrideGunAngle = this.forcedDirection;

        // update chain length
        float curChainLength
            = Mathf.Max(_MIN_CHAIN_LENGTH,_MAX_CHAIN_LENGTH * (Mathf.Abs(this.curMomentum) / _MAX_MOMENTUM));
        BasicBeamController.BeamBone lastBone = null;
        if (tieProjectilePositionToBeam)
        {
            // theCurChain.m_currentBeamDistance = curChainLength;
            // fancy computations to compute direction based on momentum
            float chainTargetDirection = this.forcedDirection +
                this.curMomentum * curChainLength;  //TODO: 0.75 is magic, do real math later
            // theCurChain.Direction = BraveMathCollege.DegreesToVector(chainTargetDirection);
            foreach (BasicBeamController.BeamBone b in _theCurChain.m_bones)
            {
                b.Velocity = BraveMathCollege.DegreesToVector(chainTargetDirection,curChainLength*C.PIXELS_PER_TILE);
                lastBone   = b;
            }
        }

        // update speed of owner as appropriate
        if (_INFLUENCE_PLAYER_MOMENTUM)
            RecomputePlayerSpeed(p,_MIN_PLAYER_MOMENTUM+_PLAYER_MOMENTUM_DELTA*(curChainLength/_MAX_CHAIN_LENGTH));

        // draw VFX showing the ball's current momentum
        if (!tieProjectilePositionToBeam)
            DrawVFXWithRespectToPlayerAngle(p,this.forcedDirection,curChainLength);

        // update and draw the ball itself
        _theCurBall.collidesWithEnemies = true;
        _theCurBall.collidesWithPlayer = false;
        Vector2 ppos =
            (p.CenterPosition + BraveMathCollege.DegreesToVector(this.forcedDirection,curChainLength+15f/C.PIXELS_PER_TILE)) // 15 == beam sprite length
            .ToVector3ZisY(-1f);

        Vector2 oldPos = _theCurBall.specRigidbody.Position.GetPixelVector2();
        if (tieProjectilePositionToBeam && (lastBone != null))
            ppos = lastBone.Position;

        _theCurBall.specRigidbody.Position = new Position(ppos);
        _theCurBall.SendInDirection(ppos-oldPos,true,true);
    }

    private void DrawVFXWithRespectToPlayerAngle(PlayerController p, float angle, float mag)
    {
        Vector2 ppos   = p.CenterPosition;
        float segments = Mathf.Floor(Mathf.Min(_MAX_CHAIN_SEGMENTS,mag/_MIN_GAP_BETWEEN_SEGMENTS));
        float gap      = mag/segments;
        for(int i = 0 ; i < segments; ++i )
            _Vfx2.SpawnAtPosition((ppos+BraveMathCollege.DegreesToVector(angle,i*gap)).ToVector3ZisY(-1f),
                angle,null, null, null, -0.05f);
        _Vfx.SpawnAtPosition((ppos+BraveMathCollege.DegreesToVector(angle,mag)).ToVector3ZisY(-1f),
            angle,null, null, null, -0.05f);
    }
}
