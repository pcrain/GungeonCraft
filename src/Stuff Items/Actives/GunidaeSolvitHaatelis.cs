using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using ItemAPI;
using System.Collections;
using Dungeonator;
using SaveAPI;

namespace NevernamedsItems
{
    class GunidaeSolvitHaatelis : PlayerItem
    {
        public static void Init()
        {
            string itemName = "Gunidae solvit Haatelis";
            string resourceName = "NevernamedsItems/Resources/gunidaesolvithaatelis_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<GunidaeSolvitHaatelis>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "As It Is Written";
            string longDesc = "An excerpt from an ancient holy text of the Order."+"\n\nReading it has enough power to bend the motion of bullets to your will.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.Damage, 350);
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.Curse, 1f, StatModifier.ModifyMethod.ADDITIVE);
            item.consumable = false;
            item.quality = ItemQuality.B;
            item.SetupUnlockOnCustomFlag(CustomDungeonFlags.ALLJAMMED_BEATEN_ABBEY, true);
        }
        public override void DoEffect(PlayerController user)
        {
            List<AIActor> activeEnemies = user.CurrentRoom.GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
            if (activeEnemies != null)
            {
                for (int i = 0; i < activeEnemies.Count; i++)
                {
                    AIActor aiactor = activeEnemies[i];
                    if (aiactor.IsBlackPhantom)
                    {
                        aiactor.UnbecomeBlackPhantom();
                    }
                }
            }
            foreach (Projectile proj in StaticReferenceManager.AllProjectiles)
            {
                if (proj && proj.isActiveAndEnabled && !(proj.Owner is PlayerController))
                {
                    if (proj.IsBlackBullet)
                    {
                        proj.ReturnFromBlackBullet();
                    }
                }
            }
            Projectile.BaseEnemyBulletSpeedMultiplier *= 0.5f;
            StartCoroutine(ItemBuilder.HandleDuration(this, duration, user, EndEffect));
        }
        float duration = 15f;
        private void EndEffect(PlayerController user)
        {         
            Projectile.BaseEnemyBulletSpeedMultiplier /= 0.5f;
        }
        public override bool CanBeUsed(PlayerController user)
        {
            return true;
        }
    }
}
