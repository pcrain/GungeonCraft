using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CwaffingTheGungy
{
    public class IDs
    {
        public static Dictionary<string, int> Pickups  { get; set; } = new Dictionary<string, int>();
        public static Dictionary<string, int> Guns     { get; set; } = new Dictionary<string, int>();
        public static Dictionary<string, int> Actives  { get; set; } = new Dictionary<string, int>();
        public static Dictionary<string, int> Passives { get; set; } = new Dictionary<string, int>();
    }
}
