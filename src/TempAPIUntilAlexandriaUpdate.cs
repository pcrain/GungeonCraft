using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NevernamedsItems
{
    public class AdvancedTransmogrifyBehaviour : MonoBehaviour
    {
        public AdvancedTransmogrifyBehaviour()
        {
        }
        private void Start()
        {
            self = base.GetComponent<Projectile>();
            if (self) self.OnHitEnemy += this.OnHitEnemy;
        }
        private void OnHitEnemy(Projectile bullet, SpeculativeRigidbody enemy, bool fatal)
        {
            if (bullet && enemy && enemy.aiActor && enemy.healthHaver && !fatal && !enemy.healthHaver.IsBoss)
            {
                List<TransmogData> RandomisedList = RandomiseListOrder(TransmogDataList);
                foreach (TransmogData data in RandomisedList)
                {
                    if (UnityEngine.Random.value <= data.TransmogChance)
                    {
                        enemy.aiActor.Transmogrify(EnemyDatabase.GetOrLoadByGuid(BraveUtility.RandomElement(data.TargetGuids)), (GameObject)ResourceCache.Acquire("Global VFX/VFX_Item_Spawn_Poof"));
                        return;
                    }
                }
            }
        }
        private List<TransmogData> RandomiseListOrder(List<TransmogData> oldList)
        {
            List<TransmogData> oldList2 = new List<TransmogData>();
            oldList2.AddRange(oldList);
            List<TransmogData> newList = new List<TransmogData>();
            int oldListcount = oldList2.Count;
            for (int i = 0; i < oldListcount; i++)
            {
                TransmogData selectedData = BraveUtility.RandomElement(oldList2);
                newList.Add(selectedData);
                oldList2.Remove(selectedData);
            }
            return newList;
        }
        private Projectile self;
        public List<TransmogData> TransmogDataList = new List<TransmogData>();
        public class TransmogData
        {
            public List<string> TargetGuids = new List<string>();
            public float TransmogChance;
            public string identifier;
            public bool maintainHPPercent = false;
        }
    }
}
