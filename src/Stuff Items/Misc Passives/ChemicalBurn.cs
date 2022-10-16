using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dungeonator;
using UnityEngine;
using ItemAPI;
using SaveAPI;
using Alexandria.Misc;

namespace NevernamedsItems
{
    public class ChemicalBurn : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Chemical Burn";
            string resourceName = "NevernamedsItems/Resources/chemicalburn_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<ChemicalBurn>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Two-Faced";
            string longDesc = "Poison immunity. Standing on poison grants massively increased firepower." + "\n\nSweet, sweet pain.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.AddToSubShop(ItemBuilder.ShopType.Goopton);
            item.quality = PickupObject.ItemQuality.B;
            item.SetupUnlockOnCustomFlag(CustomDungeonFlags.ALLJAMMED_BEATEN_OUB, true);

        }
        public void onFired(Projectile bullet, float eventchancescaler)
        {
            if (Owner && Owner.CurrentGoop != null)
            {
                if (Owner.CurrentGoop.HealthModifierEffect != null && Owner.CurrentGoop.AppliesDamageOverTime && !(Owner.CurrentGoop.HealthModifierEffect is GameActorPlagueEffect))
                {
                    bullet.baseData.damage *= 2;
                    bullet.RuntimeUpdateScale(1.2f);
                    if (Owner.CurrentGoop.HealthModifierEffect.TintColor != null) bullet.AdjustPlayerProjectileTint(Owner.CurrentGoop.HealthModifierEffect.TintColor, 1);
                    bullet.statusEffectsToApply.Add(Owner.CurrentGoop.HealthModifierEffect);
                }
            }
        }
        public void OnBeamTick(BeamController bem, SpeculativeRigidbody enemy, float what)
        {
            if (Owner && Owner.CurrentGoop != null)
            {
                if (Owner.CurrentGoop.HealthModifierEffect != null && Owner.CurrentGoop.AppliesDamageOverTime && !(Owner.CurrentGoop.HealthModifierEffect is GameActorPlagueEffect))
                {
                    if (bem.GetComponent<Projectile>())
                    {
                        if (enemy.GetComponent<GameActor>())
                        {
                            enemy.GetComponent<GameActor>().ApplyEffect(Owner.CurrentGoop.HealthModifierEffect);
                        }
                    }
                }
            }
        }
        public void BeamCreation(BeamController bem)
        {

        }
        public override void Pickup(PlayerController player)
        {
            player.PostProcessProjectile += this.onFired;
            player.PostProcessBeamTick += this.OnBeamTick;
            player.PostProcessBeam += this.BeamCreation;
            this.poisonReduc = new DamageTypeModifier();
            this.poisonReduc.damageMultiplier = 0f;
            this.poisonReduc.damageType = CoreDamageTypes.Poison;
            player.healthHaver.damageTypeModifiers.Add(this.poisonReduc);
            base.Pickup(player);
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject result = base.Drop(player);
            player.PostProcessBeamTick -= this.OnBeamTick;
            player.PostProcessProjectile -= this.onFired;
            player.PostProcessBeam -= this.BeamCreation;
            player.healthHaver.damageTypeModifiers.Remove(this.poisonReduc);
            return result;
        }
        public override void OnDestroy()
        {
            if (Owner)
            {
                Owner.PostProcessBeamTick -= this.OnBeamTick;
                Owner.PostProcessProjectile -= this.onFired;
                Owner.PostProcessBeam -= this.BeamCreation;
            }
            base.OnDestroy();
        }
        private DamageTypeModifier poisonReduc;
    }
    class ChemicalBurnBeamModifier : MonoBehaviour
    {
        public ChemicalBurnBeamModifier()
        {

        }
        public void Start()
        {
            m_projectile = base.GetComponent<Projectile>();
            if (m_projectile && m_projectile.ProjectilePlayerOwner()) owner = m_projectile.ProjectilePlayerOwner();
        }
        public void Update()
        {
            if (owner.CurrentGoop != lastcheckedgoop)
            {
                if (owner.CurrentGoop.HealthModifierEffect != null && owner.CurrentGoop.AppliesDamageOverTime && !(owner.CurrentGoop.HealthModifierEffect is GameActorPlagueEffect))
                {
                    if (!currentlyActive)
                    {
                        m_projectile.baseData.damage *= 2;
                        currentlyActive = true;
                    }
                }
                else
                {
                    if (currentlyActive)
                    {
                        m_projectile.baseData.damage /= 2;
                        currentlyActive = false;
                    }
                }
                lastcheckedgoop = owner.CurrentGoop;
            }
        }
        private bool currentlyActive = false;
        private GoopDefinition lastcheckedgoop;
        private Projectile m_projectile;
        private PlayerController owner;
    }
}
