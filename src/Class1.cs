using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dungeonator;
using EnemyAPI;
using GungeonAPI;
using ItemAPI;
using NpcApi;
using SaveAPI;
using UnityEngine;
using MonoMod.RuntimeDetour;
using System.Reflection;
using MonoMod.Utils;
using Brave.BulletScript;
using Random = System.Random;
using FullSerializer;
using Gungeon;
using LootTableAPI;
using Alexandria.CharacterAPI;
using BepInEx;
using Alexandria;

using UnityEngine.Networking;

namespace CwaffingTheGungy
{
    [BepInPlugin(GUID, "Cwaffing the Gungy", "0.0.1")]
    [BepInDependency(ETGModMainBehaviour.GUID)]
    [BepInDependency("etgmodding.etg.mtgapi")]
    [BepInDependency("kyle.etg.gapi")]
    [BepInDependency("alexandria.etgmod.alexandria")]
    public class Initialisation : BaseUnityPlugin
    {
        public const string GUID = "pretzel.etg.cwaff";
        public static Initialisation instance;
        //public static AdvancedStringDB Strings;

        public void Awake()
        {
        }
        public void Start()
        {
            ETGModMainBehaviour.WaitForGameManagerStart(GMStart);
        }
        public void GMStart(GameManager manager)
        {
            try
            {
                ETGModConsole.Log("Cwaffing the Gungy initialising...");

                instance = this;

                //Bepin bullshit
                ETGMod.Assets.SetupSpritesFromFolder(System.IO.Path.Combine(this.FolderPath(), "sprites"));

                //Tools and Toolboxes
                StaticReferences.Init();
                // ExoticPlaceables.Init();
                // DungeonHandler.Init();
                Tools.Init();
                // ShrineFakePrefabHooks.Init();

                // ShrineFactory.Init();
                // OldShrineFactory.Init();

                FakePrefabHooks.Init();

                ItemBuilder.Init();
                // CustomClipAmmoTypeToolbox.Init();
                // EnemyTools.Init();
                // NpcApi.Hooks.Init();
                // EnemyAPI.Hooks.Init();
                // SaveAPIManager.Setup("nn");
                AudioResourceLoader.InitAudio();
                // CurseManager.Init();
                // ETGModMainBehaviour.Instance.gameObject.AddComponent<GlobalUpdate>();
                // ETGModMainBehaviour.Instance.gameObject.AddComponent<CustomDarknessHandler>();
                ETGModMainBehaviour.Instance.gameObject.AddComponent<AudioSource>();
                // GameOfLifeHandler.Init();

                //Challenges
                // Challenges.Init();

                //Hooks n Shit
                // PlayerToolsSetup.Init();
                // CompanionisedEnemyUtility.InitHooks();

                // FloorAndGenerationToolbox.Init();
                // ComplexProjModBeamCompatibility.Init();
                // ReloadBreachShrineHooks.Init();

                //VFX Setup
                // VFXToolbox.InitVFX();
                // EasyVFXDatabase.Init(); //Needs to occur before goop definition
                // ShadeFlightHookFix.Init();

                //Status Effect Setup
                // StaticStatusEffects.InitCustomEffects();
                // PlagueStatusEffectSetup.Init();
                // Confusion.Init();

                //Goop Setup
                // EasyGoopDefinitions.DefineDefaultGoops();
                // DoGoopEffectHook.Init();

                //Commands and Other Console Utilities
                // Commands.Init();

                //Hats
                // HatUtility.NecessarySetup();
                // HatDefinitions.Init();

                //Gamemodes
                // AllJammedState.Init();
                // JammedChests.Init();

                //Exotic Object Shit

                //VFX
                // LockdownStatusEffect.Initialise();

                //Tweaks and Changes
                // EnemyHealthModifiers.Init();

                //Unlock Handlers and Hooks

                // MiscUnlockHooks.InitHooks();

                //Testing / Debug Items
                // ActiveTestingItem.Init();
                // PassiveTestingItem.Init();
                // BulletComponentLister.Init();
                // ObjectComponentLister.Init();

                //-----------------------------------------------------ITEMS GET INITIALISED
                #region ItemInitialisation
                    // //Character Starters
                    // ShadeHand.Init();
                    // ShadeHeart.Init();
                    // //Egg Salad and Prima Bean can go here, because they were the first
                    // EggSalad.Init();
                    // PrimaBean.Init();
                    // //Bullet modifiers
                    // BashingBullets.Init();
                    // TitanBullets.Init();
                    // MistakeBullets.Init();
                    // FiftyCalRounds.Init();
                    // UnengravedBullets.Init();
                    // EngravedBullets.Init();
                    // HardReloadBullets.Init();
                    // NitroBullets.Init();
                    // SupersonicShots.Init();
                    // GlassRounds.Init();
                    // Junkllets.Init();
                    // BloodthirstyBullets.Init();
                    // CleansingRounds.Init();
                    // HallowedBullets.Init();
                    // PromethianBullets.Init();
                    // EpimethianBullets.Init();
                    // RandoRounds.Init();
                    // IngressBullets.Init(); //Unfinished
                    // HematicRounds.Init();
                    // FullArmourJacket.Init();
                    // MirrorBullets.Init();
                    // CrowdedClip.Init();
                    // BashfulShot.Init();
                    // OneShot.Init();
                    // BulletBullets.Init();
                    // AntimatterBullets.Init();
                    // SpectreBullets.Init();
                    // Tabullets.Init();
                    // TierBullets.Init(); //Unfinished
                    // BombardierShells.Init();
                    // GildedLead.Init();
                    // DemoterBullets.Init();
                    // Voodoollets.Init();
                    // TracerRound.Init();
                    // EndlessBullets.Init();
                    // HellfireRounds.Init();
                    // Birdshot.Init();
                    // Unpredictabullets.Init();
                    // WarpBullets.Init();
                    // BulletsWithGuns.Init();
                    // LaserBullets.Init();
                    // BalancedBullets.Init(); //Unfinished
                    // WoodenBullets.Init();
                    // ComicallyGiganticBullets.Init(); //Excluded
                    // KnightlyBullets.Init();
                    // EmptyRounds.Init();
                    // LongswordShot.Init();
                    // DrillBullets.Init();
                    // FoamDarts.Init();
                    // BatterBullets.Init();
                    // ElectrumRounds.Init();
                    // BreachingRounds.Init();
                    // MagnetItem.Init();
                    // EargesplittenLoudenboomerRounds.Init();
                    // TheShell.Init();
                    // //Status Effect Bullet Mods
                    // SnailBullets.Init();
                    // LockdownBullets.Init();
                    // PestiferousLead.Init();
                    // Shrinkshot.Init();
                    // //Volley Modifying Bullet Mods
                    // Splattershot.Init();
                    // BackwardsBullets.Init();
                    // CrossBullets.Init();
                    // ShadeShot.Init();
                    // //Insta-Kill Bullet Modifiers
                    // MinersBullets.Init();
                    // AntimagicRounds.Init();
                    // AlkaliBullets.Init();
                    // ShutdownShells.Init();
                    // ERRORShells.Init();
                    // OsteoporosisBullets.Init();
                    // //NonBullet Stat Changers
                    // MicroAIContact.Init();
                    // LuckyCoin.Init();
                    // IronSights.Init();
                    // Lewis.Init();
                    // MysticOil.Init();
                    // VenusianBars.Init();
                    // NumberOneBossMug.Init();
                    // // LibramOfTheChambers.Init();
                    // OrganDonorCard.Init();
                    // GlassGod.Init();
                    // ChaosRuby.Init();
                    // BlobulonRage.Init();
                    // OverpricedHeadband.Init();
                    // GunslingerEmblem.Init();
                    // MobiusClip.Init();
                    // ClipOnAmmoPouch.Init();
                    // JawsOfDefeat.Init();
                    // IridiumSnakeMilk.Init();
                    // Starfruit.Init();
                    // //Armour
                    // ArmourBandage.Init();
                    // GoldenArmour.Init();
                    // ExoskeletalArmour.Init();
                    // PowerArmour.Init();
                    // ArmouredArmour.Init();
                    // //Consumable Givers
                    // LooseChange.Init();
                    // SpaceMetal.Init();
                    // //Blank Themed Items
                    // TrueBlank.Init();
                    // FalseBlank.Init();
                    // SpareBlank.Init();
                    // OpulentBlank.Init();
                    // GrimBlanks.Init();
                    // NNBlankPersonality.Init();
                    // Blombk.Init();
                    // Blankh.Init();
                    // //Key Themed Items
                    // BlankKey.Init();
                    // SharpKey.Init();
                    // SpareKey.Init();
                    // KeyChain.Init();
                    // KeyBullwark.Init();
                    // KeyBulletEffigy.Init();
                    // FrostKey.Init();
                    // ShadowKey.Init();
                    // Keygen.Init();
                    // CursedTumbler.Init();
                    // //Ammo Box Themed Items
                    // TheShellactery.Init();
                    // BloodyAmmo.Init();
                    // MengerAmmoBox.Init();
                    // AmmoTrap.Init();
                    // //Boxes and Stuff
                    // BloodyBox.Init();
                    // MaidenShapedBox.Init();
                    // Toolbox.Init();
                    // PocketChest.Init();
                    // DeliveryBox.Init();
                    // Wonderchest.Init();
                    // //Heart themed items
                    // HeartPadlock.Init();
                    // Mutagen.Init();
                    // ForsakenHeart.Init();
                    // HeartOfGold.Init();
                    // GooeyHeart.Init();
                    // ExaltedHeart.Init();
                    // CheeseHeart.Init();
                    // TinHeart.Init();
                    // //Chambers
                    // BarrelChamber.Init();
                    // GlassChamber.Init();
                    // FlameChamber.Init();
                    // Recyclinder.Init();
                    // Nitroglycylinder.Init();
                    // SpringloadedChamber.Init();
                    // WitheringChamber.Init();
                    // HeavyChamber.Init();
                    // CyclopeanChamber.Init();
                    // //Table Techs
                    // TableTechTable.Init();
                    // TableTechSpeed.Init();
                    // TableTechInvulnerability.Init();
                    // TableTechAmmo.Init();
                    // TableTechGuon.Init();
                    // TableTechNology.Init();
                    // UnsTableTech.Init();
                    // RectangularMirror.Init();
                    // //Guon Stones
                    // WoodGuonStone.Init();
                    // YellowGuonStone.Init();
                    // GreyGuonStone.Init();
                    // BlackGuonStone.Init();
                    // GoldGuonStone.Init();
                    // BrownGuonStone.Init();
                    // CyanGuonStone.Init();
                    // IndigoGuonStone.Init();
                    // SilverGuonStone.Init();
                    // MaroonGuonStone.Init();
                    // UltraVioletGuonStone.Init();
                    // InfraredGuonStone.Init();
                    // RainbowGuonStone.Init();
                    // KaleidoscopicGuonStone.Init();
                    // GuonBoulder.Init();
                    // BloodglassGuonStone.Init();
                    // //Ammolets
                    // GlassAmmolet.Init();
                    // WickerAmmolet.Init();
                    // FuriousAmmolet.Init();
                    // SilverAmmolet.Init();
                    // IvoryAmmolet.Init();
                    // KinAmmolet.Init();
                    // Autollet.Init();
                    // Keymmolet.Init();
                    // Ammolock.Init();
                    // HepatizonAmmolet.Init();
                    // BronzeAmmolet.Init();
                    // PearlAmmolet.Init();
                    // NeutroniumAmmolet.Init();
                    // Shatterblank.Init();
                    // // Boots
                    // CycloneCylinder.Init();
                    // BootLeg.Init();
                    // BlankBoots.Init();
                    // BulletBoots.Init();
                    // //Bracelets and Jewelry
                    // DiamondBracelet.Init();
                    // PearlBracelet.Init();
                    // PanicPendant.Init();
                    // GunknightAmulet.Init();
                    // AmuletOfShelltan.Init();
                    // //Rings
                    // RingOfOddlySpecificBenefits.Init();
                    // FowlRing.Init();
                    // RingOfAmmoRedemption.Init();
                    // RiskyRing.Init();
                    // WidowsRing.Init();
                    // ShadowRing.Init();
                    // RingOfInvisibility.Init();
                    // //Holsters
                    // BlackHolster.Init();
                    // TheBeholster.Init();
                    // HiveHolster.Init();
                    // ShoulderHolster.Init();
                    // ArtilleryBelt.Init();
                    // BulletShuffle.Init();
                    // //Companions
                    // MolotovBuddy.Init();
                    // BabyGoodChanceKin.Init();
                    // Potty.Init();
                    // Peanut.Init();
                    // DarkPrince.Init();
                    // Diode.Init();
                    // DroneCompanion.Init();
                    // GregTheEgg.Init();
                    // FunGuy.Init();
                    // BabyGoodDet.Init();
                    // AngrySpirit.Init();
                    // Gusty.Init();
                    // ScrollOfExactKnowledge.Init();
                    // LilMunchy.Init();
                    // Hapulon.Init();
                    // //Potions / Jars
                    // SpeedPotion.Init();
                    // LovePotion.Init();
                    // HoneyPot.Init();
                    // ChemicalBurn.Init();
                    // WitchsBrew.Init();
                    // Nigredo.Init();
                    // Albedo.Init();
                    // Citrinitas.Init();
                    // Rubedo.Init();
                    // HoleyWater.Init();
                    // //Remotes
                    // ReinforcementRadio.Init();
                    // //Medicine
                    // BloodThinner.Init();
                    // BoosterShot.Init();
                    // ShotInTheArm.Init();
                    // //Knives and Blades
                    // DaggerOfTheAimgel.Init();
                    // CombatKnife.Init();
                    // Bayonet.Init();
                    // //Books
                    // BookOfMimicAnatomy.Init();
                    // KalibersPrayer.Init();
                    // GunidaeSolvitHaatelis.Init();
                    // //Maps
                    // MapFragment.Init();
                    // TatteredMap.Init();
                    // //Clothing
                    // CloakOfDarkness.Init();
                    // TimeFuddlersRobe.Init();
                    // //Eyes
                    // CartographersEye.Init();
                    // BloodshotEye.Init();
                    // ShadesEye.Init();
                    // KalibersEye.Init();
                    // //Hands
                    // Lefthandedness.Init();
                    // NecromancersRightHand.Init();
                    // //Bombs
                    // InfantryGrenade.Init();
                    // DiceGrenade.Init();
                    // //Peppers
                    // PickledPepper.Init();
                    // PepperPoppers.Init();
                    // //Mushrooms
                    // PercussionCap.Init();
                    // BlastingCap.Init();
                    // //True Misc
                    // Lvl2Molotov.Init();
                    // GoldenAppleCore.Init();
                    // AppleCore.Init();
                    // AppleActive.Init();
                    // LibationtoIcosahedrax.Init(); //Unfinished
                    // BagOfHolding.Init();
                    // ItemCoupon.Init();
                    // IdentityCrisis.Init();
                    // LiquidMetalBody.Init();
                    // GunGrease.Init();
                    // BomberJacket.Init();
                    // DragunsScale.Init();
                    // GTCWTVRP.Init();
                    // BlightShell.Init();
                    // BulletKinPlushie.Init();
                    // Kevin.Init();
                    // PurpleProse.Init();
                    // RustyCasing.Init();
                    // HikingPack.Init();
                    // GunpowderPheromones.Init();
                    // GunsmokePerfume.Init();
                    // Pestilence.Init();
                    // ElevatorButton.Init();
                    // Bullut.Init();
                    // GSwitch.Init();
                    // FaultyHoverboots.Init(); //Unfinished
                    // Accelerant.Init();
                    // HornedHelmet.Init();
                    // RocketMan.Init();
                    // Roulette.Init(); //Unfinished
                    // FinishedBullet.Init();
                    // ChanceKinEffigy.Init();
                    // MagickeCauldron.Init();
                    // Bombinomicon.Init();
                    // ClaySculpture.Init();
                    // GracefulGoop.Init();
                    // MrFahrenheit.Init();
                    // MagicQuiver.Init();
                    // FocalLenses.Init();
                    // MagicMissile.Init();
                    // AmberDie.Init();
                    // ObsidianPistol.Init();
                    // Showdown.Init();
                    // LootEngineItem.Init();
                    // Ammolite.Init();
                    // PortableHole.Init();
                    // CardinalsMitre.Init();
                    // GunjurersBelt.Init();
                    // GoomperorsCrown.Init();
                    // ChemGrenade.Init();
                    // EightButton.Init();
                    // TitaniumClip.Init();
                    // PaperBadge.Init();
                    // SculptorsChisel.Init();
                    // Permafrost.Init();
                    // GlassShard.Init();
                    // EqualityItem.Init();
                    // BitBucket.Init();
                    // Eraser.Init();
                    // TackShooter.Init();
                    // Moonrock.Init();
                    // Telekinesis.Init();
                    // TabletOfOrder.Init();
                    // Bambarrage.Init();
                    // LeadSoul.Init();
                    // LeadOfLife.Init();
                    // AWholeBulletKin.Init();
                #endregion

                //-----------------------------------------------------GUNS GET INITIALISED
                #region GunInitialisation
                    // //UNFINISHED / TEST GUNS
                    // WailingMagnum.Add();
                    // Defender.Add();
                    // TestGun.Add();
                    // Gunycomb.Add();
                    // GlobbitSMALL.Add();
                    // GlobbitMED.Add();
                    // GlobbitMEGA.Add();


                    // //GUNS

                    // //CHARACTERSTARTERS
                    // ElderMagnum.Add();
                    BasicGun.Add();
                    RainCheck.Add();

                    // //REVOLVERS
                    // FlayedRevolver.Add();
                    // G20.Add();
                    // MamaGun.Add();
                    // LovePistol.Add();
                    // DiscGun.Add();
                    // Repeatovolver.Add();
                    // Pista.Add();
                    // NNGundertale.Add();
                    // DiamondGun.Add();
                    // NNMinigun.Add();
                    // ShroomedGun.Add();
                    // GoldenRevolver.Add();
                    // Nocturne.Add();
                    // BackWarder.Add();
                    // Redhawk.Add();
                    // ToolGun.Add();
                    // //GENERAL HANDGUNS
                    // StickGun.Add();
                    // Glock42.Add();
                    // StarterPistol.Add();
                    // PopGun.Add();
                    // UnusCentum.Add();
                    // StunGun.Add();
                    // CopperSidearm.Add();
                    // Rekeyter.Add();
                    // HotGlueGun.Add();
                    // UpNUp.Add();
                    // RedRobin.Add();
                    // VariableGun.Add();
                    // CrescendoBlaster.Add();
                    // Glasster.Add();
                    // HandGun.Add();
                    // Viper.Add();
                    // DiamondCutter.Add();
                    // MarchGun.Add();
                    // RebarGun.Add();
                    // MinuteGun.Add();
                    // Ulfberht.Add();
                    // HeadOfTheOrder.Add();
                    // GunOfAThousandSins.Add();
                    // DoubleGun.Add();
                    // //SHOTGUNS
                    // JusticeGun.Add();
                    // Orgun.Add();
                    // Octagun.Add();
                    // ClownShotgun.Add();
                    // Ranger.Add();
                    // RustyShotgun.Add();
                    // TheBride.Add();
                    // TheGroom.Add();
                    // IrregularShotgun.Add();
                    // GrenadeShotgun.Add();
                    // Jackhammer.Add();
                    // SaltGun.Add();
                    // SoapGun.Add();
                    // //CANNONS
                    // Felissile.Add();
                    // HandCannon.Add();
                    // Lantaka.Add();
                    // GreekFire.Add();
                    // EmberCannon.Add();
                    // ElysiumCannon.Add();
                    // DisplacerCannon.Add();
                    // //SCI-FI GUNS
                    // Blasmaster.Add();
                    // St4ke.Add();
                    // Robogun.Add();
                    // RedBlaster.Add();
                    // BeamBlade.Add();
                    // Neutrino.Add();
                    // Rico.Add();
                    // TheThinLine.Add();
                    // RocketPistol.Add();
                    // Repetitron.Add();
                    // Dimensionaliser.Add();
                    // Purpler.Add();
                    // VacuumGun.Add();
                    // Oxygun.Add();
                    // TriBeam.Add();
                    // WaveformLens.Add();
                    // KineticBlaster.Add();
                    // LaserWelder.Add();
                    // QBeam.Add();
                    // HighVelocityRifle.Add();
                    // Demolitionist.Add();
                    // PumpChargeShotgun.Add();
                    // TheOutbreak.Add();
                    // Multiplicator.Add();
                    // PunishmentRay.Add();
                    // YBeam.Add();
                    // WallRay.Add();
                    // BolaGun.Add();
                    // AlphaBeam.Add();
                    // Glazerbeam.Add();
                    // StasisRifle.Add();
                    // Gravitron.Add();
                    // Ferrobolt.Add();
                    // TauCannon.Add();
                    // GravityGun.Add();
                    // GalaxyCrusher.Add();
                    // //ARC Weapons
                    // ARCPistol.Add();
                    // ARCShotgun.Add();
                    // ARCRifle.Add();
                    // ARCTactical.Add();
                    // ARCCannon.Add();
                    // //BOWS AND CROSSBOWS
                    // IceBow.Add();
                    // Boltcaster.Add();
                    // Clicker.Add();
                    // //ANTIQUES
                    // WheelLock.Add();
                    // Welrod.Add();
                    // Welgun.Add();
                    // TheLodger.Add();
                    // Gonne.Add();
                    // Hwacha.Add();
                    // FireLance.Add();
                    // HandMortar.Add();
                    // GrandfatherGlock.Add();
                    // GatlingGun.Add();
                    // Blowgun.Add();
                    // Gaxe.Add();
                    // WoodenHorse.Add();
                    // AgarGun.Add();
                    // //KNIVES AND BLADES
                    // Carnwennan.Add();
                    // MantidAugment.Add();
                    // //REALISTIC GUNS
                    // HeatRay.Add();
                    // BarcodeScanner.Add();
                    // AntimaterielRifle.Add();
                    // Primos1.Add();
                    // DartRifle.Add();
                    // AM0.Add();
                    // RiskRifle.Add();
                    // RiotGun.Add();
                    // Kalashnirang.Add();
                    // Schwarzlose.Add();
                    // MaidenRifle.Add();
                    // Blizzkrieg.Add();
                    // Copygat.Add();
                    // Skorpion.Add();
                    // HeavyAssaultRifle.Add();
                    // DynamiteLauncher.Add();
                    // MarbledUzi.Add();
                    // BurstRifle.Add();
                    // OlReliable.Add();
                    // //MISSILE LAUNCHERS
                    // BottleRocket.Add();
                    // NNBazooka.Add();
                    // BoomBeam.Add();
                    // Pillarocket.Add();
                    // //ANIMAL / ORGANIC GUNS
                    // SporeLauncher.Add();
                    // PoisonDartFrog.Add();
                    // Corgun.Add();
                    // FungoCannon.Add();
                    // PhaserSpiderling.Add();
                    // Guneonate.Add();
                    // KillithidTendril.Add();
                    // Gunger.Add();
                    // SickWorm.Add();
                    // MiniMonger.Add();
                    // CarrionFormeTwo.Add();
                    // CarrionFormeThree.Add();
                    // Carrion.Add();
                    // UterinePolyp.Add();
                    // Wrinkler.Add();
                    // //BLADES
                    // ButchersKnife.Add();
                    // RapidRiposte.Add();
                    // //FUN GUNS
                    // Gumgun.Add();
                    // Glooper.Add();
                    // Accelerator.Add();
                    // PaintballGun.Add();
                    // Converter.Add();
                    // Spiral.Add();
                    // Gunshark.Add();
                    // FingerGuns.Add();
                    // OBrienFist.Add();
                    // GolfRifle.Add();
                    // Pandephonium.Add();
                    // Sweeper.Add();
                    // DeskFan.Add();
                    // Pencil.Add();
                    // SquarePeg.Add();
                    // Ringer.Add();
                    // Snaker.Add();
                    // GayK47.Add();
                    // DecretionCarbine.Add();
                    // RC360.Add();
                    // UziSpineMM.Add();
                    // Autogun.Add();
                    // Rebondir.Add();
                    // BigShot.Add();
                    // W3irdstar.Add();
                    // Seismograph.Add();
                    // PocoLoco.Add();
                    // BioTranstater2100.Add();
                    // //MAGICAL GUNS
                    // Icicle.Add();
                    // GunjurersStaff.Add();
                    // InitiateWand.Add();
                    // LightningRod.Add();
                    // OrbOfTheGun.Add();
                    // SpearOfJustice.Add();
                    // Protean.Add();
                    // BulletBlade.Add();
                    // Bookllet.Add();
                    // Lorebook.Add();
                    // Beastclaw.Add();
                    // Bullatterer.Add();
                    // Entropew.Add();
                    // Missinguno.Add();
                    // Paraglocks.Add();
                    // //CONSUMABLE FIRING GUNS
                    // Creditor.Add();
                    // Blankannon.Add();
                    // Viscerifle.Add();
                    // //ENDPAGE GUNS
                    // MastersGun.Add();
                    // Wrench.Add();
                    // Pumhart.Add();


                    // //SYNERGY FORME GUNS
                    // GunsharkMegasharkSynergyForme.Add();
                    // DiscGunSuperDiscForme.Add();
                    // OrgunHeadacheSynergyForme.Add();
                    // Wolfgun.Add();
                    // MinigunMiniShotgunSynergyForme.Add();
                    // PenPencilSynergy.Add();
                    // ReShelletonKeyter.Add();
                    // AM0SpreadForme.Add();
                    // BulletBladeGhostForme.Add();
                    // GlueGunGlueGunnerSynergy.Add();
                    // KingBullatterer.Add();
                    // WrenchNullRefException.Add();
                    // GatlingGunGatterUp.Add();
                    // GravityGunNegativeMatterForm.Add();
                    // GonneElder.Add();
                    // UterinePolypWombular.Add();
                    // DiamondGaxe.Add();
                    // RedRebondir.Add();
                    // DiamondCutterRangerClass.Add();
                    // StickGunQuickDraw.Add();
                    // StormRod.Add();
                    // UnrustyShotgun.Add();
                    // DARCPistol.Add();
                #endregion


                //-----------------------------------------------------SHRINES GET INITIALISED
                #region ShrineInitialisation
                // InvestmentShrine.Add();
                // RelodinShrine.Add();
                // DagunShrine.Add();
                // ArtemissileShrine.Add();
                // ExecutionerShrine.Add();
                // TurtleShrine.Add();
                // KliklokShrine.Add();
                #endregion

                //-----------------------------------------------------NPCS GET INITIALISED
                #region NPCInitialisation
                // Rusty.Init();
                // Ironside.Init();
                // Boomhildr.Init();
                #endregion

                // ChromaGun.Add();

                //GOOD MIMIC (NEEDS TO BE INITIALISED LATER)
                // GoodMimic.Add();

                //Characters
                // var data = Loader.BuildCharacter("CwaffingTheGungy/Characters/Shade",
                //    "nevernamed.etg.omitb",
                //     new Vector3(12.3f, 21.3f),
                //     false,
                //      new Vector3(13.1f, 19.1f),
                //      false,
                //      false,
                //      true,
                //      true, //Sprites used by paradox
                //      false, //Glows
                //      null, //Glow Mat
                //      null, //Alt Skin Glow Mat
                //      0, //Hegemony Cost
                //      false, //HasPast
                //      ""); //Past ID String

                //Other Features
                // AdditionalMasteries.Init();
                // CadenceAndOxShopPoolAdditions.Init();
                // CustomHuntingQuest.Init();

                //NPCS
                // TheJammomaster.Add();
                //Carto.Add();
                // ShrineFactory.PlaceBreachShrines();

                //Synergy Setup, Synergy Formes, Dual Wielding, and any changes to Basegame Guns
                // InitialiseSynergies.DoInitialisation();
                // SynergyFormInitialiser.AddSynergyForms();
                // ExistantGunModifiers.Init();

                //Setup lead of life companions
                // LeadOfLifeInitCompanions.BuildPrefabs();

                // KillUnlockHandler.Init();

                // ETGModConsole.Commands.AddUnit("nndebugflow", (args) => { DungeonHandler.debugFlow = !DungeonHandler.debugFlow; string status = DungeonHandler.debugFlow ? "enabled" : "disabled"; string color = DungeonHandler.debugFlow ? "00FF00" : "FF0000"; ETGModConsole.Log($"OMITB flow {status}", false); });

                ETGMod.StartGlobalCoroutine(this.delayedstarthandler());

                // test sound effects, comment out later
                AkSoundEngine.PostEvent("kirby-stereo", ETGModMainBehaviour.Instance.gameObject);
                AkSoundEngine.PostEvent("kirby2", ETGModMainBehaviour.Instance.gameObject);
                AkSoundEngine.PostEvent("kirby3", ETGModMainBehaviour.Instance.gameObject);

                ETGModConsole.Log("Yay! :D");
            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
                ETGModConsole.Log(e.StackTrace);
            }
        }

        public IEnumerator delayedstarthandler()
        {
            yield return null;
            this.DelayedInitialisation();
            yield break;
        }
        public void DelayedInitialisation()
        {
            try
            {
                // LibramOfTheChambers.LateInit();

                // CrossmodNPCLootPoolSetup.CheckItems();

                // OMITBChars.Shade = ETGModCompatibility.ExtendEnum<PlayableCharacters>(Initialisation.GUID, "Shade");

                ETGModConsole.Log("(Also finished DelayedInitialization)");

            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
                ETGModConsole.Log(e.StackTrace);
            }
        }
    }
}


