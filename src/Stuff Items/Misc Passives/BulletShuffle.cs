using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using ItemAPI;
using UnityEngine;
using MonoMod.RuntimeDetour;
using System.Reflection;
using Dungeonator;
using SaveAPI;
using Alexandria.Misc;

namespace NevernamedsItems
{
    public class BulletShuffle : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Bullet Shuffle";
            string resourceName = "NevernamedsItems/Resources/NeoItemSprites/bulletshuffle_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<BulletShuffle>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Ask Questions Later";
            string longDesc = "Grants completely random bullet effects on every shot."+"\n\nA belt of infinite potential, you can pluck any type of ammunition from it's ample supply.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.S;

            item.SetupUnlockOnCustomFlag(CustomDungeonFlags.BOSSRUSH_GUNSLINGER, true);
        }

        private void PostProcessProjectile(Projectile sourceProjectile, float effectChanceScalar)
        {
            try
            {
                if (sourceProjectile.GetComponent<HasBeenBulletShuffled>() == null)
                {
                    int num = UnityEngine.Random.Range(1, 55);
                    //VFXToolbox.DoStringSquirt(num.ToString(), sourceProjectile.ProjectilePlayerOwner().specRigidbody.UnitCenter, Color.red);
                    switch (num)
                    {
                        case 1: //+1 Bullets ------------------------------- WORKS
                            sourceProjectile.baseData.damage *= 1.25f;
                            break;
                        case 2: //Rocket Powered Bullets ------------------------------- WORKS
                            sourceProjectile.baseData.damage *= 1.1f;
                            sourceProjectile.baseData.speed *= 1.5f;
                            sourceProjectile.UpdateSpeed();
                            break;
                        case 3: //Devolver Bullets ------------------------------- WORKS
                            AdvancedTransmogrifyBehaviour advancedTransmog = sourceProjectile.gameObject.AddComponent<AdvancedTransmogrifyBehaviour>();
                            advancedTransmog.TransmogDataList = new List<AdvancedTransmogrifyBehaviour.TransmogData>()
                        {
                            new AdvancedTransmogrifyBehaviour.TransmogData()
                            {
                                identifier = "BulletShuffle",
                                maintainHPPercent = false,
                                TargetGuids =  new List<string>(){"05891b158cd542b1a5f3df30fb67a7ff" },
                                TransmogChance = 1,
                            }
                        };
                            break;
                        case 4: //Vorpal Bullets
                            StartCoroutine(EraseAndReplace(sourceProjectile, PickupObjectDatabase.GetById(640).GetComponent<ComplexProjectileModifier>().CriticalProjectile));
                            break;
                        case 5: //Katana Bullets ------------------------------- WORKS
                            sourceProjectile.OnHitEnemy += DoKatana;
                            break;
                        case 6: //Hungry Bullets ------------------------------- WORKS
                            HungryProjectileModifier hungry = sourceProjectile.gameObject.GetOrAddComponent<HungryProjectileModifier>();
                            hungry.HungryRadius = 1.5f;
                            hungry.DamagePercentGainPerSnack = 0.25f;
                            hungry.MaxMultiplier = 3f;
                            hungry.MaximumBulletsEaten = 10;
                            sourceProjectile.AdjustPlayerProjectileTint(ExtendedColours.purple, 1);
                            break;
                        case 7: //Heavy Bullets ------------------------------- WORKS
                            sourceProjectile.baseData.damage *= 1.25f;
                            sourceProjectile.baseData.speed *= 0.5f;
                            sourceProjectile.baseData.force *= 2f;
                            sourceProjectile.RuntimeUpdateScale(1.25f);
                            sourceProjectile.UpdateSpeed();
                            break;
                        case 8: //Bouncy Bullets ------------------------------- WORKS
                            BounceProjModifier bounce = sourceProjectile.gameObject.GetOrAddComponent<BounceProjModifier>();
                            bounce.numberOfBounces++;
                            break;
                        case 9: //Explosive Rounds ------------------------------- WORKS
                            ExplosiveModifier explosion = sourceProjectile.gameObject.AddComponent<ExplosiveModifier>();
                            explosion.doExplosion = true;
                            explosion.explosionData = StaticExplosionDatas.explosiveRoundsExplosion;
                            break;
                        case 10: //Ghost Bullets ------------------------------- WORKS
                            PierceProjModifier piercing = sourceProjectile.gameObject.AddComponent<PierceProjModifier>();
                            piercing.penetration++;
                            break;
                        case 11: //Irradiated Lead ------------------------------- WORKS
                            sourceProjectile.statusEffectsToApply.Add(StaticStatusEffects.irradiatedLeadEffect);
                            sourceProjectile.AdjustPlayerProjectileTint(ExtendedColours.poisonGreen, 1);
                            break;
                        case 12: //Hot Lead ------------------------------- WORKS
                            sourceProjectile.statusEffectsToApply.Add(StaticStatusEffects.hotLeadEffect);
                            sourceProjectile.AdjustPlayerProjectileTint(Color.red, 1);
                            break;
                        case 13: //Battery Bullets ------------------------------- WORKS
                            sourceProjectile.damageTypes |= CoreDamageTypes.Electric;
                            break;
                        case 14: //Frost Bullets (Actully just insta freeze but whatever)
                            sourceProjectile.statusEffectsToApply.Add(StaticStatusEffects.chaosBulletsFreeze);
                            sourceProjectile.AdjustPlayerProjectileTint(ExtendedColours.freezeBlue, 1);
                            break;
                        case 15: //Charming Rounds ------------------------------- WORKS
                            sourceProjectile.statusEffectsToApply.Add(StaticStatusEffects.charmingRoundsEffect);
                            sourceProjectile.AdjustPlayerProjectileTint(ExtendedColours.charmPink, 1);
                            break;
                        case 16: //Magic Bullets ------------------------------- WORKS
                            AdvancedTransmogrifyBehaviour advancedTransmog2 = sourceProjectile.gameObject.AddComponent<AdvancedTransmogrifyBehaviour>();
                            advancedTransmog2.TransmogDataList = new List<AdvancedTransmogrifyBehaviour.TransmogData>()
                        {
                            new AdvancedTransmogrifyBehaviour.TransmogData()
                            {
                                identifier = "BulletShuffle",
                                maintainHPPercent = false,
                                TargetGuids = new List<string>(){"76bc43539fc24648bff4568c75c686d1" },
                                TransmogChance = 1,
                            }
                        };
                            break;
                        case 17: //Fat Bullets ------------------------------- WORKS
                            sourceProjectile.baseData.force *= 2f;
                            sourceProjectile.RuntimeUpdateScale(2f);
                            sourceProjectile.baseData.damage *= 1.3f;
                            break;
                        case 18: //Angry Bullets ------------------------------- WORKS
                            AngryBulletsProjectileBehaviour angry = sourceProjectile.gameObject.AddComponent<AngryBulletsProjectileBehaviour>();
                            break;
                        case 19: //Blank Bullets
                            sourceProjectile.gameObject.GetOrAddComponent<BlankProjModifier>();
                            break;
                        case 20: //Orbital Bullets Behaviour ------------------------------- WORKS
                            OrbitalBulletsBehaviour orbiting = sourceProjectile.gameObject.GetOrAddComponent<OrbitalBulletsBehaviour>();
                            break;
                        case 21: //Shadow Bullets ------------------------------- WORKS
                            sourceProjectile.SpawnChainedShadowBullets(1, 0.05f, 1, null, true);
                            break;
                        case 22: //Stout Bullets ------------------------------- WORKS
                            sourceProjectile.RuntimeUpdateScale(1.5f);
                            sourceProjectile.gameObject.GetOrAddComponent<StoutBulletsProjectileBehaviour>();
                            break;
                        case 23: //Snowballets ------------------------------- WORKS
                            ScalingProjectileModifier scalingProjectileModifier = sourceProjectile.gameObject.AddComponent<ScalingProjectileModifier>();
                            scalingProjectileModifier.ScaleToDamageRatio = 2.5f / 10f;
                            scalingProjectileModifier.MaximumDamageMultiplier = 2.5f;
                            scalingProjectileModifier.IsSynergyContingent = false;
                            scalingProjectileModifier.PercentGainPerUnit = 10f;
                            break;
                        case 24: //Remote Bullets ------------------------------- WORKS
                            sourceProjectile.gameObject.GetOrAddComponent<RemoteBulletsProjectileBehaviour>();
                            break;
                        case 25: //Zombie Bullets ------------------------------- WORKS
                            sourceProjectile.OnDestruction += HandleZombieEffect;
                            break;
                        case 26: //Flak Bullets ------------------------------- WORKS
                            SpawnProjModifier flakspawnProjModifier = sourceProjectile.gameObject.AddComponent<SpawnProjModifier>();
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
                            break;
                        case 27: //Silver Bullets ------------------------------- WORKS
                            sourceProjectile.BossDamageMultiplier *= 1.25f;
                            sourceProjectile.BlackPhantomDamageMultiplier *= 2.25f;
                            sourceProjectile.AdjustPlayerProjectileTint(ExtendedColours.silvedBulletsSilver, 1);
                            break;
                        case 28: //Gilded Bullets ------------------------------- WORKS
                            float gildnum = 0f;
                            ScalingStatBoostItem gildedbullets = PickupObjectDatabase.GetById(532).GetComponent<ScalingStatBoostItem>();
                            gildnum = Mathf.Clamp01(Mathf.InverseLerp(gildedbullets.ScalingTargetMin, gildedbullets.ScalingTargetMax, sourceProjectile.ProjectilePlayerOwner().carriedConsumables.Currency));
                            gildnum = gildedbullets.ScalingCurve.Evaluate(gildnum);
                            float gildnum2 = Mathf.Lerp(gildedbullets.MinScaling, gildedbullets.MaxScaling, gildnum);
                            sourceProjectile.baseData.damage *= gildnum2;
                            sourceProjectile.AdjustPlayerProjectileTint(gildedbullets.TintColor, gildedbullets.TintPriority, 0f);
                            break;
                        case 29: //Cursed Bullets ------------------------------- WORKS
                            float cursenum = 0f;
                            ScalingStatBoostItem cursedBullets = PickupObjectDatabase.GetById(571).GetComponent<ScalingStatBoostItem>();
                            cursenum = Mathf.Clamp01(Mathf.InverseLerp(cursedBullets.ScalingTargetMin, cursedBullets.ScalingTargetMax, sourceProjectile.ProjectilePlayerOwner().stats.GetStatValue(PlayerStats.StatType.Curse)));
                            cursenum = cursedBullets.ScalingCurve.Evaluate(cursenum);
                            float cursenum2 = Mathf.Lerp(cursedBullets.MinScaling, cursedBullets.MaxScaling, cursenum);
                            sourceProjectile.baseData.damage *= cursenum2;
                            sourceProjectile.AdjustPlayerProjectileTint(cursedBullets.TintColor, cursedBullets.TintPriority, 0f);
                            sourceProjectile.CurseSparks = true;
                            break;
                        case 30: //Chance Bullets
                            DoChanceBulletsEffect(sourceProjectile);
                            break;
                        case 31: //Bumbullets
                            StartCoroutine(SpawnAdditionalProjectile(sourceProjectile.ProjectilePlayerOwner(), sourceProjectile.specRigidbody.UnitCenter, sourceProjectile.Direction.ToAngle(), (PickupObjectDatabase.GetById(14) as Gun).DefaultModule.projectiles[0], 0));
                            break;
                        case 32: //Bloody 9mm
                            StartCoroutine(EraseAndReplace(sourceProjectile, PickupObjectDatabase.GetById(524).GetComponent<RandomProjectileReplacementItem>().ReplacementProjectile));
                            break;
                        case 33: //Slow ------------------------------- WORKS
                            sourceProjectile.statusEffectsToApply.Add(StaticStatusEffects.tripleCrossbowSlowEffect);
                            sourceProjectile.AdjustPlayerProjectileTint(Color.yellow, 1);
                            break;
                        case 34: //Wallpierce
                            sourceProjectile.pierceMinorBreakables = true;
                            sourceProjectile.PenetratesInternalWalls = true;
                            break;
                        case 35: //Ignore DPS Cap
                            sourceProjectile.ignoreDamageCaps = true;
                            break;
                        case 36: //Scattershot
                            sourceProjectile.baseData.damage *= 0.55f;
                            StartCoroutine(DoAdditionalShot(sourceProjectile, 0, 30f));
                            StartCoroutine(DoAdditionalShot(sourceProjectile, 0, 30f));
                            break;
                        case 37: //Backup Gun
                            StartCoroutine(DoAdditionalShot(sourceProjectile, 180, 5f));
                            break;
                        case 38: //Helix
                            StartCoroutine(DoAdditionalShot(sourceProjectile, 0, 0f, true));
                            sourceProjectile.ConvertToHelixMotion(false);
                            break;
                        case 39: //Killed enemies become projectiles (Casey)
                            sourceProjectile.baseData.force *= 3f;
                            KilledEnemiesBecomeProjectileModifier caseymode = sourceProjectile.gameObject.AddComponent<KilledEnemiesBecomeProjectileModifier>();
                            caseymode.CompletelyBecomeProjectile = (PickupObjectDatabase.GetById(541) as Gun).DefaultModule.chargeProjectiles[0].Projectile.GetComponent<KilledEnemiesBecomeProjectileModifier>().CompletelyBecomeProjectile;
                            caseymode.BaseProjectile = (PickupObjectDatabase.GetById(541) as Gun).DefaultModule.chargeProjectiles[0].Projectile.GetComponent<KilledEnemiesBecomeProjectileModifier>().BaseProjectile;
                            break;
                        case 40: //Instant Cheese
                            sourceProjectile.statusEffectsToApply.Add(StaticStatusEffects.instantCheese);
                            sourceProjectile.AdjustPlayerProjectileTint(ExtendedColours.honeyYellow, 1);
                            break;
                        case 41: //Stun
                            sourceProjectile.StunApplyChance = 1;
                            sourceProjectile.AppliedStunDuration = 2;
                            sourceProjectile.AppliesStun = true;
                            break;
                        case 42: //High Kaliber Effect
                            sourceProjectile.OnHitEnemy += KaliberShit;
                            break;
                        case 43: //Tangler Squarify
                            sourceProjectile.ApplyClonedShaderProjModifier((PickupObjectDatabase.GetById(175) as Gun).DefaultModule.projectiles[0].GetComponent<ShaderProjModifier>());
                            break;
                        case 44: //Akey47
                            sourceProjectile.gameObject.AddComponent<KeyProjModifier>();
                            break;
                        case 45: //Combine Vapourise
                            sourceProjectile.baseData.damage *= 2f;
                            CombineEvaporateEffect origEvap = (PickupObjectDatabase.GetById(519) as Gun).alternateVolley.projectiles[0].projectiles[0].GetComponent<CombineEvaporateEffect>();
                            CombineEvaporateEffect newEvap = sourceProjectile.gameObject.AddComponent<CombineEvaporateEffect>();
                            newEvap.FallbackShader = origEvap.FallbackShader;
                            newEvap.ParticleSystemToSpawn = origEvap.ParticleSystemToSpawn;
                            break;
                        case 46: //Snakemaker Snakify
                            AdvancedTransmogrifyBehaviour advancedTransmogSnek = sourceProjectile.gameObject.AddComponent<AdvancedTransmogrifyBehaviour>();
                            advancedTransmogSnek.TransmogDataList = new List<AdvancedTransmogrifyBehaviour.TransmogData>()
                        {
                            new AdvancedTransmogrifyBehaviour.TransmogData()
                            {
                                identifier = "BulletShuffle",
                                maintainHPPercent = false,
                                TargetGuids = new List<string>(){ "1386da0f42fb4bcabc5be8feb16a7c38" },
                                TransmogChance = 1,
                            }
                        };
                            break;
                        case 47: //Sunlight Burn
                            sourceProjectile.statusEffectsToApply.Add(StaticStatusEffects.SunlightBurn);
                            break;
                        case 48: //Tiger
                            SummonTigerModifier tigerness = sourceProjectile.gameObject.AddComponent<SummonTigerModifier>();
                            tigerness.TigerProjectilePrefab = (PickupObjectDatabase.GetById(369) as Gun).DefaultModule.chargeProjectiles[0].Projectile.GetComponent<SummonTigerModifier>().TigerProjectilePrefab;
                            break;
                        case 49: //Shield on Destroy
                            sourceProjectile.OnDestruction += SpawnShieldOnDestroy;
                            break;
                        case 50: //Glitter
                            sourceProjectile.ApplyClonedShaderProjModifier((PickupObjectDatabase.GetById(28) as Gun).modifiedFinalVolley.projectiles[0].projectiles[1].GetComponent<ShaderProjModifier>());
                            break;
                        case 51: //Finished Effect
                            sourceProjectile.gameObject.AddComponent<BlackRevolverModifier>(); 
                            break;
                        case 52: //Mimic Gun Effect
                            sourceProjectile.gameObject.AddComponent<EnemyBulletsBecomeJammedModifier>();
                            break;
                        case 53: //Green Fire
                            sourceProjectile.statusEffectsToApply.Add(StaticStatusEffects.greenFireEffect);
                            sourceProjectile.AdjustPlayerProjectileTint(Color.green, 1);
                            break;
                        case 54: //Encircler
                            sourceProjectile.ApplyClonedShaderProjModifier((PickupObjectDatabase.GetById(648) as Gun).DefaultModule.projectiles[0].GetComponent<ShaderProjModifier>());
                            break;
                    }
                    sourceProjectile.gameObject.AddComponent<HasBeenBulletShuffled>();
                }
            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
            }
        }
        private void SpawnShieldOnDestroy(Projectile self)
        {
            MiscToolbox.SpawnShield(self.ProjectilePlayerOwner(), self.sprite.WorldCenter);
        }
        private void KaliberShit(Projectile self, SpeculativeRigidbody enemy, bool fatal)
        {
            GameObject prefab = (PickupObjectDatabase.GetById(761) as Gun).DefaultModule.projectiles[0].gameObject;
            GameObject spawnBee = SpawnManager.SpawnProjectile(prefab, enemy.UnitCenter, Quaternion.Euler(0f, 0f, self.Direction.ToAngle()), true);
            if (spawnBee.GetComponent<Projectile>())
            {
                Projectile beeproj = spawnBee.GetComponent<Projectile>();
                beeproj.Owner = self.Owner;
                beeproj.Shooter = self.Shooter;
                beeproj.baseData.damage *= 0;
            }
        }
        private IEnumerator DoAdditionalShot(Projectile source, float angleFromAim, float AngleVariance, bool helix = false)
        {
            //ETGModConsole.Log("add shot ran");
            yield return null;

            if (source != null && source.PossibleSourceGun != null && source.PossibleSourceGun.DefaultModule != null)
            {
                Projectile toSpawn = source.PossibleSourceGun.DefaultModule.GetCurrentProjectile();
                if (toSpawn != null)
                {
                   StartCoroutine( SpawnAdditionalProjectile(source.ProjectilePlayerOwner(), source.specRigidbody.UnitCenter, source.Direction.ToAngle(), toSpawn, AngleVariance, angleFromAim, helix));
                }
                //else ETGModConsole.Log("Tospawn nulled");
            }
            //else ETGModConsole.Log("source or sourcegun nulled");

            //ETGModConsole.Log("bonus shot finished");
            yield break;
        }
        private IEnumerator SpawnAdditionalProjectile(PlayerController Owner, Vector2 Position, float angle, Projectile toSpawn, float variance = 0f, float FromAim = 0f, bool helix = false)
        {
            //ETGModConsole.Log("spawn additional ran");

            yield return null;
            if (toSpawn != null && toSpawn.gameObject != null)
            {
                GameObject spawnBee = SpawnManager.SpawnProjectile(toSpawn.gameObject, Position, Quaternion.Euler(0f, 0f, ProjSpawnHelper.GetAccuracyAngled(angle + FromAim, variance, Owner)), true);
                if (spawnBee.GetComponent<Projectile>())
                {
                    Projectile beeproj = spawnBee.GetComponent<Projectile>();
                    beeproj.Owner = Owner;
                    beeproj.Shooter = Owner.specRigidbody;
                    beeproj.baseData.damage *= Owner.stats.GetStatValue(PlayerStats.StatType.Damage);
                    beeproj.baseData.speed *= Owner.stats.GetStatValue(PlayerStats.StatType.ProjectileSpeed);
                    beeproj.baseData.range *= Owner.stats.GetStatValue(PlayerStats.StatType.RangeMultiplier);
                    beeproj.baseData.force *= Owner.stats.GetStatValue(PlayerStats.StatType.KnockbackMultiplier);
                    beeproj.BossDamageMultiplier *= Owner.stats.GetStatValue(PlayerStats.StatType.DamageToBosses);
                    beeproj.UpdateSpeed();

                    if (helix) beeproj.ConvertToHelixMotion(true);

                    beeproj.gameObject.AddComponent<HasBeenBulletShuffled>();
                    Owner.DoPostProcessProjectile(beeproj);
                }
            }
            yield break;
        }
        private void DoChanceBulletsEffect(Projectile source)
        {
            PlayerController m_player = source.ProjectilePlayerOwner();
            if (m_player && m_player.inventory != null)
            {
                for (int j = 0; j < m_player.inventory.AllGuns.Count; j++)
                {
                    if (m_player.inventory.AllGuns[j] && !m_player.inventory.AllGuns[j].InfiniteAmmo)
                    {
                        Gun cg2 = m_player.inventory.AllGuns[j];

                        ProjectileModule defaultModule = m_player.inventory.AllGuns[j].DefaultModule;
                        if (defaultModule.shootStyle == ProjectileModule.ShootStyle.Beam)
                        {
                            BeamController.FreeFireBeam(defaultModule.GetCurrentProjectile(), m_player, m_player.CurrentGun.CurrentAngle, 3f, true);
                        }
                        else if (defaultModule.shootStyle == ProjectileModule.ShootStyle.Charged)
                        {
                            Projectile projectile = null;
                            for (int k = 0; k < 15; k++)
                            {
                                ProjectileModule.ChargeProjectile chargeProjectile = defaultModule.chargeProjectiles[UnityEngine.Random.Range(0, defaultModule.chargeProjectiles.Count)];
                                if (chargeProjectile != null)
                                {
                                    projectile = chargeProjectile.Projectile;
                                }
                                if (projectile)
                                {
                                    break;
                                }
                            }
                            if (projectile != null) StartCoroutine(EraseAndReplace(source, projectile));
                        }
                        Projectile currentProjectile = defaultModule.GetCurrentProjectile();
                        if (currentProjectile != null) StartCoroutine(EraseAndReplace(source, currentProjectile));
                    }
                }
            }
        }
        private void HandleZombieEffect(Projectile source)
        {
            if (source && source.PossibleSourceGun && !source.PossibleSourceGun.InfiniteAmmo && !source.HasImpactedEnemy)
            {
                source.PossibleSourceGun.GainAmmo(1);
            }
        }
        private void DoKatana(Projectile sourceProjectile, SpeculativeRigidbody enemy, bool fatal)
        {
            Vector2 vector = (!enemy.aiActor) ? enemy.transform.position.XY() : enemy.aiActor.CenterPosition;
            Debug.LogError(vector);
            Vector2 vector2 = (!sourceProjectile) ? ((!enemy.healthHaver) ? BraveMathCollege.DegreesToVector(base.Owner.FacingDirection, 1f) : enemy.healthHaver.lastIncurredDamageDirection) : sourceProjectile.LastVelocity.normalized;
            if (vector2.magnitude < 0.05f)
            {
                vector2 = UnityEngine.Random.insideUnitCircle.normalized;
            }
            GameManager.Instance.Dungeon.StartCoroutine(this.HandleChainExplosion(enemy, vector, vector2.normalized));
        }
        public float LCEChainDuration = 1f;
        public float LCEChainDistance = 10f;
        public int LCEChainNumExplosions = 5;
        public GameObject LCEChainTargetSprite = PickupObjectDatabase.GetById(822).GetComponent<ComplexProjectileModifier>().LCEChainTargetSprite;
        public ExplosionData LinearChainExplosionData = PickupObjectDatabase.GetById(822).GetComponent<ComplexProjectileModifier>().LinearChainExplosionData;
        private IEnumerator HandleChainExplosion(SpeculativeRigidbody enemySRB, Vector2 startPosition, Vector2 direction)
        {
            float perExplosionTime = this.LCEChainDuration / (float)this.LCEChainNumExplosions;
            float[] explosionTimes = new float[this.LCEChainNumExplosions];
            explosionTimes[0] = 0f;
            explosionTimes[1] = perExplosionTime;
            for (int i = 2; i < this.LCEChainNumExplosions; i++)
            {
                explosionTimes[i] = explosionTimes[i - 1] + perExplosionTime;
            }
            Vector2 lastValidPosition = startPosition;
            bool hitWall = false;
            int index = 0;
            float elapsed = 0f;
            lastValidPosition = startPosition;
            hitWall = false;
            Vector2 currentDirection = direction;
            RoomHandler currentRoom = startPosition.GetAbsoluteRoom();
            float enemyDistance = -1f;
            AIActor nearestEnemy = currentRoom.GetNearestEnemyInDirection(startPosition, currentDirection, 35f, out enemyDistance, true, (!enemySRB) ? null : enemySRB.aiActor);
            if (nearestEnemy && enemyDistance < 20f)
            {
                currentDirection = (nearestEnemy.CenterPosition - startPosition).normalized;
            }
            while (elapsed < this.LCEChainDuration)
            {
                elapsed += BraveTime.DeltaTime;
                while (index < this.LCEChainNumExplosions && elapsed >= explosionTimes[index])
                {
                    Vector2 vector = startPosition + currentDirection.normalized * this.LCEChainDistance;
                    Vector2 vector2 = Vector2.Lerp(startPosition, vector, ((float)index + 1f) / (float)this.LCEChainNumExplosions);
                    if (!this.ValidExplosionPosition(vector2))
                    {
                        hitWall = true;
                    }
                    if (!hitWall)
                    {
                        lastValidPosition = vector2;
                    }
                    Exploder.Explode(lastValidPosition, this.LinearChainExplosionData, currentDirection, null, false, CoreDamageTypes.None, false);
                    index++;
                }
                yield return null;
            }
            yield break;
        }
        private bool ValidExplosionPosition(Vector2 pos)
        {
            IntVector2 intVector = pos.ToIntVector2(VectorConversions.Floor);
            return GameManager.Instance.Dungeon.data.CheckInBoundsAndValid(intVector) && GameManager.Instance.Dungeon.data[intVector].type != CellType.WALL;
        }
        private IEnumerator EraseAndReplace(Projectile target, Projectile replacement)
        {
            yield return null;
            try
            {
                if (target && target.specRigidbody)
                {
                    PlayerController player = target.ProjectilePlayerOwner();
                    if (player)
                    {
                        GameObject spawnedProj = SpawnManager.SpawnProjectile(replacement.gameObject, target.specRigidbody.UnitCenter, Quaternion.Euler(0f, 0f, target.Direction.ToAngle()), true);
                        if (spawnedProj.GetComponent<Projectile>())
                        {
                            Projectile proj = spawnedProj.GetComponent<Projectile>();
                            proj.baseData.damage *= player.stats.GetStatValue(PlayerStats.StatType.Damage);
                            proj.baseData.speed *= player.stats.GetStatValue(PlayerStats.StatType.ProjectileSpeed);
                            proj.baseData.range *= player.stats.GetStatValue(PlayerStats.StatType.RangeMultiplier);
                            proj.baseData.force *= player.stats.GetStatValue(PlayerStats.StatType.KnockbackMultiplier);
                            proj.BossDamageMultiplier *= player.stats.GetStatValue(PlayerStats.StatType.DamageToBosses);
                            proj.UpdateSpeed();
                            proj.gameObject.AddComponent<HasBeenBulletShuffled>();
                            player.DoPostProcessProjectile(proj);
                        }
                        //else ETGModConsole.Log("no proj");

                        UnityEngine.Object.Destroy(target.gameObject);
                    }
                    //else ETGModConsole.Log("no player");
                }
                //else ETGModConsole.Log("no target or no target rigidbody");
            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
            }
            yield break;
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject debrisObject = base.Drop(player);
            player.PostProcessProjectile -= this.PostProcessProjectile;
            return debrisObject;
        }
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.PostProcessProjectile += this.PostProcessProjectile;
        }
        public override void OnDestroy()
        {
            if (Owner) Owner.PostProcessProjectile -= this.PostProcessProjectile;
            base.OnDestroy();
        }
        public class HasBeenBulletShuffled : MonoBehaviour { }
    }
}