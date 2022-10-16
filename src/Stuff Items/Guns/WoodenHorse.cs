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
using Dungeonator;

namespace NevernamedsItems
{

    public class WoodenHorse : AdvancedGunBehavior
    {

        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Wooden Horse", "woodenhorse");
            Game.Items.Rename("outdated_gun_mods:wooden_horse", "nn:wooden_horse");
            gun.gameObject.AddComponent<WoodenHorse>();
            gun.SetShortDescription("Let Me In!");
            gun.SetLongDescription("This equine effigy was engineered by an ancient gunslinger as a misleading gift to gain the trust of the Gundead."+"\n\nTo this day, Gundead are still fooled!");

            gun.SetupSprite(null, "woodenhorse_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 12);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(12) as Gun).gunSwitchGroup;

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Automatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1.5f;
            gun.DefaultModule.cooldownTime = 0.11f;
            gun.DefaultModule.numberOfShotsInClip = 30;
            gun.barrelOffset.transform.localPosition = new Vector3(2.75f, 1.06f, 0f);
            gun.SetBaseMaxAmmo(400);
            gun.ammo = 400;
            gun.gunClass = GunClass.SILLY;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage = 7.7f;
            projectile.SetProjectileSpriteRight("woodenhorse_projectile", 8,8, false, tk2dBaseSprite.Anchor.MiddleCenter, 7, 7);

            projectile.transform.parent = gun.barrelOffset;

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("Wooden Horse Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/woodenhorse_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/thinline_clipempty");

            gun.quality = PickupObject.ItemQuality.A;
            ETGMod.Databases.Items.Add(gun, false, "ANY");
            WoodenHorseID = gun.PickupObjectId;

        }
        public static int WoodenHorseID;
        private void OnEnteredCombat()
        {
            if (gun && gun.GunPlayerOwner() && gun.GunPlayerOwner().CurrentGun && gun.GunPlayerOwner().CurrentGun.PickupObjectId == gun.PickupObjectId)
            {
                StealthEffect();
            }
        }
        protected override void OnPickedUpByPlayer(PlayerController player)
        {
            player.OnEnteredCombat += this.OnEnteredCombat;
            base.OnPickedUpByPlayer(player);
        }
        protected override void OnPostDroppedByPlayer(PlayerController player)
        {
            player.OnEnteredCombat -= this.OnEnteredCombat;
            if (player.IsStealthed)
            {
                BreakStealth(player);
            }
            base.OnPostDroppedByPlayer(player);
        }
        public override void OnDestroy()
        {
            if (gun && gun.GunPlayerOwner())
            {
                BreakStealth(gun.CurrentOwner as PlayerController);
                gun.GunPlayerOwner().OnEnteredCombat -= this.OnEnteredCombat;
            }
            base.OnDestroy();
        }

        private void StealthEffect()
        {
            PlayerController owner = gun.CurrentOwner as PlayerController;
            this.BreakStealth(owner);
            owner.OnItemStolen += this.BreakStealthOnSteal;
            owner.ChangeSpecialShaderFlag(1, 1f);
            owner.healthHaver.OnDamaged += this.OnDamaged;
            owner.SetIsStealthed(true, "woodenhorse");
            owner.SetCapableOfStealing(true, "woodenhorse", null);
            GameManager.Instance.StartCoroutine(this.Unstealthy());
        }
        private IEnumerator Unstealthy()
        {
            yield return new WaitForSeconds(0.15f);
            (gun.CurrentOwner as PlayerController).OnDidUnstealthyAction += this.BreakStealth;
            yield break;
        }
        private void OnDamaged(float resultValue, float maxValue, CoreDamageTypes damageTypes, DamageCategory damageCategory, Vector2 damageDirection)
        {
            this.BreakStealth(gun.CurrentOwner as PlayerController);
        }
        private void BreakStealthOnSteal(PlayerController arg1, ShopItemController arg2) { this.BreakStealth(arg1); }
        private void BreakStealth(PlayerController player)
        {
            player.ChangeSpecialShaderFlag(1, 0f);
            player.OnItemStolen -= this.BreakStealthOnSteal;
            player.SetIsStealthed(false, "woodenhorse");
            player.healthHaver.OnDamaged -= this.OnDamaged;
            player.SetCapableOfStealing(false, "woodenhorse", null);
            player.OnDidUnstealthyAction -= this.BreakStealth;
            AkSoundEngine.PostEvent("Play_ENM_wizardred_appear_01", base.gameObject);
        }
        public WoodenHorse()
        {

        }
    }
}
