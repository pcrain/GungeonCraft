using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Gungeon;
using Dungeonator;
using SaveAPI;
using System.Collections;
using NpcApi;

using GungeonAPI;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using ItemAPI;
using System.Reflection;
using static NpcApi.CustomShopController;

public class FakeShopItem : BraveBehaviour, IPlayerInteractable
{
  public PickupObject item;

  public bool UseOmnidirectionalItemFacing;

  public DungeonData.Direction itemFacing = DungeonData.Direction.SOUTH;

  [NonSerialized]
  public PlayerController LastInteractingPlayer;

  public int CurrentPrice = -1;

  [NonSerialized]
  public int? OverridePrice;

  private bool pickedUp;

  private float THRESHOLD_CUTOFF_PRIMARY = 3f;

  private float THRESHOLD_CUTOFF_SECONDARY = 2f;

  public bool Locked { get; set; }

  public int ModifiedPrice
  {
    get
    {
      return CurrentPrice;
    }
  }

  public bool Acquired
  {
    get
    {
      return pickedUp;
    }
  }

  public void Initialize(PickupObject i)
  {
    InitializeInternal(i);
    base.sprite.depthUsesTrimmedBounds = true;
    base.sprite.HeightOffGround = -1.25f;
    base.sprite.UpdateZDepth();
  }

  public void Initialize(PickupObject i, BaseShopController parent)
  {
    InitializeInternal(i);
    base.sprite.depthUsesTrimmedBounds = true;
    base.sprite.HeightOffGround = -1.25f;
    base.sprite.UpdateZDepth();
  }

  public void Initialize(PickupObject i, ShopController parent)
  {
    InitializeInternal(i);
  }

  private void InitializeInternal(PickupObject i)
  {
    item = i;
    CurrentPrice = item.PurchasePrice;
    base.gameObject.AddComponent<tk2dSprite>();
    tk2dSprite tk2dSprite2 = i.GetComponent<tk2dSprite>();
    if (tk2dSprite2 == null)
      tk2dSprite2 = i.GetComponentInChildren<tk2dSprite>();
    base.sprite.SetSprite(tk2dSprite2.Collection, tk2dSprite2.spriteId);
    base.sprite.IsPerpendicular = !UseOmnidirectionalItemFacing;
    base.sprite.HeightOffGround = 1f;
    base.sprite.PlaceAtPositionByAnchor(base.transform.parent.position, tk2dBaseSprite.Anchor.MiddleCenter);
    ETGModConsole.Log("place at "+base.transform.parent.position);
    base.sprite.transform.position = base.sprite.transform.position.Quantize(0.0625f);
    DepthLookupManager.ProcessRenderer(base.sprite.renderer);
    tk2dSprite componentInParent = base.transform.parent.gameObject.GetComponentInParent<tk2dSprite>();
    if (componentInParent != null)
    {
      componentInParent.AttachRenderer(base.sprite);
    }
    SpriteOutlineManager.AddOutlineToSprite(base.sprite, Color.black, 0.1f, 0.05f);
    base.sprite.UpdateZDepth();

    SpeculativeRigidbody orAddComponent = base.gameObject.GetOrAddComponent<SpeculativeRigidbody>();
    orAddComponent.PixelColliders = new List<PixelCollider>();
    PixelCollider pixelCollider = new PixelCollider();
    pixelCollider.ColliderGenerationMode = PixelCollider.PixelColliderGeneration.Circle;
    pixelCollider.CollisionLayer = CollisionLayer.HighObstacle;
    pixelCollider.ManualDiameter = 14;
    Vector2 vector = base.sprite.WorldCenter - base.transform.position.XY();
    pixelCollider.ManualOffsetX = PhysicsEngine.UnitToPixel(vector.x) - 7;
    pixelCollider.ManualOffsetY = PhysicsEngine.UnitToPixel(vector.y) - 7;
    orAddComponent.PixelColliders.Add(pixelCollider);
    orAddComponent.Initialize();
    orAddComponent.OnPreRigidbodyCollision = null;
    orAddComponent.OnPreRigidbodyCollision = (SpeculativeRigidbody.OnPreRigidbodyCollisionDelegate)Delegate.Combine(orAddComponent.OnPreRigidbodyCollision, new SpeculativeRigidbody.OnPreRigidbodyCollisionDelegate(ItemOnPreRigidbodyCollision));
    RegenerateCache();
  }

  private void ItemOnPreRigidbodyCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
  {
    if (!otherRigidbody || otherRigidbody.PrimaryPixelCollider == null || otherRigidbody.PrimaryPixelCollider.CollisionLayer != CollisionLayer.Projectile)
    {
      PhysicsEngine.SkipCollision = true;
    }
  }

  private void Update()
  {
    // if ((bool)m_baseParentShop && m_baseParentShop.baseShopType == BaseShopController.AdditionalShopType.CURSE && !pickedUp && (bool)base.sprite)
    // {
      PickupObject.HandlePickupCurseParticles(base.sprite, 1f);
    // }
  }

  public override void OnDestroy()
  {
    base.OnDestroy();
  }

  public void OnEnteredRange(PlayerController interactor)
  {
    if (!this)
      return;
    SpriteOutlineManager.RemoveOutlineFromSprite(base.sprite);
    SpriteOutlineManager.AddOutlineToSprite(base.sprite, Color.white);
    Vector3 offset = new Vector3(base.sprite.GetBounds().max.x + 0.1875f, base.sprite.GetBounds().min.y, 0f);
    // EncounterTrackable component = item.GetComponent<EncounterTrackable>();
    // string name = (component == null) ? item.DisplayName : component.journalData.GetPrimaryDisplayName();
    string name = item.DisplayName;
    string price = ModifiedPrice.ToString() + "[sprite \"ui_coin\"]";
    string label = string.Format("[color red]{0}: {1}[/color]", name, price);
    GameObject gameObject = GameUIRoot.Instance.RegisterDefaultLabel(base.transform, offset, label);
    dfLabel componentInChildren = gameObject.GetComponentInChildren<dfLabel>();
    componentInChildren.ColorizeSymbols = false;
    componentInChildren.ProcessMarkup = true;
  }

  public void OnExitRange(PlayerController interactor)
  {
    ETGModConsole.Log("almost exited range S:");
    if (!this)
      return;
    ETGModConsole.Log("  exited range D:");
    SpriteOutlineManager.RemoveOutlineFromSprite(base.sprite);
    SpriteOutlineManager.AddOutlineToSprite(base.sprite, Color.black, 0.1f, 0.05f);
    GameUIRoot.Instance.DeregisterDefaultLabel(base.transform);
  }

  public float GetDistanceToPoint(Vector2 point)
  {
    if (!this)
    {
      return 1000f;
    }
    if (base.sprite == null)
        return 100f;
    Vector3 v = BraveMathCollege.ClosestPointOnRectangle(point, base.specRigidbody.UnitBottomLeft, base.specRigidbody.UnitDimensions);
    return Vector2.Distance(point, v) / 1.5f;
    // if (Locked)
    // {
    //   return 1000f;
    // }
    // if (UseOmnidirectionalItemFacing)
    // {
    //   Bounds bounds = base.sprite.GetBounds();
    //   return BraveMathCollege.DistToRectangle(point, bounds.min + base.transform.position, bounds.size);
    // }
    // if (itemFacing == DungeonData.Direction.EAST)
    // {
    //   Bounds bounds2 = base.sprite.GetBounds();
    //   bounds2.SetMinMax(bounds2.min + base.transform.position, bounds2.max + base.transform.position);
    //   Vector2 vector = bounds2.center.XY();
    //   float num = vector.x - point.x;
    //   float num2 = Mathf.Abs(point.y - vector.y);
    //   if (num > 0f)
    //   {
    //     return 1000f;
    //   }
    //   if (num < 0f - THRESHOLD_CUTOFF_PRIMARY)
    //   {
    //     return 1000f;
    //   }
    //   if (num2 > THRESHOLD_CUTOFF_SECONDARY)
    //   {
    //     return 1000f;
    //   }
    //   return num2;
    // }
    // if (itemFacing == DungeonData.Direction.NORTH)
    // {
    //   Bounds bounds3 = base.sprite.GetBounds();
    //   bounds3.SetMinMax(bounds3.min + base.transform.position, bounds3.max + base.transform.position);
    //   Vector2 vector2 = bounds3.center.XY();
    //   float num3 = Mathf.Abs(point.x - vector2.x);
    //   float num4 = vector2.y - point.y;
    //   if (num4 > bounds3.extents.y)
    //   {
    //     return 1000f;
    //   }
    //   if (num4 < 0f - THRESHOLD_CUTOFF_PRIMARY)
    //   {
    //     return 1000f;
    //   }
    //   if (num3 > THRESHOLD_CUTOFF_SECONDARY)
    //   {
    //     return 1000f;
    //   }
    //   return num3;
    // }
    // if (itemFacing == DungeonData.Direction.WEST)
    // {
    //   Bounds bounds4 = base.sprite.GetBounds();
    //   bounds4.SetMinMax(bounds4.min + base.transform.position, bounds4.max + base.transform.position);
    //   Vector2 vector3 = bounds4.center.XY();
    //   float num5 = vector3.x - point.x;
    //   float num6 = Mathf.Abs(point.y - vector3.y);
    //   if (num5 < 0f)
    //   {
    //     return 1000f;
    //   }
    //   if (num5 > THRESHOLD_CUTOFF_PRIMARY)
    //   {
    //     return 1000f;
    //   }
    //   if (num6 > THRESHOLD_CUTOFF_SECONDARY)
    //   {
    //     return 1000f;
    //   }
    //   return num6;
    // }
    // Bounds bounds5 = base.sprite.GetBounds();
    // bounds5.SetMinMax(bounds5.min + base.transform.position, bounds5.max + base.transform.position);
    // Vector2 vector4 = bounds5.center.XY();
    // float num7 = Mathf.Abs(point.x - vector4.x);
    // float num8 = vector4.y - point.y;
    // if (num8 < bounds5.extents.y)
    // {
    //   return 1000f;
    // }
    // if (num8 > THRESHOLD_CUTOFF_PRIMARY)
    // {
    //   return 1000f;
    // }
    // if (num7 > THRESHOLD_CUTOFF_SECONDARY)
    // {
    //   return 1000f;
    // }
    // return num7;
  }

  public float GetOverrideMaxDistance()
  {
    return -1f;
  }

  public void Interact(PlayerController player)
  {
    ETGModConsole.Log("interact success :D");
    LastInteractingPlayer = player;
    pickedUp = true; //TODO: remove later
    if (pickedUp)
      return;
    pickedUp = true;
    LootEngine.GivePrefabToPlayer(item.gameObject, player);
    // TODO: apply debuffs
    // player.HandleItemPurchased(this);  //not applicable
    GameUIRoot.Instance.DeregisterDefaultLabel(base.transform);
    AkSoundEngine.PostEvent("Play_OBJ_item_purchase_01", base.gameObject);
  }

  public string GetAnimationState(PlayerController interactor, out bool shouldBeFlipped)
  {
    shouldBeFlipped = false;
    return string.Empty;
  }
}