namespace CwaffingTheGungy;

public class BorrowedTime : PlayerItem
{
    public static string ItemName         = "Borrowed Time";
    public static string SpritePath       = "borrowed_time_icon";
    public static string ShortDescription = "Mafuba";
    public static string LongDescription  = "Captures all non-jammed, non-boss enemies in a room. Using in an empty combat room will release all captured enemies, with a chance for enemies to spawn Jammed. All captured enemies will be forcibly released in boss rooms. Cannot be dropped while enemies are captured.\n\nThe first Gungeoneer to discover this hourglass believed they had stumbled upon an incomprehensibly powerful artifact, when in fact it was quite the opposite: a dangerous failure of a prototype thrown out and forgotten about by the Sorceress. The poor Gungeoneer couldn't believe their luck as they breezed through room after room, only to reach the Trigger Twins and find themselves fighting far more than the 2 oversized Bullet Kin they signed up for....";

    internal static List<string> _BorrowedEnemies  = new List<string>{};
    internal static int _EmptyId;
    internal static int _FullId;

    internal const float _RESPAWN_AS_JAMMED_CHANCE = 0.1f;

    private PlayerController _owner              = null;
    private RoomHandler      _lastCheckedRoom    = null;
    private bool             _isBossPresent      = false;
    private bool             _roomCanHaveEnemies = true;

    public static void Init()
    {
        PlayerItem item = Lazy.SetupActive<BorrowedTime>(ItemName, SpritePath, ShortDescription, LongDescription);
        item.quality    = PickupObject.ItemQuality.C;
        item.AddToSubShop(ModdedShopType.TimeTrader);

        ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.Timed, 2);
        item.consumable   = false;
        item.CanBeDropped = true;

        _EmptyId = item.sprite.spriteId;
        _FullId  = SpriteBuilder.AddSpriteToCollection(ResMap.Get("borrowed_time_full_icon")[0], item.sprite.Collection);
    }

    public override void Pickup(PlayerController player)
    {
        this._owner = player;
        base.Pickup(player);
    }

    public override void OnPreDrop(PlayerController player)
    {
        if (_BorrowedEnemies.Count > 0)
            this._owner.StartCoroutine(ReapWhatYouSow());
        this._owner = null;
        base.OnPreDrop(player);
    }

    public override void Update()
    {
        base.Update();
        if (!this._owner || this._owner.CurrentRoom == this._lastCheckedRoom)
            return;

        RoomHandler room         = this._owner.CurrentRoom;
        this._lastCheckedRoom    = room;
        this._roomCanHaveEnemies = room.EverHadEnemies && !room.area.IsProceduralRoom && (
            room.area.PrototypeRoomCategory == PrototypeDungeonRoom.RoomCategory.NORMAL
            || room.area.PrototypeRoomCategory == PrototypeDungeonRoom.RoomCategory.HUB);
        bool wasBossPresent      = this._isBossPresent;
        this._isBossPresent      = CheckIfBossIsPresent();
        if (this._isBossPresent && !wasBossPresent)
            this._owner.StartCoroutine(ReapWhatYouSow());
    }

    public override bool CanBeUsed(PlayerController user)
    {
        return !user.InExitCell && this._roomCanHaveEnemies && base.CanBeUsed(user);
    }

    private IEnumerator ReapWhatYouSow()
    {
        while (GameManager.IsBossIntro)
            yield return null;

        if (_BorrowedEnemies.Count == 0)
            yield break;

        int enemiesToSpawn = _BorrowedEnemies.Count;
        var tpvfx = (ItemHelper.Get(Items.ChestTeleporter) as ChestTeleporterItem).TeleportVFX;
        for (int i = 0; i < enemiesToSpawn; i++)
        {
            IntVector2 bestRewardLocation = this._owner.CurrentRoom.GetRandomVisibleClearSpot(2, 2);
            AIActor TargetActor = AIActor.Spawn(EnemyDatabase.GetOrLoadByGuid(_BorrowedEnemies[i]).aiActor,
                bestRewardLocation, GameManager.Instance.Dungeon.data.GetAbsoluteRoomFromPosition(bestRewardLocation),
                true, AIActor.AwakenAnimationType.Default, true);
            if (UnityEngine.Random.value <= _RESPAWN_AS_JAMMED_CHANCE)
                TargetActor.BecomeBlackPhantom();
            PhysicsEngine.Instance.RegisterOverlappingGhostCollisionExceptions(TargetActor.specRigidbody, null, false);

            AkSoundEngine.PostEvent("Play_OBJ_chestwarp_use_01", gameObject);
            SpawnManager.SpawnVFX(tpvfx, TargetActor.sprite.WorldCenter, Quaternion.identity, true);
            yield return new WaitForSeconds(0.05f);
        }
        if (!this._owner.CurrentRoom.IsSealed)
        {
            this._owner.CurrentRoom.SealRoom();
            GameManager.Instance.DungeonMusicController.SwitchToActiveMusic(null);
        }
        _BorrowedEnemies.Clear();
        this.CanBeDropped = true;

        base.sprite.SetSprite(_EmptyId);
    }

    private bool CheckIfBossIsPresent()
    {
        if (_lastCheckedRoom == null)
            return false;
        List<AIActor> activeEnemies =
            this._owner.GetAbsoluteParentRoom().GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
        foreach (AIActor enemy in activeEnemies)
            if (enemy.healthHaver.IsBoss)
                return true;
        return false;
    }

    public override void DoEffect(PlayerController user)
    {
        // Ineffective in boss rooms
        if (this._isBossPresent)
            return;

        // Ineffective if the room has no active enemies
        RoomHandler curRoom = user.GetAbsoluteParentRoom();
        if (curRoom != this._lastCheckedRoom)
            return; // this should never happen in theory

        List<AIActor> activeEnemies = curRoom.GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
        if (activeEnemies == null)
            return;

        if (activeEnemies.Count == 0)
        {
            if (_BorrowedEnemies.Count > 0 && user.GetAbsoluteParentRoom() != null)
                user.StartCoroutine(ReapWhatYouSow());
            return;
        }

        // Capture enemies for later
        base.sprite.SetSprite(_FullId);
        VFXPool vfx = VFX.CreatePoolFromVFXGameObject((ItemHelper.Get(Items.MagicLamp) as Gun
            ).DefaultModule.projectiles[0].hitEffects.overrideMidairDeathVFX);
        AkSoundEngine.PostEvent("borrowed_time_capture_sound", gameObject);
        foreach (AIActor otherEnemy in activeEnemies)
        {
            if (!otherEnemy.IsHostileAndNotABoss() || otherEnemy.IsBlackPhantom)
                continue;

            Vector2 center = otherEnemy.sprite.WorldCenter;
            const int NUM_VFX = 7;
            for (int i = 0; i < NUM_VFX; ++i)
                vfx.SpawnAtPosition(
                    (center + (i * 360f / NUM_VFX).ToVector(0.75f)).ToVector3ZisY(-1f), /* -1 = above player sprite */
                    0, null, null, null, -0.05f);

            otherEnemy.EraseFromExistence(true);
            _BorrowedEnemies.Add(otherEnemy.EnemyGuid);
        }
        if (_BorrowedEnemies.Count > 0)
            this.CanBeDropped = false; //cannot be dropped if it contains enemies
    }
}
