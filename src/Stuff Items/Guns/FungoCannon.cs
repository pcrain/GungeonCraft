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
using Alexandria.Misc;

namespace NevernamedsItems
{

    public class FungoCannon : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Fungo Cannon", "fungocannon");
            Game.Items.Rename("outdated_gun_mods:fungo_cannon", "nn:fungo_cannon");
            var behav = gun.gameObject.AddComponent<FungoCannon>();
            behav.preventNormalFireAudio = true;
            behav.preventNormalReloadAudio = true;
            gun.SetShortDescription("PLOOMPH");
            gun.SetLongDescription("A mutated fungun from the Oubliette." + "\n\nThough horrific genetic anomalies have stripped it of it's face and legs, it still retains it's deadly spores.");
            gun.SetupSprite(null, "fungocannon_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 12);
            gun.SetAnimationFPS(gun.chargeAnimation, 6);
            gun.gunClass = GunClass.CHARGE;
            for (int i = 0; i < 20; i++)
            {
                gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);
            }

            //GUN STATS
            foreach (ProjectileModule mod in gun.Volley.projectiles)
            {
                mod.ammoCost = 1;
                mod.shootStyle = ProjectileModule.ShootStyle.Charged;
                mod.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
                mod.cooldownTime = 1f;
                mod.angleVariance = 360f;
                mod.numberOfShotsInClip = 5;
                Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(mod.projectiles[0]);
                mod.projectiles[0] = projectile;
                projectile.gameObject.SetActive(false);
                FakePrefab.MarkAsFakePrefab(projectile.gameObject);
                UnityEngine.Object.DontDestroyOnLoad(projectile);
                projectile.SetProjectileSpriteRight("enemystylespore_projectile", 14, 14, true, tk2dBaseSprite.Anchor.MiddleCenter, 14, 14);
                FungoRandomBullets orAddComponent = projectile.gameObject.GetOrAddComponent<FungoRandomBullets>();
                projectile.baseData.speed *= 0.2f;
                projectile.baseData.damage *= 2f;
                if (mod != gun.DefaultModule) { mod.ammoCost = 0; }

                ProjectileModule.ChargeProjectile chargeProj = new ProjectileModule.ChargeProjectile
                {
                    Projectile = projectile,
                    ChargeTime = 0.5f,
                };
                mod.chargeProjectiles = new List<ProjectileModule.ChargeProjectile> { chargeProj };
            }

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("FungoCannon Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/fungocannon_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/fungocannon_clipempty");

            gun.reloadTime = 1.4f;
            gun.SetBaseMaxAmmo(200);
            gun.quality = PickupObject.ItemQuality.C;

            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.chargeAnimation).wrapMode = tk2dSpriteAnimationClip.WrapMode.LoopSection;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.chargeAnimation).loopStart = 1;

            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).frames[0].eventAudio = "Play_ENM_mushroom_cloud_01";
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).frames[0].triggerEvent = true;

            ETGMod.Databases.Items.Add(gun, null, "ANY");
            gun.barrelOffset.transform.localPosition = new Vector3(1.56f, 0.62f, 0f);
            FungoCannonID = gun.PickupObjectId;
        }
        public static int FungoCannonID;
        public override void PostProcessProjectile(Projectile projectile)
        {
            if (projectile.Owner is PlayerController)
            {
                PlayerController owner = projectile.Owner as PlayerController;
                if (owner != null)
                {
                    if (owner.PlayerHasActiveSynergy("Hunter Spores"))
                    {
                        FungoRandomBullets bulletmod = projectile.GetComponent<FungoRandomBullets>();
                        if (bulletmod != null)
                        {
                            bulletmod.HasSynergyHunterSpores = true;
                        }
                    }
                    if (owner.PlayerHasActiveSynergy("Myshellium")) { projectile.baseData.damage *= 2; }
                }
            }

            base.PostProcessProjectile(projectile);
        }
        public override void OnPostFired(PlayerController player, Gun gun)
        {

            if (player.PlayerHasActiveSynergy("Enspore!"))
            {
                int id = Gungeon.Game.Items["nn:spore_launcher"].PickupObjectId;
                if (player.HasPickupID(id))
                {
                    if (UnityEngine.Random.value <= 0.45) { player.GiveAmmoToGunNotInHand(id, 1); }
                }
            }
            base.OnPostFired(player, gun);
        }
        public FungoCannon()
        {

        }
    }
    public class FungoRandomBullets : MonoBehaviour
    {
        public FungoRandomBullets()
        {
            HasSynergyHunterSpores = false;
        }
        private void Start()
        {
            try
            {
                this.m_projectile = base.GetComponent<Projectile>();
                float damageScaleAmount = UnityEngine.Random.Range(10f, 101f) / 100f;


                this.m_projectile.baseData.damage *= damageScaleAmount;
                this.m_projectile.RuntimeUpdateScale(damageScaleAmount);
                this.m_projectile.baseData.speed /= (damageScaleAmount);

                //this.m_projectile.baseData.UsesCustomAccelerationCurve = true;
                //this.m_projectile.baseData.AccelerationCurve = AnimationCurve;

                this.m_projectile.UpdateSpeed();
                this.speedMultiplierPerFrame = UnityEngine.Random.Range(86f, 98f) / 100f;
                shouldSpeedModify = true;

            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
                ETGModConsole.Log(e.StackTrace);
            }
        }
        private Projectile m_projectile;
        private float speedMultiplierPerFrame;
        private bool shouldSpeedModify;
        public bool HasSynergyHunterSpores;

        private void FixedUpdate()
        {
            if (shouldSpeedModify)
            {
                if (m_projectile.baseData.speed > 0.01f)
                {
                    m_projectile.baseData.speed *= speedMultiplierPerFrame;
                    m_projectile.UpdateSpeed();
                }
                else
                {
                    GameManager.Instance.StartCoroutine(this.FloatHandler());
                    shouldSpeedModify = false;
                }
            }
        }
        private IEnumerator FloatHandler()
        {
            float HunterChance = 0.9f;
            if (HasSynergyHunterSpores) HunterChance = 0.5f;
            if (m_projectile && m_projectile.ProjectilePlayerOwner())
            {
                if (UnityEngine.Random.value <= HunterChance || !m_projectile.ProjectilePlayerOwner().IsInCombat)
                {
                    m_projectile.baseData.speed = 0.1f;
                    m_projectile.UpdateSpeed();
                    int timesToFloat = UnityEngine.Random.Range(3, 6);
                    for (int i = 0; i < timesToFloat; i++)
                    {
                        m_projectile.SendInRandomDirection();
                        yield return new WaitForSeconds(1f);
                    }
                    if (m_projectile) m_projectile.DieInAir();
                }
                else
                {
                    float HunterSpeed = 5f;
                    if (HasSynergyHunterSpores) HunterSpeed = 10f;
                    m_projectile.baseData.speed = HunterSpeed;
                    m_projectile.UpdateSpeed();
                    Vector2 dirVec = m_projectile.GetVectorToNearestEnemy();
                    if (dirVec != Vector2.zero) m_projectile.SendInDirection(dirVec, false, true);
                }
            }

            yield break;
        }
    }
}