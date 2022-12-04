using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Gungeon;
using Dungeonator;
using SaveAPI;
using System.Collections;

namespace CwaffingTheGungy
{
    public class Commands
    {
        public static void Init()
        {
            // Base command for doing whatever I'm testing at the moment
            ETGModConsole.Commands.AddGroup("gg", delegate (string[] args)
            {
                LootEngine.SpawnItem(
                    // PickupObjectDatabase.GetById(IDs.Actives["borrowed_time"]).gameObject,
                    // PickupObjectDatabase.GetById(IDs.Passives["shine"]).gameObject,
                    // PickupObjectDatabase.GetById(IDs.Guns["ki_blast"]).gameObject,
                    // PickupObjectDatabase.GetById(IDs.Pickups["superstitious"]).gameObject,
                    PickupObjectDatabase.GetById(IDs.Pickups["deadline"]).gameObject,
                    GameManager.Instance.PrimaryPlayer.CenterPosition,
                    Vector2.zero,
                    0);
                // ETGModConsole.Log("<size=100><color=#ff0000ff>Please specify a command. Type 'nn help' for a list of commands.</color></size>", false);
            });
        }
       
    }
}

