namespace CwaffingTheGungy;

/* TODO:
    - fix audio on all noisy weapons to stop playing when appropriate
    - fix charged weapons to actually respect charge
    - fix beam weapons to not persist when restarting
    - fix burst weapons to fire more than one shot
    - test Bsg

    + fix FindPredictedTargetPosition error (possibly fixed?)
    + fix Gun.ClearCooldowns() null deref (possibly fixed?)

   Retrashed Mode Changes (NEED TO ENABLE IN SetupHeckedMode()):
    + everyone has guns
    + no easy guns
    + every chest is fused
    + enemies ignore stealth
    + 10x shop prices
    + all bosses are jammed
    + no bullet that can kill the future

    x no time slowing (too hard to implement)
*/

public static class HeckedMode
{
    public enum Hecked {
        Disabled,
        Classic,
        Retrashed,
    }

    internal static Hecked _HeckedModeStatus = Hecked.Disabled;

    // public readonly static List<int> HeckedModeGunWhiteList = new(){
    // };
    public readonly static List<int> HeckedModeGunWhiteList = new(){
        // Unfair Hitscan D:
        (int)Items.PrototypeRailgun,
        (int)Items.EyeOfTheBeholster,
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
        (int)Items.Railgun,
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
        (int)Items.ZillaShotgun,
        (int)Items.Elimentaler,
        (int)Items.BigShotgun,  // can team kill
        (int)Items.VoidCoreCannon,  // can't hit other enemies, but arcs towards them
        (int)Items.TurboGun,
        (int)Items.RadGun,
        (int)Items.KnightsGun,
        (int)Items.ThePredator, // spams the debug log, but harmless otherwise
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
        (int)Items.MakeshiftCannon, // surprisingly low fire rate
        (int)Items.KrullerGlaive, // generally avoids the player, but movement is erratic
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
        (int)Items.SkullSpitter, // invariably homes in on other enemies
        (int)Items.Starpew, // only fires a single projectile at a time
        (int)Items.BoxingGlove,
        (int)Items.QuadLaser,
        (int)Items.Casey, // actually damages unlike Blasphemy, but still funny
        (int)Items.GunslingersAshes,
        (int)Items.BubbleBlaster,

        // Hilarious
        (int)Items.Camera, // free room clear
        (int)Items.BulletBore,
        (int)Items.CatClaw, // barely threatening and very goofy looking
        (int)Items.MolotovLauncher,

        // Semi-broken, projectile module related
        // (int)Items.GungeonAnt, // null deref looking up a projectile module
        // (int)Items.CombinedRifle, // null deref looking up a projectile module
        // (int)Items.StaffOfFirepower, // null deref looking up a projectile module
        // (int)Items.TripleCrossbow, // null deref looking up a projectile module

        // Semi-broken, sound-related
        // (int)Items.ReallySpecialLute,  // overlapping sounds
        // (int)Items.FaceMelter, // works fine, but sounds never stop playing
        // (int)Items.ElTigre, // very noisy and a little silly projectile orbiting behavior, but seems fine otherwise

        // Semi-broken
        // (int)Items.Blasphemy, // no damage except from collision, null derefs on slash
        // (int)Items.CharmedBow, // fires every single frame, fixable once i figure out how to incorporate charge time
        // (int)Items.ChamberGun,  // very possibly works, but only tested the 1st chamber
        // (int)Items.AlienEngine,  // no knockback, underwhelming when not at point blank
        // (int)Items.Mailbox, // final package projectile doesn't seem to work quite right
        // (int)Items.VoidCoreAssaultRifle, // slow shoot speed, burst not respected
        // (int)Items.AlienSidearm, // only fires the large scary projectiles
        // (int)Items.Blunderbuss, // only puts out one projectile
        // (int)Items.Jk47, // very noisy on floor entrance, but seems fine otherwise

        // // Beeg broken
        // (int)Items.AlienEngine, // AI doesn't fire until getting really close, and behaviorspeculators mess up from there
        // (int)Items.LowerCaseR, // bursts don't seem to work quite right, no sound effects
        // (int)Items.DirectionalPad, // same as lowercaser
        // (int)Items.CobaltHammer, // enemies literally just self-destruct. would put it in goofy, but this has no function
        // (int)Items.CrownOfGuns, // doesn't appear on heads, behaviorspeculators mess up eventually
        // (int)Items.Singularity, // doesn't work at all
        // (int)Items.CompositeGun, // doesn't work at all, very difficult to get a working projectile from it
        // (int)Items.TrickGun, // alt form doesn't work at all
        // (int)Items.Ac15, // doesn't work at all (null deref looking up a projectile module after charge projectile fix)

        // // Beam broken
        // (int)Items.LifeOrb, // broken like any other beam after deleting LifeOrbGunModifier component
        // (int)Items.WoodBeam,
        // (int)Items.AbyssalTentacle, // GOOD LORD THE HORROR O_O
        // (int)Items.Disintegrator, // beams stay in place on screen after firing and loop their firing sound nonstop o.o
        // (int)Items.ProtonBackpack,  // beams weapons are very evidently a mistake
        // (int)Items.ScienceCannon, // yup, still bad
        // (int)Items.FossilizedGun,
        // (int)Items.Plunger,
    };

    public readonly static int _FirstWeakGun = HeckedModeGunWhiteList.IndexOf((int)Items.MakeshiftCannon);

    internal const string _CONFIG_KEY = "Hecked Mode";

    public static void Init()
    {
        CwaffEvents.BeforeRunStart += SetupHeckedMode;  // load hecked mode status before the start of each run
        CwaffEvents.OnFirstFloorFullyLoaded += OnFirstHeckedFloorLoaded;
    }

    private static void SetupHeckedMode()
    {
        string heckedConfig = CwaffConfig._Gunfig.Value(_CONFIG_KEY);
        _HeckedModeStatus = heckedConfig switch {
            "Disabled"  => Hecked.Disabled,
            "Hecked"    => Hecked.Classic,
            // "Retrashed" => Hecked.Retrashed,  //NOTE: re-enable once Retrashed Mode is ready
            _           => Hecked.Disabled,
        };
    }

    private static void OnFirstHeckedFloorLoaded()
    {
        if (_HeckedModeStatus != Hecked.Retrashed)
            return;

        // 10x shop price multiplier
        GameManager.Instance.PrimaryPlayer.ownerlessStatModifiers.Add(new(){
            statToBoost = PlayerStats.StatType.GlobalPriceMultiplier,
            modifyType = StatModifier.ModifyMethod.MULTIPLICATIVE,
            amount = 10f
        });
        GameManager.Instance.PrimaryPlayer.stats.RecalculateStats(GameManager.Instance.PrimaryPlayer);
    }

    // disable prefire animations in hecked mode since they mess with fire rate
    private static bool HeckedModeShouldSkipPrefireAnimationCheck(string s)
    {
        return (_HeckedModeStatus != Hecked.Disabled) || string.IsNullOrEmpty(s);
    }

    private static bool HeckedModeShouldSkipPrefireStateCheck(bool wouldSkipAnyway)
    {
        return (_HeckedModeStatus != Hecked.Disabled) || wouldSkipAnyway;
    }

    [HarmonyPatch(typeof(ShootGunBehavior), nameof(ShootGunBehavior.ContinuousUpdate))]
    private class DisablePrefireStateDuringHeckedModePatch
    {
        [HarmonyILManipulator]
        private static void DisablePrefireStateDuringHeckedModeIL(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt<AIShooter>("get_IsPreFireComplete")))
                return; // couldn't find the appropriate hook

            // we have a brfalse immediately after us that skips the method we want to call, so just replace that with out own method
            cursor.Emit(OpCodes.Call, typeof(HeckedMode).GetMethod("HeckedModeShouldSkipPrefireStateCheck", BindingFlags.Static | BindingFlags.NonPublic)); // replace it with our own
        }
    }

    [HarmonyPatch(typeof(ShootGunBehavior), nameof(ShootGunBehavior.Start))]
    private class DisablePrefireAnimationDuringHeckedModePatch
    {
        [HarmonyILManipulator]
        private static void DisablePrefireAnimationDuringHeckedModeIL(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<Gun>("enemyPreFireAnimation")))
                return; // couldn't find the appropriate hook

            cursor.Remove(); // remove the string.IsNullOrEmpty check
            cursor.Emit(OpCodes.Call, typeof(HeckedMode).GetMethod("HeckedModeShouldSkipPrefireAnimationCheck", BindingFlags.Static | BindingFlags.NonPublic)); // replace it with our own
        }
    }

    [HarmonyPatch(typeof(AIActor), nameof(AIActor.Awake))]
    private class HeckedEnemyAwakePatch
    {
        static void Prefix(AIActor __instance)
        {
            if ((_HeckedModeStatus == Hecked.Disabled) || __instance.IsABoss())
                return;

            Items replacementGunId;
            if (_HeckedModeStatus == Hecked.Retrashed)
                replacementGunId = (Items)HeckedModeGunWhiteList[UnityEngine.Random.Range(0, _FirstWeakGun)];
            else
                replacementGunId = (Items)HeckedModeGunWhiteList.ChooseRandom();
            __instance.HeckedShootGunBehavior(replacementGunId.AsGun());
        }
    }

    [HarmonyPatch(typeof(AIShooter), nameof(AIShooter.Initialize))]
    private class HeckedRemoveBadComponentsPatch // Removes some player-only components from guns to make them work with AI
    {
        static void Postfix(AIShooter __instance)
        {
            // ETGModConsole.Log($"    initializing aishooter for {shooter.aiActor.ActorName}");
            // ETGModConsole.Log($"      EquippedGun {(shooter.EquippedGun ? shooter.EquippedGun.EncounterNameOrDisplayName : 'null')}");
            // ETGModConsole.Log($"      CurrentGun {(shooter.CurrentGun ? shooter.CurrentGun.EncounterNameOrDisplayName : 'null')}");
            if (!__instance || __instance.CurrentGun is not Gun gun)
                return;
            gun.GetComponent<HoveringGunSynergyProcessor>().SafeDestroy();                 // fix Blooper, etc.
            gun.GetComponent<MotionTriggeredStatSynergyProcessor>().SafeDestroy();         // fix Gungine, etc.
            gun.GetComponent<TalkingGunModifier>().SafeDestroy();                          // fix Gunther
            gun.GetComponent<GunnerGunController>().SafeDestroy();                         // fix GuNNER
            gun.GetComponent<ShovelGunModifier>().SafeDestroy();                           // fix Knight's Gun
            // gun.GetComponent<LifeOrbGunModifier>().SafeDestroy();                       // fix Life Orb, not useful until beams are fixed in general
            if (gun.GetComponent<StealthOnReloadPressed>() is StealthOnReloadPressed sorp) // fix GreyMauser, etc.
            {
                gun.OnAutoReload -= sorp.HandleReloadPressedSimple;
                gun.OnReloadPressed -= sorp.HandleReloadPressed;
                sorp.SafeDestroy();
            }
            gun.RequiresFundsToShoot = false; // fix Microtransaction gun null deref in DecrementAmmoCost()
            gun.IsTrickGun = false; // fixes Trick Gun null deref in FinishReload()
        }
    }

    // private static bool OnRandomShouldBecomeMimic(Func<SharedDungeonSettings, float, bool> orig, SharedDungeonSettings sds, float overrideChance)
    // {
    //     if (HeckedModeStatus == Hecked.Retrashed)
    //         return orig(sds, 1.0f); // 100% of chests should be mimics in retrashed mode
    //     return orig(sds, overrideChance);
    // }

    [HarmonyPatch(typeof(Chest), nameof(Chest.RoomEntered))]
    private class HeckedFusedChestPatch
    {
        [HarmonyILManipulator]
        private static void FusedChestHookIL(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.After,
              instr => instr.MatchCall<UnityEngine.Random>("get_value"),
              instr => instr.MatchLdloc(0)
              ))
                return; // couldn't find the appropriate hook

            cursor.Emit(OpCodes.Call,
                typeof(HeckedMode).GetMethod("AdjustHeckedFuseTimers", BindingFlags.Static | BindingFlags.NonPublic));
        }
    }

    private static float AdjustHeckedFuseTimers(float original)
    {
        return (_HeckedModeStatus == Hecked.Retrashed) ? 1f : original;
    }

    [HarmonyPatch(typeof(TargetPlayerBehavior), nameof(TargetPlayerBehavior.Update))]
    private class HeckedNoStealthPatch
    {
        [HarmonyILManipulator]
        private static void NoStealthForYouIL(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt<GameActor>("get_IsStealthed")))
                return; // couldn't find the appropriate hook

            cursor.Emit(OpCodes.Call,
                typeof(HeckedMode).GetMethod("IsReallyStealthed", BindingFlags.Static | BindingFlags.NonPublic));
        }
    }

    private static bool IsReallyStealthed(bool stealthed)
    {
        return stealthed && (_HeckedModeStatus != Hecked.Retrashed);
    }

    [HarmonyPatch(typeof(GameManager), nameof(GameManager.GetActivePlayerClosestToPoint))]
    private class HeckedIgnoreStealthPatch // allow enemies to target stealthed players in Retrashed Mode
    {
        static void Prefix(Vector2 point, ref bool allowStealth)
        {
            allowStealth |= (_HeckedModeStatus == Hecked.Retrashed);
        }
    }

    [HarmonyPatch(typeof(AIActor), nameof(AIActor.CheckForBlackPhantomness))]
    private class ForceJammedBossesPatch
    {
        [HarmonyILManipulator]
        private static void ForceJammedBossesIL(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<AIActor>("ForceBlackPhantom")))
                return; // couldn't find the appropriate hook

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Call,
                typeof(HeckedMode).GetMethod("ForceJammedBosses", BindingFlags.Static | BindingFlags.NonPublic));
        }
    }

    [HarmonyPatch(typeof(Gun), nameof(Gun.ClearCooldowns))]
    [HarmonyPatch(typeof(Gun), nameof(Gun.ClearReloadData))]
    private class FixModuleDataPatch
    {
        static void Prefix(Gun __instance)
        {
            if (__instance.Volley != null)
            {
                foreach (ProjectileModule mod in __instance.Volley.projectiles)
                {
                    if (!__instance.m_moduleData.TryGetValue(mod, out ModuleShootData msd))
                    {
                        ETGModConsole.Log($"shoot data wasn't set up for {__instance.gunName}");
                        __instance.m_moduleData[mod] = new();
                    }
                }
            }
            else if (!__instance.m_moduleData.TryGetValue(__instance.singleModule, out ModuleShootData msd))
            {
                ETGModConsole.Log($"shoot data wasn't set up for {__instance.gunName}");
                __instance.m_moduleData[__instance.singleModule] = new();
            }
        }
    }

    private static bool ForceJammedBosses(bool original, AIActor actor)
    {
        return original || ((_HeckedModeStatus == Hecked.Retrashed) && actor && actor.healthHaver && (actor.healthHaver.IsBoss || actor.healthHaver.IsSubboss));
    }

    private static void CopyAIBulletBank(this AIBulletBank me, AIBulletBank other)
    {
        // me.Name                             = other.Name;
        // me.BulletObject                     = other.BulletObject;
        // me.OverrideProjectile               = other.OverrideProjectile;
        // me.ProjectileData                   = other.ProjectileData;
        // me.PlayAudio                        = other.PlayAudio;
        // me.AudioSwitch                      = other.AudioSwitch;
        // me.AudioEvent                       = other.AudioEvent;
        // me.AudioLimitOncePerFrame           = other.AudioLimitOncePerFrame;
        // me.AudioLimitOncePerAttack          = other.AudioLimitOncePerAttack;
        // me.MuzzleFlashEffects               = other.MuzzleFlashEffects;
        // me.MuzzleLimitOncePerFrame          = other.MuzzleLimitOncePerFrame;
        // me.MuzzleInheritsTransformDirection = other.MuzzleInheritsTransformDirection;
        // me.SpawnShells                      = other.SpawnShells;
        // me.ShellTransform                   = other.ShellTransform;
        // me.ShellPrefab                      = other.ShellPrefab;
        // me.ShellForce                       = other.ShellForce;
        // me.ShellForceVariance               = other.ShellForceVariance;
        // me.DontRotateShell                  = other.DontRotateShell;
        // me.ShellGroundOffset                = other.ShellGroundOffset;
        // me.ShellsLimitOncePerFrame          = other.ShellsLimitOncePerFrame;
        // me.rampBullets                      = other.rampBullets;
        // me.rampStartHeight                  = other.rampStartHeight;
        // me.rampTime                         = other.rampTime;
        // me.conditionalMinDegFromNorth       = other.conditionalMinDegFromNorth;
        // me.forceCanHitEnemies               = other.forceCanHitEnemies;
        // me.suppressHitEffectsIfOffscreen    = other.suppressHitEffectsIfOffscreen;
        // me.preloadCount                     = other.preloadCount;
        // me.m_playedAudioThisFrame           = other.m_playedAudioThisFrame;
        // me.m_playedEffectsThisFrame         = other.m_playedEffectsThisFrame;
        // me.m_playedShellsThisFrame          = other.m_playedShellsThisFrame;
        me.Bullets                          = other.Bullets;
        me.useDefaultBulletIfMissing        = other.useDefaultBulletIfMissing;
        me.transforms                       = other.transforms;
        me.OnProjectileCreated              = other.OnProjectileCreated;
        me.OnProjectileCreatedWithSource    = other.OnProjectileCreatedWithSource;
        me.FixedPlayerPosition              = other.FixedPlayerPosition;
    }

    private static void CopyAIShooter(this AIShooter me, AIShooter other)
    {
        me.volley                           = other.volley;
        me.equippedGunId                    = other.equippedGunId;
        me.shouldUseGunReload               = other.shouldUseGunReload;
        me.volleyShellCasing                = other.volleyShellCasing;
        me.volleyShootVfx                   = other.volleyShootVfx;
        me.usesOctantShootVFX               = other.usesOctantShootVFX;
        me.bulletName                       = other.bulletName;
        me.customShootCooldownPeriod        = other.customShootCooldownPeriod;
        me.doesScreenShake                  = other.doesScreenShake;
        me.rampBullets                      = other.rampBullets;
        me.rampStartHeight                  = other.rampStartHeight;
        me.rampTime                         = other.rampTime;
        me.overallGunAttachOffset           = other.overallGunAttachOffset;
        me.flippedGunAttachOffset           = other.flippedGunAttachOffset;
        me.handObject                       = other.handObject;
        me.AllowTwoHands                    = other.AllowTwoHands;
        me.ForceGunOnTop                    = other.ForceGunOnTop;
        me.IsReallyBigBoy                   = other.IsReallyBigBoy;
        me.BackupAimInMoveDirection         = other.BackupAimInMoveDirection;
        me.PostProcessProjectile            = other.PostProcessProjectile;

        me.volleyShootPosition     = null; // other.volleyShootPosition;
        me.volleyShellTransform    = null; // other.volleyShellTransform;
        me.bulletScriptAttachPoint = null; // other.bulletScriptAttachPoint;

        // need our own attach point transforms so we don't muck up bulletkin
        GameObject g         = new GameObject("attachyboi");
        g.transform.position = me.transform.position;
        g.transform.parent   = me.transform;
        me.gunAttachPoint    = g.transform;
        // me.gunAttachPoint          = other.gunAttachPoint;

        // me.volleyShootPosition     = null; //me.gameObject.AddChild(new GameObject());// other.volleyShootPosition ? other.volleyShootPosition.position : Vector3.zero;
        // me.volleyShellTransform    = null; //me.gameObject.AddChild(new GameObject());// other.volleyShellTransform ? other.volleyShellTransform.position : Vector3.zero;
        // me.gunAttachPoint          = null; //me.gameObject.AddChild(new GameObject());// other.gunAttachPoint ? other.gunAttachPoint.position : Vector3.zero;
        // me.bulletScriptAttachPoint = null; //me.gameObject.AddChild(new GameObject());// other.bulletScriptAttachPoint ? other.bulletScriptAttachPoint.position : Vector3.zero;

        // me.volleyShootPosition.position     = other.volleyShootPosition ? other.volleyShootPosition.position : Vector3.zero;
        // me.volleyShellTransform.position    = other.volleyShellTransform ? other.volleyShellTransform.position : Vector3.zero;
        // me.gunAttachPoint.position          = other.gunAttachPoint ? other.gunAttachPoint.position : Vector3.zero;
        // me.bulletScriptAttachPoint.position = other.bulletScriptAttachPoint ? other.bulletScriptAttachPoint.position : Vector3.zero;
    }

    private static ShootGunBehavior CopyShootGunBehavior(this ShootGunBehavior other)
    {
        return new ShootGunBehavior{
            Cooldown                         = other.Cooldown,
            CooldownVariance                 = other.CooldownVariance,
            AttackCooldown                   = other.AttackCooldown,
            GlobalCooldown                   = other.GlobalCooldown,
            InitialCooldown                  = other.InitialCooldown,
            InitialCooldownVariance          = other.InitialCooldownVariance,
            GroupName                        = other.GroupName,
            GroupCooldown                    = other.GroupCooldown,
            MinRange                         = other.MinRange,
            Range                            = other.Range,
            MinWallDistance                  = other.MinWallDistance,
            MaxEnemiesInRoom                 = other.MaxEnemiesInRoom,
            MinHealthThreshold               = other.MinHealthThreshold,
            MaxHealthThreshold               = other.MaxHealthThreshold,
            HealthThresholds                 = other.HealthThresholds,
            AccumulateHealthThresholds       = other.AccumulateHealthThresholds,
            targetAreaStyle                  = other.targetAreaStyle,
            IsBlackPhantom                   = other.IsBlackPhantom,
            resetCooldownOnDamage            = other.resetCooldownOnDamage,
            RequiresLineOfSight              = other.RequiresLineOfSight,
            MaxUsages                        = other.MaxUsages,
            m_cooldownTimer                  = other.m_cooldownTimer,
            m_resetCooldownOnDamageCooldown  = other.m_resetCooldownOnDamageCooldown,
            // m_behaviorSpeculator             = other.m_behaviorSpeculator,
            m_healthThresholdCredits         = other.m_healthThresholdCredits,
            m_lowestRecordedHealthPercentage = other.m_lowestRecordedHealthPercentage,
            m_numTimesUsed                   = other.m_numTimesUsed,

            GroupCooldownVariance            = other.GroupCooldownVariance,
            LineOfSight                      = other.LineOfSight,
            WeaponType                       = other.WeaponType,
            OverrideBulletName               = other.OverrideBulletName,
            BulletScript                     = other.BulletScript,
            FixTargetDuringAttack            = other.FixTargetDuringAttack,
            StopDuringAttack                 = other.StopDuringAttack,
            LeadAmount                       = other.LeadAmount,
            LeadChance                       = other.LeadChance,
            RespectReload                    = other.RespectReload,
            MagazineCapacity                 = other.MagazineCapacity,
            ReloadSpeed                      = other.ReloadSpeed,
            EmptiesClip                      = other.EmptiesClip,
            SuppressReloadAnim               = other.SuppressReloadAnim,
            TimeBetweenShots                 = other.TimeBetweenShots,
            PreventTargetSwitching           = other.PreventTargetSwitching,
            OverrideAnimation                = other.OverrideAnimation,
            OverrideDirectionalAnimation     = other.OverrideDirectionalAnimation,
            HideGun                          = other.HideGun,
            UseLaserSight                    = other.UseLaserSight,
            UseGreenLaser                    = other.UseGreenLaser,
            PreFireLaserTime                 = other.PreFireLaserTime,
            AimAtFacingDirectionWhenSafe     = other.AimAtFacingDirectionWhenSafe,
        };
    }

    private static void ShowAllBehaviors(this AIActor enemy)
    {
        if (!C.DEBUG_BUILD)
            return; // don't print stuff when not in debug mode

        ETGModConsole.Log($"showing behaviors for {enemy.ActorName}");
        if (enemy.behaviorSpeculator is not BehaviorSpeculator bs)
        {
            ETGModConsole.Log($"  no behaviorspeculator");
            return;
        }

        foreach (BehaviorBase bb in bs.TargetBehaviors.EmptyIfNull())
            ETGModConsole.Log($"  target: {bb.GetType().Name}");
        foreach (BehaviorBase bb in bs.MovementBehaviors.EmptyIfNull())
            ETGModConsole.Log($"  movement: {bb.GetType().Name}");
        foreach (BehaviorBase bb in bs.AttackBehaviors.EmptyIfNull())
            ETGModConsole.Log($"  attack: {bb.GetType().Name}");
        foreach (BehaviorBase bb in bs.OtherBehaviors.EmptyIfNull())
            ETGModConsole.Log($"  other: {bb.GetType().Name}");
        foreach (BehaviorBase bb in bs.OverrideBehaviors.EmptyIfNull())
            ETGModConsole.Log($"  override: {bb.GetType().Name}");
    }

    /// <summary>Gives guns to enemies that don't normally have them.</summary>
    public static AIShooter EnableGunShooting(this AIActor enemy, Gun replacementGun)
    {
        _BulletKin ??= EnemyDatabase.GetOrLoadByGuid(Enemies.BulletKin);

        if (!enemy.gameObject.GetComponent<AIBulletBank>())
            enemy.gameObject.AddComponent<AIBulletBank>().CopyAIBulletBank(_BulletKin.bulletBank);

        AIShooter shooter = enemy.gameObject.AddComponent<AIShooter>();
        shooter.CopyAIShooter(_BulletKin.aiShooter);
        shooter.RegenerateCache();

        shooter.equippedGunId = replacementGun.PickupObjectId;
        shooter.customShootCooldownPeriod = 0f;
        shooter.bulletName = null;

        // enemy.ShowAllBehaviors();

        shooter.behaviorSpeculator.TargetBehaviors ??= new();
        if (shooter.behaviorSpeculator.TargetBehaviors.Count == 0)
        {
            shooter.RegenerateCache();
            if (enemy.GetComponent<CompanionController>())
            {
                TargetPlayerBehavior targetPlayerButActuallyEnemies = new TargetPlayerBehavior{};  // why does this target enemies???
                shooter.behaviorSpeculator.TargetBehaviors.Add(targetPlayerButActuallyEnemies);
                targetPlayerButActuallyEnemies.Init(enemy.gameObject, enemy, shooter);
                targetPlayerButActuallyEnemies.Start();
                shooter.behaviorSpeculator.m_behaviors.Add(targetPlayerButActuallyEnemies);
            }
            else
            {
                TargetEnemiesBehavior targetEnemies = new TargetEnemiesBehavior{};  // why does this target players???
                shooter.behaviorSpeculator.TargetBehaviors.Add(targetEnemies);
                targetEnemies.Init(enemy.gameObject, enemy, shooter);
                targetEnemies.Start();
                shooter.behaviorSpeculator.m_behaviors.Add(targetEnemies);
            }
            shooter.RegenerateCache();
        }
        // foreach (AttackBehaviorBase myAttack in shooter.behaviorSpeculator.AttackBehaviors)
        //     shooter.behaviorSpeculator.m_behaviors.Remove(myAttack);
        shooter.behaviorSpeculator.AttackBehaviors ??= new();
        foreach (AttackBehaviorBase defaultAttack in _BulletKin.aiShooter.behaviorSpeculator.AttackBehaviors)
        {
            if (defaultAttack is not ShootGunBehavior defaultPewpew)
                continue;
            ShootGunBehavior myPewpew = defaultPewpew.CopyShootGunBehavior();
            myPewpew.m_behaviorSpeculator = shooter.behaviorSpeculator;
            shooter.behaviorSpeculator.AttackBehaviors.Add(myPewpew);
            myPewpew.Init(enemy.gameObject, enemy, shooter);
            // ETGModConsole.Log($"{myPewpew.m_aiActor!=null}");
            // ETGModConsole.Log($"{myPewpew.m_aiActor.CurrentGun!=null}");
            // Gun gg = PickupObjectDatabase.GetById(myPewpew.m_aiShooter.equippedGunId) as Gun;
            // ETGModConsole.Log($"{gg!=null}");
            // ETGModConsole.Log($"{!string.IsNullOrEmpty(gg.enemyPreFireAnimation)}");
            // ETGModConsole.Log($"{gg.spriteAnimator}");
            myPewpew.Start();
            // ETGModConsole.Log($"survived");
            shooter.behaviorSpeculator.m_behaviors.Add(myPewpew);
            shooter.RegenerateCache();
            break;
        }

        // shooter.behaviorSpeculator.OtherBehaviors ??= new();
        // shooter.behaviorSpeculator.OtherBehaviors.Clear(); // little guy o7

        shooter.RegenerateCache();
        shooter.behaviorSpeculator.aiActor = enemy;
        foreach (MovementBehaviorBase move in shooter.behaviorSpeculator.MovementBehaviors)
            move.m_aiActor = enemy;
        shooter.RegenerateCache();

        // enemy.ShowAllBehaviors();

        // shooter.behaviorSpeculator./*Fully*/RefreshBehaviors();
        // shooter.RegenerateCache();

        return shooter;
    }

    public static void ArmToTheTeeth(this AIShooter shooter, Gun replacementGun)
    {
        ProjectileModule mod = replacementGun.DefaultModule;
        Projectile defaultProjectile = (
          ((mod.shootStyle == ShootStyle.Charged) && mod.chargeProjectiles != null && mod.chargeProjectiles.Count > 0)
            ? mod.FirstValidChargeProjectile()
            : mod.projectiles[0]
          )
          ?? ((replacementGun.singleModule != null) ? replacementGun.singleModule.projectiles.SafeFirst() : null)
          ?? throw new Exception($"failed to get Hecked Mode projectile for gun {replacementGun.EncounterNameOrDisplayName}");

        shooter.equippedGunId = replacementGun.PickupObjectId;
        shooter.customShootCooldownPeriod = 0f;
        shooter.bulletName = null;
        foreach (AttackBehaviorBase attack in shooter.behaviorSpeculator.AttackBehaviors)
        {
            if (attack is not ShootGunBehavior pewpew)
                continue;

            // ETGModConsole.Log($"  givem a gun!");

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
            pewpew.Range                 = defaultProjectile.baseData.range;

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

    private static AIActor _BulletKin = null;
    public static void HeckedShootGunBehavior(this AIActor enemy, Gun replacementGun)
    {
        if (enemy.aiShooter is not AIShooter shooter)
        {
            if (_HeckedModeStatus != Hecked.Retrashed)
                return;  // disable extra guns outside the debug build for now
            if (enemy.GetComponent<CompanionController>())
                return; // companions should not get guns in retrashed mode
            shooter = enemy.EnableGunShooting(replacementGun);
        }

        // if (!replacementGun || !replacementGun.DefaultModule || replacementGun.DefaultModule.projectiles == null || replacementGun.DefaultModule.projectiles[0] is not Projectile)
        // {
        //     Lazy.DebugLog($"failed to initialize default projectiles for enemy");
        //     return;
        // }

        // if (!shooter || !shooter.behaviorSpeculator || shooter.behaviorSpeculator.AttackBehaviors is not List<AttackBehaviorBase>)
        // {
        //     Lazy.DebugLog($"failed to initialize attack behaviors for enemy");
        //     return;
        // }

        shooter.ArmToTheTeeth(replacementGun);
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
    // NOTE: All of this code is basically undoing AIShooter.Initialize() in reverse order
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
                    shooter.healthHaver.bodySprites.TryRemove(hcsprite);
                }
                shooter.m_attachedHands.TryRemove(hc);
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
            //NOTE: the next two lines fix a null deref in FindPredictedTargetPosition()
            pewpew.LeadChance         = 0f;
            pewpew.LeadAmount         = 0f;

        }

        shooter.Initialize();
        // ETGModConsole.Log($"after init: ");
        // shooter.AttachStats();
    }

}
