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
    class MaroonGuonStone : AdvancedPlayerOrbitalItem
    {
        public static PlayerOrbital orbitalPrefab;
        public static PlayerOrbital upgradeOrbitalPrefab;
        public static void Init()
        {
            string itemName = "Maroon Guon Stone"; //The name of the item
            string resourceName = "NevernamedsItems/Resources/GuonStones/maroonguon_icon"; //Refers to an embedded png in the project. Make sure to embed your resources!

            GameObject obj = new GameObject();

            var item = obj.AddComponent<MaroonGuonStone>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            string shortDesc = "Unapologetically Offensive";
            string longDesc = "Has zero defensive capabilities, but empowers bullets that it's owner shoots through it."+"\n\nLost in the Gungeon by the infamous Jammomaster, many years ago...";

            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.A;

            BuildPrefab();
            item.OrbitalPrefab = orbitalPrefab;
            BuildSynergyPrefab();

            item.AddToSubShop(ItemBuilder.ShopType.Cursula);

            item.HasAdvancedUpgradeSynergy = true;
            item.AdvancedUpgradeSynergy = "Marooner Guon Stone";
            item.AdvancedUpgradeOrbitalPrefab = MaroonGuonStone.upgradeOrbitalPrefab.gameObject;

            item.SetupUnlockOnCustomFlag(CustomDungeonFlags.JAMMEDBULLETKIN_QUEST_REWARDED, true);
        }
        public static void BuildPrefab()
        {
            if (MaroonGuonStone.orbitalPrefab != null) return;
            GameObject prefab = SpriteBuilder.SpriteFromResource("NevernamedsItems/Resources/GuonStones/maroonguon_ingame");
            prefab.name = "Maroon Guon Orbital";
            var body = prefab.GetComponent<tk2dSprite>().SetUpSpeculativeRigidbody(IntVector2.Zero, new IntVector2(10, 16));

            //prefab.GetComponent<tk2dSprite>().GetCurrentSpriteDef().ConstructOffsetsFromAnchor(tk2dBaseSprite.Anchor.MiddleCenter, body.GetComponent<tk2dSprite>().GetCurrentSpriteDef().position3);

            body.CollideWithTileMap = false;
            body.CollideWithOthers = true;
            body.UpdateCollidersOnRotation = true;
            body.PrimaryPixelCollider.CollisionLayer = CollisionLayer.BulletBlocker;

            orbitalPrefab = prefab.AddComponent<PlayerOrbital>();
            orbitalPrefab.motionStyle = PlayerOrbital.OrbitalMotionStyle.ORBIT_PLAYER_ALWAYS;
            orbitalPrefab.perfectOrbitalFactor = 10f;
            orbitalPrefab.shouldRotate = true;
            orbitalPrefab.orbitRadius = 3.5f;
            orbitalPrefab.orbitDegreesPerSecond = 50f;
            orbitalPrefab.SetOrbitalTier(0);

            GameObject.DontDestroyOnLoad(prefab);
            FakePrefab.MarkAsFakePrefab(prefab);
            prefab.SetActive(false);
        }
        public static void BuildSynergyPrefab()
        {
            bool flag = MaroonGuonStone.upgradeOrbitalPrefab == null;
            if (flag)
            {
                GameObject gameObject = SpriteBuilder.SpriteFromResource("NevernamedsItems/Resources/GuonStones/maroonguon_synergy", null);
                gameObject.name = "Maroon Guon Orbital Synergy Form";
                SpeculativeRigidbody speculativeRigidbody = gameObject.GetComponent<tk2dSprite>().SetUpSpeculativeRigidbody(IntVector2.Zero, new IntVector2(13, 20));
                //gameObject.GetComponent<tk2dSprite>().GetCurrentSpriteDef().ConstructOffsetsFromAnchor(tk2dBaseSprite.Anchor.MiddleCenter, gameObject.GetComponent<tk2dSprite>().GetCurrentSpriteDef().position3);
                MaroonGuonStone.upgradeOrbitalPrefab = gameObject.AddComponent<PlayerOrbital>();
                speculativeRigidbody.CollideWithTileMap = false;
                speculativeRigidbody.CollideWithOthers = true;
                speculativeRigidbody.UpdateCollidersOnRotation = true;
                speculativeRigidbody.PrimaryPixelCollider.CollisionLayer = CollisionLayer.BulletBlocker;
                MaroonGuonStone.upgradeOrbitalPrefab.shouldRotate = true;
                MaroonGuonStone.upgradeOrbitalPrefab.orbitRadius = 3.5f;
                MaroonGuonStone.upgradeOrbitalPrefab.perfectOrbitalFactor = 10f;
                MaroonGuonStone.upgradeOrbitalPrefab.orbitDegreesPerSecond = 30f;
                MaroonGuonStone.upgradeOrbitalPrefab.SetOrbitalTier(0);
                UnityEngine.Object.DontDestroyOnLoad(gameObject);
                FakePrefab.MarkAsFakePrefab(gameObject);
                gameObject.SetActive(false);
            }
        }
        private void OnGuonHitByBullet(SpeculativeRigidbody myRigidbody, PixelCollider myCollider, SpeculativeRigidbody other, PixelCollider otherCollider)
        {
            if (Owner && this.m_extantOrbital)
            {
                if (other.projectile && other.projectile.Owner is PlayerController)
                {
                    if (!other.projectile.GetComponent<BuffedByMaroonGuonStone>())
                    {
                        float damageMult = 1.5f;
                        if (Owner.PlayerHasActiveSynergy("Marooner Guon Stone")) damageMult = 2f;
                        other.projectile.baseData.damage *= damageMult;                    
                        other.projectile.RuntimeUpdateScale(1.2f);
                        other.projectile.gameObject.AddComponent<BuffedByMaroonGuonStone>();
                        other.projectile.AdjustPlayerProjectileTint(ExtendedColours.maroon, 2);

                        ExtremelySimpleStatusEffectBulletBehaviour StatusEffect = other.projectile.gameObject.GetOrAddComponent<ExtremelySimpleStatusEffectBulletBehaviour>();
                        if (Owner.PlayerHasActiveSynergy("Toxic Core")) StatusEffect.usesPoisonEffect = true;
                        if (Owner.PlayerHasActiveSynergy("Charming Core")) StatusEffect.usesCharmEffect = true;
                        if (Owner.PlayerHasActiveSynergy("Burning Core")) StatusEffect.usesFireEffect = true;

                        if (Owner.PlayerHasActiveSynergy("Explosive Core"))
                        {
                            ExplosiveModifier Splodey = other.projectile.gameObject.GetOrAddComponent<ExplosiveModifier>();
                            Splodey.doExplosion = true;
                            Splodey.explosionData = StaticExplosionDatas.explosiveRoundsExplosion;
                        }
                        if (Owner.PlayerHasActiveSynergy("Hungry Core"))
                        { HungryProjectileModifier hungry = other.projectile.gameObject.GetOrAddComponent<HungryProjectileModifier>(); }
                        if (Owner.PlayerHasActiveSynergy("Smart Core"))
                        { HomingModifier homing = other.projectile.gameObject.GetOrAddComponent<HomingModifier>(); }
                    }
                    PhysicsEngine.SkipCollision = true;
                }
                else if (other.projectile && !(other.projectile.Owner is PlayerController))
                {
                    PhysicsEngine.SkipCollision = true;
                }
            }
        }
        public override void OnOrbitalCreated(GameObject orbital)
        {
            if (orbital.GetComponent<SpeculativeRigidbody>())
            {
                SpeculativeRigidbody specRigidbody = orbital.GetComponent<SpeculativeRigidbody>();
                specRigidbody.OnPreRigidbodyCollision += this.OnGuonHitByBullet;
            }
            base.OnOrbitalCreated(orbital);
        }
    }
    public class BuffedByMaroonGuonStone : MonoBehaviour { }
}

