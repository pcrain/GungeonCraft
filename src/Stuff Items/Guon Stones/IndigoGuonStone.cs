using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Collections;
using System.Reflection;
using MonoMod.RuntimeDetour;
using Dungeonator;
using Alexandria.Misc;
using Alexandria.ItemAPI;

namespace NevernamedsItems
{
    class IndigoGuonStone : AdvancedPlayerOrbitalItem
    {

        public static PlayerOrbital orbitalPrefab;
        public static PlayerOrbital upgradeOrbitalPrefab;
        public static void Init()
        {
            string itemName = "Indigo Guon Stone"; //The name of the item
            string resourceName = "NevernamedsItems/Resources/GuonStones/indigoguon_icon"; //Refers to an embedded png in the project. Make sure to embed your resources!

            GameObject obj = new GameObject();

            var item = obj.AddComponent<IndigoGuonStone>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            string shortDesc = "Close To Your Heart";
            string longDesc = "Orbits close, offering bullet banishing protection." + "\n\nThe blood stone of an ancient frost giant, hardened by time and cold.";

            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.A;

            BuildPrefab();
            item.OrbitalPrefab = orbitalPrefab;
            BuildSynergyPrefab();

            item.AddToSubShop(ItemBuilder.ShopType.OldRed);
            item.SetTag("guon_stone");

            item.HasAdvancedUpgradeSynergy = true;
            item.AdvancedUpgradeSynergy = "Indigoer Guon Stone";
            item.AdvancedUpgradeOrbitalPrefab = IndigoGuonStone.upgradeOrbitalPrefab.gameObject;
        }
        public static void BuildPrefab()
        {
            if (IndigoGuonStone.orbitalPrefab != null) return;
            GameObject prefab = SpriteBuilder.SpriteFromResource("NevernamedsItems/Resources/GuonStones/indigoguon_ingame");
            prefab.name = "Indigo Guon Orbital";
            //prefab.GetComponent<tk2dSprite>().GetCurrentSpriteDef().ConstructOffsetsFromAnchor(tk2dBaseSprite.Anchor.MiddleCenter, prefab.GetComponent<tk2dSprite>().GetCurrentSpriteDef().position3);
            var body = prefab.GetComponent<tk2dSprite>().SetUpSpeculativeRigidbody(IntVector2.Zero, new IntVector2(5, 9));
            

            body.CollideWithTileMap = false;
            body.CollideWithOthers = true;
            body.PrimaryPixelCollider.CollisionLayer = CollisionLayer.EnemyBulletBlocker;

            orbitalPrefab = prefab.AddComponent<PlayerOrbital>();
            orbitalPrefab.motionStyle = PlayerOrbital.OrbitalMotionStyle.ORBIT_PLAYER_ALWAYS;
            orbitalPrefab.perfectOrbitalFactor = 10f;
            orbitalPrefab.shouldRotate = false;
            orbitalPrefab.orbitRadius = 1f;
            orbitalPrefab.orbitDegreesPerSecond = 100f;
            orbitalPrefab.SetOrbitalTier(0);

            GameObject.DontDestroyOnLoad(prefab);
            FakePrefab.MarkAsFakePrefab(prefab);
            prefab.SetActive(false);
        }
        public static void BuildSynergyPrefab()
        {
            bool flag = IndigoGuonStone.upgradeOrbitalPrefab == null;
            if (flag)
            {
                GameObject gameObject = SpriteBuilder.SpriteFromResource("NevernamedsItems/Resources/GuonStones/indigoguon_synergy", null);
                gameObject.name = "Indigo Guon Orbital Synergy Form";
                SpeculativeRigidbody speculativeRigidbody = gameObject.GetComponent<tk2dSprite>().SetUpSpeculativeRigidbody(IntVector2.Zero, new IntVector2(9, 13));
                //gameObject.GetComponent<tk2dSprite>().GetCurrentSpriteDef().ConstructOffsetsFromAnchor(tk2dBaseSprite.Anchor.MiddleCenter, gameObject.GetComponent<tk2dSprite>().GetCurrentSpriteDef().position3);
                IndigoGuonStone.upgradeOrbitalPrefab = gameObject.AddComponent<PlayerOrbital>();
                speculativeRigidbody.CollideWithTileMap = false;
                speculativeRigidbody.CollideWithOthers = true;
                speculativeRigidbody.PrimaryPixelCollider.CollisionLayer = CollisionLayer.EnemyBulletBlocker;
                IndigoGuonStone.upgradeOrbitalPrefab.shouldRotate = false;
                IndigoGuonStone.upgradeOrbitalPrefab.orbitRadius = 1f;
                IndigoGuonStone.upgradeOrbitalPrefab.perfectOrbitalFactor = 10f;
                IndigoGuonStone.upgradeOrbitalPrefab.orbitDegreesPerSecond = 100f;
                IndigoGuonStone.upgradeOrbitalPrefab.SetOrbitalTier(0);
                UnityEngine.Object.DontDestroyOnLoad(gameObject);
                FakePrefab.MarkAsFakePrefab(gameObject);
                gameObject.SetActive(false);
            }
        }
        private void OnGuonHitByBullet(SpeculativeRigidbody myRigidbody, PixelCollider myCollider, SpeculativeRigidbody other, PixelCollider otherCollider)
        {
            if (Owner && this.m_extantOrbital)
            {
                if (Owner.IsDodgeRolling)
                {
                    PhysicsEngine.SkipCollision = true;
                }
                else
                {
                    if (other.projectile && !(other.projectile.Owner is PlayerController))
                    {
                        float procChance = 0.35f;
                        if (Owner.PlayerHasActiveSynergy("Indigoer Guon Stone")) procChance = 0.6f;
                        if (UnityEngine.Random.value <= procChance)
                        {
                            EasyBlankType blankType = EasyBlankType.MINI;
                            if (Owner.PlayerHasActiveSynergy("Indigoer Guon Stone") && UnityEngine.Random.value <= 0.2) blankType = EasyBlankType.FULL;
                            Owner.DoEasyBlank(this.m_extantOrbital.transform.position, blankType);
                        }
                    }
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
}
