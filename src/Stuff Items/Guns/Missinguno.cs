using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using Alexandria.ItemAPI;
using SaveAPI;
using Alexandria.Misc;

namespace NevernamedsItems
{
    public class Missinguno : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Missinguno", "missinguno");
            Game.Items.Rename("outdated_gun_mods:missinguno", "nn:missinguno");
            gun.gameObject.AddComponent<Missinguno>();
            gun.SetShortDescription("Try Catch This!");
            gun.SetLongDescription("This gun can't seem to decide what gun it wants to be!" + "\n\nFished from the deepest waters of the Gungeon's coast.");

            gun.SetupSprite(null, "missinguno_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 14);

            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(13) as Gun).gunSwitchGroup;
            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.muzzleFlashEffects.type = VFXPoolType.None;
            gun.DefaultModule.cooldownTime = 0.15f;
            gun.DefaultModule.numberOfShotsInClip = 6;
            gun.barrelOffset.transform.localPosition = new Vector3(2.06f, 0.87f, 0f);
            gun.SetBaseMaxAmmo(200);
            gun.ammo = 200;
            gun.gunClass = GunClass.SILLY;
            gun.quality = PickupObject.ItemQuality.B;
            ETGMod.Databases.Items.Add(gun, false, "ANY");

            MissingunoID = gun.PickupObjectId;

            gun.SetupUnlockOnCustomFlag(CustomDungeonFlags.UNLOCKED_MISSINGUNO, true);
            CustomActions.OnChestPreOpen += Missinguno.OnChestPreOpen;
        }
        public static int MissingunoID;
        public static void OnChestPreOpen(Chest self, PlayerController opener)
        {
            if (self && self.IsGlitched)
            {
                if (!SaveAPIManager.GetFlag(CustomDungeonFlags.UNLOCKED_MISSINGUNO))
                {
                    SaveAPIManager.SetFlag(CustomDungeonFlags.UNLOCKED_MISSINGUNO, true);
                }
            }
        }
        private void ReRandomiseGun(PlayerController player)
        {
            int amountOfBulletsToShoot = 1;
            float randomFloat = UnityEngine.Random.value;
            if (randomFloat <= 0.025f) amountOfBulletsToShoot = 5; // 2.5%
            else if (randomFloat <= 0.1f) amountOfBulletsToShoot = 4; // 7.5%
            else if (randomFloat <= 0.2f) amountOfBulletsToShoot = 3; // 10%
            else if (randomFloat <= 0.4f) amountOfBulletsToShoot = 2; //20%
            // 60%

            Gun gunToStealFrom = (PickupObjectDatabase.GetById(BraveUtility.RandomElement(viableSourceGuns)) as Gun);

            List<Projectile> ValidBullets = new List<Projectile>();

            if (gun.DefaultModule.shootStyle == ProjectileModule.ShootStyle.Charged)
            {
                Projectile projectile = null;
                for (int k = 0; k < 15; k++)
                {
                    ProjectileModule.ChargeProjectile chargeProjectile = gunToStealFrom.RawDefaultModule().chargeProjectiles[UnityEngine.Random.Range(0, gunToStealFrom.RawDefaultModule().chargeProjectiles.Count)];
                    if (chargeProjectile != null) projectile = chargeProjectile.Projectile;
                    if (projectile) break;
                }
                ValidBullets.Add(projectile);
            }
            else
            {
                Projectile projectile = null;
                for (int k = 0; k < 15; k++)
                {
                    Projectile proj = gunToStealFrom.RawDefaultModule().projectiles[UnityEngine.Random.Range(0, gunToStealFrom.RawDefaultModule().projectiles.Count)];
                    if (proj != null) projectile = proj;
                    if (projectile) break;
                }
                ValidBullets.Add(projectile);
            }
            if (gun.RawSourceVolley.projectiles.Count() > 1)
            {
                while (gun.RawSourceVolley.projectiles.Count() > 1)
                {
                    gun.RawSourceVolley.projectiles.RemoveAt(1);
                }
            }

            if (amountOfBulletsToShoot > 1)
            {
                for (int i = 0; i < (amountOfBulletsToShoot - 1); i++)
                {
                    gun.AddProjectileModuleToRawVolleyFrom(PickupObjectDatabase.GetById(86) as Gun, true);
                }
            }
            //ETGModConsole.Log("Amount of Bullets: " + amountOfBulletsToShoot);
            //ETGModConsole.Log("Actual Volley Modules: " + gun.RawSourceVolley.projectiles.Count());
            Projectile randomproj = UnityEngine.Object.Instantiate<Projectile>(BraveUtility.RandomElement(ValidBullets));
            randomproj.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(randomproj.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(randomproj);
            randomproj.baseData.damage *= UnityEngine.Random.Range(0.5f, 2f);
            randomproj.baseData.range *= UnityEngine.Random.Range(0.2f, 3.5f);
            randomproj.baseData.force *= UnityEngine.Random.Range(0.25f, 3.5f);
            randomproj.baseData.speed *= UnityEngine.Random.Range(0.15f, 2f);
            randomproj.AdditionalScaleMultiplier *= UnityEngine.Random.Range(0.25f, 2f);
            randomproj.BossDamageMultiplier *= UnityEngine.Random.Range(0.25f, 2f);
            randomproj.BlackPhantomDamageMultiplier *= UnityEngine.Random.Range(0.1f, 3.5f);

            float cachedCooldownTime = UnityEngine.Random.Range(0.01f, 1.1f);
            int cachedClipShots = UnityEngine.Random.Range(1, 30);
            float cachedAngleVariance = UnityEngine.Random.Range(0, 25);

            gun.reloadTime = UnityEngine.Random.Range(0.1f, 2f);
            int ammo = UnityEngine.Random.Range(50, 350);
            gun.SetBaseMaxAmmo(ammo);
            gun.ammo = ammo;
            int type = UnityEngine.Random.Range(1, 4);
            float cachedBurstCooldown = UnityEngine.Random.Range(0.01f, 0.7f);
            int cachedBurstCount = UnityEngine.Random.Range(1, 11);

            gun.RawDefaultModule().projectiles[0] = randomproj;
            gun.RawDefaultModule().cooldownTime = cachedCooldownTime;
            gun.RawDefaultModule().numberOfShotsInClip = cachedClipShots;
            gun.RawDefaultModule().angleVariance = cachedAngleVariance;
            if (type == 1) gun.RawDefaultModule().shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            else if (type == 2) gun.RawDefaultModule().shootStyle = ProjectileModule.ShootStyle.Automatic;
            else if (type == 3)
            {
                gun.RawDefaultModule().shootStyle = ProjectileModule.ShootStyle.Burst;
                gun.RawDefaultModule().burstCooldownTime = cachedBurstCooldown;
                gun.RawDefaultModule().burstShotCount = cachedBurstCount;
            }

            foreach (ProjectileModule mod in gun.RawSourceVolley.projectiles)
            {
                Projectile proj = UnityEngine.Object.Instantiate<Projectile>(randomproj);
                proj.gameObject.SetActive(false);
                FakePrefab.MarkAsFakePrefab(proj.gameObject);
                UnityEngine.Object.DontDestroyOnLoad(proj);
                mod.projectiles[0] = proj;
                mod.cooldownTime = cachedCooldownTime;
                mod.numberOfShotsInClip = cachedClipShots;
                mod.angleVariance = cachedAngleVariance;
                if (mod != gun.DefaultModule) { mod.ammoCost = 0; }

                if (type == 1)
                {
                    mod.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
                }
                else if (type == 2)
                {
                    mod.shootStyle = ProjectileModule.ShootStyle.Automatic;
                }
                else if (type == 3)
                {
                    mod.shootStyle = ProjectileModule.ShootStyle.Burst;
                    mod.burstCooldownTime = cachedBurstCooldown;
                    mod.burstShotCount = cachedBurstCount;
                }
                gun.RawSourceVolley.projectiles[0].ammoCost = 1;
                //ETGModConsole.Log("Volley Module Assignment occurred");
            }

            player.stats.RecalculateStats(player, true, false);
        }
        protected override void OnPickedUpByPlayer(PlayerController player)
        {
            if (!everPickedUpByPlayer)
            {
                AddEvenMoreGuns();
                ReRandomiseGun(player);

            }
            GameManager.Instance.OnNewLevelFullyLoaded += this.NewLevelLoaded;
            base.OnPickedUpByPlayer(player);
        }
        private void NewLevelLoaded()
        {
            if (gun && gun.CurrentOwner)
            {
                if (gun.CurrentOwner is PlayerController)
                {
                    ReRandomiseGun(gun.CurrentOwner as PlayerController);
                }
            }
        }
        protected override void OnPostDroppedByPlayer(PlayerController player)
        {
            GameManager.Instance.OnNewLevelFullyLoaded -= this.NewLevelLoaded;
            base.OnPostDroppedByPlayer(player);
        }
        public override void OnDestroy()
        {
            GameManager.Instance.OnNewLevelFullyLoaded -= this.NewLevelLoaded;
            base.OnDestroy();
        }
        private void AddEvenMoreGuns()
        {
            List<int> moddedGuns = new List<int>()
            {
                LovePistol.LovePistolID,
                DiscGun.DiscGunID,
                Repeatovolver.RepeatovolverID,
                DiamondGun.DiamondGunID,
                GoldenRevolver.GoldenRevolverID,
                UnusCentum.UnusCentumID,
                StunGun.StunGunID,
                Rekeyter.RekeyterID,
                HotGlueGun.HotGlueGunID,
                CrescendoBlaster.CrescendoBlasterID,
                Glasster.GlassterID,
                HandGun.HandGunID,
                HeadOfTheOrder.HeadOfTheOrderID,
                JusticeGun.JusticeID,
                Orgun.OrgunID,
                Octagun.OctagunID,
                Ranger.RangerID,
                HandCannon.HandCannonID,
                Lantaka.LantakaID,
                GreekFire.GreekFireID,
                EmberCannon.EmberCannonID,
                Purpler.PurplerID,
                HighVelocityRifle.HighVelocityRifleID,
                Demolitionist.DemolitionistID,
                Gravitron.GravitronID,
                FireLance.FireLanceID,
                Blowgun.BlowgunID,
                AntimaterielRifle.AntimaterielRifleID,
                DartRifle.DartRifleID,
                AM0.AM0ID,
                RiotGun.RiotGunID,
                MaidenRifle.MaidenRifleID,
                HeavyAssaultRifle.HeavyAssaultRifleID,
                DynamiteLauncher.DynamiteLauncherID,
                NNBazooka.BazookaID,
                SporeLauncher.SporeLauncherID,
                Corgun.DoggunID,
                FungoCannon.FungoCannonID,
                PhaserSpiderling.PhaserSpiderlingID,
                Guneonate.GuneonateID,
                KillithidTendril.KillithidTendrilID,
                ButchersKnife.ButchersKnifeID,
                MantidAugment.MantidAugmentID,
                Gumgun.GumgunID,
                Spiral.SpiralID,
                Gunshark.GunsharkID,
                GolfRifle.GolfRifleID,
                Icicle.IcicleID,
                GunjurersStaff.GunjurersStaffID,
                SpearOfJustice.SpearOfJusticeID,
                Protean.ProteanID,
                BulletBlade.BulletBladeID,
                Bookllet.BooklletID,
                Lorebook.LorebookID,
                Bullatterer.BullattererID,
                Entropew.EntropewID,
                Creditor.CreditorID,
            };
            viableSourceGuns.AddRange(moddedGuns);
        }
        List<int> viableSourceGuns = new List<int>()
        {
            86,
            56,
            0,
            3,
            4,
            5,
            6,
            7,
            9,
            12,
            13,
            14,
            16,
            17,
            18,
            19,
            21,
            22,
            24,
            26,
            27,
            28,
            29,
            32,
            33,
            37,
            39,
            42,
            45,
            47,
            53,
            54,
            59,
            61,
            81,
            83,
            89,
            90,
            92,
            93,
            95,
            97,
            124,
            125,
            128,
            129,
            130,
            142,
            143,
            145,
            146,
            149,
            150,
            151,
            152,
            153,
            154,
            156,
            169,
            175,
            176,
            178,
            180,
            197,
            198,
            199,
            207,
            223,
            228,
            229,
            230,
            275,
            292,
            327,
            328,
            330,
            335,
            336,
            334,
            341,
            338,
            339,
            340,
            345,
            347,
            357,
            360,
            362,
            365,
            369,
            372,
            376,
            377,
            379,
            383,
            384,
            385,
            387,
            394,
            402,
            404,
            406,
            444,
            417,
            445,
            464,
            475,
            476,
            477,
            478,
            479,
            480,
            481,
            482,
            483,
            484,
            503,
            504,
            506,
            507,
            508,
            510,
            511,
            512,
            513,
            514,
            516,
            519,
            520,
            537,
            541,
            542,
            543,
            545,
            546,
            550,
            551,
            562,
            563,
            566,
            576,
            577,
            593,
            594,
            596,
            597,
            598,
            599,
            601,
            602,
            603,
            604,
            609,
            611,
            612,
            613,
            614,
            615,
            617,
            618,
            619,
            620,
            621,
            622,
            623,
            624,
            626,
            647,
            649,
            651,
            652,
            656,
            657,
            659,
            660,
            670,
            672,
            673,
            674,
            676,
            677,
            682,
            685,
            691,
            692,
            693,
            694,
            698,
            704,
            709,
            718,
            719,
            721,
            722,
            724,
            726,
            732,
            734,
            739,
            748,
            755,
            761,
            762,
            809,
            810,
            811,
            812,
            813,
            819,
            8,
            52,
            210,
            274,
            332
        };
        public Missinguno()
        {

        }
    }
}