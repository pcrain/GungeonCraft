namespace CwaffingTheGungy;

public class PlatinumStar : CwaffGun
{
    public static string ItemName         = "Platinum Star";
    public static string ShortDescription = "Unbreakable";
    public static string LongDescription  = "Fires projectiles that deal no damage on initial impact. When reloaded, summons manifestations of the user's soul to attack all previously-shot enemies with a flurry of rapid punches.";
    public static string Lore             = "This gun had a large golden arrow pierced through its barrel and grip when it was originally discovered by Ox and Cadence lying in a patch of flowers behind the Gungeon. The arrow had mysteriously vanished by the time they had gotten back to the Breach to test out the gun's capabilities, which by all accounts seemed to be nothing more than rapid-firing some harmless phantom projectiles. Having deemed the gun worthless in combat, Cadence tossed it back outside where she found it, only for it to vanish before hitting the ground. Somehow, it has found its way into the Gungeon on its own.";

    internal static Projectile _OraBullet;

    public static void Init()
    {
        Lazy.SetupGun<PlatinumStar>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.A, gunClass: GunClass.PISTOL, reloadTime: 1f, ammo: 480, shootFps: 20, reloadFps: 40,
            muzzleVFX: "muzzle_platinum_star", muzzleFps: 60, muzzleScale: 0.25f, muzzleAnchor: Anchor.MiddleCenter, fireAudio: "platinum_fire_sound")
          .SetReloadAudio("platinum_reload_sound", frame: 5)
          .AddToSubShop(ModdedShopType.TimeTrader)
          .InitProjectile(GunData.New(clipSize: 28, cooldown: 0.125f, angleVariance: 15.0f, shootStyle: ShootStyle.Automatic, customClip: true,
            damage: 3.0f, speed: 50.0f, force: 1.0f, range: 50.0f, sprite: "platinum_star_projectile", fps: 12, anchor: Anchor.MiddleLeft, spawnSound: "tomislav_shoot"))
          .Attach<PlatinumProjectile>();

        _OraBullet = Items.Polaris.CloneProjectile(GunData.New(damage: 1.0f, speed: 75.0f, force: 0.1f, range: 3.0f, shouldRotate: true))
          .AddAnimations(AnimatedBullet.Create(name: "ora_fist_fast", fps: 12, scale: 0.33f, anchor: Anchor.MiddleRight))
          .Attach<PierceProjModifier>(pierce => pierce.penetration = 999)
          .Attach<ImmuneToTimestop>();
    }

    // public override void OnPostFired(PlayerController player, Gun gun)
    // {
    //     base.OnPostFired(player, gun);
    //     gun.gameObject.Play("tomislav_shoot");
    //     // Material m = this.gun.gameObject.GetOrAddShader(Shader.Find("Brave/ItemSpecific/LootGlintAdditivePass"));
    //     // m.SetColor("_OverrideColor", Color.yellow);
    //     // m.SetFloat("_Period", 1.0f);
    //     // m.SetFloat("_PixelWidth", 5.0f);
    //     // Material m3 = this.Owner.sprite.gameObject.GetOrAddShader(Shader.Find("Brave/ItemSpecific/LootGlintAdditivePass"));
    //     // m3.SetColor("_OverrideColor", Color.yellow);
    //     // m3.SetFloat("_Period", 1.0f);
    //     // m3.SetFloat("_PixelWidth", 5.0f);
    // }

    public override void OnReloadPressed(PlayerController player, Gun gun, bool manual)
    {
        base.OnReloadPressed(player, gun, manual);
        LaunchAllBullets(this.PlayerOwner);
        if (gun.IsReloading)
            gun.muzzleFlashEffects.DestroyAll(); // since we're preventing gun rotation on reload, the muzzle vfx look weird, so just disable them
    }

    public override void OnSwitchedToThisGun()
    {
        base.OnSwitchedToThisGun();
        LaunchAllBullets(this.PlayerOwner);
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        base.OnDroppedByPlayer(player);
        LaunchAllBullets(this.PlayerOwner);
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        base.OnSwitchedAwayFromThisGun();
        LaunchAllBullets(this.PlayerOwner);
    }

    private void LaunchAllBullets(PlayerController pc)
    {
        foreach (AIActor enemy in StaticReferenceManager.AllEnemies)
            if (enemy && enemy.GetComponent<OraOra>() is OraOra oraora) oraora.OraOraOra(pc);
    }

    public override void Update()
    {
        base.Update();
        this.gun.preventRotation = (this.gun.spriteAnimator.currentClip.name == this.gun.reloadAnimation);
    }
}

public class ImmuneToTimestop : MonoBehaviour {}

public class PlatinumProjectile : MonoBehaviour
{
    private Projectile _projectile;
    private PlayerController _owner;
    private float _bankedDamage = 0f;
    private float _angle;

    private void Start()
    {
        this._projectile                 = base.GetComponent<Projectile>();
        this._owner                      = this._projectile.Owner as PlayerController;
        this._angle                      = this._projectile.Direction.ToAngle();
        this._bankedDamage               = this._projectile.baseData.damage;
        this._projectile.baseData.damage = 0f;

        this._projectile.OnHitEnemy += this.OnHitEnemy;
    }

    private void OnHitEnemy(Projectile p, SpeculativeRigidbody enemy, bool killed)
    {
        p.gameObject.Play("soul_kaliber_impact");
        enemy.gameObject.GetOrAddComponent<OraOra>().BankDamage(this._bankedDamage, this._angle);
    }
}

public class JojoReferenceHandler : MonoBehaviour
{
    private static JojoReferenceHandler _instance = null;
    private float _timestopTime                   = 0.0f;
    private bool _timeIsStopped                   = false;

    private static JojoReferenceHandler Instance {
        get {
            if (!_instance)
                _instance = GameManager.Instance.AddComponent<JojoReferenceHandler>();
            return _instance;
        }
    }

    /// <summary>Freeze all projectiles while time is stopped</summary>
    [HarmonyPatch(typeof(Projectile), nameof(Projectile.LocalTimeScale), MethodType.Getter)]
    private class ProjectileLocalTimeScalePatch
    {
        static bool Prefix(Projectile __instance, ref float __result)
        {
            if (!Instance._timeIsStopped)
                return true; // call the original method
            if (__instance.GetComponent<ImmuneToTimestop>())
                return true; // call the original method
            __result = 0.0f; // change the original result for all bullets not from Platinum Star
            return false; // skip the original method
        }
    }

    public static bool TimeIsFrozen() => Instance._timeIsStopped;
    public static void RefreshTimeStop(float duration = 0.5f)
    {
        if (duration <= 0.0f)
            return;
        if (Instance._timestopTime < duration)
            Instance._timestopTime = duration;
        if (!Instance._timeIsStopped)
            StopTime();
        Instance._timeIsStopped = true;
    }

    private void Update()
    {
        if (!_timeIsStopped)
            return;
        if ((_timestopTime -= BraveTime.DeltaTime) > 0.0f)
            return;
        _timestopTime = 0.0f;
        ResumeTime();
        _timeIsStopped = false;
    }

    private static void StopTime()
    {
        if (GameManager.Instance.BestActivePlayer is not PlayerController pc)
            return;
        if (pc.CurrentRoom is not RoomHandler room)
            return;
        if (room.activeEnemies is not List<AIActor> enemies)
            return;
        foreach (AIActor enemy in enemies)
            if (enemy)
                enemy.LocalTimeScale = 0.0f;

        Pixelator.Instance.DoFinalNonFadedLayer = true;
        foreach (PlayerController pp in GameManager.Instance.AllPlayers)
            if (pp)
                pp.gameObject.SetLayerRecursively(LayerMask.NameToLayer("Unfaded"));
        Pixelator.Instance.saturation = 0f;
        AkSoundEngine.PostEvent("Stop_SND_All", pc.gameObject);
        AkSoundEngine.StopAll();
    }

    private static void ResumeTime()
    {
        if (GameManager.Instance.BestActivePlayer is not PlayerController pc)
            return;
        if (pc.CurrentRoom is not RoomHandler room)
            return;
        if (room.activeEnemies is not List<AIActor> enemies)
            return;
        foreach (AIActor enemy in enemies)
            if (enemy)
                enemy.LocalTimeScale = 1.0f;

        Pixelator.Instance.DoFinalNonFadedLayer = false;
        foreach (PlayerController pp in GameManager.Instance.AllPlayers)
            if (pp)
                pp.gameObject.SetLayerRecursively(LayerMask.NameToLayer("FG_Reflection"));
        Pixelator.Instance.saturation = 1f;
        GameManager.Instance.DungeonMusicController.ResetForNewFloor(GameManager.Instance.Dungeon);
        GameManager.Instance.DungeonMusicController.NotifyEnteredNewRoom(room);
    }
}

public class OraOra : MonoBehaviour
{
    private const float _HIT_DELAY      = 0.1f;
    private const float _DELAY_DECAY    = 0.01f;
    private const float _ANGLE_VARIANCE = 45f;
    private const float _MOVE_TIME      = 0.1f;
    private const int   _BURST_SIZE     = 4;

    private AIActor _enemy;
    private GameObject _stand = null;
    private List<float> _bankedDamage = new();
    private List<float> _bankedAngles = new();
    private bool _activated = false;

    private void Start()
    {
        this._enemy = base.GetComponent<AIActor>();
    }

    private void OnDestroy()
    {
        this._stand.SafeDestroy();
        this._stand = null;
    }

    public void BankDamage(float damage, float angle)
    {
        if (this._activated)
            return; // can't bank damage while being actively attacked

        _bankedDamage.Add(damage);
        _bankedAngles.Add(angle);
    }

    public void OraOraOra(PlayerController pc)
    {
        if (this._activated || this._bankedDamage.Count == 0)
            return;

        this._activated = true;
        StartCoroutine(OraOraOraOra(pc));
    }

    private IEnumerator OraOraOraOra(PlayerController pc)
    {
        bool doTimeFreeze = pc.HasSynergy(Synergy.MASTERY_PLATINUM_STAR);
        float lumpDamage = doTimeFreeze ? (_BURST_SIZE * _bankedDamage.Sum()) : 0f;

        if (!this._enemy || this._enemy.healthHaver is not HealthHaver hh)
        {
            UnityEngine.GameObject.Destroy(this);
            yield break;
        }

        while (!(this._enemy && this._enemy.sprite && this._enemy.sprite.renderer.enabled && hh.IsVulnerable))
        {
            if (!hh || !hh.IsAlive)
                yield break;
            yield return null;
        }

        Vector2 enemySize = this._enemy.sprite.GetBounds().size.XY();
        float radius      = 0.5f * Mathf.Max(enemySize.x, enemySize.y);
        float offset      = radius + 2f;

        // copy lists so the for iterator isn't modified
        List<float> bankedDamage = new(_bankedDamage);
        List<float> bankedAngles = new(_bankedAngles);
        _bankedDamage.Clear();
        _bankedAngles.Clear();

        tk2dSprite standSprite = Lazy.SpriteObject(
                spriteColl: pc.spriteAnimator.CurrentClip.frames[0].spriteCollection,
                spriteId: pc.spriteAnimator.GetClipByName(Lazy.GetBaseIdleAnimationName(pc, bankedAngles[0])).frames[0].spriteId);
            standSprite.usesOverrideMaterial = true;
            standSprite.renderer.material.shader = ShaderCache.Acquire("Brave/Internal/HologramShader");
        this._stand = standSprite.gameObject;

        if (doTimeFreeze)
        {
            this._stand.SetLayerRecursively(LayerMask.NameToLayer("Unfaded"));
            JojoReferenceHandler.RefreshTimeStop();
        }

        for (float elapsed = BraveTime.DeltaTime; elapsed < _MOVE_TIME; elapsed += BraveTime.DeltaTime)
        {
            float percentDone = elapsed / _MOVE_TIME;
            standSprite.PlaceAtPositionByAnchor(
                Vector2.Lerp(pc.CenterPosition, this._enemy.CenterPosition - bankedAngles[0].ToVector(offset + 0.5f), percentDone),
                Anchor.MiddleCenter);
            yield return null;
        }

        float baseDelay = _HIT_DELAY;
        int numBursts = bankedDamage.Count;
        BehaviorSpeculator spec = this._enemy.behaviorSpeculator;
        for (int i = 0; i < numBursts; ++i)
        {
            if (!(this._enemy && this._enemy.healthHaver && this._enemy.healthHaver.IsAlive))
                break;
            float damage = bankedDamage[i];
            bool lastBurst = (i == (numBursts - 1));
            standSprite.SetSprite(pc.spriteAnimator.GetClipByName(Lazy.GetBaseIdleAnimationName(pc, bankedAngles[i])).frames[0].spriteId);
            standSprite.PlaceAtPositionByAnchor(this._enemy.CenterPosition - bankedAngles[i].ToVector(offset + 0.5f), Anchor.MiddleCenter);
            standSprite.FlipX = (Mathf.Abs(bankedAngles[i].Clamp180()) > 90f);
            for (int j = 0; j < _BURST_SIZE; ++j)
            {
                bool lastHit     = lastBurst && j == (_BURST_SIZE - 1);
                float angle      = (bankedAngles[i] + UnityEngine.Random.Range(-_ANGLE_VARIANCE, _ANGLE_VARIANCE)).Clamp360();
                Vector2 angleVec = angle.ToVector(offset);
                Vector2 startPos = this._enemy.CenterPosition - angleVec;
                Projectile proj  = SpawnManager.SpawnProjectile(PlatinumStar._OraBullet.gameObject, startPos, angle.EulerZ()).GetComponent<Projectile>();
                    proj.Owner                                    = pc;
                    proj.Shooter                                  = pc.specRigidbody;
                    proj.gameObject.SetAlphaImmediate(0.3f);
                    proj.baseData.damage                          = doTimeFreeze ? 0 : damage;
                    proj.specRigidbody.CollideWithTileMap         = false;
                    if (lastHit)
                    {
                        proj.baseData.force = numBursts * _BURST_SIZE;
                        proj.OnDestruction += (Projectile p) => {
                            p.gameObject.Play("ora_final_hit_sound");
                        };
                    }
                    else
                    {
                        proj.OnDestruction += (Projectile p) => {
                            p.gameObject.PlayUnique("ora_hit_sound");
                        };
                    }
                    proj.SendInDirection(angleVec, false);
                    proj.gameObject.PlayUnique("ora_fist_fire");

                if (doTimeFreeze)
                    JojoReferenceHandler.RefreshTimeStop();
                else if (spec && !spec.ImmuneToStun)
                    spec.Stun(1f);
                yield return new WaitForSeconds(baseDelay);
            }
            baseDelay = Mathf.Max(baseDelay - _DELAY_DECAY, 0.02f);
        }

        Vector2 finalPos = standSprite.WorldCenter;
        for (float elapsed = BraveTime.DeltaTime; elapsed < _MOVE_TIME; elapsed += BraveTime.DeltaTime)
        {
            float percentDone = elapsed / _MOVE_TIME;
            standSprite.PlaceAtPositionByAnchor(Vector2.Lerp(finalPos, pc.CenterPosition, percentDone), Anchor.MiddleCenter);
            yield return null;
        }
        UnityEngine.Object.Destroy(this._stand);
        this._stand = null;

        if (doTimeFreeze)
        {
            while (JojoReferenceHandler.TimeIsFrozen())
                yield return null;
            if (this._enemy && hh && hh.IsAlive)
            {
                this._enemy.LocalTimeScale = 1.0f;
                hh.ApplyDamage(lumpDamage, Vector2.zero, PlatinumStar.ItemName, CoreDamageTypes.Void, DamageCategory.Normal,
                    ignoreInvulnerabilityFrames: true, ignoreDamageCaps: true);
                if (spec && !spec.ImmuneToStun)
                    spec.Stun(1f);
            }
        }

        this._activated = false;
        yield break;
    }
}
