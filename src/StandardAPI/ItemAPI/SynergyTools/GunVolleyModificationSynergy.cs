using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Alexandria.ItemAPI;

namespace NevernamedsItems
{
    class GunVolleyModificationSynergy : MonoBehaviour
    {
        public GunVolleyModificationSynergy()
        {

        }
        private void Awake()
        {
            m_gun = GetComponent<Gun>();
            Gun gun = m_gun;
        }
        private void Update()
        {
            if (m_gun)
            {
                if (m_gun.GunPlayerOwner())
                {
                    if (m_gun.GunPlayerOwner() != lastRegisteredOwner)
                    {
                        if (lastRegisteredOwner != null) AssignVolley(lastRegisteredOwner, false);
                        AssignVolley(m_gun.GunPlayerOwner(), true);
                        lastRegisteredOwner = m_gun.GunPlayerOwner();
                    }
                }
                else
                {
                    if (lastRegisteredOwner != null) AssignVolley(lastRegisteredOwner, false);
                }
            }
        }
        private void Destroy()
        {
            if (lastRegisteredOwner) AssignVolley(lastRegisteredOwner, false);
        }
        private void AssignVolley(PlayerController target, bool assign)
        {
            if (assign)
            {
                target.stats.AdditionalVolleyModifiers += ModifyVolley;
                target.stats.RecalculateStats(target, false, false);
            }
            else
            {
                target.stats.AdditionalVolleyModifiers -= ModifyVolley;
                target.stats.RecalculateStats(target, false, false);
            }
        }
        public void ModifyVolley(ProjectileVolleyData volleyToModify)
        {
            int count = volleyToModify.projectiles.Count;
            for (int i = 0; i < count; i++)
            {
                ProjectileModule projectileModule = volleyToModify.projectiles[i];
                for (int j = 0; j < 2; j++)
                {
                    int sourceIndex = i;
                    if (projectileModule.CloneSourceIndex >= 0)
                    {
                        sourceIndex = projectileModule.CloneSourceIndex;
                    }
                    ProjectileModule projectileModule2 = ProjectileModule.CreateClone(projectileModule, false, sourceIndex);
                    projectileModule2.angleVariance *= 1.2f;
                    projectileModule2.ignoredForReloadPurposes = true;
                    projectileModule2.ammoCost = 0;
                    volleyToModify.projectiles.Add(projectileModule2);
                }
            }
        }
        private PlayerController lastRegisteredOwner;
        private Gun m_gun;
    }
}
