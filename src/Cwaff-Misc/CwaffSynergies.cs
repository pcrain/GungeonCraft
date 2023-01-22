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
        // private const string[] noStrings = new string[0];
        private static void NewSynergy(string name, string[] mandatory, string[] optional = null)
        {
            if (optional != null)
                CustomSynergies.Add(name, mandatory.ToList(), optional.ToList());
            else
                CustomSynergies.Add(name, mandatory.ToList());
        }

        public static void Init()
        {
            // Makes Hyper Light Dasher 20% longer and reflect bullets
            NewSynergy("Hype Yourself Up", new[]{"cg:hld", "hyper_light_blaster"});
        }
    }
}


