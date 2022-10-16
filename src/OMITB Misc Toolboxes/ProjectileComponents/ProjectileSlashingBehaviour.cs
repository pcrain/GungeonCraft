using Alexandria.Misc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NevernamedsItems
{
    public class ProjectileSlashingBehaviour : MonoBehaviour
    {
        public ProjectileSlashingBehaviour()
        {
            DestroyBaseAfterFirstSlash = false;
            timeBetweenSlashes = 1;
            SlashDamageUsesBaseProjectileDamage = true;
        }
        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            if (this.m_projectile.Owner && this.m_projectile.Owner is PlayerController) this.owner = this.m_projectile.Owner as PlayerController;
        }
        private void Update()
        {
            if (this.m_projectile)
            {
                if (timer > 0)
                {
                    timer -= BraveTime.DeltaTime;
                }
                if (timer <= 0)
                {
                    this.m_projectile.StartCoroutine(DoSlash(0, 0));
                    if (doSpinAttack)
                    {
                        this.m_projectile.StartCoroutine(DoSlash(90, 0.15f));
                        this.m_projectile.StartCoroutine(DoSlash(180, 0.30f));
                        this.m_projectile.StartCoroutine(DoSlash(-90, 0.45f));
                    }
                    timer = timeBetweenSlashes;
                }
            }
        }
        private IEnumerator DoSlash(float angle, float delay)
        {
            yield return new WaitForSeconds(delay);

            if (slashParameters == null) { slashParameters = new SlashData(); ETGModConsole.Log("Made a new slashparam"); }


            Projectile proj = this.m_projectile;
            List<GameActorEffect> effects = new List<GameActorEffect>();
            effects.AddRange(proj.GetFullListOfStatusEffects(true));

            SlashData instSlash = SlashData.CloneSlashData(slashParameters);

            if (SlashDamageUsesBaseProjectileDamage)
            {
                instSlash.damage = this.m_projectile.baseData.damage;
                instSlash.bossDamageMult = this.m_projectile.BossDamageMultiplier;
                instSlash.jammedDamageMult = this.m_projectile.BlackPhantomDamageMultiplier;
                instSlash.enemyKnockbackForce = this.m_projectile.baseData.force;
            }
            instSlash.OnHitTarget += SlashHitTarget;

            SlashDoer.DoSwordSlash(this.m_projectile.specRigidbody.UnitCenter, (this.m_projectile.Direction.ToAngle() + angle), owner, instSlash);

            if (DestroyBaseAfterFirstSlash) StartCoroutine(Suicide());
            yield break;
        }
        private IEnumerator Suicide()
        {
            yield return null;
            UnityEngine.Object.Destroy(this.m_projectile.gameObject);
            yield break;
        }
        public virtual void SlashHitTarget(GameActor target, bool fatal)
        {

        }

        private float timer;
        public float timeBetweenSlashes;
        public bool doSpinAttack;
        public bool SlashDamageUsesBaseProjectileDamage;
        public bool DestroyBaseAfterFirstSlash;
        public SlashData slashParameters;
        private Projectile m_projectile;
        private PlayerController owner;
    }
}
