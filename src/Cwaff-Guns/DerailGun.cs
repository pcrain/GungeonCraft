
namespace CwaffingTheGungy;

public class DerailGun : CwaffGun
{
    public static string ItemName         = "Derail Gun";
    public static string ShortDescription = "Chugga Chugga Pew Pew";
    public static string LongDescription  = "Fires high-velocity miniature train engines that spread oil as they travel and explode violently upon impact.";
    public static string Lore             = "The brainchild of a smart alec researcher who was tasked with designing a rail gun that used the most highly-conductive materials available. In a rare case of two wrongs making a right, the heat generated when launching the cheap plastic train projectiles had the tendency to melt them back into petroleum in transit, posing a hilarious fire hazard when properly misused.";

    private static DeadlyDeadlyGoopManager _OilGooper = null;

    public static void Init()
    {
        Lazy.SetupGun<DerailGun>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.B, gunClass: GunClass.RIFLE, reloadTime: 2.2f, ammo: 66, idleFps: 11, reloadFps: 11, shootFps: 2,
            fireAudio: "train_bell_sound", autoPlay: false)
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
        if (!_OilGooper)
            _OilGooper = DeadlyDeadlyGoopManager.GetGoopManagerForGoopType(EasyGoopDefinitions.GreenOilGoop);
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
        player.healthHaver.ModifyDamage -= this.OnMightTakeDamage;
        player.OnReceivedDamage -= this.OnReceivedDamage;
        gun.SetAnimationFPS(gun.idleAnimation, 0); // don't need to use SetIdleAnimationFPS() outside of Initializer
        gun.spriteAnimator.StopAndResetFrameToDefault();
    }

    public override void OnDestroy()
    {
        if (this.PlayerOwner)
        {
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
        if (_OilGooper && this.PlayerOwner.HasSynergy(Synergy.MASTERY_DERAIL_GUN))
            _OilGooper.AddGoopCircle(this.PlayerOwner.SpriteBottomCenter.XY() - this.PlayerOwner.m_currentGunAngle.ToVector(1f), 0.75f);
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        if (_OilGooper && this.PlayerOwner && this.PlayerOwner.HasSynergy(Synergy.MASTERY_DERAIL_GUN))
            projectile.GetComponent<GoopModifier>().goopDefinition = EasyGoopDefinitions.GreenOilGoop;
    }

    [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.ReceivesTouchDamage), MethodType.Getter)]
    private class PlayerControllerReceivesTouchDamagePatch
    {
        static bool Prefix(PlayerController __instance, ref bool __result)
        {
            if (!__instance.HasSynergy(Synergy.TANK_ENGINE) || __instance.CurrentGun.PickupObjectId != (int)Items.AlienEngine)
                return true; // call the original method
            __result = false; // change the original result
            return false;    // skip the original method
        }
    }
}
