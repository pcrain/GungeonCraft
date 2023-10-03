using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using UnityEngine;
using MonoMod.RuntimeDetour;

using Gungeon;
using Dungeonator;
using Alexandria.ItemAPI;
using Alexandria.Misc;

namespace CwaffingTheGungy
{
    /* TODO:
        - Look into logic in ConsumableStealthItem
    */
    public class CatEarHeadband : PassiveItem
    {
        public static string ItemName         = "Cat Ear Headband";
        public static string SpritePath       = "CwaffingTheGungy/Resources/ItemSprites/cat_ear_headband_icon";
        public static string ShortDescription = "Stealthy Entrances";
        public static string LongDescription  = "Gain 3 seconds of light stealth upon entering combat. Cannot steal or sneak attack while lightly stealthed.\n\nWearing these headbands for extended periods has been shown to cause increasingly severe side effects over time. These typically start with a propensity for curling your hands and uttering the occasional meow, but eventually culminate in forgetting to put on clothes in the morning and spending 6-12 hours a day in front of a webcam periodically checking that your undergarments are still intact.";

        internal const float _STEALTH_TIME = 3f;

        private bool _stealthedEntrance = false;

        public static void Init()
        {
            PickupObject item = Lazy.SetupPassive<CatEarHeadband>(ItemName, SpritePath, ShortDescription, LongDescription);
            item.quality      = PickupObject.ItemQuality.A;
        }

        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.OnEnteredCombat += this.OnEnteredCombat;
        }

        public override DebrisObject Drop(PlayerController player)
        {
            player.OnEnteredCombat -= this.OnEnteredCombat;
            BreakStealth(player);
            return base.Drop(player);
        }

        private void OnEnteredCombat()
        {
            if (this._stealthedEntrance)
                return;
            BecomeInvisible();
        }

        public override void Update()
        {
            base.Update();
            if (!this.Owner.IsStealthed)
                return;
        }

        // copied and simplified from DoEffect() of CardboardBoxItem.cs
        private void BecomeInvisible()
        {
            this.Owner.OnDidUnstealthyAction += BreakStealth;
            this.Owner.SetIsStealthed(true, "CatEarHeadband");
            this._stealthedEntrance = true;

            // Apply a shadowy shader
            foreach (Material m in this.Owner.SetOverrideShader(ShaderCache.Acquire("Brave/Internal/HighPriestAfterImage")))
            {
                m.SetFloat("_EmissivePower", 0f);
                m.SetFloat("_Opacity", 0.5f);
                m.SetColor("_DashColor", Color.gray);
            }

            this.Owner.StartCoroutine(BreakStealth_CR());
        }

        private IEnumerator BreakStealth_CR()
        {
            yield return new WaitForSeconds(_STEALTH_TIME);
            BreakStealth(this.Owner);
        }

        private void BreakStealth(PlayerController pc)
        {
            if (this.Owner != pc || !this._stealthedEntrance)
                return;

            this.Owner.ClearOverrideShader();
            this._stealthedEntrance = false;
            this.Owner.SetIsStealthed(false, "CatEarHeadband");
            this.Owner.OnDidUnstealthyAction -= BreakStealth;
        }
    }
}
