using System;
using System.Collections.Generic;
using System.Linq;
using Gungeon;
using System.Text;

using UnityEngine;
using ItemAPI;
using Dungeonator;

namespace NevernamedsItems
{
    public class FlameChamber : PassiveItem
    {
        public static void Init()
        {
            //The name of the item
            string itemName = "Flame Chamber";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it
            string resourceName = "NevernamedsItems/Resources/NeoItemSprites/flamechamber_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a PassiveItem component to the object
            var item = obj.AddComponent<FlameChamber>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Hotshot";
            string longDesc = "Reloading on an empty clip ignites nearby enemies!." + "\n\nThis artefact seems strangely familiar to you, but you've never seen anything like it... yet.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //Do this after ItemBuilder.AddSpriteToObject!
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Adds the actual passive effect to the item

            //Set the rarity of the item
            item.quality = PickupObject.ItemQuality.D;

            item.AddToSubShop(ItemBuilder.ShopType.Cursula);

            //LIST OF SYNERGIES
            // 'Burning With Passion' - Flamechamber + Charmed Bow, Charmed Horn, or Charming Rounds --> Item spawns charm creep instead.
            List<string> mandatorySynergyItems = new List<string>() { "nn:flame_chamber" };
            List<string> optionalSynergyItems = new List<string>() { "charmed_bow", "charm_horn", "charming_rounds" };
            CustomSynergies.Add("Burning With Passion", mandatorySynergyItems, optionalSynergyItems);

            ID = item.PickupObjectId;
        }
        public static int ID;
        private void HandleGunReloaded(PlayerController player, Gun playerGun)
        {
            if (playerGun.ClipShotsRemaining == 0)
            {
                LightEnemiesInRadiusOnFire(player);
                if (player.PlayerHasActiveSynergy("Burning With Passion"))
                {
                    DeadlyDeadlyGoopManager.GetGoopManagerForGoopType(EasyGoopDefinitions.CharmGoopDef).TimedAddGoopCircle(Owner.specRigidbody.UnitCenter, 3.75f, 0.75f, false);
                }
            }
        }
        private void LightEnemiesInRadiusOnFire(PlayerController user)
        {
                float effectRadius = 5f;
            if (user.PlayerHasActiveSynergy("Pyromaniac")) effectRadius *= 2;       
            List<AIActor> activeEnemies = user.CurrentRoom.GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
            if (activeEnemies != null)
            {
                for (int i = 0; i < activeEnemies.Count; i++)
                {
                    AIActor aiactor = activeEnemies[i];
                    if (aiactor.IsNormalEnemy)
                    {
                        float num = Vector2.Distance(user.CenterPosition, aiactor.CenterPosition);
                        if (num <= effectRadius)
                        {
                            ApplyDirectStatusEffects.ApplyDirectFire(aiactor, 10f, StaticStatusEffects.hotLeadEffect.DamagePerSecondToEnemies, StaticStatusEffects.hotLeadEffect.TintColor, StaticStatusEffects.hotLeadEffect.DeathTintColor, EffectResistanceType.Fire, "Fire", true, true);
                        }
                    }
                }
            }
        }
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.OnReloadedGun += this.HandleGunReloaded;
        }
        public override DebrisObject Drop(PlayerController player)
        {
            player.OnReloadedGun -= this.HandleGunReloaded;
            DebrisObject debrisObject = base.Drop(player);
            return debrisObject;
        }
        public override void OnDestroy()
        {
            if (Owner) Owner.OnReloadedGun -= this.HandleGunReloaded;
            base.OnDestroy();
        }
    }

}