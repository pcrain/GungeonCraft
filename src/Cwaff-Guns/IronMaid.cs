namespace CwaffingTheGungy;

public class IronMaid : AdvancedGunBehavior
{
    public static string ItemName         = "Iron Maid";
    public static string ShortDescription = "Night of Knives";
    public static string LongDescription  = "Bullets quickly decelerate and enter stasis after firing. Reloading or switching to a different gun releases all bullets towards the nearest wall or enemy in the player's line of sight.";
    public static string Lore             = "An urban legend tells the story of a Gungeoneer who happened upon a cosmic rift deep in the Gungeon. Upon entering the rift, they found themselves in a great mansion guarded by a maid who wielded no guns, yet produced more bullets than the mind could comprehend. After holding their own for all of 1.3 seconds, the Gungeoneer was overwhelmed by knife-like projectiles that appeared out of nowhere in seeming defiance of time and space. The Gungeoneer awoke to find themself back in the Breach, with this gun lying by their side as the only evidence of their journey.";

    private int _nextIndex = 0;
    private Vector2 _whereIsThePlayerLooking;

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<IronMaid>(ItemName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.C, gunClass: GunClass.PISTOL, reloadTime: 0.75f, ammo: 400, shootFps: 24, reloadFps: 24,
                muzzleVFX: "muzzle_iron_maid", muzzleFps: 30, muzzleScale: 0.5f, muzzleAnchor: Anchor.MiddleLeft, fireAudio: "knife_gun_launch",
                reloadAudio: "knife_gun_reload");
            gun.AddToSubShop(ItemBuilder.ShopType.Trorc);

        gun.InitProjectile(GunData.New(clipSize: 20, cooldown: 0.1f, shootStyle: ShootStyle.SemiAutomatic, customClip: true,
          damage: 5.0f, speed: 40.0f, sprite: "kunai", fps: 12, anchor: Anchor.MiddleCenter)).Attach<RainCheckBullets>();
    }

    public override void OnReloadPressed(PlayerController player, Gun gun, bool manualReload)
    {
        base.OnReloadPressed(player, gun, manualReload);
        LaunchAllBullets(player);
    }

    public override void OnSwitchedToThisGun()
    {
        base.OnSwitchedToThisGun();
        LaunchAllBullets(this.Player);
    }

    public override void OnDropped()
    {
        base.OnDropped();
        LaunchAllBullets(this.Player);
        this.Player.OnReceivedDamage -= LaunchAllBullets;
    }

    public override void OnDestroy()
    {
        if (this.Player)
            LaunchAllBullets(this.Player);
        base.OnDestroy();
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        base.OnSwitchedAwayFromThisGun();
        LaunchAllBullets(this.Player);
    }

    public int GetNextIndex()
    {
        return ++this._nextIndex;
    }

    private void LaunchAllBullets(GameActor player)
    {
        if (player is PlayerController pc)
            LaunchAllBullets(pc);
    }

    private void LaunchAllBullets(PlayerController pc)
    {
        foreach (Projectile projectile in StaticReferenceManager.AllProjectiles)
        {
            if (projectile.GetComponent<RainCheckBullets>() is not RainCheckBullets rcb)
                continue;
            rcb.StartLaunchSequenceForPlayer(pc);
        }
        this._nextIndex = 0;
    }

    protected override void Update()
    {
        base.Update();
        if (GameManager.Instance.IsLoadingLevel)
            return;
        if (this.Player is not PlayerController pc)
            return;

        this._whereIsThePlayerLooking =
            Raycast.ToNearestWallOrEnemyOrObject(pc.sprite.WorldCenter, pc.CurrentGun.CurrentAngle);
    }

    public Vector2 PointWherePlayerIsLooking()
    {
        return this._whereIsThePlayerLooking;
    }
}

public class RainCheckBullets : MonoBehaviour
{
    private const float _TIME_BEFORE_STASIS     = 0.2f;
    private const float _GLOW_TIME              = 0.5f;
    private const float _GLOW_MAX               = 10f;
    private const float _LAUNCH_DELAY           = 0.04f;
    private const float _LAUNCH_SPEED           = 50f;
    private const float _BASE_GLOW              = 3f;

    private PlayerController _owner;
    private Projectile       _projectile;
    private IronMaid        _raincheck;
    private float            _moveTimer;
    private bool             _launchSequenceStarted;
    private bool             _wasEverInStasis;
    private int              _index;

    private void Start()
    {
        this._projectile            = base.GetComponent<Projectile>();
        this._owner                 = _projectile.Owner as PlayerController;
        this._raincheck             = this._owner.CurrentGun.GetComponent<IronMaid>();
        this._launchSequenceStarted = false;
        this._wasEverInStasis       = false;
        this._index                 = this._raincheck ? this._raincheck.GetNextIndex() : 0;

        this._projectile.specRigidbody.OnCollision += (_) => {
            this._projectile.gameObject.Play("knife_gun_hit");
        };

        StartCoroutine(TakeARainCheck());
    }

    private IEnumerator TakeARainCheck()
    {
        // Phase 1 / 5 -- the initial fire
        this._projectile.sprite.SetGlowiness(glowAmount: _BASE_GLOW, glowColor: Color.cyan);
        this._moveTimer = _TIME_BEFORE_STASIS;
        float decel = this._projectile.baseData.speed / (C.FPS * _TIME_BEFORE_STASIS);
        while (this._moveTimer > 0 && !this._launchSequenceStarted)
        {
            this._moveTimer -= BraveTime.DeltaTime;
            yield return null;
            this._projectile.Accelerate(-decel);
        }

        // Phase 2 / 5 -- the freeze
        this._projectile.SetSpeed(0.01f);
        this._wasEverInStasis = true;
        Vector2 pos = this._projectile.sprite.WorldCenter;
        Vector2 targetDir = this._projectile.Direction;
        while (this._raincheck)
        {
            targetDir = this._raincheck.PointWherePlayerIsLooking() - pos;
            _projectile.SendInDirection(targetDir, true); // rotate the projectile
            if (this._launchSequenceStarted)
                break; // awkward loop construct to make sure we set our targetDir at least once
            yield return null;
        }

        // Phase 3 / 5 -- the glow
        Material m = this._projectile.sprite.renderer.material;
        this._projectile.gameObject.PlayUnique("knife_gun_glow");
        this._moveTimer = _GLOW_TIME;
        while (this._moveTimer > 0)
        {
            float glowAmount = (_GLOW_TIME - this._moveTimer) / _GLOW_TIME;
            m.SetFloat("_EmissivePower", _BASE_GLOW + glowAmount * _GLOW_MAX);
            this._moveTimer -= BraveTime.DeltaTime;
            yield return null;
        }

        // Phase 4 / 5 -- the launch queue
        this._moveTimer = _LAUNCH_DELAY * this._index;
        while (this._moveTimer > 0)
        {
            this._moveTimer -= BraveTime.DeltaTime;
            yield return null;
        }

        // Phase 5 / 5 -- the launch
        this._projectile.SetSpeed(_LAUNCH_SPEED);
        _projectile.SendInDirection(targetDir, true);
        this._projectile.gameObject.Play("knife_gun_launch");

        yield break;
    }

    public void StartLaunchSequenceForPlayer(PlayerController pc)
    {
        if (pc == this._owner) // don't launch projectiles that don't belong to us
            this._launchSequenceStarted = true;
    }
}
