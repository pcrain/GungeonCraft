namespace CwaffingTheGungy;

public class Blamethrower : AdvancedGunBehavior
{
    public static string ItemName         = "Blamethrower";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    private const float _BLAME_MULT = 4f;

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<Blamethrower>(ItemName, ProjectileName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.B, gunClass: GunClass.CHARM, reloadTime: 1.2f, ammo: 400, doesScreenShake: false);
            // gun.SetAnimationFPS(gun.shootAnimation, 30);
            // gun.SetAnimationFPS(gun.reloadAnimation, 40);
            // gun.SetMuzzleVFX(Items.Mailbox); // innocuous muzzle flash effects
            // gun.SetFireAudio("blowgun_fire_sound");
            // gun.SetReloadAudio("blowgun_reload_sound");

        gun.InitProjectile(new(clipSize: -1, cooldown: 0.08f, shootStyle: ShootStyle.Automatic,
          damage: 2f, angleVariance: 30f, speed: 17f, range: 17f,
          sprite: "blamethrower_projectile", scale: 2.0f, fps: 10, anchor: Anchor.MiddleCenter, shouldRotate: false)
        ).Attach<BlameDamage>(
        ).Attach<BlamethrowerProjectile>();
    }

    private class BlameDamage : DamageAdjuster
    {
        protected override float AdjustDamage(float currentDamage, Projectile proj, AIActor enemy)
        {
            if (enemy.GetComponent<EnemyBlamedBehavior>())
                return currentDamage * _BLAME_MULT;
            return currentDamage;
        }
    }
}

public class BlamethrowerProjectile : MonoBehaviour
{
    private const float _FEAR_CHANCE = 0.125f;

    private void Start()
    {
        Projectile proj = base.GetComponent<Projectile>();
        proj.spriteAnimator.PlayFromFrame(
          UnityEngine.Random.Range(0, proj.spriteAnimator.CurrentClip.frames.Count() - 1));
        proj.spriteAnimator.Stop(); // stop animating immediately after creation so we can stick with our initial sprite
        AkSoundEngine.PostEvent($"blame_sound_{UnityEngine.Random.Range(1, 6)}", base.gameObject);

        if (UnityEngine.Random.value > _FEAR_CHANCE)
            return;

        proj.OnHitEnemy += this.OnHitEnemy;
    }

    private void OnHitEnemy(Projectile p, SpeculativeRigidbody enemy, bool killed)
    {
        if (!(enemy.aiActor?.IsHostileAndNotABoss() ?? false))
            return;
        if (enemy.aiActor.gameObject.GetComponent<EnemyBlamedBehavior>())
            return;
        enemy.aiActor.gameObject.AddComponent<EnemyBlamedBehavior>().Setup(p);
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
        "My apologies!",
        "Please excuse me!",
        "Don't tell my parents!",
        "Don't tell my boss!",
        "I confess!",
        "I have no excuse!",
        "That's my bad!",
        "I'm in so much trouble!",
        "My mistake!",
        "What have I done?!",
        "That's on me!",
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
        enemy.StartCoroutine(DoFearEffect(enemy, player));
    }

    public static IEnumerator DoFearEffect(AIActor enemy, PlayerController player)
    {
        if (enemy.behaviorSpeculator is not BehaviorSpeculator bs)
            yield break;
        bs.FleePlayerData = new(){
            Player        = player,
            StartDistance = 100f,
            StopDistance  = 100f,
        };
    }
}
