using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NevernamedsItems
{
    public class StoutBulletsProjectileBehaviour : MonoBehaviour
    {
        public StoutBulletsProjectileBehaviour()
        {

        }

        public void Start()
        {
            try
            {
                this.m_projectile = base.GetComponent<Projectile>();
                float num = Mathf.Max(0f, m_projectile.baseData.range - this.RangeCap);
                float num2 = Mathf.Lerp(this.MinDamageIncrease, this.MaxDamageIncrease, Mathf.Clamp01(num / 15f));
                m_projectile.OnPostUpdate += this.HandlePostUpdate;
                m_projectile.AdditionalScaleMultiplier *= this.ScaleIncrease;
                m_projectile.baseData.damage *= num2;
            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
                ETGModConsole.Log(e.StackTrace);
            }
        }

        private void HandlePostUpdate(Projectile proj)
        {
            if (proj && proj.GetElapsedDistance() > this.RangeCap)
            {
                proj.RuntimeUpdateScale(this.DescaleAmount);
                proj.baseData.damage /= this.DamageCutOnDescale;
                proj.OnPostUpdate -= this.HandlePostUpdate;
            }
        }

        public float RangeCap = 7f;
        public float MaxDamageIncrease = 1.75f;
        public float MinDamageIncrease = 1.125f;
        public float ScaleIncrease = 1.5f;
        public float DescaleAmount = 0.5f;
        public float DamageCutOnDescale = 2f;

        private Projectile m_projectile;
    }
}
