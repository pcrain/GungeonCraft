using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NevernamedsItems
{
    public class ScaleProjectileStatOffConsumableCount : MonoBehaviour
    {
        public ScaleProjectileStatOffConsumableCount()
        {
            this.multiplierPerLevelOfStat = 0.1f;
        }
        public enum ProjectileStatType
        {
            DAMAGE,
            SPEED,
            RANGE,
            SCALE,
            KNOCKBACK,
            BOSSDAMAGE,
            JAMMEDDAMAGE,
        }
        public enum ConsumableType
        {
            MONEY,
            BLANKS,
            KEYS,
            ITEMS,
            GUNS,
            ARMOUR,
            HEALTH,
            RATKEYS,
        }
        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            GameActor owner = m_projectile.Owner;
            if (owner is PlayerController)
            {
                PlayerController player = m_projectile.Owner as PlayerController;
                float statAmount = 0;
                switch (this.consumableType)
                {
                    case ConsumableType.ARMOUR:
                        statAmount = player.healthHaver.Armor;
                        break;
                    case ConsumableType.BLANKS:
                        statAmount = player.Blanks;
                        break;
                    case ConsumableType.GUNS:
                        statAmount = player.inventory.AllGuns.Count;
                        break;
                    case ConsumableType.HEALTH:
                        statAmount = (player.healthHaver.GetCurrentHealth() * 2);
                        break;
                    case ConsumableType.ITEMS:
                        statAmount = player.passiveItems.Count + player.activeItems.Count;
                        break;
                    case ConsumableType.KEYS:
                        statAmount = player.carriedConsumables.KeyBullets;
                        break;
                    case ConsumableType.MONEY:
                        statAmount = player.carriedConsumables.Currency;
                        break;
                    case ConsumableType.RATKEYS:
                        statAmount = player.carriedConsumables.ResourcefulRatKeys;
                        break;
                }
                float multiplier = (statAmount * multiplierPerLevelOfStat) + 1;
                switch (this.projstat)
                {
                    case ProjectileStatType.DAMAGE:
                        m_projectile.baseData.damage *= multiplier;
                        break;
                    case ProjectileStatType.SPEED:
                        m_projectile.baseData.speed *= multiplier;
                        m_projectile.UpdateSpeed();
                        break;
                    case ProjectileStatType.RANGE:
                        m_projectile.baseData.range *= multiplier;
                        break;
                    case ProjectileStatType.KNOCKBACK:
                        m_projectile.baseData.force *= multiplier;
                        break;
                    case ProjectileStatType.SCALE:
                        m_projectile.RuntimeUpdateScale(multiplier);
                        break;
                    case ProjectileStatType.BOSSDAMAGE:
                        m_projectile.BossDamageMultiplier *= multiplier;
                        break;
                    case ProjectileStatType.JAMMEDDAMAGE:
                        m_projectile.BlackPhantomDamageMultiplier *= multiplier;
                        break;
                }

            }
        }
        private Projectile m_projectile;
        public ProjectileStatType projstat;
        public ConsumableType consumableType;
        public float multiplierPerLevelOfStat;
    }
}
