namespace CwaffingTheGungy;

public class Maestro : CwaffGun
{
    public static string ItemName         = "Maestro";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    private const float _MAX_PROJECTILE_TARGET_ANGLE  = 20f; // for controller
    private const float _MAX_PROJECTILE_TARGET_RADIUS = 3f;  // for mouse
    private const float _SQR_TARGET_RADIUS            = _MAX_PROJECTILE_TARGET_RADIUS * _MAX_PROJECTILE_TARGET_RADIUS;
    private const int   _MAX_STEPS                    = 30;

    internal static GameObject _RuneEnemy               = null;
    internal static GameObject _RuneProjectile          = null;
    // internal static GameObject _LineParticleVFX         = null;
    internal static List<Vector3> _ShootBarrelOffsets  = new();

    private int        _targetEnemyIndex    = 0;
    private AIActor    _targetEnemy         = null;
    private Projectile _targetProjectile    = null;
    private GameObject _enemyTargetVFX      = null;
    private GameObject _projectileTargetVFX = null;
    // private List<FancyVFX> _targetLine      = new();

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<Maestro>(ItemName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.B, gunClass: GunClass.CHARM, reloadTime: 0.0f, ammo: 500, shootFps: 24,
                muzzleFrom: Items.FaceMelter, doesScreenShake: false);
            gun.usesContinuousFireAnimation = true;
            gun.LoopAnimation(gun.shootAnimation);

        gun.InitProjectile(GunData.New(clipSize: -1, cooldown: 0.2f, angleVariance: 15.0f,
          shootStyle: ShootStyle.Automatic, damage: 9f, speed: 60.0f,
          sprite: "maestro_bullet", fps: 12, scale: 0.5f, anchor: Anchor.MiddleCenter));

        _RuneEnemy      = VFX.Create("maestro_target_enemy_vfx", fps: 2);
        _RuneProjectile = VFX.Create("maestro_target_projectile_vfx", fps: 2);

        // _LineParticleVFX = VFX.Create("maestro_particles", fps: 12, loops: true, anchor: Anchor.MiddleCenter);

        _ShootBarrelOffsets  = gun.GetBarrelOffsetsForAnimation(gun.shootAnimation);
    }

    private void RedirectProjectile(Projectile p, AIActor targetEnemy, float damage)
    {
        const float REFLECT_SPEED = 60f;
        const float SPREAD = 4f;

        if (this.PlayerOwner is not PlayerController pc)
            return;

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

        p.Speed = REFLECT_SPEED;
        SpawnManager.SpawnVFX(EchoChamber._EchoPrefab, p.SafeCenter, Lazy.RandomEulerZ(), ignoresPools: true).ExpireIn(seconds: 0.5f, fadeFor: 0.5f);

        switch(UnityEngine.Random.Range(0,5))
        {
            case 0: p.gameObject.Play("maestro_launch_asharp"); break;
            case 1: p.gameObject.Play("maestro_launch_csharp"); break;
            case 2: p.gameObject.Play("maestro_launch_dsharp"); break;
            case 3: p.gameObject.Play("maestro_launch_fsharp"); break;
            case 4: p.gameObject.Play("maestro_launch_gsharp"); break;
        }
    }

    private Projectile GetTargetProjectile()
    {
        if (this.PlayerOwner is not PlayerController pc)
            return null;
        Projectile target = null;
        bool keyboard = pc.IsKeyboardAndMouse();
        Vector2 aimPos = keyboard ? pc.unadjustedAimPoint.XY() : this.gun.barrelOffset.PositionVector2();
        float aimAngle = this.gun.CurrentAngle;
        float closest = float.MaxValue;
        foreach (Projectile p in StaticReferenceManager.AllProjectiles)
        {
            if (!p || !p.isActiveAndEnabled || p.Owner is PlayerController)
                continue;
            if (!GameManager.Instance.MainCameraController.PointIsVisible(p.SafeCenter))
                continue; // can't target offscreen projectiles

            float closenessWeight = 0;
            Vector2 delta = (p.SafeCenter - aimPos);
            if (keyboard)
            {
                float sqrMag = delta.sqrMagnitude;
                if (sqrMag > _SQR_TARGET_RADIUS)
                    continue;
                closenessWeight = sqrMag;
            }
            else
            {
                float angleFromAim = Mathf.Abs(delta.ToAngle().RelAngleTo(aimAngle));
                if (angleFromAim > _MAX_PROJECTILE_TARGET_ANGLE)
                    continue;
                float distFromGun = (float)Lazy.FastSqrt(delta.sqrMagnitude);
                // "closeness" is distance from gun times angle from aim -> can still hit far away projectiles, but need accurate aim
                closenessWeight = distFromGun * angleFromAim;
            }
            if (closenessWeight > closest)
                continue;

            target = p;
            closest = closenessWeight;
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
                if (IsUntargetable(enemy))
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
                if (IsUntargetable(enemy))
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
            if (IsUntargetable(enemy))
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
        AIActor lastTargetEnemy = this._targetEnemy;
        this._targetEnemy = SwitchTargetEnemy();
        if (this._targetEnemy && this._targetEnemy != lastTargetEnemy)
            base.gameObject.PlayOnce("maestro_target_sound");
    }

    private void UpdateTargetingVFXIfNecessary()
    {
        if (!this._enemyTargetVFX)
        {
            this._enemyTargetVFX = SpawnManager.SpawnVFX(Maestro._RuneEnemy, this.gun.barrelOffset.transform.position, Quaternion.identity);
            this._enemyTargetVFX.SetAlphaImmediate(0.5f);
        }
        if (!this._projectileTargetVFX)
        {
            this._projectileTargetVFX = SpawnManager.SpawnVFX(Maestro._RuneProjectile, this.gun.barrelOffset.transform.position, Quaternion.identity);
            this._projectileTargetVFX.SetAlphaImmediate(0.75f);
        }

        if (this._targetEnemy)
        {
            this._enemyTargetVFX.transform.localRotation = (270f * BraveTime.ScaledTimeSinceStartup).EulerZ();
            this._enemyTargetVFX.transform.position = Vector2.Lerp(this._enemyTargetVFX.transform.position, this._targetEnemy.CenterPosition, 0.33f);
            this._enemyTargetVFX.SetAlpha(0.5f);
        }
        else
        {
            this._enemyTargetVFX.transform.position = this.PlayerOwner.CenterPosition;
            this._enemyTargetVFX.SetAlpha(0.0f);
        }
        if (this._targetProjectile)
        {
            this._projectileTargetVFX.transform.localRotation = (270f * BraveTime.ScaledTimeSinceStartup).EulerZ();
            this._projectileTargetVFX.transform.position = Vector2.Lerp(this._projectileTargetVFX.transform.position, this._targetProjectile.SafeCenter, 0.33f);
            this._projectileTargetVFX.SetAlpha(0.75f);
        }
        else
        {
            this._projectileTargetVFX.transform.position = this.PlayerOwner.CenterPosition;
            this._projectileTargetVFX.SetAlpha(0.0f);
        }

        // UpdateTargetingLine();
    }

    // NOTE: super useful code i might want later, but doesn't quite fit on this weapon
    // private void UpdateTargetingLine()
    // {
    //     const float SEG_PHASE_TIME = 0.25f;
    //     const float SEG_SPACING    = 1.0f;

    //     bool haveTarget = this._targetProjectile != null;
    //     Vector2 start   = this.gun.barrelOffset.transform.position.XY();
    //     Vector2 end     = haveTarget ? this._targetProjectile.transform.position : start.ToNearestWall(out Vector2 _, this.gun.CurrentAngle);
    //     Vector2 delta   = (end - start);
    //     float mag       = delta.magnitude;
    //     Vector2 dir     = delta / mag;
    //     int numSegments = Mathf.FloorToInt(Mathf.Min(mag / SEG_SPACING, _MAX_STEPS));
    //     float offset    = (BraveTime.ScaledTimeSinceStartup % SEG_PHASE_TIME) / SEG_PHASE_TIME;
    //     for (int i = this._targetLine.Count; i < _MAX_STEPS; ++i)
    //     {
    //         FancyVFX fv = FancyVFX.Spawn(_LineParticleVFX, start, rotation: Lazy.RandomEulerZ());
    //         fv.GetComponent<tk2dSpriteAnimator>().PlayFromFrame(i % 4);
    //         this._targetLine.Add(fv);
    //     }
    //     for (int i = 0; i < numSegments; ++i)
    //     {
    //         Vector2 pos = start + ((i + 1 - offset) * SEG_SPACING * dir);
    //         if (!this._targetLine[i])
    //         {
    //             FancyVFX fv = FancyVFX.Spawn(_LineParticleVFX, pos, rotation: Lazy.RandomEulerZ());
    //             fv.GetComponent<tk2dSpriteAnimator>().PlayFromFrame(i % 4);
    //             this._targetLine[i] = fv;
    //         }
    //         tk2dBaseSprite sprite = this._targetLine[i].sprite;
    //         sprite.renderer.enabled = true;
    //         float alpha;
    //         if (i == 0)
    //             alpha = 1f - offset;
    //         else if (i == numSegments - 1)
    //             alpha = offset;
    //         else
    //             alpha = 1f;
    //         sprite.renderer.SetAlpha(alpha * (haveTarget ? 1f : 0.125f));
    //         sprite.transform.position = pos;
    //     }
    //     for (int i = numSegments; i < Mathf.Min(this._targetLine.Count, _MAX_STEPS); ++i)
    //         if (this._targetLine[i])
    //            this._targetLine[i].sprite.renderer.enabled = false;
    // }

    public override void Update()
    {
        base.Update();
        if (!this.PlayerOwner)
            return;
        if (!this.PlayerOwner.AcceptingNonMotionInput)
        {
            CleanUpVFX();
            return;
        }

        this._targetProjectile = GetTargetProjectile();
        DetermineTargetEnemyIfNecessary();
        UpdateTargetingVFXIfNecessary();
        AdjustBarrelOffsets();
        if (this.gun.m_isCurrentlyFiring)
            Lazy.PlaySoundUntilDeathOrTimeout("maestro_fire_sound_looped", base.gameObject, 0.05f);
    }

    private void AdjustBarrelOffsets()
    {
        tk2dSpriteAnimator anim = gun.spriteAnimator;
        if (anim.IsPlaying(gun.shootAnimation))
            gun.barrelOffset.localPosition = _ShootBarrelOffsets[anim.CurrentFrame];
        else
            gun.barrelOffset.localPosition = _ShootBarrelOffsets[0];
        if (gun.sprite.FlipY)
            gun.barrelOffset.localPosition = gun.barrelOffset.localPosition.WithY(-gun.barrelOffset.localPosition.y);
    }

    private static bool IsUntargetable(AIActor enemy)
    {
        return !enemy || !enemy.healthHaver || enemy.healthHaver.IsDead || enemy.IsGone || enemy.IsStealthed || !enemy.IsWorthShootingAt;
    }

    private void DetermineTargetEnemyIfNecessary()
    {
        if (IsUntargetable(this._targetEnemy))
            this._targetEnemy = SwitchTargetEnemy();
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        DetermineTargetEnemyIfNecessary();
        if (this._targetProjectile)
            RedirectProjectile(this._targetProjectile, this._targetEnemy, projectile.baseData.damage);
        projectile.DieInAir(suppressInAirEffects: true, allowActorSpawns: false, allowProjectileSpawns: false, killedEarly: false);
    }

    private void CleanUpVFX(bool destroyed = false)
    {
        if (this._enemyTargetVFX)
            UnityEngine.Object.Destroy(this._enemyTargetVFX);
        if (this._projectileTargetVFX)
            UnityEngine.Object.Destroy(this._projectileTargetVFX);
        // for (int i = 0; i < Mathf.Min(this._targetLine.Count, _MAX_STEPS); ++i)
        // {
        //     if (!this._targetLine[i])
        //         continue;
        //     if (destroyed)
        //         UnityEngine.Object.Destroy(this._targetLine[i]);
        //     else
        //         this._targetLine[i].sprite.renderer.enabled = false;
        // }
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
