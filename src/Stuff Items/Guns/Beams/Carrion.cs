using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using Alexandria.ItemAPI;
using System.Reflection;
using Alexandria.Misc;

namespace NevernamedsItems
{
    public class Carrion : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("CARRION", "carrion2");
            Game.Items.Rename("outdated_gun_mods:carrion", "nn:carrion");
            var behav = gun.gameObject.AddComponent<Carrion>();
            behav.preventNormalFireAudio = true;
            gun.SetShortDescription("Fresh Meat");
            gun.SetLongDescription("A wormlike colony of bizarre organisms. Unknown origin." + "\n\nIt has three base desires. \nTo grow. \nTo Spread. \nTo Feed.");

            gun.SetupSprite(null, "carrion2_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 13);
            gun.SetAnimationFPS(gun.idleAnimation, 13);
            gun.isAudioLoop = true;
            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);

            //GUN STATS
            gun.doesScreenShake = false;
            gun.DefaultModule.ammoCost = 5;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Beam;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.muzzleFlashEffects.type = VFXPoolType.None;
            gun.DefaultModule.cooldownTime = 0.001f;
            gun.DefaultModule.numberOfShotsInClip = 600;
            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.BEAM;
            gun.barrelOffset.transform.localPosition = new Vector3(0.75f, 0.56f, 0f);
            gun.SetBaseMaxAmmo(600);
            gun.ammo = 600;
            gun.gunClass = GunClass.SILLY;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).wrapMode = tk2dSpriteAnimationClip.WrapMode.LoopSection;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).loopStart = 0;

            //SUB TENTACLE
            #region subtentacle
            Projectile subTendrilProj = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(86) as Gun).DefaultModule.projectiles[0]);

            BasicBeamController subTendrilComp = subTendrilProj.GenerateBeamPrefab(
                "NevernamedsItems/Resources/BeamSprites/carrionsubtendril_mid_001",
                new Vector2(4, 2),
                new Vector2(0, 1),
                new List<string>() { "NevernamedsItems/Resources/BeamSprites/carrionsubtendril_mid_001" },
                13,
                //Impact
                null,
                -1,
                null,
                null,
                //End
                new List<string>() { "NevernamedsItems/Resources/BeamSprites/carrionsubtendril_end_001" },
                13,
                new Vector2(6, 2),
                new Vector2(0, 1),
                //Beginning
                new List<string>() { "NevernamedsItems/Resources/BeamSprites/carrionsubtendril_start_001" },
                13,
                new Vector2(7, 2),
                new Vector2(0, 1),
                //Other Variables
                0
                );

            subTendrilProj.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(subTendrilProj.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(subTendrilProj);
            subTendrilProj.baseData.damage = 10f;
            subTendrilProj.baseData.force *= 1f;
            subTendrilProj.baseData.range = 3.5f;
            subTendrilComp.ProjectileAndBeamMotionModule = new HelixProjectileMotionModule();
            subTendrilComp.boneType = BasicBeamController.BeamBoneType.Projectile;
            subTendrilComp.penetration = 1;
            subTendrilComp.homingRadius = 10;
            subTendrilComp.homingAngularVelocity = 1000;
            CarrionSubTendrilController subtendril = subTendrilProj.gameObject.AddComponent<CarrionSubTendrilController>();
            #endregion

            //MAIN TENTACLE
            #region maintentacle
            List<string> BeamAnimPaths = new List<string>()
            {
                "NevernamedsItems/Resources/BeamSprites/carrion2_mid_001",
                "NevernamedsItems/Resources/BeamSprites/carrion2_mid_002",
                "NevernamedsItems/Resources/BeamSprites/carrion2_mid_003",
                "NevernamedsItems/Resources/BeamSprites/carrion2_mid_004",
                "NevernamedsItems/Resources/BeamSprites/carrion2_mid_005",
            };
            List<string> BeamEndPaths = new List<string>()
            {
                "NevernamedsItems/Resources/BeamSprites/carrion2_end_001",
                "NevernamedsItems/Resources/BeamSprites/carrion2_end_002",
                "NevernamedsItems/Resources/BeamSprites/carrion2_end_003",
                "NevernamedsItems/Resources/BeamSprites/carrion2_end_004",
                "NevernamedsItems/Resources/BeamSprites/carrion2_end_005",
            };

            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(86) as Gun).DefaultModule.projectiles[0]);

            BasicBeamController beamComp = projectile.GenerateBeamPrefab(
                "NevernamedsItems/Resources/BeamSprites/carrion2_mid_001",
                new Vector2(16, 5),
                new Vector2(0, 6),
                BeamAnimPaths,
                13,
                //Impact
                null,
                -1,
                null,
                null,
                //End
                BeamEndPaths,
                13,
                new Vector2(10, 5),
                new Vector2(0, 6),
                //Beginning
                null,
                -1,
                null,
                null,
                //Other Variables
                0
                );

            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            projectile.baseData.damage = 30f;
            projectile.baseData.force *= 1f;
            projectile.baseData.range = 8;
            projectile.baseData.speed *= 3;

            beamComp.boneType = BasicBeamController.BeamBoneType.Projectile;
            beamComp.startAudioEvent = "Play_WPN_demonhead_shot_01";
            beamComp.endAudioEvent = "Stop_WPN_All";
            beamComp.penetration = 1;
            beamComp.homingRadius = 5;
            beamComp.homingAngularVelocity = 300;

            CarrionMainTendrilController mainTendril = projectile.gameObject.AddComponent<CarrionMainTendrilController>();
            mainTendril.subTendrilPrefab = subTendrilProj.gameObject;

            gun.DefaultModule.projectiles[0] = projectile;
            #endregion

            //ESCAPING WIGGLER
            #region escapingwiggler
            Projectile wiggler = DataCloners.CopyFields<HelixProjectile>(Instantiate((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]));
            wiggler.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(wiggler.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(wiggler);
            wiggler.baseData.speed *= 1f;
            wiggler.baseData.damage = 20;
            wiggler.baseData.range *= 10f;
            HomingModifier homing = wiggler.gameObject.GetOrAddComponent<HomingModifier>();
            homing.HomingRadius = 10;
            homing.AngularVelocity = 200;
            PierceProjModifier piercing = wiggler.gameObject.GetOrAddComponent<PierceProjModifier>();
            piercing.penetratesBreakables = true;
            piercing.penetration++;
            wiggler.SetProjectileSpriteRight("carrion_wiggler", 5, 5, true, tk2dBaseSprite.Anchor.MiddleCenter, 5, 5);
            EasyTrailBullet trail = wiggler.gameObject.AddComponent<EasyTrailBullet>();
            trail.TrailPos = projectile.transform.position;
            trail.StartWidth = 0.31f;
            trail.EndWidth = 0;
            trail.LifeTime = 0.3f;
            trail.BaseColor = ExtendedColours.carrionRed;
            trail.EndColor = ExtendedColours.carrionRed;
            EscapingWigglerProjectile = wiggler;
            #endregion
            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = "Carrion Clip";

            gun.quality = PickupObject.ItemQuality.S; //S
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            CarrionForme1ID = gun.PickupObjectId;

        }
        public static int CarrionForme1ID;
        public static int CarrionForme2ID;
        public static int CarrionForme3ID;
        private int currentForme;
        private int enemiesKilledSinceTransform;
        public static Projectile EscapingWigglerProjectile;
        private void ModifyIncomingPlayerDamage(HealthHaver player, HealthHaver.ModifyDamageEventArgs args)
        {
            ETGModConsole.Log("ModifyIncomingDamage Ran");
            if (args.InitialDamage > 0)
            {
                if (currentForme > 1)
                {
                    ETGModConsole.Log("CurrentForme = " + currentForme);

                    args.ModifiedDamage = 0;
                    DoEscapingWigglies();
                    AkSoundEngine.PostEvent("Play_ENM_bombshee_scream_01", gun.gameObject);
                    if (currentForme == 2) SwitchForme(1);
                    if (currentForme == 3) SwitchForme(2);
                    ETGModConsole.Log("CurrentForme After = " + currentForme);
                    if (player.gameActor is PlayerController) (player.gameActor as PlayerController).TriggerInvulnerableFrames(1);
                }
            }
        }
        private void SwitchForme(int targetForme)
        {
            if ((targetForme > 0) && (targetForme < 4)) currentForme = targetForme;
            else return;
            enemiesKilledSinceTransform = 0;
            switch (targetForme)
            {
                case 1:
                    this.gun.TransformToTargetGun(PickupObjectDatabase.GetById(CarrionForme1ID) as Gun);
                    break;
                case 2:
                    this.gun.TransformToTargetGun(PickupObjectDatabase.GetById(CarrionForme2ID) as Gun);
                    break;
                case 3:
                    this.gun.TransformToTargetGun(PickupObjectDatabase.GetById(CarrionForme3ID) as Gun);
                    break;
            }
            int beamCount = ExtantBeams.Count();
            for (int i = (beamCount - 1); i >= 0; i--)
            {
                if (ExtantBeams[i] != null)
                {
                    ExtantBeams[i].CeaseAttack();
                }
            }
            ExtantBeams.Clear();
        }
        private void DoEscapingWigglies()
        {
            int amt = UnityEngine.Random.Range(5, 11);
            for (int i = 0; i < amt; i++)
            {
                GameObject gameObject = SpawnManager.SpawnProjectile(EscapingWigglerProjectile.gameObject, gun.GetComponent<tk2dSprite>().WorldCenter, Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(1, 360)), true);
                Projectile component = gameObject.GetComponent<Projectile>();
                if (component != null)
                {
                    component.Owner = gun.GunPlayerOwner();
                    component.Shooter = gun.GunPlayerOwner().specRigidbody;
                    component.baseData.damage *= gun.GunPlayerOwner().stats.GetStatValue(PlayerStats.StatType.Damage);
                    component.baseData.speed *= gun.GunPlayerOwner().stats.GetStatValue(PlayerStats.StatType.ProjectileSpeed);
                    component.baseData.force *= gun.GunPlayerOwner().stats.GetStatValue(PlayerStats.StatType.KnockbackMultiplier);
                    component.AdditionalScaleMultiplier *= gun.GunPlayerOwner().stats.GetStatValue(PlayerStats.StatType.PlayerBulletScale);
                    component.UpdateSpeed();
                    gun.GunPlayerOwner().DoPostProcessProjectile(component);
                }
            }
        }
        protected override void OnPickedUpByPlayer(PlayerController player)
        {
            if (!everPickedUpByPlayer) enemiesKilledSinceTransform = 0;
            player.healthHaver.ModifyDamage += this.ModifyIncomingPlayerDamage;
            base.OnPickedUpByPlayer(player);
        }
        protected override void OnPostDroppedByPlayer(PlayerController player)
        {
            player.healthHaver.ModifyDamage -= this.ModifyIncomingPlayerDamage;

            base.OnPostDroppedByPlayer(player);
        }
        public override void OnDestroy()
        {
            if (gun && gun.GunPlayerOwner())
            {
                gun.GunPlayerOwner().healthHaver.ModifyDamage -= this.ModifyIncomingPlayerDamage;

            }
            base.OnDestroy();
        }
        protected override void PostProcessBeam(BeamController beam)
        {
            beam.projectile.OnHitEnemy += this.OnHitEnemy;
            if (beam.GetComponent<CarrionMainTendrilController>())
            {
                beam.GetComponent<CarrionMainTendrilController>().forme = currentForme;
                ExtantBeams.Add(beam);
            }
            base.PostProcessBeam(beam);
        }
        private void OnHitEnemy(Projectile proj, SpeculativeRigidbody enemy, bool fatal)
        {
            if (proj && enemy && enemy.healthHaver)
            {
                if (fatal)
                {
                    enemiesKilledSinceTransform++;
                }
            }
        }
        public void FixedUpdate()
        {
            if (currentForme > 1 && gun.IsFiring)
            {
                float knockback = 2;
                if (currentForme == 3) knockback = 4;
                if (Owner)
                {
                    Owner.knockbackDoer.ApplyKnockback(gun.CurrentAngle.DegreeToVector2(), knockback);
                }
            }
        }
        private int enemieskilledlastCheck;
        protected override void Update()
        {
            if (currentForme == 0) currentForme = 1;
            if (currentForme == 1 && enemiesKilledSinceTransform >= 30)
            {
                SwitchForme(2);
            }
            if (currentForme == 2 && enemiesKilledSinceTransform >= 60)
            {
                SwitchForme(3);

            }
            if (enemieskilledlastCheck != enemiesKilledSinceTransform)
            {
                //ETGModConsole.Log("Enemies Killed: " + enemiesKilledSinceTransform);
                enemieskilledlastCheck = enemiesKilledSinceTransform;
            }
            base.Update();
        }
        private List<BeamController> ExtantBeams = new List<BeamController>();
        public Carrion()
        {

        }
    }
    public class CarrionMainTendrilController : MonoBehaviour
    {
        public CarrionMainTendrilController()
        {

        }
        public void Start()
        {
            this.forme = 1;
            this.projectileSelf = base.GetComponent<Projectile>();
            this.beamContollerSelf = base.GetComponent<BeamController>();
            this.basicBeamControllerSelf = base.GetComponent<BasicBeamController>();
            if (this.projectileSelf.Owner is PlayerController) this.Owner = this.projectileSelf.Owner as PlayerController;
        }
        public void Update()
        {
            if (basicBeamControllerSelf)
            {
                LinkedList<BasicBeamController.BeamBone> bones;
                bones = OMITBReflectionHelpers.ReflectGetField<LinkedList<BasicBeamController.BeamBone>>(typeof(BasicBeamController), "m_bones", basicBeamControllerSelf);
                if (bones != null)
                {
                    if (bones.Count > boneNumLastChecked)
                    {
                        if (UnityEngine.Random.value <= 0.5f)
                        {
                            SpawnSubTendril(bones.Count - 1);
                        }
                    }
                    boneNumLastChecked = bones.Count;
                }
            }
        }
        public void SpawnSubTendril(int boneIndex)
        {
            if (subTendrilPrefab != null)
            {
                BeamController controllerPrefab = subTendrilPrefab.GetComponent<BeamController>();
                if (controllerPrefab == null) { Debug.LogError("CarrionMainTendril: ControllerPrefab was NULL!"); }
                if (controllerPrefab is BasicBeamController)
                {
                    GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(controllerPrefab.gameObject);

                    BasicBeamController newBasicSubBeam = gameObject.GetComponent<BasicBeamController>();
                    newBasicSubBeam.State = BasicBeamController.BeamState.Firing;
                    newBasicSubBeam.HitsPlayers = false;
                    newBasicSubBeam.HitsEnemies = true;
                    newBasicSubBeam.Origin = basicBeamControllerSelf.GetIndexedBonePosition(boneIndex);
                    newBasicSubBeam.Direction = basicBeamControllerSelf.GetFinalBoneDirection().DegreeToVector2();
                    newBasicSubBeam.usesChargeDelay = false;
                    newBasicSubBeam.projectile.Owner = this.projectileSelf.Owner;
                    newBasicSubBeam.Owner = this.basicBeamControllerSelf.Owner;
                    newBasicSubBeam.Gun = this.basicBeamControllerSelf.Gun;

                    newBasicSubBeam.projectile.baseData.damage *= forme;

                    HelixProjectileMotionModule helixProjectileMotionModule = new HelixProjectileMotionModule();
                    if (UnityEngine.Random.value <= 0.5f) helixProjectileMotionModule.ForceInvert = true;
                    newBasicSubBeam.projectile.OverrideMotionModule = helixProjectileMotionModule;

                    CarrionSubTendrilController subTendril = gameObject.GetComponent<CarrionSubTendrilController>();
                    if (subTendril)
                    {
                        subTendril.parentBeam = basicBeamControllerSelf;
                        subTendril.parentBoneIndex = boneIndex;
                        subTendril.GetComponent<Projectile>().PossibleSourceGun = projectileSelf.PossibleSourceGun;
                    }
                }
            }
        }
        public GameObject subTendrilPrefab;
        public int forme;

        private int boneNumLastChecked;
        private Projectile projectileSelf;
        private BeamController beamContollerSelf;
        private BasicBeamController basicBeamControllerSelf;
        private PlayerController Owner;
    }
    public class CarrionSubTendrilController : MonoBehaviour
    {
        public CarrionSubTendrilController()
        {

        }
        public void Start()
        {
            this.projectileSelf = base.GetComponent<Projectile>();
            this.beamContollerSelf = base.GetComponent<BeamController>();
            this.basicBeamControllerSelf = base.GetComponent<BasicBeamController>();
            if (this.projectileSelf.Owner is PlayerController) this.Owner = this.projectileSelf.Owner as PlayerController;
            if (UnityEngine.Random.value <= 0.5f) Angle = UnityEngine.Random.Range(70, 110);
            else Angle = UnityEngine.Random.Range(-70, -110);
        }
        public void Update()
        {
            if (parentBeam == null) { RemoveTendril(); return; }
            if ((parentBeam.GetBoneCount() - 1) < parentBoneIndex) { RemoveTendril(); return; }

            Vector2 bonePosition = parentBeam.GetIndexedBonePosition(parentBoneIndex);
            if (bonePosition != Vector2.zero)
            {
                basicBeamControllerSelf.Direction = (parentBeam.GetIndexedBone(parentBoneIndex).RotationAngle + Angle).DegreeToVector2();
                basicBeamControllerSelf.Origin = bonePosition;
                basicBeamControllerSelf.LateUpdatePosition(bonePosition);

                if (UnityEngine.Random.value <= 0.25f) movingLeft = !movingLeft;
                if (UnityEngine.Random.value <= 0.75f)
                {
                    if (movingLeft) Angle++;
                    else Angle--;
                }
            }
        }
        private void RemoveTendril()
        {
            beamContollerSelf.CeaseAttack();
        }
        private Projectile projectileSelf;
        private BeamController beamContollerSelf;
        private BasicBeamController basicBeamControllerSelf;
        private PlayerController Owner;
        private float Angle;
        private bool movingLeft;

        public BasicBeamController parentBeam;
        public int parentBoneIndex;
    }
    public class CarrionMovementTentacles : MonoBehaviour
    {
        public RaycastResult hit;
        public List<TentacleDraw> tentes = new List<TentacleDraw>();
        private Projectile selfProjectile;
        private BeamController selfBeam;
        private BasicBeamController selfBasicBeam;
        private PlayerController selfOwner;
        public CarrionMovementTentacles()
        {

        }
        void Start()
        {
            selfProjectile = base.GetComponent<Projectile>();
            selfBeam = base.GetComponent<BeamController>();
            selfBasicBeam = base.GetComponent<BasicBeamController>();
            if (selfProjectile && selfProjectile.ProjectilePlayerOwner()) selfOwner = selfProjectile.ProjectilePlayerOwner();

            InvokeRepeating("CreateNewTentacle", 0.05f, 0.05f);
        }
        void Update()
        {
            foreach (var tent in tentes)
            {
                if (tent.Attach1.gameObject != null && tent.Attach2.gameObject != null)
                {
                    if (Vector3.Distance(tent.Attach1.gameObject.transform.position, tent.Attach2.gameObject.transform.position) > 25) // num might need tweaking
                    {
                        var i = tentes[tentes.IndexOf(tent)];
                        var x = i;
                        tentes.RemoveAt(tentes.IndexOf(tent));
                        UnityEngine.Object.Destroy(x);
                        CreateNewTentacle();
                    }
                }
            }
        }
        void OnDestroy()
        {
            for (int i = (tentes.Count - 1); i >= 0; i--)
            {
                var thungum = tentes[i];
                tentes.RemoveAt(i);
                UnityEngine.Object.Destroy(thungum);
            }
        }
        void CreateNewTentacle()
        {
            if (tentes.Count >= 20)
            {
                var i = tentes[0];
                var x = i;
                tentes.RemoveAt(0);
                UnityEngine.Object.Destroy(x);
            }
            CreateTentacleAtBone(UnityEngine.Random.Range(0, selfBasicBeam.GetBoneCount()));
        }

        void CreateTentacleAtBone(int boneIndex)
        {
            //ETGModConsole.Log("Attempted to create tentacle at bone: " + boneIndex);
            Vector2 bonePosition = selfBasicBeam.GetIndexedBonePosition(boneIndex);
            float boneAngle = selfBasicBeam.GetIndexedBone(boneIndex).RotationAngle;
            float variedAngle = boneAngle;
            if (UnityEngine.Random.value <= 0.5f) variedAngle += UnityEngine.Random.Range(0f, 45f);
            else variedAngle -= UnityEngine.Random.Range(0f, 45f);

            if (PhysicsEngine.Instance.Raycast(bonePosition, BraveMathCollege.DegreesToVector(variedAngle), 20, out hit))
            {
                if (hit.OtherPixelCollider.IsTileCollider)
                {
                    var cable = ETGModMainBehaviour.Instance.gameObject.AddComponent<TentacleDraw>();

                    var i = new GameObject("holdPoint");
                    i.transform.position = hit.Contact;

                    GameObject NewStickler = new GameObject("TentacleStickler");
                    TentacleBoneSticker NewSticklerComp = NewStickler.AddComponent<TentacleBoneSticker>();
                    NewSticklerComp.parentBeam = selfBasicBeam;
                    NewSticklerComp.parentBeamBoneIndex = boneIndex;
                    NewSticklerComp.cable = cable;
                    NewStickler.transform.position = bonePosition;

                    cable.Initialize(NewStickler.transform, i.transform);

                    tentes.Add(cable);

                    RaycastResult.Pool.Free(ref hit);
                }
                //ETGModConsole.Log("Succeeded in placing Tentacle");
            }
        }
        public class TentacleBoneSticker : MonoBehaviour
        {
            public BasicBeamController parentBeam;
            public int parentBeamBoneIndex;
            public TentacleDraw cable;
            private GameObject self;
            private void Start()
            {
                self = base.gameObject;
            }
            private void Update()
            {
                if (!self || !parentBeam || parentBeamBoneIndex < 0 || !cable) Suicide();
                if ((parentBeam.GetBoneCount() - 1) < parentBeamBoneIndex) Suicide(true);
                Vector2 parentBonePosition = parentBeam.GetIndexedBonePosition(parentBeamBoneIndex);
                self.transform.position = parentBonePosition;
            }
            private void Suicide(bool destroyCable = false)
            {
                //ETGModConsole.Log("Sticker at index " + parentBeamBoneIndex + " committed suicide.");

                if (parentBeam && cable && destroyCable)
                {
                    CarrionMovementTentacles tentacleController = parentBeam.GetComponent<CarrionMovementTentacles>();
                    if (tentacleController && tentacleController.tentes.Contains(cable))
                    {
                        var i = tentacleController.tentes[tentacleController.tentes.IndexOf(cable)];
                        var x = i;
                        tentacleController.tentes.RemoveAt(tentacleController.tentes.IndexOf(cable));
                        UnityEngine.Object.Destroy(x);
                    }
                }
                UnityEngine.Object.Destroy(self);
            }
        }

    }
}
