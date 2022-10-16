using Alexandria.Misc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NevernamedsItems
{
    public class SlowDownOverTimeModifier : MonoBehaviour
    {
        public SlowDownOverTimeModifier()
        {
            timeTillKillAfterCompleteStop = 7f;
            killAfterCompleteStop = false;
            timeToSlowOver = 1;
            doRandomTimeMultiplier = false;
            extendTimeByRangeStat = true;
        }
        private Projectile self;
        private PlayerController owner;
        private float initialSpeed;
        public bool killAfterCompleteStop;
        public float timeToSlowOver;
        public bool extendTimeByRangeStat;
        public bool doRandomTimeMultiplier;
        public float timeTillKillAfterCompleteStop;
        private void Start()
        {
            this.self = base.GetComponent<Projectile>();
            owner = self.ProjectilePlayerOwner();
            initialSpeed = self.baseData.speed;

            StartCoroutine(DoSpeedChange());
        }
        private IEnumerator DoSpeedChange()
        {
            float realTime = timeToSlowOver;
            if (doRandomTimeMultiplier) realTime *= UnityEngine.Random.Range(0.5f, 1f);
            if (extendTimeByRangeStat) realTime *= self.ProjectilePlayerOwner().stats.GetStatValue(PlayerStats.StatType.RangeMultiplier);

            float elapsed = 0f;
            while (elapsed < realTime)
            {
                elapsed += BraveTime.DeltaTime;
                float t = Mathf.Clamp01(elapsed / realTime);
                float speedMod = Mathf.Lerp(initialSpeed, 0, t);

                self.baseData.speed = speedMod;
                self.UpdateSpeed();

                if (!self) break;
                yield return null;
            }
            if (killAfterCompleteStop)
            {
                BulletLifeTimer timer = self.gameObject.AddComponent<BulletLifeTimer>();
                timer.secondsTillDeath = timeTillKillAfterCompleteStop;
            }
        }
    }
}
