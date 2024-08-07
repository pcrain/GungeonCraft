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

        _OutbreakSmokeVFX = VFX.Create("outbreak_smoke_small", 2, loops: true, anchor: Anchor.MiddleCenter);
        _OutbreakSmokeLargeVFX = VFX.Create("outbreak_smoke_large", 2, loops: true, anchor: Anchor.MiddleCenter);
    }

    public override void OnPostFired(PlayerController player, Gun gun)
    {
        base.OnPostFired(player, gun);
        if (gun.GetComponent<Outbreak>() is not Outbreak outbreak)
            return;

        bool found = false;
        Vector2 target = Vector2.zero;
        if (_INFECT_TOWARDS_CURSOR)
            target = player.CenterPosition.ToNearestWallOrEnemyOrObject(player.m_currentGunAngle, 1f);

        foreach (AIActor enemy in StaticReferenceManager.AllEnemies) //REFACTOR: limit this to current room
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
            Projectile p = VolleyUtility.ShootSingleProjectile(
                _InfectionProjectile, enemy.CenterPosition /*enemy.GunPivot.PositionVector2()*/, infectAngle, false, player);
            p.specRigidbody.RegisterSpecificCollisionException(enemy.specRigidbody);
        }
        if (found)
            player.gameObject.Play("outbreak_spread_sound");
    }
}

public class InfectionBehavior : MonoBehaviour
{
    // private const int _STUN_DELAY = 10;
    // private const int _STUN_TIME  = 3600; // one hour

    private Projectile _projectile;

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._projectile.OnHitEnemy += (Projectile _, SpeculativeRigidbody enemy, bool _) => {
            if (enemy.aiActor && enemy.aiActor.IsHostileAndNotABoss())
                enemy.aiActor.gameObject.GetOrAddComponent<InfectedBehavior>();
        };
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
