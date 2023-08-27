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
        public static string ItemName         = "B. B. Gun";
        public static string SpriteName       = "embercannon";
        public static string ProjectileName   = "ak-47";
        public static string ShortDescription = "Spare No One";
        public static string LongDescription  = "(Three Strikes)";

        private static readonly float[] _CHARGE_LEVELS  = {0.25f,0.5f,1.0f,2.0f};
        private static Projectile       _FakeProjectile = null;
        private float                   _lastCharge     = 0.0f;

        public static void Add()
        {
            Gun gun = Lazy.SetupGun(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription);
                gun.gunClass                             = GunClass.CHARGE;
                gun.quality                              = PickupObject.ItemQuality.B;
                gun.reloadTime                           = 0.01f;
                gun.CanGainAmmo                          = false;
                gun.muzzleFlashEffects                   = (ItemHelper.Get(Items.SeriousCannon) as Gun).muzzleFlashEffects;
                gun.DefaultModule.shootStyle             = ProjectileModule.ShootStyle.Charged;
                gun.DefaultModule.sequenceStyle          = ProjectileModule.ProjectileSequenceStyle.Ordered;
                gun.barrelOffset.transform.localPosition = new Vector3(1.93f, 0.87f, 0f);
                gun.SetBaseMaxAmmo(1);
                gun.SetAnimationFPS(gun.shootAnimation, 10);
                gun.SetAnimationFPS(gun.chargeAnimation, 8);
                gun.LoopAnimation(gun.chargeAnimation, 2);

            var comp = gun.gameObject.AddComponent<BBGun>();
                comp.SetFireAudio("Play_WPN_seriouscannon_shot_01");
                comp.SetReloadAudio("Play_ENM_flame_veil_01");

            ProjectileModule mod = gun.DefaultModule;
                mod.ammoCost            = 1;
                mod.numberOfShotsInClip = 1;
                mod.shootStyle          = ProjectileModule.ShootStyle.Charged;
                mod.sequenceStyle       = ProjectileModule.ProjectileSequenceStyle.Ordered;
                mod.cooldownTime        = 0.70f;
                mod.angleVariance       = 10f;

            List<ProjectileModule.ChargeProjectile> tempChargeProjectiles = new();
            for (int i = 0; i < _CHARGE_LEVELS.Length; i++)
            {
                Projectile projectile = mod.projectiles[0].ClonePrefab();
                if (i < mod.projectiles.Count)
                    mod.projectiles[i] = projectile;
                else
                    mod.projectiles.Add(projectile);

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
                    },
                    20, true, new IntVector2(bbSpriteDiameter, bbSpriteDiameter),
                    false, tk2dBaseSprite.Anchor.MiddleCenter, true, false);

                TheBB bb = projectile.gameObject.AddComponent<TheBB>();
                    bb.chargeLevel = i+1;

                ProjectileModule.ChargeProjectile chargeProj = new ProjectileModule.ChargeProjectile
                {
                    Projectile = projectile,
                    ChargeTime = _CHARGE_LEVELS[i],
                };
                tempChargeProjectiles.Add(chargeProj);
            }
            mod.chargeProjectiles = tempChargeProjectiles;

            _FakeProjectile = Lazy.PrefabProjectileFromGun(gun);
            _FakeProjectile.gameObject.AddComponent<FakeProjectileComponent>();
        }

        public override void PostProcessProjectile(Projectile projectile)
        {
            projectile.baseData.speed = 100 * this._lastCharge;
            base.PostProcessProjectile(projectile);
        }

        protected override void Update()
        {
            base.Update();
            if (!this.Player)
                return;
            if (this.gun.IsCharging)
                this._lastCharge = this.gun.GetChargeFraction();
        }
    }

    public class TheBB : MonoBehaviour
    {
        private const float _BB_DAMAGE_SCALE = 2.0f;
        private const float _BB_FORCE_SCALE = 2.0f;
        private const float _BB_SPEED_DECAY = 3.0f;

        public int chargeLevel = 0;

        private Projectile _projectile;
        private PlayerController _owner;
        private float _lifetime = 0f;
        private float _maxSpeed = 0f;

        private void Start()
        {
            this._projectile = base.GetComponent<Projectile>();
            if (this._projectile.Owner is PlayerController pc)
                this._owner = pc;

            this._projectile.collidesWithPlayer = true;
            this._maxSpeed = this._projectile.baseData.speed;

            this._projectile.sprite.usesOverrideMaterial = true;
            Material m = this._projectile.sprite.renderer.material;
                m.shader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTintableTiltedCutoutEmissive");
                m.SetFloat("_EmissivePower", 1000f);
                m.SetFloat("_EmissiveColorPower", 1.55f);
                m.SetColor("_EmissiveColor", Color.magenta);

            BounceProjModifier bounce = this._projectile.gameObject.GetOrAddComponent<BounceProjModifier>();
                bounce.numberOfBounces     = Mathf.Max(bounce.numberOfBounces, 999);
                bounce.chanceToDieOnBounce = 0f;
                bounce.onlyBounceOffTiles  = true;
                bounce.OnBounce += OnBounce;

            PierceProjModifier pierce = this._projectile.gameObject.GetOrAddComponent<PierceProjModifier>();
                pierce.penetration = Mathf.Max(pierce.penetration,999);
                pierce.penetratesBreakables = true;
        }

        private void OnBounce()
        {
            this._projectile.baseData.speed *= 0.9f;
        }

        private void Update()
        {
            float deltatime = BraveTime.DeltaTime;
            this._lifetime += deltatime;
            this._projectile.UpdateSpeed();
            float newSpeed = Mathf.Max(this._projectile.baseData.speed-_BB_SPEED_DECAY*deltatime,0.0001f);
            this._projectile.baseData.speed = newSpeed;
            this._projectile.UpdateSpeed();

            this._projectile.sprite.renderer.material.SetFloat(
                "_EmissivePower", 300.0f+1000.0f*(newSpeed/_maxSpeed));
            // this.m_projectile.sprite.renderer.material.SetFloat(
            //     "_EmissiveColorPower", 1.55f*(newSpeed/maxSpeed));
            this._projectile.sprite.renderer.material.SetFloat(
                "_Cutoff", 0.1f);
            // this.m_projectile.sprite.renderer.material.SetFloat(
            //     "_VertexColor", 1.0f*(newSpeed/maxSpeed));

            if (this._projectile.baseData.speed > 1)
            {
                this._projectile.baseData.damage = _BB_DAMAGE_SCALE * this._projectile.baseData.speed;
                this._projectile.baseData.force = _BB_FORCE_SCALE * this._projectile.baseData.speed;
                return;
            }

            MiniInteractable mi = MiniInteractable.CreateInteractableAtPosition(
                this._projectile.sprite,
                this._projectile.sprite.WorldCenter,
                BBInteractScript);
                mi.sprite.usesOverrideMaterial = true;
                Material mat = mi.sprite.renderer.material;
                    mat.shader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTintableTiltedCutoutEmissive");
                    mat.SetFloat("_EmissivePower", 300f);
                    mat.SetFloat("_EmissiveColorPower", 1.55f);
                    mat.SetColor("_EmissiveColor", Color.magenta);

            this._projectile.DieInAir(true,false,false,true);
            return;
        }

        public IEnumerator BBInteractScript(MiniInteractable i, PlayerController p)
        {
            if (p != this._owner)
            {
                i.interacting = false;
                yield break;
            }
            foreach (Gun gun in this._owner.inventory.AllGuns)
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
