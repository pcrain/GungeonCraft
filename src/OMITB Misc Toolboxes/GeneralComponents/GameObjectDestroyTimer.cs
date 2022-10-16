using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NevernamedsItems
{
    public class GameObjectDestroyTimer : MonoBehaviour
    {
        public GameObjectDestroyTimer()
        {
            this.secondsTillDeath = 1;
        }
        private void Start()
        {
            timer = secondsTillDeath;

        }
        private void FixedUpdate()
        {
            if (base.gameObject != null)
            {
                if (timer > 0)
                {
                    timer -= BraveTime.DeltaTime;
                }
                if (timer <= 0)
                {
                    UnityEngine.Object.Destroy(base.gameObject);
                }
            }
        }

        public float secondsTillDeath;
        private float timer;
    }
}
