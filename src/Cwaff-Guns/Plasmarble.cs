
namespace CwaffingTheGungy;

public class Plasmarble : CwaffGun
{
    public static string ItemName         = "Plasmarble";
    public static string ShortDescription = "Stunning in the 80's";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    internal static GameObject _PlasmaLighting = null;
    internal static GameObject _LinkVFXPrefab = null;
    internal static Projectile _FlakProjectile = null;

    private int _chargeLevel = -1;
    private Material _mat = null;

    public static void Init()
    {
        Lazy.SetupGun<Plasmarble>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.C, gunClass: GunClass.CHARGE, reloadTime: 1.0f, ammo: 80,
            fireAudio: "plasmarble_explode_sound", reloadFps: 10, reloadAudio: "plasmarble_charge_sound")
          .AssignGun(out Gun gun)
          .UpdateAnimationFPS(gun.emptyAnimation, 0)
          .InitSpecialProjectile<FancyGrenadeProjectile>(GunData.New(
            clipSize: 1, cooldown: 0.15f, angleVariance: 10.0f, shootStyle: ShootStyle.Charged, range: 9999f, speed: 50f, damage: 3.5f,
            sequenceStyle: ProjectileSequenceStyle.Ordered, sprite: "plasmarble_projectile", fps: 20, anchor: Anchor.MiddleCenter,
            shouldRotate: true, shouldFlipHorizontally: true, surviveRigidbodyCollisions: true, chargeTime: 0.5f))
          .Attach<BounceProjModifier>(bounce => {
            bounce.numberOfBounces = 2;
            bounce.onlyBounceOffTiles = false;
            bounce.ExplodeOnEnemyBounce = false; })
          .Attach<FancyGrenadeProjectile>(g => {
            g.startingHeight   = 0.5f;
            g.minBounceAngle   = 10f;
            g.maxBounceAngle   = 30f;
            g.startingVelocity = 0.5f; })
          .Attach<PlasmarbleProjectile>();

      _FlakProjectile = Items._38Special
        .CloneProjectile(GunData.New(
          sprite: "plasmarble_flak", fps: 1, damage: 3.5f, shouldRotate: true, speed: 50f))
        .Attach<PlasmarbleFlak>();

      _PlasmaLighting = VFX.Create("plasmarble_lightning", fps: 30, loops: true, anchor: Anchor.MiddleLeft);
      _LinkVFXPrefab = Game.Items["shock_rounds"].GetComponent<ComplexProjectileModifier>().ChainLightningVFX.ClonePrefab(deactivate: false);
      _LinkVFXPrefab.GetComponent<tk2dSpriteAnimator>().Library = _PlasmaLighting.GetComponent<tk2dSpriteAnimator>().Library;
      _LinkVFXPrefab.GetComponent<tk2dSpriteAnimator>().DefaultClipId = _PlasmaLighting.GetComponent<tk2dSpriteAnimator>().DefaultClipId;
      _LinkVFXPrefab.GetComponent<tk2dTiledSprite>().SetSprite(
        _PlasmaLighting.DefaultAnimation().frames[0].spriteCollection, _PlasmaLighting.DefaultAnimation().frames[0].spriteId);
    }

    public override void Update()
    {
        base.Update();
        if (BraveTime.DeltaTime == 0.0f)
            return;
        if (this.PlayerOwner is not PlayerController)
            return;

        if (gun.ClipShotsRemaining <= 0 && !gun.IsReloading)
            gun.PlayIfExistsAndNotPlaying(gun.emptyAnimation);

        if (!this._mat)
        {
            this._mat = this.gun.sprite.renderer.material;
            this.gun.sprite.SetGlowiness(0f, glowColor: Color.magenta);
            this.gun.sprite.usesOverrideMaterial = false;
        }

        this.gun.LoopSoundIf(this.gun.IsCharging, "plasmarble_charge_sound");
        if (!this.gun.IsCharging)
        {
            this._chargeLevel = -1;
            this.gun.sprite.usesOverrideMaterial = false;
            this._mat.SetFloat("_EmissivePower", 0f);
            this.gun.sprite.UpdateMaterial();
            return;
        }


        int newChargeLevel = 1 + this.gun.GetChargeLevel();
        if (newChargeLevel == this._chargeLevel)
            return;

        this._chargeLevel = newChargeLevel;
        if (newChargeLevel > 0)
            this.gun.sprite.SetGlowiness(newChargeLevel * 100f, glowColor: Color.magenta);
        else
            this.gun.sprite.usesOverrideMaterial = false;
    }
}

public class PlasmarbleFlak : MonoBehaviour
{
    private Vector2 _startPos;

    private void Start()
    {
        Projectile p = base.GetComponent<Projectile>();
        this._startPos = p.SafeCenter;
        p.OnDestruction += this.OnDestruction;
        p.spriteAnimator.PickFrame();
    }

    private void OnDestruction(Projectile p)
    {
        p.gameObject.PlayUnique("plasma_zap_sound");
        PlasmarbleProjectile.ZapRandomEnemies(p, 1, attachToProjectile: false, overrideAngle: (this._startPos - p.SafeCenter).ToAngle());
    }
}

public class PlasmarbleProjectile : MonoBehaviour
{
    private const int _MAX_BOUNCES          = 4;
    private const float _AIR_FRICTION       = 0.90f;
    private const float _BOUNCE_FRICTION    = 0.75f;
    private const float _ZAP_FREQUENCY      = 0.25f;

    private static List<AIActor> _ZappableEnemies = new();

    private Projectile _projectile          = null;
    private PlayerController _owner         = null;
    private FancyGrenadeProjectile _grenade = null;
    private int _bounces                    = 0;
    private float _lastZap                  = 0;
    private RoomHandler _room               = null;
    private bool _inWater                   = false;

    private void Start()
    {
        this._projectile        = base.GetComponent<Projectile>();
        this._owner             = this._projectile.Owner as PlayerController;
        this._grenade           = base.GetComponent<FancyGrenadeProjectile>();
        this._grenade.OnBounce += OnGroundBounce;
        this.GetComponent<BounceProjModifier>().OnBounce += OnGroundBounce;

        this._projectile.m_usesNormalMoveRegardless = true; // disable Helix projectile shenanigans after first bounce
        this._projectile.OnDestruction += this.OnDestruction;
        this._projectile.specRigidbody.CollideWithOthers = false;
        this._projectile.collidesWithEnemies = false;
        this._projectile.UpdateCollisionMask();
        this._projectile.sprite.SetGlowiness(100f, glowColor: Color.magenta);

        this._room = this._projectile.SafeCenter.GetAbsoluteRoom();
    }

    private void OnDestruction(Projectile p)
    {
        Exploder.Explode(p.transform.position, CarpetBomber._CarpetExplosion, p.Direction, ignoreQueues: true);
        ExplodeIntoFlak(4);
    }

    internal static void ZapRandomEnemies(Projectile proj, int numZaps = 1, bool attachToProjectile = true, float? overrideAngle = null)
    {
        Vector2 pos = proj.specRigidbody.UnitCenter;
        Lazy.GetAllNearbyEnemies(ref _ZappableEnemies, pos);
        int numZappableEnemies = _ZappableEnemies.Count;
        if (numZappableEnemies > numZaps) // don't bother shuffling the list if we're going to use the whole thing
            _ZappableEnemies.Shuffle();
        for (int i = 0; i < numZaps; ++i)
        {
            AIActor enemy = (i < numZappableEnemies) ? _ZappableEnemies[i] : null;
            Vector2 zapPos = enemy ? enemy.CenterPosition : proj.specRigidbody.UnitCenter.ToNearestWall(overrideAngle ?? Lazy.RandomAngle());
            OwnerConnectLightningModifier zap = (attachToProjectile ? proj.gameObject : new GameObject("lightningboi"))
              .AddComponent<OwnerConnectLightningModifier>();
            zap.disowned      = true; // don't connect to our owner, we position ourselves manually
            zap.owner         = proj.Owner;
            zap.targetBody    = enemy ? enemy.specRigidbody : null;
            zap.originPos     = pos;
            zap.targetPos     = zapPos;
            zap.linkPrefab    = Plasmarble._LinkVFXPrefab;
            zap.disownTimer   = 0.1f;
            zap.fadeTimer     = 0.15f;
            zap.shrinkFade    = true;
            zap.color         = Color.magenta;
            zap.emissivePower = 50f;
            zap.baseDamage    = proj.baseData.damage;
            zap.MakeGlowy();
        }
    }

    private void Update()
    {
        if (!this._projectile)
            return;

        if (this._bounces >= _MAX_BOUNCES)
        {
            this._projectile.DieInAir(suppressInAirEffects: true);
            return;
        }

        this._projectile.ApplyFriction(_AIR_FRICTION);
        float now = BraveTime.ScaledTimeSinceStartup;
        if ((now - this._lastZap) < _ZAP_FREQUENCY * (this._inWater ? 0.5f : 1f))
            return;

        this._lastZap = now;
        ZapRandomEnemies(this._projectile, 2);
        this._projectile.gameObject.PlayUnique("plasma_zap_sound");

        this._inWater = false;
        Vector2 pos = this._projectile.SafeCenter;
        if (this._room != null && this._room.RoomGoops != null)
            foreach (DeadlyDeadlyGoopManager goopManager in this._room.RoomGoops)
                if (goopManager && goopManager.IsPositionElectrified(pos))
                {
                    this._inWater = true;
                    break;
                }
    }

    public void OnGroundBounce()
    {
        this._projectile.m_usesNormalMoveRegardless = true; // disable Helix projectile shenanigans after first bounce
        this._projectile.baseData.speed *= _BOUNCE_FRICTION;
        ++this._bounces;
    }

    private void ExplodeIntoFlak(int numFlak)
    {
        Vector2 center = this._projectile.SafeCenter;
        float baseAngle = Lazy.RandomAngle();
        float baseSpread = 360.0f / numFlak;
        for (int i = 0; i < numFlak; ++i)
        {
            float flakAngle = baseAngle + (i + UnityEngine.Random.value) * baseSpread;
            Projectile flakProj = SpawnManager.SpawnProjectile(Plasmarble._FlakProjectile.gameObject, center, flakAngle.EulerZ())
              .GetComponent<Projectile>();
            flakProj.SpawnedFromOtherPlayerProjectile = true;
            flakProj.Owner = this._projectile.Owner;
            flakProj.Shooter = this._projectile.Shooter;
            if (this._owner)
                this._owner.DoPostProcessProjectile(flakProj);
        }
        this._projectile.gameObject.PlayUnique("plasmarble_explode_sound");
    }
}