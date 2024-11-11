namespace CwaffingTheGungy;

using static Hallaeribut.State;

public class Hallaeribut : CwaffGun
{
    public static string ItemName         = "Hallaeribut";
    public static string ShortDescription = "Modern Warfare Cod";
    public static string LongDescription  = "Fires piranhas that devour any targets in their sight. Becomes increasingly hungry as ammo is depleted, spawning more piranhas per shot but having increasingly negative side effects: when Peckish, cannot be dropped; when Hungry, cannot pick up ammo for other guns; when Starving, cannot switch to other guns; when Famished, feeds on the player every 30 seconds.";
    public static string Lore             = "Piranhas are not known to be picky eaters. It's uncertain who first brought them into the Gungeon or for what purpose, but the fact that they're more than willing to eat flesh, metal, and lead is by all means a good enough reason to stick them in a gun and fire away. Just be extra careful whan handling the ammunition....";

    private const int _SWARM_SIZE = 2;
    private const int _BURSTS_PER_CLIP = 5;
    private const float _STARVE_TIMER = 30f;

    // NOTE: spaced to be at 25% increments assuming we shoot 2, 4, 6, and 8 shots 40 times each == 800 total ammo
    private static readonly float[] _AmmoThresholds = [1.0f, 0.9f, 0.7f, 0.4f, 0.0f];
    internal enum State {
        Satiated, //
        Peckish,  //   can't drop gun
        Hungry,   // + can't pick up ammo for other guns
        Starving, // + can't switch to a different gun
        Famished, // + feeds on the player every 30 seconds
        Ravenous, // MASTERY: can be fed grounded items
    }

    internal static GameObject _BiteVFX;

    private State _state = Satiated;
    private float _famishTimer = 0.0f;
    private int _cachedAmmo = -1;
    private bool _mastered = false;

    public static void Init()
    {
        Lazy.SetupGun<Hallaeribut>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.B, gunClass: GunClass.RIFLE, reloadTime: 1.2f, ammo: 800, shootFps: 24, reloadFps: 16,
            loopReloadAt: 0, muzzleVFX: "muzzle_hallaeribut", muzzleFps: 30, muzzleScale: 0.5f, fireAudio: "chomp_small_sound",
            reloadAudio: "chomp_small_sound", modulesAreTiers: true)
          .Attach<Unthrowable>() // throwing circumvents a primary mechanic, so don't allow it
          .Attach<HallaeributAmmoDisplay>()
          .AssignGun(out Gun gun)
          .InitProjectile(GunData.New(sprite: "hallaeribut_projectile", fps: 24, scale: 0.75f, clipSize: 32, cooldown: 0.33f,
            shootStyle: ShootStyle.Burst, angleVariance: 20f, damage: 9.0f, speed: 75f, range: 1000f, force: 12f, burstCooldown: 0.04f,
            customClip: true))
          .AudioEvent("snap_sound") // play every time animation returns to frame 0, not just on projectile creation
          .Attach<HallaeributProjectile>()
          .AttachTrail("hallaeribut_trail", fps: 24, cascadeTimer: C.FRAME, softMaxLength: 1f);

        ProjectileModule mod = gun.DefaultModule;
        gun.Volley.projectiles = new(_AmmoThresholds.Length - 1);
        for (int i = 1; i <= _AmmoThresholds.Length; ++i)
        {
            ProjectileModule newMod = ProjectileModule.CreateClone(mod, inheritGuid: false);
            newMod.burstShotCount = i * _SWARM_SIZE;
            newMod.numberOfShotsInClip = newMod.burstShotCount * _BURSTS_PER_CLIP;
            gun.Volley.projectiles.Add(newMod);
        } //REFACTOR: burst builder

        _BiteVFX = VFX.Create("bite_vfx", fps: 40, loops: false, scale: 0.33f);
    }

    public override void Update()
    {
        base.Update();
        if (!this.PlayerOwner || !this.PlayerOwner.AcceptingNonMotionInput)
            return;
        this._mastered = this.PlayerOwner.HasSynergy(Synergy.MASTERY_HALLAERIBUT);
        UpdateAmmo();
        UpdateStarvation();
        this.gun.m_prepThrowTime = -999f; //HACK: prevent the gun from being thrown
    }

    private void UpdateStarvation()
    {
        if ((int)this._state < (int)Famished)
            return;
        if (this._state == Ravenous)
        {
            if (this.gun.CurrentAmmo > 0 || this.gun.InfiniteAmmo || this.gun.LocalInfiniteAmmo)
                return; // don't snack on player while ravenous unless we're out of ammo
            if (AttemptRavenousItemConsume(checkInventory: true))
                return;
        }
        else if (this._state == Famished && (BraveTime.ScaledTimeSinceStartup - this._famishTimer) < _STARVE_TIMER)
            return;

        this._famishTimer = BraveTime.ScaledTimeSinceStartup;
        if (this.PlayerOwner.healthHaver is not HealthHaver hh || hh.IsDead)
            return;

        this.PlayerOwner.healthHaver.ApplyDamage(0.5f, Vector2.zero, "Insatiable Hunger", CoreDamageTypes.None,
            DamageCategory.Unstoppable, ignoreInvulnerabilityFrames: true);
        CwaffVFX.Spawn(prefab: Hallaeribut._BiteVFX, position: this.PlayerOwner.CenterPosition, lifetime: 0.3f, fadeOutTime: 0.1f);
        this.PlayerOwner.gameObject.Play("chomp_large_sound");
        if (this._state == Ravenous)
            this.gun.GainAmmo(Mathf.CeilToInt(0.2f * this.gun.AdjustedMaxAmmo)); // restore 20% when ravenous
    }

    public override void OnFullClipReload(PlayerController player, Gun gun)
    {
        if (!this._mastered)
            return;
        if (GameManager.Instance.IsLoadingLevel || GameManager.Instance.IsPaused || BraveTime.DeltaTime == 0.0f)
            return;
        if (player.IsDodgeRolling || !player.AcceptingNonMotionInput)
            return;
        AttemptRavenousItemConsume(false);
    }

    private bool AttemptRavenousItemConsume(bool checkInventory)
    {
        bool onGround = true;
        PickupObject item = GetNearestEdibleDroppedItem();
        if (!item && checkInventory)
        {
            item = GetWorstItemWeCanEat();
            onGround = false;
        }
        if (!item)
            return false; // we have no more items to scrap

        DebrisObject debris = Lazy.MakeDebrisFromSprite(item.sprite, onGround ? item.sprite.WorldCenter : this.PlayerOwner.CenterPosition);
            debris.doesDecay     = true;
            debris.bounceCount   = 0;
            debris.StartCoroutine(Lazy.DecayOverTime(debris, 0.5f, shrink: true));
        CwaffVFX.Spawn(prefab: Hallaeribut._BiteVFX, position: debris.sprite.WorldCenter, lifetime: 0.3f, fadeOutTime: 0.1f);
        debris.gameObject.Play("chomp_large_sound");

        float percentAmmoToRestore = 0;
        if (item.PickupObjectId == (int)Items.Junk)
            percentAmmoToRestore = 0.1f;
        else
            percentAmmoToRestore = Mathf.Max(1, item.QualityGrade()) * 0.2f;
        this.gun.GainAmmo(Mathf.CeilToInt(percentAmmoToRestore * this.gun.AdjustedMaxAmmo));

        // drop and destroy the item so we properly call the Drop() / Destroy() events and can't pick it back up
        if (onGround)
            UnityEngine.Object.Destroy(item.gameObject);
        else if (item is Gun g)
        {
            g.HasEverBeenAcquiredByPlayer = true;
            this.PlayerOwner.inventory.RemoveGunFromInventory(g);
            g.ToggleRenderers(true);
            UnityEngine.Object.Destroy(g.DropGun().gameObject); //TODO: pop this code in utility vest code as well
        }
        else if (item is PassiveItem p)
            UnityEngine.Object.Destroy(this.PlayerOwner.DropPassiveItem(p).gameObject);
        else if (item is PlayerItem i)
            UnityEngine.Object.Destroy(this.PlayerOwner.DropActiveItem(i).gameObject);

        return true;
    }

    private PickupObject GetNearestEdibleDroppedItem()
    {
        PlayerController owner = this.PlayerOwner;
        if (owner.CurrentRoom is not RoomHandler room)
            return null;
        PickupObject nearest = null;
        float nearestDist = 999f;
        Vector2 ownerPos = owner.CenterPosition;
        foreach (PickupObject pickup in CwaffEvents._DebrisPickups)
        {
            if (!pickup || pickup.IsBeingSold || pickup.QualityGrade() == 0)
                continue;
            Vector3 pos = pickup.transform.position;
            if (pos.GetAbsoluteRoom() != room)
                continue;
            float sqrDist = (pos.XY() - ownerPos).sqrMagnitude;
            if (sqrDist >= nearestDist)
                continue;
            nearestDist = sqrDist;
            nearest = pickup;
        }
        return nearest;
    }

    /* Item Priorities:
        -1: [undroppable item]
         0: Utility Vest
         1: S tier (+0.5 for empty gun)
         2: A tier (+0.5 for empty gun)
         3: B tier (+0.5 for empty gun)
         4: C tier (+0.5 for empty gun)
         5: D tier (+0.5 for empty gun)
         6: Junk
    */
    private static float GetItemFoodPriority(PickupObject item, PlayerController owner)
    {
        if (!item.CanActuallyBeDropped(owner))
            return -1f;
        if (item.PickupObjectId == Lazy.PickupId<Hallaeribut>())
            return 0f;
        if (item.PickupObjectId == (int)Items.Junk)
            return 6f;
        float priority;
        switch(item.quality)
        {
            case ItemQuality.S: priority = 1f; break;
            case ItemQuality.A: priority = 2f; break;
            case ItemQuality.B: priority = 3f; break;
            case ItemQuality.C: priority = 4f; break;
            case ItemQuality.D: priority = 5f; break;
            default:
                return -1f; // unknown item quality
        }
        if (item is Gun g)
            return priority + ((g.CurrentAmmo == 0) ? 0.5f: 0f);
        return priority;
    }

    private PickupObject GetWorstItemWeCanEat()
    {
        float highestPriority  = -1;
        PickupObject worstItem = null;
        PlayerController owner = this.PlayerOwner;
        foreach(PickupObject item in owner.AllItems())
        {
            float p = GetItemFoodPriority(item, owner);
            if (p <= highestPriority)
                continue;
            worstItem       = item;
            highestPriority = p;
        }
        return worstItem;
    }

    private void UpdateAmmo()
    {
        if (this._cachedAmmo == this.gun.CurrentAmmo)
            return;
        this._cachedAmmo = this.gun.CurrentAmmo;
        float ammoPercent = (float)this._cachedAmmo / this.gun.AdjustedMaxAmmo;
        int ti = this._mastered ? 5 : _AmmoThresholds.FirstGE(ammoPercent);
        UpdateState((State)ti);
        int newTier = Mathf.Max(ti - 1, 0);
        if (this.gun.CurrentStrengthTier != newTier)
            this.gun.CurrentStrengthTier = newTier;
    }

    private void UpdateState(State newState)
    {
        if (this._state == newState)
            return;

        this.gun.CanBeDropped = newState == Satiated;
        this.gun.CanBeSold = this.gun.CanBeDropped;
        this.PlayerOwner.inventory.GunLocked.SetOverride(ItemName, newState >= Starving);
        if ((int)this._state < (int)Famished && (int)newState >= (int)Famished)
            this._famishTimer = BraveTime.ScaledTimeSinceStartup;

        this._state = newState;
    }

    [HarmonyPatch(typeof(AmmoPickup), nameof(AmmoPickup.Interact))]
    private class AmmoPickupInteractPatch
    {
        static bool Prefix(AmmoPickup __instance, PlayerController interactor)
        {
            if (interactor.GetGun<Hallaeribut>() is not Hallaeribut hal)
                return true; // call the original method
            hal.UpdateAmmo(); // refresh ammo just in case it's changed
            if (hal._state <= Peckish)
                return true; // call the original method
            if (interactor.CurrentGun == hal.gameObject.GetComponent<Gun>())
                return true; // call the original method

            foreach (var label in GameUIRoot.Instance.m_extantReloadLabels)
                label.ProcessMarkup = true;
            GameUIRoot.Instance.InformNeedsReload(interactor, new Vector3(interactor.specRigidbody.UnitCenter.x - interactor.transform.position.x, 1.25f, 0f),
                1f, "[color #dd6666]It Hungers[/color]");
            return false;    // skip the original method
        }
    }

    private class HallaeributAmmoDisplay : CustomAmmoDisplay
    {
        private const string _RAVENOUS_STRING = "[color #bb33bb]R[/color][color #bb33aa]a[/color][color #bb3399]v[/color][color #bb3388]e[/color][color #bb3377]n[/color][color #bb3366]o[/color][color #bb3355]u[/color][color #bb3344]s[/color]";

        private Gun _gun;
        private Hallaeribut _hal;
        private PlayerController _owner;

        private void Start()
        {
            this._gun = base.GetComponent<Gun>();
            this._hal = this._gun.GetComponent<Hallaeribut>();
            this._owner = this._gun.CurrentOwner as PlayerController;
        }

        public override bool DoCustomAmmoDisplay(GameUIAmmoController uic)
        {
            if (!this._owner)
                return false;

            if (this._hal._state == Ravenous)
                uic.GunAmmoCountLabel.Text = $"{_RAVENOUS_STRING}\n{this._owner.VanillaAmmoDisplay()}";
            else if (this._hal._state == Famished)
                uic.GunAmmoCountLabel.Text = $"[color #bb33bb]{this._hal._state}[/color]\n{this._owner.VanillaAmmoDisplay()}";
            else
                uic.GunAmmoCountLabel.Text = $"{this._hal._state}\n{this._owner.VanillaAmmoDisplay()}";
            return true;
        }
    }
}

public class HallaeributProjectile : MonoBehaviour
{
    private const float _DECEL_START = 0.05f;
    private const float _HALT_START  = 0.25f;
    private const float _RELAUNCH_START  = 0.5f;
    private const float _LERP_RATE = 10f;

    private Projectile _projectile;
    private float _lifetime = 0f;
    private State _state = State.START;
    private float _startSpeed;
    private AIActor _target;

    private enum State
    {
        START,
        DECEL,
        HALT,
        RELAUNCH,
    }

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._projectile.OnHitEnemy += this.OnHitEnemy;
        this._startSpeed = this._projectile.baseData.speed;
        this._projectile.m_usesNormalMoveRegardless = true; // ignore all motion module overrides, helix bullets doeesn't play well with speed changing projectiles
    }

    private void OnHitEnemy(Projectile arg1, SpeculativeRigidbody arg2, bool arg3)
    {
        Vector2 center = (arg2.sprite is tk2dBaseSprite sprite) ? sprite.WorldCenter : arg2.UnitCenter;
        CwaffVFX.Spawn(prefab: Hallaeribut._BiteVFX, position: center, lifetime: 0.3f, fadeOutTime: 0.1f);
        arg2.gameObject.Play("chomp_large_sound");
    }

    private void Update()
    {
        this._lifetime += BraveTime.DeltaTime;
        switch (this._state)
        {
            case State.START:
                if (this._lifetime >= _DECEL_START)
                    this._state = State.DECEL;
                break;
            case State.DECEL:
                if (this._lifetime >= _HALT_START)
                {
                    this._projectile.baseData.speed = 0.01f;
                    this._target = Lazy.NearestEnemy(this._projectile.SafeCenter);
                    this._state = State.HALT;
                }
                else
                  this._projectile.baseData.speed = Lazy.SmoothestLerp(this._projectile.baseData.speed, 0f, _LERP_RATE);
                this._projectile.UpdateSpeed();
                break;
            case State.HALT:
                if (this._lifetime >= _RELAUNCH_START)
                {
                    if (this._target)
                        this._projectile.SendInDirection(this._target.CenterPosition - this._projectile.SafeCenter, false);
                    this._state = State.RELAUNCH;
                }
                break;
            case State.RELAUNCH:
                this._projectile.baseData.speed = Lazy.SmoothestLerp(this._projectile.baseData.speed, this._startSpeed, _LERP_RATE);
                this._projectile.UpdateSpeed();
                break;
        }
    }
}
