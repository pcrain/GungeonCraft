namespace CwaffingTheGungy;

public class BBGun : CwaffGun
{
    public static string ItemName         = "B. B. Gun";
    public static string ShortDescription = "Spare No One";
    public static string LongDescription  = "Fires a single large projectile that bounces off walls and knocks enemies around with extreme force. Ammo can only be regained by interacting with the projectiles once they have come to a halt.";
    public static string Lore             = "This gun was originally used in the mid-18th century for hunting turkeys, as they were the only birds slow enough to actually hit with any degree of reliability. While hunters quickly decided that using a large, slow, rolling projectile wasn't ideal for hunting, the gun's legacy lives on today in shooting arenas known as \"alleys\", where sporting enthusiasts roll similar projectiles against red and white wooden objects in hopes of scoring a \"turkey\" themselves.";

    private static readonly float[] _CHARGE_LEVELS  = {0.25f,0.5f,1.0f,2.0f};
    private float                   _lastCharge     = 0.0f;

    public static void Init()
    {
        Gun gun = Lazy.SetupGun<BBGun>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.B, gunClass: GunClass.CHARGE, reloadTime: 0.5f, ammo: 3, canGainAmmo: false,
            shootFps: 10, chargeFps: 16, loopChargeAt: 32, muzzleVFX: "muzzle_b_b_gun", muzzleFps: 30, muzzleScale: 0.5f, muzzleAnchor: Anchor.MiddleCenter,
            fireAudio: "Play_WPN_seriouscannon_shot_01", reloadAudio: "Play_ENM_flame_veil_01");

        Projectile p = gun.InitProjectile(GunData.New(
          clipSize: 3, cooldown: 0.7f, angleVariance: 10.0f, shootStyle: ShootStyle.Charged, sequenceStyle: ProjectileSequenceStyle.Ordered,
          customClip: true, speed: 20f, range: 999999f, sprite: "bball", fps: 20, anchor: Anchor.MiddleCenter, hitSound: "bb_impact_sound",
          anchorsChangeColliders: false, overrideColliderPixelSizes: new IntVector2(2, 2))) // prevent uneven colliders from glitching into walls
        .Attach<PierceProjModifier>(pierce => {
          pierce.penetration = Mathf.Max(pierce.penetration, 999);
          pierce.penetratesBreakables = true; })
        .Attach<BounceProjModifier>(bounce => {
          bounce.numberOfBounces     = Mathf.Max(bounce.numberOfBounces, 999);
          bounce.chanceToDieOnBounce = 0f;
          bounce.onlyBounceOffTiles  = true; })
        .CopyAllImpactVFX(Items.Crestfaller);

        gun.DefaultModule.chargeProjectiles.Clear();
        for (int i = 0; i < _CHARGE_LEVELS.Length; i++)
            gun.DefaultModule.chargeProjectiles.Add(new ProjectileModule.ChargeProjectile {
                Projectile = p.Clone(GunData.New(speed: 40f + 20f * i)).Attach<TheBB>(),
                ChargeTime = _CHARGE_LEVELS[i],
            });
    }
}

public class TheBB : MonoBehaviour
{
    private const float _BB_DAMAGE_SCALE    = 2.0f;
    private const float _BB_FORCE_SCALE     = 2.0f;
    private const float _BB_SPEED_DECAY     = 3.0f;
    private const float _BASE_EMISSION      = 3.0f;
    private const float _EXTRA_EMISSION     = 30.0f;
    private const float _BASE_ANIM_SPEED    = 2.0f;
    private const float _BOUNCE_SPEED_DECAY = 0.9f;

    private Projectile _projectile;
    private PlayerController _owner;
    private float _maxSpeed = 0f;

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._owner = this._projectile.Owner as PlayerController;

        if (!this._projectile.FiredForFree())
            this._projectile.OnDestruction += CreateInteractible;
        this._maxSpeed = this._projectile.baseData.speed;

        this._projectile.sprite.SetGlowiness(glowAmount: 1000f, glowColor: Color.magenta);

        this.GetComponent<BounceProjModifier>().OnBounce += this.OnBounce;
    }

    public void OnBounce()
    {
        this._projectile.MultiplySpeed(_BOUNCE_SPEED_DECAY);
        this._projectile.SendInDirection(this._projectile.m_currentDirection, resetDistance: false, updateRotation: true);
        base.gameObject.Play("bb_impact_sound");
    }

    private void CreateInteractible(Projectile p)
    {
        MiniInteractable mi = MiniInteractable.CreateInteractableAtPosition(
          this._projectile.sprite,
          this._projectile.SafeCenter,
          BBInteractScript);
            mi.doHover = true;
            mi.sprite.SetGlowiness(glowAmount: _BASE_EMISSION, glowColor: Color.magenta);
    }

    private void Update()
    {
        float newSpeed = Mathf.Max(this._projectile.baseData.speed - _BB_SPEED_DECAY * BraveTime.DeltaTime, 0.0001f); //TODO: maybe use real friction

        Material m = this._projectile.sprite.renderer.material;
        m.SetFloat("_EmissivePower", _BASE_EMISSION + _EXTRA_EMISSION * (newSpeed / _maxSpeed));
        m.SetFloat("_Cutoff", 0.1f);

        if (newSpeed <= 1)
        {
            this._projectile.DieInAir(suppressInAirEffects: true);
            return;
        }

        this._projectile.SetSpeed(newSpeed);
        this._projectile.baseData.damage        = _BB_DAMAGE_SCALE * newSpeed;
        this._projectile.baseData.force         = _BB_FORCE_SCALE * newSpeed;
        this._projectile.spriteAnimator.ClipFps = Mathf.Min(_BASE_ANIM_SPEED * newSpeed, 60f);
        Lazy.PlaySoundUntilDeathOrTimeout("bb_rolling", this._projectile.gameObject, 0.1f);
    }

    public static IEnumerator BBInteractScript(MiniInteractable i, PlayerController p)
    {
        if ((p.FindBaseGun<BBGun>() is Gun gun) && (gun.CurrentAmmo < gun.AdjustedMaxAmmo))
        {
            gun.CurrentAmmo += 1;
            gun.ForceImmediateReload();
            Lazy.DoPickupAt(i.sprite.WorldCenter);
            UnityEngine.Object.Destroy(i.gameObject);
        }
        i.interacting = false;
        yield break;
    }
}
