using ItemAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace CwaffingTheGungy
{
    class SoulLinkStatusEffectSetup
    {
        public static GameActorSoulLinkEffect StandardSoulLinkEffect;
        public static GameObject SoulLinkOverheadVFX;

        public static void Init()
        {
            SoulLinkOverheadVFX = VFX.animations["PlagueOverhead"];
            GameActorSoulLinkEffect StandSoulLink = GenerateSoulLinkEffect(
                100, 2, true, Color.green, true, Color.green);
            StandardSoulLinkEffect = StandSoulLink;
        }

        public static GameActorSoulLinkEffect GenerateSoulLinkEffect(float duration, float dps, bool tintEnemy, Color bodyTint, bool tintCorpse, Color corpseTint)
        {
            GameActorSoulLinkEffect soulLink = new GameActorSoulLinkEffect
            {
                duration                 = 60,
                effectIdentifier         = "SoulLink",
                resistanceType           = EffectResistanceType.None,
                DamagePerSecondToEnemies = 0,
                ignitesGoops             = false,
                OverheadVFX              = SoulLinkOverheadVFX,
                AffectsEnemies           = true,
                AffectsPlayers           = false,
                AppliesOutlineTint       = true,
                PlaysVFXOnActor          = false,
                AppliesTint              = tintEnemy,
                AppliesDeathTint         = tintCorpse,
                TintColor                = bodyTint,
                DeathTintColor           = corpseTint,
            };
            return soulLink;
        }

        public static List<string> PlagueVFXPaths = new List<string>()
        {
            "CwaffingTheGungy/Resources/StatusEffectVFX/plaguevfxframe_001",
            "CwaffingTheGungy/Resources/StatusEffectVFX/plaguevfxframe_002",
            "CwaffingTheGungy/Resources/StatusEffectVFX/plaguevfxframe_003",
            "CwaffingTheGungy/Resources/StatusEffectVFX/plaguevfxframe_004",
            "CwaffingTheGungy/Resources/StatusEffectVFX/plaguevfxframe_005",
        };
    }
    public class GameActorSoulLinkEffect : GameActorHealthEffect
    {
        public GameActorSoulLinkEffect()
        {
            this.DamagePerSecondToEnemies = 1f;
            this.TintColor                = Color.green;
            this.DeathTintColor           = Color.green;
            this.AppliesTint              = true;
            this.AppliesDeathTint         = true;
        }
        public override void EffectTick(GameActor actor, RuntimeGameActorEffectData effectData)
        {
            // if (EasyGoopDefinitions.PlagueGoop != null)
            // {
            //     DeadlyDeadlyGoopManager goop = DeadlyDeadlyGoopManager.GetGoopManagerForGoopType(EasyGoopDefinitions.PlagueGoop);
            //     goop.TimedAddGoopCircle(actor.specRigidbody.UnitCenter, 1.5f, 0.75f, true);
            // }
            base.EffectTick(actor, effectData);
        }

    }
}

