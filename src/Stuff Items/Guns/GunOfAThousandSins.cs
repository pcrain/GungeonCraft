using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using ItemAPI;
using MonoMod.RuntimeDetour;
using System.Reflection;
using SaveAPI;

namespace NevernamedsItems
{
    public class GunOfAThousandSins : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Gun of a Thousand Sins", "gunofathousandsins");
            Game.Items.Rename("outdated_gun_mods:gun_of_a_thousand_sins", "nn:gun_of_a_thousand_sins");
            gun.gameObject.AddComponent<GunOfAThousandSins>();
            gun.SetShortDescription("Evisceration");
            gun.SetLongDescription("This sidearm was once carried by an accursed sorcerer who put countless innocents to death in order to secure his grab at penultimate power.");

            gun.SetupSprite(null, "gunofathousandsins_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 12);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(198) as Gun).gunSwitchGroup;
            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);

            gun.AddPassiveStatModifier(PlayerStats.StatType.Curse, 1f, StatModifier.ModifyMethod.ADDITIVE);


            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(83) as Gun).muzzleFlashEffects;
            gun.DefaultModule.cooldownTime = 0.11f;
            gun.DefaultModule.numberOfShotsInClip = 7;
            gun.barrelOffset.transform.localPosition = new Vector3(2.37f, 0.81f, 0f);
            gun.SetBaseMaxAmmo(200);
            gun.ammo = 200;
            gun.gunClass = GunClass.PISTOL;

            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage = 20f;
            projectile.baseData.speed *= 1f;
            projectile.baseData.range *= 2f;
            PierceProjModifier piercing = projectile.gameObject.GetOrAddComponent<PierceProjModifier>();
            ThousandSinsProjectileModifier sins = projectile.gameObject.GetOrAddComponent<ThousandSinsProjectileModifier>();
            projectile.hitEffects.alwaysUseMidair = true;
            projectile.hitEffects.overrideMidairDeathVFX = EasyVFXDatabase.BloodiedScarfPoofVFX;

            //ANIMATE BULLET
            projectile.AnimateProjectile(new List<string> {
                "gunofathousandsins_projectile_001",
                "gunofathousandsins_projectile_002",
                "gunofathousandsins_projectile_003",
                "gunofathousandsins_projectile_004",
            }, 12, true, new List<IntVector2> {
                new IntVector2(23, 22), //1
                new IntVector2(23, 22), //2            
                new IntVector2(23, 22), //3
               new IntVector2(23, 22),//4
            }, AnimateBullet.ConstructListOfSameValues(true, 4), //Lightened
            AnimateBullet.ConstructListOfSameValues(tk2dBaseSprite.Anchor.MiddleCenter, 4), //Anchors
            AnimateBullet.ConstructListOfSameValues(true, 4), //Anchors Change Colliders
            AnimateBullet.ConstructListOfSameValues(false, 4), //Fixes Scales
            AnimateBullet.ConstructListOfSameValues<Vector3?>(null, 4), //Offsets
            AnimateBullet.ConstructListOfSameValues<IntVector2?>(new IntVector2(20, 11), 4), //Override Collider Sizes
            AnimateBullet.ConstructListOfSameValues<IntVector2?>(new IntVector2(2, 5), 4), //Override Collider Offsets
            AnimateBullet.ConstructListOfSameValues<Projectile>(null, 4));

            projectile.SetProjectileSpriteRight("gunofathousandsins_projectile_001", 23, 22, true, tk2dBaseSprite.Anchor.MiddleCenter, 20, 11);

            projectile.transform.parent = gun.barrelOffset;

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("Thousand Sins Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/thousandsins_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/thousandsins_clipempty");

            gun.quality = PickupObject.ItemQuality.S;
            ETGMod.Databases.Items.Add(gun, false, "ANY");

            GunOfAThousandSinsID = gun.PickupObjectId;
            gun.SetupUnlockOnCustomFlag(CustomDungeonFlags.ALLJAMMED_BEATEN_HELL, true);

        }
        public static int GunOfAThousandSinsID;       
        public GunOfAThousandSins()
        {

        }
    }
    class ThousandSinsProjectileModifier : MonoBehaviour
    {
        public ThousandSinsProjectileModifier()
        {

        }
        public void Start()
        {
            self = base.GetComponent<Projectile>();
            if (self)
            {
                self.OnHitEnemy += this.OnHitEnemy;
            }
        }
        private void OnHitEnemy(Projectile me, SpeculativeRigidbody enemy, bool fatal)
        {
            if (enemy && fatal)
            {
                if (enemy.healthHaver)
                {
                    float bloodDPS = enemy.healthHaver.GetMaxHealth();
                    bloodDPS *= 0.5f;
                    GoopDefinition Blood = EasyGoopDefinitions.GenerateBloodGoop(bloodDPS, Color.red, 10);
                    DeadlyDeadlyGoopManager goopManagerForGoopType = DeadlyDeadlyGoopManager.GetGoopManagerForGoopType(Blood);
                    goopManagerForGoopType.TimedAddGoopCircle(enemy.UnitCenter, 3, 0.5f, false);
                }
            }
        }
        private Projectile self;
    }
}