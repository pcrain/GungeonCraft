using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using ItemAPI;
using Dungeonator;
using SaveAPI;

namespace NevernamedsItems
{

    public class Paraglocks : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Paraglocks", "paraglocks");
            Game.Items.Rename("outdated_gun_mods:paraglocks", "nn:paraglocks");
            gun.gameObject.AddComponent<Paraglocks>();
            gun.SetShortDescription("Corrupts Absolutely");
            gun.SetLongDescription("The Rusty Sidearm was brou-to the Gungeon by an infamous fug-a low-ranking Primerdyne soldier-It's never let him down." + "\n\nJust because children are young, doesn't mea-ssault robot that fled to the Gunge-legendary gunplay expert." + "\n\nBetrayer!");

            gun.SetupSprite(null, "paraglocks_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 14);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(38) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.gunHandedness = GunHandedness.OneHanded;
            gun.DefaultModule.cooldownTime = 0.15f;
            gun.DefaultModule.angleVariance = 7;
            gun.muzzleFlashEffects.type = VFXPoolType.None;
            gun.DefaultModule.numberOfShotsInClip = 6;
            gun.barrelOffset.transform.localPosition = new Vector3(1.0f, 1.0f, 0f);
            gun.SetBaseMaxAmmo(300);
            gun.ammo = 300;
            gun.gunClass = GunClass.SILLY;

            gun.quality = PickupObject.ItemQuality.D;
            ETGMod.Databases.Items.Add(gun, null, "ANY");
            ParaglocksID = gun.PickupObjectId;
            paradoxShader = ShaderCache.Acquire("Brave/PlayerShaderEevee");
            gun.SetupUnlockOnCustomFlag(CustomDungeonFlags.BOSSRUSH_PARADOX, true);
        }

        protected override void Update()
        {
            if (!(Dungeon.IsGenerating || Dungeon.ShouldAttemptToLoadFromMidgameSave))
            {
                if (gun && gun.CurrentOwner)
                {
                    if (timeSinceLastTransform <= 0)
                    {
                        currentFormBuffedBySynergy = false;
                        int targetID = BraveUtility.RandomElement(starterGunIDs);
                        timeSinceLastTransform = UnityEngine.Random.Range(1f, 20f);
                        gun.TransformToTargetGun(PickupObjectDatabase.GetById(targetID) as Gun);
                        ProcessGunShader();
                        gun.gunHandedness = GunHandedness.OneHanded;
                        if (idsBuffedByAssociatedDissasociationsSynergy.Contains(targetID)) currentFormBuffedBySynergy = true;
                        isTransformed = true;
                    }
                    else
                    {
                        timeSinceLastTransform -= BraveTime.DeltaTime;
                    }
                }
                else if (gun)
                {
                    if (isTransformed)
                    {
                        isTransformed = false;
                        gun.TransformToTargetGun(PickupObjectDatabase.GetById(ParaglocksID) as Gun);
                        RemoveGunShader();
                    }
                }
            }
            base.Update();
        }
        private void ProcessGunShader()
        {
            MeshRenderer component = gun.GetComponent<MeshRenderer>();
            if (!component)
            {
                return;
            }
            Material[] sharedMaterials = component.sharedMaterials;
            for (int i = 0; i < sharedMaterials.Length; i++)
            {
                if (sharedMaterials[i].shader == ShaderCache.Acquire("Brave/PlayerShaderEevee"))
                {
                    return;
                }
            }
            Array.Resize<Material>(ref sharedMaterials, sharedMaterials.Length + 1);
            Material material = new Material(paradoxShader);
            material.SetTexture("_EeveeTex", ResourceManager.LoadAssetBundle("shared_auto_001").LoadAsset<Texture2D>("nebula_reducednoise"));
            material.SetTexture("_MainTex", sharedMaterials[0].GetTexture("_MainTex"));
            sharedMaterials[sharedMaterials.Length - 1] = material;
            component.sharedMaterials = sharedMaterials;
        }
        private void RemoveGunShader()
        {
            if (!gun)
            {
                return;
            }
            MeshRenderer component = gun.GetComponent<MeshRenderer>();
            if (!component)
            {
                return;
            }
            Material[] sharedMaterials = component.sharedMaterials;
            List<Material> list = new List<Material>();
            for (int i = 0; i < sharedMaterials.Length; i++)
            {
                if (sharedMaterials[i].shader != paradoxShader)
                {
                    list.Add(sharedMaterials[i]);
                }
            }
            component.sharedMaterials = list.ToArray();
        }
        private bool currentFormBuffedBySynergy = false;
        private static Shader paradoxShader;
        protected override void OnPickedUpByPlayer(PlayerController player)
        {
            isTransformed = false;
            timeSinceLastTransform = 0;
            base.OnPickedUpByPlayer(player);
            
        }
        public List<int> idsBuffedByAssociatedDissasociationsSynergy = new List<int>();
        private bool isTransformed = false;
        private float timeSinceLastTransform = 0;
        private List<int> starterGunIDs = new List<int>()
        {
            417, //Blasphemy
            604, //Slinger
            24, //Dart Gun
            89, //Rogue Special
            86, //Marine Sidearm
            99, //Rusty Sidearm
            80, //Budget Revolver
            88, //Robot's Right Hand
            603, //Lamey Gun
            //Alts
            809, //Marine Alt
            810, //Rusty Alt
            811, //Dart Alt
            812, //Robot Alt
            813, //Blasphemy Alt
            652, //Budget Alt
            651, //Rogue Alt
        };
        public override void PostProcessProjectile(Projectile projectile)
        {
            projectile.baseData.damage *= 1.3f;
            if (currentFormBuffedBySynergy) projectile.baseData.damage *= 2;
            base.PostProcessProjectile(projectile);
        }
        public static int ParaglocksID;
        public Paraglocks()
        {

        }
    }
}
