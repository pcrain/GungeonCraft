using CwaffingTheGungy;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SynergyTools
{
    class AdvancedFireOnReloadSynergyProcessor : MonoBehaviour
    {
        public AdvancedFireOnReloadSynergyProcessor()
        {
            synergyToCheck = null;
            projToFire = null;
            numToFire = 1;
            angleVariance = 5;
        }
        private void Awake()
        {
            m_gun = GetComponent<Gun>();
            Gun gun = m_gun;
            gun.OnReloadPressed += Reload;
        }
        private void Reload(PlayerController player, Gun gun, bool manual)
        {
            //ETGModConsole.Log("ran 1");
            if (gun.IsReloading && (gun.ClipShotsRemaining < gun.ClipCapacity))
            {
                //ETGModConsole.Log("ran 2");
                if (hasFired)
                {
                    return;
                }
                //if (player == null) ETGModConsole.Log("Player should null themselves, NOW!");
                //if (!gun.GunPlayerOwner()) ETGModConsole.Log("Gun owner does not exist");
                //if (gun.GunPlayerOwner() != player) ETGModConsole.Log("Gun owner is not 'player'");
                //ETGModConsole.Log("Checking: "+synergyToCheck);
                if (player.PlayerHasActiveSynergy(synergyToCheck))
                {
                    //ETGModConsole.Log("ran 3");
                    for (int i = 0; i < numToFire; i++)
                    {
                        if (projToFire != null)
                        {
                            Vector2 gunbarrel = gun.barrelOffset.position;

                            if (player != null) angleVariance *= player.stats.GetStatValue(PlayerStats.StatType.Accuracy);
                            float positiveVariance = angleVariance * 0.5f;
                            float negativeVariance = positiveVariance * -1f;
                            float finalVariance = UnityEngine.Random.Range(negativeVariance, positiveVariance);
                            float angle = gun.CurrentAngle + finalVariance;

                            GameObject gameObject = SpawnManager.SpawnProjectile(projToFire.gameObject, gunbarrel, Quaternion.Euler(0f, 0f, angle), true);
                            Projectile component = gameObject.GetComponent<Projectile>();
                            if (component != null)
                            {
                                component.Owner = player;
                                component.Shooter = player.specRigidbody;
                                component.PossibleSourceGun = gun;
                                component.baseData.damage *= player.stats.GetStatValue(PlayerStats.StatType.Damage);
                                component.baseData.speed *= player.stats.GetStatValue(PlayerStats.StatType.ProjectileSpeed);
                                component.baseData.force *= player.stats.GetStatValue(PlayerStats.StatType.KnockbackMultiplier);
                                component.baseData.range *= player.stats.GetStatValue(PlayerStats.StatType.RangeMultiplier);
                                component.BossDamageMultiplier *= player.stats.GetStatValue(PlayerStats.StatType.DamageToBosses);
                                component.AdditionalScaleMultiplier *= player.stats.GetStatValue(PlayerStats.StatType.PlayerBulletScale);
                                component.UpdateSpeed();
                                player.DoPostProcessProjectile(component);
                            }
                        }
                    }
                }
                else
                {
                    //ETGModConsole.Log("Player does not have synergy: " + synergyToCheck);
                }
                hasFired = true;
                if (this.hasFired)
                {
                    player.StartCoroutine(this.HandleReloadDelay(gun));
                }
            }
        }
        private IEnumerator HandleReloadDelay(Gun sourceGun)
        {
            yield return new WaitForSeconds(sourceGun.reloadTime);
            this.hasFired = false;
            yield break;
        }
        private bool hasFired = false;
        [SerializeField]
        public string synergyToCheck;
        private Gun m_gun;
        public Projectile projToFire;
        public int numToFire;
        public float angleVariance;
    }
}
