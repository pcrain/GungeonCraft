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
        public static string SpriteName       = "ringer";
        public static string ProjectileName   = "ak-47";
        public static string ShortDescription = "Gundead or Alive";
        public static string LongDescription  = "(hitting an enemy gives them the soul link status effect; all soul linked enemies receive damage when one of them is hit)";

        public static void Add()
        {
            Gun gun = Lazy.SetupGun(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription);
                gun.muzzleFlashEffects.type           = VFXPoolType.None;
                gun.gunSwitchGroup                    = (ItemHelper.Get(Items.GunslingersAshes) as Gun).gunSwitchGroup;
                gun.DefaultModule.ammoCost            = 1;
                gun.DefaultModule.shootStyle          = ProjectileModule.ShootStyle.SemiAutomatic;
                gun.DefaultModule.sequenceStyle       = ProjectileModule.ProjectileSequenceStyle.Random;
                gun.reloadTime                        = 1.1f;
                gun.DefaultModule.cooldownTime        = 0.1f;
                gun.DefaultModule.numberOfShotsInClip = 10;
                gun.quality                           = PickupObject.ItemQuality.D;
                gun.SetBaseMaxAmmo(250);
                gun.SetAnimationFPS(gun.shootAnimation, 24);

            var comp = gun.gameObject.AddComponent<SoulKaliber>();
                comp.preventNormalFireAudio = true;

            Projectile projectile = Lazy.PrefabProjectileFromGun(gun);
                projectile.baseData.speed   = 30.0f;
                projectile.baseData.damage  = 1f;
                projectile.gameObject.AddComponent<SoulLinkProjectile>();
        }
    }

    public class SoulLinkProjectile : MonoBehaviour
    {
        private void Start()
        {
            base.GetComponent<Projectile>().OnHitEnemy += (Projectile _, SpeculativeRigidbody enemy, bool _) =>
                enemy.aiActor.gameObject.GetOrAddComponent<SoulLinkStatus>();
        }
    }

    public class SoulLinkStatus : MonoBehaviour
    {
        public static GameActorHealthEffect StandardSoulLinkEffect;

        private const int   _ORBIT_NUM       = 3;
        private const float _ORBIT_RPS       = 0.5f;
        private const float _ORBIT_RAD_X     = 1.0f;
        private const float _ORBIT_RAD_Y     = 0.4f;
        private const float _ORBIT_SPR       = 1.0f / _ORBIT_RPS; // seconds per rotation
        private const float _ORBIT_GAP       = 360.0f / (float)_ORBIT_NUM;

        internal static VFXPool _SoulLinkHitVFXPool      = null;
        internal static GameObject _SoulLinkHitVFX       = null;
        internal static GameObject _SoulLinkOverheadVFX  = null;

        private static bool _SoulLinkEffectHappening = false;

        private AIActor _enemy;
        private float _orbitTimer;
        private List<GameObject> _orbitals;

        public static void Init()
        {
            _SoulLinkHitVFXPool    = VFX.CreatePoolFromVFXGameObject((ItemHelper.Get(Items.MagicLamp) as Gun).DefaultModule.projectiles[0].hitEffects.overrideMidairDeathVFX);
            _SoulLinkHitVFX        = _SoulLinkHitVFXPool.effects[0].effects[0].effect.gameObject;
            _SoulLinkOverheadVFX   = VFX.animations["SoulLinkParticle"];
            StandardSoulLinkEffect = new GameActorHealthEffect
            {
                duration                 = 60,
                effectIdentifier         = "SoulLink",
                resistanceType           = EffectResistanceType.None,
                DamagePerSecondToEnemies = 0,
                ignitesGoops             = false,
                // OverheadVFX              = _SoulLinkOverheadVFX,
                OverheadVFX              = null,
                AffectsEnemies           = true,
                AffectsPlayers           = false,
                AppliesOutlineTint       = true,
                PlaysVFXOnActor          = false,
                AppliesTint              = false,
                // AppliesTint              = true,
                // TintColor                = ExtendedColours.pink,
                AppliesDeathTint         = false,
            };
        }

        private void Start()
        {
            this._enemy        = base.GetComponent<AIActor>();
            this._orbitTimer   = 0;
            this._orbitals     = new();

            // Spawn orbitals
            for (int i = 0; i < _ORBIT_NUM; ++i)
            {
                float angle = (_ORBIT_GAP * i).Clamp360();
                Quaternion rot = angle.EulerZ();
                Vector2 offset = _ORBIT_RAD_X * angle.ToVector();
                this._orbitals.Add(SpawnManager.SpawnVFX(
                    SoulLinkStatus._SoulLinkOverheadVFX, (this._enemy.sprite.WorldCenter + offset).ToVector3ZisY(-1), rot));
            }

            this._enemy.ApplyEffect(SoulLinkStatus.StandardSoulLinkEffect);
            this._enemy.healthHaver.ModifyDamage += this.OnTakeDamage;
        }

        private void Update()
        {
            if (this._enemy.healthHaver.IsDead)
            {
                HandleEnemyDied();
                return;
            }

            this._orbitTimer += BraveTime.DeltaTime;
            if (this._orbitTimer > _ORBIT_SPR)
                this._orbitTimer -= _ORBIT_SPR;
            float orbitOffset = this._orbitTimer / _ORBIT_SPR;

            int i = 0;
            // Update positions of orbitals
            foreach (GameObject g in this._orbitals)
            {
                g.GetComponent<tk2dSprite>().renderer.enabled = this._enemy.renderer.enabled;
                float angle = (_ORBIT_GAP * i + 360.0f * orbitOffset).Clamp360();
                Vector2 avec   = angle.ToVector();
                Vector2 offset = new Vector2(_ORBIT_RAD_X * avec.x, _ORBIT_RAD_Y * avec.y);
                g.transform.position = this._enemy.sprite.WorldCenter + offset;
                g.transform.rotation = angle.EulerZ();
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
            foreach (AIActor otherEnemy in activeEnemies)
            {
                if (!(otherEnemy && otherEnemy.specRigidbody && !otherEnemy.IsGone && otherEnemy.healthHaver))
                    continue; // we don't care about harmless enemies
                if (enemy == otherEnemy)
                    continue; // don't apply damage to ourselves
                otherEnemy.gameObject.GetComponent<SoulLinkStatus>()?.ShareThePain(data.ModifiedDamage);
            }
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
            for (int i = 0; i < 3; ++i)
            {
                Vector2 finalpos = ppos + BraveMathCollege.DegreesToVector(120*i,1);
                GameObject v = SpawnManager.SpawnVFX(_SoulLinkHitVFX, finalpos, (120f*i).EulerZ());
                // v.transform.parent = enemyHH.transform;
                // _Vfx.SpawnAtPosition(finalpos.ToVector3ZisY(-1f /* -1 = above player sprite */), zRotation: 120*i, heightOffGround: -0.05f);
            }
        }
    }
}
