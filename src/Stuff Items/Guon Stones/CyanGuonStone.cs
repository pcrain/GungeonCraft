using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Collections;
using System.Reflection;
using Alexandria.Misc;
using MonoMod.RuntimeDetour;
using Alexandria.ItemAPI;

namespace NevernamedsItems
{
    class CyanGuonStone : AdvancedPlayerOrbitalItem
    {

        public static PlayerOrbital orbitalPrefab;
        public static PlayerOrbital upgradeOrbitalPrefab;
        public static void Init()
        {
            string itemName = "Cyan Guon Stone"; //The name of the item
            string resourceName = "NevernamedsItems/Resources/GuonStones/cyanguon_icon"; //Refers to an embedded png in the project. Make sure to embed your resources!

            GameObject obj = new GameObject();

            var item = obj.AddComponent<CyanGuonStone>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            string shortDesc = "Slow and Steady";
            string longDesc = "Targets enemies when you stand still."+"\n\nThis rock is inhabited by a powerful spirit of lethargy.";

            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.A;
            item.SetTag("guon_stone");

            BuildPrefab();
            item.OrbitalPrefab = orbitalPrefab;
            BuildSynergyPrefab();

            item.HasAdvancedUpgradeSynergy = true;
            item.AdvancedUpgradeSynergy = "Cyaner Guon Stone";
            item.AdvancedUpgradeOrbitalPrefab = CyanGuonStone.upgradeOrbitalPrefab.gameObject;

            Projectile projectile2 = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(86) as Gun).DefaultModule.projectiles[0]);
            projectile2.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile2.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile2);
            projectile2.baseData.speed *= 2f;
            projectile2.baseData.damage = 2;
            projectile2.baseData.range *= 2f;
            projectile2.SetProjectileSpriteRight("cyanguon_proj", 5, 5, true, tk2dBaseSprite.Anchor.MiddleCenter, 5, 5);
            cyanGuonProj = projectile2;
        }
        public static Projectile cyanGuonProj;
        public static void BuildPrefab()
        {
            if (CyanGuonStone.orbitalPrefab != null) return;
            GameObject prefab = SpriteBuilder.SpriteFromResource("NevernamedsItems/Resources/GuonStones/cyanguon_ingame");
            prefab.name = "Cyan Guon Orbital";
            var body = prefab.GetComponent<tk2dSprite>().SetUpSpeculativeRigidbody(IntVector2.Zero, new IntVector2(5, 9));
            //prefab.GetComponent<tk2dSprite>().GetCurrentSpriteDef().ConstructOffsetsFromAnchor(tk2dBaseSprite.Anchor.MiddleCenter, body.GetComponent<tk2dSprite>().GetCurrentSpriteDef().position3);
            body.CollideWithTileMap = false;
            body.CollideWithOthers = true;
            body.PrimaryPixelCollider.CollisionLayer = CollisionLayer.EnemyBulletBlocker;

            orbitalPrefab = prefab.AddComponent<PlayerOrbital>();
            orbitalPrefab.motionStyle = PlayerOrbital.OrbitalMotionStyle.ORBIT_PLAYER_ALWAYS;
            orbitalPrefab.shouldRotate = false;
            orbitalPrefab.orbitRadius = 2.5f;
            orbitalPrefab.orbitDegreesPerSecond = 120f;
            orbitalPrefab.SetOrbitalTier(0);

            GameObject.DontDestroyOnLoad(prefab);
            FakePrefab.MarkAsFakePrefab(prefab);
            prefab.SetActive(false);
        }
        public static void BuildSynergyPrefab()
        {
            bool flag = CyanGuonStone.upgradeOrbitalPrefab == null;
            if (flag)
            {
                GameObject gameObject = SpriteBuilder.SpriteFromResource("NevernamedsItems/Resources/GuonStones/cyanguon_synergy", null);
                gameObject.name = "Cyan Guon Orbital Synergy Form";
                SpeculativeRigidbody speculativeRigidbody = gameObject.GetComponent<tk2dSprite>().SetUpSpeculativeRigidbody(IntVector2.Zero, new IntVector2(9, 13));
                //gameObject.GetComponent<tk2dSprite>().GetCurrentSpriteDef().ConstructOffsetsFromAnchor(tk2dBaseSprite.Anchor.MiddleCenter, gameObject.GetComponent<tk2dSprite>().GetCurrentSpriteDef().position3);
                CyanGuonStone.upgradeOrbitalPrefab = gameObject.AddComponent<PlayerOrbital>();
                speculativeRigidbody.CollideWithTileMap = false;
                speculativeRigidbody.CollideWithOthers = true;
                speculativeRigidbody.PrimaryPixelCollider.CollisionLayer = CollisionLayer.EnemyBulletBlocker;
                CyanGuonStone.upgradeOrbitalPrefab.shouldRotate = false;
                CyanGuonStone.upgradeOrbitalPrefab.orbitRadius = 2.5f;
                CyanGuonStone.upgradeOrbitalPrefab.orbitDegreesPerSecond = 120f;
                CyanGuonStone.upgradeOrbitalPrefab.perfectOrbitalFactor = 10f;
                CyanGuonStone.upgradeOrbitalPrefab.SetOrbitalTier(0);
                UnityEngine.Object.DontDestroyOnLoad(gameObject);
                FakePrefab.MarkAsFakePrefab(gameObject);
                gameObject.SetActive(false);
            }
        }
        bool canFire = true;
        public override void Update()
        {
            if (this.m_extantOrbital != null)
            {
                if (Owner && Owner.IsInCombat && Owner.specRigidbody.Velocity == Vector2.zero && canFire)
                {
                    GameObject gameObject = SpawnManager.SpawnProjectile(cyanGuonProj.gameObject, this.m_extantOrbital.GetComponent<tk2dSprite>().WorldCenter, Quaternion.Euler(0f, 0f, this.m_extantOrbital.GetComponent<tk2dSprite>().WorldCenter.CalculateVectorBetween(this.m_extantOrbital.GetComponent<tk2dSprite>().WorldCenter.GetNearestEnemyToPosition(true, Dungeonator.RoomHandler.ActiveEnemyType.All, null, null).CenterPosition).ToAngle()), true);
                    Projectile component = gameObject.GetComponent<Projectile>();
                    if (component != null)
                    {
                        component.Owner = Owner;
                        component.Shooter = Owner.specRigidbody;
                        component.baseData.damage *= Owner.stats.GetStatValue(PlayerStats.StatType.Damage);
                        component.baseData.speed *= Owner.stats.GetStatValue(PlayerStats.StatType.ProjectileSpeed);
                        component.baseData.force *= Owner.stats.GetStatValue(PlayerStats.StatType.KnockbackMultiplier);
                        component.AdditionalScaleMultiplier *= Owner.stats.GetStatValue(PlayerStats.StatType.PlayerBulletScale);
                        component.UpdateSpeed();
                        Owner.DoPostProcessProjectile(component);
                    }
                    //component.ReAimBulletToNearestEnemy(100, 0);
                    canFire = false;
                    float cooldownTime = 0.35f;
                    if (Owner.PlayerHasActiveSynergy("Cyaner Guon Stone")) cooldownTime = 0.16f;
                    Invoke("resetFireCooldown", cooldownTime);
                }
            }
            base.Update();
        }
        private void resetFireCooldown() { canFire = true; }
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