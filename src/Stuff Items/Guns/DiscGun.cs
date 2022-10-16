using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using ItemAPI;
using UnityEngine;
using System.Reflection;

namespace NevernamedsItems
{

    public class DiscGun : GunBehaviour
    {
        public static int DiscGunID;

        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Disc Gun", "discgun");
            Game.Items.Rename("outdated_gun_mods:disc_gun", "nn:disc_gun");
            gun.gameObject.AddComponent<DiscGun>();
            gun.SetShortDescription("Bad Choices");
            gun.SetLongDescription("Fires sharp, bouncing discs." + "\n\nCapable of hurting it's bearer, because someone thought that would be funny.");

            gun.SetupSprite(null, "discgun_idle_001", 8);
            gun.SetAnimationFPS(gun.shootAnimation, 14);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.DefaultModule.cooldownTime = 0.25f;
            gun.DefaultModule.numberOfShotsInClip = 10;
            gun.SetBaseMaxAmmo(300);
            //gun.DefaultModule.positionOffset = new Vector3(1f, 0f, 0f);
            gun.barrelOffset.transform.localPosition = new Vector3(1.37f, 0.68f, 0f);
            gun.gunClass = GunClass.PISTOL;

            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage *= 4f;
            projectile.baseData.range *= 20f;
            projectile.baseData.speed *= 0.4f;
            projectile.SetProjectileSpriteRight("discgun_projectile", 15, 15, true, tk2dBaseSprite.Anchor.MiddleCenter, 9, 9);
            SelfHarmBulletBehaviour SuicidalTendancies = projectile.gameObject.AddComponent<SelfHarmBulletBehaviour>();

            PierceProjModifier Piercing = projectile.gameObject.GetOrAddComponent<PierceProjModifier>();
            Piercing.penetratesBreakables = true;
            Piercing.penetration += 10;
            BounceProjModifier Bouncing = projectile.gameObject.GetOrAddComponent<BounceProjModifier>();
            Bouncing.numberOfBounces = 10;

            projectile.transform.parent = gun.barrelOffset;
            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("Disc Gun Discs", "NevernamedsItems/Resources/CustomGunAmmoTypes/discgun_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/discgun_clipempty");
            gun.quality = PickupObject.ItemQuality.D;

            gun.encounterTrackable.EncounterGuid = "this is the Disc Gun";
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            DiscGunID = gun.PickupObjectId;

            string bleh = "Not a Bot, if you're sniffing around in my code, lookin to steal for the Nuclear Throne Mode, you're a stinker. It's cool, I'm a stinker too, just wanted to let you know";
            if (bleh == null) ETGModConsole.Log("BOT WHAT THE FUCK DID YOU DO");
        }
        public override void PostProcessProjectile(Projectile projectile)
        {
            base.PostProcessProjectile(projectile);
            PlayerController playerController = projectile.Owner as PlayerController;
            if (playerController.PlayerHasActiveSynergy("Even Worse Choices")) projectile.baseData.damage *= 2f;
            if (playerController.PlayerHasActiveSynergy("Discworld"))
            {
                HomingModifier homing = projectile.gameObject.AddComponent<HomingModifier>();
                homing.AngularVelocity = 50f;
                homing.HomingRadius = 50f;
            }
        }
        public override void OnPostFired(PlayerController player, Gun gun)
        {
            gun.PreventNormalFireAudio = true;
            AkSoundEngine.PostEvent("Play_WPN_saw_impact_01", gameObject);
        }
        public DiscGun()
        {

        }
    }
    public class SelfHarmBulletBehaviour : MonoBehaviour
    {
        public SelfHarmBulletBehaviour()
        {
        }

        private void Awake()
        {
            try
            {
                //ETGModConsole.Log("Awake is being called");
                this.m_projectile = base.GetComponent<Projectile>();
                canDealDamage = false;
                Invoke("HandleCooldown", 1f);
                PlayerController playerController = this.m_projectile.Owner as PlayerController;
                //if (playerController.PlayerHasActiveSynergy("Even Worse Choices")) this.m_projectile.baseData.damage *= 2f;
                this.m_projectile.allowSelfShooting = true;
                this.m_projectile.collidesWithEnemies = true;
                this.m_projectile.collidesWithPlayer = true;
                this.m_projectile.SetNewShooter(this.m_projectile.Shooter);
                this.m_projectile.UpdateCollisionMask();
                this.m_projectile.specRigidbody.OnPreRigidbodyCollision += this.OnHitSelf;
                //ETGModConsole.Log("Awake finished");

            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
                ETGModConsole.Log(e.StackTrace);
            }
        }
        private Projectile m_projectile;
        private bool canDealDamage = true;
        private void OnHitSelf(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
        {
            try
            {
                //ETGModConsole.Log("OnHitSelf Triggered");
                bool isProtectedByDiscworld = false;
                float selfHarmDamageAmount = 0.5f;
                FieldInfo field = typeof(Projectile).GetField("m_hasPierced", BindingFlags.Instance | BindingFlags.NonPublic);
                field.SetValue(myRigidbody.projectile, false);
                PlayerController playerController = this.m_projectile.Owner as PlayerController;
                if (playerController.PlayerHasActiveSynergy("Even Worse Choices")) selfHarmDamageAmount = 1.0f;
                if (playerController.PlayerHasActiveSynergy("Discworld"))
                {
                    PlayableCharacters characterIdentity = playerController.characterIdentity;
                    if(playerController.ForceZeroHealthState)
                    {
                        if (playerController.healthHaver.Armor <= 1)
                        {
                            isProtectedByDiscworld = true;
                        }
                    }
                    else 
                    {
                        if (playerController.healthHaver.Armor == 0 || playerController.healthHaver.NextDamageIgnoresArmor)
                        {
                            if (playerController.healthHaver.GetCurrentHealth() == 0.5f)
                            {
                                isProtectedByDiscworld = true;

                            }
                            else if (playerController.healthHaver.GetCurrentHealth() == 1f && playerController.PlayerHasActiveSynergy("Even Worse Choices"))
                            {
                                isProtectedByDiscworld = true;
                            }
                        }
                        else if (playerController.healthHaver.NextShotKills)
                        {
                            isProtectedByDiscworld = true;
                        }
                    }                    
                }
                PlayerController component = otherRigidbody.GetComponent<PlayerController>();
                if (component && !component.IsGhost)
                {
                    if (this.m_projectile.PossibleSourceGun)
                    {
                        if (canDealDamage && !component.IsDodgeRolling && !isProtectedByDiscworld)
                        {
                            component.healthHaver.ApplyDamage(selfHarmDamageAmount, Vector2.zero, "Disc Gun", CoreDamageTypes.None, DamageCategory.Normal, true, null, false);
                            canDealDamage = false;
                            Invoke("HandleCooldown", 1f);
                        }
                        PhysicsEngine.SkipCollision = true;
                    }
                }

            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
                ETGModConsole.Log(e.StackTrace);
            }

        }
        private void HandleCooldown()
        {
            canDealDamage = true;
        }
        private void Update()
        {

        }
    }
}