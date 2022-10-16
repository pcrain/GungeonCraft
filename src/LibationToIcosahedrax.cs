using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dungeonator;
using UnityEngine;
using ItemAPI;

namespace NevernamedsItems
{
    class LibationtoIcosahedrax : PlayerItem
    {
        //Call this method from the Start() method of your ETGModule extension class
        public static void Init()
        {
            //The name of the item
            string itemName = "<WIP> Libation to Icosahedrax <WIP>";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it.
            string resourceName = "NevernamedsItems/Resources/libation_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a ActiveItem component to the object
            var item = obj.AddComponent<LibationtoIcosahedrax>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Standing Oblation";
            string longDesc = "This ancient chalice is inscribed with 1, 100, and all the numbers in between. The runes inside the cup constantly ooze 'challenge juice', keeping this offering forever-full.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //"kts" here is the item pool. In the console you'd type kts:sweating_bullets
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Set the cooldown type and duration of the cooldown
            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.Timed, 5);

            //Adds a passive modifier, like curse, coolness, damage, etc. to the item. Works for passives and actives.


            //Set some other fields
            item.consumable = false;
            item.quality = ItemQuality.EXCLUDED;
            //DefineEffects();
        }
        private void MakeKyleDisappointedInMe(int effect)
        {
            string notifText = null;
            switch (effect)
            {
                case 1: //Wrath of Icosahedrax
                    notifText = "1. Wrath of Icosahedrax!";
                    break;
                case 2: //Damage Up
                    notifText = "2. Damage Up";
                    StatModifierStuff(PlayerStats.StatType.Damage, 1.1f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                    break;
                case 3: //Damage Down
                    notifText = "3. Damage Down";
                    StatModifierStuff(PlayerStats.StatType.Damage, 0.92f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                    break;
                case 4: //Speed Up
                    notifText = "4. Speed Up";
                    StatModifierStuff(PlayerStats.StatType.MovementSpeed, 1.1f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                    break;
                case 5: //Speed Down
                    notifText = "5. Speed Down";
                    StatModifierStuff(PlayerStats.StatType.MovementSpeed, 0.95f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                    break;
                case 6: //Reload Speed Up
                    notifText = "6. Reload Speed Up";
                    StatModifierStuff(PlayerStats.StatType.ReloadSpeed, 0.9f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                    break;
                case 7: //Reload Speed Down
                    notifText = "7. Reload Speed Down";
                    StatModifierStuff(PlayerStats.StatType.ReloadSpeed, 1.08f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                    break;
                case 8: //Accuracy Up
                    notifText = "8. Accuracy Up";
                    StatModifierStuff(PlayerStats.StatType.Accuracy, 0.9f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                    break;
                case 9: //Accuracy Down
                    notifText = "9. Accuracy Down";
                    StatModifierStuff(PlayerStats.StatType.Accuracy, 1.08f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                    break;
                case 10: //Active Items Up
                    notifText = "10. Active Item Storage Up";
                    StatModifierStuff(PlayerStats.StatType.AdditionalItemCapacity, 1f, StatModifier.ModifyMethod.ADDITIVE);
                    break;
                case 11: //Active Items Down
                    notifText = "11. Active Item Storage Down";
                    StatModifierStuff(PlayerStats.StatType.AdditionalItemCapacity, -1f, StatModifier.ModifyMethod.ADDITIVE);
                    break;
                case 12: //Clip Size Up
                    notifText = "12. Clip Size Up";
                    StatModifierStuff(PlayerStats.StatType.AdditionalClipCapacityMultiplier, 1.1f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                    break;
                case 13: //Clip Size Down
                    notifText = "13. Clip Size Down";
                    StatModifierStuff(PlayerStats.StatType.AdditionalClipCapacityMultiplier, 0.92f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                    break;
                case 14: //Ammo capacity Up
                    notifText = "14. Ammo Capacity Up";
                    StatModifierStuff(PlayerStats.StatType.AmmoCapacityMultiplier, 1.1f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                    break;
                case 15: //Ammo Capacity Down
                    notifText = "15. Ammo Capacity Down";
                    StatModifierStuff(PlayerStats.StatType.AmmoCapacityMultiplier, 0.92f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                    break;
                case 16: //Charge Speed Up
                    notifText = "16. Charge Speed Up";
                    StatModifierStuff(PlayerStats.StatType.ChargeAmountMultiplier, 1.1f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                    break;
                case 17: //Charge Speed Down
                    notifText = "17. Charge Speed Down";
                    StatModifierStuff(PlayerStats.StatType.ChargeAmountMultiplier, 0.92f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                    break;
                case 18: //Coolness Up
                    notifText = "18. Cool-dude-ify!";
                    StatModifierStuff(PlayerStats.StatType.Coolness, 1f, StatModifier.ModifyMethod.ADDITIVE);
                    break;
                case 19: //Coolness Down
                    notifText = "19. Uncool...";
                    StatModifierStuff(PlayerStats.StatType.Coolness, -1f, StatModifier.ModifyMethod.ADDITIVE);
                    break;
                case 20: //Curse Up
                    notifText = "20. Cursed!";
                    StatModifierStuff(PlayerStats.StatType.Curse, 1f, StatModifier.ModifyMethod.ADDITIVE);
                    break;
                case 21: //Curse Down
                    notifText = "21. Cleansed";
                    StatModifierStuff(PlayerStats.StatType.Curse, -1f, StatModifier.ModifyMethod.ADDITIVE);
                    break;
                case 22: //Boss Damage Up
                    notifText = "22. Damage To Bosses Up";
                    StatModifierStuff(PlayerStats.StatType.DamageToBosses, 1.1f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                    break;
                case 23: //Boss Damage Down
                    notifText = "23. Damage To Bosses Down";
                    StatModifierStuff(PlayerStats.StatType.DamageToBosses, 0.92f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                    break;
                case 24: //Dodge Roll Damage Up
                    notifText = "24. Dodge Roll Damage Up";
                    StatModifierStuff(PlayerStats.StatType.DodgeRollDamage, 1.1f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                    break;
                case 25: //Dodge Roll Damage Down
                    notifText = "25. Dodge Roll Damage Down";
                    StatModifierStuff(PlayerStats.StatType.DodgeRollDamage, 0.92f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                    break;
                case 26: //Dodge Roll Speed Up
                    notifText = "26. Dodge Roll Speed Up";
                    StatModifierStuff(PlayerStats.StatType.DodgeRollSpeedMultiplier, 1.1f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                    break;
                case 27: //Dodge Roll Speed Down
                    notifText = "27. Dodge Roll Speed Down";
                    StatModifierStuff(PlayerStats.StatType.DodgeRollSpeedMultiplier, 0.92f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                    break;
                case 28: //Enemy Bullet Speed Up
                    notifText = "28. Enemy Bullets Hastened";
                    StatModifierStuff(PlayerStats.StatType.EnemyProjectileSpeedMultiplier, 1.08f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                    break;
                case 29: //Enemy Bullet Speed Down
                    notifText = "29. Enemy Bullets Slowed";
                    StatModifierStuff(PlayerStats.StatType.EnemyProjectileSpeedMultiplier, 0.9f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                    break;
                case 30: //Prices Lowered
                    notifText = "30. Prices Lowered";
                    StatModifierStuff(PlayerStats.StatType.GlobalPriceMultiplier, 0.9f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                    break;
                case 31: //Prices Raised
                    notifText = "31. Prices Raised";
                    StatModifierStuff(PlayerStats.StatType.GlobalPriceMultiplier, 1.08f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                    break;
                case 32: //Knockback Up
                    notifText = "32. Knockback Up";
                    StatModifierStuff(PlayerStats.StatType.KnockbackMultiplier, 1.1f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                    break;
                case 33: //Knockback Down
                    notifText = "33. Knockback Down";
                    StatModifierStuff(PlayerStats.StatType.KnockbackMultiplier, 0.92f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                    break;
                case 34: //Bullet Speed Up
                    notifText = "34. Bullet Speed Up";
                    StatModifierStuff(PlayerStats.StatType.ProjectileSpeed, 1.1f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                    break;
                case 35: //Bullet Speed Down
                    notifText = "35. Bullet Speed Down";
                    StatModifierStuff(PlayerStats.StatType.ProjectileSpeed, 0.92f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                    break;
                case 36: //Bullet Size Up
                    notifText = "36. Bullet Size Up";
                    StatModifierStuff(PlayerStats.StatType.PlayerBulletScale, 1.1f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                    break;
                case 37: //Bullet Size Down
                    notifText = "37. Bullet Size Down";
                    StatModifierStuff(PlayerStats.StatType.PlayerBulletScale, 0.92f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                    break;
                case 38: //Range Up
                    notifText = "38. Range Up";
                    StatModifierStuff(PlayerStats.StatType.RangeMultiplier, 1.1f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                    break;
                case 39: //Range Down
                    notifText = "39. Range Down";
                    StatModifierStuff(PlayerStats.StatType.RangeMultiplier, 0.92f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                    break;
                case 40: //Firerate Up
                    notifText = "40. Rate of Fire Up";
                    StatModifierStuff(PlayerStats.StatType.RateOfFire, 1.1f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                    break;
                case 41: //Firerate Down
                    notifText = "41. Rate of Fire Down";
                    StatModifierStuff(PlayerStats.StatType.RateOfFire, 0.92f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                    break;
                case 42: //Throwing Damage Up
                    notifText = "42. Throwing Damage Up";
                    StatModifierStuff(PlayerStats.StatType.ThrownGunDamage, 1.1f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                    break;
                case 43: //Health up
                    notifText = "43. Health Up";
                    StatModifierStuff(PlayerStats.StatType.Health, 1f, StatModifier.ModifyMethod.ADDITIVE);
                    break;
                case 44: //Health Down
                    notifText = "44. Health Down";
                    StatModifierStuff(PlayerStats.StatType.Health, -1f, StatModifier.ModifyMethod.ADDITIVE);
                    break;
                case 45: // Give Junk
                    notifText = null;
                    ItemGiver(127, UnityEngine.Random.Range(1, 5), "45. Junked");
                    break;
                case 46: //Give Glass Guons
                    notifText = null;
                    ItemGiver(565, UnityEngine.Random.Range(1, 4), "46. Glassed");
                    break;
                case 47: //Spookem
                    notifText = "47. " + BraveUtility.RandomElement(scaryMessages);
                    break;
                case 48:
                    notifText = "58. Rewarded!";
                    SpawnChest();
                    break;
                case 49:
                    notifText = "59. Paid";
                    GiveConsumable(68, UnityEngine.Random.Range(10, 45));
                    break;


            }
            if (notifText != null)
            {
                Notify(notifText, "");
            }
        }
        private void GiveConsumable(int id, int amount)
        {
            if (amount <= 0)
            {
                //Remove Consumables
            }
            else
            {
                LootEngine.GivePrefabToPlayer(PickupObjectDatabase.GetById(id).gameObject, LastOwner);
            }
        }
        private void SpawnChest()
        {
            IntVector2 bestRewardLocation = LastOwner.CurrentRoom.GetBestRewardLocation(IntVector2.One * 3, RoomHandler.RewardLocationStyle.PlayerCenter, true);
            Chest spawnedChest = GameManager.Instance.RewardManager.SpawnRewardChestAt(bestRewardLocation);
            spawnedChest.RegisterChestOnMinimap(spawnedChest.GetAbsoluteParentRoom());
        }
        public static List<string> scaryMessages = new List<string>()
        {
            "Tarry not, they come for you",
            "Darkness Shrouds You",
            "You are Doomed",
            "Living a Lie",
            "He comes",
            "The Dentist is but a servant",
            "Secret Message",
            "Doom",
            "31/07/1715",
            "Broken",
            "Caused Irreparable Damage",
            "You will never recover",
            "Kaliber's Wrath",
            "Summoned Horror",
            "Kaliber k'pow uboom k'bhang",
            "Hope Lost",
            "Courage Down",
            "Nobody loves you",
            "Everything is dead",
            "Trail of Tears",
            "Is this the real life?",
            "Is this just fantasy?",
            "The world is a dark place",
            "You will never succeed",
            "The Past is Forgotten",
            "Who am I?",
            "Am I Alone?",
            "Feast Upon Yourself",
            "The Mistake Holds Secrets",
            "Prince of Errors",
            "I AM ERROR",
            "My mind, slipping",
            "Shall we dance, mortal?",
            "Arousal",
            "Clatter of the Bones",
            "The Jungle is Dark",
            "Kylius Physicus",
            "Broken Legacy",
            "Did you really expect to win?",
            "All Jammed Mode",
            "Next Hit Kills",
            "CURSED!!!",
            "I'm Blue",
            "Perish",
            "I love you",
            "What's going on?...",
            "I'm scared...",
            "I feel so alone",
            "I feel so cold",
            "Lost and Forgotten",
            "Wrath of Icosahidrax",
            "Bad Time",
            "On days like these...",
            "Fin",
        };
        private void ItemGiver(int itemID, int amount, string notifyText)
        {
            if (notifyText != null)
            {
                Notify(notifyText, "");
            }
            for (int i = 0; i < amount; i++)
            {
                LootEngine.GivePrefabToPlayer(PickupObjectDatabase.GetById(itemID).gameObject, LastOwner);
            }
        }
        private void StatModifierStuff(PlayerStats.StatType stat, float amount, StatModifier.ModifyMethod modifyType)
        {
            if (modifyType == StatModifier.ModifyMethod.MULTIPLICATIVE)
            {
                StatModifier statModifier = new StatModifier();
                statModifier.amount = amount;
                statModifier.modifyType = modifyType;
                statModifier.statToBoost = stat;
                LastOwner.ownerlessStatModifiers.Add(statModifier);
                LastOwner.stats.RecalculateStats(LastOwner, false, false);
            }
            else
            {
                if (amount < 0)
                {
                    if ((amount * amount) <= LastOwner.stats.GetStatValue(stat))
                    {
                        StatModifier statModifier = new StatModifier();
                        statModifier.amount = amount;
                        statModifier.modifyType = modifyType;
                        statModifier.statToBoost = stat;
                        LastOwner.ownerlessStatModifiers.Add(statModifier);
                        LastOwner.stats.RecalculateStats(LastOwner, false, false);
                    }
                }
                else
                {
                    StatModifier statModifier = new StatModifier();
                    statModifier.amount = amount;
                    statModifier.modifyType = modifyType;
                    statModifier.statToBoost = stat;
                    LastOwner.ownerlessStatModifiers.Add(statModifier);
                    LastOwner.stats.RecalculateStats(LastOwner, false, false);
                }
            }
        }

        public override void DoEffect(PlayerController user)
        {
            try
            {
                user.PlayEffectOnActor(ResourceCache.Acquire("Global VFX/vfx_daisukefavor") as GameObject, Vector3.zero, true, false, false);
                MakeKyleDisappointedInMe(UnityEngine.Random.Range(1, 48));
            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
                ETGModConsole.Log(e.StackTrace);
            }
        }
        private void Notify(string header, string text)
        {
            var sprite = GameUIRoot.Instance.notificationController.notificationObjectSprite;
            GameUIRoot.Instance.notificationController.DoCustomNotification(
                header,
                text,
                sprite.Collection,
                sprite.spriteId,
                UINotificationController.NotificationColor.PURPLE,
                false,
                false);
        }

        /*protected override void DoEffect(PlayerController user)
        {
            PlayableCharacters characterIdentity = user.characterIdentity;
            int randomDivineIntervention = UnityEngine.Random.Range(1, 21);
            ETGModConsole.Log("The random number selected is: " + randomDivineIntervention);
            user.PlayEffectOnActor(ResourceCache.Acquire("Global VFX/vfx_daisukefavor") as GameObject, Vector3.zero, true, false, false);
            if (randomDivineIntervention == 1)
            {
                string header = "1. WRATH OF ICOSAHEDRAX";
                string text = "";
                this.Notify(header, text);
            }
            else if (randomDivineIntervention == 2)
            {
                string header = "2. Health Up";
                string text = "";
                this.Notify(header, text);
                if (characterIdentity != PlayableCharacters.Robot)
                {
                    float currentHealth = user.stats.GetBaseStatValue(PlayerStats.StatType.Health);                   
                    currentHealth += 1f;
                    user.stats.SetBaseStatValue(PlayerStats.StatType.Health, currentHealth, user);
                }
                else if (characterIdentity == PlayableCharacters.Robot)
                {
                    user.healthHaver.Armor = user.healthHaver.Armor + 1;
                }
            }
            else if (randomDivineIntervention == 3)
            {
                string header = "3. Health Down";
                string text = "";
                this.Notify(header, text);
                if (characterIdentity != PlayableCharacters.Robot)
                {
                    float currentHealth = user.stats.GetBaseStatValue(PlayerStats.StatType.Health);
                    if (currentHealth > 1)
                    {
                        currentHealth -= 1f;
                        user.stats.SetBaseStatValue(PlayerStats.StatType.Health, currentHealth, user);
                    }
                }
                else if (characterIdentity == PlayableCharacters.Robot)
                {
                    if (user.healthHaver.Armor > 1)
                    {
                        user.healthHaver.Armor = user.healthHaver.Armor - 1;
                    }
                }


            }
            else if (randomDivineIntervention == 4)
            {
                string header = "4. Damage Up";
                string text = "";
                this.Notify(header, text);
                float currentDamage = user.stats.GetBaseStatValue(PlayerStats.StatType.Damage);
                currentDamage *= 1.1f;
                user.stats.SetBaseStatValue(PlayerStats.StatType.Damage, currentDamage, user);
            }
            else if (randomDivineIntervention == 5)
            {
                string header = "5. Damage Down";
                string text = "";
                this.Notify(header, text);
                float currentDamage = user.stats.GetBaseStatValue(PlayerStats.StatType.Damage);
                currentDamage *= 0.9f;
                user.stats.SetBaseStatValue(PlayerStats.StatType.Damage, currentDamage, user);
            }
            else if (randomDivineIntervention == 6)
            {
                string header = "6. Speed Up";
                string text = "";
                this.Notify(header, text);
                float currentSpeed = user.stats.GetBaseStatValue(PlayerStats.StatType.MovementSpeed);
                currentSpeed *= 1.2f;
                user.stats.SetBaseStatValue(PlayerStats.StatType.MovementSpeed, currentSpeed, user);
            }
            else if (randomDivineIntervention == 7)
            {
                string header = "7. Speed Down";
                string text = "";
                this.Notify(header, text);
                float currentSpeed = user.stats.GetBaseStatValue(PlayerStats.StatType.MovementSpeed);
                currentSpeed *= 0.8f;
                user.stats.SetBaseStatValue(PlayerStats.StatType.MovementSpeed, currentSpeed, user);
            }
            else if (randomDivineIntervention == 8)
            {
                string header = "8. Junked";
                string text = "";
                this.Notify(header, text);
                int junkAmount = UnityEngine.Random.Range(1, 3);
                PickupObject byId = PickupObjectDatabase.GetById(127);
                if (junkAmount == 1)
                {
                    user.AcquirePassiveItemPrefabDirectly(byId as PassiveItem);
                }
                else if (junkAmount == 2)
                {
                    user.AcquirePassiveItemPrefabDirectly(byId as PassiveItem);
                    user.AcquirePassiveItemPrefabDirectly(byId as PassiveItem);
                }
                else
                {
                    user.AcquirePassiveItemPrefabDirectly(byId as PassiveItem);
                    user.AcquirePassiveItemPrefabDirectly(byId as PassiveItem);
                    user.AcquirePassiveItemPrefabDirectly(byId as PassiveItem);
                }
            }
            else if (randomDivineIntervention == 9)
            {
                string header = "9. Accuracy Up";
                string text = "";
                this.Notify(header, text);
                float currentStat = user.stats.GetBaseStatValue(PlayerStats.StatType.Accuracy);
                currentStat *= 0.8f;
                user.stats.SetBaseStatValue(PlayerStats.StatType.Accuracy, currentStat, user); 
            }
            else if (randomDivineIntervention == 10)
            {
                string header = "10. Accuracy Down";
                string text = "";
                this.Notify(header, text);
                float currentStat = user.stats.GetBaseStatValue(PlayerStats.StatType.Accuracy);
                currentStat *= 1.2f;
                user.stats.SetBaseStatValue(PlayerStats.StatType.Accuracy, currentStat, user);
            }
            else if (randomDivineIntervention == 11)
            {
                string header = "11. Glassed";
                string text = "";
                this.Notify(header, text);
                int glassAmount = UnityEngine.Random.Range(2, 4);
                PickupObject byId = PickupObjectDatabase.GetById(565);
                if (glassAmount == 2)
                {
                    user.AcquirePassiveItemPrefabDirectly(byId as PassiveItem);
                    user.AcquirePassiveItemPrefabDirectly(byId as PassiveItem);
                }
                else if (glassAmount == 3)
                {
                    user.AcquirePassiveItemPrefabDirectly(byId as PassiveItem);
                    user.AcquirePassiveItemPrefabDirectly(byId as PassiveItem);
                    user.AcquirePassiveItemPrefabDirectly(byId as PassiveItem);
                }
                else
                {
                    user.AcquirePassiveItemPrefabDirectly(byId as PassiveItem);
                    user.AcquirePassiveItemPrefabDirectly(byId as PassiveItem);
                    user.AcquirePassiveItemPrefabDirectly(byId as PassiveItem);
                    user.AcquirePassiveItemPrefabDirectly(byId as PassiveItem);
                }
            }
            else if (randomDivineIntervention == 12)
            {
                string header = "12. Cursed";
                string text = "";
                this.Notify(header, text);
                float currentStat = user.stats.GetBaseStatValue(PlayerStats.StatType.Curse);
                currentStat += 1;
                user.stats.SetBaseStatValue(PlayerStats.StatType.Curse, currentStat, user);
            }
            else if (randomDivineIntervention == 13)
            {
                string header = "13. Cleansed";
                string text = "";
                this.Notify(header, text);
                float currentStat = user.stats.GetBaseStatValue(PlayerStats.StatType.Curse);
                if (currentStat > 0)
                {
                    currentStat -= 1;
                    user.stats.SetBaseStatValue(PlayerStats.StatType.Curse, currentStat, user);
                }
            }
            else if (randomDivineIntervention == 14)
            {
                string header = "14. Cool Dude-ified Bro";
                string text = "";
                this.Notify(header, text);
                float currentStat = user.stats.GetBaseStatValue(PlayerStats.StatType.Coolness);
                currentStat += 1;
                user.stats.SetBaseStatValue(PlayerStats.StatType.Coolness, currentStat, user);
            }
            else if (randomDivineIntervention == 15)
            {
                string header = "15. Uncool";
                string text = "";
                this.Notify(header, text);
                float currentStat = user.stats.GetBaseStatValue(PlayerStats.StatType.Coolness);
                if (currentStat > 0)
                {
                    currentStat -= 1;
                    user.stats.SetBaseStatValue(PlayerStats.StatType.Coolness, currentStat, user);
                }
            }
            else if (randomDivineIntervention == 16)
            {
                string header = "16. Shot Speed Up";
                string text = "";
                this.Notify(header, text);
                float currentStat = user.stats.GetBaseStatValue(PlayerStats.StatType.ProjectileSpeed);
                currentStat *= 1.1f;
                user.stats.SetBaseStatValue(PlayerStats.StatType.ProjectileSpeed, currentStat, user);
            }
            else if (randomDivineIntervention == 17)
            {
                string header = "17. Shot Speed Down";
                string text = "";
                this.Notify(header, text);
                float currentStat = user.stats.GetBaseStatValue(PlayerStats.StatType.ProjectileSpeed);
                currentStat *= 0.9f;
                user.stats.SetBaseStatValue(PlayerStats.StatType.ProjectileSpeed, currentStat, user);
            }
            else if (randomDivineIntervention == 18)
            {
                string header = "18. Firerate Up";
                string text = "";
                this.Notify(header, text);
                float currentStat = user.stats.GetBaseStatValue(PlayerStats.StatType.RateOfFire);
                currentStat *= 1.1f;
                user.stats.SetBaseStatValue(PlayerStats.StatType.RateOfFire, currentStat, user);
            }
            else if (randomDivineIntervention == 19)
            {
                string header = "19. Firerate Down";
                string text = "";
                this.Notify(header, text);
                float currentStat = user.stats.GetBaseStatValue(PlayerStats.StatType.RateOfFire);
                currentStat *= 0.9f;
                user.stats.SetBaseStatValue(PlayerStats.StatType.RateOfFire, currentStat, user);
            }
            else if (randomDivineIntervention == 20)
            {
                string header = "20. Ammo Capacity Up";
                string text = "";
                this.Notify(header, text);
                float currentStat = user.stats.GetBaseStatValue(PlayerStats.StatType.AmmoCapacityMultiplier);
                currentStat *= 1.1f;
                user.stats.SetBaseStatValue(PlayerStats.StatType.AmmoCapacityMultiplier, currentStat, user);
            }
            else if (randomDivineIntervention == 20)
            {
                string header = "21. Ammo Capacity Down";
                string text = "";
                this.Notify(header, text);
                float currentStat = user.stats.GetBaseStatValue(PlayerStats.StatType.AmmoCapacityMultiplier);
                currentStat *= 0.9f;
                user.stats.SetBaseStatValue(PlayerStats.StatType.AmmoCapacityMultiplier, currentStat, user);
            }
            else
            {
                string header = "Icosahedrax deems you unworthy.";
                string text = "";
                this.Notify(header, text);
            }
        }*/
        //user.carriedconsumable.currency
    }
}
