using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NevernamedsItems
{
    class StatusEffectHelper
    {
        public static GameActorCheeseEffect GenerateCheese(float length = 10f, float intensity = 50f)
        {
            GameActorCheeseEffect customCheese = new GameActorCheeseEffect
            {
                duration = length,
                TintColor = StaticStatusEffects.elimentalerCheeseEffect.TintColor,
                DeathTintColor = StaticStatusEffects.elimentalerCheeseEffect.DeathTintColor,
                effectIdentifier = "Cheese",
                AppliesTint = true,
                AppliesDeathTint = true,
                resistanceType = EffectResistanceType.None,
                CheeseAmount = intensity,

                //Eh
                OverheadVFX = StaticStatusEffects.elimentalerCheeseEffect.OverheadVFX,
                AffectsPlayers = StaticStatusEffects.elimentalerCheeseEffect.AffectsPlayers,
                AppliesOutlineTint = StaticStatusEffects.elimentalerCheeseEffect.AppliesOutlineTint,
                OutlineTintColor = StaticStatusEffects.elimentalerCheeseEffect.OutlineTintColor,
                PlaysVFXOnActor = StaticStatusEffects.elimentalerCheeseEffect.PlaysVFXOnActor,
                AffectsEnemies = StaticStatusEffects.elimentalerCheeseEffect.AffectsEnemies,
                debrisAngleVariance = StaticStatusEffects.elimentalerCheeseEffect.debrisAngleVariance,
                debrisMaxForce = StaticStatusEffects.elimentalerCheeseEffect.debrisMaxForce,
                debrisMinForce = StaticStatusEffects.elimentalerCheeseEffect.debrisMinForce,
                CheeseCrystals = StaticStatusEffects.elimentalerCheeseEffect.CheeseCrystals,
                CheeseGoop = StaticStatusEffects.elimentalerCheeseEffect.CheeseGoop,
                CheeseGoopRadius = StaticStatusEffects.elimentalerCheeseEffect.CheeseGoopRadius,
                crystalNum = StaticStatusEffects.elimentalerCheeseEffect.crystalNum,
                crystalRot = StaticStatusEffects.elimentalerCheeseEffect.crystalRot,
                crystalVariation = StaticStatusEffects.elimentalerCheeseEffect.crystalVariation,
                maxStackedDuration = StaticStatusEffects.elimentalerCheeseEffect.maxStackedDuration,
                stackMode = StaticStatusEffects.elimentalerCheeseEffect.stackMode,
                vfxExplosion = StaticStatusEffects.elimentalerCheeseEffect.vfxExplosion,
            };
            return customCheese;
        }
        public static GameActorHealthEffect GeneratePoison(float dps  = 3, bool damagesEnemies = true, float duration = 4, bool affectsPlayers = true)
        {
            GameActorHealthEffect customPoison = new GameActorHealthEffect
            {
                duration = duration,
                TintColor = StaticStatusEffects.irradiatedLeadEffect.TintColor,
                DeathTintColor = StaticStatusEffects.irradiatedLeadEffect.DeathTintColor,
                effectIdentifier = "Poison",
                AppliesTint = true,
                AppliesDeathTint = true,
                resistanceType = EffectResistanceType.Poison,
                DamagePerSecondToEnemies = dps,
                ignitesGoops = false,              

                //Eh
                OverheadVFX = StaticStatusEffects.irradiatedLeadEffect.OverheadVFX,
                AffectsEnemies = damagesEnemies,
                AffectsPlayers = StaticStatusEffects.irradiatedLeadEffect.AffectsPlayers,
                AppliesOutlineTint = StaticStatusEffects.irradiatedLeadEffect.AppliesOutlineTint,
                OutlineTintColor = StaticStatusEffects.irradiatedLeadEffect.OutlineTintColor,
                PlaysVFXOnActor = StaticStatusEffects.irradiatedLeadEffect.PlaysVFXOnActor,
            };
            return customPoison;
        }
        public static GameActorFireEffect GenerateFireEffect(float dps = 3, bool damagesEnemies = true, float duration = 4)
        {
            GameActorFireEffect customFire = new GameActorFireEffect
            {
                duration = duration,
                TintColor = StaticStatusEffects.hotLeadEffect.TintColor,
                DeathTintColor = StaticStatusEffects.hotLeadEffect.DeathTintColor,
                effectIdentifier = StaticStatusEffects.hotLeadEffect.effectIdentifier,
                AppliesTint = true,
                AppliesDeathTint = true,
                resistanceType = EffectResistanceType.Fire,
                DamagePerSecondToEnemies = dps,
                ignitesGoops = true,

                //Eh
                OverheadVFX = StaticStatusEffects.hotLeadEffect.OverheadVFX,
                AffectsEnemies = damagesEnemies,
                AffectsPlayers = StaticStatusEffects.hotLeadEffect.AffectsPlayers,
                AppliesOutlineTint = StaticStatusEffects.hotLeadEffect.AppliesOutlineTint,
                OutlineTintColor = StaticStatusEffects.hotLeadEffect.OutlineTintColor,
                PlaysVFXOnActor = StaticStatusEffects.hotLeadEffect.PlaysVFXOnActor,

                FlameVfx = StaticStatusEffects.hotLeadEffect.FlameVfx,
                flameBuffer = StaticStatusEffects.hotLeadEffect.flameBuffer,
                flameFpsVariation = StaticStatusEffects.hotLeadEffect.flameFpsVariation,
                flameMoveChance = StaticStatusEffects.hotLeadEffect.flameMoveChance,
                flameNumPerSquareUnit = StaticStatusEffects.hotLeadEffect.flameNumPerSquareUnit,
                maxStackedDuration = StaticStatusEffects.hotLeadEffect.maxStackedDuration,
                stackMode = StaticStatusEffects.hotLeadEffect.stackMode,
                IsGreenFire = StaticStatusEffects.hotLeadEffect.IsGreenFire,
            };
            return customFire;
        }
        public static GameActorSpeedEffect GenerateLockdown(float duration = 4f)
        {
            GameActorSpeedEffect lockdownToApply = new GameActorSpeedEffect
            {
                duration = duration,
                TintColor = Color.grey,
                DeathTintColor = Color.grey,
                effectIdentifier = "Lockdown",
                AppliesTint = true,
                AppliesDeathTint = true,
                resistanceType = EffectResistanceType.None,
                SpeedMultiplier = 0f,

                //Eh
                OverheadVFX = LockdownStatusEffect.lockdownVFXObject,
                AffectsEnemies = true,
                AffectsPlayers = false,
                AppliesOutlineTint = false,
                OutlineTintColor = Color.grey,
                PlaysVFXOnActor = false,
            };
            return lockdownToApply;
        }
        public static GameActorCharmEffect GenerateCharmEffect(float duration)
        {
            GameActorCharmEffect charmEffect = new GameActorCharmEffect
            {
                duration = duration,
                TintColor = StaticStatusEffects.charmingRoundsEffect.TintColor,
                AppliesDeathTint = StaticStatusEffects.charmingRoundsEffect.AppliesDeathTint,
                AppliesTint = StaticStatusEffects.charmingRoundsEffect.AppliesTint,
                effectIdentifier = StaticStatusEffects.charmingRoundsEffect.effectIdentifier,
                DeathTintColor = StaticStatusEffects.charmingRoundsEffect.DeathTintColor,
                OverheadVFX = StaticStatusEffects.charmingRoundsEffect.OverheadVFX,
                AffectsEnemies = StaticStatusEffects.charmingRoundsEffect.AffectsEnemies,
                AppliesOutlineTint = StaticStatusEffects.charmingRoundsEffect.AppliesOutlineTint,
                AffectsPlayers = StaticStatusEffects.charmingRoundsEffect.AffectsPlayers,
                maxStackedDuration = StaticStatusEffects.charmingRoundsEffect.maxStackedDuration,
                OutlineTintColor = StaticStatusEffects.charmingRoundsEffect.OutlineTintColor,
                PlaysVFXOnActor = StaticStatusEffects.charmingRoundsEffect.PlaysVFXOnActor,
                resistanceType = StaticStatusEffects.charmingRoundsEffect.resistanceType,
                stackMode = StaticStatusEffects.charmingRoundsEffect.stackMode,
            };
            return charmEffect;
        }
        public static GameActorPlagueEffect GeneratePlagueEffect(float duration, float dps, bool tintEnemy, Color bodyTint, bool tintCorpse, Color corpseTint)
        {
            GameActorPlagueEffect commonPlague = new GameActorPlagueEffect
            {
                duration = 10,
                effectIdentifier = "Plague",
                resistanceType = EffectResistanceType.None,
                DamagePerSecondToEnemies = 2f,
                ignitesGoops = false,
                OverheadVFX = PlagueStatusEffectSetup.PlagueOverheadVFX,
                AffectsEnemies = true,
                AffectsPlayers = false,
                AppliesOutlineTint = false,
                PlaysVFXOnActor = false,
                AppliesTint = tintEnemy,
                AppliesDeathTint = tintCorpse,
                TintColor = bodyTint,
                DeathTintColor = corpseTint,
            };
            return commonPlague;
        }
        public static GameActorConfusionEffect GenerateConfusionEfffect(float duration)
        {
            GameActorConfusionEffect confusion = new GameActorConfusionEffect
            {
                duration = duration,
                effectIdentifier = "Confusion",
                resistanceType = EffectResistanceType.None,
                OverheadVFX = null,
                AffectsEnemies = true,
                AffectsPlayers = false,
                AppliesOutlineTint = false,
                PlaysVFXOnActor = false,
            };
            return confusion;
        }
        public static GameActorSizeEffect GenerateSizeEffect(float duration, Vector2 targetScale)
        {
            GameActorSizeEffect commonPlague = new GameActorSizeEffect
            {
                duration = duration,
                newScaleMultiplier = targetScale,
                effectIdentifier = "Shrink",
                resistanceType = EffectResistanceType.None,
                OverheadVFX = null,
                AffectsEnemies = true,
                AffectsPlayers = false,
                AppliesOutlineTint = false,
                PlaysVFXOnActor = false,
                AppliesTint = false,
                AppliesDeathTint = false,
                TintColor = Color.red,
                DeathTintColor = Color.red,
            };
            return commonPlague;
        }
    }
    public class StatusEffectBulletMod : MonoBehaviour
    {
        public StatusEffectBulletMod()
        {
            pickRandom = false;
        }
        private void Start()
        {
            self = base.GetComponent<Projectile>();
            if (pickRandom)
            {
                List<StatusData> validStatuses = new List<StatusData>();
                foreach (StatusData data in datasToApply)
                {
                    if (UnityEngine.Random.value <= data.applyChance)
                    {
                        validStatuses.Add(data);
                    }
                }
                if (validStatuses.Count() > 0)
                {
                    StatusData selectedStatus = BraveUtility.RandomElement(validStatuses);
                    if (selectedStatus.applyTint) self.AdjustPlayerProjectileTint(selectedStatus.effectTint, 1);
                    self.statusEffectsToApply.Add(selectedStatus.effect);
                }
            }
            else
            {
                foreach (StatusData data in datasToApply)
                {
                    if (UnityEngine.Random.value <= data.applyChance)
                    {
                        if (data.applyTint) self.AdjustPlayerProjectileTint(data.effectTint, 1);
                        self.statusEffectsToApply.Add(data.effect);
                    }
                }
            }
        }

        private Projectile self;
        public List<StatusData> datasToApply = new List<StatusData>();
        public bool pickRandom;
        public class StatusData
        {
            public GameActorEffect effect;
            public float applyChance;
            public Color effectTint;
            public bool applyTint = false;
        }
    }
}
