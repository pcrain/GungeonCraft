namespace CwaffingTheGungy;

public class HandCannon : CwaffGun
{
    public static string ItemName         = "Hand Cannon";
    public static string ShortDescription = "Fire Arms";
    public static string LongDescription  = "Fires a high-powered glove that slaps enemies perpendicular to the glove's trajectory with extreme force.";
    public static string Lore             = "Second only to guns, hands are widely considered to be one of the most effective weapons ever brought to the battlefield. In ancient times, combatants would often throw the severed hands of their fallen comrades at their enemies to simultaneously inflict physical and emotional damage, ergo the modern expression \"tossing hands\". The venerable Gun Tzu is thought to be the first to marry guns and hands with his legendary Finger Gun, known for inflicting panic and fear in all who opposed his army. The Hand Cannon is a direct descendant and natural evolution of Gun Tzu's original Finger Gun, packing enough force to make Vasilii Kamotskii blush.";

    private const float _CHARGE_TIME       = 0.5f;
    private const float _CHARGE_LOOP_FRAME = 11f;

    internal static GameObject _SlapppAnimation;
    internal static GameObject _ClapppShockwave;

    public static void Init()
    {
        Gun gun = Lazy.SetupGun<HandCannon>(ItemName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.B, gunClass: GunClass.CHARGE, reloadTime: 0.75f, ammo: 100, shootFps: 30,
                reloadFps: (int)(gun.spriteAnimator.GetClipByName(gun.reloadAnimation).frames.Length / gun.reloadTime),
                chargeFps: (int)(_CHARGE_LOOP_FRAME / _CHARGE_TIME), loopChargeAt: (int)_CHARGE_LOOP_FRAME,
                muzzleVFX: "muzzle_hand_cannon", muzzleFps: 30, muzzleScale: 0.5f, muzzleAnchor: Anchor.MiddleCenter,
                fireAudio: "hand_cannon_shoot_sound", reloadAudio: "hand_cannon_reload_sound");
            gun.SetChargeAudio("hand_cannon_charge_sound", 0, 10);

        gun.InitProjectile(GunData.New(clipSize: 2, cooldown: 0.1f, angleVariance: 15.0f, shootStyle: ShootStyle.Charged,
          customClip: true, damage: 40.0f, speed: 40.0f, sprite: "slappp", fps: 30, scale: 0.5f, anchor: Anchor.MiddleCenter,
          chargeTime: _CHARGE_TIME)).Attach<SlappProjectile>();

        _SlapppAnimation = VFX.Create("slappp_vfx", fps: 30, loops: false, anchor: Anchor.MiddleCenter, scale: 0.5f);

        _ClapppShockwave = Items.ChargeShot.AsGun().muzzleFlashEffects.effects[0].effects[0].effect;
    }
}

public class SlappProjectile : MonoBehaviour
{
    private const int   _SLAPPP_FRAME         = 8;   // frame 8 is the meat of the slappp animation
    private const float _SLAPPP_FORCE         = 300f;
    private const float _SLAPPP_STUN          = 2f;
    private const float _CLAPPP_STUN          = 10f;
    private const float _SLAPP_RADIUS_SQUARED = 9f;

    private Projectile _projectile = null;
    private AIActor    _slapVictim = null;
    private Vector2    _slapAngle  = Vector2.zero;
    private bool       _flipped    = false;
    private float      _slapDamage = 0f;
    private FancyVFX   _vfx        = null;
    private bool       _isMastered = false;

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        if (this._projectile.Owner is not PlayerController player)
            return;

        this._flipped    = player.sprite.FlipX;
        this._isMastered = player.PlayerHasActiveSynergy(Synergy.MASTERY_HAND_CANNON);
        this._slapDamage = this._projectile.baseData.damage * (this._isMastered ? 2f : 1f);
        this._projectile.specRigidbody.OnPreRigidbodyCollision += this.OnPreCollision;
    }

    private void OnPreCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
    {
        if (!otherRigidbody || otherRigidbody.gameActor is not AIActor enemy || !enemy.IsHostile(canBeNeutral: true))
            return;

        PhysicsEngine.SkipCollision = true;
        this._slapVictim = enemy;
        this._slapAngle = this._isMastered
            ? this._projectile.Direction
            : this._projectile.Direction.Rotate(this._flipped ? -90f : 90f);
        this._vfx = FancyVFX.SpawnUnpooled( //NOTE: absolutely MUST ignore pools or VFX objects with preexisting FancyVFX components might get reused
            prefab      : HandCannon._SlapppAnimation,
            position    : this._projectile.sprite.transform.position,
            rotation    : this._projectile.sprite.transform.rotation,
            lifetime    : 0.5f,
            fadeOutTime : 0.20f,
            parent      : enemy.sprite.transform);
        this._vfx.sprite.FlipY = this._flipped; //smack in the opposite direction by flipping vertically, not horizontally
        this._vfx.gameObject.AddAnimationEvent(SlapppEvent, _SLAPPP_FRAME, this._isMastered ? "clappp_sound" : "slappp_sound");
        if (this._isMastered)
        {
            FancyVFX otherHand = FancyVFX.SpawnUnpooled(
                prefab      : HandCannon._SlapppAnimation,
                position    : this._projectile.sprite.transform.position,
                rotation    : this._projectile.sprite.transform.rotation,
                lifetime    : 0.5f,
                fadeOutTime : 0.20f,
                parent      : enemy.sprite.transform);
            otherHand.sprite.FlipY = !this._flipped;
        }
        this._projectile.DieInAir(suppressInAirEffects: true, allowActorSpawns: false, allowProjectileSpawns: false, killedEarly: false);
    }

    private void SlapppEvent(tk2dSpriteAnimator animator, tk2dSpriteAnimationClip clip, int frame)
    {
        this._vfx.transform.parent = null; // don't follow the enemy after we've followed through on the slap
        if (!this._slapVictim)
            return;

        Vector2 victimPos = this._slapVictim.CenterPosition;
        foreach (AIActor enemy in StaticReferenceManager.AllEnemies)
        {
            if (!enemy || !enemy.isActiveAndEnabled)
                continue;
            if ((enemy.sprite.WorldCenter - victimPos).sqrMagnitude > _SLAPP_RADIUS_SQUARED)
                continue;
            if (enemy.healthHaver is not HealthHaver hh)
                continue;

            hh.ApplyDamage(this._slapDamage, Vector2.zero, "SLAPPP", CoreDamageTypes.None, DamageCategory.Collision, true);
            if (!hh.IsBoss && !hh.IsSubboss && enemy.knockbackDoer is KnockbackDoer kb)
                kb.ApplyKnockback(this._slapAngle, _SLAPPP_FORCE);
            if (enemy.behaviorSpeculator && !enemy.behaviorSpeculator.ImmuneToStun)
                enemy.behaviorSpeculator.Stun(this._isMastered ? _CLAPPP_STUN : _SLAPPP_STUN);
            GameObject go = SpawnManager.SpawnVFX(HandCannon._ClapppShockwave, victimPos, Quaternion.identity, ignoresPools: true);
            tk2dSprite sprite = go.GetComponent<tk2dSprite>();
            if (this._isMastered)
                sprite.scale = new Vector3(3f, 3f, 1f);
            sprite.PlaceAtScaledPositionByAnchor(this._vfx.sprite.WorldCenter, Anchor.MiddleCenter);
        }
        UnityEngine.Object.Destroy(this);
    }
}
