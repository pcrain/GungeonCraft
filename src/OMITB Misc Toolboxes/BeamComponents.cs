using Alexandria.ItemAPI;
using Alexandria.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NevernamedsItems
{
    //BEAM COMPONENTS!
    public class SpawnProjectileAtBeamPoint : MonoBehaviour
    {
        public SpawnProjectileAtBeamPoint()
        {
            this.tickCooldown = 0.1f;
            this.maxDistance = 1;
            this.minDistance = 0;
            this.pickRandomPosition = true;
            this.chanceToFirePerTick = 0.5f;
            this.projectileToFire = UnityEngine.Object.Instantiate(((Gun)ETGMod.Databases.Items[86]).DefaultModule.projectiles[0]);
            this.doPostProcess = true;
            this.addFromBulletWithGunComponent = false;
        }
        public float minDistance;
        public float maxDistance;
        public bool pickRandomPosition;
        public float tickCooldown;
        public float chanceToFirePerTick;
        public Projectile projectileToFire;
        public bool doPostProcess;
        public bool addFromBulletWithGunComponent;
        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            this.m_beam = base.GetComponent<BeamController>();
            this.m_basicBeam = base.GetComponent<BasicBeamController>();
            if (this.m_projectile.Owner is PlayerController) this.m_owner = this.m_projectile.Owner as PlayerController;
        }
        private Projectile m_projectile;
        private BeamController m_beam;
        private BasicBeamController m_basicBeam;
        private PlayerController m_owner;
        private void DoProjectileSpawn()
        {
            Vector2 projectileSpawnPosition = this.m_basicBeam.GetPointOnBeam(UnityEngine.Random.value);
            Vector2 nearestEnemyPosition = projectileSpawnPosition.GetNearestEnemyToPosition().CenterPosition;
            if (nearestEnemyPosition != Vector2.zero)
            {
                GameObject gameObject = ProjSpawnHelper.SpawnProjectileTowardsPoint(this.projectileToFire.gameObject, projectileSpawnPosition, nearestEnemyPosition, 0, 5);
                gameObject.AddComponent<BulletIsFromBeam>();
                Projectile component = gameObject.GetComponent<Projectile>();
                if (component != null)
                {
                    component.Owner = this.m_owner;
                    component.TreatedAsNonProjectileForChallenge = true;
                    component.Shooter = this.m_owner.specRigidbody;
                    component.collidesWithPlayer = false;
                    if (this.addFromBulletWithGunComponent) component.gameObject.GetOrAddComponent<BulletsWithGuns.BulletFromBulletWithGun>();
                    //Stats
                    if (this.doPostProcess)
                    {
                        component.baseData.damage *= this.m_owner.stats.GetStatValue(PlayerStats.StatType.Damage);
                        component.baseData.speed *= this.m_owner.stats.GetStatValue(PlayerStats.StatType.ProjectileSpeed);
                        component.baseData.range *= this.m_owner.stats.GetStatValue(PlayerStats.StatType.RangeMultiplier);
                        component.AdditionalScaleMultiplier *= this.m_owner.stats.GetStatValue(PlayerStats.StatType.PlayerBulletScale); ;
                        component.baseData.force *= this.m_owner.stats.GetStatValue(PlayerStats.StatType.KnockbackMultiplier);
                        component.BossDamageMultiplier *= this.m_owner.stats.GetStatValue(PlayerStats.StatType.DamageToBosses);
                        this.m_owner.DoPostProcessProjectile(component);
                    }
                }
            }
        }
        private void FixedUpdate()
        {
            if (this.m_beam != null)
            {
                if (timer > 0)
                {
                    timer -= BraveTime.DeltaTime;
                }
                if (timer <= 0)
                {
                    if (UnityEngine.Random.value <= this.chanceToFirePerTick) DoProjectileSpawn();
                    timer = tickCooldown;
                }
            }
        }
        private float timer;
    } //Makes the beam spawn projectiles at a position along it's length on a timer.
    public class WanderingBeamComp : MonoBehaviour
    {
        public WanderingBeamComp()
        {
        }
        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            this.m_beam = base.GetComponent<BeamController>();
            this.m_basicBeam = base.GetComponent<BasicBeamController>();
            if (this.m_projectile.Owner is PlayerController) this.m_owner = this.m_projectile.Owner as PlayerController;
        }
        private Projectile m_projectile;
        private BeamController m_beam;
        private BasicBeamController m_basicBeam;
        private PlayerController m_owner;

        private void FixedUpdate()
        {
            if (this.m_beam != null)
            {
                this.m_basicBeam.Direction = this.m_basicBeam.Direction.Rotate(1);
            }
        }

    } //Makes the beam wander NONFUNCTIONAL
    public class BeamExplosiveModifier : MonoBehaviour
    {
        public BeamExplosiveModifier()
        {
            canHarmOwner = false;
            explosionData = GameManager.Instance.Dungeon.sharedSettingsPrefab.DefaultSmallExplosionData;
            chancePerTick = 1;
            tickDelay = 0.1f;
            ignoreQueues = true;
        }
        private void Start()
        {
            timer = tickDelay;
            this.projectile = base.GetComponent<Projectile>();
            this.beamController = base.GetComponent<BeamController>();
            this.basicBeamController = base.GetComponent<BasicBeamController>();
            if (this.projectile.Owner is PlayerController) this.owner = this.projectile.Owner as PlayerController;
        }
        private void Update()
        {
            if (timer > 0)
            {
                timer -= BraveTime.DeltaTime;
            }
            if (timer <= 0)
            {
                DoTick();
                timer = tickDelay;
            }
        }
        private void DoTick()
        {
            //ETGModConsole.Log("Tick Triggered");
            if (UnityEngine.Random.value < chancePerTick)
            {
                LinkedList<BasicBeamController.BeamBone> bones;
                bones = OMITBReflectionHelpers.ReflectGetField<LinkedList<BasicBeamController.BeamBone>>(typeof(BasicBeamController), "m_bones", basicBeamController);
                LinkedListNode<BasicBeamController.BeamBone> linkedListNode = bones.Last;
                Vector2 bonePosition = basicBeamController.GetBonePosition(linkedListNode.Value);
                Explode(bonePosition);

            }
        }
        private void Explode(Vector2 pos)
        {
            if (!canHarmOwner && owner != null)
            {
                for (int i = 0; i < GameManager.Instance.AllPlayers.Length; i++)
                {
                    PlayerController playerController = GameManager.Instance.AllPlayers[i];
                    if (playerController && playerController.specRigidbody)
                    {
                        this.explosionData.ignoreList.Add(playerController.specRigidbody);
                    }
                }
            }
            Exploder.Explode(pos, this.explosionData, Vector2.zero, null, this.ignoreQueues);
        }
        public bool ignoreQueues;

        public float chancePerTick;
        public float tickDelay;

        public ExplosionData explosionData;
        public bool canHarmOwner;

        private float timer;
        private Projectile projectile;
        private BasicBeamController basicBeamController;
        private BeamController beamController;
        private PlayerController owner;
    } //Makes the end of the beam explode!
    internal class EmmisiveBeams : MonoBehaviour
    {
        public EmmisiveBeams()
        {
            this.EmissivePower = 100;
            this.EmissiveColorPower = 1.55f;
        }
        public void Start()
        {
            Shader glowshader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTiltedCutoutEmissive");

            foreach (Transform transform in base.transform)
            {
                if (TransformList.Contains(transform.name))
                {
                    tk2dSprite sproot = transform.GetComponent<tk2dSprite>();
                    if (sproot != null)
                    {
                        sproot.usesOverrideMaterial = true;
                        sproot.renderer.material.shader = glowshader;
                        sproot.renderer.material.EnableKeyword("BRIGHTNESS_CLAMP_ON");
                        sproot.renderer.material.SetFloat("_EmissivePower", EmissivePower);
                        sproot.renderer.material.SetFloat("_EmissiveColorPower", EmissiveColorPower);
                    }
                }
            }
            this.beamcont = base.GetComponent<BasicBeamController>();
            BasicBeamController beam = this.beamcont;
            beam.sprite.usesOverrideMaterial = true;
            BasicBeamController component = beam.gameObject.GetComponent<BasicBeamController>();
            bool flag = component != null;
            bool flag2 = flag;
            if (flag2)
            {
                component.sprite.renderer.material.shader = glowshader;
                component.sprite.renderer.material.EnableKeyword("BRIGHTNESS_CLAMP_ON");
                component.sprite.renderer.material.SetFloat("_EmissivePower", EmissivePower);
                component.sprite.renderer.material.SetFloat("_EmissiveColorPower", EmissiveColorPower);
            }
        }


        private List<string> TransformList = new List<string>()
        {
            "Sprite",
            "beam impact vfx 2",
            "beam impact vfx",
        };


        public void Update()
        {

            Shader glowshader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTiltedCutoutEmissive");
            Transform trna = base.transform.Find("beam pierce impact vfx");
            if (trna != null)
            {
                tk2dSprite sproot = trna.GetComponent<tk2dSprite>();
                if (sproot != null)
                {
                    sproot.renderer.material.shader = glowshader;
                    sproot.renderer.material.EnableKeyword("BRIGHTNESS_CLAMP_ON");
                    sproot.renderer.material.SetFloat("_EmissivePower", EmissivePower);
                    sproot.renderer.material.SetFloat("_EmissiveColorPower", EmissiveColorPower);
                }
            }

        }

        private BasicBeamController beamcont;
        public float EmissivePower;
        public float EmissiveColorPower;
    } //Allows the beam to glow
    public class BeamSplittingModifier : MonoBehaviour
    {
        public BeamSplittingModifier()
        {
            subBeams = new Dictionary<BasicBeamController, float>();
            distanceTilSplit = 7f;
            amtToSplitTo = 0;
            splitAngles = 39;
            dmgMultOnSplit = 0.66f;
        }
        private void Start()
        {
            this.projectile = base.GetComponent<Projectile>();
            this.beamController = base.GetComponent<BeamController>();
            this.basicBeamController = base.GetComponent<BasicBeamController>();
            if (this.projectile.Owner is PlayerController) this.owner = this.projectile.Owner as PlayerController;

            if (projectile.baseData.range > distanceTilSplit)
            {
                originalRange = projectile.baseData.range;
                projectile.baseData.range = distanceTilSplit;
            }
            else
            {
                distanceTilSplit = projectile.baseData.range;
            }
        }

        private void ClearExtantSubBeams()
        {
            if (subBeams.Count <= 0) { return; }
            for (int i = subBeams.Count - 1; i >= 0; i--)
            {
                if (subBeams.ElementAt(i).Key && subBeams.ElementAt(i).Key.gameObject)
                {
                    subBeams.ElementAt(i).Key.CeaseAttack();
                }
            }
            subBeams.Clear();
        }
        private void CreateNewSubBeams()
        {
            ClearExtantSubBeams();
            float ProjectileInterval = splitAngles / ((float)amtToSplitTo - 1);
            float currentAngle = basicBeamController.GetFinalBoneDirection();
            float startAngle = currentAngle + (splitAngles * 0.5f);
            int iteration = 0;

            for (int i = 0; i < amtToSplitTo; i++)
            {
                LinkedList<BasicBeamController.BeamBone> bones;
                bones = OMITBReflectionHelpers.ReflectGetField<LinkedList<BasicBeamController.BeamBone>>(typeof(BasicBeamController), "m_bones", basicBeamController);
                LinkedListNode<BasicBeamController.BeamBone> linkedListNode = null;
                if (bones != null) linkedListNode = bones.Last;
                else { Debug.LogError("Bones was NULL"); return; }

                Vector2 bonePosition = basicBeamController.GetBonePosition(linkedListNode.Value);

                float finalAngle = startAngle - (ProjectileInterval * iteration);

                GameObject newSubBeamPrefab = FakePrefab.Clone(projectile.gameObject);
                if (newSubBeamPrefab == null) Debug.LogError("BeamSplitComp: Cloned Beam Prefab was NULL!");

                BeamController controllerPrefab = newSubBeamPrefab.GetComponent<BeamController>();
                if (controllerPrefab == null) { Debug.LogError("BeamSplitComp: ControllerPrefab was NULL!"); }
                if (controllerPrefab is BasicBeamController)
                {
                    GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(controllerPrefab.gameObject);

                    BasicBeamController newBasicSubBeam = gameObject.GetComponent<BasicBeamController>();
                    newBasicSubBeam.State = BasicBeamController.BeamState.Firing;
                    newBasicSubBeam.HitsPlayers = false;
                    newBasicSubBeam.HitsEnemies = true;
                    newBasicSubBeam.Origin = bonePosition;
                    newBasicSubBeam.Direction = finalAngle.DegreeToVector2();
                    newBasicSubBeam.usesChargeDelay = false;
                    newBasicSubBeam.muzzleAnimation = string.Empty;
                    newBasicSubBeam.chargeAnimation = string.Empty;
                    newBasicSubBeam.beamStartAnimation = string.Empty;
                    newBasicSubBeam.projectile.Owner = this.projectile.Owner;
                    newBasicSubBeam.Owner = this.basicBeamController.Owner;
                    newBasicSubBeam.Gun = this.basicBeamController.Gun;
                    if (originalRange > 0) newBasicSubBeam.projectile.baseData.range = originalRange;
                    newBasicSubBeam.projectile.baseData.damage *= dmgMultOnSplit;

                    if (newBasicSubBeam.GetComponent<BeamSplittingModifier>()) Destroy(newBasicSubBeam.GetComponent<BeamSplittingModifier>());

                    subBeams.Add(newBasicSubBeam, (ProjectileInterval * iteration));
                }
                else { Debug.LogError("BeamSplitComp: Controller prefab was not beam????"); }

                iteration++;
            }
        }
        private void Update()
        {
            if (projectile.baseData.range > distanceTilSplit) { originalRange = projectile.baseData.range; projectile.baseData.range = distanceTilSplit; }
            if ((basicBeamController.ApproximateDistance >= distanceTilSplit) && subBeams.Count < amtToSplitTo)
            {
                CreateNewSubBeams();
            }
            if ((basicBeamController.ApproximateDistance < distanceTilSplit) && subBeams.Count > 0)
            {
                ClearExtantSubBeams();
            }
            float currentAngle = basicBeamController.GetFinalBoneDirection();
            float startAngle = currentAngle + (splitAngles * 0.5f);
            if (subBeams.Count > 0)
            {
                for (int i = 0; i < subBeams.Count; i++)
                {
                    BasicBeamController particularSubBeam = subBeams.ElementAt(i).Key;
                    LinkedList<BasicBeamController.BeamBone> bones;
                    bones = OMITBReflectionHelpers.ReflectGetField<LinkedList<BasicBeamController.BeamBone>>(typeof(BasicBeamController), "m_bones", basicBeamController);
                    LinkedListNode<BasicBeamController.BeamBone> linkedListNode = bones.Last;
                    Vector2 bonePosition = basicBeamController.GetBonePosition(linkedListNode.Value);

                    float angleOffset = subBeams.ElementAt(i).Value;
                    particularSubBeam.Direction = (startAngle - angleOffset).DegreeToVector2();
                    particularSubBeam.Origin = bonePosition;
                    particularSubBeam.LateUpdatePosition(bonePosition);
                }
            }
        }
        private void OnDestroy()
        {
            ClearExtantSubBeams();
        }
        private Dictionary<BasicBeamController, float> subBeams;
        public float distanceTilSplit;
        public int amtToSplitTo;
        public float splitAngles;
        public float dmgMultOnSplit;

        private float originalRange;
        private Projectile projectile;
        private BasicBeamController basicBeamController;
        private BeamController beamController;
        private PlayerController owner;
    } //Makes the beam split into multiple weaker beams after travelling a certain distance
    public class BeamProjSpewModifier : MonoBehaviour
    {
        public BeamProjSpewModifier()
        {
            chancePerTick = 1;
            tickDelay = 0.01f;
            accuracyVariance = 7;
            bulletToSpew = (PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0];
        }
        private void Start()
        {
            timer = tickDelay;
            this.projectile = base.GetComponent<Projectile>();
            this.beamController = base.GetComponent<BeamController>();
            this.basicBeamController = base.GetComponent<BasicBeamController>();
            if (this.projectile.Owner is PlayerController) this.owner = this.projectile.Owner as PlayerController;


        }
        private void Update()
        {
            if (timer > 0)
            {
                timer -= BraveTime.DeltaTime;
            }
            if (timer <= 0)
            {
                DoTick();
                timer = tickDelay;
            }
        }
        private void DoTick()
        {
            //ETGModConsole.Log("Tick Triggered");
            if (UnityEngine.Random.value < chancePerTick)
            {
                LinkedList<BasicBeamController.BeamBone> bones;
                bones = OMITBReflectionHelpers.ReflectGetField<LinkedList<BasicBeamController.BeamBone>>(typeof(BasicBeamController), "m_bones", basicBeamController);
                LinkedListNode<BasicBeamController.BeamBone> linkedListNode = bones.Last;
                Vector2 bonePosition = basicBeamController.GetBonePosition(linkedListNode.Value);

                Explode(bonePosition, basicBeamController.GetFinalBoneDirection());

            }
        }
        private void Explode(Vector2 pos, float angle)
        {
            float variance = UnityEngine.Random.Range(0, accuracyVariance);
            if (UnityEngine.Random.value <= 0.5) variance *= -1;
            float angleVaried = angle + variance;
            GameObject spawnedBulletOBJ = SpawnManager.SpawnProjectile(bulletToSpew.gameObject, pos, Quaternion.Euler(0f, 0f, angleVaried), true);
            spawnedBulletOBJ.AddComponent<BulletIsFromBeam>();
            Projectile component = spawnedBulletOBJ.GetComponent<Projectile>();
            if (component != null)
            {
                component.Owner = projectile.ProjectilePlayerOwner();
                component.Shooter = projectile.ProjectilePlayerOwner().specRigidbody;
                owner.DoPostProcessProjectile(component);
            }
        }
        public float chancePerTick;
        public float tickDelay;
        public Projectile bulletToSpew;
        public float accuracyVariance;

        private float timer;
        private Projectile projectile;
        private BasicBeamController basicBeamController;
        private BeamController beamController;
        private PlayerController owner;
    } //Makes the beam fire projectiles from the end
    public class AlwaysPointAwayFromPlayerBeam : MonoBehaviour
    {
        public AlwaysPointAwayFromPlayerBeam()
        {
        }
        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            this.m_beam = base.GetComponent<BeamController>();
            this.m_basicBeam = base.GetComponent<BasicBeamController>();
            if (this.m_projectile.Owner is PlayerController) this.m_owner = this.m_projectile.Owner as PlayerController;
        }
        private Projectile m_projectile;
        private BeamController m_beam;
        private BasicBeamController m_basicBeam;
        private PlayerController m_owner;

        private void FixedUpdate()
        {
            if (this.m_beam != null)
            {
                Vector2 pointTowards = m_owner.sprite.WorldCenter.CalculateVectorBetween(this.m_basicBeam.Origin);
                float flotsam = pointTowards.normalized.ToAngle();
                this.m_basicBeam.Direction = flotsam.DegreeToVector2();
            }
        }

    } //Makes the beam always angle away from it's owner
    public class BeamBlankModifier : MonoBehaviour
    {
        public BeamBlankModifier()
        {
            blankType = EasyBlankType.MINI;
            chancePerTick = 0.25f;
            tickDelay = 1f;
        }
        private void Start()
        {
            timer = tickDelay;
            this.projectile = base.GetComponent<Projectile>();
            this.beamController = base.GetComponent<BeamController>();
            this.basicBeamController = base.GetComponent<BasicBeamController>();
            if (this.projectile.Owner is PlayerController) this.owner = this.projectile.Owner as PlayerController;


        }
        private void Update()
        {
            if (timer > 0)
            {
                timer -= BraveTime.DeltaTime;
            }
            if (timer <= 0)
            {
                DoTick();
                timer = tickDelay;
            }
        }
        private void DoTick()
        {
            if (UnityEngine.Random.value < chancePerTick)
            {
                LinkedList<BasicBeamController.BeamBone> bones;
                bones = OMITBReflectionHelpers.ReflectGetField<LinkedList<BasicBeamController.BeamBone>>(typeof(BasicBeamController), "m_bones", basicBeamController);
                LinkedListNode<BasicBeamController.BeamBone> linkedListNode = bones.Last;
                Vector2 bonePosition = basicBeamController.GetBonePosition(linkedListNode.Value);
                Blank(bonePosition);

            }
        }
        private void Blank(Vector2 pos)
        {
            GameObject silencerVFX = (GameObject)ResourceCache.Acquire("Global VFX/BlankVFX_Ghost");
            AkSoundEngine.PostEvent("Play_OBJ_silenceblank_small_01", base.gameObject);
            GameObject gameObject = new GameObject("silencer");
            SilencerInstance silencerInstance = gameObject.AddComponent<SilencerInstance>();
            float additionalTimeAtMaxRadius = 0.25f;
            silencerInstance.TriggerSilencer(pos, 20f, 5, silencerVFX, 0f, 3f, 3f, 3f, 30f, 3f, additionalTimeAtMaxRadius, owner, true, false);
        }
        public float chancePerTick;
        public float tickDelay;
        EasyBlankType blankType;
        private float timer;
        private Projectile projectile;
        private BasicBeamController basicBeamController;
        private BeamController beamController;
        private PlayerController owner;
    }
    public class EnemyBulletConverterBeam : MonoBehaviour
    {
        public EnemyBulletConverterBeam()
        {
        }
        private void Start()
        {

            this.projectile = base.GetComponent<Projectile>();
            this.beamController = base.GetComponent<BeamController>();
            this.basicBeamController = base.GetComponent<BasicBeamController>();
            if (this.projectile.Owner is PlayerController) this.owner = this.projectile.Owner as PlayerController;


        }
        private void Update()
        {
            foreach (Projectile proj in StaticReferenceManager.AllProjectiles)
            {
                if (proj && (proj.Owner == null || !(proj.Owner is PlayerController)) && proj.specRigidbody != null)
                {
                    if (basicBeamController.PosIsNearAnyBoneOnBeam(proj.specRigidbody.UnitCenter, 1))
                    {
                        //proj.RemoveBulletScriptControl();
                        if (proj.Owner && proj.Owner.specRigidbody) proj.specRigidbody.DeregisterSpecificCollisionException(proj.Owner.specRigidbody);
                        if (proj.GetComponent<BeamController>() != null)
                        {
                            proj.GetComponent<BeamController>().HitsPlayers = false;
                            proj.GetComponent<BeamController>().HitsEnemies = true;
                        }
                        else if (proj.GetComponent<BasicBeamController>() != null)
                        {
                            proj.GetComponent<BasicBeamController>().HitsPlayers = false;
                            proj.GetComponent<BasicBeamController>().HitsEnemies = true;
                        }
                        proj.Owner = owner;
                        proj.SetNewShooter(owner.specRigidbody);
                        proj.allowSelfShooting = false;
                        proj.collidesWithPlayer = false;
                        proj.collidesWithEnemies = true;
                        proj.baseData.damage = 5;
                        if (proj.IsBlackBullet) proj.baseData.damage *= 2;
                        PlayerController player = (owner as PlayerController);
                        if (player != null)
                        {
                            proj.baseData.damage *= player.stats.GetStatValue(PlayerStats.StatType.Damage);
                            proj.baseData.speed *= player.stats.GetStatValue(PlayerStats.StatType.ProjectileSpeed);
                            proj.UpdateSpeed();
                            proj.baseData.force *= player.stats.GetStatValue(PlayerStats.StatType.KnockbackMultiplier);
                            proj.baseData.range *= player.stats.GetStatValue(PlayerStats.StatType.RangeMultiplier);
                            proj.BossDamageMultiplier *= player.stats.GetStatValue(PlayerStats.StatType.DamageToBosses);
                            proj.RuntimeUpdateScale(player.stats.GetStatValue(PlayerStats.StatType.PlayerBulletScale));
                            player.DoPostProcessProjectile(proj);
                        }
                        proj.AdjustPlayerProjectileTint(ExtendedColours.honeyYellow, 1);
                        proj.UpdateCollisionMask();
                        proj.RemoveFromPool();
                        proj.Reflected();
                    }

                }

            }
        }

        private Projectile projectile;
        private BasicBeamController basicBeamController;
        private BeamController beamController;
        private PlayerController owner;
    }
    public class EnemyBulletReflectorBeam : MonoBehaviour
    {
        public EnemyBulletReflectorBeam()
        {
        }
        private void Start()
        {

            this.projectile = base.GetComponent<Projectile>();
            this.beamController = base.GetComponent<BeamController>();
            this.basicBeamController = base.GetComponent<BasicBeamController>();
            if (this.projectile.Owner is PlayerController) this.owner = this.projectile.Owner as PlayerController;


        }
        private void Update()
        {
            if (timer <= 0.1f)
            {
                timer += BraveTime.DeltaTime;
            }
            else
            {
                timer = 0;
                if (UnityEngine.Random.value <= 0.25)
                {
                    DoReflect();
                }
            }
        }
        private void DoReflect()
        {
            foreach (Projectile proj in StaticReferenceManager.AllProjectiles)
            {
                if (proj && (proj.Owner == null || !(proj.Owner is PlayerController)) && proj.specRigidbody != null)
                {
                    if (basicBeamController.PosIsNearAnyBoneOnBeam(proj.specRigidbody.UnitCenter, 1))
                    {
                        proj.ReflectBullet(true, owner, 10, false);
                    }

                }

            }
        }
        private float timer;
        private Projectile projectile;
        private BasicBeamController basicBeamController;
        private BeamController beamController;
        private PlayerController owner;
    }
    public class TintingBeamModifier : MonoBehaviour
    {
        public TintingBeamModifier()
        {
            targetColour = Color.red;
            designatedSource = "unset";
        }
        public void Start()
        {
            self = base.GetComponent<Projectile>();
            self.OnHitEnemy += OnHitEnemy;
        }
        private Projectile self;
        public Color targetColour;
        public string designatedSource;
        public void OnHitEnemy ( Projectile self, SpeculativeRigidbody enemy, bool fatal)
        {
            if (enemy && enemy.aiActor)
            {
                enemy.aiActor.RegisterOverrideColor(targetColour, designatedSource);
            }
        }
    }
}
