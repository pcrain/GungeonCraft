using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ItemAPI;
using UnityEngine;

namespace NevernamedsItems
{
    class LuckyCoin : PlayerItem
    {
        //Call this method from the Start() method of your ETGModule extension class
        public static void Init()
        {
            //The name of the item
            string itemName = "Lucky Coin";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it.
            string resourceName = "NevernamedsItems/Resources/luckycoin_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a ActiveItem component to the object
            var item = obj.AddComponent<LuckyCoin>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Heads or Tails";
            string longDesc = "50/50 change for a temporary stat bonus or a temporary stat penalty when used.\n\n" + "Legends tell of a time when coins such as this one were commonplace in the gungeon. They've since been exchanged for more...modern currency.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //"kts" here is the item pool. In the console you'd type kts:sweating_bullets
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Set the cooldown type and duration of the cooldown
            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.Damage, 500);

            //Adds a passive modifier, like curse, coolness, damage, etc. to the item. Works for passives and actives.

            //Set some other fields
            item.consumable = false;
            item.quality = ItemQuality.C;
           
            item.AddToSubShop(ItemBuilder.ShopType.Cursula);

            List<string> mandatorySynergyItems = new List<string>() { "nn:lucky_coin", "seven_leaf_clover" };
            CustomSynergies.Add("Even Luckier!", mandatorySynergyItems);

            LuckyCoinID = item.PickupObjectId;
        }
        public static int LuckyCoinID;
        //SYNERGIES
        //W/ Seven Leaf Clover --> Even Luckier!
        //W/ Lump of Space Metal / Loose Change / Coin Crown / Iron Coin / Gold Junk / Table Tech Money --> Prosperity

        //Add the item's functionality down here! I stole most of this from the Stuffed Star active item code!
        bool GoodEffectActive = false;
        bool BadEffectActive = false;
        float movementBuff = -1;
        float movementDeBuff = -1;
        float damageBuff = -1;
        float damageDeBuff = -1;
        float duration = 25f;
        public override void DoEffect(PlayerController user)
        {
            if (user.HasPickupID(289))
            {
                if (UnityEngine.Random.value > .25f)
                {
                    GoodEffectActive = true;
                    AkSoundEngine.PostEvent("Play_WPN_radgun_noice_01", base.gameObject);
                    if (user.HasPickupID(Gungeon.Game.Items["nn:lump_of_space_metal"].PickupObjectId) || user.HasPickupID(Gungeon.Game.Items["nn:loose_change"].PickupObjectId) || user.HasPickupID(214) || user.HasPickupID(272) || user.HasPickupID(614) || user.HasPickupID(397))
                    {
                        LootEngine.SpawnItem(PickupObjectDatabase.GetById(68).gameObject, user.specRigidbody.UnitCenter, Vector2.zero, 1f, false, true, false);
                        LootEngine.SpawnItem(PickupObjectDatabase.GetById(68).gameObject, user.specRigidbody.UnitCenter, Vector2.zero, 1f, false, true, false);
                        LootEngine.SpawnItem(PickupObjectDatabase.GetById(68).gameObject, user.specRigidbody.UnitCenter, Vector2.zero, 1f, false, true, false);
                        LootEngine.SpawnItem(PickupObjectDatabase.GetById(68).gameObject, user.specRigidbody.UnitCenter, Vector2.zero, 1f, false, true, false);
                        LootEngine.SpawnItem(PickupObjectDatabase.GetById(68).gameObject, user.specRigidbody.UnitCenter, Vector2.zero, 1f, false, true, false);
                        LootEngine.SpawnItem(PickupObjectDatabase.GetById(68).gameObject, user.specRigidbody.UnitCenter, Vector2.zero, 1f, false, true, false);
                        LootEngine.SpawnItem(PickupObjectDatabase.GetById(68).gameObject, user.specRigidbody.UnitCenter, Vector2.zero, 1f, false, true, false);
                        LootEngine.SpawnItem(PickupObjectDatabase.GetById(68).gameObject, user.specRigidbody.UnitCenter, Vector2.zero, 1f, false, true, false);
                        LootEngine.SpawnItem(PickupObjectDatabase.GetById(68).gameObject, user.specRigidbody.UnitCenter, Vector2.zero, 1f, false, true, false);
                        LootEngine.SpawnItem(PickupObjectDatabase.GetById(68).gameObject, user.specRigidbody.UnitCenter, Vector2.zero, 1f, false, true, false);
                    }
                    StartGoodEffect(user);
                    StartCoroutine(ItemBuilder.HandleDuration(this, duration, user, EndGoodEffect));
                    //ETGModConsole.Log("Lucky Coin has given you the positive effect, and thinks you have the Seven Leaf Clover");
                }
                else
                {
                    BadEffectActive = true;
                    AkSoundEngine.PostEvent("Play_WPN_radgun_wack_01", base.gameObject);
                    StartBadEffect(user);
                    StartCoroutine(ItemBuilder.HandleDuration(this, duration, user, EndBadEffect));
                    //ETGModConsole.Log("Lucky Coin has given you the negative effect, and thinks you have the Seven Leaf Clover");
                }
            }
            else
            {
                if (UnityEngine.Random.value < .5f)
                {
                    GoodEffectActive = true;
                    AkSoundEngine.PostEvent("Play_WPN_radgun_noice_01", base.gameObject);
                    if (user.HasPickupID(Gungeon.Game.Items["nn:lump_of_space_metal"].PickupObjectId) || user.HasPickupID(Gungeon.Game.Items["nn:loose_change"].PickupObjectId) || user.HasPickupID(214) || user.HasPickupID(272) || user.HasPickupID(614) || user.HasPickupID(397))
                    {
                        LootEngine.SpawnItem(PickupObjectDatabase.GetById(68).gameObject, user.specRigidbody.UnitCenter, Vector2.zero, 1f, false, true, false);
                        LootEngine.SpawnItem(PickupObjectDatabase.GetById(68).gameObject, user.specRigidbody.UnitCenter, Vector2.zero, 1f, false, true, false);
                        LootEngine.SpawnItem(PickupObjectDatabase.GetById(68).gameObject, user.specRigidbody.UnitCenter, Vector2.zero, 1f, false, true, false);
                        LootEngine.SpawnItem(PickupObjectDatabase.GetById(68).gameObject, user.specRigidbody.UnitCenter, Vector2.zero, 1f, false, true, false);
                        LootEngine.SpawnItem(PickupObjectDatabase.GetById(68).gameObject, user.specRigidbody.UnitCenter, Vector2.zero, 1f, false, true, false);
                        LootEngine.SpawnItem(PickupObjectDatabase.GetById(68).gameObject, user.specRigidbody.UnitCenter, Vector2.zero, 1f, false, true, false);
                        LootEngine.SpawnItem(PickupObjectDatabase.GetById(68).gameObject, user.specRigidbody.UnitCenter, Vector2.zero, 1f, false, true, false);
                        LootEngine.SpawnItem(PickupObjectDatabase.GetById(68).gameObject, user.specRigidbody.UnitCenter, Vector2.zero, 1f, false, true, false);
                        LootEngine.SpawnItem(PickupObjectDatabase.GetById(68).gameObject, user.specRigidbody.UnitCenter, Vector2.zero, 1f, false, true, false);
                        LootEngine.SpawnItem(PickupObjectDatabase.GetById(68).gameObject, user.specRigidbody.UnitCenter, Vector2.zero, 1f, false, true, false);
                    }
                    StartGoodEffect(user);
                    StartCoroutine(ItemBuilder.HandleDuration(this, duration, user, EndGoodEffect));
                    //ETGModConsole.Log("Lucky Coin has given you the positive effect.");
                }
                else
                {
                    BadEffectActive = true;
                    AkSoundEngine.PostEvent("Play_WPN_radgun_wack_01", base.gameObject);
                    StartBadEffect(user);
                    StartCoroutine(ItemBuilder.HandleDuration(this, duration, user, EndBadEffect));
                    //ETGModConsole.Log("Lucky Coin has given you the negative effect.");
                }
            }
        }
        private void StartGoodEffect(PlayerController user)
        {
            //MOVEMENT SPEED BONUS
            float curMovement = user.stats.GetBaseStatValue(PlayerStats.StatType.MovementSpeed); //Get's the player's speed and stores it in a var called 'curMovement'
            float newMovement = curMovement * 1.3f; //Makes a variable named 'newMovement by multiplying 'curMovement' by 2
            user.stats.SetBaseStatValue(PlayerStats.StatType.MovementSpeed, newMovement, user); //Sets their movement speed to 'newMovement'
            movementBuff = newMovement - curMovement;
            
            //DAMAGE BONUS
            float curDamage = user.stats.GetBaseStatValue(PlayerStats.StatType.Damage); //Get's the player's speed and stores it in a var called 'curMovement'
            float newDamage = curDamage * 2f; //Makes a variable named 'newMovement by multiplying 'curMovement' by 2
            user.stats.SetBaseStatValue(PlayerStats.StatType.Damage, newDamage, user); //Sets their movement speed to 'newMovement'
            damageBuff = newDamage - curDamage;
        }
        private void StartBadEffect(PlayerController user)
        {
            this.CanBeDropped = false;
            //MOVEMENT SPEED DEFICIT
            float curMovement = user.stats.GetBaseStatValue(PlayerStats.StatType.MovementSpeed); //Get's the player's speed and stores it in a var called 'curMovement'
            float newMovement = curMovement * 0.7f; //Makes a variable named 'newMovement' by halving 'Currentmovement'
            user.stats.SetBaseStatValue(PlayerStats.StatType.MovementSpeed, newMovement, user); //Sets their movement speed to 'newMovement'
            movementDeBuff = curMovement - newMovement;
            //ETGModConsole.Log("INITIATING SPEED DEBUFF\n" + "Current Speed: " + curMovement + "\nNew Speed: " + newMovement + " (curSpeed(" + curMovement + ") x 0.5" + "\nSpeed Debuff: " + movementDeBuff + "(curMovement("+curMovement+") - (newMovement("+newMovement+")");
            
            //DAMAGE DEFICIT
            float curDamage = user.stats.GetBaseStatValue(PlayerStats.StatType.Damage); //Get's the player's speed and stores it in a var called 'curMovement'
            float newDamage = curDamage * 0.5f; //Makes a variable named 'newMovement by multiplying 'curMovement' by 2
            user.stats.SetBaseStatValue(PlayerStats.StatType.Damage, newDamage, user); //Sets their movement speed to 'newMovement'
            damageDeBuff = curDamage - newDamage;
            //ETGModConsole.Log("INITIATING DAMAGE DEBUFF\n"+"Current Damage: "+curDamage+"\nNew Damage: "+newDamage+" (curDamage("+curDamage+") x 0.5"+"\nDamage Debuff: " + damageDeBuff);
        }

        //Resets the player back to their original stats
        private void EndGoodEffect(PlayerController user)
        {
            //REMOVE MOVEMENT SPEED
            if (movementBuff <= 0)
            {
                ETGModConsole.Log("The variable 'movementBuff' is less than or equal to 0 (" + movementBuff + ")");
                return;
            }
            float curMovement = user.stats.GetBaseStatValue(PlayerStats.StatType.MovementSpeed);
            float newMovement = curMovement - movementBuff;
            user.stats.SetBaseStatValue(PlayerStats.StatType.MovementSpeed, newMovement, user);
            movementBuff = -1;
            GoodEffectActive = false;

            //REMOVE DAMAGE
            if (damageBuff <= 0)
            {
                ETGModConsole.Log("The variable 'damageBuff' is less than or equal to 0 (" + damageBuff + ")");
                return;
            }
            float curDamage = user.stats.GetBaseStatValue(PlayerStats.StatType.Damage);
            float newDamage = curDamage - damageBuff;
            user.stats.SetBaseStatValue(PlayerStats.StatType.Damage, newDamage, user);
            damageBuff = -1;
        }
        private void EndBadEffect(PlayerController user)
        {
            //GIVE BACK SPEED
            if (movementDeBuff <= 0) return;
            float curMovement = user.stats.GetBaseStatValue(PlayerStats.StatType.MovementSpeed);
            float newMovement = curMovement + movementDeBuff;
            user.stats.SetBaseStatValue(PlayerStats.StatType.MovementSpeed, newMovement, user);
            //ETGModConsole.Log("INITIATING SPEED GIVEBACK\n" + "Current Speed: " + curMovement + "\nNew Speed: " + newMovement + " (curSpeed(" + curMovement + ") + (speedDeBuff(" + movementDeBuff + ")" + "\nSpeed Debuff: " + movementDeBuff);
            movementDeBuff = -1;
            BadEffectActive = false;

            //GIVE BACK DAMAGE
            if (damageDeBuff <= 0) return;
            float curDamage = user.stats.GetBaseStatValue(PlayerStats.StatType.Damage);
            float newDamage = curDamage + damageDeBuff;
            user.stats.SetBaseStatValue(PlayerStats.StatType.Damage, newDamage, user);
            //ETGModConsole.Log("INITIATING DAMAGE GIVEBACK\n" + "Current Damage: " + curDamage + "\nNew Damage: " + newDamage + " (curDamage(" + curDamage + ") + (damageDeBuff(" + damageDeBuff +")" + "\nDamage Debuff: " + damageDeBuff);
            damageDeBuff = -1;
            this.CanBeDropped = true;
        }
        public override void OnPreDrop(PlayerController user)
        {
            if (base.IsCurrentlyActive)
            {
                base.IsCurrentlyActive = false;
                if (GoodEffectActive == true)
                {
                    EndGoodEffect(user);
                }
                else if (BadEffectActive == true)
                {
                    EndBadEffect(user);
                }
                else return;
            }
        }
    }
}
