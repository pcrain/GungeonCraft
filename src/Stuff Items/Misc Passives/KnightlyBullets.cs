using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Alexandria.ItemAPI;
using Dungeonator;
using Alexandria.Misc;

namespace NevernamedsItems
{
    public class KnightlyBullets : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Knightly Bullets";
            string resourceName = "NevernamedsItems/Resources/knightlybullets_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<KnightlyBullets>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Charthurian";
            string longDesc = "These high class slugs are reluctant to harm those higher than them in status, but they have no problem squashing the peasantry." + "\n\nFavoured by the mighty Ser Lammorack, famed Knight of the Octagonal Table, before his untimely demise...";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.C;
            item.SetTag("bullet_modifier");
            item.CanBeDropped = true;
            KnightlyBulletsID = item.PickupObjectId;
        }
        public static int KnightlyBulletsID;
        private RoomHandler lastCheckedRoom;
        public override void Update()
        {
            if (Owner && Owner.CurrentRoom != null)
            {
                if (Owner.CurrentRoom != lastCheckedRoom)
                {
                    RoomHandler curRoom = Owner.CurrentRoom;
                    bool isInOrAdjacentToBossRoom = false;
                    if (curRoom.area.PrototypeRoomCategory == PrototypeDungeonRoom.RoomCategory.BOSS) isInOrAdjacentToBossRoom = true;
                    else
                    {
                        for (int i = 0; i < curRoom.connectedRooms.Count; i++)
                        {
                            if (curRoom.connectedRooms[i].area.PrototypeRoomCategory == PrototypeDungeonRoom.RoomCategory.BOSS)
                            {
                                isInOrAdjacentToBossRoom = true;
                            }
                        }
                    }
                    if (isInOrAdjacentToBossRoom && this.CanBeDropped)
                    {
                        this.CanBeDropped = false;
                    }
                    else if (!this.CanBeDropped)
                    {
                        this.CanBeDropped = true;
                    }
                    lastCheckedRoom = Owner.CurrentRoom;
                }
            }
            base.Update();
        }

        private void PostProcess(Projectile bullet, float th)
        {
            if (bullet && bullet.ProjectilePlayerOwner())
            {
                if (bullet.ProjectilePlayerOwner().CurrentRoom.area.PrototypeRoomCategory == PrototypeDungeonRoom.RoomCategory.BOSS)
                {
                    bullet.baseData.damage *= 0.8f;
                }
                else
                {
                    bullet.baseData.damage *= 1.3f;
                    bullet.RuntimeUpdateScale(1.2f);
                }
            }
        }
        private void PostProcessBem(BeamController bem)
        {
            if (bem && bem.GetComponent<Projectile>())
            {
                PostProcess(bem.GetComponent<Projectile>(), 0);
            }
        }
        public override void Pickup(PlayerController player)
        {
            player.PostProcessProjectile += PostProcess;
            player.PostProcessBeam += PostProcessBem;
            base.Pickup(player);
        }
        public override DebrisObject Drop(PlayerController player)
        {
            player.PostProcessProjectile -= PostProcess;
            player.PostProcessBeam -= PostProcessBem;

            return base.Drop(player);
        }
        public override void OnDestroy()
        {
            if (Owner)
            {
                Owner.PostProcessProjectile -= PostProcess;
                Owner.PostProcessBeam -= PostProcessBem;
            }
            base.OnDestroy();
        }
    }
}

