using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NevernamedsItems
{
    public class ScaleProjectileStatOffPlayerStat : MonoBehaviour
    {
        public ScaleProjectileStatOffPlayerStat()
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
        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            GameActor owner = m_projectile.Owner;
            if (owner is PlayerController)
            {
                PlayerController player = m_projectile.Owner as PlayerController;
                float statAmount = player.stats.GetStatValue(playerstat);
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
        public PlayerStats.StatType playerstat;
        public float multiplierPerLevelOfStat;
    }
}
