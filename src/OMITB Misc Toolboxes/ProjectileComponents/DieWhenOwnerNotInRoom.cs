using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NevernamedsItems
{
    public class DieWhenOwnerNotInRoom : MonoBehaviour
    {
        public DieWhenOwnerNotInRoom()
        {
        }
        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            if (m_projectile && m_projectile.Owner && m_projectile.Owner is PlayerController) Owner = m_projectile.Owner as PlayerController;
        }
        private void Update()
        {
            if (!Owner || Owner.CurrentRoom != m_projectile.transform.position.GetAbsoluteRoom())
            {
                m_projectile.DieInAir(true, false, false, false);
            }
          
        }
        private Projectile m_projectile;
        private PlayerController Owner;
    }
}
