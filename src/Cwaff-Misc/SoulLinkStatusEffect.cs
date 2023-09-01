using ItemAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

using Dungeonator;

namespace CwaffingTheGungy
{
    public class SoulLinkStatus : MonoBehaviour
    {
        public static GameActorHealthEffect StandardSoulLinkEffect;

        private const int   _ORBIT_NUM       = 3;
        private const float _ORBIT_RPS       = 0.5f;
        private const float _ORBIT_RAD_X     = 1.0f;
        private const float _ORBIT_RAD_Y     = 0.4f;
        private const float _ORBIT_SPR       = 1.0f / _ORBIT_RPS; // seconds per rotation
        private const float _ORBIT_GAP       = 360.0f / (float)_ORBIT_NUM;
        private const float _EFFECT_COOLDOWN = 0.01f; // MUST be nonzero or game can freeze

        internal static VFXPool _SoulLinkHitVFXPool      = null;
        internal static GameObject _SoulLinkHitVFX       = null;
        internal static GameObject _SoulLinkOverheadVFX  = null;

        private AIActor _enemy;
        private float _cooldown;
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
                AppliesTint              = true,
                TintColor                = ExtendedColours.pink,
                AppliesDeathTint         = false,
            };
        }

        private void Start()
        {
            this._enemy        = base.GetComponent<AIActor>();
            this._cooldown     = 0;
            this._orbitTimer   = 0;
            this._orbitals     = new();

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

            if (this._cooldown > 0f)
                this._cooldown -= BraveTime.DeltaTime;

            this._orbitTimer += BraveTime.DeltaTime;
            if (this._orbitTimer > _ORBIT_SPR)
                this._orbitTimer -= _ORBIT_SPR;
            float orbitOffset = this._orbitTimer / _ORBIT_SPR;

            int i = 0;
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

        private void OnTakeDamage(HealthHaver enemy, HealthHaver.ModifyDamageEventArgs data)
        {
            List<AIActor> activeEnemies = enemy.aiActor.GetAbsoluteParentRoom().GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
            if (activeEnemies == null)
                return;
            foreach (AIActor otherEnemy in activeEnemies)
            {
                if (!(otherEnemy && otherEnemy.specRigidbody && !otherEnemy.IsGone && otherEnemy.healthHaver))
                    continue;
                otherEnemy.gameObject.GetComponent<SoulLinkStatus>()?.TryApplyDamage(otherEnemy.healthHaver);
            }
        }

        private void HandleEnemyDied()
        {
            foreach (GameObject g in this._orbitals)
                UnityEngine.Object.Destroy(g);
        }

        public void TryApplyDamage(HealthHaver enemyHH)
        {
            if (this._cooldown > 0)
                return;

            this._cooldown = _EFFECT_COOLDOWN;
            enemyHH.ApplyDamage(1f, new Vector2(10f,0f), "Soul Link",
                CoreDamageTypes.Magic, DamageCategory.Unstoppable,
                true, null, false);
            enemyHH.knockbackDoer.ApplyKnockback(new Vector2(10f,0f), 2f);

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


