namespace CwaffingTheGungy;

public class DerailGun : CwaffGun
{
    public static string ItemName         = "Derail Gun";
    public static string ShortDescription = "Chugga Chugga Pew Pew";
    public static string LongDescription  = "Fires high-velocity miniature train engines that spread oil as they travel and explode violently upon impact.";
    public static string Lore             = "The brainchild of a smart alec researcher who was tasked with designing a rail gun that used the most highly-conductive materials available. In a rare case of two wrongs making a right, the heat generated when launching the cheap plastic train projectiles had the tendency to melt them back into petroleum in transit, posing a hilarious fire hazard when properly misused.";

    private static DeadlyDeadlyGoopManager _OilGooper = null;

    private bool _cachedContactImmunity = false;

    public static void Init()
    {
        Lazy.SetupGun<DerailGun>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.B, gunClass: GunClass.RIFLE, reloadTime: 2.2f, ammo: 66, idleFps: 11, reloadFps: 11, shootFps: 2,
            fireAudio: "train_bell_sound", autoPlay: false, smoothReload: 0.1f)
          .SetIdleAudio("steam_engine_a", 1)
          .SetIdleAudio("steam_engine_b", 3, 5, 7)
          .SetReloadAudio("steam_engine_a", 7, 15)
          .SetReloadAudio("steam_engine_b", 9, 11, 13, 17, 19, 21)
          .AddToShop(ModdedShopType.Boomhildr)
          .InitProjectile(GunData.New(sprite: "derail_gun_projectile", clipSize: 1, cooldown: 0.5f, shootStyle: ShootStyle.SemiAutomatic,
            damage: 30.0f, speed: 100f, range: 100f, force: 100f, hitSound: "train_launch_sound", customClip: true))
          .Attach<ExplosiveModifier>(e => e.explosionData =
            Explosions.DefaultLarge.With(damage: 20f, force: 100f, debrisForce: 30f, radius: 3f, preventPlayerForce: false))
          .Attach<GoopModifier>(g => {
            g.goopDefinition         = EasyGoopDefinitions.OilDef;
            g.SpawnGoopOnCollision   = true;
            g.CollisionSpawnRadius   = 5f;
            g.SpawnGoopInFlight      = true;
            g.InFlightSpawnRadius    = 2f;
            g.InFlightSpawnFrequency = 0.01f;})
          .AttachTrail("derail_gun_beam", fps: 15, cascadeTimer: 2f * C.FRAME, softMaxLength: 1f, destroyOnEmpty: true,
            boneSpawnOffset: new Vector2(0, -0.375f));
    }

    public override void OwnedUpdate(GameActor owner, GunInventory inventory)
    {
        base.OwnedUpdate(owner, inventory);
        if (this.PlayerOwner is PlayerController player && player.CurrentGun is Gun gun && gun.PickupObjectId == (int)Items.AlienEngine)
          EnableContactImmunity(player);
    }

    private void EnableContactImmunity(PlayerController player)
    {
        if (this._cachedContactImmunity)
            return;
        this._cachedContactImmunity = true;
        player.SetImmuneToContactDamage(true, Synergy.TANK_ENGINE.SynergyName());
    }

    private void DisableContactImmunity(PlayerController player)
    {
        if (!this._cachedContactImmunity)
            return;
        this._cachedContactImmunity = false;
        player.SetImmuneToContactDamage(false, Synergy.TANK_ENGINE.SynergyName());
    }

    public override void OnSwitchedToThisGun()
    {
        base.OnSwitchedToThisGun();
        if (this.PlayerOwner is PlayerController player)
          DisableContactImmunity(player);
    }

    public override void OnReloadPressed(PlayerController player, Gun gun, bool manualReload)
    {
        base.OnReloadPressed(player, gun, manualReload);
        if (manualReload && gun.DefaultModule.numberOfShotsInClip == Mathf.Min(gun.ClipShotsRemaining, gun.AdjustedMaxAmmo))
            gun.gameObject.PlayUnique("toy_train_whistle_sound");
    }

    public override void OnPlayerPickup(PlayerController player)
    {
        base.OnPlayerPickup(player);
        player.healthHaver.ModifyDamage += this.OnMightTakeDamage;
        player.OnReceivedDamage += this.OnReceivedDamage;
        gun.SetAnimationFPS(gun.idleAnimation, 11); // don't need to use SetIdleAnimationFPS() outside of Initializer
        gun.spriteAnimator.Play();
    }

    private void OnMightTakeDamage(HealthHaver haver, HealthHaver.ModifyDamageEventArgs args)
    {
        if (haver.gameActor is not PlayerController player)
            return;
        if (!player.CurrentGun || !player.CurrentGun.IsFiring || player.CurrentGun.PickupObjectId != (int)Items.AlienEngine)
            return;
        if (!player.HasSynergy(Synergy.TANK_ENGINE))
            return;
        args.ModifiedDamage = 0f;
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        base.OnDroppedByPlayer(player);
        DisableContactImmunity(player);
        player.healthHaver.ModifyDamage -= this.OnMightTakeDamage;
        player.OnReceivedDamage -= this.OnReceivedDamage;
        gun.SetAnimationFPS(gun.idleAnimation, 0); // don't need to use SetIdleAnimationFPS() outside of Initializer
        gun.spriteAnimator.StopAndResetFrameToDefault();
    }

    public override void OnDestroy()
    {
        if (this.PlayerOwner)
        {
            DisableContactImmunity(this.PlayerOwner);
            this.PlayerOwner.healthHaver.ModifyDamage -= this.OnMightTakeDamage;
            this.PlayerOwner.OnReceivedDamage -= this.OnReceivedDamage;
        }
        base.OnDestroy();
    }

    private void OnReceivedDamage(PlayerController player)
    {
        if (!player.HasSynergy(Synergy.TROLLEY_PROBLEM))
            return;
        if (player.GetPassive((int)Items.TurtleProblem) is not MulticompanionItem tp)
            return;
        for (int i = 0; i < 5; ++i)
            tp.CreateNewCompanion(player);
    }

    public override void Update()
    {
        base.Update();
        if (!this.PlayerOwner || !this.PlayerOwner.AcceptingNonMotionInput)
            return;
        if (!this.gun.IsReloading && this.gun.ClipShotsRemaining < Mathf.Min(this.gun.ClipCapacity, this.gun.CurrentAmmo))
            this.gun.Reload(); // force reload immediately after firing to prevent single frame of idle animation looking funny
        if (this.Mastered)
        {
            if (!_OilGooper)
                _OilGooper = DeadlyDeadlyGoopManager.GetGoopManagerForGoopType(EasyGoopDefinitions.GreenOilGoop);
            _OilGooper.AddGoopCircle(this.PlayerOwner.SpriteBottomCenter.XY() - this.PlayerOwner.m_currentGunAngle.ToVector(1f), 0.75f);
        }
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        if (this.Mastered)
            projectile.GetComponent<GoopModifier>().goopDefinition = EasyGoopDefinitions.GreenOilGoop;
    }
}
