namespace CwaffingTheGungy;

/*
   TODO:
    - due to barreloffset technically being in the bottom left corner of the sprite, sometimes we can't shoot when our left side is against the wall
    - figure out how to render gun sprite behind player when player is facing backwards

   NOTES:
    - juggling order:   red, blue, yellow, green, purple, orange
*/

public class Jugglernaut : CwaffGun
{
    public static string ItemName         = "Jugglernaut";
    public static string ShortDescription = "Balancing Act";
    public static string LongDescription  = "Fires projectiles whose damage scales with the number of unique enemies hit in a row, represented by the number of guns being juggled. The juggling multiplier caps at 6 and resets upon switching guns or taking damage.";
    public static string Lore             = "Somehow even more impressive and dangerous than juggling swords, gun juggling is a burgeoning art form among Gungeoneers trying to justify their ever-growing collections of guns. Several enthusiasts have attempted at various points to establish gun juggling as a mainstream circus act, an international olympic sport, and a legitimate martial art, with each of these attempts resulting in some variation of the response: 'absolutely not, and please stop throwing those things around near us, it's terrifying and we're scared for our lives.'";

    internal const int _IDLE_FPS = 16;
    internal const float _DEBRIS_GLOW = 500f;

    internal static List<string> _JuggleAnimations;
    internal static List<float> _MinEmission = new(){10f, 50f, 100f, 200f, 300f, 400f};
    internal static string _TrueIdleAnimation;
    internal static IntVector2 _CarryOffset        = new IntVector2(-14, -8);
    internal static IntVector2 _FlippedCarryOffset = new IntVector2(21, -8);
    internal static List<Color> _Colors = new(){
        Color.red,
        Color.blue,
        Color.yellow,
        Color.green,
        new Color(1.0f, 0.647f, 0.0f, 1.0f),
        new Color(0.627f, 0.125f, 0.941f),
    };

    private int _juggleLevel = 0;
    private Coroutine _glowRoutine = null;
    private List<AIActor> _juggledEnemies = new();
    private bool _cachedFlipped = false;
    private bool _firstSpriteCheck = true;

    public static void Init()
    {
        Gun gun = Lazy.SetupGun<Jugglernaut>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.B, gunClass: GunClass.SILLY, reloadTime: 0.0f, ammo: 240, shootFps: 30, reloadFps: 40, preventRotation: true);

        _JuggleAnimations = new(){
            gun.QuickUpdateGunAnimation("1_gun", returnToIdle: false),
            gun.QuickUpdateGunAnimation("2_gun", returnToIdle: false),
            gun.QuickUpdateGunAnimation("3_gun", returnToIdle: false),
            gun.QuickUpdateGunAnimation("4_gun", returnToIdle: false),
            gun.QuickUpdateGunAnimation("5_gun", returnToIdle: false),
            gun.QuickUpdateGunAnimation("6_gun", returnToIdle: false),
        };

        //NOTE: Manual adjustments to prevent wonky firing animations by effectively locking barrel offset in place
        gun.LockedHorizontalOnCharge = true;
        gun.LockedHorizontalCenterFireOffset = 0f;
        gun.barrelOffset.transform.position -= (C.PIXEL_SIZE * _CarryOffset.ToVector3());
        gun.AddFlippedCarryPixelOffsets(offset: _CarryOffset, flippedOffset: _FlippedCarryOffset);

        string tossSound = "juggle_toss_sound";
        for (int i = 0; i < _JuggleAnimations.Count; ++i)
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

        gun.InitProjectile(GunData.New(clipSize: -1, cooldown: 0.4f, shootStyle: ShootStyle.SemiAutomatic, damage: 10.0f, speed: 70.0f, customClip: true,
          sprite: "jugglernaut_projectile", fps: 2,  anchor: Anchor.MiddleCenter, shouldRotate: false, spawnSound: "jugglernaut_throw_sound", destroySound: "wall_thunk"));
    }

    /// <summary>Make sure Jugglernaut appears correctly in the weapons panel</summary>
    [HarmonyPatch(typeof(GameUIAmmoController), nameof(GameUIAmmoController.GetOffsetVectorForGun))]
    private class JugglernautWeaponBoxPatch
    {
        private static readonly Vector3 _WeaponBoxCorrection = new Vector3(-0.125f, -0.125f, 0f);

        static void Postfix(Gun newGun, bool isFlippingGun, ref Vector3 __result)
        {
            if (newGun.GetComponent<Jugglernaut>())
                __result += _WeaponBoxCorrection;
        }
    }

    /// <summary>Prevent Gun.CeaseAttack() from updating Jugglernaut's attach points for a single frame and messing up the sprite</summary>
    [HarmonyPatch(typeof(Gun), nameof(Gun.CeaseAttack))]
    private class JugglernautCeaseAttackPatch
    {
        [HarmonyILManipulator]
        private static void JugglernautCeaseAttackIL(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<Gun>("LockedHorizontalOnCharge")))
                return;

            cursor.Emit(OpCodes.Ldarg_0); // load the gun
            cursor.CallPrivate(typeof(JugglernautCeaseAttackPatch), nameof(JugglernautCeaseAttackPatch.ShouldUpdateAttachPoints));
        }

        private static bool ShouldUpdateAttachPoints(bool origVal, Gun gun)
        {
            return origVal && !gun.GetComponent<Jugglernaut>();
        }
    }

    public override void OnPlayerPickup(PlayerController player)
    {
        base.OnPlayerPickup(player);

        player.healthHaver.OnDamaged += OnPlayerDamaged;

        gun.spriteAnimator.StopAndResetFrameToDefault();
        ResetJuggle();
        gun.sprite.gameObject.SetGlowiness(0f);
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        base.OnDroppedByPlayer(player);

        player.healthHaver.OnDamaged -= OnPlayerDamaged;

        ResetJuggle();
        gun.spriteAnimator.currentClip = gun.spriteAnimator.GetClipByName(_TrueIdleAnimation);
        gun.spriteAnimator.StopAndResetFrameToDefault();
    }

    public override void OnDestroy()
    {
        if (this.PlayerOwner && this.PlayerOwner.healthHaver)
            this.PlayerOwner.healthHaver.OnDamaged -= OnPlayerDamaged;
        base.OnDestroy();
    }

    private void OnPlayerDamaged(float resultValue, float maxValue, CoreDamageTypes damageTypes, DamageCategory damageCategory, Vector2 damageDirection)
    {
        if (this.PlayerOwner)
            ResetJuggle();
    }

    private void DropGuns(PlayerController player, int oldGuns, int newGuns)
    {
        if (newGuns >= oldGuns)
            return;
        Vector2 pos = player.CenterPosition;
        tk2dSpriteAnimationClip clip = this.gun.DefaultModule.projectiles[0].sprite.GetComponent<tk2dSpriteAnimator>().DefaultClip;
        for (int i = newGuns; i < oldGuns; ++i)
        {
            GameObject debrisObject = new GameObject();
                debrisObject.transform.position = pos;
            tk2dSprite sprite = debrisObject.AddComponent<tk2dSprite>();
                sprite.SetSprite(clip.frames[i].spriteCollection, clip.frames[i].spriteId);
                sprite.SetGlowiness(glowAmount: _DEBRIS_GLOW);
            debrisObject.AutoRigidBody(Anchor.MiddleCenter, CollisionLayer.TileBlocker);
            DebrisObject debris = debrisObject.AddComponent<DebrisObject>();

            debris.angularVelocity         = 45;
            debris.angularVelocityVariance = 20;
            debris.decayOnBounce           = 0.5f;
            debris.bounceCount             = 2;
            debris.canRotate               = true;
            debris.shouldUseSRBMotion      = true;
            debris.sprite                  = sprite;
            debris.animatePitFall          = true;
            // debris.audioEventName          = "monkey_tennis_bounce_first";
            debris.AssignFinalWorldDepth(-0.5f);
            debris.Trigger(Lazy.RandomVector(4f), 1f);
        }
    }

    public override void OnSwitchedToThisGun()
    {
        base.OnSwitchedToThisGun();
        gun.spriteAnimator.StopAndResetFrameToDefault();
        ResetJuggle(); //WARNING: possibly need to delete one of these due to bug where jugglernaut drops combo seemingly out of nowhere, but might be fixed already
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        base.OnSwitchedAwayFromThisGun();
        if (this.PlayerOwner)
            ResetJuggle(); //WARNING: possibly need to delete one of these due to bug where jugglernaut drops combo seemingly out of nowhere, but might be fixed already
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        int spriteId = UnityEngine.Random.Range(0, 1 + this._juggleLevel);
        projectile.gameObject.AddComponent<JugglernautProjectile>().Setup(this, spriteId);
        projectile.baseData.damage *= (1f + this._juggleLevel);
        projectile.DestroyMode = Projectile.ProjectileDestroyMode.BecomeDebris;
    }

    public void RegisterEnemyHit(AIActor enemy)
    {
        // scan backwards until we find the enemy in our list, then remove it an all previous enemies
        for (int i = this._juggledEnemies.Count - 1; i >= 0; --i)
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
        this._juggleLevel = Mathf.Clamp(this._juggledEnemies.Count - 1, 0, _JuggleAnimations.Count - 1);
        if (returnIfUnchanged && this._juggleLevel == oldLevel)
            return;

        if (this._juggleLevel > oldLevel)
        {
            if (this._glowRoutine != null)
                gun.StopCoroutine(this._glowRoutine);
            this._glowRoutine = gun.StartCoroutine(GlowUp());
        }
        else
        {
            gun.sprite.renderer.material.SetFloat("_EmissivePower", _MinEmission[this._juggleLevel]);
            if (this._juggleLevel < oldLevel)
            {
                if (this._juggledEnemies.Count == 0)
                    this.PlayerOwner.gameObject.Play("juggle_drop_sound");
                DropGuns(player: this.PlayerOwner, oldGuns: 1 + oldLevel, newGuns: 1 + this._juggleLevel);
            }
        }
        UpdateIdleAnimation();
    }

    private void UpdateIdleAnimation()
    {
        gun.idleAnimation = _JuggleAnimations[this._juggleLevel];
        gun.spriteAnimator.currentClip = gun.spriteAnimator.GetClipByName(gun.idleAnimation);
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
        gun.gameObject.Play("juggle_add_sound");

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

    public override void Update()
    {
        base.Update();
        if (this.PlayerOwner is not PlayerController pc)
            return;
        Lazy.PlaySoundUntilDeathOrTimeout("circus_music", pc.gameObject, 0.1f);
        UpdateIdleAnimation(); // fixes idle animation not playing when picked up, not sure why this is necessary
    }

    private void LateUpdate()
    {
        if (this.PlayerOwner is not PlayerController pc)
            return;
        if (pc.m_currentGunAngle > 25f && pc.m_currentGunAngle < 155f)
            this.gun.sprite.HeightOffGround = -0.075f; // vanilla back-facing depth when preventRotation is false
        else
            this.gun.sprite.HeightOffGround = 0.4f; // vanilla depth when preventRotation is true
        this.gun.sprite.UpdateZDepth();
    }
}

public class JugglernautProjectile : MonoBehaviour
{
    private const float _ROT_RATE = 24f;
    private Projectile _projectile;
    private PlayerController _owner;
    private Jugglernaut _jugglernaut;
    private float _rotation;
    private int _frame;

    public void Setup(Jugglernaut jugglernaut, int frame)
    {
        this._projectile  = base.GetComponent<Projectile>();
        this._owner       = this._projectile.Owner as PlayerController;
        this._jugglernaut = jugglernaut;
        this._frame       = frame;

        this._projectile.OnHitEnemy += OnHitEnemy;
        this._projectile.OnBecameDebris += OnBecameDebris;
    }

    private void OnDestroy()
    {
        CleanupSelf();
    }

    private void CleanupSelf()
    {
        if (this._projectile)
        {
            this._projectile.OnHitEnemy -= OnHitEnemy;
            this._projectile.OnBecameDebris -= OnBecameDebris;
            this._projectile = null;
        }
    }

    private void OnBecameDebris(DebrisObject obj)
    {
        CleanupSelf();
        UnityEngine.Object.Destroy(this);
    }

    private void Start()
    {
        this._projectile.PickFrame(this._frame);
        this._rotation = 360f * UnityEngine.Random.value; // randomize the starting rotation
        this._projectile.sprite.SetGlowiness(glowAmount: Jugglernaut._DEBRIS_GLOW);
    }

    private void Update()
    {
        if (!this._projectile || !this._projectile.specRigidbody)
            return;
        this._rotation += Mathf.Sign(this._projectile.specRigidbody.Velocity.x) * _ROT_RATE * this._projectile.baseData.speed * BraveTime.DeltaTime;
        this._projectile.transform.localRotation = this._rotation.EulerZ();
    }

    private void OnHitEnemy(Projectile bullet, SpeculativeRigidbody body, bool killed)
    {
        if (this._projectile && this._jugglernaut && body.aiActor is AIActor enemy)
            this._jugglernaut.RegisterEnemyHit(enemy);
    }
}
