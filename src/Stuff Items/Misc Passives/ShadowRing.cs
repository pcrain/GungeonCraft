using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dungeonator;
using UnityEngine;
using ItemAPI;
using SaveAPI;

namespace NevernamedsItems
{
    class ShadowRing : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Shadow Ring";
            string resourceName = "NevernamedsItems/Resources/NeoItemSprites/shadowring_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<ShadowRing>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Umbral";
            string longDesc = "Creates a shadow realm duplicate upon damage."+"\n\nForged of darkness and misery, mined from the shrunken dead heart of an emo teenager.";

            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            item.quality = PickupObject.ItemQuality.D;

            ID = item.PickupObjectId;
        }
        public static int ID;
        private void charmAll(PlayerController user)
        {
            GameObject shadowPrefab = PickupObjectDatabase.GetById(820).GetComponent<SpawnObjectPlayerItem>().objectToSpawn;
            GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(shadowPrefab, user.specRigidbody.UnitCenter, Quaternion.identity);
            tk2dBaseSprite component2 = gameObject.GetComponent<tk2dBaseSprite>();
            if (component2 != null)
            {
                component2.PlaceAtPositionByAnchor(user.specRigidbody.UnitCenter.ToVector3ZUp(component2.transform.position.z), tk2dBaseSprite.Anchor.MiddleCenter);
                if (component2.specRigidbody != null)
                {
                    component2.specRigidbody.RegisterGhostCollisionException(user.specRigidbody);
                }
            }
            KageBunshinController component3 = gameObject.GetComponent<KageBunshinController>();
            if (component3)
            {
                component3.InitializeOwner(user);
            }
            gameObject.transform.position = gameObject.transform.position.Quantize(0.0625f);
        }
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.OnReceivedDamage += this.charmAll;
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject debrisObject = base.Drop(player);
            player.OnReceivedDamage -= this.charmAll;
            return debrisObject;
        }
        public override void OnDestroy()
        {
            if (Owner) Owner.OnReceivedDamage -= this.charmAll;
            base.OnDestroy();
        }
    }
}
