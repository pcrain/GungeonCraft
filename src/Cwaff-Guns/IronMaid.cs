namespace CwaffingTheGungy;

public class IronMaid : CwaffGun
{
    public static string ItemName         = "Iron Maid";
    public static string ShortDescription = "Night of Knives";
    public static string LongDescription  = "Fires bullets that quickly decelerate and enter stasis after firing. Reloading or switching to a different gun releases all bullets towards the nearest wall or enemy in the player's line of sight.";
    public static string Lore             = "An urban legend tells the story of a Gungeoneer who happened upon a cosmic rift deep in the Gungeon. Upon entering the rift, they found themselves in a great mansion guarded by a maid who wielded no guns, yet produced more bullets than the mind could comprehend. After holding their own for all of 1.3 seconds, the Gungeoneer was overwhelmed by knife-like projectiles that appeared out of nowhere in seeming defiance of time and space. The Gungeoneer awoke to find themself back in the Breach, with this gun lying by their side as the only evidence of their journey.";

    private const float _MAX_AIM_DEV = 15f; // we must be aiming within 15 degrees of an enemy to autotarget

    private int _nextIndex = 0;
    private Vector2 _whereIsThePlayerLooking;
    private AIActor _targetEnemy;
    private LinkedList<IronMaidBullets> _bulletsInStasis = new();

    public static void Init()
    {
        Lazy.SetupGun<IronMaid>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.C, gunClass: GunClass.PISTOL, reloadTime: 0.75f, ammo: 400, shootFps: 24, reloadFps: 24,
            muzzleVFX: "muzzle_iron_maid", muzzleFps: 30, muzzleScale: 0.5f, muzzleAnchor: Anchor.MiddleLeft, fireAudio: "knife_gun_launch",
            reloadAudio: "knife_gun_reload")
          .AddReticle<CwaffReticle>(reticleVFX : VFX.BasicReticle, reticleAlpha : 0.4f, visibility : CwaffReticle.Visibility.ALWAYS, smoothLerp: true)
          .AddToShop(ItemBuilder.ShopType.Trorc)
          .AssignGun(out Gun gun)
          .InitProjectile(GunData.New(clipSize: 20, cooldown: 0.1f, shootStyle: ShootStyle.SemiAutomatic, customClip: true, damage: 5.0f, speed: 40.0f,
            sprite: "kunai", fps: 12, anchor: Anchor.MiddleCenter, hitEnemySound: "knife_hit_enemy_sound", hitWallSound: "knife_hit_wall_sound",
            glowAmount: IronMaidBullets._BASE_GLOW, glowColor: Color.cyan))
          .CopyAllImpactVFX(Items.Thunderclap)
          .Attach<IronMaidBullets>()
          .Assign(out Projectile proj);

        gun.AddSynergyModules(Synergy.MASTERY_IRON_MAID, new ProjectileModule().InitSingleProjectileModule(GunData.New(gun: gun, ammoCost: 0, clipSize: 20, cooldown: 0.1f,
          shootStyle: ShootStyle.SemiAutomatic, angleFromAim: 20f, angleVariance: 8f, ignoredForReloadPurposes: true, mirror: true, baseProjectile: proj)));
    }

    private GameObject GetTargetEnemy(CwaffReticle reticle) => this._targetEnemy ? this._targetEnemy.gameObject : null;
    private Vector2 GetTargetPos(CwaffReticle reticle) => this.PointWherePlayerIsLooking();
    public AIActor CurrentTargetEnemy() => this._targetEnemy ? this._targetEnemy : null;
    public Vector2 PointWherePlayerIsLooking() => this._targetEnemy ? this._targetEnemy.CenterPosition : this._whereIsThePlayerLooking;

    public override void OnPlayerPickup(PlayerController player)
    {
        base.OnPlayerPickup(player);
        gun.GetComponent<CwaffReticle>().targetPosFunc = GetTargetPos;
    }

    public override void OnReloadPressed(PlayerController player, Gun gun, bool manualReload)
    {
        base.OnReloadPressed(player, gun, manualReload);
        LaunchAllBullets(player);
    }

    public override void OnSwitchedToThisGun()
    {
        base.OnSwitchedToThisGun();
        LaunchAllBullets(this.PlayerOwner);
        //NOTE: this might be a vanilla bug where automatic reloads while guns are switched out don't reload secondary modules
        //TODO: can possibly patch ForceImmediateReload() to fix this
        if (this.Mastered && this.gun.ClipShotsRemaining == this.gun.DefaultModule.numberOfShotsInClip)
            foreach (ProjectileModule mod in this.gun.Volley.projectiles)
                if (this.gun.m_moduleData.TryGetValue(mod, out ModuleShootData msd))
                    msd.numberShotsFired = 0;
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        LaunchAllBullets(player);
        player.OnReceivedDamage -= LaunchAllBullets;
        base.OnDroppedByPlayer(player);
    }

    public override void OnDestroy()
    {
        if (this.PlayerOwner)
            LaunchAllBullets(this.PlayerOwner);
        base.OnDestroy();
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        base.OnSwitchedAwayFromThisGun();
        LaunchAllBullets(this.PlayerOwner);
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
        int i = 0;
        foreach (IronMaidBullets rcb in this._bulletsInStasis)
            if (rcb)
                rcb.StartLaunchSequence(i++);
        this._bulletsInStasis.Clear();
        this._nextIndex = 0;
    }

    private AIActor SwitchTargetEnemy()
    {
        if (this.PlayerOwner is not PlayerController pc)
            return null;

        AIActor target = null;
        float closest = _MAX_AIM_DEV;
        Vector2 gunPos = this.gun.barrelOffset.PositionVector2();
        float aimAngle = pc.m_currentGunAngle;
        foreach (AIActor enemy in pc.CurrentRoom.SafeGetEnemiesInRoom())
        {
            if (!enemy.IsHostile(canBeNeutral: true))
                continue;
            Vector2 delta = (enemy.CenterPosition - gunPos);
            float angleFromAim = Mathf.Abs(delta.ToAngle().RelAngleTo(aimAngle));
            if (angleFromAim > closest)
                continue;

            target = enemy;
            closest = angleFromAim;
        }
        return target;
    }

    public override void Update()
    {
        base.Update();
        if (GameManager.Instance.IsLoadingLevel)
            return;
        if (this.PlayerOwner is not PlayerController pc)
            return;

        this._targetEnemy = SwitchTargetEnemy();
        if (!this._targetEnemy)
            this._whereIsThePlayerLooking = Raycast.ToNearestWallOrEnemyOrObject(pc.CenterPosition, pc.CurrentGun.CurrentAngle);
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        if (projectile.GetComponent<IronMaidBullets>() is not IronMaidBullets imb)
            return;
        imb.Setup(this);
        this._bulletsInStasis.AddLast(imb);
    }
}

public class IronMaidBullets : MonoBehaviour
{
    private const float _TIME_BEFORE_STASIS     = 0.2f;
    private const float _GLOW_TIME              = 0.5f;
    private const float _GLOW_MAX               = 10f;
    private const float _LAUNCH_DELAY           = 0.04f;
    private const float _LAUNCH_SPEED           = 50f;

    internal const float _BASE_GLOW              = 3f;

    private PlayerController _owner;
    private Projectile       _projectile;
    private IronMaid         _ironMaid;
    private float            _moveTimer;
    private bool             _launchSequenceStarted;
    private bool             _wasEverInStasis;
    private int              _index;

    public void Setup(IronMaid ironMaid)
    {
        this._ironMaid = ironMaid;
    }

    private IEnumerator Start()
    {
        this._projectile            = base.GetComponent<Projectile>();
        this._owner                 = _projectile.Owner as PlayerController;
        this._launchSequenceStarted = false;
        this._wasEverInStasis       = false;
        this._index                 = 0;

        // Phase 1 / 5 -- the initial fire
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
        Vector2 pos = this._projectile.SafeCenter;
        Vector2 targetDir = this._projectile.Direction;
        AIActor targetEnemy = null;
        while (this._ironMaid)
        {
            targetEnemy = this._ironMaid.CurrentTargetEnemy();
            targetDir = (targetEnemy ? targetEnemy.CenterPosition : this._ironMaid.PointWherePlayerIsLooking()) - pos;
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
            if (targetEnemy)
            {
                targetDir = targetEnemy.CenterPosition - pos;
                _projectile.SendInDirection(targetDir, true); // rotate the projectile
            }
            float glowAmount = (_GLOW_TIME - this._moveTimer) / _GLOW_TIME;
            m.SetFloat("_EmissivePower", _BASE_GLOW + glowAmount * _GLOW_MAX);
            this._moveTimer -= BraveTime.DeltaTime;
            yield return null;
        }

        // Phase 4 / 5 -- the launch queue
        this._moveTimer = _LAUNCH_DELAY * this._index;
        while (this._moveTimer > 0)
        {
            if (targetEnemy)
            {
                targetDir = targetEnemy.CenterPosition - pos;
                _projectile.SendInDirection(targetDir, true); // rotate the projectile
            }
            this._moveTimer -= BraveTime.DeltaTime;
            yield return null;
        }

        // Phase 5 / 5 -- the launch
        this._projectile.SetSpeed(_LAUNCH_SPEED * (this._owner ? this._owner.ProjSpeedMult() : 1f));
        _projectile.SendInDirection(targetDir, true);
        this._projectile.gameObject.Play("knife_gun_launch");

        yield break;
    }

    public void StartLaunchSequence(int index)
    {
        this._index = index;
        this._launchSequenceStarted = true;
    }
}
