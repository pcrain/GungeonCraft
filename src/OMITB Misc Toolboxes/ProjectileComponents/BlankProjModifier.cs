using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Alexandria.Misc;

namespace NevernamedsItems
{
    public class BlankProjModifier : MonoBehaviour
    {
        public BlankProjModifier()
        {
            blankType = EasyBlankType.MINI;
        }
        private void Awake()
        {
            m_projectile = base.GetComponent<Projectile>();
            m_projectile.OnDestruction += this.HandleBlankOnDestruction;
        }
        private void HandleBlankOnDestruction(Projectile obj)
        {
            if (m_projectile && m_projectile.ProjectilePlayerOwner())
            {
                m_projectile.ProjectilePlayerOwner().DoEasyBlank((!obj.specRigidbody) ? obj.transform.position.XY() : obj.specRigidbody.UnitCenter, EasyBlankType.MINI);
            }              
        }
        private Projectile m_projectile;

        public EasyBlankType blankType;
    }
}
