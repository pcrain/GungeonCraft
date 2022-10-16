using Alexandria.Misc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NevernamedsItems
{
    public class SelfReAimBehaviour : MonoBehaviour
    {
        public SelfReAimBehaviour()
        {
            trigger = ReAimTrigger.IMMEDIATE;
            reAimAmount = 1;
            reAimDelay = 0;
            maxReloadReAims = 1;
        }

        public void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            this.owner = m_projectile.ProjectilePlayerOwner();
            if (trigger == ReAimTrigger.IMMEDIATE) StartCoroutine(DoReAim(true));
            else if (trigger == ReAimTrigger.TIMER) StartCoroutine(DoReAim(false));
            else if (trigger == ReAimTrigger.RELOAD && owner != null) owner.OnReloadedGun += OnReload;
        }
        private void OnDestroy()
        {
            if (trigger == ReAimTrigger.RELOAD) owner.OnReloadedGun -= OnReload;
        }
        private void OnReload(PlayerController reloader, Gun gun)
        {
            if (storedReloadReaims < maxReloadReAims)
            {
                StartCoroutine(DoReAim(false));
                storedReloadReaims++;
            }
        }
        public IEnumerator DoReAim(bool instant)
        {
            for (int i = 0; i < reAimAmount; i++)
            {
                if (instant) yield return null;
                else if (reAimDelay > 0) yield return new WaitForSeconds(reAimDelay);
                if (m_projectile)
                {
                    Vector2 dirVec = m_projectile.GetVectorToNearestEnemy();
                    m_projectile.SendInDirection(dirVec, false, true);
                }
            }
            yield break;
        }
        public enum ReAimTrigger
        {
            TIMER,
            RELOAD,
            IMMEDIATE
        }
        public ReAimTrigger trigger;
       // public float angleVariance;
       // public bool scaleAccuracyOffOwner;
        public float reAimDelay;
        public float reAimAmount;
        public float maxReloadReAims;

        private float storedReloadReaims;
        private Projectile m_projectile;
        private PlayerController owner;
    }
}
