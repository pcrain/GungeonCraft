using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NevernamedsItems
{
    class StaticExplosionDatas
    {
        public static ExplosionData explosiveRoundsExplosion = PickupObjectDatabase.GetById(304).GetComponent<ComplexProjectileModifier>().ExplosionData;
        public static ExplosionData genericSmallExplosion = GameManager.Instance.Dungeon.sharedSettingsPrefab.DefaultSmallExplosionData;
        public static ExplosionData genericLargeExplosion = GameManager.Instance.Dungeon.sharedSettingsPrefab.DefaultExplosionData;
        public static ExplosionData tetrisBlockExplosion = (PickupObjectDatabase.GetById(483) as Gun).DefaultModule.projectiles[0].GetComponent<TetrisBuff>().tetrisExplosion;
        public static ExplosionData customDynamiteExplosion = new ExplosionData()
        {
            effect = genericLargeExplosion.effect,
            ignoreList = genericLargeExplosion.ignoreList,
            ss = genericLargeExplosion.ss,
            damageRadius = 5f,
            damageToPlayer = 0f,
            doDamage = true,
            damage = 45,
            doDestroyProjectiles = true,
            doForce = true,
            debrisForce = 30f,
            preventPlayerForce = true,
            explosionDelay = 0.1f,
            usesComprehensiveDelay = false,
            doScreenShake = true,
            playDefaultSFX = true,
        };
    }
}
