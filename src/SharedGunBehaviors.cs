using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Gungeon;
using Dungeonator;
using SaveAPI;
using System.Collections;

namespace CwaffingTheGungy
{
    public class FakeProjectileComponent : MonoBehaviour
    {
        // dummy compponent
        private void Start()
        {
            Projectile p = base.GetComponent<Projectile>();
            p.sprite.renderer.enabled = false;
            p.damageTypes &= (~CoreDamageTypes.Electric);
        }
    }
}

