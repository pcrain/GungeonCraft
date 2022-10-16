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
    class KaleidoscopicGuonStone : AdvancedPlayerOrbitalItem
    {
        public static PlayerOrbital orbitalPrefab;
        public static PlayerOrbital upgradeOrbitalPrefab;
        public static void Init()
        {
            string itemName = "Kaleidoscopic Guon Stone"; //The name of the item
            string resourceName = "NevernamedsItems/Resources/GuonStones/kaleidoscopicguon_icon"; //Refers to an embedded png in the project. Make sure to embed your resources!

            GameObject obj = new GameObject();

            var item = obj.AddComponent<KaleidoscopicGuonStone>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            string shortDesc = "Twisted!";
            string longDesc = "Spirals in beautiful patterns. Capable of twisting the fabric of bullet-based matter to leave you with more bullet than you started with.";

            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.S;

            BuildPrefab();
            item.OrbitalPrefab = orbitalPrefab;
            BuildSynergyPrefab();

            item.AddToSubShop(ItemBuilder.ShopType.Cursula);

            item.HasAdvancedUpgradeSynergy = true;
            item.AdvancedUpgradeSynergy = "Kaleidoscopicer Guon Stone";
            item.AdvancedUpgradeOrbitalPrefab = KaleidoscopicGuonStone.upgradeOrbitalPrefab.gameObject;
        }
        public static void BuildPrefab()
        {
            if (KaleidoscopicGuonStone.orbitalPrefab != null) return;
            GameObject prefab = SpriteBuilder.SpriteFromResource("NevernamedsItems/Resources/GuonStones/kaleidoscopicguon_icon");
            prefab.name = "Kaleidoscopic Guon Orbital";
            var body = prefab.GetComponent<tk2dSprite>().SetUpSpeculativeRigidbody(IntVector2.Zero, new IntVector2(14, 14));

            //prefab.GetComponent<tk2dSprite>().GetCurrentSpriteDef().ConstructOffsetsFromAnchor(tk2dBaseSprite.Anchor.MiddleCenter, body.GetComponent<tk2dSprite>().GetCurrentSpriteDef().position3);

            body.CollideWithTileMap = false;
            body.CollideWithOthers = true;
            body.UpdateCollidersOnRotation = true;
            body.PrimaryPixelCollider.CollisionLayer = CollisionLayer.BulletBlocker;

            orbitalPrefab = prefab.AddComponent<PlayerOrbital>();
            orbitalPrefab.motionStyle = PlayerOrbital.OrbitalMotionStyle.ORBIT_PLAYER_ALWAYS;
            orbitalPrefab.perfectOrbitalFactor = 10f;
            orbitalPrefab.shouldRotate = false;
            orbitalPrefab.orbitRadius = 3.5f;
            orbitalPrefab.orbitDegreesPerSecond = 50f;
            orbitalPrefab.SetOrbitalTier(0);

            GameObject.DontDestroyOnLoad(prefab);
            FakePrefab.MarkAsFakePrefab(prefab);
            prefab.SetActive(false);
        }
        public static void BuildSynergyPrefab()
        {
            bool flag = KaleidoscopicGuonStone.upgradeOrbitalPrefab == null;
            if (flag)
            {
                GameObject gameObject = SpriteBuilder.SpriteFromResource("NevernamedsItems/Resources/GuonStones/kaleidoscopicguon_synergy", null);
                gameObject.name = "Kaleidoscopic Guon Orbital Synergy Form";
                SpeculativeRigidbody speculativeRigidbody = gameObject.GetComponent<tk2dSprite>().SetUpSpeculativeRigidbody(IntVector2.Zero, new IntVector2(20, 20));
                //gameObject.GetComponent<tk2dSprite>().GetCurrentSpriteDef().ConstructOffsetsFromAnchor(tk2dBaseSprite.Anchor.MiddleCenter, gameObject.GetComponent<tk2dSprite>().GetCurrentSpriteDef().position3);
                KaleidoscopicGuonStone.upgradeOrbitalPrefab = gameObject.AddComponent<PlayerOrbital>();
                speculativeRigidbody.CollideWithTileMap = false;
                speculativeRigidbody.CollideWithOthers = true;
                speculativeRigidbody.UpdateCollidersOnRotation = true;
                speculativeRigidbody.PrimaryPixelCollider.CollisionLayer = CollisionLayer.BulletBlocker;
                KaleidoscopicGuonStone.upgradeOrbitalPrefab.shouldRotate = false;
                KaleidoscopicGuonStone.upgradeOrbitalPrefab.orbitRadius = 3.5f;
                KaleidoscopicGuonStone.upgradeOrbitalPrefab.perfectOrbitalFactor = 10f;
                KaleidoscopicGuonStone.upgradeOrbitalPrefab.orbitDegreesPerSecond = 30f;
                KaleidoscopicGuonStone.upgradeOrbitalPrefab.SetOrbitalTier(0);
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
                    if (other.projectile.gameObject.GetComponent<KaleidoscopicBullet>() == null)
                    {
                        float anglesOfSpread = 40;
                        if (Owner.PlayerHasActiveSynergy("Kaleidoscopicer Guon Stone")) anglesOfSpread = 20;
                        float ProjectileInterval = anglesOfSpread / ((float)6 - 1);
                        float currentAngle = other.projectile.Direction.ToAngle();
                        float startAngle = currentAngle + (anglesOfSpread * 0.5f);
                        int iteration = 0;
                        for (int i = 0; i < 6; i++)
                        {
                            float finalAngle = startAngle - (ProjectileInterval * iteration);

                            GameObject newBulletOBJ = FakePrefab.Clone(other.projectile.gameObject);
                            GameObject spawnedBulletOBJ = SpawnManager.SpawnProjectile(newBulletOBJ, other.projectile.LastPosition, Quaternion.Euler(0f, 0f, finalAngle), true);
                            Projectile component = spawnedBulletOBJ.GetComponent<Projectile>();
                            if (component != null)
                            {
                                component.Owner = Owner;
                                component.Shooter = Owner.specRigidbody;
                                if (!Owner.PlayerHasActiveSynergy(synergyNameForIndex[i]))
                                {
                                    component.baseData.damage *= 0.5f;
                                    component.RuntimeUpdateScale(0.9f);
                                }
                                else
                                {
                                    component.baseData.speed *= 1.2f;
                                    component.baseData.range *= 3f;
                                    BounceProjModifier bounce = component.gameObject.GetComponent<BounceProjModifier>();
                                    if (bounce)
                                    {
                                        bounce.numberOfBounces++;
                                    }
                                    else
                                    {
                                        component.gameObject.AddComponent<BounceProjModifier>();
                                    }

                                }
                                component.AdjustPlayerProjectileTint(indexToColors[i], 2);
                            }
                            spawnedBulletOBJ.AddComponent<KaleidoscopicBullet>();
                            iteration++;
                        }
                        UnityEngine.Object.Destroy(other.projectile.gameObject);
                    }
                    PhysicsEngine.SkipCollision = true;
                }
                else if (other.projectile && !(other.projectile.Owner is PlayerController))
                {
                    PhysicsEngine.SkipCollision = false;
                }
            }
        }
        private Dictionary<int, Color> indexToColors = new Dictionary<int, Color>()
        {
            {0, Color.red},
            {1, ExtendedColours.vibrantOrange},
            {2, ExtendedColours.honeyYellow},
            {3, Color.green},
            {4, Color.blue},
            {5, ExtendedColours.purple},
        };
        private Dictionary<int, string> synergyNameForIndex = new Dictionary<int, string>()
        {
            {0, "Reddy Steady"},
            {1, "Orange U Glad"},
            {2, "Yellow There"},
            {3, "Green Behind The Ears"},
            {4, "da ba dee da ba die"},
            {5, "Tomorrow, or just the end of time?"},
        };
        public override void OnOrbitalCreated(GameObject orbital)
        {
            if (orbital.GetComponent<SpeculativeRigidbody>())
            {
                SpeculativeRigidbody specRigidbody = orbital.GetComponent<SpeculativeRigidbody>();
                specRigidbody.OnPreRigidbodyCollision += this.OnGuonHitByBullet;
            }
            base.OnOrbitalCreated(orbital);
        }
        private class KaleidoscopicBullet : MonoBehaviour { }
    }
}

