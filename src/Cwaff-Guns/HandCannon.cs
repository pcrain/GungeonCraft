﻿namespace CwaffingTheGungy;

public class HandCannon : CwaffGun
{
    public static string ItemName         = "Hand Cannon";
    public static string ShortDescription = "Fire Arms";
    public static string LongDescription  = "Fires a high-powered glove that slaps enemies with extreme force perpendicular to the glove's trajectory.";
    public static string Lore             = "Second only to guns, hands are widely considered to be one of the most effective weapons ever brought to the battlefield. In ancient times, combatants would often throw the severed hands of their fallen comrades at their enemies to simultaneously inflict physical and emotional damage, ergo the modern expression \"tossing hands\". The venerable Gun Tzu is thought to be the first to marry guns and hands with his legendary Finger Gun, known for inflicting panic and fear in all who opposed his army. The Hand Cannon is a direct descendant and natural evolution of Gun Tzu's original Finger Gun, packing enough force to make Vasilii Kamotskii blush.";

    private const float _CHARGE_TIME       = 0.5f;
    private const float _CHARGE_LOOP_FRAME = 11f;

    internal static GameObject _SlapppAnimation;
    internal static GameObject _ClapppShockwave;

    public static void Init()
    {
        Lazy.SetupGun<HandCannon>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.B, gunClass: GunClass.CHARGE, reloadTime: 0.75f, ammo: 100, shootFps: 30, reloadFps: 1,
            chargeFps: (int)(_CHARGE_LOOP_FRAME / _CHARGE_TIME), loopChargeAt: (int)_CHARGE_LOOP_FRAME, muzzleVFX: "muzzle_hand_cannon", muzzleFps: 30,
            muzzleScale: 0.5f, muzzleAnchor: Anchor.MiddleCenter, fireAudio: "hand_cannon_shoot_sound", reloadAudio: "hand_cannon_reload_sound", smoothReload: 0.1f)
          .SetChargeAudio("hand_cannon_charge_sound", 0, 10)
          .InitProjectile(GunData.New(clipSize: 2, cooldown: 0.1f, angleVariance: 15.0f, shootStyle: ShootStyle.Charged,
            customClip: true, damage: 40.0f, speed: 40.0f, sprite: "slappp", fps: 30, scale: 0.5f, anchor: Anchor.MiddleCenter,
            chargeTime: _CHARGE_TIME))
          .Attach<SlappProjectile>();

        _SlapppAnimation = VFX.Create("slappp_vfx", fps: 30, loops: false, scale: 0.5f);
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
    private GameObject _vfx        = null;
    private bool       _isMastered = false;

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        if (this._projectile.Owner is not PlayerController player)
            return;

        this._flipped    = player.sprite.FlipX;
        this._isMastered = player.HasSynergy(Synergy.MASTERY_HAND_CANNON);
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
        Transform pt = this._projectile.sprite.transform;
        //NOTE: absolutely MUST ignore pools or VFX objects with preexisting components might get reused
        this._vfx = SpawnManager.SpawnVFX(HandCannon._SlapppAnimation, pt.position, pt.rotation, ignoresPools: true);
        this._vfx.transform.parent = enemy.sprite.transform;
        this._vfx.ExpireIn(0.5f, fadeFor: 0.2f);
        this._vfx.AddAnimationEvent(SlapppEvent, _SLAPPP_FRAME, this._isMastered ? "clappp_sound" : "slappp_sound");
        this._vfx.GetComponent<tk2dSprite>().FlipY = this._flipped; //smack in the opposite direction by flipping vertically, not horizontally
        if (this._isMastered)
        {
            GameObject otherHand = SpawnManager.SpawnVFX(HandCannon._SlapppAnimation, pt.position, pt.rotation, ignoresPools: true);
            otherHand.transform.parent = enemy.sprite.transform;
            otherHand.ExpireIn(0.5f, fadeFor: 0.2f);
            otherHand.AddAnimationEvent(SlapppEvent, _SLAPPP_FRAME, this._isMastered ? "clappp_sound" : "slappp_sound");
            otherHand.GetComponent<tk2dSprite>().FlipY = !this._flipped;
        }
        this._projectile.DieInAir(suppressInAirEffects: true, allowActorSpawns: false, allowProjectileSpawns: false, killedEarly: false);
    }

    private static List<AIActor> _Enemies = new();
    private void SlapppEvent(tk2dSpriteAnimator animator, tk2dSpriteAnimationClip clip, int frame)
    {
        this._vfx.transform.parent = null; // don't follow the enemy after we've followed through on the slap
        if (!this._slapVictim)
            return;
        PlayerController owner = this._projectile.Owner as PlayerController;
        Vector2 victimPos = this._slapVictim.CenterPosition;
        Vector2 impactPos = this._vfx.GetComponent<tk2dSprite>().WorldCenter;
        _Enemies.Clear();
        if (owner.CurrentRoom != null)
            owner.CurrentRoom.SafeGetEnemiesInRoom(ref _Enemies);
        _Enemies.AddUnique(this._slapVictim); // even if our room is null, make sure we hit our original victim
        foreach (AIActor enemy in _Enemies)
        {
            if (!enemy || !enemy.isActiveAndEnabled)
                continue;
            if ((enemy.CenterPosition - victimPos).sqrMagnitude > _SLAPP_RADIUS_SQUARED)
                continue;
            if (enemy.healthHaver is not HealthHaver hh)
                continue;

            hh.ApplyDamage(this._slapDamage, Vector2.zero, "SLAPPP", CoreDamageTypes.None, DamageCategory.Collision, true);
            if (!hh.IsBoss && !hh.IsSubboss && enemy.knockbackDoer is KnockbackDoer kb)
                kb.ApplyKnockback(this._slapAngle, _SLAPPP_FORCE * (owner ? owner.KnockbackMult() : 1f));
            if (enemy.behaviorSpeculator && !enemy.behaviorSpeculator.ImmuneToStun)
                enemy.behaviorSpeculator.Stun(this._isMastered ? _CLAPPP_STUN : _SLAPPP_STUN);
            GameObject go = SpawnManager.SpawnVFX(HandCannon._ClapppShockwave, victimPos, Quaternion.identity, ignoresPools: true);
            tk2dSprite sprite = go.GetComponent<tk2dSprite>();
            if (this._isMastered)
                sprite.scale = new Vector3(3f, 3f, 1f);
            sprite.PlaceAtScaledPositionByAnchor(impactPos, Anchor.MiddleCenter);
        }
        UnityEngine.Object.Destroy(this);
    }
}
