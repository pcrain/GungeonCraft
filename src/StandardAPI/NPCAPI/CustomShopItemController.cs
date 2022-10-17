using Dungeonator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static NpcApi.CustomShopController;

namespace NpcApi
{
	public class CustomShopItemController : ShopItemController, IPlayerInteractable
	{
		public CustomShopItemController()
		{
			this.itemFacing = DungeonData.Direction.SOUTH;
			this.CurrentPrice = -1;
			this.THRESHOLD_CUTOFF_PRIMARY = 3f;
			this.THRESHOLD_CUTOFF_SECONDARY = 2f;
		}

		/*public Func<CustomShopController, PlayerController, int, bool> CustomCanBuy;
		public Func<CustomShopController, PlayerController, int, int> RemoveCurrency;
		public Func2<CustomShopController, PickupObject, int> CustomPrice;

		public event CustomCanBuy customCanBuy;
		public event RemoveCurrency removeCurrency;
		public event CustomPrice customPrice;*/

		public Func<CustomShopController, PlayerController, int, bool> customCanBuy;
		public Func<CustomShopController, PlayerController, int, int> removeCurrency;
		public Func<CustomShopController, CustomShopItemController, PickupObject, int> customPrice;
		public Func<PlayerController, PickupObject, int, bool> OnPurchase;
		public Func<PlayerController, PickupObject, int, bool> OnSteal;
		public string customPriceSprite;

		public new bool Locked { get; set; }

		public new int ModifiedPrice
		{
			get
			{
				if (this.CurrencyType == CustomShopItemController.ShopCurrencyType.META_CURRENCY)
				{
					return this.CurrentPrice;
				}
				if (this.CurrencyType == CustomShopItemController.ShopCurrencyType.KEYS)
				{
					return this.CurrentPrice;
				}
				if (this.CurrencyType == CustomShopItemController.ShopCurrencyType.BLANKS)
				{
					return this.CurrentPrice;
				}
				if (this.CurrencyType == CustomShopItemController.ShopCurrencyType.CUSTOM)
				{
					return this.CurrentPrice;
				}
				if (this.OverridePrice != null)
				{
					return this.OverridePrice.Value;
				}
				if (this.PrecludeAllDiscounts)
				{
					return this.CurrentPrice;
				}
				float num3 = GameManager.Instance.PrimaryPlayer.stats.GetStatValue(PlayerStats.StatType.GlobalPriceMultiplier);
				if (GameManager.Instance.CurrentGameType == GameManager.GameType.COOP_2_PLAYER && GameManager.Instance.SecondaryPlayer)
				{
					num3 *= GameManager.Instance.SecondaryPlayer.stats.GetStatValue(PlayerStats.StatType.GlobalPriceMultiplier);
				}
				GameLevelDefinition lastLoadedLevelDefinition = GameManager.Instance.GetLastLoadedLevelDefinition();
				float num4 = (lastLoadedLevelDefinition == null) ? 1f : lastLoadedLevelDefinition.priceMultiplier;
				float num5 = 1f;
				if (this.m_baseParentShop != null && this.m_baseParentShop.ShopCostModifier != 1f)
				{
					num5 *= this.m_baseParentShop.ShopCostModifier;
				}
				if (this.m_baseParentShop.GetAbsoluteParentRoom().area.PrototypeRoomName.Contains("Black Market"))
				{
					num5 *= 0.5f;
				}
				return Mathf.RoundToInt((float)this.CurrentPrice * num3 * num4 * num5);
			}
		}
		public new bool Acquired
		{
			get
			{
				return this.pickedUp;
			}
		}
		public void Initialize(PickupObject i, CustomShopController parent)
		{
			this.m_baseParentShop = parent;
			this.InitializeInternal(i);
			if (parent.baseShopType != BaseShopController.AdditionalShopType.NONE)
			{
				base.sprite.depthUsesTrimmedBounds = true;
				base.sprite.HeightOffGround = -1.25f;
				base.sprite.UpdateZDepth();
			}
		}

		public new void InitializeInternal(PickupObject i)
		{
			this.item = i;
			if (this.item && this.item.encounterTrackable)
			{
				GameStatsManager.Instance.SingleIncrementDifferentiator(this.item.encounterTrackable);
			}

			this.CurrentPrice = this.item.PurchasePrice;
			if (this.m_baseParentShop != null && this.CurrencyType == ShopCurrencyType.KEYS)
			{
				this.CurrentPrice = 1;
				if (this.item.quality == PickupObject.ItemQuality.A)
				{
					this.CurrentPrice = 2;
				}
				if (this.item.quality == PickupObject.ItemQuality.S)
				{
					this.CurrentPrice = 3;
				}
			}
			if (this.m_baseParentShop != null && this.CurrencyType == ShopCurrencyType.BLANKS)
			{
				this.CurrentPrice = 1;
				if (this.item.quality == PickupObject.ItemQuality.A || this.item.quality == PickupObject.ItemQuality.S)
				{
					this.CurrentPrice = 2;
				}
			}
			if (this.m_baseParentShop != null && this.CurrencyType == ShopCurrencyType.CUSTOM)
			{
				this.CurrentPrice = 1;
				if (customPrice != null)
				{
					this.CurrentPrice = customPrice(this.m_baseParentShop, this, this.item);
				}
				else
				{
					ETGModConsole.Log("CustomPrice is sadly null please look into this!");
				}
			}

			base.gameObject.AddComponent<tk2dSprite>();
			tk2dSprite tk2dSprite = i.GetComponent<tk2dSprite>();
			if (tk2dSprite == null)
			{
				tk2dSprite = i.GetComponentInChildren<tk2dSprite>();
			}
			base.sprite.SetSprite(tk2dSprite.Collection, tk2dSprite.spriteId);
			base.sprite.IsPerpendicular = true;
			if (this.UseOmnidirectionalItemFacing)
			{
				base.sprite.IsPerpendicular = false;
			}
			base.sprite.HeightOffGround = 1f;
			this.UseOmnidirectionalItemFacing = true;
			base.sprite.PlaceAtPositionByAnchor(base.transform.parent.position, tk2dBaseSprite.Anchor.MiddleCenter);
			base.sprite.transform.position = base.sprite.transform.position.Quantize(0.0625f);
			DepthLookupManager.ProcessRenderer(base.sprite.renderer);
			tk2dSprite componentInParent = base.transform.parent.gameObject.GetComponentInParent<tk2dSprite>();
			if (componentInParent != null)
			{
				componentInParent.AttachRenderer(base.sprite);
			}
			SpriteOutlineManager.AddOutlineToSprite(base.sprite, Color.black, 0.1f, 0.05f, SpriteOutlineManager.OutlineType.NORMAL);
			GameObject gameObject = null;
			if (this.m_parentShop != null && this.m_parentShop.shopItemShadowPrefab != null)
			{
				gameObject = this.m_parentShop.shopItemShadowPrefab;
			}
			if (this.m_baseParentShop != null && this.m_baseParentShop.shopItemShadowPrefab != null)
			{
				gameObject = this.m_baseParentShop.shopItemShadowPrefab;
			}
			if (gameObject != null)
			{
				if (!this.m_shadowObject)
				{
					this.m_shadowObject = UnityEngine.Object.Instantiate<GameObject>(gameObject);
				}
				tk2dBaseSprite component = this.m_shadowObject.GetComponent<tk2dBaseSprite>();
				component.PlaceAtPositionByAnchor(base.sprite.WorldBottomCenter, tk2dBaseSprite.Anchor.MiddleCenter);
				component.transform.position = component.transform.position.Quantize(0.0625f);
				base.sprite.AttachRenderer(component);
				component.transform.parent = base.sprite.transform;
				component.HeightOffGround = -0.5f;
			}
			base.sprite.UpdateZDepth();
			SpeculativeRigidbody orAddComponent = base.gameObject.GetOrAddComponent<SpeculativeRigidbody>();
			orAddComponent.PixelColliders = new List<PixelCollider>();
			PixelCollider pixelCollider = new PixelCollider
			{
				ColliderGenerationMode = PixelCollider.PixelColliderGeneration.Circle,
				CollisionLayer = CollisionLayer.HighObstacle,
				ManualDiameter = 14
			};
			Vector2 vector = base.sprite.WorldCenter - base.transform.position.XY();
			pixelCollider.ManualOffsetX = PhysicsEngine.UnitToPixel(vector.x) - 7;
			pixelCollider.ManualOffsetY = PhysicsEngine.UnitToPixel(vector.y) - 7;
			orAddComponent.PixelColliders.Add(pixelCollider);
			orAddComponent.Initialize();
			orAddComponent.OnPreRigidbodyCollision = null;
			SpeculativeRigidbody speculativeRigidbody = orAddComponent;
			speculativeRigidbody.OnPreRigidbodyCollision = (SpeculativeRigidbody.OnPreRigidbodyCollisionDelegate)Delegate.Combine(speculativeRigidbody.OnPreRigidbodyCollision, new SpeculativeRigidbody.OnPreRigidbodyCollisionDelegate(this.ItemOnPreRigidbodyCollision));
			base.RegenerateCache();
			if (!GameManager.Instance.IsFoyer && this.item is Gun && GameManager.Instance.PrimaryPlayer.CharacterUsesRandomGuns)
			{
				this.ForceOutOfStock();
			}
		}
		private new void ItemOnPreRigidbodyCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
		{
			if (!otherRigidbody || otherRigidbody.PrimaryPixelCollider == null || otherRigidbody.PrimaryPixelCollider.CollisionLayer != CollisionLayer.Projectile)
			{
				PhysicsEngine.SkipCollision = true;
			}
		}
		private new void Update()
		{
			if (this.m_baseParentShop && this.m_baseParentShop.baseShopType == BaseShopController.AdditionalShopType.CURSE && !this.pickedUp && base.sprite)
			{
				PickupObject.HandlePickupCurseParticles(base.sprite, 1f);
			}
		}

		public new void OnEnteredRange(PlayerController interactor)
		{
			if (!this)
			{
				return;
			}
			SpriteOutlineManager.RemoveOutlineFromSprite(base.sprite, false);
			SpriteOutlineManager.AddOutlineToSprite(base.sprite, Color.white);
			Vector3 offset = new Vector3(base.sprite.GetBounds().max.x + 0.1875f, base.sprite.GetBounds().min.y, 0f);
			EncounterTrackable component = this.item.GetComponent<EncounterTrackable>();
			string arg = (!(component != null)) ? this.item.DisplayName : component.journalData.GetPrimaryDisplayName(false);
			string text = this.ModifiedPrice.ToString();
			if (this.m_baseParentShop != null)
			{


				if (this.CurrencyType == CustomShopItemController.ShopCurrencyType.META_CURRENCY)
				{
					text += "[sprite \"hbux_text_icon\"]";
				}
				else if (this.CurrencyType == CustomShopItemController.ShopCurrencyType.COINS)
				{
					text += "[sprite \"ui_coin\"]";
				}
				else if (this.CurrencyType == CustomShopItemController.ShopCurrencyType.KEYS)
				{
					text += "[sprite \"ui_key\"]";
				}
				else if (this.CurrencyType == CustomShopItemController.ShopCurrencyType.BLANKS)
				{
					text += "[sprite \"ui_blank\"]";
				}
				else if (this.CurrencyType == CustomShopItemController.ShopCurrencyType.CUSTOM)
				{
					text += "[sprite \"" + customPriceSprite + "\"]";
				}
				else
				{
					text += "[sprite \"ui_coin\"]";
				}
			}
			string text2;
			if ((this.m_baseParentShop && (this.m_baseParentShop.IsCapableOfBeingStolenFrom) || interactor.IsCapableOfStealing) && this.m_baseParentShop.canBeRobbed)
			{
				text2 = string.Format("[color red]{0}: {1} {2}[/color]", arg, text, StringTableManager.GetString("#STEAL"));
			}
			else
			{
				text2 = string.Format("{0}: {1}", arg, text);
			}
			GameObject gameObject = GameUIRoot.Instance.RegisterDefaultLabel(base.transform, offset, text2);
			dfLabel componentInChildren = gameObject.GetComponentInChildren<dfLabel>();
			componentInChildren.ColorizeSymbols = false;
			componentInChildren.ProcessMarkup = true;


		}

		public new void OnExitRange(PlayerController interactor)
		{
			if (!this)
			{
				return;
			}
			SpriteOutlineManager.RemoveOutlineFromSprite(base.sprite, false);
			SpriteOutlineManager.AddOutlineToSprite(base.sprite, Color.black, 0.1f, 0.05f, SpriteOutlineManager.OutlineType.NORMAL);
			GameUIRoot.Instance.DeregisterDefaultLabel(base.transform);
		}
		public new float GetDistanceToPoint(Vector2 point)
		{
			if (!this)
			{
				return 1000f;
			}
			if (base.Locked)
			{
				return 1000f;
			}
			if (this.UseOmnidirectionalItemFacing)
			{
				Bounds bounds = base.sprite.GetBounds();
				return BraveMathCollege.DistToRectangle(point, bounds.min + base.transform.position, bounds.size);
			}
			if (this.itemFacing == DungeonData.Direction.EAST)
			{
				Bounds bounds2 = base.sprite.GetBounds();
				bounds2.SetMinMax(bounds2.min + base.transform.position, bounds2.max + base.transform.position);
				Vector2 vector = bounds2.center.XY();
				float num = vector.x - point.x;
				float num2 = Mathf.Abs(point.y - vector.y);
				if (num > 0f)
				{
					return 1000f;
				}
				if (num < -this.THRESHOLD_CUTOFF_PRIMARY)
				{
					return 1000f;
				}
				if (num2 > this.THRESHOLD_CUTOFF_SECONDARY)
				{
					return 1000f;
				}
				return num2;
			}
			else if (this.itemFacing == DungeonData.Direction.NORTH)
			{
				Bounds bounds3 = base.sprite.GetBounds();
				bounds3.SetMinMax(bounds3.min + base.transform.position, bounds3.max + base.transform.position);
				Vector2 vector2 = bounds3.center.XY();
				float num3 = Mathf.Abs(point.x - vector2.x);
				float num4 = vector2.y - point.y;
				if (num4 > bounds3.extents.y)
				{
					return 1000f;
				}
				if (num4 < -this.THRESHOLD_CUTOFF_PRIMARY)
				{
					return 1000f;
				}
				if (num3 > this.THRESHOLD_CUTOFF_SECONDARY)
				{
					return 1000f;
				}
				return num3;
			}
			else if (this.itemFacing == DungeonData.Direction.WEST)
			{
				Bounds bounds4 = base.sprite.GetBounds();
				bounds4.SetMinMax(bounds4.min + base.transform.position, bounds4.max + base.transform.position);
				Vector2 vector3 = bounds4.center.XY();
				float num5 = vector3.x - point.x;
				float num6 = Mathf.Abs(point.y - vector3.y);
				if (num5 < 0f)
				{
					return 1000f;
				}
				if (num5 > this.THRESHOLD_CUTOFF_PRIMARY)
				{
					return 1000f;
				}
				if (num6 > this.THRESHOLD_CUTOFF_SECONDARY)
				{
					return 1000f;
				}
				return num6;
			}
			else
			{
				Bounds bounds5 = base.sprite.GetBounds();
				bounds5.SetMinMax(bounds5.min + base.transform.position, bounds5.max + base.transform.position);
				Vector2 vector4 = bounds5.center.XY();
				float num7 = Mathf.Abs(point.x - vector4.x);
				float num8 = vector4.y - point.y;
				if (num8 < bounds5.extents.y)
				{
					return 1000f;
				}
				if (num8 > this.THRESHOLD_CUTOFF_PRIMARY)
				{
					return 1000f;
				}
				if (num7 > this.THRESHOLD_CUTOFF_SECONDARY)
				{
					return 1000f;
				}
				return num7;
			}
		}
		public new float GetOverrideMaxDistance()
		{
			return -1f;
		}

		private new bool ShouldSteal(PlayerController player)
		{
			return GameManager.Instance.CurrentLevelOverrideState != GameManager.LevelOverrideState.FOYER && ((this.m_baseParentShop.IsCapableOfBeingStolenFrom) || player.IsCapableOfStealing) && this.m_baseParentShop.canBeRobbed;
		}

		public new void Interact(PlayerController player)
		{
			if (this.item && this.item is HealthPickup)
			{
				if ((this.item as HealthPickup).healAmount > 0f && (this.item as HealthPickup).armorAmount <= 0 && player.healthHaver.GetCurrentHealthPercentage() >= 1f)
				{
					return;
				}
			}
			else if (this.item && this.item is AmmoPickup && (player.CurrentGun == null || player.CurrentGun.ammo == player.CurrentGun.AdjustedMaxAmmo || !player.CurrentGun.CanGainAmmo || player.CurrentGun.InfiniteAmmo))
			{
				GameUIRoot.Instance.InformNeedsReload(player, new Vector3(player.specRigidbody.UnitCenter.x - player.transform.position.x, 1.25f, 0f), 1f, "#RELOAD_FULL");
				return;
			}
			this.LastInteractingPlayer = player;
			bool flag = false;
			bool flag2 = true;
			if (this.ShouldSteal(player))
			{
				flag = this.m_baseParentShop.AttemptToSteal();
				flag2 = false;
				if (!flag)
				{
					player.DidUnstealthyAction();
					this.m_baseParentShop.NotifyStealFailed();
					return;
				}
			}
			if (flag2)
			{
				bool flag3 = false;
				if (this.CurrencyType == CustomShopItemController.ShopCurrencyType.COINS)
				{
					flag3 = (player.carriedConsumables.Currency >= this.ModifiedPrice);
				}
				else if (this.CurrencyType == CustomShopItemController.ShopCurrencyType.META_CURRENCY)
				{
					int num2 = Mathf.RoundToInt(GameStatsManager.Instance.GetPlayerStatValue(TrackedStats.META_CURRENCY));
					flag3 = (num2 >= this.ModifiedPrice);
				}
				else if (this.CurrencyType == CustomShopItemController.ShopCurrencyType.KEYS)
				{
					flag3 = (player.carriedConsumables.KeyBullets >= this.ModifiedPrice);
				}
				else if (this.CurrencyType == CustomShopItemController.ShopCurrencyType.BLANKS)
				{
					flag3 = (player.Blanks >= this.ModifiedPrice);
				}
				else if (this.CurrencyType == CustomShopItemController.ShopCurrencyType.CUSTOM)
				{
					if (customCanBuy != null)
					{
						flag3 = customCanBuy(this.m_baseParentShop, player, ModifiedPrice);
					}
					else
					{
						ETGModConsole.Log("customCanBuy is sadly null please look into this!");
					}
				}
				if (!flag3)
				{
					AkSoundEngine.PostEvent("Play_OBJ_purchase_unable_01", base.gameObject);
					if (this.m_parentShop != null)
					{
						this.m_parentShop.NotifyFailedPurchase(this);
					}
					if (this.m_baseParentShop != null)
					{
						this.m_baseParentShop.NotifyFailedPurchase(this);
					}
					return;
				}
			}
			if (!this.pickedUp)
			{
				this.pickedUp = !this.item.PersistsOnPurchase;
				LootEngine.GivePrefabToPlayer(this.item.gameObject, player);
				if (flag2)
				{
					if (this.CurrencyType == CustomShopItemController.ShopCurrencyType.COINS)
					{
						player.carriedConsumables.Currency -= this.ModifiedPrice;
					}
					else if (this.CurrencyType == CustomShopItemController.ShopCurrencyType.KEYS)
					{
						player.carriedConsumables.KeyBullets -= this.ModifiedPrice;
					}
					else if (this.CurrencyType == CustomShopItemController.ShopCurrencyType.BLANKS)
					{
						player.Blanks -= this.ModifiedPrice;
					}
					else if (this.CurrencyType == CustomShopItemController.ShopCurrencyType.META_CURRENCY)
					{
						int num2 = Mathf.RoundToInt(GameStatsManager.Instance.GetPlayerStatValue(TrackedStats.META_CURRENCY));
						GameStatsManager.Instance.ClearStatValueGlobal(TrackedStats.META_CURRENCY);
						GameStatsManager.Instance.SetStat(TrackedStats.META_CURRENCY, (float)(num2 - this.ModifiedPrice));
					}
					else if (this.CurrencyType == CustomShopItemController.ShopCurrencyType.CUSTOM)
					{
						if (removeCurrency != null)
						{
							removeCurrency(this.m_baseParentShop, player, ModifiedPrice);
						}
						else
						{
							ETGModConsole.Log("removeCurrency is sadly null please look into this!");
						}
					}
					if (OnPurchase != null)
					{
						OnPurchase(player, this.item, this.ModifiedPrice);

					}
				}
				if (this.m_baseParentShop != null)
				{
					this.m_baseParentShop.PurchaseItem(this, !flag, true);
				}
				if (flag)
				{
					StatModifier statModifier = new StatModifier();
					statModifier.statToBoost = PlayerStats.StatType.Curse;
					statModifier.amount = 1f;
					statModifier.modifyType = StatModifier.ModifyMethod.ADDITIVE;
					player.ownerlessStatModifiers.Add(statModifier);
					player.stats.RecalculateStats(player, false, false);
					player.HandleItemStolen(this);
					this.m_baseParentShop.NotifyStealSucceeded();
					player.IsThief = true;
					GameStatsManager.Instance.RegisterStatChange(TrackedStats.MERCHANT_ITEMS_STOLEN, 1f);
					if (this.SetsFlagOnSteal)
					{
						GameStatsManager.Instance.SetFlag(this.FlagToSetOnSteal, true);
					}
					if (OnSteal != null)
					{
						OnSteal(player, this.item, this.ModifiedPrice);

					}
				}
				else
				{
					player.HandleItemPurchased(this);
				}
				if (!this.item.PersistsOnPurchase)
				{
					GameUIRoot.Instance.DeregisterDefaultLabel(base.transform);
				}
				AkSoundEngine.PostEvent("Play_OBJ_item_purchase_01", base.gameObject);
			}
		}

		public new void ForceSteal(PlayerController player)
		{
			this.pickedUp = true;
			LootEngine.GivePrefabToPlayer(this.item.gameObject, player);
			if (this.m_parentShop != null)
			{
				this.m_parentShop.PurchaseItem(this, false, false);
			}
			if (this.m_baseParentShop != null)
			{
				this.m_baseParentShop.PurchaseItem(this, false, false);
			}
			StatModifier statModifier = new StatModifier();
			statModifier.statToBoost = PlayerStats.StatType.Curse;
			statModifier.amount = 1f;
			statModifier.modifyType = StatModifier.ModifyMethod.ADDITIVE;
			player.ownerlessStatModifiers.Add(statModifier);
			player.stats.RecalculateStats(player, false, false);
			player.HandleItemStolen(this);
			this.m_baseParentShop.NotifyStealSucceeded();
			player.IsThief = true;
			GameStatsManager.Instance.RegisterStatChange(TrackedStats.MERCHANT_ITEMS_STOLEN, 1f);
			if (!this.m_baseParentShop.AttemptToSteal())
			{
				player.DidUnstealthyAction();
				this.m_baseParentShop.NotifyStealFailed();
			}
			GameUIRoot.Instance.DeregisterDefaultLabel(base.transform);
			AkSoundEngine.PostEvent("Play_OBJ_item_purchase_01", base.gameObject);
		}

		public new void ForceOutOfStock()
		{
			this.pickedUp = true;
			if (this.m_parentShop != null)
			{
				this.m_parentShop.PurchaseItem(this, false, true);
			}
			if (this.m_baseParentShop != null)
			{
				this.m_baseParentShop.PurchaseItem(this, false, true);
			}
			GameUIRoot.Instance.DeregisterDefaultLabel(base.transform);
		}

		public new string GetAnimationState(PlayerController interactor, out bool shouldBeFlipped)
		{
			shouldBeFlipped = false;
			return string.Empty;
		}
		public new PickupObject item;
		public new bool UseOmnidirectionalItemFacing;
		public new DungeonData.Direction itemFacing;
		public new PlayerController LastInteractingPlayer;
		public new CustomShopItemController.ShopCurrencyType CurrencyType;
		public new bool PrecludeAllDiscounts;
		public new int CurrentPrice;
		public new int? OverridePrice;
		public new bool SetsFlagOnSteal;
		public new GungeonFlags FlagToSetOnSteal;
		public new bool IsResourcefulRatKey;

		private new bool pickedUp;
		// private new CustomShopController m_parentShop;
		private new CustomShopController m_baseParentShop;
		private new float THRESHOLD_CUTOFF_PRIMARY;
		private new float THRESHOLD_CUTOFF_SECONDARY;
		private new GameObject m_shadowObject;
		public new enum ShopCurrencyType
		{
			COINS,
			META_CURRENCY,
			KEYS,
			BLANKS,
			CUSTOM
		}
	}
}

