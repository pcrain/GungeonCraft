namespace CwaffingTheGungy;

public class GlassAmmoBox : CwaffActive
{
    public static string ItemName         = "Glass Ammo Box";
    public static string ShortDescription = "Infinite Ammo?";
    public static string LongDescription  = "Grants a single gun infinite ammo, but reduces the gun's ammo to zero upon taking damage while using it or dropping it on the ground.";
    public static string Lore             = "A magical wellspring of ammo whose bounty is matched only by its fragility. Though it gets its name from its glasslike appearance, in reality the box of ammo is held together by something even more brittle and fragile -- the hopes and prayers of hundreds of ammo-starved Gungeoneers forced to fight the Dragun with their rusty budget-grade weaponry.";

    internal static GameObject _GlassVFX = null;

    public static void Init()
    {
        PlayerItem item = Lazy.SetupActive<GlassAmmoBox>(ItemName, ShortDescription, LongDescription, Lore);
        item.AddToSubShop(ItemBuilder.ShopType.Trorc);
        item.quality    = ItemQuality.C;

        _GlassVFX = VFX.Create("glass_shard_vfx");
    }

    public override bool CanBeUsed(PlayerController user)
    {
        if (user.CurrentGun is not Gun gun)
            return false;
        if (gun.InfiniteAmmo || gun.LocalInfiniteAmmo || gun.AdjustedMaxAmmo <= 0)
            return false;
        if (gun.gameObject.GetComponent<GlassAmmoGun>())
            return false;
        return base.CanBeUsed(user);
    }

    public override void DoEffect(PlayerController user)
    {
        if (user.CurrentGun is Gun gun)
            gun.gameObject.AddComponent<GlassAmmoGun>().Setup(user);
    }

    internal static void RestoreMidGameData(PlayerController p)
    {
        int pid = p.PlayerIDX;
        List<int> glassGunIds = CwaffRunData.Instance.glassGunIds[pid];
        foreach (Gun gun in p.inventory.AllGuns)
            if (glassGunIds.Contains(gun.PickupObjectId))
            {
                Lazy.DebugLog($"restoring glass component for gun {gun.DisplayName}");
                gun.gameObject.AddComponent<GlassAmmoGun>().Setup(p);
            }
    }
}

public class GlassAmmoGun : MonoBehaviour
{
    private PlayerController _owner;
    private Gun _gun;
    private Shader _originalShader = null;

    public void Setup(PlayerController owner)
    {
        this._owner = owner;

        this._gun = base.gameObject.GetComponent<Gun>();
        this._gun.GainAmmo(owner.CurrentGun.AdjustedMaxAmmo);
        this._gun.ForceImmediateReload();
        this._gun.InfiniteAmmo = true;

        CwaffRunData.Instance.glassGunIds[this._owner.PlayerIDX].AddUnique(this._gun.PickupObjectId);
        this._owner.healthHaver.OnDamaged -= this.ShatterOnDamaged;
        this._owner.healthHaver.OnDamaged += this.ShatterOnDamaged;
        this._gun.OnDropped -= this.OnDropped;
        this._gun.OnDropped += this.OnDropped;

        if (!MidGameSaveData.IsInitializingPlayerData)
            this._owner.gameObject.Play("glass_assemble_sound");
        CwaffVFX.SpawnBurst(
            prefab           : GlassAmmoBox._GlassVFX,
            numToSpawn       : 50,
            basePosition     : this._owner.CenterPosition,
            positionVariance : 6f,
            velocityVariance : 15f,
            velType          : CwaffVFX.Vel.InwardToCenter,
            rotType          : CwaffVFX.Rot.Random,
            lifetime         : 0.4f,
            startScale       : 1.0f,
            endScale         : 0.1f,
            randomFrame      : true
          );
    }

    private void OnDropped()
    {
        this._gun.OnDropped -= this.OnDropped;
        ShatterInternal(dropped: true);
    }

    private void LateUpdate()
    {
        if (!this._gun || !this._gun.sprite)
            return;

        Material m = this._gun.sprite.renderer.material;
        if (m.shader == CwaffShaders.GoldShader)
            return;

        this._originalShader = m.shader;
        this._gun.sprite.usesOverrideMaterial = true;
        m.shader = CwaffShaders.GoldShader;
        m.SetColor("_GoldColor", new Color(0.5f, 1.0f, 1.0f));
        m.SetColor("_SheenColor", new Color(0.5f, 1.0f, 1.0f));
        m.SetFloat("_GoldNorm", 0f);
        m.SetFloat("_SheenAngle", 0f);
        m.SetFloat("_SheenWidth", 1.5f);
        m.SetFloat("_SheenSpacing", 0f);
        m.SetFloat("_SheenStrength", 0.25f);
        m.SetFloat("_SheenEmission", 3.0f);
        m.SetFloat("_SheenSpeed", 1.5f);
    }

    private void ShatterOnDamaged(float resultValue, float maxValue, CoreDamageTypes damageTypes, DamageCategory damageCategory, Vector2 damageDirection)
    {
        ShatterInternal(dropped: false);
    }

    private void ShatterInternal(bool dropped = false)
    {
        if (!this._owner || (!dropped && this._owner.CurrentGun != this._gun))
            return;

        this._owner.gameObject.Play("glass_shatter_sound");
        CwaffVFX.SpawnBurst(
            prefab           : GlassAmmoBox._GlassVFX,
            numToSpawn       : 50,
            basePosition     : this._owner.CenterPosition,
            positionVariance : 1f,
            minVelocity      : 6f,
            velocityVariance : 2f,
            velType          : CwaffVFX.Vel.Away,
            rotType          : CwaffVFX.Rot.Random,
            lifetime         : 0.6f,
            startScale       : 1.0f,
            endScale         : 0.1f,
            randomFrame      : true
          );

        this._gun.InfiniteAmmo = false;
        //NOTE: when used on guns with very large clip sizes (e.g., Natascha), the game can freeze if we don't clear the Ammo UI's cached shots
        this._gun.SetAmmoAndClearUICache(0);
        this._gun.OnDropped -= this.OnDropped;
        this._owner.healthHaver.OnDamaged -= this.ShatterOnDamaged;
        CwaffRunData.Instance.glassGunIds[this._owner.PlayerIDX].TryRemove(this._gun.PickupObjectId);
        UnityEngine.Object.Destroy(this);
    }

    private void OnDestroy()
    {
        if (this._gun)
        {
            if (this._originalShader != null)
                this._gun.sprite.renderer.material.shader = this._originalShader;
            this._gun.OnDropped -= this.OnDropped;
        }
        if (this._owner && this._owner.healthHaver)
            this._owner.healthHaver.OnDamaged -= this.ShatterOnDamaged;
    }
}
