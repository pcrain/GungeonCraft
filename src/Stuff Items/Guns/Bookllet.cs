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

    public class Bookllet : AdvancedGunBehavior
    {


        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Bookllet", "bookllet");
            Game.Items.Rename("outdated_gun_mods:bookllet", "nn:bookllet");

            var behav = gun.gameObject.AddComponent<Bookllet>();
            behav.preventNormalFireAudio = true;
            behav.overrideNormalFireAudio = "Play_OBJ_book_drop_01";
            behav.preventNormalReloadAudio = true;
            behav.overrideNormalReloadAudio = "Play_ENM_book_blast_01";

            gun.SetShortDescription("Ammomancy For Beginners");
            gun.SetLongDescription("A powerful ammomantic textbook studied by Apprentice Gunjurers");

            gun.SetupSprite(null, "bookllet_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 18);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Automatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 0.7f;
            gun.DefaultModule.cooldownTime = 0.05f;
            gun.muzzleFlashEffects.type = VFXPoolType.None;
            gun.DefaultModule.numberOfShotsInClip = 20;
            gun.barrelOffset.transform.localPosition = new Vector3(0.93f, 0.87f, 0f);
            gun.SetBaseMaxAmmo(400);
            gun.gunClass = GunClass.FULLAUTO;

            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage *= 1f;
            projectile.baseData.speed *= 0.0001f;
            projectile.SetProjectileSpriteRight("yellow_enemystyle_projectile", 10, 10, true, tk2dBaseSprite.Anchor.MiddleCenter, 8, 8);
            projectile.transform.parent = gun.barrelOffset;
            BooklletTimedReAim timedReAim = projectile.gameObject.AddComponent<BooklletTimedReAim>();


            //RING BULLET
            Ringbullet = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(86) as Gun).DefaultModule.projectiles[0]);
            Ringbullet.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(Ringbullet.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(Ringbullet);
            Ringbullet.baseData.damage *= 1.4f;
            Ringbullet.baseData.speed *= 0.6f;
            Ringbullet.SetProjectileSpriteRight("yellow_enemystyle_projectile", 10, 10, true, tk2dBaseSprite.Anchor.MiddleCenter, 8, 8);
            Ringbullet.transform.parent = gun.barrelOffset;

            // Here we just set the quality of the gun and the "EncounterGuid", which is used by Gungeon to identify the gun.
            gun.quality = PickupObject.ItemQuality.B; //B
            gun.encounterTrackable.EncounterGuid = "this is the Bookllet";
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            BooklletID = gun.PickupObjectId;
        }
        public static int BooklletID;


        public static Projectile Ringbullet;
        public override void OnReloadPressed(PlayerController player, Gun gun, bool bSOMETHING)
        {
            if (ActiveBullets.Count > 0)
            {
                foreach (Projectile bullet in ActiveBullets)
                {
                    if (bullet)
                    {
                        Vector2 dirVec = bullet.GetVectorToNearestEnemy();
                        bullet.SendInDirection(dirVec, false, true);
                        bullet.baseData.speed *= 7500;
                        bullet.UpdateSpeed();
                        BulletsToRemoveFromActiveBullets.Add(bullet);
                    }
                }
                foreach (Projectile bullet in BulletsToRemoveFromActiveBullets)
                {
                    ActiveBullets.Remove(bullet);
                }
                BulletsToRemoveFromActiveBullets.Clear();
            }
            if (gun.ClipShotsRemaining == 0 && gun.CurrentAmmo != 0 && canReleaseRing)
            {
                GameManager.Instance.StartCoroutine(this.DoRingBullets(Ringbullet, gun));
                canReleaseRing = false;
                Invoke("HandleRingCooldown", 1f);
            }
            base.OnReloadPressed(player, gun, bSOMETHING);
        }
        bool canReleaseRing = true;
        private void HandleRingCooldown()
        {
            canReleaseRing = true;
        }
        private IEnumerator DoRingBullets(Projectile bullet, Gun gun)
        {
            yield return new WaitForSeconds(0.1f);
            if (bullet != null)
            {
                //TOP AND BOTTOM LINES PART 1
                this.SpawnProjectile(bullet, gun.sprite.WorldCenter, 90, null);
                this.SpawnProjectile(bullet, gun.sprite.WorldCenter, -90, null);
                yield return new WaitForSeconds(0.05f);
                //RING
                float spawnAngle = 0;
                for (int i = 0; i < 30; i++)
                {
                    this.SpawnProjectile(bullet, gun.sprite.WorldCenter, spawnAngle, null);
                    spawnAngle += 12;
                }
                yield return new WaitForSeconds(0.05f);
                //BOTTOM AND TOP LINES PART 2
                for (int i = 0; i < 2; i++)
                {
                    this.SpawnProjectile(bullet, gun.sprite.WorldCenter, 90, null);
                    this.SpawnProjectile(bullet, gun.sprite.WorldCenter, -90, null);
                    yield return new WaitForSeconds(0.05f);
                }
            }
            yield break;
        }
        private void SpawnProjectile(Projectile proj, Vector3 spawnPosition, float zRotation, SpeculativeRigidbody collidedRigidbody = null)
        {
            GameObject gameObject = SpawnManager.SpawnProjectile(proj.gameObject, spawnPosition, Quaternion.Euler(0f, 0f, zRotation), true);
            Projectile component = gameObject.GetComponent<Projectile>();
            if (component)
            {
                component.SpawnedFromOtherPlayerProjectile = false;
                PlayerController playerController = gun.CurrentOwner as PlayerController;
                if (playerController != null)
                {
                    component.baseData.damage *= playerController.stats.GetStatValue(PlayerStats.StatType.Damage);
                    component.baseData.speed *= playerController.stats.GetStatValue(PlayerStats.StatType.ProjectileSpeed);
                    playerController.DoPostProcessProjectile(component);
                }
            }
        }
        public override void PostProcessProjectile(Projectile projectile)
        {
            PlayerController player = projectile.Owner as PlayerController;
            if (player != null)
            {
                if (player.PlayerHasActiveSynergy("Advanced Ammomancy"))
                {
                    if (projectile.gameObject.GetComponent<BooklletTimedReAim>())
                    {
                        Destroy(projectile.gameObject.GetComponent<BooklletTimedReAim>());
                    }
                    ActiveBullets.Add(projectile);
                }
            }
            base.PostProcessProjectile(projectile);
        }
        public static List<Projectile> ActiveBullets = new List<Projectile>() { };
        public static List<Projectile> BulletsToRemoveFromActiveBullets = new List<Projectile>() { };
        public Bookllet()
        {

        }
    }
    public class BooklletTimedReAim : MonoBehaviour
    {
        public BooklletTimedReAim()
        {
        }
        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            Invoke("ChangeSpeedAndReAim", 1.5f);
        }
        private void ChangeSpeedAndReAim()
        {
            Vector2 dirVec = m_projectile.GetVectorToNearestEnemy();
            m_projectile.SendInDirection(dirVec, false, true);
            m_projectile.baseData.speed *= 7500;
            m_projectile.UpdateSpeed();
        }

        private Projectile m_projectile;
    }
}