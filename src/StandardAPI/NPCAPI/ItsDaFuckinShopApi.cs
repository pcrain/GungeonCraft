using Dungeonator;
using GungeonAPI;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using ItemAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using static NpcApi.CustomShopController;

namespace NpcApi
{
    static class ItsDaFuckinShopApi
    {

        public static Dictionary<string, GameObject> builtShops = new Dictionary<string, GameObject>();

        public static Vector3[] defaultItemPositions = new Vector3[] { new Vector3(1.125f, 2.125f, 1), new Vector3(2.625f, 1f, 1), new Vector3(4.125f, 2.125f, 1) };
        public static Vector3 defaultTalkPointOffset = new Vector3(0.8125f, 2.1875f, -1.31f);

        /// <summary>
        /// Creates a shop object along with an npc
        /// </summary>
        /// <param name="name">Name of the npc</param> 
        /// <param name="prefix">Mod prefix (for example Bot)</param> 
        /// 
        /// <param name="idleSpritePaths">List of *FULL* sprite paths for the idle animation</param> 
        /// <param name="idleFps">Fps of the idle animation (base game tends to use around 6)</param> 
        /// 
        /// <param name="talkSpritePaths">List of *FULL* sprite paths for the talk animation</param> 
        /// <param name="talkFps">Fps of the talk animation (base game tends to use around 8)</param> 
        /// 
        /// <param name="lootTable">Shop loot table</param> 
        /// <param name="currency">What is used to buy items at the shop</param> 
        /// 
        /// <param name="runBasedMultilineGenericStringKey">String key for normal convos</param> 
        /// <param name="runBasedMultilineStopperStringKey">String key for if you try talking to an npc to much</param> 
        /// <param name="purchaseItemStringKey">String key for when the player buys something</param> 
        /// <param name="purchaseItemFailedStringKey">String key for when the player tries but fails to buy something</param> 
        /// <param name="introStringKey">String key for when the player enters the room</param> 
        /// <param name="attackedStringKey">String key for when the player shoots at the npc</param> 
        /// <param name="costModifier">The multiplier for shop prices</param> 
        /// <param name="itemPositions">The offset for the item(s) sold by your npc, the amount of items sold is based off how many offsets you add here (if you just want the 3 normally items spots you can use ItsDaFuckinShopApi.defaultItemPositions)</param> 
        /// <param name="giveStatsOnPurchase">Whether the shop modifies stats after the player buys an item for example how cursula gives curse</param> 
        /// <param name="statsToGiveOnPurchase"> The stats given when the player buys an item (will be ingored if statsToGiveOnPurchase is false)</param> 
        /// 
        /// <param name="CustomCanBuy">The method that gets called to check if the player can buy an item (useless if currency isnt set to CUSTOM)</param> 
        /// <param name="CustomRemoveCurrency">The method that gets called remove currency from the player (useless if currency isnt set to CUSTOM)</param> 
        /// <param name="CustomPrice">The method that gets called to get the price of an item (useless if currency isnt set to CUSTOM)</param> 
        /// 
        /// <param name="currencyIconPath">Sprite path for your custom currency sprite</param> 
        /// <param name="currencyName">The name you want your custom currecy sprite to have (i should probably remove this...)</param> 
        ///
        /// <param name="hasCarpet">Whether the shop has a carpet or something else that they sit on</param> 
        /// <param name="carpetSpritePath">Sprite path for the carpet or whatever</param> 
        ///         
        /// <param name="hasMinimapIcon">Whether the shop has a minimap icon to show what room theyre in</param> 
        /// <param name="minimapIconSpritePath">Sprite path minimap icon leave blank to just use deafult smiley face</param> 
        /// 
        /// <param name="addToMainNpcPool">Whether the shop should be added to the pool of npcs that show up in the main shop a long side bello</param> 
        /// <param name="percentChanceForMainPool">How likely it is for the shop to show up in the main pool base game shops use 0.1</param> 
        /// 
        /// <param name="prerequisites">These do unlocks and shit</param> 
        /// <returns></returns>
        public static GameObject SetUpShop(string name, string prefix, List<string> idleSpritePaths, int idleFps, List<string> talkSpritePaths, int talkFps, GenericLootTable lootTable, CustomShopItemController.ShopCurrencyType currency, string runBasedMultilineGenericStringKey,
            string runBasedMultilineStopperStringKey, string purchaseItemStringKey, string purchaseItemFailedStringKey, string introStringKey, string attackedStringKey, Vector3 talkPointOffset, Vector3[] itemPositions = null, float costModifier = 1, bool giveStatsOnPurchase = false,
            StatModifier[] statsToGiveOnPurchase = null, Func<CustomShopController, PlayerController, int, bool> CustomCanBuy = null, Func<CustomShopController, PlayerController, int, int> CustomRemoveCurrency = null, Func<CustomShopController, CustomShopItemController, PickupObject, int> CustomPrice = null,
            Func<PlayerController, PickupObject, int, bool> OnPurchase = null, Func<PlayerController, PickupObject, int, bool> OnSteal = null, string currencyIconPath = "", string currencyName = "", bool canBeRobbed = true, bool hasCarpet = false, string carpetSpritePath = "", bool hasMinimapIcon = false,
            string minimapIconSpritePath = "", bool addToMainNpcPool = false, float percentChanceForMainPool = 0.1f, DungeonPrerequisite[] prerequisites = null)
        {

            try
            {

                if (prerequisites == null)
                {
                    prerequisites = new DungeonPrerequisite[0];
                }
                //bool isBreachShop = false;
                Vector3 breachPos = Vector3.zero;

                var shared_auto_001 = ResourceManager.LoadAssetBundle("shared_auto_001");
                var shared_auto_002 = ResourceManager.LoadAssetBundle("shared_auto_002");
                var SpeechPoint = new GameObject("SpeechPoint");
                SpeechPoint.transform.position = talkPointOffset;



                var npcObj = SpriteBuilder.SpriteFromResource(idleSpritePaths[0], new GameObject(prefix + ":" + name));

                FakePrefab.MarkAsFakePrefab(npcObj);
                UnityEngine.Object.DontDestroyOnLoad(npcObj);
                npcObj.SetActive(false);

                npcObj.layer = 22;

                var collection = npcObj.GetComponent<tk2dSprite>().Collection;
                SpeechPoint.transform.parent = npcObj.transform;

                FakePrefab.MarkAsFakePrefab(SpeechPoint);
                UnityEngine.Object.DontDestroyOnLoad(SpeechPoint);
                SpeechPoint.SetActive(true);


                var idleIdsList = new List<int>();
                var talkIdsList = new List<int>();

                foreach (string sprite in idleSpritePaths)
                {
                    idleIdsList.Add(SpriteBuilder.AddSpriteToCollection(sprite, collection));
                }

                foreach (string sprite in talkSpritePaths)
                {
                    talkIdsList.Add(SpriteBuilder.AddSpriteToCollection(sprite, collection));
                }

                tk2dSpriteAnimator spriteAnimator = npcObj.AddComponent<tk2dSpriteAnimator>();

                SpriteBuilder.AddAnimation(spriteAnimator, collection, idleIdsList, name + "_idle", tk2dSpriteAnimationClip.WrapMode.Loop, idleFps);
                SpriteBuilder.AddAnimation(spriteAnimator, collection, talkIdsList, name + "_talk", tk2dSpriteAnimationClip.WrapMode.Loop, talkFps);

                SpeculativeRigidbody rigidbody = GenerateOrAddToRigidBody(npcObj, CollisionLayer.BulletBlocker, PixelCollider.PixelColliderGeneration.Manual, true, true, true, false, false, false, false, true, new IntVector2(20, 18), new IntVector2(5, 0));

                TalkDoerLite talkDoer = npcObj.AddComponent<TalkDoerLite>();

                talkDoer.placeableWidth = 4;
                talkDoer.placeableHeight = 3;
                talkDoer.difficulty = 0;
                talkDoer.isPassable = true;
                talkDoer.usesOverrideInteractionRegion = false;
                talkDoer.overrideRegionOffset = Vector2.zero;
                talkDoer.overrideRegionDimensions = Vector2.zero;
                talkDoer.overrideInteractionRadius = -1;
                talkDoer.PreventInteraction = false;
                talkDoer.AllowPlayerToPassEventually = true;
                talkDoer.speakPoint = SpeechPoint.transform;
                talkDoer.SpeaksGleepGlorpenese = false;
                talkDoer.audioCharacterSpeechTag = "oldman";
                talkDoer.playerApproachRadius = 5;
                talkDoer.conversationBreakRadius = 5;
                talkDoer.echo1 = null;
                talkDoer.echo2 = null;
                talkDoer.PreventCoopInteraction = false;
                talkDoer.IsPaletteSwapped = false;
                talkDoer.PaletteTexture = null;
                talkDoer.OutlineDepth = 0.5f;
                talkDoer.OutlineLuminanceCutoff = 0.05f;
                talkDoer.MovementSpeed = 3;
                talkDoer.PathableTiles = CellTypes.FLOOR;

                UltraFortunesFavor dreamLuck = npcObj.AddComponent<UltraFortunesFavor>();

                dreamLuck.goopRadius = 2;
                dreamLuck.beamRadius = 2;
                dreamLuck.bulletRadius = 2;
                dreamLuck.bulletSpeedModifier = 0.8f;

                dreamLuck.vfxOffset = 0.625f;
                dreamLuck.sparkOctantVFX = shared_auto_001.LoadAsset<GameObject>("FortuneFavor_VFX_Spark");


                AIAnimator aIAnimator = GenerateBlankAIAnimator(npcObj);
                aIAnimator.spriteAnimator = spriteAnimator;
                aIAnimator.IdleAnimation = new DirectionalAnimation
                {
                    Type = DirectionalAnimation.DirectionType.Single,
                    Prefix = name + "_idle",
                    AnimNames = new string[]
                    {
                        ""
                    },
                    Flipped = new DirectionalAnimation.FlipType[]
                    {
                        DirectionalAnimation.FlipType.None
                    }

                };

                aIAnimator.TalkAnimation = new DirectionalAnimation
                {
                    Type = DirectionalAnimation.DirectionType.Single,
                    Prefix = name + "_talk",
                    AnimNames = new string[]
                    {
                        ""
                    },
                    Flipped = new DirectionalAnimation.FlipType[]
                    {
                        DirectionalAnimation.FlipType.None
                    }
                };

                var basenpc = ResourceManager.LoadAssetBundle("shared_auto_001").LoadAsset<GameObject>("Merchant_Key").transform.Find("NPC_Key").gameObject;

                PlayMakerFSM iHaveNoFuckingClueWhatThisIs = npcObj.AddComponent<PlayMakerFSM>();

                UnityEngine.JsonUtility.FromJsonOverwrite(UnityEngine.JsonUtility.ToJson(basenpc.GetComponent<PlayMakerFSM>()), iHaveNoFuckingClueWhatThisIs);

                FieldInfo fsmStringParams = typeof(ActionData).GetField("fsmStringParams", BindingFlags.NonPublic | BindingFlags.Instance);

                (fsmStringParams.GetValue(iHaveNoFuckingClueWhatThisIs.FsmStates[1].ActionData) as List<FsmString>)[0].Value = runBasedMultilineGenericStringKey;
                (fsmStringParams.GetValue(iHaveNoFuckingClueWhatThisIs.FsmStates[1].ActionData) as List<FsmString>)[1].Value = runBasedMultilineStopperStringKey;

                (fsmStringParams.GetValue(iHaveNoFuckingClueWhatThisIs.FsmStates[4].ActionData) as List<FsmString>)[0].Value = purchaseItemStringKey;

                (fsmStringParams.GetValue(iHaveNoFuckingClueWhatThisIs.FsmStates[5].ActionData) as List<FsmString>)[0].Value = purchaseItemFailedStringKey;

                (fsmStringParams.GetValue(iHaveNoFuckingClueWhatThisIs.FsmStates[7].ActionData) as List<FsmString>)[0].Value = introStringKey;

                (fsmStringParams.GetValue(iHaveNoFuckingClueWhatThisIs.FsmStates[8].ActionData) as List<FsmString>)[0].Value = attackedStringKey;

                (fsmStringParams.GetValue(iHaveNoFuckingClueWhatThisIs.FsmStates[9].ActionData) as List<FsmString>)[0].Value = "#SUBSHOP_GENERIC_CAUGHT_STEALING";

                (fsmStringParams.GetValue(iHaveNoFuckingClueWhatThisIs.FsmStates[10].ActionData) as List<FsmString>)[0].Value = "#SHOP_GENERIC_NO_SALE_LABEL";

                (fsmStringParams.GetValue(iHaveNoFuckingClueWhatThisIs.FsmStates[12].ActionData) as List<FsmString>)[0].Value = "#COOP_REBUKE";


                npcObj.name = prefix + ":" + name;

                var posList = new List<Transform>();
                for (int i = 0; i < itemPositions.Length; i++)
                {

                    var ItemPoint = new GameObject("ItemPoint" + i);
                    ItemPoint.transform.position = itemPositions[i];
                    FakePrefab.MarkAsFakePrefab(ItemPoint);
                    UnityEngine.Object.DontDestroyOnLoad(ItemPoint);
                    ItemPoint.SetActive(true);
                    posList.Add(ItemPoint.transform);
                }

                var ItemPoint1 = new GameObject("ItemPoint1");
                ItemPoint1.transform.position = new Vector3(1.125f, 2.125f, 1);
                FakePrefab.MarkAsFakePrefab(ItemPoint1);
                UnityEngine.Object.DontDestroyOnLoad(ItemPoint1);
                ItemPoint1.SetActive(true);
                var ItemPoint2 = new GameObject("ItemPoint2");
                ItemPoint2.transform.position = new Vector3(2.625f, 1f, 1);
                FakePrefab.MarkAsFakePrefab(ItemPoint2);
                UnityEngine.Object.DontDestroyOnLoad(ItemPoint2);
                ItemPoint2.SetActive(true);
                var ItemPoint3 = new GameObject("ItemPoint3");
                ItemPoint3.transform.position = new Vector3(4.125f, 2.125f, 1);
                FakePrefab.MarkAsFakePrefab(ItemPoint3);
                UnityEngine.Object.DontDestroyOnLoad(ItemPoint3);
                ItemPoint3.SetActive(true);


                var shopObj = new GameObject(prefix + ":" + name + "_Shop").AddComponent<CustomShopController>();
                FakePrefab.MarkAsFakePrefab(shopObj.gameObject);
                UnityEngine.Object.DontDestroyOnLoad(shopObj.gameObject);

                shopObj.gameObject.SetActive(false);

                shopObj.currencyType = currency;

                shopObj.ActionAndFuncSetUp(CustomCanBuy, CustomRemoveCurrency, CustomPrice, OnPurchase, OnSteal);

                if (!string.IsNullOrEmpty(currencyIconPath))
                {
                    shopObj.customPriceSprite = AddCustomCurrencyType(currencyIconPath, $"{prefix}:{currencyName}");
                }
                else
                {
                    shopObj.customPriceSprite = currencyName;
                }


                //GungeonAPI.ToolsGAPI.AddNewItemToAtlas()

                shopObj.canBeRobbed = canBeRobbed;

                shopObj.placeableHeight = 5;
                shopObj.placeableWidth = 5;
                shopObj.difficulty = 0;
                shopObj.isPassable = true;
                shopObj.baseShopType = BaseShopController.AdditionalShopType.TRUCK;//shopType;

                shopObj.FoyerMetaShopForcedTiers = false;
                shopObj.IsBeetleMerchant = false;
                shopObj.ExampleBlueprintPrefab = null;
                shopObj.shopItems = lootTable;
                shopObj.spawnPositions = posList.ToArray();//{ ItemPoint1.transform, ItemPoint2.transform, ItemPoint3.transform };

                foreach (var pos in shopObj.spawnPositions)
                {
                    pos.parent = shopObj.gameObject.transform;
                }

                shopObj.shopItemsGroup2 = null;
                shopObj.spawnPositionsGroup2 = null;
                shopObj.spawnGroupTwoItem1Chance = 0.5f;
                shopObj.spawnGroupTwoItem2Chance = 0.5f;
                shopObj.spawnGroupTwoItem3Chance = 0.5f;
                shopObj.shopkeepFSM = npcObj.GetComponent<PlayMakerFSM>();
                shopObj.shopItemShadowPrefab = shared_auto_001.LoadAsset<GameObject>("Merchant_Key").GetComponent<BaseShopController>().shopItemShadowPrefab;

                shopObj.prerequisites = prerequisites;
                //shopObj.shopItemShadowPrefab = 

                shopObj.cat = null;


                if (hasMinimapIcon)
                {
                    if (!string.IsNullOrEmpty(minimapIconSpritePath))
                    {
                        shopObj.OptionalMinimapIcon = SpriteBuilder.SpriteFromResource(minimapIconSpritePath);
                        UnityEngine.Object.DontDestroyOnLoad(shopObj.OptionalMinimapIcon);
                        FakePrefab.MarkAsFakePrefab(shopObj.OptionalMinimapIcon);
                    }
                    else
                    {
                        shopObj.OptionalMinimapIcon = ResourceCache.Acquire("Global Prefabs/Minimap_NPC_Icon") as GameObject;
                    }
                }

                shopObj.ShopCostModifier = costModifier;
                shopObj.FlagToSetOnEncounter = GungeonFlags.NONE;

                shopObj.giveStatsOnPurchase = giveStatsOnPurchase;
                shopObj.statsToGive = statsToGiveOnPurchase;

                //shopObj.

                /*if (isBreachShop)
                {
                    shopObj.gameObject.AddComponent<BreachShopComp>().offset = breachPos;
                    BreachShopTools.registeredShops.Add(prefix + ":" + name, shopObj.gameObject);

                    shopObj.FoyerMetaShopForcedTiers = true;

                    var exampleBlueprintObj = SpriteBuilder.SpriteFromResource(carpetSpritePath, new GameObject(prefix + ":" + name + "_ExampleBlueprintPrefab"));
                    exampleBlueprintObj.GetComponent<tk2dSprite>().SortingOrder = 2;
                    FakePrefab.MarkAsFakePrefab(exampleBlueprintObj);
                    UnityEngine.Object.DontDestroyOnLoad(exampleBlueprintObj);
                    exampleBlueprintObj.SetActive(false);

                    //var item = exampleBlueprintObj.AddComponent<ItemBlueprintItem>();
                    //item.quality = PickupObject.ItemQuality.SPECIAL;
                    //item.PickupObjectId = 99999999;
                    


                    shopObj.ExampleBlueprintPrefab = shared_auto_001.LoadAsset<GameObject>("NPC_Beetle_Merchant_Foyer").GetComponent<BaseShopController>().ExampleBlueprintPrefab;
                }*/

                npcObj.transform.parent = shopObj.gameObject.transform;
                npcObj.transform.position = new Vector3(1.9375f, 3.4375f, 5.9375f);




                if (hasCarpet)
                {
                    var carpetObj = SpriteBuilder.SpriteFromResource(carpetSpritePath, new GameObject(prefix + ":" + name + "_Carpet"));
                    carpetObj.GetComponent<tk2dSprite>().SortingOrder = 2;
                    FakePrefab.MarkAsFakePrefab(carpetObj);
                    UnityEngine.Object.DontDestroyOnLoad(carpetObj);
                    carpetObj.SetActive(true);

                    carpetObj.transform.position = new Vector3(0, 0, 1.7f);
                    carpetObj.transform.parent = shopObj.gameObject.transform;
                    carpetObj.layer = 20;
                }
                npcObj.SetActive(true);

                if (addToMainNpcPool)
                {
                    shared_auto_002.LoadAsset<DungeonPlaceable>("shopannex_contents_01").variantTiers.Add(new DungeonPlaceableVariant
                    {
                        percentChance = percentChanceForMainPool,
                        unitOffset = new Vector2(-0.5f, -1.25f),
                        nonDatabasePlaceable = shopObj.gameObject,
                        enemyPlaceableGuid = "",
                        pickupObjectPlaceableId = -1,
                        forceBlackPhantom = false,
                        addDebrisObject = false,
                        prerequisites = prerequisites, //shit for unlocks gose here sooner or later
                        materialRequirements = new DungeonPlaceableRoomMaterialRequirement[0],

                    });
                }

                ItsDaFuckinShopApi.builtShops.Add(prefix + ":" + name, shopObj.gameObject);
                return shopObj.gameObject;
            }
            catch (Exception message)
            {
                ETGModConsole.Log(message.ToString());
                return null;
            }
        }

        public static GameObject SetUpGenericNPCObject(string name, string prefix, List<string> idleSpritePaths, int idleFps, List<string> talkSpritePaths, int talkFps, string runBasedMultilineGenericStringKey,
            string runBasedMultilineStopperStringKey, string purchaseItemStringKey, string purchaseItemFailedStringKey, string introStringKey, string attackedStringKey, Vector3 talkPointOffset)
        {

            try
            {
                Vector3 breachPos = Vector3.zero;

                var shared_auto_001 = ResourceManager.LoadAssetBundle("shared_auto_001");
                var shared_auto_002 = ResourceManager.LoadAssetBundle("shared_auto_002");
                var SpeechPoint = new GameObject("SpeechPoint");
                SpeechPoint.transform.position = talkPointOffset;

                var npcObj = SpriteBuilder.SpriteFromResource(idleSpritePaths[0], new GameObject(prefix + ":" + name));

                FakePrefab.MarkAsFakePrefab(npcObj);
                UnityEngine.Object.DontDestroyOnLoad(npcObj);
                npcObj.SetActive(false);

                npcObj.layer = 22;

                var collection = npcObj.GetComponent<tk2dSprite>().Collection;
                SpeechPoint.transform.parent = npcObj.transform;

                FakePrefab.MarkAsFakePrefab(SpeechPoint);
                UnityEngine.Object.DontDestroyOnLoad(SpeechPoint);
                SpeechPoint.SetActive(true);

                var idleIdsList = new List<int>();
                var talkIdsList = new List<int>();

                foreach (string sprite in idleSpritePaths)
                {
                    idleIdsList.Add(SpriteBuilder.AddSpriteToCollection(sprite, collection));
                }

                foreach (string sprite in talkSpritePaths)
                {
                    talkIdsList.Add(SpriteBuilder.AddSpriteToCollection(sprite, collection));
                }

                tk2dSpriteAnimator spriteAnimator = npcObj.AddComponent<tk2dSpriteAnimator>();

                SpriteBuilder.AddAnimation(spriteAnimator, collection, idleIdsList, name + "_idle", tk2dSpriteAnimationClip.WrapMode.Loop, idleFps);
                SpriteBuilder.AddAnimation(spriteAnimator, collection, talkIdsList, name + "_talk", tk2dSpriteAnimationClip.WrapMode.Loop, talkFps);

                SpeculativeRigidbody rigidbody = GenerateOrAddToRigidBody(npcObj, CollisionLayer.BulletBlocker, PixelCollider.PixelColliderGeneration.Manual, true, true, true, false, false, false, false, true, new IntVector2(20, 18), new IntVector2(5, 0));

                TalkDoerLite talkDoer = npcObj.AddComponent<TalkDoerLite>();
                    talkDoer.placeableWidth = 4;
                    talkDoer.placeableHeight = 3;
                    talkDoer.difficulty = 0;
                    talkDoer.isPassable = true;
                    talkDoer.usesOverrideInteractionRegion = false;
                    talkDoer.overrideRegionOffset = Vector2.zero;
                    talkDoer.overrideRegionDimensions = Vector2.zero;
                    talkDoer.overrideInteractionRadius = -1;
                    talkDoer.PreventInteraction = false;
                    talkDoer.AllowPlayerToPassEventually = true;
                    talkDoer.speakPoint = SpeechPoint.transform;
                    talkDoer.SpeaksGleepGlorpenese = false;
                    talkDoer.audioCharacterSpeechTag = "oldman";
                    talkDoer.playerApproachRadius = 5;
                    talkDoer.conversationBreakRadius = 5;
                    talkDoer.echo1 = null;
                    talkDoer.echo2 = null;
                    talkDoer.PreventCoopInteraction = false;
                    talkDoer.IsPaletteSwapped = false;
                    talkDoer.PaletteTexture = null;
                    talkDoer.OutlineDepth = 0.5f;
                    talkDoer.OutlineLuminanceCutoff = 0.05f;
                    talkDoer.MovementSpeed = 3;
                    talkDoer.PathableTiles = CellTypes.FLOOR;

                // TalkDoer talkDoerExtreme  = npcObj.AddComponent<TalkDoer>();
                //     talkDoerExtreme.placeableWidth = 4;
                //     talkDoerExtreme.placeableHeight = 3;
                //     talkDoerExtreme.difficulty = 0;
                //     talkDoerExtreme.isPassable = true;
                //     talkDoerExtreme.usesOverrideInteractionRegion = false;
                //     talkDoerExtreme.overrideRegionOffset = Vector2.zero;
                //     talkDoerExtreme.overrideRegionDimensions = Vector2.zero;
                //     // talkDoerExtreme.overrideInteractionRadius = -1;
                //     // talkDoerExtreme.PreventInteraction = false;
                //     // talkDoerExtreme.AllowPlayerToPassEventually = true;
                //     talkDoerExtreme.speakPoint = SpeechPoint.transform;
                //     // talkDoerExtreme.SpeaksGleepGlorpenese = false;
                //     talkDoerExtreme.audioCharacterSpeechTag = "oldman";
                //     // talkDoerExtreme.playerApproachRadius = 5;
                //     talkDoerExtreme.conversationBreakRadius = 5;
                //     talkDoerExtreme.echo1 = null;
                //     talkDoerExtreme.echo2 = null;
                //     // talkDoerExtreme.PreventCoopInteraction = false;
                //     // talkDoerExtreme.IsPaletteSwapped = false;
                //     // talkDoerExtreme.PaletteTexture = null;
                //     // talkDoerExtreme.OutlineDepth = 0.5f;
                //     // talkDoerExtreme.OutlineLuminanceCutoff = 0.05f;
                //     // talkDoerExtreme.MovementSpeed = 3;
                //     // talkDoerExtreme.PathableTiles = CellTypes.FLOOR;

                UltraFortunesFavor dreamLuck = npcObj.AddComponent<UltraFortunesFavor>();
                    dreamLuck.goopRadius = 2;
                    dreamLuck.beamRadius = 2;
                    dreamLuck.bulletRadius = 2;
                    dreamLuck.bulletSpeedModifier = 0.8f;
                    dreamLuck.vfxOffset = 0.625f;
                    dreamLuck.sparkOctantVFX = shared_auto_001.LoadAsset<GameObject>("FortuneFavor_VFX_Spark");

                AIAnimator aIAnimator = GenerateBlankAIAnimator(npcObj);
                    aIAnimator.spriteAnimator = spriteAnimator;
                    aIAnimator.IdleAnimation = new DirectionalAnimation
                    {
                        Type = DirectionalAnimation.DirectionType.Single,
                        Prefix = name + "_idle",
                        AnimNames = new string[] {""},
                        Flipped = new DirectionalAnimation.FlipType[]{DirectionalAnimation.FlipType.None}
                    };

                    aIAnimator.TalkAnimation = new DirectionalAnimation
                    {
                        Type = DirectionalAnimation.DirectionType.Single,
                        Prefix = name + "_talk",
                        AnimNames = new string[] {""},
                        Flipped = new DirectionalAnimation.FlipType[]{DirectionalAnimation.FlipType.None}
                    };

                // Code for overwriting dialogue
                var basenpc = ResourceManager.LoadAssetBundle("shared_auto_001").LoadAsset<GameObject>("Merchant_Key").transform.Find("NPC_Key").gameObject;
                // var basenpc = ResourceManager.LoadAssetBundle("shared_auto_001").LoadAsset<GameObject>("npc_gunslingking");
                // var basenpc = ResourceManager.LoadAssetBundle("shared_auto_001").LoadAsset<GameObject>("npc_lostadventurer");

                PlayMakerFSM iHaveNoFuckingClueWhatThisIs = npcObj.AddComponent<PlayMakerFSM>();
                UnityEngine.JsonUtility.FromJsonOverwrite(UnityEngine.JsonUtility.ToJson(basenpc.GetComponent<PlayMakerFSM>()), iHaveNoFuckingClueWhatThisIs);
                FieldInfo fsmStringParams = typeof(ActionData).GetField("fsmStringParams", BindingFlags.NonPublic | BindingFlags.Instance);
                // (fsmStringParams.GetValue(iHaveNoFuckingClueWhatThisIs.FsmStates[1].ActionData) as List<FsmString>)[0].Value = runBasedMultilineGenericStringKey;
                // (fsmStringParams.GetValue(iHaveNoFuckingClueWhatThisIs.FsmStates[1].ActionData) as List<FsmString>)[1].Value = runBasedMultilineStopperStringKey;
                // (fsmStringParams.GetValue(iHaveNoFuckingClueWhatThisIs.FsmStates[4].ActionData) as List<FsmString>)[0].Value = purchaseItemStringKey;
                // (fsmStringParams.GetValue(iHaveNoFuckingClueWhatThisIs.FsmStates[5].ActionData) as List<FsmString>)[0].Value = purchaseItemFailedStringKey;
                // (fsmStringParams.GetValue(iHaveNoFuckingClueWhatThisIs.FsmStates[7].ActionData) as List<FsmString>)[0].Value = introStringKey;
                // (fsmStringParams.GetValue(iHaveNoFuckingClueWhatThisIs.FsmStates[8].ActionData) as List<FsmString>)[0].Value = attackedStringKey;
                // (fsmStringParams.GetValue(iHaveNoFuckingClueWhatThisIs.FsmStates[9].ActionData) as List<FsmString>)[0].Value = "#SUBSHOP_GENERIC_CAUGHT_STEALING";
                // (fsmStringParams.GetValue(iHaveNoFuckingClueWhatThisIs.FsmStates[10].ActionData) as List<FsmString>)[0].Value = "#SHOP_GENERIC_NO_SALE_LABEL";
                // (fsmStringParams.GetValue(iHaveNoFuckingClueWhatThisIs.FsmStates[12].ActionData) as List<FsmString>)[0].Value = "#COOP_REBUKE";

                npcObj.name = prefix + ":" + name;
                return npcObj;
            }
            catch (Exception message)
            {
                ETGModConsole.Log(message.ToString());
                return null;
            }
        }

        // public static GameObject SetUpGenericNpc(string name, string prefix, List<string> idleSpritePaths, List<string> talkSpritePaths, int idleFps, Vector3 talkPointOffset/*, GungeonFlags flag*/)
        // {
        //     var shared_auto_001 = ResourceManager.LoadAssetBundle("shared_auto_001");
        //     var shared_auto_002 = ResourceManager.LoadAssetBundle("shared_auto_002");
        //     var SpeechPoint = new GameObject("SpeechPoint");
        //     SpeechPoint.transform.position = talkPointOffset;

        //     var npcObj = SpriteBuilder.SpriteFromResource(idleSpritePaths[0], new GameObject(prefix + ":" + name));

        //     FakePrefab.MarkAsFakePrefab(npcObj);
        //     UnityEngine.Object.DontDestroyOnLoad(npcObj);
        //     npcObj.SetActive(false);

        //     npcObj.layer = 22;

        //     var collection = npcObj.GetComponent<tk2dSprite>().Collection;
        //     SpeechPoint.transform.parent = npcObj.transform;

        //     FakePrefab.MarkAsFakePrefab(SpeechPoint);
        //     UnityEngine.Object.DontDestroyOnLoad(SpeechPoint);
        //     SpeechPoint.SetActive(true);

        //     var idleIdsList = new List<int>();
        //     var talkIdsList = new List<int>();

        //     foreach (string sprite in idleSpritePaths)
        //     {
        //         idleIdsList.Add(SpriteBuilder.AddSpriteToCollection(sprite, collection));
        //     }

        //     foreach (string sprite in talkSpritePaths)
        //     {
        //         talkIdsList.Add(SpriteBuilder.AddSpriteToCollection(sprite, collection));
        //     }

        //     tk2dSpriteAnimator spriteAnimator = npcObj.AddComponent<tk2dSpriteAnimator>();

        //     SpriteBuilder.AddAnimation(spriteAnimator, collection, idleIdsList, name + "_idle", tk2dSpriteAnimationClip.WrapMode.Loop, idleFps);
        //     SpriteBuilder.AddAnimation(spriteAnimator, collection, talkIdsList, name + "_talk", tk2dSpriteAnimationClip.WrapMode.Loop, idleFps);

        //     SpeculativeRigidbody rigidbody = GenerateOrAddToRigidBody(npcObj, CollisionLayer.BulletBlocker, PixelCollider.PixelColliderGeneration.Manual, true, true, true, false, false, false, false, true, new IntVector2(20, 18), new IntVector2(5, 0));

        //     var basenpc = ResourceManager.LoadAssetBundle("shared_auto_001").LoadAsset<GameObject>("Merchant_Key").transform.Find("NPC_Key").gameObject;
        //     PlayMakerFSM iHaveNoFuckingClueWhatThisIs = npcObj.AddComponent<PlayMakerFSM>();
        //     UnityEngine.JsonUtility.FromJsonOverwrite(UnityEngine.JsonUtility.ToJson(basenpc.GetComponent<PlayMakerFSM>()), iHaveNoFuckingClueWhatThisIs);
        //     FieldInfo fsmStringParams = typeof(ActionData).GetField("fsmStringParams", BindingFlags.NonPublic | BindingFlags.Instance);
        //         (fsmStringParams.GetValue(iHaveNoFuckingClueWhatThisIs.FsmStates[1].ActionData) as List<FsmString>)[0].Value = "#COOP_REBUKE";
        //         (fsmStringParams.GetValue(iHaveNoFuckingClueWhatThisIs.FsmStates[1].ActionData) as List<FsmString>)[1].Value = "#COOP_REBUKE";
        //         (fsmStringParams.GetValue(iHaveNoFuckingClueWhatThisIs.FsmStates[4].ActionData) as List<FsmString>)[0].Value = "#COOP_REBUKE";
        //         (fsmStringParams.GetValue(iHaveNoFuckingClueWhatThisIs.FsmStates[5].ActionData) as List<FsmString>)[0].Value = "#COOP_REBUKE";
        //         (fsmStringParams.GetValue(iHaveNoFuckingClueWhatThisIs.FsmStates[7].ActionData) as List<FsmString>)[0].Value = "#COOP_REBUKE";
        //         (fsmStringParams.GetValue(iHaveNoFuckingClueWhatThisIs.FsmStates[8].ActionData) as List<FsmString>)[0].Value = "#COOP_REBUKE";
        //         (fsmStringParams.GetValue(iHaveNoFuckingClueWhatThisIs.FsmStates[9].ActionData) as List<FsmString>)[0].Value = "#SUBSHOP_GENERIC_CAUGHT_STEALING";
        //         (fsmStringParams.GetValue(iHaveNoFuckingClueWhatThisIs.FsmStates[10].ActionData) as List<FsmString>)[0].Value = "#SHOP_GENERIC_NO_SALE_LABEL";
        //         (fsmStringParams.GetValue(iHaveNoFuckingClueWhatThisIs.FsmStates[12].ActionData) as List<FsmString>)[0].Value = "#COOP_REBUKE";

        //     // PlayMakerFSM nightmareNightmareNightmare = npcObj.AddComponent<PlayMakerFSM>();

        //     // var basenpc = shared_auto_002.LoadAsset<GameObject>("NPC_Key_Jailed");

        //     // UnityEngine.JsonUtility.FromJsonOverwrite(UnityEngine.JsonUtility.ToJson(basenpc.GetComponent<PlayMakerFSM>()), nightmareNightmareNightmare);

        //     // foreach (var state in nightmareNightmareNightmare.Fsm.FsmComponent.FsmStates)
        //     // {
        //     //     foreach (var action in state.Actions)
        //     //     {
        //     //         if (action is SetSaveFlag)
        //     //         {
        //     //             ((SetSaveFlag)action).targetFlag = flag;
        //     //         }
        //     //     }
        //     // }

        //     npcObj.name = prefix + ":" + name;

        //     AIAnimator aIAnimator = GenerateBlankAIAnimator(npcObj);
        //     aIAnimator.spriteAnimator = spriteAnimator;
        //     aIAnimator.IdleAnimation = new DirectionalAnimation
        //     {
        //         Type = DirectionalAnimation.DirectionType.Single,
        //         Prefix = name + "_idle",
        //         AnimNames = new string[]
        //         {
        //                 ""
        //         },
        //         Flipped = new DirectionalAnimation.FlipType[]
        //         {
        //                 DirectionalAnimation.FlipType.None
        //         }

        //     };

        //     TalkDoerLite talkDoer = npcObj.AddComponent<TalkDoerLite>();

        //     talkDoer.placeableWidth = 4;
        //     talkDoer.placeableHeight = 3;
        //     talkDoer.difficulty = 0;
        //     talkDoer.isPassable = true;
        //     talkDoer.usesOverrideInteractionRegion = false;
        //     talkDoer.overrideRegionOffset = Vector2.zero;
        //     talkDoer.overrideRegionDimensions = Vector2.zero;
        //     talkDoer.overrideInteractionRadius = -1;
        //     talkDoer.PreventInteraction = false;
        //     talkDoer.AllowPlayerToPassEventually = true;
        //     talkDoer.speakPoint = SpeechPoint.transform;
        //     talkDoer.SpeaksGleepGlorpenese = false;
        //     talkDoer.audioCharacterSpeechTag = "oldman";
        //     talkDoer.playerApproachRadius = 5;
        //     talkDoer.conversationBreakRadius = 5;
        //     talkDoer.echo1 = null;
        //     talkDoer.echo2 = null;
        //     talkDoer.PreventCoopInteraction = false;
        //     talkDoer.IsPaletteSwapped = false;
        //     talkDoer.PaletteTexture = null;
        //     talkDoer.OutlineDepth = 0.5f;
        //     talkDoer.OutlineLuminanceCutoff = 0.05f;
        //     talkDoer.MovementSpeed = 3;
        //     talkDoer.PathableTiles = CellTypes.FLOOR;

        //     talkDoer.IsInteractable = true;
        //     talkDoer.ShowOutlines = true;

        //     UltraFortunesFavor dreamLuck = npcObj.AddComponent<UltraFortunesFavor>();

        //     dreamLuck.goopRadius = 2;
        //     dreamLuck.beamRadius = 2;
        //     dreamLuck.bulletRadius = 2;
        //     dreamLuck.bulletSpeedModifier = 0.8f;

        //     dreamLuck.vfxOffset = 0.625f;
        //     dreamLuck.sparkOctantVFX = shared_auto_001.LoadAsset<GameObject>("FortuneFavor_VFX_Spark");

        //     // NPCInteractable ia = npcObj.AddComponent<NPCInteractable>();
        //     // ia.conversation = new List<string>(){ "hi there o:" };

        //     // npcObj.SetActive(true);

        //     return npcObj;
        // }

        public static string AddCustomCurrencyType(string ammoTypeSpritePath, string name)
        {
            return GameUIRoot.Instance.ConversationBar.portraitSprite.Atlas.AddNewItemToAtlas(NpcTools.GetTextureFromResource(ammoTypeSpritePath), name).name;
        }






        public static void RegisterShopRoom(GameObject shop, PrototypeDungeonRoom protoroom, Vector2 vector)
        {
            protoroom.category = PrototypeDungeonRoom.RoomCategory.NORMAL;
            DungeonPrerequisite[] array = shop.GetComponent<CustomShopController>()?.prerequisites != null ? shop.GetComponent<CustomShopController>().prerequisites : new DungeonPrerequisite[0];
            //Vector2 vector = new Vector2((float)(protoroom.Width / 2) + offset.x, (float)(protoroom.Height / 2) + offset.y);
            protoroom.placedObjectPositions.Add(vector);
            protoroom.placedObjects.Add(new PrototypePlacedObjectData
            {
                contentsBasePosition = vector,
                fieldData = new List<PrototypePlacedObjectFieldData>(),
                instancePrerequisites = array,
                linkedTriggerAreaIDs = new List<int>(),
                placeableContents = new DungeonPlaceable
                {
                    width = 2,
                    height = 2,
                    respectsEncounterableDifferentiator = true,
                    variantTiers = new List<DungeonPlaceableVariant>
                    {
                        new DungeonPlaceableVariant
                        {
                            percentChance = 1f,
                            nonDatabasePlaceable = shop,
                            prerequisites = array,
                            materialRequirements = new DungeonPlaceableRoomMaterialRequirement[0]
                        }
                    }
                }
            });
            RoomFactory.RoomData roomData = new RoomFactory.RoomData
            {
                room = protoroom,
                isSpecialRoom = true,
                category = "SPECIAL",
                specialSubCategory = "WEIRD_SHOP"
            };
            RoomFactory.rooms.Add(shop.name, roomData);
            DungeonHandler.Register(roomData);
        }


        public static AIAnimator GenerateBlankAIAnimator(GameObject targetObject)
        {
            AIAnimator aianimator = targetObject.AddComponent<AIAnimator>();
            aianimator.facingType = AIAnimator.FacingType.Default;
            aianimator.faceSouthWhenStopped = false;
            aianimator.faceTargetWhenStopped = false;
            aianimator.AnimatedFacingDirection = -90f;
            aianimator.directionalType = AIAnimator.DirectionalType.Sprite;
            aianimator.RotationQuantizeTo = 0f;
            aianimator.RotationOffset = 0f;
            aianimator.ForceKillVfxOnPreDeath = false;
            aianimator.SuppressAnimatorFallback = false;
            aianimator.IsBodySprite = true;
            aianimator.IdleAnimation = new DirectionalAnimation
            {
                Type = DirectionalAnimation.DirectionType.None,
                Prefix = string.Empty,
                AnimNames = new string[0],
                Flipped = new DirectionalAnimation.FlipType[0]
            };
            aianimator.MoveAnimation = new DirectionalAnimation
            {
                Type = DirectionalAnimation.DirectionType.None,
                Prefix = string.Empty,
                AnimNames = new string[0],
                Flipped = new DirectionalAnimation.FlipType[0]
            };
            aianimator.FlightAnimation = new DirectionalAnimation
            {
                Type = DirectionalAnimation.DirectionType.None,
                Prefix = string.Empty,
                AnimNames = new string[0],
                Flipped = new DirectionalAnimation.FlipType[0]
            };
            aianimator.HitAnimation = new DirectionalAnimation
            {
                Type = DirectionalAnimation.DirectionType.None,
                Prefix = string.Empty,
                AnimNames = new string[0],
                Flipped = new DirectionalAnimation.FlipType[0]
            };
            aianimator.TalkAnimation = new DirectionalAnimation
            {
                Type = DirectionalAnimation.DirectionType.None,
                Prefix = string.Empty,
                AnimNames = new string[0],
                Flipped = new DirectionalAnimation.FlipType[0]
            };
            aianimator.OtherAnimations = new List<AIAnimator.NamedDirectionalAnimation>(0);
            aianimator.OtherVFX = new List<AIAnimator.NamedVFXPool>(0);
            aianimator.OtherScreenShake = new List<AIAnimator.NamedScreenShake>(0);
            aianimator.IdleFidgetAnimations = new List<DirectionalAnimation>(0);
            aianimator.HitReactChance = 1f;
            aianimator.HitType = AIAnimator.HitStateType.Basic;
            return aianimator;
        }

        public static tk2dSpriteAnimationClip AddAnimation(tk2dSpriteAnimator animator, tk2dSpriteCollectionData collection, List<int> spriteIDs,
            string clipName, tk2dSpriteAnimationClip.WrapMode wrapMode = tk2dSpriteAnimationClip.WrapMode.Loop, float fps = 15)
        {
            if (animator.Library == null)
            {
                animator.Library = animator.gameObject.AddComponent<tk2dSpriteAnimation>();
                animator.Library.clips = new tk2dSpriteAnimationClip[0];
                animator.Library.enabled = true;

            }

            List<tk2dSpriteAnimationFrame> frames = new List<tk2dSpriteAnimationFrame>();
            for (int i = 0; i < spriteIDs.Count; i++)
            {
                tk2dSpriteDefinition sprite = collection.spriteDefinitions[spriteIDs[i]];
                if (sprite.Valid)
                {
                    frames.Add(new tk2dSpriteAnimationFrame()
                    {
                        spriteCollection = collection,
                        spriteId = spriteIDs[i]
                    });
                }
            }

            var clip = new tk2dSpriteAnimationClip()
            {
                name = clipName,
                fps = fps,
                wrapMode = wrapMode,
            };
            Array.Resize(ref animator.Library.clips, animator.Library.clips.Length + 1);
            animator.Library.clips[animator.Library.clips.Length - 1] = clip;

            clip.frames = frames.ToArray();
            return clip;
        }

        public static SpeculativeRigidbody GenerateOrAddToRigidBody(GameObject targetObject, CollisionLayer collisionLayer, PixelCollider.PixelColliderGeneration colliderGenerationMode = PixelCollider.PixelColliderGeneration.Tk2dPolygon, bool collideWithTileMap = false, bool CollideWithOthers = true, bool CanBeCarried = true, bool CanBePushed = false, bool RecheckTriggers = false, bool IsTrigger = false, bool replaceExistingColliders = false, bool UsesPixelsAsUnitSize = false, IntVector2? dimensions = null, IntVector2? offset = null)
        {
            SpeculativeRigidbody orAddComponent = targetObject.GetOrAddComponent<SpeculativeRigidbody>();
            orAddComponent.CollideWithOthers = CollideWithOthers;
            orAddComponent.CollideWithTileMap = collideWithTileMap;
            orAddComponent.Velocity = Vector2.zero;
            orAddComponent.MaxVelocity = Vector2.zero;
            orAddComponent.ForceAlwaysUpdate = false;
            orAddComponent.CanPush = false;
            orAddComponent.CanBePushed = CanBePushed;
            orAddComponent.PushSpeedModifier = 1f;
            orAddComponent.CanCarry = false;
            orAddComponent.CanBeCarried = CanBeCarried;
            orAddComponent.PreventPiercing = false;
            orAddComponent.SkipEmptyColliders = false;
            orAddComponent.RecheckTriggers = RecheckTriggers;
            orAddComponent.UpdateCollidersOnRotation = false;
            orAddComponent.UpdateCollidersOnScale = false;
            IntVector2 intVector = IntVector2.Zero;
            IntVector2 intVector2 = IntVector2.Zero;
            if (colliderGenerationMode != PixelCollider.PixelColliderGeneration.Tk2dPolygon)
            {
                if (dimensions != null)
                {
                    intVector2 = dimensions.Value;
                    if (!UsesPixelsAsUnitSize)
                    {
                        intVector2 = new IntVector2(intVector2.x * 16, intVector2.y * 16);
                    }
                }
                if (offset != null)
                {
                    intVector = offset.Value;
                    if (!UsesPixelsAsUnitSize)
                    {
                        intVector = new IntVector2(intVector.x * 16, intVector.y * 16);
                    }
                }
            }
            PixelCollider item = new PixelCollider
            {
                ColliderGenerationMode = colliderGenerationMode,
                CollisionLayer = collisionLayer,
                IsTrigger = IsTrigger,
                BagleUseFirstFrameOnly = (colliderGenerationMode == PixelCollider.PixelColliderGeneration.Tk2dPolygon),
                SpecifyBagelFrame = string.Empty,
                BagelColliderNumber = 0,
                ManualOffsetX = intVector.x,
                ManualOffsetY = intVector.y,
                ManualWidth = intVector2.x,
                ManualHeight = intVector2.y,
                ManualDiameter = 0,
                ManualLeftX = 0,
                ManualLeftY = 0,
                ManualRightX = 0,
                ManualRightY = 0
            };
            if (replaceExistingColliders | orAddComponent.PixelColliders == null)
            {
                orAddComponent.PixelColliders = new List<PixelCollider>
                {
                    item
                };
            }
            else
            {
                orAddComponent.PixelColliders.Add(item);
            }
            if (orAddComponent.sprite && colliderGenerationMode == PixelCollider.PixelColliderGeneration.Tk2dPolygon)
            {
                Bounds bounds = orAddComponent.sprite.GetBounds();
                orAddComponent.sprite.GetTrueCurrentSpriteDef().colliderVertices = new Vector3[]
                {
                    bounds.center - bounds.extents,
                    bounds.center + bounds.extents
                };
            }
            return orAddComponent;
        }

    }
}
