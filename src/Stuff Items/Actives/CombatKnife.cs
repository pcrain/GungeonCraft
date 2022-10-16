using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Dungeonator;
using ItemAPI;
using UnityEngine;

namespace NevernamedsItems
{
    class CombatKnife : PlayerItem
    {
        public static void Init()
        {
            string itemName = "Combat Knife";
            string resourceName = "NevernamedsItems/Resources/combatknife_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<CombatKnife>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Quiet and Always Available";
            string longDesc = "In the galaxy at large, knife kills are considered demonstrations of extreme skill, and many bounty hunters, soldiers, and general vagabonds often forego more effective weaponry in hopes of gaining that prestige.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.Timed, 3);
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.Curse, 1f, StatModifier.ModifyMethod.ADDITIVE);

            item.consumable = false;
            item.quality = ItemQuality.D; //D


        }
        public override void DoEffect(PlayerController user)
        {
            SlashData KnifeSlash = new SlashData();
            KnifeSlash.damage = 30 * user.stats.GetStatValue(PlayerStats.StatType.Damage);
            KnifeSlash.enemyKnockbackForce = 10 * user.stats.GetStatValue(PlayerStats.StatType.KnockbackMultiplier);
            KnifeSlash.projInteractMode = SlashDoer.ProjInteractMode.IGNORE;

            List<GameActorEffect> effectlist = null;
            if (user.PlayerHasActiveSynergy("1000 Degree Knife")) { effectlist = new List<GameActorEffect>(); effectlist.Add(StaticStatusEffects.hotLeadEffect); }
            KnifeSlash.statusEffects = effectlist;

            if (user.PlayerHasActiveSynergy("Mirror Blade")) KnifeSlash.projInteractMode = SlashDoer.ProjInteractMode.REFLECT;

            if (user.PlayerHasActiveSynergy("Tri-Tip Dagger")) KnifeSlash.enemyKnockbackForce /= 3f;
            if (user.PlayerHasActiveSynergy("Whirling Blade")) KnifeSlash.enemyKnockbackForce = 0;

            user.StartCoroutine(DoSlash(user, 0, 0, KnifeSlash));
            if (user.PlayerHasActiveSynergy("Whirling Blade"))
            {
                user.StartCoroutine(DoSlash(user, 90, 0.25f, KnifeSlash));
                user.StartCoroutine(DoSlash(user, 180, 0.5f, KnifeSlash));
                user.StartCoroutine(DoSlash(user, 270, 0.75f, KnifeSlash));
            }
        }
        private IEnumerator DoSlash(PlayerController user, float angle, float delay, SlashData slashParameters)
        {
            yield return new WaitForSeconds(delay);
            AkSoundEngine.PostEvent("Play_WPN_blasphemy_shot_01", user.gameObject);
            Vector2 vector = user.CenterPosition;
            Vector2 normalized = (user.unadjustedAimPoint.XY() - vector).normalized;
            normalized = normalized.Rotate(angle);
            Vector2 dir = (user.CenterPosition + normalized * 0.75f);
            float angleToUse = user.CurrentGun.CurrentAngle + angle;

            SlashDoer.DoSwordSlash(dir, angleToUse, user, slashParameters, user.transform);
            if (user.PlayerHasActiveSynergy("Tri-Tip Dagger"))
            {
                Vector2 normalized2 = normalized.Rotate(45);
                Vector2 dir2 = (user.CenterPosition + normalized2 * 0.75f);
                SlashDoer.DoSwordSlash(dir2, angleToUse + 45, user, slashParameters, user.transform);
                Vector2 normalized3 = normalized.Rotate(-45);
                Vector2 dir3 = (user.CenterPosition + normalized3 * 0.75f);
                SlashDoer.DoSwordSlash(dir3, angleToUse + -45, user, slashParameters, user.transform);
            }
            yield break;
        }
    }
}
