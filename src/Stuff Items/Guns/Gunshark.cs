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

namespace NevernamedsItems
{

    public class Gunshark : GunBehaviour
    {
        public static int GunsharkID;


        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Gunshark", "gunshark");
            Game.Items.Rename("outdated_gun_mods:gunshark", "nn:gunshark");
            gun.gameObject.AddComponent<Gunshark>();
            gun.SetShortDescription("Completely Awesome");
            gun.SetLongDescription("A bullet shark that is also a gun, what more could you ask for?");

            gun.SetupSprite(null, "gunshark_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 17);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Automatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 2f;
            gun.DefaultModule.cooldownTime = 0.04f;
            gun.DefaultModule.numberOfShotsInClip = 200;
            gun.barrelOffset.transform.localPosition = new Vector3(1.68f, 0.43f, 0f);
            gun.SetBaseMaxAmmo(3996);
            gun.ammo = 3996;
            
            gun.gunClass = GunClass.FULLAUTO;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage *= 1f;
            projectile.baseData.speed *= 2f;
            projectile.pierceMinorBreakables = true;
            //projectile.shouldRotate = true;
            projectile.SetProjectileSpriteRight("gunshark_projectile", 17, 4, true, tk2dBaseSprite.Anchor.MiddleCenter, 17, 4);

            projectile.transform.parent = gun.barrelOffset;

            gun.quality = PickupObject.ItemQuality.A;
            ETGMod.Databases.Items.Add(gun, null, "ANY");
            GunsharkID = gun.PickupObjectId;
            gun.SetupUnlockOnCustomFlag(CustomDungeonFlags.JAMMEDBULLETSHARK_QUEST_REWARDED, true);
            gun.SetTag("non_companion_living_item");
        }
        public override void PostProcessProjectile(Projectile projectile)
        {
            PlayerController playerController = projectile.Owner as PlayerController;
            if (playerController.PlayerHasActiveSynergy("Blood In The Water")) projectile.OnHitEnemy += this.OnHitEnemy;
            base.PostProcessProjectile(projectile);
        }
        private void OnHitEnemy(Projectile bullet, SpeculativeRigidbody enemy, bool fatal)
        {
            //ETGModConsole.Log("Onhitenemy is triggering");
            try
            {
                if (fatal && UnityEngine.Random.value <= 0.05f && !enemy.healthHaver.IsBoss)
                {
                    //ETGModConsole.Log("We're passing the check");
                    enemy.aiActor.EraseFromExistenceWithRewards();
                    float zRotation = 0;
                    for (int i = 0; i < 8; i++)
                    {
                        PlayerController playerController = bullet.Owner as PlayerController;
                        Projectile proj = ((Gun)PickupObjectDatabase.GetById(359)).DefaultModule.chargeProjectiles[0].Projectile;

                        GameObject gameObject = SpawnManager.SpawnProjectile(proj.gameObject, bullet.sprite.WorldCenter, Quaternion.Euler(0f, 0f, zRotation), true);

                        Projectile component = gameObject.GetComponent<Projectile>();
                        if (component)
                        {
                            PierceProjModifier pierce = component.gameObject.GetOrAddComponent<PierceProjModifier>();
                            component.SpawnedFromOtherPlayerProjectile = true;
                            component.baseData.damage *= playerController.stats.GetStatValue(PlayerStats.StatType.Damage);
                            component.baseData.speed *= playerController.stats.GetStatValue(PlayerStats.StatType.ProjectileSpeed);
                            component.AdditionalScaleMultiplier *= playerController.stats.GetStatValue(PlayerStats.StatType.PlayerBulletScale);
                            playerController.DoPostProcessProjectile(component);
                        }
                        zRotation += 45f;
                    }
                }
            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
                ETGModConsole.Log(e.StackTrace);
            }
        }
        public override void OnPostFired(PlayerController player, Gun gun)
        {

        }


        //All that's left now is sprite stuff. 
        //Your sprites should be organized, like how you see in the mod folder. 
        //Every gun requires that you have a .json to match the sprites or else the gun won't spawn at all
        //.Json determines the hand sprites for your character. You can make a gun two handed by having both "SecondaryHand" and "PrimaryHand" in the .json file, which can be edited through Notepad or Visual Studios
        //By default this gun is a one-handed weapon
        //If you need a basic two handed .json. Just use the jpxfrd2.json.
        //And finally, don't forget to add your Gun to your ETGModule class!

        public Gunshark()
        {

        }
    }
}
