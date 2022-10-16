using UnityEngine;
using ItemAPI;
using Dungeonator;
using System.Collections;
using System;
using MonoMod.RuntimeDetour;
using System.Reflection;
using Gungeon;
using System.Linq;
using System.Collections.Generic;


namespace NevernamedsItems
{
    public class FuriousAmmolet : BlankModificationItem
    {

        //Call this method from the Start() method of your ETGModule extension
        public static void Init()
        {
            //The name of the item
            string itemName = "Furious Ammolet";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it
            string resourceName = "NevernamedsItems/Resources/furiousammolet_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a PassiveItem component to the object
            var item = obj.AddComponent<FuriousAmmolet>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Blanks Enrage";
            string longDesc = "Using a blank sends the bearer of this Ammolet into a bloody rage."+"\n\nMade of a disgusting alloy of blood and iron, this ammolet is warm to the touch.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //Do this after ItemBuilder.AddSpriteToObject!
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Set the rarity of the item
            item.quality = PickupObject.ItemQuality.B;
            item.AddToSubShop(ItemBuilder.ShopType.OldRed);


            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.AdditionalBlanksPerFloor, 1f, StatModifier.ModifyMethod.ADDITIVE);

            //ID of the item if you need it to be used in other methods
            FuriousAmmoletID = item.PickupObjectId;

            Hook BlankHook = new Hook(
                typeof(SilencerInstance).GetMethod("ProcessBlankModificationItemAdditionalEffects", BindingFlags.Instance | BindingFlags.NonPublic),
                typeof(FuriousAmmolet).GetMethod("BlankModHook", BindingFlags.Static | BindingFlags.Public)
            );
        }
        private static int FuriousAmmoletID;

        public override void Pickup(PlayerController player)
        {
            instance = this;
            base.Pickup(player);
        }

        public static FuriousAmmolet instance;
        public static void BlankModHook(Action<SilencerInstance, BlankModificationItem, Vector2, PlayerController> orig, SilencerInstance silencer, BlankModificationItem bmi, Vector2 centerPoint, PlayerController user)
        {
            orig(silencer, bmi, centerPoint, user);

            if (user.HasPickupID(FuriousAmmoletID) && instance != null)
            {
                try
                {
                    user.StartCoroutine(instance.InflictRage(user));
                }
                catch (Exception e)
                {
                    ETGModConsole.Log(e.Message);
                }
            }
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject debrisObject = base.Drop(player);
            return debrisObject;
        }
        public override void OnDestroy()
        {
            base.OnDestroy();
        }

        public GameObject RageOverheadVFX;
        public bool rageActive = false;
        public bool stopRageCoroutineActive = false;
        private IEnumerator InflictRage(PlayerController player)
        {
            if (rageActive == true)
            {
                StopCoroutine(removeRageCoroutine);
                RemoveStat(PlayerStats.StatType.Damage);
                player.stats.RecalculateStats(player, true, false);
                rageActive = false;
            }
            player.stats.RecalculateStats(player, true, false);
            RagePassiveItem rageitem = PickupObjectDatabase.GetById(353).GetComponent<RagePassiveItem>();
            RageOverheadVFX = rageitem.OverheadVFX.gameObject;
            this.instanceVFX = Owner.PlayEffectOnActor(this.RageOverheadVFX, new Vector3(0f, 1.375f, 0f), true, true, false);
            AddStat(PlayerStats.StatType.Damage, 2, StatModifier.ModifyMethod.MULTIPLICATIVE);
            player.stats.RecalculateStats(player, true, false);
            rageActive = true;
            float elapsed = 0f;
            float particleCounter = 0f;
            float Duration = 7f;
            while (elapsed < Duration)
            {
                elapsed += BraveTime.DeltaTime;
                Owner.baseFlatColorOverride = this.flatColorOverride.WithAlpha(Mathf.Lerp(this.flatColorOverride.a, 0f, Mathf.Clamp01(elapsed - (Duration - 1f))));
                if (GameManager.Options.ShaderQuality != GameOptions.GenericHighMedLowOption.LOW && GameManager.Options.ShaderQuality != GameOptions.GenericHighMedLowOption.VERY_LOW && Owner && Owner.IsVisible && !Owner.IsFalling)
                {
                    particleCounter += BraveTime.DeltaTime * 40f;
                    if (this.instanceVFX && elapsed > 1f)
                    {
                        this.instanceVFX.GetComponent<tk2dSpriteAnimator>().PlayAndDestroyObject("rage_face_vfx_out", null);
                        this.instanceVFX = null;
                    }
                    if (particleCounter > 1f)
                    {
                        int num = Mathf.FloorToInt(particleCounter);
                        particleCounter %= 1f;
                        GlobalSparksDoer.DoRandomParticleBurst(num, Owner.sprite.WorldBottomLeft.ToVector3ZisY(0f), Owner.sprite.WorldTopRight.ToVector3ZisY(0f), Vector3.up, 90f, 0.5f, null, null, null, GlobalSparksDoer.SparksType.BLACK_PHANTOM_SMOKE);
                    }
                }
                yield return null;
            }
            removeRageCoroutine = GameManager.Instance.StartCoroutine(this.RemoveRage(player));
        }
        Coroutine removeRageCoroutine;
        private IEnumerator RemoveRage(PlayerController player)
        {
            stopRageCoroutineActive = true;
            //yield return new WaitForSeconds(7f);
            if (this.instanceVFX)
            {
                this.instanceVFX.GetComponent<tk2dSpriteAnimator>().PlayAndDestroyObject("rage_face_vfx_out", null);
            }
            RemoveStat(PlayerStats.StatType.Damage);
            player.stats.RecalculateStats(player, true, false);
            rageActive = false;
            stopRageCoroutineActive = false;
            yield break;
        }
        private void AddStat(PlayerStats.StatType statType, float amount, StatModifier.ModifyMethod method = StatModifier.ModifyMethod.ADDITIVE)
        {
            StatModifier modifier = new StatModifier
            {
                amount = amount,
                statToBoost = statType,
                modifyType = method
            };

            if (this.passiveStatModifiers == null)
                this.passiveStatModifiers = new StatModifier[] { modifier };
            else
                this.passiveStatModifiers = this.passiveStatModifiers.Concat(new StatModifier[] { modifier }).ToArray();
        }

        private void RemoveStat(PlayerStats.StatType statType)
        {
            var newModifiers = new List<StatModifier>();
            for (int i = 0; i < passiveStatModifiers.Length; i++)
            {
                if (passiveStatModifiers[i].statToBoost != statType)
                    newModifiers.Add(passiveStatModifiers[i]);
            }
            this.passiveStatModifiers = newModifiers.ToArray();
        }
        // Token: 0x04007941 RID: 31041

        // Token: 0x04007943 RID: 31043
        public Color flatColorOverride = new Color(0.5f, 0f, 0f, 0.75f);

        // Token: 0x04007947 RID: 31047
        private GameObject instanceVFX;
    }
}

