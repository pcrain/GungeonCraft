namespace CwaffingTheGungy;

public class Blamethrower : CwaffGun
{
    public static string ItemName         = "Blamethrower";
    public static string ShortDescription = "Take It Up with HR";
    public static string LongDescription  = "Fires harsh words that deal emotional damage and may inflict blame, making enemies run away and take 4x emotional damage. Taking damage from an enemy projectile assigns a random scapegoat, who becomes 100% susceptible to blame and drops an appropriate health / armor pickup when killed with the Blamethrower.";
    public static string Lore             = "Whoever claimed that actions speak louder than words has clearly never stared down the barrel of a military-grade bullhorn. As the saying goes, sticks and stones may break your bones, but words can get you grounded, expelled, fired, ostracized, defenestrated, imprisoned, executed, or flipped off depending on your specific life circumstances.";

    internal static GameObject _BlameImpact = null;
    internal static GameObject _BlameTrail = null;
    internal static GameObject _ScapeGoatVFX = null;

    public static void Init()
    {
        Gun gun = Lazy.SetupGun<Blamethrower>(ItemName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.A, gunClass: GunClass.CHARM, reloadTime: 0.0f, ammo: 300,
                doesScreenShake: false, canReloadNoMatterAmmo: true, shootFps: 40, muzzleVFX: "muzzle_blamethrower", muzzleFps: 30);

        Projectile proj = gun.InitProjectile(GunData.New(clipSize: -1, cooldown: 0.08f, shootStyle: ShootStyle.Automatic,
          damage: 2f, angleVariance: 30f, speed: 17f, range: 17f, customClip: true,
          sprite: "blamethrower_projectile", scale: 2.0f, fps: 10, anchor: Anchor.MiddleCenter, shouldRotate: false)
        ).Attach<BlameDamage>(
        ).Attach<BlamethrowerProjectile>();

        _BlameImpact = VFX.Create("blamethrower_projectile_vfx", fps: 1, loops: false, anchor: Anchor.MiddleCenter);
        _BlameTrail = VFX.Create("blamethrower_trail", fps: 16, loops: true, anchor: Anchor.MiddleCenter);
        _ScapeGoatVFX = VFX.Create("goat_vfx", fps: 16, loops: true, scale: 1f, anchor: Anchor.MiddleCenter);
    }

    public override void OnPlayerPickup(PlayerController player)
    {
        base.OnPlayerPickup(player);
        player.OnReceivedDamage += this.OnReceivedDamage;
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        player.OnReceivedDamage -= this.OnReceivedDamage;
        base.OnDroppedByPlayer(player);
    }

    public override void OnDestroy()
    {
        if (this.PlayerOwner)
            this.PlayerOwner.OnReceivedDamage -= this.OnReceivedDamage;
        base.OnDestroy();
    }

    private void OnReceivedDamage(PlayerController player)
    {
        if (player.CurrentRoom?.GetActiveEnemies(RoomHandler.ActiveEnemyType.All) is not List<AIActor> enemies)
            return;
        if (enemies.Count() == 0)
            return;

        const int TRIES = 10;
        for (int i = 0; i < TRIES; ++i)
        {
            AIActor enemy = enemies.ChooseRandom();
            if (!enemy || !enemy.IsHostileAndNotABoss())
                continue;
            if (enemy.GetComponent<EnemyBlamedBehavior>())
                continue;  // can't scapegoat the same enemy twice
            enemy.gameObject?.GetOrAddComponent<ScapeGoat>();
            break;
        }
    }

    private class BlameDamage : DamageAdjuster
    {
        private const float _BLAME_MULT = 4f;

        protected override float AdjustDamage(float currentDamage, Projectile proj, AIActor enemy)
          => currentDamage * (enemy.GetComponent<EnemyBlamedBehavior>() ? _BLAME_MULT : 1f);
    }
}

internal class ScapeGoat : MonoBehaviour
{
    private bool _active = true;

    private IEnumerator Start()
    {
        // make sure our enemy is alive
        if (base.GetComponent<AIActor>() is not AIActor enemy)
            yield break;
        if (base.GetComponent<HealthHaver>() is not HealthHaver hh)
            yield break;
        if (base.GetComponent<EnemyBlamedBehavior>())
            yield break;  // can't scapegoat the same enemy twice

        // spawn scapegoat VFX above their head
        GameObject vfx = SpawnManager.SpawnVFX(Blamethrower._ScapeGoatVFX, enemy.sprite.WorldTopCenter, Quaternion.identity);
        tk2dSprite sprite = vfx.GetComponent<tk2dSprite>();

        // destroy the VFX once the enemy is dead
        while (this._active && enemy && hh && hh.IsAlive)
        {
            sprite.PlaceAtPositionByAnchor(enemy.sprite.WorldTopCenter.HoverAt(
                amplitude: 0.1f, frequency: 10f, offset: 0.75f), Anchor.MiddleCenter);
            yield return null;
        }

        vfx.SafeDestroy();
        this.SafeDestroy();
    }

    public void TakeTheBlame(PlayerController player)
    {
        AIActor enemy = base.GetComponent<AIActor>();
        Items item = player.ForceZeroHealthState ? Items.Armor : (enemy.IsBlackPhantom ? Items.Heart : Items.HalfHeart);
        LootEngine.SpawnItem(item: ItemHelper.Get(item).gameObject, spawnPosition: enemy.CenterPosition,
            spawnDirection: Vector2.zero, force: 0f, doDefaultItemPoof: true);
        this._active = false;
    }
}

public class BlamethrowerProjectile : MonoBehaviour
{
    private const float _FEAR_CHANCE = 0.125f;
    private const float _VFX_RATE    = 0.1f;

    private float _vfxTimer = 0.0f;
    private Projectile _projectile = null;

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._projectile.PickFrame();
        base.gameObject.Play($"blame_sound_{UnityEngine.Random.Range(1, 6)}");
        this._projectile.OnHitEnemy += this.OnHitEnemy;
    }

    private void OnHitEnemy(Projectile p, SpeculativeRigidbody enemy, bool killed)
    {
        base.gameObject.Play("blamethrower_impact_sound");

        FancyVFX.SpawnBurst(prefab: Blamethrower._BlameImpact, numToSpawn: 2, basePosition: enemy.sprite.WorldCenter,
            positionVariance: 1f, baseVelocity: 10f * Vector2.up, velocityVariance: 5f, velType: FancyVFX.Vel.Radial,
            lifetime: 0.5f, fadeOutTime: 0.5f, randomFrame: true);

        if (p.Owner is not PlayerController player)
            return;

        // if we successfully blame the scapegoat, return some armor
        ScapeGoat scapeGoat = enemy.GetComponent<ScapeGoat>();
        if (killed && scapeGoat)
            scapeGoat.TakeTheBlame(player);

        if ((UnityEngine.Random.value > _FEAR_CHANCE) && !scapeGoat)
            return;  // if we fail the fear check and we're not already a designated scapegoat, we're done
        if (!(enemy.aiActor?.IsHostileAndNotABoss() ?? false))
            return;
        if (enemy.GetComponent<EnemyBlamedBehavior>())
            return;
        enemy.gameObject.AddComponent<EnemyBlamedBehavior>().Setup(p);
    }

    private void Update()
    {
        if ((this._vfxTimer += BraveTime.DeltaTime) < _VFX_RATE)
            return;
        this._vfxTimer -= _VFX_RATE;
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
