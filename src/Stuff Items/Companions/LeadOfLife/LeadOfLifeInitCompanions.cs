using Alexandria.ItemAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Alexandria.Misc;

namespace NevernamedsItems
{

    public static class LeadOfLifeInitCompanions
    {
        public static GameObject BuildIndividualPrefab(
            LeadOfLifeCompanionStats companionStats,
            string filePath,
            int spawningItemID,
            int fps,
            IntVector2 hitboxSize,
            IntVector2 hitboxOffset,
            bool nowalk = false,
            bool twoway = false,
            bool moddedFolder = false)
        {
            if (companionStats.prefab == null || !CompanionBuilder.companionDictionary.ContainsKey(companionStats.guid))
            {
                string folder = moddedFolder? "LeadOfLifeModded" : "LeadOfLife";
                string initialIdle = twoway ? "idleright" : "idle";

                //Setup the Actor
                GameObject newCompanion = CompanionBuilder.BuildPrefab($"LeadOfLife {filePath}", companionStats.guid, $"NevernamedsItems/Resources/Companions/{folder}/{filePath}_{initialIdle}_001", hitboxOffset, hitboxSize);
                if (!twoway)
                {
                    newCompanion.AddAnimation("idle", $"NevernamedsItems/Resources/Companions/{folder}/{filePath}_idle", fps, CompanionBuilder.AnimationType.Idle, DirectionalAnimation.DirectionType.Single, DirectionalAnimation.FlipType.None);
                    if (!nowalk) newCompanion.AddAnimation("run", $"NevernamedsItems/Resources/Companions/{folder}/{filePath}_run", fps, CompanionBuilder.AnimationType.Move, DirectionalAnimation.DirectionType.Single, DirectionalAnimation.FlipType.None);
                }
                else
                {
                    newCompanion.AddAnimation("idle_right", $"NevernamedsItems/Resources/Companions/{folder}/{filePath}_idleright", fps, CompanionBuilder.AnimationType.Idle, DirectionalAnimation.DirectionType.TwoWayHorizontal, DirectionalAnimation.FlipType.None);
                    newCompanion.AddAnimation("idle_left", $"NevernamedsItems/Resources/Companions/{folder}/{filePath}_idleleft", fps, CompanionBuilder.AnimationType.Idle, DirectionalAnimation.DirectionType.TwoWayHorizontal, DirectionalAnimation.FlipType.None);
                    if (!nowalk) newCompanion.AddAnimation("move_right", $"NevernamedsItems/Resources/Companions/{folder}/{filePath}_moveright", fps, CompanionBuilder.AnimationType.Move, DirectionalAnimation.DirectionType.TwoWayHorizontal, DirectionalAnimation.FlipType.None);
                    if (!nowalk) newCompanion.AddAnimation("move_left", $"NevernamedsItems/Resources/Companions/{folder}/{filePath}_moveleft", fps, CompanionBuilder.AnimationType.Move, DirectionalAnimation.DirectionType.TwoWayHorizontal, DirectionalAnimation.FlipType.None);
                }

                //Add it to the companion dictionary
                if (LeadOfLife.CompanionItemDictionary.ContainsKey(spawningItemID)) { LeadOfLife.CompanionItemDictionary[spawningItemID].Add(companionStats.guid); }
                else { LeadOfLife.CompanionItemDictionary.Add(spawningItemID, new List<string>() { companionStats.guid }); }

                return newCompanion;
            }
            else { return companionStats.prefab; }
        }
        public static Projectile GetProjectileForID(int id)
        {
            Projectile wip;
            if ((PickupObjectDatabase.GetById(id) as Gun).DefaultModule.shootStyle == ProjectileModule.ShootStyle.Charged)
            {
                wip = (PickupObjectDatabase.GetById(id) as Gun).DefaultModule.chargeProjectiles[0].Projectile;
            }
            else wip = (PickupObjectDatabase.GetById(id) as Gun).DefaultModule.projectiles[0];

            GameObject wip2 = wip.gameObject.InstantiateAndFakeprefab();
            return wip2.GetComponent<Projectile>();
        }
        public static void BuildPrefabs()
        {
            try
            {
                #region Bullet Modifiers

                LeadOfLife.HotLeadCompanion.prefab = BuildIndividualPrefab(LeadOfLife.HotLeadCompanion, "hotleadcompanion", 295, 7, new IntVector2(7, 7), new IntVector2(6, 1));
                LeadOfLifeBasicShooter attackComp = LeadOfLifeBasicShooter.AddToPrefab(LeadOfLife.HotLeadCompanion.prefab, 295);
                Projectile hotleadBullet = GetProjectileForID(336);
                hotleadBullet.AdditionalScaleMultiplier = 0.5f;
                attackComp.bulletsToFire = new List<Projectile>() { hotleadBullet };
                attackComp.fireCooldown = 2.5f;
                attackComp.ignitesGoop = true;

                LeadOfLife.IrradiatedLeadCompanion.prefab = BuildIndividualPrefab(LeadOfLife.IrradiatedLeadCompanion, "irradiatedleadcompanion", 204, 7, new IntVector2(7, 7), new IntVector2(6, 1));
                LeadOfLifeBasicShooter IrradiatedLeadCompanionattackComp = LeadOfLifeBasicShooter.AddToPrefab(LeadOfLife.IrradiatedLeadCompanion.prefab, 204);
                Projectile irradiatedLeadBullet = GetProjectileForID(86);
                irradiatedLeadBullet.AdjustPlayerProjectileTint(ExtendedColours.poisonGreen, 1);
                irradiatedLeadBullet.AppliesPoison = true; irradiatedLeadBullet.PoisonApplyChance = 1; irradiatedLeadBullet.healthEffect = StaticStatusEffects.irradiatedLeadEffect;
                IrradiatedLeadCompanionattackComp.bulletsToFire = new List<Projectile>() { irradiatedLeadBullet };

                LeadOfLife.BatteryBulletsCompanion.prefab = BuildIndividualPrefab(LeadOfLife.BatteryBulletsCompanion, "batterybulletscompanion", 410, 7, new IntVector2(7, 7), new IntVector2(6, 1));
                LeadOfLifeBasicShooter BatteryBulletsCompanionattackComp = LeadOfLifeBasicShooter.AddToPrefab(LeadOfLife.BatteryBulletsCompanion.prefab, 410);
                Projectile BatteryBulletsCompanionBullet = GetProjectileForID(86);
                BatteryBulletsCompanionBullet.damageTypes |= CoreDamageTypes.Electric;
                BatteryBulletsCompanionattackComp.bulletsToFire = new List<Projectile>() { BatteryBulletsCompanionBullet };
                BatteryBulletsCompanionattackComp.angleVariance = 2f;

                LeadOfLife.PlusOneBulletsCompanion.prefab = BuildIndividualPrefab(LeadOfLife.PlusOneBulletsCompanion, "plusonebulletscompanion", 286, 7, new IntVector2(7, 7), new IntVector2(7, 1));
                LeadOfLifeBasicShooter PlusOneBulletsCompanionattackComp = LeadOfLifeBasicShooter.AddToPrefab(LeadOfLife.PlusOneBulletsCompanion.prefab, 286);
                Projectile PlusOneBulletsCompanionBullet = GetProjectileForID(86);
                PlusOneBulletsCompanionBullet.baseData.damage *= 1.25f;
                PlusOneBulletsCompanionattackComp.bulletsToFire = new List<Projectile>() { PlusOneBulletsCompanionBullet };

                LeadOfLife.AngryBulletsCompanion.prefab = BuildIndividualPrefab(LeadOfLife.AngryBulletsCompanion, "angrybulletscompanion", 323, 7, new IntVector2(7, 7), new IntVector2(6, 1));
                LeadOfLifeBasicShooter AngryBulletsCompanionattackComp = LeadOfLifeBasicShooter.AddToPrefab(LeadOfLife.AngryBulletsCompanion.prefab, 323);
                Projectile AngryBulletsCompanionBullet = GetProjectileForID(86);
                AngryBulletsCompanionBullet.gameObject.AddComponent<AngryBulletsProjectileBehaviour>();
                AngryBulletsCompanionattackComp.bulletsToFire = new List<Projectile>() { AngryBulletsCompanionBullet };

                LeadOfLife.CursedBulletsCompanion.prefab = BuildIndividualPrefab(LeadOfLife.CursedBulletsCompanion, "cursedbulletscompanion", 571, 7, new IntVector2(7, 7), new IntVector2(6, 1));
                LeadOfLifeBasicShooter CursedBulletsCompanionattackComp = LeadOfLifeBasicShooter.AddToPrefab(LeadOfLife.CursedBulletsCompanion.prefab, 571);
                Projectile CursedBulletsCompanionBullet = GetProjectileForID(86);
                ScaleProjectileStatOffPlayerStat curseScale = CursedBulletsCompanionBullet.gameObject.AddComponent<ScaleProjectileStatOffPlayerStat>();
                curseScale.multiplierPerLevelOfStat = 0.15f;
                curseScale.projstat = ScaleProjectileStatOffPlayerStat.ProjectileStatType.DAMAGE;
                curseScale.playerstat = PlayerStats.StatType.Curse;
                CursedBulletsCompanionBullet.AdjustPlayerProjectileTint(ExtendedColours.cursedBulletsPurple, 1);
                CursedBulletsCompanionBullet.CurseSparks = true;
                CursedBulletsCompanionattackComp.bulletsToFire = new List<Projectile>() { CursedBulletsCompanionBullet };

                LeadOfLife.EasyReloadBulletsCompanion.prefab = BuildIndividualPrefab(LeadOfLife.EasyReloadBulletsCompanion, "easyreloadbulletscompanion", 375, 7, new IntVector2(7, 7), new IntVector2(8, 1));
                LeadOfLifeCompanion EasyReloadBulletsCompanionattackComp = LeadOfLifeCompanion.AddToPrefab(LeadOfLife.EasyReloadBulletsCompanion.prefab, 375, 5, new List<MovementBehaviorBase>() { new CompanionFollowPlayerBehavior { IdleAnimations = new string[] { "idle" } } });
                EasyReloadBulletsCompanionattackComp.globalCompanionFirerateMultiplier = 1.25f;

                LeadOfLife.GhostBulletsCompanion.prefab = BuildIndividualPrefab(LeadOfLife.GhostBulletsCompanion, "ghostbulletscompanion", 172, 7, new IntVector2(7, 7), new IntVector2(1, 3), true);
                LeadOfLifeBasicShooter GhostBulletsCompanionattackComp = LeadOfLifeBasicShooter.AddToPrefab(LeadOfLife.GhostBulletsCompanion.prefab, 172);
                Projectile GhostBulletsCompanionBullet = GetProjectileForID(86);
                PierceProjModifier ghostPierce = GhostBulletsCompanionBullet.gameObject.AddComponent<PierceProjModifier>();
                ghostPierce.penetration = 1;
                GhostBulletsCompanionattackComp.bulletsToFire = new List<Projectile>() { GhostBulletsCompanionBullet };
                GhostBulletsCompanionattackComp.CanCrossPits = true;
                GhostBulletsCompanionattackComp.aiActor.ActorShadowOffset = new Vector3(0, -0.5f);

                LeadOfLife.FlakBulletsCompanion.prefab = BuildIndividualPrefab(LeadOfLife.FlakBulletsCompanion, "flakbulletscompanion", 531, 7, new IntVector2(7, 7), new IntVector2(8, 1));
                LeadOfLifeBasicShooter FlakBulletsCompanionattackComp = LeadOfLifeBasicShooter.AddToPrefab(LeadOfLife.FlakBulletsCompanion.prefab, 531);
                Projectile FlakBulletsCompanionBullet = GetProjectileForID(56);
                SpawnProjModifier flakspawnProjModifier = FlakBulletsCompanionBullet.gameObject.AddComponent<SpawnProjModifier>();
                flakspawnProjModifier.SpawnedProjectilesInheritAppearance = true;
                flakspawnProjModifier.SpawnedProjectileScaleModifier = 0.5f;
                flakspawnProjModifier.SpawnedProjectilesInheritData = true;
                flakspawnProjModifier.spawnProjectilesOnCollision = true;
                flakspawnProjModifier.spawnProjecitlesOnDieInAir = true;
                flakspawnProjModifier.doOverrideObjectCollisionSpawnStyle = true;
                flakspawnProjModifier.startAngle = UnityEngine.Random.Range(0, 180);
                flakspawnProjModifier.numberToSpawnOnCollison = 3;
                flakspawnProjModifier.projectileToSpawnOnCollision = Gungeon.Game.Items["flak_bullets"].GetComponent<ComplexProjectileModifier>().CollisionSpawnProjectile;
                flakspawnProjModifier.collisionSpawnStyle = SpawnProjModifier.CollisionSpawnStyle.FLAK_BURST;
                FlakBulletsCompanionattackComp.bulletsToFire = new List<Projectile>() { FlakBulletsCompanionBullet };

                LeadOfLife.HeavyBulletsCompanion.prefab = BuildIndividualPrefab(LeadOfLife.HeavyBulletsCompanion, "heavybulletscompanion", 111, 5, new IntVector2(7, 7), new IntVector2(4, 1));
                LeadOfLifeBasicShooter HeavyBulletsCompanionattackComp = LeadOfLifeBasicShooter.AddToPrefab(LeadOfLife.HeavyBulletsCompanion.prefab, 111, 3.5f);
                Projectile HeavyBulletsBullet = GetProjectileForID(86);
                HeavyBulletsBullet.baseData.damage *= 1.25f;
                HeavyBulletsBullet.baseData.speed *= 0.5f;
                HeavyBulletsBullet.baseData.force *= 2f;
                HeavyBulletsBullet.AdditionalScaleMultiplier = 1.25f;
                HeavyBulletsCompanionattackComp.bulletsToFire = new List<Projectile>() { HeavyBulletsBullet };

                LeadOfLife.RemoteBulletsCompanion.prefab = BuildIndividualPrefab(LeadOfLife.RemoteBulletsCompanion, "remotebulletscompanion", 530, 7, new IntVector2(7, 7), new IntVector2(7, 1));
                LeadOfLifeBasicShooter RemoteBulletsCompanionattackComp = LeadOfLifeBasicShooter.AddToPrefab(LeadOfLife.RemoteBulletsCompanion.prefab, 530);
                Projectile RemoteBulletsBullet = GetProjectileForID(86);
                RemoteBulletsBullet.baseData.damage *= 1.1f;
                RemoteBulletsBullet.gameObject.AddComponent<RemoteBulletsProjectileBehaviour>();
                RemoteBulletsCompanionattackComp.bulletsToFire = new List<Projectile>() { RemoteBulletsBullet };

                LeadOfLife.KatanaBulletsCompanion.prefab = BuildIndividualPrefab(LeadOfLife.KatanaBulletsCompanion, "katanabulletscompanion", 822, 7, new IntVector2(7, 7), new IntVector2(8, 1));
                LeadOfLifeExploder KatanaBulletsAttackComp = LeadOfLifeExploder.AddToPrefab(LeadOfLife.KatanaBulletsCompanion.prefab, 822);
                KatanaBulletsAttackComp.doChainExplosion = true;
                KatanaBulletsAttackComp.chainExplosionDuration = Gungeon.Game.Items["katana_bullets"].GetComponent<ComplexProjectileModifier>().LCEChainDuration;
                KatanaBulletsAttackComp.chainExplosionDistance = Gungeon.Game.Items["katana_bullets"].GetComponent<ComplexProjectileModifier>().LCEChainDistance;
                KatanaBulletsAttackComp.chainExplosionAmount = Gungeon.Game.Items["katana_bullets"].GetComponent<ComplexProjectileModifier>().LCEChainNumExplosions;
                KatanaBulletsAttackComp.explosion = Gungeon.Game.Items["katana_bullets"].GetComponent<ComplexProjectileModifier>().LinearChainExplosionData;
                KatanaBulletsAttackComp.fireCooldown = 5f;

                LeadOfLife.BouncyBulletsCompanion.prefab = BuildIndividualPrefab(LeadOfLife.BouncyBulletsCompanion, "bouncybulletscompanion", 288, 7, new IntVector2(7, 7), new IntVector2(6, 1));
                LeadOfLifeBasicShooter BouncyBulletsCompanionattackComp = LeadOfLifeBasicShooter.AddToPrefab(LeadOfLife.BouncyBulletsCompanion.prefab, 288);
                Projectile BouncyBulletsBullet = GetProjectileForID(86);
                BouncyBulletsBullet.gameObject.AddComponent<BounceProjModifier>();
                BouncyBulletsCompanionattackComp.bulletsToFire = new List<Projectile>() { BouncyBulletsBullet };

                LeadOfLife.SilverBulletsCompanion.prefab = BuildIndividualPrefab(LeadOfLife.SilverBulletsCompanion, "silverbulletscompanion", 538, 7, new IntVector2(7, 7), new IntVector2(7, 1));
                LeadOfLifeBasicShooter SilverBulletsCompanionattackComp = LeadOfLifeBasicShooter.AddToPrefab(LeadOfLife.SilverBulletsCompanion.prefab, 538);
                Projectile SilverBulletsBullet = GetProjectileForID(56);
                SilverBulletsBullet.AdjustPlayerProjectileTint(ExtendedColours.silvedBulletsSilver, 1);
                SilverBulletsBullet.BlackPhantomDamageMultiplier *= 3.25f;
                SilverBulletsBullet.BossDamageMultiplier *= 1.25f;
                SilverBulletsCompanionattackComp.bulletsToFire = new List<Projectile>() { SilverBulletsBullet };

                LeadOfLife.ZombieBulletsCompanion.prefab = BuildIndividualPrefab(LeadOfLife.ZombieBulletsCompanion, "zombiebulletscompanion", 528, 7, new IntVector2(7, 7), new IntVector2(6, 1));
                CustomCompanionBehaviours.LeadOfLifeCompanionApproach ZombieWalk = new CustomCompanionBehaviours.LeadOfLifeCompanionApproach();
                ZombieWalk.DesiredDistance = 1.2f;
                ZombieWalk.isZombieBullets = true;
                LeadOfLifeCompanion ZombieBulletsCompanionattackComp = LeadOfLifeCompanion.AddToPrefab(LeadOfLife.ZombieBulletsCompanion.prefab, 528, 5,
                    new List<MovementBehaviorBase>() { new CompanionFollowPlayerBehavior { IdleAnimations = new string[] { "idle" } }, ZombieWalk });

                LeadOfLife.Bloody9mmCompanion.prefab = BuildIndividualPrefab(LeadOfLife.Bloody9mmCompanion, "bloody9mmcompanion", 524, 7, new IntVector2(7, 7), new IntVector2(8, 3));
                LeadOfLifeBasicShooter Bloody9mmCompanionattackComp = LeadOfLifeBasicShooter.AddToPrefab(LeadOfLife.Bloody9mmCompanion.prefab, 524);
                Bloody9mmCompanionattackComp.bulletsToFire = new List<Projectile>() { PickupObjectDatabase.GetById(524).GetComponent<RandomProjectileReplacementItem>().ReplacementProjectile };
                Bloody9mmCompanionattackComp.fireCooldown = 5f;

                LeadOfLife.BumbulletsCompanion.prefab = BuildIndividualPrefab(LeadOfLife.BumbulletsCompanion, "bumbulletscompanion", 630, 7, new IntVector2(7, 7), new IntVector2(8, 1));
                LeadOfLifeBasicShooter BumbulletsCompanionattackComp = LeadOfLifeBasicShooter.AddToPrefab(LeadOfLife.BumbulletsCompanion.prefab, 630);
                BumbulletsCompanionattackComp.bulletsToFire = new List<Projectile>() { GetProjectileForID(14) };

                LeadOfLife.ChanceBulletsCompanion.prefab = BuildIndividualPrefab(LeadOfLife.ChanceBulletsCompanion, "chancebulletscompanion", 521, 7, new IntVector2(7, 7), new IntVector2(10, 1));
                LeadOfLifeRandomShooter ChanceBulletsCompanionattackComp = LeadOfLifeRandomShooter.AddToPrefab(LeadOfLife.ChanceBulletsCompanion.prefab, 521);
                ChanceBulletsCompanionattackComp.fireCooldown = 1.5f;

                LeadOfLife.CharmingRoundsCompanion.prefab = BuildIndividualPrefab(LeadOfLife.CharmingRoundsCompanion, "charmingroundscompanion", 527, 7, new IntVector2(7, 7), new IntVector2(6, 1));
                LeadOfLifeBasicShooter CharmingRoundsCompanionattackComp = LeadOfLifeBasicShooter.AddToPrefab(LeadOfLife.CharmingRoundsCompanion.prefab, 527);
                Projectile CharmingRoundsBullet = GetProjectileForID(86);
                CharmingRoundsBullet.AdjustPlayerProjectileTint(ExtendedColours.charmPink, 1);
                CharmingRoundsBullet.AppliesCharm = true; CharmingRoundsBullet.CharmApplyChance = 1; CharmingRoundsBullet.charmEffect = StaticStatusEffects.charmingRoundsEffect;
                CharmingRoundsCompanionattackComp.bulletsToFire = new List<Projectile>() { CharmingRoundsBullet };

                LeadOfLife.DevolverRoundsCompanion.prefab = BuildIndividualPrefab(LeadOfLife.DevolverRoundsCompanion, "devolverroundscompanion", 638, 7, new IntVector2(7, 7), new IntVector2(7, 1));
                LeadOfLifeBasicShooter DevolverRoundsCompanionattackComp = LeadOfLifeBasicShooter.AddToPrefab(LeadOfLife.DevolverRoundsCompanion.prefab, 638);
                Projectile DevolverRoundsCompanionBullet = GetProjectileForID(484);
                DevolverRoundsCompanionBullet.ChanceToTransmogrify = 0.1f;
                DevolverRoundsCompanionattackComp.bulletsToFire = new List<Projectile>() { DevolverRoundsCompanionBullet };

                LeadOfLife.GildedBulletsCompanion.prefab = BuildIndividualPrefab(LeadOfLife.GildedBulletsCompanion, "gildedbulletscompanion", 532, 7, new IntVector2(7, 7), new IntVector2(10, 0));
                LeadOfLifeBasicShooter GildedBulletsCompanionattackComp = LeadOfLifeBasicShooter.AddToPrefab(LeadOfLife.GildedBulletsCompanion.prefab, 532);
                Projectile GildedBulletsCompanionBullet = GetProjectileForID(86);
                ScaleProjectileStatOffConsumableCount moneyscale = GildedBulletsCompanionBullet.gameObject.AddComponent<ScaleProjectileStatOffConsumableCount>();
                moneyscale.multiplierPerLevelOfStat = 0.0038f;
                moneyscale.projstat = ScaleProjectileStatOffConsumableCount.ProjectileStatType.DAMAGE;
                moneyscale.consumableType = ScaleProjectileStatOffConsumableCount.ConsumableType.MONEY;
                GildedBulletsCompanionBullet.AdjustPlayerProjectileTint(ExtendedColours.gildedBulletsGold, 1);
                GildedBulletsCompanionattackComp.bulletsToFire = new List<Projectile>() { GildedBulletsCompanionBullet };
                GildedBulletsCompanionattackComp.spawnsCurrencyOnRoomClear = true;

                LeadOfLife.HelixBulletsCompanion.prefab = BuildIndividualPrefab(LeadOfLife.HelixBulletsCompanion, "helixbulletscompanion", 568, 7, new IntVector2(7, 7), new IntVector2(7, 1));
                LeadOfLifeBasicShooter HelixBulletsCompanionattackComp = LeadOfLifeBasicShooter.AddToPrefab(LeadOfLife.HelixBulletsCompanion.prefab, 568);
                HelixBulletsCompanionattackComp.bulletsToFire = new List<Projectile>() {
                 (PickupObjectDatabase.GetById(230) as Gun).RawSourceVolley.projectiles[0].projectiles[0],
                 (PickupObjectDatabase.GetById(230) as Gun).RawSourceVolley.projectiles[1].projectiles[0]};
                HelixBulletsCompanionattackComp.fireCooldown = 1.6f;

                LeadOfLife.HomingBulletsCompanion.prefab = BuildIndividualPrefab(LeadOfLife.HomingBulletsCompanion, "homingbulletscompanion", 284, 7, new IntVector2(7, 7), new IntVector2(5, 0));
                LeadOfLifeBasicShooter HomingBulletsCompanionattackComp = LeadOfLifeBasicShooter.AddToPrefab(LeadOfLife.HomingBulletsCompanion.prefab, 284);
                Projectile HomingBulletsBullet = GetProjectileForID(86);
                HomingModifier homingbulletcomp = HomingBulletsBullet.gameObject.AddComponent<HomingModifier>(); homingbulletcomp.HomingRadius = 100f;
                HomingBulletsCompanionattackComp.bulletsToFire = new List<Projectile>() { HomingBulletsBullet };

                LeadOfLife.MagicBulletsCompanion.prefab = BuildIndividualPrefab(LeadOfLife.MagicBulletsCompanion, "magicbulletscompanion", 533, 7, new IntVector2(7, 7), new IntVector2(6, 0));
                LeadOfLifeBasicShooter MagicBulletsCompanionattackComp = LeadOfLifeBasicShooter.AddToPrefab(LeadOfLife.MagicBulletsCompanion.prefab, 533);
                Projectile MagicBulletsCompanionBullet = GetProjectileForID(61);
                MagicBulletsCompanionBullet.ChanceToTransmogrify = 1;
                MagicBulletsCompanionattackComp.bulletsToFire = new List<Projectile>() { MagicBulletsCompanionBullet };
                MagicBulletsCompanionattackComp.fireCooldown = 7.5f;

                LeadOfLife.RocketPoweredBulletsCompanion.prefab = BuildIndividualPrefab(LeadOfLife.RocketPoweredBulletsCompanion, "rocketpoweredbulletscompanion", 113, 7, new IntVector2(7, 7), new IntVector2(2, 0), true);
                LeadOfLifeBasicShooter RocketPoweredBulletsCompanionattackComp = LeadOfLifeBasicShooter.AddToPrefab(LeadOfLife.RocketPoweredBulletsCompanion.prefab, 113, 8);
                Projectile RocketPoweredBulletsCompanionBullet = GetProjectileForID(16);
                RocketPoweredBulletsCompanionBullet.baseData.speed *= 1.5f;
                RocketPoweredBulletsCompanionattackComp.bulletsToFire = new List<Projectile>() { RocketPoweredBulletsCompanionBullet };
                RocketPoweredBulletsCompanionattackComp.CanCrossPits = true;
                RocketPoweredBulletsCompanionattackComp.ignitesGoop = true;
                RocketPoweredBulletsCompanionattackComp.aiActor.ActorShadowOffset = new Vector3(0, -0.5f);
                RocketPoweredBulletsCompanionattackComp.fireCooldown = 1f;

                LeadOfLife.ScattershotCompanion.prefab = BuildIndividualPrefab(LeadOfLife.ScattershotCompanion, "scattershotcompanion", 241, 7, new IntVector2(7, 7), new IntVector2(6, 0));
                LeadOfLifeBasicShooter ScattershotCompanionattackComp = LeadOfLifeBasicShooter.AddToPrefab(LeadOfLife.ScattershotCompanion.prefab, 241);
                Projectile ScattershotBullet = GetProjectileForID(56); ScattershotBullet.baseData.damage *= 0.6f;
                ScattershotCompanionattackComp.bulletsToFire = new List<Projectile>() { ScattershotBullet, ScattershotBullet, ScattershotBullet };
                ScattershotCompanionattackComp.angleVariance = 35f;

                LeadOfLife.ShadowBulletsCompanion.prefab = BuildIndividualPrefab(LeadOfLife.ShadowBulletsCompanion, "shadowbulletscompanion", 352, 7, new IntVector2(7, 7), new IntVector2(6, 0));
                LeadOfLifeBasicShooter ShadowBulletsCompanionattackComp = LeadOfLifeBasicShooter.AddToPrefab(LeadOfLife.ShadowBulletsCompanion.prefab, 352);
                Projectile ShadowBulletsBullet = GetProjectileForID(86);
                ShadowBulletsBullet.AdjustPlayerProjectileTint(ExtendedColours.shadowBulletsBlue, 1);
                AutoDoShadowChainOnSpawn chain = ShadowBulletsBullet.gameObject.GetOrAddComponent<AutoDoShadowChainOnSpawn>();
                chain.NumberInChain = 1;
                chain.pauseLength = 0.1f;
                ShadowBulletsCompanionattackComp.bulletsToFire = new List<Projectile>() { ShadowBulletsBullet };

                LeadOfLife.StoutBulletsCompanion.prefab = BuildIndividualPrefab(LeadOfLife.StoutBulletsCompanion, "stoutbulletscompanion", 523, 5, new IntVector2(6, 7), new IntVector2(6, 0));
                LeadOfLifeProximityShooter StoutBulletsCompanionattackComp = LeadOfLifeProximityShooter.AddToPrefab(LeadOfLife.StoutBulletsCompanion.prefab, 523, 3.5f);
                Projectile StoutBulletsBullet = GetProjectileForID(35);
                StoutBulletsBullet.baseData.damage = 15f;
                StoutBulletsCompanionattackComp.bulletsToFire = new List<Projectile>() { StoutBulletsBullet };
                StoutBulletsCompanionattackComp.scaleProxClose = true;

                LeadOfLife.AlphaBulletsCompanion.prefab = BuildIndividualPrefab(LeadOfLife.AlphaBulletsCompanion, "alphabulletscompanion", 373, 7, new IntVector2(7, 7), new IntVector2(5, 0));
                LeadOfLifeClipBasedShooter AlphaBulletsCompanionattackComp = LeadOfLifeClipBasedShooter.AddToPrefab(LeadOfLife.AlphaBulletsCompanion.prefab, 373);
                AlphaBulletsCompanionattackComp.bulletsToFire = new List<Projectile>() { GetProjectileForID(519) };
                AlphaBulletsCompanionattackComp.fireCooldown = 1.1f;
                AlphaBulletsCompanionattackComp.isAlpha = true;

                LeadOfLife.OmegaBulletsCompanion.prefab = BuildIndividualPrefab(LeadOfLife.OmegaBulletsCompanion, "omegabulletscompanion", 374, 7, new IntVector2(7, 7), new IntVector2(5, 0));
                LeadOfLifeClipBasedShooter OmegaBulletsCompanionattackComp = LeadOfLifeClipBasedShooter.AddToPrefab(LeadOfLife.OmegaBulletsCompanion.prefab, 374);
                OmegaBulletsCompanionattackComp.bulletsToFire = new List<Projectile>() { GetProjectileForID(519) };
                OmegaBulletsCompanionattackComp.fireCooldown = 1.1f;
                OmegaBulletsCompanionattackComp.isOmega = true;

                LeadOfLife.ChaosBulletsCompanion.prefab = BuildIndividualPrefab(LeadOfLife.ChaosBulletsCompanion, "chaosbulletscompanion", 569, 9, new IntVector2(5, 5), new IntVector2(9, 0), true);
                LeadOfLifeBasicShooter ChaosBulletsCompanionattackComp = LeadOfLifeBasicShooter.AddToPrefab(LeadOfLife.ChaosBulletsCompanion.prefab, 569, 7);
                Projectile ChaosBulletsCompanionBullet = GetProjectileForID(86);
                ChaosBulletsModifierComp chaos = ChaosBulletsCompanionBullet.gameObject.AddComponent<ChaosBulletsModifierComp>(); chaos.chanceOfActivatingStatusEffect = 0.5f;
                ChaosBulletsCompanionattackComp.bulletsToFire = new List<Projectile>() { ChaosBulletsCompanionBullet };
                ChaosBulletsCompanionattackComp.CanCrossPits = true;
                ChaosBulletsCompanionattackComp.aiActor.ActorShadowOffset = new Vector3(0, -0.5f);

                LeadOfLife.ExplosiveRoundsCompanion.prefab = BuildIndividualPrefab(LeadOfLife.ExplosiveRoundsCompanion, "explosiveroundscompanion", 304, 7, new IntVector2(7, 7), new IntVector2(4, 0));
                LeadOfLifeBasicShooter ExplosiveRoundsCompanionattackComp = LeadOfLifeBasicShooter.AddToPrefab(LeadOfLife.ExplosiveRoundsCompanion.prefab, 304);
                ExplosiveRoundsCompanionattackComp.bulletsToFire = new List<Projectile>() { GetProjectileForID(19) };
                ExplosiveRoundsCompanionattackComp.fireCooldown = 5f;
                ExplosiveRoundsCompanionattackComp.objectSpawnChance = 0.1f;
                ExplosiveRoundsCompanionattackComp.objectToToss = PickupObjectDatabase.GetById(108).GetComponent<SpawnObjectPlayerItem>().objectToSpawn.gameObject;
                ExplosiveRoundsCompanionattackComp.tossedObjectBounces = true;
                ExplosiveRoundsCompanionattackComp.objectTossForce = 6;

                LeadOfLife.FatBulletsCompanion.prefab = BuildIndividualPrefab(LeadOfLife.FatBulletsCompanion, "fatbulletscompanion", 277, 5, new IntVector2(7, 7), new IntVector2(5, 0));
                LeadOfLifeBasicShooter FatBulletsCompanionattackComp = LeadOfLifeBasicShooter.AddToPrefab(LeadOfLife.FatBulletsCompanion.prefab, 277, 3.5f);
                Projectile FatBulletsBullet = GetProjectileForID(86);
                FatBulletsBullet.baseData.damage *= 1.45f;
                FatBulletsBullet.baseData.force *= 2f;
                FatBulletsBullet.AdditionalScaleMultiplier *= 2f;
                FatBulletsCompanionattackComp.bulletsToFire = new List<Projectile>() { FatBulletsBullet };

                LeadOfLife.FrostBulletsCompanion.prefab = BuildIndividualPrefab(LeadOfLife.FrostBulletsCompanion, "frostbulletscompanion", 278, 7, new IntVector2(7, 7), new IntVector2(5, 0));
                LeadOfLifeBasicShooter FrostBulletsCompanionattackComp = LeadOfLifeBasicShooter.AddToPrefab(LeadOfLife.FrostBulletsCompanion.prefab, 278);
                Projectile FrostBulletsBullet = GetProjectileForID(86);
                FrostBulletsBullet.AdjustPlayerProjectileTint(ExtendedColours.freezeBlue, 1);
                FrostBulletsBullet.AppliesFreeze = true; FrostBulletsBullet.FreezeApplyChance = 1; FrostBulletsBullet.freezeEffect = StaticStatusEffects.frostBulletsEffect;
                FrostBulletsCompanionattackComp.bulletsToFire = new List<Projectile>() { FrostBulletsBullet };

                LeadOfLife.HungryBulletsCompanion.prefab = BuildIndividualPrefab(LeadOfLife.HungryBulletsCompanion, "hungrybulletscompanion", 655, 7, new IntVector2(7, 7), new IntVector2(9, 0), false, true);
                LeadOfLifeBasicShooter HungryBulletsCompanionattackComp = LeadOfLifeBasicShooter.AddToPrefab(LeadOfLife.HungryBulletsCompanion.prefab, 655);
                Projectile HungryBulletsBullet = GetProjectileForID(736);
                if (HungryBulletsBullet.GetComponent<EnemyBulletsBecomeJammedModifier>()) UnityEngine.Object.Destroy(HungryBulletsBullet.GetComponent<EnemyBulletsBecomeJammedModifier>());
                HungryBulletsBullet.AdjustPlayerProjectileTint(ExtendedColours.purple, 1);
                HungryProjectileModifier hungry = HungryBulletsBullet.gameObject.AddComponent<HungryProjectileModifier>(); hungry.HungryRadius = 1.5f;
                HungryBulletsCompanionattackComp.bulletsToFire = new List<Projectile>() { HungryBulletsBullet };
                HungryBulletsCompanionattackComp.fireCooldown = 2.5f;

                LeadOfLife.OrbitalBulletsCompanion.prefab = BuildIndividualPrefab(LeadOfLife.OrbitalBulletsCompanion, "orbitalbulletscompanion", 661, 9, new IntVector2(6, 6), new IntVector2(6, 0), true);
                LeadOfLifeComplexShooter OrbitalBulletsCompanionattackComp = LeadOfLifeComplexShooter.AddToPrefab(LeadOfLife.OrbitalBulletsCompanion.prefab, 661);
                Projectile OrbitalBulletsCompanionBullet = GetProjectileForID(86);
                OrbitalBulletsCompanionBullet.baseData.damage *= 0.6f;
                OrbitalBulletsCompanionattackComp.bulletsToFire = new List<Projectile>() { OrbitalBulletsCompanionBullet };
                OrbitalBulletsCompanionattackComp.CanCrossPits = true;
                OrbitalBulletsCompanionattackComp.aiActor.ActorShadowOffset = new Vector3(0, -0.5f);
                OrbitalBulletsCompanionattackComp.angleVariance = 45f;
                OrbitalBulletsCompanionattackComp.fireCooldown = 0.5f;
                OrbitalBulletsCompanionattackComp.orbital = true;

                LeadOfLife.ShockRoundsCompanion.prefab = BuildIndividualPrefab(LeadOfLife.ShockRoundsCompanion, "shockroundscompanion", 298, 7, new IntVector2(7, 7), new IntVector2(6, 0));
                LeadOfLifeBasicShooter ShockRoundsCompanionattackComp = LeadOfLifeBasicShooter.AddToPrefab(LeadOfLife.ShockRoundsCompanion.prefab, 298);
                Projectile ShockRoundsBullet = GetProjectileForID(86);
                ShockRoundsBullet.baseData.damage *= 0.2f;
                ShockRoundsBullet.baseData.speed *= 0.25f;
                ShockRoundsBullet.sprite.renderer.enabled = false;
                NoCollideBehaviour ShockRoundsBulletnocollide = ShockRoundsBullet.gameObject.GetOrAddComponent<NoCollideBehaviour>();
                ShockRoundsBulletnocollide.worksOnProjectiles = true; ShockRoundsBulletnocollide.worksOnEnemies = true;
                ComplexProjectileModifier shockRounds = PickupObjectDatabase.GetById(298) as ComplexProjectileModifier;
                ChainLightningModifier shockroundsshocky = ShockRoundsBullet.gameObject.GetOrAddComponent<ChainLightningModifier>();
                shockroundsshocky.LinkVFXPrefab = shockRounds.ChainLightningVFX;
                shockroundsshocky.damageTypes = shockRounds.ChainLightningDamageTypes;
                shockroundsshocky.maximumLinkDistance = 20;
                shockroundsshocky.damagePerHit = 5;
                shockroundsshocky.damageCooldown = shockRounds.ChainLightningDamageCooldown;
                if (shockRounds.ChainLightningDispersalParticles != null)
                {
                    shockroundsshocky.UsesDispersalParticles = true;
                    shockroundsshocky.DispersalParticleSystemPrefab = shockRounds.ChainLightningDispersalParticles;
                    shockroundsshocky.DispersalDensity = shockRounds.ChainLightningDispersalDensity;
                    shockroundsshocky.DispersalMinCoherency = shockRounds.ChainLightningDispersalMinCoherence;
                    shockroundsshocky.DispersalMaxCoherency = shockRounds.ChainLightningDispersalMaxCoherence;
                }
                else shockroundsshocky.UsesDispersalParticles = false;
                ShockRoundsCompanionattackComp.bulletsToFire = new List<Projectile>() { ShockRoundsBullet };
                ShockRoundsCompanionattackComp.fireCooldown = 0.25f;
                ShockRoundsCompanionattackComp.angleVariance = 60;

                LeadOfLife.SnowballetsCompanion.prefab = BuildIndividualPrefab(LeadOfLife.SnowballetsCompanion, "snowballetscompanion", 636, 7, new IntVector2(7, 7), new IntVector2(7, 0));
                LeadOfLifeProximityShooter SnowballetsCompanionattackComp = LeadOfLifeProximityShooter.AddToPrefab(LeadOfLife.SnowballetsCompanion.prefab, 636);
                Projectile SnowballetsBullet = GetProjectileForID(402);
                SnowballetsBullet.baseData.damage *= 1.4284f;
                SnowballetsCompanionattackComp.bulletsToFire = new List<Projectile>() { SnowballetsBullet };
                SnowballetsCompanionattackComp.scaleProxDistant = true;

                LeadOfLife.VorpalBulletsCompanion.prefab = BuildIndividualPrefab(LeadOfLife.VorpalBulletsCompanion, "vorpalbulletscompanion", 640, 7, new IntVector2(7, 7), new IntVector2(8, 0));
                LeadOfLifeBasicShooter VorpalBulletsCompanionattackComp = LeadOfLifeBasicShooter.AddToPrefab(LeadOfLife.VorpalBulletsCompanion.prefab, 640);
                VorpalBulletsCompanionattackComp.bulletsToFire = new List<Projectile>() { PickupObjectDatabase.GetById(640).GetComponent<ComplexProjectileModifier>().CriticalProjectile };
                VorpalBulletsCompanionattackComp.fireCooldown = 7f;

                LeadOfLife.BlankBulletsCompanion.prefab = BuildIndividualPrefab(LeadOfLife.BlankBulletsCompanion, "blankbulletscompanion", 579, 7, new IntVector2(7, 7), new IntVector2(5, 0));
                LeadOfLifeMassRoomDamager BlankBulletsCompanionattackComp = LeadOfLifeMassRoomDamager.AddToPrefab(LeadOfLife.BlankBulletsCompanion.prefab, 579);
                BlankBulletsCompanionattackComp.fireCooldown = 5;
                BlankBulletsCompanionattackComp.doesBlank = true;
                BlankBulletsCompanionattackComp.roomDamageAmount = 0;

                LeadOfLife.PlatinumBulletsCompanion.prefab = BuildIndividualPrefab(LeadOfLife.PlatinumBulletsCompanion, "platinumbulletscompanion", 627, 7, new IntVector2(7, 7), new IntVector2(6, 0));
                LeadOfLifeComplexShooter PlatinumBulletsCompanionattackComp = LeadOfLifeComplexShooter.AddToPrefab(LeadOfLife.PlatinumBulletsCompanion.prefab, 627);
                Projectile PlatinumBulletsBullet = GetProjectileForID(545);
                PlatinumBulletsBullet.baseData.damage = 7;
                PlatinumBulletsCompanionattackComp.bulletsToFire = new List<Projectile>() { PlatinumBulletsBullet };
                PlatinumBulletsCompanionattackComp.platinum = true;
                #endregion

                #region Other bullety items

                LeadOfLife.LichsEyeBulletsCompanionA.prefab = BuildIndividualPrefab(LeadOfLife.LichsEyeBulletsCompanionA, "lichseyebulletscompaniona", 815, 7, new IntVector2(6, 6), new IntVector2(6, 2));
                LeadOfLifeBasicShooter LichsEyeBulletsACompanionattackComp = LeadOfLifeBasicShooter.AddToPrefab(LeadOfLife.LichsEyeBulletsCompanionA.prefab, 815);
                Projectile LichsEyeBulletsABullet = GetProjectileForID(604);
                LichsEyeBulletsACompanionattackComp.bulletsToFire = new List<Projectile>() { LichsEyeBulletsABullet, LichsEyeBulletsABullet, LichsEyeBulletsABullet,
                LichsEyeBulletsABullet, LichsEyeBulletsABullet, LichsEyeBulletsABullet, LichsEyeBulletsABullet, (PickupObjectDatabase.GetById(604) as Gun).GetComponent<FireOnReloadSynergyProcessor>().DirectedBurstSettings.ProjectileInterface.SpecifiedProjectile };
                LichsEyeBulletsACompanionattackComp.multiShotsSequential = true;

                LeadOfLife.LichsEyeBulletsCompanionB.prefab = BuildIndividualPrefab(LeadOfLife.LichsEyeBulletsCompanionB, "lichseyebulletscompanionb", 815, 7, new IntVector2(6, 6), new IntVector2(6, 2));
                LeadOfLifeBasicShooter LichsEyeBulletsBCompanionattackComp = LeadOfLifeBasicShooter.AddToPrefab(LeadOfLife.LichsEyeBulletsCompanionB.prefab, 815);
                LichsEyeBulletsBCompanionattackComp.bulletsToFire = new List<Projectile>() { LichsEyeBulletsABullet, LichsEyeBulletsABullet, LichsEyeBulletsABullet,
                LichsEyeBulletsABullet, LichsEyeBulletsABullet, LichsEyeBulletsABullet, LichsEyeBulletsABullet, (PickupObjectDatabase.GetById(604) as Gun).GetComponent<FireOnReloadSynergyProcessor>().DirectedBurstSettings.ProjectileInterface.SpecifiedProjectile };
                LichsEyeBulletsBCompanionattackComp.multiShotsSequential = true;

                LeadOfLife.BulletTimeCompanion.prefab = BuildIndividualPrefab(LeadOfLife.BulletTimeCompanion, "bullettimecompanion", 69, 7, new IntVector2(7, 7), new IntVector2(6, 2));
                LeadOfLifeTimeSlower BulletTimeCompanionattackComp = LeadOfLifeTimeSlower.AddToPrefab(LeadOfLife.BulletTimeCompanion.prefab, 69, 7f);
                BulletTimeCompanionattackComp.fireCooldown = 25f;

                LeadOfLife.DarumaCompanion.prefab = BuildIndividualPrefab(LeadOfLife.DarumaCompanion, "darumacompanion", 643, 7, new IntVector2(7, 7), new IntVector2(6, 2));
                LeadOfLifeBasicShooter DarumaCompanionattackComp = LeadOfLifeBasicShooter.AddToPrefab(LeadOfLife.DarumaCompanion.prefab, 643);
                DarumaCompanionattackComp.bulletsToFire = new List<Projectile>() { GetProjectileForID(53) };
                DarumaCompanionattackComp.attackOnTimer = false;
                DarumaCompanionattackComp.attacksOnActiveUse = true;
                DarumaCompanionattackComp.activeItemIDToAttackOn = 643;

                LeadOfLife.RiddleOfLeadCompanion.prefab = BuildIndividualPrefab(LeadOfLife.RiddleOfLeadCompanion, "riddleofleadcompanion", 271, 7, new IntVector2(8, 7), new IntVector2(6, 2));
                LeadOfLifeBasicShooter RiddleOfLeadCompanionattackComp = LeadOfLifeBasicShooter.AddToPrefab(LeadOfLife.RiddleOfLeadCompanion.prefab, 271, 3.5f);
                RiddleOfLeadCompanionattackComp.bulletsToFire = new List<Projectile>() { GetProjectileForID(GunOfAThousandSins.GunOfAThousandSinsID) };
                RiddleOfLeadCompanionattackComp.fireCooldown = 5f;

                LeadOfLife.ShotgunCoffeeCompanion.prefab = BuildIndividualPrefab(LeadOfLife.ShotgunCoffeeCompanion, "shotguncoffeecompanion", 427, 11, new IntVector2(7, 7), new IntVector2(6, 2));
                LeadOfLifeGooper ShotgunCoffeeCompanionattackComp = LeadOfLifeGooper.AddToPrefab(LeadOfLife.ShotgunCoffeeCompanion.prefab, 427, 10f);
                ShotgunCoffeeCompanionattackComp.fireCooldown = 4.5f;
                ShotgunCoffeeCompanionattackComp.goopRadiusOrWidth = 3f;
                ShotgunCoffeeCompanionattackComp.goopDefToSpawn = EasyGoopDefinitions.GenerateBloodGoop(5f, ExtendedColours.brown, 10);
                ShotgunCoffeeCompanionattackComp.doGoopCircle = true;

                LeadOfLife.ShotgaColaCompanion.prefab = BuildIndividualPrefab(LeadOfLife.ShotgaColaCompanion, "shotgacolacompanion", 426, 11, new IntVector2(7, 7), new IntVector2(5, 2));
                LeadOfLifeGooper ShotgaColaCompanionattackComp = LeadOfLifeGooper.AddToPrefab(LeadOfLife.ShotgaColaCompanion.prefab, 426, 10f);
                ShotgaColaCompanionattackComp.fireCooldown = 2f;
                ShotgaColaCompanionattackComp.goopRadiusOrWidth = 1.5f;
                ShotgaColaCompanionattackComp.goopDefToSpawn = EasyGoopDefinitions.GenerateBloodGoop(5f, ExtendedColours.brown, 10);

                LeadOfLife.ElderBlankCompanion.prefab = BuildIndividualPrefab(LeadOfLife.ElderBlankCompanion, "elderblankcompanion", 499, 7, new IntVector2(7, 7), new IntVector2(5, 2));
                LeadOfLifeMassRoomDamager ElderBlankCompanionattackComp = LeadOfLifeMassRoomDamager.AddToPrefab(LeadOfLife.ElderBlankCompanion.prefab, 499);
                ElderBlankCompanionattackComp.attackOnTimer = false;
                ElderBlankCompanionattackComp.attacksOnActiveUse = true;
                ElderBlankCompanionattackComp.activeItemIDToAttackOn = 499;
                ElderBlankCompanionattackComp.doesBlank = true;
                ElderBlankCompanionattackComp.roomDamageAmount = 20;
                ElderBlankCompanionattackComp.blankType = EasyBlankType.FULL;

                LeadOfLife.BulletGunCompanion.prefab = BuildIndividualPrefab(LeadOfLife.BulletGunCompanion, "bulletguncompanion", 503, 7, new IntVector2(5, 5), new IntVector2(4, 1));
                LeadOfLifeBasicShooter BulletGunCompanionattackComp = LeadOfLifeBasicShooter.AddToPrefab(LeadOfLife.BulletGunCompanion.prefab, 503);
                BulletGunCompanionattackComp.bulletsToFire = new List<Projectile>() { GetProjectileForID(503) };
                BulletGunCompanionattackComp.fireCooldown = 1.6f;

                LeadOfLife.ShellGunCompanion.prefab = BuildIndividualPrefab(LeadOfLife.ShellGunCompanion, "shellguncompanion", 512, 7, new IntVector2(7, 7), new IntVector2(7, 2));
                LeadOfLifeBasicShooter ShellGunCompanionattackComp = LeadOfLifeBasicShooter.AddToPrefab(LeadOfLife.ShellGunCompanion.prefab, 512);
                Projectile ShellGunCompanionBullet = GetProjectileForID(512);
                ShellGunCompanionattackComp.bulletsToFire = new List<Projectile>() { ShellGunCompanionBullet, ShellGunCompanionBullet, ShellGunCompanionBullet };
                ShellGunCompanionattackComp.fireCooldown = 2f;
                ShellGunCompanionattackComp.angleVariance = 16f;

                LeadOfLife.CaseyCompanion.prefab = BuildIndividualPrefab(LeadOfLife.CaseyCompanion, "caseycompanion", 541, 7, new IntVector2(4, 4), new IntVector2(8, 2));
                LeadOfLifeBasicShooter CaseyCompanionattackComp = LeadOfLifeBasicShooter.AddToPrefab(LeadOfLife.CaseyCompanion.prefab, 541);
                CaseyCompanionattackComp.bulletsToFire = new List<Projectile>() { GetProjectileForID(541) };
                CaseyCompanionattackComp.fireCooldown = 3.5f;

                LeadOfLife.BTCKTPCompanion.prefab = BuildIndividualPrefab(LeadOfLife.BTCKTPCompanion, "BTCKTPcompanion", 303, 7, new IntVector2(8, 8), new IntVector2(6, 2));
                LeadOfLifeCompanion BTCKTPCompanionattackComp = LeadOfLifeCompanion.AddToPrefab(LeadOfLife.BTCKTPCompanion.prefab, 303, 7, new List<MovementBehaviorBase>() { new CompanionFollowPlayerBehavior { IdleAnimations = new string[] { "idle" } } });
                BTCKTPCompanionattackComp.aiActor.CollisionDamage = 0f;
                BTCKTPCompanionattackComp.aiActor.specRigidbody.CollideWithOthers = true;
                BTCKTPCompanionattackComp.aiActor.specRigidbody.CollideWithTileMap = false;
                BTCKTPCompanionattackComp.healthHaver.PreventAllDamage = true;
                BTCKTPCompanionattackComp.aiActor.specRigidbody.PixelColliders.Clear();
                BTCKTPCompanionattackComp.aiActor.specRigidbody.PixelColliders.Add(new PixelCollider
                {
                    ColliderGenerationMode = PixelCollider.PixelColliderGeneration.Manual,
                    CollisionLayer = CollisionLayer.EnemyCollider,
                    IsTrigger = false,
                    BagleUseFirstFrameOnly = false,
                    SpecifyBagelFrame = string.Empty,
                    BagelColliderNumber = 0,
                    ManualOffsetX = 6,
                    ManualOffsetY = 2,
                    ManualWidth = 8,
                    ManualHeight = 17,
                    ManualDiameter = 0,
                    ManualLeftX = 0,
                    ManualLeftY = 0,
                    ManualRightX = 0,
                    ManualRightY = 0
                });
                BTCKTPCompanionattackComp.aiAnimator.specRigidbody.PixelColliders.Add(new PixelCollider
                {
                    ColliderGenerationMode = PixelCollider.PixelColliderGeneration.Manual,
                    CollisionLayer = CollisionLayer.PlayerHitBox,
                    IsTrigger = false,
                    BagleUseFirstFrameOnly = false,
                    SpecifyBagelFrame = string.Empty,
                    BagelColliderNumber = 0,
                    ManualOffsetX = 6,
                    ManualOffsetY = 2,
                    ManualWidth = 8,
                    ManualHeight = 8,
                    ManualDiameter = 0,
                    ManualLeftX = 0,
                    ManualLeftY = 0,
                    ManualRightX = 0,
                    ManualRightY = 0
                });
                BTCKTPCompanionattackComp.CanInterceptBullets = true;
                #endregion

                #region Custom Bullet Modifiers
                LeadOfLife.OneShotCompanion.prefab = BuildIndividualPrefab(LeadOfLife.OneShotCompanion, "oneshotcompanion", OneShot.OneShotID, 7, new IntVector2(7, 7), new IntVector2(10, 1), false, false, true);
                LeadOfLifeBasicShooter OneShotCompanionattackComp = LeadOfLifeBasicShooter.AddToPrefab(LeadOfLife.OneShotCompanion.prefab, OneShot.OneShotID, 8f) ;
                Projectile OneShotCompanionBullet = GetProjectileForID(86);
                OneShotCompanionBullet.baseData.damage *= 2;
                OneShotCompanionBullet.baseData.speed *= 2;
                OneShotCompanionBullet.baseData.range *= 2;
                OneShotCompanionBullet.baseData.force *= 2;
                OneShotCompanionBullet.BossDamageMultiplier *= 2;
                OneShotCompanionBullet.BlackPhantomDamageMultiplier *= 2;
                OneShotCompanionBullet.AdditionalScaleMultiplier *= 2;
                OneShotCompanionattackComp.bulletsToFire = new List<Projectile>() { OneShotCompanionBullet };
                OneShotCompanionattackComp.fireCooldown = 0.65f;

                LeadOfLife.FiftyCalRoundsCompanion.prefab = BuildIndividualPrefab(LeadOfLife.FiftyCalRoundsCompanion, "fiftycalroundscompanion", FiftyCalRounds.FiftyCalRoundsID, 7, new IntVector2(6, 6), new IntVector2(6, 1), false, false, true);
                LeadOfLifeBasicShooter FiftyCalRoundsCompanionattackComp = LeadOfLifeBasicShooter.AddToPrefab(LeadOfLife.FiftyCalRoundsCompanion.prefab, FiftyCalRounds.FiftyCalRoundsID);
                Projectile FiftyCalRoundsCompanionBullet = GetProjectileForID(49);
                FiftyCalRoundsCompanionBullet.baseData.damage = 8;
                FiftyCalRoundsCompanionattackComp.bulletsToFire = new List<Projectile>() { FiftyCalRoundsCompanionBullet };

                LeadOfLife.AlkaliBulletsCompanion.prefab = BuildIndividualPrefab(LeadOfLife.AlkaliBulletsCompanion, "alkalibulletscompanion", AlkaliBullets.AlkaliBulletsID, 7, new IntVector2(7, 7), new IntVector2(6, 1), false, false, true);
                LeadOfLifeBasicShooter AlkaliBulletsCompanionattackComp = LeadOfLifeBasicShooter.AddToPrefab(LeadOfLife.AlkaliBulletsCompanion.prefab, AlkaliBullets.AlkaliBulletsID);
                Projectile AlkaliBulletsCompanionBullet = GetProjectileForID(VacuumGun.ID);
                ProjectileInstakillBehaviour blobkill = AlkaliBulletsCompanionBullet.gameObject.GetOrAddComponent<ProjectileInstakillBehaviour>();
                blobkill.tagsToKill.Add("blobulon");
                blobkill.protectBosses = false;
                blobkill.enemyGUIDSToEraseFromExistence.Add(EnemyGuidDatabase.Entries["bloodbulon"]);
                AlkaliBulletsCompanionattackComp.bulletsToFire = new List<Projectile>() { AlkaliBulletsCompanionBullet };

                LeadOfLife.AntimagicRoundsCompanion.prefab = BuildIndividualPrefab(LeadOfLife.AntimagicRoundsCompanion, "antimagicroundscompanion", AntimagicRounds.AntimagicRoundsID, 7, new IntVector2(7, 7), new IntVector2(9, 1), false, false, true);
                LeadOfLifeBasicShooter AntimagicRoundsCompanionattackComp = LeadOfLifeBasicShooter.AddToPrefab(LeadOfLife.AntimagicRoundsCompanion.prefab, AntimagicRounds.AntimagicRoundsID);
                Projectile AntimagicRoundsCompanionBullet = GetProjectileForID(56);
                AntimagicRoundsCompanionBullet.AdjustPlayerProjectileTint(ExtendedColours.purple, 1); 
                ProjectileInstakillBehaviour wizkill = AntimagicRoundsCompanionBullet.gameObject.GetOrAddComponent<ProjectileInstakillBehaviour>();
                wizkill.tagsToKill.AddRange(new List<string> { "gunjurer", "gunsinger", "bookllet" });
                wizkill.enemyGUIDsToKill.AddRange(new List<string> { EnemyGuidDatabase.Entries["wizbang"], EnemyGuidDatabase.Entries["pot_fairy"] });
                AntimagicRoundsCompanionattackComp.bulletsToFire = new List<Projectile>() { AntimagicRoundsCompanionBullet };

                LeadOfLife.AntimatterBulletsCompanion.prefab = BuildIndividualPrefab(LeadOfLife.AntimatterBulletsCompanion, "antimatterbulletscompanion", AntimatterBullets.AntimatterBulletsID, 7, new IntVector2(7, 7), new IntVector2(6, 1), false, false, true);
                LeadOfLifeBasicShooter AntimatterBulletsCompanionattackComp = LeadOfLifeBasicShooter.AddToPrefab(LeadOfLife.AntimatterBulletsCompanion.prefab, AntimatterBullets.AntimatterBulletsID);
                Projectile AntimatterBulletsCompanionBullet = GetProjectileForID(86);
                AntimatterBulletsModifier antimattermod = AntimatterBulletsCompanionBullet.gameObject.GetOrAddComponent<AntimatterBulletsModifier>();
                antimattermod.explosionData = AntimatterBullets.smallPlayerSafeExplosion;
                AntimatterBulletsCompanionattackComp.bulletsToFire = new List<Projectile>() { AntimatterBulletsCompanionBullet };

                LeadOfLife.BashfulShotCompanion.prefab = BuildIndividualPrefab(LeadOfLife.BashfulShotCompanion, "bashfulshotcompanion", BashfulShot.BashfulShotID, 7, new IntVector2(8, 8), new IntVector2(5, 1), false, false, true);
                LeadOfLifeCompanionCountReactiveShooter BashfulShotCompanionAttackComp = LeadOfLifeCompanionCountReactiveShooter.AddToPrefab(LeadOfLife.BashfulShotCompanion.prefab, BashfulShot.BashfulShotID);
                Projectile BashfulShotCompanionBullet = GetProjectileForID(9);
                BashfulShotCompanionBullet.baseData.damage *= 0.5f;
                BashfulShotCompanionAttackComp.bulletsToFire = new List<Projectile>() { BashfulShotCompanionBullet };
                BashfulShotCompanionAttackComp.bashfulShot = true;

                LeadOfLife.BashingBulletsCompanion.prefab = BuildIndividualPrefab(LeadOfLife.BashingBulletsCompanion, "bashingbulletscompanion", BashingBullets.BashingBulletsID, 7, new IntVector2(7, 7), new IntVector2(7, 1), false, false, true);
                LeadOfLifeBasicShooter BashingBulletsCompanionattackComp = LeadOfLifeBasicShooter.AddToPrefab(LeadOfLife.BashingBulletsCompanion.prefab, BashingBullets.BashingBulletsID);
                Projectile BashingBulletsBullet = GetProjectileForID(541);
                BashingBulletsBullet.baseData.damage *= 0.125f;
                BashingBulletsBullet.baseData.range *= 0.5f;
                BashingBulletsBullet.AppliesStun = true;
                BashingBulletsBullet.StunApplyChance = 0.5f;
                BashingBulletsBullet.AppliedStunDuration = 1;
                BashingBulletsCompanionattackComp.bulletsToFire = new List<Projectile>() { BashingBulletsBullet };

                LeadOfLife.BirdshotCompanion.prefab = BuildIndividualPrefab(LeadOfLife.BirdshotCompanion, "birdshotcompanion", Birdshot.BirdshotID, 7, new IntVector2(7, 7), new IntVector2(14, 1), false, false, true);
                LeadOfLifeBasicShooter BirdshotCompanionattackComp = LeadOfLifeBasicShooter.AddToPrefab(LeadOfLife.BirdshotCompanion.prefab, Birdshot.BirdshotID);
                Projectile BirdshotCompanionBullet = GetProjectileForID(86);
                SelectiveDamageMult birdshotmult = BirdshotCompanionBullet.gameObject.GetOrAddComponent<SelectiveDamageMult>();
                birdshotmult.multOnFlyingEnemies = true; birdshotmult.multiplier = 1.4f;
                BirdshotCompanionattackComp.bulletsToFire = new List<Projectile>() { BirdshotCompanionBullet };

                LeadOfLife.BlightShellCompanion.prefab = BuildIndividualPrefab(LeadOfLife.BlightShellCompanion, "blightshellcompanion", BlightShell.BlightShellID, 7, new IntVector2(7, 7), new IntVector2(8, 1), false, false, true);
                LeadOfLifeBasicShooter BlightShellCompanionattackComp = LeadOfLifeBasicShooter.AddToPrefab(LeadOfLife.BlightShellCompanion.prefab, BlightShell.BlightShellID);
                Projectile BlightShellCompanionBullet = GetProjectileForID(86);
                ScaleProjectileStatOffPlayerStat blightScale = BlightShellCompanionBullet.gameObject.AddComponent<ScaleProjectileStatOffPlayerStat>();
                blightScale.multiplierPerLevelOfStat = 0.15f;
                blightScale.projstat = ScaleProjectileStatOffPlayerStat.ProjectileStatType.DAMAGE;
                blightScale.playerstat = PlayerStats.StatType.Curse;
                BlightShellCompanionBullet.AdjustPlayerProjectileTint(ExtendedColours.cursedBulletsPurple, 1);
                BlightShellCompanionBullet.CurseSparks = true;
                BlightShellCompanionattackComp.bulletsToFire = new List<Projectile>() { BlightShellCompanionBullet, BlightShellCompanionBullet, BlightShellCompanionBullet, BlightShellCompanionBullet };
                BlightShellCompanionattackComp.fireCooldown = 3.5f;

                LeadOfLife.BloodthirstyBulletsCompanion.prefab = BuildIndividualPrefab(LeadOfLife.BloodthirstyBulletsCompanion, "bloodthirstybulletscompanion", BloodthirstyBullets.BloodthirstyBulletsID, 7, new IntVector2(7, 7), new IntVector2(5, 1), false, false, true);
                LeadOfLifeBasicShooter BloodthirstyBulletsCompanionattackComp = LeadOfLifeBasicShooter.AddToPrefab(LeadOfLife.BloodthirstyBulletsCompanion.prefab, BloodthirstyBullets.BloodthirstyBulletsID);
                Projectile BloodthirstyBulletsCompanionBullet = GetProjectileForID(86);
                BloodthirstyBulletsCompanionBullet.gameObject.GetOrAddComponent<BloodthirstyBulletsComp>();
                BloodthirstyBulletsCompanionattackComp.bulletsToFire = new List<Projectile>() { BloodthirstyBulletsCompanionBullet };

                LeadOfLife.TitanBulletsCompanion.prefab = BuildIndividualPrefab(LeadOfLife.TitanBulletsCompanion, "titanbulletscompanion", TitanBullets.ID, 5, new IntVector2(9, 9), new IntVector2(11, 2), false, false, true);
                LeadOfLifeBasicShooter TitanBulletsCompanionattackComp = LeadOfLifeBasicShooter.AddToPrefab(LeadOfLife.TitanBulletsCompanion.prefab, TitanBullets.ID, 3);
                Projectile TitanBulletsBullet = GetProjectileForID(541);
                TitanBulletsBullet.AdditionalScaleMultiplier = 10000;
                TitanBulletsBullet.baseData.damage *= 1.05f;
                TitanBulletsBullet.AppliesStun = true;
                TitanBulletsBullet.StunApplyChance = 0.7f;
                TitanBulletsBullet.AppliedStunDuration = 2;
                TitanBulletsCompanionattackComp.bulletsToFire = new List<Projectile>() { TitanBulletsBullet };               
                #endregion
            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
                ETGModConsole.Log(e.StackTrace);
            }
        }       
    }
}
