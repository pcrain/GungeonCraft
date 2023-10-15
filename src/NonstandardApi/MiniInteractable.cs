using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using UnityEngine;
using Dungeonator;

namespace CwaffingTheGungy
{
  public class MiniInteractable : BraveBehaviour, IPlayerInteractable
  {
    public delegate IEnumerator InteractionScript(MiniInteractable i, PlayerController p);
    public InteractionScript interactionScript = DefaultInteractionScript;

    public bool interacting = false;
    public bool doVFX = false;
    public bool doHover = false;
    public bool autoInteract = false;

    public DungeonData.Direction itemFacing = DungeonData.Direction.SOUTH;

    private VFXPool effect;
    private float _vfxTimer = 0;
    private float _hoverTimer = 0;

    public void Initialize(tk2dBaseSprite i)
    {
      InitializeInternal(i);
      base.sprite.depthUsesTrimmedBounds = true;
      base.sprite.HeightOffGround = -1.25f;
      base.sprite.UpdateZDepth();
    }

    private void InitializeInternal(tk2dBaseSprite sprite)
    {
      this.effect = (ItemHelper.Get(Items.MagicLamp) as Gun).DefaultModule.projectiles[0].hitEffects.tileMapVertical;

      base.gameObject.AddComponent<tk2dSprite>();
      base.sprite.SetSprite(sprite.Collection, sprite.spriteId);
      base.sprite.IsPerpendicular = true;
      base.sprite.HeightOffGround = 1f;
      base.sprite.PlaceAtPositionByAnchor(base.transform.parent.position, tk2dBaseSprite.Anchor.MiddleCenter);
      base.sprite.transform.position = base.sprite.transform.position.Quantize(0.0625f);
      DepthLookupManager.ProcessRenderer(base.sprite.renderer);
      tk2dSprite componentInParent = base.transform.parent.gameObject.GetComponentInParent<tk2dSprite>();
      componentInParent?.AttachRenderer(base.sprite);
      // SpriteOutlineManager.AddOutlineToSprite(base.sprite, Color.black, 0.1f, 0.05f);
      SpriteOutlineManager.AddOutlineToSprite(base.sprite, Color.black);
      base.sprite.UpdateZDepth();

      SpeculativeRigidbody orAddComponent = base.gameObject.GetOrAddComponent<SpeculativeRigidbody>();
      orAddComponent.CollideWithOthers = false;
      orAddComponent.CollideWithTileMap = false;
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
      orAddComponent.OnPreRigidbodyCollision = (SpeculativeRigidbody.OnPreRigidbodyCollisionDelegate)Delegate.Combine(orAddComponent.OnPreRigidbodyCollision, new SpeculativeRigidbody.OnPreRigidbodyCollisionDelegate(ItemOnPreRigidbodyCollision));
      RegenerateCache();
    }

    private void ItemOnPreRigidbodyCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
    {
      // if (otherRigidbody?.PrimaryPixelCollider?.CollisionLayer != CollisionLayer.Projectile)
        PhysicsEngine.SkipCollision = true;
    }

    private void Update()
    {
      // PickupObject.HandlePickupCurseParticles(base.sprite, 1f);
      if (doVFX)
        UpdateVFX();
      if (doHover)
        UpdateHover();
    }

    private void UpdateVFX()
    {
      const float RADIUS      = 1.3f;
      const int NUM_VFX       = 10;
      const float ANGLE_DELTA = 360f / NUM_VFX;
      const float VFX_FREQ    = 0.4f;

      _vfxTimer += BraveTime.DeltaTime;
      if (_vfxTimer < VFX_FREQ)
        return;

      _vfxTimer = 0;
      for (int i = 0; i < NUM_VFX; ++i)
      {
        Vector2 ppos = base.sprite.WorldCenter + BraveMathCollege.DegreesToVector(i*ANGLE_DELTA,RADIUS);
        effect.SpawnAtPosition(ppos.ToVector3ZisY(1f), 0, null, null, null, -0.05f);
      }
    }

    private void UpdateHover()
    {
      _hoverTimer += BraveTime.DeltaTime;
      base.sprite.transform.localPosition = Vector3.zero.WithY(Mathf.CeilToInt(2f*Mathf.Sin(4f*_hoverTimer))/C.PIXELS_PER_TILE);
    }

    public override void OnDestroy()
    {
      if (RoomHandler.unassignedInteractableObjects.Contains(this))
          RoomHandler.unassignedInteractableObjects.Remove(this);
      base.OnDestroy();
    }

    public void OnEnteredRange(PlayerController interactor)
    {
      if (!this)
        return;
      SpriteOutlineManager.RemoveOutlineFromSprite(base.sprite);
      SpriteOutlineManager.AddOutlineToSprite(base.sprite, Color.white);
      if (this.autoInteract)
        Interact(interactor);
    }

    public void OnExitRange(PlayerController interactor)
    {
      if (!this)
        return;
      SpriteOutlineManager.RemoveOutlineFromSprite(base.sprite);
      SpriteOutlineManager.AddOutlineToSprite(base.sprite, Color.black, 0.1f, 0.05f);
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
    }

    public float GetOverrideMaxDistance()
    {
      return -1f;
    }

    public void Interact(PlayerController player)
    {
      if (interacting)
        return;
      interacting = true;
      player.StartCoroutine(interactionScript(this,player));
    }

    public static IEnumerator DefaultInteractionScript(MiniInteractable i, PlayerController p)
    {
      ETGModConsole.Log("interacted! now override this and actually do something o:");
      i.interacting = false;
      yield break;
    }

    public string GetAnimationState(PlayerController interactor, out bool shouldBeFlipped)
    {
      shouldBeFlipped = false;
      return string.Empty;
    }

    public static MiniInteractable CreateInteractableAtPosition(tk2dBaseSprite sprite, Vector2 position, InteractionScript iscript = null)
    {
        GameObject iPos = new GameObject("Mini interactible position test");
            iPos.transform.position = position.ToVector3ZisY();
        GameObject iMini = new GameObject("Mini interactible test");
            iMini.transform.parent        = iPos.transform;
            iMini.transform.localPosition = Vector3.zero;
            iMini.transform.position      = Vector3.zero;
        MiniInteractable mini = iMini.AddComponent<MiniInteractable>();
            // NOTE: the below transform position absolutely has to be linked to a game object
            if (!RoomHandler.unassignedInteractableObjects.Contains(mini))
                RoomHandler.unassignedInteractableObjects.Add(mini);
            mini.interactionScript = iscript ?? DefaultInteractionScript;
            mini.Initialize(sprite);
        return mini;
    }
  }
}
