using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ItemAPI;
using UnityEngine;

namespace NevernamedsItems
{
    class SynergyFormInitialiser
    {
        public static void AddSynergyForms()
        {
            //------------------------------------------------------SYNERGY FORMES
            #region PreBigUpdate
            //GUNSHARK - MEGASHARK SYNERGY FORM
            AdvancedTransformGunSynergyProcessor MegaSharkSynergyForme = (PickupObjectDatabase.GetById(Gunshark.GunsharkID) as Gun).gameObject.AddComponent<AdvancedTransformGunSynergyProcessor>();
            MegaSharkSynergyForme.NonSynergyGunId = Gunshark.GunsharkID;
            MegaSharkSynergyForme.SynergyGunId = GunsharkMegasharkSynergyForme.GunsharkMegasharkSynergyFormeID;
            MegaSharkSynergyForme.SynergyToCheck = "Megashark";
            //DISC GUN - SUPER DISC FORM
            AdvancedTransformGunSynergyProcessor SuperDiscSynergyForme = (PickupObjectDatabase.GetById(DiscGun.DiscGunID) as Gun).gameObject.AddComponent<AdvancedTransformGunSynergyProcessor>();
            SuperDiscSynergyForme.NonSynergyGunId = DiscGun.DiscGunID;
            SuperDiscSynergyForme.SynergyGunId = DiscGunSuperDiscForme.DiscGunSuperDiscSynergyFormeID;
            SuperDiscSynergyForme.SynergyToCheck = "Super Disc";
            //ORGUN - HEADACHE FORM
            AdvancedTransformGunSynergyProcessor HeadacheSynergyForme = (PickupObjectDatabase.GetById(Orgun.OrgunID) as Gun).gameObject.AddComponent<AdvancedTransformGunSynergyProcessor>();
            HeadacheSynergyForme.NonSynergyGunId = Orgun.OrgunID;
            HeadacheSynergyForme.SynergyGunId = OrgunHeadacheSynergyForme.OrgunHeadacheSynergyFormeID;
            HeadacheSynergyForme.SynergyToCheck = "Headache";
            //MINI GUN - MINI SHOTGUN FORM
            AdvancedTransformGunSynergyProcessor MiniShotgunSynergyForme = (PickupObjectDatabase.GetById(NNMinigun.MiniGunID) as Gun).gameObject.AddComponent<AdvancedTransformGunSynergyProcessor>();
            MiniShotgunSynergyForme.NonSynergyGunId = NNMinigun.MiniGunID;
            MiniShotgunSynergyForme.SynergyGunId = MinigunMiniShotgunSynergyForme.MiniShotgunID;
            MiniShotgunSynergyForme.SynergyToCheck = "Mini Shotgun";
            //DOGGUN - DISCORD AND RHYME (WOLFGUN) FORME
            AdvancedTransformGunSynergyProcessor WolfgunSynergyForme = (PickupObjectDatabase.GetById(Corgun.DoggunID) as Gun).gameObject.AddComponent<AdvancedTransformGunSynergyProcessor>();
            WolfgunSynergyForme.NonSynergyGunId = Corgun.DoggunID;
            WolfgunSynergyForme.SynergyGunId = Wolfgun.WolfgunID;
            WolfgunSynergyForme.SynergyToCheck = "Discord and Rhyme";
            #endregion
            #region BigUpdate (1.14)
            //PENCIL - MIGHTIER THAN THE GUN FORME
            AdvancedTransformGunSynergyProcessor MightierThanTheGunForme = (PickupObjectDatabase.GetById(Pencil.pencilID) as Gun).gameObject.AddComponent<AdvancedTransformGunSynergyProcessor>();
            MightierThanTheGunForme.NonSynergyGunId = Pencil.pencilID;
            MightierThanTheGunForme.SynergyGunId = PenPencilSynergy.penID;
            MightierThanTheGunForme.SynergyToCheck = "Mightier Than The Gun";
            //REKEYTER - RESHELLETONKEYTER
            AdvancedTransformGunSynergyProcessor ReShelletonKeyterForme = (PickupObjectDatabase.GetById(Rekeyter.RekeyterID) as Gun).gameObject.AddComponent<AdvancedTransformGunSynergyProcessor>();
            ReShelletonKeyterForme.NonSynergyGunId = Rekeyter.RekeyterID;
            ReShelletonKeyterForme.SynergyGunId = ReShelletonKeyter.ReShelletonKeyterID;
            ReShelletonKeyterForme.SynergyToCheck = "ReShelletonKeyter";
            //AM0 - SPREAD FORME
            AdvancedTransformGunSynergyProcessor AM0SpreadForm = (PickupObjectDatabase.GetById(AM0.AM0ID) as Gun).gameObject.AddComponent<AdvancedTransformGunSynergyProcessor>();
            AM0SpreadForm.NonSynergyGunId = AM0.AM0ID;
            AM0SpreadForm.SynergyGunId = AM0SpreadForme.AM0SpreadFormeID;
            AM0SpreadForm.SynergyToCheck = "Spreadshot";
            //BULLET BLADE - GHOST SWORD
            AdvancedTransformGunSynergyProcessor GhostBladeForme = (PickupObjectDatabase.GetById(BulletBlade.BulletBladeID) as Gun).gameObject.AddComponent<AdvancedTransformGunSynergyProcessor>();
            GhostBladeForme.NonSynergyGunId = BulletBlade.BulletBladeID;
            GhostBladeForme.SynergyGunId = BulletBladeGhostForme.GhostBladeID;
            GhostBladeForme.SynergyToCheck = "GHOST SWORD!!!";
            //HOT GLUE GUN - GLUE GUNNER
            AdvancedTransformGunSynergyProcessor GlueGunnerForme = (PickupObjectDatabase.GetById(HotGlueGun.HotGlueGunID) as Gun).gameObject.AddComponent<AdvancedTransformGunSynergyProcessor>();
            GlueGunnerForme.NonSynergyGunId = HotGlueGun.HotGlueGunID;
            GlueGunnerForme.SynergyGunId = GlueGunGlueGunnerSynergy.GlueGunnerID;
            GlueGunnerForme.SynergyToCheck = "Glue Gunner";
            //BULLATTERER - KING BULLATTERER
            AdvancedTransformGunSynergyProcessor KingBullattererForme = (PickupObjectDatabase.GetById(Bullatterer.BullattererID) as Gun).gameObject.AddComponent<AdvancedTransformGunSynergyProcessor>();
            KingBullattererForme.NonSynergyGunId = Bullatterer.BullattererID;
            KingBullattererForme.SynergyGunId = KingBullatterer.KingBullattererID;
            KingBullattererForme.SynergyToCheck = "King Bullatterer";
            //WRENCH - NULL REFERENCE EXCEPTION
            AdvancedTransformGunSynergyProcessor NullReferenceExceptionForme = (PickupObjectDatabase.GetById(Wrench.WrenchID) as Gun).gameObject.AddComponent<AdvancedTransformGunSynergyProcessor>();
            NullReferenceExceptionForme.NonSynergyGunId = Wrench.WrenchID;
            NullReferenceExceptionForme.SynergyGunId = WrenchNullRefException.NullWrenchID;
            NullReferenceExceptionForme.SynergyToCheck = "NullReferenceException";
            //GRAVITY GUN - NEGATIVE MATTER
            AdvancedTransformGunSynergyProcessor NegativeMatterSynergyForm = (PickupObjectDatabase.GetById(GravityGun.GravityGunID) as Gun).gameObject.AddComponent<AdvancedTransformGunSynergyProcessor>();
            NegativeMatterSynergyForm.NonSynergyGunId = GravityGun.GravityGunID;
            NegativeMatterSynergyForm.SynergyGunId = GravityGunNegativeMatterForm.GravityGunNegativeMatterID;
            NegativeMatterSynergyForm.SynergyToCheck = "Negative Matter";
            //GATLING GUN - GATTER UP
            AdvancedTransformGunSynergyProcessor GatGunSynergy = (PickupObjectDatabase.GetById(GatlingGun.GatlingGunID) as Gun).gameObject.AddComponent<AdvancedTransformGunSynergyProcessor>();
            GatGunSynergy.NonSynergyGunId = GatlingGun.GatlingGunID;
            GatGunSynergy.SynergyGunId = GatlingGunGatterUp.GatGunID;
            GatGunSynergy.SynergyToCheck = "Gatter Up";
            //GONNE - DISCWORLD
            AdvancedTransformGunSynergyProcessor DiscworldSynergy = (PickupObjectDatabase.GetById(Gonne.GonneID) as Gun).gameObject.AddComponent<AdvancedTransformGunSynergyProcessor>();
            DiscworldSynergy.NonSynergyGunId = Gonne.GonneID;
            DiscworldSynergy.SynergyGunId = GonneElder.ElderGonneId;
            DiscworldSynergy.SynergyToCheck = "Discworld";
            #endregion
            #region ShadowsAndSorcery
            //UTERINE POLYP --- WOMBULAR
            AdvancedTransformGunSynergyProcessor WombularPolypForme = (PickupObjectDatabase.GetById(UterinePolyp.UterinePolypID) as Gun).gameObject.AddComponent<AdvancedTransformGunSynergyProcessor>();
            WombularPolypForme.NonSynergyGunId = UterinePolyp.UterinePolypID;
            WombularPolypForme.SynergyGunId = UterinePolypWombular.WombularPolypID;
            WombularPolypForme.SynergyToCheck = "Wombular";
            //GAXE ---- DIAMOND GAXE
            AdvancedTransformGunSynergyProcessor DiamondGaxeSyn = (PickupObjectDatabase.GetById(Gaxe.GaxeID) as Gun).gameObject.AddComponent<AdvancedTransformGunSynergyProcessor>();
            DiamondGaxeSyn.NonSynergyGunId = Gaxe.GaxeID;
            DiamondGaxeSyn.SynergyGunId = DiamondGaxe.DiamondGaxeID;
            DiamondGaxeSyn.SynergyToCheck = "Diamond Gaxe";
            //REBONDIR ---- Rebondissement
            AdvancedTransformGunSynergyProcessor Rebondissement = (PickupObjectDatabase.GetById(Rebondir.RebondirID) as Gun).gameObject.AddComponent<AdvancedTransformGunSynergyProcessor>();
            Rebondissement.NonSynergyGunId = Rebondir.RebondirID;
            Rebondissement.SynergyGunId = RedRebondir.RedRebondirID;
            Rebondissement.SynergyToCheck = "Rebondissement";
            //DIAMOND CUTTER ------- Ranger Class
            AdvancedTransformGunSynergyProcessor RangerClass = (PickupObjectDatabase.GetById(DiamondCutter.DiamondCutterID) as Gun).gameObject.AddComponent<AdvancedTransformGunSynergyProcessor>();
            RangerClass.NonSynergyGunId = DiamondCutter.DiamondCutterID;
            RangerClass.SynergyGunId = DiamondCutterRangerClass.RedDiamondCutterID;
            RangerClass.SynergyToCheck = "Ranger Class";
            //STICK GUN ---------- Quick, Draw!
            AdvancedTransformGunSynergyProcessor QuickDraw = (PickupObjectDatabase.GetById(StickGun.StickGunID) as Gun).gameObject.AddComponent<AdvancedTransformGunSynergyProcessor>();
            QuickDraw.NonSynergyGunId = StickGun.StickGunID;
            QuickDraw.SynergyGunId = StickGunQuickDraw.FullAutoStickGunID;
            QuickDraw.SynergyToCheck = "Quick, Draw!";
            //LIGHTNING ROD ------ Storm Rod
            AdvancedTransformGunSynergyProcessor StormRodSyn = (PickupObjectDatabase.GetById(LightningRod.LightningRodID) as Gun).gameObject.AddComponent<AdvancedTransformGunSynergyProcessor>();
            StormRodSyn.NonSynergyGunId = LightningRod.LightningRodID;
            StormRodSyn.SynergyGunId = StormRod.StormRodID;
            StormRodSyn.SynergyToCheck = "Storm Rod";
            //RUSTY SHOTGUN -------- Proper Care And Maintenance
            AdvancedTransformGunSynergyProcessor ProperCareNMaintenance = (PickupObjectDatabase.GetById(RustyShotgun.RustyShotgunID) as Gun).gameObject.AddComponent<AdvancedTransformGunSynergyProcessor>();
            ProperCareNMaintenance.NonSynergyGunId = RustyShotgun.RustyShotgunID;
            ProperCareNMaintenance.SynergyGunId = UnrustyShotgun.UnrustyShotgunID;
            ProperCareNMaintenance.SynergyToCheck = "Proper Care & Maintenance";
            #endregion

            AddSynergyForm(ARCPistol.ID, DARCPistol.ID, "DARC Pistol");
            //-------------------------------------------------------------DUAL WIELDING
            #region Dual Wielding
            //STUN GUN & TRANQ GUN
            AdvancedDualWieldSynergyProcessor StunTranqDualSTUN = (PickupObjectDatabase.GetById(StunGun.StunGunID) as Gun).gameObject.AddComponent<AdvancedDualWieldSynergyProcessor>();
            StunTranqDualSTUN.PartnerGunID = 42;
            StunTranqDualSTUN.SynergyNameToCheck = "Non Lethal Solutions";
            AdvancedDualWieldSynergyProcessor StunTranqDualTRANK = (PickupObjectDatabase.GetById(42) as Gun).gameObject.AddComponent<AdvancedDualWieldSynergyProcessor>();
            StunTranqDualTRANK.PartnerGunID = StunGun.StunGunID;
            StunTranqDualTRANK.SynergyNameToCheck = "Non Lethal Solutions";
            //BLOWGUN & POISON DART FROG
            AdvancedDualWieldSynergyProcessor BlowFrogDualBLOW = (PickupObjectDatabase.GetById(Blowgun.BlowgunID) as Gun).gameObject.AddComponent<AdvancedDualWieldSynergyProcessor>();
            BlowFrogDualBLOW.PartnerGunID = PoisonDartFrog.PoisonDartFrogID;
            BlowFrogDualBLOW.SynergyNameToCheck = "Dartistry";
            AdvancedDualWieldSynergyProcessor BlowFrogDualFROG = (PickupObjectDatabase.GetById(PoisonDartFrog.PoisonDartFrogID) as Gun).gameObject.AddComponent<AdvancedDualWieldSynergyProcessor>();
            BlowFrogDualFROG.PartnerGunID = Blowgun.BlowgunID;
            BlowFrogDualFROG.SynergyNameToCheck = "Dartistry";
            //BOOKLLET & LOREBOOK
            AdvancedDualWieldSynergyProcessor BooklletLorebookDualLORE = (PickupObjectDatabase.GetById(Lorebook.LorebookID) as Gun).gameObject.AddComponent<AdvancedDualWieldSynergyProcessor>();
            BooklletLorebookDualLORE.PartnerGunID = Bookllet.BooklletID;
            BooklletLorebookDualLORE.SynergyNameToCheck = "Librarian";
            AdvancedDualWieldSynergyProcessor BooklletLorebookDualBOOK = (PickupObjectDatabase.GetById(Bookllet.BooklletID) as Gun).gameObject.AddComponent<AdvancedDualWieldSynergyProcessor>();
            BooklletLorebookDualBOOK.PartnerGunID = Lorebook.LorebookID;
            BooklletLorebookDualBOOK.SynergyNameToCheck = "Librarian";
            //WELROD & WELGUN
            AdvancedDualWieldSynergyProcessor WelWelDualROD = (PickupObjectDatabase.GetById(Welrod.WelrodID) as Gun).gameObject.AddComponent<AdvancedDualWieldSynergyProcessor>();
            WelWelDualROD.PartnerGunID = Welgun.WelgunID;
            WelWelDualROD.SynergyNameToCheck = "Wel Wel Wel";
            AdvancedDualWieldSynergyProcessor WelWelDualGUN = (PickupObjectDatabase.GetById(Welgun.WelgunID) as Gun).gameObject.AddComponent<AdvancedDualWieldSynergyProcessor>();
            WelWelDualGUN.PartnerGunID = Welrod.WelrodID;
            WelWelDualGUN.SynergyNameToCheck = "Wel Wel Wel";
            //SHOTGUN WEDDING
            AdvancedDualWieldSynergyProcessor WeddingBride = (PickupObjectDatabase.GetById(TheBride.TheBrideID) as Gun).gameObject.AddComponent<AdvancedDualWieldSynergyProcessor>();
            WeddingBride.PartnerGunID = TheGroom.TheGroomID;
            WeddingBride.SynergyNameToCheck = "Shotgun Wedding";
            AdvancedDualWieldSynergyProcessor WeddingGroom = (PickupObjectDatabase.GetById(TheGroom.TheGroomID) as Gun).gameObject.AddComponent<AdvancedDualWieldSynergyProcessor>();
            WeddingGroom.PartnerGunID = TheBride.TheBrideID;
            WeddingGroom.SynergyNameToCheck = "Shotgun Wedding";
            //SUPER BOUNCE BROS
            AdvancedDualWieldSynergyProcessor SBBRico = (PickupObjectDatabase.GetById(Rico.RicoID) as Gun).gameObject.AddComponent<AdvancedDualWieldSynergyProcessor>();
            SBBRico.PartnerGunID = Rebondir.RebondirID;
            SBBRico.SynergyNameToCheck = "Super Bounce Bros";
            AdvancedDualWieldSynergyProcessor SBBRebondir = (PickupObjectDatabase.GetById(Rebondir.RebondirID) as Gun).gameObject.AddComponent<AdvancedDualWieldSynergyProcessor>();
            SBBRebondir.PartnerGunID = Rico.RicoID;
            SBBRebondir.SynergyNameToCheck = "Super Bounce Bros";
            #endregion
        }
        public static void AddSynergyForm(int baseGun, int newGun, string synergy)
        {
            AdvancedTransformGunSynergyProcessor forme = (PickupObjectDatabase.GetById(baseGun) as Gun).gameObject.AddComponent<AdvancedTransformGunSynergyProcessor>();
            forme.NonSynergyGunId = baseGun;
            forme.SynergyGunId = newGun;
            forme.SynergyToCheck = synergy;
        }
    }
}
