namespace CwaffingTheGungy;

/*
   TODO:
    - due to barreloffset technically being in the bottom left corner of the sprite, sometimes we can't shoot when our left side is against the wall
    - figure out how to render gun sprite behind player when player is facing backwards

   NOTES:
    - juggling order:   red, blue, yellow, green, purple, orange
*/

public class Jugglernaut : AdvancedGunBehavior
{
    public static string ItemName         = "Jugglernaut";
    public static string SpriteName       = "jugglernaut";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "Balancing Act";
    public static string LongDescription  = "Fires projectiles whose damage scales with the number of unique enemies hit in a row, represented by the number of guns being juggled. The juggling multiplier caps at 6 and resets upon switching guns or taking damage.";
    public static string Lore             = "Somehow even more impressive and dangerous than juggling swords, gun juggling is a burgeoning art form among Gungeoneers trying to justify their ever-growing collections of guns. Several enthusiasts have attempted at various points to establish gun juggling as a mainstream circus act, an international olympic sport, and a legitimate martial art, with each of these attempts resulting in some variation of the response: 'absolutely not, and please stop throwing those things around near us, it's terrifying and we're scared for our lives.'";

    internal const int _IDLE_FPS = 16;

    internal static List<string> _JuggleAnimations;
    internal static List<float> _MinEmission = new(){0f, 10f, 50f, 100f, 200f, 400f};
    internal static string _TrueIdleAnimation;
    internal static Hook _AdjustAnimationHook;
    internal static Hook _AdjustWeaponPanelHook;
    internal static IntVector2 _CarryOffset        = new IntVector2(21, -8);
    internal static IntVector2 _FlippedCarryOffset = new IntVector2(-14, -8);

    private int _juggleLevel = 0;
    private Coroutine _glowRoutine = null;
    private List<AIActor> _juggledEnemies = new();

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<Jugglernaut>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.B, gunClass: GunClass.SILLY, reloadTime: 0.0f, ammo: 150/*, defaultAudio: true*/);
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
            // gun.SetMuzzleVFX("muzzle_jugglernaut", fps: 10, scale: 0.2f, anchor: Anchor.MiddleCenter);
            // Manual adjustments to prevent wonky firing animations
            gun.preventRotation                 = true;
            gun.barrelOffset.transform.position = new Vector3(0.75f, 0.75f, 0f);
            gun.carryPixelOffset                = _CarryOffset;
            _AdjustAnimationHook                = new Hook(
                typeof(Gun).GetMethod("HandleSpriteFlip", BindingFlags.Instance | BindingFlags.Public),
                typeof(Jugglernaut).GetMethod("FixAttachPointsImmediately", BindingFlags.Static | BindingFlags.NonPublic)
                );
            _AdjustWeaponPanelHook              = new Hook(
                typeof(GameUIAmmoController).GetMethod("GetOffsetVectorForGun", BindingFlags.Instance | BindingFlags.Public),
                typeof(Jugglernaut).GetMethod("FixWeaponBoxSprite", BindingFlags.Static | BindingFlags.NonPublic)
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
            gun.reloadAnimation         = null; // animation shouldn't change when reloading
            gun.shootAnimation          = null; // animation shouldn't change when firing

        gun.DefaultModule.SetAttributes(clipSize: -1, cooldown: 1.0f, shootStyle: ShootStyle.SemiAutomatic);

        Projectile projectile = Lazy.PrefabProjectileFromGun(gun);
            projectile.AddDefaultAnimation(AnimatedBullet.Create(name: "jugglernaut_ball", fps: 12, scale: 0.5f, anchor: Anchor.MiddleLeft));
            projectile.transform.parent = gun.barrelOffset;
            projectile.baseData.speed   = 70f;
            projectile.baseData.damage  = 10f;
    }

    private bool _cachedFlipped = false;
    private bool _firstCheck = true;
    private static void FixAttachPointsImmediately(Action<Gun, bool> orig, Gun gun, bool flipped)
    {
        orig(gun, flipped);
        if (gun.GetComponent<Jugglernaut>() is not Jugglernaut jugglernaut)
            return;
        if (gun.CurrentOwner is not PlayerController player)
            return;
        if (flipped == jugglernaut._cachedFlipped && !jugglernaut._firstCheck)
            return;

        if (flipped)
            gun.carryPixelOffset = _CarryOffset;
        else
            gun.carryPixelOffset = _FlippedCarryOffset;
        jugglernaut._cachedFlipped = flipped;
        jugglernaut._firstCheck = false;
    }

    private static Vector3 _WeaponBoxCorrection = new Vector3(-0.125f, -0.125f, 0f);
    private static Vector3 FixWeaponBoxSprite(Func<GameUIAmmoController, Gun, bool, Vector3> orig, GameUIAmmoController guiac, Gun gun, bool flipped)
    {
        Vector3 vec = orig(guiac, gun, flipped);
        if (gun.GetComponent<Jugglernaut>() is not Jugglernaut jugglernaut)
            return vec;
        return vec + _WeaponBoxCorrection;
    }

    protected override void OnPickedUpByPlayer(PlayerController player)
    {
        base.OnPickedUpByPlayer(player);

        player.healthHaver.OnDamaged += DroppingTheBall;

        ResetJuggle();
        gun.spriteAnimator.currentClip = gun.spriteAnimator.GetClipByName(_JuggleAnimations[0]);
        gun.spriteAnimator.Play();
        gun.sprite.gameObject.SetGlowiness(0f);
    }

    protected override void OnPostDroppedByPlayer(PlayerController player)
    {
        base.OnPostDroppedByPlayer(player);

        player.healthHaver.OnDamaged -= DroppingTheBall;

        ResetJuggle();
        gun.spriteAnimator.currentClip = gun.spriteAnimator.GetClipByName(_TrueIdleAnimation);
        gun.spriteAnimator.StopAndResetFrameToDefault();
    }

    private void DroppingTheBall(float resultValue, float maxValue, CoreDamageTypes damageTypes, DamageCategory damageCategory, Vector2 damageDirection)
    {
        if (this._juggledEnemies.Count() == 0)
            return;

        AkSoundEngine.PostEvent("juggle_drop_sound", this.Player.gameObject);
        ResetJuggle();
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

        AkSoundEngine.PostEvent("alyx_shoot_sound", gun.gameObject);
    }

    public void RegisterEnemyHit(AIActor enemy)
    {
        // scan backwards until we find the enemy in our list, then remove it an all previous enemies
        for (int i = this._juggledEnemies.Count() - 1; i >= 0; --i)
        {
            if (this._juggledEnemies[i] != enemy)
                continue;
            for (int j = i; j >= 0; --j)
                this._juggledEnemies.RemoveAt(j);
            break;
        }
        // add our enemy and return
        this._juggledEnemies.Add(enemy);
        UpdateLevel(returnIfUnchanged: true);
    }

    private void ResetJuggle()
    {
        this._juggledEnemies.Clear();
        UpdateLevel();
    }

    private void UpdateLevel(bool returnIfUnchanged = false)
    {
        int oldLevel = this._juggleLevel;
        this._juggleLevel = Mathf.Clamp(this._juggledEnemies.Count() - 1, 0, _JuggleAnimations.Count() - 1);
        if (returnIfUnchanged && this._juggleLevel == oldLevel)
            return;

        if (this._juggleLevel > oldLevel)
        {
            if (this._glowRoutine != null)
                gun.StopCoroutine(this._glowRoutine);
            this._glowRoutine = gun.StartCoroutine(GlowUp());
        }
        else
            gun.sprite.renderer.material.SetFloat("_EmissivePower", _MinEmission[this._juggleLevel]);

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
        const float MAX_EMISSION   = 800f;
        float minEmit = _MinEmission[this._juggleLevel];

        Material m = gun.sprite.renderer.material;
        m.SetFloat("_EmissivePower", 0f);
        AkSoundEngine.PostEvent("juggle_add_sound", gun.gameObject);
        // AkSoundEngine.PostEvent("gunbrella_fire_sound", gun.gameObject);

        for (float elapsed = 0f; elapsed < GLOW_TIME; elapsed += BraveTime.DeltaTime)
        {
            float percentDone = elapsed / GLOW_TIME;
            m.SetFloat("_EmissivePower", MAX_EMISSION * percentDone * percentDone);
            yield return null;
        }
        for (float elapsed = 0f; elapsed < GLOW_FADE_TIME; elapsed += BraveTime.DeltaTime)
        {
            float percentLeft = 1f - elapsed / GLOW_FADE_TIME;
            m.SetFloat("_EmissivePower", minEmit + (MAX_EMISSION - minEmit) * percentLeft * percentLeft);
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
