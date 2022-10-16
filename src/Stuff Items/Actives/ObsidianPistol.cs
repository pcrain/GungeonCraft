using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dungeonator;
using UnityEngine;
using Alexandria.ItemAPI;
using System.Collections;

namespace NevernamedsItems
{
    class ObsidianPistol : PlayerItem
    {
        public static void Init()
        {
            string itemName = "Obsidian Pistol";
            string resourceName = "NevernamedsItems/Resources/obsidianpistol_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<ObsidianPistol>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Sacrifice I’m Willing to Take";
            string longDesc = "Sacrifices those you hold dear in a bloody ritual of reverence to Kaliber, the Gun Mother." + "\n\nThose who worship shall be rewarded for their faith." + "\n\nPraise Be";

            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.Timed, 0.5f);
            item.consumable = false;
            item.quality = ItemQuality.C;
            item.AddToSubShop(ItemBuilder.ShopType.Cursula);
        }
        public override void DoEffect(PlayerController user)
        {
            //ETGModConsole.Log("Item was used");

            ClearItemLists();
            GetCompanionItemsOnUser(user);
            GetCompanionItemsOnGround();
            int totalItemsFound = CompanionItems.Count + DebrisCompanionItems.Count + DebrisGuns.Count;
            //ETGModConsole.Log("Total items found: " + totalItemsFound);
            if (totalItemsFound > 0)
            {
                int randomValue = UnityEngine.Random.Range(1, (totalItemsFound + 1));

                if (randomValue > (CompanionItems.Count + DebrisCompanionItems.Count))
                {
                    //GunDebris
                    if (DebrisGuns.Count > 0)
                    {
                        KillGroundCompanion(user, BraveUtility.RandomElement(DebrisGuns).gameObject);
                    }
                    else { ETGModConsole.Log("DebrisGuns had nothing in it? This should never happen."); }
                }
                else
                {
                    if (randomValue > CompanionItems.Count)
                    {
                        //Debris
                        if (DebrisCompanionItems.Count > 0)
                        {
                            KillGroundCompanion(user, BraveUtility.RandomElement(DebrisCompanionItems).gameObject);
                        }
                        else { ETGModConsole.Log("DebrisCompanionItems had nothing in it? This should never happen."); }
                    }
                    else
                    {
                        //Companion
                        if (CompanionItems.Count > 0)
                        {
                            GameManager.Instance.StartCoroutine(this.KillInventoryCompanion(user));
                        }
                        else { ETGModConsole.Log("CompanionItems had nothing in it? This should never happen."); }
                    }
                }
            }
        }
        private void KillGroundCompanion(PlayerController user, GameObject companion)
        {
            //Result is a Debris Object
            if (companion != null)
            {
                PickupObject itemness = companion.gameObject.GetComponent<PickupObject>();

                if (itemness != null)
                {
                    AkSoundEngine.PostEvent("Play_WPN_smileyrevolver_shot_01", gameObject);
                    Instantiate<GameObject>(EasyVFXDatabase.TeleporterPrototypeTelefragVFX, itemness.sprite.WorldCenter, Quaternion.identity);
                    GameManager.Instance.StartCoroutine(DoReward(user, itemness.sprite.WorldCenter, itemness.PickupObjectId));
                    Destroy(companion.gameObject);
                }
            }
            else { ETGModConsole.Log("Companion was Null in the Debris Object handler, this should never happen."); }
        }
        private IEnumerator KillInventoryCompanion(PlayerController user)
        {
            //ETGModConsole.Log("KillInventoryCompanion Triggered");

            PickupObject item = BraveUtility.RandomElement(CompanionItems);
            Gun gunness = item.gameObject.GetComponent<Gun>();
            DebrisObject debrisObject = SpecialDrop.DropItem(user, item, gunness != null);


            yield return new WaitForSeconds(1f);
            if (debrisObject != null)
            {
                AkSoundEngine.PostEvent("Play_WPN_smileyrevolver_shot_01", gameObject);
                Instantiate<GameObject>(EasyVFXDatabase.TeleporterPrototypeTelefragVFX, debrisObject.sprite.WorldCenter, Quaternion.identity);
                GameManager.Instance.StartCoroutine(DoReward(user, debrisObject.sprite.WorldCenter, item.PickupObjectId));
                UnityEngine.Object.Destroy(debrisObject.gameObject);
            }
            else { ETGModConsole.Log("DebrisObject was null in the kill code, this should never happen."); }

            yield break;
        }
        private ItemQuality AOrSWhatWillItBe()
        {
            if (UnityEngine.Random.value < 0.35f)
            {
                return ItemQuality.S;
            }
            else
            {
                return ItemQuality.A;
            }
        }
        private IEnumerator DoReward(PlayerController user, Vector2 positionToSpawn, int pickupObject)
        {
            float curseAmount = 1;
            if (pickupObject == 442)
            {
                curseAmount = 3;
                StatModifier damageup = new StatModifier();
                damageup.statToBoost = PlayerStats.StatType.Damage;
                damageup.amount = 1.2f;
                damageup.modifyType = StatModifier.ModifyMethod.MULTIPLICATIVE;
                user.ownerlessStatModifiers.Add(damageup);
                user.stats.RecalculateStats(user, false, false);
            }
            //ETGModConsole.Log("DoReward Triggered");
            yield return new WaitForSeconds(1f);
            // ETGModConsole.Log("Passed The Wait");

            if (UnityEngine.Random.value <= 0.5f)
            {
                PassiveItem itemOfTypeAndQuality = LootEngine.GetItemOfTypeAndQuality<PassiveItem>(AOrSWhatWillItBe(), GameManager.Instance.RewardManager.ItemsLootTable, false);
                LootEngine.SpawnItem(itemOfTypeAndQuality.gameObject, positionToSpawn, Vector2.left, 0f, false, true, true);
            }
            else
            {
                Gun itemOfTypeAndQuality = LootEngine.GetItemOfTypeAndQuality<Gun>(AOrSWhatWillItBe(), GameManager.Instance.RewardManager.GunsLootTable, false);
                LootEngine.SpawnItem(itemOfTypeAndQuality.gameObject, positionToSpawn, Vector2.left, 0f, false, true, true);
            }

            StatModifier statModifier4 = new StatModifier();
            statModifier4.statToBoost = PlayerStats.StatType.Curse;
            statModifier4.amount = curseAmount;
            statModifier4.modifyType = StatModifier.ModifyMethod.ADDITIVE;
            user.ownerlessStatModifiers.Add(statModifier4);
            user.stats.RecalculateStats(user, false, false);
            yield break;
        }

        private void ClearItemLists()
        {
            CompanionItems.Clear();
            DebrisCompanionItems.Clear();
            DebrisGuns.Clear();
        }

        private void GetCompanionItemsOnUser(PlayerController user)
        {
            foreach (PassiveItem item in user.passiveItems)
            {

                if (ItemIsValid(item))
                {
                    CompanionItems.Add(item);
                }
            }
            foreach (PlayerItem active in user.activeItems)
            {
                if (ItemIsValid(active))
                {
                    CompanionItems.Add(active);
                }
            }
            foreach (Gun gun in user.inventory.AllGuns)
            {
                if (ItemIsValid(gun))
                {
                    CompanionItems.Add(gun);
                }
            }
        }
        private bool ItemIsValid(PickupObject item)
        {
            CompanionItem companion = item.GetComponent<CompanionItem>();
            MulticompanionItem multiCompanion = item.GetComponent<MulticompanionItem>();
            BankMaskItem clownCompanion = item.GetComponent<BankMaskItem>();
            PlayerOrbitalItem orbitalItem = item.GetComponent<PlayerOrbitalItem>();
            if ((companion != null) || (multiCompanion != null) || (clownCompanion != null) || (orbitalItem != null && !item.HasTag("guon_stone")) || item.HasTag("non_companion_living_item"))
            {
                if (!BannedItems.Contains(item.PickupObjectId))
                {
                    if (item.DisplayName != "Magic Lamp") { return true; }
                    else { return false; }
                }
                else { return false; }
            }
            else
            {
                //ETGModConsole.Log("Companion Component: " + (companion != null) + "\nMultiCompanionItem Component: " + (multiCompanion != null) + "\nOrbitalItem Component: " + (orbitalItem != null) + "\nIsGuonStone: " + (GuonStone != null) + " (Should be false)" + "\nOverrideItem: " + (OverridePossibleItems.Contains(item.PickupObjectId)));
                return false;
            }
        }
        private void GetCompanionItemsOnGround()
        {
            DebrisObject[] shitOnGround = FindObjectsOfType<DebrisObject>();
            foreach (DebrisObject debris in shitOnGround)
            {
                PickupObject itemness = debris.gameObject.GetComponent<PickupObject>();
                Gun gunness = debris.gameObject.GetComponent<Gun>();
                if (itemness != null || gunness != null)
                {
                    //ETGModConsole.Log("Detected: " + itemness.name);
                    if (ItemIsValid(itemness))
                    {
                        DebrisCompanionItems.Add(debris);
                    }
                }
            }
            Gun[] gunsonground = FindObjectsOfType<Gun>();
            foreach (Gun debris in gunsonground)
            {
                PickupObject itemness = debris.gameObject.GetComponent<PickupObject>();
                if ((itemness != null) && debris.CurrentOwner == null && debris.gameObject.transform.position != Vector3.zero)
                {
                    //ETGModConsole.Log("Detected: " + itemness.name);
                    if (ItemIsValid(itemness))
                    {
                        DebrisGuns.Add(debris);
                    }
                }
            }
        }
        public static List<PickupObject> CompanionItems = new List<PickupObject>() { };
        public static List<DebrisObject> DebrisCompanionItems = new List<DebrisObject>() { };
        public static List<Gun> DebrisGuns = new List<Gun>() { };

        public static List<int> BannedItems = new List<int>()
        {
            263, //Orange Guon Stone
            262, // White Guon Stone
        };
        public override bool CanBeUsed(PlayerController user)
        {
            return true;
        }
    }
}
