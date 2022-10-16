using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using ItemAPI;
using SaveAPI;

namespace NevernamedsItems
{
    public class Demolitionist : GunBehaviour
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Demolitionist", "demolitionist");
            Game.Items.Rename("outdated_gun_mods:demolitionist", "nn:demolitionist");
            gun.gameObject.AddComponent<Demolitionist>();
            gun.SetShortDescription("Up in Smoke!");
            gun.SetLongDescription("Reloading on a full clip consumes some ammo and places a proximity mine." + "\n\nAn old Hegemony of Man weapon, repurposed by Minelets for blasting open ore deposits in the mines.");

            gun.SetupSprite(null, "demolitionist_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 12);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Automatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.DefaultModule.cooldownTime = 0.3f;
            gun.DefaultModule.numberOfShotsInClip = 20;
            gun.barrelOffset.transform.localPosition = new Vector3(1.75f, 0.81f, 0f);
            gun.SetBaseMaxAmmo(300);
            gun.gunClass = GunClass.FULLAUTO;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage *= 1.8f;
            projectile.baseData.speed *= 1f;
            projectile.baseData.range *= 1f;
            projectile.SetProjectileSpriteRight("demolitionist_projectile", 16, 7, false, tk2dBaseSprite.Anchor.MiddleCenter, 15, 6);

            projectile.transform.parent = gun.barrelOffset;

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("Demolitionist Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/demolitionist_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/thinline_clipempty");

            gun.quality = PickupObject.ItemQuality.B;
            gun.encounterTrackable.EncounterGuid = "this is the Demolitionist";
            ETGMod.Databases.Items.Add(gun, null, "ANY");
            DemolitionistID = gun.PickupObjectId;
            gun.SetupUnlockOnCustomFlag(CustomDungeonFlags.PURCHASED_DEMOLITIONIST, true);
        }
        public static int DemolitionistID;
        public override void OnReloadPressed(PlayerController player, Gun gun, bool bSOMETHING)
        {
            base.OnReloadPressed(player, gun, bSOMETHING);
            if ((gun.ClipCapacity == gun.ClipShotsRemaining) || (gun.CurrentAmmo == gun.ClipShotsRemaining))
            {
                if (gun.CurrentAmmo >= 5)
                {
                    gun.CurrentAmmo -= 5;
                    SpawnObjectPlayerItem bigbombPrefab = PickupObjectDatabase.GetById(66).GetComponent<SpawnObjectPlayerItem>();
                    GameObject bigbombObject = bigbombPrefab.objectToSpawn.gameObject;

                    GameObject bigbombObject2 = UnityEngine.Object.Instantiate<GameObject>(bigbombObject, player.sprite.WorldBottomCenter, Quaternion.identity);
                    tk2dBaseSprite bombsprite = bigbombObject2.GetComponent<tk2dBaseSprite>();
                    if (bombsprite)
                    {
                        bombsprite.PlaceAtPositionByAnchor(player.sprite.WorldBottomCenter, tk2dBaseSprite.Anchor.MiddleCenter);
                    }
                }
            }
            else if (gun.ClipShotsRemaining == 0 && gun.CurrentAmmo != 0)
            {
                if (player.PlayerHasActiveSynergy("Demolition Man"))
                {
                    SpawnObjectPlayerItem bigbombPrefab = PickupObjectDatabase.GetById(66).GetComponent<SpawnObjectPlayerItem>();
                    GameObject bigbombObject = bigbombPrefab.objectToSpawn.gameObject;

                    GameObject bigbombObject2 = UnityEngine.Object.Instantiate<GameObject>(bigbombObject, player.sprite.WorldBottomCenter, Quaternion.identity);
                    tk2dBaseSprite bombsprite = bigbombObject2.GetComponent<tk2dBaseSprite>();
                    if (bombsprite)
                    {
                        bombsprite.PlaceAtPositionByAnchor(player.sprite.WorldBottomCenter, tk2dBaseSprite.Anchor.MiddleCenter);
                    }
                }
            }
        }
        public Demolitionist()
        {

        }
    }
}
