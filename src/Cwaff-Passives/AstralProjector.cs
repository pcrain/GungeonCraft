namespace CwaffingTheGungy;

public class AstralProjector : PassiveItem
{
    public static string ItemName         = "Astral Projector";
    public static string SpritePath       = "astral_projector_icon";
    public static string ShortDescription = "Enter the Gungeon's Walls";
    public static string LongDescription  = $"Allows phasing through the inner walls of most rooms. Can not shoot, reload, teleport, or use items while phased.\n\nCreated after Bello accidentally dropped a run-of-the-mill projector into one of the cosmic rifts scattered throughout the Gungeon, the {ItemName} is now completely useless for its original purpose of displaying HD videos in decidedly non-HD quality on the walls of Bello's shop. Bello is still searching for a good projector and prefers not to talk about the whole incident.";

    private const float _UNPHASE_TIMER = 0.5f; // delay between exiting a wall and being able to perform non-movement actions again

    private bool _phased = false;
    private bool _intangible = false;
    private int _insideWalls = 0;
    private float _intangibleTimer = 0.0f;
    private Shader _originalShader;
    private RoomHandler _phasedRoom;

    private static int _AstralProjectorId;
    // private static Hook astralProjectorHook;
    private static ILHook _AstralProjectorILHook;

    public static void Init()
    {
        PickupObject item  = Lazy.SetupPassive<AstralProjector>(ItemName, SpritePath, ShortDescription, LongDescription);
        item.quality       = PickupObject.ItemQuality.A;
        item.AddToSubShop(ModdedShopType.TimeTrader);

        _AstralProjectorId   = item.PickupObjectId;
        // astralProjectorHook = new Hook(
        //     typeof(PlayerController).GetMethod("HandlePlayerInput", BindingFlags.Instance | BindingFlags.NonPublic),
        //     typeof(AstralProjector).GetMethod("HandlePlayerPhasingInput", BindingFlags.Static | BindingFlags.NonPublic)
        //     );
        _AstralProjectorILHook = new ILHook(
            typeof(PlayerController).GetMethod("HandlePlayerInput", BindingFlags.Instance | BindingFlags.NonPublic),
            HandlePlayerPhasingInputIL
            );
    }

    public override void Pickup(PlayerController player)
    {
        base.Pickup(player);
        player.specRigidbody.OnPreTileCollision += this.OnPreTileCollision;
    }

    public override DebrisObject Drop(PlayerController player)
    {
        player.specRigidbody.OnPreTileCollision -= this.OnPreTileCollision;
        return base.Drop(player);
    }

    private static PrototypeDungeonRoom.RoomCategory[] _BannedRoomTypes = {
        PrototypeDungeonRoom.RoomCategory.BOSS,
        PrototypeDungeonRoom.RoomCategory.CONNECTOR,
        PrototypeDungeonRoom.RoomCategory.ENTRANCE,
        PrototypeDungeonRoom.RoomCategory.EXIT,
    };

    private bool CanStartPhase()
    {
        if (!this.Owner)
            return false; // can't phase if we're not owner
        if (this.Owner.CurrentInputState != PlayerInputState.AllInput && !this._intangible)
            return false; // can't phase if we're not fully mobile, unless we're in our intangible phase
        if (_BannedRoomTypes.Contains(this.Owner.CurrentRoom.area.PrototypeRoomCategory))
            return false; // can only phase in normal rooms
        if (this.Owner.CurrentRoom.area.IsProceduralRoom || (this.Owner.CurrentRoom.area.proceduralCells?.Count ?? 0) > 0)
            return false; // can only phase in non-procedural rooms
        return true;
    }

    public override void Update()
    {
        base.Update();
        if (!this.Owner)
            return;

        this.CanBeDropped = !(this._intangible || this._phased);

        if (this._intangibleTimer > 0)
        {
            this._intangibleTimer -= BraveTime.DeltaTime;
            if (this._intangibleTimer <= 0f)
                BecomeTangible();
        }

        if (!this._phased)
            return;

        if (this.Owner.ForceConstrainToRoom(this._phasedRoom))
            return;

        if (--this._insideWalls == 0)
            this._phased = false;
    }

    private void OnPreTileCollision(SpeculativeRigidbody me, PixelCollider myPixelCollider, PhysicsEngine.Tile other, PixelCollider otherPixelCollider)
    {
        RoomHandler targetRoom = this._phased ? this._phasedRoom : this.Owner.CurrentRoom;
        if (this._phased)
            this.Owner.ForceConstrainToRoom(targetRoom);
        else if (!CanStartPhase())
            return; // can't phase here
        else if (!this.Owner.FullyWithinRoom(targetRoom))
            return; // outside the room

        this._phasedRoom = this.Owner.CurrentRoom;
        this._insideWalls = 2;  // 2 frames of leniency for checking if we're inside walls
        this._intangibleTimer = _UNPHASE_TIMER;
        PhysicsEngine.SkipCollision = true;

        this._phased = true;
        if (!this._intangible)
            BecomeIntangible();
    }

   private void BecomeIntangible()
    {
        this._intangible = true;

        this.Owner.CurrentInputState = PlayerInputState.FoyerInputOnly;
        tk2dBaseSprite sprite = this.Owner.sprite;
        sprite.usesOverrideMaterial = true;
        this._originalShader = sprite.renderer.material.shader;
        sprite.renderer.material.shader = ShaderCache.Acquire("Brave/Internal/HologramShader");
        AkSoundEngine.PostEvent("phase_through_wall_sound_stop_all", this.Owner.gameObject);
        AkSoundEngine.PostEvent("phase_through_wall_sound", this.Owner.gameObject);
    }

    private void BecomeTangible()
    {
        this._intangible = false;

        this.Owner.CurrentInputState = PlayerInputState.AllInput;
        tk2dBaseSprite sprite = this.Owner.sprite;
        sprite.usesOverrideMaterial = true;
        sprite.renderer.material.shader = this._originalShader;
        AkSoundEngine.PostEvent("phase_through_wall_sound_stop_all", this.Owner.gameObject);
        AkSoundEngine.PostEvent("phase_through_wall_sound", this.Owner.gameObject);
    }

    /* References for using ILHooks:
        https://en.wikipedia.org/wiki/List_of_CIL_instructions
        https://github.com/StrawberryJam2021/StrawberryJam2021/blob/21079f1c2521aa704fc5ddc91f67ff3ebc95c317/Triggers/SkateboardTrigger.cs#L18
        https://github.com/lostinnowhere314/CelesteCollabUtils2/blob/b6b7fde825a6bdc218d201bf1a4feaa709487f3b/Entities/MiniHeartDoor.cs#L17
    */
    public static float PreventRigidbodyCastDuringHandlePlayerInput(PlayerController pc, float inValue)
    {
        if (pc.passiveItems.Contains(_AstralProjectorId))
            return inValue > 0 ? 999f : -999f; // replace the value we're checking against with something absurdly high so we avoid doing RigidBodyCasts
        return inValue; // return the original value
    }

    // This IL Hook replaces some checks in HandlePlayerInput that do RigidBodyCasts on each axis if the absolutely velocity is greater than 0.01
    // If we have this item, this hook instead checks if each axis has an absolute velocity greater than 999,
    //   ensuring the RigidBodyCasts will never run under any sane circumstance
    private static void HandlePlayerPhasingInputIL(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);
        // cursor.DumpILOnce("HandlePlayerPhasingInputIL");

        //Replace positive movement checks
        while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(0.01f)))
        {
            cursor.Emit(OpCodes.Pop); // pop the check for 0.01f itself
            cursor.Emit(OpCodes.Ldarg_0); // load the player instance as arg0
            cursor.Emit(OpCodes.Ldc_R4, 0.01f); // replace the check for 0.01f as arg1

            // call our method with player instance and original threshold value as args
            cursor.Emit(OpCodes.Call, typeof(AstralProjector).GetMethod("PreventRigidbodyCastDuringHandlePlayerInput"));
            // the return value from our hook is now on the stack, replacing 0.01f with 999f if we have the item
            // this ensures the RigidBodyCast() will never happen
        }

        // Replcae negative movement checks
        cursor.Index = 0;
        while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(-0.01f)))
        {
            cursor.Emit(OpCodes.Pop); // pop the check for -0.01f itself
            cursor.Emit(OpCodes.Ldarg_0); // load the player instance as arg0
            cursor.Emit(OpCodes.Ldc_R4, -0.01f); // replace the check for -0.01f as arg1

            // call our method with player instance and original threshold value as args
            cursor.Emit(OpCodes.Call, typeof(AstralProjector).GetMethod("PreventRigidbodyCastDuringHandlePlayerInput"));
            // the return value from our hook is now on the stack, replacing -0.01f with -999f if we have the item
            // this ensures the RigidBodyCast() will never happen
        }
        return;
    }

    // OBSOLETE: better ILHook method above that effectively disables the checks on the spot and saves some RigidBodyCasts
    // we have to do this nonsense because even when we're ignoring tile collisions, HandlePlayerInput insists on zeroing our movement unless we're rolling
    // private static Vector2 HandlePlayerPhasingInput(Func<PlayerController, Vector2> orig, PlayerController player)
    // {
    //     // Run the original movement function and return its output if we don't have this item
    //     Vector2 ovec = orig(player);
    //     if (!player.passiveItems.Contains(astralProjectorId))
    //         return ovec;

    //     #region Perform all of the original checks to make sure we're not doing illegal movements
    //         if (player.m_activeActions == null)
    //             return ovec; // If we have no active actions, we're not doing anything, so return
    //         if (player.CurrentInputState == PlayerInputState.NoMovement)
    //             return ovec; // AdjustInputVector never gets called if we are in the NoMovement input state, so return
    //         if (player.IsGhost)
    //             return ovec; // Original function checks if we're a ghost and returns immediately if so. Ironically, we need to respect walls if we're a ghost
    //     #endregion

    //     // If we've made it here, then only the RigidbodyCast() functions could have reset our movement vector to zero, so recalculate AdjustInputVector()
    //     Vector2 moveVector = player.AdjustInputVector(player.m_activeActions.Move.Vector, BraveInput.MagnetAngles.movementCardinal, BraveInput.MagnetAngles.movementOrdinal);
    //     if (moveVector.magnitude > 1f)
    //         moveVector.Normalize();
    //     return moveVector;
    // }
}
