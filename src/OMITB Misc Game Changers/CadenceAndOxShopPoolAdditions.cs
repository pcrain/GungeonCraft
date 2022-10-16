using SaveAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NevernamedsItems
{
    class CadenceAndOxShopPoolAdditions
    {
        public static void Init()
        {
            BreachShopTool.AddBaseMetaShopTier(
                RiskRifle.RiskRifleID, 20, 
                Creditor.CreditorID, 25, 
                OverpricedHeadband.OverpricedHeadbandID, 35               
                );
            BreachShopTool.AddBaseMetaShopTier(
                PowerArmour.PowerArmourID, 10,
                Recyclinder.RecyclinderID, 25,
                Blasmaster.BlasmasterID, 10
                );
            BreachShopTool.AddBaseMetaShopTier(
                DartRifle.DartRifleID, 30,
                Demolitionist.DemolitionistID, 25,
                Repeatovolver.RepeatovolverID, 20
                );
            BreachShopTool.AddBaseMetaShopTier(
                Spiral.SpiralID, 40,
                StunGun.StunGunID, 10,
                DroneCompanion.DroneID, 25
                );
            BreachShopTool.AddBaseMetaShopTier(
                Autogun.AutogunID, 20,
                Rebondir.RebondirID, 30,
                Converter.ConverterID, 40
                );
        }
    }
}
