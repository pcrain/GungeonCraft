namespace CwaffingTheGungy;

public class QuarterPounder : CwaffGun
{
    public static string ItemName         = "Quarter Pounder";
    public static string ShortDescription = "Pay Per Pew";
    public static string LongDescription  = "Uses casings as ammo. Fires high-powered projectiles that transmute enemies to gold upon death, spawning an extra casing.";
    public static string Lore             = "Legend says that Dionysus granted King Midas' wish that everything he touched would turn to gold. Midas was overjoyed at first, but upon turning his food and daughter to gold, realized his wish was ill thought out, and eventually died of starvation.\n\nThe average person might interpret King Midas as a cautionary tale to be mindful of what you wish for. One gunsmith, however, heard the tale and thought, \"wow, turning my enemies to gold sure would be useful!\". Despite completely missing the moral of King Midas, the gunsmith did succeed in forging a rather powerful weapon, proving that the meaning of art is indeed up to the beholder.";

    internal static GameObject _MidasParticleVFX;

    public static void Init()
    {
        Gun gun = Lazy.SetupGun<QuarterPounder>(ItemName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.C, gunClass: GunClass.RIFLE, reloadTime: 1.1f, ammo: 9999, canGainAmmo: false,
                shootFps: 24, reloadFps: 16, muzzleVFX: "muzzle_quarter_pounder", muzzleFps: 30, muzzleScale: 0.4f, muzzleAnchor: Anchor.MiddleCenter,
                fireAudio: "fire_coin_sound", reloadAudio: "coin_gun_reload");

        gun.InitProjectile(GunData.New(clipSize: 10, angleVariance: 15.0f, shootStyle: ShootStyle.SemiAutomatic, customClip: true, damage: 20.0f, speed: 44.0f,
          sprite: "coin_gun_projectile", fps: 2, anchor: Anchor.MiddleCenter)).Attach<MidasProjectile>();

        _MidasParticleVFX = VFX.Create("midas_sparkle",
            fps: 8, loops: true, anchor: Anchor.MiddleCenter, emissivePower: 5);
    }

    public override void OnPlayerPickup(PlayerController player)
    {
        base.OnPlayerPickup(player);
        AdjustAmmoToMoney();
    }

    public override void OnSwitchedToThisGun()
    {
        base.OnSwitchedToThisGun();
        AdjustAmmoToMoney();
    }

    public override void Update()
    {
        base.Update();
        if (!this.PlayerOwner)
            return;
        AdjustAmmoToMoney();
    }

    public override void OnPostFired(PlayerController player, Gun gun)
    {
        base.OnPostFired(player, gun);
        --GameManager.Instance.PrimaryPlayer.carriedConsumables.Currency;
        AdjustAmmoToMoney();
    }

    private void AdjustAmmoToMoney()
    {
        int money = GameManager.Instance.PrimaryPlayer.carriedConsumables.Currency;
        this.gun.CurrentAmmo = money;
        if (this.gun.ClipShotsRemaining > money)
            this.gun.ClipShotsRemaining = money;
    }
}

public class MidasProjectile : MonoBehaviour
{
    private const float _SHEEN_WIDTH = 20.0f;
    internal static Color _Gold      = new Color(1f,1f,0f,1f);
    internal static Color _White     = new Color(1f,1f,1f,1f);

    private void Start()
    {
        Projectile p = base.GetComponent<Projectile>();
        p.OnWillKillEnemy += this.OnWillKillEnemy;
    }

    private static tk2dSpriteCollectionData _GoldSpriteCollection = null;
    internal static Dictionary<string, int> _GoldenSprites = new();
    private void OnWillKillEnemy(Projectile bullet, SpeculativeRigidbody enemy)
    {
        if (!enemy.aiActor || !enemy.healthHaver || enemy.healthHaver.IsBoss || enemy.healthHaver.IsSubboss)
            return; // don't do anything to bosses //NOTE: technically works on most bosses, but causes problems with Dragun and who knows what modded bosses...so just disabling

        _GoldSpriteCollection ??= SpriteBuilder.ConstructCollection(new(), "goldcollection");
        GameObject g = new();
        if (!_GoldenSprites.TryGetValue(enemy.aiActor.EnemyGuid, out int spriteId))
        {
            Texture2D goldSprite = Lazy.GetTexturedEnemyIdleAnimation(enemy.aiActor, _Gold, 0.3f, _White, _SHEEN_WIDTH);
            spriteId = _GoldenSprites[enemy.aiActor.EnemyGuid] = SpriteBuilder.AddSpriteToCollection(goldSprite, _GoldSpriteCollection, "goldsprite"); //NOTE: this doesn't use PackerHelper since it's done at runtime
        }
        tk2dBaseSprite sprite               = g.AddComponent<tk2dSprite>();
            sprite.SetSprite(_GoldSpriteCollection, spriteId);
            sprite.PlaceAtPositionByAnchor(enemy.sprite.WorldCenter.ToVector3ZisY(), Anchor.MiddleCenter);
            sprite.HeightOffGround        = enemy.sprite.HeightOffGround;
            sprite.depthUsesTrimmedBounds = enemy.sprite.depthUsesTrimmedBounds;
            sprite.SortingOrder           = enemy.sprite.SortingOrder;
            sprite.renderLayer            = enemy.sprite.renderLayer;
            sprite.UpdateZDepth();
        PixelCollider pixelCollider = new PixelCollider();
            pixelCollider.CollisionLayer         = CollisionLayer.PlayerBlocker;
            pixelCollider.Enabled                = true;
            pixelCollider.IsTrigger              = false; //true;
            pixelCollider.ColliderGenerationMode = PixelCollider.PixelColliderGeneration.Manual;
            pixelCollider.ManualOffsetX          = 0;
            pixelCollider.ManualOffsetY          = 0;
            pixelCollider.ManualWidth            = Mathf.CeilToInt(C.PIXELS_PER_TILE * sprite.GetBounds().size.x);
            pixelCollider.ManualHeight           = Mathf.CeilToInt(C.PIXELS_PER_TILE * sprite.GetBounds().size.y);
        SpeculativeRigidbody s = g.AddComponent<SpeculativeRigidbody>();
            s.CanBePushed        = true;
            s.CanBeCarried       = true;
            s.CollideWithOthers  = true;
            s.CollideWithTileMap = false;
            s.TK2DSprite         = sprite;
            s.PixelColliders     = new List<PixelCollider>(1);
            s.PixelColliders.Add(pixelCollider);
            s.Initialize();
        g.AddComponent<GoldenDeath>();
        // g.GetOrAddShader(Shader.Find("Brave/ItemSpecific/LootGlintAdditivePass"))?.SetColor("_OverrideColor", Color.yellow);

        // if (enemy.aiActor.IsABoss()) // Unsure why this doesn't trigger normally, but this seems to fix it
        //     enemy.aiActor.ParentRoom.HandleRoomClearReward(); //TODO: it's possible non-boss room rewards also don't spawn if final enemy is midas'd...look into later
        LootEngine.SpawnCurrency(enemy.aiActor.CenterPosition, 1);
        enemy.aiActor.EraseFromExistenceWithRewards(true);
    }
}

public class GoldenDeath : MonoBehaviour
{
    private const float _START_EMIT    = 30.0f;
    private const float _MAX_EMIT      = 50.0f;
    private const float _MIN_EMIT      = 0.5f;
    private const float _GROW_TIME     = 0.25f;
    private const float _DECAY_TIME    = 0.5f;
    private const int   _NUM_PARTICLES = 10;
    private const float _PART_SPEED    = 2f;
    private const float _PART_SPREAD   = 0.5f;
    private const float _PART_LIFE     = 0.5f;
    private const float _PART_EMIT     = 20f;

    private float _lifetime;
    private bool _decaying;
    private tk2dSprite _sprite;

    private void Start()
    {
        this._lifetime = 0.0f;
        this._decaying = false;

        this._sprite = base.gameObject.GetComponent<tk2dSprite>();
        this._sprite.usesOverrideMaterial = true;
        this._sprite.renderer.material.shader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTiltedCutoutEmissive");
        this._sprite.renderer.material.DisableKeyword("BRIGHTNESS_CLAMP_OFF");
        this._sprite.renderer.material.EnableKeyword("BRIGHTNESS_CLAMP_ON");
        this._sprite.renderer.material.SetFloat("_EmissivePower", _lifetime);
        this._sprite.renderer.material.SetFloat("_EmissiveColorPower", 1.55f);
        this._sprite.renderer.material.SetColor("_EmissiveColor", ExtendedColours.paleYellow);

        FancyVFX.SpawnBurst(prefab: QuarterPounder._MidasParticleVFX, numToSpawn: _NUM_PARTICLES, basePosition: this._sprite.WorldCenter,
            positionVariance: _PART_SPREAD, baseVelocity: Vector2.zero, velocityVariance: _PART_SPEED, velType: FancyVFX.Vel.Radial,
            rotType: FancyVFX.Rot.Random, lifetime: _PART_LIFE, fadeOutTime: _PART_LIFE, emissivePower: _PART_EMIT, emissiveColor: Color.white);

        base.gameObject.Play("turn_to_gold");
    }

    private void Update()
    {
        float emit;
        this._sprite.UpdateZDepth();
        if (this._decaying)
        {
            if (this._lifetime >= _DECAY_TIME)
                return;
            this._lifetime = Mathf.Min(this._lifetime + BraveTime.DeltaTime, _DECAY_TIME);
            emit = _MAX_EMIT - (_MAX_EMIT - _MIN_EMIT) * (this._lifetime / _DECAY_TIME);
            this._sprite.renderer.material.SetFloat("_EmissivePower", emit);
            return;
        }
        this._lifetime = Mathf.Min(this._lifetime + BraveTime.DeltaTime, _GROW_TIME);
        emit = _START_EMIT + (_MAX_EMIT - _START_EMIT) * (this._lifetime / _GROW_TIME);
        this._sprite.renderer.material.SetFloat("_EmissivePower", emit);
        if (this._lifetime >= _GROW_TIME)
        {
            this._decaying = true;
            this._lifetime = 0.0f;
        }
    }
}
