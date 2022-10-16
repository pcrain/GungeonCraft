using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ItemAPI;
using System.Collections;
using System.Reflection;
using MonoMod.RuntimeDetour;

namespace NevernamedsItems
{
    class UltraVioletGuonStone : AdvancedPlayerOrbitalItem
    {

        public static PlayerOrbital orbitalPrefab;
        public static PlayerOrbital upgradeOrbitalPrefab;
        public static void Init()
        {
            string itemName = "Ultraviolet Guon Stone"; //The name of the item
            string resourceName = "NevernamedsItems/Resources/GuonStones/ultravioletguonstone_icon"; //Refers to an embedded png in the project. Make sure to embed your resources!

            GameObject obj = new GameObject();

            var item = obj.AddComponent<UltraVioletGuonStone>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            string shortDesc = "Beyond the Pale";
            string longDesc = "A jittery crystal from a realm beyond the Gungeon." + "\n\nErratically jumps to different orbits.";

            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.C;

            BuildPrefab();
            item.OrbitalPrefab = orbitalPrefab;
            BuildSynergyPrefab();

            item.HasAdvancedUpgradeSynergy = true;
            item.AdvancedUpgradeSynergy = "Ultravioleter Guon Stone";
            item.AdvancedUpgradeOrbitalPrefab = UltraVioletGuonStone.upgradeOrbitalPrefab.gameObject;

            xenochromePrefab = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(86) as Gun).DefaultModule.projectiles[0]);
            xenochromePrefab.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(xenochromePrefab.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(xenochromePrefab);
            xenochromePrefab.baseData.damage = 20f;
            xenochromePrefab.baseData.speed = 0f;
            xenochromePrefab.AnimateProjectile(new List<string> {
                "thinline_pinkproj_001",
                "thinline_pinkproj_002",
                "thinline_pinkproj_003",
                "thinline_pinkproj_004",
                "thinline_pinkproj_005",
                "thinline_pinkproj_006",
            }, 10, true, new List<IntVector2> {
                new IntVector2(10, 10),
                new IntVector2(10, 10),
                new IntVector2(10, 10),
                new IntVector2(10, 10),
                new IntVector2(10, 10),
                new IntVector2(10, 10),
            }, AnimateBullet.ConstructListOfSameValues(true, 6), AnimateBullet.ConstructListOfSameValues(tk2dBaseSprite.Anchor.MiddleCenter, 6), AnimateBullet.ConstructListOfSameValues(true, 6), AnimateBullet.ConstructListOfSameValues(false, 6),
            AnimateBullet.ConstructListOfSameValues<Vector3?>(null, 6), AnimateBullet.ConstructListOfSameValues<IntVector2?>(null, 6), AnimateBullet.ConstructListOfSameValues<IntVector2?>(null, 6), AnimateBullet.ConstructListOfSameValues<Projectile>(null, 6));
            BulletLifeTimer timer = xenochromePrefab.gameObject.AddComponent<BulletLifeTimer>();
            timer.secondsTillDeath = 2;
            ExplosiveModifier splode = xenochromePrefab.gameObject.AddComponent<ExplosiveModifier>();
            splode.doExplosion = true;
            splode.explosionData = StaticExplosionDatas.explosiveRoundsExplosion;
            splode.IgnoreQueues = true;
        }
        public static Projectile xenochromePrefab;
        public static void BuildPrefab()
        {
            if (UltraVioletGuonStone.orbitalPrefab != null) return;
            GameObject prefab = SpriteBuilder.SpriteFromResource("NevernamedsItems/Resources/GuonStones/ultravioletguon_ingame");
            prefab.name = "Ultraviolet Guon Orbital";
            var body = prefab.GetComponent<tk2dSprite>().SetUpSpeculativeRigidbody(IntVector2.Zero, new IntVector2(8, 9));
            body.CollideWithTileMap = false;
            body.CollideWithOthers = true;
            body.PrimaryPixelCollider.CollisionLayer = CollisionLayer.EnemyBulletBlocker;

            orbitalPrefab = prefab.AddComponent<PlayerOrbital>();
            orbitalPrefab.motionStyle = PlayerOrbital.OrbitalMotionStyle.ORBIT_PLAYER_ALWAYS;
            orbitalPrefab.shouldRotate = true;
            orbitalPrefab.orbitRadius = 4.1f;
            orbitalPrefab.orbitDegreesPerSecond = 420f;
            orbitalPrefab.SetOrbitalTier(0);

            EasyTrailMisc trail = prefab.AddComponent<EasyTrailMisc>();
            trail.TrailPos = prefab.transform.position;
            trail.TrailPos.x += 0.2f;
            trail.StartWidth = 0.2f;
            trail.EndWidth = 0;
            trail.LifeTime = 0.1f;
            trail.BaseColor = ExtendedColours.charmPink;
            trail.EndColor = ExtendedColours.pink;

            GameObject.DontDestroyOnLoad(prefab);
            FakePrefab.MarkAsFakePrefab(prefab);
            prefab.SetActive(false);
        }
        public static void BuildSynergyPrefab()
        {
            bool flag = UltraVioletGuonStone.upgradeOrbitalPrefab == null;
            if (flag)
            {
                GameObject gameObject = SpriteBuilder.SpriteFromResource("NevernamedsItems/Resources/GuonStones/ultravioletguon_synergy", null);
                gameObject.name = "Ultraviolet Guon Orbital Synergy Form";
                SpeculativeRigidbody speculativeRigidbody = gameObject.GetComponent<tk2dSprite>().SetUpSpeculativeRigidbody(IntVector2.Zero, new IntVector2(14, 14));
                UltraVioletGuonStone.upgradeOrbitalPrefab = gameObject.AddComponent<PlayerOrbital>();
                speculativeRigidbody.CollideWithTileMap = false;
                speculativeRigidbody.CollideWithOthers = true;
                speculativeRigidbody.PrimaryPixelCollider.CollisionLayer = CollisionLayer.EnemyBulletBlocker;
                UltraVioletGuonStone.upgradeOrbitalPrefab.shouldRotate = true;
                UltraVioletGuonStone.upgradeOrbitalPrefab.orbitRadius = 4.1f;
                UltraVioletGuonStone.upgradeOrbitalPrefab.orbitDegreesPerSecond = 500f;
                UltraVioletGuonStone.upgradeOrbitalPrefab.perfectOrbitalFactor = 10f;
                UltraVioletGuonStone.upgradeOrbitalPrefab.SetOrbitalTier(0);

                EasyTrailMisc trail = gameObject.AddComponent<EasyTrailMisc>();
                trail.TrailPos = gameObject.transform.position;
                trail.TrailPos.x += 0.4f;
                trail.StartWidth = 0.4f;
                trail.EndWidth = 0;
                trail.LifeTime = 0.2f;
                trail.BaseColor = ExtendedColours.charmPink;
                trail.EndColor = ExtendedColours.pink;

                UnityEngine.Object.DontDestroyOnLoad(gameObject);
                FakePrefab.MarkAsFakePrefab(gameObject);
                gameObject.SetActive(false);
            }
        }
        private float timer;
        public override void Update()
        {
            if (this.m_extantOrbital != null)
            {
                if (timer > 0)
                {
                    timer -= BraveTime.DeltaTime;
                }
                else if (timer <= 0)
                {
                    if (Owner.PlayerHasActiveSynergy("Ultravioleter Guon Stone"))
                    {
                        timer = UnityEngine.Random.Range(0.1f, 0.5f);
                        m_extantOrbital.GetComponent<PlayerOrbital>().orbitRadius = UnityEngine.Random.Range(1f, 5f);
                    }
                    else
                    {
                        timer = UnityEngine.Random.Range(0.1f, 1f);
                        m_extantOrbital.GetComponent<PlayerOrbital>().orbitRadius = UnityEngine.Random.Range(2f, 6f);
                    }
                    if (Owner.IsInCombat && Owner.PlayerHasActiveSynergy("Xenochrome"))
                    {
                        if (UnityEngine.Random.value <= 0.5f)
                        {
                            GameObject obj = ProjSpawnHelper.SpawnProjectileTowardsPoint(xenochromePrefab.gameObject, m_extantOrbital.transform.position, Owner.transform.position);
                            Projectile component = obj.GetComponent<Projectile>();
                            if (component != null)
                            {
                                component.Owner = Owner;
                                component.Shooter = Owner.specRigidbody;
                                component.collidesWithPlayer = false;

                                component.baseData.damage *= Owner.stats.GetStatValue(PlayerStats.StatType.Damage);
                                component.baseData.range *= Owner.stats.GetStatValue(PlayerStats.StatType.RangeMultiplier);
                                component.baseData.force *= Owner.stats.GetStatValue(PlayerStats.StatType.KnockbackMultiplier);
                                component.baseData.speed *= Owner.stats.GetStatValue(PlayerStats.StatType.ProjectileSpeed);
                                Owner.DoPostProcessProjectile(component);
                            }
                        }
                    }
                }
                base.Update();
            }
        }
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
        }
        public override DebrisObject Drop(PlayerController player)
        {
            return base.Drop(player);
        }
        public override void OnDestroy()
        {
            base.OnDestroy();
        }
    }
}