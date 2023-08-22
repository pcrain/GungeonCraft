using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using UnityEngine;
using MonoMod.RuntimeDetour;

using Gungeon;
using Dungeonator;
using Alexandria.ItemAPI;
using Alexandria.Misc;

namespace CwaffingTheGungy
{
    public class BBGun : AdvancedGunBehavior
    {
        public static string GunName          = "B. B. Gun";
        public static string SpriteName       = "embercannon";
        public static string ProjectileName   = "ak-47";
        public static string ShortDescription = "Spare No One";
        public static string LongDescription  = "(Three Strikes)";

        private float lastCharge = 0.0f;

        private static readonly float[] CHARGE_LEVELS = {0.25f,0.5f,1.0f,2.0f};

        private static Projectile fakeProjectile;

        public static void Add()
        {
            Gun gun = Lazy.InitGunFromStrings(GunName, SpriteName, ProjectileName, ShortDescription, LongDescription);
            var comp = gun.gameObject.AddComponent<BBGun>();

            comp.preventNormalFireAudio = true;
            comp.preventNormalReloadAudio = true;
            comp.overrideNormalReloadAudio = "Play_ENM_flame_veil_01";

            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).frames[0].eventAudio = "Play_WPN_seriouscannon_shot_01";
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).frames[0].triggerEvent = true;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.chargeAnimation).wrapMode = tk2dSpriteAnimationClip.WrapMode.LoopSection;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.chargeAnimation).loopStart = 2;

            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(37) as Gun).muzzleFlashEffects;
            gun.barrelOffset.transform.localPosition = new Vector3(1.93f, 0.87f, 0f);
            gun.SetAnimationFPS(gun.shootAnimation, 10);
            gun.SetAnimationFPS(gun.chargeAnimation, 8);
            gun.gunClass = GunClass.CHARGE;

            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Charged;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Ordered;

            //GUN STATS
            ProjectileModule mod = gun.DefaultModule;
                mod.ammoCost            = 1;
                mod.numberOfShotsInClip = 1;
                mod.shootStyle          = ProjectileModule.ShootStyle.Charged;
                mod.sequenceStyle       = ProjectileModule.ProjectileSequenceStyle.Ordered;
                mod.cooldownTime        = 0.70f;
                mod.angleVariance       = 10f;

            List<ProjectileModule.ChargeProjectile> tempChargeProjectiles =
                new List<ProjectileModule.ChargeProjectile>();

            for (int i = 0; i < CHARGE_LEVELS.Length; i++)
            {
                Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(mod.projectiles[0]);
                if (i < mod.projectiles.Count)
                    mod.projectiles[i] = projectile;
                else
                    mod.projectiles.Add(projectile);
                projectile.gameObject.SetActive(false);
                FakePrefab.MarkAsFakePrefab(projectile.gameObject);
                UnityEngine.Object.DontDestroyOnLoad(projectile);

                const int bbSpriteDiameter = 20;
                projectile.baseData.range = 999999f;
                projectile.baseData.speed = 20f;
                projectile.AnimateProjectile(
                    new List<string> {
                        "bball1",
                        "bball2",
                        "bball3",
                        "bball4",
                        "bball5",
                        "bball6",
                        "bball7",
                        "bball8",
                        // "bb9",
                        // "bb10",
                    },
                    20, true, new IntVector2(bbSpriteDiameter, bbSpriteDiameter),
                    false, tk2dBaseSprite.Anchor.MiddleCenter, true, false);

                TheBB bb = projectile.gameObject.AddComponent<TheBB>();
                bb.chargeLevel = i+1;

                // if (mod != gun.DefaultModule) { mod.ammoCost = 0; }
                ProjectileModule.ChargeProjectile chargeProj = new ProjectileModule.ChargeProjectile
                {
                    Projectile = projectile,
                    ChargeTime = CHARGE_LEVELS[i],
                };
                tempChargeProjectiles.Add(chargeProj);
            }
            mod.chargeProjectiles = tempChargeProjectiles;
            gun.reloadTime = 0.01f;
            gun.CanGainAmmo = false;
            gun.SetBaseMaxAmmo(1);

            gun.quality = PickupObject.ItemQuality.B;

            fakeProjectile = Lazy.PrefabProjectileFromGun(gun);
            fakeProjectile.gameObject.AddComponent<FakeProjectileComponent>();
        }

        public override void PostProcessProjectile(Projectile projectile)
        {
            ETGModConsole.Log("for speed, last charge was "+lastCharge);
            projectile.baseData.speed = 100*lastCharge;
            base.PostProcessProjectile(projectile);
        }

        protected override void Update()
        {
            base.Update();
            if (!this.Player)
                return;
            if (this.gun.IsCharging)
                lastCharge = this.gun.GetChargeFraction();
            // p.CurrentGun.charge
            // ETGModConsole.Log("charge is "+this.gun.GetChargeFraction());
        }
    }

    public class TheBB : MonoBehaviour
    {
        private const float BB_DAMAGE_SCALE = 2.0f;
        private const float BB_FORCE_SCALE = 2.0f;
        private const float BB_SPEED_DECAY = 3.0f;

        public int chargeLevel = 0;

        private Projectile m_projectile;
        private PlayerController m_owner;
        private float lifetime = 0f;
        private float maxSpeed = 0f;

        private void Start()
        {
            ETGModConsole.Log("created projectile with charge level "+chargeLevel);
            this.m_projectile = base.GetComponent<Projectile>();
            if (this.m_projectile.Owner && this.m_projectile.Owner is PlayerController)
                this.m_owner = this.m_projectile.Owner as PlayerController;

            this.m_projectile.collidesWithPlayer = true;
            this.maxSpeed = this.m_projectile.baseData.speed;

            this.m_projectile.sprite.usesOverrideMaterial = true;
            this.m_projectile.sprite.renderer.material.shader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTintableTiltedCutoutEmissive");
                this.m_projectile.sprite.renderer.material.SetFloat("_EmissivePower", 1000f);
                this.m_projectile.sprite.renderer.material.SetFloat("_EmissiveColorPower", 1.55f);
                this.m_projectile.sprite.renderer.material.SetColor("_EmissiveColor", Color.magenta);

            BounceProjModifier bounce = this.m_projectile.gameObject.GetOrAddComponent<BounceProjModifier>();
                bounce.numberOfBounces     = Mathf.Max(bounce.numberOfBounces, 999);
                bounce.chanceToDieOnBounce = 0f;
                bounce.onlyBounceOffTiles  = true;
                bounce.OnBounce += OnBounce;

            PierceProjModifier pierce = this.m_projectile.gameObject.GetOrAddComponent<PierceProjModifier>();
                pierce.penetration = Mathf.Max(pierce.penetration,999);
                pierce.penetratesBreakables = true;
        }

        private void OnBounce()
        {
            this.m_projectile.baseData.speed *= 0.9f;
        }

        private void Update()
        {
            float deltatime = BraveTime.DeltaTime;
            lifetime += deltatime;
            this.m_projectile.UpdateSpeed();
            float newSpeed = Mathf.Max(
                this.m_projectile.baseData.speed-BB_SPEED_DECAY*deltatime,0.0001f);
            this.m_projectile.baseData.speed = newSpeed;
            this.m_projectile.UpdateSpeed();

            this.m_projectile.sprite.renderer.material.SetFloat(
                "_EmissivePower", 300.0f+1000.0f*(newSpeed/maxSpeed));
            // this.m_projectile.sprite.renderer.material.SetFloat(
            //     "_EmissiveColorPower", 1.55f*(newSpeed/maxSpeed));
            this.m_projectile.sprite.renderer.material.SetFloat(
                "_Cutoff", 0.1f);
            // this.m_projectile.sprite.renderer.material.SetFloat(
            //     "_VertexColor", 1.0f*(newSpeed/maxSpeed));

            if (this.m_projectile.baseData.speed < 1)
            {
                MiniInteractable mi = MiniInteractable.CreateInteractableAtPosition(
                    this.m_projectile.sprite,
                    this.m_projectile.sprite.WorldCenter,
                    BBInteractScript);
                    mi.sprite.usesOverrideMaterial = true;
                    mi.sprite.renderer.material.shader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTintableTiltedCutoutEmissive");
                        mi.sprite.renderer.material.SetFloat("_EmissivePower", 300f);
                        mi.sprite.renderer.material.SetFloat("_EmissiveColorPower", 1.55f);
                        mi.sprite.renderer.material.SetColor("_EmissiveColor", Color.magenta);

                this.m_projectile.DieInAir(true,false,false,true);
                return;
            }

            this.m_projectile.baseData.damage = BB_DAMAGE_SCALE * this.m_projectile.baseData.speed;
            this.m_projectile.baseData.force = BB_FORCE_SCALE * this.m_projectile.baseData.speed;
        }

        public IEnumerator BBInteractScript(MiniInteractable i, PlayerController p)
        {
            if (p != this.m_owner)
            {
                i.interacting = false;
                yield break;
            }
            foreach (Gun gun in this.m_owner.inventory.AllGuns)
            {
                if (!gun.GetComponent<BBGun>())
                    continue;
                gun.CurrentAmmo = 1;
                gun.ForceImmediateReload();
                AkSoundEngine.PostEvent("Play_OBJ_item_pickup_01", p.gameObject);
                GameObject original = (GameObject)ResourceCache.Acquire("Global VFX/VFX_Item_Pickup");
                  GameObject gameObject = UnityEngine.Object.Instantiate(original);
                    tk2dSprite sprite = gameObject.GetComponent<tk2dSprite>();
                        sprite.PlaceAtPositionByAnchor(i.sprite.WorldCenter, tk2dBaseSprite.Anchor.MiddleCenter);
                        sprite.HeightOffGround = 6f;
                        sprite.UpdateZDepth();

                UnityEngine.Object.Destroy(i.gameObject);
                yield break;
            }
            i.interacting = false;
        }
    }
}
