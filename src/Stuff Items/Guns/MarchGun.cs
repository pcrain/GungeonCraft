using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using Alexandria.ItemAPI;
using SaveAPI;
using Alexandria.Misc;

namespace NevernamedsItems
{
    public class MarchGun : GunBehaviour
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("March Gun", "marchgun");
            Game.Items.Rename("outdated_gun_mods:march_gun", "nn:march_gun");
            gun.gameObject.AddComponent<MarchGun>();
            gun.SetShortDescription("Direct To The Point");
            gun.SetLongDescription("Deals bonus damage when fired in the same direction the user is moving."+"\n\nBrought to the Gungeon by notorious tapdancer Tom Toe Tucker.");

            gun.SetupSprite(null, "marchgun_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 12);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(732) as Gun).gunSwitchGroup;

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.DefaultModule.cooldownTime = 0.1f;
            gun.DefaultModule.numberOfShotsInClip = 9;
            gun.barrelOffset.transform.localPosition = new Vector3(1.37f, 0.63f, 0f);
            gun.SetBaseMaxAmmo(222);
            gun.gunClass = GunClass.PISTOL;
            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(18) as Gun).muzzleFlashEffects;


            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage = 7.5f;
            projectile.baseData.speed *= 1f;
            projectile.baseData.range *= 1f;
            projectile.hitEffects.overrideMidairDeathVFX = EasyVFXDatabase.SmoothLightBlueLaserCircleVFX;
            projectile.hitEffects.alwaysUseMidair = true;
            projectile.SetProjectileSpriteRight("march_none_projectile", 9, 9, false, tk2dBaseSprite.Anchor.MiddleCenter, 7, 7);

            GunTools.SetupDefinitionForProjectileSprite("march_left_projectile", ETGMod.Databases.Items.ProjectileCollection.inst.GetSpriteIdByName("march_left_projectile"), 17, 11, false, 15, 9, 0, 0, null);
            GunTools.SetupDefinitionForProjectileSprite("march_right_projectile", ETGMod.Databases.Items.ProjectileCollection.inst.GetSpriteIdByName("march_right_projectile"), 17, 11, false, 15, 9, 0, 0, null);
            GunTools.SetupDefinitionForProjectileSprite("march_up_projectile", ETGMod.Databases.Items.ProjectileCollection.inst.GetSpriteIdByName("march_up_projectile"), 11, 17, false, 9, 15, 0, 0, null);
            GunTools.SetupDefinitionForProjectileSprite("march_down_projectile", ETGMod.Databases.Items.ProjectileCollection.inst.GetSpriteIdByName("march_down_projectile"), 11, 17, false, 9, 15, 0, 0, null);

            projectile.gameObject.AddComponent<MarchGunBulletController>();
            projectile.transform.parent = gun.barrelOffset;

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("March gun Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/marchgun_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/marchgun_clipempty");

            gun.quality = PickupObject.ItemQuality.C; //C

            ETGMod.Databases.Items.Add(gun, null, "ANY");
            DemolitionistID = gun.PickupObjectId;
        }
        public static int DemolitionistID;
        public MarchGun()
        {

        }
    }
    public class MarchGunBulletController : MonoBehaviour
    {
        public MarchGunBulletController()
        {
            correctDirDamageMult = 2f;
            correctDirScaleMult = 1.2f;
            oppositeDirDamageMult = 1f;
            oppositeDirScaleMult = 1f;
        }
        private void Start()
        {
            self = base.GetComponent<Projectile>();
            PlayerController owner = self.ProjectilePlayerOwner();
            if (owner)
            {
                if (owner.PlayerHasActiveSynergy("Tappy Toes"))
                {
                    oppositeDirDamageMult = 3;
                    oppositeDirScaleMult = 1.4f;
                }
                if (owner.specRigidbody.Velocity != Vector2.zero)
                {
                    float dir = owner.LastCommandedDirection.ToAngle();
                    float projDir = self.Direction.ToAngle();
                     
                    //Handle effects
                    if (owner.PlayerHasActiveSynergy("Step To The Beat"))
                    {
                        self.SendInDirection(dir.DegreeToVector2(), false, false);
                        projDir = dir;
                        self.baseData.damage *= 2;
                    }

                    if (projDir.IsBetweenRange(-45, 45)) //right
                    {
                        if (dir.IsBetweenRange(-45, 45)) { self.baseData.damage *= correctDirDamageMult; self.RuntimeUpdateScale(correctDirScaleMult); }
                        else if (dir.IsBetweenRange(135, 180) || dir.IsBetweenRange(-180, -135)) { self.baseData.damage *= oppositeDirDamageMult; self.RuntimeUpdateScale(oppositeDirScaleMult); }

                    }
                    else if (projDir.IsBetweenRange(46, 134)) //up
                    {
                        if (dir.IsBetweenRange(46, 134)) { self.baseData.damage *= correctDirDamageMult; self.RuntimeUpdateScale(correctDirScaleMult); }
                        else if (dir.IsBetweenRange(-134, -46)) { self.baseData.damage *= oppositeDirDamageMult; self.RuntimeUpdateScale(oppositeDirScaleMult); }

                    }
                    else if (projDir.IsBetweenRange(-134, -46)) //down
                    {
                        if (dir.IsBetweenRange(-134, -46)) { self.baseData.damage *= correctDirDamageMult; self.RuntimeUpdateScale(correctDirScaleMult); }
                        else if (dir.IsBetweenRange(46, 136) || dir.IsBetweenRange(-135, -180)) { self.baseData.damage *= oppositeDirDamageMult; self.RuntimeUpdateScale(oppositeDirScaleMult); }

                    }
                    else if (projDir.IsBetweenRange(135, 180) || projDir.IsBetweenRange(-180, -135)) //left
                    {
                        if (dir.IsBetweenRange(135, 180) || dir.IsBetweenRange(-180, -135)) { self.baseData.damage *= correctDirDamageMult; self.RuntimeUpdateScale(correctDirScaleMult); }
                        else if (dir.IsBetweenRange(-45, 45)) { self.baseData.damage *= oppositeDirDamageMult; self.RuntimeUpdateScale(oppositeDirScaleMult); }
                    }

                    //Handle sprites
                    if (dir.IsBetweenRange(-45, 45)) //right
                    {                     
                        self.sprite.spriteId = ETGMod.Databases.Items.ProjectileCollection.inst.GetSpriteIdByName("march_right_projectile");
                    }
                    else if (dir.IsBetweenRange(46, 134)) //up
                    {                       
                        self.sprite.spriteId = ETGMod.Databases.Items.ProjectileCollection.inst.GetSpriteIdByName("march_up_projectile");
                    }
                    else if (dir.IsBetweenRange(-134, -46)) //down
                    {                    
                        self.sprite.spriteId = ETGMod.Databases.Items.ProjectileCollection.inst.GetSpriteIdByName("march_down_projectile");
                    }
                    else if (dir.IsBetweenRange(135, 180) || dir.IsBetweenRange(-180, -135)) //left
                    {                       
                        self.sprite.spriteId = ETGMod.Databases.Items.ProjectileCollection.inst.GetSpriteIdByName("march_left_projectile");
                    }
                }
                else
                {
                    self.sprite.spriteId = ETGMod.Databases.Items.ProjectileCollection.inst.GetSpriteIdByName("march_none_projectile");
                    self.baseData.damage *= 0.7f; 
                }
            }

        }
        private Projectile self;
        private float correctDirDamageMult;
        private float correctDirScaleMult;
        private float oppositeDirDamageMult;
        private float oppositeDirScaleMult;
    }
}

