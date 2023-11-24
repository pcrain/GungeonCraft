namespace CwaffingTheGungy;

public class PlatinumStar : AdvancedGunBehavior
{
    public static string ItemName         = "Platinum Star";
    public static string SpriteName       = "platinum_star";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "Unbreakable";
    public static string LongDescription  = "Fires projectiles that deal no damage on initial impact. When reloaded, summons manifestations of the user's soul to attack all previously-shot enemies with a flurry of rapid punches.";
    public static string Lore             = "This gun had a large golden arrow pierced through its barrel and grip when it was originally discovered by Ox and Cadence lying in a patch of flowers behind the Gungeon. The arrow had mysteriously vanished by the time they had gotten back to the Breach to test out the gun's capabilities, which by all accounts seemed to be nothing more than rapid-firing some harmless phantom projectiles. Having deemed the gun worthless in combat, Cadence tossed it back outside where she found it, only for it to vanish before hitting the ground. Somehow, it has found its way into the Gungeon on its own.";

    internal static tk2dSpriteAnimationClip _BulletSprite;
    internal static Projectile _OraBullet;

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<PlatinumStar>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.A, gunClass: GunClass.PISTOL, reloadTime: 1f, ammo: 480);
            gun.SetAnimationFPS(gun.shootAnimation, 20);
            gun.SetAnimationFPS(gun.reloadAnimation, 40);
            gun.SetMuzzleVFX("muzzle_platinum_star", fps: 60, scale: 0.25f, anchor: Anchor.MiddleCenter);
            gun.SetFireAudio("platinum_fire_sound");
            gun.SetReloadAudio("platinum_reload_sound", frame: 5);
            gun.AddToSubShop(ModdedShopType.TimeTrader);

        ProjectileModule mod = gun.DefaultModule;
            mod.ammoCost            = 1;
            mod.shootStyle          = ShootStyle.Automatic;
            mod.sequenceStyle       = ProjectileSequenceStyle.Random;
            mod.angleVariance       = 15.0f;
            mod.cooldownTime        = 0.125f;
            mod.numberOfShotsInClip = 28;
            mod.SetupCustomAmmoClip(SpriteName);

        _BulletSprite = AnimateBullet.CreateProjectileAnimation(
            ResMap.Get("platinum_star_projectile").Base(),
            12, true, new IntVector2(29, 9),
            false, Anchor.MiddleLeft, true, true);

        Projectile projectile = Lazy.PrefabProjectileFromGun(gun);
            projectile.AddDefaultAnimation(_BulletSprite);
            projectile.baseData.damage  = 3f;
            projectile.baseData.force   = 1f;
            projectile.baseData.speed   = 50.0f;
            projectile.baseData.range   = 50.0f;
            projectile.transform.parent = gun.barrelOffset;
            projectile.gameObject.AddComponent<PlatinumProjectile>();

        _OraBullet = Lazy.PrefabProjectileFromGun(ItemHelper.Get(Items.Polaris) as Gun);
            _OraBullet.AddDefaultAnimation(AnimateBullet.CreateProjectileAnimation(
                ResMap.Get("ora_fist_fast").Base(),
                12, true, new IntVector2(63 / 3, 27 / 3),
                false, Anchor.MiddleRight, true, true));
            _OraBullet.shouldRotate    = true;
            _OraBullet.baseData.damage = 1f;
            _OraBullet.baseData.force  = 0.1f;
            _OraBullet.baseData.range  = 3f;
            _OraBullet.baseData.speed  = 75f;
            _OraBullet.gameObject.GetOrAddComponent<PierceProjModifier>().penetration = 999;
    }

    public override void OnPostFired(PlayerController player, Gun gun)
    {
        base.OnPostFired(player, gun);
        AkSoundEngine.PostEvent("tomislav_shoot", gun.gameObject);
        // Material m = this.gun.gameObject.GetOrAddShader(Shader.Find("Brave/ItemSpecific/LootGlintAdditivePass"));
        // m.SetColor("_OverrideColor", Color.yellow);
        // m.SetFloat("_Period", 1.0f);
        // m.SetFloat("_PixelWidth", 5.0f);
        // Material m3 = this.Owner.sprite.gameObject.GetOrAddShader(Shader.Find("Brave/ItemSpecific/LootGlintAdditivePass"));
        // m3.SetColor("_OverrideColor", Color.yellow);
        // m3.SetFloat("_Period", 1.0f);
        // m3.SetFloat("_PixelWidth", 5.0f);
    }

    public override void OnReload(PlayerController player, Gun gun)
    {
        base.OnReload(player, gun);
        gun.muzzleFlashEffects.DestroyAll(); // since we're preventing gun rotation on reload, the muzzle vfx look weird, so just disable them
        LaunchAllBullets(this.Player);
    }

    public override void OnSwitchedToThisGun()
    {
        base.OnSwitchedToThisGun();
        LaunchAllBullets(this.Player);
    }

    public override void OnDropped()
    {
        base.OnDropped();
        LaunchAllBullets(this.Player);
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        base.OnSwitchedAwayFromThisGun();
        LaunchAllBullets(this.Player);
    }

    private void LaunchAllBullets(PlayerController pc)
    {
        foreach (AIActor enemy in StaticReferenceManager.AllEnemies)
            if (enemy?.GetComponent<OraOra>() is OraOra oraora) oraora.OraOraOra(pc);
    }

    protected override void Update()
    {
        base.Update();
        this.gun.preventRotation = (this.gun.spriteAnimator.currentClip.name == this.gun.reloadAnimation);
    }
}

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

        this._projectile.OnHitEnemy += (Projectile p, SpeculativeRigidbody enemy, bool _) => {
            AkSoundEngine.PostEvent("soul_kaliber_impact", p.gameObject);
            OraOra oraora = enemy.aiActor.gameObject.GetOrAddComponent<OraOra>();
                oraora.BankDamage(this._bankedDamage, this._angle);
        };
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
        if (this._stand != null)
            UnityEngine.Object.Destroy(this._stand);
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
        if (this._activated || this._bankedDamage.Count() == 0)
            return;

        this._activated = true;
        StartCoroutine(OraOraOraOra(pc));
    }

    private IEnumerator OraOraOraOra(PlayerController pc)
    {
        if (this._enemy?.healthHaver is not HealthHaver hh)
        {
            UnityEngine.GameObject.Destroy(this);
            yield break;
        }

        while (!(this._enemy?.sprite && this._enemy.sprite.renderer.enabled && hh.IsVulnerable))
        {
            if (!this._enemy || !hh || !hh.IsAlive)
                yield break;
            yield return null;
        }

        BehaviorSpeculator spec = this._enemy.behaviorSpeculator;
        if (spec && !spec.ImmuneToStun)
            spec.Stun(1f);

        Vector2 enemySize = this._enemy.sprite.GetBounds().size.XY();
        float radius      = 0.5f * Mathf.Max(enemySize.x, enemySize.y);
        float offset      = radius + 2f;

        // copy lists so the for iterator isn't modified
        List<float> bankedDamage = new(_bankedDamage);
        List<float> bankedAngles = new(_bankedAngles);
        _bankedDamage.Clear();
        _bankedAngles.Clear();

        this._stand = new GameObject();
        tk2dSprite standSprite = this._stand.AddComponent<tk2dSprite>();
            standSprite.SetSprite(
                newCollection: pc.spriteAnimator.CurrentClip.frames[0].spriteCollection,
                newSpriteId: pc.spriteAnimator.GetClipByName(Lazy.GetBaseIdleAnimationName(pc, bankedAngles[0])).frames[0].spriteId);
            standSprite.usesOverrideMaterial = true;
            standSprite.renderer.material.shader = ShaderCache.Acquire("Brave/Internal/HologramShader");
        this._stand.GetComponent<BraveBehaviour>().sprite = standSprite;

        for (float elapsed = BraveTime.DeltaTime; elapsed < _MOVE_TIME; elapsed += BraveTime.DeltaTime)
        {
            float percentDone = elapsed / _MOVE_TIME;
            standSprite.PlaceAtPositionByAnchor(
                Vector2.Lerp(pc.sprite.WorldCenter, this._enemy.sprite.WorldCenter - bankedAngles[0].ToVector(offset + 0.5f), percentDone),
                Anchor.MiddleCenter);
            yield return null;
        }

        float baseDelay = _HIT_DELAY;
        int numBursts = bankedDamage.Count();
        for (int i = 0; i < numBursts; ++i)
        {
            if (!(this._enemy?.healthHaver?.IsAlive ?? false))
                break;
            float damage = bankedDamage[i];
            bool lastBurst = (i == (numBursts - 1));
            standSprite.SetSprite(pc.spriteAnimator.GetClipByName(Lazy.GetBaseIdleAnimationName(pc, bankedAngles[i])).frames[0].spriteId);
            standSprite.PlaceAtPositionByAnchor(this._enemy.sprite.WorldCenter - bankedAngles[i].ToVector(offset + 0.5f), Anchor.MiddleCenter);
            standSprite.FlipX = (Mathf.Abs(bankedAngles[i].Clamp180()) > 90f);
            for (int j = 0; j < _BURST_SIZE; ++j)
            {
                bool lastHit     = lastBurst && j == (_BURST_SIZE - 1);
                float angle      = (bankedAngles[i] + UnityEngine.Random.Range(-_ANGLE_VARIANCE, _ANGLE_VARIANCE)).Clamp360();
                Vector2 angleVec = angle.ToVector(offset);
                Vector2 startPos = this._enemy.sprite.WorldCenter - angleVec;
                Projectile proj  = SpawnManager.SpawnProjectile(PlatinumStar._OraBullet.gameObject, startPos, angle.EulerZ()).GetComponent<Projectile>();
                    proj.Owner                                    = pc;
                    proj.Shooter                                  = pc.specRigidbody;
                    proj.gameObject.SetAlphaImmediate(0.3f);
                    proj.baseData.damage                          = damage;
                    proj.specRigidbody.CollideWithTileMap         = false;
                    if (lastHit)
                    {
                        proj.baseData.force = numBursts * _BURST_SIZE;
                        proj.OnDestruction += (Projectile p) => {
                            AkSoundEngine.PostEvent("ora_final_hit_sound", p.gameObject);
                        };
                    }
                    else
                    {
                        proj.OnDestruction += (Projectile p) => {
                            AkSoundEngine.PostEvent("ora_hit_sound_stop_all", p.gameObject);
                            AkSoundEngine.PostEvent("ora_hit_sound", p.gameObject);
                        };
                    }
                    proj.SendInDirection(angleVec, false);
                    proj.UpdateSpeed();
                    AkSoundEngine.PostEvent("ora_fist_fire_stop_all", proj.gameObject);
                    AkSoundEngine.PostEvent("ora_fist_fire", proj.gameObject);

                if (spec && !spec.ImmuneToStun)
                    spec.UpdateStun(1f);
                yield return new WaitForSeconds(baseDelay);
            }
            baseDelay = Mathf.Max(baseDelay - _DELAY_DECAY, 0.02f);
        }

        Vector2 finalPos = standSprite.WorldCenter;
        for (float elapsed = BraveTime.DeltaTime; elapsed < _MOVE_TIME; elapsed += BraveTime.DeltaTime)
        {
            float percentDone = elapsed / _MOVE_TIME;
            standSprite.PlaceAtPositionByAnchor(Vector2.Lerp(finalPos, pc.sprite.WorldCenter, percentDone), Anchor.MiddleCenter);
            yield return null;
        }

        UnityEngine.Object.Destroy(this._stand);
        this._stand = null;
        this._activated = false;
        yield break;
    }
}
