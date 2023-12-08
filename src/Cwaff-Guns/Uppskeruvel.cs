namespace CwaffingTheGungy;

public class Uppskeruvel : AdvancedGunBehavior
{
    public static string ItemName         = "Uppskeruvel"; // é
    public static string SpriteName       = "uppskeruvel";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    internal const string _SoulSpriteUI = $"{C.MOD_PREFIX}:_SoulSpriteUI";  // need the string immediately for safe(ish) async loading

    public int souls = 0;

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<Uppskeruvel>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.B, gunClass: GunClass.RIFLE, reloadTime: 0.5f, ammo: 800);
            gun.SetMuzzleVFX(Items.Mailbox); // innocuous muzzle flash effects

        gun.InitProjectile(new(clipSize: 12, cooldown: 0.1f, shootStyle: ShootStyle.SemiAutomatic,
          sprite: "uppskeruvel_projectile", fps: 12, scale: 0.5f, anchor: Anchor.MiddleLeft)
        // ).Attach<TranquilizerBehavior>(
        );

        gun.gameObject.AddComponent<UppskeruvelAmmoDisplay>();
    }
}

public class UppskeruvelAmmoDisplay : CustomAmmoDisplay
{
    private Gun _gun;
    private Uppskeruvel _uppies;
    private PlayerController _owner;
    private void Start()
    {
        this._gun = base.GetComponent<Gun>();
        this._uppies = this._gun.GetComponent<Uppskeruvel>();
        this._owner = this._gun.CurrentOwner as PlayerController;
    }

    public override bool DoCustomAmmoDisplay(GameUIAmmoController uic)
    {
        if (!this._owner)
            return false;

        // uic.SetAmmoCountLabelColor(Color.blue);
        uic.SetAmmoCountLabelColor(Color.white);
        Vector3 relVec = Vector3.zero;
        uic.GunAmmoCountLabel.AutoHeight = true; // enable multiline text
        uic.GunAmmoCountLabel.ProcessMarkup = true; // enable multicolor text
        uic.GunAmmoCountLabel.Text = $" [sprite \"{Uppskeruvel._SoulSpriteUI}\"][color #6666dd]x{this._uppies.souls}[/color]\n{this._gun.CurrentAmmo}/{this._gun.AdjustedMaxAmmo}";
        return true;
    }
}
