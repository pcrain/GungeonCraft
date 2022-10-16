using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dungeonator;
using ItemAPI;
using UnityEngine;

namespace NevernamedsItems
{
    class HoneyPot : PlayerItem
    {
        public static void Init()
        {
            string itemName = "Honey Pot";
            string resourceName = "NevernamedsItems/Resources/honeypot_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<HoneyPot>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "A Little Something";
            string longDesc = "A handy, throwable pot of sticky honey." + "\n\nSome Gundead tell whispers of buzzing coming from the Oubliette...";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.Damage, 250);

            item.consumable = false;
            item.quality = ItemQuality.D;

            PotProjectile = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(86) as Gun).DefaultModule.projectiles[0]);
            PotProjectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(PotProjectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(PotProjectile);
            PotProjectile.baseData.damage *= 0f;
            PotProjectile.baseData.speed *= 0.5f;
            PotProjectile.baseData.range *= 0.5f;
            PotProjectile.collidesWithEnemies = false;
            PotProjectile.collidesWithPlayer = false;
            PotProjectile.collidesWithProjectiles = false;
            PotProjectile.pierceMinorBreakables = true;
            PierceProjModifier keepComponent = PotProjectile.gameObject.GetOrAddComponent<PierceProjModifier>();
            keepComponent.penetratesBreakables = true;
            keepComponent.penetration += 100;
            BounceProjModifier Bouncing = PotProjectile.gameObject.GetOrAddComponent<BounceProjModifier>();
            Bouncing.numberOfBounces += 100;

            PotProjectile.AnimateProjectile(new List<string> {
                "honeypotproj_1",
                "honeypotproj_2",
                "honeypotproj_3",
                "honeypotproj_4",
            }, 8, true, new List<IntVector2> {
                new IntVector2(19, 16), //1
                new IntVector2(16, 19), //2            
                new IntVector2(19, 16), //3
                new IntVector2(16, 19), //4
            }, AnimateBullet.ConstructListOfSameValues(false, 4), AnimateBullet.ConstructListOfSameValues(tk2dBaseSprite.Anchor.MiddleCenter, 4),
            AnimateBullet.ConstructListOfSameValues(true, 4),
            AnimateBullet.ConstructListOfSameValues(false, 4),
            AnimateBullet.ConstructListOfSameValues<Vector3?>(null, 4),
            new List<IntVector2?> {
                new IntVector2(14, 14), //1
                new IntVector2(14, 14), //2            
                new IntVector2(14, 14), //3
                new IntVector2(14, 14), //3
            },
            AnimateBullet.ConstructListOfSameValues<IntVector2?>(null, 4),
            AnimateBullet.ConstructListOfSameValues<Projectile>(null, 4));
        }
        private static Projectile PotProjectile;
        public override void DoEffect(PlayerController user)
        {
            GameObject gameObject = SpawnManager.SpawnProjectile(PotProjectile.gameObject, user.sprite.WorldCenter, Quaternion.Euler(0f, 0f, (user.CurrentGun == null) ? 0f : user.CurrentGun.CurrentAngle), true);
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
            if (projectile.Owner is PlayerController)
            {
                float radius = 7f;
                if ((projectile.Owner as PlayerController).PlayerHasActiveSynergy("Honey, I'm Home!")) radius = 10f;
                DeadlyDeadlyGoopManager goop = DeadlyDeadlyGoopManager.GetGoopManagerForGoopType(EasyGoopDefinitions.HoneyGoop);
                goop.TimedAddGoopCircle(projectile.specRigidbody.UnitCenter, radius, 0.75f, true);
            }
        }
    }
}
