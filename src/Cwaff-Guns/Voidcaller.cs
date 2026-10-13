namespace CwaffingTheGungy;

public class Voidcaller : CwaffGun
{
    public static string ItemName         = "Voidcaller";
    public static string ShortDescription = "Ascetic Aesthetic";
    public static string LongDescription  = "Fires an extra projectile for each of the following the player has none of: keys, blanks, casings, hearts, armor, passives, actives, and (other) guns.";
    public static string Lore             = "TBD";

    private enum VoidType { Default, Key, Blank, Casing, Heart, Armor, Passive, Active, Gun }

    public static void Init()
    {
        Lazy.SetupGun<Voidcaller>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.D, gunClass: GunClass.SHOTGUN, reloadTime: 1.5f, ammo: 350, shootFps: 20, reloadFps: 30,
            muzzleFrom: Items.Mailbox, fireAudio: "voidcaller_shoot_sound", smoothReload: 0.1f)
          .SetReloadAudio("voidcaller_reload_sound", 0, 2, 4, 6, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25)
          .AssignGun(out Gun gun)
          .InitProjectile(GunData.New(clipSize: 6, cooldown: 0.5f, shootStyle: ShootStyle.SemiAutomatic, damage: 7.5f))
          .SetAllImpactVFX(VFX.CreatePool("voidcaller_impact", fps: 30, loops: false, emissivePower: 2f,
            emissiveColorPower: 10.0f, emissiveColour: new Color(0.7f, 0.4f, 0.7f), emissiveSensitivity: 0.6f))
          .Assign(out Projectile p);

        gun.Volley.UsesShotgunStyleVelocityRandomizer = true;
        gun.Volley.DecreaseFinalSpeedPercentMin = -25.0f;
        gun.Volley.IncreaseFinalSpeedPercentMax = 25.0f;
        gun.Volley.projectiles = Enumerable.Repeat<ProjectileModule>(null, Enum.GetValues(typeof(VoidType)).Length).ToList();
          gun.Volley.projectiles[(int)VoidType.Default]   = VoidcallerModule(gun, p, sprite: "voidcaller_projectile_default", sound: "voidcaller_shoot_proj_1", ammoCost: 1);
          gun.Volley.projectiles[(int)VoidType.Key]       = VoidcallerModule(gun, p, sprite: "voidcaller_projectile_key",     sound: "voidcaller_shoot_proj_2");
          gun.Volley.projectiles[(int)VoidType.Blank]     = VoidcallerModule(gun, p, sprite: "voidcaller_projectile_blank",   sound: "voidcaller_shoot_proj_3");
          gun.Volley.projectiles[(int)VoidType.Casing]    = VoidcallerModule(gun, p, sprite: "voidcaller_projectile_casing",  sound: "voidcaller_shoot_proj_4");
          gun.Volley.projectiles[(int)VoidType.Heart]     = VoidcallerModule(gun, p, sprite: "voidcaller_projectile_heart",   sound: "voidcaller_shoot_proj_5");
          gun.Volley.projectiles[(int)VoidType.Armor]     = VoidcallerModule(gun, p, sprite: "voidcaller_projectile_armor",   sound: "voidcaller_shoot_proj_6");
          gun.Volley.projectiles[(int)VoidType.Passive]   = VoidcallerModule(gun, p, sprite: "voidcaller_projectile_passive", sound: "voidcaller_shoot_proj_7");
          gun.Volley.projectiles[(int)VoidType.Active]    = VoidcallerModule(gun, p, sprite: "voidcaller_projectile_active",  sound: "voidcaller_shoot_proj_8");
          gun.Volley.projectiles[(int)VoidType.Gun]       = VoidcallerModule(gun, p, sprite: "voidcaller_projectile_gun",     sound: "voidcaller_shoot_proj_9");
    }

    private void Start()
    {
        gun.sprite.MakeGlowyBetter(glowAmount: 60f, glowColorPower: 27f, glowColor: Color.white, sensitivity: 0.25f);
    }

    public override void Update()
    {
        base.Update();
        if (this.PlayerOwner)
            this.gun.SynchronizeReloadAcrossAllModules(); // Synchronize ammo clips between projectile modules as necessary
        Material mat = this.gun.sprite.renderer.material;
        if (this.gun.IsReloading)
        {
          mat.shader = CwaffShaders.UberEmissiveShader;
          mat.SetFloat(CwaffVFX._EmissivePowerId, 50f);
          mat.SetColor(CwaffVFX._EmissiveColorId, Color.white);
        }
        else
          mat.shader = CwaffShaders.PlayerShader;
    }

    private static ProjectileModule VoidcallerModule(Gun gun, Projectile proj, string sprite, string sound, int ammoCost = 0)
    {
        Projectile p = proj.Clone(GunData.New(sprite: sprite, spawnSound: sound, hitSound: "voidcaller_impact_sound"));
        p.sprite.MakeGlowyBetter(glowAmount: 37.0f, glowColor: Color.white, glowColorPower: 7.0f, sensitivity: 0.5f);
        return new ProjectileModule() { projectiles = [p] }
          .SetAttributes(GunData.New(gun: gun, ammoCost: ammoCost, clipSize: 5, cooldown: 0.5f, angleVariance: 16f, shootStyle: ShootStyle.SemiAutomatic, sequenceStyle: ProjectileSequenceStyle.Ordered, customClip: true));
    }

    private bool OwnerOnlyHasThisGunAndUndroppableGuns(PlayerController player)
    {
      if (player.inventory == null || player.inventory.m_guns == null)
        return false;
      foreach (Gun gun in player.inventory.m_guns)
        if (gun.CanActuallyBeDropped(player) && gun != this.gun)
          return false;
      return true;
    }

    private bool OwnerOnlyHasUndroppablePassives(PlayerController player)
    {
      if (player.passiveItems == null)
        return false;
      foreach (PassiveItem passive in player.passiveItems)
        if (passive.CanActuallyBeDropped(player))
          return false;
      return true;
    }

    private bool OwnerOnlyHasUndroppableActives(PlayerController player)
    {
      if (player.activeItems == null)
        return false;
      foreach (PlayerItem active in player.activeItems)
        if (active.CanActuallyBeDropped(player))
          return false;
      return true;
    }

    public override bool IsModuleActive(ProjectileModule module)
    {
      if (this.gun.GunPlayerOwner() is not PlayerController player)
        return true;

      int index = this.gun.Volley.projectiles.IndexOf(module);
      bool canFire = ((VoidType)index) switch {
        VoidType.Default   => true,
        VoidType.Key       => player.carriedConsumables.KeyBullets == 0,
        VoidType.Blank     => player.m_blanks == 0,
        VoidType.Casing    => player.carriedConsumables.Currency == 0,
        VoidType.Heart     => player.healthHaver.currentHealth == 0,
        VoidType.Armor     => player.healthHaver.currentArmor == 0,
        VoidType.Passive   => OwnerOnlyHasUndroppablePassives(player),
        VoidType.Active    => OwnerOnlyHasUndroppableActives(player),
        VoidType.Gun       => OwnerOnlyHasThisGunAndUndroppableGuns(player),
        _                  => true,
      };
      return canFire;
    }
}
