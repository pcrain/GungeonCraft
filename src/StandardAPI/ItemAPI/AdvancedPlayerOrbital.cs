using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ItemAPI;
using UnityEngine;

namespace NevernamedsItems
{
    public class AdvancedPlayerOrbitalItem : PassiveItem
    {
        private void CreateOrbital(PlayerController owner)
        {
            GameObject targetOrbitalPrefab = (!(this.OrbitalPrefab != null)) ? this.OrbitalFollowerPrefab.gameObject : this.OrbitalPrefab.gameObject;
            bool flag = this.HasUpgradeSynergy && this.m_synergyUpgradeActive;
            if (flag)
            {
                targetOrbitalPrefab = ((!(this.UpgradeOrbitalPrefab != null)) ? this.UpgradeOrbitalFollowerPrefab.gameObject : this.UpgradeOrbitalPrefab.gameObject);
            }
            bool flag2 = this.HasAdvancedUpgradeSynergy && this.m_advancedSynergyUpgradeActive;
            if (flag2)
            {
                targetOrbitalPrefab = ((!(this.AdvancedUpgradeOrbitalPrefab != null)) ? this.AdvancedUpgradeOrbitalFollowerPrefab.gameObject : this.AdvancedUpgradeOrbitalPrefab.gameObject);
            }
            this.m_extantOrbital = PlayerOrbitalItem.CreateOrbital(owner, targetOrbitalPrefab, this.OrbitalFollowerPrefab != null, null);
            bool flag3 = this.BreaksUponContact && this.m_extantOrbital;
            if (flag3)
            {
                SpeculativeRigidbody component = this.m_extantOrbital.GetComponent<SpeculativeRigidbody>();
                bool flag4 = component;
                if (flag4)
                {
                    SpeculativeRigidbody speculativeRigidbody = component;
                    speculativeRigidbody.OnRigidbodyCollision = (SpeculativeRigidbody.OnRigidbodyCollisionDelegate)Delegate.Combine(speculativeRigidbody.OnRigidbodyCollision, new SpeculativeRigidbody.OnRigidbodyCollisionDelegate(this.HandleBreakOnCollision));
                }
            }
            bool flag5 = this.BreaksUponOwnerDamage && owner;
            if (flag5)
            {
                owner.OnReceivedDamage += this.HandleBreakOnOwnerDamage;
            }
            this.OnOrbitalCreated(this.m_extantOrbital);
        }

        // Token: 0x060000F6 RID: 246 RVA: 0x0000B5A8 File Offset: 0x000097A8
        public static GameObject CreateOrbital(PlayerController owner, GameObject targetOrbitalPrefab, bool isFollower, PlayerOrbitalItem sourceItem = null)
        {
            GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(targetOrbitalPrefab, owner.transform.position, Quaternion.identity);
            bool flag = !isFollower;
            if (flag)
            {
                PlayerOrbital component = gameObject.GetComponent<PlayerOrbital>();
                component.Initialize(owner);
                component.SourceItem = sourceItem;
            }
            else
            {
                PlayerOrbitalFollower component2 = gameObject.GetComponent<PlayerOrbitalFollower>();
                bool flag2 = component2;
                if (flag2)
                {
                    component2.Initialize(owner);
                }
            }
            return gameObject;
        }
        public virtual void OnOrbitalCreated(GameObject orbital)
        {

        }

        // Token: 0x060000F7 RID: 247 RVA: 0x0000B618 File Offset: 0x00009818
        private void HandleBreakOnOwnerDamage(PlayerController arg1)
        {
            bool flag = !this;
            if (!flag)
            {
                bool flag2 = this.BreakVFX && this.m_extantOrbital && this.m_extantOrbital.GetComponentInChildren<tk2dSprite>();
                if (flag2)
                {
                    SpawnManager.SpawnVFX(this.BreakVFX, this.m_extantOrbital.GetComponentInChildren<tk2dSprite>().WorldCenter.ToVector3ZisY(0f), Quaternion.identity);
                }
                bool flag3 = this.m_owner;
                if (flag3)
                {
                    this.m_owner.RemovePassiveItem(this.PickupObjectId);
                    this.m_owner.OnReceivedDamage -= this.HandleBreakOnOwnerDamage;
                }
                UnityEngine.Object.Destroy(base.gameObject);
            }
        }

        // Token: 0x060000F8 RID: 248 RVA: 0x0000B6E0 File Offset: 0x000098E0
        private void HandleBreakOnCollision(CollisionData rigidbodyCollision)
        {
            bool flag = this.m_owner;
            if (flag)
            {
                this.m_owner.RemovePassiveItem(this.PickupObjectId);
            }
            UnityEngine.Object.Destroy(base.gameObject);
        }

        // Token: 0x060000F9 RID: 249 RVA: 0x0000B720 File Offset: 0x00009920
        public void DecoupleOrbital()
        {
            this.m_extantOrbital = null;
            bool flag = this.BreaksUponOwnerDamage && this.m_owner;
            if (flag)
            {
                this.m_owner.OnReceivedDamage -= this.HandleBreakOnOwnerDamage;
            }
        }

        // Token: 0x060000FA RID: 250 RVA: 0x0000B76C File Offset: 0x0000996C
        private void DestroyOrbital()
        {
            bool flag = !this.m_extantOrbital;
            if (!flag)
            {
                bool flag2 = this.BreaksUponOwnerDamage && this.m_owner;
                if (flag2)
                {
                    this.m_owner.OnReceivedDamage -= this.HandleBreakOnOwnerDamage;
                }
                UnityEngine.Object.Destroy(this.m_extantOrbital.gameObject);
                this.m_extantOrbital = null;
            }
        }

        // Token: 0x060000FB RID: 251 RVA: 0x0000B7DC File Offset: 0x000099DC
        public override void Update()
        {
            base.Update();
            bool hasAdvancedUpgradeSynergy = this.HasAdvancedUpgradeSynergy;
            if (hasAdvancedUpgradeSynergy)
            {
                bool flag = this.m_advancedSynergyUpgradeActive && (!this.m_owner || !this.m_owner.PlayerHasActiveSynergy(this.AdvancedUpgradeSynergy));
                if (flag)
                {
                    bool flag2 = this.m_owner;
                    if (flag2)
                    {
                        for (int i = 0; i < this.advancedSynergyModifiers.Count; i++)
                        {
                            this.m_owner.healthHaver.damageTypeModifiers.Remove(this.advancedSynergyModifiers[i]);
                        }
                    }
                    this.m_advancedSynergyUpgradeActive = false;
                    this.DestroyOrbital();
                    bool flag3 = this.m_owner;
                    if (flag3)
                    {
                        this.CreateOrbital(this.m_owner);
                    }
                }
                else
                {
                    bool flag4 = !this.m_advancedSynergyUpgradeActive && this.m_owner && this.m_owner.PlayerHasActiveSynergy(this.AdvancedUpgradeSynergy);
                    if (flag4)
                    {
                        this.m_advancedSynergyUpgradeActive = true;
                        this.DestroyOrbital();
                        bool flag5 = this.m_owner;
                        if (flag5)
                        {
                            this.CreateOrbital(this.m_owner);
                        }
                        for (int j = 0; j < this.advancedSynergyModifiers.Count; j++)
                        {
                            this.m_owner.healthHaver.damageTypeModifiers.Add(this.advancedSynergyModifiers[j]);
                        }
                    }
                }
            }
            bool hasUpgradeSynergy = this.HasUpgradeSynergy;
            if (hasUpgradeSynergy)
            {
                bool flag6 = this.m_synergyUpgradeActive && (!this.m_owner || !this.m_owner.HasActiveBonusSynergy(this.UpgradeSynergy, false));
                if (flag6)
                {
                    bool flag7 = this.m_owner;
                    if (flag7)
                    {
                        for (int k = 0; k < this.synergyModifiers.Length; k++)
                        {
                            this.m_owner.healthHaver.damageTypeModifiers.Remove(this.synergyModifiers[k]);
                        }
                    }
                    this.m_synergyUpgradeActive = false;
                    this.DestroyOrbital();
                    bool flag8 = this.m_owner;
                    if (flag8)
                    {
                        this.CreateOrbital(this.m_owner);
                    }
                }
                else
                {
                    bool flag9 = !this.m_synergyUpgradeActive && this.m_owner && this.m_owner.HasActiveBonusSynergy(this.UpgradeSynergy, false);
                    if (flag9)
                    {
                        this.m_synergyUpgradeActive = true;
                        this.DestroyOrbital();
                        bool flag10 = this.m_owner;
                        if (flag10)
                        {
                            this.CreateOrbital(this.m_owner);
                        }
                        for (int l = 0; l < this.synergyModifiers.Length; l++)
                        {
                            this.m_owner.healthHaver.damageTypeModifiers.Add(this.synergyModifiers[l]);
                        }
                    }
                }
            }
        }

        // Token: 0x060000FC RID: 252 RVA: 0x0000BAD0 File Offset: 0x00009CD0
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.OnNewFloorLoaded = (Action<PlayerController>)Delegate.Combine(player.OnNewFloorLoaded, new Action<PlayerController>(this.HandleNewFloor));
            for (int i = 0; i < this.modifiers.Length; i++)
            {
                player.healthHaver.damageTypeModifiers.Add(this.modifiers[i]);
            }
            this.CreateOrbital(player);
        }

        // Token: 0x060000FD RID: 253 RVA: 0x0000243D File Offset: 0x0000063D
        private void HandleNewFloor(PlayerController obj)
        {
            this.DestroyOrbital();
            this.CreateOrbital(obj);
        }

        // Token: 0x060000FE RID: 254 RVA: 0x0000BB44 File Offset: 0x00009D44
        public override DebrisObject Drop(PlayerController player)
        {
            this.DestroyOrbital();
            player.OnNewFloorLoaded = (Action<PlayerController>)Delegate.Remove(player.OnNewFloorLoaded, new Action<PlayerController>(this.HandleNewFloor));
            for (int i = 0; i < this.modifiers.Length; i++)
            {
                player.healthHaver.damageTypeModifiers.Remove(this.modifiers[i]);
            }
            for (int j = 0; j < this.synergyModifiers.Length; j++)
            {
                player.healthHaver.damageTypeModifiers.Remove(this.synergyModifiers[j]);
            }
            return base.Drop(player);
        }

        // Token: 0x060000FF RID: 255 RVA: 0x0000BBEC File Offset: 0x00009DEC
        public override void OnDestroy()
        {
            bool flag = this.m_owner != null;
            if (flag)
            {
                PlayerController owner = this.m_owner;
                owner.OnNewFloorLoaded = (Action<PlayerController>)Delegate.Remove(owner.OnNewFloorLoaded, new Action<PlayerController>(this.HandleNewFloor));
                for (int i = 0; i < this.modifiers.Length; i++)
                {
                    this.m_owner.healthHaver.damageTypeModifiers.Remove(this.modifiers[i]);
                }
                for (int j = 0; j < this.synergyModifiers.Length; j++)
                {
                    this.m_owner.healthHaver.damageTypeModifiers.Remove(this.synergyModifiers[j]);
                }
                this.m_owner.OnReceivedDamage -= this.HandleBreakOnOwnerDamage;
            }
            this.DestroyOrbital();
            base.OnDestroy();
        }

        public PlayerOrbital OrbitalPrefab;
        public PlayerOrbitalFollower OrbitalFollowerPrefab;
        public bool HasUpgradeSynergy;
        public CustomSynergyType UpgradeSynergy;
        public GameObject UpgradeOrbitalPrefab;
        public GameObject UpgradeOrbitalFollowerPrefab;
        public bool CanBeMimicked;
        public DamageTypeModifier[] modifiers;
        public DamageTypeModifier[] synergyModifiers;
        public bool BreaksUponContact;
        public bool BreaksUponOwnerDamage;
        public GameObject BreakVFX;
        protected GameObject m_extantOrbital;
        protected bool m_synergyUpgradeActive;
        public bool HasAdvancedUpgradeSynergy;
        public string AdvancedUpgradeSynergy;
        public GameObject AdvancedUpgradeOrbitalPrefab;
        public GameObject AdvancedUpgradeOrbitalFollowerPrefab;
        public List<DamageTypeModifier> advancedSynergyModifiers = new List<DamageTypeModifier>();
        protected bool m_advancedSynergyUpgradeActive;
    }
}
