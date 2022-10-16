using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using ItemAPI;
using Alexandria.Misc;

namespace NevernamedsItems
{
    public class PumpChargeShotgun : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Pump-Charge Shotgun", "pumpchargeshotgun");
            Game.Items.Rename("outdated_gun_mods:pumpcharge_shotgun", "nn:pump_charge_shotgun");
            gun.gameObject.AddComponent<PumpChargeShotgun>();
            gun.SetShortDescription("HELL IS FULL");
            gun.SetLongDescription("Continuing to reload this shotgun when it's already full will charge it up. Be careful not to overcharge it!"+"\n\nForged by a bloodthirsty adventurer who sought to cleanse Bullet Hell.");

            gun.SetupSprite(null, "pumpchargeshotgun_idle_001", 8);
            //gun.SetAnimationFPS(gun.shootAnimation, 10);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(55) as Gun).gunSwitchGroup;
            //GUN STATS
            gun.CanReloadNoMatterAmmo = true;
            gun.reloadTime = 1f;
            gun.barrelOffset.transform.localPosition = new Vector3(2.06f, 0.62f, 0f);
            gun.SetBaseMaxAmmo(80);
            gun.ammo = 80;
            gun.gunClass = GunClass.SHOTGUN;
            //BULLET STATS
            for (int i = 0; i < 5; i++)
            {
                gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);
            }

            //GUN STATS
            foreach (ProjectileModule mod in gun.Volley.projectiles)
            {
                mod.ammoCost = 1;
                mod.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
                mod.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
                mod.cooldownTime = 0.5f;
                mod.angleVariance = 20f;
                mod.numberOfShotsInClip = 5;
                Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(mod.projectiles[0]);
                mod.projectiles[0] = projectile;
                projectile.gameObject.SetActive(false);
                FakePrefab.MarkAsFakePrefab(projectile.gameObject);
                UnityEngine.Object.DontDestroyOnLoad(projectile);
                projectile.baseData.speed *= 1f;
                projectile.baseData.damage *= 1f;
                projectile.hitEffects.alwaysUseMidair = true;
                projectile.hitEffects.overrideMidairDeathVFX = EasyVFXDatabase.GreenLaserCircleVFX;
                projectile.SetProjectileSpriteRight("pumpcharge_green_projectile", 8, 6, true, tk2dBaseSprite.Anchor.MiddleCenter, 7, 5);
                projectile.gameObject.AddComponent<BloodBurstOnKill>();
                if (mod != gun.DefaultModule) { mod.ammoCost = 0; }
            }

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("Pump-Charge Shotgun Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/pumpchargeshotgun_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/pumpchargeshotgun_clipempty");

            gun.quality = PickupObject.ItemQuality.B;
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            //Level 2 Proj
            Projectile projectile2 = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            projectile2.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile2.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile2);
            projectile2.baseData.speed *= 1f;
            projectile2.baseData.damage *= 1.4f;
                projectile2.hitEffects.alwaysUseMidair = true;
            projectile2.hitEffects.overrideMidairDeathVFX = EasyVFXDatabase.YellowLaserCircleVFX;
            projectile2.gameObject.AddComponent<BloodBurstOnKill>();

            projectile2.SetProjectileSpriteRight("pumpcharge_yellow_projectile", 10, 6, true, tk2dBaseSprite.Anchor.MiddleCenter, 9, 5);
            Level2Projectile = projectile2;

            //Level 3 Proj
            Projectile projectile3 = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            projectile3.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile3.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile3);
            projectile3.baseData.speed *= 1f;
                projectile3.hitEffects.alwaysUseMidair = true;
            
            projectile3.hitEffects.overrideMidairDeathVFX = EasyVFXDatabase.YellowLaserCircleVFX;
            projectile3.gameObject.AddComponent<BloodBurstOnKill>();
            projectile3.baseData.damage *= 1.8f;
            projectile3.SetProjectileSpriteRight("pumpcharge_orange_projectile", 12, 6, true, tk2dBaseSprite.Anchor.MiddleCenter, 11, 5);
            Level3Projectile = projectile3;

            //Level 4 Proj
            Projectile projectile4 = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            projectile4.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile4.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile4);
                projectile4.gameObject.AddComponent<BloodBurstOnKill>();
            projectile4.baseData.speed *= 1f;
                projectile4.hitEffects.alwaysUseMidair = true;
            projectile4.hitEffects.overrideMidairDeathVFX = EasyVFXDatabase.RedLaserCircleVFX;

            projectile4.baseData.damage *= 2.2f;
            projectile4.SetProjectileSpriteRight("pumpcharge_red_projectile", 17, 8, true, tk2dBaseSprite.Anchor.MiddleCenter, 16, 7);
            Level4Projectile = projectile4;

            SetupCollection();
        }
        public static Projectile Level2Projectile;
        public static Projectile Level3Projectile;
        public static Projectile Level4Projectile;

        public List<GameObject> extantSprites;
        private static tk2dSpriteCollectionData GunVFXCollection;
        private static GameObject VFXScapegoat;
        private static int Meter1ID;
        private static int Meter2ID;
        private static int Meter3ID;
        private static int Meter4ID;
        private static int Meter5ID;

        public override Projectile OnPreFireProjectileModifier(Gun gun, Projectile projectile, ProjectileModule mod)
        {
            if (currentChargeLevel == 2) return Level2Projectile;
            else if (currentChargeLevel == 3) return Level3Projectile;
            else if (currentChargeLevel == 4) return Level4Projectile;
            return base.OnPreFireProjectileModifier(gun, projectile, mod);
        }
        private static void SetupCollection()
        {
            VFXScapegoat = new GameObject();
            UnityEngine.Object.DontDestroyOnLoad(VFXScapegoat);
            PumpChargeShotgun.GunVFXCollection = SpriteBuilder.ConstructCollection(VFXScapegoat, "PumpChargeVFX_Collection");
            UnityEngine.Object.DontDestroyOnLoad(PumpChargeShotgun.GunVFXCollection);
            Meter1ID = SpriteBuilder.AddSpriteToCollection("NevernamedsItems/Resources/MiscVFX/PumpChargeMeter1", PumpChargeShotgun.GunVFXCollection);
            Meter2ID = SpriteBuilder.AddSpriteToCollection("NevernamedsItems/Resources/MiscVFX/PumpChargeMeter2", PumpChargeShotgun.GunVFXCollection);
            Meter3ID = SpriteBuilder.AddSpriteToCollection("NevernamedsItems/Resources/MiscVFX/PumpChargeMeter3", PumpChargeShotgun.GunVFXCollection);
            Meter4ID = SpriteBuilder.AddSpriteToCollection("NevernamedsItems/Resources/MiscVFX/PumpChargeMeter4", PumpChargeShotgun.GunVFXCollection);
            Meter5ID = SpriteBuilder.AddSpriteToCollection("NevernamedsItems/Resources/MiscVFX/PumpChargeMeter5", PumpChargeShotgun.GunVFXCollection);
        }
        private IEnumerator ShowChargeLevel(GameActor gunOwner, int chargeLevel)
        {
            if (extantSprites.Count > 0)
            {
                for (int i = extantSprites.Count - 1; i >= 0; i--)
                {
                    UnityEngine.Object.Destroy(extantSprites[i].gameObject);
                }
                extantSprites.Clear();
            }
            GameObject newSprite = new GameObject("Level Popup", new Type[] { typeof(tk2dSprite) }) { layer = 0 };
            newSprite.transform.position = (gunOwner.transform.position + new Vector3(0.5f, 2));
            tk2dSprite m_ItemSprite = newSprite.AddComponent<tk2dSprite>();
            extantSprites.Add(newSprite);
            int spriteID = -1;
            switch (chargeLevel)
            {
                case 1:
                    spriteID = Meter1ID;
                    break;
                case 2:
                    spriteID = Meter2ID;
                    break;
                case 3:
                    spriteID = Meter3ID;
                    break;
                case 4:
                    spriteID = Meter4ID;
                    break;
                case 5:
                    spriteID = Meter5ID;
                    break;
            }
            m_ItemSprite.SetSprite(PumpChargeShotgun.GunVFXCollection, spriteID);
            m_ItemSprite.PlaceAtPositionByAnchor(newSprite.transform.position, tk2dBaseSprite.Anchor.LowerCenter);
            m_ItemSprite.transform.localPosition = m_ItemSprite.transform.localPosition.Quantize(0.0625f);
            newSprite.transform.parent = gunOwner.transform;
            if (m_ItemSprite)
            {
                sprite.AttachRenderer(m_ItemSprite);
                m_ItemSprite.depthUsesTrimmedBounds = true;
                m_ItemSprite.UpdateZDepth();
            }
            sprite.UpdateZDepth();
            yield return new WaitForSeconds(2);
            if (newSprite)
            {
                extantSprites.Remove(newSprite);
                Destroy(newSprite.gameObject);
            }
            yield break;
        }

        public int currentChargeLevel = 1;
        private void DoExplosion(PlayerController player, Gun gun)
        {
            Exploder.DoDefaultExplosion(gun.sprite.WorldCenter, Vector2.zero);
        }
        public override void OnReloadPressed(PlayerController player, Gun gun, bool manualReload)
        {
            if (currentChargeLevel <= 0 || currentChargeLevel >= 6) currentChargeLevel = 1;
            if (gun.ClipShotsRemaining == gun.ClipCapacity)
            {
                if (!gun.IsReloading)
                {
                    gun.ClipShotsRemaining = (gun.ClipCapacity - 1);
                    gun.Reload();
                    gun.MoveBulletsIntoClip(1);
                    switch (currentChargeLevel)
                    {
                        case 1:
                            currentChargeLevel = 2;
                            player.StartCoroutine(ShowChargeLevel(player.gameActor, 2));
                            break;
                        case 2:
                            currentChargeLevel = 3;
                            player.StartCoroutine(ShowChargeLevel(player.gameActor, 3));
                            break;
                        case 3:
                            currentChargeLevel = 4;
                            player.StartCoroutine(ShowChargeLevel(player.gameActor, 4));
                            break;
                        case 4:
                            currentChargeLevel = 1;
                            player.StartCoroutine(ShowChargeLevel(player.gameActor, 5));
                            DoExplosion(player, gun);
                            break;
                    }
                }
            }
            else
            {
                currentChargeLevel = 1;
                player.StartCoroutine(ShowChargeLevel(player.gameActor, 1));
            }

            base.OnReloadPressed(player, gun, manualReload);
        }
        protected override void OnPickup(GameActor owner)
        {
            currentChargeLevel = 1;
            owner.StartCoroutine(ShowChargeLevel(owner, 1));
            base.OnPickup(owner);
        }
        public override void OnSwitchedAwayFromThisGun()
        {
            currentChargeLevel = 1;
            base.OnSwitchedAwayFromThisGun();
        }
        public PumpChargeShotgun()
        {

        }
    }
    public class BloodBurstOnKill : MonoBehaviour
    {
        public BloodBurstOnKill()
        {

        }
        private void Start()
        {
            this.projectile = base.GetComponent<Projectile>();
            if (projectile.ProjectilePlayerOwner() != null)
            {
                owner = this.projectile.ProjectilePlayerOwner();
            }
            this.projectile.OnHitEnemy += this.OnHitEnemy;
        }
        private void OnHitEnemy(Projectile bullet, SpeculativeRigidbody enemy, bool fatal)
        {
            if (enemy && enemy.healthHaver && fatal)
            {
                float procChance = 0.1f;
                if (owner && owner.PlayerHasActiveSynergy("Blood For The Blood God")) procChance = 0.2f;
                if (UnityEngine.Random.value <= procChance)
                {
                    UnityEngine.Object.Instantiate<GameObject>(EasyVFXDatabase.TeleporterPrototypeTelefragVFX, enemy.UnitCenter, Quaternion.identity);
                    if (owner && owner.PlayerHasActiveSynergy("Blood For The Blood God"))
                    {
                        GoopDefinition Blood = EasyGoopDefinitions.GenerateBloodGoop(15, Color.red, 20);
                        DeadlyDeadlyGoopManager goopManagerForGoopType = DeadlyDeadlyGoopManager.GetGoopManagerForGoopType(Blood);
                        goopManagerForGoopType.TimedAddGoopCircle(enemy.UnitCenter, 3, 0.5f, false);
                    }
                    if (owner && owner.PlayerHasActiveSynergy("BLOOD IS FUEL"))
                    {
                        if (Vector2.Distance(owner.sprite.WorldCenter, enemy.sprite.WorldCenter) <= 4)
                        {
                            owner.healthHaver.ApplyHealing(0.5f);
                        }
                    }
                }

            }
        }
        private Projectile projectile;
        private PlayerController owner;
    }
}