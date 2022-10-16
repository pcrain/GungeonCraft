using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ItemAPI;
using UnityEngine;
using Gungeon;

namespace NevernamedsItems
{
    class BulletComponentLister : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Bullet Component Lister";
            string resourceName = "NevernamedsItems/Resources/workinprogress_icon";
            GameObject obj = new GameObject();
            var item = obj.AddComponent<BulletComponentLister>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);


            string shortDesc = "wip";
            string longDesc = "Lists the components present in fired projectiles.";


            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            item.quality = PickupObject.ItemQuality.EXCLUDED;
            item.CanBeDropped = true;
            item.CanBeSold = true;
        }
        public void onFired(Projectile bullet, float eventchancescaler)
        {
            foreach (Component component in bullet.GetComponentsInChildren<Component>())
            {
                ETGModConsole.Log(component.GetType().ToString());
                if (component is TrailController)
                {
                    TrailController cont = (component as TrailController);
                    ETGModConsole.Log($"<color=#ff0000ff>    usesStartAnimation: </color>{cont.usesStartAnimation}");
                    ETGModConsole.Log($"<color=#ff0000ff>    usesAnimation: </color>{cont.usesAnimation}");
                    ETGModConsole.Log($"<color=#ff0000ff>    usesCascadeTimer: </color>{cont.usesCascadeTimer}");
                    ETGModConsole.Log($"<color=#ff0000ff>    cascadeTimer: </color>{cont.cascadeTimer}");
                    ETGModConsole.Log($"<color=#ff0000ff>    usesSoftMaxLength: </color>{cont.usesSoftMaxLength}");
                    ETGModConsole.Log($"<color=#ff0000ff>    softMaxLength: </color>{cont.softMaxLength}");
                    ETGModConsole.Log($"<color=#ff0000ff>    usesGlobalTimer: </color>{cont.usesGlobalTimer}");
                    ETGModConsole.Log($"<color=#ff0000ff>    globalTimer: </color>{cont.globalTimer}");
                    ETGModConsole.Log($"<color=#ff0000ff>    destroyOnEmpty: </color>{cont.destroyOnEmpty}");
                }
            }
            ETGModConsole.Log("<color=#ff0000ff>---------------------------------</color>");
        }
        public override void Pickup(PlayerController player)
        {
            player.PostProcessProjectile += this.onFired;
            base.Pickup(player);
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject result = base.Drop(player);
            player.PostProcessProjectile -= this.onFired;
            return result;
        }

    }
    class ObjectComponentLister : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Object Component Lister";
            string resourceName = "NevernamedsItems/Resources/workinprogress_icon";
            GameObject obj = new GameObject();
            var item = obj.AddComponent<ObjectComponentLister>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);


            string shortDesc = "wip";
            string longDesc = "Lists the components present in objects that you shoot.";


            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            item.quality = PickupObject.ItemQuality.EXCLUDED;
            item.CanBeDropped = true;
            item.CanBeSold = true;
        }
        public void onFired(Projectile bullet, float eventchancescaler)
        {
            bullet.specRigidbody.OnCollision += this.onHitEnemy;
        }
        public void onHitEnemy(CollisionData data)
        {
            if (data.OtherRigidbody && data.OtherRigidbody.gameObject)
            {
                ETGModConsole.Log("<color=#ff0000ff>Name: </color>" + data.OtherRigidbody.gameObject.name);
                foreach (Component component in data.OtherRigidbody.gameObject.GetComponentsInChildren<Component>())
                {
                    if (component != null)
                    {

                        ETGModConsole.Log(component.GetType().ToString());
                        if (component is HealthHaver)
                        {
                            ETGModConsole.Log("<color=#ff0000ff>Max HP: </color>" + (component as HealthHaver).GetMaxHealth());
                            ETGModConsole.Log("<color=#ff0000ff>BossBarType: </color>" + (component as HealthHaver).bossHealthBar);
                        }
                        if (component is AIActor)
                        {
                            ETGModConsole.Log("<color=#ff0000ff>Normal Enemy: </color>" + (component as AIActor).IsNormalEnemy);

                        }
                    }
                }
                ETGModConsole.Log("<color=#ff0000ff>---------------------------------</color>");
            }
        }
        public override void Pickup(PlayerController player)
        {
            player.PostProcessProjectile += this.onFired;
            base.Pickup(player);
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject result = base.Drop(player);
            player.PostProcessProjectile -= this.onFired;
            return result;
        }

    }
}
