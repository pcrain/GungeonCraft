namespace CwaffingTheGungy;

public class BorrowedTime : CwaffActive
{
    public static string ItemName         = "Borrowed Time";
    public static string ShortDescription = "Mafuba";
    public static string LongDescription  = "Captures all non-jammed, non-boss enemies in a room. Using in an empty combat room will release all captured enemies, with a chance for enemies to spawn Jammed. All captured enemies will be forcibly released in boss rooms. Cannot be dropped while enemies are captured.";
    public static string Lore             = "The first Gungeoneer to discover this hourglass believed they had stumbled upon an incomprehensibly powerful artifact, when in fact it was quite the opposite: a dangerous failure of a prototype thrown out and forgotten about by the Sorceress. The poor Gungeoneer couldn't believe their luck as they breezed through room after room, only to reach the Trigger Twins and find themselves fighting far more than the 2 oversized Bullet Kin they signed up for....";

    internal static int _EmptyId;
    internal static int _FullId;
    internal static VFXPool _MafubaVFX = null;

    internal const float _RESPAWN_AS_JAMMED_CHANCE = 0.1f;

    private List<string>     _borrowedEnemies    = new List<string>{};
    private PlayerController _owner              = null;
    private RoomHandler      _lastCheckedRoom    = null;
    private bool             _isBossPresent      = false;
    private bool             _roomCanHaveEnemies = true;

    public static void Init()
    {
        PlayerItem item = Lazy.SetupActive<BorrowedTime>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality    = ItemQuality.C;
        item.AddToShop(ModdedShopType.TimeTrader);

        ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.Timed, 2);
        item.consumable   = false;

        _EmptyId = item.sprite.spriteId;
        _FullId  = item.sprite.collection.GetSpriteIdByName("borrowed_time_full_icon");

        _MafubaVFX = VFX.CreatePoolFromVFXGameObject(Items.MagicLamp.AsGun().DefaultModule.projectiles[0].hitEffects.overrideMidairDeathVFX);
    }

    public override void Pickup(PlayerController player)
    {
        this._owner = player;
        base.Pickup(player);
    }

    public override void OnPreDrop(PlayerController player)
    {
        if (this._roomCanHaveEnemies && this._borrowedEnemies.Count > 0)
            this._owner.StartCoroutine(ReapWhatYouSow(this, this._owner, this._borrowedEnemies));
        this._owner = null;
        base.OnPreDrop(player);
    }

    public override void OnDestroy()
    {
        if (this._owner)
            this._owner.StartCoroutine(ReapWhatYouSow(this, this._owner, this._borrowedEnemies));
        base.OnDestroy();
    }

    public override void Update()
    {
        base.Update();
        if (!this._owner || this._owner.CurrentRoom == this._lastCheckedRoom)
            return;

        RoomHandler room         = this._owner.CurrentRoom;
        this._lastCheckedRoom    = room;
        this._roomCanHaveEnemies = (room != null) && room.EverHadEnemies && !room.area.IsProceduralRoom && (
            room.area.PrototypeRoomCategory == PrototypeDungeonRoom.RoomCategory.NORMAL
            || room.area.PrototypeRoomCategory == PrototypeDungeonRoom.RoomCategory.HUB);
        bool wasBossPresent      = this._isBossPresent;
        this._isBossPresent      = CheckIfBossIsPresent();
        if (this._isBossPresent && !wasBossPresent && this._borrowedEnemies.Count > 0)
            this._owner.StartCoroutine(ReapWhatYouSow(this, this._owner, this._borrowedEnemies));
    }

    public override bool CanBeUsed(PlayerController user)
    {
        return !user.InExitCell && this._roomCanHaveEnemies && base.CanBeUsed(user);
    }

    private static IEnumerator ReapWhatYouSow(BorrowedTime bt, PlayerController reaper, List<string> enemies)
    {
        if (enemies.Count == 0)
            yield break;

        while (GameManager.IsBossIntro)
            yield return null;

        if (!reaper || reaper.CurrentRoom is not RoomHandler room)
        {
            if (!reaper)
                ETGModConsole.Log($"Borrowed Time failed by activating without an owner, tell Captain Pretzel");
            else
                ETGModConsole.Log($"Borrowed Time failed by activating without a valid room, tell Captain Pretzel");
            yield break;
        }

        int enemiesToSpawn = enemies.Count;
        var tpvfx = (ItemHelper.Get(Items.ChestTeleporter) as ChestTeleporterItem).TeleportVFX;
        for (int i = 0; i < enemiesToSpawn; i++)
        {
            IntVector2 bestRewardLocation = room.GetRandomVisibleClearSpot(2, 2);
            AIActor TargetActor = AIActor.Spawn(EnemyDatabase.GetOrLoadByGuid(enemies[i]).aiActor,
                bestRewardLocation, GameManager.Instance.Dungeon.data.GetAbsoluteRoomFromPosition(bestRewardLocation),
                true, AIActor.AwakenAnimationType.Default, true);
            if (UnityEngine.Random.value <= _RESPAWN_AS_JAMMED_CHANCE)
                TargetActor.BecomeBlackPhantom();
            PhysicsEngine.Instance.RegisterOverlappingGhostCollisionExceptions(TargetActor.specRigidbody, null, false);

            reaper.gameObject.Play("Play_OBJ_chestwarp_use_01");
            SpawnManager.SpawnVFX(tpvfx, TargetActor.CenterPosition, Quaternion.identity, true);
            yield return new WaitForSeconds(0.05f);
        }
        if (!room.IsSealed)
        {
            room.SealRoom();
            GameManager.Instance.DungeonMusicController.SwitchToActiveMusic(null);
        }
        if (bt)
        {
            bt._borrowedEnemies.Clear();
            bt.CanBeDropped = true;
            bt.sprite.SetSprite(_EmptyId);
        }
    }

    private bool CheckIfBossIsPresent()
    {
        if (!this._owner || this._owner.CurrentRoom is not RoomHandler room)
            return false;
        return room.SafeGetEnemiesInRoom().Any(enemy => enemy && enemy.healthHaver && enemy.healthHaver.IsBoss);
    }

    public override void DoEffect(PlayerController user)
    {
        // Ineffective in boss rooms
        if (this._isBossPresent)
            return;

        // Ineffective if the room has no active enemies
        RoomHandler curRoom = user.GetAbsoluteParentRoom();
        if (curRoom == null || curRoom != this._lastCheckedRoom)
            return; // this should never happen in theory

        List<AIActor> activeEnemies = curRoom.SafeGetEnemiesInRoom();
        if (activeEnemies.Count == 0)
        {
            if (this._borrowedEnemies.Count > 0 && user.GetAbsoluteParentRoom() != null)
                user.StartCoroutine(ReapWhatYouSow(this, user, this._borrowedEnemies));
            return;
        }

        // Capture enemies for later
        base.sprite.SetSprite(_FullId);
        gameObject.Play("borrowed_time_capture_sound");
        for (int n = activeEnemies.Count - 1; n >= 0; --n)
        {
            AIActor otherEnemy = activeEnemies[n];
            if (!otherEnemy || !otherEnemy.IsHostileAndNotABoss() || otherEnemy.IsBlackPhantom)
                continue;

            Vector2 center = otherEnemy.CenterPosition;
            const int NUM_VFX = 7;
            for (int i = 0; i < NUM_VFX; ++i)
                _MafubaVFX.SpawnAtPosition(
                    (center + (i * 360f / NUM_VFX).ToVector(0.75f)).ToVector3ZisY(-1f), /* -1 = above player sprite */
                    0, null, null, null, -0.05f);

            otherEnemy.EraseFromExistence(true);
            this._borrowedEnemies.Add(otherEnemy.EnemyGuid);
        }
        if (this._borrowedEnemies.Count > 0)
            this.CanBeDropped = false; //cannot be dropped if it contains enemies
    }

    public override void MidGameSerialize(List<object> data)
    {
        base.MidGameSerialize(data);
        data.Add(this._borrowedEnemies.Count);
        foreach (string enemy in this._borrowedEnemies)
            data.Add(enemy);
    }

    public override void MidGameDeserialize(List<object> data)
    {
        base.MidGameDeserialize(data);
        int i = 0;
        int count = (int)data[i++];
        for (int n = 0; n < count; ++n)
            this._borrowedEnemies.Add((string)data[i++]);
        base.sprite.SetSprite((count > 0) ? _FullId : _EmptyId);
    }
}
