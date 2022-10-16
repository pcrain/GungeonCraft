using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dungeonator;
using ItemAPI;
using UnityEngine;

namespace NevernamedsItems
{
    class PortableHole : PlayerItem
    {
        //Call this method from the Start() method of your ETGModule extension class
        public static void Init()
        {
            string itemName = "Portable Hole";
            string resourceName = "NevernamedsItems/Resources/portablehole_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<PortableHole>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "The Hole Nine Yards";
            string longDesc = "Not only is this hole portable, it's almost bottomless. And depressed. I mean, you would be too if you didn't have a bottom.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.Damage, 600);

            item.consumable = false;
            item.quality = ItemQuality.B;

            HoleProjectile = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(86) as Gun).DefaultModule.projectiles[0]);
            HoleProjectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(HoleProjectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(HoleProjectile);
            HoleProjectile.baseData.damage *= 0f;
            HoleProjectile.baseData.speed *= 0.5f;
            HoleProjectile.baseData.range *= 0.5f;
            HoleProjectile.collidesWithEnemies = false;
            HoleProjectile.collidesWithPlayer = false;
            HoleProjectile.collidesWithProjectiles = false;
            HoleProjectile.pierceMinorBreakables = true;
            PierceProjModifier keepComponent = HoleProjectile.gameObject.GetOrAddComponent<PierceProjModifier>();
            keepComponent.penetratesBreakables = true;
            keepComponent.penetration += 100;
            BounceProjModifier Bouncing = HoleProjectile.gameObject.GetOrAddComponent<BounceProjModifier>();
            Bouncing.numberOfBounces += 100;

            HoleProjectile.SetProjectileSpriteRight("portablehole_projectile", 17, 17, false, tk2dBaseSprite.Anchor.MiddleCenter, 17, 17);
        }
        private static Projectile HoleProjectile;
        public override void DoEffect(PlayerController user)
        {
            GameObject gameObject = SpawnManager.SpawnProjectile(HoleProjectile.gameObject, user.sprite.WorldCenter, Quaternion.Euler(0f, 0f, (user.CurrentGun == null) ? 0f : user.CurrentGun.CurrentAngle), true);
            Projectile component = gameObject.GetComponent<Projectile>();
            if (component != null)
            {
                component.Owner = user;
                component.Shooter = user.specRigidbody;
                component.OnDestruction += DoHoleSpawn;
            }
        }
        private void DoHoleSpawn(Projectile projectile)
        {
            DeadlyDeadlyGoopManager goop = DeadlyDeadlyGoopManager.GetGoopManagerForGoopType(EasyGoopDefinitions.PitGoop);
            goop.TimedAddGoopCircle(projectile.specRigidbody.UnitCenter, 7f, 0.75f, true);
            SpecialGoopBehaviours specialBehav = goop.gameObject.AddComponent<SpecialGoopBehaviours>();
            specialBehav.ForcesEnemiesToFall = true;
        }
    }
}