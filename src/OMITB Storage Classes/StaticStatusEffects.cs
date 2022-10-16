using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NevernamedsItems
{
    public class StaticStatusEffects
    {
        //---------------------------------------BASEGAME STATUS EFFECTS
        //Fires
        public static GameActorFireEffect hotLeadEffect = PickupObjectDatabase.GetById(295).GetComponent<BulletStatusEffectItem>().FireModifierEffect;
        public static GameActorFireEffect greenFireEffect = PickupObjectDatabase.GetById(706).GetComponent<Gun>().DefaultModule.projectiles[0].fireEffect;
        public static GameActorFireEffect SunlightBurn = PickupObjectDatabase.GetById(748).GetComponent<Gun>().DefaultModule.chargeProjectiles[0].Projectile.fireEffect;


        //Freezes
        public static GameActorFreezeEffect frostBulletsEffect = PickupObjectDatabase.GetById(278).GetComponent<BulletStatusEffectItem>().FreezeModifierEffect;
        public static GameActorFreezeEffect chaosBulletsFreeze = PickupObjectDatabase.GetById(569).GetComponent<ChaosBulletsItem>().FreezeModifierEffect;

        //Poisons
        public static GameActorHealthEffect irradiatedLeadEffect = PickupObjectDatabase.GetById(204).GetComponent<BulletStatusEffectItem>().HealthModifierEffect;

        //Charms
        public static GameActorCharmEffect charmingRoundsEffect = PickupObjectDatabase.GetById(527).GetComponent<BulletStatusEffectItem>().CharmModifierEffect;

        //Cheeses
        public static GameActorCheeseEffect elimentalerCheeseEffect = (PickupObjectDatabase.GetById(626) as Gun).DefaultModule.projectiles[0].cheeseEffect;
        public static GameActorCheeseEffect instantCheese = StatusEffectHelper.GenerateCheese(10, 150);

        //Speed Changes
        public static GameActorSpeedEffect tripleCrossbowSlowEffect = (PickupObjectDatabase.GetById(381) as Gun).DefaultModule.projectiles[0].speedEffect;


        //----------------------------------------CUSTOM STATUS EFFECTS
        //Speed Mods
        public static GameActorSpeedEffect FriendlyWebGoopSpeedMod;
        public static GameActorSpeedEffect HoneySpeedMod;
        public static GameActorSpeedEffect FriendlyHoneySpeedMod;
        public static GameActorSpeedEffect PropulsionGoopSpeedMod;

        //Confusion
        public static GameActorConfusionEffect ConfusionEffect;

        //Plague Effects
        public static GameActorPlagueEffect StandardPlagueEffect;
        public static void InitCustomEffects()
        {
            FriendlyWebGoopSpeedMod = new GameActorSpeedEffect
            {
                duration = 1,
                TintColor = tripleCrossbowSlowEffect.TintColor,
                DeathTintColor = tripleCrossbowSlowEffect.DeathTintColor,
                effectIdentifier = "FriendlyWebSlow",
                AppliesTint = false,
                AppliesDeathTint = false,
                resistanceType = EffectResistanceType.None,
                SpeedMultiplier = 0.40f,

                //Eh
                OverheadVFX = null,
                AffectsEnemies = true,
                AffectsPlayers = false,
                AppliesOutlineTint = false,
                OutlineTintColor = tripleCrossbowSlowEffect.OutlineTintColor,
                PlaysVFXOnActor = false,
            };
            HoneySpeedMod = new GameActorSpeedEffect
            {
                duration = 1,
                TintColor = tripleCrossbowSlowEffect.TintColor,
                DeathTintColor = tripleCrossbowSlowEffect.DeathTintColor,
                effectIdentifier = "HoneySlow",
                AppliesTint = false,
                AppliesDeathTint = false,
                resistanceType = EffectResistanceType.None,
                SpeedMultiplier = 0.60f,

                //Eh
                OverheadVFX = tripleCrossbowSlowEffect.OverheadVFX,
                AffectsEnemies = true,
                AffectsPlayers = true,
                AppliesOutlineTint = false,
                OutlineTintColor = tripleCrossbowSlowEffect.OutlineTintColor,
                PlaysVFXOnActor = false,

            };
            FriendlyHoneySpeedMod = new GameActorSpeedEffect
            {
                duration = 1,
                TintColor = tripleCrossbowSlowEffect.TintColor,
                DeathTintColor = tripleCrossbowSlowEffect.DeathTintColor,
                effectIdentifier = "HoneySlow",
                AppliesTint = false,
                AppliesDeathTint = false,
                resistanceType = EffectResistanceType.None,
                SpeedMultiplier = 0.60f,

                //Eh
                OverheadVFX = tripleCrossbowSlowEffect.OverheadVFX,
                AffectsEnemies = true,
                AffectsPlayers = false,
                AppliesOutlineTint = false,
                OutlineTintColor = tripleCrossbowSlowEffect.OutlineTintColor,
                PlaysVFXOnActor = false,

            };
            PropulsionGoopSpeedMod = new GameActorSpeedEffect
            {
                duration = 1,
                TintColor = tripleCrossbowSlowEffect.TintColor,
                DeathTintColor = tripleCrossbowSlowEffect.DeathTintColor,
                effectIdentifier = "PropulsionGoopSpeed",
                AppliesTint = false,
                AppliesDeathTint = false,
                resistanceType = EffectResistanceType.None,
                SpeedMultiplier = 1.40f,

                //Eh
                OverheadVFX = null,
                AffectsEnemies = true,
                AffectsPlayers = true,
                AppliesOutlineTint = false,
                OutlineTintColor = tripleCrossbowSlowEffect.OutlineTintColor,
                PlaysVFXOnActor = false,
            };
        }
    }
}
