using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NevernamedsItems
{
    public class SpawnObjectBehaviour : MonoBehaviour
    {
		public SpawnObjectBehaviour()
        {
			canBounce = true;
			tossForce = 5f;
		}
        private void Start()
        {
            self = base.GetComponent<Projectile>();
            if (self && self.specRigidbody) specBody = self.specRigidbody;
               
            self.OnDestruction += OnDestroyed;
        }
        private void OnDestroyed(Projectile victim)
        {
			Vector2 positionToSpawn = victim.specRigidbody.UnitCenter;

			GameObject spawnedObject = UnityEngine.Object.Instantiate<GameObject>(objectToSpawn, positionToSpawn, Quaternion.identity);
			tk2dBaseSprite spawnedSprite = spawnedObject.GetComponent<tk2dBaseSprite>();
			if (spawnedSprite) { spawnedSprite.PlaceAtPositionByAnchor(positionToSpawn, tk2dBaseSprite.Anchor.MiddleCenter); }

			DebrisObject debrisObject = LootEngine.DropItemWithoutInstantiating(spawnedObject, spawnedObject.transform.position, UnityEngine.Random.insideUnitCircle, this.tossForce, false, false, true, false);

			debrisObject.IsAccurateDebris = true;
			debrisObject.Priority = EphemeralObject.EphemeralPriority.Critical;
			debrisObject.bounceCount = ((!this.canBounce) ? 0 : 1);
		}
        public GameObject objectToSpawn;
        public float tossForce;
		public bool canBounce;

        private Projectile self;
        private SpeculativeRigidbody specBody;
    }
}
