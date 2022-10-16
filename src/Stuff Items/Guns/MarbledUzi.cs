using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using Dungeonator;
using GungeonAPI;
using Alexandria.ItemAPI;

namespace NevernamedsItems
{
    public class MarbledUzi : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Marbled Uzi", "marbleduzi");
            Game.Items.Rename("outdated_gun_mods:marbled_uzi", "nn:marbled_uzi");
            var behav = gun.gameObject.AddComponent<MarbledUzi>();
            behav.preventNormalReloadAudio = true;
            behav.overrideNormalReloadAudio = "Play_ENM_gorgun_gaze_01";
            gun.SetShortDescription("At First I Was Afraid");
            gun.SetLongDescription("Favoured sidearm of Meduzi, the fearsome Gorgun. Legends say she emerged from her egg already clutching it's cold steel."+"\n\nReleases a stunning wave upon reloading, and deals more damage to stunned enemies.");

            gun.SetupSprite(null, "marbleduzi_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 16);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(673) as Gun).gunSwitchGroup;

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.reloadAnimation).wrapMode = tk2dSpriteAnimationClip.WrapMode.LoopSection;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.reloadAnimation).loopStart = 2;
            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Automatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1.1f;
            gun.DefaultModule.cooldownTime = 0.05f;
            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(97) as Gun).muzzleFlashEffects;
            gun.DefaultModule.numberOfShotsInClip = 80;
            gun.barrelOffset.transform.localPosition = new Vector3(1.56f, 1.0f, 0f);
            gun.SetBaseMaxAmmo(2000);
            gun.ammo = 2000;
            gun.gunClass = GunClass.FULLAUTO;
            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(23) as Gun).muzzleFlashEffects;

            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage = 3f;

            SelectiveDamageMult dmgup = projectile.gameObject.GetOrAddComponent<SelectiveDamageMult>();
            dmgup.multiplier = 2;
            dmgup.multOnStunnedEnemies = true;

            gun.quality = PickupObject.ItemQuality.S;
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            MarbledUziID = gun.PickupObjectId;



            foreach (WeightedRoom wRoom in StaticReferences.RoomTables["gorgun"].includedRooms.elements)
            {
                if (wRoom.room != null)
                {
                    if (wRoom.room.placedObjects != null)
                    {
                        foreach (var placeable in wRoom.room.placedObjects)
                        {
                            if (placeable.nonenemyBehaviour != null)
                            {
                                if (placeable.nonenemyBehaviour.gameObject != null && !string.IsNullOrEmpty(placeable.nonenemyBehaviour.gameObject.name))
                                {
                                    if (placeable.nonenemyBehaviour.gameObject.name.Contains("Bullet"))
                                    {
                                        stoneBulletKin.Add(placeable.nonenemyBehaviour.gameObject);
                                    }
                                    if (placeable.nonenemyBehaviour.gameObject.name.Contains("Shotgun"))
                                    {
                                        stoneShotgunKin.Add(placeable.nonenemyBehaviour.gameObject);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        public static List<GameObject> stoneBulletKin = new List<GameObject>();
        public static List<GameObject> stoneShotgunKin = new List<GameObject>();
        Vector2 lastCenter = Vector2.zero;
        public override void OnReloadPressed(PlayerController player, Gun gun, bool manualReload)
        {
            if (gun.ClipShotsRemaining != gun.ClipCapacity)
            {
                if (!hasScreamed)
                {
                    this.m_timer = 1.5f - BraveTime.DeltaTime;
                    this.m_prevWaveDist = 0f;
                    hasScreamed = true;
                    Vector2 center = gun.sprite.WorldCenter;
                    Exploder.DoDistortionWave(center, 0.5f, 0.04f, 20f, 1.5f);
                    lastCenter = center;
                    /*Vector3 vector = GameManager.Instance.MainCameraController.GetComponent<Camera>().WorldToViewportPoint(center.ToVector3ZUp(0f));
                    Vector4 vec4 = new Vector4(vector.x, vector.y, distRadius, distIntensity);

                    Material m_distortionMaterial = new Material(ShaderCache.Acquire("Brave/Internal/DistortionWave"));
                    m_distortionMaterial.SetVector("_WaveCenter", vec4);
                    Pixelator.Instance.RegisterAdditionalRenderPass(m_distortionMaterial);
                    StartCoroutine(HandleDistort(gun, m_distortionMaterial));*/
                }
            }
            base.OnReloadPressed(player, gun, manualReload);
        }
        /*private IEnumerator HandleDistort(Gun gun, Material distort)
        {
            float currentRadius = 0f;
            while (currentRadius < 1)
            {
                currentRadius += BraveTime.DeltaTime;

                Vector2 center = gun.sprite.WorldCenter;
                Vector3 vector = GameManager.Instance.MainCameraController.GetComponent<Camera>().WorldToViewportPoint(center.ToVector3ZUp(0f));
                Vector4 vec4 = new Vector4(vector.x, vector.y, distRadius, distIntensity);

                distort.SetVector("_WaveCenter", vec4);
                distort.SetFloat("_DistortProgress", currentRadius);

                yield return null;
            }
            if (Pixelator.Instance != null && distort != null)
            {
                Pixelator.Instance.DeregisterAdditionalRenderPass(distort);
                UnityEngine.Object.Destroy(distort);
            }
            yield break;
        }
        private static float distIntensity = 0.5f;
        private static float distRadius = 1f;*/
        protected override void Update()
        {
            if (gun.CurrentOwner && gun.GunPlayerOwner())
            {
                if (m_timer > 0)
                {
                    this.m_timer -= BraveTime.DeltaTime;
                    float num = BraveMathCollege.LinearToSmoothStepInterpolate(0f, 20f, 1f - this.m_timer / 1.5f);

                    List<AIActor> activeEnemies = gun.GunPlayerOwner().CurrentRoom.GetActiveEnemies(RoomHandler.ActiveEnemyType.All);

                    if (activeEnemies != null)
                    {
                        for (int i = 0; i < activeEnemies.Count; i++)
                        {
                            AIActor aiactor = activeEnemies[i];

                            Vector2 unitCenter = aiactor.specRigidbody.GetUnitCenter(ColliderType.HitBox);

                            float num2 = Vector2.Distance(unitCenter, lastCenter);
                            if (num2 >= this.m_prevWaveDist - 0.25f && num2 <= num + 0.25f)
                            {
                                aiactor.behaviorSpeculator.Stun(2f);
                                if ((aiactor.healthHaver && aiactor.healthHaver.GetCurrentHealthPercentage() < 0.5f) || gun.GunPlayerOwner().PlayerHasActiveSynergy("Gorgun's Gaze"))
                                {
                                    if (aiactor.HasTag("shotgun_kin"))
                                    {
                                        UnityEngine.Object.Instantiate<GameObject>((PickupObjectDatabase.GetById(37) as Gun).DefaultModule.chargeProjectiles[0].Projectile.hitEffects.overrideMidairDeathVFX, aiactor.specRigidbody.UnitCenter, Quaternion.identity);
                                        aiactor.EraseFromExistenceWithRewards();
                                        UnityEngine.Object.Instantiate<GameObject>(BraveUtility.RandomElement(stoneShotgunKin), aiactor.specRigidbody.UnitBottomLeft, Quaternion.identity);
                                    }
                                    else if (ValidBulletKin.Contains(aiactor.EnemyGuid))
                                    {
                                        UnityEngine.Object.Instantiate<GameObject>((PickupObjectDatabase.GetById(37) as Gun).DefaultModule.chargeProjectiles[0].Projectile.hitEffects.overrideMidairDeathVFX, aiactor.specRigidbody.UnitCenter, Quaternion.identity);
                                        aiactor.EraseFromExistenceWithRewards();
                                       UnityEngine.Object.Instantiate<GameObject>(BraveUtility.RandomElement(stoneBulletKin), aiactor.specRigidbody.UnitBottomLeft, Quaternion.identity);
                                    }
                                }

                            }

                        }
                    }
                    this.m_prevWaveDist = num;
                }
            }
            base.Update();
            if (!gun.IsReloading && hasScreamed) hasScreamed = false;
        }
        private bool hasScreamed = false;
        private float m_timer;
        private float m_prevWaveDist;
        public static int MarbledUziID;
        public static List<string> ValidBulletKin = new List<string>()
        {
            EnemyGuidDatabase.Entries["bullet_kin"],
            EnemyGuidDatabase.Entries["ak47_bullet_kin"],
            EnemyGuidDatabase.Entries["bandana_bullet_kin"],
            EnemyGuidDatabase.Entries["veteran_bullet_kin"],
            EnemyGuidDatabase.Entries["treadnaughts_bullet_kin"],
            EnemyGuidDatabase.Entries["minelet"],
            EnemyGuidDatabase.Entries["cardinal"],
            EnemyGuidDatabase.Entries["ashen_bullet_kin"],
            EnemyGuidDatabase.Entries["mutant_bullet_kin"],
            EnemyGuidDatabase.Entries["fallen_bullet_kin"],
            EnemyGuidDatabase.Entries["office_bullet_kin"],
            EnemyGuidDatabase.Entries["office_bullette_kin"],
            EnemyGuidDatabase.Entries["brollet"],
            EnemyGuidDatabase.Entries["western_bullet_kin"],
            EnemyGuidDatabase.Entries["pirate_bullet_kin"],
            EnemyGuidDatabase.Entries["summoned_treadnaughts_bullet_kin"],
            EnemyGuidDatabase.Entries["gummy"],
        };
    }
}