using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Alexandria.ItemAPI;
using UnityEngine;
using MonoMod.RuntimeDetour;
using System.Reflection;
using Dungeonator;

namespace NevernamedsItems
{
    public class FoamDarts : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Foam Darts";
            string resourceName = "NevernamedsItems/Resources/foamdarts_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<FoamDarts>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Or Nothing";
            string longDesc = "Knockback slightly up. Bullets soak up goop off the floor, and inflict it upon enemies!" + "\n\nPart of an 'Epix Ammo Expansion Pak' by the manufacturer that made the Dart Gun.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.KnockbackMultiplier, 1.45f, StatModifier.ModifyMethod.MULTIPLICATIVE);
            item.quality = PickupObject.ItemQuality.D;
            item.SetTag("bullet_modifier");
        }

        private void PostProcessProjectile(Projectile sourceProjectile, float effectChanceScalar)
        {
            SoakGoopProjModifier soak = sourceProjectile.gameObject.GetOrAddComponent<SoakGoopProjModifier>();
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject debrisObject = base.Drop(player);
            player.PostProcessProjectile -= this.PostProcessProjectile;
            return debrisObject;
        }
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.PostProcessProjectile += this.PostProcessProjectile;
        }
        public override void OnDestroy()
        {
            if (Owner)
            {
                Owner.PostProcessProjectile -= this.PostProcessProjectile;
            }
            base.OnDestroy();
        }
    }
    class SoakGoopProjModifier : MonoBehaviour
    {
        public SoakGoopProjModifier()
        {

        }
        private void Start()
        {
            AbsorbedGoops = new List<GoopDefinition>();
            self = base.GetComponent<Projectile>();
        }
        private void Update()
        {
            if (GameManager.Instance.Dungeon == null)
            {
                return;
            }
            RoomHandler absoluteRoomFromPosition = self.transform.position.GetAbsoluteRoom();
            List<DeadlyDeadlyGoopManager> roomGoops = absoluteRoomFromPosition.RoomGoops;
            if (roomGoops != null)
            {
                for (int i = 0; i < roomGoops.Count; i++)
                {
                    bool isOverGoop = roomGoops[i].IsPositionInGoop(self.specRigidbody.UnitCenter);
                    if (isOverGoop)
                    {
                        IntVector2 goopedCellVector = (self.specRigidbody.UnitCenter / DeadlyDeadlyGoopManager.GOOP_GRID_SIZE).ToIntVector2(VectorConversions.Floor);

                        DeadlyDeadlyGoopManager currentGoopManager = roomGoops[i];
                        GoopDefinition currentGoopDef = currentGoopManager.goopDefinition;
                        if (currentGoopDef.AppliesCharm && currentGoopDef.CharmModifierEffect != null)
                        {
                            self.statusEffectsToApply.Add(currentGoopDef.CharmModifierEffect);
                            if (currentGoopDef.CharmModifierEffect.AppliesTint) self.AdjustPlayerProjectileTint(currentGoopDef.CharmModifierEffect.TintColor, 2);
                        }
                        if (currentGoopDef.AppliesCheese && currentGoopDef.CheeseModifierEffect != null)
                        {
                            self.statusEffectsToApply.Add(currentGoopDef.CheeseModifierEffect);
                            if (currentGoopDef.CheeseModifierEffect.AppliesTint) self.AdjustPlayerProjectileTint(currentGoopDef.CheeseModifierEffect.TintColor, 2);
                        }
                        if (currentGoopDef.AppliesDamageOverTime && currentGoopDef.HealthModifierEffect != null)
                        {
                            self.statusEffectsToApply.Add(currentGoopDef.HealthModifierEffect);
                            if (currentGoopDef.HealthModifierEffect.AppliesTint) self.AdjustPlayerProjectileTint(currentGoopDef.HealthModifierEffect.TintColor, 2);
                        }
                        if ((currentGoopDef.AppliesSpeedModifier || currentGoopDef.AppliesSpeedModifierContinuously) && currentGoopDef.SpeedModifierEffect != null)
                        {
                            self.statusEffectsToApply.Add(currentGoopDef.SpeedModifierEffect);
                            if (currentGoopDef.SpeedModifierEffect.AppliesTint) self.AdjustPlayerProjectileTint(currentGoopDef.HealthModifierEffect.TintColor, 2);
                        }
                        if (currentGoopDef.damagesEnemies)
                        {
                            self.baseData.damage += currentGoopDef.damagePerSecondtoEnemies;
                            self.AdjustPlayerProjectileTint(currentGoopDef.baseColor32, 2);
                        }

                        if (currentGoopManager.IsPositionOnFire(self.specRigidbody.UnitCenter))
                        {
                            self.statusEffectsToApply.Add(currentGoopDef.fireEffect);
                            if (currentGoopDef.fireEffect.AppliesTint) self.AdjustPlayerProjectileTint(currentGoopDef.fireEffect.TintColor, 2);
                        }
                        if (currentGoopManager.IsPositionFrozen(self.specRigidbody.UnitCenter))
                        {
                            self.statusEffectsToApply.Add(StaticStatusEffects.frostBulletsEffect);
                            self.AdjustPlayerProjectileTint(ExtendedColours.frostBulletsTint, 2);
                        }

                        AbsorbedGoops.Add(currentGoopDef);
                        for (int j = 0; j < StaticReferenceManager.AllGoops.Count; j++)
                        {
                            if (StaticReferenceManager.AllGoops[j])
                            {
                                StaticReferenceManager.AllGoops[j].RemoveGoopCircle(self.specRigidbody.UnitCenter, 0.5f);
                            }
                        }
                    }
                }
            }
        }
        private Projectile self;
        private List<GoopDefinition> AbsorbedGoops;
    }
}