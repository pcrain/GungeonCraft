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
    public class HolyWaterGun : AdvancedGunBehavior
    {
        public static string ItemName         = "Holy Water Gun";
        public static string SpriteName       = "holy_water_gun";
        public static string ProjectileName   = "10"; // mega douser
        public static string ShortDescription = "The Water, The Gun, & The Holy Soak";
        public static string LongDescription  = "(Deals some damage to jammed; killing a jammed enemy reduces curse by 1)";

        internal static Dictionary<string, Texture2D> _GhostTextures = new();

        public static void Add()
        {
            Gun gun = Lazy.SetupGun(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription);
                gun.gunSwitchGroup                       = (ItemHelper.Get(Items.MegaDouser) as Gun).gunSwitchGroup;
                gun.barrelOffset.transform.localPosition = new Vector3(1.9375f, 0.5f, 0f); // should match "Casing" in JSON file
                // gun.muzzleFlashEffects.type              = VFXPoolType.None; // prevent visual glitch with non-rotating beam near gun muzzle
                // gun.usesContinuousMuzzleFlash            = false;
                // gun.finalMuzzleFlashEffects.type         = VFXPoolType.None;
                gun.gunClass                             = GunClass.BEAM;
                gun.DefaultModule.ammoType               = GameUIAmmoType.AmmoType.BEAM;
                gun.DefaultModule.shootStyle             = ProjectileModule.ShootStyle.Beam;
                gun.DefaultModule.numberOfShotsInClip    = 500;
                gun.SetBaseMaxAmmo(500);

            var comp = gun.gameObject.AddComponent<HolyWaterGun>();

            Projectile projectile = Lazy.PrefabProjectileFromGun(gun);
                projectile.baseData.speed  = 50f;
                projectile.baseData.damage = 0f;
                projectile.baseData.force  = 50f;

            BasicBeamController beamComp = projectile.SetupBeamSprites(
              spriteName: "holy_water_gun", fps: 20, dims: new Vector2(15, 15), impactDims: new Vector2(7, 7));
                beamComp.sprite.usesOverrideMaterial = true;
                beamComp.sprite.renderer.material.shader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTiltedCutoutEmissive");
                beamComp.sprite.renderer.material.SetFloat("_EmissivePower", 15f);
                // fix some animation glitches (don't blindly copy paste; need to be set on a case by case basis depending on your beam's needs)
                beamComp.muzzleAnimation = beamComp.beamStartAnimation;  //use start animation for muzzle animation, make start animatino null
                beamComp.beamStartAnimation = null;

            projectile.gameObject.AddComponent<ExorcismJuice>();
        }

        protected override void OnPickup(GameActor owner)
        {
            base.OnPickup(owner);
            if (owner as PlayerController != this.Player)
                return;

            foreach (AIActor enemy in StaticReferenceManager.AllEnemies)
                OnEnemySpawn(enemy);
            ETGMod.AIActor.OnPreStart += this.OnEnemySpawn;
        }

        private void OnEnemySpawn(AIActor enemy)
        {
            enemy.gameObject.GetOrAddComponent<Exorcisable>();
        }
    }


    public class Exorcisable : MonoBehaviour
    {
        private AIActor _enemy;
        private void Start()
        {
            this._enemy = base.GetComponent<AIActor>();
            this._enemy.specRigidbody.OnBeamCollision += this.CheckForHolyWater;
        }

        private void CheckForHolyWater(BeamController beam)
        {
            // if (!this._enemy.IsBlackPhantom) // TODO: put this back when done debugging
            //     return;
            if (!this._enemy.healthHaver || this._enemy.healthHaver.IsBoss || this._enemy.healthHaver.IsDead  || !this._enemy.healthHaver.IsAlive)
                return;
            if (beam.GetComponent<ExorcismJuice>() is not ExorcismJuice exorcism)
                return;

            // float epower = exorcism.GetPower(); // TODO: put this back when done debugging
            float epower = 1000f;
            if (epower >= this._enemy.healthHaver.currentHealth)
            {
                PlayerController pc = beam.projectile.Owner as PlayerController;
                pc.ownerlessStatModifiers.Add(new StatModifier() {
                    amount      = -1f,
                    modifyType  = StatModifier.ModifyMethod.ADDITIVE,
                    statToBoost = PlayerStats.StatType.Curse,
                    });
                pc.stats.RecalculateStats(pc);

                Texture2D ghostSprite;
                if (HolyWaterGun._GhostTextures.ContainsKey(this._enemy.EnemyGuid))
                    ghostSprite = HolyWaterGun._GhostTextures[this._enemy.EnemyGuid]; // If we've already computed a texture for this enemy, don't do it again
                else
                {
                    ghostSprite = Lazy.GetTexturedEnemyIdleAnimation(this._enemy, new Color(1f,1f,1f,1f), 0.3f);
                    HolyWaterGun._GhostTextures[this._enemy.EnemyGuid] = ghostSprite; // Cache the texture for this enemy for later
                }
                GameObject g                        = UnityEngine.Object.Instantiate(new GameObject(), this._enemy.sprite.WorldTopCenter.ToVector3ZisY(-10f), Quaternion.identity);
                tk2dSpriteCollectionData collection = SpriteBuilder.ConstructCollection(g, "ghostcollection");
                int spriteId                        = SpriteBuilder.AddSpriteToCollection(ghostSprite, collection, "ghostsprite");
                tk2dBaseSprite sprite               = g.AddComponent<tk2dSprite>();
                sprite.SetSprite(collection, spriteId);
                g.AddComponent<GhostlyDeath>();

                // this._enemy.aiActor.EraseFromExistenceWithRewards(true);
                // this._enemy.ForceDeath(beam.projectile.Direction, true);
            }
            this._enemy.healthHaver.ApplyDamage(
                epower, beam.projectile.Direction, "Exorcism", CoreDamageTypes.Water, DamageCategory.Unstoppable, true, null, true);
        }
    }


    public class GhostlyDeath : MonoBehaviour
    {
        private const float _FADE_TIME = 1.4f;
        private const float _DRIFT_SPEED = 0.3f / C.PIXELS_PER_TILE;

        private float _lifetime;
        private tk2dSprite _sprite;
        private Vector3 _velocity;

        private void Start()
        {
            this._lifetime = 0.0f;
            this._sprite = base.gameObject.GetComponent<tk2dSprite>();

            this._velocity = new Vector3(_DRIFT_SPEED, _DRIFT_SPEED, 0f);

            AkSoundEngine.PostEvent("turn_to_gold", base.gameObject);
        }

        private void Update()
        {
            this._lifetime += BraveTime.DeltaTime;
            if (this._lifetime >= _FADE_TIME)
            {
                UnityEngine.Object.Destroy(this.gameObject);
                return;
            }
            this._sprite.transform.position += this._velocity;
            this._sprite.renderer.SetAlpha(1f - (this._lifetime / _FADE_TIME));
        }
    }

    public class ExorcismJuice : MonoBehaviour
    {
        private const float _EXORCISM_DPS   = 15.0f; // damage per second
        private const float _EXORCISM_POWER = _EXORCISM_DPS / C.FPS; // damage per frame

        private Projectile _projectile;
        private BasicBeamController _beam;
        private PlayerController _owner;
        private void Start()
        {
            this._projectile = base.GetComponent<Projectile>();
            this._owner = this._projectile.Owner as PlayerController;
            this._beam = this._projectile.GetComponent<BasicBeamController>();
            // this._projectile.specRigidbody.OnPreRigidbodyCollision += this.OnPreCollision;
            // this._projectile.OnHitEnemy += this.OnHitEnemy;
        }

        public float GetPower()
        {
            return _EXORCISM_POWER;
        }

        private void OnHitEnemy(Projectile bullet, SpeculativeRigidbody spec, bool fatal)
        {
            if (spec.GetComponent<AIActor>() is not AIActor enemy)
                return;
            if (enemy.IsBlackPhantom)
                bullet.baseData.damage = 1000f; // instantly kill black phantoms

            // var t = enemy.aiActor.gameObject.AddComponent<EnemyTranquilizedBehavior>();
            // t.stuntime = this.stuntime;
            // t.stundelay = this.stundelay;
        }

        // private void OnPreCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
        // {
        //     if (otherRigidbody.GetComponent<AIActor>() is not AIActor enemy)
        //         return; // nothing to do against non-enemies

        //     if (enemy.IsBlackPhantom)
        //     {
        //         ETGModConsole.Log($"THOUSAND");
        //         this._projectile.baseData.damage = 1000f; // instantly kill black phantoms
        //     }
        // }
    }
}
