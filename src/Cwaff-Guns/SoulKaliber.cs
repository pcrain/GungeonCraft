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
    public class SoulKaliber : AdvancedGunBehavior
    {
        public static string ItemName         = "Soul Kaliber";
        public static string SpriteName       = "soul_kaliber";
        public static string ProjectileName   = "38_special";
        public static string ShortDescription = "Gundead or Alive";
        public static string LongDescription  = "(hitting an enemy gives them the soul link status effect; all soul linked enemies receive damage when one of them is hit)";

        internal static tk2dSpriteAnimationClip _ProjSprite;

        public static void Add()
        {
            Gun gun = Lazy.SetupGun(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription);
                gun.muzzleFlashEffects.type              = VFXPoolType.None;
                gun.gunSwitchGroup                       = (ItemHelper.Get(Items.GunslingersAshes) as Gun).gunSwitchGroup;
                gun.DefaultModule.ammoCost               = 1;
                gun.DefaultModule.shootStyle             = ProjectileModule.ShootStyle.SemiAutomatic;
                gun.DefaultModule.sequenceStyle          = ProjectileModule.ProjectileSequenceStyle.Random;
                gun.reloadTime                           = 1.1f;
                gun.DefaultModule.cooldownTime           = 0.1f;
                gun.DefaultModule.numberOfShotsInClip    = 10;
                gun.quality                              = PickupObject.ItemQuality.D;
                gun.barrelOffset.transform.localPosition = new Vector3(1.8125f, 0.625f, 0f); // should match "Casing" in JSON file
                gun.SetBaseMaxAmmo(250);
                gun.SetAnimationFPS(gun.shootAnimation, 24);
                gun.SetAnimationFPS(gun.reloadAnimation, 12);

            var comp = gun.gameObject.AddComponent<SoulKaliber>();
                comp.SetFireAudio("soul_kaliber_fire");
                comp.SetReloadAudio("soul_kaliber_reload");

            _ProjSprite = AnimateBullet.CreateProjectileAnimation(
                new List<string> {
                    "soul-kaliber-projectile",
                }, 2, true, new IntVector2(10, 10),
                false, tk2dBaseSprite.Anchor.MiddleCenter, true, true);

            Projectile projectile = Lazy.PrefabProjectileFromGun(gun);
                projectile.baseData.speed   = 30.0f;
                projectile.baseData.damage  = 1f;
                projectile.transform.parent = gun.barrelOffset;
                projectile.AddAnimation(_ProjSprite);
                projectile.SetAnimation(_ProjSprite);
                projectile.gameObject.AddComponent<SoulLinkProjectile>();
        }
    }

    public class SoulLinkProjectile : MonoBehaviour
    {
        private void Start()
        {
            Projectile proj = base.GetComponent<Projectile>();
            Material m = proj.sprite.renderer.material;
                m.shader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTintableTiltedCutoutEmissive");
                m.SetFloat("_EmissivePower", 0f);
                m.SetFloat("_EmissiveColorPower", 1.55f);
                m.SetColor("_EmissiveColor", Color.red);

            proj.OnHitEnemy += (Projectile p, SpeculativeRigidbody enemy, bool _) => {
                AkSoundEngine.PostEvent("soul_kaliber_impact", p.gameObject);
                enemy.aiActor.gameObject.GetOrAddComponent<SoulLinkStatus>();
            };
        }
    }

    public class SoulLinkStatus : MonoBehaviour
    {
        public static GameActorHealthEffect StandardSoulLinkEffect;

        private const int   _ORBIT_NUM         = 3;
        private const float _ORBIT_RPS         = 0.5f;
        private const float _ORBIT_SPR         = 1.0f / _ORBIT_RPS; // seconds per rotation
        private const float _ORBIT_GAP         = 360.0f / (float)_ORBIT_NUM;
        private const int   _NUM_HIT_PARTICLES = 12;
        private const float _SOUL_PART_SPEED   = 3f;

        internal static VFXPool _SoulLinkHitVFXPool      = null;
        internal static GameObject _SoulLinkHitVFX       = null;
        internal static GameObject _SoulLinkOverheadVFX  = null;
        internal static GameObject _SoulLinkSoulVFX      = null;

        private static bool _SoulLinkEffectHappening = false;

        private AIActor _enemy;
        private float _enemyGirth;
        private float _orbitTimer;
        private List<GameObject> _orbitals;

        public static void Init()
        {
            _SoulLinkHitVFXPool    = VFX.CreatePoolFromVFXGameObject((ItemHelper.Get(Items.MagicLamp) as Gun).DefaultModule.projectiles[0].hitEffects.overrideMidairDeathVFX);
            _SoulLinkHitVFX        = _SoulLinkHitVFXPool.effects[0].effects[0].effect.gameObject;
            _SoulLinkOverheadVFX   = VFX.animations["SoulLinkParticle"];
            _SoulLinkSoulVFX       = VFX.animations["SoulLinkSoul"];
            StandardSoulLinkEffect = new GameActorHealthEffect
            {
                duration                 = 60,
                effectIdentifier         = "SoulLink",
                resistanceType           = EffectResistanceType.None,
                DamagePerSecondToEnemies = 0,
                ignitesGoops             = false,
                OverheadVFX              = null,
                AffectsEnemies           = true,
                AffectsPlayers           = false,
                AppliesOutlineTint       = true,
                PlaysVFXOnActor          = false,
                AppliesTint              = false,
                AppliesDeathTint         = false,
            };
        }

        private void Start()
        {
            this._enemy        = base.GetComponent<AIActor>();
            this._enemyGirth   = this._enemy.sprite.GetBounds().size.x / 2.0f; // get the radius of the enemy's sprite
            this._orbitTimer   = 0;
            this._orbitals     = new();

            // Spawn orbitals
            for (int i = 0; i < _ORBIT_NUM; ++i)
                this._orbitals.Add(SpawnManager.SpawnVFX(
                    SoulLinkStatus._SoulLinkOverheadVFX, this._enemy.sprite.WorldCenter.ToVector3ZisY(-1), Quaternion.identity));
            UpdateOrbitals();

            this._enemy.ApplyEffect(SoulLinkStatus.StandardSoulLinkEffect);
            this._enemy.healthHaver.ModifyDamage += this.OnTakeDamage;
            this._enemy.healthHaver.OnPreDeath += (_) => HandleEnemyDied();  // deal with some despawn gliches
        }

        private void Update()
        {
            if (this._enemy?.healthHaver?.IsDead ?? true)
            {
                HandleEnemyDied();
                return;
            }

            this._orbitTimer += BraveTime.DeltaTime;
            if (this._orbitTimer > _ORBIT_SPR)
                this._orbitTimer -= _ORBIT_SPR;

            UpdateOrbitals();
        }

        private void UpdateOrbitals()
        {
            int i = 0;
            float orbitOffset = this._orbitTimer / _ORBIT_SPR;
            float z = C.PIXELS_PER_TILE * this._enemyGirth;

            float power = 2f * Mathf.Abs(Mathf.Sin(2.0f * Mathf.PI * orbitOffset));

            foreach (GameObject g in this._orbitals)
            {
                tk2dSprite sprite = g.GetComponent<tk2dSprite>();
                sprite.renderer.enabled = this._enemy.renderer.enabled;
                float angle = (_ORBIT_GAP * i + 360.0f * orbitOffset).Clamp360();
                Vector2 avec   = angle.ToVector();
                Vector2 offset = new Vector2(1.5f * this._enemyGirth * avec.x, 0.75f * this._enemyGirth * avec.y);
                g.transform.position = (this._enemy.sprite.WorldCenter + offset).ToVector3ZisY(angle < 180 ? z : -z);
                g.transform.rotation = angle.EulerZ();

                Material m = sprite.renderer.material;
                    m.SetFloat("_EmissivePower", power);

                ++i;
            }
        }

        private void OnTakeDamage(HealthHaver hh, HealthHaver.ModifyDamageEventArgs data)
        {
            // prevent ourselves from taking damage in an infinite loop from other soul-linked enemies
            if (_SoulLinkEffectHappening)
                return;
            _SoulLinkEffectHappening = true;

            AIActor enemy = hh.aiActor;
            List<AIActor> activeEnemies = enemy.GetAbsoluteParentRoom().GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
            if (activeEnemies == null)
                return;

            bool didAnything = false;
            foreach (AIActor otherEnemy in activeEnemies)
            {
                if (!(otherEnemy && otherEnemy.specRigidbody && !otherEnemy.IsGone && otherEnemy.healthHaver))
                    continue; // we don't care about harmless enemies
                if (enemy == otherEnemy)
                    continue; // don't apply damage to ourselves
                if (otherEnemy.gameObject.GetComponent<SoulLinkStatus>() is not SoulLinkStatus soulLink)
                    continue;
                soulLink.ShareThePain(data.ModifiedDamage);
                didAnything = true;
            }
            if (didAnything)
                AkSoundEngine.PostEvent("soul_kaliber_drain", hh.gameObject);
            _SoulLinkEffectHappening = false;
        }

        private void HandleEnemyDied()
        {
            foreach (GameObject g in this._orbitals)
                UnityEngine.Object.Destroy(g);
        }

        public void ShareThePain(float damage)
        {
            HealthHaver hh = this._enemy.healthHaver;
            float curHealth = hh.currentHealth;
            float maxHealth = hh.maximumHealth;
            hh.ApplyDamage(damage, new Vector2(0f,0f), "Soul Link",
                CoreDamageTypes.Magic, DamageCategory.Unstoppable,
                true, null, false);
            hh.knockbackDoer.ApplyKnockback(new Vector2(10f,0f), 2f);

            Vector2 ppos = this._enemy.sprite.WorldCenter;
            for (int i = 0; i < _NUM_HIT_PARTICLES; ++i)
            {
                float angle = Lazy.RandomAngle();
                Vector2 finalpos = ppos + BraveMathCollege.DegreesToVector(angle);
                GameObject v = SpawnManager.SpawnVFX(_SoulLinkSoulVFX, finalpos, 0f.EulerZ());
                FancyVFX f = v.AddComponent<FancyVFX>();
                    f.Setup(_SOUL_PART_SPEED * Vector2.up, lifetime: 0.5f, fadeOutTime: 0.5f, emissivePower: 50f, emissiveColor: Color.white);
            }
        }
    }
}
