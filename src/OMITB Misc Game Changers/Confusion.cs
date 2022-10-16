using Alexandria.ItemAPI;
using Alexandria.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NevernamedsItems
{
    class Confusion
    {
        //public static GameObject PlagueOverheadVFX;

        public static void Init()
        {
            ConfusionDecoyTarget = new GameObject();
            SpeculativeRigidbody orAddComponent = ConfusionDecoyTarget.GetOrAddComponent<SpeculativeRigidbody>();
            PixelCollider pixelCollider = new PixelCollider();
            pixelCollider.ColliderGenerationMode = PixelCollider.PixelColliderGeneration.Manual;
            pixelCollider.CollisionLayer = CollisionLayer.EnemyCollider;
            pixelCollider.ManualWidth = 5;
            pixelCollider.ManualHeight = 5;
            pixelCollider.ManualOffsetX = 0;
            pixelCollider.ManualOffsetY = 0;
            orAddComponent.PixelColliders = new List<PixelCollider>
            {
                pixelCollider
            };
            ConfusionDecoyTarget.AddComponent<ConfusionDecoyTargetController>();
            ConfusionDecoyTarget.MakeFakePrefab();



            //Setup the standard Plague effect
            
            StaticStatusEffects.ConfusionEffect = StatusEffectHelper.GenerateConfusionEfffect(5);
        }
        public static GameObject ConfusionDecoyTarget;
    }
    public class ConfusionDecoyTargetController : MonoBehaviour
    {
        public ConfusionDecoyTargetController()
        {

        }
        private void Start()
        {
            baseBody = base.GetComponent<SpeculativeRigidbody>();
        }
        private void Update()
        {
            if (baseBody && surroundObject && GameManager.Instance.PrimaryPlayer)
            {
                Vector2 betweenPlayerAndObject = GameManager.Instance.PrimaryPlayer.specRigidbody.UnitCenter.CalculateVectorBetween(surroundObject.UnitCenter);
                Vector2 realPos = surroundObject.UnitCenter + (betweenPlayerAndObject.normalized) * 5;
                transform.position = realPos;
                baseBody.Reinitialize();
                UnityEngine.Object.Instantiate<GameObject>(EasyVFXDatabase.YellowLaserCircleVFX, baseBody.UnitCenter, Quaternion.identity);
            }
            if (baseBody && surroundObject && surroundObject.aiActor != null)
            {
                surroundObject.aiActor.OverrideTarget = baseBody;
            }

            if (surroundObject == null)
            {
                UnityEngine.Object.Destroy(base.gameObject);
            }
            else if (surroundObject.healthHaver != null && surroundObject.healthHaver.IsDead)
            {
                UnityEngine.Object.Destroy(base.gameObject);
            }
        }
        private SpeculativeRigidbody baseBody;
        public SpeculativeRigidbody surroundObject;
    }
    public class GameActorConfusionEffect : GameActorEffect
    {
        public GameActorConfusionEffect()
        {
        }
        public override void OnEffectApplied(GameActor actor, RuntimeGameActorEffectData effectData, float partialAmount = 1)
        {
            if (actor && actor.specRigidbody)
            {
                GameObject newDecoy = UnityEngine.Object.Instantiate<GameObject>(Confusion.ConfusionDecoyTarget, actor.CenterPosition, Quaternion.identity);
                newDecoy.GetComponent<ConfusionDecoyTargetController>().surroundObject = actor.specRigidbody;
                extantConfusionDecoy = newDecoy;
            }

            base.OnEffectApplied(actor, effectData, partialAmount);
        }
        private GameObject extantConfusionDecoy;
        public override void OnEffectRemoved(GameActor actor, RuntimeGameActorEffectData effectData)
        {
            if (extantConfusionDecoy)
            {
                UnityEngine.Object.Destroy(extantConfusionDecoy);
            }
            base.OnEffectRemoved(actor, effectData);
        }
    }
}