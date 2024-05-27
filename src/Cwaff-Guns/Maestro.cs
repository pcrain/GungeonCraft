namespace CwaffingTheGungy;


/* TODO:
    - sound effects
    - muzzle flash
    - better enemy targeting vfx
    - better projectile targeting vfx
    - projectile trails after being redirected
*/

public class Maestro : CwaffGun
{
    public static string ItemName         = "Maestro";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    private const float _MAX_PROJECTILE_TARGET_ANGLE  = 20f; // for controller
    private const float _MAX_PROJECTILE_TARGET_RADIUS = 2f; // for mouse

    internal static GameObject _RuneLarge               = null;
    internal static GameObject _RuneSmall               = null;
    internal static TrailController _MaestroTrailPrefab = null;
    internal static GameObject _LineParticleVFX         = null;

    private int        _targetEnemyIndex = 0;
    private AIActor    _targetEnemy      = null;
    private Projectile _targetProjectile = null;
    private GameObject _enemyTargetVFX   = null;
    private List<FancyVFX> _targetLine   = new();

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<Maestro>(ItemName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.D, gunClass: GunClass.CHARM, reloadTime: 0.0f, ammo: 500, shootFps: 2);

        gun.InitProjectile(GunData.New(clipSize: -1, cooldown: 0.2f, angleVariance: 15.0f,
          shootStyle: ShootStyle.Automatic, damage: 9f, speed: 60.0f,
          sprite: "maestro_bullet", fps: 12, scale: 0.5f, anchor: Anchor.MiddleCenter));

        _RuneLarge  = VFX.Create("maestro_target_enemy_vfx", fps: 2);
        _RuneSmall  = VFX.Create("maestro_target_proj_vfx", fps: 2);

        _MaestroTrailPrefab = VFX.CreateTrailObject(ResMap.Get("maestro_trail")[0], new Vector2(23, 4), new Vector2(0, 0),
            ResMap.Get("maestro_trail"), 60, cascadeTimer: C.FRAME, softMaxLength: 1f, destroyOnEmpty: true);
        _LineParticleVFX = VFX.Create("maestro_particles", fps: 12, loops: true, anchor: Anchor.MiddleCenter);
    }

    private void RedirectProjectile(Projectile p, AIActor targetEnemy, float damage)
    {
        const float REFLECT_SPEED = 60f;
        const float SPREAD = 10f;

        if (this.PlayerOwner is not PlayerController pc)
            return;

        AkSoundEngine.PostEvent("Play_OBJ_metalskin_deflect_01", GameManager.Instance.gameObject);

        p.RemoveBulletScriptControl();
        if (targetEnemy)
        {
            p.Direction = (targetEnemy.CenterPosition - p.specRigidbody.UnitCenter).normalized;
            p.Direction = p.Direction.Rotate(UnityEngine.Random.Range(-SPREAD, SPREAD));
        }
        else
            p.Direction = Lazy.RandomVector();
        if (p.Owner && p.Owner.specRigidbody)
            p.specRigidbody.DeregisterSpecificCollisionException(p.Owner.specRigidbody);
        p.Owner = pc;
        p.SetNewShooter(pc.specRigidbody);
        p.allowSelfShooting = false;
        p.collidesWithPlayer = false;
        p.collidesWithEnemies = true;
        p.specRigidbody.CollideWithTileMap = false;
        p.baseData.damage = damage;
        p.UpdateCollisionMask();
        p.ResetDistance();
        p.Reflected();

        // p.AdjustPlayerProjectileTint(Color.yellow, 1);
        p.AddComponent<CapturedMaestroProjectile>();

        // p.FreezeAndLaunchWithDelay(0.1f, REFLECT_SPEED, sound: "knife_gun_launch");
        p.Speed = REFLECT_SPEED;
    }

    private const bool USE_DISTANCE = false;
    private Projectile GetTargetProjectile()
    {
        if (this.PlayerOwner is not PlayerController pc)
            return null;
        Projectile target = null;
        Vector2 gunPos = this.gun.barrelOffset.PositionVector2();
        float aimAngle = this.gun.CurrentAngle;
        float closest = _MAX_PROJECTILE_TARGET_ANGLE;
        foreach (Projectile p in StaticReferenceManager.AllProjectiles)
        {
            if (!p || !p.isActiveAndEnabled || p.Owner is PlayerController)
                continue;
            Vector2 delta = (p.SafeCenter - gunPos);
            float angleFromAim = Mathf.Abs(delta.ToAngle().RelAngleTo(aimAngle));
            if (angleFromAim > closest)
                continue;

            target = p;
            closest = angleFromAim;
        }
        return target;
    }

    private AIActor SwitchTargetEnemy()
    {
        if (this.PlayerOwner is not PlayerController pc)
            return null;
        if (pc.CurrentRoom is not RoomHandler room)
            return null;
        if (room.GetActiveEnemies(RoomHandler.ActiveEnemyType.All) is not List<AIActor> enemiesInRoom)
            return null;
        if (enemiesInRoom.Count == 0)
            return null;

        AIActor target = null;
        float closest = float.MaxValue;

        if (pc.IsKeyboardAndMouse())
        {
            Vector2 mousePos = pc.unadjustedAimPoint.XY();
            foreach (AIActor enemy in enemiesInRoom)
            {
                if (!enemy || !enemy.healthHaver || enemy.healthHaver.IsDead)
                    continue;
                float sqrMag = (enemy.CenterPosition - mousePos).sqrMagnitude;
                if (sqrMag > closest)
                    continue;
                target = enemy;
                closest = sqrMag;
            }
            return target;
        }

        Vector2 aimVec = pc.m_activeActions.Aim.Vector;
        if (aimVec == Vector2.zero)
        {
            int numEnemies = enemiesInRoom.Count;
            for (int i = 1; i <= numEnemies; ++i)
            {
                int nextIndex = (this._targetEnemyIndex + i) % numEnemies;
                AIActor enemy = enemiesInRoom[nextIndex];
                if (!enemy || !enemy.healthHaver || enemy.healthHaver.IsDead)
                    continue;
                this._targetEnemyIndex = nextIndex;  // cache this so we can cycle through enemies more naturally
                return enemy;
            }
            return null;
        }

        Vector2 gunPos = this.gun.barrelOffset.PositionVector2();
        float aimAngle = aimVec.ToAngle();
        foreach (AIActor enemy in enemiesInRoom)
        {
            if (!enemy || !enemy.healthHaver || enemy.healthHaver.IsDead)
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

    public override void OnReloadPressed(PlayerController player, Gun gun, bool manualReload)
    {
        base.OnReloadPressed(player, gun, manualReload);
        this._targetEnemy = SwitchTargetEnemy();
    }

    const int MAX_STEPS = 30;
    private void UpdateTargetingVFXIfNecessary()
    {
        if (this._enemyTargetVFX == null)
        {
            this._enemyTargetVFX = SpawnManager.SpawnVFX(Maestro._RuneLarge, this.gun.barrelOffset.transform.position, Quaternion.identity);
            this._enemyTargetVFX.SetAlphaImmediate(0.5f);
        }

        if (this._targetEnemy)
        {
            this._enemyTargetVFX.transform.localRotation = (270f * BraveTime.ScaledTimeSinceStartup).EulerZ();
            this._enemyTargetVFX.transform.position = this._targetEnemy.CenterPosition;
            this._enemyTargetVFX.SetAlpha(0.5f);
        }
        else
            this._enemyTargetVFX.SetAlpha(0.0f);
        UpdateTargetingLine();
    }

    private void UpdateTargetingLine()
    {
        const float SEG_PHASE_TIME = 0.25f;
        const float SEG_SPACING    = 1.0f;

        bool haveTarget = this._targetProjectile != null;
        Vector2 start   = this.gun.barrelOffset.transform.position.XY();
        Vector2 end     = haveTarget ? this._targetProjectile.transform.position : start.ToNearestWall(out Vector2 _, this.gun.CurrentAngle);
        Vector2 delta   = (end - start);
        float mag       = delta.magnitude;
        Vector2 dir     = delta / mag;
        int numSegments = Mathf.FloorToInt(Mathf.Min(mag / SEG_SPACING, MAX_STEPS));
        float offset    = (BraveTime.ScaledTimeSinceStartup % SEG_PHASE_TIME) / SEG_PHASE_TIME;
        for (int i = this._targetLine.Count; i < MAX_STEPS; ++i)
        {
            FancyVFX fv = FancyVFX.Spawn(_LineParticleVFX, start, rotation: Lazy.RandomEulerZ());
            fv.GetComponent<tk2dSpriteAnimator>().PlayFromFrame(i % 4);
            this._targetLine.Add(fv);
        }
        for (int i = 0; i < numSegments; ++i)
        {
            Vector2 pos = start + ((i + 1 - offset) * SEG_SPACING * dir);
            if (!this._targetLine[i])
            {
                FancyVFX fv = FancyVFX.Spawn(_LineParticleVFX, pos, rotation: Lazy.RandomEulerZ());
                fv.GetComponent<tk2dSpriteAnimator>().PlayFromFrame(i % 4);
                this._targetLine[i] = fv;
            }
            tk2dBaseSprite sprite = this._targetLine[i].sprite;
            sprite.renderer.enabled = true;
            float alpha;
            if (i == 0)
                alpha = 1f - offset;
            else if (i == numSegments - 1)
                alpha = offset;
            else
                alpha = 1f;
            sprite.renderer.SetAlpha(alpha * (haveTarget ? 1f : 0.125f));
            sprite.transform.position = pos;
        }
        for (int i = numSegments; i < Mathf.Min(this._targetLine.Count, MAX_STEPS); ++i)
            if (this._targetLine[i])
               this._targetLine[i].sprite.renderer.enabled = false;
    }

    public override void Update()
    {
        base.Update();
        if (!this.PlayerOwner)
            return;

        this._targetProjectile = GetTargetProjectile();
        DetermineTargetEnemyIfNecessary();
        UpdateTargetingVFXIfNecessary();
    }

    private void DetermineTargetEnemyIfNecessary()
    {
        if (!this._targetEnemy || !this._targetEnemy.healthHaver || this._targetEnemy.healthHaver.IsDead)
            this._targetEnemy = SwitchTargetEnemy();
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        DetermineTargetEnemyIfNecessary();
        if (this._targetProjectile)
        {
            RedirectProjectile(this._targetProjectile, this._targetEnemy, projectile.baseData.damage);
            base.gameObject.Play("soul_launch_sound");
        }
        projectile.DieInAir(suppressInAirEffects: true, allowActorSpawns: false, allowProjectileSpawns: false, killedEarly: false);
    }

    private void CleanUpVFX(bool destroyed = false)
    {
        if (this._enemyTargetVFX)
            UnityEngine.Object.Destroy(this._enemyTargetVFX);
        for (int i = 0; i < Mathf.Min(this._targetLine.Count, MAX_STEPS); ++i)
            if (this._targetLine[i])
            {
                if (destroyed)
                    UnityEngine.Object.Destroy(this._targetLine[i]);
                else
                    this._targetLine[i].sprite.renderer.enabled = false;
            }
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        base.OnSwitchedAwayFromThisGun();
        CleanUpVFX();
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        base.OnDroppedByPlayer(player);
        CleanUpVFX();
    }

    public override void OnDestroy()
    {
        CleanUpVFX(destroyed: true);
        base.OnDestroy();
    }
}

public class CapturedMaestroProjectile : MonoBehaviour
{
    private Projectile _projectile  = null;
    private PlayerController _owner = null;
    private Shader _oldShader = null;

    private void Awake()
    {

    }

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._owner = this._projectile.Owner as PlayerController;
        this._projectile.OnDestruction += this.OnDestruction;

        // if (this._projectile.sprite)
        // {
        //     this._oldShader = this._projectile.sprite.renderer.material.shader;
        //     this._projectile.sprite.MakeHolographic(green: true);
        // }

        TrailController tc = this._projectile.AddTrailToProjectileInstance(Maestro._MaestroTrailPrefab);
    }

    public void OnDestruction(Projectile p)
    {
        this._projectile.OnDestruction -= this.OnDestruction;
        Teardown();
    }

    public void Teardown()
    {
        // if (this._oldShader && this._projectile && this._projectile.sprite)
        //     this._projectile.sprite.renderer.material.shader = this._oldShader;
        // ETGModConsole.Log($"teardown");
        UnityEngine.Object.Destroy(this);
    }
}
