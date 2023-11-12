namespace CwaffingTheGungy;

// red, blue, yellow, green, purple, orange

public class Jugglernaut : AdvancedGunBehavior
{
    public static string ItemName         = "Jugglernaut";
    public static string SpriteName       = "jugglernaut";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";

    internal const int _IDLE_FPS = 16;

    internal static List<string> _JuggleAnimations;
    internal static List<AIActor> _JuggledEnemies = new();
    internal static string _TrueIdleAnimation;
    internal static Hook _AdjustAnimationHook;
    internal static IntVector2 _CarryOffset        = new IntVector2(21, -8);
    internal static IntVector2 _FlippedCarryOffset = new IntVector2(-14, -8);

    private int _juggleLevel = 0;
    private Coroutine _glowRoutine = null;

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<Jugglernaut>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription);
            gun.SetAttributes(quality: PickupObject.ItemQuality.C, gunClass: GunClass.SILLY, reloadTime: 1.2f, ammo: 300, defaultAudio: true);
            gun.SetAnimationFPS(gun.shootAnimation, 30);
            gun.SetAnimationFPS(gun.reloadAnimation, 40);
            _JuggleAnimations = new(){
                gun.UpdateAnimation("1_gun", returnToIdle: false),
                gun.UpdateAnimation("2_gun", returnToIdle: false),
                gun.UpdateAnimation("3_gun", returnToIdle: false),
                gun.UpdateAnimation("4_gun", returnToIdle: false),
                gun.UpdateAnimation("5_gun", returnToIdle: false),
                gun.UpdateAnimation("6_gun", returnToIdle: false),
            };
            gun.muzzleFlashEffects              = null;
            // Manual adjustments to prevent wonky firing animations
            gun.preventRotation                 = true;
            gun.barrelOffset.transform.position = new Vector3(0.75f, 0.75f, 0f);
            gun.carryPixelOffset                = _CarryOffset;
            _AdjustAnimationHook                = new Hook(
                typeof(Gun).GetMethod("HandleSpriteFlip", BindingFlags.Instance | BindingFlags.Public),
                typeof(Jugglernaut).GetMethod("FixAttachPointsImmediately", BindingFlags.Static | BindingFlags.NonPublic)
                );

            string tossSound = "juggle_toss_sound";
            for (int i = 0; i < _JuggleAnimations.Count(); ++i)
            {
                gun.SetAnimationFPS(_JuggleAnimations[i], _IDLE_FPS);
                gun.SetGunAudio(_JuggleAnimations[i], tossSound, frame: 5);
                gun.SetGunAudio(_JuggleAnimations[i], tossSound, frame: 17);
                if (i > 0)
                {
                    gun.SetGunAudio(_JuggleAnimations[i], tossSound, frame: 9);
                    gun.SetGunAudio(_JuggleAnimations[i], tossSound, frame: 21);
                }
                if (i > 1)
                {
                    gun.SetGunAudio(_JuggleAnimations[i], tossSound, frame: 1);
                    gun.SetGunAudio(_JuggleAnimations[i], tossSound, frame: 13);
                }
                if (i > 2)
                {
                    gun.SetGunAudio(_JuggleAnimations[i], tossSound, frame: 7);
                    gun.SetGunAudio(_JuggleAnimations[i], tossSound, frame: 19);
                }
                if (i > 3)
                {
                    gun.SetGunAudio(_JuggleAnimations[i], tossSound, frame: 11);
                    gun.SetGunAudio(_JuggleAnimations[i], tossSound, frame: 23);
                }
                if (i > 4)
                {
                    gun.SetGunAudio(_JuggleAnimations[i], tossSound, frame: 3);
                    gun.SetGunAudio(_JuggleAnimations[i], tossSound, frame: 15);
                }
            }
            _TrueIdleAnimation          = gun.idleAnimation;
            gun.reloadAnimation         = null;
            gun.shootAnimation          = null;

        ProjectileModule mod = gun.DefaultModule;
            mod.ammoCost            = 1;
            mod.shootStyle          = ProjectileModule.ShootStyle.SemiAutomatic;
            mod.sequenceStyle       = ProjectileModule.ProjectileSequenceStyle.Random;
            mod.cooldownTime        = 0.5f;
            mod.numberOfShotsInClip = -1;

        Projectile projectile = Lazy.PrefabProjectileFromGun(gun);
            // projectile.AddDefaultAnimation(AnimateBullet.CreateProjectileAnimation(
            //     ResMap.Get("jugglernaut_projectile").Base(),
            //     12, true, new IntVector2(10, 3),
            //     false, tk2dBaseSprite.Anchor.MiddleLeft, true, true));
            projectile.transform.parent = gun.barrelOffset;
            projectile.baseData.speed   = 70f;
            projectile.baseData.damage  = 10f;
    }

    private bool _cachedFlipped = false;
    private static void FixAttachPointsImmediately(Action<Gun, bool> orig, Gun gun, bool flipped)
    {
        orig(gun, flipped);
        if (gun.GetComponent<Jugglernaut>() is not Jugglernaut jugglernaut)
            return;
        if (flipped == jugglernaut._cachedFlipped)
            return;

        if (flipped)
            gun.carryPixelOffset = _CarryOffset;
        else
            gun.carryPixelOffset = _FlippedCarryOffset;
        jugglernaut._cachedFlipped = flipped;
    }

    protected override void OnPickedUpByPlayer(PlayerController player)
    {
        base.OnPickedUpByPlayer(player);

        ResetJuggle();
        gun.spriteAnimator.currentClip = gun.spriteAnimator.GetClipByName(_JuggleAnimations[0]);
        gun.spriteAnimator.Play();
        gun.sprite.gameObject.SetGlowiness(0f);
    }

    protected override void OnPostDroppedByPlayer(PlayerController player)
    {
        base.OnPostDroppedByPlayer(player);

        ResetJuggle();
        gun.spriteAnimator.currentClip = gun.spriteAnimator.GetClipByName(_TrueIdleAnimation);
        gun.spriteAnimator.StopAndResetFrameToDefault();
    }

    public override void OnSwitchedToThisGun()
    {
        base.OnSwitchedToThisGun();
        ResetJuggle();
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        base.OnSwitchedAwayFromThisGun();
        ResetJuggle();
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        projectile.gameObject.AddComponent<JugglernautProjectile>().Setup(this);
            projectile.baseData.damage *= (1f + this._juggleLevel);

        // Animation wonkiness due to manually adjusting carry offsets messes with aim position, so redirect towards the cursor
        projectile.SendInDirection((this.Player.unadjustedAimPoint.XY() - projectile.sprite.WorldCenter), resetDistance: true);
        projectile.UpdateSpeed();
    }

    public void RegisterEnemyHit(AIActor enemy)
    {
        // scan backwards until we find the enemy in our list, then remove it an all previous enemies
        for (int i = _JuggledEnemies.Count() - 1; i >= 0; --i)
        {
            if (_JuggledEnemies[i] != enemy)
                continue;
            for (int j = i; j >= 0; --j)
                _JuggledEnemies.RemoveAt(j);
            break;
        }
        // add our enemy and return
        _JuggledEnemies.Add(enemy);
        UpdateLevel(returnIfUnchanged: true);
    }

    private void ResetJuggle()
    {
        _JuggledEnemies.Clear();
        UpdateLevel();
    }

    private void UpdateLevel(bool returnIfUnchanged = false)
    {
        int oldLevel = this._juggleLevel;
        this._juggleLevel = Mathf.Clamp(_JuggledEnemies.Count() - 1, 0, _JuggleAnimations.Count() - 1);
        if (returnIfUnchanged && this._juggleLevel == oldLevel)
            return;

        if (this._juggleLevel > oldLevel)
        {
            if (this._glowRoutine != null)
                gun.StopCoroutine(this._glowRoutine);
            this._glowRoutine = gun.StartCoroutine(GlowUp());
        }

        gun.idleAnimation = _JuggleAnimations[this._juggleLevel];
        gun.spriteAnimator.currentClip = gun.spriteAnimator.GetClipByName(gun.idleAnimation);
        gun.spriteAnimator.StopAndResetFrameToDefault();
        gun.spriteAnimator.Play();
        gun.sprite.usesOverrideMaterial = true;
    }

    private IEnumerator GlowUp()
    {
        const float GLOW_TIME      = 0.25f;
        const float GLOW_FADE_TIME = 1.00f;
        const float MAX_EMIT       = 800f;

        Material m = gun.sprite.renderer.material;
        m.SetFloat("_EmissivePower", 0f);
        AkSoundEngine.PostEvent("gunbrella_fire_sound", gun.gameObject);

        for (float elapsed = 0f; elapsed < GLOW_TIME; elapsed += BraveTime.DeltaTime)
        {
            float percentDone = elapsed / GLOW_TIME;
            m.SetFloat("_EmissivePower", MAX_EMIT * percentDone * percentDone);
            yield return null;
        }
        for (float elapsed = 0f; elapsed < GLOW_FADE_TIME; elapsed += BraveTime.DeltaTime)
        {
            float percentLeft = 1f - elapsed / GLOW_FADE_TIME;
            m.SetFloat("_EmissivePower", MAX_EMIT * percentLeft * percentLeft);
            yield return null;
        }
        yield break;
    }
}

public class JugglernautProjectile : MonoBehaviour
{
    private Projectile _projectile;
    private PlayerController _owner;
    private Jugglernaut _jugglernaut;

    public void Setup(Jugglernaut jugglernaut)
    {
        this._projectile  = base.GetComponent<Projectile>();
        this._owner       = this._projectile.Owner as PlayerController;
        this._jugglernaut = jugglernaut;

        this._projectile.OnHitEnemy += OnHitEnemy;
    }

    private void OnHitEnemy(Projectile bullet, SpeculativeRigidbody body, bool what)
    {
        if (body.aiActor is AIActor enemy)
            this._jugglernaut?.RegisterEnemyHit(enemy);
    }
}
