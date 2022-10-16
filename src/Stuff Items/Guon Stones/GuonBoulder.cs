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
    class GuonBoulder : IounStoneOrbitalItem
    {

        public static Hook guonHook;
        public static bool speedUp = false;
        public static PlayerOrbital orbitalPrefab;

        //Call this method from the Start() method of your ETGModule extension
        public static void Init()
        {
            string itemName = "Guon Boulder"; //The name of the item
            string resourceName = "NevernamedsItems/Resources/guonboulder_icon"; //Refers to an embedded png in the project. Make sure to embed your resources!

            GameObject obj = new GameObject();

            var item = obj.AddComponent<GuonBoulder>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            string shortDesc = "Hefty Chunk";
            string longDesc = "An experiment to see just how huge a Guon Stone can be."+"\n\nAll the magic in this stone is solely dedicated just to keeping it aloft, and thus no further special effects are able to fit inside.";

            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.A;
            item.SetTag("guon_stone");

            BuildPrefab();
            item.OrbitalPrefab = orbitalPrefab;
            item.Identifier = IounStoneIdentifier.GENERIC;           
        }

        public static void BuildPrefab()
        {
            if (GuonBoulder.orbitalPrefab != null) return;
            GameObject prefab = SpriteBuilder.SpriteFromResource("NevernamedsItems/Resources/guonboulder_icon");
            prefab.name = "Guon Boulder Orbital";
            var body = prefab.GetComponent<tk2dSprite>().SetUpSpeculativeRigidbody(IntVector2.Zero, new IntVector2(30, 30));
            body.CollideWithTileMap = false;
            body.CollideWithOthers = true;
            body.PrimaryPixelCollider.CollisionLayer = CollisionLayer.EnemyBulletBlocker;

            orbitalPrefab = prefab.AddComponent<PlayerOrbital>();
            orbitalPrefab.motionStyle = PlayerOrbital.OrbitalMotionStyle.ORBIT_PLAYER_ALWAYS;
            orbitalPrefab.shouldRotate = false;
            orbitalPrefab.orbitRadius = 7f;
            orbitalPrefab.orbitDegreesPerSecond = 5f;
            orbitalPrefab.SetOrbitalTier(0);

            GameObject.DontDestroyOnLoad(prefab);
            FakePrefab.MarkAsFakePrefab(prefab);
            prefab.SetActive(false);
        }

        public override void Pickup(PlayerController player)
        {            
            guonHook = new Hook(
                typeof(PlayerOrbital).GetMethod("Initialize"),
                typeof(GuonBoulder).GetMethod("GuonInit")
            );

            base.Pickup(player);
        }

        public override DebrisObject Drop(PlayerController player)
        {
            guonHook.Dispose();
            speedUp = false;

            return base.Drop(player);
        }
        public override void OnDestroy()
        {
            guonHook.Dispose();
            speedUp = false;
            base.OnDestroy();
        }

        public static void GuonInit(Action<PlayerOrbital, PlayerController> orig, PlayerOrbital self, PlayerController player)
        {
            self.orbitDegreesPerSecond = speedUp ? 180f : 90f;
            orig(self, player);
        }       
    }
}
