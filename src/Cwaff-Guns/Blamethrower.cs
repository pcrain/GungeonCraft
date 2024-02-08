namespace CwaffingTheGungy;

public class Blamethrower : AdvancedGunBehavior
{
    public static string ItemName         = "Blamethrower";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    internal static GameObject _BlameImpact = null;
    internal static GameObject _BlameTrail = null;

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<Blamethrower>(ItemName, ProjectileName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.B, gunClass: GunClass.CHARM, reloadTime: 1.2f, ammo: 750, doesScreenShake: false);
            gun.SetAnimationFPS(gun.shootAnimation, 30);
            gun.SetMuzzleVFX("blamethrower_trail", fps: 30);
            // gun.SetFireAudio("blowgun_fire_sound");

        Projectile proj = gun.InitProjectile(new(clipSize: -1, cooldown: 0.08f, shootStyle: ShootStyle.Automatic,
          damage: 2f, angleVariance: 30f, speed: 17f, range: 17f,
          sprite: "blamethrower_projectile", scale: 2.0f, fps: 10, anchor: Anchor.MiddleCenter, shouldRotate: false)
        ).Attach<BlameDamage>(
        ).Attach<BlamethrowerProjectile>();

        _BlameImpact = VFX.Create("blamethrower_projectile", fps: 1, loops: false, anchor: Anchor.MiddleCenter);
        _BlameTrail = VFX.Create("blamethrower_trail", fps: 16, loops: true, anchor: Anchor.MiddleCenter);
    }

    private class BlameDamage : DamageAdjuster
    {
        private const float _BLAME_MULT = 4f;

        protected override float AdjustDamage(float currentDamage, Projectile proj, AIActor enemy)
          => currentDamage * (enemy.GetComponent<EnemyBlamedBehavior>() ? _BLAME_MULT : 1f);
    }
}

public class BlamethrowerProjectile : MonoBehaviour
{
    private const float _FEAR_CHANCE = 0.125f;
    private const float _VFX_RATE = 0.1f;

    private float _vfxTimer = 0.0f;
    private Projectile _projectile = null;

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._projectile.PickFrame();
        AkSoundEngine.PostEvent($"blame_sound_{UnityEngine.Random.Range(1, 6)}", base.gameObject);
        this._projectile.OnHitEnemy += this.OnHitEnemy;
    }

    private void OnHitEnemy(Projectile p, SpeculativeRigidbody enemy, bool killed)
    {
        AkSoundEngine.PostEvent("blamethrower_impact_sound", base.gameObject);

        FancyVFX.SpawnBurst(prefab: Blamethrower._BlameImpact, numToSpawn: 2, basePosition: enemy.sprite.WorldCenter,
            positionVariance: 1f, baseVelocity: 10f * Vector2.up, velocityVariance: 5f, velType: FancyVFX.Vel.Radial,
            lifetime: 0.5f, fadeOutTime: 0.5f, randomFrame: true);

        if (UnityEngine.Random.value > _FEAR_CHANCE)
            return;
        if (!(enemy.aiActor?.IsHostileAndNotABoss() ?? false))
            return;
        if (enemy.aiActor.gameObject.GetComponent<EnemyBlamedBehavior>())
            return;
        enemy.aiActor.gameObject.AddComponent<EnemyBlamedBehavior>().Setup(p);
    }

    private void Update()
    {
        if ((this._vfxTimer += BraveTime.DeltaTime) < _VFX_RATE)
            return;
        this._vfxTimer -= 0.1f;
        FancyVFX.Spawn(Blamethrower._BlameTrail, this._projectile.sprite.WorldCenter.ToVector3ZisY(-1f), velocity: 0.2f * this._projectile.LastVelocity,
            rotation: this._projectile.LastVelocity.EulerZ(), lifetime: 0.18f, fadeOutTime: 0.15f);
    }
}

internal class EnemyBlamedBehavior : MonoBehaviour
{
    internal static readonly List<string> _BlameQuotes = new(){
        "It's my fault!",
        "I messed up!",
        "It was me, I did it!",
        "Won't happen again!",
        "I'm sorry!",
        "I'm so sorry!",
        "My apologies!",
        "Please excuse me!",
        // "Don't tell my parents!",
        "Don't tell my boss!",
        "I confess!",
        "I have no excuse!",
        "That's my bad!",
        "I'm in so much trouble!",
        "My mistake!",
        "What have I done?!",
        // "That's on me!",
        "I slipped up!",
    };

    public void Setup(Projectile p)
    {
        AIActor enemy = base.GetComponent<AIActor>();
        if ((enemy?.healthHaver?.currentHealth ?? 0) <= 0)
            return;
        if (p.Owner is not PlayerController player)
            return;

        if (TextBoxManager.HasTextBox(enemy.sprite.transform))
            TextBoxManager.ClearTextBox(enemy.sprite.transform);
        TextBoxManager.ShowTextBox(
            worldPosition    : enemy.sprite.WorldTopCenter,
            parent           : enemy.sprite.transform,
            duration         : 2f,
            text             : _BlameQuotes.ChooseRandom(),
            audioTag         : "",
            instant          : false,
            showContinueText : true
            );
        if (enemy.behaviorSpeculator is BehaviorSpeculator bs)
            bs.FleePlayerData = new(){
                Player        = player,
                StartDistance = 100f,
                StopDistance  = 100f,
            };
    }
}
