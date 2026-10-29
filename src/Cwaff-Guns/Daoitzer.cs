namespace CwaffingTheGungy;

public class Daoitzer : CwaffGun
{
    public static string ItemName         = "Daoitzer";
    public static string ShortDescription = "Balanced Approach";
    public static string LongDescription  = "Killing enemies with this gun increases Yin. Killing enemies by other means increases Yang. Fires stronger projectiles the more balanced Yin and Yang are kept.";
    public static string Lore             = "In many ancient philosophies, Yin and Yang represent the harmony and interdependence of opposite forces. Light and darkness, sound and silence, ketchup and mustard: none can meaningfully exist without its counterpart. In Gungeon philosophy, Yin represents shooting your enemies, while Yang represents shooting your enemies slightly differently. Gungeon philosophers all seem to agree it's hard to go wrong by just shooting stuff.";

    private const int _BALANCE_GOOD  = 2;
    private const int _BALANCE_BAD   = 6;
    private const int _MAX_IMBALANCE = 10;

    internal static readonly Color _YinColor = new Color(34/255f, 32/255f, 52/255f);
    internal static readonly Color _YangColor = new Color(203/255f, 219/255f, 252/255f);
    internal static readonly Color _YinTrailColor = new Color(250/255f, 169/255f, 255/255f);
    internal static readonly Color _YangTrailColor = new Color(203/255f, 219/255f, 252/255f);

    internal static Projectile _YinProj = null;
    internal static Projectile _YangProj = null;
    internal static Projectile _YinYangProj = null;
    internal static Projectile _PerfectYinYangProj = null;
    internal static Projectile _WeakYinProj = null;
    internal static Projectile _WeakYangProj = null;

    public int balance = 0;

    private int _lastBalance = -9999;

    public static void Init()
    {
        Lazy.SetupGun<Daoitzer>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.C, gunClass: GunClass.SILLY, reloadTime: 0.9f, ammo: 333, shootFps: 30, reloadFps: 12,
            dynamicBarrelOffsets: true, suppressFireAnim: true, suppressReloadAnim: true, autoPlay: false)
          .Attach<DaoitzerAmmoDisplay>()
          .AssignGun(out Gun gun)
          .InitProjectile(GunData.New(sprite: "daoitzer_yin_taijitu", fps: 30, clipSize: 3, cooldown: 0.333f, shootStyle: ShootStyle.Automatic,
            damage: 9.0f, speed: 40f, range: 9999f, force: 12f, hitSound: "daoitzer_impact_sound"))
          .SetAllImpactVFX(VFX.CreatePool("daoitzer_impact_vfx", fps: 30, loops: false, emissiveColour: _YinColor, emissivePower: 30.0f, emissiveColorPower: 10.0f))
          .AttachTrail("daoitzer_trail", fps: 60, cascadeTimer: C.FRAME, softMaxLength: 1f, destroyOnEmpty: false, emissivePower: 30.0f, emissiveColorPower: 10.0f,
            emissiveColor: _YinTrailColor, emissiveSensitivity: 0.1f)
          .Assign(out _YinProj);

        gun.QuickUpdateGunAnimation($"idle_yin", fps: 1);
        gun.QuickUpdateGunAnimation($"idle_yang", fps: 1);
        gun.QuickUpdateGunAnimation($"fire_yin", fps: 30, returnToIdle: true);
        gun.QuickUpdateGunAnimation($"fire_yang", fps: 30, returnToIdle: true);

        _YinProj.sprite.MakeGlowyBetter(glowAmount: 50f, glowColor: Color.white, glowColorPower: 10.0f, sensitivity: 0.05f);

        _YangProj    = MakeProjectile(baseProj: _YinProj, newSprite: "daoitzer_yang_taijitu");
        _YangProj.GetComponentInChildren<CwaffTrailController>().sprite.MakeGlowyBetter(
          glowAmount: 30.0f, glowColorPower: 10.0f, glowColor: Daoitzer._YangTrailColor, sensitivity: 0.1f, skipSetup: true);

        _YinYangProj = MakeProjectile(baseProj: _YinProj, newSprite: "daoitzer_yinyang_taijitu", damageMult: 2.0f);
        _YinYangProj.GetComponentInChildren<CwaffTrailController>().sprite.MakeGlowyBetter(
          glowAmount: 30.0f, glowColorPower: 10.0f, glowColor: Daoitzer._YinTrailColor, sensitivity: 1.0f, skipSetup: true);

        _PerfectYinYangProj = MakeProjectile(baseProj: _YinYangProj, damageMult: 2.5f);

        _WeakYinProj = MakeProjectile(baseProj: _YinProj, damageMult: 0.67f);
        UnityEngine.Object.Destroy(_WeakYinProj.GetComponentInChildren<CwaffTrailController>().gameObject);

        _WeakYangProj = MakeProjectile(baseProj: _YangProj, damageMult: 0.67f);
        UnityEngine.Object.Destroy(_WeakYangProj.GetComponentInChildren<CwaffTrailController>().gameObject);
    }

    private static Projectile MakeProjectile(Projectile baseProj, string newSprite = null, float damageMult = 1.0f)
    {
      return baseProj.Clone(GunData.New(sprite: newSprite, hitSound: "daoitzer_impact_sound", damage: baseProj.baseData.damage * damageMult));
    }

    public override void OnPlayerPickup(PlayerController player)
    {
        base.OnPlayerPickup(player);
        player.OnDealtDamageContext -= this.OnDealtDamageContext;
        player.OnDealtDamageContext += this.OnDealtDamageContext;
        gun.spriteAnimator.Play();
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        base.OnDroppedByPlayer(player);
        player.OnDealtDamageContext -= this.OnDealtDamageContext;
        gun.spriteAnimator.StopAndResetFrameToDefault();
        StopGlowing();
    }

    public override void OnSwitchedToThisGun()
    {
        base.OnSwitchedToThisGun();
        UpdateAnimations(force: true);
    }

    private void StopGlowing()
    {
        this.gun.sprite.MakeGlowyBetter(glowAmount: 0.0f, glowColorPower: 0.0f,
          glowColor: Color.Lerp(_YinTrailColor, _YangTrailColor, 0.5f), sensitivity: 0.0f);
        this.gun.sprite.renderer.material.shader = CwaffShaders.PlayerShader;
    }

    private void UpdateAnimations(bool force = false)
    {
      if (!force && this.balance == this._lastBalance)
        return;

      if (this.balance == 0)
      {
        this.gun.sprite.MakeGlowyBetter(glowAmount: 30.0f, glowColorPower: 10.0f,
          glowColor: Color.Lerp(_YinTrailColor, _YangTrailColor, 0.5f), sensitivity: 1.2f);
      }
      else
        StopGlowing();

      this._lastBalance = this.balance;
      int absBalance = (this.balance > 0) ? this.balance : -this.balance;
      if (absBalance <= _BALANCE_GOOD)
      {
        this.suppressFireAnimations = true;
        this.gun.idleAnimation = "daoitzer_idle";
      }
      else if (this.balance > 0)
      {
        this.suppressFireAnimations = false;
        this.gun.idleAnimation = "daoitzer_idle_yin";
        this.gun.shootAnimation = "daoitzer_fire_yin";
      }
      else
      {
        this.suppressFireAnimations = false;
        this.gun.idleAnimation = "daoitzer_idle_yang";
        this.gun.shootAnimation = "daoitzer_fire_yang";
      }
      this.gun.reloadAnimation = this.gun.idleAnimation;
      this.gun.spriteAnimator.StopAndResetFrameToDefault();
      this.gun.spriteAnimator.Play(this.gun.IsFiring ? this.gun.shootAnimation : this.gun.idleAnimation);
    }

    public override void OnPostFired(PlayerController player, Gun gun)
    {
        base.OnPostFired(player, gun);
        if (this.balance > _BALANCE_BAD)
          base.gameObject.PlayOnce("daoitzer_imbalance_yin");
        else if (this.balance < -_BALANCE_BAD)
          base.gameObject.PlayOnce("daoitzer_imbalance_yang");
        else if (this.balance > _BALANCE_GOOD)
          base.gameObject.PlayOnce("daoitzer_balance_yin");
        else if (this.balance < -_BALANCE_GOOD)
          base.gameObject.PlayOnce("daoitzer_balance_yang");
        else if (this.balance == 0)
          base.gameObject.PlayOnce("daoitzer_balance_perfect");
        else
          base.gameObject.PlayOnce("daoitzer_balance_yinyang");
    }

    public override Projectile OnPreFireProjectileModifier(Gun gun, Projectile projectile, ProjectileModule mod)
    {
        if (this.balance == 0)
          return _PerfectYinYangProj;
        int absBalance = (this.balance > 0) ? this.balance : -this.balance;
        if (absBalance <= _BALANCE_GOOD)
          return _YinYangProj;
        if (absBalance <= _BALANCE_BAD)
          return this.balance > 0 ? _YinProj : _YangProj;
        return this.balance > 0 ? _WeakYinProj : _WeakYangProj;
    }

    private void OnDealtDamageContext(PlayerController source, float damage, bool fatal, HealthHaver target)
    {
        if (!fatal || source.GetGun<Daoitzer>() is not Daoitzer daoitzer || source.CurrentGun is not Gun gun)
          return;
        bool daoitzerActive = gun.gameObject.GetComponent<Daoitzer>() == daoitzer;
        if (daoitzerActive && (daoitzer.balance < _MAX_IMBALANCE))
          daoitzer.balance++;
        else if (!daoitzerActive && (daoitzer.balance > -_MAX_IMBALANCE))
          daoitzer.balance--;
        UpdateAnimations();
    }

    private class DaoitzerAmmoDisplay : CustomAmmoDisplay
    {
        private const string _NC              = "[/color]";
        private const string _YIN_COLOR       = "[color #faa9ff]";
        private const string _YANG_COLOR      = "[color #ddddff]";
        private const string _YIN_STRING      = $"{_YIN_COLOR}Yin";
        private const string _YANG_STRING     = $"{_YANG_COLOR}Yang";
        private const string _BALANCED_STRING = $"{_YIN_COLOR}B{_NC}{_YANG_COLOR}A{_NC}{_YIN_COLOR}L{_NC}{_YANG_COLOR}A{_NC}{_YIN_COLOR}N{_NC}{_YANG_COLOR}C{_NC}{_YIN_COLOR}E{_NC}{_YANG_COLOR}D{_NC}";

        private Daoitzer _dao               = null;
        private PlayerController _owner     = null;
        private string _cachedBalanceString = string.Empty;
        private int _cachedBalance          = -9999;

        private void Start()
        {
            Gun gun     = base.GetComponent<Gun>();
            this._dao   = gun.GetComponent<Daoitzer>();
            this._owner = gun.CurrentOwner as PlayerController;
        }

        public override bool DoCustomAmmoDisplay(GameUIAmmoController uic)
        {
            if (!this._owner || !this._dao)
                return false;

            if (this._cachedBalance != this._dao.balance)
            {
              if (this._dao.balance > 0)
                this._cachedBalanceString = $"{_YIN_STRING} ({this._dao.balance}){_NC}\n";
              else if (this._dao.balance < 0)
                this._cachedBalanceString = $"{_YANG_STRING} ({-this._dao.balance}){_NC}\n";
              else
                this._cachedBalanceString = $"{_BALANCED_STRING}\n";
              this._cachedBalance = this._dao.balance;
            }
            uic.GunAmmoCountLabel.Text = this._cachedBalanceString + this._owner.VanillaAmmoDisplay();
            return true;
        }
    }
}
