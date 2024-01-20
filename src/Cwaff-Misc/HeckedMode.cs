namespace CwaffingTheGungy;


/* TODO:
    - Foyer Awake seems to be causing issues in Gunfig on floor 2
    - whatever gun uses HoveringGunSynergyProcessor is broken
*/

public static class HeckedMode
{
    public static bool HeckedModeEnabled = false; // the world is almost ready o.o

    public readonly static List<int> HeckedModeGunWhiteList = new(){
        // Unfair Hitscan D:
        (int)Items.StrafeGun,
        (int)Items.GlassCannon,  // almost fair due to cooldown, but not quite
        (int)Items.DuelingLaser,
        (int)Items.Trident,
        (int)Items.Rattler,
        (int)Items.MineCutter,
        (int)Items.FlashRay,
        (int)Items.ShockRifle, // robot's amusingly immune, but still
        (int)Items.LaserRifle,
        (int)Items.LightGun,
        (int)Items.SniperRifle,
        (int)Items.HeckBlaster,

        // Terrifying O.O
        (int)Items.MrAccretionJr,  // but hilarious
        (int)Items.MoonlightTiara,
        (int)Items.TheFatLine,
        // (int)Items.TripleGunForm3,  // normal beam + audio issues (but it's hilariously threatening and makes for good content)
        (int)Items.Shell,
        (int)Items.Bullet,
        (int)Items.Gunzheng,
        (int)Items.SuperMeatGun,
        (int)Items.Banana,
        (int)Items.TheScrambler,  // it's always the quiet ones (and the homing ones)
        (int)Items.Gungine,
        (int)Items.BrickBreaker,
        (int)Items.BaitLauncher,  // tigers D:
        (int)Items.ChargeShot,  // terrifying charge shot o.o
        (int)Items.LuxinCannon,  // only mildly bad at first, but upgrades to large shots sometimes at random o.o
        (int)Items.GreyMauser,
        (int)Items.GungeonAnt,
        (int)Items.TheMembrane,
        (int)Items.Dragunfire,
        (int)Items.ShotgunFullOfHate,
        (int)Items.RubeAdynePrototype,
        (int)Items.CrescentCrossbow,
        (int)Items.SeriousCannon,
        (int)Items.Com4nd0,
        (int)Items.RubeAdyneMk2,
        (int)Items.VulcanCannon,
        (int)Items.RobotsLeftHand,
        (int)Items.GrenadeLauncher,
        (int)Items.YariLauncher,
        (int)Items.VoidMarshal,
        (int)Items.SunlightJavelin,

        // Ouchie
        (int)Items.Elimentaler,
        (int)Items.BigShotgun,  // can team kill
        (int)Items.VoidCoreCannon,  // can't hit other enemies, but arcs towards them
        (int)Items.TurboGun,
        (int)Items.RadGun,
        (int)Items.KnightsGun,
        (int)Items.ThePredator,
        (int)Items.VorpalGun,  // can team kill
        (int)Items.TripleGunForm2,
        (int)Items.HyperLightBlaster,
        (int)Items.MicrotransactionGun,
        (int)Items.GummyGun,
        (int)Items.Shellegun,
        (int)Items.Phoenix,
        (int)Items.Sling,
        (int)Items.BetrayersShield,
        (int)Items.ShotgunFullOfLove,
        (int)Items.Excaliber,
        (int)Items.RcRocket,
        (int)Items.Snakemaker,
        (int)Items.CompressedAirTank,  // sharks; almost balanced and quite entertaining
        (int)Items.Shotgrub,
        (int)Items.Huntsman,
        (int)Items.Buzzkill,
        (int)Items.Gunther,
        (int)Items.Pitchfork,
        (int)Items.Silencer,
        (int)Items.TheEmperor,
        (int)Items.Corsair,  // large blinking Xs on destruction
        (int)Items.Fightsabre,
        (int)Items.HegemonyCarbine,
        (int)Items.Wristbow,  // imagine needing to charge
        (int)Items.IceBreaker,
        (int)Items.Cold45,
        (int)Items.PlaguePistol,
        (int)Items.MachineFist,
        (int)Items.TheJudge,
        (int)Items.Crestfaller,
        (int)Items.Tangler,
        (int)Items.BigIron,
        (int)Items.LaserLotus,
        (int)Items.Trashcannon,
        (int)Items.TheKiln,
        (int)Items.TShirtCannon,
        (int)Items.WitchPistol,
        (int)Items.Glacier,
        (int)Items.M16,
        (int)Items.Akey47,
        (int)Items.Mac10,
        (int)Items.OldGoldie,
        (int)Items.RobotsRightHand,
        (int)Items.UnfinishedGun,
        (int)Items.BudgetRevolver,
        (int)Items.BundleOfWands,
        (int)Items.Saa,
        (int)Items.Jolter,
        (int)Items.MachinePistol,
        (int)Items.TrankGun,
        (int)Items.Magnum,
        (int)Items.SmileysRevolver,
        (int)Items.Klobbe,
        (int)Items.M1911,
        (int)Items.M1, // hitscan but slowish rate of fire (slightly faster than awp)
        (int)Items.DungeonEagle,
        (int)Items.Bow,
        (int)Items.Zorgun,
        (int)Items.Awp,  // hitscan but slowish rate of fire
        (int)Items.Screecher,
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
        (int)Items.HighKaliber,
        (int)Items.Gunner,
        (int)Items.StoneDome,  // generally avoids the player (dome does not appear on top of enemies' heads though)
        (int)Items.Teapot,
        (int)Items.TheExotic,  // hard to dodge a little but homing missile kill enemies
        (int)Items.TripleGun,
        (int)Items.Poxcannon,
        (int)Items.Devolver,
        (int)Items.Anvillain,
        (int)Items.Derringer,
        (int)Items.MassShotgun,
        (int)Items.LilBomber,
        (int)Items.FlareGun,
        (int)Items.GildedHydra,
        (int)Items.Helix,
        (int)Items.Particulator, // as dangerous to other enemies as it is to you
        (int)Items.SawedOff,  // only scary at close range
        (int)Items.PeaShooter,
        (int)Items.SerManuelsRevolver,
        (int)Items.WinchesterRifle,
        (int)Items.Grasschopper,
        (int)Items.RustySidearm,
        (int)Items.RogueSpecial,
        (int)Items.MarineSidearm,
        (int)Items.Deck4rd,
        (int)Items.Makarov,
        (int)Items.Colt1851,
        (int)Items._38Special,
        (int)Items.VoidShotgun,
        (int)Items.AuGun,
        (int)Items.RegularShotgun,
        (int)Items.TearJerker,
        (int)Items.DartGun,
        (int)Items.ShadesRevolver,
        (int)Items.Thunderclap,
        (int)Items.DuelingPistol,
        (int)Items.Barrel,
        (int)Items.MagicLamp,
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

        // Easy to Deal With
        (int)Items.BoxingGlove,
        (int)Items.QuadLaser,
        (int)Items.Casey, // actually damages unlike Blasphemy, but still funny
        (int)Items.GunslingersAshes,
        (int)Items.BubbleBlaster,

        // Ineffective
        (int)Items.Blasphemy, // no damage except from collision

        // Hilarious
        (int)Items.Camera, // free room clear
        (int)Items.BulletBore,
        (int)Items.CatClaw, // barely threatening and very goofy looking
        (int)Items.MolotovLauncher,

        // Semi-broken
        // (int)Items.ChamberGun,  // very possibly works, but only tested the 1st chamber
        // (int)Items.CombinedRifle, // null derefs after first volley is fired
        // (int)Items.ReallySpecialLute,  // overlapping sounds
        // (int)Items.StaffOfFirepower, // null derefs after first volley is fired
        // (int)Items.TripleCrossbow, // null derefs after first volley is fired
        // (int)Items.ElTigre, // very noisy and a little silly projectile orbiting behavior, but seems fine otherwise
        // (int)Items.AlienEngine,  // no knockback, underwhelming when not at point blank
        // (int)Items.FaceMelter, // works fine, but sounds never stop playing
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
        // (int)Items.Singularity, // doesn't work at all
        // (int)Items.CharmedBow, // doesn't work at all
        // (int)Items.ZillaShotgun, // doesn't work at all
        // (int)Items.CompositeGun, // doesn't work at all
        // (int)Items.TrickGun, // alt form doesn't work at all
        // (int)Items.MakeshiftCannon, // doesn't work at all
        // (int)Items.Starpew, // doesn't work at all
        // (int)Items.Ac15, // doesn't work at all
        // (int)Items.LifeOrb, // doesn't work at all
        // (int)Items.KrullerGlaive, // doesn't work at all

        // // Beam broken
        // (int)Items.WoodBeam,
        // (int)Items.AbyssalTentacle, // GOOD LORD THE HORROR O_O
        // (int)Items.Disintegrator, // beams stay in place on screen after firing and loop their firing sound nonstop o.o
        // (int)Items.ProtonBackpack,  // beams weapons are very evidently a mistake
        // (int)Items.ScienceCannon, // yup, still bad
        // (int)Items.FossilizedGun,
        // (int)Items.Plunger,
    };

    private static Hook _EnemyAwakeHook;
    // private static Hook _EnemyShootHook;
    private static ILHook _DisablePrefireAnimationHook;
    private static ILHook _DisablePrefireStateHook;

    internal static readonly string _CONFIG_KEY = "Hecked Mode";

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

        CwaffEvents.BeforeRunStart += () => {  // load hecked mode status before the start of each run
            HeckedModeEnabled = (CwaffConfig._Gunfig.Value(_CONFIG_KEY) != "Disabled");
        };
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
        if (HeckedModeEnabled && !enemy.IsABoss())
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
