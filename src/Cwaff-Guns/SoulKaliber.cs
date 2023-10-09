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
        public static string LongDescription  = "Fires projectiles that give enemies the soul link status effect. All soul linked enemies receive damage when any one of them is hit.\n\nA traveling missionary of Kaliber was once rudely interrupted mid-sermon by a bandit army of sword-wielding heathens. With no weapons on hand to defend their congregation, the missionary prayed to the goddess for a firearm to deliver them from impending doom. Kaliber asked an acolyte to prepare and deliver one of her strongest guns; the acolyte, however, accidentally dropped the gun and its ammunition while loading it. The ammo rained down rather harmlessly on the bandits' heads, but by some miracle, the gun itself managed to bludgeon one of the bandits, knocking all of them out in the process.";

        internal static tk2dSpriteAnimationClip _ProjSprite;

        public static void Add()
        {
            Gun gun = Lazy.SetupGun<SoulKaliber>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription);
                gun.muzzleFlashEffects.type              = VFXPoolType.None;
                gun.reloadTime                           = 1.1f;
                gun.quality                              = PickupObject.ItemQuality.D;
                gun.SetBaseMaxAmmo(250);
                gun.SetAnimationFPS(gun.shootAnimation, 24);
                gun.SetAnimationFPS(gun.reloadAnimation, 12);
                gun.ClearDefaultAudio();
                gun.SetFireAudio("soul_kaliber_fire");
                gun.SetReloadAudio("soul_kaliber_reload");

            ProjectileModule mod = gun.DefaultModule;
                mod.ammoCost               = 1;
                mod.shootStyle             = ProjectileModule.ShootStyle.SemiAutomatic;
                mod.sequenceStyle          = ProjectileModule.ProjectileSequenceStyle.Random;
                mod.cooldownTime           = 0.1f;
                mod.numberOfShotsInClip    = 10;

            _ProjSprite = AnimateBullet.CreateProjectileAnimation(
                ResMap.Get("soul_kaliber_projectile").Base(),
                2, true, new IntVector2(10, 10),
                false, tk2dBaseSprite.Anchor.MiddleCenter, true, true);

            Projectile projectile = Lazy.PrefabProjectileFromGun(gun);
                projectile.baseData.speed   = 30.0f;
                projectile.baseData.damage  = 1f;
                projectile.transform.parent = gun.barrelOffset;
                projectile.AddDefaultAnimation(_ProjSprite);
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

        private const int   _NUM_HIT_PARTICLES = 12;
        private const float _SOUL_PART_SPEED   = 3f;

        internal static VFXPool _SoulLinkHitVFXPool      = null;
        internal static GameObject _SoulLinkHitVFX       = null;
        internal static GameObject _SoulLinkOverheadVFX  = null;
        internal static GameObject _SoulLinkSoulVFX      = null;

        private static bool _SoulLinkEffectHappening = false;

        private AIActor _enemy;
        private OrbitalEffect _orbitalEffect = null;

        public static void Init()
        {
            _SoulLinkHitVFXPool    = VFX.CreatePoolFromVFXGameObject((ItemHelper.Get(Items.MagicLamp) as Gun).DefaultModule.projectiles[0].hitEffects.overrideMidairDeathVFX);
            _SoulLinkHitVFX        = _SoulLinkHitVFXPool.effects[0].effects[0].effect.gameObject;
            _SoulLinkOverheadVFX   = VFX.RegisterVFXObject("SoulLinkParticle", ResMap.Get("soul_link_particle"),
                fps: 16, loops: true, anchor: tk2dBaseSprite.Anchor.LowerCenter, scale: 0.3f, emissivePower: 100);
            _SoulLinkSoulVFX       = VFX.RegisterVFXObject("SoulLinkSoul", ResMap.Get("soul_link_soul"),
                fps: 5, loops: true, anchor: tk2dBaseSprite.Anchor.MiddleCenter, scale: 0.3f, emissivePower: 200);
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
            this._enemy         = base.GetComponent<AIActor>();
            this._orbitalEffect = this._enemy.gameObject.AddComponent<OrbitalEffect>();
                this._orbitalEffect.SetupOrbitals(vfx: _SoulLinkOverheadVFX, numOrbitals: 3, rps: 0.5f, isEmissive: true);

            this._enemy.ApplyEffect(SoulLinkStatus.StandardSoulLinkEffect);
            this._enemy.healthHaver.ModifyDamage += this.OnTakeDamage;
            this._enemy.healthHaver.OnPreDeath +=
                (_) => this._orbitalEffect.HandleEnemyDied();  // deal with some despawn gliches
        }

        private void OnTakeDamage(HealthHaver hh, HealthHaver.ModifyDamageEventArgs data)
        {
            // prevent ourselves from taking damage in an infinite loop from other soul-linked enemies
            if (_SoulLinkEffectHappening)
                return;
            try
            {
                _SoulLinkEffectHappening = true;

                AIActor enemy = hh.aiActor;
                List<AIActor> activeEnemies = enemy.GetAbsoluteParentRoom().GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
                if (activeEnemies == null)
                    return;

                bool didAnything = false;
                foreach (AIActor otherEnemy in activeEnemies)
                {
                    if (!(otherEnemy.IsHostileAndNotABoss()))
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
            }
            finally
            {
                _SoulLinkEffectHappening = false;
            }
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
                FancyVFX.Spawn(_SoulLinkSoulVFX, finalpos, 0f.EulerZ(),
                    velocity: _SOUL_PART_SPEED * Vector2.up, lifetime: 0.5f, fadeOutTime: 0.5f, emissivePower: 50f, emissiveColor: Color.white);
            }
        }
    }
}
