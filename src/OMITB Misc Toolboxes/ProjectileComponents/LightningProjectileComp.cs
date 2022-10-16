using Alexandria.Misc;
using Dungeonator;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NevernamedsItems
{
    public class LightningProjectileComp : MonoBehaviour
    {
        public LightningProjectileComp()
        {
            initialAngle = float.NegativeInfinity;
            targetEnemies = true;
            wiggleCooldown = 0.005f;
            arcToEnemyRange = 4;
            arcBetweenEnemiesRange = 4;
        }
        private void Start()
        {
            curTimer = wiggleCooldown;
            self = base.GetComponent<Projectile>();
            self.OnHitEnemy += OnHitEnemy;

            owner = self.ProjectilePlayerOwner();
            if (self.GetComponent<BounceProjModifier>()) self.GetComponent<BounceProjModifier>().OnBounceContext += OnBounce;
        }
        private void OnBounce(BounceProjModifier mod, SpeculativeRigidbody collider) { initialAngle = self.Direction.ToAngle(); }

        
        private void Update()
        {
            if (self)
            {
                 if (curTimer > 0) { curTimer -= BraveTime.DeltaTime; }
                 else
                 {
                    if (initialAngle == float.NegativeInfinity) initialAngle = self.Direction.ToAngle(); //If initial angle is not set to the placeholder, set it
                    float newArc = ProjSpawnHelper.GetAccuracyAngled(initialAngle, 80, owner); //Determine accuracy

                    if (targetEnemies)
                    {
                        AIActor nearest = self.specRigidbody.UnitCenter.GetNearestEnemyToPosition(true, RoomHandler.ActiveEnemyType.All, new List<AIActor>() { lastHitEnemy, secondToLastHitEnemy });
                        if (nearest != null && Vector2.Distance(self.specRigidbody.UnitCenter, GivenActorPos(nearest)) < arcToEnemyRange)
                        {
                            newArc = self.specRigidbody.UnitCenter.CalculateVectorBetween(GivenActorPos(nearest)).ToAngle();
                        }
                    }
                    self.SendInDirection(newArc.DegreeToVector2(), false, true); //Send the projectile in the new direction

                    curTimer = wiggleCooldown;
                 }
            }
        }
        private void OnHitEnemy(Projectile self, SpeculativeRigidbody enemy, bool fatal)
        {
            if (enemy && enemy.aiActor)
            {
                if (enemy.aiActor != lastHitEnemy)
                {
                    if (lastHitEnemy != null) secondToLastHitEnemy = lastHitEnemy;
                    lastHitEnemy = enemy.aiActor;
                }

                //Check if there's another valid enemy around to arc to
                if (targetEnemies)
                {
                    AIActor nearest = enemy.UnitCenter.GetNearestEnemyToPosition(true, RoomHandler.ActiveEnemyType.All, new List<AIActor>() { lastHitEnemy, secondToLastHitEnemy });
                    if (nearest != null && Vector2.Distance(enemy.UnitCenter, GivenActorPos(nearest)) < arcBetweenEnemiesRange)
                    {
                        PierceProjModifier piercing = self.gameObject.GetComponent<PierceProjModifier>();
                        if (piercing != null) piercing.penetration++;
                        else self.gameObject.AddComponent<PierceProjModifier>();

                        float newArc = self.specRigidbody.UnitCenter.CalculateVectorBetween(GivenActorPos(nearest)).ToAngle();
                        self.SendInDirection(newArc.DegreeToVector2(), true, true); //Send the projectile in the new direction

                    curTimer = wiggleCooldown;
                    }
                }

            }
        }
       
        private Vector2 GivenActorPos(AIActor actor)
        {
            if (actor.sprite != null) return actor.sprite.WorldCenter;
            else if (actor.specRigidbody != null) return actor.specRigidbody.UnitCenter;
            else return actor.transform.position;
        }     
        
        //Public
        public bool targetEnemies;
        public float wiggleCooldown;
        public float arcToEnemyRange;
        public float arcBetweenEnemiesRange;

        //Private
        private float initialAngle;
        private float curTimer;
        private Projectile self;
        private PlayerController owner;
        private AIActor lastHitEnemy;
        private AIActor secondToLastHitEnemy;
    }
}
