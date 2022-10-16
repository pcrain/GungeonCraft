using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NevernamedsItems
{
    class MagnetBehaviour : MonoBehaviour
    {
        public MagnetBehaviour()
        {
            radius = 15f;
            gravitationalForce = 50f;
            debugMode = false;
        }
        public float radius;
        public float gravitationalForce;
        public float radiusSquared;
        public bool debugMode;
        public float statMult;
        private SpeculativeRigidbody baseBody;
        private void Start()
        {
            this.radiusSquared = this.radius * this.radius;
            baseBody = base.GetComponent<SpeculativeRigidbody>();
            PhysicsEngine.Instance.OnPreRigidbodyMovement += PreRigidMovement;
            base.GetComponent<Projectile>().OnDestruction += Destruction;
        }
        private void Destruction(Projectile proj)
        {
            base.GetComponent<Projectile>().OnDestruction -= Destruction;
            PhysicsEngine.Instance.OnPreRigidbodyMovement -= PreRigidMovement;
        }
        private void OnDestroy()
        {
            base.GetComponent<Projectile>().OnDestruction -= Destruction;
            PhysicsEngine.Instance.OnPreRigidbodyMovement -= PreRigidMovement;
        }
        private Vector2 GetFrameAccelerationForRigidbody(Vector2 unitCenter, float currentDistance, float g)
        {
            Vector2 zero = Vector2.zero;
            float num = Mathf.Clamp01(1f - currentDistance / this.radius);
            float d = g * num * num;
            Vector2 normalized = (baseBody.UnitCenter - unitCenter).normalized;
            return normalized * d;
        }
        private bool AdjustDebrisVelocity(DebrisObject debris)
        {
            if (debris.IsPickupObject) return false;
            if (debris.GetComponent<BlackHoleDoer>() != null || debris.GetComponent<MagnetBehaviour>() != null) return false;

            Vector2 a = debris.sprite.WorldCenter - baseBody.UnitCenter;
            float num = Vector2.SqrMagnitude(a);
            if (num >= radiusSquared) return false;

            float g = (gravitationalForce / 5) * statMult;
            float num2 = Mathf.Sqrt(num);

            Vector2 frameAccelerationForRigidbody = this.GetFrameAccelerationForRigidbody(debris.sprite.WorldCenter, num2, g);
            float d = Mathf.Clamp(BraveTime.DeltaTime, 0f, 0.02f);
            if (debris.HasBeenTriggered)
            {
                debris.ApplyVelocity(frameAccelerationForRigidbody * d);
            }
            else if (num2 < this.radius / 2f)
            {
                debris.Trigger(frameAccelerationForRigidbody * d, 0.5f, 1f);
            }
            return true;
        }
        private bool AdjustRigidbodyVelocity(SpeculativeRigidbody other)
        {
            Vector2 a = other.UnitCenter - baseBody.UnitCenter;
            float num = Vector2.SqrMagnitude(a);
            if (num < radiusSquared)
            {
                Vector2 velocity = other.Velocity;
                if (debugMode) ETGModConsole.Log("---------------------");
                if (debugMode) ETGModConsole.Log($"Checking Rigidbody: {other.name}");
                if (other.projectile || !other.aiActor || !other.aiActor.enabled || !other.aiActor.HasBeenEngaged || (other.healthHaver && other.healthHaver.IsBoss))
                {
                    if (debugMode)
                    {
                        ETGModConsole.Log("Rigidbody was invalid");
                        ETGModConsole.Log($"Projectile: {other.projectile != null}");
                        ETGModConsole.Log($"AiActor: {other.aiActor != null}");
                        if (other.aiActor) ETGModConsole.Log($"AiActor Enabled: {other.aiActor.enabled}");
                        if (other.aiActor) ETGModConsole.Log($"AiActor Engaged: {other.aiActor.HasBeenEngaged}");
                        ETGModConsole.Log($"IsBoss: {(other.healthHaver && other.healthHaver.IsBoss)}");
                    }
                    return false;
                }
                if (debugMode) ETGModConsole.Log($"Rigidbody was valid. Velocity: {velocity.magnitude}");
                Vector2 frameAccelerationForRigidbody = this.GetFrameAccelerationForRigidbody(other.UnitCenter, Mathf.Sqrt(num), (gravitationalForce * statMult));
                float d = Mathf.Clamp(BraveTime.DeltaTime, 0f, 0.02f);
                Vector2 b = frameAccelerationForRigidbody * d;
                Vector2 vector = velocity + b;
                if (BraveTime.DeltaTime > 0.02f) vector *= 0.02f / BraveTime.DeltaTime;
                if (debugMode) ETGModConsole.Log($"Target velocity vector: {vector.magnitude}");
                other.Velocity = vector;
                return true;
            }
            return false;
        }
        private void PreRigidMovement()
        {
            if (enabled && base.gameObject.activeSelf)
            {
                for (int i = 0; i < PhysicsEngine.Instance.AllRigidbodies.Count; i++)
                {
                    if (PhysicsEngine.Instance.AllRigidbodies[i].gameObject.activeSelf)
                    {
                        if (PhysicsEngine.Instance.AllRigidbodies[i].enabled)
                        {
                            this.AdjustRigidbodyVelocity(PhysicsEngine.Instance.AllRigidbodies[i]);
                        }
                    }
                }
                for (int j = 0; j < StaticReferenceManager.AllDebris.Count; j++)
                {
                    this.AdjustDebrisVelocity(StaticReferenceManager.AllDebris[j]);
                }
            }
        }
    }
}
