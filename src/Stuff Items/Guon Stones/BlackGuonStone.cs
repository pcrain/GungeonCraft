using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Alexandria.ItemAPI;
using System.Collections;
using System.Reflection;
using MonoMod.RuntimeDetour;
using Alexandria.Misc;

namespace NevernamedsItems
{
    class BlackGuonStone : AdvancedPlayerOrbitalItem
    {

        public static PlayerOrbital orbitalPrefab;
        public static PlayerOrbital upgradeOrbitalPrefab;
        public static void Init()
        {
            string itemName = "Black Guon Stone";
            string resourceName = "NevernamedsItems/Resources/GuonStones/blackguonstone_icon";

            GameObject obj = new GameObject();

            var item = obj.AddComponent<BlackGuonStone>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            string shortDesc = "No Bullets Can Escape";
            string longDesc = "Chance to crush enemy bullets into a single point of infinite density."+"\n\nThis ancient stone, though appearing arcane, is entirely based on scientific principles. Batteries are included.";

            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.B;

            item.OrbitalPrefab = BuildPrefab();

            item.SetTag("guon_stone");

            item.HasAdvancedUpgradeSynergy = true;
            item.AdvancedUpgradeSynergy = "Blacker Guon Stone";
            item.AdvancedUpgradeOrbitalPrefab = BuildSynergyPrefab().gameObject;
        }
        public static PlayerOrbital BuildPrefab()
        {
            GameObject prefab = SpriteBuilder.SpriteFromResource("NevernamedsItems/Resources/GuonStones/blackguonstone_orbital");
            prefab.name = "Black Guon Orbital";
            var body = prefab.GetComponent<tk2dSprite>().SetUpSpeculativeRigidbody(IntVector2.Zero, new IntVector2(6, 9));
            body.CollideWithTileMap = false;
            body.CollideWithOthers = true;
            body.PrimaryPixelCollider.CollisionLayer = CollisionLayer.EnemyBulletBlocker;

            orbitalPrefab = prefab.AddComponent<PlayerOrbital>();
            orbitalPrefab.motionStyle = PlayerOrbital.OrbitalMotionStyle.ORBIT_PLAYER_ALWAYS;
            orbitalPrefab.shouldRotate = false;
            orbitalPrefab.orbitRadius = 2.5f;
            orbitalPrefab.orbitDegreesPerSecond = 120f;
            orbitalPrefab.SetOrbitalTier(0);

            prefab.MakeFakePrefab();
            return orbitalPrefab;
        }
        public static PlayerOrbital BuildSynergyPrefab()
        {
            GameObject gameObject = SpriteBuilder.SpriteFromResource("NevernamedsItems/Resources/GuonStones/blackguonstone_synergy", null);
            gameObject.name = "Black Guon Orbital Synergy Form";
            SpeculativeRigidbody speculativeRigidbody = gameObject.GetComponent<tk2dSprite>().SetUpSpeculativeRigidbody(IntVector2.Zero, new IntVector2(9, 13));
            upgradeOrbitalPrefab = gameObject.AddComponent<PlayerOrbital>();
            speculativeRigidbody.CollideWithTileMap = false;
            speculativeRigidbody.CollideWithOthers = true;
            speculativeRigidbody.PrimaryPixelCollider.CollisionLayer = CollisionLayer.EnemyBulletBlocker;
            upgradeOrbitalPrefab.shouldRotate = false;
            upgradeOrbitalPrefab.orbitRadius = 2.5f;
            upgradeOrbitalPrefab.orbitDegreesPerSecond = 120f;
            upgradeOrbitalPrefab.perfectOrbitalFactor = 10f;
            upgradeOrbitalPrefab.SetOrbitalTier(0);

            gameObject.MakeFakePrefab();
            return upgradeOrbitalPrefab;
        }
        public override void OnOrbitalCreated(GameObject orbital)
        {
            SpeculativeRigidbody orbBody = orbital.GetComponent<SpeculativeRigidbody>();
            if (orbBody) orbBody.OnPreRigidbodyCollision += this.OnGuonHit;
            base.OnOrbitalCreated(orbital);
        }
        private void OnGuonHit(SpeculativeRigidbody myRigidbody, PixelCollider myCollider, SpeculativeRigidbody other, PixelCollider otherCollider)
        {
            if (UnityEngine.Random.value <= 0.25f)
            {
                Projectile component = other.GetComponent<Projectile>();
                if (component != null && component.ProjectilePlayerOwner() == null && Owner)
                {
                    Projectile blackHolePrefab = ((Gun)PickupObjectDatabase.GetById(169)).DefaultModule.projectiles[0];
                    float angle = (myRigidbody.UnitCenter - Owner.specRigidbody.UnitCenter).ToAngle();
                    GameObject instanceObj = blackHolePrefab.InstantiateAndFireInDirection(myRigidbody.UnitCenter, angle);
                    Projectile instanceBlackHole = instanceObj.GetComponent<Projectile>();
                    if (instanceBlackHole != null)
                    {
                        instanceBlackHole.Owner = base.Owner;
                        instanceBlackHole.Shooter = base.Owner.specRigidbody;
                        if (!Owner.PlayerHasActiveSynergy("Blacker Guon Stone"))
                        {
                            instanceBlackHole.RuntimeUpdateScale(0.5f);
                            instanceBlackHole.baseData.damage *= 0.5f;
                        }
                        instanceBlackHole.baseData.damage *= Owner.stats.GetStatValue(PlayerStats.StatType.Damage);
                        Owner.DoPostProcessProjectile(instanceBlackHole);

                        SlowDownOverTimeModifier slowdown = instanceBlackHole.gameObject.GetOrAddComponent<SlowDownOverTimeModifier>();
                        slowdown.timeToSlowOver = 0.5f;
                        slowdown.timeTillKillAfterCompleteStop = Owner.PlayerHasActiveSynergy("Schwarzschild Radius") ? 1 : 0.5f;
                        slowdown.killAfterCompleteStop = true;
                        slowdown.extendTimeByRangeStat = false;
                    }
                }
            }
        }
    }
}