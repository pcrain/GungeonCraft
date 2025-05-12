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
    internal static tk2dSpriteAnimationClip _RedSprite;
    internal static tk2dSpriteAnimationClip _RedBackSprite;

    public static void Init()
    {
        Lazy.SetupGun<Blackjack>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.C, gunClass: GunClass.SILLY, reloadTime: 0.8f, ammo: _AMMO, canGainAmmo: false,
            shootFps: 30, reloadFps: 30, muzzleFrom: Items.Mailbox, reloadAudio: "card_shuffle_sound", fireAudio: "card_throw_sound")
          .AssignGun(out Gun gun)
          .InitProjectile(GunData.New(clipSize: _CLIP_SIZE, cooldown: 0.16f, angleVariance: 24.0f, shootStyle: ShootStyle.Automatic,
            customClip: true, damage: 6f, speed: 22f, range: 999f, hitSound: "blackjack_card_impact_sound"))
          .AddAnimations(
            AnimatedBullet.Create(refClip: ref _BulletSprite,  name: "playing_card",          fps: 0, scale: 0.25f, anchor: Anchor.MiddleLeft),
            AnimatedBullet.Create(refClip: ref _BackSprite,    name: "playing_card_back",     fps: 0, scale: 0.25f, anchor: Anchor.MiddleLeft),
            AnimatedBullet.Create(refClip: ref _RedSprite,     name: "playing_card_red",      fps: 0, scale: 0.25f, anchor: Anchor.MiddleLeft),
            AnimatedBullet.Create(refClip: ref _RedBackSprite, name: "playing_card_back_red", fps: 0, scale: 0.25f, anchor: Anchor.MiddleLeft))
          .SetAllImpactVFX(VFX.CreatePool("blackjack_card_impact_vfx", fps: 16, loops: false, scale: 0.75f, anchor: Anchor.MiddleCenter))
          .Attach<ThrownCard>()
          .Assign(out Projectile p);

        gun.AddSynergyModules(Synergy.PIT_BOSS, new ProjectileModule().InitSingleProjectileModule(GunData.New(gun: gun, ammoCost: 0, clipSize: _CLIP_SIZE,
          cooldown: 0.16f, shootStyle: ShootStyle.Automatic, customClip: true, angleFromAim: 20f, angleVariance: 5f, ignoredForReloadPurposes: true,
          mirror: true, baseProjectile: Items._38Special.Projectile(), sprite: "chip_projectile", hitSound: "chess_move", becomeDebris: true)));

        gun.AddSynergyFinalProjectile(Synergy.MASTERY_BLACKJACK, p.Clone().Attach<ThrownCard>(t => t.explosive = true), "blackjack_red", _CLIP_SIZE);
    }
}

public class ThrownCard : MonoBehaviour
{
    private const float _SPIN_SPEED = 2.0f;
    private const float _BASE_LIFE  = 0.33f;
    private const float _AIR_DRAG   = 0.94f;

    public bool explosive = false;

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

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._owner = this._projectile.Owner as PlayerController;

        this._projectile.OnDestruction += CreatePlayingCardPickup;

        // don't use an emissive / tinted shader so we can turn off the glowing yellow tint effect
        // this._projectile.sprite.usesOverrideMaterial = true; // keep this off so we still get nice lighting
        this._projectile.sprite.renderer.material.shader = ShaderCache.Acquire("Brave/LitBlendUber");

        if (!this.explosive)
        {
            this._cardFront  = Blackjack._BulletSprite.GetFrame(0).spriteId;
            this._cardBack   = Blackjack._BackSprite.GetFrame(0).spriteId;
            BounceProjModifier bounce = this._projectile.gameObject.GetOrAddComponent<BounceProjModifier>();
            bounce.numberOfBounces     = 1;
            bounce.chanceToDieOnBounce = 0f;
            bounce.onlyBounceOffTiles  = false;
            bounce.OnBounce += this.OnBounce;
        }
        else
        {
            this._cardFront  = Blackjack._RedSprite.GetFrame(0).spriteId;
            this._cardBack   = Blackjack._RedBackSprite.GetFrame(0).spriteId;
            this._projectile.gameObject.AddComponent<ExplosiveModifier>().explosionData = Bouncer._MiniExplosion;
        }

        CalculateStatsFromPlayerStats();

        this._startScale = (Lazy.CoinFlip() ? -1f : 1f);
        this._startAngle = this._projectile.OriginalDirection();
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
        if (!p.FiredForFree() && p.sprite) // don't create free ammo from, e.g., scattershot
            PlayingCard.Create(p);
    }

    private class PlayingCard : MonoBehaviour
    {
        const float PICKUP_RADIUS = 3f;
        const float PICKUP_RADIUS_SQR = PICKUP_RADIUS * PICKUP_RADIUS;

        private tk2dBaseSprite _sprite = null;

        internal static PlayingCard Create(Projectile p)
        {
            tk2dBaseSprite sprite = p.sprite.DuplicateInWorld();
            PlayingCard card = sprite.gameObject.AddComponent<PlayingCard>();
            card._sprite = sprite;
            return card;
        }

        private void Update()
        {
            Vector2 pos = this._sprite.WorldCenter;
            for (int i = 0; i < GameManager.Instance.AllPlayers.Length; ++i)
            {
                PlayerController p = GameManager.Instance.AllPlayers[i];
                if (!p || p.IsGhost || p.healthHaver.IsDead)
                    continue;
                if ((p.CenterPosition - pos).sqrMagnitude > PICKUP_RADIUS_SQR)
                    continue;
                if ((p.FindBaseGun<Blackjack>() is not Gun gun) || (gun.CurrentAmmo >= gun.AdjustedMaxAmmo))
                    continue;
                gun.CurrentAmmo += 1;
                p.gameObject.PlayUnique("card_pickup_sound");
                SpawnManager.SpawnVFX(VFX.MiniPickup, pos, Lazy.RandomEulerZ());
                UnityEngine.Object.Destroy(base.gameObject);
                break;
            }
        }
    }
}
