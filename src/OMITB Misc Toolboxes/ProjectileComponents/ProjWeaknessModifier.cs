using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NevernamedsItems
{
    public class ProjWeaknessModifier : MonoBehaviour
    {
        public ProjWeaknessModifier()
        {
            chanceToApply = 1;

            EnemyHealthModifier = 0.65f;
            EnemyCooldownModifier = 1.2f;
            EnemySpeedMultiplier = 0.8f;
            EnemyKeepHealthPercentage = true;
            EnemyDuration = 100000f;

            BossHealthModifier = 0.75f;
            BossCooldownModifier = 1.2f;
            BossSpeedMultiplier = 0.8f;
            BossKeepHealthPercentage = true;
            BossDuration = 100000f;

            UsesSeparateStatsForBosses = true;
            isDarkPrince = false;
        }

        public float chanceToApply;

        public float EnemyHealthModifier;
        public float EnemyCooldownModifier;
        public float EnemySpeedMultiplier;
        public bool EnemyKeepHealthPercentage;
        public float EnemyDuration;

        public bool UsesSeparateStatsForBosses;
        public float BossHealthModifier;
        public float BossCooldownModifier;
        public float BossSpeedMultiplier;
        public bool BossKeepHealthPercentage;
        public float BossDuration;

        public bool isDarkPrince;
        private void Start()
        {
            if (chanceToApply >= UnityEngine.Random.value)
            {
                WeakenedDebuff = new AIActorDebuffEffect
                {
                    HealthMultiplier = EnemyHealthModifier,
                    CooldownMultiplier = EnemyCooldownModifier,
                    SpeedMultiplier = EnemySpeedMultiplier,
                    KeepHealthPercentage = EnemyKeepHealthPercentage,
                    OverheadVFX = MagickeCauldron.overheadder,
                    duration = EnemyDuration
                };
                BossWeakenedDebuff = new AIActorDebuffEffect
                {
                    HealthMultiplier = BossHealthModifier,
                    CooldownMultiplier = BossCooldownModifier,
                    SpeedMultiplier = BossSpeedMultiplier,
                    KeepHealthPercentage = BossKeepHealthPercentage,
                    OverheadVFX = MagickeCauldron.overheadder,
                    duration = BossDuration
                };

                base.GetComponent<Projectile>().OnHitEnemy += this.OnHitenemy;
            }
        }
        private void OnHitenemy(Projectile self, SpeculativeRigidbody enemy, bool fatal)
        {
            if (enemy && enemy.aiActor && !fatal)
            {
                if (enemy.healthHaver && enemy.healthHaver.IsBoss && UsesSeparateStatsForBosses) enemy.aiActor.ApplyEffect(BossWeakenedDebuff, 1f, null);
                else enemy.aiActor.ApplyEffect(WeakenedDebuff, 1f, null);


                if (isDarkPrince) enemy.gameObject.AddComponent<DarkPrince.DebuffedByDarkPrince>();
            }
        }
        public static AIActorDebuffEffect WeakenedDebuff;
        public static AIActorDebuffEffect BossWeakenedDebuff;
    }
}
