namespace CwaffingTheGungy;

public class Missiletoe : CwaffGun
{
    public static string ItemName         = "Missiletoe";
    public static string ShortDescription = "O Tannenbomb";
    public static string LongDescription  = "Fires wrapped gifts with special attributes depending on the quality of item they contain. Reloading with a full clip while a dropped item or gun is nearby wraps it and adds it to the gun's clip. Reloading with a full clip while no items are nearby unwraps the most recently wrapped item and removes it from the gun's clip. Items and guns do not count as part of the player's normal inventory while they are wrapped.";
    public static string Lore             = "Leaving all of the gift-giving to Santa Claus during the Christmas season seems silly when modern firearm technology allows for the expedient delivery of high-velocity presents all year round. The sheer inertia with which the {ItemName} can launch presents is sure to leave a lasting impression on its lucky recipients, and while concussive force may render those recipients unable to actually enjoy those gifts, it's the thought that counts, right?";

    internal static GameObject _SparklePrefab;

    internal static GameObject _WrapVFXS;
    internal static GameObject _WrapVFXA;
    internal static GameObject _WrapVFXB;
    internal static GameObject _WrapVFXC;
    internal static GameObject _WrapVFXD;

    internal static GameObject _UnwrapVFXS;
    internal static GameObject _UnwrapVFXA;
    internal static GameObject _UnwrapVFXB;
    internal static GameObject _UnwrapVFXC;
    internal static GameObject _UnwrapVFXD;

    internal static float _WrapAnimLength;

    internal static Projectile _OrnamentProjectile;
    internal static Projectile _ExplodingOrnamentProjectile;
    internal static Projectile _GiftProjectileS;
    internal static Projectile _GiftProjectileA;
    internal static Projectile _GiftProjectileB;
    internal static Projectile _GiftProjectileC;
    internal static Projectile _GiftProjectileD;

    private const int _WRAP_FPS = 16;
    private const float _MAX_DIST = 5f;

    private static readonly List<ItemQuality> _BannedQualities = new(){
        ItemQuality.COMMON,
        ItemQuality.EXCLUDED,
        ItemQuality.SPECIAL,
    };

    private ItemQuality _lastQualityFired;

    internal List<ItemQuality> _shuffledQualities = new();

    public List<PickupObject> wrappedGifts = new();
    public List<ItemQuality> wrappedQualities = new();

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<Missiletoe>(ItemName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.A, gunClass: GunClass.CHARM, reloadTime: 1.0f, ammo: 300, canReloadNoMatterAmmo: true,
                shootFps: 45, reloadFps: 20, muzzleFrom: Items.Mailbox, fireAudio: "missiletoe_shoot_sound_1", reloadAudio: "missiletoe_reload_sound");
            gun.AddToSubShop(ModdedShopType.Boomhildr);

        gun.DefaultModule.SetAttributes(GunData.New(gun: gun, clipSize: 1, cooldown: 0.2f, shootStyle: ShootStyle.SemiAutomatic, customClip: true));

        ExplosionData defaultExplosion = GameManager.Instance.Dungeon.sharedSettingsPrefab.DefaultSmallExplosionData;
        ExplosionData giftExplosion = new ExplosionData()
        {
            forceUseThisRadius     = true,
            pushRadius             = 0.5f,
            damageRadius           = 0.5f,
            damageToPlayer         = 0f,
            doDamage               = true,
            damage                 = 10,
            doDestroyProjectiles   = false,
            doForce                = true,
            debrisForce            = 10f,
            preventPlayerForce     = true,
            explosionDelay         = 0.01f,
            usesComprehensiveDelay = false,
            doScreenShake          = false,
            playDefaultSFX         = true,
            effect                 = defaultExplosion.effect,
            ignoreList             = defaultExplosion.ignoreList,
            ss                     = defaultExplosion.ss,
        };

        tk2dSpriteAnimationClip ball        = AnimatedBullet.Create(name: "missiletoe_projectile_ball",        anchor: Anchor.MiddleLeft);
        tk2dSpriteAnimationClip gingerbread = AnimatedBullet.Create(name: "missiletoe_projectile_gingerbread", anchor: Anchor.MiddleLeft);
        tk2dSpriteAnimationClip mistletoe   = AnimatedBullet.Create(name: "missiletoe_projectile_mistletoe",   anchor: Anchor.MiddleLeft);
        tk2dSpriteAnimationClip sock        = AnimatedBullet.Create(name: "missiletoe_projectile_sock",        anchor: Anchor.MiddleLeft);
        tk2dSpriteAnimationClip star        = AnimatedBullet.Create(name: "missiletoe_projectile_star",        anchor: Anchor.MiddleLeft);
        tk2dSpriteAnimationClip wreath      = AnimatedBullet.Create(name: "missiletoe_projectile_wreath",      anchor: Anchor.MiddleLeft);

        _OrnamentProjectile = Items._38Special.CloneProjectile(
          ).AddAnimations(ball, gingerbread, mistletoe, sock, star, wreath
          ).Attach<GlowyChristmasProjectileBehavior>();
        _ExplodingOrnamentProjectile = Items._38Special.CloneProjectile(
          ).AddAnimations(ball, gingerbread, mistletoe, sock, star, wreath
          ).Attach<GlowyChristmasProjectileBehavior>(glow => glow.Glow(40)
          ).Attach<ExplosiveModifier>(e => e.explosionData = giftExplosion);
        _GiftProjectileS = SetupProjectile(gun: gun, name: "gift_projectile_black", damage: 30f, speed: 30f, force: 30f
            ).Attach<ExplosiveModifier>(e => e.explosionData = giftExplosion
            ).Attach<SpawnProjModifier>(s => {
              s.spawnProjectilesOnCollision  = true;
              s.numberToSpawnOnCollison      = 9;
              s.startAngle                   = 180;
              s.projectileToSpawnOnCollision = _ExplodingOrnamentProjectile;
              s.collisionSpawnStyle          = SpawnProjModifier.CollisionSpawnStyle.RADIAL;
            });
        _GiftProjectileA = SetupProjectile(gun: gun, name: "gift_projectile_red",   damage: 25f, speed: 30f, force: 25f
            ).Attach<ExplosiveModifier>(e => e.explosionData = Bouncer._MiniExplosion
            ).Attach<SpawnProjModifier>(s => {
              s.spawnProjectilesOnCollision  = true;
              s.numberToSpawnOnCollison      = 7;
              s.startAngle                   = 180;
              s.projectileToSpawnOnCollision = _OrnamentProjectile;
              s.collisionSpawnStyle          = SpawnProjModifier.CollisionSpawnStyle.RADIAL;
            });
        _GiftProjectileB = SetupProjectile(gun: gun, name: "gift_projectile_green", damage: 20f, speed: 25f, force: 20f
            ).Attach<SpawnProjModifier>(s => {
              s.spawnProjectilesOnCollision  = true;
              s.numberToSpawnOnCollison      = 5;
              s.startAngle                   = 180;
              s.projectileToSpawnOnCollision = _OrnamentProjectile;
              s.collisionSpawnStyle          = SpawnProjModifier.CollisionSpawnStyle.RADIAL;
            });
        _GiftProjectileC = SetupProjectile(gun: gun, name: "gift_projectile_blue",  damage: 15f, speed: 25f, force: 15f
            ).Attach<SpawnProjModifier>(s => {
              s.spawnProjectilesOnCollision  = true;
              s.numberToSpawnOnCollison      = 2;
              s.startAngle                   = 180;
              s.projectileToSpawnOnCollision = _OrnamentProjectile;
              s.collisionSpawnStyle          = SpawnProjModifier.CollisionSpawnStyle.FLAK_BURST;
            });
        _GiftProjectileD = SetupProjectile(gun: gun, name: "gift_projectile_brown", damage: 10f, speed: 25f, force: 10f);

        _WrapVFXS       = SetupVFX("black_gift_wrap");
        _WrapVFXA       = SetupVFX("red_gift_wrap");
        _WrapVFXB       = SetupVFX("green_gift_wrap");
        _WrapVFXC       = SetupVFX("blue_gift_wrap");
        _WrapVFXD       = SetupVFX("brown_gift_wrap");
        _UnwrapVFXS     = SetupVFX("black_gift_unwrap");
        _UnwrapVFXA     = SetupVFX("red_gift_unwrap");
        _UnwrapVFXB     = SetupVFX("green_gift_unwrap");
        _UnwrapVFXC     = SetupVFX("blue_gift_unwrap");
        _UnwrapVFXD     = SetupVFX("brown_gift_unwrap");
        _WrapAnimLength = _WrapVFXB.GetComponent<tk2dSpriteAnimator>().DefaultClip.BaseClipLength;
        _SparklePrefab  = VFX.Create("missiletoe_sparkles", fps: 8, scale: 0.75f, loops: false, anchor: Anchor.MiddleCenter);
    }

    private static GameObject SetupVFX(string name)
    {
        return VFX.Create(name, fps: _WRAP_FPS, loops: false, anchor: Anchor.LowerCenter, scale: 0.75f, persist: true);
    }

    private static Projectile SetupProjectile(Gun gun, string name, float damage, float speed, float force)
    {
        Projectile projectile = gun.CloneProjectile(GunData.New(damage: damage, speed: speed, range: 50.0f, force: force));
            projectile.AddDefaultAnimation(AnimatedBullet.Create(name: name, fps: 1, scale: 0.5f, anchor: Anchor.MiddleLeft));
            projectile.transform.parent       = gun.barrelOffset;
            projectile.shouldFlipHorizontally = true;
            projectile.shouldFlipVertically   = false;
            projectile.shouldRotate           = false;
            projectile.onDestroyEventName = "gift_impact_sound";
            projectile.gameObject.AddComponent<ChristmasSparkleDoer>();
        return projectile;
    }

    public override Projectile OnPreFireProjectileModifier(Gun gun, Projectile projectile, ProjectileModule mod)
    {
        ItemQuality quality;
        if (mod.ammoCost == 0)
            quality = this._lastQualityFired;
        else
            quality = this._lastQualityFired = _shuffledQualities[mod.numberOfShotsInClip - gun.ClipShotsRemaining];
        switch (quality)
        {
            case ItemQuality.S: return _GiftProjectileS;
            case ItemQuality.A: return _GiftProjectileA;
            case ItemQuality.B: return _GiftProjectileB;
            case ItemQuality.C: return _GiftProjectileC;
            case ItemQuality.D: return _GiftProjectileD;
            default                        : return _GiftProjectileD;
        }
    }

    public static GameObject GetGiftVFX(ItemQuality quality, bool wrap)
    {
        switch (quality)
        {
            case ItemQuality.S: return wrap ? _WrapVFXS : _UnwrapVFXS;
            case ItemQuality.A: return wrap ? _WrapVFXA : _UnwrapVFXA;
            case ItemQuality.B: return wrap ? _WrapVFXB : _UnwrapVFXB;
            case ItemQuality.C: return wrap ? _WrapVFXC : _UnwrapVFXC;
            case ItemQuality.D: return wrap ? _WrapVFXD : _UnwrapVFXD;
            default                        : return wrap ? _WrapVFXD : _UnwrapVFXD;
        }
    }

    protected override void OnPickup(GameActor owner)
    {
        RecalculateClip();
        base.OnPickup(owner);
    }

    public override void OnReloadPressed(PlayerController player, Gun gun, bool manualReload)
    {
        if (manualReload && gun.DefaultModule.numberOfShotsInClip == gun.ClipShotsRemaining)
            WrapPresent();
        else
            RecalculateClip();
        base.OnReloadPressed(player, gun, manualReload);
    }

    public override void OnAmmoChangedSafe(PlayerController player, Gun gun)
    {
        base.OnAmmoChangedSafe(player, gun);
        RecalculateClip();  // fixings a bug where clip size resets to 1 when picking up ammo
    }

    public override void OnSwitchedToThisGun()
    {
        base.OnSwitchedToThisGun();
        RecalculateClip();
    }

    private void WrapPresent()
    {
        PickupObject nearestPickup = null;
        float nearestDist = _MAX_DIST;
        foreach (DebrisObject debris in StaticReferenceManager.AllDebris)
        {
            if (!debris.IsPickupObject)
                continue;
            if (debris.GetComponentInChildren<PickupObject>() is not PickupObject pickup)
                continue;
            if (pickup.IsBeingSold)
                continue;
            if (_BannedQualities.Contains(pickup.quality))
                continue;

            float pickupDist = (debris.sprite.WorldCenter - this.Owner.sprite.WorldCenter).magnitude;
            if (pickupDist >= nearestDist)
                continue;

            nearestPickup = pickup;
            nearestDist   = pickupDist;
        }
        if (!nearestPickup)
        {
            UnwrapPresent();
            return;
        }

        WrappableGift.Spawn(this, this.gun.barrelOffset.position, nearestPickup, unwrapping: false);
    }

    internal void RecalculateClip()
    {
        wrappedQualities.Add(ItemQuality.D);  // make sure our list has at least one item
        _shuffledQualities = wrappedQualities.CopyAndShuffle();
        wrappedQualities.Pop();
        this.gun.DefaultModule.numberOfShotsInClip = _shuffledQualities.Count();
    }

    private void UnwrapPresent()
    {
        if (wrappedGifts.Count() == 0)
            return;
        PickupObject gift = wrappedGifts.Pop();
        wrappedQualities.Pop();
        RecalculateClip();
        WrappableGift.Spawn(this, this.gun.barrelOffset.position, gift, unwrapping: true);
    }

    public override void MidGameSerialize(List<object> data, int i)
    {
        base.MidGameSerialize(data, i);
        data.Add(wrappedGifts.Count);
        foreach(PickupObject pickup in wrappedGifts)
        {
            data.Add(pickup.PickupObjectId);
            if (pickup.GetComponent<PlayerItem>() is PlayerItem active)
                data.Add(new MidGameActiveItemData(active));
            else if (pickup.GetComponent<PassiveItem>() is PassiveItem passive)
                data.Add(new MidGamePassiveItemData(passive));
            else if (pickup.GetComponent<Gun>() is Gun gun)
                data.Add(new MidGameGunData(gun));
            else
                ETGModConsole.Log($"  SERIALIZING SOMETHING THAT ISN'T A GUN, ACTIVE, OR PASSIVE, TELL PRETZEL");
        }
    }

    public override void MidGameDeserialize(List<object> data, ref int i)
    {
        base.MidGameDeserialize(data, ref i);
        wrappedGifts.Clear();
        int numGifts = (int)data[i++];
        for (int n = 0; n < numGifts; ++n)
        {
            int pickupId = (int)data[i++];
            PickupObject pickup = UnityEngine.Object.Instantiate(
                PickupObjectDatabase.GetById(pickupId).gameObject).GetComponent<PickupObject>();

            if (pickup.GetComponent<PlayerItem>() is PlayerItem active)
            {
                MidGameActiveItemData itemData = (MidGameActiveItemData)data[i++];
                active.CurrentDamageCooldown = itemData.DamageCooldown;
                active.CurrentRoomCooldown = itemData.RoomCooldown;
                active.CurrentTimeCooldown = itemData.TimeCooldown;
                active.numberOfUses = itemData.NumberOfUses;
                active.MidGameDeserialize(itemData.SerializedData);
                active.GetRidOfMinimapIcon();
                active.m_pickedUp = true;
            }
            else if (pickup.GetComponent<PassiveItem>() is PassiveItem passive)
            {
                MidGamePassiveItemData itemData = (MidGamePassiveItemData)data[i++];
                passive.MidGameDeserialize(itemData.SerializedData);
                passive.GetRidOfMinimapIcon();
                passive.m_pickedUp = true;
            }
            else if (pickup.GetComponent<Gun>() is Gun gun)
            {
                MidGameGunData itemData = (MidGameGunData)data[i++];
                gun.MidGameDeserialize(itemData.SerializedData);
                for (int k = 0; k < itemData.DuctTapedGunIDs.Count; k++)
                {
                    if (PickupObjectDatabase.GetById(itemData.DuctTapedGunIDs[k]) is Gun ductTapeGun)
                        DuctTapeItem.DuctTapeGuns(ductTapeGun, gun);
                }
                gun.CurrentAmmo = itemData.CurrentAmmo;
            }

            pickup.renderer.enabled = false;
            pickup.m_isBeingEyedByRat = false;

            wrappedGifts.Add(pickup);
            wrappedQualities.Add(pickup.quality);
        }
        RecalculateClip();
    }
}

public class GlowyChristmasProjectileBehavior : MonoBehaviour
{
    public float glow = 0f;

    private void Start()
    {
        Projectile proj = base.GetComponent<Projectile>();
        proj.sprite.spriteAnimator.Play(proj.sprite.spriteAnimator.Library.clips.ChooseRandom());
        if (this.glow > 0f)
            proj.sprite.SetGlowiness(glowAmount: this.glow, glowColor: Color.white);
    }

    public void Glow(float amount)
    {
        this.glow = amount;
    }
}

public class ChristmasSparkleDoer : MonoBehaviour
{
    private const float _SPARKLE_TIME = 0.03f;
    private const float _SPARKLE_LIFE = 0.45f;
    private const float _SPARKLE_FADE = 0.25f;
    private const float _PART_EMIT = 5f;

    private Projectile _projectile;
    private PlayerController _owner;
    private float _lifetime = 0.0f;

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._owner = this._projectile.Owner as PlayerController;
    }

    private void Update()
    {
        this._lifetime += BraveTime.DeltaTime;
        if (this._lifetime < _SPARKLE_TIME)
            return;

        this._lifetime -= _SPARKLE_TIME;
        SpawnManager.SpawnVFX(Missiletoe._SparklePrefab, this._projectile.sprite.WorldCenter, Lazy.RandomEulerZ())
            .ExpireIn(_SPARKLE_LIFE, _SPARKLE_FADE, shrink: false);
    }
}

public class WrappableGift : MonoBehaviour
{
    private const float _GROW_TIME      = 0.5f; // amount of time it takes for our present to grow in
    private const float _MIN_SCALE      = 0.4f; // minimum scale our pickup can shrink down to
    private const float _VANISH_PERCENT = 0.5f; // percent of the way through the wrap animation the pickup should vanish

    private static readonly Vector2 _EXTRA_OFFSET = new Vector2(0f, 0.75f); // make the pickup enter near the center of the present

    private FancyVFX _vfx;
    private tk2dBaseSprite _sprite;
    private tk2dSpriteAnimator _animator;
    private Vector3 _position;
    private PickupObject _pickup;
    private Missiletoe _gun;
    private bool _wasEyedByRat = false;

    public static WrappableGift Spawn(Missiletoe gun, Vector3 position, PickupObject pickup, bool unwrapping)
    {
        GameObject go = UnityEngine.Object.Instantiate(new GameObject(), position, Quaternion.identity);
        WrappableGift gift = go.AddComponent<WrappableGift>();
        gift.Setup(gun, position, pickup, unwrapping);
        return gift;
    }

    public void Setup(Missiletoe gun, Vector3 position, PickupObject pickup, bool unwrapping)
    {
        this._gun      = gun;
        this._position = position;
        this._pickup   = pickup;
        this._vfx      = FancyVFX.Spawn(Missiletoe.GetGiftVFX(pickup.quality, !unwrapping), this._position, Quaternion.identity,
            velocity: Vector2.zero, lifetime: Missiletoe._WrapAnimLength + 0.5f, fadeOutTime: 0.25f);
        this._sprite   = this._vfx.sprite;
        this._animator = this._sprite.spriteAnimator;

        StartCoroutine(WrapItUp(unwrapping));
    }

    private IEnumerator WrapItUp(bool unwrapping)
    {
        // Set up some useful variables
        bool wrapping          = !unwrapping;
        bool isGun             = this._pickup.GetComponent<Gun>() is Gun gun;
        Vector2 targetPosition = this._position.XY() + _EXTRA_OFFSET;
        float animLength       = Missiletoe._WrapAnimLength;

        // Create a VFX object for the pickup
        FancyVFX pickupvfx = null;
        if (wrapping)
            pickupvfx = FancyVFX.FromCurrentFrame(this._pickup.sprite);

        // Clone and destroy the pickup itself (logic is largely from Pickup() methods without actually picking items up)
        if (wrapping)
        {
            this._gun.wrappedQualities.Add(this._pickup.quality);
            if (isGun)
            {
                PickupObject oldPickup = this._pickup;
                this._pickup = UnityEngine.Object.Instantiate(oldPickup);
                // Duct-taped guns aren't actually serialized, so we need to do this manually
                this._pickup.GetComponent<Gun>().DuctTapeMergedGunIDs = oldPickup.GetComponent<Gun>().DuctTapeMergedGunIDs;
                if (oldPickup.transform.parent != null)
                    UnityEngine.Object.Destroy(oldPickup.transform.parent?.gameObject);
                else
                    UnityEngine.Object.Destroy(oldPickup);
            }
            else
            {
                if (this._pickup.GetComponent<PlayerItem>() is PlayerItem active)
                {
                    active.GetRidOfMinimapIcon();
                    active.m_pickedUp = true;
                }
                else if (this._pickup.GetComponent<PassiveItem>() is PassiveItem passive)
                {
                    passive.GetRidOfMinimapIcon();
                    passive.m_pickedUp = true;
                }
                SpriteOutlineManager.RemoveOutlineFromSprite(this._pickup.sprite, true);
                this._pickup.renderer.enabled = false;
                this._wasEyedByRat = this._pickup.m_isBeingEyedByRat;
                this._pickup.m_isBeingEyedByRat = false;
                if (this._pickup.gameObject.GetComponent<DebrisObject>() is DebrisObject debris)
                    UnityEngine.Object.Destroy(debris);
                if (this._pickup.gameObject.GetComponent<SquishyBounceWiggler>() is SquishyBounceWiggler squish)
                    UnityEngine.Object.Destroy(squish);
            }
            DontDestroyOnLoad(this._pickup.gameObject); // needed for persisting between floors
            this._gun.wrappedGifts.Add(this._pickup);
            this._gun.RecalculateClip();
        }

        // Pause the gift's default animation and let it grow into existence first
        this._vfx.gameObject.SetAlphaImmediate(0f); // make sure we start invisible to avoid first-frame glitches
        yield return null;
        this._vfx.gameObject.SetAlpha(1f);
        this._animator.StopAndResetFrame();
        base.gameObject.Play("present_create_sound");
        for (float elapsed = 0f; elapsed < _GROW_TIME; elapsed += BraveTime.DeltaTime)
        {
            float percentDone = elapsed / _GROW_TIME;
            this._sprite.transform.localScale = new Vector3(percentDone, percentDone, 1.0f);
            yield return null;
        }
        this._animator.Play();
        base.gameObject.Play(wrapping ? "present_wrap_sound" : "present_unwrap_sound");

        // If we're wrapping the pickup, make it magically hover over to the present
        if (wrapping)
        {
            pickupvfx.ArcTowards(
              animLength: animLength, targetSprite: this._vfx.sprite, useBottom: true, minScale: _MIN_SCALE, vanishPercent: _VANISH_PERCENT);
            yield break;
        }

        // Otherwise, for the appropriate point in the animation, then drop the original pickup
        yield return new WaitForSeconds(animLength * (1f - _VANISH_PERCENT));
        Vector2 trueTarget = targetPosition - this._pickup.sprite.GetRelativePositionFromAnchor(Anchor.LowerCenter);
        if (isGun)
            trueTarget += _EXTRA_OFFSET; // guns are weirdly offset for some reason
        LootEngine.DropItemWithoutInstantiating(this._pickup.gameObject, trueTarget, Vector2.zero, 0f, true, false, true);
        if (this._pickup.GetComponent<PlayerItem>() is PlayerItem active2)
        {
            active2.RegisterMinimapIcon();
            active2.m_pickedUp = false;
        }
        else if (this._pickup.GetComponent<PassiveItem>() is PassiveItem passive2)
        {
            passive2.RegisterMinimapIcon();
            passive2.m_pickedUp = false;
        }
        this._pickup.m_isBeingEyedByRat = this._wasEyedByRat;
    }
}
