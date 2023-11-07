namespace CwaffingTheGungy;

public class Blackjack : AdvancedGunBehavior
{
    public static string ItemName         = "Blackjack";
    public static string SpriteName       = "blackjack";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "Gambit's Queens";
    public static string LongDescription  = "Fires cards whose range increase with accuracy. Ammo can only be regained by picking up cards from the floor.\n\nMany would argue that cards do not make the best projectiles for a gun...and many would largely be correct, as their lack of raw power and aerodynamics make them rather weak and unreliable in the hands of a novice. The most proficient and well-prepared duelists, however, have demonstrated that a single deck of cards is more than capable of dealing with the Gungeon's greatest threats.";

    private const int _DECK_SIZE = 52; // need to finish up individual playing cards later
    private const int _CLIP_SIZE = 13; // 1 suit
    private const int _NUM_DECKS = 2;

    internal static tk2dSpriteAnimationClip _BulletSprite;
    internal static tk2dSpriteAnimationClip _BackSprite;

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<Blackjack>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription);
            gun.SetAttributes(quality: PickupObject.ItemQuality.C, gunClass: GunClass.SILLY, reloadTime: 0.8f, ammo: _DECK_SIZE * _NUM_DECKS, canGainAmmo: false);
            gun.SetAnimationFPS(gun.shootAnimation, 30);
            gun.SetAnimationFPS(gun.reloadAnimation, 30);
            gun.SetFireAudio(); // prevent fire audio, as it's handled in OnPostFired()
            gun.SetReloadAudio("card_shuffle_sound"); // todo: this is still playing the default reload sound as well, for some reason

        ProjectileModule mod = gun.DefaultModule;
            mod.ammoCost            = 1;
            mod.shootStyle          = ProjectileModule.ShootStyle.Automatic;
            mod.sequenceStyle       = ProjectileModule.ProjectileSequenceStyle.Random;
            mod.angleVariance       = 24.0f;
            mod.cooldownTime        = 0.16f;
            mod.numberOfShotsInClip = _CLIP_SIZE;
            mod.SetupCustomAmmoClip(SpriteName);

        _BulletSprite = AnimateBullet.CreateProjectileAnimation(
            ResMap.Get("playing_card").Base(),
            0, true, new IntVector2(12, 8),
            false, tk2dBaseSprite.Anchor.MiddleLeft, true, true);

        _BackSprite = AnimateBullet.CreateProjectileAnimation(
            ResMap.Get("playing_card_back").Base(),
            0, true, new IntVector2(12, 8),
            false, tk2dBaseSprite.Anchor.MiddleLeft, true, true);

        Projectile projectile = Lazy.PrefabProjectileFromGun(gun);
            projectile.AddDefaultAnimation(_BulletSprite);
            projectile.baseData.damage = 8f;
            projectile.baseData.range  = 999f; // we implement a custom range-like behavior
            projectile.baseData.speed  = 18f;
            projectile.gameObject.AddComponent<ThrownCard>();
            projectile.transform.parent = gun.barrelOffset;
    }

    public override Projectile OnPreFireProjectileModifier(Gun gun, Projectile projectile, ProjectileModule mod)
    {
        if (projectile.GetComponent<ThrownCard>() is not ThrownCard tc)
            return projectile;
        if (gun.CurrentOwner is not PlayerController player)
            return projectile;

        tc.isAFreebie = (mod.ammoCost == 0 || gun.InfiniteAmmo || gun.LocalInfiniteAmmo || gun.CanGainAmmo || player.InfiniteAmmo.Value);
        return projectile;
    }
}

public class ThrownCard : MonoBehaviour
{
    private const float _SPIN_SPEED = 2.0f;
    private const float _BASE_LIFE  = 0.33f;
    private const float _AIR_DRAG   = 0.94f;

    private Projectile       _projectile;
    private PlayerController _owner;
    private float            _lifetime         = 0.0f;
    private float            _distanceTraveled = 0.0f;
    private int              _cardFront        = 0;
    private int              _cardBack         = 0;
    private float            _timeAtMaxPower   = 0.0f;
    private bool             _faltering        = false;
    private float            _curveAmount      = 0.0f;
    private float            _startScale       = 1f;

    public bool isAFreebie = true; // false if we fired directly from the gun and it cost us ammo, true otherwise

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._owner = this._projectile.Owner as PlayerController;

        this._projectile.OnDestruction += CreatePlayingCardPickup;

        // don't use an emissive / tinted shader so we can turn off the glowing yellow tint effect
        // this._projectile.sprite.usesOverrideMaterial = true; // keep this off so we still get nice lighting
        this._projectile.sprite.renderer.material.shader = ShaderCache.Acquire("Brave/LitBlendUber");

        BounceProjModifier bounce = this._projectile.gameObject.GetOrAddComponent<BounceProjModifier>();
            bounce.numberOfBounces     = 1;
            bounce.chanceToDieOnBounce = 0f;
            bounce.onlyBounceOffTiles  = false;
            bounce.OnBounce += () => {
                this._faltering = true;
                this._projectile.baseData.speed *= 0.4f;
            };

        CalculateStatsFromPlayerStats();

        this._cardFront  = Blackjack._BulletSprite.GetFrame(0).spriteId;
        this._cardBack   = Blackjack._BackSprite.GetFrame(0).spriteId;
        this._startScale = (Lazy.CoinFlip() ? -1f : 1f);

        AkSoundEngine.PostEvent("card_throw_sound_stop_all", this._projectile.gameObject);
        AkSoundEngine.PostEvent("card_throw_sound", this._projectile.gameObject);
    }

    private void CalculateStatsFromPlayerStats()
    {
        float acc            = this._owner.stats.GetStatModifier(PlayerStats.StatType.Accuracy);
        float inverseRootAcc = Mathf.Sqrt(1.0f / acc);
        this._timeAtMaxPower = _BASE_LIFE * inverseRootAcc * UnityEngine.Random.Range(0.8f, 1.2f);
        this._projectile.baseData.damage *= inverseRootAcc;
    }

    private void Update()
    {
        if (this._projectile.baseData.speed < 1f)
        {
            this._projectile.DieInAir(suppressInAirEffects: true);
            return;
        }

        if (BraveTime.DeltaTime == 0)
            return;

        float timeScale = BraveTime.DeltaTime * C.FPS;
        this._lifetime += BraveTime.DeltaTime;
        if (this._faltering || this._lifetime >= this._timeAtMaxPower)
        {
            if (!this._faltering)
            {
                this._faltering = true;
                this._curveAmount = (Lazy.CoinFlip() ? -1f : 1f) * 5f * UnityEngine.Random.value;
            }
            // this._projectile.baseData.speed *= _AIR_DRAG;
            this._projectile.baseData.speed *= Mathf.Pow(_AIR_DRAG, timeScale); // todo: see if this slows things down too much
            this._projectile.SendInDirection(
                (this._projectile.m_currentDirection.ToAngle() + this._curveAmount * timeScale).ToVector(), true, true);
            this._projectile.UpdateSpeed();
        }

        this._distanceTraveled += BraveTime.DeltaTime * this._projectile.baseData.speed;
        float scale = this._startScale * Mathf.Cos(_SPIN_SPEED * this._distanceTraveled);
        this._projectile.sprite.scale = this._projectile.sprite.scale.WithY(scale);
        this._projectile.spriteAnimator.SetSprite(
            this._projectile.sprite.collection, scale > 0 ? this._cardFront : this._cardBack);
    }

    private void CreatePlayingCardPickup(Projectile p)
    {
        if (this.isAFreebie)
            return; // don't create free ammo from, e.g., scattershot

        MiniInteractable mi = MiniInteractable.CreateInteractableAtPosition(
          p.sprite, // correct transform w.r.t. MiddleLeft anchor
          p.sprite.transform.position + new Vector3(0.5f * 12f / C.PIXELS_PER_TILE, 0, 0),
          PickUpPlayingCardScript);
        mi.autoInteract = true;
        mi.transform.rotation = p.transform.rotation;
        // mi.sprite.usesOverrideMaterial = true;
        mi.sprite.renderer.material = p.sprite.renderer.material;
    }

    public IEnumerator PickUpPlayingCardScript(MiniInteractable i, PlayerController p)
    {
        foreach (Gun gun in p.inventory.AllGuns)
        {
            if (!gun.GetComponent<Blackjack>())
                continue;
            if (gun.CurrentAmmo >= gun.AdjustedMaxAmmo)
                break;
            gun.CurrentAmmo += 1;
            AkSoundEngine.PostEvent("card_pickup_sound_stop_all", p.gameObject);
            AkSoundEngine.PostEvent("card_pickup_sound", p.gameObject);
            SpawnManager.SpawnVFX(VFX.animations["MiniPickup"], i.sprite.WorldCenter, Lazy.RandomEulerZ());
            UnityEngine.Object.Destroy(i.gameObject);
            break;
        }
        i.interacting = false;
        yield break;
    }
}
