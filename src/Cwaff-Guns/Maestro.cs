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

    private int        _targetEnemyIndex    = 0;
    private AIActor    _targetEnemy         = null;
    private Projectile _targetProjectile    = null;
    private GameObject _enemyTargetVFX      = null;
    private GameObject _projectileTargetVFX = null;

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<Maestro>(ItemName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.D, gunClass: GunClass.CHARM, reloadTime: 1.0f, ammo: 500, shootFps: 2);

        gun.InitProjectile(GunData.New(clipSize: -1, cooldown: 0.16f, angleVariance: 15.0f,
          shootStyle: ShootStyle.Automatic, damage: 7.5f, speed: 60.0f,
          sprite: "maestro_bullet", fps: 12, scale: 0.5f, anchor: Anchor.MiddleCenter));
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
        // p.GetOrAddComponent<CapturedMaestroProjectile>();

        p.FreezeAndLaunchWithDelay(0.1f, REFLECT_SPEED, sound: "knife_gun_launch");
    }

    private Projectile GetTargetProjectile()
    {
        if (this.PlayerOwner is not PlayerController pc)
            return null;
        Projectile target = null;
        float closest = float.MaxValue;

        if (pc.IsKeyboardAndMouse())
        {
            Vector2 mousePos = pc.unadjustedAimPoint.XY();
            closest = _MAX_PROJECTILE_TARGET_RADIUS * _MAX_PROJECTILE_TARGET_RADIUS;
            foreach (Projectile p in StaticReferenceManager.AllProjectiles)
            {
                if (!p || !p.isActiveAndEnabled || p.Owner is PlayerController)
                    continue;
                float sqrMag = (p.SafeCenter - mousePos).sqrMagnitude;
                if (sqrMag > closest)
                    continue;
                target = p;
                closest = sqrMag;
            }
            return target;
        }

        Vector2 aimVec = pc.m_activeActions.Aim.Vector;
        if (aimVec == Vector2.zero)
            return null;

        Vector2 gunPos = this.gun.barrelOffset.PositionVector2();
        float aimAngle = aimVec.ToAngle();
        closest = _MAX_PROJECTILE_TARGET_ANGLE; // since we're using angle here as a measure of closeness, cap it out at _MAX_PROJECTILE_TARGET_ANGLE
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

    private void UpdateTargetingVFXIfNecessary()
    {
        if (this._enemyTargetVFX == null)
        {
            this._enemyTargetVFX = SpawnManager.SpawnVFX(KingsLaw._RuneMuzzle, this.gun.barrelOffset.transform.position, Quaternion.identity);
            this._enemyTargetVFX.SetAlphaImmediate(0.5f);
        }
        if (this._projectileTargetVFX == null)
        {
            this._projectileTargetVFX = SpawnManager.SpawnVFX(KingsLaw._RuneMuzzle, this.gun.barrelOffset.transform.position, Quaternion.identity);
            this._projectileTargetVFX.SetAlphaImmediate(0.5f);
        }

        this._projectileTargetVFX.transform.localRotation = (-KingsLaw._RUNE_ROT_MID * BraveTime.ScaledTimeSinceStartup).EulerZ();

        if (this._targetEnemy)
        {
            this._enemyTargetVFX.transform.localRotation = (-KingsLaw._RUNE_ROT_MID * BraveTime.ScaledTimeSinceStartup).EulerZ();
            this._enemyTargetVFX.transform.position = this._targetEnemy.CenterPosition;
            this._enemyTargetVFX.SetAlpha(0.5f);
        }
        else
            this._enemyTargetVFX.SetAlpha(0.0f);

        if (this._targetProjectile)
        {
            this._projectileTargetVFX.transform.localRotation = (-KingsLaw._RUNE_ROT_MID * BraveTime.ScaledTimeSinceStartup).EulerZ();
            this._projectileTargetVFX.transform.position = this._targetProjectile.SafeCenter;
            this._projectileTargetVFX.SetAlpha(0.5f);
        }
        else
            this._projectileTargetVFX.SetAlpha(0.0f);
    }

    public override void Update()
    {
        base.Update();
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

    private void CleanUpVFX()
    {
        if (this._enemyTargetVFX)
            UnityEngine.Object.Destroy(this._enemyTargetVFX);
        if (this._projectileTargetVFX)
            UnityEngine.Object.Destroy(this._projectileTargetVFX);
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
        CleanUpVFX();
        base.OnDestroy();
    }
}

public class CapturedMaestroProjectile : MonoBehaviour
{
    private Projectile _projectile  = null;
    private PlayerController _owner = null;
    private Shader _oldShader = null;

    public void Setup()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._owner = this._projectile.Owner as PlayerController;
        this._projectile.OnDestruction += this.OnDestruction;

        if (this._projectile.sprite)
        {
            this._oldShader = this._projectile.sprite.renderer.material.shader;
            this._projectile.sprite.MakeHolographic(green: true);
        }
    }

    public void OnDestruction(Projectile p)
    {
        this._projectile.OnDestruction -= this.OnDestruction;
        Teardown();
    }

    public void Teardown()
    {
        if (this._oldShader && this._projectile && this._projectile.sprite)
            this._projectile.sprite.renderer.material.shader = this._oldShader;
        UnityEngine.Object.Destroy(this);
    }
}
