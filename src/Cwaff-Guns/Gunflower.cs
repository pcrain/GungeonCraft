namespace CwaffingTheGungy;

public class Gunflower : CwaffGun
{
    public static string ItemName         = "Gunflower";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    private float _strength;

    public static void Init()
    {
        Gun gun = Lazy.SetupGun<Gunflower>(ItemName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.C, gunClass: GunClass.PISTOL, reloadTime: 0.9f, ammo: 600, shootFps: 14, reloadFps: 4,
                muzzleFrom: Items.Mailbox, fireAudio: "paintball_shoot_sound", reloadAudio: "paintball_reload_sound");

        gun.InitProjectile(GunData.New(sprite: null, clipSize: 12, cooldown: 0.18f, shootStyle: ShootStyle.SemiAutomatic,
            damage: 9.0f, speed: 25f, range: 18f, force: 12f, hitEnemySound: "paintball_impact_enemy_sound", hitWallSound: "paintball_impact_wall_sound"));

        gun.gameObject.AddComponent<GunflowerAmmoDisplay>();
    }

    public override void OnSwitchedToThisGun()
    {
        base.OnSwitchedToThisGun();
        SetupLights();
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        base.OnSwitchedAwayFromThisGun();
        DestroyLights();
    }

    public override void OnPlayerPickup(PlayerController player)
    {
        base.OnPlayerPickup(player);
        SetupLights();
    }

    private AdditionalBraveLight _light;
    private void SetupLights() // called whenever you want to attach a flashlight to your gun
    {
        this._strength = 1f;
        DestroyLights();
        GameObject gameObject         = new GameObject();
        gameObject.transform.position = this.gun.barrelOffset.transform.position;
        gameObject.transform.parent   = this.gun.barrelOffset.transform;
        this._light = gameObject.AddComponent<AdditionalBraveLight>();
        this._light.LightColor              = Color.yellow;
        this._light.LightIntensity          = this._strength;
        this._light.LightRadius             = 15f;
        this._light.UsesCone                = true;
        this._light.LightAngle              = 30f; // misnomer, width of cone
        this._light.LightOrient             = this.PlayerOwner.m_currentGunAngle;
        this._light.Initialize();
    }

    public override void Update()
    {
        base.Update();
        if (!this.PlayerOwner)
            return;
        this._light.LightOrient    = this.PlayerOwner.m_currentGunAngle;
        this._light.LightIntensity = this._strength;
    }

    private void DestroyLights() // called whenever you want to destroy the flashlight for your gun
    {
        if (this._light)
            UnityEngine.Object.Destroy(this._light.gameObject);
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        DestroyLights();
        base.OnDroppedByPlayer(player);
    }

    public override void OnDestroy()
    {
        DestroyLights();
        base.OnDestroy();
    }

    public override void OnReloadPressed(PlayerController player, Gun gun, bool manualReload)
    {
        base.OnReloadPressed(player, gun, manualReload);
        if (!player.IsDodgeRolling && player.AcceptingNonMotionInput)
            this._strength *= 10f;
    }

    private class GunflowerAmmoDisplay : CustomAmmoDisplay
    {
        private Gun _gun;
        private Gunflower _gunflower;
        private PlayerController _owner;
        private void Start()
        {
            this._gun = base.GetComponent<Gun>();
            this._gunflower = this._gun.GetComponent<Gunflower>();
            this._owner = this._gun.CurrentOwner as PlayerController;
        }

        public override bool DoCustomAmmoDisplay(GameUIAmmoController uic)
        {
            if (!this._owner)
                return false;

            uic.GunAmmoCountLabel.Text = $"Brightness: {this._gunflower._strength}";
            return true;
        }
    }
}

