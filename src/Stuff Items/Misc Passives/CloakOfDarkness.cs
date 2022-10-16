using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using ItemAPI;
using Dungeonator;
using System.Collections;
using SaveAPI;

namespace NevernamedsItems
{
    public class CloakOfDarkness : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Cloak of Darkness";
            string resourceName = "NevernamedsItems/Resources/cloakofdarkness_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<CloakOfDarkness>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Shrouded in Mystery";
            string longDesc = "Temporarily fools the Jammed by robing you in the shadows typical of their lord."+"\nAlso occasionally allows one to themselves become a shadow.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.S;
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.Curse, 2f, StatModifier.ModifyMethod.ADDITIVE);
            item.SetupUnlockOnCustomFlag(CustomDungeonFlags.ALLJAMMED_BEATEN_FORGE, true);

        }
        public void AIActorMods(AIActor target)
        {
            //ETGModConsole.Log("ran");

            if (target)
            {
                //ETGModConsole.Log("passed target check");

                if (target.IsBlackPhantom && target.healthHaver && !target.healthHaver.IsBoss)
                {
                    //ETGModConsole.Log("target was jammed");

                    float procChance = 1;
                    if (AllJammedState.AllJammedActive) procChance = 0.5f;
                    if (UnityEngine.Random.value <= procChance)
                    {
                       // ETGModConsole.Log("procced");
                            GameActorCharmEffect charm = StatusEffectHelper.GenerateCharmEffect(13);
                        target.ApplyEffect(charm);
                        if (UnityEngine.Random.value <= 0.5f)
                        {
                            AdvancedKillOnRoomClear advKill = target.gameObject.AddComponent<AdvancedKillOnRoomClear>();
                            advKill.triggersOnRoomUnseal = true;
                        }
                    }
                }
            }
        }
        private float particleCounter;

        public override void Update()
        {
            if (Owner)
            {
            particleCounter += BraveTime.DeltaTime * 40f;
            if (particleCounter > 1f)
            {
                int num = Mathf.FloorToInt(particleCounter);
                particleCounter %= 1f;
                GlobalSparksDoer.DoRandomParticleBurst(num, Owner.sprite.WorldBottomLeft.ToVector3ZisY(0f), Owner.sprite.WorldTopRight.ToVector3ZisY(0f), Vector3.up, 90f, 0.5f, null, null, null, GlobalSparksDoer.SparksType.BLACK_PHANTOM_SMOKE);
            }

            }
            base.Update();
        }
        private void EnteredCombat()
        {
            if (UnityEngine.Random.value <= 0.5f || Owner.PlayerHasActiveSynergy("Cloak and Mirrors"))
            {
                StealthEffect();
            }
        }
        private void StealthEffect()
        {
            PlayerController owner = Owner;
            this.BreakStealth(owner);
            owner.OnItemStolen += this.BreakStealthOnSteal;
            owner.ChangeSpecialShaderFlag(1, 1f);
            owner.healthHaver.OnDamaged += this.OnDamaged;
            owner.SetIsStealthed(true, "cloak of darkness");
            owner.SetCapableOfStealing(true, "cloak of darkness", null);
            GameManager.Instance.StartCoroutine(this.Unstealthy());
        }
        private IEnumerator Unstealthy()
        {
            yield return new WaitForSeconds(0.15f);
            Owner.OnDidUnstealthyAction += this.BreakStealth;
            yield break;
        }
        private void OnDamaged(float resultValue, float maxValue, CoreDamageTypes damageTypes, DamageCategory damageCategory, Vector2 damageDirection)
        {
            this.BreakStealth(Owner);
        }
        private void BreakStealth(PlayerController player)
        {
            player.ChangeSpecialShaderFlag(1, 0f);
            player.OnItemStolen -= this.BreakStealthOnSteal;
            player.SetIsStealthed(false, "cloak of darkness");
            player.healthHaver.OnDamaged -= this.OnDamaged;
            player.SetCapableOfStealing(false, "cloak of darkness", null);
            player.OnDidUnstealthyAction -= this.BreakStealth;
            AkSoundEngine.PostEvent("Play_ENM_wizardred_appear_01", base.gameObject);
        }

        private void BreakStealthOnSteal(PlayerController arg1, ShopItemController arg2)
        {
            this.BreakStealth(arg1);
        }
        public override void Pickup(PlayerController player)
        {
            player.OnEnteredCombat += this.EnteredCombat;
            base.Pickup(player);
            ETGMod.AIActor.OnPostStart += AIActorMods;
        }
        public override DebrisObject Drop(PlayerController player)
        {
            player.OnEnteredCombat -= this.EnteredCombat;

            DebrisObject debrisObject = base.Drop(player);
            ETGMod.AIActor.OnPostStart -= AIActorMods;
            return debrisObject;
        }
        public override void OnDestroy()
        {
            if (Owner)
            {
                Owner.OnEnteredCombat -= this.EnteredCombat;

            }
            ETGMod.AIActor.OnPostStart -= AIActorMods;
            base.OnDestroy();
        }
    }
}