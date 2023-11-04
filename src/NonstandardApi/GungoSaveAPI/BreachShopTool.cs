namespace SaveAPI
{
    /// <summary>
    /// Class that handles custom item blueprints in breach shops and allows adding custom items to breach shops.
    /// </summary>
    public static class BreachShopTool
    {
        /// <summary>
        /// Adds hooks required for custom item blueprints to work and loads shops and shop item tables
        /// </summary>
        public static void DoSetup()
        {
            if (m_loaded)
            {
                return;
            }
            BaseMetaShopController = SaveTools.LoadAssetFromAnywhere<GameObject>("Foyer_MetaShop").GetComponent<MetaShopController>();
            TrorcMetaShopItems = SaveTools.LoadAssetFromAnywhere<GenericLootTable>("Shop_Truck_Meta");
            GooptonMetaShopItems = SaveTools.LoadAssetFromAnywhere<GenericLootTable>("Shop_Goop_Meta");
            DougMetaShopItems = SaveTools.LoadAssetFromAnywhere<GenericLootTable>("Shop_Beetle_Meta");
            pickupObjectEncounterableHook = new Hook(
                typeof(PickupObject).GetMethod("HandleEncounterable", BindingFlags.NonPublic | BindingFlags.Instance),
                typeof(BreachShopTool).GetMethod("HandleEncounterableHook")
            );
            baseShopSetupHook = new Hook(
                typeof(BaseShopController).GetMethod("DoSetup", BindingFlags.NonPublic | BindingFlags.Instance),
                typeof(BreachShopTool).GetMethod("BaseShopSetupHook")
            );
            metaShopSetupHook = new Hook(
                typeof(MetaShopController).GetMethod("DoSetup", BindingFlags.NonPublic | BindingFlags.Instance),
                typeof(BreachShopTool).GetMethod("MetaSetupHook")
            );
            metaShopCurrentTierHook = new Hook(
                typeof(MetaShopController).GetMethod("GetCurrentTier", BindingFlags.NonPublic | BindingFlags.Instance),
                typeof(BreachShopTool).GetMethod("MetaShopCurrentTierHook")
            );
            metaShopProximateTierHook = new Hook(
                typeof(MetaShopController).GetMethod("GetProximateTier", BindingFlags.NonPublic | BindingFlags.Instance),
                typeof(BreachShopTool).GetMethod("MetaShopProximateTierHook")
            );
            m_loaded = true;
        }

        /// <summary>
        /// Disposes all breach shop hooks, removes all added items from shop tables and nulls shop tables
        /// </summary>
        public static void Unload()
        {
            if (!m_loaded)
            {
                return;
            }
            if(baseShopAddedItems != null)
            {
                for(int i = 0; i < baseShopAddedItems.Keys.Count; i++)
                {
                    WeightedGameObjectCollection collection = baseShopAddedItems.Keys.ToList()[i];
                    if(collection != null && baseShopAddedItems[collection] != null)
                    {
                        for(int j = 0; j < baseShopAddedItems[collection].Count; j++)
                        {
                            WeightedGameObject wgo = baseShopAddedItems[collection][j];
                            if(wgo != null && collection.elements.Contains(wgo))
                            {
                                collection.elements.Remove(wgo);
                            }
                        }
                    }
                }
                baseShopAddedItems.Clear();
                baseShopAddedItems = null;
            }
            if (metaShopAddedTiers != null)
            {
                for(int i = 0; i < metaShopAddedTiers.Count; i++)
                {
                    MetaShopTier tier = metaShopAddedTiers[i];
                    if (tier != null && BaseMetaShopController.metaShopTiers.Contains(tier))
                    {
                        BaseMetaShopController.metaShopTiers.Remove(tier);
                    }
                }
                metaShopAddedTiers.Clear();
                metaShopAddedTiers = null;
            }
            BaseMetaShopController = null;
            TrorcMetaShopItems = null;
            GooptonMetaShopItems = null;
            DougMetaShopItems = null;
            pickupObjectEncounterableHook?.Dispose();
            baseShopSetupHook?.Dispose();
            metaShopSetupHook?.Dispose();
            metaShopCurrentTierHook?.Dispose();
            metaShopProximateTierHook?.Dispose();
            m_loaded = false;
        }

        public static void HandleEncounterableHook(Action<PickupObject, PlayerController> orig, PickupObject po, PlayerController player)
        {
            orig(po, player);
            if (po != null && po.GetComponent<SpecialPickupObject>() != null && po.GetComponent<SpecialPickupObject>().CustomSaveFlagToSetOnAcquisition != CustomDungeonFlags.NONE)
            {
                AdvancedGameStatsManager.Instance.SetFlag(po.GetComponent<SpecialPickupObject>().CustomSaveFlagToSetOnAcquisition, true);
            }
        }

        public static void BaseShopSetupHook(Action<BaseShopController> orig, BaseShopController self)
        {
            orig(self);
            if (self.baseShopType == BaseShopController.AdditionalShopType.FOYER_META && self.ExampleBlueprintPrefab != null)
            {
                List<ShopItemController> shopItems = (List<ShopItemController>)BaseItemControllersInfo.GetValue(self);
                if (shopItems != null)
                {
                    foreach (ShopItemController shopItem in shopItems)
                    {
                        if (shopItem != null && shopItem.item != null && shopItem.item.encounterTrackable != null && shopItem.item.encounterTrackable.journalData != null)
                        {
                            //ETGModConsole.Log("GETTING BLUEPRINT ITEM FOR: " + shopItem.item.name);
                            PickupObject po = GetBlueprintUnlockedItem(shopItem.item.encounterTrackable);
                            //ETGModConsole.Log("BLUEPRINT ISN'T NULL: " + (po != null) + ", ENCOUNTERTRACKABLE ISN'T NULL: " + (po.encounterTrackable != null) + ", PREREQUISITES ISN'T NULL: " + (po.encounterTrackable != null));
                            if (po != null && po.encounterTrackable != null && po.encounterTrackable.prerequisites != null)
                            {
                                //ETGModConsole.Log("GAMEOBJECT NAME: " + po.name);
                                //ETGModConsole.Log("DATABASE OBJECT NAME: " + ETGMod.Databases.Items[po.gameObject.name].gameObject.name);
                               // ETGModConsole.Log("DATABASE OBJECT UNLOCK COUNT: " + ETGMod.Databases.Items[po.gameObject.name].encounterTrackable.prerequisites.Length);

                                CustomDungeonFlags saveFlagToSetOnAcquisition = CustomDungeonFlags.NONE;
                                for (int i = 0; i < po.encounterTrackable.prerequisites.Length; i++)
                                {
                                    //ETGModConsole.Log("LOOKING AT PREREQUISITE #" + i + ": " + po.encounterTrackable.prerequisites[i].GetType());
                                    if (po.encounterTrackable.prerequisites[i] is CustomDungeonPrerequisite && (po.encounterTrackable.prerequisites[i] as CustomDungeonPrerequisite).advancedPrerequisiteType ==
                                        CustomDungeonPrerequisite.AdvancedPrerequisiteType.CUSTOM_FLAG)
                                    {
                                        saveFlagToSetOnAcquisition = (po.encounterTrackable.prerequisites[i] as CustomDungeonPrerequisite).customFlagToCheck;
                                        //ETGModConsole.Log("FOUND FLAG: " + saveFlagToSetOnAcquisition);
                                    }
                                }
                                //ETGModConsole.Log("FINAL VALUE FOR FLAG: " + saveFlagToSetOnAcquisition);
                                if (saveFlagToSetOnAcquisition != CustomDungeonFlags.NONE)
                                {
                                    shopItem.item.gameObject.AddComponent<SpecialPickupObject>().CustomSaveFlagToSetOnAcquisition = saveFlagToSetOnAcquisition;
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void MetaSetupHook(Action<MetaShopController> orig, MetaShopController meta)
        {
            orig(meta);
            List<ShopItemController> shopItems = (List<ShopItemController>)ItemControllersInfo.GetValue(meta);
            if (shopItems != null)
            {
                foreach (ShopItemController shopItem in shopItems)
                {
                    if (shopItem != null && shopItem.item != null && shopItem.item.encounterTrackable != null && shopItem.item.encounterTrackable.journalData != null)
                    {
                        PickupObject po = GetBlueprintUnlockedItem(shopItem.item.encounterTrackable);
                        if (po != null && po.encounterTrackable != null && po.encounterTrackable.prerequisites != null)
                        {
                            CustomDungeonFlags saveFlagToSetOnAcquisition = GetCustomFlagFromTargetItem(po.PickupObjectId);
                            if (saveFlagToSetOnAcquisition != CustomDungeonFlags.NONE)
                            {
                                shopItem.item.gameObject.AddComponent<SpecialPickupObject>().CustomSaveFlagToSetOnAcquisition = saveFlagToSetOnAcquisition;
                                if (AdvancedGameStatsManager.Instance.GetFlag(saveFlagToSetOnAcquisition))
                                {
                                    shopItem.ForceOutOfStock();
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns <see langword="true"/> if the item with id of <paramref name="pickupObjectId"/> doesn't have a prerequisite with the type <see cref="CustomDungeonPrerequisite.AdvancedPrerequisiteType.CUSTOM_FLAG"/> or the value of the prerequisite's flag is 
        /// <see langword="true"/>
        /// </summary>
        /// <param name="pickupObjectId">Id of the item to check</param>
        /// <returns>
        /// <see langword="true"/> if the item with id of <paramref name="pickupObjectId"/> doesn't have a prerequisite with the type <see cref="CustomDungeonPrerequisite.AdvancedPrerequisiteType.CUSTOM_FLAG"/> or the value of the prerequisite's 
        /// flag is <see langword="true"/>
        /// </returns>
        private static bool GetMetaItemUnlockedAdvanced(int pickupObjectId)
        {
            CustomDungeonFlags flag = GetCustomFlagFromTargetItem(pickupObjectId);
            if(flag == CustomDungeonFlags.NONE)
            {
                return true;
            }
            return AdvancedGameStatsManager.Instance.GetFlag(flag);
        }

        public static MetaShopTier MetaShopCurrentTierHook(Func<MetaShopController, MetaShopTier> orig, MetaShopController self)
        {
            MetaShopTier advancedResult = null;
            for (int i = 0; i < self.metaShopTiers.Count; i++)
            {
                if (!GetMetaItemUnlockedAdvanced(self.metaShopTiers[i].itemId1) || !GetMetaItemUnlockedAdvanced(self.metaShopTiers[i].itemId2) || !GetMetaItemUnlockedAdvanced(self.metaShopTiers[i].itemId3))
                {
                    advancedResult = self.metaShopTiers[i];
                    break;
                }
            }
            List<MetaShopTier> origTiers = self.metaShopTiers;
            List<MetaShopTier> tempTiers = new List<MetaShopTier>();
            for (int i = 0; i < origTiers.Count; i++)
            {
                if (origTiers[i] != null)
                {
                    if ((!ItemConditionsFulfilled(origTiers[i].itemId1) || !ItemConditionsFulfilled(origTiers[i].itemId2) || !ItemConditionsFulfilled(origTiers[i].itemId3)) || i == origTiers.Count - 1)
                    {
                        tempTiers.Add(origTiers[i]);
                    }
                }
            }
            self.metaShopTiers = tempTiers;
            MetaShopTier result = orig(self);
            self.metaShopTiers = origTiers;
            if (advancedResult == null)
            {
                return result;
            }
            else if (result == null)
            {
                return advancedResult;
            }
            else
            {
                return self.metaShopTiers.IndexOf(advancedResult) < self.metaShopTiers.IndexOf(result) ? advancedResult : result;
            }
        }

        public static MetaShopTier MetaShopProximateTierHook(Func<MetaShopController, MetaShopTier> orig, MetaShopController self)
        {
            MetaShopTier advancedResult = null;
            for (int i = 0; i < self.metaShopTiers.Count - 1; i++)
            {
                if (!GetMetaItemUnlockedAdvanced(self.metaShopTiers[i].itemId1) || !GetMetaItemUnlockedAdvanced(self.metaShopTiers[i].itemId2) || !GetMetaItemUnlockedAdvanced(self.metaShopTiers[i].itemId3))
                {
                    advancedResult = self.metaShopTiers[i + 1];
                    break;
                }
            }
            List<MetaShopTier> origTiers = self.metaShopTiers;
            List<MetaShopTier> tempTiers = new List<MetaShopTier>();
            for (int i = 0; i < origTiers.Count; i++)
            {
                if (origTiers[i] != null)
                {
                    if (!ItemConditionsFulfilled(origTiers[i].itemId1) || !ItemConditionsFulfilled(origTiers[i].itemId2) || !ItemConditionsFulfilled(origTiers[i].itemId3))
                    {
                        tempTiers.Add(origTiers[i]);
                    }
                }
            }
            self.metaShopTiers = tempTiers;
            MetaShopTier result = orig(self);
            self.metaShopTiers = origTiers;
            if(advancedResult == null)
            {
                return result;
            }
            else if(result == null)
            {
                return advancedResult;
            }
            else
            {
                return self.metaShopTiers.IndexOf(advancedResult) < self.metaShopTiers.IndexOf(result) ? advancedResult : result;
            }
        }

        /// <summary>
        /// Gets the custom flag to check from the last <see cref="CustomDungeonPrerequisite"/> with the type <see cref="CustomDungeonPrerequisite.AdvancedPrerequisiteType.CUSTOM_FLAG"/> of item with <paramref name="shopItemId"/> id
        /// </summary>
        /// <param name="shopItemId">The item's id</param>
        /// <returns>The flag from the last <see cref="CustomDungeonPrerequisite"/> with the type <see cref="CustomDungeonPrerequisite.AdvancedPrerequisiteType.CUSTOM_FLAG"/> of the item or <see cref="CustomDungeonFlags.NONE"/> if it didn't find it</returns>
        public static CustomDungeonFlags GetCustomFlagFromTargetItem(int shopItemId)
        {
            CustomDungeonFlags result = CustomDungeonFlags.NONE;
            PickupObject byId = PickupObjectDatabase.GetById(shopItemId);
            for (int i = 0; i < byId.encounterTrackable.prerequisites.Length; i++)
            {
                if (byId.encounterTrackable.prerequisites[i] is CustomDungeonPrerequisite && (byId.encounterTrackable.prerequisites[i] as CustomDungeonPrerequisite).advancedPrerequisiteType ==
                    CustomDungeonPrerequisite.AdvancedPrerequisiteType.CUSTOM_FLAG)
                {
                    result = (byId.encounterTrackable.prerequisites[i] as CustomDungeonPrerequisite).customFlagToCheck;
                }
            }
            return result;
        }

        public static GungeonFlags GetFlagFromTargetItem(int shopItemId)
        {
            GungeonFlags result = GungeonFlags.NONE;
            PickupObject byId = PickupObjectDatabase.GetById(shopItemId);
            for (int i = 0; i < byId.encounterTrackable.prerequisites.Length; i++)
            {
                if (byId.encounterTrackable.prerequisites[i].prerequisiteType == DungeonPrerequisite.PrerequisiteType.FLAG)
                {
                    result = byId.encounterTrackable.prerequisites[i].saveFlagToCheck;
                }
            }
            return result;
        }

        public static bool ItemConditionsFulfilled(int shopItemId)
        {
            return PickupObjectDatabase.GetById(shopItemId) != null && PickupObjectDatabase.GetById(shopItemId).PrerequisitesMet();
        }

        /// <summary>
        /// Gets the item unlocked by <paramref name="blueprintTrackable"/>
        /// </summary>
        /// <param name="blueprintTrackable">Target blueprint</param>
        /// <returns>The item unlocked by <paramref name="blueprintTrackable"/> or <see langword="null"/> if it didn't find it</returns>
        public static PickupObject GetBlueprintUnlockedItem(EncounterTrackable blueprintTrackable)
        {
            for (int i = 0; i < PickupObjectDatabase.Instance.Objects.Count; i++)
            {
                PickupObject pickupObject = PickupObjectDatabase.Instance.Objects[i];
                if (pickupObject)
                {
                    EncounterTrackable encounterTrackable = pickupObject.encounterTrackable;
                    if (encounterTrackable)
                    {
                        string itemkey = encounterTrackable.journalData.PrimaryDisplayName;
                        if (itemkey.Equals(blueprintTrackable.journalData.PrimaryDisplayName, StringComparison.OrdinalIgnoreCase))
                        {
                            string itemkey2 = encounterTrackable.journalData.NotificationPanelDescription;
                            if (itemkey2.Equals(blueprintTrackable.journalData.NotificationPanelDescription, StringComparison.OrdinalIgnoreCase))
                            {
                                string itemkey3 = encounterTrackable.journalData.AmmonomiconFullEntry;
                                if (itemkey3.Equals(blueprintTrackable.journalData.AmmonomiconFullEntry, StringComparison.OrdinalIgnoreCase))
                                {
                                    string sprite = encounterTrackable.journalData.AmmonomiconSprite;
                                    if (sprite.Equals(blueprintTrackable.journalData.AmmonomiconSprite, StringComparison.OrdinalIgnoreCase))
                                    {
                                        return pickupObject;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }


        /// <summary>
        /// Adds <paramref name="po"/> to Trorc's breach shop. Note that it doesn't automatically lock the item, so you will have to do that manually
        /// </summary>
        /// <param name="po">Item to add</param>
        /// <param name="cost">The item's cost in hegemony credits</param>
        /// <param name="index">The index to InsertOrAdd the item at. If null, defaults to the last index in the shop items list</param>
        /// <returns>The weighted game object that was added</returns>
        public static WeightedGameObject AddItemToTrorcMetaShop(this PickupObject po, int cost, int? index = null)
        {
            if(TrorcMetaShopItems == null)
            {
                DoSetup();
            }
            WeightedGameObject wgo = new WeightedGameObject
            {
                rawGameObject = null,
                pickupId = po.PickupObjectId,
                weight = cost,
                forceDuplicatesPossible = false,
                additionalPrerequisites = new DungeonPrerequisite[0]
            };
            if (index == null)
            {
                TrorcMetaShopItems.defaultItemDrops.elements.Add(wgo);
            }
            else
            {
                if (index.Value < 0)
                {
                    TrorcMetaShopItems.defaultItemDrops.elements.Add(wgo);
                }
                else
                {
                    TrorcMetaShopItems.defaultItemDrops.elements.InsertOrAdd(index.Value, wgo);
                }
            }
            RegisterBaseShopControllerAddedItem(wgo, TrorcMetaShopItems.defaultItemDrops);
            return wgo;
        }

        /// <summary>
        /// Adds <paramref name="po"/> to Goopton's breach shop. Note that it doesn't automatically lock the item, so you will have to do that manually
        /// </summary>
        /// <param name="po">Item to add</param>
        /// <param name="cost">The item's cost in hegemony credits</param>
        /// <param name="index">The index to InsertOrAdd the item at. If null, defaults to the last index in the shop items list</param>
        /// <returns>The weighted game object that was added</returns>
        public static WeightedGameObject AddItemToGooptonMetaShop(this PickupObject po, int cost, int? index = null)
        {
            if (GooptonMetaShopItems == null)
            {
                DoSetup();
            }
            WeightedGameObject wgo = new WeightedGameObject
            {
                rawGameObject = null,
                pickupId = po.PickupObjectId,
                weight = cost,
                forceDuplicatesPossible = false,
                additionalPrerequisites = new DungeonPrerequisite[0]
            };
            if (index == null)
            {
                GooptonMetaShopItems.defaultItemDrops.elements.Add(wgo);
            }
            else
            {
                if (index.Value < 0)
                {
                    TrorcMetaShopItems.defaultItemDrops.elements.Add(wgo);
                }
                else
                {
                    GooptonMetaShopItems.defaultItemDrops.elements.InsertOrAdd(index.Value, wgo);
                }
            }
            RegisterBaseShopControllerAddedItem(wgo, GooptonMetaShopItems.defaultItemDrops);
            return wgo;
        }

        /// <summary>
        /// Adds <paramref name="po"/> to Doug's breach shop. Note that it doesn't automatically lock the item, so you will have to do that manually
        /// </summary>
        /// <param name="po">Item to add</param>
        /// <param name="cost">The item's cost in hegemony credits</param>
        /// <param name="index">The index to InsertOrAdd the item at. If null, defaults to the last index in the shop items list</param>
        /// <returns>The weighted game object that was added</returns>
        public static WeightedGameObject AddItemToDougMetaShop(this PickupObject po, int cost, int? index = null)
        {
            if (DougMetaShopItems == null)
            {
                DoSetup();
            }
            WeightedGameObject wgo = new WeightedGameObject
            {
                rawGameObject = null,
                pickupId = po.PickupObjectId,
                weight = cost,
                forceDuplicatesPossible = false,
                additionalPrerequisites = new DungeonPrerequisite[0]
            };
            if (index == null)
            {
                DougMetaShopItems.defaultItemDrops.elements.Add(wgo);
            }
            else
            {
                if (index.Value < 0)
                {
                    DougMetaShopItems.defaultItemDrops.elements.Add(wgo);
                }
                else
                {
                    DougMetaShopItems.defaultItemDrops.elements.InsertOrAdd(index.Value, wgo);
                }
            }
            RegisterBaseShopControllerAddedItem(wgo, DougMetaShopItems.defaultItemDrops);
            return wgo;
        }

        private static void RegisterBaseShopControllerAddedItem(WeightedGameObject obj, WeightedGameObjectCollection collection)
        {
            if(baseShopAddedItems == null)
            {
                baseShopAddedItems = new Dictionary<WeightedGameObjectCollection, List<WeightedGameObject>>();
            }
            if (!baseShopAddedItems.ContainsKey(collection))
            {
                baseShopAddedItems.Add(collection, new List<WeightedGameObject>());
            }
            if(baseShopAddedItems[collection] == null)
            {
                baseShopAddedItems[collection] = new List<WeightedGameObject>();
            }
            baseShopAddedItems[collection].Add(obj);
        }

        /// <summary>
        /// Adds two new tiers to Ox and Cadence's breach shop
        /// </summary>
        /// <param name="topLeftItemId">The first tier's first item id</param>
        /// <param name="topLeftItemPrice">The first tier's first item price</param>
        /// <param name="topMiddleItemId">The first tier's second item id</param>
        /// <param name="topMiddleItemPrice">The first tier's second item price</param>
        /// <param name="topRightItemId">The first tier's third item id</param>
        /// <param name="topRightItemPrice">The first tier's third item price</param>
        /// <param name="bottomLeftItemId">The second tier's first item id</param>
        /// <param name="bottomLeftItemPrice">The second tier's first item price</param>
        /// <param name="bottomMiddleItemId">The second tier's second item id</param>
        /// <param name="bottomMiddleItemPrice">The second tier's second item price</param>
        /// <param name="bottomRightItemId">The second tier's third item id</param>
        /// <param name="bottomRightItemPrice">The second tier's third item price</param>
        /// <param name="index">The index to InsertOrAdd the tiers at. If null, defaults to the last index in the list</param>
        /// <returns>The tiers that were added</returns>
        public static List<MetaShopTier> AddBaseMetaShopDoubleTier(int topLeftItemId, int topLeftItemPrice, int topMiddleItemId, int topMiddleItemPrice, int topRightItemId, int topRightItemPrice, int bottomLeftItemId, int bottomLeftItemPrice,
            int bottomMiddleItemId, int bottomMiddleItemPrice, int bottomRightItemId, int bottomRightItemPrice, int? index = null)
        {
            return AddBaseMetaShopDoubleTier(new DoubleMetaShopTier(new MetaShopTier()
            {
                itemId1 = topLeftItemId,
                overrideItem1Cost = topLeftItemPrice,
                itemId2 = topMiddleItemId,
                overrideItem2Cost = topMiddleItemPrice,
                itemId3 = topRightItemId,
                overrideItem3Cost = topRightItemPrice,
                overrideTierCost =
                topLeftItemId
            }, new MetaShopTier
            {
                itemId1 = bottomLeftItemId,
                overrideItem1Cost = bottomLeftItemPrice,
                itemId2 = bottomMiddleItemId,
                overrideItem2Cost = bottomMiddleItemPrice,
                itemId3 = bottomRightItemId,
                overrideItem3Cost = bottomRightItemPrice,
                overrideTierCost =
                topLeftItemId
            }), index);
        }

        /// <summary>
        /// Adds two new tiers to Ox and Cadence's breach shop
        /// </summary>
        /// <param name="topLeftItemId">The first tier's first item id</param>
        /// <param name="topLeftItemPrice">The first tier's first item price</param>
        /// <param name="topMiddleItemId">The first tier's second item id</param>
        /// <param name="topMiddleItemPrice">The first tier's second item price</param>
        /// <param name="topRightItemId">The first tier's third item id</param>
        /// <param name="topRightItemPrice">The first tier's third item price</param>
        /// <param name="bottomLeftItemId">The second tier's first item id</param>
        /// <param name="bottomLeftItemPrice">The second tier's first item price</param>
        /// <param name="bottomMiddleItemId">The second tier's second item id</param>
        /// <param name="bottomMiddleItemPrice">The second tier's second item price</param>
        /// <param name="index">The index to InsertOrAdd the tiers at. If null, defaults to the last index in the list</param>
        /// <returns>The tiers that were added</returns>
        public static List<MetaShopTier> AddBaseMetaShopDoubleTier(int topLeftItemId, int topLeftItemPrice, int topMiddleItemId, int topMiddleItemPrice, int topRightItemId, int topRightItemPrice, int bottomLeftItemId, int bottomLeftItemPrice,
            int bottomMiddleItemId, int bottomMiddleItemPrice, int? index = null)
        {
            return AddBaseMetaShopDoubleTier(new DoubleMetaShopTier(new MetaShopTier()
            {
                itemId1 = topLeftItemId,
                overrideItem1Cost = topLeftItemPrice,
                itemId2 = topMiddleItemId,
                overrideItem2Cost = topMiddleItemPrice,
                itemId3 = topRightItemId,
                overrideItem3Cost = topRightItemPrice,
                overrideTierCost =
                topLeftItemId
            }, new MetaShopTier
            {
                itemId1 = bottomLeftItemId,
                overrideItem1Cost = bottomLeftItemPrice,
                itemId2 = bottomMiddleItemId,
                overrideItem2Cost = bottomMiddleItemPrice,
                itemId3 = -1,
                overrideItem3Cost = -1,
                overrideTierCost =
                topLeftItemId
            }), index);
        }

        /// <summary>
        /// Adds two new tiers to Ox and Cadence's breach shop
        /// </summary>
        /// <param name="topLeftItemId">The first tier's first item id</param>
        /// <param name="topLeftItemPrice">The first tier's first item price</param>
        /// <param name="topMiddleItemId">The first tier's second item id</param>
        /// <param name="topMiddleItemPrice">The first tier's second item price</param>
        /// <param name="topRightItemId">The first tier's third item id</param>
        /// <param name="topRightItemPrice">The first tier's third item price</param>
        /// <param name="bottomLeftItemId">The second tier's first item id</param>
        /// <param name="bottomLeftItemPrice">The second tier's first item price</param>
        /// <param name="index">The index to InsertOrAdd the tiers at. If null, defaults to the last index in the list</param>
        /// <returns>The tiers that were added</returns>
        public static List<MetaShopTier> AddBaseMetaShopDoubleTier(int topLeftItemId, int topLeftItemPrice, int topMiddleItemId, int topMiddleItemPrice, int topRightItemId, int topRightItemPrice, int bottomLeftItemId, int bottomLeftItemPrice, int? index = null)
        {
            return AddBaseMetaShopDoubleTier(new DoubleMetaShopTier(new MetaShopTier()
            {
                itemId1 = topLeftItemId,
                overrideItem1Cost = topLeftItemPrice,
                itemId2 = topMiddleItemId,
                overrideItem2Cost = topMiddleItemPrice,
                itemId3 = topRightItemId,
                overrideItem3Cost = topRightItemPrice,
                overrideTierCost =
                topLeftItemId
            }, new MetaShopTier
            {
                itemId1 = bottomLeftItemId,
                overrideItem1Cost = bottomLeftItemPrice,
                itemId2 = -1,
                overrideItem2Cost = -1,
                itemId3 = -1,
                overrideItem3Cost = -1,
                overrideTierCost =
                topLeftItemId
            }), index);
        }

        /// <summary>
        /// Adds a new tier to Ox and Cadence's breach shop
        /// </summary>
        /// <param name="leftItemId">The first tier's first item id</param>
        /// <param name="leftItemPrice">The first tier's first item price</param>
        /// <param name="middleItemId">The first tier's second item id</param>
        /// <param name="middleItemPrice">The first tier's second item price</param>
        /// <param name="rightItemId">The first tier's third item id</param>
        /// <param name="rightItemPrice">The first tier's third item price</param>
        /// <param name="index">The index to InsertOrAdd the tier at. If null, defaults to the last index in the list</param>
        /// <returns>The tier that was added</returns>
        public static MetaShopTier AddBaseMetaShopTier(int leftItemId, int leftItemPrice, int middleItemId, int middleItemPrice, int rightItemId, int rightItemPrice, int? index = null)
        {
            return AddBaseMetaShopTier(new MetaShopTier()
            {
                itemId1 = leftItemId,
                overrideItem1Cost = leftItemPrice,
                itemId2 = middleItemId,
                overrideItem2Cost = middleItemPrice,
                itemId3 = rightItemId,
                overrideItem3Cost = rightItemPrice,
                overrideTierCost =
                leftItemPrice
            }, index);
        }

        /// <summary>
        /// Adds a new tier to Ox and Cadence's breach shop
        /// </summary>
        /// <param name="leftItemId">The first tier's first item id</param>
        /// <param name="leftItemPrice">The first tier's first item price</param>
        /// <param name="middleItemId">The first tier's second item id</param>
        /// <param name="middleItemPrice">The first tier's second item price</param>
        /// <param name="index">The index to InsertOrAdd the tier at. If null, defaults to the last index in the list</param>
        /// <returns>The tier that was added</returns>
        public static MetaShopTier AddBaseMetaShopTier(int leftItemId, int leftItemPrice, int middleItemId, int middleItemPrice, int? index = null)
        {
            return AddBaseMetaShopTier(new MetaShopTier()
            {
                itemId1 = leftItemId,
                overrideItem1Cost = leftItemPrice,
                itemId2 = middleItemId,
                overrideItem2Cost = middleItemPrice,
                itemId3 = -1,
                overrideItem3Cost = -1,
                overrideTierCost =
                leftItemPrice
            }, index);
        }

        /// <summary>
        /// Adds a new tier to Ox and Cadence's breach shop
        /// </summary>
        /// <param name="leftItemId">The first tier's first item id</param>
        /// <param name="leftItemPrice">The first tier's first item price</param>
        /// <param name="index">The index to InsertOrAdd the tier at. If null, defaults to the last index in the list</param>
        /// <returns>The tier that was added</returns>
        public static MetaShopTier AddBaseMetaShopTier(int leftItemId, int leftItemPrice, int? index = null)
        {
            return AddBaseMetaShopTier(new MetaShopTier()
            {
                itemId1 = leftItemId,
                overrideItem1Cost = leftItemPrice,
                itemId2 = -1,
                overrideItem2Cost = -1,
                itemId3 = -1,
                overrideItem3Cost = -1,
                overrideTierCost =
                leftItemPrice
            }, index);
        }

        /// <summary>
        /// Adds two new tier to Ox and Cadence's breach shop
        /// </summary>
        /// <param name="tier">Container with the tiers</param>
        /// <param name="index">The index to InsertOrAdd the tiers at. If null, defaults to the last index in the list</param>
        /// <returns>The tiers that were added</returns>
        public static List<MetaShopTier> AddBaseMetaShopDoubleTier(DoubleMetaShopTier tier, int? index = null)
        {
            return new List<MetaShopTier> {
                AddBaseMetaShopTier(tier.GetBottomTier(), index),
                AddBaseMetaShopTier(tier.GetTopTier(), index)
            };
        }

        /// <summary>
        /// Adds a new tier to Ox and Cadence's breach shop
        /// </summary>
        /// <param name="tier">Tier to add</param>
        /// <param name="index">The index to InsertOrAdd the tier at. If null, defaults to the last index in the list</param>
        /// <returns><paramref name="tier"/></returns>
        public static MetaShopTier AddBaseMetaShopTier(MetaShopTier tier, int? index = null)
        {
            if (BaseMetaShopController == null)
            {
                DoSetup();
            }
            if (index == null)
            {
                BaseMetaShopController.metaShopTiers.Add(tier);
            }
            else
            {
                if (index.Value < 0)
                {
                    BaseMetaShopController.metaShopTiers.Add(tier);
                }
                else
                {
                    BaseMetaShopController.metaShopTiers.InsertOrAdd(index.Value, tier);
                }
            }
            if(metaShopAddedTiers == null)
            {
                metaShopAddedTiers = new List<MetaShopTier>();
            }
            metaShopAddedTiers.Add(tier);
            ReloadInstanceMetaShopTiers();
            return tier;
        }

        /// <summary>
        /// Reloads the shop tiers of all instance meta shop controllers
        /// </summary>
        public static void ReloadInstanceMetaShopTiers()
        {
            foreach(MetaShopController meta in UnityEngine.Object.FindObjectsOfType<MetaShopController>())
            {
                meta.metaShopTiers = SaveTools.CloneList(BaseMetaShopController.metaShopTiers);
            }
        }

        public static MetaShopController BaseMetaShopController;
        public static GenericLootTable TrorcMetaShopItems;
        public static GenericLootTable GooptonMetaShopItems;
        public static GenericLootTable DougMetaShopItems;
        private static FieldInfo ItemControllersInfo = typeof(ShopController).GetField("m_itemControllers", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo BaseItemControllersInfo = typeof(BaseShopController).GetField("m_itemControllers", BindingFlags.NonPublic | BindingFlags.Instance);
        private static Hook pickupObjectEncounterableHook;
        private static Hook baseShopSetupHook;
        private static Hook metaShopSetupHook;
        private static Hook metaShopCurrentTierHook;
        private static Hook metaShopProximateTierHook;
        public static Dictionary<WeightedGameObjectCollection, List<WeightedGameObject>> baseShopAddedItems;
        public static List<MetaShopTier> metaShopAddedTiers;
        private static bool m_loaded;
        /// <summary>
        /// Container that can contain 2 <see cref="MetaShopTier"/>s
        /// </summary>
        public class DoubleMetaShopTier
        {
            /// <summary>
            /// Creates a new <see cref="DoubleMetaShopTier"/> and assigns the first tier to <paramref name="topTier"/> and the second tier to <paramref name="bottomTier"/>
            /// </summary>
            /// <param name="topTier"></param>
            /// <param name="bottomTier"></param>
            public DoubleMetaShopTier(MetaShopTier topTier, MetaShopTier bottomTier)
            {
                this.m_topTier = topTier;
                this.m_bottomTier = bottomTier;
            }

            /// <summary>
            /// Creates a new <see cref="DoubleMetaShopTier"/> and assigns the first tier to <paramref name="other"/>'s first tier and the second tier to <paramref name="other"/>'s second tier
            /// </summary>
            /// <param name="other"></param>
            public DoubleMetaShopTier(DoubleMetaShopTier other)
            {
                this.m_topTier = other.m_topTier;
                this.m_bottomTier = other.m_bottomTier;
            }

            /// <summary>
            /// Gets the first tier
            /// </summary>
            /// <returns>The first tier</returns>
            public MetaShopTier GetTopTier()
            {
                return this.m_topTier;
            }

            /// <summary>
            /// Gets the second tier
            /// </summary>
            /// <returns>The second tier</returns>
            public MetaShopTier GetBottomTier()
            {
                return this.m_topTier;
            }

            /// <summary>
            /// Creates a list with both tiers in this container
            /// </summary>
            /// <returns></returns>
            public List<MetaShopTier> GetTierList()
            {
                return new List<MetaShopTier>
                {
                    this.m_topTier,
                    this.m_bottomTier
                };
            }

            private MetaShopTier m_topTier;
            private MetaShopTier m_bottomTier;
        }
    }
}
