namespace CwaffingTheGungy;

public static class HeckedMode
{
    public static bool HeckedModeEnabled = false; // the world isn't ready yet o.o

    // public readonly static List<int> HeckedModeGunWhiteList = new(){
    //     (int)Items.SniperRifle,
    // };
    public readonly static List<int> HeckedModeGunWhiteList = new(){
        // Unfair Hitscan D:
        (int)Items.LightGun,
        (int)Items.SniperRifle,

        // Terrifying O.O
        (int)Items.Com4nd0,
        (int)Items.RubeAdyneMk2,
        (int)Items.VulcanCannon,
        (int)Items.RobotsLeftHand,
        (int)Items.GrenadeLauncher,
        (int)Items.YariLauncher,
        (int)Items.VoidMarshal,
        (int)Items.SunlightJavelin,

        // Ouchie
        (int)Items.Siren,
        (int)Items.FrostGiant,
        (int)Items.Hexagun,
        (int)Items.PlaguePistol,
        (int)Items.Trashcannon,
        (int)Items.H4mmer,
        (int)Items.NailGun,
        (int)Items.Blooper,
        (int)Items.BeeHive,
        (int)Items.Thompson,
        (int)Items.Slinger,
        (int)Items.Ak47,
        (int)Items.ElephantGun,
        (int)Items.FlameHand,
        (int)Items.Cactus,
        (int)Items.WindUpGun,
        (int)Items.Origuni,
        (int)Items.Snowballer,

        // Balanced Enough
        (int)Items.BalloonGun,
        (int)Items.Tetrominator,
        (int)Items.PulseCannon,
        (int)Items.Rpg,
        (int)Items.HegemonyRifle,
        (int)Items.Polaris,
        (int)Items.Winchester,
        (int)Items.StickyCrossbow,
        (int)Items.Patriot,  // doesn't increase in speed like the player's
        (int)Items.Stinger,
        (int)Items.Shotbow,
        (int)Items.Mahoguny,

        // Goofy
        (int)Items.MolotovLauncher,
        (int)Items.BubbleBlaster,

        // Semi-broken
        // (int)Items.Mailbox, // final package projectile doesn't seem to work quite right
        // (int)Items.VoidCoreAssaultRifle, // slow shoot speed, burst not respected
        // (int)Items.AlienSidearm, // only fires the large scary projectiles
        // (int)Items.Blunderbuss, // only puts out one projectile
        // (int)Items.Jk47, // very noisy on floor entrance, but seems fine otherwise

        // // Beeg broken
        // (int)Items.SkullSpitter, // invariably homes in on other enemies
        // (int)Items.AlienEngine, // AI doesn't fire until getting really close, and behaviorspeculators mess up from there
        // (int)Items.LowerCaseR, // bursts don't seem to work quite right, no sound effects
        // (int)Items.DirectionalPad, // same as lowercaser
        // (int)Items.Railgun, // completely non-functional, doesn't even appear on enemy sprites
        // (int)Items.PrototypeRailgun,  // same as railgun
        // (int)Items.CobaltHammer, // enemies literally just self-destruct. would put it in goofy, but this has no function
        // (int)Items.CrownOfGuns, // doesn't appear on heads, behaviorspeculators mess up eventually

        // // Beam broken
        // (int)Items.Disintegrator, // beams stay in place on screen after firing and loop their firing sound nonstop o.o
        // (int)Items.ProtonBackpack,  // beams weapons are very evidently a mistake
        // (int)Items.ScienceCannon, // yup, still bad

        // Testing
        // (int)Items.RobotsLeftHand,
        // (int)Items.
    };

    private static Hook _EnemyAwakeHook;
    // private static Hook _EnemyShootHook;
    private static ILHook _DisablePrefireAnimationHook;
    private static ILHook _DisablePrefireStateHook;

    public static void Init()
    {
        _EnemyAwakeHook = new Hook(
            typeof(AIActor).GetMethod("Awake", BindingFlags.Public | BindingFlags.Instance),
            typeof(HeckedMode).GetMethod("OnEnemyPreAwake"));
        _DisablePrefireAnimationHook = new ILHook(
            typeof(ShootGunBehavior).GetMethod("Start", BindingFlags.Instance | BindingFlags.Public),
            DisablePrefireAnimationDuringHeckedModeIL
            );
        _DisablePrefireStateHook = new ILHook(
            typeof(ShootGunBehavior).GetMethod("ContinuousUpdate", BindingFlags.Instance | BindingFlags.Public),
            DisablePrefireStateDuringHeckedModeIL
            );
        // _EnemyShootHook = new Hook(
        //     typeof(AIShooter).GetMethod("Shoot", BindingFlags.Public | BindingFlags.Instance),
        //     typeof(HeckedMode).GetMethod("OnEnemyShoot"));
    }

    // public static void OnEnemyShoot(Action<AIShooter, string> action, AIShooter shooter, string overrideBulletName)
    // {
    //     if (shooter.aiActor.EnemyGuid == Enemies.BulletKin)
    //     {
    //         overrideBulletName = null;
    //     }
    //     action(shooter, overrideBulletName);
    // }

    // disable prefire animations in hecked mode since they mess with fire rate
    private static bool HeckedModeShouldSkipPrefireAnimationCheck(ShootGunBehavior sgb, string s)
    {
        return HeckedModeEnabled || string.IsNullOrEmpty(s);
    }

    private static bool HeckedModeShouldSkipPrefireStateCheck(bool wouldSkipAnyway)
    {
        return HeckedModeEnabled || wouldSkipAnyway;
    }

    private static void DisablePrefireStateDuringHeckedModeIL(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);
        if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt<AIShooter>("get_IsPreFireComplete")))
            return; // couldn't find the appropriate hook

        // we have a brfalse immediately after us that skips the method we want to call, so just replace that with out own method
        cursor.Emit(OpCodes.Call, typeof(HeckedMode).GetMethod("HeckedModeShouldSkipPrefireStateCheck", BindingFlags.Static | BindingFlags.NonPublic)); // replace it with our own
    }

    private static void DisablePrefireAnimationDuringHeckedModeIL(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);
        if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<Gun>("enemyPreFireAnimation")))
            return; // couldn't find the appropriate hook

        cursor.Remove(); // remove the string.IsNullOrEmpty check
        cursor.Emit(OpCodes.Ldarg_0); // load the player instance as arg0
        cursor.Emit(OpCodes.Call, typeof(HeckedMode).GetMethod("HeckedModeShouldSkipPrefireAnimationCheck", BindingFlags.Static | BindingFlags.NonPublic)); // replace it with our own
    }

    public static void OnEnemyPreAwake(Action<AIActor> action, AIActor enemy)
    {
        if (HeckedModeEnabled)
        {
            Items replacementGunId = (Items)HeckedModeGunWhiteList.ChooseRandom();
            enemy.HeckedShootGunBehavior(ItemHelper.Get(replacementGunId) as Gun);
        }
        action(enemy);
    }

    public static void HeckedShootGunBehavior(this AIActor enemy, Gun replacementGun)
    {
        if (enemy.aiShooter is not AIShooter shooter)
            return;

        shooter.equippedGunId = replacementGun.PickupObjectId;
        shooter.customShootCooldownPeriod = 0f;
        shooter.bulletName = null;
        foreach (AttackBehaviorBase attack in shooter.behaviorSpeculator.AttackBehaviors)
        {
            if (attack is not ShootGunBehavior pewpew)
                continue;
            // ETGModConsole.Log($"  found attack behavior with cooldown {pewpew.Cooldown}");
            pewpew.WeaponType            = WeaponType.AIShooterProjectile;
            pewpew.OverrideBulletName    = null; // must be null to allow firing normal gun projectiles
            pewpew.m_preFireTime         = 0f; // NECESSARY: some guns have custom enemy animations that prevent them from firing at their full rate

            pewpew.Cooldown              = 0f;
            pewpew.GroupCooldownVariance = 0f;
            pewpew.RespectReload         = true;
            pewpew.EmptiesClip           = true;
            pewpew.LeadAmount            = 0f; // don't let them shoot ahead of us...that's too mean for now
            pewpew.LeadChance            = 0f; // don't let them shoot ahead of us...that's too mean for now
            pewpew.TimeBetweenShots      = replacementGun.DefaultModule.cooldownTime;
            pewpew.MagazineCapacity      = replacementGun.ClipCapacity;
            pewpew.ReloadSpeed           = replacementGun.reloadTime;
            pewpew.Range                 = replacementGun.DefaultModule.projectiles[0].baseData.range;

            // ETGModConsole.Log($"replaced gun {replacementGun.name} with cooldown {replacementGun.DefaultModule.cooldownTime}");

            // pewpew.EmptiesClip                  = false;  // setting to false prevents dumb firing behaviors, but also makes them fire ludicrously fast
            pewpew.RequiresLineOfSight          = true;
            pewpew.AimAtFacingDirectionWhenSafe = true;
            pewpew.StopDuringAttack             = true;  // enemies shouldn't move while attacking

            /* Default bulletkin behavior

            "GroupCooldownVariance"        : 0.200000002980232,
            "LineOfSight"                  : true,
            "WeaponType"                   : "AIShooterProjectile",
            "OverrideBulletName"           : "default",
            "BulletScript"                 : null,
            "FixTargetDuringAttack"        : false,
            "StopDuringAttack"             : false,
            "LeadAmount"                   : 0,
            "LeadChance"                   : 1,
            "RespectReload"                : true,
            "MagazineCapacity"             : 6,
            "ReloadSpeed"                  : 2,
            "EmptiesClip"                  : false,
            "SuppressReloadAnim"           : false,
            "TimeBetweenShots"             : -1,
            "PreventTargetSwitching"       : false,
            "OverrideAnimation"            : null,
            "OverrideDirectionalAnimation" : null,
            "HideGun"                      : false,
            "UseLaserSight"                : false,
            "UseGreenLaser"                : false,
            "PreFireLaserTime"             : -1,
            "AimAtFacingDirectionWhenSafe" : false,
            "Cooldown"                     : 1.60000002384186,
            "CooldownVariance"             : 0,
            "AttackCooldown"               : 0,
            "GlobalCooldown"               : 0,
            "InitialCooldown"              : 0,
            "InitialCooldownVariance"      : 0,
            "GroupName"                    : null,
            "GroupCooldown"                : 0,
            "MinRange"                     : 0,
            "Range"                        : 12,
            "MinWallDistance"              : 0,
            "MaxEnemiesInRoom"             : 0,
            "MinHealthThreshold"           : 0,
            "MaxHealthThreshold"           : 1,
            "HealthThresholds"             : [],
            "AccumulateHealthThresholds"   : true,
            "targetAreaStyle"              : null,
            "IsBlackPhantom"               : false,
            "resetCooldownOnDamage"        : null,
            "RequiresLineOfSight"          : false,
            "MaxUsages"                    : 0,
            "$type"                        : "ShootGunBehavior"

            */
        }
    }

    // public static void AttachStats(this AIShooter shooter)
    // {
    //     ETGModConsole.Log($"  equipped gun is {shooter.EquippedGun.name}");
    //     ETGModConsole.Log($"  current gun is {shooter.CurrentGun.name}");
    //     ETGModConsole.Log($"  handObject is {shooter.handObject != null}");
    //     for (int i = shooter.sprite.attachedRenderers.Count() - 1; i >= 0; --i)
    //         ETGModConsole.Log($"    sprite attached: {shooter.sprite.attachedRenderers[i].name}");
    //     for (int i = shooter.CurrentGun.GetSprite().attachedRenderers.Count() - 1; i >= 0; --i)
    //         ETGModConsole.Log($"    gun attached: {shooter.CurrentGun.GetSprite().attachedRenderers[i].name}");
    //     for (int i = shooter.m_attachedHands.Count() - 1; i >= 0; --i)
    //         ETGModConsole.Log($"    hands attached: {shooter.m_attachedHands[i].name}");

    //     for (int i = shooter.transform.childCount - 1; i >= 0; --i)
    //     {
    //         Transform t = shooter.transform.GetChild(i);
    //         ETGModConsole.Log($"    transform attached: {t.name}");
    //         for (int ti = t.gameObject.transform.childCount - 1; ti >= 0; --ti)
    //         {
    //             Transform ts = t.gameObject.transform.GetChild(ti);
    //             ETGModConsole.Log($"      transform subattached: {ts.name}");
    //         }
    //     }
    // }

    // If an enemy has already run their Awake() method, replacing their guns gets a lot more complicated
    // All of this code is basically undoing AIShooter.Initialize() in reverse order
    public static void ReplaceGun(this AIActor enemy, Items replacementGunId)
    {
        if (enemy.aiShooter is not AIShooter shooter)
            return;

        // ETGModConsole.Log($"  BUBBLETIME");
        // ETGModConsole.Log($"before init: ");
        // shooter.AttachStats();

        for (int i = shooter.transform.childCount - 1; i >= 0; --i)
        {
            Transform t = shooter.transform.GetChild(i);
            if (t.gameObject.GetComponent<PlayerHandController>() is PlayerHandController hc)
            {
                if (shooter.healthHaver)
                {
                    tk2dSprite hcsprite = hc.GetComponent<tk2dSprite>();
                    if (shooter.healthHaver.bodySprites.Contains(hcsprite))
                        shooter.healthHaver.bodySprites.Remove(hcsprite);
                }
                if (shooter.m_attachedHands.Contains(hc))
                    shooter.m_attachedHands.Remove(hc);
                // hc.attachPoint = null;
                shooter.CurrentGun.GetSprite().DetachRenderer(hc.sprite);
                // UnityEngine.Object.Destroy(hc);
                t.parent = null;
                UnityEngine.Object.Destroy(t.gameObject);
            }
            else if (t.name == "GunAttachPoint")
            {
                for (int ti = t.gameObject.transform.childCount - 1; ti >= 0; --ti)
                {
                    Transform ts = t.gameObject.transform.GetChild(ti);
                    ts.parent = null;
                    UnityEngine.Object.Destroy(ts.gameObject);
                }
            }
        }
        shooter.sprite.DetachRenderer(shooter.CurrentGun.GetSprite());
        SpriteOutlineManager.RemoveOutlineFromSprite(shooter.CurrentGun.GetSprite());

        shooter.equippedGunId = (int)replacementGunId;
        // shooter.customShootCooldownPeriod = 0f;
        shooter.m_hasCachedGun = false;
        shooter.bulletName = null;

        // Reset the gunattachpoint by inverting the Initialize() calculations for attachPointCachedPosition (unnecessary?)
        if (shooter.attachPointCachedPosition != default(Vector3))
        {
            // ETGModConsole.Log($"cur attach point is {shooter.attachPointCachedPosition}");
            Vector3 originalGunAttachPoint = shooter.attachPointCachedPosition - (Vector3)PhysicsEngine.PixelToUnit(shooter.overallGunAttachOffset);
            shooter.gunAttachPoint.localPosition = originalGunAttachPoint;
            // ETGModConsole.Log($"new attach point is {originalGunAttachPoint}");
        }

        // Override the attack behaviors
        foreach (AttackBehaviorBase attack in shooter.behaviorSpeculator.AttackBehaviors)
        {
            if (attack is not ShootGunBehavior pewpew)
                continue;
            pewpew.WeaponType         = WeaponType.AIShooterProjectile;
            pewpew.OverrideBulletName = null;
        }

        shooter.Initialize();
        // ETGModConsole.Log($"after init: ");
        // shooter.AttachStats();
    }

}
