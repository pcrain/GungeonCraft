using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dungeonator;
using UnityEngine;
using ItemAPI;

namespace NevernamedsItems
{
    class Bombinomicon : PlayerItem
    {
        //Call this method from the Start() method of your ETGModule extension class
        public static void Init()
        {
            //The name of the item
            string itemName = "Bombinomicon";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it.
            string resourceName = "NevernamedsItems/Resources/bombinomicon_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a ActiveItem component to the object
            var item = obj.AddComponent<Bombinomicon>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "GUTS AN' GLORY, LADS!";
            string longDesc = "An ancient tome literally full to bursting with explosive knowledge."+"\n\nPeople who would read this recreationally probably huff gunpowder.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //"kts" here is the item pool. In the console you'd type kts:sweating_bullets
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Set the cooldown type and duration of the cooldown
            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.Damage, 200);
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.Curse, 1f, StatModifier.ModifyMethod.ADDITIVE);

            //Adds a passive modifier, like curse, coolness, damage, etc. to the item. Works for passives and actives.

            //Set some other fields
            item.consumable = false;
            item.quality = ItemQuality.B;

            item.AddToSubShop(ItemBuilder.ShopType.Cursula);
            ID = item.PickupObjectId;
        }
        public static int ID;

        //Add the item's functionality down here! I stole most of this from the Stuffed Star active item code!
        public override void DoEffect(PlayerController user)
        {
            int bombToSpawn = 108;
            if (user.HasPickupID(109) || user.HasPickupID(364) || user.HasPickupID(170)) bombToSpawn = 109;

            if (user.HasPickupID(118)) RandomBombSpawn(user, bombToSpawn);

            List<AIActor> activeEnemies = user.CurrentRoom.GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
            if (activeEnemies != null)
            {
                for (int i = 0; i < activeEnemies.Count; i++)
                {
                    AIActor enemy = activeEnemies[i];
                    SpawnObjectPlayerItem bigbombPrefab = PickupObjectDatabase.GetById(bombToSpawn).GetComponent<SpawnObjectPlayerItem>();
                    GameObject bigbombObject = bigbombPrefab.objectToSpawn.gameObject;

                    GameObject bigbombObject2 = UnityEngine.Object.Instantiate<GameObject>(bigbombObject, enemy.sprite.WorldBottomCenter, Quaternion.identity);
                    tk2dBaseSprite bombsprite = bigbombObject2.GetComponent<tk2dBaseSprite>();
                    if (bombsprite)
                    {
                        bombsprite.PlaceAtPositionByAnchor(enemy.sprite.WorldBottomCenter, tk2dBaseSprite.Anchor.MiddleCenter);
                    }
                    if (user.HasPickupID(19) || user.HasPickupID(332))
                    {
                        RestoreAmmo(user, activeEnemies.Count);
                        SpawnObjectPlayerItem secondarybigbombPrefab = PickupObjectDatabase.GetById(bombToSpawn).GetComponent<SpawnObjectPlayerItem>();
                        GameObject secondarybigbombObject = secondarybigbombPrefab.objectToSpawn.gameObject;

                        GameObject secondarybigbombObject2 = UnityEngine.Object.Instantiate<GameObject>(secondarybigbombObject, enemy.sprite.WorldBottomCenter, Quaternion.identity);
                        tk2dBaseSprite secondarybombsprite = secondarybigbombObject2.GetComponent<tk2dBaseSprite>();
                        if (secondarybombsprite)
                        {
                            secondarybombsprite.PlaceAtPositionByAnchor(enemy.sprite.WorldBottomCenter, tk2dBaseSprite.Anchor.MiddleCenter);
                        }
                    }
                }
            }
        }
        private void RestoreAmmo(PlayerController user, int amount)
        {
            foreach (Gun gun in user.inventory.AllGuns)
            {
                if (gun.PickupObjectId == 19 || gun.PickupObjectId == 332)
                {
                    gun.GainAmmo(amount);
                }
            }
        }
        private void RandomBombSpawn(PlayerController user, int bombToSpawn)
        {
            for (int i = 0; i < 10; i++)
            {
                Vector3 positionToSpawn3 = user.CurrentRoom.GetRandomVisibleClearSpot(1, 1).ToVector3();

                SpawnObjectPlayerItem bigbombPrefab = PickupObjectDatabase.GetById(bombToSpawn).GetComponent<SpawnObjectPlayerItem>();
                GameObject bigbombObject = bigbombPrefab.objectToSpawn.gameObject;

                GameObject bigbombObject2 = UnityEngine.Object.Instantiate<GameObject>(bigbombObject, positionToSpawn3, Quaternion.identity);
                tk2dBaseSprite bombsprite = bigbombObject2.GetComponent<tk2dBaseSprite>();
                if (bombsprite)
                {
                    bombsprite.PlaceAtPositionByAnchor(positionToSpawn3, tk2dBaseSprite.Anchor.MiddleCenter);
                }
                if (user.HasPickupID(19) || user.HasPickupID(332))
                {
                    RestoreAmmo(user, 10);
                    SpawnObjectPlayerItem secondarybigbombPrefab = PickupObjectDatabase.GetById(bombToSpawn).GetComponent<SpawnObjectPlayerItem>();
                    GameObject secondarybigbombObject = secondarybigbombPrefab.objectToSpawn.gameObject;

                    GameObject secondarybigbombObject2 = UnityEngine.Object.Instantiate<GameObject>(secondarybigbombObject, positionToSpawn3, Quaternion.identity);
                    tk2dBaseSprite secondarybombsprite = secondarybigbombObject2.GetComponent<tk2dBaseSprite>();
                    if (secondarybombsprite)
                    {
                        secondarybombsprite.PlaceAtPositionByAnchor(positionToSpawn3, tk2dBaseSprite.Anchor.MiddleCenter);
                    }
                }
            }
        }
    }
}