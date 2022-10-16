using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Collections;
using System.Reflection;
using MonoMod.RuntimeDetour;
using Alexandria.ItemAPI;

namespace NevernamedsItems
{
    class GoldGuonStone : AdvancedPlayerOrbitalItem
    {
        public static void Init()
        {
            string name = "Gold Guon Stone";
            string resourcePath = "NevernamedsItems/Resources/goldguonstone_icon";
            GameObject gameObject = new GameObject();
            GoldGuonStone item = gameObject.AddComponent<GoldGuonStone>();
            ItemBuilder.AddSpriteToObject(name, resourcePath, gameObject);
            string shortDesc = "Greedy Rock";
            string longDesc = "This opulent stone will occasionally suck the casings right off of enemy bullets that make contact with it."+"\n\nDespite being illogical, bullets in the Gungeon are often fired casing and all for extra damage. That's 65% more bullet per bullet!";
            
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.SetTag("guon_stone");

            item.quality = PickupObject.ItemQuality.C;
            GoldGuonStone.BuildPrefab();
            item.OrbitalPrefab = GoldGuonStone.orbitalPrefab;
        }
        public static int cashSpawnedThisRoom;
        public static void BuildPrefab()
        {
            bool flag = GoldGuonStone.orbitalPrefab != null;
            if (!flag)
            {
                GameObject gameObject = SpriteBuilder.SpriteFromResource("NevernamedsItems/Resources/goldguonstone_ingame");
                gameObject.name = "Gold Guon Orbital";
                SpeculativeRigidbody speculativeRigidbody = gameObject.GetComponent<tk2dSprite>().SetUpSpeculativeRigidbody(IntVector2.Zero, new IntVector2(7, 13));
                speculativeRigidbody.CollideWithTileMap = false;
                speculativeRigidbody.CollideWithOthers = true;
                speculativeRigidbody.PrimaryPixelCollider.CollisionLayer = CollisionLayer.EnemyBulletBlocker;
                GoldGuonStone.orbitalPrefab = gameObject.AddComponent<PlayerOrbital>();
                GoldGuonStone.orbitalPrefab.motionStyle = PlayerOrbital.OrbitalMotionStyle.ORBIT_PLAYER_ALWAYS;
                GoldGuonStone.orbitalPrefab.shouldRotate = false;
                GoldGuonStone.orbitalPrefab.orbitRadius = 2.5f;
                GoldGuonStone.orbitalPrefab.SetOrbitalTier(0);
                GoldGuonStone.orbitalPrefab.orbitDegreesPerSecond = 120f;
                UnityEngine.Object.DontDestroyOnLoad(gameObject);
                FakePrefab.MarkAsFakePrefab(gameObject);
                gameObject.SetActive(false);
            }
        }
        public override void Pickup(PlayerController player)
        {
            player.OnEnteredCombat += this.resetCash;
            base.Pickup(player);
        }
        public override DebrisObject Drop(PlayerController player)
        {
            player.OnEnteredCombat -= this.resetCash;
            return base.Drop(player);
        }
        public override void OnDestroy()
        {
            if (Owner) Owner.OnEnteredCombat -= this.resetCash;
            base.OnDestroy();
        }
        private void resetCash()
        {
            cashSpawnedThisRoom = 0;
        }
        public override void OnOrbitalCreated(GameObject orbital)
        {
            SpeculativeRigidbody orbBody = orbital.GetComponent<SpeculativeRigidbody>();
            if (orbBody)
            {
                orbBody.specRigidbody.OnPreRigidbodyCollision += this.OnGuonHit;
            }
            base.OnOrbitalCreated(orbital);
        }
        private void OnGuonHit(SpeculativeRigidbody myRigidbody, PixelCollider myCollider, SpeculativeRigidbody other, PixelCollider otherCollider)
        {
            Projectile component = other.GetComponent<Projectile>();
            if (component != null && !(component.Owner is PlayerController))
            {
                if (UnityEngine.Random.value < 0.1f && cashSpawnedThisRoom < 20)
                {
                    LootEngine.SpawnItem(PickupObjectDatabase.GetById(68).gameObject, other.specRigidbody.UnitCenter, Vector2.zero, 1f, false, true, false);
                    cashSpawnedThisRoom += 1;
                }
            }
        }
        public static PlayerOrbital orbitalPrefab;       
    }
}