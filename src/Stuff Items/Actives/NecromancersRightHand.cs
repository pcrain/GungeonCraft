using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using ItemAPI;
using Gungeon;

namespace NevernamedsItems
{
    class NecromancersRightHand : PlayerItem
    {
        public static void Init()
        {
            string itemName = "Necromancer's Right Hand";
            string resourceName = "NevernamedsItems/Resources/necromancersrighthand_icon";

            GameObject obj = new GameObject(itemName);

            var item = obj.AddComponent<NecromancersRightHand>();

            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            string shortDesc = "Spacebar To Necromance";
            string longDesc = "The severed hand of an ancient purple necromancer." + "\n\nRaise an army of the dead!";

            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.Damage, 210);

            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.Curse, 1f, StatModifier.ModifyMethod.ADDITIVE);

            item.consumable = false;
            item.quality = ItemQuality.D;
            item.AddToSubShop(ItemBuilder.ShopType.Cursula);
            Game.Items.Rename("nn:necromancer's_right_hand", "nn:necromancers_right_hand");
        }
        public override bool CanBeUsed(PlayerController user)
        {
            if (user != null && user.CurrentRoom != null && user.CurrentRoom.IsSealed) return true;
            else return false;
        }
        public override void DoEffect(PlayerController user)
        {
            AkSoundEngine.PostEvent("Play_OBJ_dead_again_01", base.gameObject);
            for (int i = 0; i < StaticReferenceManager.AllCorpses.Count; i++)
            {
                GameObject gameObject = StaticReferenceManager.AllCorpses[i];
                if (gameObject && gameObject.GetComponent<tk2dBaseSprite>() && gameObject.transform.position.GetAbsoluteRoom() == user.CurrentRoom && gameObject.transform.position.GetAbsoluteRoom().IsSealed)
                {
                    Vector2 worldCenter = gameObject.GetComponent<tk2dBaseSprite>().WorldCenter;

                    string guid = "249db525a9464e5282d02162c88e0357";
                    if (user.PlayerHasActiveSynergy("Roll Dem Bones") && UnityEngine.Random.value <= 0.25f) guid = "336190e29e8a4f75ab7486595b700d4a";
                    AIActor Spent = CompanionisedEnemyUtility.SpawnCompanionisedEnemy(user, guid, worldCenter.ToIntVector2(), true, ExtendedColours.purple, 5, 2, false, true);
                    if (Spent.GetComponent<SpawnEnemyOnDeath>()) UnityEngine.Object.Destroy(Spent.GetComponent<SpawnEnemyOnDeath>());
                    if (Spent.CorpseObject != null) Spent.CorpseObject = null;

                    if (guid == "249db525a9464e5282d02162c88e0357")
                    {
                        Spent.OverrideHitEnemies = true;
                        Spent.CollisionDamage = 1f;
                        Spent.CollisionDamageTypes |= CoreDamageTypes.Electric;
                    }

                    if (user.PlayerHasActiveSynergy("The Sprinting Dead")) Spent.MovementSpeed *= 2;

                    UnityEngine.Object.Destroy(gameObject.gameObject);
                }
            }
        }
    }
}
