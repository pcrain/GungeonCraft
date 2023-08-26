using ItemAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

using Dungeonator;

namespace CwaffingTheGungy
{
    class SoulLinkStatusEffectSetup
    {
        public static GameActorSoulLinkEffect StandardSoulLinkEffect;
        public static GameObject SoulLinkOverheadVFX;

        public static void Init()
        {
            SoulLinkOverheadVFX = VFX.animations["PlagueOverhead"];
            GameActorSoulLinkEffect StandSoulLink = GenerateSoulLinkEffect(
                100, 2, true, Color.green, true, Color.green);
            StandardSoulLinkEffect = StandSoulLink;
        }

        public static GameActorSoulLinkEffect GenerateSoulLinkEffect(float duration, float dps, bool tintEnemy, Color bodyTint, bool tintCorpse, Color corpseTint)
        {
            GameActorSoulLinkEffect soulLink = new GameActorSoulLinkEffect
            {
                duration                 = 60,
                effectIdentifier         = "SoulLink",
                resistanceType           = EffectResistanceType.None,
                DamagePerSecondToEnemies = 0,
                ignitesGoops             = false,
                OverheadVFX              = SoulLinkOverheadVFX,
                AffectsEnemies           = true,
                AffectsPlayers           = false,
                AppliesOutlineTint       = true,
                PlaysVFXOnActor          = false,
                AppliesTint              = tintEnemy,
                AppliesDeathTint         = tintCorpse,
                TintColor                = bodyTint,
                DeathTintColor           = corpseTint,
            };
            return soulLink;
        }
    }
    public class GameActorSoulLinkEffect : GameActorHealthEffect
    {
        public GameActorSoulLinkEffect()
        {
            this.DamagePerSecondToEnemies = 1f;
            this.TintColor                = Color.green;
            this.DeathTintColor           = Color.green;
            this.AppliesTint              = true;
            this.AppliesDeathTint         = true;
        }

        public override void EffectTick(GameActor actor, RuntimeGameActorEffectData effectData)
        {
            base.EffectTick(actor, effectData);
        }
    }

    public class SoulLinkProjectile : MonoBehaviour
    {
        private Projectile m_projectile;

        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            this.m_projectile.OnHitEnemy += this.OnHitEnemy;
        }

        private void OnHitEnemy(Projectile bullet, SpeculativeRigidbody enemy, bool what)
        {
            if (enemy.aiActor.gameObject.GetComponent<UnderEffectsOfSoulLink>())
                return;
            enemy.aiActor.ApplyEffect(SoulLinkStatusEffectSetup.StandardSoulLinkEffect);
            var comp = enemy.aiActor.gameObject.AddComponent<UnderEffectsOfSoulLink>();
            enemy.healthHaver.ModifyDamage += this.OnTakeDamage;
        }

        private void OnTakeDamage(HealthHaver enemy, HealthHaver.ModifyDamageEventArgs data)
        {
            List<AIActor> activeEnemies = enemy.aiActor.GetAbsoluteParentRoom().GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
            if (activeEnemies == null)
                return;
            for (int i = 0; i < activeEnemies.Count; i++)
            {
                AIActor otherEnemy = activeEnemies[i];
                if (!(otherEnemy && otherEnemy.specRigidbody && !otherEnemy.IsGone && otherEnemy.healthHaver))
                    continue;
                var comp = otherEnemy.gameObject.GetComponent<UnderEffectsOfSoulLink>();
                if (!comp)
                    continue;
                comp.TryApplyDamage(otherEnemy.healthHaver);
            }
        }
    }

    public class UnderEffectsOfSoulLink : MonoBehaviour
    {
        private AIActor m_enemy;
        private float m_cooldown;
        private static VFXPool vfx = null;

        private void Start()
        {
            vfx ??= VFX.CreatePoolFromVFXGameObject((ItemHelper.Get(Items.MagicLamp) as Gun).DefaultModule.projectiles[0].hitEffects.overrideMidairDeathVFX);
            this.m_enemy = base.GetComponent<AIActor>();
            this.m_cooldown = 0;
        }

        public void TryApplyDamage(HealthHaver enemyHH)
        {
            if (this.m_cooldown > 0)
                return;
            this.m_cooldown = 0.1f;
            enemyHH.ApplyDamage(1f, new Vector2(10f,0f), "Soul Link",
                CoreDamageTypes.Magic, DamageCategory.Collision,
                false, null, false);
            enemyHH.knockbackDoer.ApplyKnockback(new Vector2(10f,0f), 2f);

            Vector2 ppos = this.m_enemy.sprite.WorldCenter;
            for (int i = 0; i < 3; ++i)
            {
                Vector2 finalpos = ppos + BraveMathCollege.DegreesToVector(120*i,1);
                vfx.SpawnAtPosition(
                    finalpos.ToVector3ZisY(-1f), /* -1 = above player sprite */
                    120*i,
                    null, null, null, -0.05f);
            }
        }

        private void Update()
        {
            if (this.m_cooldown > 0f)
                this.m_cooldown -= BraveTime.DeltaTime;
        }
    }
}


