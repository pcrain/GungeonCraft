namespace CwaffingTheGungy;

public class KingsLaw : CwaffGun
{
    public static string ItemName         = "King's Law";
    public static string ShortDescription = "Accept Your Fate";
    public static string LongDescription  = "Conjures projectiles that hover behind the player as long as the fire button is held. Projectiles are launched with a short delay when fire is released, when reloading, or when changing weapons.";
    public static string Lore             = "The trusty weapon of an ancient sorcerer-king who ruled Gunymede with an iron fist. Imbued with only a small fraction of his power and weakened further by the passage of time, this gun still resonates with enough destructive magic to wipe out hordes of lesser Gundead in the blink of an eye.";

    internal const float _ANGLE_GAP    = 20.0f;
    internal const float _MAG_GAP      = 0.75f;
    internal const float _MIN_MAG      = 3.0f;
    internal const float _MAX_SPREAD   = 45.0f;
    internal const int   _MAX_BULLETS  = 400;
    internal const float _SPAWN_RATE   = 0.1f;
    internal const float _RUNE_ROT_MID = 59.0f;

    internal static List<Vector3> _OffsetAnglesMagsAndRings = new(_MAX_BULLETS);
    internal static GameObject _RuneLarge;
    internal static GameObject _RuneSmall;
    internal static GameObject _RuneMuzzle;

    private int        _nextIndex        = 0;
    private int        _curBatch        = 0;
    private GameObject _extantMuzzleRune = null;
    private float      _muzzleRuneAlpha  = 0.0f;

    public static void Init()
    {
        Lazy.SetupGun<KingsLaw>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.A, gunClass: GunClass.CHARGE, reloadTime: 0.75f, ammo: 1000, doesScreenShake: false,
            reloadAudio: "knife_gun_reload", attacksThroughWalls: true)
          .AddToShop(ModdedShopType.TimeTrader)
          .InitProjectile(GunData.New(clipSize: 20, shootStyle: ShootStyle.Automatic, damage: 7.5f, speed: 40.0f, range: 999999f, force: 9f, customClip: true,
            cooldown: _SPAWN_RATE, sprite: "kings_law_projectile", fps: 12, scale: 0.5f, anchor: Anchor.MiddleCenter, hitSound: "knife_gun_hit",
            useDummyChargeModule: true, shouldRotate: false))
          .Attach<KingsLawBullets>();

        // Projectiles should spawn in semi-circles around some offset point behind the player, filling in each
        //   semi-ring from the outside inward (the math checks out I promise)
        int   ring            = 0;
        int   ringIndex       = 0;
        float angle           = 0f;
        float mag             = 0f;
        float maxAngleForRing = 0f;
        float gapAngleForRing = 0f;
        for (int i = 0; i < _MAX_BULLETS; ++i)
        {
            if (ringIndex > ring) // build up the sides
            {
                angle = maxAngleForRing;
                mag   = _MIN_MAG + _MAG_GAP * (ring * 2 - ringIndex);
            }
            else // build up the back
            {
                angle = gapAngleForRing * ringIndex;
                mag   = _MIN_MAG + _MAG_GAP * ring;
            }
            _OffsetAnglesMagsAndRings.Add(new Vector3(angle, mag, ringIndex));
            if (ringIndex > 0)
            {
                _OffsetAnglesMagsAndRings.Add(new Vector3(-angle, mag, ringIndex));
                ++i;
                --ringIndex;
            }
            else
            {
                ring            = ring + 1;
                ringIndex       = ring;
                maxAngleForRing = _MAX_SPREAD * ((float)ring / (float)(ring + 1));
                gapAngleForRing = maxAngleForRing / ring;
            }
        }

        _RuneLarge  = VFX.Create("law_rune_large", fps: 2);
        _RuneSmall  = VFX.Create("law_rune_small", fps: 2);
        _RuneMuzzle = VFX.Create("muzzle_kings_law", fps: 10);
    }

    public override void OnPostFired(PlayerController player, Gun gun)
    {
        base.OnPostFired(player, gun);
        base.gameObject.Play("snd_undynedis");
    }

    public override void OnReloadPressed(PlayerController player, Gun gun, bool manualReload)
    {
        base.OnReloadPressed(player, gun, manualReload);
        Reset();
    }

    public override void OnSwitchedToThisGun()
    {
        base.OnSwitchedToThisGun();
        Reset();
        this._muzzleRuneAlpha = 0f;
        if (this._extantMuzzleRune)
            this._extantMuzzleRune.SetAlphaImmediate(0f);
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        base.OnDroppedByPlayer(player);
        Reset();
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        base.OnSwitchedAwayFromThisGun();
        Reset();
    }

    private void Reset(bool clearVfx = true)
    {
        if (clearVfx)
        {
            if (this._extantMuzzleRune)
                this._extantMuzzleRune.SetAlpha(0f);
            this._muzzleRuneAlpha = 0f;
        }
        if (this._nextIndex == 0)
            return;

        this._nextIndex = 0;
        ++this._curBatch;
    }

    public int GetNextIndex()
    {
        return this._nextIndex++;
    }

    public int GetBatch()
    {
        return this._curBatch;
    }

    public override void Update()
    {
        base.Update();
        if (BraveTime.DeltaTime == 0.0f)
            return;
        if (this.PlayerOwner is not PlayerController)
            return;

        if (this._extantMuzzleRune == null)
        {
            this._extantMuzzleRune = SpawnManager.SpawnVFX(KingsLaw._RuneMuzzle, this.gun.barrelOffset.transform.position, Quaternion.identity);
            this._extantMuzzleRune.SetAlphaImmediate(0.0f);
            this._extantMuzzleRune.transform.parent = this.gun.barrelOffset;
        }

        if (this.gun.IsCharging)
        {
            this._muzzleRuneAlpha = Mathf.Min(1f, this._muzzleRuneAlpha + 2f * BraveTime.DeltaTime);
            this._extantMuzzleRune.SetAlpha(Mathf.Clamp01(this._muzzleRuneAlpha));
            this._extantMuzzleRune.transform.localRotation = (-_RUNE_ROT_MID * BraveTime.ScaledTimeSinceStartup).EulerZ();
            return;
        }

        if (!this.gun.IsReloading && this.gun.ClipShotsRemaining < Mathf.Min(this.gun.ClipCapacity, this.gun.CurrentAmmo))
            this.gun.Reload(); // force reload while we're not at max clip capacity

        this._muzzleRuneAlpha = Mathf.Max(0f, this._muzzleRuneAlpha - 4f * BraveTime.DeltaTime);
        this._extantMuzzleRune.SetAlpha(Mathf.Clamp01(this._muzzleRuneAlpha));

        Reset(clearVfx: false);
        // Synchronize ammo clips between projectile modules as necessary
        // (don't do while charging or bullets will all be forcibly released)
        this.gun.SynchronizeReloadAcrossAllModules();
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        if (this.PlayerOwner && projectile.gameObject.GetComponent<KingsLawBullets>() is KingsLawBullets klb)
            klb.Setup(this, this.Mastered);
    }
}

public class KingsLawBullets : MonoBehaviour
{
    private const float _TIME_BEFORE_STASIS     = 0.2f;
    private const float _GLOW_TIME              = 0.5f;
    private const float _GLOW_MAX               = 10f;
    private const float _LAUNCH_DELAY           = 0.04f;
    private const float _LAUNCH_SPEED           = 50f;
    private const float _RUNE_ALPHA             = 0.30f;
    private const float _RUNE_ROT_FAST          = 69.0f;
    private const float _RUNE_ROT_SLOW          = 47.0f;

    private PlayerController _owner                 = null;
    private Projectile       _projectile            = null;
    private bool             _launchSequenceStarted = false;
    private bool             _wasEverInStasis       = false;
    private GameObject       _runeLarge             = null;
    private GameObject       _runeSmall             = null;
    private float            _offsetAngle           = 0.0f;
    private float            _offsetMag             = 0.0f;
    private float            _offsetRing            = 0.0f;
    private int              _index                 = 0;
    private int              _batch                 = 0;
    private bool             _naturalSpawn          = false;
    private KingsLaw         _king                  = null;
    private Gun              _gun                   = null;
    private bool             _mastered              = false;

    public void Setup(KingsLaw king, bool mastered)
    {
        this._index        = king.GetNextIndex();
        this._batch        = king.GetBatch();
        this._naturalSpawn = true;
        this._king         = king;
        this._gun          = king.gameObject.GetComponent<Gun>();
        this._mastered     = mastered;
    }

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._owner      = this._projectile.Owner as PlayerController;

        if (!this._owner)
            return; // shouldn't happen, but just be safe

        Vector3 baseOffset = KingsLaw._OffsetAnglesMagsAndRings[this._index % KingsLaw._MAX_BULLETS];
        // offset angle can be reduced by at most half of the max depending on the player's accuracy
        this._offsetAngle  = baseOffset.x * Mathf.Min(1.5f, 0.5f + 0.5f * this._owner.AccuracyMult());
        this._offsetMag    = baseOffset.y;
        this._offsetRing   = baseOffset.z;

        this._projectile.specRigidbody.OnPreRigidbodyCollision += SkipCorpseAndDoorCollisions;
        if (this._mastered)
        {
            this._projectile.PenetratesInternalWalls = true;
            this._projectile.specRigidbody.OnPreTileCollision += this._projectile.OnPreTileCollision;
        }

        StartCoroutine(TheLaw());
    }

    private void OnDestroy()
    {
        this._runeLarge.SafeDestroy();
        this._runeSmall.SafeDestroy();
    }

    private void SkipCorpseAndDoorCollisions(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
    {
        if (otherRigidbody.GetComponent<AIActor>() is AIActor actor && (!actor.healthHaver || actor.healthHaver.IsDead))
            PhysicsEngine.SkipCollision = true;
        else if (otherRigidbody.transform.parent && otherRigidbody.transform.parent.GetComponent<DungeonDoorController>() is DungeonDoorController door)
            if (!this._projectile.specRigidbody.CollideWithTileMap)
                PhysicsEngine.SkipCollision = true; // skip door collisions when not coliding with tile map
    }

    private IEnumerator TheLaw()
    {
        // Phase 1 / 5 -- the initial fire
        this._projectile.SetSpeed(0.01f);
        this._projectile.specRigidbody.CollideWithTileMap = false;
        this._projectile.specRigidbody.CollideWithOthers = false;
        this._projectile.specRigidbody.Reinitialize();
        this._runeLarge = SpawnManager.SpawnVFX(KingsLaw._RuneLarge, this._projectile.transform.position, Quaternion.identity);
            this._runeLarge.SetAlphaImmediate(_RUNE_ALPHA);
            this._runeLarge.transform.parent = this._projectile.transform;
        this._runeSmall = SpawnManager.SpawnVFX(KingsLaw._RuneSmall, this._projectile.transform.position, Quaternion.identity);
            this._runeSmall.SetAlphaImmediate(_RUNE_ALPHA);
            this._runeSmall.transform.parent = this._projectile.transform;

        float earlyRelease = 0f;
        float targetAngle                = this._owner.m_currentGunAngle;
        Vector2 originalPlayerPosition   = this._owner.CurrentGun.barrelOffset.PositionVector2();
        Vector2 ownerCenter              = this._owner.CenterPosition;
        for (float elapsed = 0f; elapsed < _TIME_BEFORE_STASIS; elapsed += BraveTime.DeltaTime)
        {
            if (this._naturalSpawn && earlyRelease == 0f)
                if (!this._gun || !this._gun.IsCharging || !this._king || this._batch != this._king.GetBatch())
                    earlyRelease = _TIME_BEFORE_STASIS - elapsed;
            if (earlyRelease == 0f)
            {
                ownerCenter = this._owner.CenterPosition;
                targetAngle = this._owner.m_currentGunAngle;
                originalPlayerPosition = this._owner.CurrentGun.barrelOffset.PositionVector2();
            }
            float percentLeft        = 1f - elapsed / _TIME_BEFORE_STASIS;
            float cubicEase          = 1f - (percentLeft * percentLeft * percentLeft);
            Vector2 offset           = (targetAngle + 180f + this._offsetAngle).Clamp360().ToVector(this._offsetMag);
            Vector2 targetPosition   = Vector2.Lerp(originalPlayerPosition, ownerCenter + offset, cubicEase);
            this._projectile.specRigidbody.Position = new Position(targetPosition);
            this._projectile.SendInDirection(targetAngle.ToVector(), resetDistance: true);
            this._projectile.transform.localRotation = targetAngle.EulerZ();
            this._projectile.specRigidbody.UpdateColliderPositions();
            this._runeLarge.transform.localRotation = (_RUNE_ROT_FAST * BraveTime.ScaledTimeSinceStartup).EulerZ();
            this._runeSmall.transform.localRotation = (-_RUNE_ROT_SLOW * BraveTime.ScaledTimeSinceStartup).EulerZ();
            yield return null;
        }

        // Phase 2 / 5 -- the freeze (skipped if the projectile didn't spawn with King's Law)
        while (this._naturalSpawn && this._owner && this._gun && this._gun.IsCharging)
        {
            if (!this._king || this._batch != this._king.GetBatch())
                break; // King's Law has disappeared or moved onto a new batch, so launch our current projectiles

            ownerCenter = this._owner.CenterPosition;
            targetAngle = this._owner.m_currentGunAngle;
            originalPlayerPosition = this._owner.CurrentGun.barrelOffset.PositionVector2() + targetAngle.ToVector(2f);

            float behindPlayerAngle = targetAngle + 180f;
            Vector2 offset          = (behindPlayerAngle + this._offsetAngle).Clamp360().ToVector(this._offsetMag);
            this._projectile.specRigidbody.Position = new Position(ownerCenter + offset);
            this._projectile.SendInDirection(targetAngle.ToVector(), resetDistance: true);
            this._projectile.transform.localRotation = targetAngle.EulerZ();
            this._projectile.specRigidbody.UpdateColliderPositions();
            this._runeLarge.transform.localRotation = (_RUNE_ROT_FAST * BraveTime.ScaledTimeSinceStartup).EulerZ();
            this._runeSmall.transform.localRotation = (-_RUNE_ROT_SLOW * BraveTime.ScaledTimeSinceStartup).EulerZ();
            yield return null;
        }

        // Phase 3 / 5 -- the glow
        Vector2 targetDir                = targetAngle.ToVector();
        this._runeLarge.transform.parent = null;
        this._runeSmall.transform.parent = null;
        this._projectile.sprite.SetGlowiness(glowAmount: 0f, glowColor: Color.cyan);
        Material m = this._projectile.sprite.renderer.material;
        this._projectile.gameObject.PlayUnique("knife_gun_glow");
        float moveTimer = _GLOW_TIME - earlyRelease;
        while (moveTimer > 0)
        {
            float runeAlpha = _RUNE_ALPHA * moveTimer / _GLOW_TIME;
            this._runeLarge.SetAlpha(runeAlpha);
            this._runeSmall.SetAlpha(runeAlpha);
            float glowAmount = (_GLOW_TIME - moveTimer) / _GLOW_TIME;
            m.SetFloat(CwaffVFX._EmissivePowerId, glowAmount * _GLOW_MAX);
            moveTimer -= BraveTime.DeltaTime;
            yield return null;
        }
        this._runeLarge.SetAlpha(0f);
        this._runeSmall.SetAlpha(0f);

        // Phase 4 / 5 -- the launch queue
        moveTimer = _LAUNCH_DELAY * this._offsetRing + C.FRAME * (this._index - this._offsetRing * this._offsetRing);
        while (moveTimer > 0)
        {
            moveTimer -= BraveTime.DeltaTime;
            yield return null;
        }

        // Phase 5 / 5 -- the launch
        this._projectile.SetSpeed(_LAUNCH_SPEED * (this._owner ? this._owner.ProjSpeedMult() : 1f));
        this._projectile.specRigidbody.CollideWithOthers = true;
        this._projectile.specRigidbody.Reinitialize();
        _projectile.SendInDirection(targetDir, true);
        this._projectile.gameObject.Play("knife_gun_launch");
        if (this._mastered)
        {
            this._projectile.shouldRotate = true;
            HomingModifier homing = this._projectile.gameObject.AddComponent<HomingModifier>();
            homing.AngularVelocity = Mathf.Max(homing.AngularVelocity, 360f);
            homing.HomingRadius    = Mathf.Max(homing.HomingRadius, 36f);
        }

        // Post-launch: wait for the projectiles to pass the player's original point at their launch, then re-enable tile collision
        float ignoreTileTime = 2.0f; // don't ignore collisions for more than 2 seconds regardless of what happens
        while (this._owner && this._projectile && this._projectile.isActiveAndEnabled)
        {
            if ((ignoreTileTime -= BraveTime.DeltaTime) <= 0)
                break;
            float angleToPlayerOriginalPosition = (this._projectile.transform.position.XY() - originalPlayerPosition).ToAngle();
            if (angleToPlayerOriginalPosition.IsNearAngle(targetAngle, 90f))
                break;
            yield return null;
        }
        this._projectile.specRigidbody.CollideWithTileMap = true;

        yield break;
    }
}
