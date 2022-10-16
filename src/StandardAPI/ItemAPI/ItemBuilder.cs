using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using System.Collections;
using MonoMod.RuntimeDetour;
using ItemAPI;

namespace ItemAPI
{
    public static class ItemBuilder
    {
       /* /// <summary>
        /// Adds a tk2dSpriteAnimator component to an object and adds that sprite to the 
        /// </summary>
        public static GameObject AddAnimatedSpriteToObject(string name, List<string> resourcePaths, GameObject obj = null)
        {
            GameObject spriteObject = SpriteBuilder.SpriteFromResource(resourcePaths[0], obj);
            tk2dSpriteAnimator spriteAnimator = obj.AddComponent<tk2dSpriteAnimator>();
            var idList = new List<int>();
            foreach (string sprite in resourcePaths)
            {
                idList.Add(SpriteBuilder.AddSpriteToCollection(sprite, spriteObject.GetComponent<tk2dSprite>().Collection));
            }
            spriteAnimator.playAutomatically = true;
            SpriteBuilder.AddAnimation(spriteAnimator, spriteObject.GetComponent<tk2dSprite>().Collection, idList, name + "_idle", tk2dSpriteAnimationClip.WrapMode.Loop, fps);
            spriteObject.name = name;
            return spriteObject;
        }*/
        public enum CooldownType
        {
            Timed, Damage, PerRoom, None
        }

        public enum ShopType
        {
            Goopton, Flynt, Cursula, Trorc, OldRed
        }

        public static Dictionary<ShopType, GenericLootTable> shopInventories;

        /// <summary>
        /// Initializes hooks and grabs necessary assets for building items
        /// </summary>
        public static void Init()
        {
            FakePrefabHooks.Init();
            CompanionBuilder.Init();
            LoadShopTables();
        }

        /// <summary>
        /// Loads the loot tables of shops for later modification.
        /// </summary>
        private static void LoadShopTables()
        {
            shopInventories = new Dictionary<ShopType, GenericLootTable>();
            shopInventories.Add(ShopType.Flynt, LoadShopTable("Shop_Key_Items_01"));
            shopInventories.Add(ShopType.Trorc, LoadShopTable("Shop_Truck_Items_01"));
            shopInventories.Add(ShopType.Cursula, LoadShopTable("Shop_Curse_Items_01"));
            shopInventories.Add(ShopType.Goopton, LoadShopTable("Shop_Goop_Items_01"));
            shopInventories.Add(ShopType.OldRed, LoadShopTable("Shop_Blank_Items_01"));
        }

        /// <summary>
        /// Gets a loot table from shared_auto_001 from name
        /// </summary>
        public static GenericLootTable LoadShopTable(string assetName)
        {
            return ResourceManager.LoadAssetBundle("shared_auto_001").LoadAsset<GenericLootTable>(assetName);
        }

        /// <summary>
        /// Adds a tk2dSprite component to an object and adds that sprite to the 
        /// ammonomicon for later use. If obj is null, returns a new GameObject with the sprite
        /// </summary>
        public static GameObject AddSpriteToObject(string name, string resourcePath, GameObject obj = null)
        {
            GameObject spriteObject = SpriteBuilder.SpriteFromResource(resourcePath, obj);
            FakePrefab.MarkAsFakePrefab(spriteObject);
            obj.SetActive(false);

            spriteObject.name = name;
            return spriteObject;
        }

        /// <summary>
        /// Finishes the item setup, adds it to the item databases, adds an encounter trackable 
        /// blah, blah, blah
        /// </summary>
        public static void SetupItem(this PickupObject item, string shortDesc, string longDesc, string idPool = "ItemAPI")
        {
            try
            {
                item.encounterTrackable = null;

                ETGMod.Databases.Items.SetupItem(item, item.name);
                SpriteBuilder.AddToAmmonomicon(item.sprite.GetCurrentSpriteDef());
                item.encounterTrackable.journalData.AmmonomiconSprite = item.sprite.GetCurrentSpriteDef().name;

                item.SetName(item.name);
                item.SetShortDescription(shortDesc);
                item.SetLongDescription(longDesc);

                if (item is PlayerItem)
                    (item as PlayerItem).consumable = false;
                Gungeon.Game.Items.Add(idPool + ":" + item.name.ToLower().Replace(" ", "_"), item);
                ETGMod.Databases.Items.Add(item);
            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
                ETGModConsole.Log(e.StackTrace);
            }
        }

        public static void AddToSubShop(this PickupObject po, ShopType type, float weight = 1)
        {
            shopInventories[type].defaultItemDrops.Add(new WeightedGameObject()
            {
                pickupId = po.PickupObjectId,
                weight = weight,
                rawGameObject = po.gameObject,
                forceDuplicatesPossible = false,
                additionalPrerequisites = new DungeonPrerequisite[0]
            });
        }

        /// <summary>
        /// Sets the cooldown type and length of a PlayerItem, and resets all other cooldown types
        /// </summary>
        public static void SetCooldownType(this PlayerItem item, CooldownType cooldownType, float value)
        {
            item.damageCooldown = -1;
            item.roomCooldown = -1;
            item.timeCooldown = -1;

            switch (cooldownType)
            {
                case CooldownType.Timed:
                    item.timeCooldown = value;
                    break;
                case CooldownType.Damage:
                    item.damageCooldown = value;
                    break;
                case CooldownType.PerRoom:
                    item.roomCooldown = (int)value;
                    break;
            }
        }

        /// <summary>
        /// Adds a passive player stat modifier to a PlayerItem or PassiveItem
        /// </summary>
        public static StatModifier AddPassiveStatModifier(this PickupObject po, PlayerStats.StatType statType, float amount, StatModifier.ModifyMethod method = StatModifier.ModifyMethod.ADDITIVE)
        {
            StatModifier modifier = new StatModifier();
            modifier.amount = amount;
            modifier.statToBoost = statType;
            modifier.modifyType = method;

            po.AddPassiveStatModifier(modifier);
            return modifier;
        }

        public static void AddPassiveStatModifier(this PickupObject po, StatModifier modifier)
        {
            if (po is PlayerItem)
            {
                var item = (po as PlayerItem);
                if (item.passiveStatModifiers == null)
                    item.passiveStatModifiers = new StatModifier[] { modifier };
                else
                    item.passiveStatModifiers = item.passiveStatModifiers.Concat(new StatModifier[] { modifier }).ToArray();
            }
            else if (po is PassiveItem)
            {
                var item = (po as PassiveItem);
                if (item.passiveStatModifiers == null)
                    item.passiveStatModifiers = new StatModifier[] { modifier };
                else
                    item.passiveStatModifiers = item.passiveStatModifiers.Concat(new StatModifier[] { modifier }).ToArray();
            }
            else
            {
                throw new NotSupportedException("Object must be of type PlayerItem or PassiveItem");
            }
        }

        public static bool RemovePassiveStatModifier(this PickupObject po, StatModifier modifier)
        {
            bool success = false;
            if (po is PlayerItem)
            {
                var item = (po as PlayerItem);
                if (item.passiveStatModifiers == null) return false;

                var list = item.passiveStatModifiers.ToList();
                success = list.Remove(modifier);
                item.passiveStatModifiers = list.ToArray();
            }
            else if (po is PassiveItem)
            {
                var item = (po as PassiveItem);
                if (item.passiveStatModifiers == null) return false;

                var list = item.passiveStatModifiers.ToList();
                success = list.Remove(modifier);
                item.passiveStatModifiers = list.ToArray();
            }
            else
            {
                throw new NotSupportedException("Object must be of type PlayerItem or PassiveItem");
            }
            return success;
        }


        public static IEnumerator HandleDuration(PlayerItem item, float duration, PlayerController user, Action<PlayerController> OnFinish)
        {
            if (item.IsCurrentlyActive)
            {
                yield break;
            }

            SetPrivateType<PlayerItem>(item, "m_isCurrentlyActive", true);
            SetPrivateType<PlayerItem>(item, "m_activeElapsed", 0f);
            SetPrivateType<PlayerItem>(item, "m_activeDuration", duration);
            item.OnActivationStatusChanged?.Invoke(item);

            float elapsed = GetPrivateType<PlayerItem, float>(item, "m_activeElapsed");
            float dur = GetPrivateType<PlayerItem, float>(item, "m_activeDuration");

            while (GetPrivateType<PlayerItem, float>(item, "m_activeElapsed") < GetPrivateType<PlayerItem, float>(item, "m_activeDuration") && item.IsCurrentlyActive)
            {
                yield return null;
            }
            SetPrivateType<PlayerItem>(item, "m_isCurrentlyActive", false);
            item.OnActivationStatusChanged?.Invoke(item);
            OnFinish?.Invoke(user);
            yield break;
        }

        private static void SetPrivateType<T>(T obj, string field, bool value)
        {
            FieldInfo f = typeof(T).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            f.SetValue(obj, value);
        }

        private static void SetPrivateType<T>(T obj, string field, float value)
        {
            FieldInfo f = typeof(T).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            f.SetValue(obj, value);
        }

        private static T2 GetPrivateType<T, T2>(T obj, string field)
        {
            FieldInfo f = typeof(T).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            return (T2)f.GetValue(obj);
        }
    }
}
