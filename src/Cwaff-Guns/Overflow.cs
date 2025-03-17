namespace CwaffingTheGungy;

using static Overflow.ProjType;

public class Overflow : CwaffGun
{
    public static string ItemName         = "Overflow";
    public static string ShortDescription = "Contents Under Pressure";
    public static string LongDescription  = "Fires compressed blobs of goop, decreasing in fire rate and accuracy as ammo is depleted. Can be linked to barrels by interacting with them to siphon goop and restore ammo. Siphoning goop different than the currently held type will drain all held goop before siphoning any new goop.";
    public static string Lore             = "Outfitted with a state-of-the-art magneto-hydraulic compression unit, this weapon was designed by Professor Goopton specifically to take advantage of all of the liquid ammunition found in barrels throughout the Gungeon. Nobody had the heart to tell him the 'ammunition' inside these barrels was actually just various household waste products tossed in by residents of the Breach.";

    private const float _FILL_RATE       = 0.04f; // time per unit of ammo refilled while attached to a barrel with same type
    private const float _DRAIN_RATE      = 0.01f; // time per unit of ammo drained while attached to a barrel with different type
    private const float _SQR_SNAP_DIST   = 81.0f; // square distance from barrel before hose snaps
    private const float _PUMP_SOUND_RATE = 0.2f;  // delay between pump sounds
    private const float _MASTERY_AMMO_MULT = 4.0f;

    internal static tk2dSpriteAnimationClip[] _Hoses;
    internal static GameObject _SnapVFX = null;

    private KickableObject _attachedBarrel = null;
    private CwaffBezierMesh _cable = null;
    private float _fillTimer = 0.0f;
    private float _pumpSoundTimer = 0.0f;
    private int _newGoopType;
    private float _nextLeakTime = 0.0f;

    public bool didMasteryAmmoAdjust = false;
    public int curGoopType;

    internal enum ProjType { WATER, FIRE, POISON, OIL, ICE, VOID, }

    public static void Init()
    {
        Lazy.SetupGun<Overflow>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.B, gunClass: GunClass.FULLAUTO, reloadTime: 0.0f, ammo: 250, shootFps: 60, reloadFps: 4,
            fireAudio: "overflow_shoot_sound", rampUpFireRate: true, modulesAreTiers: true, muzzleVFX: "muzzle_overflow", muzzleFps: 120,
            muzzleScale: 0.5f, muzzleAnchor: Anchor.MiddleCenter, muzzleEmission: 10f)
          .AddToShop(ItemBuilder.ShopType.Goopton)
          .Attach<OverflowAmmoDisplay>()
          .AssignGun(out Gun gun)
          .InitProjectile(GunData.New(sprite: null, clipSize: -1, cooldown: 0.05f, shootStyle: ShootStyle.Automatic,
            damage: 5.0f, speed: 50f, range: 18f, force: 12f, glowAmount: 100f))
          .Assign(out Projectile baseProj);

        Projectile[] projectiles = [
            baseProj.Clone(GunData.New(sprite: "overflow_water_projectile",  damage: 6.5f) ).MakeGoop(EasyGoopDefinitions.WaterGoop),
            baseProj.Clone(GunData.New(sprite: "overflow_fire_projectile",   fire: 0.25f)  ).MakeGoop(EasyGoopDefinitions.FireDef),
            baseProj.Clone(GunData.New(sprite: "overflow_poison_projectile", poison: 0.25f)).MakeGoop(EasyGoopDefinitions.PoisonDef),
            baseProj.Clone(GunData.New(sprite: "overflow_oil_projectile",    slow: 0.25f)  ).MakeGoop(EasyGoopDefinitions.OilDef),
            baseProj.Clone(GunData.New(sprite: "overflow_ice_projectile",    freeze: 0.5f) ).MakeGoop(EasyGoopDefinitions.WaterGoop),
            baseProj.Clone(GunData.New(sprite: "overflow_void_projectile",   damage: 8.0f) ), //TODO: implement pit goop
        ];

        string[] clips = [ "overflow_water", "overflow_fire", "overflow_poison", "overflow_oil", "overflow_ice", "overflow_void" ];
        gun.Volley.projectiles = new(projectiles.Length);
        for (int i = 0; i < projectiles.Length; ++i)
            gun.Volley.projectiles.Add(new ProjectileModule(){ projectiles = new(){ projectiles[i] } }
              .SetAttributes(GunData.New(gun: gun, clipSize: -1, cooldown: 0.05f, angleVariance: 4f, shootStyle: ShootStyle.Automatic))
              .SetupCustomAmmoClip(clips[i]));

        _Hoses = [
            VFX.Create("overflow_hose_water",  fps: 120).DefaultAnimation(),
            VFX.Create("overflow_hose_fire",   fps: 120).DefaultAnimation(),
            VFX.Create("overflow_hose_poison", fps: 120).DefaultAnimation(),
            VFX.Create("overflow_hose_oil",    fps: 120).DefaultAnimation(),
            VFX.Create("overflow_hose_ice",    fps: 120).DefaultAnimation(),
            VFX.Create("overflow_hose_void",   fps: 120).DefaultAnimation(),
        ];

        _SnapVFX = VFX.Create("overflow_snap_vfx", fps: 30, loops: false);
    }

    private void ConnectBarrel(KickableObject barrel, string prefabName)
    {
        DisconnectBarrel();
        this._fillTimer = 0.0f;
        this._attachedBarrel = barrel;
        this._newGoopType = (int)(prefabName switch
        {
            "red barrel"  => ProjType.FIRE,
            "red drum"    => ProjType.FIRE,
            "purple drum" => ProjType.OIL,
            "yellow drum" => ProjType.POISON,
            "blue drum"   => WaterIsFrozen() ? ProjType.ICE : ProjType.WATER,
            _             => ProjType.WATER,
        });
        this._cable = CwaffBezierMesh.Create(_Hoses[this._newGoopType],
          this._attachedBarrel.sprite.WorldCenter, this.gun.PrimaryHandAttachPoint.position);
        this._cable.gameObject.SetLayerRecursively(LayerMask.NameToLayer("BG_Critical")); // render below most objects
    }

    //TODO: possibly add more checks for this later
    private static bool WaterIsFrozen() =>
        GameManager.Instance.GetLastLoadedLevelDefinition().dungeonSceneName == "tt_catacombs";

    private void DisconnectBarrel()
    {
        if (this._cable)
        {
            this.gun.gameObject.Play("overflow_snap_sound");
            for (int i = 1; i < 9; ++i)
                CwaffVFX.Spawn(prefab: _SnapVFX, position: this._cable.GetPointOnMainBezier(0.1f * i));
            UnityEngine.Object.Destroy(this._cable.gameObject);
        }
        this._cable = null;
        this._attachedBarrel = null;
    }

    public override void OnSwitchedToThisGun()
    {
        base.OnSwitchedToThisGun();
        if (this.gun.CurrentStrengthTier != this.curGoopType)
            this.gun.CurrentStrengthTier = this.curGoopType;
        #if DEBUG
            Commands._OnDebugKeyPressed -= SpawnBarrel;
            Commands._OnDebugKeyPressed += SpawnBarrel;
        #endif
    }

    #if DEBUG
    private static void SpawnBarrel()
    {
        List<string> _Barrels = [ "red barrel", "red drum", "blue drum", "purple drum", "yellow drum" ];
        RoomHandler room = GameManager.Instance.PrimaryPlayer.CurrentRoom;
        Vector2 pos = GameManager.Instance.PrimaryPlayer.CenterPosition;
        for (int i = 0; i < _Barrels.Count; ++i)
        {
            Vector2 bpos = pos + (i * 360f / _Barrels.Count).ToVector(2f);
            GameObject barrelPrefab = Femtobyte._NameToPrefabMap[_Barrels[i]].prefab;
            GameObject barrel = UnityEngine.Object.Instantiate(barrelPrefab, bpos, Quaternion.identity);
            barrel.GetComponentInChildren<tk2dSprite>().PlaceAtPositionByAnchor(bpos, Anchor.MiddleCenter);
            KickableObject kickable = barrel.GetComponentInChildren<KickableObject>();
            room.RegisterInteractable(kickable);
            kickable.ConfigureOnPlacement(room);
            SpeculativeRigidbody barrelBody = barrel.GetComponentInChildren<SpeculativeRigidbody>();
            barrelBody.Initialize();
            barrelBody.CorrectForWalls();
            PhysicsEngine.Instance.RegisterOverlappingGhostCollisionExceptions(barrelBody, null, false);
        }
    }
    #endif

    public override void OnSwitchedAwayFromThisGun()
    {
        DisconnectBarrel();
        base.OnSwitchedAwayFromThisGun();
    }

    public override void OnPlayerPickup(PlayerController player)
    {
        base.OnPlayerPickup(player);
        if (this.gun.CurrentStrengthTier != this.curGoopType)
            this.gun.CurrentStrengthTier = this.curGoopType;
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        DisconnectBarrel();
        base.OnDroppedByPlayer(player);
    }

    public override void OnDestroy()
    {
        DisconnectBarrel();
        base.OnDestroy();
    }

    private void HandleGoopOverflow()
    {
        const float _LEAK_RATE = 0.1f; // delay between goop leaks when overflowing while mastered
        const int   _MAX_LEAK  = 7;    // maximum number of projectiles we can leak at a time

        int unmasteredBaseAmmo = GetEffectiveMaxAmmo();
        if (this.gun.CurrentAmmo <= unmasteredBaseAmmo)
            return;

        float now = BraveTime.ScaledTimeSinceStartup;
        if (now < this._nextLeakTime)
            return;

        // how much the gun is overflown on a scale of 0% to 100%
        float overflowFactor = (float)(this.gun.CurrentAmmo - unmasteredBaseAmmo) / (float)(this.gun.AdjustedMaxAmmo - unmasteredBaseAmmo);
        int amountToLeak = Mathf.CeilToInt(overflowFactor * _MAX_LEAK);
        float gunAngle = this.PlayerOwner.m_currentGunAngle;
        for (int i = 0; i < amountToLeak; ++i)
        {
            float angle = gunAngle.AddRandomSpread(3f * amountToLeak);
            Vector2 avec = angle.ToVector();
            Projectile p = SpawnManager.SpawnProjectile(
              this.gun.DefaultModule.projectiles[0].gameObject,
              this.PlayerOwner.CurrentGun.barrelOffset.position, angle.EulerZ()).GetComponent<Projectile>();
            p.Owner = this.PlayerOwner;
            p.Shooter = this.PlayerOwner.specRigidbody;
        }

        this.PlayerOwner.gameObject.PlayOnce("overflow_shoot_sound");
        this.gun.CurrentAmmo -= amountToLeak;
        this._nextLeakTime = now + _LEAK_RATE;
    }

    public override void OwnedUpdatePlayer(PlayerController player, GunInventory inventory)
    {
        base.OwnedUpdatePlayer(player, inventory);
        if (this.Mastered && !this._cable)
            HandleGoopOverflow();
    }

    public override void Update()
    {
        base.Update();

        if (!this.didMasteryAmmoAdjust && this.Mastered)
        {
            this.gun.SetBaseMaxAmmo(Mathf.CeilToInt(_MASTERY_AMMO_MULT * this.gun.GetBaseMaxAmmo()));
            this.didMasteryAmmoAdjust = true;
        }

        if (!this.PlayerOwner)
            return;

        if (!this._attachedBarrel || !this._attachedBarrel.isActiveAndEnabled)
            DisconnectBarrel();

        if (this._cable)
        {
            this._cable.startPos = this._attachedBarrel.sprite.WorldCenter;
            this._cable.endPos = this.gun.PrimaryHandAttachPoint.position;
            if ((this._cable.startPos - this._cable.endPos).sqrMagnitude > _SQR_SNAP_DIST)
                DisconnectBarrel();
        }
        if (!this._cable)
        {
            this._fillTimer = 0.0f;
            return;
        }

        float now = BraveTime.ScaledTimeSinceStartup;
        bool playPumpSound = false;

        bool draining = this.gun.CurrentStrengthTier != this._newGoopType;
        float rate = (draining || this.Mastered) ? _DRAIN_RATE : _FILL_RATE;
        this._fillTimer += BraveTime.DeltaTime;
        while (this._fillTimer > rate)
        {
            playPumpSound = ((now - this._pumpSoundTimer) > _PUMP_SOUND_RATE);
            this._fillTimer -= rate;
            if (draining)
            {
                if (this.gun.CurrentAmmo == 0)
                {
                    this.gun.CurrentStrengthTier = this._newGoopType;
                    this.curGoopType = this._newGoopType;
                    draining = false;
                }
                else
                    this.gun.LoseAmmo(1);
            }
            else if (this.gun.CurrentAmmo < this.gun.AdjustedMaxAmmo)
                this.gun.GainAmmo(1);
        }

        if (playPumpSound)
        {
            this.gun.gameObject.Play("overflow_pump_sound");
            this._pumpSoundTimer = now;
        }
    }

    internal int GetEffectiveMaxAmmo() => this.didMasteryAmmoAdjust ? Mathf.FloorToInt(this.gun.AdjustedMaxAmmo / _MASTERY_AMMO_MULT) : this.gun.AdjustedMaxAmmo;

    //NOTE: Only works if GainsRateOfFireAsContinueAttack is true (i.e., rampUpFireRate: true is set in attributes)
    public override float GetDynamicFireRate() =>
        Mathf.Clamp((float)this.gun.CurrentAmmo / (float)GetEffectiveMaxAmmo(), 0.2f, 1.0f);

    public override float GetDynamicAccuracy() =>
        1f / Mathf.Clamp((float)this.gun.CurrentAmmo / (float)GetEffectiveMaxAmmo(), 0.2f, 1.0f);

    [HarmonyPatch(typeof(KickableObject), nameof(KickableObject.Interact))]
    private class KickableObjectInteractPatch
    {
        static bool Prefix(KickableObject __instance, PlayerController player)
        {
            if (player.CurrentGun is not Gun gun)
                return true;
            if (gun.GetComponent<Overflow>() is not Overflow overflow)
                return true;
            if (!Femtobyte.IsWhiteListedPrefab(__instance.gameObject, out Femtobyte.PrefabData prefab))
                return true; //REFACTOR: move out of Femtobyte to somewhere more neutral
            if (!prefab.prefabName.Contains("barrel") && !prefab.prefabName.Contains("drum"))
                return true;

            overflow.ConnectBarrel(__instance, prefab.prefabName);
            return false;    // skip the original method
        }
    }

    public override void MidGameSerialize(List<object> data, int i)
    {
        base.MidGameSerialize(data, i);
        data.Add(this.curGoopType);
        data.Add(this.didMasteryAmmoAdjust);
    }

    public override void MidGameDeserialize(List<object> data, ref int i)
    {
        base.MidGameDeserialize(data, ref i);
        this.curGoopType = (int)data[i++];
        if (this.gun.CurrentStrengthTier != this.curGoopType)
            this.gun.CurrentStrengthTier = this.curGoopType;
        this.didMasteryAmmoAdjust = (bool)data[i++];
    }
}

public class OverflowAmmoDisplay : CustomAmmoDisplay
{
    private Overflow _overflow;
    private Gun _gun;
    private PlayerController _owner;
    private string _goopText = null;
    private int _cachedTier = -1;
    private float _phase = 0.0f;

    private void Start()
    {
        this._gun = base.GetComponent<Gun>();
        this._overflow = base.GetComponent<Overflow>();
        this._owner = this._gun.CurrentOwner as PlayerController;
    }

    public override bool DoCustomAmmoDisplay(GameUIAmmoController uic)
    {
        const float _MIN_FLASH_RATE = 1.5f;
        const float _MAX_FLASH_RATE = 7.5f;
        const float _DLT_FLASH_RATE = _MAX_FLASH_RATE - _MIN_FLASH_RATE;

        if (!this._owner)
            return false;

        if (this._cachedTier != this._gun.CurrentStrengthTier)
        {
            this._cachedTier = this._gun.CurrentStrengthTier;
            this._goopText = (Overflow.ProjType)this._cachedTier switch
            {
                WATER  => "[color #6666dd]Water[/color]",
                FIRE   => "[color #dd6666]Fire[/color]",
                POISON => "[color #66dd66]Poison[/color]",
                OIL    => "[color #ddaa66]Oil[/color]",
                ICE    => "[color #9999ee]Ice[/color]",
                VOID   => "[color #dd66dd]Void[/color]",
                _      => "[color #dddddd]???[/color]",
            };
        }

        if (!this._overflow.didMasteryAmmoAdjust || this._gun.InfiniteAmmo)
        {
            uic.GunAmmoCountLabel.Text = $"{this._goopText}\n{this._owner.VanillaAmmoDisplay()}";
            return true;
        }

        int unmasteredBaseAmmo = this._overflow.GetEffectiveMaxAmmo();
        string ammoString = $"{this._gun.CurrentAmmo}/{unmasteredBaseAmmo}";
        if (this._gun.CurrentAmmo <= unmasteredBaseAmmo)
        {
            this._phase = 0.0f;
            uic.GunAmmoCountLabel.Text = $"{this._goopText}\n{ammoString}";
            return true;
        }

        float flashRate = _MIN_FLASH_RATE + _DLT_FLASH_RATE * (float)(this._gun.CurrentAmmo - unmasteredBaseAmmo) / (float)(this._gun.AdjustedMaxAmmo - unmasteredBaseAmmo);
        this._phase += flashRate * BraveTime.DeltaTime;
        Color phaseColor = Color.Lerp(Color.magenta, Color.white, Mathf.Abs(Mathf.Sin(this._phase)));
        uic.GunAmmoCountLabel.Text = $"{this._goopText}\n[color #{ColorUtility.ToHtmlStringRGB(phaseColor)}]{ammoString}[/color]";

        return true;
    }
}

public static class OverflowHelpers
{
    public static Projectile MakeGoop(this Projectile p, GoopDefinition goop)
    {
        return p.Attach<GoopModifier>(g => {
            g.goopDefinition         = goop;
            g.SpawnGoopOnCollision   = true;
            g.CollisionSpawnRadius   = 1f;
            g.SpawnGoopInFlight      = false; });
    }
}
