
namespace CwaffingTheGungy;

public class Outbreak : CwaffGun
{
    public static string ItemName         = "Outbreak";
    public static string ShortDescription = "Going Viral";
    public static string LongDescription  = "Fires a parasitic projectile that infects enemies on contact. All infected enemies fire additional parasitic projectiles towards the player's target whenever this gun is fired.";
    public static string Lore             = "For years, Gungineers have tried to develop synthetic self-replicating projectiles in the lab, with the closest they've gotten being the discovery that glass beakers shatter when you throw them against the wall. As a last ditch effort after research funding inevitably ran dry, one Gungineer decided to stuff live parasites of unknown origin into a casing and fire it at a target dummy. To everyone's surprise, a new projectile emerged right back from the puncture area. After a few generations of design tweaks and genetic mutations, the {ItemName} emerged in its current form.";

    internal static GameObject _OutbreakSmokeVFX = null;
    internal static GameObject _OutbreakSmokeLargeVFX = null;
    internal static Projectile _InfectionProjectile = null;

    internal readonly bool _INFECT_TOWARDS_CURSOR = true;

    public static void Init()
    {
        Lazy.SetupGun<Outbreak>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.B, gunClass: GunClass.PISTOL, reloadTime: 1.2f, ammo: 300, shootFps: 24, reloadFps: 20,
            muzzleVFX: "muzzle_outbreak", muzzleFps: 40, muzzleScale: 0.3f, muzzleAnchor: Anchor.MiddleCenter,
            fireAudio: "outbreak_shoot_sound", reloadAudio: "outbreak_reload_sound")
          .AddToShop(ItemBuilder.ShopType.Cursula)
          .InitProjectile(GunData.New(clipSize: 10, cooldown: 0.2f, shootStyle: ShootStyle.SemiAutomatic, customClip: true,
            damage: 8.0f, speed: 17.0f, range: 100.0f, sprite: "outbreak_projectile", fps: 12, anchor: Anchor.MiddleLeft))
          .Attach<InfectionBehavior>()
          .Assign(out _InfectionProjectile);

        _OutbreakSmokeVFX = VFX.Create("outbreak_smoke_small");
        _OutbreakSmokeLargeVFX = VFX.Create("outbreak_smoke_large");
    }

    public override void OnPostFired(PlayerController player, Gun gun)
    {
        base.OnPostFired(player, gun);
        if (player.CurrentRoom is not RoomHandler room)
            return;
        if (gun.GetComponent<Outbreak>() is not Outbreak outbreak)
            return;

        bool found = false;
        Vector2 target = Vector2.zero;
        if (_INFECT_TOWARDS_CURSOR)
            target = player.CenterPosition.ToNearestWallOrEnemyOrObject(player.m_currentGunAngle, 1f);

        foreach (AIActor enemy in room.SafeGetEnemiesInRoom())
        {
            if (enemy.GetComponent<InfectedBehavior>() is not InfectedBehavior infection)
                continue;
            found = true;
            float infectAngle = player.m_currentGunAngle;
            if (_INFECT_TOWARDS_CURSOR)
            {
                Vector2 delta = target - enemy.CenterPosition;
                if (delta.sqrMagnitude > 1f) // prevents random angles from enemies targeting themselves
                    infectAngle = delta.ToAngle();
            }
            Projectile p = VolleyUtility.ShootSingleProjectile(_InfectionProjectile, enemy.CenterPosition, infectAngle, false, player);
            if (this.Mastered)
                p.gameObject.GetOrAddComponent<OutbreakHomingModifier>();
            p.SetOwnerAndStats(player);
            player.DoPostProcessProjectile(p);
            p.specRigidbody.RegisterSpecificCollisionException(enemy.specRigidbody);
        }
        if (found)
            player.gameObject.Play("outbreak_spread_sound");
    }
}

public class InfectionBehavior : MonoBehaviour
{
    private Projectile _projectile;

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._projectile.OnHitEnemy += this.OnHitEnemy;
        if (this._projectile.Owner is PlayerController player && player.HasSynergy(Synergy.MASTERY_OUTBREAK))
            this._projectile.gameObject.GetOrAddComponent<OutbreakHomingModifier>();
    }

    private void OnHitEnemy(Projectile proj, SpeculativeRigidbody enemy, bool killed)
    {
        if (enemy.aiActor && enemy.aiActor.IsHostileAndNotABoss())
            enemy.aiActor.gameObject.GetOrAddComponent<InfectedBehavior>();
    }

    private void Update()
    {
        if (UnityEngine.Random.value > 0.3f)
            return;

        CwaffVFX.Spawn(Outbreak._OutbreakSmokeVFX, this._projectile.SafeCenter.ToVector3ZisY(-1f), Lazy.RandomEulerZ(),
            velocity: Lazy.RandomVector(0.1f), lifetime: 0.3f, fadeOutTime: 0.6f);
    }
}

public class InfectedBehavior : MonoBehaviour
{
    private AIActor _enemy = null;

    private void Start()
    {
        this._enemy = base.GetComponent<AIActor>();
        if (!this._enemy || !this._enemy.healthHaver || this._enemy.healthHaver.currentHealth <= 0)
            return;

        this._enemy.gameObject.Play("outbreak_infect_sound");
    }

    private void Update()
    {
        if (UnityEngine.Random.value > 0.02f)
            return;

        CwaffVFX.Spawn(Outbreak._OutbreakSmokeVFX, this._enemy.sprite.WorldTopCenter.ToVector3ZisY(-1f), Lazy.RandomEulerZ(),
            velocity: Lazy.RandomVector(0.5f), lifetime: 0.3f, fadeOutTime: 0.6f);
    }
}

// modified from HomingModifier with slight optimizations + blacklisting our origin enemy
public class OutbreakHomingModifier : BraveBehaviour
{
    public float HomingRadius         = 10f;
    public float AngularVelocity      = 1080f;
    public AIActor originEnemy        = null;
    protected Projectile m_projectile = null;

    private void Start()
    {
        if (!this.m_projectile)
            this.m_projectile = GetComponent<Projectile>();
        this.m_projectile.ModifyVelocity += this.ModifyVelocity;
    }

    public override void OnDestroy()
    {
        if (this.m_projectile)
            this.m_projectile.ModifyVelocity -= this.ModifyVelocity;
        base.OnDestroy();
    }

    private Vector2 ModifyVelocity(Vector2 inVel)
    {
        Vector2 newVel = inVel;
        RoomHandler absoluteRoomFromPosition = GameManager.Instance.Dungeon.data.GetAbsoluteRoomFromPosition(m_projectile.LastPosition.IntXY(VectorConversions.Floor));
        List<AIActor> activeEnemies = absoluteRoomFromPosition.GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
        if (activeEnemies == null || activeEnemies.Count == 0)
            return inVel;

        float nearestSqrDist = HomingRadius * HomingRadius;
        Vector2 nearestDelta = Vector2.zero;
        AIActor nearestActor = null;
        Vector2 myPos = ((!base.sprite) ? base.transform.position.XY() : base.sprite.WorldCenter);
        for (int i = 0; i < activeEnemies.Count; i++)
        {
            AIActor enemy = activeEnemies[i];
            if (!enemy || !enemy.IsWorthShootingAt || enemy.IsGone || enemy == originEnemy)
                continue;
            Vector2 delta = enemy.CenterPosition - myPos;
            float sqrMagnitude = delta.sqrMagnitude;
            if (sqrMagnitude > nearestSqrDist)
                continue;
            nearestDelta   = delta;
            nearestSqrDist = sqrMagnitude;
            nearestActor   = enemy;
        }
        if (nearestActor == null)
            return inVel;

        float homeAmount = 1f - Mathf.Sqrt(nearestSqrDist) / HomingRadius;
        float curAngle = inVel.ToAngle();
        float maxDelta = AngularVelocity * homeAmount * m_projectile.LocalDeltaTime;
        float newAngle = Mathf.MoveTowardsAngle(curAngle, nearestDelta.ToAngle(), maxDelta);
        if (m_projectile.OverrideMotionModule != null)
            m_projectile.OverrideMotionModule.AdjustRightVector(newAngle - curAngle);
        if (m_projectile is HelixProjectile hp)
        {
            hp.AdjustRightVector(newAngle - curAngle);
            return inVel;
        }
        if (m_projectile.shouldRotate)
            base.transform.rotation = Quaternion.Euler(0f, 0f, newAngle);
        newVel = BraveMathCollege.DegreesToVector(newAngle, inVel.magnitude);
        if (newVel == Vector2.zero || float.IsNaN(newVel.x) || float.IsNaN(newVel.y))
            return inVel;
        return newVel;
    }
}
