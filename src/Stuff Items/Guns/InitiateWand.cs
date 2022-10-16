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

namespace NevernamedsItems
{

    public class InitiateWand : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Initiate Wand", "initiatewand");
            Game.Items.Rename("outdated_gun_mods:initiate_wand", "nn:initiate_wand");
            gun.gameObject.AddComponent<InitiateWand>();
            gun.SetShortDescription("You Just Got Witch'd");
            gun.SetLongDescription("Adopts one of four random spell types each time it is encountered."+"\n\nCrafted (poorly) by an Apprentice Gunjurer.");

            gun.SetupSprite(null, "initiatewand_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 15);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(145) as Gun).gunSwitchGroup;
            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(33) as Gun).muzzleFlashEffects;

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 0.7f;
            gun.DefaultModule.cooldownTime = 0.25f;
            gun.DefaultModule.numberOfShotsInClip = 4;
            gun.barrelOffset.transform.localPosition = new Vector3(0.93f, 0.5f, 0f);
            gun.SetBaseMaxAmmo(180);
            gun.gunClass = GunClass.SILLY;

            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.reloadAnimation).wrapMode = tk2dSpriteAnimationClip.WrapMode.Loop;

            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage = 8;
            projectile.baseData.range *= 2;
            projectile.baseData.speed *= 2;
            EasyTrailBullet trail = projectile.gameObject.AddComponent<EasyTrailBullet>();
            trail.TrailPos = projectile.transform.position;
            trail.StartWidth = 0.25f;
            trail.EndWidth = 0f;
            trail.LifeTime = 0.5f;
            trail.BaseColor = ExtendedColours.pink;
            trail.EndColor = ExtendedColours.pink;
            projectile.hitEffects.alwaysUseMidair = true;
            projectile.hitEffects.overrideMidairDeathVFX = (PickupObjectDatabase.GetById(145) as Gun).DefaultModule.projectiles[0].hitEffects.enemy.effects[0].effects[0].effect;
            projectile.SetProjectileSpriteRight("initiatewand_sparkbolt", 7, 7, true, tk2dBaseSprite.Anchor.MiddleCenter, 7, 7);
            SpecialProjectileIdentifier ID1 = projectile.gameObject.AddComponent<SpecialProjectileIdentifier>();
            ID1.SpecialIdentifier = "INITIATE_WAND";

            //Energy Ball
            Projectile energyball = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(86) as Gun).DefaultModule.projectiles[0]);
            energyball.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(energyball.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(energyball);
            energyball.baseData.damage = 17;
            energyball.baseData.speed *= 0.6f;
            energyball.baseData.range *= 0.5f;
            EasyTrailBullet trail3 = energyball.gameObject.AddComponent<EasyTrailBullet>();
            trail3.TrailPos = energyball.transform.position;
            trail3.StartWidth = 0.5f;
            trail3.EndWidth = 0f;
            trail3.LifeTime = 0.2f;
            trail3.BaseColor = ExtendedColours.freezeBlue;
            trail3.EndColor = ExtendedColours.freezeBlue;
            energyball.hitEffects.alwaysUseMidair = true;
            energyball.hitEffects.overrideMidairDeathVFX = (PickupObjectDatabase.GetById(18) as Gun).DefaultModule.projectiles[0].hitEffects.overrideMidairDeathVFX;
            energyball.SetProjectileSpriteRight("initiatewand_energyball", 8, 8, true, tk2dBaseSprite.Anchor.MiddleCenter, 7, 7);
            SpecialProjectileIdentifier ID2 = energyball.gameObject.AddComponent<SpecialProjectileIdentifier>();
            ID2.SpecialIdentifier = "INITIATE_WAND";
            EnergyBall = energyball;

            //Bouncer
            Projectile bouncer = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(86) as Gun).DefaultModule.projectiles[0]);
            bouncer.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(bouncer.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(bouncer);
            bouncer.baseData.damage = 5.5f;
            bouncer.baseData.speed *= 3f;
            bouncer.baseData.range *= 10f;
            bouncer.hitEffects.alwaysUseMidair = true;
            EasyTrailBullet trail4 = bouncer.gameObject.AddComponent<EasyTrailBullet>();
            trail4.TrailPos = bouncer.transform.position;
            trail4.StartWidth = 0.125f;
            trail4.EndWidth = 0f;
            trail4.LifeTime = 0.5f;
            trail4.BaseColor = ExtendedColours.poisonGreen;
            trail4.EndColor = ExtendedColours.poisonGreen;
            BounceProjModifier bouncing = bouncer.gameObject.GetOrAddComponent<BounceProjModifier>();
            bouncing.numberOfBounces = 3;
            bouncing.damageMultiplierOnBounce = 2;
            bouncer.hitEffects.overrideMidairDeathVFX = (PickupObjectDatabase.GetById(89) as Gun).DefaultModule.projectiles[0].hitEffects.overrideMidairDeathVFX;
            bouncer.SetProjectileSpriteRight("initiatewand_bouncer", 4, 4, true, tk2dBaseSprite.Anchor.MiddleCenter, 4, 4);
            SpecialProjectileIdentifier ID3 = bouncer.gameObject.AddComponent<SpecialProjectileIdentifier>();
            ID3.SpecialIdentifier = "INITIATE_WAND";
            GreenBouncyProj = bouncer;

            //Pink Shitty
            Projectile pinkshitty = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(86) as Gun).DefaultModule.projectiles[0]);
            pinkshitty.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(pinkshitty.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(pinkshitty);
            pinkshitty.baseData.damage = 22f;
            pinkshitty.baseData.speed *= 1.5f;
            
            pinkshitty.hitEffects.alwaysUseMidair = true;
            ScaleChangeOverTimeModifier scaler = pinkshitty.gameObject.GetOrAddComponent<ScaleChangeOverTimeModifier>();
            scaler.destroyAfterChange = true;
            scaler.timeToChangeOver = 0.3f;
            scaler.ScaleToChangeTo = 0.01f;
            scaler.suppressDeathFXIfdestroyed = true;
            BounceProjModifier pinkshittybonce = pinkshitty.gameObject.GetOrAddComponent<BounceProjModifier>();
            pinkshittybonce.numberOfBounces = 1;
            pinkshittybonce.damageMultiplierOnBounce = 1;
            pinkshitty.hitEffects.overrideMidairDeathVFX = RainbowGuonStone.RedGuonTransitionVFX;
            pinkshitty.SetProjectileSpriteRight("initiatewand_pinkshitty", 8, 8, true, tk2dBaseSprite.Anchor.MiddleCenter, 6, 6);
            SpecialProjectileIdentifier ID4 = pinkshitty.gameObject.AddComponent<SpecialProjectileIdentifier>();
            ID4.SpecialIdentifier = "INITIATE_WAND";
            PinkBouncer = pinkshitty;

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("Initiate Wand Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/initiatewand_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/initiatewand_clipempty");

            gun.quality = PickupObject.ItemQuality.C;
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            InitiateWandID = gun.PickupObjectId;
        }
        public override Projectile OnPreFireProjectileModifier(Gun gun, Projectile projectile, ProjectileModule mod)
        {
            if ((currentForm > 0) && projectile.GetComponent<SpecialProjectileIdentifier>() != null && projectile.GetComponent<SpecialProjectileIdentifier>().SpecialIdentifier == "INITIATE_WAND")
            {
                switch (currentForm)
                {
                    case 2:
                        return EnergyBall;
                    case 3:
                        return GreenBouncyProj;
                    case 4:
                        return PinkBouncer;
                }
            }
            return base.OnPreFireProjectileModifier(gun, projectile, mod);
        }
        public static Projectile GreenBouncyProj;
        public static Projectile EnergyBall;
        public static Projectile PinkBouncer;
        public static int InitiateWandID;
        public int currentForm = -1;
        protected override void OnPickedUpByPlayer(PlayerController player)
        {
            if (currentForm == -1)
            {
                currentForm = UnityEngine.Random.Range(1, 5);
            }
            base.OnPickedUpByPlayer(player);
        }
        public InitiateWand()
        {

        }
    }
}