using ItemAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NevernamedsItems
{
    class PlagueStatusEffectSetup
    {
        public static GameObject PlagueOverheadVFX;

        public static void Init()
        {
            PlagueOverheadVFX = VFXToolbox.CreateOverheadVFX(PlagueVFXPaths, "PlagueOverhead", 7);
            GameActorPlagueEffect StandPlague = StatusEffectHelper.GeneratePlagueEffect(100, 2, true, ExtendedColours.plaguePurple, true, ExtendedColours.plaguePurple);
            StaticStatusEffects.StandardPlagueEffect = StandPlague;
        }

        public static List<string> PlagueVFXPaths = new List<string>()
        {
            "NevernamedsItems/Resources/StatusEffectVFX/plaguevfxframe_001",
            "NevernamedsItems/Resources/StatusEffectVFX/plaguevfxframe_002",
            "NevernamedsItems/Resources/StatusEffectVFX/plaguevfxframe_003",
            "NevernamedsItems/Resources/StatusEffectVFX/plaguevfxframe_004",
            "NevernamedsItems/Resources/StatusEffectVFX/plaguevfxframe_005",
        };
    }
    public class GameActorPlagueEffect : GameActorHealthEffect
    {
        public GameActorPlagueEffect()
        {
            this.DamagePerSecondToEnemies = 1f;
            this.TintColor = ExtendedColours.plaguePurple;
            this.DeathTintColor = ExtendedColours.plaguePurple;
            this.AppliesTint = true;
            this.AppliesDeathTint = true;
        }
        public override void EffectTick(GameActor actor, RuntimeGameActorEffectData effectData)
        {
            if (EasyGoopDefinitions.PlagueGoop != null)
            {
                DeadlyDeadlyGoopManager goop = DeadlyDeadlyGoopManager.GetGoopManagerForGoopType(EasyGoopDefinitions.PlagueGoop);
                goop.TimedAddGoopCircle(actor.specRigidbody.UnitCenter, 1.5f, 0.75f, true);
            }
            base.EffectTick(actor, effectData);
        }

    }
}


