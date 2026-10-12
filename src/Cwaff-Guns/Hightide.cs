namespace CwaffingTheGungy;

public class Hightide : CwaffGun
{
    public static string ItemName         = "Hightide";
    public static string ShortDescription = "Washed Up";
    public static string LongDescription  = "Fires a slow-moving tidal wave that pushes enemies backwards and can hit them multiple times. Longer charge times result in larger waves.";
    public static string Lore             = "";

    private const int   _MAX_PROJECTILES       = 6;
    private const float _CHARGE_PER_PROJECTILE = 0.55f;

    public static void Init()
    {
        Lazy.SetupGun<Hightide>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.C, gunClass: GunClass.CHARGE, reloadTime: 0.0f, ammo: 540, shootFps: 60, chargeFps: 40, smoothReload: 0.1f, muzzleFrom: Items.Blooper, fireAudio: "hightide_shoot_sound")
          .AssignGun(out Gun gun)
          .InitProjectile(GunData.New(
            clipSize: 1, cooldown: 0.15f, angleVariance: 10.0f, shootStyle: ShootStyle.Charged, range: 9999f, speed: 7f, force: 30f, damage: 2f, sequenceStyle: ProjectileSequenceStyle.Ordered,
            barrageSize: _MAX_PROJECTILES * 2 + 1, surviveRigidbodyCollisions: true, multihitDelay: 0.05f, sprite: "tidal_projectile", fps: 30, pierceInternalWalls: true, pierceBreakables: true,
            customClip: true))
          .SetAllImpactVFX(VFX.CreatePool("tidal_splash_vfx", fps: 20, loops: false, anchor: Anchor.MiddleLeft, scale: 0.5f,
            emissivePower: 37.0f, emissiveColour: new Color(0.0f, 0.0f, 1.0f), emissiveColorPower: 7.0f, emissiveSensitivity: 0.8f))
          .Attach<GoopModifier>(g => {
            g.goopDefinition         = EasyGoopDefinitions.WaterGoop;
            g.SpawnGoopOnCollision   = true;
            g.CollisionSpawnRadius   = 2f;
            g.SpawnGoopInFlight      = true;
            g.InFlightSpawnRadius    = 0.4f;
            g.InFlightSpawnFrequency = 0.01f;})
          .Assign(out Projectile p);
        p.sprite.MakeGlowyBetter(glowAmount: 37.0f, glowColor: new Color(0.0f, 0.0f, 1.0f), glowColorPower: 7.0f, sensitivity: 0.6f, overrideColor: new Color(0.5f, 0.5f, 1.0f));

        p.gameObject.GetComponent<SpeculativeRigidbody>().CanPush = true;
        int n = 0;
        for (int i = 0; i <= _MAX_PROJECTILES; i++)
        {
            for (int j = 0; j < ((i == 0) ? 1 : 2); ++j)
            {
              ProjectileModule imod = gun.RawSourceVolley.projectiles[n++];
              imod.angleVariance = 0.0f;
              imod.angleFromAim = 0.0f;
              imod.projectiles.Clear();
              imod.triggerCooldownForAnyChargeAmount = true;
              float spread = ((j == 0) ? i : -i) * 0.8f;
              imod.chargeProjectiles = [new ProjectileModule.ChargeProjectile {
                  Projectile     = p.Clone(GunData.New(hitWallSound: "hightide_impact_wall_sound")).Attach<TidalSpreader>(t => { t.targetSpread = spread; t.spreadTime = Mathf.Abs(i * 0.15f); }),
                  ChargeTime     = _CHARGE_PER_PROJECTILE * (i + 1),
              }];
            }
        }
    }
}

public class TidalSpreader : MonoBehaviour
{
    public float targetSpread = 0.0f;
    public float spreadTime = 0.0f;

    private Projectile _projectile;
    private PlayerController _owner;
    private float _spawnTime;
    private Vector2 _spawnPerp;
    private float _curSpread;
    private bool _mastered;

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._owner = this._projectile.Owner as PlayerController;
        this._spawnTime = BraveTime.ScaledTimeSinceStartup;
        this._spawnPerp = (this._projectile.OriginalDirection() + 90f).ToVector();
        this._curSpread = 0.0f;
        this._mastered = this._projectile.Mastered<Hightide>();
        if (!this._mastered)
          return;

        this._projectile.collidesWithProjectiles = true;
        this._projectile.UpdateCollisionMask();
        this._projectile.specRigidbody.OnPreRigidbodyCollision += this.OnPreRigidbodyCollision;
        this._projectile.specRigidbody.OnCollision += this.OnCollision;
    }

    private void OnPreRigidbodyCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
    {
      if (this._mastered && otherRigidbody.gameObject.GetComponent<Projectile>() is Projectile proj && proj.Owner is PlayerController)
        PhysicsEngine.SkipCollision = true;
    }

    private void OnCollision(CollisionData collisionData)
    {
      if (collisionData.OtherRigidbody is not SpeculativeRigidbody body)
        return;
      if (body.gameObject.GetComponent<Projectile>() is not Projectile proj)
        return;
      if (proj.Owner is PlayerController)
        return;
      Vector2 projPos = proj.SafeCenter;
      proj.DieInAir();
    }

    private void Update()
    {
      if (this._curSpread >= 1.0f)
        return;
      float lastSpread = this._curSpread;
      this._curSpread = Ease.OutQuad(Mathf.Min((BraveTime.ScaledTimeSinceStartup - _spawnTime) / this.spreadTime, 1.0f));
      float spreadDelta = this._curSpread - lastSpread;
      Vector2 offset = (targetSpread * spreadDelta) * _spawnPerp;
      if (this._projectile)
        this._projectile.specRigidbody.Position = new Position(this._projectile.specRigidbody.Position.UnitPosition + offset);
    }
}
