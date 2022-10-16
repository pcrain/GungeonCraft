using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NevernamedsItems
{
    public class GameActorSizeEffect : GameActorEffect
    {
        public GameActorSizeEffect()
        {
            this.TintColor = ExtendedColours.plaguePurple;
            this.DeathTintColor = ExtendedColours.plaguePurple;
            this.AppliesTint = false;
            this.AppliesDeathTint = false;
            newScaleMultiplier = new Vector2(0.4f, 0.4f);
            adjustsSpeed = false;
        }
        public Vector2 newScaleMultiplier;
        public bool adjustsSpeed;
        public override void OnEffectApplied(GameActor actor, RuntimeGameActorEffectData effectData, float partialAmount = 1)
        {
            base.OnEffectApplied(actor, effectData, partialAmount);
            if (actor.aiActor)
            {
                if (actor.healthHaver && (actor.healthHaver.IsBoss || actor.healthHaver.IsSubboss)) return;
                SpecialSizeStatModification sizeStats = actor.gameObject.GetOrAddComponent<SpecialSizeStatModification>();
                sizeStats.canBeSteppedOn = true;
                sizeStats.adjustsSpeed = adjustsSpeed;
                Vector2 newSize = new Vector2((actor.aiActor.EnemyScale.x * newScaleMultiplier.x), (actor.aiActor.EnemyScale.y * newScaleMultiplier.y));
                actor.StartCoroutine(LerpToSize(actor.aiActor, newSize));
            }
        }
        private IEnumerator LerpToSize(AIActor target, Vector2 targetScale)
        {
            float elapsed = 0f;
            Vector2 startScale = target.EnemyScale;
            int cachedLayer = target.gameObject.layer;
            int cachedOutlineLayer = cachedLayer;
            target.gameObject.layer = LayerMask.NameToLayer("Unpixelated");
            cachedOutlineLayer = SpriteOutlineManager.ChangeOutlineLayer(target.sprite, LayerMask.NameToLayer("Unpixelated"));
            

            while (elapsed < 1)
            {
                elapsed += target.LocalDeltaTime;
                target.EnemyScale = Vector2.Lerp(startScale, targetScale, elapsed / 1);
                yield return null;
            }
        }
        public override void OnEffectRemoved(GameActor actor, RuntimeGameActorEffectData effectData)
        {
            base.OnEffectRemoved(actor, effectData);
            if (actor.aiActor)
            {
                if (actor.healthHaver && (actor.healthHaver.IsBoss || actor.healthHaver.IsSubboss)) return;
                Vector2 newSize = new Vector2((actor.aiActor.EnemyScale.x / newScaleMultiplier.x), (actor.aiActor.EnemyScale.y / newScaleMultiplier.y));
                actor.StartCoroutine(LerpToSize(actor.aiActor, newSize));
            }
        }
    }
}
