namespace CwaffingTheGungy;

public class Blamethrower : AdvancedGunBehavior
{
    public static string ItemName         = "Blamethrower";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<Blamethrower>(ItemName, ProjectileName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.B, gunClass: GunClass.CHARM, reloadTime: 1.2f, ammo: 400);
            // gun.SetAnimationFPS(gun.shootAnimation, 30);
            // gun.SetAnimationFPS(gun.reloadAnimation, 40);
            // gun.SetMuzzleVFX(Items.Mailbox); // innocuous muzzle flash effects
            // gun.SetFireAudio("blowgun_fire_sound");
            // gun.SetReloadAudio("blowgun_reload_sound");

        gun.InitProjectile(new(clipSize: 1, cooldown: 0.1f, shootStyle: ShootStyle.SemiAutomatic, customClip: true, damage: 0f, angleVariance: 18f,
          sprite: "blamethrower_projectile", fps: 12, anchor: Anchor.MiddleLeft)).Attach<BlamethrowerProjectile>();
    }
}

public class BlamethrowerProjectile : MonoBehaviour
{
    private const float _FEAR_CHANCE = 0.1f;

    private void Start()
    {
        if (UnityEngine.Random.value > _FEAR_CHANCE)
            return;

        Projectile proj = base.GetComponent<Projectile>();
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

    private class EnemyBlamedBehavior : MonoBehaviour
    {
        internal static readonly List<string> _BlameQuotes = new(){
            "It's my fault!",
            "I messed up!",
            "I did it!",
            "Won't happen again!",
            "I'm sorry!",
            "My apologies!",
            "Please excuse me!",
            "Don't tell my parents!",
            "Don't tell my boss!",
            "I confess!",
        };

        public void Setup(Projectile p)
        {
            AIActor enemy = base.GetComponent<AIActor>();
            if ((enemy?.healthHaver?.currentHealth ?? 0) <= 0)
                return;

            if (TextBoxManager.HasTextBox(enemy.sprite.transform))
                TextBoxManager.ClearTextBox(enemy.sprite.transform);
            TextBoxManager.ShowTextBox(
                worldPosition    : enemy.sprite.WorldTopCenter,
                parent           : enemy.sprite.transform,
                duration         : 0f,
                text             : _BlameQuotes.ChooseRandom(),
                audioTag         : "",
                instant          : false,
                showContinueText : true
                );
            enemy.StartCoroutine(DoFearEffect(enemy, p.Owner as PlayerController));
        }

        public static IEnumerator DoFearEffect(AIActor enemy, PlayerController player)
        {
            if (enemy.behaviorSpeculator is not BehaviorSpeculator bs)
                yield break;
            bs.FleePlayerData = new(){
                Player        = player,
                StartDistance = 0f,
                StopDistance  = 0f,
            };
            //TODO: potentially unnecessary if FleePlayerData already handles it
            var o = enemy.PlayEffectOnActor(ResourceCache.Acquire("Global VFX/VFX_Fear") as GameObject, new Vector3(0,1f,0));
            yield return new WaitForSeconds(1.25f);
            UnityEngine.Object.Destroy(o);
        }
    }
}
