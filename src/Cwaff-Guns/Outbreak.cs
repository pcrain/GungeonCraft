﻿namespace CwaffingTheGungy;

public class Outbreak : AdvancedGunBehavior
{
    public static string ItemName         = "Outbreak";
    public static string SpriteName       = "outbreak";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "Going Viral";
    public static string LongDescription  = "Fires a parasitic projectile that infects enemies on contact. All infected enemies fire additional parasitic projectiles towards the player's target whenever this gun is fired.";
    public static string Lore             = "For years, Gungineers have tried to develop synthetic self-replicating projectiles in the lab, with the closest they've gotten being the discovery that glass beakers shatter when you throw them against the wall. As a last ditch effort after research funding inevitably ran dry, one Gungineer decided to stuff live parasites of unknown origin into a casing and fire it at a target dummy. To everyone's surprise, a new projectile emerged right back from the puncture area. After a few generations of design tweaks and genetic mutations, the {ItemName} emerged in its current form.";

    internal static GameObject _OutbreakSmokeVFX = null;
    internal static GameObject _OutbreakSmokeLargeVFX = null;
    internal static Projectile _InfectionProjectile = null;

    internal readonly bool _INFECT_TOWARDS_CURSOR = true;

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<Outbreak>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.B, gunClass: GunClass.PISTOL, reloadTime: 1.2f, ammo: 300);
            gun.SetAnimationFPS(gun.shootAnimation, 24);
            gun.SetAnimationFPS(gun.reloadAnimation, 20);
            gun.SetMuzzleVFX("muzzle_outbreak", fps: 40, scale: 0.3f, anchor: Anchor.MiddleCenter);
            gun.SetFireAudio("outbreak_shoot_sound");
            gun.SetReloadAudio("outbreak_reload_sound");
            gun.AddToSubShop(ItemBuilder.ShopType.Cursula);

        _InfectionProjectile = gun.InitProjectile(new(clipSize: 10, cooldown: 0.2f, shootStyle: ShootStyle.SemiAutomatic, customClip: SpriteName,
          damage: 8.0f, speed: 17.0f, range: 100.0f, sprite: "outbreak_projectile", fps: 12, anchor: Anchor.MiddleLeft
          )).Attach<InfectionBehavior>();

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
            target = player.sprite.WorldCenter.ToNearestWallOrEnemyOrObject(player.m_currentGunAngle, 1f);

        foreach (AIActor enemy in StaticReferenceManager.AllEnemies)
        {
            if (enemy.GetComponent<InfectedBehavior>() is not InfectedBehavior infection)
                continue;
            found = true;
            float infectAngle = player.m_currentGunAngle;
            if (_INFECT_TOWARDS_CURSOR)
            {
                Vector2 delta = target - enemy.sprite.WorldCenter;
                if (delta.sqrMagnitude > 1f) // prevents random angles from enemies targeting themselves
                    infectAngle = delta.ToAngle();
            }
            Projectile p = VolleyUtility.ShootSingleProjectile(
                _InfectionProjectile, enemy.sprite.WorldCenter /*enemy.GunPivot.PositionVector2()*/, infectAngle, false, player);
            p.specRigidbody.RegisterSpecificCollisionException(enemy.specRigidbody);
        }
        if (found)
            AkSoundEngine.PostEvent("outbreak_spread_sound", player.gameObject);
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
            if (enemy.aiActor?.IsHostileAndNotABoss() ?? false)
                enemy.aiActor.gameObject.GetOrAddComponent<InfectedBehavior>();
        };
    }

    private void Update()
    {
        if (UnityEngine.Random.value > 0.3f)
            return;

        FancyVFX.Spawn(Outbreak._OutbreakSmokeVFX, this._projectile.sprite.WorldCenter.ToVector3ZisY(-1f), Lazy.RandomEulerZ(),
            velocity: Lazy.RandomVector(0.1f), lifetime: 0.3f, fadeOutTime: 0.6f);
    }
}

public class InfectedBehavior : MonoBehaviour
{
    private AIActor _enemy = null;

    private void Start()
    {
        this._enemy = base.GetComponent<AIActor>();
        if ((this._enemy?.healthHaver?.currentHealth ?? 0) <= 0)
            return;

        AkSoundEngine.PostEvent("outbreak_infect_sound", this._enemy.gameObject);
    }

    private void Update()
    {
        if (UnityEngine.Random.value > 0.02f)
            return;

        FancyVFX.Spawn(Outbreak._OutbreakSmokeVFX, this._enemy.sprite.WorldTopCenter.ToVector3ZisY(-1f), Lazy.RandomEulerZ(),
            velocity: Lazy.RandomVector(0.5f), lifetime: 0.3f, fadeOutTime: 0.6f);
    }
}
