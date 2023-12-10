namespace CwaffingTheGungy;

public class VacuumCleaner : AdvancedGunBehavior
{
    public static string ItemName         = "Vacuum Cleaner";
    public static string SpriteName       = "vacuum_cleaner";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "Lean Mean Cleaning Machine";
    public static string LongDescription  = "Cleans up debris lying around the Gungeon. Each piece of debris vacuumed has a 1% chance to restore 1% of a random gun's ammo.";
    public static string Lore             = "Over time, the Gungeon naturally accrues a substantial amount of shrapnel, corpses, and other garbage as Gungeoneers fight their way through hordes of Gundead. The Gungeon's relatively pristine state as each new adventurer begins their descent is thanks largely to the Gungeon Janitorial Crew, whose work largely goes unnoticed and unthanked. Observing how adventurers had a penchant for using guns with flashy particle effects, one cunning janitor modified a few vacuum cleaners to electrify the latent argon in the Gungeon, creating some fancy green eddies in the air as the vacuums are running. The janitor stuffed a few of these modified vacuums in chests, hoping adventurers would be distracted enough by the particles to not notice the complete lack of damage as they unwittingly cleaned the dungeon and made the GJC's lives a little easier.";

    internal static GameObject _VacuumVFX = null;

    internal const float _REACH       =  8.00f; // how far (in tiles) the gun reaches
    internal const float _SPREAD      =    10f; // width (in degrees) of how wide our cone of suction is at the end of our reach
    internal const float _BEG_WIDTH   =  0.40f; // width (in tiles) of cone of suction at the beginning of the gun's muzzle
    internal const float _END_WIDTH   =  1.00f; // width (in tiles) of cone of suction at the end of the gun's range
    internal const float _ACCEL_SEC   =  1.80f; // speed (in tiles per second) at which debris accelerates towards the gun near the end of the gun's reach
    internal const float _UPDATE_RATE =   0.1f; // amount of time between debris checks / updates
    internal const float _AMMO_CHANCE =  0.01f; // percent chance debris restores ammo
    internal const float _AMMO_AMT    =  0.01f; // percent ammo restored to a random gun selected with _AMMO_CHANCE per debris

    internal const float _SQR_REACH   = _REACH * _REACH; // avoid an unnecessary sqrt() by using sqrmagnitude

    private float _timeOfLastCheck = 0.0f;
    private int _debrisSucked = 0;

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<VacuumCleaner>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.D, gunClass: GunClass.CHARGE, reloadTime: 1.2f, ammo: 999, infiniteAmmo: true);
            gun.SetAnimationFPS(gun.chargeAnimation, 16);
            gun.AddToSubShop(ItemBuilder.ShopType.Goopton);
            gun.AddToSubShop(ModdedShopType.Rusty);

        gun.InitProjectile(new(clipSize: -1, shootStyle: ShootStyle.Charged, ammoType: GameUIAmmoType.AmmoType.BEAM, chargeTime: float.MaxValue)); // absurdly high charge value so we never actually shoot

        _VacuumVFX = VFX.Create("vacuum_wind_sprite_a", fps: 30, loops: true, loopStart: 6, anchor: Anchor.MiddleCenter, scale: 0.5f);
    }

    private void MaybeRestoreAmmo()
    {
        if (UnityEngine.Random.value > _AMMO_CHANCE)
            return; // Make sure we restore any ammo at all
        if (this.Owner is not PlayerController player)
            return; // Make sure our owner is a player

        // Look for guns missing any ammo whatsoever
        List<Gun> candidates = new();
        foreach (Gun g in player.inventory.AllGuns)
        {
            if (!g.InfiniteAmmo && g.CanGainAmmo && g.CurrentAmmo < g.AdjustedMaxAmmo)
                candidates.Add(g);
        }
        if (candidates.Count == 0)
            return; // No guns are missing any ammo

        // Pick a gun
        Gun gunToGainAmmo = candidates.ChooseRandom();
        int ammoToRestore = Lazy.RoundWeighted(_AMMO_AMT * gunToGainAmmo.AdjustedMaxAmmo);
        if (ammoToRestore == 0)
            return; // our fractional ammo gain did not restore anything

        // Actually restore the ammo
        gunToGainAmmo.GainAmmo(ammoToRestore);
        AkSoundEngine.PostEvent("vacuum_process_ammo_sound", this.gun.gameObject);
    }

    protected override void Update()
    {
        base.Update();
        if (BraveTime.DeltaTime == 0.0f)
            return;
        if (!this.gun.IsCharging)
            return;

        // Play vacuum noise
        Lazy.PlaySoundUntilDeathOrTimeout(soundName: "suction_loop", source: this.gun.gameObject, timer: 0.05f);

        Vector2 gunpos = this.gun.barrelOffset.position;

        // Particle effect creation logic should not be tied to framerate
        if (UnityEngine.Random.value < 0.66f * (BraveTime.DeltaTime * C.FPS))
        {
            float angleFromGun = this.gun.CurrentAngle + UnityEngine.Random.Range(-_SPREAD, _SPREAD);
            GameObject o = SpawnManager.SpawnVFX(_VacuumVFX, (gunpos + angleFromGun.ToVector(_REACH)).ToVector3ZUp(), Lazy.RandomEulerZ());
            o.AddComponent<VacuumParticle>().Setup(this.gun, _REACH);
        }

        if (BraveTime.ScaledTimeSinceStartup - this._timeOfLastCheck < _UPDATE_RATE)
            return; // don't need to update 60 times a second
        this._timeOfLastCheck = BraveTime.ScaledTimeSinceStartup;

        // TODO: figure out how to make this less resource intensive...there can be a lot of debris
        float minAngle = this.gun.CurrentAngle - _SPREAD;
        float maxAngle = this.gun.CurrentAngle + _SPREAD;
        foreach(DebrisObject debris in StaticReferenceManager.AllDebris)
        {
            if (!debris.HasBeenTriggered)
                continue; // not triggered yet
            if (debris.IsPickupObject || debris.Priority == EphemeralObject.EphemeralPriority.Critical)
                continue; // don't vacuum up important objects
            if (debris.gameObject.GetComponent<VacuumParticle>())
                continue; // already added a vacuum particle component
            Vector2 deltaVec = (debris.gameObject.transform.position.XY() - gunpos);
            if (deltaVec.sqrMagnitude > _SQR_REACH || !deltaVec.ToAngle().IsBetweenRange(minAngle, maxAngle))
                continue; // out of range

            // Make sure our debris doesn't glitch out with existing additional movement modifiers
            debris.ClearVelocity();
            debris.PreventFallingInPits = true;
            debris.IsAccurateDebris = false;
            if (debris.GetComponent<SpeculativeRigidbody>() is SpeculativeRigidbody body)
                body.enabled = false;
            debris.isStatic = true;
            debris.enabled = false;

            // Actually add the VacuumParticle component to it
            debris.gameObject.AddComponent<VacuumParticle>().Setup(this.gun, deltaVec.magnitude);
            ++this._debrisSucked;
            MaybeRestoreAmmo();
        }
    }
}

// TODO: setting alpha on the first frame a sprite exists doesn't seem to work, so we create a dummy sprite
public class VacuumParticle : MonoBehaviour
{
    private const float _MAX_LIFE           = 1.0f;
    private const float _MIN_DIST_TO_VACUUM = 0.5f;
    private const float _MIN_ALPHA          = 0.01f;
    private const float _MAX_ALPHA          = 0.5f;
    private const float _DLT_ALPHA          = 0.01f;

    private Gun _gun               = null;
    private tk2dBaseSprite _sprite = null;
    private float _accel           = 0.0f;
    private Vector2 _velocity      = Vector2.zero;
    private float _lifetime        = 0.0f;
    private float _alpha           = _MIN_ALPHA;
    private bool _isDebris         = true; // false for the VFX particles created by the vacuum animation itself, true for actual debris
    private DebrisObject _debris   = null;
    private float _startDistance   = 0.0f;
    private float _startScaleX     = 1.0f;
    private float _startScaleY     = 1.0f;

    public void Setup(Gun g, float startDistance = 0.0f)
    {
        this._gun           = g;
        this._startDistance = startDistance;
        this._debris        = base.gameObject.GetComponent<DebrisObject>();
        this._isDebris      = this._debris != null;
        this._sprite        = this._isDebris ? this._debris.sprite : base.gameObject.GetComponent<tk2dSprite>();
        this._startScaleX   = this._isDebris ? this._sprite.scale.x : 1.0f;
        this._startScaleY   = this._isDebris ? this._sprite.scale.y : 1.0f;
    }

    // Using LateUpdate() here so alpha is updated correctly
    private void LateUpdate()
    {
        if (BraveTime.DeltaTime == 0.0f)
            return; // nothing to do if time isn't passing

        // handle particle fading logic exclusive to the vacuum particles
        if (!this._isDebris)
        {
            this._alpha = Mathf.Min(this._alpha + _DLT_ALPHA, _MAX_ALPHA);
            this._sprite.renderer.SetAlpha(this._alpha);
            this._lifetime += BraveTime.DeltaTime;
            if (!this._gun || this._lifetime > _MAX_LIFE)
            {
                UnityEngine.GameObject.Destroy(base.gameObject);
                return;
            }
        }

        Vector2 towardsVacuum = (this._gun.barrelOffset.position - this._sprite.transform.position);
        float mag = towardsVacuum.magnitude;
        if (mag < _MIN_DIST_TO_VACUUM)
        {
            UnityEngine.GameObject.Destroy(base.gameObject);
            return;
        }

        // Shrink on our way to the vacuum
        float scale = mag / this._startDistance;
        this._sprite.scale = new Vector3(this._startScaleX * scale, this._startScaleY * scale, 1f);
        this._velocity = this._sprite.transform.position.XY().LerpNaturalAndDirectVelocity(
            target          : this._gun.barrelOffset.position,
            naturalVelocity : this._velocity,
            accel           : VacuumCleaner._ACCEL_SEC * BraveTime.DeltaTime,
            lerpFactor      : 0.5f);
        this.gameObject.transform.position += (this._velocity * C.FPS * BraveTime.DeltaTime).ToVector3ZUp(0f);
    }
}
