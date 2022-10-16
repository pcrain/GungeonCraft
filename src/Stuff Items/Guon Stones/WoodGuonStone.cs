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
using Alexandria.Misc;

namespace NevernamedsItems
{
    class WoodGuonStone : IounStoneOrbitalItem
    {
        public static bool speedUp = false;
        public static PlayerOrbital orbitalPrefab;

        //Call this method from the Start() method of your ETGModule extension
        public static void Init()
        {
            string itemName = "Wood Guon Stone"; //The name of the item
            string resourceName = "NevernamedsItems/Resources/woodguon_icon"; //Refers to an embedded png in the project. Make sure to embed your resources!

            GameObject obj = new GameObject();

            var item = obj.AddComponent<WoodGuonStone>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            string shortDesc = "Fleeting Protection";
            string longDesc = "Provides brief protection, but destroys itself after a short time.";

            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.EXCLUDED;

            BuildPrefab();
            item.OrbitalPrefab = orbitalPrefab;
            item.Identifier = IounStoneIdentifier.GENERIC;

            item.CanBeDropped = false;
        }

        public static void BuildPrefab()
        {
            if (WoodGuonStone.orbitalPrefab != null) return;
            GameObject prefab = SpriteBuilder.SpriteFromResource("NevernamedsItems/Resources/woodguon_ingame");
            prefab.name = "Wood Guon Orbital";
            var body = prefab.GetComponent<tk2dSprite>().SetUpSpeculativeRigidbody(IntVector2.Zero, new IntVector2(7, 13));
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

        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            StartCoroutine(HandleDeathTimer());        
        }
        public bool shouldBeKilledNextOpportunity;
        public override void Update()
        {
            if (!Dungeon.IsGenerating && Owner && shouldBeKilledNextOpportunity) InstaKillGuon();
            base.Update();
        }
        private IEnumerator HandleDeathTimer()
        {
            float seconds = 20f;
            if (Owner.PlayerHasActiveSynergy("Mahoguny Guon Stones")) seconds *= 2;
            yield return new WaitForSeconds(seconds);
            shouldBeKilledNextOpportunity = true;
            InstaKillGuon();
            yield break;
        }
        public void InstaKillGuon()
        {
            UnityEngine.Object.Destroy(this.m_extantOrbital);
            Owner.RemoveItemFromInventory(this);
        }
    }
}
