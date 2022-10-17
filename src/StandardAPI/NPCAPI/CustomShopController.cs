using System;
using System.Collections.Generic;
using Dungeonator;
using HutongGames.PlayMaker;
using UnityEngine;

namespace NpcApi
{
	public class CustomShopController : BaseShopController
	{
		/*[Serializable]
		public delegate bool CustomCanBuy(CustomShopController shop, PlayerController player, int cost);
		[Serializable]
		public delegate int RemoveCurrency(CustomShopController shop, PlayerController player, int cost);
		[Serializable]
		public delegate int CustomPrice(CustomShopController shop, PickupObject item);


		public event CustomCanBuy customCanBuy;
		public event RemoveCurrency removeCurrency;	
		public event CustomPrice customPrice;*/

		//[Serializable]
		//public delegate TResult Func<T1, T2, T3, TResult>(T1 arg1, T2 arg2, T3 arg3);
		public Func<CustomShopController, PlayerController, int, bool> customCanBuy;
		public Func<CustomShopController, PlayerController, int, int> removeCurrency;
		public Func<CustomShopController, CustomShopItemController, PickupObject, int> customPrice;
		public Func<PlayerController, PickupObject, int, bool> OnPurchase;
		public Func<PlayerController, PickupObject, int, bool> OnSteal;

		//public DungeonPrerequisite

		public string customPriceSprite;

		public CustomShopItemController.ShopCurrencyType currencyType;
		public bool giveStatsOnPurchase;
		public bool canBeRobbed;
		public StatModifier[] statsToGive;
		public DungeonPrerequisite[] prerequisites = new DungeonPrerequisite[0];

		//public List<CustomShopItemController> m_customShopItemControllers;
		//public List<ShopItemController> m_shopItemControllers;

		public int CustomPriceMethod(CustomShopController shop, CustomShopItemController shopItem, PickupObject item)
		{
			if (customPrice != null)
				return customPrice(shop, shopItem, item);
			return 0;
		}
		public int RemoveCurrencyMethod(CustomShopController shop, PlayerController player, int cost)
		{
			if (removeCurrency != null)
				return removeCurrency(shop, player, cost);
			return 0;
		}
		public bool CustomCanBuyMethod(CustomShopController shop, PlayerController player, int cost)
		{
			if (customCanBuy != null)
				return customCanBuy(shop, player, cost);
			return true;
		}

		public bool OnStealMethod(PlayerController player, PickupObject item, int cost)
		{
			if (OnSteal != null)
			{
				return OnSteal(player, item, cost);
			}
			return false;
		}

		public bool OnPurchaseMethod(PlayerController player, PickupObject item, int cost)
		{
			if (OnPurchase != null)
			{
				return OnPurchase(player, item, cost);
			}
			return false;
		}

		public new void LockItems()
		{
			for (int i = 0; i < this.m_itemControllers.Count; i++)
			{
				if (this.m_itemControllers[i])
				{
					this.m_itemControllers[i].Locked = true;
				}
			}
			for (int j = 0; j < GameManager.Instance.AllPlayers.Length; j++)
			{
				PlayerController playerController = GameManager.Instance.AllPlayers[j];
				if (playerController && playerController.healthHaver.IsAlive)
				{
					playerController.ForceRefreshInteractable = true;
				}
			}
		}

		public override void PurchaseItem(ShopItemController itemBad, bool actualPurchase = true, bool allowSign = true)
		{
			var item = itemBad as CustomShopItemController;
			float heightOffGround = -1f;
			if (item && item.sprite)
			{
				heightOffGround = item.sprite.HeightOffGround;
			}
			if (actualPurchase)
			{
				if (giveStatsOnPurchase)
				{
					foreach (var stat in statsToGive)
					{
						item.LastInteractingPlayer.ownerlessStatModifiers.Add(stat);
					}
					item.LastInteractingPlayer.stats.RecalculateStats(item.LastInteractingPlayer, false, false);
				}
				if (this.shopkeepFSM != null)
				{
					FsmObject fsmObject = this.shopkeepFSM.FsmVariables.FindFsmObject("referencedItem");
					if (fsmObject != null)
					{
						fsmObject.Value = item;
					}
					this.shopkeepFSM.SendEvent("succeedPurchase");
				}
			}
			if (!item.item.PersistsOnPurchase)
			{
				if (allowSign)
				{
					GameObject gameObject = (GameObject)UnityEngine.Object.Instantiate(BraveResources.Load("Global Prefabs/Sign_SoldOut", ".prefab"));
					tk2dBaseSprite component = gameObject.GetComponent<tk2dBaseSprite>();
					component.PlaceAtPositionByAnchor(item.sprite.WorldCenter, tk2dBaseSprite.Anchor.MiddleCenter);
					gameObject.transform.position = gameObject.transform.position.Quantize(0.0625f);
					component.HeightOffGround = heightOffGround;
					component.UpdateZDepth();
				}
				GameObject gameObject2 = (GameObject)UnityEngine.Object.Instantiate(ResourceCache.Acquire("Global VFX/VFX_Item_Spawn_Poof"));
				tk2dBaseSprite component2 = gameObject2.GetComponent<tk2dBaseSprite>();
				component2.PlaceAtPositionByAnchor(item.sprite.WorldCenter.ToVector3ZUp(0f), tk2dBaseSprite.Anchor.MiddleCenter);
				component2.transform.position = component2.transform.position.Quantize(0.0625f);
				component2.HeightOffGround = 5f;
				component2.UpdateZDepth();
				this.m_room.DeregisterInteractable(item);
				UnityEngine.Object.Destroy(item.gameObject);
			}
		}

		public void ActionAndFuncSetUp(Func<CustomShopController, PlayerController, int, bool> CustomCanBuySetUp, Func<CustomShopController, PlayerController, int, int> RemoveCurrencySetUp, Func<CustomShopController, CustomShopItemController, PickupObject, int> CustomPriceSetUp,
			Func<PlayerController, PickupObject, int, bool> OnPurchase, Func<PlayerController, PickupObject, int, bool> OnSteal)
		{
			this.customCanBuy = CustomCanBuySetUp;
			this.removeCurrency = RemoveCurrencySetUp;
			this.customPrice = CustomPriceSetUp;
			this.OnPurchase = OnPurchase;
			this.OnSteal = OnSteal;

		}


		public override void DoSetup()
		{

			base.m_shopItems = new List<GameObject>();
			List<int> list = new List<int>();
			Func<GameObject, float, float> weightModifier = null;
			if (SecretHandshakeItem.NumActive > 0)
			{
				weightModifier = delegate (GameObject prefabObject, float sourceWeight)
				{
					PickupObject component10 = prefabObject.GetComponent<PickupObject>();
					float num7 = sourceWeight;
					if (component10 != null)
					{
						int quality = (int)component10.quality;
						num7 *= 1f + (float)quality / 10f;
					}
					return num7;
				};
			}
			bool flag = GameStatsManager.Instance.IsRainbowRun;
			for (int i = 0; i < base.spawnPositions.Length; i++)
			{
				if (flag)
				{
					base.m_shopItems.Add(null);
				}
				else
				{
					GameObject gameObject5 = this.shopItems.SubshopSelectByWeightWithoutDuplicatesFullPrereqs(this.m_shopItems, weightModifier, 1, GameManager.Instance.IsSeeded);
					this.m_shopItems.Add(gameObject5);
				}
			}
			m_itemControllers = new List<ShopItemController>();
			for (int m = 0; m < base.spawnPositions.Length; m++)
			{
				Transform transform = base.spawnPositions[m];
				if (!flag && !(base.m_shopItems[m] == null))
				{
					PickupObject component4 = base.m_shopItems[m].GetComponent<PickupObject>();
					if (!(component4 == null))
					{
						GameObject gameObject6 = new GameObject("Shop item " + m.ToString());
						Transform transform2 = gameObject6.transform;
						transform2.parent = transform;
						transform2.localPosition = Vector3.zero;
						EncounterTrackable component5 = component4.GetComponent<EncounterTrackable>();
						if (component5 != null)
						{
							GameManager.Instance.ExtantShopTrackableGuids.Add(component5.EncounterGuid);
						}
						CustomShopItemController shopItemController = gameObject6.AddComponent<CustomShopItemController>();


						this.AssignItemFacing(transform, shopItemController);
						if (!base.m_room.IsRegistered(shopItemController))
						{
							base.m_room.RegisterInteractable(shopItemController);
						}

						shopItemController.CurrencyType = currencyType;


						shopItemController.customCanBuy += CustomCanBuyMethod;
						shopItemController.customPrice += CustomPriceMethod;
						shopItemController.removeCurrency += RemoveCurrencyMethod;

						shopItemController.OnPurchase += OnPurchaseMethod;
						shopItemController.OnSteal += OnStealMethod;

						shopItemController.customPriceSprite = this.customPriceSprite;

						shopItemController.Initialize(component4, this);
						m_itemControllers.Add(shopItemController);
					}
				}
			}
			bool flag3 = false;
			if (base.shopItemsGroup2 != null && base.spawnPositionsGroup2.Length > 0)
			{
				int count = base.m_shopItems.Count;
				for (int num2 = 0; num2 < base.spawnPositionsGroup2.Length; num2++)
				{
					if (flag)
					{
						base.m_shopItems.Add(null);
					}
					else
					{
						float num3 = base.spawnGroupTwoItem1Chance;
						if (num2 == 1)
						{
							num3 = base.spawnGroupTwoItem2Chance;
						}
						else if (num2 == 2)
						{
							num3 = base.spawnGroupTwoItem3Chance;
						}
						bool isSeeded = GameManager.Instance.IsSeeded;
						if (((!isSeeded) ? UnityEngine.Random.value : BraveRandom.GenerationRandomValue()) < num3)
						{
							float replaceFirstRewardWithPickup = GameManager.Instance.RewardManager.CurrentRewardData.ReplaceFirstRewardWithPickup;
							if (!flag3 && ((!isSeeded) ? UnityEngine.Random.value : BraveRandom.GenerationRandomValue()) < replaceFirstRewardWithPickup)
							{
								flag3 = true;
								GameObject item2 = base.shopItems.SelectByWeightWithoutDuplicatesFullPrereqs(base.m_shopItems, weightModifier, GameManager.Instance.IsSeeded);
								base.m_shopItems.Add(item2);
							}
							else if (!GameStatsManager.Instance.IsRainbowRun)
							{
								GameObject rewardObjectShopStyle2 = GameManager.Instance.RewardManager.GetRewardObjectShopStyle(GameManager.Instance.PrimaryPlayer, false, false, base.m_shopItems);
								base.m_shopItems.Add(rewardObjectShopStyle2);
							}
							else
							{
								base.m_shopItems.Add(null);
							}
						}
						else
						{
							base.m_shopItems.Add(null);
						}
					}
				}
				for (int num4 = 0; num4 < base.spawnPositionsGroup2.Length; num4++)
				{
					Transform transform3 = base.spawnPositionsGroup2[num4];
					if (!flag && !(base.m_shopItems[count + num4] == null))
					{
						PickupObject component8 = base.m_shopItems[count + num4].GetComponent<PickupObject>();
						if (!(component8 == null))
						{
							GameObject gameObject8 = new GameObject("Shop 2 item " + num4.ToString());
							Transform transform4 = gameObject8.transform;
							transform4.parent = transform3;
							transform4.localPosition = Vector3.zero;
							EncounterTrackable component9 = component8.GetComponent<EncounterTrackable>();
							if (component9 != null)
							{
								GameManager.Instance.ExtantShopTrackableGuids.Add(component9.EncounterGuid);
							}
							CustomShopItemController shopItemController2 = gameObject8.AddComponent<CustomShopItemController>();
							this.AssignItemFacing(transform3, shopItemController2);
							if (!base.m_room.IsRegistered(shopItemController2))
							{
								base.m_room.RegisterInteractable(shopItemController2);
							}
							shopItemController2.Initialize(component8, this);
							m_itemControllers.Add(shopItemController2);
						}
					}
				}
			}
			/*for (int num6 = 0; num6 < m_customShopItemControllers.Count; num6++)
			{
				
				m_customShopItemControllers[num6].CurrencyType = currencyType;

				m_customShopItemControllers[num6].CustomCanBuy += Balls.CustomCanBuy;
				m_customShopItemControllers[num6].CustomPrice += Balls.CustomPrice;
				m_customShopItemControllers[num6].RemoveCurrency += Balls.RemoveCurrency;
				m_customShopItemControllers[num6].customPriceSprite = this.customPriceSprite;
			}*/
		}

		private void AssignItemFacing(Transform spawnTransform, CustomShopItemController itemController)
		{
			if (spawnTransform.name.Contains("SIDE") || spawnTransform.name.Contains("EAST"))
			{
				itemController.itemFacing = DungeonData.Direction.EAST;
			}
			else if (spawnTransform.name.Contains("WEST"))
			{
				itemController.itemFacing = DungeonData.Direction.WEST;
			}
			else if (spawnTransform.name.Contains("NORTH"))
			{
				itemController.itemFacing = DungeonData.Direction.NORTH;
			}
		}
	}
}
