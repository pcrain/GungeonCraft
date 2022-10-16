using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Reflection;
using Gungeon;
using MonoMod;
using UnityEngine;
using ItemAPI;
using Dungeonator;

namespace NevernamedsItems
{

    public class ButchersKnife : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Butchers Knife", "butchersknife");
            Game.Items.Rename("outdated_gun_mods:butchers_knife", "nn:butchers_knife");
        var behav =    gun.gameObject.AddComponent<ButchersKnife>();
            behav.preventNormalReloadAudio = true;
            behav.preventNormalFireAudio = true;
            behav.overrideNormalFireAudio = "Play_WPN_blasphemy_shot_01";
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(417) as Gun).gunSwitchGroup;
            gun.SetShortDescription("Word of Kaliber");
            gun.SetLongDescription("Cuts enemies to bits."+"\n\nForged and sharpened by a Gun Cultist who believed she heard the voice of Kaliber speaking to her... asking her for a sacrifice... her son.");
            gun.SetupSprite(null, "butchersknife_idle_001", 8);
            gun.SetAnimationFPS(gun.shootAnimation, 16);
            gun.SetAnimationFPS(gun.chargeAnimation, 8);
            gun.SetAnimationFPS(gun.reloadAnimation, 1);
            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);
            gun.AddPassiveStatModifier(PlayerStats.StatType.Curse, 2f, StatModifier.ModifyMethod.ADDITIVE);
            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Charged;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.DefaultModule.cooldownTime = 1f;
            gun.gunClass = GunClass.CHARGE;
            gun.DefaultModule.angleVariance = 1f;
            gun.DefaultModule.numberOfShotsInClip = 1;
            Projectile projectile = DataCloners.CopyFields<SuperPierceProjectile>(Instantiate(gun.DefaultModule.projectiles[0]));
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);           
            projectile.baseData.damage *= 0.2f;
            projectile.baseData.speed *= 0.7f;
            projectile.pierceMinorBreakables = true;
            projectile.AdditionalScaleMultiplier *= 1f;
            projectile.baseData.range *= 1000f;
            projectile.SetProjectileSpriteRight("butchersknife_projectile", 29, 7, false, tk2dBaseSprite.Anchor.MiddleCenter, 35, 14);
            projectile.specRigidbody.CollideWithTileMap = false;

            NoCollideBehaviour nocollide = projectile.gameObject.GetOrAddComponent<NoCollideBehaviour>();
            nocollide.worksOnEnemies = false;
            nocollide.worksOnProjectiles = true;

            PierceProjModifier pierce = projectile.gameObject.GetOrAddComponent<PierceProjModifier>();
            pierce.penetration = 1000;

            TickDamageBehaviour tickdmg = projectile.gameObject.GetOrAddComponent<TickDamageBehaviour>();
            tickdmg.damageSource = "Butchers Knife";
            tickdmg.starterDamage = 3f;

            ProjectileModule.ChargeProjectile chargeProj = new ProjectileModule.ChargeProjectile
            {
                Projectile = projectile,
                ChargeTime = 0f,
            };
            gun.DefaultModule.chargeProjectiles = new List<ProjectileModule.ChargeProjectile> { chargeProj };

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("ButchersKnife Clip", "NevernamedsItems/Resources/CustomGunAmmoTypes/butchersknife_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/butchersknife_clipempty");

            gun.reloadTime = 5f;
            gun.SetBaseMaxAmmo(35);
            gun.quality = PickupObject.ItemQuality.S;

            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.chargeAnimation).wrapMode = tk2dSpriteAnimationClip.WrapMode.LoopSection;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.chargeAnimation).loopStart = 0;

            ETGMod.Databases.Items.Add(gun, null, "ANY");
            gun.barrelOffset.transform.localPosition = new Vector3(0.87f, 0.25f, 0f);
            ButchersKnifeID = gun.PickupObjectId;
        }
        public static int ButchersKnifeID;
        public override void PostProcessProjectile(Projectile projectile)
        {
            //ETGModConsole.Log("Fired Projectile");
            ProjectileReturned = false;
            if (projectile.Owner is PlayerController)
            {
                PlayerController player = projectile.Owner as PlayerController;
                float rangeToSet = rangeTime;
                if (rangeTime > 12) rangeToSet = 12;
               // ETGModConsole.Log("RangeTime at time of Firing: " + rangeTime);
                KnifeReturnEffect returnknife = projectile.gameObject.GetOrAddComponent<KnifeReturnEffect>();
                returnknife.range = rangeToSet;
                Invoke("ResetRangeTime", 0.1f);
            }
            base.PostProcessProjectile(projectile);
        }
        private void ResetRangeTime() { rangeTime = 0; }

        public void OnProjectileReturn()
        {
            if (!ProjectileReturned && this.gun.IsReloading)
            {
            ProjectileReturned = true;
                this.gun.ForceImmediateReload(false);
                if (gun.CurrentOwner is PlayerController)
                {

                int i = (this.gun.CurrentOwner as PlayerController).PlayerIDX;
                GameUIRoot.Instance.ForceClearReload(i);
                }
                //this.gun.MoveBulletsIntoClip(1);
                //this.gun.PlayIdleAnimation();
            }
        }
        private float rangeTime = 0;
        private bool ProjectileReturned = true;
        private bool canAddToRangeTime = true;
        private void FixedUpdate()
        {
            if (this.gun && this.gun.CurrentOwner != null)
            {
                if (gun.IsCharging)
                {
                    if (canAddToRangeTime)
                    {
                        canAddToRangeTime = false;
                        rangeTime += 1f;
                        Invoke("ChargeRangeCooldown", 0.25f);
                    }
                }
            }
        }
        private void ChargeRangeCooldown() { canAddToRangeTime = true; }


        public ButchersKnife()
        {

        }
    }


    public class KnifeReturnEffect : MonoBehaviour
    {
        public KnifeReturnEffect()
        {
            range = .1f;
        }
        private IEnumerator HandleKnifeSlowdownAndReturn()
        {
            //ETGModConsole.Log("HandleKnifeSlowAndReturn procced");
            float starterSpeed = this.m_projectile.Speed;
            float progressiveMultiplier1 = 0.9f;
            for (int i = 0; i < 7; i++)
            {               
                this.m_projectile.Speed = starterSpeed * progressiveMultiplier1;                
                progressiveMultiplier1 -= 0.1f;
                yield return new WaitForSeconds(0.1f);
            }

            HomeInOnPlayerModifyer orAddComponent = this.m_projectile.gameObject.GetOrAddComponent<HomeInOnPlayerModifyer>();
            orAddComponent.HomingRadius = 1000;
            orAddComponent.AngularVelocity = 4000;
            CollideWithPlayerBehaviour killknife = this.m_projectile.gameObject.GetOrAddComponent<CollideWithPlayerBehaviour>();
            
            
            float progressiveMultiplier2 = 0.3f;
            for (int i = 0; i < 8; i++)
            {
                this.m_projectile.Speed = starterSpeed * progressiveMultiplier2;
                
                progressiveMultiplier2 += 0.1f;
                yield return new WaitForSeconds(0.1f);
            }
            yield break;
        }
        public void Start()
        {
            try
            {
                this.m_projectile = base.GetComponent<Projectile>();
                this.m_projectile.specRigidbody.UpdateCollidersOnScale = true;
                this.m_projectile.OnPostUpdate += this.HandlePostUpdate;
                this.m_projectile.specRigidbody.OnPreRigidbodyCollision += this.HandlePreCollision;
                this.hasAddedHoming = false;
            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
                ETGModConsole.Log(e.StackTrace);
            }
        }
        private void HandlePreCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
        {
            if (otherRigidbody && otherRigidbody.gameObject)
            {
                PlayerController playerness = otherRigidbody.gameObject.GetComponent<PlayerController>();
                if (playerness != null)
                {
                    foreach (Gun gun in playerness.inventory.AllGuns)
                    {
                        if (gun.PickupObjectId == ButchersKnife.ButchersKnifeID)
                        {
                            ButchersKnife butchersknife = gun.GetComponent<ButchersKnife>();
                            if (butchersknife != null)
                            {
                                butchersknife.OnProjectileReturn();
                            }
                        }
                    }
                    this.m_projectile.DieInAir();
                }
            }
        }

        private void HandlePostUpdate(Projectile proj)
        {
            try
            {
                if (!proj)
                {
                    return;
                }
                float elapsedDistance = proj.GetElapsedDistance();
                if (elapsedDistance - m_lastElapsedDistance > range)
                {
                    if (!hasAddedHoming)
                    {
                        GameManager.Instance.StartCoroutine(this.HandleKnifeSlowdownAndReturn());
                        hasAddedHoming = true;
                    }
                }
            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
                ETGModConsole.Log(e.StackTrace);
            }
        }
        private Projectile m_projectile;

        private float m_lastElapsedDistance = 0;
        private bool hasAddedHoming = false;

        public float range;
    }
}
