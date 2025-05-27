namespace CwaffingTheGungy;

public class GlassAmmoBox : CwaffActive
{
    public static string ItemName         = "Glass Ammo Box";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    internal static GameObject _GlassVFX = null;

    public static void Init()
    {
        PlayerItem item = Lazy.SetupActive<GlassAmmoBox>(ItemName, ShortDescription, LongDescription, Lore);
        item.AddToSubShop(ItemBuilder.ShopType.Trorc);
        item.quality    = ItemQuality.B;

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
                Lazy.DebugLog($"restoring class component for gun {gun.DisplayName}");
                gun.gameObject.AddComponent<GlassAmmoGun>().Setup(p);
            }
    }
}

public class GlassAmmoGun : MonoBehaviour
{
    private PlayerController _owner;
    private Gun _gun;

    public void Setup(PlayerController owner)
    {
        this._owner = owner;

        this._gun = base.gameObject.GetComponent<Gun>();
        this._gun.GainAmmo(owner.CurrentGun.AdjustedMaxAmmo);
        this._gun.ForceImmediateReload();
        this._gun.InfiniteAmmo = true;

        CwaffRunData.Instance.glassGunIds[this._owner.PlayerIDX].AddUnique(this._gun.PickupObjectId);
        this._owner.healthHaver.OnDamaged -= this.Shatter;
        this._owner.healthHaver.OnDamaged += this.Shatter;
        this._gun.OnDropped -= this.OnDropped;
        this._gun.OnDropped += this.OnDropped;

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
        Shatter(dropped: true);
    }

    private void Shatter(float resultValue, float maxValue, CoreDamageTypes damageTypes, DamageCategory damageCategory, Vector2 damageDirection)
    {
        Shatter();
    }

    private void Shatter(bool dropped = false)
    {
        System.Console.WriteLine($"shatter check");
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
        this._gun.CurrentAmmo = 0;
        this._gun.OnDropped -= this.OnDropped;
        this._owner.healthHaver.OnDamaged -= this.Shatter;
        CwaffRunData.Instance.glassGunIds[this._owner.PlayerIDX].TryRemove(this._gun.PickupObjectId);
        UnityEngine.Object.Destroy(this);
    }

    private void OnDestroy()
    {
        if (this._gun)
            this._gun.OnDropped -= this.OnDropped;
        if (this._owner && this._owner.healthHaver)
            this._owner.healthHaver.OnDamaged -= this.Shatter;
    }
}
