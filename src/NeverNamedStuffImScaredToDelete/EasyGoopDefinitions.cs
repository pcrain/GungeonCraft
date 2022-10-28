using ItemAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace CwaffingTheGungy
{
    class EasyGoopDefinitions
    {
        //Basegame Goops
        public static GoopDefinition FireDef;
        public static GoopDefinition OilDef;
        public static GoopDefinition PoisonDef;
        public static GoopDefinition BlobulonGoopDef;
        public static GoopDefinition WebGoop;
        public static GoopDefinition WaterGoop;
        public static GoopDefinition CharmGoopDef = PickupObjectDatabase.GetById(310)?.GetComponent<WingsItem>()?.RollGoop;
        public static GoopDefinition GreenFireDef = (PickupObjectDatabase.GetById(698) as Gun).DefaultModule.projectiles[0].GetComponent<GoopModifier>().goopDefinition;
        public static GoopDefinition CheeseDef    = (PickupObjectDatabase.GetById(808) as Gun).DefaultModule.projectiles[0].GetComponent<GoopModifier>().goopDefinition;

        public static List<GoopDefinition> ColorGoops = new List<GoopDefinition>();
        public static List<Color> ColorGoopColors     = new List<Color>
        {
            ExtendedColours.pink,
            Color.red,
            ExtendedColours.orange,
            Color.yellow,
            Color.green,
            Color.blue,
            ExtendedColours.purple,
            Color.cyan,
        };
        private static string[] baseGoopAssets = new string[]
        {
            "assets/data/goops/napalmgoopthatworks.asset",
            "assets/data/goops/oil goop.asset",
            "assets/data/goops/poison goop.asset",
            "assets/data/goops/blobulongoop.asset",
            "assets/data/goops/phasewebgoop.asset",
            "assets/data/goops/water goop.asset",
        };

        public static void DefineDefaultGoops()
        {
            //Sets up the goops that have to be extracted from asset bundles
            AssetBundle assetBundle = ResourceManager.LoadAssetBundle("shared_auto_001");
            List<GoopDefinition> baseGoops = new List<GoopDefinition>();
            foreach (string text in EasyGoopDefinitions.baseGoopAssets)
            {
                GoopDefinition goopDefinition;
                try
                {
                    GameObject gameObject = assetBundle.LoadAsset(text) as GameObject;
                    goopDefinition = gameObject.GetComponent<GoopDefinition>();
                }
                catch
                {
                    goopDefinition = (assetBundle.LoadAsset(text) as GoopDefinition);
                }
                goopDefinition.name = text.Replace("assets/data/goops/", "").Replace(".asset", "");
                baseGoops.Add(goopDefinition);
            }

            //Define the asset bundle goops
            FireDef         = baseGoops[0];
            OilDef          = baseGoops[1];
            PoisonDef       = baseGoops[2];
            BlobulonGoopDef = baseGoops[3];
            WebGoop         = baseGoops[4];
            WaterGoop       = baseGoops[5];

            //Define colored water goop for paintball gun
            for (int i = 0; i < ColorGoopColors.Count; i++)
            {
                GoopDefinition g   = UnityEngine.Object.Instantiate<GoopDefinition>(WaterGoop);
                g.CanBeElectrified = false;
                g.baseColor32      = ColorGoopColors[i];
                ColorGoops.Add(g);
            }
        }
    }

}
