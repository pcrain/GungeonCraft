using ItemAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NevernamedsItems
{
    public class ProjectileSetupUtility
    {
        public static Projectile MakeProjectile(int idToCopy, float damage, float range, float speed, float bossDamageMult =0)
        {
           Projectile midBuildProj = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(idToCopy) as Gun).DefaultModule.projectiles[0]);
            midBuildProj.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(midBuildProj.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(midBuildProj);

            //Stats
            midBuildProj.baseData.damage = damage;
            midBuildProj.baseData.speed = speed;
            midBuildProj.baseData.range = range;
            midBuildProj.BossDamageMultiplier = bossDamageMult;
            return midBuildProj;
        }
    }
}
