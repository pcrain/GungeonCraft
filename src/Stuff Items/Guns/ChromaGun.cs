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
    public class ChromaGun : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("ChromaGun", "chromagun");
            Game.Items.Rename("outdated_gun_mods:chromagun", "nn:chromagun");
            gun.gameObject.AddComponent<ChromaGun>();
            gun.SetShortDescription("");
            gun.SetLongDescription("");

            gun.SetupSprite(null, "chromagun_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 15);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 0.85f;
            gun.DefaultModule.cooldownTime = 0.4f;
            gun.muzzleFlashEffects.type = VFXPoolType.None;
            gun.DefaultModule.numberOfShotsInClip = 5;
            gun.barrelOffset.transform.localPosition = new Vector3(1.25f, 0.62f, 0f);
            gun.SetBaseMaxAmmo(150);

            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage = 7f;
            projectile.baseData.speed *= 3f;
            projectile.gameObject.AddComponent<ChromaGunBulletBehav>();
            projectile.transform.parent = gun.barrelOffset;
            gun.quality = PickupObject.ItemQuality.EXCLUDED;
            ETGMod.Databases.Items.Add(gun, false, "ANY");
            ChromaGunId = gun.PickupObjectId;
            gun.gunClass = GunClass.SILLY;
            SetupChromaDroids();
        }
        private ColourType CurrentColourFiringMode = ColourType.RED;
        public static int ChromaGunId;

        public static GameObject redDroidPrefab;
        public static GameObject yellowDroidPrefab;
        public static GameObject blueDroidPrefab;

        private GameObject extantRedDroid; //Tom
        private GameObject extantYellowDroid; //Dick
        private GameObject extantBlueDroid; //Harry

        private static readonly string RedDroidGuid = "tom93279832647466348743748";
        private static readonly string YellowDroidGuid = "dick0001191029210190129109";
        private static readonly string BlueDroidGuid = "harry152347362562232323532";
        public override void PostProcessProjectile(Projectile projectile)
        {
            if (projectile.GetComponent<ChromaGunBulletBehav>())
            {
                ChromaGunBulletBehav behav = projectile.GetComponent<ChromaGunBulletBehav>();
                StartCoroutine(LateUpdateProjColour(behav));
            }
            base.PostProcessProjectile(projectile);
        }
        private IEnumerator LateUpdateProjColour(ChromaGunBulletBehav behav)
        {
            yield return null;
            behav.ColourType = CurrentColourFiringMode;
            behav.ChangeColour();
            yield break;
        }
        public override void OnReloadPressed(PlayerController player, Gun gun, bool manualReload)
        {
            if (CurrentColourFiringMode == ColourType.RED)
            {
                CurrentColourFiringMode = ColourType.YELLOW;
                VFXToolbox.DoStringSquirt("YELLOW", gun.sprite.WorldCenter, ExtendedColours.honeyYellow);
            }
            else if (CurrentColourFiringMode == ColourType.YELLOW)
            {
                CurrentColourFiringMode = ColourType.BLUE;
                VFXToolbox.DoStringSquirt("BLUE", gun.sprite.WorldCenter, Color.blue);
            }
            else if (CurrentColourFiringMode == ColourType.BLUE)
            {
                CurrentColourFiringMode = ColourType.RED;
                VFXToolbox.DoStringSquirt("RED", gun.sprite.WorldCenter, Color.red);
            }
            base.OnReloadPressed(player, gun, manualReload);
        }
        public override void OnSwitchedToThisGun()
        {
             RecalculateDrones();
            base.OnSwitchedToThisGun();
        }
        public override void OnSwitchedAwayFromThisGun()
        {
              RecalculateDrones();
            base.OnSwitchedAwayFromThisGun();
        }
        private float recalcTimer;
        private GameObject SpawnNewCompanion(string guid)
        {
            AIActor orLoadByGuid = EnemyDatabase.GetOrLoadByGuid(guid);
            Vector3 vector = Owner.transform.position;
            if (GameManager.Instance.CurrentLevelOverrideState == GameManager.LevelOverrideState.FOYER)
            {
                vector += new Vector3(1.125f, -0.3125f, 0f);
            }
            GameObject extantCompanion2 = UnityEngine.Object.Instantiate<GameObject>(orLoadByGuid.gameObject, vector, Quaternion.identity);
            CompanionController orAddComponent = extantCompanion2.GetOrAddComponent<CompanionController>();  
            orAddComponent.Initialize(gun.CurrentOwner as PlayerController);
            if (orAddComponent.specRigidbody)
            {
                PhysicsEngine.Instance.RegisterOverlappingGhostCollisionExceptions(orAddComponent.specRigidbody, null, false);
            }
           
            return extantCompanion2;
        }
        private void RecalculateDrones()
        {
            //Tom
            if (extantRedDroid != null && !gun.IsCurrentGun())
            {
                UnityEngine.Object.Destroy(extantRedDroid);
                extantRedDroid = null;
            }
            else if (extantRedDroid == null && gun.IsCurrentGun())
            {
                extantRedDroid = SpawnNewCompanion(RedDroidGuid);
            }
            //Dick
            if (extantYellowDroid != null && !gun.IsCurrentGun())
            {
                Destroy(extantYellowDroid);
                extantYellowDroid = null;
            }
            else if (extantYellowDroid == null && gun.IsCurrentGun())
            {

            }
            //Harry
            if (extantBlueDroid != null && !gun.IsCurrentGun())
            {
                Destroy(extantBlueDroid);
                extantBlueDroid = null;
            }
            else if (extantBlueDroid == null && gun.IsCurrentGun())
            {

            }

        }
        protected override void Update()
        {
            if (Owner)
            {
                if (recalcTimer > 0)
                {
                    recalcTimer -= BraveTime.DeltaTime;
                }
                else if (recalcTimer <= 0)
                {
                    // RecalculateDrones();
                    recalcTimer = 1;
                }
            }
            base.Update();
        }
        protected override void OnPickedUpByPlayer(PlayerController player)
        {
            recalcTimer = 1;
            base.OnPickedUpByPlayer(player);
        }
        public static void SetupChromaDroids()
        {
            //TOM
            #region SetupTom
            if (redDroidPrefab == null || !CompanionBuilder.companionDictionary.ContainsKey(RedDroidGuid))
            {
                redDroidPrefab = CompanionBuilder.BuildPrefab("ChromaGun Red Droid", RedDroidGuid, "NevernamedsItems/Resources/Companions/ChromaGunDroids/chromadroid_red_idle_001", new IntVector2(5, 1), new IntVector2(6, 6));
                var companionController = redDroidPrefab.AddComponent<CompanionController>();
                companionController.aiActor.MovementSpeed = 6.5f;
                companionController.CanCrossPits = true;
                companionController.aiActor.CollisionDamage = 0;
                companionController.aiActor.ActorShadowOffset = new Vector3(0, -0.5f);
                redDroidPrefab.AddAnimation("idle", "NevernamedsItems/Resources/Companions/ChromaGunDroids/chromadroid_red_idle", 12, CompanionBuilder.AnimationType.Idle, DirectionalAnimation.DirectionType.None, DirectionalAnimation.FlipType.None);
                BehaviorSpeculator component = redDroidPrefab.GetComponent<BehaviorSpeculator>();
                CustomCompanionBehaviours.SimpleCompanionMeleeAttack redDroidAttack = new CustomCompanionBehaviours.SimpleCompanionMeleeAttack();
                redDroidAttack.DamagePerTick = 5;
                redDroidAttack.DesiredDistance = 2;
                redDroidAttack.TickDelay = 1f;
                redDroidAttack.selfKnockbackAmount = 10;
                redDroidAttack.targetKnockbackAmount = 10;
                CustomCompanionBehaviours.ChromaGunDroneApproach redDroidApproach = new CustomCompanionBehaviours.ChromaGunDroneApproach();
                redDroidApproach.DesiredDistance = 1;
                redDroidApproach.droneColour = ColourType.RED;
                component.MovementBehaviors.Add(redDroidApproach);
                component.AttackBehaviors.Add(redDroidAttack);
                component.MovementBehaviors.Add(new CompanionFollowPlayerBehavior { IdleAnimations = new string[] { "idle" } });
            }
            #endregion
        }
        public static List<ColourType> RandomBaseColours = new List<ColourType>()
        {
            ColourType.RED,
            ColourType.YELLOW,
            ColourType.BLUE,
        };
        public enum ColourType
        {
            NONE,
            //CORE COLOURS
            RED,
            YELLOW,
            BLUE,
            //MIXED COLOURS
            PURPLE,
            GREEN,
            ORANGE,
            //BLACK
            BLACK,
        }
        public ChromaGun()
        {

        }
        public class ChromaGunBulletBehav : MonoBehaviour
        {
            public ChromaGunBulletBehav()
            {
                ColourType = ColourType.NONE;
            }
            private void Start()
            {
                self = base.GetComponent<Projectile>();
                if (self.PossibleSourceGun == null || ((self.PossibleSourceGun != null) && (self.PossibleSourceGun.PickupObjectId != ChromaGunId))) ColourType = BraveUtility.RandomElement(RandomBaseColours);
                self.OnHitEnemy += this.OnHitEnemy;

               if (ColourType != ColourType.NONE) ChangeColour();
            }
            public void ChangeColour()
            {
                self.AdjustPlayerProjectileTint(actualColours[ColourType], 1);

            /*    EasyTrailBullet trail = self.gameObject.GetOrAddComponent<EasyTrailBullet>();
                trail.TrailPos = self.transform.position;
                trail.StartWidth = 0.31f;
                trail.EndWidth = 0;
                trail.LifeTime = 0.3f;
                trail.BaseColor = actualColours[ColourType];
                trail.EndColor = actualColours[ColourType];*/
            }
            private void OnHitEnemy(Projectile self, SpeculativeRigidbody enemy, bool fatal)
            {
                if (self && (enemy != null) && (enemy.aiActor != null) && (enemy.healthHaver != null) && (enemy.gameObject != null))
                {
                    if (!fatal)
                    {
                        ChromaGunColoured colourdness = enemy.gameObject.GetOrAddComponent<ChromaGunColoured>();
                        colourdness.AddColour(ColourType);
                    }
                }
            }
            private Projectile self;
            public ChromaGun.ColourType ColourType;
        }
        public class ChromaGunColoured : MonoBehaviour
        {
            public ChromaGunColoured()
            {
                ColourType = ColourType.NONE;
            }
            private void Start()
            {
                self = base.GetComponent<AIActor>();
            }
            public void AddColour(ChromaGun.ColourType colour)
            {
                if (colour == ColourType) return;
                if (colour == ColourType.NONE) return;
                if (ColourType == ColourType.BLACK) return;

                //Colour Mixing
                ColourType finalColour = ColourType.NONE;
                if (ColourType != ColourType.NONE)
                {
                    if (colour == ColourType.RED)
                    {
                        if (ColourType == ColourType.YELLOW) finalColour = ColourType.ORANGE;
                        else if (ColourType == ColourType.BLUE) finalColour = ColourType.PURPLE;
                        else finalColour = ColourType.BLACK;
                    }
                    else if (colour == ColourType.YELLOW)
                    {
                        if (ColourType == ColourType.RED) finalColour = ColourType.ORANGE;
                        else if (ColourType == ColourType.BLUE) finalColour = ColourType.GREEN;
                        else finalColour = ColourType.BLACK;
                    }
                    else if (colour == ColourType.BLUE)
                    {
                        if (ColourType == ColourType.YELLOW) finalColour = ColourType.GREEN;
                        else if (ColourType == ColourType.RED) finalColour = ColourType.PURPLE;
                        else finalColour = ColourType.BLACK;
                    }
                }
                else finalColour = colour;

                //Actually set the enemy colour to the specified colour
                if (ColourType != ColourType.NONE) self.DeregisterOverrideColor("ChromaGunTint");
                self.gameActor.RegisterOverrideColor(actualColours[finalColour], "ChromaGunTint");
                ColourType = finalColour;
            }
            private AIActor self;
            public ColourType ColourType;
        }
        public static Dictionary<ColourType, Color> actualColours = new Dictionary<ColourType, Color>()
        {
            {ColourType.RED, Color.red },
            {ColourType.YELLOW, ExtendedColours.honeyYellow },
            {ColourType.BLUE, Color.blue },

            {ColourType.PURPLE, ExtendedColours.purple },
            {ColourType.GREEN, Color.green },
            {ColourType.ORANGE, ExtendedColours.vibrantOrange },

            {ColourType.BLACK, ExtendedColours.darkBrown },
        };
    }
}