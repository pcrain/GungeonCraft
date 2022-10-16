using Dungeonator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NevernamedsItems
{
  public  class AffectEnemiesInProximityTickItem : PassiveItem
    {
        public AffectEnemiesInProximityTickItem()
        {
            rangeMultSynergy = "";
            synergyRangeMult = 1;
            range = 2;
        }
        public string rangeMultSynergy;
        public float synergyRangeMult;
        public float range;
        public override void Update()
        {
            if (Owner && Owner.CurrentRoom != null)
            {
                List<AIActor> activeEnemies = Owner.CurrentRoom.GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
                if (activeEnemies != null)
                {
                    for (int i = 0; i < activeEnemies.Count; i++)
                    {
                        AIActor aiactor = activeEnemies[i];
                        if (aiactor.IsNormalEnemy)
                        {
                            float instRange = range;
                            if (!string.IsNullOrEmpty(rangeMultSynergy) && Owner.PlayerHasActiveSynergy(rangeMultSynergy)) instRange *= synergyRangeMult;
                            float num = Vector2.Distance(Owner.CenterPosition, aiactor.CenterPosition);
                            if (num <= instRange)
                            {
                                this.AffectEnemy(aiactor);
                            }
                        }
                    }
                }
            }
            base.Update();
        }
        public virtual void AffectEnemy(AIActor aiactor)
        {
            
        }
    }
}
