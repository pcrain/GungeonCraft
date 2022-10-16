using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ItemAPI;
using System.Collections;
using System.Reflection;
using MonoMod.RuntimeDetour;
using Dungeonator;
using SaveAPI;

namespace NevernamedsItems
{
    class SilverGuonStone : AdvancedPlayerOrbitalItem
    {

        public static PlayerOrbital orbitalPrefab;
        public static PlayerOrbital upgradeOrbitalPrefab;
        public static void Init()
        {
            string itemName = "Silver Guon Stone"; //The name of the item
            string resourceName = "NevernamedsItems/Resources/GuonStones/silverguon_icon"; //Refers to an embedded png in the project. Make sure to embed your resources!

            GameObject obj = new GameObject();

            var item = obj.AddComponent<SilverGuonStone>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            string shortDesc = "Relic From Beyond";
            string longDesc = "Forged from a lump of silvery metal that slipped through a tear in the curtain."+"\n\nCombats the Jammed.";

            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.B;

            BuildPrefab();
            item.OrbitalPrefab = orbitalPrefab;
            BuildSynergyPrefab();

            item.AddToSubShop(ItemBuilder.ShopType.Cursula);

            item.HasAdvancedUpgradeSynergy = true;
            item.AdvancedUpgradeSynergy = "Silverer Guon Stone";
            item.AdvancedUpgradeOrbitalPrefab = SilverGuonStone.upgradeOrbitalPrefab.gameObject;

            item.SetupUnlockOnCustomFlag(CustomDungeonFlags.ALLJAMMED_BEATEN_PROPER, true);
        }
        public static void BuildPrefab()
        {
            if (SilverGuonStone.orbitalPrefab != null) return;
            GameObject prefab = SpriteBuilder.SpriteFromResource("NevernamedsItems/Resources/GuonStones/silverguon_ingame");
            prefab.name = "Silver Guon Orbital";
            var body = prefab.GetComponent<tk2dSprite>().SetUpSpeculativeRigidbody(IntVector2.Zero, new IntVector2(7, 7));

            //prefab.GetComponent<tk2dSprite>().GetCurrentSpriteDef().ConstructOffsetsFromAnchor(tk2dBaseSprite.Anchor.MiddleCenter, body.GetComponent<tk2dSprite>().GetCurrentSpriteDef().position3);

            body.CollideWithTileMap = false;
            body.CollideWithOthers = true;
            body.PrimaryPixelCollider.CollisionLayer = CollisionLayer.EnemyBulletBlocker;

            orbitalPrefab = prefab.AddComponent<PlayerOrbital>();
            orbitalPrefab.motionStyle = PlayerOrbital.OrbitalMotionStyle.ORBIT_PLAYER_ALWAYS;
            orbitalPrefab.perfectOrbitalFactor = 0f;
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
            bool flag = SilverGuonStone.upgradeOrbitalPrefab == null;
            if (flag)
            {
                GameObject gameObject = SpriteBuilder.SpriteFromResource("NevernamedsItems/Resources/GuonStones/silverguon_synergy", null);
                gameObject.name = "Silver Guon Orbital Synergy Form";
                SpeculativeRigidbody speculativeRigidbody = gameObject.GetComponent<tk2dSprite>().SetUpSpeculativeRigidbody(IntVector2.Zero, new IntVector2(12, 12));
                //gameObject.GetComponent<tk2dSprite>().GetCurrentSpriteDef().ConstructOffsetsFromAnchor(tk2dBaseSprite.Anchor.MiddleCenter, gameObject.GetComponent<tk2dSprite>().GetCurrentSpriteDef().position3);
                SilverGuonStone.upgradeOrbitalPrefab = gameObject.AddComponent<PlayerOrbital>();
                speculativeRigidbody.CollideWithTileMap = false;
                speculativeRigidbody.CollideWithOthers = true;
                speculativeRigidbody.PrimaryPixelCollider.CollisionLayer = CollisionLayer.EnemyBulletBlocker;
                SilverGuonStone.upgradeOrbitalPrefab.shouldRotate = false;
                SilverGuonStone.upgradeOrbitalPrefab.orbitRadius = 2.5f;
                SilverGuonStone.upgradeOrbitalPrefab.perfectOrbitalFactor = 10f;
                SilverGuonStone.upgradeOrbitalPrefab.orbitDegreesPerSecond = 120f;
                SilverGuonStone.upgradeOrbitalPrefab.SetOrbitalTier(0);
                UnityEngine.Object.DontDestroyOnLoad(gameObject);
                FakePrefab.MarkAsFakePrefab(gameObject);
                gameObject.SetActive(false);
            }
        }
        private void OnGuonHitByBullet(SpeculativeRigidbody myRigidbody, PixelCollider myCollider, SpeculativeRigidbody other, PixelCollider otherCollider)
        {
            if (Owner && this.m_extantOrbital)
            {
                if (other.projectile && other.projectile.Owner && !(other.projectile.Owner is PlayerController))
                {
                    float maxDMGMult = 5f;
                    if (Owner.PlayerHasActiveSynergy("Silverer Guon Stone")) maxDMGMult = 8f;
                    if (other.projectile.IsBlackBullet && storedDamageMult < maxDMGMult) storedDamageMult += 0.5f;

                    if (Owner.PlayerHasActiveSynergy("Turn Gundead") && other.projectile.Owner.aiActor && other.projectile.Owner.aiActor.IsBlackPhantom)
                    {
                        if (other.projectile.Owner.healthHaver && !other.projectile.Owner.healthHaver.IsBoss && UnityEngine.Random.value <= 0.1f)
                        { other.projectile.Owner.aiActor.UnbecomeBlackPhantom(); }
                        else if (other.projectile.Owner.healthHaver && other.projectile.Owner.healthHaver.IsBoss && UnityEngine.Random.value <= 0.05f)
                        { other.projectile.Owner.aiActor.UnbecomeBlackPhantom(); }
                    }
                }
            }
        }
        float storedDamageMult = 1f;
        public override void OnOrbitalCreated(GameObject orbital)
        {
            if (orbital.GetComponent<SpeculativeRigidbody>())
            {
                SpeculativeRigidbody specRigidbody = orbital.GetComponent<SpeculativeRigidbody>();
                specRigidbody.OnPreRigidbodyCollision += this.OnGuonHitByBullet;
            }
            base.OnOrbitalCreated(orbital);
        }
        private void PostProcessBeam(BeamController beam)
        {
            if (beam)
            {
                Projectile projectile = beam.projectile;
                if (projectile)
                {
                    this.PostProcessProjectile(projectile, 1f);
                }
            }
        }
        private void PostProcessProjectile(Projectile bullet, float meh)
        {
            float jammedDMGMult = 1.15f;
            if (Owner.PlayerHasActiveSynergy("Silverer Guon Stone")) jammedDMGMult = 1.3f;
            bullet.BlackPhantomDamageMultiplier *= jammedDMGMult;
            if (storedDamageMult > 1)
            {
                bullet.baseData.damage *= storedDamageMult;
                bullet.AdjustPlayerProjectileTint(Color.red, 1, 0f);
                storedDamageMult = 1;
            }
        }
        public override void Pickup(PlayerController player)
        {
            player.PostProcessProjectile += this.PostProcessProjectile;
            player.PostProcessBeam += this.PostProcessBeam;
            base.Pickup(player);
        }
        public override DebrisObject Drop(PlayerController player)
        {
            player.PostProcessProjectile -= this.PostProcessProjectile;
            player.PostProcessBeam -= this.PostProcessBeam;
            return base.Drop(player);
        }
        public override void OnDestroy()
        {
            if (Owner)
            {
                Owner.PostProcessBeam -= this.PostProcessBeam;
                Owner.PostProcessProjectile -= this.PostProcessProjectile;
            }
            base.OnDestroy();
        }
    }
}
