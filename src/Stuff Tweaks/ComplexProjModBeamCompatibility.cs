using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace NevernamedsItems
{
    class ComplexProjModBeamCompatibility
    {
        public static void Init()
        {
            complexProjPostProcessBeam = new Hook(
            typeof(ComplexProjectileModifier).GetMethod("PostProcessBeam", BindingFlags.Instance | BindingFlags.NonPublic),
            typeof(ComplexProjModBeamCompatibility).GetMethod("PostProcessBeamHook", BindingFlags.Instance | BindingFlags.Public),
            typeof(ComplexProjectileModifier)
            );
        }
        public void PostProcessBeamHook(Action<ComplexProjectileModifier, BeamController> orig, ComplexProjectileModifier self, BeamController beam)
        {
            orig(self, beam);
            if (self.AddsExplosino && self.ExplosionData != null)
            {
                BeamExplosiveModifier explodingBeam = beam.gameObject.AddComponent<BeamExplosiveModifier>();
                explodingBeam.explosionData = self.ExplosionData;
                explodingBeam.canHarmOwner = false;
                explodingBeam.chancePerTick = self.ActivationChance;
                explodingBeam.tickDelay = 0.25f;
            }
            if (self.AddsChanceToBlank)
            {
                BeamBlankModifier blankingbeam = beam.gameObject.AddComponent<BeamBlankModifier>();
            }
        }

        private static Hook complexProjPostProcessBeam;
    }
}
