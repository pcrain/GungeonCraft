using Dungeonator;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NevernamedsItems
{
    class AdvancedKillOnRoomClear : MonoBehaviour
    {
        public AdvancedKillOnRoomClear()
        {
            triggersOnRoomClear = true;
            triggersOnRoomUnseal = false;
            deathHasBegun = false;
        }
        public void Start()
        {
            self = base.GetComponent<AIActor>();
            if (self)
            {
                if (self.aiActor.ParentRoom != null)
                {
                    parentRoom = self.aiActor.ParentRoom;
                    if (triggersOnRoomClear) parentRoom.OnEnemiesCleared += RoomCleared;
                }
            }
        }
        private void OnDestroy()
        {
            if (self && parentRoom != null) parentRoom.OnEnemiesCleared -= RoomCleared;
        }

        private void Update()
        {
            if (self.enabled && self.behaviorSpeculator && self.behaviorSpeculator.enabled && (parentRoom == null || !parentRoom.IsSealed))
            {
                if (deathHasBegun || self.aiAnimator.IsPlaying("spawn") || self.aiAnimator.IsPlaying("awaken"))
                {
                    return;
                }
                self.enabled = false;
                ETGMod.StartGlobalCoroutine(DoDeath());
            }
        }
        private void RoomCleared()
        {
          if (deathHasBegun) ETGMod.StartGlobalCoroutine(DoDeath());
        }
        public IEnumerator DoDeath()
        {
            deathHasBegun = true;
            if (!string.IsNullOrEmpty(this.overrideDeathAnim) && self.aiAnimator)
            {
                bool flag = false;
                for (int i = 0; i < self.aiAnimator.OtherAnimations.Count; i++)
                {
                    if (self.aiAnimator.OtherAnimations[i].name == "death")
                    {
                        self.aiAnimator.OtherAnimations[i].anim.Type = DirectionalAnimation.DirectionType.Single;
                        self.aiAnimator.OtherAnimations[i].anim.Prefix = this.overrideDeathAnim;
                        flag = true;
                    }
                }
                if (!flag)
                {
                    AIAnimator.NamedDirectionalAnimation namedDirectionalAnimation = new AIAnimator.NamedDirectionalAnimation();
                    namedDirectionalAnimation.name = "death";
                    namedDirectionalAnimation.anim = new DirectionalAnimation();
                    namedDirectionalAnimation.anim.Type = DirectionalAnimation.DirectionType.Single;
                    namedDirectionalAnimation.anim.Prefix = this.overrideDeathAnim;
                    namedDirectionalAnimation.anim.Flipped = new DirectionalAnimation.FlipType[1];
                    self.aiAnimator.OtherAnimations.Add(namedDirectionalAnimation);
                }
            }
            if (this.preventExplodeOnDeath)
            {
                if (self.GetComponent<ExplodeOnDeath>()) self.GetComponent<ExplodeOnDeath>().enabled = false;
            }
            self.healthHaver.PreventAllDamage = false;
            self.healthHaver.ApplyDamage(100000f, Vector2.zero, "Death on Room Clear", CoreDamageTypes.None, DamageCategory.Unstoppable, true, null, false);

            yield return new WaitForSeconds(1f);

            if (self && self.healthHaver && self.healthHaver.IsAlive) self.EraseFromExistenceWithRewards(false);

            yield break;
        }

        private AIActor self;
        private RoomHandler parentRoom;

        public string overrideDeathAnim;
        public bool preventExplodeOnDeath;

        public bool triggersOnRoomClear;
        public bool triggersOnRoomUnseal;
        private bool deathHasBegun;

    }
}
