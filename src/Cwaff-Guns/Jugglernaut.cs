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

    internal static List<string> _JuggleAnimations;
    internal static List<float> _MinEmission = new(){0f, 10f, 50f, 100f, 200f, 400f};
    internal static string _TrueIdleAnimation;
    internal static IntVector2 _CarryOffset        = new IntVector2(-14, -8);
    internal static IntVector2 _FlippedCarryOffset = new IntVector2(21, -8);

    private int _juggleLevel = 0;
    private Coroutine _glowRoutine = null;
    private List<AIActor> _juggledEnemies = new();
    private bool _cachedFlipped = false;
    private bool _firstSpriteCheck = true;

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<Jugglernaut>(ItemName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.B, gunClass: GunClass.SILLY, reloadTime: 0.0f, ammo: 240, shootFps: 30, reloadFps: 40, preventRotation: true);
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

        gun.InitProjectile(GunData.New(clipSize: -1, cooldown: 0.4f, shootStyle: ShootStyle.SemiAutomatic, damage: 10.0f, speed: 70.0f,
          sprite: "jugglernaut_ball", fps: 12, scale: 0.5f, anchor: Anchor.MiddleLeft));
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
            cursor.Emit(OpCodes.Call, typeof(JugglernautCeaseAttackPatch).GetMethod("ShouldUpdateAttachPoints", BindingFlags.Static | BindingFlags.NonPublic));
        }

        private static bool ShouldUpdateAttachPoints(bool origVal, Gun gun)
        {
            return origVal && !gun.GetComponent<Jugglernaut>();
        }
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

    public override void OnDestroy()
    {
        if (this.Player && this.Player.healthHaver)
            this.Player.healthHaver.OnDamaged -= DroppingTheBall;
        base.OnDestroy();
    }

    private void DroppingTheBall(float resultValue, float maxValue, CoreDamageTypes damageTypes, DamageCategory damageCategory, Vector2 damageDirection)
    {
        if (this._juggledEnemies.Count() == 0)
            return;

        this.Player.gameObject.Play("juggle_drop_sound");
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

        gun.gameObject.PlayOnce("alyx_shoot_sound"); // necessary here since the gun doesn't use a fire animation and won't trigger a fire audio event
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

    private void OnHitEnemy(Projectile bullet, SpeculativeRigidbody body, bool killed)
    {
        if (body.aiActor is AIActor enemy)
            this._jugglernaut?.RegisterEnemyHit(enemy);
    }
}
