namespace CwaffingTheGungy;

public class Zealot : CwaffGun
{
    public static string ItemName         = "Zealot";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "A gun with an unyielding fervor for combat. It's said that even the Lich himself could not contain its rampage upon firing it once. While the Lich likely didn't have to worry about running out of ammo, accidentally breaking chests, or cheesing off Bello, you unfortunately do not have those same luxuries.";

    private bool _zealous = false;

    public static void Init()
    {
        Lazy.SetupGun<Zealot>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.C, gunClass: GunClass.SHITTY, reloadTime: 0.75f, ammo: 1200, shootFps: 60, reloadFps: 12,
            muzzleFps: 60, muzzleScale: 0.5f, muzzleAnchor: Anchor.MiddleLeft, attacksThroughWalls: true, fireAudio: "zealot_shoot_sound",
            canAttackWhileRolling: false)
          .InitProjectile(GunData.New(sprite: "zealot_projectile", clipSize: -1, cooldown: 0.1f, shootStyle: ShootStyle.Automatic,
            damage: 10.0f, speed: 60f, range: 9999f, force: 12f, scale: 0.5f, hitSound: "zealot_impact_sound"))
          .Attach<EasyTrailBullet>(trail => {
            trail.TrailPos   = trail.transform.position;
            trail.StartWidth = 0.3f;
            trail.EndWidth   = 0.05f;
            trail.LifeTime   = 0.07f;
            trail.BaseColor  = Color.red;
            trail.StartColor = Color.Lerp(Color.red, Color.white, 0.25f);
            trail.EndColor   = Color.red; })
          .SetAllImpactVFX((ItemHelper.Get(Items.WitchPistol) as Gun).DefaultModule.projectiles[0].hitEffects.enemy);
    }

    public override void OnPostFired(PlayerController player, Gun gun)
    {
        base.OnPostFired(player, gun);
        if (!this._zealous && gun.CurrentAmmo > 0)
            Zealify(true, player);
        else if (this._zealous && gun.CurrentAmmo == 0)
            Zealify(false, player);
    }

    private void Zealify(bool zealous, PlayerController player)
    {
        this._zealous    = zealous;
        player.forceFire = zealous;
        gun.CanBeDropped = !zealous;
        gun.CanBeSold    = !zealous;
        player.inventory.GunLocked.SetOverride(ItemName, zealous);
    }

    public override void Update()
    {
        base.Update();
        if (!this._zealous)
            return;
        if (this.PlayerOwner is not PlayerController player)
            return;
        if (player.AcceptingNonMotionInput)
            return;
        if (player.CurrentInputState == PlayerInputState.NoInput)
            return;
        player.HandleGunFiringInternal(this.gun, BraveInput.GetInstanceForPlayer(player.PlayerIDX), this.gun == player.CurrentSecondaryGun);
    }

    public override void OnMasteryStatusChanged()
    {
        base.OnMasteryStatusChanged();
        this.canAttackWhileRolling = this.gun.LocalInfiniteAmmo = this.Mastered;
    }

    private void OnNewFloorLoaded(PlayerController player)
    {
        if (this._zealous)
            Zealify(false, player);
    }

    public override void OnPlayerPickup(PlayerController player)
    {
        base.OnPlayerPickup(player);
        player.OnNewFloorLoaded += this.OnNewFloorLoaded;
        this.canAttackWhileRolling = this.gun.LocalInfiniteAmmo = this.Mastered;
        Zealify(false, player);
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        base.OnDroppedByPlayer(player);
        player.OnReceivedDamage -= this.OnNewFloorLoaded;
        if (this._zealous)
            this.gun.CurrentAmmo = 0;
        Zealify(false, player);
    }

    public override void OnDestroy()
    {
        if (this.PlayerOwner)
            this.PlayerOwner.OnReceivedDamage -= this.OnNewFloorLoaded;
        base.OnDestroy();
    }
}
