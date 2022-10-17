using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using CwaffingTheGungy;

namespace ItemAPI
{
    public class AdvancedDualWieldSynergyProcessor : MonoBehaviour
    {
        public void Awake()
        {
            this.m_gun = base.GetComponent<Gun>();
        }

        private bool EffectValid(PlayerController p)
        {
            if (!p)
            {
                return false;
            }
            if (!p.PlayerHasActiveSynergy(this.SynergyNameToCheck))
            {
                return false;
            }
            if (this.m_gun.CurrentAmmo == 0)
            {
                return false;
            }
            if (p.inventory.GunLocked.Value)
            {
                return false;
            }
            if (!this.m_isCurrentlyActive)
            {
                int indexForGun = this.GetIndexForGun(p, this.PartnerGunID);
                if (indexForGun < 0)
                {
                    return false;
                }
                if (p.inventory.AllGuns[indexForGun].CurrentAmmo == 0)
                {
                    return false;
                }
            }
            else if (p.CurrentSecondaryGun != null && p.CurrentSecondaryGun.PickupObjectId == this.PartnerGunID && p.CurrentSecondaryGun.CurrentAmmo == 0)
            {
                return false;
            }
            return true;
        }

        private bool PlayerUsingCorrectGuns()
        {
            return this.m_gun && this.m_gun.CurrentOwner && this.m_cachedPlayer && this.m_cachedPlayer.inventory.DualWielding && this.m_cachedPlayer.PlayerHasActiveSynergy(this.SynergyNameToCheck) && (!(this.m_cachedPlayer.CurrentGun != this.m_gun) || this.m_cachedPlayer.CurrentGun.PickupObjectId == this.PartnerGunID) && (!(this.m_cachedPlayer.CurrentSecondaryGun != this.m_gun) || this.m_cachedPlayer.CurrentSecondaryGun.PickupObjectId == this.PartnerGunID);
        }

        private void Update()
        {
            this.CheckStatus();
        }

        private void CheckStatus()
        {
            if (this.m_isCurrentlyActive)
            {
                if (!this.PlayerUsingCorrectGuns() || !this.EffectValid(this.m_cachedPlayer))
                {
                    this.DisableEffect();
                }
            }
            else if (this.m_gun && this.m_gun.CurrentOwner is PlayerController)
            {
                PlayerController playerController = this.m_gun.CurrentOwner as PlayerController;
                if (playerController.inventory.DualWielding && playerController.CurrentSecondaryGun.PickupObjectId == this.m_gun.PickupObjectId && playerController.CurrentGun.PickupObjectId == this.PartnerGunID)
                {
                    this.m_isCurrentlyActive = true;
                    this.m_cachedPlayer = playerController;
                    return;
                }
                this.AttemptActivation(playerController);
            }
        }

        private void AttemptActivation(PlayerController ownerPlayer)
        {
            if (this.EffectValid(ownerPlayer))
            {
                this.m_isCurrentlyActive = true;
                this.m_cachedPlayer = ownerPlayer;
                ownerPlayer.inventory.SetDualWielding(true, "synergy");
                int indexForGun = this.GetIndexForGun(ownerPlayer, this.m_gun.PickupObjectId);
                int indexForGun2 = this.GetIndexForGun(ownerPlayer, this.PartnerGunID);
                ownerPlayer.inventory.SwapDualGuns();
                if (indexForGun >= 0 && indexForGun2 >= 0)
                {
                    while (ownerPlayer.inventory.CurrentGun.PickupObjectId != this.PartnerGunID)
                    {
                        ownerPlayer.inventory.ChangeGun(1, false, false);
                    }
                }
                ownerPlayer.inventory.SwapDualGuns();
                if (ownerPlayer.CurrentGun && !ownerPlayer.CurrentGun.gameObject.activeSelf)
                {
                    ownerPlayer.CurrentGun.gameObject.SetActive(true);
                }
                if (ownerPlayer.CurrentSecondaryGun && !ownerPlayer.CurrentSecondaryGun.gameObject.activeSelf)
                {
                    ownerPlayer.CurrentSecondaryGun.gameObject.SetActive(true);
                }
                this.m_cachedPlayer.GunChanged += this.HandleGunChanged;
            }
        }

        private int GetIndexForGun(PlayerController p, int gunID)
        {
            for (int i = 0; i < p.inventory.AllGuns.Count; i++)
            {
                if (p.inventory.AllGuns[i].PickupObjectId == gunID)
                {
                    return i;
                }
            }
            return -1;
        }

        private void HandleGunChanged(Gun arg1, Gun newGun, bool arg3)
        {
            this.CheckStatus();
        }

        private void DisableEffect()
        {
            if (this.m_isCurrentlyActive)
            {
                this.m_isCurrentlyActive = false;
                this.m_cachedPlayer.inventory.SetDualWielding(false, "synergy");
                this.m_cachedPlayer.GunChanged -= this.HandleGunChanged;
                this.m_cachedPlayer.stats.RecalculateStats(this.m_cachedPlayer, false, false);
                this.m_cachedPlayer = null;
            }
        }

        public string SynergyNameToCheck;
        public int PartnerGunID;
        private Gun m_gun;
        private bool m_isCurrentlyActive;
        private PlayerController m_cachedPlayer;
    }
}
