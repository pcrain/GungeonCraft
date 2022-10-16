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
    public class TheThinLine : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("The Thin Line", "thinline");
            Game.Items.Rename("outdated_gun_mods:the_thin_line", "nn:the_thin_line");
            gun.gameObject.AddComponent<TheThinLine>();
            gun.SetShortDescription("Scienced To The Max");
            gun.SetLongDescription("A slimmed down, pocket version of the tachyon projectile emmitter known as the Fat Line." + "\n\nIt's projectiles defy each other, and have volatile effects upon meeting.");

            gun.SetupSprite(null, "thinline_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 10);

            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(562) as Gun).gunSwitchGroup;
            for (int i = 0; i < 2; i++)
            {
                gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);
            }

            //GUN STATS
            gun.reloadTime = 1f;
            gun.barrelOffset.transform.localPosition = new Vector3(1.37f, 0.87f, 0f);
            gun.SetBaseMaxAmmo(260);
            gun.ammo = 200;
            gun.gunClass = GunClass.PISTOL;
            foreach (ProjectileModule mod in gun.Volley.projectiles)
            {
                mod.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
                mod.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
                mod.cooldownTime = 0.20f;
                mod.numberOfShotsInClip = 6;
                mod.angleVariance = 0f;

                if (mod != gun.DefaultModule)
                {
                    Projectile projectile = DataCloners.CopyFields<TachyonProjectile>(Instantiate(mod.projectiles[0]));
                    projectile.gameObject.SetActive(false);
                    FakePrefab.MarkAsFakePrefab(projectile.gameObject);
                    UnityEngine.Object.DontDestroyOnLoad(projectile);
                    projectile.baseData.damage *= 2f;
                    projectile.baseData.speed *= 0.5f;
                    projectile.pierceMinorBreakables = true;
                    ThinLineCollidee collidee = projectile.gameObject.GetOrAddComponent<ThinLineCollidee>();
                    mod.ammoCost = 0;
                    projectile.AnimateProjectile(new List<string> {
                "thinline_pinkproj_001",
                "thinline_pinkproj_002",
                "thinline_pinkproj_003",
                "thinline_pinkproj_004",
                "thinline_pinkproj_005",
                "thinline_pinkproj_006",
            }, 10, true, new List<IntVector2> {
                new IntVector2(10, 10),
                new IntVector2(10, 10),
                new IntVector2(10, 10),
                new IntVector2(10, 10),
                new IntVector2(10, 10),
                new IntVector2(10, 10),
            }, AnimateBullet.ConstructListOfSameValues(true, 6), AnimateBullet.ConstructListOfSameValues(tk2dBaseSprite.Anchor.MiddleCenter, 6), AnimateBullet.ConstructListOfSameValues(true, 6), AnimateBullet.ConstructListOfSameValues(false, 6),
            AnimateBullet.ConstructListOfSameValues<Vector3?>(null, 6), AnimateBullet.ConstructListOfSameValues<IntVector2?>(null, 6), AnimateBullet.ConstructListOfSameValues<IntVector2?>(null, 6), AnimateBullet.ConstructListOfSameValues<Projectile>(null, 6));
                    mod.projectiles[0] = projectile;

                }
                else
                {
                    Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(mod.projectiles[0]);
                    projectile.gameObject.SetActive(false);
                    FakePrefab.MarkAsFakePrefab(projectile.gameObject);
                    UnityEngine.Object.DontDestroyOnLoad(projectile);
                    projectile.baseData.damage *= 2f;
                    projectile.baseData.speed *= 0.5f;
                    projectile.AnimateProjectile(new List<string> {
                "thinline_blueproj_001",
                "thinline_blueproj_002",
                "thinline_blueproj_003",
                "thinline_blueproj_004",
                "thinline_blueproj_005",
                "thinline_blueproj_006",
            }, 10, true, new List<IntVector2> {
                new IntVector2(10, 10),
                new IntVector2(10, 10),
                new IntVector2(10, 10),
                new IntVector2(10, 10),
                new IntVector2(10, 10),
                new IntVector2(10, 10),
            }, AnimateBullet.ConstructListOfSameValues(true, 6), AnimateBullet.ConstructListOfSameValues(tk2dBaseSprite.Anchor.MiddleCenter, 6), AnimateBullet.ConstructListOfSameValues(true, 6), AnimateBullet.ConstructListOfSameValues(false, 6),
           AnimateBullet.ConstructListOfSameValues<Vector3?>(null, 6), AnimateBullet.ConstructListOfSameValues<IntVector2?>(null, 6), AnimateBullet.ConstructListOfSameValues<IntVector2?>(null, 6), AnimateBullet.ConstructListOfSameValues<Projectile>(null, 6));
                    mod.ammoCost = 1;
                    ThinLineCollision collider = projectile.gameObject.GetOrAddComponent<ThinLineCollision>();
                    mod.projectiles[0] = projectile;

                }
            }
            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("Thinline Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/thinline_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/thinline_clipempty");

            gun.quality = PickupObject.ItemQuality.B;
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            gun.SetupUnlockOnCustomFlag(CustomDungeonFlags.PURCHASED_THETHINLINE, true);
            gun.AddItemToDougMetaShop(25);
            ID = gun.PickupObjectId;
        }
        public static int ID;
        public static ExplosionData DataForProjectiles = null;
        protected override void OnPickedUpByPlayer(PlayerController player)
        {
            if (DataForProjectiles == null)
            {
                DataForProjectiles = DataCloners.CopyExplosionData(StaticExplosionDatas.explosiveRoundsExplosion);
            }
            if (!DataForProjectiles.ignoreList.Contains(player.specRigidbody)) DataForProjectiles.ignoreList.Add(player.specRigidbody);
            base.OnPickedUpByPlayer(player);
        }
        public TheThinLine()
        {

        }

    }
    public class ThinLineCollision : MonoBehaviour
    {
        public ThinLineCollision()
        {

        }
        public void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            if (this.m_projectile.Owner is PlayerController) this.owner = this.m_projectile.Owner as PlayerController;
            GameManager.Instance.StartCoroutine(delayedAssign());
        }
        public IEnumerator delayedAssign()
        {
            yield return null;
            this.m_projectile.collidesWithProjectiles = true;
            this.m_projectile.collidesOnlyWithPlayerProjectiles = true;
            this.m_projectile.UpdateCollisionMask();
            this.m_projectile.specRigidbody.OnPreRigidbodyCollision += this.OnCollision;
            //ETGModConsole.Log("DelayedAssigned the Collision");
            yield break;
        }
        private void OnCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
        {
            try
            {
                if (otherRigidbody && otherRigidbody.projectile)
                {
                    ThinLineCollidee shouldCollide = otherRigidbody.projectile.gameObject.GetComponent<ThinLineCollidee>();
                    if (shouldCollide != null)
                    {
                        DoOnCollisionEffect(myRigidbody.sprite.WorldCenter);
                    }
                    else
                    {
                        PhysicsEngine.SkipCollision = true;
                    }
                }
            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
                ETGModConsole.Log(e.StackTrace);
            }

        }
        private void DoOnCollisionEffect(Vector2 position)
        {
            if (this.owner)
            {
                if (UnityEngine.Random.value <= 0.5f && this.owner.PlayerHasActiveSynergy("Parallel Lines"))
                {
                    doMiniBlank(position);
                }
                else
                {                    

                    Exploder.Explode(position, TheThinLine.DataForProjectiles, Vector2.zero);
                }
            }
        }
        private void doMiniBlank(Vector2 position)
        {
            GameObject silencerVFX = (GameObject)ResourceCache.Acquire("Global VFX/BlankVFX_Ghost");
            AkSoundEngine.PostEvent("Play_OBJ_silenceblank_small_01", base.gameObject);
            GameObject gameObject = new GameObject("silencer");
            SilencerInstance silencerInstance = gameObject.AddComponent<SilencerInstance>();
            float additionalTimeAtMaxRadius = 0.25f;
            silencerInstance.TriggerSilencer(position, 25f, 5f, silencerVFX, 0f, 3f, 3f, 3f, 250f, 5f, additionalTimeAtMaxRadius, owner, false, false);
        }
        private Projectile m_projectile;
        private PlayerController owner;


    }
    public class ThinLineCollidee : MonoBehaviour
    {
        public ThinLineCollidee()
        {

        }
    }

}
