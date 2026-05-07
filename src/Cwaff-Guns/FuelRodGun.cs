namespace CwaffingTheGungy;

public class FuelRodGun : CwaffGun
{
    public static string ItemName         = "Fuel Rod Gun";
    public static string ShortDescription = "Barely Contained";
    public static string LongDescription  = "Launches highly explosive fuel rods. Cannot use conventional ammo, but can extract fuel by interacting with barrels while equipped. Reloading cycles through available fuel types.";
    public static string Lore             = "A highly destructive support weapon, manufactured in an alternate timeline by an advanced alien race waging war against humanity. It was designed with several mechanisms for rendering it inoperable should it fall into the wrong hands. Naturally, it was most frequently wielded by low-level grunts with these mechanisms completely disabled.";

    public enum FuelRodAmmoType { WATER, EXPLOSIVE, OIL, POISON, /* VOID,*/ }

    private const int _AMMO_PER_BARREL = 5;

    internal static readonly List<Projectile> _Rods = new();
    internal static readonly List<GameObject> _AmmoCells = new();
    internal static readonly List<String> _AmmoSprites = new();

    private int _newGoopType;
    private bool _ammoDisplayDirty = true;
    private int _cachedAmmo = 1;
    private int _uiAmmoType = -1;

    public int curAmmoType = 0;
    public List<int> ammoOfEachType = new();

    public static void Init()
    {
        Lazy.SetupGun<FuelRodGun>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.B, gunClass: GunClass.EXPLOSIVE, reloadTime: 1.0f, ammo: 15, shootFps: 24, smoothReload: 0.0f,
            fireAudio: "fuel_rod_gun_fire_sound", canGainAmmo: false, canReloadNoMatterAmmo: true)
          .SetReloadAudio("fuel_rod_reload_place_sound", 8)
          .SetReloadAudio("fuel_rod_reload_click_sound", 14)
          .AddToShop(ItemBuilder.ShopType.Goopton)
          .AddToShop(ItemBuilder.ShopType.Trorc)
          .Attach<FuelRodGunAmmoDisplay>()
          .AssignGun(out Gun gun)
          .InitProjectile(GunData.New(sprite: "fuel_rod_cannon_projectile_empty", clipSize: 1, cooldown: 0.1f, shootStyle: ShootStyle.SemiAutomatic,
            damage: 40.0f, speed: 50f, range: 100f, force: 25f, recoil: 20f, shouldRotate: true, customClip: true, pierceBreakables: true,
            glowColor: ExtendedColours.lime, glowAmount: 100f))
          .AttachTrail("fuel_rod_trail", fps: 120, timeTillAnimStart: 0.00f, glowAmount: 55f,
            destroyOnEmpty: true, dispersalPrefab: Lazy.DispersalParticles(ExtendedColours.lime))
          .Attach<ExplosiveModifier>(ex => ex.explosionData = Explosions.DefaultLarge.Scale(1.5f))
          .Assign(out Projectile baseProj);

        // WATER
        _Rods.Add(baseProj.Clone(GunData.New(sprite: "fuel_rod_cannon_projectile_water")).MakeFuelRodProjectile(goop: EasyGoopDefinitions.WaterGoop));
        _AmmoCells.Add(VFX.Create("fuel_rod_cannon_ammo_water", emissivePower: 100f));
        _AmmoSprites.Add(Lazy.SetupCustomAmmoClip("fuel_rod_gun_water"));
        // EXPLOSIVE
        _Rods.Add(baseProj.Clone(GunData.New(sprite: "fuel_rod_cannon_projectile_fire")).MakeFuelRodProjectile(power: 2.0f));
        _AmmoCells.Add(VFX.Create("fuel_rod_cannon_ammo_fire", emissivePower: 100f));
        _AmmoSprites.Add(Lazy.SetupCustomAmmoClip("fuel_rod_gun_fire"));
        // OIL
        _Rods.Add(baseProj.Clone(GunData.New(sprite: "fuel_rod_cannon_projectile_oil")).MakeFuelRodProjectile(goop: EasyGoopDefinitions.OilDef));
        _AmmoCells.Add(VFX.Create("fuel_rod_cannon_ammo_oil", emissivePower: 100f));
        _AmmoSprites.Add(Lazy.SetupCustomAmmoClip("fuel_rod_gun_oil"));
        // POISON
        _Rods.Add(baseProj.Clone(GunData.New(sprite: "fuel_rod_cannon_projectile_poison")).MakeFuelRodProjectile(goop: EasyGoopDefinitions.PoisonDef));
        _AmmoCells.Add(VFX.Create("fuel_rod_cannon_ammo_poison", emissivePower: 100f));
        _AmmoSprites.Add(Lazy.SetupCustomAmmoClip("fuel_rod_gun_poison"));
    }

    public override Projectile OnPreFireProjectileModifier(Gun gun, Projectile projectile, ProjectileModule mod)
    {
      return _Rods[this.curAmmoType % _Rods.Count];
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
      base.PostProcessProjectile(projectile);
      projectile.DestroyMode = Projectile.ProjectileDestroyMode.BecomeDebris;
      projectile.OnBecameDebris += this.OnBecameDebris;
    }

    private void OnBecameDebris(DebrisObject debris)
    {
      debris.sprite.spriteId = debris.sprite.collection.GetSpriteIdByName("fuel_rod_cannon_projectile_empty");
      debris.sprite.SetGlowiness(1f);
    }

    public override void OnReloadPressed(PlayerController player, Gun gun, bool manualReload)
    {
      base.OnReloadPressed(player, gun, manualReload);
      if (!manualReload)
        return;
      this.curAmmoType = (this.curAmmoType + 1) % _Rods.Count;
      this.gun.CurrentAmmo = this.ammoOfEachType[this.curAmmoType];
      UpdateAmmo();
      player.gameObject.Play("replicant_select_sound");
      this._ammoDisplayDirty = true;
    }

    public override void Update()
    {
      base.Update();
      if (this._cachedAmmo != this.gun.CurrentAmmo)
      {
        this._cachedAmmo = this.gun.CurrentAmmo;
        UpdateAmmo();
      }
      if (this._uiAmmoType != this.curAmmoType)
      {
          this._uiAmmoType = this.curAmmoType;
          this.gun.DefaultModule.customAmmoType = _AmmoSprites[this.curAmmoType];
      }
    }

    public override void OnMasteryStatusChanged()
    {
        base.OnMasteryStatusChanged();
        this.gun.CanGainAmmo = this.Mastered;
    }

    public override void OnFirstPickup(PlayerController player)
    {
        base.OnFirstPickup(player);
        //NOTE: this happens before deserialization, so midgame saves still work properly
        for (int i = 0; i < _Rods.Count; ++i)
        {
          if (this.ammoOfEachType.Count <= i)
            this.ammoOfEachType.Add(0);
          else
            this.ammoOfEachType[i] = 0;
        }
        this.gun.CurrentAmmo = 0;
    }

    private void ConvertBarrelToAmmo(KickableObject barrel, string prefabName)
    {
        this._newGoopType = (int)(prefabName switch
        {
            "red barrel"  => FuelRodAmmoType.EXPLOSIVE,
            "red drum"    => FuelRodAmmoType.EXPLOSIVE,
            "purple drum" => FuelRodAmmoType.OIL,
            "yellow drum" => FuelRodAmmoType.POISON,
            "blue drum"   => FuelRodAmmoType.WATER,
            _             => FuelRodAmmoType.WATER,
        });
        //TODO: nicer VFX for spawning ammo
        Lazy.DoPickupAt(barrel.specRigidbody.UnitCenter);
        MinorBreakable breakable = barrel.gameObject.GetComponent<MinorBreakable>();
        breakable.destroyOnBreak = true;
        breakable.makeParallelOnBreak = false;
        breakable.explodesOnBreak = false;
        breakable.goopsOnBreak = false;
        breakable.Break(Vector2.zero);
        //REFACTOR: FuelRodAmmoCollector.EasySetup() (inherit from some interface that makes that convenient)
        int ammoToGain = (this.Mastered ? 3 : 1) * _AMMO_PER_BARREL;
        new GameObject().AddComponent<FuelRodAmmoCollector>().Setup((FuelRodAmmoType)this._newGoopType, ammoToGain, this, this.PlayerOwner);
    }

    public override void OnSwitchedToThisGun()
    {
        base.OnSwitchedToThisGun();
        UpdateAmmo();
        #if DEBUG
            Commands._OnDebugKeyPressed -= Lazy.DebugSpawnBarrels;
            Commands._OnDebugKeyPressed += Lazy.DebugSpawnBarrels;
        #endif
    }

    private void UpdateAmmo()
    {
      if (this. PlayerOwner is not PlayerController player)
        return;
      this._ammoDisplayDirty = true;
      if (this.gun.InfiniteAmmo || this.gun.LocalInfiniteAmmo || player.InfiniteAmmo.Value)
        return;

      int curAmmo = this.ammoOfEachType[this.curAmmoType] = this.gun.CurrentAmmo;
      if (curAmmo > 0)
        return;

      int numAmmoTypes = _Rods.Count;
      for (int tries = numAmmoTypes - 1; tries >= 0; --tries)
      {
        this.curAmmoType = (this.curAmmoType + 1) % numAmmoTypes;
        if (this.ammoOfEachType[this.curAmmoType] == 0)
          continue;

        this.gun.CurrentAmmo = this.ammoOfEachType[this.curAmmoType];
        break;
      }
    }

    public override void OnAmmoChanged(PlayerController player, Gun gun)
    {
        base.OnAmmoChanged(player, gun);
        if (this.Mastered && this._cachedAmmo < this.gun.CurrentAmmo)
        {
          int ammoGained = this.gun.CurrentAmmo - this._cachedAmmo;
          for (int i = 0; i < _Rods.Count; ++i)
            this.ammoOfEachType[i] = Mathf.Min(this.ammoOfEachType[i] + ammoGained, this.gun.AdjustedMaxAmmo);
        }
        UpdateAmmo();
    }

    public override void MidGameSerialize(List<object> data, int i)
    {
        base.MidGameSerialize(data, i);
        data.Add(this.curAmmoType);
        for (int n = 0; n < _Rods.Count; ++n)
          data.Add(this.ammoOfEachType[n]);
    }

    public override void MidGameDeserialize(List<object> data, ref int i)
    {
        base.MidGameDeserialize(data, ref i);
        this.curAmmoType = (int)data[i++];
        for (int n = 0; n < _Rods.Count; ++n)
        {
          if (this.ammoOfEachType.Count <= n)
            this.ammoOfEachType.Add((int)data[i++]);
          else
            this.ammoOfEachType[n] = (int)data[i++];
        }
    }

    private class FuelRodGunAmmoDisplay : CustomAmmoDisplay
    {
        private FuelRodGun _frg;
        private PlayerController _owner;
        private string _ammoString = string.Empty;

        private static StringBuilder _SB = new StringBuilder("", 1000);

        private void Start()
        {
            this._frg = base.GetComponent<FuelRodGun>();
            this._owner = this._frg.gun.CurrentOwner as PlayerController;
        }

        public override bool DoCustomAmmoDisplay(GameUIAmmoController uic)
        {
            if (!this._owner || !this._frg)
                return false;

            if (this._frg._ammoDisplayDirty || string.IsNullOrEmpty(this._ammoString))
            {
              _SB.Length = 0;
              for (int i = 0; i < _Rods.Count; ++i)
              {
                if (this._frg.ammoOfEachType[i] == 0)
                {
                  if (this._frg.curAmmoType == i)
                    _SB.Append($"[sprite \"fuel_rod_ui_006\"]");
                  else
                    _SB.Append($"[sprite \"fuel_rod_ui_001\"]");
                }
                else if (this._frg.curAmmoType == i)
                  _SB.Append($"[sprite \"fuel_rod_ui_0{7+i:D2}\"]");
                else
                  _SB.Append($"[sprite \"fuel_rod_ui_0{2+i:D2}\"]");
              }
              this._ammoString = _SB.ToString();
            }

            uic.GunAmmoCountLabel.Text = $"{this._ammoString}\n{this._owner.VanillaAmmoDisplay()}";
            return true;
        }
    }


    [HarmonyPatch(typeof(KickableObject), nameof(KickableObject.Interact))]
    private class KickableObjectInteractPatch
    {
        static bool Prefix(KickableObject __instance, PlayerController player)
        {
            if (player.CurrentGun is not Gun gun)
                return true;
            if (gun.GetComponent<FuelRodGun>() is not FuelRodGun frg)
                return true;
            if (!Femtobyte.IsWhiteListedPrefab(__instance.gameObject, out Femtobyte.PrefabData prefab))
                return true; //REFACTOR: move out of Femtobyte to somewhere more neutral
            if (!prefab.prefabName.Contains("barrel") && !prefab.prefabName.Contains("drum"))
                return true;

            frg.ConvertBarrelToAmmo(__instance, prefab.prefabName);
            return false; // skip the original method
        }
    }
}

public class FuelRodAmmoCollector : MonoBehaviour
{
  public void Setup(FuelRodGun.FuelRodAmmoType ammoType, int amount, FuelRodGun gun, PlayerController player)
  {
    StartCoroutine(Run(ammoType, amount, gun, player));
  }

  private IEnumerator Run(FuelRodGun.FuelRodAmmoType ammoType, int amount, FuelRodGun gun, PlayerController player)
  {
    const float OUT_TIME    = 0.35f;
    const float IN_TIME     = 0.35f;
    const float STAGGER     = 0.5f;
    const float TOTAL_TIME  = OUT_TIME + IN_TIME + STAGGER + 0.1f;
    const float TARGET_DIST = 2.5f;
    const float SPREAD      = 80.0f;
    const float BASEANGLE   = 90.0f;

    GameObject vfxPrefab = FuelRodGun._AmmoCells[(int)ammoType];
    List<tk2dSprite> vfx = new();
    Vector2 ppos = player.CenterPosition;
    for (int i = 0; i < amount; ++i)
    {
      tk2dSprite newVFX = vfxPrefab.Instantiate(ppos).GetComponent<tk2dSprite>();
      newVFX.transform.localRotation = 90f.EulerZ();
      vfx.Add(newVFX);
    }
    float spreadDelta = SPREAD / amount;
    float staggerDelta = STAGGER / Mathf.Max(amount - 1, 1);

    for (float elapsed = 0f; elapsed < TOTAL_TIME; elapsed += BraveTime.DeltaTime)
    {
        ppos = player.CenterPosition;
        float angle = BASEANGLE - 0.5f * SPREAD;
        float vtime = elapsed;
        for (int i = 0; i < amount; ++i)
        {
          if (vfx[i])
          {
            float percentDist = 0.0f;
            if (vtime < OUT_TIME)
              percentDist = Ease.OutQuad(vtime / OUT_TIME);
            else if (vtime < (OUT_TIME + IN_TIME))
              percentDist = 1.0f - Ease.OutQuad((vtime - OUT_TIME) / IN_TIME);
            else
            {
              if (gun)
              {
                gun.curAmmoType = (int)ammoType;
                gun.gun.CurrentAmmo = gun.ammoOfEachType[gun.curAmmoType] = Mathf.Min(gun.ammoOfEachType[gun.curAmmoType] + 1, gun.gun.AdjustedMaxAmmo);
              }
              vfx[i].gameObject.Play("fuel_rod_cell_collect_sound");
              UnityEngine.Object.Destroy(vfx[i].gameObject);
              vfx[i] = null;
            }
            if (vfx[i])
            {
              float mag = TARGET_DIST * percentDist;
              Vector2 vfxpos = ppos + angle.ToVector(mag);
              Vector2 finalpos = vfxpos.HoverAt(amplitude: 3f/16f, frequency: 3f, phase: (float)i / amount);
              vfx[i].scale = percentDist * Vector3.one;
              vfx[i].PlaceAtRotatedPositionByAnchor(finalpos, Anchor.MiddleCenter);
            }
          }
          angle += spreadDelta;
          vtime = Mathf.Max(vtime - staggerDelta, 0.0f);
        }
        yield return null;
    }

    for (int i = 0; i < amount; ++i)
      if (vfx[i])
        UnityEngine.Object.Destroy(vfx[i].gameObject);

    UnityEngine.Object.Destroy(base.gameObject);
    yield break;
  }
}

public static class FuelRodGunHelpers
{
    public static Projectile MakeFuelRodProjectile(this Projectile p, GoopDefinition goop = null, float power = 1.0f)
    {
        if (goop != null)
          p.Attach<GoopModifier>(g => {
            g.goopDefinition         = goop;
            g.SpawnGoopOnCollision   = true;
            g.CollisionSpawnRadius   = 6f;
            g.SpawnGoopInFlight      = false; });
        if (power != 1.0f)
          p.Attach<ExplosiveModifier>(ex => { ex.explosionData = ex.explosionData.Clone(); ex.explosionData.damage *= power; });
        return p;
    }
}
