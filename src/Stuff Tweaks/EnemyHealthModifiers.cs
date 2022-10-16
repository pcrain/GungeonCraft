using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NevernamedsItems
{
    class EnemyHealthModifiers
    {
        public static void Init()
        {
            ETGMod.AIActor.OnPostStart += onEnemyPostSpawn;
        }
        public static void onEnemyPostSpawn(AIActor enemy)
        {
            if (enemy && !string.IsNullOrEmpty(enemy.EnemyGuid) && enemy.healthHaver != null)
            {
                if (enemy.EnemyGuid == EnemyGuidDatabase.Entries["ammoconda_ball"] && enemy.GetAbsoluteParentRoom().area.PrototypeRoomCategory == PrototypeDungeonRoom.RoomCategory.BOSS)
                {
                    if (enemy.healthHaver.GetMaxHealth() > (15 * AIActor.BaseLevelHealthModifier))
                    {
                        float newHP = 15 * AIActor.BaseLevelHealthModifier;
                        enemy.healthHaver.ForceSetCurrentHealth(newHP);
                        enemy.healthHaver.SetHealthMaximum(newHP);
                    }
                }
                if (enemy.EnemyGuid == EnemyGuidDatabase.Entries["black_skusket"])
                {
                    if (enemy.healthHaver.GetMaxHealth() > (10 * AIActor.BaseLevelHealthModifier))
                    {
                        float newHP = 10 * AIActor.BaseLevelHealthModifier;
                        enemy.healthHaver.ForceSetCurrentHealth(newHP);
                        enemy.healthHaver.SetHealthMaximum(newHP);
                    }
                }
            }
        }
    }
}
