namespace CwaffingTheGungy;

using static Overflow.ProjType;

public class Overflow : CwaffGun
{
    public static string ItemName         = "Overflow";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    private const float _FILL_RATE       = 0.04f; // time per unit of ammo refilled while attached to a barrel with same type
    private const float _DRAIN_RATE      = 0.01f; // time per unit of ammo drained while attached to a barrel with different type
    private const float _SQR_SNAP_DIST   = 81.0f; // square distance from barrel before hose snaps
    private const float _PUMP_SOUND_RATE = 0.2f;  // delay between pump sounds

    internal static tk2dSpriteAnimationClip[] _Hoses;
    internal static GameObject _SnapVFX = null;

    private KickableObject _attachedBarrel = null;
    private CwaffBezierMesh _cable = null;
    private float _fillTimer = 0.0f;
    private float _pumpSoundTimer = 0.0f;
    private int _newGoopType;

    public int curGoopType;

    internal enum ProjType { WATER, FIRE, POISON, OIL, ICE, VOID, }

    public static void Init()
    {
        Gun gun = Lazy.SetupGun<Overflow>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.B, gunClass: GunClass.FULLAUTO, reloadTime: 0.0f, ammo: 250, shootFps: 60, reloadFps: 4,
            fireAudio: "overflow_shoot_sound", rampUpFireRate: true, modulesAreTiers: true, muzzleVFX: "muzzle_overflow", muzzleFps: 120,
            muzzleScale: 0.5f, muzzleAnchor: Anchor.MiddleCenter, muzzleEmission: 10f)
          .AddToShop(ItemBuilder.ShopType.Goopton)
          .Attach<OverflowAmmoDisplay>();

        Projectile baseProj = gun.InitProjectile(GunData.New(sprite: null, clipSize: -1, cooldown: 0.05f, shootStyle: ShootStyle.Automatic,
            damage: 5.0f, speed: 50f, range: 18f, force: 12f));

        Projectile[] projectiles = [
            baseProj.Clone(GunData.New(gun: gun, sprite: "overflow_water_projectile",
                damage: 6.5f)).AddOverflowGoop(EasyGoopDefinitions.WaterGoop),
            baseProj.Clone(GunData.New(gun: gun, sprite: "overflow_fire_projectile",
                fire: 0.25f)).AddOverflowGoop(EasyGoopDefinitions.FireDef),
            baseProj.Clone(GunData.New(gun: gun, sprite: "overflow_poison_projectile",
                poison: 0.25f)).AddOverflowGoop(EasyGoopDefinitions.PoisonDef),
            baseProj.Clone(GunData.New(gun: gun, sprite: "overflow_oil_projectile",
                slow: 0.25f)).AddOverflowGoop(EasyGoopDefinitions.OilDef),
            baseProj.Clone(GunData.New(gun: gun, sprite: "overflow_ice_projectile",
                freeze: 0.5f)).AddOverflowGoop(EasyGoopDefinitions.WaterGoop),
            baseProj.Clone(GunData.New(gun: gun, sprite: "overflow_void_projectile",
                damage: 8.0f))/*.AddGoop(EasyGoopDefinitions.PitGoop)*/, //TODO: implement pit goop
        ];

        string[] clips = [
            "overflow_water",
            "overflow_fire",
            "overflow_poison",
            "overflow_oil",
            "overflow_ice",
            "overflow_void",
        ];

        gun.Volley.projectiles = new();
        for (int i = 0; i < projectiles.Length; ++i)
        {
            ProjectileModule mod = new ProjectileModule().SetAttributes(GunData.New(
              gun: gun, clipSize: -1, cooldown: 0.05f, angleVariance: 4f, shootStyle: ShootStyle.Automatic));
            projectiles[i].sprite.SetGlowiness(100f);
            mod.SetupCustomAmmoClip(clips[i]);
            mod.projectiles = new(){ projectiles[i] };
            gun.Volley.projectiles.Add(mod);
        }

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

    public override void Update()
    {
        base.Update();
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
        float rate = draining ? _DRAIN_RATE : _FILL_RATE;
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

    //NOTE: Only works if GainsRateOfFireAsContinueAttack is true (i.e., rampUpFireRate: true is set in attributes)
    public override float GetDynamicFireRate() =>
        Mathf.Clamp((float)this.gun.CurrentAmmo / (float)this.gun.AdjustedMaxAmmo, 0.2f, 1.0f);

    public override float GetDynamicAccuracy() =>
        1f / Mathf.Clamp((float)this.gun.CurrentAmmo / (float)this.gun.AdjustedMaxAmmo, 0.2f, 1.0f);

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
    }

    public override void MidGameDeserialize(List<object> data, ref int i)
    {
        base.MidGameDeserialize(data, ref i);
        this.curGoopType = (int)data[i++];
        if (this.gun.CurrentStrengthTier != this.curGoopType)
            this.gun.CurrentStrengthTier = this.curGoopType;
    }
}

public class OverflowAmmoDisplay : CustomAmmoDisplay
{
    private Gun _gun;
    private PlayerController _owner;
    private string _goopText = null;
    private int _cachedTier = -1;

    private void Start()
    {
        this._gun = base.GetComponent<Gun>();
        this._owner = this._gun.CurrentOwner as PlayerController;
    }

    public override bool DoCustomAmmoDisplay(GameUIAmmoController uic)
    {
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

        uic.GunAmmoCountLabel.Text = $"{this._goopText}\n{this._owner.VanillaAmmoDisplay()}";
        return true;
    }
}

public static class OverflowHelpers
{
    public static Projectile AddOverflowGoop(this Projectile p, GoopDefinition goop)
    {
        return p.Attach<GoopModifier>(g => {
            g.goopDefinition         = goop;
            g.SpawnGoopOnCollision   = true;
            g.CollisionSpawnRadius   = 1f;
            g.SpawnGoopInFlight      = false; });
    }
}
