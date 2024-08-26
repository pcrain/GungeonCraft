namespace CwaffingTheGungy;

public class SeltzerPelter : CwaffGun
{
    public static string ItemName         = "Seltzer Pelter";
    public static string ShortDescription = "Shaken, not Stirred";
    public static string LongDescription  = "Launches soda cans that fly around wildly after initial impact, pushing enemies away with highly pressurized streams of seltzer water.";
    public static string Lore             = "The best designs are inspired by nature, but those inspired by fraternities come in at a close second. This weapon was first conceptualized when a frat bro stuffed a beer can in a spud launcher and fired it at the ceiling. Although the can burst immediately and ruined the launcher, another frat bro desperate for a cool term project to bring his engineering class grade up to a D- ran with the idea. After investing in sturdier titanium-alloy cans and substituting the beer for soda, the remodeled launcher created as big a mess as ever, but by virtue of externalizing that mess, was considered a resounding success. The frat bro got a D+ in his class, and an actually competent engineer bought the rights to the design and tweaked it to be a bit more marketable and combat-viable, resulting in win-wins all around.";

    internal static BasicBeamController _BubbleBeam;
    internal static List<string> _ReloadAnimations;

    private int _loadedCanIndex = 0;

    public static void Init()
    {
        Gun gun = Lazy.SetupGun<SeltzerPelter>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.B, gunClass: GunClass.CHARGE, reloadTime: 1.0f, ammo: 150, shootFps: 36, muzzleFrom: Items.Mailbox)
          .AddToShop(ItemBuilder.ShopType.Goopton);

        _ReloadAnimations = new(){
            gun.QuickUpdateGunAnimation("reload",   returnToIdle: true), // coke can
            gun.QuickUpdateGunAnimation("reload_b", returnToIdle: true), // pepsi can
            gun.QuickUpdateGunAnimation("reload_c", returnToIdle: true), // sprite can
        };
        foreach(string animation in _ReloadAnimations)
        {
            gun.SetAnimationFPS(animation, 52);
            gun.SetGunAudio(animation, "seltzer_shake_sound", 0, 10, 22, 29, 35);
            gun.SetGunAudio(animation, "seltzer_insert_sound", 42);
        }

        gun.InitProjectile(GunData.New(clipSize: 1, cooldown: 0.5f, shootStyle: ShootStyle.SemiAutomatic, customClip: true, preventOrbiting: true,
          damage: 16.0f, speed: 30.0f, force: 75.0f, range: 999.0f, sprite: "can_projectile", fps: 1, anchor: Anchor.MiddleCenter, // 1 FPS minimum, stop animator manually later
          anchorsChangeColliders: false, overrideColliderPixelSizes: new IntVector2(2, 2))) // prevent uneven colliders from glitching into walls
        .Attach<SeltzerProjectile>();

        // the perfect seltzer stats, do not tweak without testing! (beam damage == DPS)
        _BubbleBeam = Items.MarineSidearm.CloneProjectile(GunData.New(damage: 40.0f, speed: 20.0f, force: 100.0f, range: 4.0f, preventOrbiting: true, doBeamSetup: true,
            beamSprite: "bubble_stream", beamFps: 8, beamEmission: 5f, beamChargeDelay: 0f, beamSeparation: true, beamIsRigid: false,
            beamInterpolate: false, beamKnockback: 100f, beamTiling: BasicBeamController.BeamTileType.Flowing, beamEndType: BasicBeamController.BeamEndType.Persist))
          .GetComponent<BasicBeamController>();

        GoopModifier gmod = _BubbleBeam.gameObject.GetOrAddComponent<GoopModifier>();
            gmod.SpawnAtBeamEnd         = true;
            gmod.BeamEndRadius          = 0.5f;
            gmod.SpawnGoopInFlight      = true;
            gmod.InFlightSpawnRadius    = 0.5f;
            gmod.InFlightSpawnFrequency = 0.01f;
            gmod.goopDefinition         = EasyGoopDefinitions.SeltzerGoop;
    }

    public override void OnActualReload(PlayerController player, Gun gun, bool manual)
    {
        base.OnActualReload(player, gun, manual);

        this._loadedCanIndex = UnityEngine.Random.Range(0, _ReloadAnimations.Count);
        gun.spriteAnimator.Stop();
        gun.spriteAnimator.Play(_ReloadAnimations[this._loadedCanIndex]);
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        projectile.spriteAnimator.PlayFromFrame(this._loadedCanIndex);
    }
}

public class SeltzerProjectile : MonoBehaviour
{
    private Projectile _canProjectile;
    private PlayerController _owner;
    private BounceProjModifier  _bounce        = null;
    private BasicBeamController _beam = null;
    private float _rotationRate = 0f;
    private bool _startedSpraying = false;

    private void Start()
    {
        this._canProjectile = base.GetComponent<Projectile>();
        this._owner = this._canProjectile.Owner as PlayerController;
        this._canProjectile.BulletScriptSettings.surviveRigidbodyCollisions = true;
        this._canProjectile.DestroyMode = Projectile.ProjectileDestroyMode.BecomeDebris;
        this._canProjectile.shouldRotate = false; // prevent automatic rotation after creation
        this._canProjectile.specRigidbody.OnRigidbodyCollision += OnRigidbodyCollision;
        this._bounce = this._canProjectile.gameObject.GetOrAddComponent<BounceProjModifier>(); //REFACTOR: do in setup
            this._bounce.numberOfBounces      = 9999;
            this._bounce.chanceToDieOnBounce  = 0f;
            this._bounce.onlyBounceOffTiles   = false;
            this._bounce.ExplodeOnEnemyBounce = false;
            this._bounce.bouncesTrackEnemies  = true;
            this._bounce.bounceTrackRadius    = 3f;
            this._bounce.OnBounce += this.StartSprayingSoda;

        this._canProjectile.spriteAnimator.Stop(); // stop animating immediately after creation so we can stick with our initial sprite

        base.gameObject.Play("seltzer_shoot_sound_alt_2");
    }

    private void OnRigidbodyCollision(CollisionData rigidbodyCollision)
    {
        if (!this || !this._canProjectile)
            return;
        this._canProjectile.SendInDirection(rigidbodyCollision.Normal, false);

        if (!this._startedSpraying)
        {
            StartSprayingSoda();
            return;
        }

        base.gameObject.Play("seltzer_pelter_collide_sound");
        this._canProjectile.MultiplySpeed(0.5f);
    }

    private void StartSprayingSoda()
    {
        this._startedSpraying = true;
        this._bounce.OnBounce -= this.StartSprayingSoda;
        this._bounce.OnBounce += this.RestartBeamOnBounce;
        this._canProjectile.m_usesNormalMoveRegardless = true; // disable helix projectile shenanigans after hitting a wall
        this._canProjectile.MultiplySpeed(0.5f);
        this._canProjectile.OnDestruction += this.DestroyBeam;
        this._canProjectile.StartCoroutine(SpraySoda_CR(this, this._canProjectile));

        base.gameObject.Play("seltzer_shoot_sound");
        base.gameObject.Play("seltzer_pelter_collide_sound");
    }

    private void CreateBeam()
    {
        GameObject theBeamObject = SpawnManager.SpawnProjectile(SeltzerPelter._BubbleBeam.gameObject, this._canProjectile.SafeCenter, Quaternion.identity);
        Projectile beamProjectile = theBeamObject.GetComponent<Projectile>();
            beamProjectile.SetOwnerAndStats(this._owner);

        this._beam = theBeamObject.GetComponent<BasicBeamController>();
            this._beam.Owner       = this._owner;
            this._beam.HitsPlayers = false;
            this._beam.HitsEnemies = true;
            this._beam.Origin      = this._canProjectile.SafeCenter;
            this._beam.Direction   = -this._canProjectile.sprite.transform.rotation.z.ToVector();
    }

    private void RestartBeamOnBounce()
    {
        if (this._canProjectile)
            this._canProjectile.baseData.speed *= 0.5f;
        if (this._beam)
            this._beam.CeaseAttack();
        this._beam = null;
        base.gameObject.Play("seltzer_pelter_collide_sound");
    }

    private void UpdateRotationRate()
    {
        this._rotationRate = UnityEngine.Random.Range(-5f, 5f);
    }

    private void DestroyBeam(Projectile p)
    {
        if (this._beam)
            this._beam.CeaseAttack();
    }

    private const float SPRAY_TIME = 2f;
    private const float SPIN_TIME  = 4f;
    private const float ACCEL      = 40f;
    // private const float _AIR_DRAG  = 0.25f;
    private const float _AIR_DRAG  = 0.25f;
    private const float _SOUND_RATE = 0.2f;

    private static IEnumerator SpraySoda_CR(SeltzerProjectile seltzer, Projectile p)
    {
        yield return null;
        if (!p || !seltzer)
            yield break;
        float startAngle = p.LastVelocity.ToAngle();
        float curAngle = startAngle;
        seltzer.UpdateRotationRate();

        p.gameObject.Play("seltzer_spray_sound");
        float lastSoundTime = BraveTime.ScaledTimeSinceStartup;

        int maxSounds = 20; // if the cans get stuck, things can get really noisy, so don't let them make too much noise

        #region The Ballistics
            for (float elapsed = 0f; elapsed < SPRAY_TIME; elapsed += BraveTime.DeltaTime)
            {
                while (BraveTime.DeltaTime == 0)
                    yield return null;
                if (!p || !p.isActiveAndEnabled || p.HasDiedInAir || !seltzer)
                    yield break;
                if (!seltzer._beam)
                    seltzer.CreateBeam();

                if ((maxSounds > 0) && (lastSoundTime + _SOUND_RATE < BraveTime.ScaledTimeSinceStartup))
                {
                    --maxSounds;
                    lastSoundTime = BraveTime.ScaledTimeSinceStartup;
                    p.gameObject.Play("seltzer_spray_sound");
                }

                Vector2 oldSpeed = p.LastVelocity;
                curAngle += seltzer._rotationRate;
                Vector2 newSpeed = oldSpeed + curAngle.ToVector(ACCEL * BraveTime.DeltaTime);
                p.SetSpeed(newSpeed.magnitude);
                p.SendInDirection(newSpeed, false, false);
                p.SetRotation(newSpeed.ToAngle());
                seltzer._beam.Origin = p.SafeCenter;
                seltzer._beam.Direction = -p.LastVelocity;
                seltzer._beam.LateUpdatePosition(seltzer._beam.Origin);
                yield return null;
            }
        #endregion

        #region The Rapid Spin
            if (!p)
                yield break;
            curAngle = p.LastVelocity.ToAngle(); // reset this to match the actual sprite
            float rotIncrease = 5f * Mathf.Sign(seltzer._rotationRate);
            for (float elapsed = 0f; elapsed < SPIN_TIME; elapsed += BraveTime.DeltaTime)
            {
                while (BraveTime.DeltaTime == 0)
                    yield return null;
                if (!p || !p.isActiveAndEnabled || p.HasDiedInAir || !seltzer)
                    yield break;
                if (!seltzer._beam)
                    seltzer.CreateBeam();

                if ((maxSounds > 0) && (lastSoundTime + _SOUND_RATE < BraveTime.ScaledTimeSinceStartup))
                {
                    --maxSounds;
                    lastSoundTime = BraveTime.ScaledTimeSinceStartup;
                    p.gameObject.Play("seltzer_spray_sound");
                }

                if (p.baseData.speed > 0.1f)
                    p.ApplyFriction(_AIR_DRAG);
                seltzer._rotationRate += rotIncrease * BraveTime.DeltaTime;
                curAngle += seltzer._rotationRate * C.FPS * BraveTime.DeltaTime;
                p.SetRotation(curAngle);
                seltzer._beam.Origin = p.SafeCenter;
                seltzer._beam.Direction = -curAngle.ToVector();
                seltzer._beam.LateUpdatePosition(seltzer._beam.Origin);
                yield return null;
            }
        #endregion

        #region Die Down
            if (seltzer && seltzer._beam)
                seltzer._beam.CeaseAttack();
            if (p)
                p.DieInAir();
        #endregion

        yield break;
    }
}
