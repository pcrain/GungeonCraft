using Dungeonator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NevernamedsItems
{
    class GlobalUpdate : MonoBehaviour
    {
        public void Update()
        {
            if (Dungeon.IsGenerating != DungeonWasGeneratingLastChecked)
            {
                if (GameManager.Instance)
                {
                    if (Dungeon.IsGenerating && !DungeonWasGeneratingLastChecked)
                    {
                        List<PlayerController> player = new List<PlayerController>();
                        if (GameManager.Instance.PrimaryPlayer != null) player.Add(GameManager.Instance.PrimaryPlayer);
                        if (GameManager.Instance.SecondaryPlayer != null) player.Add(GameManager.Instance.SecondaryPlayer);
                        FloorAndGenerationToolbox.OnFloorUnloaded(player);
                    }
                    if (!Dungeon.IsGenerating && DungeonWasGeneratingLastChecked)
                    {
                        FloorAndGenerationToolbox.OnFloorLoaded();
                    }
                }
                DungeonWasGeneratingLastChecked = Dungeon.IsGenerating;
            }
        }
        public static bool DungeonWasGeneratingLastChecked;
    }
}
