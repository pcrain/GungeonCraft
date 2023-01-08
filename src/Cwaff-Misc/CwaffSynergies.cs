using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

using ItemAPI;

namespace CwaffingTheGungy
{
    public static class CwaffSynergies
    {
        public static void Init()
        {
            // Makes Hyper Light Dasher 20% longer and reflect bullets
            List<string> mandatorySynergyItemsHypeYourselfUp = new List<string>() { "cg:hld", "hyper_light_blaster" };
            CustomSynergies.Add("Hype Yourself Up", mandatorySynergyItemsHypeYourselfUp);
        }
    }
}


