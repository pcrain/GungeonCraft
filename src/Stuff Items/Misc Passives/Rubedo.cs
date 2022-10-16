using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using ItemAPI;
using Dungeonator;

namespace NevernamedsItems
{
    public class Rubedo : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Rubedo";
            string resourceName = "NevernamedsItems/Resources/rubedo_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<Rubedo>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Absolution";
            string longDesc = "Periodically heals from the pool of stored hearts."+"\n\nOnce the Citrinitas stage of the Prime Materia matures into it's reddish hues, it has achieved completion. The Masterwork. THE Magnum Opus. Perfection.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.B;
            item.AddToSubShop(ItemBuilder.ShopType.Goopton);
        }
        private float timer;
        public override void Update()
        {
            if (Owner)
            {
                if (timer > 0)
                {
                    timer -= BraveTime.DeltaTime;
                }
                if (timer <= 0)
                {
                    if (UnityEngine.Random.value <= 0.5f) DoHeal();
                    timer = 5f;
                }
            }
            else { return; }
        }
        public override void Pickup(PlayerController player)
        {
            if (!m_pickedUpThisRun) HeartDispenser.CurrentHalfHeartsStored += 2;
            timer = 5;
            GameManager.Instance.OnNewLevelFullyLoaded += this.NewFloor;
            base.Pickup(player);
        }
        public override DebrisObject Drop(PlayerController player)
        {
            GameManager.Instance.OnNewLevelFullyLoaded -= this.NewFloor;
            return base.Drop(player);
        }
        public override void OnDestroy()
        {
            GameManager.Instance.OnNewLevelFullyLoaded -= this.NewFloor;
            base.OnDestroy();
        }
        private void NewFloor()
        {
            HeartDispenser.CurrentHalfHeartsStored += 2;
            if (Owner && Owner.PlayerHasActiveSynergy("Magnum Opus")) Owner.healthHaver.ApplyHealing(10000000000000f);
        }
        private void DoHeal()
        {
            if (HeartDispenser.CurrentHalfHeartsStored > 0 && Owner.healthHaver.GetCurrentHealthPercentage() < 1)
            {
                AkSoundEngine.PostEvent("Play_OBJ_heart_heal_01", Owner.gameObject);
                Owner.PlayEffectOnActor((PickupObjectDatabase.GetById(73).GetComponent<HealthPickup>().healVFX), Vector3.zero, true, false, false);
                Owner.healthHaver.ApplyHealing(0.5f);
                HeartDispenser.CurrentHalfHeartsStored -= 1;
            }
        }
    }
}

