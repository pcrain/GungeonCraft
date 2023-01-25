using System;
using System.Collections.Generic;
using Gungeon;
using ItemAPI;
using EnemyAPI;
using UnityEngine;
//using DirectionType = DirectionalAnimation.DirectionType;
// using AnimationType = ItemAPI.BossBuilder.AnimationType;
using System.Collections;
using Dungeonator;
using System.Linq;
using Brave.BulletScript;
// using GungeonAPI;

namespace CwaffingTheGungy
{
  public static class BH
  {
    public static List<int> Range(int start, int end)
    {
      return Enumerable.Range(start, end-start+1).ToList();
    }
  }
}
