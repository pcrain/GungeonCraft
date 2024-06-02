namespace CwaffingTheGungy;

public class Maestro : CwaffGun
{
    public static string ItemName         = "Maestro";
    public static string ShortDescription = "Guided by the Winds";
    public static string LongDescription  = "Redirects enemy projectiles near the cursor towards the targeted enemy while fire is held. Reloading changes the targeted enemy to the enemy closest to the cursor. On controller, projectiles and enemies are targeted using angle from aim instead of distance from the cursor. Redirected projectiles cannot harm the player.";
    public static string Lore             = "A conductor's baton that was lost at sea near Dragun's Roost long ago, eventually finding its way into the Gungeon via the sewers. Though unable to fire projectiles itself, it grants its wielder the ability to redirect projectiles by bending the ether around them, providing excellent offensive and defensive utility alike.";

    private const float _MAX_PROJECTILE_TARGET_ANGLE  = 20f; // for controller
    private const float _MAX_PROJECTILE_TARGET_RADIUS = 3f;  // for mouse
    private const float _SQR_TARGET_RADIUS            = _MAX_PROJECTILE_TARGET_RADIUS * _MAX_PROJECTILE_TARGET_RADIUS;
    private const int   _MAX_STEPS                    = 30;

    private int        _targetEnemyIndex    = 0;
    private AIActor    _targetEnemy         = null;
    private Projectile _targetProjectile    = null;

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<Maestro>(ItemName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.C, gunClass: GunClass.CHARM, reloadTime: 0.0f, ammo: 500, shootFps: 24,
                muzzleFrom: Items.FaceMelter, doesScreenShake: false, continuousFire: true, curse: 1f, dynamicBarrelOffsets: true);
            gun.AddToSubShop(ItemBuilder.ShopType.Cursula);

        gun.InitProjectile(GunData.New(clipSize: -1, cooldown: 0.2f, angleVariance: 15.0f,
          shootStyle: ShootStyle.Automatic, damage: 9f, speed: 60.0f, ammoType: GameUIAmmoType.AmmoType.BEAM,
          sprite: "maestro_bullet", fps: 12, scale: 0.5f, anchor: Anchor.MiddleCenter));
        gun.AddReticle<CwaffEnemyReticle>(reticleVFX : VFX.Create("maestro_target_enemy_vfx", fps: 2),
            reticleAlpha  : 0.5f, smoothLerp : true, rotateSpeed : 270f, visibility : CwaffReticle.Visibility.WITHTARGET);
        gun.AddReticle<CwaffProjectileReticle>(reticleVFX : VFX.Create("maestro_target_projectile_vfx", fps: 2),
            reticleAlpha : 0.75f, smoothLerp : true, rotateSpeed : 270f, visibility : CwaffReticle.Visibility.WITHTARGET);
    }

    private GameObject GetTargetEnemy(CwaffReticle reticle) => this._targetEnemy ? this._targetEnemy.gameObject : null;
    private GameObject GetTargetProjectile(CwaffReticle reticle) => this._targetProjectile ? this._targetProjectile.gameObject : null;

    public override void OnPlayerPickup(PlayerController player)
    {
        base.OnPlayerPickup(player);
        gun.GetComponent<CwaffEnemyReticle>().targetObjFunc = GetTargetEnemy;
        gun.GetComponent<CwaffProjectileReticle>().targetObjFunc = GetTargetProjectile;
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
                closenessWeight = distFromGun * angleFromAim; // weight using both angle and distance -> can still hit far away projectiles, but need accurate aim
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

    public override void Update()
    {
        base.Update();
        if (!this.PlayerOwner)
            return;
        if (!this.PlayerOwner.AcceptingNonMotionInput)
            return;

        this._targetProjectile = GetTargetProjectile();
        DetermineTargetEnemyIfNecessary();
        if (this.gun.m_isCurrentlyFiring)
            Lazy.PlaySoundUntilDeathOrTimeout("maestro_fire_sound_looped", base.gameObject, 0.05f);
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
}
