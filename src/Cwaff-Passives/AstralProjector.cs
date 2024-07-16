namespace CwaffingTheGungy;

//TODO: look into cellData.isRoomInternal

public class AstralProjector : CwaffPassive
{
    public static string ItemName         = "Astral Projector";
    public static string ShortDescription = "Enter the Gungeon's Walls";
    public static string LongDescription  = "Allows phasing through the inner walls of most rooms. Can not shoot, reload, teleport, or use items while phased.";
    public static string Lore             = "Created after Bello accidentally dropped a run-of-the-mill projector into one of the cosmic rifts scattered throughout the Gungeon, the {ItemName} is now completely useless for its original purpose of displaying HD videos in decidedly non-HD quality on the walls of Bello's shop. Bello is still searching for a good projector and prefers not to talk about the whole incident.";

    private const float _UNPHASE_TIMER = 0.5f; // delay between exiting a wall and being able to perform non-movement actions again

    private bool _phased = false;
    private bool _intangible = false;
    private bool _insideWalls = false;
    private float _intangibleTimer = 0.0f;
    private Shader _originalShader;
    private RoomHandler _phasedRoom;

    private static Vector2 _LastSanePosition = Vector2.zero;

    public static void Init()
    {
        PassiveItem item   = Lazy.SetupPassive<AstralProjector>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality       = ItemQuality.A;
        item.AddToSubShop(ModdedShopType.TimeTrader);
    }

    public override void Pickup(PlayerController player)
    {
        base.Pickup(player);
        _LastSanePosition = player.specRigidbody.PrimaryPixelCollider.UnitBottomCenter;
        player.specRigidbody.OnPreTileCollision += this.OnPreTileCollision;
    }

    public override DebrisObject Drop(PlayerController player)
    {
        player.specRigidbody.OnPreTileCollision -= this.OnPreTileCollision;
        return base.Drop(player);
    }

    public override void OnDestroy()
    {
        if (this.Owner)
            this.Owner.specRigidbody.OnPreTileCollision -= this.OnPreTileCollision;
        base.OnDestroy();
    }

    private static PrototypeDungeonRoom.RoomCategory[] _BannedRoomTypes = {
        PrototypeDungeonRoom.RoomCategory.BOSS,
        PrototypeDungeonRoom.RoomCategory.CONNECTOR,
        PrototypeDungeonRoom.RoomCategory.ENTRANCE,
        PrototypeDungeonRoom.RoomCategory.EXIT,
    };

    private static bool IsBossFoyer(RoomHandler room)
    {
        for (int i = 0; i < room.connectedRooms.Count; i++)
            if (room.connectedRooms[i].area.PrototypeRoomCategory == PrototypeDungeonRoom.RoomCategory.BOSS)
                return true;
        return false;
    }

    private bool CanStartPhase()
    {
        if (!this.Owner)
            return false; // can't phase if we're not owner
        if (this.Owner.CurrentInputState != PlayerInputState.AllInput && !this._intangible)
            return false; // can't phase if we're not fully mobile, unless we're in our intangible phase
        RoomHandler room = this.Owner.CurrentRoom;
        if (_BannedRoomTypes.Contains(room.area.PrototypeRoomCategory) || IsBossFoyer(room))
            return false; // can only phase in normal rooms
        if (room.area.IsProceduralRoom || (room.area.proceduralCells != null && room.area.proceduralCells.Count > 0))
            return false; // can only phase in non-procedural rooms
        return true;
    }

    public override void Update()
    {
        base.Update();
        if (!this.Owner || BraveTime.DeltaTime == 0f)
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

        Vector2 ppos = this.Owner.transform.PositionVector2();
        Vector2 unitBottomCenter = this.Owner.specRigidbody.PrimaryPixelCollider.UnitBottomCenter;
        if (!GameManager.Instance.Dungeon.data.CheckInBoundsAndValid(unitBottomCenter.ToIntVector2(VectorConversions.Floor)))
        {
            this._intangibleTimer = _UNPHASE_TIMER;
            ppos = _LastSanePosition;
            return;
        }
        _LastSanePosition = ppos;

        if (this._insideWalls)
        {
            this._insideWalls = false;
            return;
        }
        this._phased = false;
        // this._intangibleTimer = 0; BecomeTangible(); // for testing
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

        if (!this._phased)
            this._phasedRoom = this.Owner.CurrentRoom;
        this._insideWalls = true;  // 2 frames of leniency for checking if we're inside walls
        this._intangibleTimer = _UNPHASE_TIMER;
        PhysicsEngine.SkipCollision = true;

        this._phased = true;
        if (!this._intangible)
            BecomeIntangible();
    }

   private void BecomeIntangible()
    {
        this._intangible = true;

        GameManager.Instance.PreventPausing = true; // if we allow pausing, then our _insideWalls might skip a frame and unphase us in a wall, getting us stuck
        this.Owner.CurrentInputState = PlayerInputState.FoyerInputOnly;
        tk2dBaseSprite sprite = this.Owner.sprite;
        sprite.usesOverrideMaterial = true;
        this._originalShader = sprite.renderer.material.shader;
        sprite.renderer.material.shader = ShaderCache.Acquire("Brave/Internal/HologramShader");
        this.Owner.gameObject.PlayUnique("phase_through_wall_sound");
    }

    private void BecomeTangible()
    {
        this._intangible = false;

        GameManager.Instance.PreventPausing = false;
        this.Owner.CurrentInputState = PlayerInputState.AllInput;
        tk2dBaseSprite sprite = this.Owner.sprite;
        sprite.usesOverrideMaterial = true;
        sprite.renderer.material.shader = this._originalShader;
        this.Owner.gameObject.PlayUnique("phase_through_wall_sound");
    }

    public static float PreventRigidbodyCastDuringHandlePlayerInput(PlayerController pc, float inValue)
    {
        if (pc.HasPassive<AstralProjector>())
            return inValue > 0 ? 999f : -999f; // replace the value we're checking against with something absurdly high so we avoid doing RigidBodyCasts
        return inValue; // return the original value
    }

    [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.HandlePlayerInput))]
    private class HandlePlayerPhasingInputPatch
    {
    // This IL Hook replaces some checks in HandlePlayerInput that do RigidBodyCasts on each axis if the absolutely velocity is greater than 0.01
    // If we have this item, this hook instead checks if each axis has an absolute velocity greater than 999,
    //   ensuring the RigidBodyCasts will never run under any sane circumstance
        [HarmonyILManipulator]
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
    }
}
