using Dungeonator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NevernamedsItems
{
    public class GameObjectDamageAura : MonoBehaviour
    {
        public GameObjectDamageAura()
        {
            damageFallsOff = false;
            radius = 3;
            damagePerSecond = 5;
            startActivated = true;
        }
        public virtual void Start()
        {
            damageAuraActivated = startActivated;
            if (base.GetComponent<SpeculativeRigidbody>()) body = base.GetComponent<SpeculativeRigidbody>();
            if (base.GetComponent<tk2dSprite>()) sprite = base.GetComponent<tk2dSprite>();
        }
        private void Update()
        {          
            if (CenterPosition.GetAbsoluteRoom() != null && damageAuraActivated)
            {
                List<AIActor> activeEnemies = CenterPosition.GetAbsoluteRoom().GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
                if (activeEnemies != null)
                {
                    for (int i = 0; i < activeEnemies.Count; i++)
                    {
                        AIActor aiactor = activeEnemies[i];
                        if (aiactor.IsNormalEnemy && aiactor.healthHaver)
                        {
                            float distance = Vector2.Distance(CenterPosition, aiactor.CenterPosition);
                            if (distance <= ScaledRadius)
                            {

                                float damagethisframe = this.ScaledDamagePerSecond * BraveTime.DeltaTime;
                                if ( damageFallsOff) { damagethisframe = Mathf.Lerp(damagethisframe, 0f, (distance / ScaledRadius)); }

                                aiactor.healthHaver.ApplyDamage(damagethisframe, Vector2.zero, "Aura", CoreDamageTypes.None, DamageCategory.Normal, false, null, false);
                                TickedOnEnemy(aiactor);
                            }
                        }
                    }
                }
            }
        }
        public virtual void TickedOnEnemy(AIActor enemy)
        {

        }
        private float ScaledDamagePerSecond
        {
            get
            {
                return this.damagePerSecond;
            }
        }
        private float ScaledRadius
        {
            get
            {
                return this.radius;
            }
        }
        private Vector2 CenterPosition
        {
            get
            {
                if (sprite) return sprite.WorldCenter;
                if (body) return body.UnitCenter;
                return transform.position;
            }
        }
        private SpeculativeRigidbody body;
        private tk2dSprite sprite;

        public float damagePerSecond;
        public bool damageFallsOff;
        public float radius;

        public bool startActivated;
        public bool damageAuraActivated;
    }
}
