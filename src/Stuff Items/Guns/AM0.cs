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

namespace NevernamedsItems
{
    public class AM0 : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("AM-0", "am0");
            Game.Items.Rename("outdated_gun_mods:am0", "nn:am0");
            gun.gameObject.AddComponent<AM0>();
            gun.SetShortDescription("Fires Ammunition");
            gun.SetLongDescription("Becomes more powerful the more times it's ammo is refilled." + "\n\nThis gun is comically stuffed with whole ammo boxes.");

            gun.SetupSprite(null, "am0_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 12);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(519) as Gun).gunSwitchGroup;
            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Automatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 0.8f;
            gun.DefaultModule.cooldownTime = 0.11f;
            gun.DefaultModule.numberOfShotsInClip = 30;
            gun.barrelOffset.transform.localPosition = new Vector3(2.43f, 0.75f, 0f);
            gun.SetBaseMaxAmmo(500);
            gun.ammo = 500;
            gun.gunClass = GunClass.FULLAUTO;

            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage *= 1f;
            projectile.baseData.speed *= 0.7f;
            projectile.baseData.range *= 2f;

            //ANIMATE BULLET
            projectile.AnimateProjectile(new List<string> {
                "ammoproj_1",
                "ammoproj_2",
                "ammoproj_3",
                "ammoproj_4",
                "ammoproj_5",
                "ammoproj_6",
                "ammoproj_7",
                "ammoproj_8",
                "ammoproj_9",
                "ammoproj_10",
                "ammoproj_11",
                "ammoproj_12",
                "ammoproj_13",
                "ammoproj_14",
                "ammoproj_15",
                "ammoproj_16"
            }, 16, true, new List<IntVector2> {
                new IntVector2(11, 14), //1
                new IntVector2(13, 16), //2            All frames are 13x16 except select ones that are 11-14
                new IntVector2(13, 16), //3
                new IntVector2(13, 16),//4
                new IntVector2(11, 14),//5
                new IntVector2(13, 16),//6
                new IntVector2(13, 16),//7
                new IntVector2(13, 16),//8
                new IntVector2(11, 14),//9
                new IntVector2(13, 16),//10
                new IntVector2(13, 16),//11
                new IntVector2(13, 16),//12
                new IntVector2(11, 14),//13
                new IntVector2(13, 16),//14
                new IntVector2(13, 16),//15
                new IntVector2(13, 16),//16
            }, AnimateBullet.ConstructListOfSameValues(false, 16), AnimateBullet.ConstructListOfSameValues(tk2dBaseSprite.Anchor.MiddleCenter, 16), AnimateBullet.ConstructListOfSameValues(true, 16), AnimateBullet.ConstructListOfSameValues(false, 16),
            AnimateBullet.ConstructListOfSameValues<Vector3?>(null, 16), AnimateBullet.ConstructListOfSameValues<IntVector2?>(null, 16), AnimateBullet.ConstructListOfSameValues<IntVector2?>(null, 16), AnimateBullet.ConstructListOfSameValues<Projectile>(null, 16));

            projectile.SetProjectileSpriteRight("ammoproj_1", 11, 14, false, tk2dBaseSprite.Anchor.MiddleCenter, 11, 14);

            projectile.transform.parent = gun.barrelOffset;
            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("AM0 Ammo Boxes", "NevernamedsItems/Resources/CustomGunAmmoTypes/am0_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/am0_clipempty");
            gun.quality = PickupObject.ItemQuality.B;
            gun.encounterTrackable.EncounterGuid = "this is the AM-0";
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            gun.AddToSubShop(ItemBuilder.ShopType.Trorc);
            AM0ID = gun.PickupObjectId;
        }
        public static int AM0ID;
        Hook ammoPickupHook = new Hook(
                typeof(AmmoPickup).GetMethod("Pickup", BindingFlags.Instance | BindingFlags.Public),
                typeof(AM0).GetMethod("ammoPickupHookMethod")
            );
        protected override void OnPickedUpByPlayer(PlayerController player)
        {
            base.OnPickedUpByPlayer(player);
        }
        public override void PostProcessProjectile(Projectile projectile)
        {
            projectile.baseData.damage *= damageMult;
            base.PostProcessProjectile(projectile);
        }
        public static void ammoPickupHookMethod(Action<AmmoPickup, PlayerController> orig, AmmoPickup self, PlayerController player)
        {
            orig(self, player);
            if (player.HasPickupID(AM0ID))
            {
                if (canGiveDMG)
                {
                    if (self.mode == AmmoPickup.AmmoPickupMode.FULL_AMMO && (player.CurrentGun != null))
                    {
                        if (player.CurrentGun.GetComponent<AM0>() != null)
                        {
                            player.CurrentGun.GetComponent<AM0>().damageMult += 0.1f;
                        }
                        else if (player.PlayerHasActiveSynergy("Menger Clip"))
                        {
                            foreach (Gun gun in player.inventory.AllGuns)
                            {
                                if (gun != null && gun.GetComponent<AM0>() != null)
                                {
                                    gun.GetComponent<AM0>().damageMult += 0.02f;
                                }
                            }
                        }
                    }
                    else if (self.mode == AmmoPickup.AmmoPickupMode.SPREAD_AMMO && player.CurrentGun != null)
                    {
                        if (player.CurrentGun.GetComponent<AM0>() != null)
                        {
                            if (player.PlayerHasActiveSynergy("Menger Clip")) player.CurrentGun.GetComponent<AM0>().damageMult += 0.1f;
                            else player.CurrentGun.GetComponent<AM0>().damageMult += 0.05f;
                        }
                        else
                        {
                            foreach (Gun gun in player.inventory.AllGuns)
                            {
                                if (gun != null && gun.GetComponent<AM0>() != null)
                                {
                                    gun.GetComponent<AM0>().damageMult += 0.02f;
                                }
                            }
                        }
                    }
                    canGiveDMG = false;
                }
                else
                {
                    canGiveDMG = true;
                }
            }
        }
        static bool canGiveDMG = false;
        float damageMult = 1;
        public override void InheritData(Gun other)
        {
            base.InheritData(other);
            AM0 behav = other.GetComponent<AM0>();
            if (behav != null)
            {
                this.damageMult = behav.damageMult;
            }
        }
        public override void MidGameSerialize(List<object> data, int i)
        {
            base.MidGameSerialize(data, i);
            data.Add(this.damageMult);
        }

        public override void MidGameDeserialize(List<object> data, ref int i)
        {
            base.MidGameDeserialize(data, ref i);
            this.damageMult = (float)data[i];
            i += 1;
        }
        public AM0()
        {

        }
    }
}
