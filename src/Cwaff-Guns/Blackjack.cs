namespace CwaffingTheGungy;

public class Blackjack : CwaffGun
{
    public static string ItemName         = "Blackjack";
    public static string ShortDescription = "Gambit's Queens";
    public static string LongDescription  = "Fires cards whose speed, range, and damage increase with accuracy. Ammo can only be regained by picking up cards from the floor.";
    public static string Lore             = "Many would argue that cards do not make the best projectiles for a gun...and many would largely be correct, as their lack of raw power and aerodynamics make them rather weak and unreliable in the hands of a novice. The most proficient and well-prepared duelists, however, have demonstrated that a single deck of cards is more than capable of dealing with the Gungeon's greatest threats.";

    private const int _DECK_SIZE = 52; // need to finish up individual playing cards later
    private const int _CLIP_SIZE = 13; // 1 suit
    private const int _NUM_DECKS = 2;
    private const int _AMMO      = 104; // _DECK_SIZE * _NUM_DECKS //NOTE: set manually for now so wiki generation code has an easier time

    internal static tk2dSpriteAnimationClip _BulletSprite;
    internal static tk2dSpriteAnimationClip _BackSprite;

    public static void Init()
    {
        Gun gun = Lazy.SetupGun<Blackjack>(ItemName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.C, gunClass: GunClass.SILLY, reloadTime: 0.8f, ammo: _AMMO, canGainAmmo: false,
                shootFps: 30, reloadFps: 30, muzzleFrom: Items.Mailbox, reloadAudio: "card_shuffle_sound");

        gun.InitProjectile(GunData.New(clipSize: _CLIP_SIZE, cooldown: 0.16f, angleVariance: 24.0f, shootStyle: ShootStyle.Automatic,
          customClip: true, damage: 6f, speed: 22f, range: 999f, hitSound: "blackjack_card_impact_sound"
          )).AddAnimations(
            AnimatedBullet.Create(refClip: ref _BulletSprite, name: "playing_card",      fps: 0, scale: 0.25f, anchor: Anchor.MiddleLeft),
            AnimatedBullet.Create(refClip: ref _BackSprite,   name: "playing_card_back", fps: 0, scale: 0.25f, anchor: Anchor.MiddleLeft)
          ).SetAllImpactVFX(VFX.CreatePool("blackjack_card_impact_vfx", fps: 16, loops: false, scale: 0.75f, anchor: Anchor.MiddleCenter)
          ).Attach<ThrownCard>();
    }

    public override Projectile OnPreFireProjectileModifier(Gun gun, Projectile projectile, ProjectileModule mod)
    {
        if (projectile.GetComponent<ThrownCard>() is not ThrownCard tc)
            return projectile;
        if (gun.CurrentOwner is not PlayerController player)
            return projectile;

        tc.isAFreebie = projectile.FiredForFree(gun, mod);
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
    private float            _startAngle       = 0f;
    private bool             _bounced          = false;

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
            bounce.OnBounce += this.OnBounce;

        CalculateStatsFromPlayerStats();

        this._cardFront  = Blackjack._BulletSprite.GetFrame(0).spriteId;
        this._cardBack   = Blackjack._BackSprite.GetFrame(0).spriteId;
        this._startScale = (Lazy.CoinFlip() ? -1f : 1f);
        this._startAngle = this._projectile.OriginalDirection();

        this._projectile.gameObject.PlayUnique("card_throw_sound");
    }

    private void OnBounce()
    {
        this._lifetime = this._timeAtMaxPower; // force falter next frame for Helix Bullets compatibility
        this._bounced = true;
        this._projectile.m_usesNormalMoveRegardless = true; // ignore Helix projectiles and other motion modifiers after bouncing
        this._projectile.baseData.speed *= 0.4f;
    }

    private void CalculateStatsFromPlayerStats()
    {
        float acc            = this._owner.AccuracyMult();
        float inverseRootAcc = Mathf.Sqrt(1.0f / Mathf.Max(0.1f, acc));
        this._timeAtMaxPower = _BASE_LIFE * UnityEngine.Random.Range(0.8f, 1.2f);
        this._projectile.baseData.damage *= Mathf.Clamp(inverseRootAcc, 1f, 3f);
        this._projectile.baseData.speed *= inverseRootAcc;
        this._projectile.UpdateSpeed();
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

        this._lifetime += BraveTime.DeltaTime;
        if (this._faltering || this._lifetime >= this._timeAtMaxPower)
        {
            float oldAngle = this._projectile.Direction.ToAngle();
            bool wasFaltering = this._faltering;
            if (!wasFaltering)
            {
                this._faltering = true;
                this._curveAmount = (Lazy.CoinFlip() ? -1f : 1f) * 5f * UnityEngine.Random.value;
                if (!this._bounced)
                    oldAngle = this._startAngle; // if we start faltering while a projectilemotinmodule is active, we want to reference are original angle
            }
            float timeScale = BraveTime.DeltaTime * C.FPS;
            this._projectile.ApplyFriction(_AIR_DRAG);

            float newAngle = (oldAngle + (this._curveAmount * timeScale));
            if (wasFaltering && this._projectile.OverrideMotionModule is HelixProjectileMotionModule hpmm)
                hpmm.AdjustRightVector(Mathf.DeltaAngle(oldAngle, newAngle));
            else
                this._projectile.SendInDirection(BraveMathCollege.DegreesToVector(newAngle), true);
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

    public static IEnumerator PickUpPlayingCardScript(MiniInteractable i, PlayerController p)
    {
        if ((p.FindBaseGun<Blackjack>() is Gun gun) && (gun.CurrentAmmo < gun.AdjustedMaxAmmo))
        {
            gun.CurrentAmmo += 1;
            p.gameObject.PlayUnique("card_pickup_sound");
            SpawnManager.SpawnVFX(VFX.MiniPickup, i.sprite.WorldCenter, Lazy.RandomEulerZ());
            UnityEngine.Object.Destroy(i.gameObject);
        }
        i.interacting = false;
        yield break;
    }
}
